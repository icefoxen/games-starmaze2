# -*- coding: utf-8 -*-

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
    def __init__(self, position, corners, color):
        self.corners = corners
        self.color = color
        Actor.__init__(self)
        self.physicsObj = BlockPhysicsObj(self, position=position)

        verts = [Vertex(x, y, color) for (x,y) in corners]
        poly = Polygon(verts)
        self.img = LineImage([poly])
        self.renderer = rcache.getRenderer(BlockRenderer)

    def findShapeCenter(self, corners):
        maxx = 0
        maxy = 0
        for (x,y) in corners:
            maxx = max(maxx, x)
            maxy = max(maxy, y)
        return (maxx/2, maxy/2)

@described
class DestroyableBlock(Block):
    """A block that can be blown up by shooting it."""
    def __init__(self, position, corners, color, hp=20):
        Block.__init__(self, position, corners, color)
        self.life = Life(self, hp)

@described
class PassableBlock(Block):
    """A block that you can pass through."""
    def __init__(self, position, corners, color):
        Block.__init__(self, position, corners, color)
        # XXX
        # We create and then throw away a physicsObj in the Block.__init__ call,
        # which I'd prefer to avoid, but...
        self.physicsObj = PhysicsObj(self, position=position)

        
@described
class FallingBlock(Actor):
    """A block that falls when the player lands on it.
"""
    def __init__(self, position, corners, color):
        self.corners = corners
        self.color = color
        Actor.__init__(self)
        self.physicsObj = FallingBlockPhysicsObj(self, position=position)

        
        verts = [Vertex(x, y, color) for (x,y) in corners]
        poly = Polygon(verts)
        self.img = LineImage([poly])
        self.renderer = rcache.getRenderer(BlockRenderer)

def createBlockCenter(x, y, w, h, color=(255, 255, 255, 255)):
    """Creates a `Terrain` object representing a block of the given size.
x and y are the coordinates of the center."""
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCornersCenter(0, 0, w, h)
    t = Block((x, y), corners, color)
    return t

def createBlockCorner(x, y, w, h, color=(255, 255, 255, 255)):
    """Creates a `Terrain` object representing a block of the given size.
x and y are the coordinates of the lower-left point."""
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCornersCorner(0, 0, w, h)
    t = Block((x, y), corners, color)
    return t

@described
class Gate(Actor):
    def __init__(self, position, destination, destx, desty):
        Actor.__init__(self)
        self.physicsObj = GatePhysicsObj(self, position=position)
        self.passable = True
        self.destination = destination
        self.destx = destx
        self.desty = desty
        self.rotation = 0.0
        self.renderer = rcache.getRenderer(GateRenderer)

    def update(self, dt):
        self.rotation += dt


@described
class Tree(Actor):
    def __init__(self, position):
        Actor.__init__(self)
        self.physicsObj = PhysicsObj(self, position=position)
        self.img = rcache.getLineImage(images.tree)
        self.renderer = rcache.getRenderer(TreeRenderer)

# TODO:
# Door, locked door/gate, keys, proper

class Room(object):
    """Basically a specification of a bunch of Actors to create,
along with code to create them.
    """
    def __init__(self, name, zone, descr):
        self.name = name
        self.descr = descr
        self.music = None
        self.gatePoints = []
        self.zone = zone
        self.zone.addRoom(self)

    def getActors(self):
        return map(lambda descfunc: descfunc(), self.descr)

    @staticmethod
    def fromLayout(name, layout):
        descrs = []
        for (x,y), chunk in layout.iteritems():
            dx = x * chunk.size
            dy = y * chunk.size
            d = chunk.getRelocatedDescrs((dx, dy))
            descrs += d
        return Room(name, descrs)
 

class Zone(object):
    """A collection of Rooms, with connections between them,
and also Zone-wide properties like music and background.

Will eventually dynamically generate and connect rooms, but
for now, they're all wired together in a fixed layout."""
    def __init__(self, name):
        self.name = name
        self.music = None
        self.rooms = {}
        self.backgroundActors = []

    def addRoom(self, room):
        self.rooms[room.name] = room

    def generate(self):
        pass
    

    def getZoneActors(self):
        """Returns a list of actors that any Room in the Zone should have.
Mainly, backgrounds."""
        return self.backgroundActors
