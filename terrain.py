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
    def __init__(s, position, corners, color):
        s.corners = corners
        s.color = color
        Actor.__init__(s)
        s.physicsObj = BlockPhysicsObj(s, position=position)

        verts = [Vertex(x, y, color) for (x,y) in corners]
        poly = Polygon(verts)
        s.img = LineImage([poly])
        s.renderer = rcache.getRenderer(BlockRenderer)

    def findShapeCenter(s, corners):
        maxx = 0
        maxy = 0
        for (x,y) in corners:
            maxx = max(maxx, x)
            maxy = max(maxy, y)
        return (maxx/2, maxy/2)

@described
class DestroyableBlock(Block):
    """A block that can be blown up by shooting it."""
    def __init__(s, position, corners, color, hp=20):
        Block.__init__(s, position, corners, color)
        s.life = Life(s, hp)
        # XXX
        # We create and then throw away a physicsObj in the Block.__init__ call,
        # which I'd prefer to avoid, but...
        s.physicsObj = BlockPhysicsObj(s, position=position)

@described
class FallingBlock(Actor):
    """A block that falls when the player lands on it.
"""
    def __init__(s, position, corners, color):
        s.corners = corners
        s.color = color
        Actor.__init__(s)
        s.physicsObj = FallingBlockPhysicsObj(s, position=position)

        
        verts = [Vertex(x, y, color) for (x,y) in corners]
        poly = Polygon(verts)
        s.img = LineImage([poly])
        s.renderer = rcache.getRenderer(BlockRenderer)

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
    def __init__(s, position, destination, destx, desty):
        Actor.__init__(s)
        s.physicsObj = GatePhysicsObj(s, position=position)
        s.passable = True
        s.destination = destination
        s.destx = destx
        s.desty = desty
        s.rotation = 0.0
        s.renderer = rcache.getRenderer(GateRenderer)

    def update(s, dt):
        s.rotation += dt


@described
class Tree(Actor):
    def __init__(s, position):
        Actor.__init__(s)
        s.physicsObj = PhysicsObj(s, position=position)
        s.img = rcache.getLineImage(images.tree)
        s.renderer = rcache.getRenderer(TreeRenderer)

# TODO:
# Door, locked door/gate, keys, proper

class Room(object):
    """Basically a specification of a bunch of Actors to create,
along with code to create them.
    """
    def __init__(s, name, zone, descr):
        s.name = name
        s.descr = descr
        s.music = None
        s.gatePoints = []
        s.zone = zone
        s.zone.addRoom(s)

    def getActors(s):
        return map(lambda descfunc: descfunc(), s.descr)

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
    def __init__(s, name):
        s.name = name
        s.music = None
        s.rooms = {}
        s.backgroundActors = []

    def addRoom(s, room):
        s.rooms[room.name] = room

    def generate(s):
        pass
    

    def getZoneActors(s):
        """Returns a list of actors that any Room in the Zone should have.
Mainly, backgrounds."""
        return s.backgroundActors
