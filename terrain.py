import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util

from graphics import *




STATIC_BODY = pymunk.Body()

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

corners is a list of the corners of the polygon.  NOT line endpoins.
"""
    def __init__(s, corners, colors, batch=None):
        "Create a `Terrain` object."
        s.corners = corners
        s.colors = colors
        s.body = STATIC_BODY #pymunk.Body(mass=None, moment=None)
        poly = pymunk.Poly(s.body, corners)
        poly.friction = 0.8
        poly.elasticity = 0.5
        s.shapes = [poly]

        s.batch = None or pyglet.graphics.Batch()

        lines = cornersToLines(corners)
        #print lines
        s.image = LineImage(lines, colors, batch=batch)
        #print corners
        #s.image = LineImage(corners, colors, batch=batch)
        s.sprite = LineSprite(s.image, batch=batch)
        
    def draw(s):
        "Draws the terrain feature."
        s.sprite.draw()
        #pymunk.pyglet_util.draw(s.physicsObjects)

    def addToSpace(s, space):
        space.add(s.shapes)
        if s.body != STATIC_BODY:
            space.add(s.body)

    def update(s, dt):
        pass

def createBlock(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    "Creates a `Terrain` object representing a block of the given size."
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = [
        (xf, yf), (xf+wf, yf), 
        (xf+wf, yf+hf), (xf, yf+hf),
    ]
    #print "FOO", verts
    colors = []
    if isinstance(color, list):
        if len(color) != len(corners) * 2:
            raise Exception("Color array not right size, expected {}, got {}: {}".format(len(corners) * 2, len(color), color))
        else:
            colors = color
    else:
        if len(color) != 4:
            raise Exception("color is not a 4-tuple: {}".format(color))
        else:
            colors = [color, color] * len(corners)

    t = Terrain(corners, colors, batch)
    return t

class Room(object):
    """A collection of `Terrain` objects and environmental data.
Also handles physics.  There's only ever one `Room` on screen
at a time."""
    def __init__(s):
        s.terrain = set()
        s.space = pymunk.Space()
        s.space.gravity = (0.0, -400.0)

        s.actors = set()

        s.name = ""

    def addTerrain(s, t):
        "Adds a `Terrain` object to the room."
        s.terrain.add(t)
        s.space.add(t.shapes)

    def removeTerrain(s, t):
        "Removes a `Terrain` object."
        s.terrain.remove(t)
        s.space.remove(t.shapes)

    def addActor(s, a):
        "Adds the given `Actor` to the room."
        s.actors.add(a)
        s.space.add(a.shapes, a.body)

    def removeActor(s, a):
        "Removes the given `Actor` from the room."
        s.actors.remove(a)
        s.space.remove(a.shapes, a.body)
        
    def update(s,dt):
        "Updates all the physics objects in the room."
        #print s.space.bodies
        #print s.space.shapes
        s.space.step(dt)
        for act in s.actors:
            act.update(dt)
        aliveActors = []
        deadActors = []
        aliveActors = [act for act in s.actors if act.alive]
        deadActors = [act for act in s.actors if not act.alive]
        s.actors = aliveActors
        for act in deadActors:
            act.die()
            s.space.remove(act.body, act.shapes)


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

