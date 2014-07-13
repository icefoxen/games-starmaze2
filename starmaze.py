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

DISPLAY_FPS = True
PHYSICS_FPS = 60.0


vprog = '''#version 120
// That's opengl 2.1
// WHICH I GUESS WE'RE USING CAUSE I CAN'T FIND DOCS ON ANYTHING ELSE
// AND WE GOTTA AIM AT THE LOWEST COMMON DENOMINATOR ANYWAY
// BECAUSE COMPUTERS SUCK AND I HATE THEM.

// Vertex shader


uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
 
uniform vec4 inp;
 
void main(void) {
//	gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
   //gl_Position = ftransform() + inp;
   //gl_Position = ftransform();
   gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex + inp;
   //gl_PointSize = 100;
   gl_FrontColor = gl_Color;
}

'''

fprog = '''#version 120
// Fragment shader

uniform sampler2D tex;

void main() {
   gl_FragColor = vec4(0, 0, 0.8, 1);
   //gl_FragColor = gl_Color;
   //gl_FragColor = vec4(1,0,1,1);
   //gl_FragColor = texture2D(tex, gl_TexCoord[0].st);
}
'''



class World(object):
    """Contains all the state for the game."""
    def __init__(s, screenw, screenh):
        s.window = pyglet.window.Window(width=screenw, height=screenh)
        s.window.set_vsync(True)
        #pyglet.clock.set_fps_limit(10)
        print s.window.vsync
        s.screenw = screenw
        s.screenh = screenh
        s.fps_display = pyglet.clock.ClockDisplay()

        s.physicsSteps = 30.0

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
        )

        #s.setupWorld()

        s.initNewSpace()

        s.player = Player(s.keyboard)
        s.camera = Camera(s.player.physicsObj.body, s.screenw, s.screenh)
        s.actors = set()
        s.newActors = set()
        s._addActor(s.player)

        s.currentRoom = makeSomeRoom()
        s.enterRoom(s.currentRoom)


    def initNewSpace(s):
        s.space = pymunk.Space()
        s.space.gravity = (0.0, -400.0)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_COLLECTABLE,
            begin=World.collidePlayerCollectable
        )

    def birthActor(s, act):
        """You see, we can't have actors add or remove other actors inside
their update() method, 'cause that'd modify the set of actors while we're
iterating through it, which is a no-no.

So instead of calling addActor directly, call this, which will cause the
actor to be added next update frame."""
        s.newActors.add(act)

    def killActor(s, act):
        """The complement to birthActor(), kills the given actor so it gets removed next
update frame."""
        act.alive = False

    def _addActor(s, act):
        s.actors.add(act)
        s.space.add(act.physicsObj.shapes)
        if not act.physicsObj.body.is_static:
            s.space.add(act.physicsObj.body)
        act.world = s

    def _removeActor(s, act):
        s.actors.remove(act)
        s.space.remove(act.physicsObj.shapes)
        if not act.physicsObj.body.is_static:
            s.space.remove(act.physicsObj.body)
        # Break backlinks
        act.body.actor = None
        act.world = None


    def enterRoom(s, room):
        """Actually creates all the game objects for the given room and adds them to the current state."""
        actors = room.getActors()
        for act in actors:
            s._addActor(act)

    def leaveRoom(s):
        """Removes all the game objects in the current state (sans player) and preps for a new room."""
        for act in list(s.actors):
            s._removeActor(act)
        s._addActor(s.player)

    def update(s, dt):
        #print 'foo'
        #s.player.handleInput(s.keyboard)
        step = dt / s.physicsSteps
        for _ in range(int(s.physicsSteps)):
            s.space.step(step)
        s.camera.update(dt)
        for act in s.actors:
            act.update(dt)
        deadActors = [act for act in s.actors if not act.alive]
        for act in deadActors:
            act.onDeath()
            s._removeActor(act)

    def on_draw(s):
        s.window.clear()
        with s.camera:
            for act in s.actors:
                act.draw()

        s.fps_display.draw()

    def on_key_press(s, k, modifiers):
        s.player.controller.handleInputEvent(k, modifiers)

    @staticmethod
    def collidePlayerCollectable(space, arbiter, *args, **kwargs):
        "The handler for a player collecting a Collectable."
        #print space, arbiter, args, kwargs
        playerShape, collectableShape = arbiter.shapes
        player = playerShape.body.actor
        collectable = collectableShape.body.actor
        collectable.collect(player)
        collectable.alive = False
        return False




def main():
    screenw = 1024
    screenh = 768

    world = World(screenw, screenh)

    pyglet.clock.schedule_interval(lambda dt: world.update(dt), 1.0/PHYSICS_FPS)
    pyglet.app.run()

if __name__ == '__main__':
    main()
