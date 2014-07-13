import random

import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util

from graphics import *
from actor import *


STATIC_BODY = pymunk.Body()

class Block(Actor):
    """A wall, platform, or other terrain feature in a room.
Currently, just uses a `pymunk.Poly` for its shape.  More complex
things might come later.

corners is a list of the corners of the polygon.  NOT line endpoins.
"""
    def __init__(s, corners, color, batch=None):
        s.corners = corners
        s.color = color
        Actor.__init__(s, batch)
        s.physicsObj = BlockPhysicsObj(s)

    def setupPhysics(s):
        s.body = STATIC_BODY
        s.shapes=[pymunk.Poly(s.body, s.corners)]
        for shape in s.shapes:
            shape.friction = 0.8
            shape.elasticity = 0.8

        s.setCollisionTerrain()

    def setupSprite(s):        
        lines = cornersToLines(s.corners)
        colors = colorLines(lines, s.color)

        image = LineImage(lines, colors, batch=s.batch)
        s.sprite = LineSprite(image, batch=s.batch)

class BlockDescription(object):
    """An object that contains the description of a `Block` with none
of the runtime data."""
    def __init__(s, x, y, w, h, color):
        s.x = x
        s.y = y
        s.w = w
        s.h = h
        s.color = color

    def create(s):
        """Returns the block described by this."""
        return createBlockCorner(s.x, s.y, s.w, s.h, color=s.color)

    @staticmethod
    def fromObject(block):
        """Returns a `BlockDescription` for the given `Block`."""
        color = block.color
        x = 999999
        y = 999999
        w = 0
        h = 0
        for corner in block.corners:
            cx, cy = corner
            x = min(x, cx)
            y = min(y, cy)
            w = max(w, cx - x)
            h = max(h, cy - y)
        return BlockDescription(x, y, w, h, color)

def createBlockCenter(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    """Creates a `Terrain` object representing a block of the given size.
x and y are the coordinates of the center."""
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCornersCenter(x, y, w, h)
    t = Block(corners, color, batch)
    return t

def createBlockCorner(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    """Creates a `Terrain` object representing a block of the given size.
x and y are the coordinates of the lower-left point."""
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCornersCorner(x, y, w, h)
    t = Block(corners, color, batch)
    return t

class Door(object):
    def __init__(s, position, destination):
        s.position = position
        s.passable = True
        s.destination = target

        x, y = s.position
        s.corners = rectCornersCenter(x, y, 30, 30)
        lines = cornersToLines(s.corners)
        colors = colorLines(lines, (255, 0, 255, 255))
        image = LineImage(lines, colors)
        s.sprite = LineSprite(image)
        s.sprite.position = s.position

    def draw(s):
        s.sprite.draw()

    def intersecting(s, player):
        """Returns true if the player is in the door, and can wander through it.
Doors don't move, are rectangular, don't do physics-y things, and are only 
tested for collision rarely, so we just use a simple bounding circle. """
        pass

    def enter(s, player):
        pass

class Room(object):
    """Basically a specification of a bunch of Actors to create,
along with code to create them.
    """
    def __init__(s):
        s.name = ""
        s.descr = [
            BlockDescription(-300, -200, 600, 30, COLOR_WHITE),
            BlockDescription(-330, -200, 30, 400, COLOR_WHITE),
            BlockDescription(300, -200, 30, 400, COLOR_WHITE),
            BlockDescription(-100, -100, 200, 30, COLOR_WHITE),
            ]

    def getActors(s):
        return map(lambda desc: desc.create(), s.descr)

def makeSomeRoom():
    r = Room()
    return r

class Zone(object):
    """A collection of interconnected `Room`s.  A Zone
defines the boss, background, color palette, tile set,
music, types of enemies...

One question is, do rooms have a reference to a Zone that defines
all these things, or does a Zone just generate a Room with thematic
properties?"""
    def __init__(s):
        pass

