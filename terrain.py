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
        return Block((s.x, s.y), s.corners, s.color)

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


class FallingBlockDescription(object):
    def __init__(s, x, y, corners, color):
        s.x = x
        s.y = y
        s.corners = corners
        s.color = color

    def create(s):
        """Returns the block described by this."""
        return FallingBlock((s.x, s.y), s.corners, s.color)

    @staticmethod
    def fromObject(block):
        """Returns a `BlockDescription` for the given `Block`."""
        color = block.color
        x, y = block.physicsObj.position
        corners = block.corners
        return FallingBlockDescription(x, y, corners, color)

    def __repr__(s):
        return "FallingBlockDescription({}, {}, {}, {})".format(
            s.x, s.y, s.corners, s.color
            )
    
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


class DoorDescription(object):
    def __init__(s, x, y, dest, destx, desty):
        s.x = x
        s.y = y
        s.destination = dest
        s.destx = destx
        s.desty = desty

    def create(s):
        """Returns the block described by this."""
        return Door((s.x, s.y), s.destination, s.destx, s.desty)

    @staticmethod
    def fromObject(door):
        """Returns a `DoorDescription` for the given `Door`."""
        x, y = door.physicsObj.position
        return DoorDescription(x, y, s.destination, s.destx, s.desty)

    def __repr__(s):
        return "DoorDescription({}, {}, {}, {}, {})".format(
            s.x, s.y, s.destination, s.destx, s.desty
            )

@described
class Tree(Actor):
    def __init__(s, position):
        Actor.__init__(s)
        s.physicsObj = PhysicsObj(s, position=position)
        img = rcache.getLineImage(images.tree)
        s.sprite = LineSprite(s, img)

class TreeDescription(object):
    def __init__(s, x, y):
        s.x = x
        s.y = y

    def create(s):
        """Returns the block described by this."""
        return Tree((s.x, s.y))

    @staticmethod
    def fromObject(tree):
        """Returns a `DoorDescription` for the given `Door`."""
        x, y = door.position
        return TreeDescription(x, y)

    def __repr__(s):
        return "Tree({}, {})".format(
            s.x, s.y
            )
    
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

 
#     def initNewSpace(s):
#         s.space = pymunk.Space()
#         # XXX: This isn't QUITE the same as a max velocity, but prevents
#         # motion from getting _too_ out of control.
#         s.space.damping = 0.9
#         s.space.gravity = (0.0, GRAVITY_FORCE)
#         s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_COLLECTABLE,
#                                       begin=World.collidePlayerCollectable)
#         s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_TERRAIN,
#                                       begin=World.collidePlayerTerrain,
#                                       separate=World.collidePlayerTerrainEnd)
#         s.space.add_collision_handler(CGROUP_PLAYERBULLET, CGROUP_TERRAIN,
#                                       begin=World.collideBulletTerrain)
#         s.space.add_collision_handler(CGROUP_PLAYERBULLET, CGROUP_ENEMY,
#                                       begin=World.collidePlayerBulletEnemy)
#         s.space.add_collision_handler(CGROUP_ENEMYBULLET, CGROUP_TERRAIN,
#                                       begin=World.collideBulletTerrain)
#         s.space.add_collision_handler(CGROUP_PLAYER, CGROUP_DOOR,
#                                       begin=World.collidePlayerDoor,
#                                       separate=World.collidePlayerDoorEnd)

#     def birthActor(s, act):
#         """You see, we can't have actors add or remove other actors inside
# their update() method, 'cause that'd modify the set of actors while we're
# iterating through it, which is a no-no.

# So instead of calling _addActor directly, call this, which will cause the
# actor to be added next update frame."""
#         s.newActors.add(act)

#     def killActor(s, act):
#         """The complement to birthActor(), kills the given actor so it gets removed next
# update frame."""
#         act.alive = False
#         s.actorsToRemove.add(act)

#     def removeActors(s):
#         for act in s.actorsToRemove:
#             s.removeActorFromSpace(act)
#         s.activeActors.

#     def addActorToSpace(s, act):
#         s.activeActors.add(act)        
#         act.world = s.world

#         if not act.physicsObj.body.is_static:
#             s.space.add(act.physicsObj.body)
#         for b in act.physicsObj.auxBodys:
#             if not b.is_static:
#                 s.space.add(b)
#         for constraint in act.physicsObj.constraints:
#             s.space.add(constraint)
#         for shape in act.physicsObj.shapes:
#             s.space.add(shape)

#     def removeActorFromSpace(s, act):
#         s.actors.remove(act)
#         # Break backlinks
#         # TODO: This should break all backlinks in an actor's
#         # components, too.  Or the actor should have a delete
#         # method that gets called here.  Probably the best way.
#         act.world = None

#         if not act.physicsObj.body.is_static:
#             s.space.remove(act.physicsObj.body)
#         for b in act.physicsObj.auxBodys:
#             if not b.is_static:
#                 s.space.remove(b)
#         for constraint in act.physicsObj.constraints:
#             s.space.remove(constraint)
#         for shape in act.physicsObj.shapes:
#             s.space.remove(shape)



enter = enum("LEFT", "RIGHT", "UP", "DOWN")

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
