import itertools
import math

import pyglet
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util

from graphics import *

# COLLISION GROUPS!  \O/
# Collision groups in pymunk determine types
# of objects; you can assign different callbacks to be called
# when objects in different groups collide.
CGROUP_NONE = 0
CGROUP_PLAYER = 1
CGROUP_COLLECTABLE = 2
CGROUP_ENEMY = 3
CGROUP_PLAYERBULLET = 4
CGROUP_ENEMYBULLET = 5
CGROUP_TERRAIN = 6

# Collision layers
# Determine what collides with what at all; probably faster
# and certainly simpler than making a bunch of callbacks between
# collision groups that do nothing.
# Collectables do not collide with enemies or player/enemy bullets
# Enemies do not collide with their own bullets,
# players do not collide with their own bullets.
LAYER_PLAYER       = 0X00000001
LAYER_COLLECTABLE  = 0x00000002
LAYER_ENEMY        = 0x00000004
LAYER_PLAYERBULLET = 0x00000008
LAYER_ENEMYBULLET  = 0x00000010
LAYER_BULLET       = LAYER_PLAYERBULLET & LAYER_ENEMYBULLET
LAYER_TERRAIN      = 0x00000020

LAYERSPEC_ALL         = 0xFFFFFFFF
LAYERSPEC_PLAYER      = LAYERSPEC_ALL & ~LAYER_PLAYERBULLET
LAYERSPEC_ENEMY       = LAYERSPEC_ALL & ~LAYER_BULLET
LAYERSPEC_COLLECTABLE = LAYERSPEC_ALL & ~LAYER_ENEMY

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
        # XXX Circular reference, might be better as a weak reference
        s.world = None

    def setCollisionProperties(s, group, layerspec):
        "Set the actor's collision properties."
        for shape in s.shapes:
            shape.collision_type = group
            shape.layers = layerspec
        # XXX Circular reference, might be better as a weak reference
        s.body.actor = s

    def setCollisionPlayer(s):
        "Sets the actor's collision properties to that suitable for a player."
        s.setCollisionProperties(CGROUP_PLAYER, LAYERSPEC_PLAYER)

    def setCollisionEnemy(s):
        "Sets the actor's collision properties to that suitable for an enemy."
        s.setCollisionProperties(CGROUP_ENEMY, LAYERSPEC_ENEMY)

    def setCollisionCollectable(s):
        "Sets the actor's collision properties to that suitable for a collectable."
        s.setCollisionProperties(CGROUP_COLLECTABLE, LAYERSPEC_ENEMY)

    def setCollisionPlayerBullet(s):
        "Sets the actor's collision properties to that suitable for a player bullet."
        s.setCollisionProperties(CGROUP_PLAYERBULLET, LAYERSPEC_PLAYERBULLET)

    def setCollisionEnemyBullet(s):
        "Sets the actor's collision properties to that suitable for an enemy bullet."
        s.setCollisionProperties(CGROUP_ENEMYBULLET, LAYERSPEC_ENEMYBULLET)

    def setCollisionTerrain(s):
        s.setCollisionProperties(CGROUP_TERRAIN, LAYERSPEC_ALL)


    def setupPhysics(s):
        """Sets up the actor-specific shape and physics parameters.
Override in children and it will be called in `__init__`."""
        s.corners = rectCornersCenter(0, 0, 10, 10)
        s.body = pymunk.Body(1, 200)
        s.shapes = [pymunk.Poly(s.body, s.corners, radius=1)]
        for shape in s.shapes:
            shape.friction = 5.8
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

    def onDeath(s):
        pass

    def update(s, dt):
        pass

class Player(Actor):
    """The player object."""
    def __init__(s, keyboard, batch=None):
        super(s.__class__, s).__init__(batch)
        s.keyboard = keyboard
        s.setCollisionPlayer()

        s.currentPower = Power(s)

    def setupPhysics(s):
        s.radius = 20
        s.body = pymunk.Body(1, 200)
        s.shapes = [pymunk.Circle(s.body, radius=s.radius)]
        for shape in s.shapes:
            shape.friction = 5.8
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
        s.handleInputState()
        s.currentPower.update(dt)
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

        if s.keyboard[key.SPACE]:
            print "Enter room"

        # Powers
        if s.keyboard[key.Z]:
            s.currentPower.defend()
        if s.keyboard[key.X]:
            s.currentPower.attack2()
        if s.keyboard[key.C]:
            s.currentPower.attack1()
        if s.keyboard[key.UP]:
            s.currentPower.jump()

        #if s.keyboard[key.W]:
        #    pass


    def handleInputEvent(s, k, mod):
        """Handles edge-triggered keyboard actions (key presses, not holds)"""
        # Switch powers
        if k == key.Q:
            print 'foo'
        elif k == key.E:
            print 'bar'

    def switchPowers(s, power):
        "Switches to the given power.  Should eventually do shiny things and such."
        s.currentPower = power


class Collectable(Actor):
    """Something you can collect which does things to you,
whether restoring your health or unlocking a new Power or whatever."""

    def __init__(s, batch=None):
        super(s.__class__, s).__init__(batch)
        s.life = 15.0
        s.setCollisionCollectable()

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

    def setupSprite(s):
        lineList = [cornersToLines(cs) for cs in s.corners]

        allLines = list(itertools.chain.from_iterable(lineList))
        colors = [(192, 0, 0, 255) for _ in allLines]
        image = LineImage(allLines, colors)
        s.sprite = LineSprite(image)

    def collect(s, player):
        print "Collected!"

    def update(s, dt):
        s.life -= dt
        if s.life < 0:
            s.alive = False

class Powerup(Actor):
    """A Collectable that doesn't time out and doesn't move."""
    def __init__(s):
        super(s.__class__, s).__init__()
        s.setCollisionCollectable()

    def setupPhysics(s):
        s.corners = []
        s.corners.append(rectCorners(0, 0, 20, 20))
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
    """A class representing a set of powers for the player."""
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
        super(s.__class__, s).__init__(player)

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
