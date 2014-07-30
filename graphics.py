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

    def distanceSquared(s, v2):
        dx = v2.x - s.x
        dy = v2.y - s.y
        return dx**2 + dy**2
    
    def distance(s, v2):
        return math.sqrt(s.distanceSquared(v2))
    

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

Don't use this for representing line shapes; this is more or
less only used internally by `Polygon`s."""
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

Generates nice lines using the "fade-polygon" technique discussed
here:

http://www.codeproject.com/Articles/199525/Drawing-nearly-perfect-D-line-segments-in-OpenGL

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
        # XXX: Gods these names are awful
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
        r1, g1, b1, a1 = s.v1.color
        r2, g2, b2, a2 = s.v2.color
        color1edge = (r1, g1, b1, 0)
        color2edge = (r2, g2, b2, 0)
        
        # Construct triangles
        vert1l = Vertex(v1lx, v1ly, color1edge)
        vert1c = Vertex(x1, y1, color1center)
        vert1r = Vertex(v1rx, v1ry, color1edge)
        vert2l = Vertex(v2lx, v2ly, color2edge)
        vert2c = Vertex(x2, y2, color2center)
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

    @staticmethod
    def circle(cx, cy, r, color, numSegments=32, **kwargs):
        """Returns a Poly outlining an approximation
of a circle.
            
Uses the algorithm described at http://slabode.exofire.net/circle_draw.shtml

BUGGO: Solid colors only"""
        return Polygon.arc(cx, cy, r, 360, color, numSegments=numSegments, **kwargs)

    @staticmethod
    def arc(cx, cy, r, angle, color, numSegments=32, **kwargs):
        """Same as `circle` but only makes a partial arc instead
of a full circle.

TODO: Be able to specify the starting angle!

BUGGO: Solid colors only"""
        radians = math.radians(angle)
        theta = radians / float(numSegments)
        tangentialFactor = math.tan(theta)
        radialFactor = math.cos(theta)
        x = r
        y = 0

        verts = []
        for i in range(numSegments+1):
            verts.append(Vertex(x + cx, y + cy, color))
            tx = -y
            ty = x
            x += tx * tangentialFactor
            y += ty * tangentialFactor
            x *= radialFactor
            y *= radialFactor
        return Polygon(verts, **kwargs)

    @staticmethod
    def rectCenter(cx, cy, w, h, color, **kwargs):
        """Returns a `Polygon` outlining a rectangle, specified from
the center.

BUGGO: Solid colors only"""
        ww = float(w) / 2
        hh = float(h) / 2
        return Polygon.rectCorner(cx - ww, cy - hh, w, h, color, **kwargs)

    @staticmethod
    def rectCorner(x, y, w, h, color, **kwargs):
        """Returns a list of points outlining a rectangle, given the lower-left point.
BUGGO: solid colors only
BUGGO: should be implemented in terms of rectCorner, or vice versa?"""
        verts = [
            Vertex(x, y, color),
            Vertex(x+w, y, color),
            Vertex(x+w, y+h, color),
            Vertex(x, y+h, color)
            ]
        return Polygon(verts, **kwargs)

    @staticmethod
    def line(x1, y1, x2, y2, color, **kwargs):
        """Returns a list of points making a single line.

BUGGO: Solid colors only"""
        verts = [
            Vertex(x1, y1, color),
            Vertex(x2, y2, color)
            ]
        return Polygon(verts, closed=False, **kwargs)

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


class LineImage(object):
    """An image created from a bunch of Polygon objects.

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

Also, our framework code doesn't use batches right at all argh
"""
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

def lerp(frm, to, amount):
    """Takes two floats and a float between 0 and 1.
Returns a float linerly interpolated between the two
given ones by the given amount.
"""
    return frm + (to - frm) * amount

def lerpSeq(frm, to, amount):
    """Applies `lerp` to two sequences."""
    # Man I love Python sometimes.
    #print frm, to
    return tuple([lerp(a, b, amount) for (a, b) in zip(frm, to)])


def lerpVertex(v1, v2, amount):
    lx = lerp(v1.x, v2.x, amount)
    ly = lerp(v1.y, v2.y, amount)
    lcolor = tuple([int(c) for c in lerpSeq(v1.color, v2.color, amount)])
    return Vertex(lx, ly, lcolor)
