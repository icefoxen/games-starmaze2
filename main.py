#!/usr/bin/env python

import os

import pyglet
import pyglet.window.key as key
import pymunk
from pymunk import Vec2d
import pymunk.pyglet_util

from shader import *

DISPLAY_FPS = True
PHYSICS_FPS = 60.0
PHYSICS_STEPS = 10.0


class Room(object):
    def __init__(self):
        pass


vprog = '''#version 130
// That's opengl 3.0


uniform mat4 projection_matrix;
uniform mat4 modelview_matrix;
 
in vec3 vertex;
 
void main(void) {
//	gl_Position = projection_matrix * modelview_matrix * vec4(vertex, 1.0);
   gl_Position = ftransform();
}

'''

fprog = '''#version 130

uniform sampler2D tex;

void main() {
   gl_FragColor = vec4(1,0,1,1);
   //gl_FragColor = texture2D(tex, gl_TexCoord[0].st);
}
'''

shader_on = True



def main():
    screenw = 1024
    screenh = 768

    window = pyglet.window.Window(width=screenw, height=screenh)

    fps_display = pyglet.clock.ClockDisplay()

    space = pymunk.Space()
    space.gravity = (0.0, -500.0)

    body = pymunk.Body(1, 2000)
    body.position = (screenw / 2, screenh / 2)
    circ = pymunk.Circle(body, 10)
    circ.friction = 0.8
    space.add(body, circ)

    shader = Shader([vprog], [fprog])

    static_body = pymunk.Body()
    static_lines = [
        pymunk.Segment(static_body, Vec2d(300, 100), Vec2d(600, 100), 1),
        pymunk.Segment(static_body, Vec2d(300, 100), Vec2d(300, 300), 1),
        pymunk.Segment(static_body, Vec2d(600, 100), Vec2d(600, 300), 1),
    ]

    for l in static_lines:
        l.friction = 0.8
    space.add(static_lines)

    def update(dt):
        for _ in range(int(PHYSICS_STEPS)):
            space.step(dt/PHYSICS_STEPS)

    @window.event
    def on_draw():
        global shader_on
        window.clear()
        # Unbinds whatever
        shader.unbind()
        if shader_on:
            shader.bind()
        pymunk.pyglet_util.draw(space)
        fps_display.draw()

    @window.event
    def on_key_press(k, modifiers):
        global shader_on
        if k == key.LEFT:
            body.apply_impulse(Vec2d(-100, 0))
        elif k == key.RIGHT:
            body.apply_impulse(Vec2d(100, 0))
        elif k == key.UP:
            body.apply_impulse(Vec2d(0, 100))
        elif k == key.DOWN:
            body.apply_impulse(Vec2d(0, -100))
        elif k == key.SPACE:
            body2 = pymunk.Body(1, 2000)
            body2.position = (screenw / 2, screenh / 2)
            circ2 = pymunk.Circle(body2, 10)
            space.add(body2, circ2)
        elif k == key.ENTER:
            shader_on = not shader_on
            print shader_on
            
    pyglet.clock.schedule_interval(update, 1/PHYSICS_FPS)
    pyglet.app.run()

if __name__ == '__main__':
    main()
