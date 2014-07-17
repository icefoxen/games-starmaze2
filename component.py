import pyglet
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util

from graphics import *
import resource
#import graphics

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
LAYER_PLAYER       = 1 << 0
LAYER_COLLECTABLE  = 1 << 1
LAYER_ENEMY        = 1 << 2
LAYER_PLAYERBULLET = 1 << 3
LAYER_ENEMYBULLET  = 1 << 4
LAYER_BULLET       = LAYER_PLAYERBULLET & LAYER_ENEMYBULLET
#LAYER_TERRAIN      = 1 << 5

# Terrain occupies all layers, it touches everything.
LAYERSPEC_ALL         = 0xFFFFFFFF
# Players only touch
LAYERSPEC_PLAYER      = LAYER_ENEMYBULLET
# Enemies touch everything except enemy bullets
LAYERSPEC_ENEMY       = LAYERSPEC_ALL & ~LAYER_ENEMYBULLET
# Collectables touch everything except enemies and bullets
LAYERSPEC_COLLECTABLE = LAYERSPEC_ALL & ~LAYER_ENEMY & ~LAYER_BULLET
# Player bullets only touch enemies and terrain
LAYERSPEC_PLAYERBULLET = LAYER_ENEMY

class Component(object):
    """Gameobjects are made out of components.
We don't strictly need this class (yet), but having
all components inherit from it is good for clarity
and might be useful someday if we want a more generalized
component system."""
    def __init__(s, owner):
        s.owner = owner

FACING_LEFT = -1
FACING_RIGHT = 1
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
        s.owner.facing = FACING_LEFT

    def moveRight(s):
        s.motionX = 1
        s.owner.facing = FACING_RIGHT

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
            s.owner.powers.defend()
        if s.keyboard[key.X]:
            s.owner.powers.attack2()
        if s.keyboard[key.C]:
            s.owner.powers.attack1()
        if s.keyboard[key.UP]:
            s.owner.powers.jump()

        #if s.keyboard[key.W]:
        #    pass


    def handleInputEvent(s, k, mod):
        """Handles edge-triggered keyboard actions (key presses, not holds)"""
        # Switch powers
        if k == key.Q:
            s.owner.powers.prevPower()
            print "Current power: ", s.owner.powers.currentPower
        elif k == key.E:
            s.owner.powers.nextPower()
            print "Current power: ", s.owner.powers.currentPower


# XXX You know you're doing it right when the best way to handle a
# problem is multiple inheritance.
# We could make this a wrapper instead and expose various methods
# of the body via properties, instead?
class PhysicsObj(Component):
    def __init__(s, owner, mass=None, moment=None):
        Component.__init__(s, owner)
        s.body = pymunk.Body(mass, moment)
        s.owner = owner
        #s.body = None # pymunk.Body(1, 200)
        s.shapes = []
        s.facing = 0

    def _set_position(s, pos):
        s.body.position = pos
        

    def _set_angle(s, ang):
        s.body.angle = ang

    # It seems easiest to wrap these properties to expose them
    # rather than inheriting from Body or anything silly like that.
    angle = property(lambda s: s.body.angle, _set_angle)
    position = property(lambda s: s.body.position, _set_position)
    is_static = property(lambda s: s.body.is_static)
    velocity = property(lambda s: s.body.velocity)
    def apply_impulse(s, impulse):
        s.body.apply_impulse(impulse)
    def apply_force(s, force):
        s.body.apply_force(force)

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

    def setFriction(s, f):
        for shape in s.shapes:
            shape.friction = f

    def setElasticity(s, e):
        for shape in s.shapes:
            shape.elasticity = e

class PlayerPhysicsObj(PhysicsObj):
    def __init__(s, owner):
        PhysicsObj.__init__(s, owner, 1, 200)
        # BUGGO: We have to hold on to a reference to the shapes
        # because it appears that the pymunk.Body doesn't do it
        # for us!
        s.shapes = [pymunk.Circle(s.body, radius=s.owner.radius)]
        s.setFriction(6.0)
        s.position = (0,0)
        s.setCollisionPlayer()

class PlayerBulletPhysicsObj(PhysicsObj):
    """A generic physics object for small round things that hit stuff."""
    def __init__(s, owner, position=(0, 0)):
        PhysicsObj.__init__(s, owner, 1, 10)
        s.body.position = position
        s.shapes = [pymunk.Circle(s.body, radius=2)]
        s.setElasticity(0.8)
        s.setCollisionPlayerBullet()

class BlockPhysicsObj(PhysicsObj):
    def __init__(s, owner):
        # Static body init here
        PhysicsObj.__init__(s, owner)
        s.shapes = [pymunk.Poly(s.body, owner.corners)]
        s.setFriction(0.8)
        s.setElasticity(0.8)

        s.setCollisionTerrain()

class CollectablePhysicsObj(PhysicsObj):
    def __init__(s, owner):
        PhysicsObj.__init__(s, owner, 1, 200)
        corners = []
        corners.append(rectCornersCenter(0, 0, 20, 10))
        corners.append(rectCornersCenter(0, 0, 10, 20))

        s.shapes = [
            pymunk.Poly(s.body, c)
            for c in s.corners
            ]
        s.setElasticity(0.8)
        s.setFriction(2.0)



class LineSprite(Component):
    """A class that draws a positioned, scaled, rotated
and maybe someday animated `LineImage`s.

The question is, how do we animate these things...

Apart from animations, this is mostly API-compatible with
`pyglet.sprite.Sprite`.  Huzzah~

Except I took out all the group stuff so I can not worry
about it until I need to.  Though it looks like it might
be ideal for shaders and ordering maybe...
"""

    def __init__(s, owner, lineimage, x=0, y=0, batch=None, group=None):
        Component.__init__(s, owner)
        s._image = lineimage
        s._x = x
        s._y = y
        s._batch = batch or lineimage.batch
        s._rotation = 0.0
        s._scale = 1.0

    def delete(s):
        pass

    def draw(s):
        glPushAttrib(GL_COLOR_BUFFER_BIT)
        glEnable(GL_BLEND)
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
        with Affine((s._x, s._y), s.rotation, (s._scale, s._scale)):
            s._batch.draw()
        glPopAttrib()

    # def _get_group(s):
    #     return s._group
    # def _set_group(s, group):
    #     s._group = group
    # group = property(_get_group, _set_group)

    def _set_image(s, image):
        s._image = image
    image = property(lambda s: s._image, _set_image)
    
    def _set_x(s, x):
        s._x = x
    x = property(lambda s: s._x, _set_x)

    def _set_y(s, y):
        s._y = y
    y = property(lambda s: s._y, _set_y)

    def _set_position(s, pos):
        (x, y) = pos
        s._x = x
        s._y = y
    position = property(lambda s: (s._x, s._y), _set_position)

    def _set_rotation(s, rotation):
        s._rotation = rotation
    rotation = property(lambda s: s._rotation, _set_rotation)

    def _set_scale(s, scale):
        s._scale = scale
    scale = property(lambda s: s._scale, _set_scale)

    #width = property(lambda s: s._y, _set_y)

    #height = property(lambda s: s._y, _set_y)
    

class CollectableSprite(LineSprite):
    def __init__(s, owner):
        lineList = [cornersToLines(cs) for cs in s.corners]

        allLines = list(itertools.chain.from_iterable(lineList))
        colors = [(192, 0, 0, 255) for _ in allLines]
        image = LineImage(allLines, colors)
        LineSprite.__init__(s, owner, image)

class BlockSprite(LineSprite):
    def __init__(s, owner, corners, color):
        lines = cornersToLines(corners)
        colors = colorLines(lines, color)

        image = LineImage(lines, colors)
        LineSprite.__init__(s, owner, image)

