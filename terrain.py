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
        super(s.__class__, s).__init__(batch)

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


def createBlock(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    "Creates a `Terrain` object representing a block of the given size."
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCorners(x, y, w, h)
    t = Block(corners, color, batch)
    return t

class Door(object):
    def __init__(s, position, destination):
        s.position = position
        s.passable = True
        s.destination = target

        x, y = s.position
        s.corners = rectCorners(x, y, 30, 30)
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
    """A collection of `Terrain` objects and environmental data.
Also handles physics.  There's only ever one `Room` on screen
at a time."""
    def __init__(s):
        s.name = ""

    def getActors(s):
        acts = []
        acts.append(createBlock(0, -200, 600, 30))
        acts.append(createBlock(-315, -65, 30, 300))
        acts.append(createBlock(315, -65, 30, 300))
        acts.append(createBlock(-70, -100, 270, 30))

        for i in range(5):
            c = Collectable()
            rx = random.random() * 1000 - 500
            ry = random.random() * 1000
            c.position = (100+rx, 100+ry)
            vx = random.random() * 100
            vy = random.random() * 1000
            c.body.apply_impulse((vx, vy))
            acts.append(c)

        p = Powerup()
        p.position = (0, -150)
        acts.append(p)
        return acts

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

