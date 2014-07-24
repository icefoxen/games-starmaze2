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
        s.facing = FACING_RIGHT
    

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
        glowImage = resource.getLineImage(images.playerImageGlow)
        s.glowSprite = LineSprite(s, glowImage)

        s.powers = PowerSet(s)
        s.facing = FACING_RIGHT

    def update(s, dt):
        s.controller.update(dt)
        s.powers.update(dt)

    def switchPowers(s, power):
        "Switches to the given power.  Should eventually do shiny things and such."
        s.currentPower = power

    def draw(s):
        if (s.sprite is not None) and (s.physicsObj is not None):
            s.glowSprite.position = s.physicsObj.position
            s.glowSprite.rotation = math.degrees(s.physicsObj.angle)
            s.glowSprite.draw()

            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            s.sprite.draw()


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

class BeginningsPowerup(Actor):
    "Powerups don't time out and don't move."
    def __init__(s):
        Actor.__init__(s)
        s.physicsObj = PowerupPhysicsObj(s)
        img = resource.getLineImage(images.powerup)
        s.sprite = LineSprite(s, img)

    def collect(s, player):
        print "Gained Beginnings power!"
        player.powers.addPower(BeginningsPower())

class BeginningsPowerupDescription(object):
    def __init__(s, x, y):
        s.x = x
        s.y = y

    def create(s):
        p = BeginningsPowerup()
        p.physicsObj.position = (s.x, s.y)
        return p

    @staticmethod
    def fromObject(powerup):
        x, y = powerup.position
        return BeginningsPowerupDescription(x, y)

    def __repr__(s):
        return "BeginningsPowerupDescription({}, {})".format(
            s.x, s.y
            )


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

class NullPower(object):
    "A power set that does nothing."
    def __init__(s):
        pass

    def update(s, dt):
        pass

    def attack1(s, owner):
        pass

    def attack2(s, owner):
        pass

    def defend(s, owner):
        pass

    def jump(s, owner):
        pass
        
class BeginningsPower(object):
    "The Beginnings elemental power set."
    def __init__(s):
        pass

    def update(s, dt):
        pass

    def attack1(s, owner):
        print "Attack1"
        x, y = owner.physicsObj.position
        direction = owner.facing
        bullet = BeginningP1Bullet(x, y, direction)
        owner.world.birthActor(bullet)

    def attack2(s, owner):
        print "Attack2"

    def defend(s, owner):
        print "Defend"

    def jump(s, owner):
        print "Jump"

class PowerSet(Component):
    def __init__(s, owner):
        Component.__init__(s, owner)
        s.powerIndex = 0
        s.powers = [NullPower()]
        s.currentPower = s.powers[s.powerIndex]

    def addPower(s, power):
        # Remove the null power if it exists before adding
        # the other power
        if len(s.powers) == 1 and isinstance(s.powers[0], NullPower):
            s.powers = [power]
        else:
            s.powers.add(power)
            s.powers.sort()
        s.currentPower = power
        s.powerIndex = s.powers.index(power)
        print "ADded power:", power
        print s.powers
        
    def update(s, dt):
        s.currentPower.update(dt)

    def attack1(s):
        s.currentPower.attack1(s.owner)

    def attack2(s):
        s.currentPower.attack2(s.owner)

    def defend(s):
        s.currentPower.defend(s.owner)

    def jump(s):
        s.currentPower.jump(s.owner)


    def nextPower(s):
        s.powerIndex = (s.powerIndex + 1) % len(s.powers)
        s.currentPower = s.powers[s.powerIndex]

    def prevPower(s):
        s.powerIndex = (s.powerIndex - 1) % len(s.powers)
        s.currentPower = s.powers[s.powerIndex]
