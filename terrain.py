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

 

entranceDirection = enum("LEFT", "RIGHT", "UP", "DOWN")

class Chunk(object):
    """A piece of terrain; rooms are created by selecting and tiling together
Chunks into a grid.

Chunks have information that tells where entrances are, so they can be matched
up to fit.  For now this only tells direction...

XXX: For the moment, Chunks are fixed-size, 500x500 units (pixels).  Sometime
in the future it may be possible to make Chunks smaller or larger; to keep it
from being an unsolvable (and irritating) knapsack problem, I suggest limiting
it to power-of-two (250, 500, 1000, 2000 units, etc).

Oh gods.  Can we rotate or mirror chunks automatically?
The thought makes my nipples tingle in fear.
Also a lot of things, like platforms, become invalid if rotated
though possibly still workable if mirrored left-right.  Hmm.

KISS!

Actually, what would be useful is a routine to automagically block off
particular entrances so we don't have to go through another pass and add
more things to do that later."""

    size = 500

    def __init__(s, name="", descrs=[], entrances=[]):
        s.name = name
        s.entrances = entrances
        s.descrs = descrs
    
    def hasEntrance(s, entrance):
        return entrance in s.entrances

    def _relocateDescr(s, descr, offset):
        ox, oy = offset
        def relocatedDescr():
            act = descr()
            x,y = act.physicsObj.position
            act.physicsObj.position = (x+ox, y+oy)
            return act
        return relocatedDescr

    def getRelocatedDescrs(s, offset):
        """Returns all the descriptors in the Chunk,
relocated to start at the given offset coordinate."""
        acts = [descr() for descr in s.descrs]
        s._relocateActors(offset, acts)
        return acts

    def __str__(s):
        return u"Chunk({})".format(s.name)


def layOutChunks(num, chunklist):
    """Returns a list of relocated descrs made out of num number of chunks
selected from chunklist, laid out at random.
Just feed the list into a Room object and bob's yer uncle

...okay, some invariants.
First, assume all chunks have at least 2 entrances.
Unresolved entrances at the end of the process will be
capped off (TODO: eventually).
"""
    resolvedChunks = {}

    entranceDirectionAll = [entranceDirection.LEFT, entranceDirection.RIGHT,
                            entranceDirection.UP, entranceDirection.DOWN]
    directionChunks = {
            direction : filter(lambda x: x.hasEntrance(direction), chunklist)
            for direction in entranceDirectionAll
            }
    
    def chunkExistsAt(x,y):
        return resolvedChunks.get((x,y),False)

    def chunkExistsPastEntrance(x, y, entrance):
        if entrance == entranceDirection.UP:
            return chunkExistsAt(x, y+1)
        elif entrance == entranceDirection.DOWN:
            return chunkExistsAt(x, y-1)
        elif entrance == entranceDirection.LEFT:
            return chunkExistsAt(x-1, y)
        elif entrance == entranceDirection.RIGHT:
            return chunkExistsAt(x+1, y)
        else:
            raise Exception("Not possible!")        

    def chunkExistsAbove(x,y):
        return chunkExistsAt(x,y+1)

    def chunkExistsBelow(x,y):
        return chunkExistsAt(x,y-1)
    
    def chunkExistsRight(x,y):
        return chunkExistsAt(x+1,y)

    def chunkExistsLeft(x,y):
        return chunkExistsAt(x-1,y)

    def addChunkAt(x,y, chunk):
        resolvedChunks[(x,y)] = chunk

    def inverseDirection(direction):
        if direction == entranceDirection.UP:
            return entranceDirection.DOWN
        elif direction == entranceDirection.DOWN:
            return entranceDirection.UP
        elif direction == entranceDirection.LEFT:
            return entranceDirection.RIGHT
        elif direction == entranceDirection.RIGHT:
            return entranceDirection.LEFT
        else:
            raise Exception("Really not possible!")

    def coordPastDirection(x, y, direction):
        if direction == entranceDirection.UP:
            return (x, y+1)
        elif direction == entranceDirection.DOWN:
            return (x, y-1)
        elif direction == entranceDirection.LEFT:
            return (x-1, y)
        elif direction == entranceDirection.RIGHT:
            return (x+1, y)
        else:
            raise Exception("Really not possible!")

    chunk = random.choice(chunklist)
    addChunkAt(0, 0, chunk)
    numchunks = num - 1
    while True:
        for coord, chunk in resolvedChunks.copy().iteritems():
            x,y = coord
            #print "Numchunks:", numchunks
            for entrance in chunk.entrances:
                if not chunkExistsPastEntrance(x, y, entrance):
                    # Add a chunk there
                    newChunk = random.choice(directionChunks[inverseDirection(entrance)])
                    nx, ny = coordPastDirection(x, y, entrance)
                    #print "Adding chunk at", nx, ny
                    addChunkAt(nx, ny, newChunk)

                    # If we have enough chunks, return.
                    # (Keeping in mind that we started with one)
                    numchunks -= 1
                    if numchunks <= 0: return resolvedChunks


    return resolvedChunks


hall   = Chunk(u'═', [], [entranceDirection.LEFT, entranceDirection.RIGHT])
cross  = Chunk(u'╬', [], [entranceDirection.LEFT, entranceDirection.RIGHT, entranceDirection.UP, entranceDirection.DOWN])
tUp    = Chunk(u'╩', [], [entranceDirection.LEFT, entranceDirection.RIGHT, entranceDirection.UP])
tDown  = Chunk(u'╦', [], [entranceDirection.LEFT, entranceDirection.RIGHT, entranceDirection.DOWN])
tLeft  = Chunk(u'╠', [], [entranceDirection.LEFT, entranceDirection.UP, entranceDirection.DOWN])
tRight = Chunk(u'╣', [], [entranceDirection.RIGHT, entranceDirection.UP, entranceDirection.DOWN])
shaft  = Chunk(u'║', [], [entranceDirection.UP, entranceDirection.DOWN])
ulCorn = Chunk(u'╔', [], [entranceDirection.RIGHT, entranceDirection.DOWN])
llCorn = Chunk(u'╚', [], [entranceDirection.RIGHT, entranceDirection.UP])
urCorn = Chunk(u'╗', [], [entranceDirection.LEFT, entranceDirection.DOWN])
lrCorn = Chunk(u'╝', [], [entranceDirection.LEFT, entranceDirection.UP])

chunks = [hall, cross, tUp, tDown, tLeft, tRight, shaft, ulCorn, llCorn, urCorn, lrCorn]
layedout = layOutChunks(50, chunks)

print "Number of chunks created:", len(layedout)
chars = []
for i in xrange(-5, 6):
    for j in xrange(-5, 6):
        chunk = layedout.get((i,j))
        if chunk is not None:
            chars.append(chunk.name)
        else:
            chars.append('.')
    chars.append('\n')

print ''.join(chars)
    
