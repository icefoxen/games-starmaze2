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

        s.life = 1.0
        s.alive = True

        s.moveForce = 400
        s.brakeForce = 400
        s.motionX = 0
        s.braking = False
        # XXX Circular reference, might be better as a weak reference
        s.world = None
        s.facing = FACING_RIGHT
        s.onGround = False
    

    # def _setPosition(s, pt):
    #     s.physicsObj.position = pt

    # position = property(lambda s: s.body.position, _setPosition,
    #                     doc="The position of the Actor.")

    def draw(s):
        if (s.sprite is not None) and (s.physicsObj is not None):
            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            s.sprite.draw()

    def onDeath(s, world):
        pass

    def update(s, dt):
        pass

    def takeDamage(s, damager, damage):
        s.life -= damage
        print "Damaged, life is now", s.life
        if s.life < 0:
            s.alive = False

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

# TODO: Bullet class?
# Should bullets keep a reference to their firer?
# Might be useful, I dunno.  No immediate need for
# it though.

# TODO: WE MIGHT NEED SOME PROPER EVENTS TO OCCUR FOR ACTORS
# Hit ground, leave ground
# Hit terrain, leave contact with terrain
# Hit with attack, touch enemy (useful for bullets)
# Take damage, as well.
# onDeath is already the start of this.
# Other stuff maybe.  Hmmmm.
# We could even consider the more fundamental methods to be
# onDraw and onUpdate, really

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
        
        s.damage = 1

    def update(s, dt):
        s.life -= dt
        if s.life < 0.0:
            s.alive = False

    def onDeath(s, world):
        print 'bullet died'

class BeginningP2Bullet(Actor):
    def __init__(s, x, y, direction):
        Actor.__init__(s)
        s.physicsObj = PlayerBulletPhysicsObj(s, position=(x, y))
        # TODO: Placeholder image
        image = resource.getLineImage(images.powerup)
        s.sprite = LineSprite(s, image)
        xImpulse = 300 * direction
        yImpulse = 200
        s.physicsObj.apply_impulse((xImpulse, yImpulse))
        
        s.damage = 10

    def update(s, dt):
        pass

    def collideWithEnemy(s, enemy):
        enemy.takeDamage(s.damage)

    def onDeath(s, world):
        return
        for i in range(5):
            force = 100
            xForce = math.cos(force)

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
        s.timer = 0.0
        s.refireTime1 = 0.05
        s.refireTime2 = 1.5

    def update(s, dt):
        s.timer -= dt

    # BUGGO: It's concievable we'd have to fire multiple shots in the same frame...
    # If we lag real bad at least.
    # But since that'd currently involve going 20 FPS...
    def attack1(s, owner):
        if s.timer < 0.0:
            s.timer = s.refireTime1
            x, y = owner.physicsObj.position
            direction = owner.facing
            bullet = BeginningP1Bullet(x, y, direction)
            owner.world.birthActor(bullet)

    def attack2(s, owner):
        if s.timer < 0.0:
            s.timer = s.refireTime2
            x, y = owner.physicsObj.position
            direction = owner.facing
            bullet = BeginningP2Bullet(x, y, direction)
            owner.world.birthActor(bullet)

    def defend(s, owner):
        print "Defend"

    def jump(s, owner):
        if owner.onGround:
            owner.physicsObj.apply_impulse((0, 500))
            owner.onGround = False

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

class CrawlerEnemy(Actor):
    """"""
    def __init__(s, batch=None):
        s.radius = 20
        Actor.__init__(s, batch)
        #s.controller = KeyboardController(s, keyboard)
        s.physicsObj = CrawlerPhysicsObj(s)
        img = resource.getLineImage(images.crawler)
        s.sprite = LineSprite(s, img)
        #glowImage = resource.getLineImage(images.playerImageGlow)
        #s.glowSprite = LineSprite(s, glowImage)

        #s.powers = PowerSet(s)
        s.facing = FACING_RIGHT
        s.life = 3

    def update(s, dt):
        # If it flips off of upright, apply restoring force.
        # XXX: Can we make these beasties stick to walls?
        # In the end that will all be the job of a Controller
        # object; sticking to walls will be easy just by applying
        # a force toward the wall with some friction, and maybe
        # countering gravity if necessary
        movementForce = 100 * dt
        if s.physicsObj.angle < -0.3:
            s.physicsObj.apply_impulse((-movementForce*10, 0), r=(0, 50))
        elif s.physicsObj.angle > 0.3:
            s.physicsObj.apply_impulse((movementForce*10, 0), r=(0, 50))
        else:
            s.physicsObj.apply_impulse((movementForce * s.facing, 0))
        #s.controller.update(dt)
        #s.powers.update(dt)

    def draw(s):
        if (s.sprite is not None) and (s.physicsObj is not None):
            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            s.sprite.draw()


class CrawlerEnemyDescription(object):
    def __init__(s, x, y):
        s.x = x
        s.y = y

    def create(s):
        c = CrawlerEnemy()
        c.physicsObj.position = (s.x, s.y)
        return c

    @staticmethod
    def fromObject(crawler):
        x, y = crawler.position
        return CrawlerEnemyDescription(x, y)

    def __repr__(s):
        return "CrawlerEnemyDescription({}, {})".format(
            s.x, s.y
            )
