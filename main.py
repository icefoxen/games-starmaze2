#!/usr/bin/env python

import os

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

shader_on = False


class World(object):
    """Contains all the state for the game."""
    def __init__(s, screenw, screenh):
        s.window = pyglet.window.Window(width=screenw, height=screenh)
        s.screenw = screenw
        s.screenh = screenh
        s.fps_display = pyglet.clock.ClockDisplay()

        s.physicsSteps = 30.0

        s.shader = Shader([vprog], [fprog])

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
            #on_key_release = lambda k, mods: s.on_key_release(k, mods)
        )
        #s.window.event.on_draw = lambda: s.on_draw()
        #s.window.event.on_keypress = lambda k, mods: s.on_keypress(k, mods)

        s.setupWorld()

        
    def setupWorld(s):
        s.room = Room()
        colors = [(0, 0, 255, 255), (0, 255, 0, 255), 
                  (255, 0, 0, 255), (255, 255, 255, 255),
                  (255, 0, 0, 255), (255, 0, 0, 255),
                  (255, 255, 0, 255), (255, 255, 0, 255),
                  ]
        b1 = createBlock(330, 100, 570, 30)
        b2 = createBlock(300, 100, 30, 300)
        b3 = createBlock(800, 100, 30, 300)
        b4 = createBlock(300, 200, 270, 30)
        s.room.addTerrain(b1)
        s.room.addTerrain(b2)
        s.room.addTerrain(b3)
        s.room.addTerrain(b4)

        s.player = Player(s.keyboard)
        s.player.position = (s.screenw / 2, s.screenh / 2)
        s.room.addActor(s.player)
        c = Collectable()
        c.position = (s.screenw / 3, s.screenh)
        s.room.addActor(c)
        s.camera = Camera(s.player, s.screenw, s.screenh)


    def update(s, dt):
        #s.player.handleInput(s.keyboard)
        step = dt / s.physicsSteps
        for _ in range(int(s.physicsSteps)):
            s.room.update(step)
        s.camera.update(dt)

    def on_draw(s):
        s.window.clear()
        with s.camera:
            if shader_on:
                with s.shader:
                    # X, Y, Z, scale
                    s.shader.uniformf("inp", 0.0, 0.0, 0.0, 0.0)
                    s.room.draw()
            else:
                s.room.draw()
                #pymunk.pyglet_util.draw(s.room.space)

        s.fps_display.draw()

    def on_key_press(s, k, modifiers):
        s.player.handleInputEvent(k, modifiers)
        #return False


def main():
    screenw = 1024
    screenh = 768

    world = World(screenw, screenh)

    pyglet.clock.schedule_interval(lambda dt: world.update(dt), 1.0/PHYSICS_FPS)
    pyglet.app.run()


    ##window = pyglet.window.Window(width=screenw, height=screenh)

    #fps_display = pyglet.clock.ClockDisplay()

    #shader = Shader([vprog], [fprog])

    #camera = Camera(a, screenw, screenh)

    #def update(dt):
    #    step = dt / PHYSICS_STEPS
    #    for _ in range(int(PHYSICS_STEPS)):
    #        room.update(step)
    #    x, y = a.body.position
    #    camera.update(dt)
    #    #room.focusOn(x - (screenw / 2), y - (screenh / 2))


if __name__ == '__main__':
    main()
