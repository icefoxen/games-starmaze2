import itertools
import math

import pyglet
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util

from component import *

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
    

    def _setPosition(s, pt):
        s.body.position = pt

    position = property(lambda s: s.body.position, _setPosition,
                        doc="The position of the Actor.")

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
        s.sprite = PlayerSprite(s)

        s.currentPower = Power(s)

    def update(s, dt):
        s.controller.update(dt)
        s.currentPower.update(dt)
        # if s.braking:
        #     (vx, vy) = s.body.velocity
        #     if vx > 0:
        #         s.body.apply_impulse((-s.brakeForce * dt, 0))
        #     else:
        #         s.body.apply_impulse((s.brakeForce * dt, 0))
        # else:
        #     xImpulse = s.moveForce * s.motionX * dt
        #     s.body.apply_impulse((xImpulse, 0))

        # #fmtstr = "position: {} force: {} torque: {}"
        # #print fmtstr.format(s.body.position, s.body.force, s.body.torque)

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
        s.sprite = LineSprite(image)

    def collect(s, player):
        print "Powered up!"


class Power(object):
    """A class representing a set of powers for the player.
This is also a null class which does nothing, handily."""
    def __init__(s, player):
        s.player = player

    def update(s, dt):
        pass

    def attack1(s):
        print "attack1"

    def attack2(s):
        print "attack2"

    def defend(s):
        print "Defend"

    def jump(s):
        print "jump"

class BeginningsPower(Power):
    "The Beginnings elemental power set."
    def __init__(s, player):
        Power.__init__(s, player)

    def update(s, dt):
        pass

    def attack1(s):
        pass

    def attack2(s):
        pass

    def defend(s):
        pass

    def jump(s):
        pass

class PowerSet(Component):
    def __init__(s, owner):
        Component.__init__(owner)

    def update(s, dt):
        pass

    def attack1(s):
        pass

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
