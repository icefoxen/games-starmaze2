#!/usr/bin/env python

import itertools
import os

import pyglet
import pyglet.window.key as key
from pyglet.gl import *
import pymunk
from pymunk import Vec2d
import pymunk.pyglet_util

from shader import *

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


class Room(object):
    def __init__(s, lines, colors):
        s.lines = lines
        s.colors = colors
        s.body = pymunk.Body()
        s.physicsObjects = [pymunk.Segment(
            s.body, 
            Vec2d(x1, y1), Vec2d(x2, y2), 
            1
        )
                            for (x1, y1, x2, y2) in lines]
        for o in s.physicsObjects:
            o.friction = 0.8

        s.batch = pyglet.graphics.Batch()
        Room.points2vertlist(lines, colors, s.batch)

    @classmethod
    def points2vertlist(s, pts, colors, batch):
        "Takes a list of pairs of points and adds it to a batch... hmr"
        coordsPerVert = 2
        vertFormat = 'v2f'
        colorFormat = 'c4B'
        # Unpack/flatten list
        v = list(itertools.chain.from_iterable(pts))
        c = list(itertools.chain.from_iterable(colors))
        numPoints = len(v) / coordsPerVert
        batch.add(numPoints, pyglet.graphics.GL_LINES, None, (vertFormat, v),
                  (colorFormat, c))
        
    def draw(s):
        s.batch.draw()

    def activatePhysics(s, space):
        space.add(s.physicsObjects)

    def deactivatePhysics(s, space):
        space.remove(s.physicsObjects)

class Actor(object):
    def __init__(s, x, y):
        s.body = pymunk.Body(1, 2000)
        s.shape = pymunk.Circle(s.body, 10)
        s.shape.friction = 0.8
        s.body.position = (x,y)

    def activatePhysics(s, space):
        space.add(s.body, s.shape)

    def deactivatePhysics(s, space):
        space.remove(s.body, s.shape)

    def draw(s):
        pymunk.pyglet_util.draw(s.shape)

lines = [
    (300, 100, 600, 100), 
    (300, 100, 300, 300), 
    (600, 100, 600, 300)
]

colors = [
    (255, 255, 255, 255), (128, 255, 128, 255),
    (128, 0, 255, 255), (255, 0, 128, 255),
    (255, 255, 0, 255), (255, 0, 255, 255)
]

def main():
    screenw = 1024
    screenh = 768

    window = pyglet.window.Window(width=screenw, height=screenh)

    fps_display = pyglet.clock.ClockDisplay()

    space = pymunk.Space()
    space.gravity = (0.0, -500.0)

    a = Actor(screenw / 2, screenh / 2)
    a.activatePhysics(space)

    r = Room(lines, colors)
    r.activatePhysics(space)

    def update(dt):
        for _ in range(int(PHYSICS_STEPS)):
            space.step(dt/PHYSICS_STEPS)

    shader = Shader([vprog], [fprog])


    @window.event
    def on_draw():
        glEnable(GL_LINE_SMOOTH)
        glLineWidth(3)
        window.clear()
        # Unbinds whatever
        Shader.unbind()
        if shader_on:
            shader.bind()
            # X, Y, Z, scale
            shader.uniformf("inp", 0.0, 0.0, 0.0, 0.0)
        #glPushMatrix()
        r.draw()
        a.draw()
        #glPopMatrix()
        pymunk.pyglet_util.draw(space)

        Shader.unbind()
        fps_display.draw()

    @window.event
    def on_key_press(k, modifiers):
        global shader_on
        if k == key.LEFT:
            a.body.apply_impulse(Vec2d(-100, 0))
        elif k == key.RIGHT:
            a.body.apply_impulse(Vec2d(100, 0))
        elif k == key.UP:
            a.body.apply_impulse(Vec2d(0, 100))
        elif k == key.DOWN:
            a.body.apply_impulse(Vec2d(0, -100))
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
