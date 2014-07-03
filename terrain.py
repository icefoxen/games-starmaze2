import itertools

import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util



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
can be off-center.  It defaults to 70% of the screen.
"""

    def __init__(s, target, screenw, screenh, hardBoundaryFactor = 0.70):
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

        if deltaX > s.hardBoundaryX:
            s.currentX = s.aimedAtX - s.hardBoundaryX
        elif deltaX < -s.hardBoundaryX:
            s.currentX = s.aimedAtX + s.hardBoundaryX
        else:
            s.currentX += deltaX * s.speedFactor * dt

        if deltaY > s.hardBoundaryY:
            s.currentY = s.aimedAtY - s.hardBoundaryY
        elif deltaY < -s.hardBoundaryY:
            s.currentY = s.aimedAtY + s.hardBoundaryY
        else:
            s.currentY += deltaY * s.speedFactor * dt
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

Shaders remain an open issue, though upon contemplation
it'd probably be better to make a ShaderGroup class inheriting
from `pyglet.graphics.Group`.

One open question is how we handle memory, since these create
a `pyglet.graphics.VertexList` to hold the drawing data.
Right now we don't handle anything, on the assumption that
a) we're going to load all of these and then cache them without
creating new ones at random, and b) they'll all be freed more
or less correctly by pyglet should they ever become redundant.
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
        coordsPerVert = 2
        vertFormat = 'v2f/{}'.format(s._usage)
        colorFormat = 'c4B/{}'.format(s._usage)
        # Unpack/flatten list
        v = list(itertools.chain.from_iterable(s._verts))
        c = list(itertools.chain.from_iterable(s._colors))
        numPoints = len(v) / coordsPerVert
        s._vertexList = s.batch.add(numPoints, 
                                  pyglet.graphics.GL_LINE_LOOP, 
                                  None, 
                                  (vertFormat, v),
                                  (colorFormat, c)
                              )

    def draw(s, x, y):
        """This shouldn't really be here, we should usually do drawing from the batch.  But, I guess it's valid."""
        s._vertexList.draw(GL_LINE_LOOP)
        

class LineSprite(object):
    """A class that draws a positioned, scaled, rotated
and maybe someday animated `LineImage`s.

The question is, how do we animate these things...

Apart from animations, this is mostly API-compatible with
`pyglet.sprite.Sprite`.  Huzzah~
"""

    def __init__(s, lineimage, x=0, y=0, batch=None, group=None):
        s._image = lineimage
        s._x = x
        s._y = y
        s._batch = batch or lineimage.batch
        s._group = None # XXX
        s._rotation = 0.0
        s._scale = 1.0

    def delete(s):
        pass

    def set_position(self, x, y):
        s._x = x
        s._y = y

    def draw(s):
        with Affine((s._x, s._y), s.rotation, (s._scale, s._scale)) as _:
            s._batch.draw()

    def _get_group(s):
        return s._group
    def _set_group(s, group):
        s._group = group
    group = property(_get_group, _set_group)

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
    




# Of course.
# Rooms should be constructed out of sub-objects.
# These sub-objects can be walls, bouncers, moving
# platforms, spikes, whatever.
# The room itself is a collection of these features,
# plus exits and whatever other environmental stuff
# is necessary.
class Terrain(object):
    """A wall, platform, or other terrain feature in a room.
Currently, just uses a `pymunk.Poly` for its shape.  More complex
things might come later.
"""
    def __init__(s, verts, colors, batch=None):
        "Create a `Terrain` object."
        s.verts = verts
        s.colors = colors
        s.body = pymunk.Body(mass=None, moment=None)
        poly = pymunk.Poly(s.body, verts)
        poly.friction = 0.8
        s.physicsObjects = [poly]

        s.image = LineImage(verts, colors, batch=batch)
        s.sprite = LineSprite(s.image, batch=batch)
        
    def draw(s):
        "Draws the terrain feature."
        s.sprite.draw()
        #pymunk.pyglet_util.draw(s.physicsObjects)
        

def createBlock(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    "Creates a `Terrain` object representing a block of the given size."
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    verts = [(xf, yf), (xf+wf, yf), (xf+wf, yf+hf), (xf, yf+hf)]
    #print "FOO", verts
    colors = []
    if isinstance(color, list):
        if len(color) != len(verts):
            raise Exception("Invalid set of colors: {}".format(color))
        else:
            colors = color
    else:
        if len(color) != 4:
            raise Exception("color is not a 4-tuple: {}".format(color))
        else:
            colors = [color] * len(verts)

    t = Terrain(verts, colors, batch)
    return t

class Room(object):
    """A collection of `Terrain` objects and environmental data.
Also handles physics.  There's only ever one `Room` on screen
at a time."""
    def __init__(s):
        s.terrain = set()
        s.space = pymunk.Space()
        s.space.gravity = (0.0, -500.0)

        s.actors = set()

        s.name = ""

    def addTerrain(s, t):
        "Adds a `Terrain` object to the room."
        s.terrain.add(t)
        s.space.add(t.physicsObjects)

    def removeTerrain(s, t):
        "Removes a `Terrain` object."
        s.terrain.remove(t)
        s.space.remove(t.physicsObjects)

    def addActor(s, a):
        "Adds the given `Actor` to the room."
        s.actors.add(a)
        s.space.add(a.shape, a.body)

    def removeActor(s, a):
        "Removes the given `Actor` from the room."
        s.actors.remove(a)
        s.space.remove(a.shape, a.body)
        
    def update(s,dt):
        "Updates all the physics objects in the room."
        s.space.step(dt)

    def draw(s):
        "Draws the room and all its contents."
        for ter in s.terrain:
            ter.draw()
        for act in s.actors:
            act.draw()

class Zone(object):
    """A collection of interconnected `Room`s.  A Zone
defines the boss, background, color palette, tile set,
music, types of enemies...

One question is, do rooms have a reference to a Zone that defines
all these things, or does a Zone just generate a Room with thematic
properties?"""
    def __init__(s):
        pass
