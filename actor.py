import itertools
import math
import random

import pyglet
from pyglet.sprite import *
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util

from component import *
import rcache
import images
from powers import *

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
    
    def draw(s, shader):
        if (s.sprite is not None) and (s.physicsObj is not None):
            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            s.sprite.draw()

    def onDeath(s):
        pass

    def update(s, dt):
        pass

    def takeDamage(s, damager, damage):
        s.life -= damage
        print "Damaged, life is now", s.life
        if s.life <= 0:
            s.alive = False

class Player(Actor):
    """The player object."""
    def __init__(s, keyboard, batch=None):
        s.radius = 20
        Actor.__init__(s, batch)
        s.keyboard = keyboard
        s.controller = KeyboardController(s, keyboard)
        s.physicsObj = PlayerPhysicsObj(s)
        img = rcache.getLineImage(images.playerImage)
        s.sprite = LineSprite(s, img)
        #img = rcache.get_image('playertest')
        #s.sprite = Sprite(img)

        # Experimental glow effect, just overlay the sprite
        # with a diffuse, alpha-blended sprite.  Works surprisingly well.
        glowImage = rcache.getLineImage(images.playerImageGlow)
        s.glowSprite = LineSprite(s, glowImage)
        #glowImage = rcache.get_image('playertest')
        #s.glowSprite = Sprite(img)

        s.powers = PowerSet(s)
        s.facing = FACING_RIGHT
        s.glow = 0.0

        s.door = None

        s.life = Life(s, 100)

    def update(s, dt):
        s.controller.update(dt)
        s.powers.update(dt)
        s.glow += 0.05

    def draw(s, shader):
        if (s.sprite is not None) and (s.physicsObj is not None):
            #s.glowSprite.position = s.physicsObj.position
            #s.glowSprite.rotation = math.degrees(s.physicsObj.angle)
            #s.glowSprite.draw()

            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            s.sprite.draw()


            glow = -0.3 * abs(math.sin(s.glow))
            shader.uniformf("vertexDiff", 0, 0, 0.0, glow)
            shader.uniformf("colorDiff", 0, 0, 0, glow)
            
            s.powers.draw(shader)
            
            shader.uniformf("alpha", 0.2)
            s.glowSprite.position = s.physicsObj.position
            s.glowSprite.draw()

class Collectable(Actor):
    """Something you can collect which does things to you,
whether restoring your health or unlocking a new Power or whatever."""

    def __init__(s, batch=None):
        Actor.__init__(s, batch)
        s.physicsObj = CollectablePhysicsObj(s)
        s.sprite = LineSprite(s, rcache.getLineImage(images.collectable))
        s.life = TimedLife(s, 15)

    def collect(s, player):
        print "Collected collectable!"

    def update(s, dt):
        s.life.update(dt)

class BeginningsPowerup(Actor):
    "Powerups don't time out and don't move."
    def __init__(s):
        Actor.__init__(s)
        s.physicsObj = PowerupPhysicsObj(s)
        img = rcache.getLineImage(images.powerup)
        s.sprite = LineSprite(s, img)

    def collect(s, player):
        print "Gained Beginnings power!"
        player.powers.addPower(BeginningsPower(player))

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


class CrawlerEnemy(Actor):
    """"""
    def __init__(s, batch=None):
        s.radius = 20
        Actor.__init__(s, batch)
        #s.controller = KeyboardController(s, keyboard)
        s.physicsObj = CrawlerPhysicsObj(s)
        img = rcache.getLineImage(images.crawler)
        s.sprite = LineSprite(s, img)
        #glowImage = resource.getLineImage(images.playerImageGlow)
        #s.glowSprite = LineSprite(s, glowImage)

        #s.powers = PowerSet(s)
        s.facing = FACING_RIGHT
        s.life = Life(s, 3, reduction=8)

    def update(s, dt):
        # If it flips off of upright, apply restoring force.
        # XXX: Can we make these beasties stick to walls?
        # In the end that will all be the job of a Controller
        # object; sticking to walls will be easy just by applying
        # a force toward the wall with some friction, and maybe
        # countering gravity if necessary
        #movementForce = 100 * dt
        #if s.physicsObj.angle < -0.3:
        #    s.physicsObj.apply_impulse((-movementForce*10, 0), r=(0, 50))
        #elif s.physicsObj.angle > 0.3:
        #    s.physicsObj.apply_impulse((movementForce*10, 0), r=(0, 50))
        #else:
        #    s.physicsObj.apply_impulse((movementForce * s.facing, 0))
        #s.physicsObj.angle -= dt
        pass
        #s.controller.update(dt)
        #s.powers.update(dt)

    def draw(s, shader):
        if (s.sprite is not None) and (s.physicsObj is not None):
            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            s.sprite.draw()

    def onDeath(s):
        c = Collectable()
        c.physicsObj.position = s.physicsObj.position
        yForce = 350
        xForce = (random.random() * 150) - 75
        c.physicsObj.apply_impulse((xForce, yForce))
        s.world.birthActor(c)


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
