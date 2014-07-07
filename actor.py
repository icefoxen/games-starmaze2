import itertools
import math

import pyglet
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util

from graphics import *

class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s, batch=None):
        s.batch = batch or pyglet.graphics.Batch()
        s.setupPhysics()
        s.setupSprite()

        s.alive = True

        s.moveForce = 400
        s.brakeForce = 400
        s.motionX = 0
        s.braking = False

    def setupPhysics(s):
        """Sets up the actor-specific shape and physics parameters.
Override in children and it will be called in `__init__`."""
        s.corners = rectCorners(0, 0, 10, 10)
        s.body = pymunk.Body(1, 200)
        s.shapes = [pymunk.Poly(s.body, s.corners, radius=1)]
        s.shapes[0].friction = 5.8
        s.body.position = (0,0)

    def setupSprite(s):
        """Sets up the actor-specific sprite and graphics stuff.
Override in children and it will be called in `__init__`."""
        lines = cornersToLines(s.corners)
        colors = [(255, 255, 255, 255) for _ in lines]
        image = LineImage(lines, colors)
        s.sprite = LineSprite(image)


    def _setPosition(s, pt):
        s.body.position = pt

    position = property(lambda s: s.body.position, _setPosition,
                        doc="The position of the Actor.")

    def draw(s):
        #pymunk.pyglet_util.draw(s.shape)
        s.sprite.position = s.body.position
        s.sprite.rotation = math.degrees(s.body.angle)
        s.sprite.draw()

    def stopMoving(s):
        s.motionX = 0
    
    def moveLeft(s):
        s.motionX = -1

    def moveRight(s):
        s.motionX = 1

    def brake(s):
        s.braking = True
    def stopBrake(s):
        s.braking = False

    def update(s, dt):
        if s.braking:
            (vx, vy) = s.body.velocity
            if vx > 0:
                s.body.apply_impulse((-s.brakeForce * dt, 0))
            else:
                s.body.apply_impulse((s.brakeForce * dt, 0))
        else:
            xImpulse = s.moveForce * s.motionX * dt
            s.body.apply_impulse((xImpulse, 0))

        #fmtstr = "position: {} force: {} torque: {}"
        #print fmtstr.format(s.body.position, s.body.force, s.body.torque)

    def die(s):
        pass

class Player(Actor):
    """The player object."""
    def __init__(s, keyboard):
        super(s.__class__, s).__init__()
        s.keyboard = keyboard

    def update(s, dt):
        s.handleInputState()
        super(s.__class__, s).update(dt)

    def setupPhysics(s):
        s.radius = 20
        s.body = pymunk.Body(1, 200)
        s.shapes = [pymunk.Circle(s.body, radius=s.radius)]
        for shape in s.shapes:
            shape.friction = 5.8
            shape.collision_type = 1
        s.body.position = (0,0)

    def setupSprite(s):
        lineList = []
        corners1 = circleCorners(0, 0, s.radius)
        lineList.append(cornersToLines(corners1))

        spokeLength = s.radius + 18
        spokeBase = 8
        lineList.append(lineCorners(0, spokeBase, spokeLength, 0))
        lineList.append(lineCorners(0, -spokeBase, spokeLength, 0))
        lineList.append(lineCorners(spokeBase, 0, 0, spokeLength))
        lineList.append(lineCorners(-spokeBase, 0, 0, spokeLength))
        lineList.append(lineCorners(0, spokeBase, -spokeLength, 0))
        lineList.append(lineCorners(0, -spokeBase, -spokeLength, 0))
        lineList.append(lineCorners(spokeBase, 0, 0, -spokeLength))
        lineList.append(lineCorners(-spokeBase, 0, 0, -spokeLength))

        allLines = list(itertools.chain.from_iterable(lineList))
        colors = [(64, 224, 64, 255) for _ in allLines]
        image = LineImage(allLines, colors)
        s.sprite = LineSprite(image)



    def handleInputState(s):
        """Handles level-triggered keyboard actions; ie
things that keep happening as long as you hold the button
down."""
        #print 'bop'
        s.stopBrake()
        s.stopMoving()
        if s.keyboard[key.DOWN]:
            s.brake()
        elif s.keyboard[key.LEFT]:
            s.moveLeft()
        elif s.keyboard[key.RIGHT]:
            s.moveRight()

        # Jump, maybe
        if s.keyboard[key.UP]:
            pass

        if s.keyboard[key.SPACE]:
            print "Enter room"

        # Powers
        if s.keyboard[key.A]:
            pass
        if s.keyboard[key.S]:
            pass
        if s.keyboard[key.D]:
            pass
        if s.keyboard[key.W]:
            pass


    def handleInputEvent(s, k, mod):
        """Handles edge-triggered keyboard actions (key presses, not holds)"""
        # Switch powers
        if k == key.Q:
            print 'foo'
        elif k == key.E:
            print 'bar'


class Collectable(Actor):
    """Something you can collect which does things to you,
whether restoring your health or unlocking a new Power or whatever."""

    def __init__(s):
        super(s.__class__, s).__init__()
        s.life = 15.0

    def setupPhysics(s):
        s.corners = []
        s.corners.append(rectCorners(0, 0, 20, 10))
        s.corners.append(rectCorners(0, 0, 10, 20))

        s.body = pymunk.Body(1, 200)
        s.shapes = [
            pymunk.Poly(s.body, c)
            for c in s.corners
            ]
        for shape in s.shapes:
            #shape.friction = 5.8
            shape.elasticity = 0.9
            shape.collision_type = 2
        
        s.body.position = (0,0)

    def setupSprite(s):
        lineList = [cornersToLines(cs) for cs in s.corners]

        allLines = list(itertools.chain.from_iterable(lineList))
        colors = [(192, 0, 0, 255) for _ in allLines]
        image = LineImage(allLines, colors)
        s.sprite = LineSprite(image)

    def collect(s, player):
        pass

    def update(s, dt):
        s.life -= dt
        if s.life < 0:
            s.alive = False
