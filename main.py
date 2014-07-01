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
PHYSICS_STEPS = 10.0


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
   //gl_FragColor = vec4(0, 0, 0.8, 1);
   gl_FragColor = gl_Color;
   //gl_FragColor = vec4(1,0,1,1);
   //gl_FragColor = texture2D(tex, gl_TexCoord[0].st);
}
'''

shader_on = False


def main():
    screenw = 1024
    screenh = 768

    window = pyglet.window.Window(width=screenw, height=screenh)

    fps_display = pyglet.clock.ClockDisplay()

    room = Room()
    b1 = createBlock(300, 100, 300, 5)
    b2 = createBlock(300, 100, 5, 300)
    b3 = createBlock(600, 100, 5, 300)
    room.addTerrain(b1)
    room.addTerrain(b2)
    room.addTerrain(b3)

    a = Actor(screenw / 2, screenh / 2)
    room.addActor(a)

    def update(dt):
        step = dt / PHYSICS_STEPS
        for _ in range(int(PHYSICS_STEPS)):
            room.update(step)

    shader = Shader([vprog], [fprog])

    @window.event
    def on_draw():
        #glLineWidth(3)
        window.clear()
        if shader_on:
            with shader:
                # X, Y, Z, scale
                shader.uniformf("inp", 0.0, 0.0, 0.0, 0.0)
                room.draw()
                a.draw()
        else:
            room.draw()
            a.draw()

        fps_display.draw()

    @window.event
    def on_key_press(k, modifiers):
        global shader_on
        if k == key.LEFT:
            a.body.apply_force(Vec2d(-100, 0))
        elif k == key.RIGHT:
            a.body.apply_force(Vec2d(100, 0))
        elif k == key.UP:
            a.body.apply_force(Vec2d(0, 100))
        elif k == key.DOWN:
            a.body.apply_force(Vec2d(0, -100))
        elif k == key.SPACE:
            body2 = pymunk.Body(1, 2000)
            body2.position = (screenw / 2, screenh / 2)
            circ2 = pymunk.Circle(body2, 10)
            room.space.add(body2, circ2)
        elif k == key.ENTER:
            shader_on = not shader_on
            print shader_on
            
    pyglet.clock.schedule_interval(update, 1/PHYSICS_FPS)
    pyglet.app.run()

if __name__ == '__main__':
    main()
