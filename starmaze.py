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
import shader
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
        s.actorsToAdd = set()
        s.actorsToRemove = set()

        s.createWorld()
        s.currentRoom = None
        s.nextRoom = s.rooms['Arena']
        s.nextRoomLoc = (0.0, 0.0)
        # The player is automatically added to the room here.
        s.enterNextRoom()

        s.time = 0.0

        s.shader = shader.Shader([shader.vprog], [shader.fprog])

    def initNewSpace(s):
        s.space = pymunk.Space()
        # XXX: This isn't QUITE the same as a max velocity, but prevents
        # motion from getting _too_ out of control.
        s.space.damping = 0.9
        s.space.gravity = (0.0, GRAVITY_FORCE)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_COLLECTABLE,
                                      begin=World.collidePlayerCollectable)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_TERRAIN,
                                      begin=World.collidePlayerTerrain,
                                      separate=World.collidePlayerTerrainEnd)
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
                
        
    def addActor(s, act):
        """You see, we can't have actors add or remove other actors inside
their update() method, 'cause that'd modify the set of actors while we're
iterating through it, which is a no-no.

So instead of calling _addActor directly, call this, which will cause the
actor to be added next update frame."""
        s.actorsToAdd.add(act)

    def removeActor(s, act):
        """The complement to addActor(), sets its so te given actor gets removed next
update frame."""
        s.actorsToRemove.add(act)

    def _addActor(s, act):
        s.actors.add(act)
        act.world = s
        s.addActorToSpace(act)

    def _removeActor(s, act):
        s.actors.remove(act)
        # Break backlinks
        # TODO: This should break all backlinks in an actor's
        # components, too.  Or the actor should have a delete
        # method that gets called here.  Probably the best way.
        act.world = None
        s.removeActorFromSpace(act)

    def addActorToSpace(s, act):
        if not act.physicsObj.body.is_static:
            s.space.add(act.physicsObj.body)
        for b in act.physicsObj.auxBodys:
            if not b.is_static:
                s.space.add(b)
        for constraint in act.physicsObj.constraints:
            s.space.add(constraint)
        for shape in act.physicsObj.shapes:
            s.space.add(shape)

    def removeActorFromSpace(s, act):                
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
        s.nextRoom = s.rooms[door.destination]
        s.nextRoomLoc = (door.destx, door.desty)

    def enterNextRoom(s):
        """Actually creates all the game objects for the given room and adds them to the current state."""
        s.clearRoom()
        room = s.nextRoom
        s.currentRoom = room
        print "Entering", room.name
        actors = room.getActors()
        for act in actors:
            s.addActor(act)
        locx, locy = s.nextRoomLoc
        s.addActor(s.player)
        s.player.physicsObj.position = s.nextRoomLoc
        s.camera.snapTo(s.nextRoomLoc)
        s.nextRoom = None

    def clearRoom(s):
        """Removes all the game objects in the current state and preps for a new room."""
        for act in list(s.actors):
            s._removeActor(act)


    def update(s, dt):
        step = dt / s.physicsSteps
        for _ in range(int(s.physicsSteps)):
            s.space.step(step)
        s.camera.update(dt)

        for act in s.actors:
            act.update(dt)
        
        for act in s.actorsToAdd:
            s._addActor(act)
        s.actorsToAdd.clear()
        
        deadActors = {act for act in s.actors if not act.alive}
        for act in deadActors:
            act.onDeath()
        s.actorsToRemove.update(deadActors)
        for act in s.actorsToRemove:
            s._removeActor(act)
        s.actorsToRemove.clear()

        # Shit gets a little whack if we try to remove
        # a bunch of actors and add a bunch of new ones
        # _while updating the actors_.
        # Doing it all at once at the end of the frame
        # makes iterating through them all easier, and
        # also means it's technically deterministic.
        if s.nextRoom is not None:
            s.enterNextRoom()

        s.time += dt

    def reportStats(s):
        # Not really a good way of getting memory used by program...
        import resource
        usage = resource.getrusage(resource.RUSAGE_SELF)
        rss = usage.ru_maxrss / 1024
        print "Currently holding {} kb allocated".format(rss)

    def resetShaderDefaults(s, act):
        s.shader.uniformi("facing", act.facing)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        s.shader.uniformf("colorDiff", 0, 0, 0, 0)
        s.shader.uniformf("alpha", 1.0)
        
    def on_draw(s):
        s.window.clear()
        with s.camera:
            with s.shader:
                for act in s.actors:
                    s.resetShaderDefaults(act)
                    act.draw(s.shader)
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
    def collidePlayerTerrainEnd(space, arbiter, *args, **kwargs):
        playerShape, _ = arbiter.shapes
        player = playerShape.body.component.owner
        player.onGround = False
    

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
        enemy.life.takeDamage(bullet, bullet.damage)
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
