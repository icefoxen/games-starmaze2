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

class Component(object):
    """Gameobjects are made out of components.
We don't strictly need this class (yet), but having
all components inherit from it is good for clarity
and might be useful someday if we want a more generalized
component system."""
    def __init__(s, owner):
        s.owner = owner

class KeyboardController(Component):
    def __init__(s, owner, keyboard):
        Component.__init__(s, owner)
        s.keyboard = keyboard
        s.moveForce = 400
        s.brakeForce = 400
        s.motionX = 0
        s.braking = False
        s.owner = owner
        # XXX: Circ. reference here I guess...

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
        if s.braking:
            (vx, vy) = s.body.velocity
            if vx > 0:
                s.body.apply_impulse((-s.brakeForce * dt, 0))
            else:
                s.body.apply_impulse((s.brakeForce * dt, 0))
        else:
            xImpulse = s.moveForce * s.motionX * dt
            s.body.apply_impulse((xImpulse, 0))
    
    def handleInputState(s):
        """Handles level-triggered keyboard actions; ie
things that keep happening as long as you hold the button
down."""
        #print 'bop'
        s.body = s.owner.physicsObj
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
            s.owner.currentPower.defend()
        if s.keyboard[key.X]:
            s.owner.currentPower.attack2()
        if s.keyboard[key.C]:
            s.owner.currentPower.attack1()
        if s.keyboard[key.UP]:
            s.owner.currentPower.jump()

        #if s.keyboard[key.W]:
        #    pass


    def handleInputEvent(s, k, mod):
        """Handles edge-triggered keyboard actions (key presses, not holds)"""
        # Switch powers
        if k == key.Q:
            print 'switch previous power'
        elif k == key.E:
            print 'switch next power'


# XXX You know you're doing it right when the best way to handle a
# problem is multiple inheritance.
# We could make this a wrapper instead and expose various methods
# of the body via properties, instead?
class PhysicsObj(Component, pymunk.Body):
    def __init__(s, owner, mass=None, moment=None):
        Component.__init__(s, owner)
        pymunk.Body.__init__(s, mass, moment)
        s.owner = owner
        #s.body = None # pymunk.Body(1, 200)
        #s.shapes = []


    # def setupPhysics(s):
    #     """Sets up the actor-specific shape and physics parameters."""
    #     s.corners = rectCornersCenter(0, 0, 10, 10)
    #     s.shapes = [pymunk.Poly(s.body, s.corners, radius=1)]
    #     for shape in s.shapes:
    #         shape.friction = 5.8
    #     s.body.position = (0,0)

    def setCollisionProperties(s, group, layerspec):
        "Set the actor's collision properties."
        for shape in s.shapes:
            shape.collision_type = group
            shape.layers = layerspec

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

class PlayerPhysicsObj(PhysicsObj):
    def __init__(s, owner):
        PhysicsObj.__init__(s, owner, 1, 200)
        #s.body = pymunk.Body(1, 200)
        pymunk.Circle(s, radius=s.owner.radius)
        # BUGGO: We have to hold on to a reference to the shapes
        # because it appears that the pymunk.Body doesn't do it
        # for us!
        s._shapes = [pymunk.Circle(s, radius=s.owner.radius)]
        for shape in s._shapes:
            shape.friction = 5.8
        s.position = (0,0)
        s.setCollisionPlayer()

class BlockPhysicsObj(PhysicsObj):
    def __init__(s, owner):
        # Static body init here
        PhysicsObj.__init__(s, owner)
        s._shapes = [pymunk.Poly(s, owner.corners)]
        
        for shape in s._shapes:
            shape.friction = 0.8
            shape.elasticity = 0.8

        s.setCollisionTerrain()

class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s, batch=None):
        s.batch = batch or pyglet.graphics.Batch()
        s.physicsObj = None #PhysicsObj(s)
        s.setupSprite()

        s.alive = True

        s.moveForce = 400
        s.brakeForce = 400
        s.motionX = 0
        s.braking = False
        # XXX Circular reference, might be better as a weak reference
        s.world = None
    
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

        s.currentPower = Power(s)

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
