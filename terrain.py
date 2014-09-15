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

 

DIRECTION = enum("LEFT", "RIGHT", "UP", "DOWN")
DIRECTIONS = [DIRECTION.LEFT, DIRECTION.RIGHT, DIRECTION.UP, DIRECTION.DOWN]

def oppositeDirection(direction):
    if direction == DIRECTION.UP:
        return DIRECTION.DOWN
    elif direction == DIRECTION.DOWN:
        return DIRECTION.UP
    elif direction == DIRECTION.LEFT:
        return DIRECTION.RIGHT
    elif direction == DIRECTION.RIGHT:
        return DIRECTION.LEFT
    else:
        raise Exception("Invalid direction: {}".format(direction))

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
    # A tuple->chunk table that has the current
    # state of the map.
    resolvedChunks = {}

    # A table where we can specify a direction and get a list of
    # all the chunks that have that direction.
    directionChunks = {
            direction : filter(lambda x: x.hasEntrance(direction), chunklist)
            for direction in DIRECTIONS
            }
    # for d,chunks in directionChunks.iteritems():
    #     print "DIRECTION: {}".format(d)
    #     for c in chunks:
    #         print c.name
    
    def chunkExistsAt(x,y):
        return resolvedChunks.get((x,y),False)

    def coordPastDirection(x, y, direction):
        if direction == DIRECTION.UP:
            return (x, y+1)
        elif direction == DIRECTION.DOWN:
            return (x, y-1)
        elif direction == DIRECTION.LEFT:
            return (x-1, y)
        elif direction == DIRECTION.RIGHT:
            return (x+1, y)
        else:
            raise Exception("Not possible!")        

    def chunkExistsPastEntrance(x, y, entrance):
        coord = coordPastDirection(x, y, entrance)
        return chunkExistsAt(*coord)

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

    chunk = random.choice(chunklist)
    addChunkAt(0, 0, chunk)
    numchunks = num - 1

    def generateChunkOffOfCoord(x, y, chunks):
        chunk = chunks[(x,y)]
        print u"Adding things off chunk {} at {},{}".format(chunk.name, x,y)
        print "Chunk has entrances: {}".format(chunk.entrances)
        # Aieee, this mutates chunk.entrances
        random.shuffle(chunk.entrances)
        for entrance in chunk.entrances:
            print "Checking entrance {}".format(entrance)
            if chunkExistsPastEntrance(x, y, entrance):
                print "Chunk exists past that entrance, skipping."
            else:
                newChunk = random.choice(directionChunks[oppositeDirection(entrance)])
                coord = coordPastDirection(x,y,entrance)
                print u"Selected chunk {} to put at {}".format(newChunk.name, coord)
                return (coord, newChunk)
        # If we get here there are no entrances
        print "No open entrances off that chunk."
        return False

    while True:
        newChunks = {}    
        for (x,y), chunk in resolvedChunks.iteritems():
            print "Adding things off chunk at {},{}".format(x,y)
            print "Chunk has entrances: {}".format(chunk.entrances)
            newChunk = generateChunkOffOfCoord(x, y, resolvedChunks)
            print u"New chunk: {}".format(newChunk)
            if newChunk:
                (x,y), newC = newChunk
                newChunks[(x,y)] = newC
        for coord, chunk in newChunks.iteritems():
            resolvedChunks[coord] = chunk
            if len(resolvedChunks) > num:
                return resolvedChunks
            

        
        # for coord, chunk in resolvedChunks.copy().iteritems():
        #     x,y = coord
        #     #print "Numchunks:", numchunks
        #     for entrance in chunk.entrances:
        #         if not chunkExistsPastEntrance(x, y, entrance):
        #             # Add a chunk there
        #             newChunk = random.choice(directionChunks[oppositeDirection(entrance)])
        #             nx, ny = coordPastDirection(x, y, entrance)
        #             #print "Adding chunk at", nx, ny
        #             addChunkAt(nx, ny, newChunk)

        #             # If we have enough chunks, return.
        #             # (Keeping in mind that we started with one)
        #             numchunks -= 1
        #             if numchunks <= 0: return resolvedChunks
        #         else:
        #             print "Chunk exists at {},{}


    #return resolvedChunks


hall   = Chunk(u'═', [], [DIRECTION.LEFT, DIRECTION.RIGHT])
cross  = Chunk(u'╬', [], [DIRECTION.LEFT, DIRECTION.RIGHT, DIRECTION.UP, DIRECTION.DOWN])
tUp    = Chunk(u'╩', [], [DIRECTION.LEFT, DIRECTION.RIGHT, DIRECTION.UP])
tDown  = Chunk(u'╦', [], [DIRECTION.LEFT, DIRECTION.RIGHT, DIRECTION.DOWN])
tLeft  = Chunk(u'╠', [], [DIRECTION.RIGHT, DIRECTION.UP, DIRECTION.DOWN])
tRight = Chunk(u'╣', [], [DIRECTION.LEFT, DIRECTION.UP, DIRECTION.DOWN])
shaft  = Chunk(u'║', [], [DIRECTION.UP, DIRECTION.DOWN])
ulCorn = Chunk(u'╔', [], [DIRECTION.RIGHT, DIRECTION.DOWN])
llCorn = Chunk(u'╚', [], [DIRECTION.RIGHT, DIRECTION.UP])
urCorn = Chunk(u'╗', [], [DIRECTION.LEFT, DIRECTION.DOWN])
lrCorn = Chunk(u'╝', [], [DIRECTION.LEFT, DIRECTION.UP])

chunks = [hall, cross, tUp, tDown, tLeft, tRight, shaft, ulCorn, llCorn, urCorn, lrCorn]
layedout = layOutChunks(20, chunks)

print "Number of chunks created:", len(layedout)
chars = []
for i in xrange(5, -6, -1):
    for j in xrange(-5, 6):
        chunk = layedout.get((j,i))
        if chunk is not None:
            chars.append(chunk.name)
        else:
            chars.append('.')
    chars.append('\n')

print ''.join(chars)
    
