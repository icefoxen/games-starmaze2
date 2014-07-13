import itertools
import math

import pyglet
from pyglet.gl import *

COLOR_WHITE = (255, 255, 255, 255)
COLOR_RED   = (255, 0, 0, 255)
COLOR_BLUE  = (0, 0, 255, 255)
COLOR_GREEN = (0, 255, 0, 255)

class ShaderGroup(pyglet.graphics.Group):
    def __init__(s, parent=None):
        super(s.__class__, s).__init__(parent)
        print 'made shader group'

    def set_state(s):
        print 'set state'

    def unset_state(s):
        print 'unset state'

    def __hash__(s):
        print 'hash'
        return hash(1)

    def __eq__(s, other):
        print 'eq'
        return True


def cornersToLines(corners):
    """Turns a list of (x,y) coordinates representing the corners of closed
polygon into a list of (x1, y1) (x2, y2) line endpoints."""
    # Sanity check
    if len(corners) < 2:
        return []

    endpointPairs = zip(corners, corners[1:])
    expandedEndpoints = list(itertools.chain.from_iterable(endpointPairs))
    
    # Close the last side
    expandedEndpoints.append(corners[-1])
    expandedEndpoints.append(corners[0])
    
    return expandedEndpoints

def circleCorners(cx, cy, r, numSegments=32):
    """Returns a list of points outlining an approximation
of a circle.  Can then be turned into actual lines with
`cornersToLines`.

Uses the algorithm described at http://slabode.exofire.net/circle_draw.shtml"""
    theta = (2 * math.pi) / float(numSegments)
    tangentialFactor = math.tan(theta)
    radialFactor = math.cos(theta)
    x = r
    y = 0

    verts = []
    for i in range(numSegments):
        verts.append((x + cx, y + cy))
        tx = -y
        ty = x
        x += tx * tangentialFactor
        y += ty * tangentialFactor
        x *= radialFactor
        y *= radialFactor
    return verts

def rectCornersCenter(cx, cy, w, h):
    """Returns a list of points outlining a rectangle, given the center point"""
    ww = float(w) / 2
    hh = float(h) / 2
    verts = [
        (cx - ww, cy - hh),
        (cx + ww, cy - hh),
        (cx + ww, cy + hh),
        (cx - ww, cy + hh)
    ]
    return verts

def rectCornersCorner(x, y, w, h):
    """Returns a list of points outlining a rectangle, given the lower-left point."""
    verts = [
        (x, y),
        (x+w, y),
        (x+w, y+h),
        (x, y+h)
        ]
    return verts

def lineCorners(x1, y1, x2, y2):
    """Returns a list of points making a single line."""
    return [(x1, y1), (x2, y2)]

def colorLines(lines, color=(255,255,255,255)):
    """Returns an array of colors suitable for coloring the
given lines the given color."""
    if len(color) != 4:
        raise Exception("color is not a 4-tuple: {}".format(color))
    else:
        return [color, color] * len(lines)



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
        super(s.__class__, s).__init__()
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

Takes a list of (x,y) coordinates representing vertices
(line endpoints, two per line; it doesn't assume a closed
loop of lines!)
a list of (r,g,b,a) colors (one
for each vertex), and optionally a `pyglet.graphics.Batch`
to draw in.

Generates nice lines using the "fade-polygon" technique discussed
here:

http://www.codeproject.com/Articles/199525/Drawing-nearly-perfect-D-line-segments-in-OpenGL

TODO:
One open question is how we handle memory, since these create
a `pyglet.graphics.VertexList` to hold the drawing data.
Right now we don't handle anything, on the assumption that
a) we're going to load all of these and then cache them without
creating new ones at random, and b) they'll all be freed more
or less correctly by pyglet should they ever become redundant.
These assumptions are probably pretty safe.
"""
    def __init__(s, verts, colors, lineWidth=2, batch=None, group=None, usage='static'):
        s._verts = verts
        s._colors = colors
        s.batch = batch or pyglet.graphics.Batch()
        s._usage = usage
        s._lineWidth = lineWidth

        s._group = group #or ShaderGroup()

        s._addToBatch()

    def _addToBatch(s):
        """Adds the verts and colors to the assigned batch.
Tesselates the lines to polygons, too."""
        
        # First we take the list of (x,y) line endpoints
        # and turn it into a list of (x1, y1, x2, y2) lines
        #print s._verts
        lineses = [(x1, y1, x2, y2) 
                   for ((x1, y1), (x2, y2))
                   in zip(s._verts[::2], s._verts[1::2])]
        # Then we use _line() to turn each line into a list of vertices
        tesselatedLines = map(lambda l: s._line(*l, width=s._lineWidth), lineses)
        # We also have to pair up the colors similarly
        #colorses = [(x1, y1, x2, y2) 
        #           for ((r1, b1, g1, a1), (r1, g2, b2, a2))
        #           in zip(s._colors[::2], s._colors[1::2])]
        colorses = zip(s._colors[::2], s._colors[1::2])
        # And get a list of colors for each vertex
        tesselatedColors = map(lambda col: s._color(*col), colorses)

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
            #print line #, color
            vertexList = s.batch.add(
                numPoints, 
                pyglet.graphics.GL_TRIANGLE_STRIP, 
                s._group, 
                (vertFormat, line),
                (colorFormat, color)
            )
            s._vertexLists.append(vertexList)


    def _line(s, x1, y1, x2, y2, width=2):
        """Returns a list of verts, creating a quad
suitable for drawing with GL_TRIANGLE_STRIP.

TODO: Endcaps (fairly easy)
TODO MORE: Polylines (harder)
TODO (POWER GOAL): Handle overlapping nicely
"""
        rise = y2 - y1
        run = x2 - x1
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
    
    def _color(s, lineColor1, lineColor2=None):
        """Makes a list of colors for the verts returned by lines().

lineColor is the color of the line; if lineColor 2 is not None
the line is a gradient between the two colors.

TODO: We could easily add butt caps to the end of the lines,
which might make them look rather nicer.
"""
        # Construct colors
        if lineColor2 is None:
            lineColor2 = lineColor1
        r1, g1, b1, a1 = lineColor1
        r2, g2, b2, a2 = lineColor2
        edgeColor1 = (r1, g1, b1, 0)
        edgeColor2 = (r2, g2, b2, 0)
        colors = [
            edgeColor1, edgeColor2, 
            lineColor1, lineColor2,
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

    def __init__(s, lineimage, x=0, y=0, batch=None, group=None):
        s._image = lineimage
        s._x = x
        s._y = y
        s._batch = batch or lineimage.batch
        s._rotation = 0.0
        s._scale = 1.0

    def delete(s):
        pass

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

    def _set_position(s, pos):
        (x, y) = pos
        s._x = x
        s._y = y
    position = property(lambda s: (s._x, s._y), _set_position)

    def _set_rotation(s, rotation):
        s._rotation = rotation
    rotation = property(lambda s: s._rotation, _set_rotation)

    def _set_scale(s, scale):
        s._scale = scale
    scale = property(lambda s: s._scale, _set_scale)

    #width = property(lambda s: s._y, _set_y)

    #height = property(lambda s: s._y, _set_y)
    
