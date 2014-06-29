#!/usr/bin/env python

import itertools
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
   gl_Position = ftransform() + inp;
   //gl_Position = ftransform();
   gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex + inp;
   //gl_Color = vec4(0, 0, 1, 1);
}

'''

fprog = '''#version 120
// Fragment shader

uniform sampler2D tex;

void main() {
   gl_FragColor = vec4(0, 0, 0.8, 1);
   //gl_FragColor = vec4(1,0,1,1);
   //gl_FragColor = texture2D(tex, gl_TexCoord[0].st);
}
'''

shader_on = False


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

def points2vertlist(pts, cols, batch):
    "Takes a list of pairs of points and adds it to a batch... hmr"
    coordsPerVert = 2
    vertFormat = 'v2f'
    colorFormat = 'c4B'
    # Unpack list
    v = list(itertools.chain.from_iterable(pts))
    c = list(itertools.chain.from_iterable(cols))
    numPoints = len(v) / coordsPerVert
    batch.add(numPoints, pyglet.graphics.GL_LINES, None, (vertFormat, v),
              (colorFormat, c))
    #vertex_list = pyglet.graphics.vertex_list(numPoints, (vertFormat, v))
    #return vertex_list
        
loc = 0.0

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
    static_lines = [pymunk.Segment(static_body, 
                                   Vec2d(x1, y1), Vec2d(x2, y2), 
                                   1)
                    for (x1, y1, x2, y2) in lines]
    batch = pyglet.graphics.Batch()
    points2vertlist(lines, colors, batch)
    #static_lines = [
    #    pymunk.Segment(static_body, Vec2d(300, 100), Vec2d(600, 100), 1),
    #    pymunk.Segment(static_body, Vec2d(300, 100), Vec2d(300, 300), 1),
    #    pymunk.Segment(static_body, Vec2d(600, 100), Vec2d(600, 300), 1),
    #]

    for l in static_lines:
        l.friction = 0.8
    space.add(static_lines)

    def update(dt):
        for _ in range(int(PHYSICS_STEPS)):
            space.step(dt/PHYSICS_STEPS)

    @window.event
    def on_draw():
        global loc 
        global shader_on
        loc += 0.01
        window.clear()
        # Unbinds whatever
        Shader.unbind()
        if shader_on:
            shader.bind()
            shader.uniformf("inp", loc, loc, 1.0, 3.0)
        batch.draw()
        pymunk.pyglet_util.draw(circ)
        #pymunk.pyglet_util.draw(space)
        Shader.unbind()
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
