import random

import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util

from graphics import *
from actor import *
from component import *
from util import *

@described
class Block(Actor):
    """A wall, platform, or other terrain feature in a room.
Currently, just uses a `pymunk.Poly` for its shape.  More complex
things might come later.

corners is a list of the corners of the polygon.  NOT line endpoins.
"""
    def __init__(s, position, corners, color, batch=None):
        s.corners = corners
        s.color = color
        Actor.__init__(s, batch)
        s.physicsObj = BlockPhysicsObj(s, position=position)
        #xf, yf = s.findShapeCenter(corners)
        #s.physicsObj.position = (x+xf,y+yf)
        s.sprite = BlockSprite(s, corners, color, batch=batch)

    def findShapeCenter(s, corners):
        maxx = 0
        maxy = 0
        for (x,y) in corners:
            maxx = max(maxx, x)
            maxy = max(maxy, y)
        return (maxx/2, maxy/2)


@described
class FallingBlock(Actor):
    """A block that falls when the player lands on it.
"""
    def __init__(s, position, corners, color, batch=None):
        s.corners = corners
        s.color = color
        Actor.__init__(s, batch)
        s.physicsObj = FallingBlockPhysicsObj(s, position=position)
        s.sprite = BlockSprite(s, corners, color, batch=batch)


def createBlockCenter(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    """Creates a `Terrain` object representing a block of the given size.
x and y are the coordinates of the center."""
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCornersCenter(0, 0, w, h)
    t = Block((x, y), corners, color, batch)
    return t

def createBlockCorner(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    """Creates a `Terrain` object representing a block of the given size.
x and y are the coordinates of the lower-left point."""
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCornersCorner(0, 0, w, h)
    t = Block((x, y), corners, color, batch)
    return t

@described
class Door(Actor):
    def __init__(s, position, destination, destx, desty):
        Actor.__init__(s)
        s.physicsObj = DoorPhysicsObj(s, position=position)
        img = rcache.getLineImage(images.door)
        s.sprite = LineSprite(s, img)
        s.passable = True
        s.destination = destination
        s.destx = destx
        s.desty = desty
        s.rotation = 0.0
        s.sprite.position = s.physicsObj.position

    def update(s, dt):
        s.rotation += dt * 100

    def draw(s, shader):
        s.sprite.rotation = s.rotation
        s.sprite.draw()
        s.sprite.rotation = -s.rotation
        s.sprite.draw()


@described
class Tree(Actor):
    def __init__(s, position):
        Actor.__init__(s)
        s.physicsObj = PhysicsObj(s, position=position)
        img = rcache.getLineImage(images.tree)
        s.sprite = LineSprite(s, img)

    
class Room(object):
    """Basically a specification of a bunch of Actors to create,
along with code to create them.
    """
    def __init__(s, name, descr):
        s.name = name
        s.descr = descr

        # XXX: Not sure if rooms should handle more stuff themselves
        # or whether the world should just do it.
        # s.world = world

        # s.newActors = set()
        # s.actorsToRemove = set()
        # s.activeActors = set()

        # s.initNewSpace()

    def getActors(s):
        return map(lambda descfunc: descfunc(), s.descr)

 

enteranceDirection = enum("LEFT", "RIGHT", "UP", "DOWN")

class Chunk(object):
    """A piece of terrain; rooms are created by selecting and tiling together
Chunks into a grid.

Chunks have information that tells where entrances are, so they can be matched
up to fit.  For now this only tells direction...

XXX: For the moment, Chunks are fixed-size, 500x500 units (pixels).  Sometime
in the future it may be possible to make Chunks smaller or larger; to keep it
from being an unsolvable (and irritating) knapsack problem, I suggest limiting
it to power-of-two (250, 500, 1000, 2000 units, etc)."""

    def __init__(s):
        s.entrances = []
        s.size = 500
        s.descrs = []

    
    def hasEntrance(s, entrance):
        return entrance in s.entrances

    def getDescriptions(s):
        pass
