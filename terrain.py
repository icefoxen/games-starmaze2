import itertools

import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util



def _addVertsToBatch(verts, colors, batch):
    "Takes a list of points and adds them to the given `pyglet.graphics.Batch` as a `GL_LINE_LOOP`."
    #print verts, colors, batch
    coordsPerVert = 2
    vertFormat = 'v2f'
    colorFormat = 'c4B'
    # Unpack/flatten list
    v = list(itertools.chain.from_iterable(verts))
    c = list(itertools.chain.from_iterable(colors))
    numPoints = len(v) / coordsPerVert
    batch.add(numPoints, 
              pyglet.graphics.GL_LINE_LOOP, 
              None, 
              (vertFormat, v),
              (colorFormat, c)
    )


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

        s.batch = batch or pyglet.graphics.Batch()
        _addVertsToBatch(s.verts, s.colors, s.batch)
        
    def draw(s):
        "Draws the terrain feature."
        s.batch.draw()
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
        for t in s.terrain:
            t.draw()

class Zone(object):
    """A collection of interconnected `Room`s.  A Zone
defines the boss, background, color palette, tile set,
music, types of enemies...

One question is, do rooms have a reference to a Zone that defines
all these things, or does a Zone just generate a Room with thematic
properties?"""
    def __init__(s):
        pass
