import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util

from graphics import *
from actor import *


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
    def __init__(s, corners, color, batch=None):
        "Create a `Terrain` object."
        s.corners = corners
        s.body = STATIC_BODY #pymunk.Body(mass=None, moment=None)
        poly = pymunk.Poly(s.body, corners)
        poly.friction = 0.8
        poly.elasticity = 0.5
        s.shapes = [poly]

        s.batch = None or pyglet.graphics.Batch()

        lines = cornersToLines(corners)
        colors = colorLines(lines, color)

        s.image = LineImage(lines, colors, batch=batch)
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
    corners = rectCorners(x, y, w, h)
    t = Terrain(corners, color, batch)
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
        s.space = pymunk.Space()
        s.space.gravity = (0.0, -400.0)
        s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_COLLECTABLE,
            begin=Room.collidePlayerCollectable
        )
    
        s.terrain = set()
        s.actors = set()
        s.doors = set()

        s.name = ""

    @staticmethod
    def collidePlayerCollectable(space, arbiter, *args, **kwargs):
        "The handler for a player collecting a Collectable."
        #print space, arbiter, args, kwargs
        playerShape, collectableShape = arbiter.shapes
        player = playerShape.body.actor
        collectable = collectableShape.body.actor
        collectable.collect(player)
        collectable.alive = False
        return False


    def addTerrain(s, t):
        "Adds a `Terrain` object to the room."
        s.terrain.add(t)
        s.space.add(t.shapes)
        if not t.body.is_static:
            s.space.add(t.body)

    def removeTerrain(s, t):
        "Removes a `Terrain` object."
        s.terrain.remove(t)
        s.space.remove(t.shapes)
        if not t.body.is_static:
            s.space.remove(t.body)

    def addActor(s, a):
        "Adds the given `Actor` to the room."
        s.actors.add(a)
        s.space.add(a.shapes)
        if not a.body.is_static:
            s.space.add(a.body)

    def removeActor(s, a):
        "Removes the given `Actor` from the room."
        s.actors.remove(a)
        s.space.remove(a.shapes)
        if not a.body.is_static:
            s.space.remove(a.body)
        
    def update(s,dt):
        "Updates all the physics objects in the room."
        s.space.step(dt)
        for act in s.actors:
            act.update(dt)
        deadActors = [act for act in s.actors if not act.alive]
        for act in deadActors:
            act.die()
            s.removeActor(act)


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

