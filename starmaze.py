#!/usr/bin/env python

import collections
import os
import random
import time

#import lepton
import pyglet
pyglet.options['shadow_window'] = False
import pyglet.window.key as key
from pyglet.gl import *
import pymunk
from pymunk import Vec2d

from actor import *
#import physics
import shader
from terrain import *

#from particle import *

import renderer


PHYSICS_FPS = 60.0
GRAVITY_FORCE = -400.0

class World(object):
    """Contains all the state for the game."""
    def __init__(self, screenw, screenh):
        self.window = pyglet.window.Window(width=screenw, height=screenh)
        self.window.set_vsync(True)
        #pyglet.clock.set_fps_limit(10)
        #print self.window.vsync
        self.screenw = screenw
        self.screenh = screenh
        self.fps_display = pyglet.clock.ClockDisplay()

        self.physicsSteps = 10.0

        self.keyboard = key.KeyStateHandler()

        # Apparently you can't have two handlers in the
        # same handler stack that intercept the same events,
        # in this case, key presses.  So we use multiple levels
        # of the stack and let the events propegate down.
        self.window.push_handlers(self.keyboard)
        self.window.push_handlers(
            on_draw = lambda: self.on_draw(),
            on_key_press = lambda k, mods: self.on_key_press(k, mods),
            on_key_release = lambda k, mods: self.on_key_release(k, mods),
            on_close = lambda: self.on_close()
        )

        self.initNewSpace()
        self.renderManager = renderer.RenderManager(self.screenw, self.screenh)
        renderer.preloadRenderers()
        
        self.player = Player(self.keyboard)
        self.gui = GUI(self.player)
        self.camera = Camera(self.player.physicsObj, self.screenw, self.screenh)
        self.actors = set()
        self.actorsToAdd = set()
        self.actorsToRemove = set()

        import zone_beginnings
        self.zones = {"Beginnings" : zone_beginnings.theZone}
        self.currentZone = None
        self.currentRoom = None
        self.nextRoom = self.zones["Beginnings"].rooms["Entryway"]
        self.nextRoomLoc = (0.0, 0.0)
        # The player is automatically added to the room here.
        self.enterNextRoom()

        self.time = 0.0

        #self.particleGroup = ParticleGroup()
        #self.particleController = ParticleController()
        #self.particleRenderer = ParticleRenderer()
        #self.particleEmitter = ParticleEmitter()

        self.frameTimes = collections.defaultdict(lambda: 0)

    def initNewSpace(self):
        self.space = pymunk.Space()
        # XXX: This isn't QUITE the same as a max velocity, but prevents
        # motion from getting _too_ out of control.
        self.space.damping = 0.9
        self.space.gravity = (0.0, GRAVITY_FORCE)
        self.space.add_collision_handler(CGROUP_NONE, CGROUP_NONE,
                                      begin=World.handleCollision,
                                      separate=World.handleCollisionEnd)

    def addActor(self, act):
        """You see, we can't have actors add or remove other actors inside
their update() method, 'cause that'd modify the set of actors while we're
iterating through it, which is a no-no.

So instead of calling _addActor directly, call this, which will cause the
actor to be added next update frame."""
        self.actorsToAdd.add(act)

    def addActors(self, act):
        """Add a collection of actors."""
        for a in act:
            self.addActor(a)

    def removeActor(self, act):
        """The complement to addActor(), sets its so te given actor gets removed next
update frame."""
        self.actorsToRemove.add(act)

    def _addActor(self, act):
        self.actors.add(act)
        act.world = self
        self.addActorToSpace(act)
        if act.renderer is not None:
            self.renderManager.add(act.renderer, act)

    def _removeActor(self, act):
        self.actors.remove(act)
        # Break backlinks
        # TODO: This should break all backlinks in an actor's
        # components, too.  Or the actor should have a delete
        # method that gets called here.  Probably the best way.
        act.world = None
        self.removeActorFromSpace(act)
        if act.renderer is not None:
            self.renderManager.remove(act.renderer, act)

    def addActorToSpace(self, act):
        if not act.physicsObj.is_static:
            self.space.add(act.physicsObj.body)
        for b in act.physicsObj.auxBodys:
           if not b.is_static:
               self.space.add(b)
        for constraint in act.physicsObj.constraints:
           self.space.add(constraint)
        for shape in act.physicsObj.shapes:
           self.space.add(shape)

    def removeActorFromSpace(self, act):                
        if not act.physicsObj.is_static:
            self.space.remove(act.physicsObj.body)
        for b in act.physicsObj.auxBodys:
            if not b.is_static:
                self.space.remove(b)
        for constraint in act.physicsObj.constraints:
            self.space.remove(constraint)
        for shape in act.physicsObj.shapes:
            self.space.remove(shape)


    def enterGate(self, gate):
        self.nextRoom = self.currentZone.rooms[gate.destination]
        self.nextRoomLoc = (gate.destx, gate.desty)

    def enterNextRoom(self):
        """Actually creates all the game objects for the given room and adds them to the current state."""
        self.clearRoom()
        room = self.nextRoom
        self.currentRoom = room
        self.currentZone = self.currentRoom.zone
        print "Entering {} in zone {}".format(room.name, room.zone.name)
        self.addActors(room.getActors())
        self.addActors(room.zone.getZoneActors())
        locx, locy = self.nextRoomLoc
        self.addActor(self.player)
        self.addActor(self.gui)
        self.player.physicsObj.position = self.nextRoomLoc
        self.camera.snapTo(self.nextRoomLoc)
        self.nextRoom = None

    def clearRoom(self):
        """Removes all the game objects in the current state and preps for a new room."""
        for act in list(self.actors):
            self._removeActor(act)


    def update(self, dt):
        step = dt / self.physicsSteps
        for _ in xrange(int(self.physicsSteps)):
            self.space.step(step)
        self.camera.update(dt)

        for act in self.actors:
            act.update(dt)
        
        for act in self.actorsToAdd:
            self._addActor(act)
        self.actorsToAdd.clear()
        
        deadActors = {act for act in self.actors if not act.alive}
        for act in deadActors:
            act.onDeath()
        self.actorsToRemove.update(deadActors)
        for act in self.actorsToRemove:
            self._removeActor(act)
        self.actorsToRemove.clear()

        # Check if player is dead, 
        if not self.player.alive:
            self.window.close()

        # Shit gets a little whack if we try to remove
        # a bunch of actors and add a bunch of new ones
        # _while updating the actors_.
        # Doing it all at once at the end of the frame
        # makes iterating through them all easier, and
        # also means it's technically deterministic.
        if self.nextRoom is not None:
            self.enterNextRoom()

        # Update the particle system.
        #self.particleEmitter.update(self.particleGroup, dt)
        #self.particleController.update(self.particleGroup, dt)
            
        self.time += dt

        roundedTime = round(dt, 3)
        self.frameTimes[roundedTime] += 1

    def reportStats(self):
        # Not really a good way of getting memory used by program...
        import resource
        usage = resource.getrusage(resource.RUSAGE_SELF)
        rss = usage.ru_maxrss / 1024
        print "Currently holding {} kb allocated".format(rss)

        
    def on_draw(self):
        self.window.clear()
        #with self.camera:
        #    self.renderManager.render()
        self.renderManager.render(self.camera)
        self.fps_display.draw()

        #self.particleRenderer.draw(self.particleGroup)

    def on_key_press(self, k, modifiers):
        self.player.controller.handleKeyPress(k, modifiers)

    def on_key_release(self, k, modifiers):
        self.player.controller.handleKeyRelease(k, modifiers)

    @staticmethod
    def handleCollision(space, arbiter, *args, **kwargs):
        shape1, shape2 = arbiter.shapes
        physicsObj1 = shape1.body.component
        physicsObj2 = shape2.body.component
        r1 = physicsObj1.startCollisionWith(physicsObj2, arbiter)
        r2 = physicsObj2.startCollisionWith(physicsObj1, arbiter)
        #print "{} collided with {}, results: {} {}".format(physicsObj1, physicsObj2, r1, r2)
        # XXX: I THINK this is right, a physics-collision only happens
        # if both objects agree that it should.
        # The default is 'yes', so either object can change that.
        return r1 and r2

    @staticmethod
    def handleCollisionEnd(space, arbiter, *args, **kwargs):
        shape1, shape2 = arbiter.shapes
        physObj1 = shape1.body.component
        physObj2 = shape2.body.component
        physObj1.endCollisionWith(physObj2, arbiter)
        physObj2.endCollisionWith(physObj1, arbiter)

    def report(self):
        "Print out misc useful about the state of the world."
        #print "Particle count: {}".format(len(self.particleGroup.particles))
        pass

    def on_close(self):
        items = self.frameTimes.items()
        items.sort()
        print "Frame latencies:"
        print "Seconds\tNumber\tFraction"
        totalFrames = sum(num for time, num in items)
        for time, num in items:
            percentage = float(num) / totalFrames
            print "{}\t{}\t{:0.2f}".format(time, num, percentage)
        print "Total frames: {}".format(totalFrames)

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
