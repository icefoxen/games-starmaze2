#!/usr/bin/env python

import os
import random
import time

import pyglet
import pyglet.window.key as key
from pyglet.gl import *
import pymunk
from pymunk import Vec2d

from actor import *
from shader import *
from terrain import *

import zone_beginnings

DISPLAY_FPS = True
PHYSICS_FPS = 60.0
GRAVITY_FORCE = -400

class World(object):
    """Contains all the state for the game."""
    def __init__(s, screenw, screenh):
        s.window = pyglet.window.Window(width=screenw, height=screenh)
        s.window.set_vsync(True)
        #pyglet.clock.set_fps_limit(10)
        #print s.window.vsync
        s.screenw = screenw
        s.screenh = screenh
        s.fps_display = pyglet.clock.ClockDisplay()

        s.physicsSteps = 10.0

        #s.shader = Shader([vprog], [fprog])

        s.keyboard = key.KeyStateHandler()

        # Apparently you can't have two handlers in the
        # same handler stack that intercept the same events,
        # in this case, key presses.  So we use multiple levels
        # of the stack and let the events propegate down.
        s.window.push_handlers(s.keyboard)
        s.window.push_handlers(
            #s.keyboard,
            on_draw = lambda: s.on_draw(),
            on_key_press = lambda k, mods: s.on_key_press(k, mods),
            on_key_release = lambda k, mods: s.on_key_release(k, mods)
        )

        s.initNewSpace()

        s.player = Player(s.keyboard)
        s.camera = Camera(s.player.physicsObj, s.screenw, s.screenh)
        s.actors = set()
        s.newActors = set()
        s.birthActor(s.player)

        s.createWorld()
        s.currentRoom = s.rooms['Entryway']
        s.enterRoom(s.currentRoom)

    def initNewSpace(s):
        s.space = pymunk.Space()
        # XXX: This isn't QUITE the same as a max velocity, but prevents
        # motion from getting _too_ out of control.
        s.space.damping = 0.9
        s.space.gravity = (0.0, GRAVITY_FORCE)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_COLLECTABLE,
                                      begin=World.collidePlayerCollectable)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_TERRAIN,
                                      begin=World.collidePlayerTerrain)
        s.space.add_collision_handler(CGROUP_PLAYERBULLET, CGROUP_TERRAIN,
                                      begin=World.collideBulletTerrain)
        s.space.add_collision_handler(CGROUP_PLAYERBULLET, CGROUP_ENEMY,
                                      begin=World.collidePlayerBulletEnemy)
        s.space.add_collision_handler(CGROUP_ENEMYBULLET, CGROUP_TERRAIN,
                                      begin=World.collideBulletTerrain)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_DOOR,
                                      begin=World.collidePlayerDoor,
                                      separate=World.collidePlayerDoorEnd)

    def createWorld(s):
        s.rooms = {}
        zones = [zone_beginnings]
        for zone in zones:
            for room in zone.generateZone():
                s.rooms[room.name] = room
                
        
    def birthActor(s, act):
        """You see, we can't have actors add or remove other actors inside
their update() method, 'cause that'd modify the set of actors while we're
iterating through it, which is a no-no.

So instead of calling _addActor directly, call this, which will cause the
actor to be added next update frame."""
        s.newActors.add(act)

    def killActor(s, act):
        """The complement to birthActor(), kills the given actor so it gets removed next
update frame."""
        act.alive = False


    def _addActor(s, act):
        s.actors.add(act)        
        act.world = s

        if not act.physicsObj.body.is_static:
            s.space.add(act.physicsObj.body)
        for b in act.physicsObj.auxBodys:
            if not b.is_static:
                s.space.add(b)
        for constraint in act.physicsObj.constraints:
            s.space.add(constraint)
        for shape in act.physicsObj.shapes:
            s.space.add(shape)

    def _removeActor(s, act):
        s.actors.remove(act)
        # Break backlinks
        # TODO: This should break all backlinks in an actor's
        # components, too.  Or the actor should have a delete
        # method that gets called here.  Probably the best way.
        act.world = None

        if not act.physicsObj.body.is_static:
            s.space.remove(act.physicsObj.body)
        for b in act.physicsObj.auxBodys:
            if not b.is_static:
                s.space.remove(b)
        for constraint in act.physicsObj.constraints:
            s.space.remove(constraint)
        for shape in act.physicsObj.shapes:
            s.space.remove(shape)



    def enterDoor(s, door):
        s.leaveRoom()
        s.enterRoom(s.rooms[door.destination])
        s.player.physicsObj.position = (door.destx, door.desty)
        s.camera.snapTo(door.destx, door.desty)

    def enterRoom(s, room):
        """Actually creates all the game objects for the given room and adds them to the current state."""
        print "Entering", room.name
        actors = room.getActors()
        for act in actors:
            s.birthActor(act)

    # BUGGO: This doesn't work right!
    # Mainly because the onDeath methods of said actors
    # get triggered...  :-(
    def leaveRoom(s):
        """Removes all the game objects in the current state (sans player) and preps for a new room."""
        for act in list(s.actors):
            s.killActor(act)
        s.player.alive = True
        #s.birthActor(s.player)

    def update(s, dt):
        #print 'foo'
        #s.player.handleInput(s.keyboard)
        step = dt / s.physicsSteps
        for _ in range(int(s.physicsSteps)):
            s.space.step(step)
        s.camera.update(dt)
        
        for act in s.newActors:
            s.actors.add(act)
            s._addActor(act)
        s.newActors.clear()
        for act in s.actors:
            act.update(dt)
        deadActors = [act for act in s.actors if not act.alive]
        for act in deadActors:
            act.onDeath(s)
            s._removeActor(act)

    def on_draw(s):
        s.window.clear()
        with s.camera:
            with DEFAULT_SHADER:
                for act in s.actors:
                    DEFAULT_SHADER.uniformi("facing", act.facing)
                    DEFAULT_SHADER.uniformf("vertexDiff", 0, 0, 0, 0)
                    DEFAULT_SHADER.uniformf("colorDiff", 0, 0, 0, 0)
                    act.draw(DEFAULT_SHADER)
        s.fps_display.draw()

    def on_key_press(s, k, modifiers):
        s.player.controller.handleKeyPress(k, modifiers)

    def on_key_release(s, k, modifiers):
        s.player.controller.handleKeyRelease(k, modifiers)

    @staticmethod
    def collidePlayerCollectable(space, arbiter, *args, **kwargs):
        "The handler for a player collecting a Collectable."
        #print space, arbiter, args, kwargs
        playerShape, collectableShape = arbiter.shapes
        player = playerShape.body.component.owner
        collectable = collectableShape.body.component.owner
        collectable.collect(player)
        collectable.alive = False
        return False

    @staticmethod
    def collidePlayerTerrain(space, arbiter, *args, **kwargs):
        playerShape, _ = arbiter.shapes
        player = playerShape.body.component.owner
        for c in arbiter.contacts:
            normal = c.normal
            # This is not exactly 0 because floating point error
            # means a lot of the time a horizontal collision has
            # a vertical component of like -1.0e-15
            # But in general, if we hit something moving downward,
            # the y component of the normal is < 0
            #
            # TODO: Oooh, we should probably see if there's a
            # callback for when two things _stop_ colliding with each other,
            # I think there is.  Setting onGround to false in such a callback
            # would be a good thing
            if normal.y < -0.001:
                player.onGround = True
        return True

    @staticmethod
    def collideBulletTerrain(space, arbiter, *args, **kwargs):
        bulletShape, collectableShape = arbiter.shapes
        bullet = bulletShape.body.component.owner
        bullet.alive = False
        return False

    @staticmethod
    def collidePlayerBulletEnemy(space, arbiter, *args, **kwargs):
        bulletShape, enemyShape = arbiter.shapes
        bullet = bulletShape.body.component.owner
        bullet.alive = False
        enemy = enemyShape.body.component.owner
        enemy.takeDamage(bullet, bullet.damage)
        return False

    @staticmethod
    def collidePlayerDoor(space, arbiter, *args, **kwargs):
        playerShape, doorShape = arbiter.shapes
        player = playerShape.body.component.owner
        door = doorShape.body.component.owner
        player.door = door
        return False

    @staticmethod
    def collidePlayerDoorEnd(space, arbiter, *args, **kwargs):
        playerShape, _ = arbiter.shapes
        player = playerShape.body.component.owner
        player.door = None



def main():
    screenw = 1024
    screenh = 768

    world = World(screenw, screenh)

    pyglet.clock.schedule_interval(lambda dt: world.update(dt), 1.0/PHYSICS_FPS)
    pyglet.app.run()

if __name__ == '__main__':
    main()
