import random

import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util

from graphics import *
from actor import *
from component import *


class Block(Actor):
    """A wall, platform, or other terrain feature in a room.
Currently, just uses a `pymunk.Poly` for its shape.  More complex
things might come later.

corners is a list of the corners of the polygon.  NOT line endpoins.
"""
    def __init__(s, x, y, corners, color, batch=None):
        s.corners = corners
        s.color = color
        Actor.__init__(s, batch)
        s.physicsObj = BlockPhysicsObj(s)
        s.physicsObj.position = (x,y)
        s.sprite = BlockSprite(s, corners, color, batch=batch)


class BlockDescription(object):
    """An object that contains the description of a `Block` with none
of the runtime data."""
    def __init__(s, x, y, corners, color):
        s.x = x
        s.y = y
        s.corners = corners
        s.color = color

    def create(s):
        """Returns the block described by this."""
        return Block(s.x, s.y, s.corners, s.color)

    @staticmethod
    def fromObject(block):
        """Returns a `BlockDescription` for the given `Block`."""
        color = block.color
        x, y = block.physicsObj.position
        corners = block.corners
        return BlockDescription(x, y, corners, color)

    def __repr__(s):
        return "BlockDescription({}, {}, {}, {})".format(
            s.x, s.y, s.corners, s.color
            )

# def createDescription(actor):
#     """Creates a description object from *any* kind of `Actor`.

#     BUGGO: Keeping this in sync is a pain; it's already fallen out.
#     Also during the game we never actually use this... yet.  Room
#     creation goes strictly from static to dynamic, we should never
#     be _saving_ room state apart from Powerups which are collected
#     once and never respawn, and so should probably be some sort of
#     global state flag.

#     So do we need this at all?  Well it might be quite useful for
#     the level designer, perhaps...  Well maybe not."""
#     if isinstance(actor, Block):
#         return BlockDescription.fromObject(actor)
#     elif isinstance(actor, BeginningsPowerupDescription):
#         return BeginningsPowerupDescription.fromObject(actor)
        
#     else:
#         raise Exception("Type is not describable: ", actor)
    
def createBlockCenter(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    """Creates a `Terrain` object representing a block of the given size.
x and y are the coordinates of the center."""
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCornersCenter(0, 0, w, h)
    t = Block(x, y, corners, color, batch)
    return t

def createBlockCorner(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    """Creates a `Terrain` object representing a block of the given size.
x and y are the coordinates of the lower-left point."""
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    corners = rectCornersCorner(0, 0, w, h)
    t = Block(x, y, corners, color, batch)
    return t

class Door(Actor):
    def __init__(s, x, y, destination, destx, desty):
        Actor.__init__(s)
        s.physicsObj = DoorPhysicsObj(s, position=(x, y))
        img = resource.getLineImage(images.door)
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


class DoorDescription(object):
    def __init__(s, x, y, dest, destx, desty):
        s.x = x
        s.y = y
        s.destination = dest
        s.destx = destx
        s.desty = desty

    def create(s):
        """Returns the block described by this."""
        return Door(s.x, s.y, s.destination, s.destx, s.desty)

    @staticmethod
    def fromObject(door):
        """Returns a `DoorDescription` for the given `Door`."""
        x, y = door.physicsObj.position
        return DoorDescription(x, y, s.destination, s.destx, s.desty)

    def __repr__(s):
        return "Door({}, {}, {}, {}, {})".format(
            s.x, s.y, s.destination, s.destx, s.desty
            )

class Room(object):
    """Basically a specification of a bunch of Actors to create,
along with code to create them.
    """
    def __init__(s, descr=[]):
        s.name = ""
        s.descr = descr

    def getActors(s):
        return map(lambda desc: desc.create(), s.descr)

class RoomDescription(object):
    def __init__(s, descr):
        s.descr = descr

def makeSomeRoom():
    import zone_beginnings
    room = zone_beginnings.STARTROOM
    return room
    
# BUGGO: Again, incomplete
class Zone(object):
    """A collection of interconnected `Room`s.  A Zone
defines the boss, background, color palette, tile set,
music, types of enemies...

One question is, do rooms have a reference to a Zone that defines
all these things, or does a Zone just generate a Room with thematic
properties?"""
    def __init__(s):
        pass

