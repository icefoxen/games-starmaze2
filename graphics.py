from collections import *
import itertools
import math

import pyglet
from pyglet.gl import *

COLOR_WHITE = (255, 255, 255, 255)
COLOR_RED   = (255, 0, 0, 255)
COLOR_BLUE  = (0, 0, 255, 255)
COLOR_GREEN = (0, 255, 0, 255)

class ShaderGroup(pyglet.graphics.Group):
    def __init__(s, vshader, fshader, parent=None):
        pyglet.graphics.Group.__init__(s, parent)
        print 'made shader group'
        s.vshader = vshader
        s.fshader = fshader

        s.fullShader = vshader + fshader

    def set_state(s):
        print 'set state'

    def unset_state(s):
        print 'unset state'

    def __hash__(s):
        return hash(s.fullShader)

    def __eq__(s, other):
        return s.fullShader == other.fullShader

class Vertex(namedtuple("Vertex", ["x", "y", "color"])):
    """A 2D vertex, which might also contain color and other properties.
    Vertices are made into triangles, which are then made into other shapes."""

    @property
    def coords(s):
        """Returns the coordinates of the vertex."""
        return (s.x, s.y)

    

class Triangle(namedtuple("Triangle", ["v1", "v2", "v3"])):
    """A triangle made of 3 vertices."""

    def fixWinding(s):
        """Makes sure that the vertices are wound in
counter-clockwise order.
TODO: implement this
Basically you have two line segments CA and AB, and
you can take the cross product of them to see which
way the normal points."""
        pass

    def getCoordList(s):
        # Because we inherit from a namedtuple, 'self' is a
        # sequence of all its members.  Huzzah!
        return [v.coords for v in s]

    def getColorList(s):
        return [v.color for v in s]

class Line(object):
    """A line between two points, specified by two (x,y) tuples.
Color is optional, defaults to white.  If color2 is None, color1
is used as a solid color, otherwise it produces a gradient between
color1 and color2."""
    def __init__(s, v1, v2, width=2):
        s.v1 = v1
        s.v2 = v2
        s.width = width

    def toTriangles(s):
        """Returns a list of Triangles,
suitable for drawing with GL_TRIANGLES.

GL_TRIANGLE_STRIP is for losers.  And also causes isses
if we ever try to group disconnected lines together in
the same batch.

TODO: Endcaps (fairly easy)
TODO MORE: Polylines (harder)
TODO (POWER GOAL): Handle overlapping nicely
"""
        rise = s.v2.y - s.v1.y
        run = s.v2.x - s.v1.x
        angle = math.atan2(rise, run)
        xoff = math.sin(angle) * s.width
        yoff = math.cos(angle) * s.width
        # Calculate points to the 'left' and 'right' of the endpoints
        x1 = s.v1.x
        x2 = s.v2.x
        y1 = s.v1.y
        y2 = s.v2.y
        v1lx = x1 - xoff
        v1ly = y1 + yoff
        v1rx = x1 + xoff
        v1ry = y1 - yoff

        v2lx = x2 - xoff
        v2ly = y2 + yoff
        v2rx = x2 + xoff
        v2ry = y2 - yoff

        color1center = s.v1.color
        color2center = s.v2.color
        r1, g1, b1, a1 = color1center
        r2, g2, b2, a2 = color2center
        color1edge = (r1, g1, b1, 0)
        color2edge = (r2, g2, b2, 0)
        
        # Construct triangles
        vert1l = Vertex(v1lx, v1ly, color1edge)
        vert1c = Vertex(x1, y1, s.v1.color)
        vert1r = Vertex(v1rx, v1ry, color1edge)
        vert2l = Vertex(v2lx, v2ly, color2edge)
        vert2c = Vertex(x2, y2, s.v2.color)
        vert2r = Vertex(v2rx, v2ry, color2edge)
        tris = [
            Triangle(vert1l, vert2l, vert1c),
            Triangle(vert1c, vert2l, vert2c),
            Triangle(vert2c, vert1c, vert2r),
            Triangle(vert2r, vert1c, vert1r)
            ]
        return tris


class Polygon(object):
    """A closed polygon specified by a list of vertices."""
    def __init__(s, verts, closed=True, solid=False, strokeWidth=2):
        s.verts = verts
        s.closed = closed
        s.solid = solid
        s.strokeWidth = strokeWidth
        
    def toLines(s):
        """Returns a list of `Line`s representing the polygon."""
        endpointPairs = zip(s.verts, s.verts[1:])
        lines = [Line(p1, p2, width=s.strokeWidth) for p1, p2 in endpointPairs]
        if s.closed:
            # Close the last side
            lines.append(Line(s.verts[-1], s.verts[0], width=s.strokeWidth))

        return lines

class LineImage2(object):
    """An image created from a bunch of Polygon objects."""
    def __init__(s, polys, batch=None, group=None):
        s.polys = polys
        s.batch = batch or pyglet.graphics.Batch()
        s.group = group
        
        s._vertexLists = []
        s._addToBatch()


    def _addToBatch(s):
        """Turns the lines given into a bunch of triangles, then adds
them to the image's batch."""
        def polysToLines(polys):
            lines = [poly.toLines() for poly in polys]
            return list(itertools.chain.from_iterable(lines))
        def linesToTriangles(lines):
            tris = [line.toTriangles() for line in lines]
            return list(itertools.chain.from_iterable(tris))
        def trisToCoords(tris):
            coordList = [tri.getCoordList() for tri in tris]
            coords = list(itertools.chain.from_iterable(coordList))
            return list(itertools.chain.from_iterable(coords))
        def trisToColors(tris):
            colorsList = [tri.getColorList() for tri in tris]
            colors = list(itertools.chain.from_iterable(colorsList))
            return list(itertools.chain.from_iterable(colors))
        lines = polysToLines(s.polys)
        tris = linesToTriangles(lines)
        coords = trisToCoords(tris)
        colors = trisToColors(tris)

        coordsPerVert = 2
        colorsPerVert = 4
        vertFormat = 'v2f/static'
        colorFormat = 'c4B/static'
        numPoints = len(tris) * 3
        vertexList = s.batch.add(
            numPoints,
            pyglet.graphics.GL_TRIANGLES,
            s.group,
            (vertFormat, coords),
            (colorFormat, colors)
            )
        s._vertexLists.append(vertexList)


        
def cornersToLines(corners):
    """Turns a list of (x,y) coordinates representing the corners of closed
polygon into a list of (x1, y1) (x2, y2) line endpoints."""
    # Sanity check
    if len(corners) < 2:
        return []
 
    endpointPairs = zip(corners, corners[1:])
    flattenedEndpoints = list(itertools.chain.from_iterable(endpointPairs))
     
    # Close the last side
    flattenedEndpoints.append(corners[-1])
    flattenedEndpoints.append(corners[0])

    return flattenedEndpoints

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
    # There ISN'T an off-by-one error here because it automatically
    # draws a closed loop between the last and first points.
    for i in range(numSegments):
        verts.append((x + cx, y + cy))
        tx = -y
        ty = x
        x += tx * tangentialFactor
        y += ty * tangentialFactor
        x *= radialFactor
        y *= radialFactor
    return verts

def arcCorners(cx, cy, r, angle, numSegments=32):
    """Same as `circleCorners` but only makes a partial arc instead
of a full circle.

Semi-unfortunately still draws it as a closed loop, but works for now.

TODO: Be able to specify the starting angle!"""
    radians = math.radians(angle)
    theta = radians / float(numSegments)
    tangentialFactor = math.tan(theta)
    radialFactor = math.cos(theta)
    x = r
    y = 0

    verts = []
    for i in range(numSegments+1):
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
        return [color] * len(lines)


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
        # 'target' is any pymunk.body
        Affine.__init__(s)
        s.target = target
        s.speedFactor = 2.5
        s.halfScreenW = screenw / 2
        s.halfScreenH = screenh / 2
        s.aimedAtX = 0.0
        s.aimedAtY = 0.0
        s.currentX = s.target.position[0]
        s.currentY = s.target.position[1]

        s.hardBoundaryX = s.halfScreenW * hardBoundaryFactor
        s.hardBoundaryY = s.halfScreenH * hardBoundaryFactor

    def update(s, dt):
        """Calculates the camera's position for a new frame.

Basically, we lerp towards the target's position."""
        s.aimedAtX, s.aimedAtY = s.target.position
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

Also we need to do proper polylines.  How to do that without assuming
a line loop?  Well, more classes I guess.

Also, doesn't really use groups right I think.

Also, our framework code doesn't use batches at all argh
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
        colorses = zip(s._colors[::2], s._colors[1::2])
        # And get a list of colors for each vertex instead of each line
        tesselatedColors = map(lambda col: s._color(*col), colorses)

        flattenedLines = list(itertools.chain.from_iterable(tesselatedLines))
        flattenedColors = list(itertools.chain.from_iterable(tesselatedColors))

        # Then we make a vertex list for the lines
        s._vertexLists = []
        coordsPerVert = 2
        numPoints = len(flattenedLines) / coordsPerVert

        #print unpackedVerts
        #print unpackedColors
        #print numPoints

        vertFormat = 'v2f/{}'.format(s._usage)
        colorFormat = 'c4B/{}'.format(s._usage)
        #print len(flattenedLines), len(flattenedColors)
        vertexList = s.batch.add(
            numPoints, 
            pyglet.graphics.GL_TRIANGLES, 
            s._group, 
            (vertFormat, flattenedLines),
            (colorFormat, flattenedColors)
        )
        #vertexList = s.batch.add_indexed(
        #    count, mode, group, indices, data)
        s._vertexLists.append(vertexList)


    def _line(s, x1, y1, x2, y2, width=2):
        """Returns a list of verts, creating a quad
suitable for drawing with GL_TRIANGLES.

GL_TRIANGLE_STRIP is for losers.  And also causes isses
if we ever try to group disconnected lines together in
the same batch.

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
        verts = [
            v1lx, v1ly,
            v2lx, v2ly,
            x1, y1,

            x1, y1,
            v2lx, v2ly,
            x2, y2,

            x2, y2,
            x1, y1,
            v2rx, v2ry,

            v2rx, v2ry,
            x1, y1,
            v1rx, v1ry,
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
            edgeColor1, edgeColor2, lineColor1,
            lineColor1, edgeColor2, lineColor2,
            lineColor2, lineColor1, edgeColor2,
            edgeColor2, lineColor1, edgeColor1,
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
        

def vertsToIndexedVerts(verts):
    """Turns a sequence of vertex pairs into a (smaller) sequence of verts and
a list of indices to draw the shape the verts makes.

BUGGO: Doesn't really work.

Also isn't really worth it unless we figure out a way to coalesce vertices that
are almost but not exactly quite the same point, due to floating-point math having
happened to them."""
    vertDict = {}
    counter = 0
    vertPairs = zip(verts[::2], verts[1::2])
    for v in vertPairs:
        # Turn floating-point-calculated nearly-zeros into 0
        print v
        x, y = v
        if abs(x) < 0.0000001:
            x = 0.0
        if abs(y) < 0.0000001:
            y = 0.0
        v = (x,y)
        if not vertDict.has_key(v):
            vertDict[v] = counter
            counter += 1
    print vertDict
    indices = [vertDict[i] for i in verts]
    vertIndexList = [(val,key) for key,val in vertDict.iteritems()]
    vertIndexList.sort()
    vertList = [val for _, val in vertIndexList]
    return vertList, indices
