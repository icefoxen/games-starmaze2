import itertools
import math
import random

import pyglet
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util

from component import *
import resource
import images

class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s, batch=None):
        s.batch = batch or pyglet.graphics.Batch()
        s.physicsObj = None #PhysicsObj(s)


        #lines = cornersToLines(s.corners)
        #colors = [(255, 255, 255, 255) for _ in lines]
        #image = LineImage(lines, colors)
        s.sprite = None #LineSprite(image)

        s.alive = True

        s.moveForce = 400
        s.brakeForce = 400
        s.motionX = 0
        s.braking = False
        # XXX Circular reference, might be better as a weak reference
        s.world = None
    

    # def _setPosition(s, pt):
    #     s.physicsObj.position = pt

    # position = property(lambda s: s.body.position, _setPosition,
    #                     doc="The position of the Actor.")

    def draw(s):
        if (s.sprite is not None) and (s.physicsObj is not None):
            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            s.sprite.draw()

    def onDeath(s):
        pass

    def update(s, dt):
        pass

class Player(Actor):
    """The player object."""
    def __init__(s, keyboard, batch=None):
        s.radius = 20
        Actor.__init__(s, batch)
        s.keyboard = keyboard
        s.controller = KeyboardController(s, keyboard)
        s.physicsObj = PlayerPhysicsObj(s)
        img = resource.getLineImage(images.playerImage)
        s.sprite = LineSprite(s, img)

        s.powers = PowerSet(s)
        s.facing = FACING_RIGHT

    def update(s, dt):
        s.controller.update(dt)
        s.powers.update(dt)

    def switchPowers(s, power):
        "Switches to the given power.  Should eventually do shiny things and such."
        s.currentPower = power


class Collectable(Actor):
    """Something you can collect which does things to you,
whether restoring your health or unlocking a new Power or whatever."""

    def __init__(s, batch=None):
        Actor.__init__(s, batch)
        s.life = 15.0
        s.setCollisionCollectable()
        s.sprite = CollectableSprite(s)

    def setupPhysics(s):
        s.corners = []
        s.corners.append(rectCornersCenter(0, 0, 20, 10))
        s.corners.append(rectCornersCenter(0, 0, 10, 20))

        s.body = pymunk.Body(1, 200)
        s.shapes = [
            pymunk.Poly(s.body, c)
            for c in s.corners
            ]
        for shape in s.shapes:
            #shape.friction = 5.8
            shape.elasticity = 0.9

    def collect(s, player):
        print "Collected!"

    def update(s, dt):
        s.life -= dt
        if s.life < 0:
            s.alive = False

class Powerup(Actor):
    """A Collectable that doesn't time out and doesn't move."""
    def __init__(s):
        Actor.__init__(s)
        s.setCollisionCollectable()

    def setupPhysics(s):
        s.corners = []
        s.corners.append(rectCornersCenter(0, 0, 20, 20))
        s.body = pymunk.Body()
        s.shapes = [
            pymunk.Poly(s.body, c)
            for c in s.corners
            ]
        for shape in s.shapes:
            #shape.friction = 5.8
            shape.elasticity = 0.9

    def setupSprite(s):
        lineList = [cornersToLines(cs) for cs in s.corners]

        allLines = list(itertools.chain.from_iterable(lineList))
        colors = [(128, 128, 255, 255) for _ in allLines]
        image = LineImage(allLines, colors)
        s.sprite = LineSprite(s, image)

    def collect(s, player):
        print "Powered up!"

class BeginningP1Bullet(Actor):
    def __init__(s, x, y, direction):
        Actor.__init__(s)
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)
        s.physicsObj = PlayerBulletPhysicsObj(s, position=(x, y+yOffset))

        image = resource.getLineImage(images.beginningsP1Bullet)
        s.sprite = LineSprite(s, image)
        xImpulse = 400 * direction
        yImpulse = yOffset * 10
        s.physicsObj.apply_impulse((xImpulse, yImpulse))
        # Counteract gravity?
        s.physicsObj.apply_force((0, 400))
        s.life = 0.4 + (random.random() / 3.0)

    def update(s, dt):
        s.life -= dt
        if s.life < 0.0:
            s.alive = False

    def onDeath(s):
        pass
        print 'bullet died'

class BeginningsPower(object):
    "The Beginnings elemental power set."
    def __init__(s):
        pass

    def update(s, dt):
        pass

    def attack1(s):
        x, y = s.owner.physicsObj.position
        direction = 1
        bullet = BeginningP1Bullet(x, y, direction)
        s.owner.world.addActor(bullet)

    def attack2(s):
        pass

    def defend(s):
        pass

    def jump(s):
        pass

class PowerSet(Component):
    def __init__(s, owner):
        Component.__init__(s, owner)
        #s.currentPower = BeginningsPower(

    def update(s, dt):
        pass

    def attack1(s):
        x, y = s.owner.physicsObj.position
        direction = 1
        bullet = BeginningP1Bullet(x, y, s.owner.facing)
        s.owner.world.birthActor(bullet)


    def attack2(s):
        pass

    def defend(s):
        pass

    def jump(s):
        pass


    def nextPower(s):
        pass

    def prevPower(s):
        pass

    def unlockPower(s, power):
        pass
