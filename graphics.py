import itertools
import math

import pyglet
from pyglet.gl import *

class Affine(object):
    """A class set up to do an OpenGL affine transform (in 2d).

You use it something like::

  with Affine(translation, rotation, scale):
      draw_stuff()

Pretty cool, huh?
"""
    def __init__(s, translation=(0.0, 0.0), rotation=0.0, scale=(1.0, 1.0)):
        s.x, s.y = translation
        s.rotation = rotation
        s.scaleX, s.scaleY = scale

    def __enter__(s):
        glPushMatrix()
        glTranslatef(s.x, s.y, 0)
        glRotatef(s.rotation, 0.0, 0.0, 1.0)
        glScalef(s.scaleX, s.scaleY, 0.0)

    def __exit__(s, kind, exception, traceback):
        glPopMatrix()

    def __repr__(s):
        return "Affine(({}, {}), {}, ({}, {})".format(
            s.x, s.y, s.rotation, s.scaleX, s.scaleY)

class Camera(Affine):
    """An `Affine` which takes a target `Actor` and
updates its transform to follow it, moving smoothly.

hardBoundaryFactor is a hard limit of how far the target
can be off-center.  It defaults to 50% of the screen.
"""

    def __init__(s, target, screenw, screenh, hardBoundaryFactor = 0.50):
        super(Camera, s).__init__()
        s.target = target
        s.speedFactor = 2.5
        s.halfScreenW = screenw / 2
        s.halfScreenH = screenh / 2
        s.aimedAtX = 0.0
        s.aimedAtY = 0.0
        s.currentX = s.target.body.position[0]
        s.currentY = s.target.body.position[1]

        s.hardBoundaryX = s.halfScreenW * hardBoundaryFactor
        s.hardBoundaryY = s.halfScreenH * hardBoundaryFactor

    def update(s, dt):
        """Calculates the camera's position for a new frame.

Basically, we lerp towards the target's position."""
        s.aimedAtX, s.aimedAtY = s.target.body.position
        deltaX = s.aimedAtX - s.currentX
        deltaY = s.aimedAtY - s.currentY

        s.currentX += deltaX * s.speedFactor * dt
        s.currentY += deltaY * s.speedFactor * dt

        # We have to test whether the _next_ frame
        # will be out of bounds and account for that;
        # if we do it on the current frame's numbers
        # then once you get close to the corners
        # of the screen the view gets 'sucked' into them.
        deltaX2 = s.aimedAtX - s.currentX
        deltaY2 = s.aimedAtY - s.currentY

        if deltaX2 > s.hardBoundaryX:
            s.currentX = s.aimedAtX - s.hardBoundaryX
        elif deltaX2 < -s.hardBoundaryX:
            s.currentX = s.aimedAtX + s.hardBoundaryX

        if deltaY2 > s.hardBoundaryY:
            s.currentY = s.aimedAtY - s.hardBoundaryY
        elif deltaY2 < -s.hardBoundaryY:
            s.currentY = s.aimedAtY + s.hardBoundaryY

        #print("Current:", s.currentX, s.currentY)
        #print("Target: ", s.aimedAtX, s.aimedAtY)
        #print("Delta:  ", deltaX, deltaY)
        s.x = -s.currentX + s.halfScreenW
        s.y = -s.currentY + s.halfScreenH


class LineImage(object):
    """A collection of lines that can be drawn like an image.

Takes a list of (x,y) coordinates, a list of (r,g,b,a) colors (one
for each coordinate), and optionally a `pyglet.graphics.Batch`
to draw in.

Generates nice lines using the "fade-polygon" technique discussed
here:

TODO:
One open question is how we handle memory, since these create
a `pyglet.graphics.VertexList` to hold the drawing data.
Right now we don't handle anything, on the assumption that
a) we're going to load all of these and then cache them without
creating new ones at random, and b) they'll all be freed more
or less correctly by pyglet should they ever become redundant.
These assumptions are probably pretty safe.

TODO: Triangle strips?
"""
    def __init__(s, verts, colors, batch=None, usage='static'):
        s._verts = verts
        s._colors = colors
        s.batch = batch or pyglet.graphics.Batch()
        s._vertexList = None
        s._usage = usage

        s._addToBatch()

    def _addToBatch(s):
        """Adds the verts and colors to the assigned batch.
For now we use GL_LINE_LOOP and don't do anything fancy.
Eventually we're going to tesselate this into triangles
and be generally nicer."""
        finalVerts = []
        finalColors = []
        for v1, v2 in zip(s._verts[::2], s._verts[1::2]):
            v1x, v1y = v1
            v2x, v2y = v2
            #v1x, v1y = s._verts[0]
            #v2x, v2y = s._verts[1]
            l = s._line(v1x, v1y, v2x, v2y, 8)
            colors = s._color((255, 0, 0, 255))
            
            unpackedVerts = list(itertools.chain.from_iterable(l))
            unpackedColors = list(itertools.chain.from_iterable(colors))

            finalVerts.append(unpackedVerts)
            finalColors.append(unpackedColors)
        
        vs = list(itertools.chain.from_iterable(finalVerts))
        cs = list(itertools.chain.from_iterable(finalColors))
        #print vs
        #print cs

        coordsPerVert = 2
        numPoints = len(vs) / coordsPerVert

            #print unpackedVerts
            #print unpackedColors
            #print numPoints

        vertFormat = 'v2f/{}'.format(s._usage)
        colorFormat = 'c4B/{}'.format(s._usage)
        s._vertexList = s.batch.add(numPoints, 
                                    pyglet.graphics.GL_TRIANGLES, 
                                    None, 
                                    (vertFormat, vs),
                                    (colorFormat, cs)
        )
        return
        
        # Tesselate lines
        # Oh man python is so cool
        # This turns every pair of vertices in the list
        # into a pair of endpoints which then get fed 
        # through s._line
        v1s = s._verts[::2]
        v2s = s._verts[1::2]

        verts = [s._line(x1, y1, x2, y2, 3)
                 for ((x1, y1), (x2, y2)) in zip(v1s, v2s)]
        colors = s._color((255, 0, 0, 255))
        # Verts is now a list of tesselated lines.
        # [line] where line is [(x,y)]
        #print 'RAWR'
        #print verts
        #print lines[0][0]
        #print lines[1]
        #for l in zip(v1s, v2s):
        #    ((x1, y1), (x2, y2)) = l
            

        # Unpack/flatten list
        print verts
        v = list(itertools.chain.from_iterable(verts))
        v2 = list(itertools.chain.from_iterable(v))
        c = list(itertools.chain.from_iterable(colors))
        print len(v2), v2
        print len(c), c
        numPoints = len(v2) / coordsPerVert
        s._vertexList = s.batch.add(numPoints, 
                                    pyglet.graphics.GL_TRIANGLES, 
                                    None, 
                                    (vertFormat, v2),
                                    (colorFormat, c)
                              )

    def _line(s, x1, y1, x2, y2, width):
        """Returns a (verts, colors) tuple, where verts is
a list of (x,y) vertices and colors (r,g,b,a), outlining a quad
with alpha-y edges when drawn with GL_TRIANGLES.

Right now the lines are solid colors, only gradients.
TODO: Do we need gradients?  They'd be easy to add."""
        #print x1, y1, x2, y2
        rise = y2 - y1
        run = x2 - x1
        # XXX div0
        #slope = float(rise) / float(run)
        #normal = 1.0 / slope
        angle = math.atan2(rise, run)
        xoff = math.sin(angle) * width
        yoff = math.cos(angle) * width
        v1 = (x1, y1)
        v2 = (x2, y2)
        # Calculate points to the 'left' and 'right' of the endpoints
        v1l = (x1 - xoff, y1 + yoff)
        v1r = (x1 + xoff, y1 - yoff)
        v2l = (x2 - xoff, y1 + yoff)
        v2r = (x2 + xoff, y2 - yoff)
        
        # Construct triangles
        # Remember, OpenGL triangles go CCW
        verts = [
            v1,  v1l, v2l,
            v2l, v2,  v1,
            v1,  v2,  v2r,
            v2r, v1r, v1
        ]
        return verts
    
    def _color(s, color):
        "Makes a list of colors for the verts returned by lines()"
        # Construct colors
        r, g, b, a = color
        vc = (r, g, b, 0)
        colors = [
            color, vc, vc,
            vc, color, color,
            color, color, vc,
            vc, vc, color
        ]
        return colors
            
            



    def draw(s, x, y):
        """This shouldn't really be here, we should usually do drawing from the batch.  But, I guess it's valid."""
        glPushMatrix()
        glLoadIdentity()
        glTranslatef(x, y, 0)
        s._vertexList.draw(GL_LINE_LOOP)
        glPopMatrix()
        

class LineSprite(object):
    """A class that draws a positioned, scaled, rotated
and maybe someday animated `LineImage`s.

The question is, how do we animate these things...

Apart from animations, this is mostly API-compatible with
`pyglet.sprite.Sprite`.  Huzzah~

Except I took out all the group stuff so I can not worry
about it until I need to.  Though it looks like it might
be ideal for shaders and ordering maybe...
"""

    def __init__(s, lineimage, x=0, y=0, batch=None):
        s._image = lineimage
        s._x = x
        s._y = y
        s._batch = batch or lineimage.batch
        s._rotation = 0.0
        s._scale = 1.0

    def delete(s):
        pass

    def set_position(self, x, y):
        s._x = x
        s._y = y

    def draw(s):
        glPushAttrib(GL_COLOR_BUFFER_BIT)
        glEnable(GL_BLEND)
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
        with Affine((s._x, s._y), s.rotation, (s._scale, s._scale)):
            s._batch.draw()
        glPopAttrib()

    # def _get_group(s):
    #     return s._group
    # def _set_group(s, group):
    #     s._group = group
    # group = property(_get_group, _set_group)

    def _set_image(s, image):
        s._image = image
    image = property(lambda s: s._image, _set_image)
    
    def _set_x(s, x):
        s._x = x
    x = property(lambda s: s._x, _set_x)

    def _set_y(s, y):
        s._y = y
    y = property(lambda s: s._y, _set_y)

    def _set_rotation(s, rotation):
        s._rotation = rotation
    rotation = property(lambda s: s._rotation, _set_rotation)

    def _set_scale(s, scale):
        s._scale = scale
    scale = property(lambda s: s._scale, _set_scale)

    #width = property(lambda s: s._y, _set_y)

    #height = property(lambda s: s._y, _set_y)
    
