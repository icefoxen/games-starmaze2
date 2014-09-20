#!/usr/bin/env python

import os
import random
import time

#import lepton
import pyglet
import pyglet.window.key as key
from pyglet.gl import *
import pymunk
from pymunk import Vec2d

from actor import *
#import physics
import shader
from terrain import *

from particle import *

import renderer

import zone_beginnings

DISPLAY_FPS = True
PHYSICS_FPS = 60.0
GRAVITY_FORCE = -400.0

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
        s.renderManager = renderer.RenderManager()
        
        s.player = Player(s, s.keyboard)
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

        s.particleGroup = ParticleGroup()
        s.particleController = ParticleController()
        s.particleRenderer = ParticleRenderer()
        s.particleEmitter = ParticleEmitter()

    def initNewSpace(s):
        s.space = pymunk.Space()
        # XXX: This isn't QUITE the same as a max velocity, but prevents
        # motion from getting _too_ out of control.
        s.space.damping = 0.9
        s.space.gravity = (0.0, GRAVITY_FORCE)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_COLLECTABLE,
                                      begin=World.handleCollision,
                                      separate=World.handleCollisionEnd)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_TERRAIN,
                                      begin=World.handleCollision,
                                      separate=World.handleCollisionEnd)
        s.space.add_collision_handler(CGROUP_PLAYERBULLET, CGROUP_TERRAIN,
                                      begin=World.handleCollision,
                                      separate=World.handleCollisionEnd)
        s.space.add_collision_handler(CGROUP_PLAYERBULLET, CGROUP_ENEMY,
                                      begin=World.handleCollision,
                                      separate=World.handleCollisionEnd)
        s.space.add_collision_handler(CGROUP_ENEMYBULLET, CGROUP_TERRAIN,
                                      begin=World.handleCollision,
                                      separate=World.handleCollisionEnd)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_DOOR,
                                      begin=World.handleCollision,
                                      separate=World.handleCollisionEnd)

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
        r = rcache.getRenderer(renderer.LineSpriteRenderer)
        s.renderManager.add(r, act)

    def _removeActor(s, act):
        s.actors.remove(act)
        # Break backlinks
        # TODO: This should break all backlinks in an actor's
        # components, too.  Or the actor should have a delete
        # method that gets called here.  Probably the best way.
        act.world = None
        s.removeActorFromSpace(act)
        r = rcache.getRenderer(renderer.LineSpriteRenderer)
        s.renderManager.remove(r, act)

    def addActorToSpace(s, act):
        if not act.physicsObj.is_static:
            s.space.add(act.physicsObj.body)
        for b in act.physicsObj.auxBodys:
           if not b.is_static:
               s.space.add(b)
        for constraint in act.physicsObj.constraints:
           s.space.add(constraint)
        for shape in act.physicsObj.shapes:
           s.space.add(shape)

    def removeActorFromSpace(s, act):                
        if not act.physicsObj.is_static:
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
        for _ in xrange(int(s.physicsSteps)):
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

        # Update the particle system.
        #lepton.default_system.update(dt)
        #s.particleEmitter.update(s.particleGroup, dt)
        #s.particleController.update(s.particleGroup, dt)
            
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
            s.renderManager.render()
            #with s.shader:
            #    for act in s.actors:
            #        s.resetShaderDefaults(act)
            #        act.draw(s.shader)
            #lepton.default_system.draw()
        s.fps_display.draw()

        s.particleRenderer.draw(s.particleGroup)

    def on_key_press(s, k, modifiers):
        s.player.controller.handleKeyPress(k, modifiers)

    def on_key_release(s, k, modifiers):
        s.player.controller.handleKeyRelease(k, modifiers)

    @staticmethod
    def handleCollision(space, arbiter, *args, **kwargs):
        shape1, shape2 = arbiter.shapes
        physicsObj1 = shape1.body.component
        physicsObj2 = shape2.body.component
        r1 = physicsObj1.startCollisionWith(physicsObj2, arbiter)
        r2 = physicsObj2.startCollisionWith(physicsObj1, arbiter)
        # XXX: Is this right?
        return r1 and r2

    @staticmethod
    def handleCollisionEnd(space, arbiter, *args, **kwargs):
        shape1, shape2 = arbiter.shapes
        physObj1 = shape1.body.component
        physObj2 = shape2.body.component
        physObj1.endCollisionWith(physObj2, arbiter)
        physObj2.endCollisionWith(physObj1, arbiter)

    def report(s):
        "Print out misc useful about the state of the world."
        print "Particle count: {}".format(len(s.particleGroup.particles))


def main():
    screenw = 1024
    screenh = 768

    world = World(screenw, screenh)

    pyglet.clock.schedule_interval(lambda dt: world.update(dt), 1.0/PHYSICS_FPS)
    pyglet.clock.schedule_interval(lambda dt: world.report(), 2.0)
    pyglet.app.run()

if __name__ == '__main__':
    #import cProfile
    #cProfile.run('main()')
    main()
