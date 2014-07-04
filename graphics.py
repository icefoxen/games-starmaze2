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
        
        # First we take the list of (x,y) line endpoints
        # and turn it into a list of (x1, y1, x2, y2) lines

        lineses = [(x1, y1, x2, y2) 
                   for ((x1, y1), (x2, y2))
                   in zip(s._verts[::2], s._verts[1::2])]
        # Then we use _line() to turn each line into a list of vertices
        tesselatedLines = map(lambda l: s._line(*l, width=3), lineses)

        # And get a list of colors for each vertex
        tesselatedColors = map(lambda l: s._color((255, 0, 0, 255), (0, 0, 255, 255)), tesselatedLines)

        # Then we make a vertex list for each line
        s._vertexLists = []
        for line, color in zip(tesselatedLines, tesselatedColors):
            coordsPerVert = 2
            numPoints = len(line) / coordsPerVert

            #print unpackedVerts
            #print unpackedColors
            #print numPoints

            vertFormat = 'v2f/{}'.format(s._usage)
            colorFormat = 'c4B/{}'.format(s._usage)
            #print line, color
            vertexList = s.batch.add(
                numPoints, 
                pyglet.graphics.GL_TRIANGLE_STRIP, 
                None, 
                (vertFormat, line),
                (colorFormat, color)
            )
            s._vertexLists.append(vertexList)


    def _line(s, x1, y1, x2, y2, width=2):
        """Returns a list of verts, creating a quad
suitable for drawing with GL_TRIANGLE_STRIP.
"""
        #print x1, y1, x2, y2
        rise = y2 - y1
        run = x2 - x1
        # XXX div0
        #slope = float(rise) / float(run)
        #normal = 1.0 / slope
        angle = math.atan2(rise, run)
        xoff = math.sin(angle) * width
        yoff = math.cos(angle) * width
        # Calculate points to the 'left' and 'right' of the endpoints
        v1lx = x1 - xoff
        v1ly = y1 + yoff
        v1rx = x1 + xoff
        v1ry = y1 - yoff

        v2lx = x2 - xoff
        v2ly = y2 + yoff
        v2rx = x2 + xoff
        v2ry = y2 - yoff
        
        # Construct triangles
        # Remember, OpenGL triangles go CCW
        # And GL_TRIANGLE_STRIP draws v1, v2, v3,
        # then v3, v2, v4.  Whew!
        verts = [
            v1lx, v1ly, 
            v2lx, v2ly,
            x1, y1,
            x2, y2,
            v1rx, v1ry,
            v2rx, v2ry
        ]
        return verts
    
    def _color(s, lineColor, lineColor2=None):
        """Makes a list of colors for the verts returned by lines().

lineColor is the color of the line; if lineColor 2 is not None
the line is a gradient between the two colors.
"""
        # Construct colors
        if lineColor2 is None:
            lineColor2 = lineColor
        r1, g1, b1, a1 = lineColor
        r2, g2, b2, a2 = lineColor2
        edgeColor1 = (r1, g1, b1, 0)
        edgeColor2 = (r2, g2, b2, 0)
        colors = [
            edgeColor1, edgeColor2, 
            lineColor, lineColor2,
            edgeColor1, edgeColor2
        ]
        # Flatten the list-of-tuples into a list
        unpackedColors = list(itertools.chain.from_iterable(colors))
        return unpackedColors
        #else:
        #    r1, g1, b1, a1 = lineColor
        #    r2, g2, b2, a2 = lineColor2
            
            



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
    
