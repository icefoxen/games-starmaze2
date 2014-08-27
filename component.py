#from lepton import *
#from lepton.controller import Lifetime, Movement, Fader
#from lepton.emitter import StaticEmitter
#from lepton.renderer import BillboardRenderer
#from lepton.texturizer import SpriteTexturizer
import pyglet
import pyglet.graphics
import pyglet.sprite
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util

from graphics import *
import rcache
#import physics

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
CGROUP_DOOR = 7

# Collision layers
# Determine what collides with what at all; probably faster
# and certainly simpler than making a bunch of callbacks between
# collision groups that do nothing.
# These layers aren't quite 100% right but are close enough for now.
# Collectables do not collide with enemies or player/enemy bullets
# Enemies do not collide with their own bullets,
# players do not collide with their own bullets.
LAYER_PLAYER       = 1 << 0
LAYER_COLLECTABLE  = 1 << 1
LAYER_ENEMY        = 1 << 2
LAYER_PLAYERBULLET = 1 << 3
LAYER_ENEMYBULLET  = 1 << 4
LAYER_BULLET       = LAYER_PLAYERBULLET & LAYER_ENEMYBULLET
LAYER_DOOR         = 1 << 5

# These get a little weird because we're specifying what layers
# things occupy...
# Terrain occupies all layers, it touches everything.
LAYERSPEC_ALL          = 0xFFFFFFFF
# Players have their own layer
LAYERSPEC_PLAYER       = LAYER_PLAYER
# Enemies touch everything except enemy bullets
LAYERSPEC_ENEMY        = LAYERSPEC_ALL & ~LAYER_ENEMYBULLET
# Collectables touch everything except enemies and bullets
LAYERSPEC_COLLECTABLE  = LAYERSPEC_ALL & ~LAYER_ENEMY & ~LAYER_BULLET
# Player bullets only touch enemies and terrain
LAYERSPEC_PLAYERBULLET = LAYER_ENEMY
# Enemy bullets only touch players and terrain...
LAYERSPEC_ENEMYBULLET  = LAYER_ENEMYBULLET
# Doors only touch the player
LAYERSPEC_DOOR         = LAYERSPEC_ALL

class Component(object):
    """Actors are made out of components.
We don't strictly need this class (yet), but having
all components inherit from it is good for clarity
and might be useful someday if we want a more generalized
component system."""
    def __init__(s, owner):
        s.owner = owner

# TODO:
# Be able to gauge jumping by how long you hold the button down
# Have a maximum speed (should that be on the PhysicsObj instead?)
FACING_LEFT = -1
FACING_RIGHT = 1
class KeyboardController(Component):
    """Lets a keyboard move an `Actor` around."""
    def __init__(s, owner, keyboard):
        Component.__init__(s, owner)
        s.keyboard = keyboard
        s.moveForce = 400
        s.brakeForce = 400
        s.motionX = 0
        s.braking = False
        # XXX: Circ. reference here I guess...
        s.owner = owner
        s.body = None  # BUGGO: Something weird with initialization order here, see below

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
        # BUGGO: It works if we initialize s.body here but not
        # in the actual initializer.
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
            if s.owner.door is not None:
                s.owner.world.enterDoor(s.owner.door)

        # Powers
        if s.keyboard[key.Z]:
            s.owner.powers.defend()
        if s.keyboard[key.X]:
            s.owner.powers.attack2()
        if s.keyboard[key.C]:
            s.owner.powers.attack1()
        if s.keyboard[key.UP]:
            s.owner.powers.jump()

    def handleKeyPress(s, k, mod):
        """Handles edge-triggered keyboard actions (key presses, not holds)"""
        # Switch powers
        if k == key.Q:
            s.owner.powers.prevPower()
            print "Current power: ", s.owner.powers.currentPower
        elif k == key.E:
            s.owner.powers.nextPower()
            print "Current power: ", s.owner.powers.currentPower

        # Powers
        elif k == key.Z:
            s.owner.powers.startDefend()
        elif k == key.X:
            s.owner.powers.startAttack2()
        elif k == key.C:
            s.owner.powers.startAttack1()
        elif k == key.UP:
            s.owner.powers.startJump()


    def handleKeyRelease(s, k, mod):
        # Powers
        if k == key.Z:
            s.owner.powers.stopDefend()
        elif k == key.X:
            s.owner.powers.stopAttack2()
        elif k == key.C:
            s.owner.powers.stopAttack1()
        elif k == key.UP:
            s.owner.powers.stopJump()


class RoamAIController(Component):
    """A controller that sorta wanders at random..."""
    def __init__(s, owner):
        Component.__init__(s, owner)

    def update(s, dt):
        moveForce = 400
        s.owner.physicsObj.apply_impulse((moveForce * dt * s.owner.facing, 0))


class PhysicsObj(Component):
    """A component that handles an `Actor`'s position and movement
and physics interactions, all through pymunk.

Pretty much all `Actor`s will have one of these.  Inherit from it
and set the shapes and stuff in the initializer.
Call one of the setCollision*() methods too."""
    def __init__(s, owner, mass=None, moment=None):
        Component.__init__(s, owner)
        s.owner = owner
        
        s.body = pymunk.Body(mass, moment)
        # Add a backlink to the Body so we can get this object
        # from collision handler callbacks
        s.body.component = s
        s.auxBodys = []
        # We have to hold on to a reference to the shapes
        # because it appears that the pymunk.Body doesn't do it
        # for us!  Per the pymunk dev, the body only holds weak
        # references to the shapes, the shapes hold strong refs
        # to the body.
        s.shapes = []
        s.constraints = []

        s.facing = 0

        # Set a reasonable default max velocity so things don't
        # get too crazy
        s.body.velocity_limit = 800

    def addAuxBodys(s, *bodys):
        s.auxBodys.extend(bodys)

    def addShapes(s, *shapes):
        s.shapes.extend(shapes)

    def addConstraints(s, *constraints):
        s.constraints.extend(constraints)
        
    def _set_position(s, pos):
        s.body.position = pos

    def _set_angle(s, ang):
        s.body.angle = ang

    def _set_velocity(s, vel):
        s.body.velocity = vel

    def _set_angular_velocity(s, vel):
        s.body.angular_velocity = vel

    def _set_velocity_limit(s, vel):
        s.body.velocity_limit = vel



    # It seems easiest to wrap these properties to expose them
    # rather than inheriting from Body or anything silly like that.
    angle = property(lambda s: s.body.angle, _set_angle)
    position = property(lambda s: s.body.position, _set_position)
    is_static = property(lambda s: s.body.is_static)
    velocity = property(lambda s: s.body.velocity, _set_velocity)
    angular_velocity = property(lambda s: s.body.angular_velocity, _set_angular_velocity)
    velocity_limit = property(lambda s: s.body.velocity_limit, _set_velocity_limit)
    torque = property(lambda s: s.body.torque)
    def apply_impulse(s, impulse, r=(0,0)):
        s.body.apply_impulse(impulse, r=r)
        
    def apply_force(s, force, r=(0,0)):
        s.body.apply_force(force, r=r)

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
        s.setCollisionProperties(CGROUP_COLLECTABLE, LAYERSPEC_COLLECTABLE)

    def setCollisionPlayerBullet(s):
        "Sets the actor's collision properties to that suitable for a player bullet."
        s.setCollisionProperties(CGROUP_PLAYERBULLET, LAYERSPEC_PLAYERBULLET)

    def setCollisionEnemyBullet(s):
        "Sets the actor's collision properties to that suitable for an enemy bullet."
        s.setCollisionProperties(CGROUP_ENEMYBULLET, LAYERSPEC_ENEMYBULLET)

    def setCollisionTerrain(s):
        s.setCollisionProperties(CGROUP_TERRAIN, LAYERSPEC_ALL)

    def setCollisionDoor(s):
        s.setCollisionProperties(CGROUP_DOOR, LAYERSPEC_DOOR)

    def setFriction(s, f):
        for shape in s.shapes:
            shape.friction = f

    def setElasticity(s, e):
        for shape in s.shapes:
            shape.elasticity = e

#from physics import *
class NativePhysicsObj(Component):
    def __init__(s, owner, mass=None, moment=None):
        Component.__init__(s, owner)
        s._position = Vec(0,0)
        s._velocity = Vec(0,0)
        s.mass = mass
        s.facing = 0
        # XXX: Unused
        #s.moment = moment
        s.velocity_limit = 1000
        s._angle = 0

        s.bbox = physics.BBox(0, 0, 10, 10)

    def addAuxBodys(s, *bodys):
        pass

    def addShapes(s, *shapes):
        pass

    def addConstraints(s, *constraints):
        pass
        
    def update(s, dt):
        before = s._position
        s._position += s.velocity * dt
        s.bbox = BBox(s._position.x, s._position.y, s.bbox.w, s.bbox.h)

        #if before != s._position:
        #    print "DIFFERENCE", before, s._position

    def isColliding(s, other):
        return s.bbox.overlapping(other.bbox)
                
    def _set_position(s, pos):
        s._position = Vec(*pos)

    def _set_angle(s, ang):
        s._angle = ang

    def _set_velocity(s, vel):
        s._velocity = Vec(*vel)

    def _set_angular_velocity(s, vel):
        pass

    def _set_velocity_limit(s, vel):
        pass



    # It seems easiest to wrap these properties to expose them
    # rather than inheriting from Body or anything silly like that.
    angle = property(lambda s: s._angle, _set_angle)
    position = property(lambda s: s._position, _set_position)
    is_static = property(lambda s: False)
    velocity = property(lambda s: s._velocity, _set_velocity)
    angular_velocity = property(lambda s: s._angular_velocity, _set_angular_velocity)
    velocity_limit = property(lambda s: s._velocity_limit, _set_velocity_limit)

    def apply_impulse(s, impulse, r=(0,0)):
        if s.mass is not None:
            #print "Applying impulse:", impulse
            impulse = Vec(*impulse)
            deltav = impulse / s.mass
            s.velocity = s.velocity + deltav

    def apply_force(s, force, r=(0,0)):
        s.apply_impulse(force, r=r)

    def setCollisionProperties(s, group, layerspec):
        "Set the actor's collision properties."
        pass

    def setCollisionPlayer(s):
        "Sets the actor's collision properties to that suitable for a player."
        pass

    def setCollisionEnemy(s):
        "Sets the actor's collision properties to that suitable for an enemy."
        pass

    def setCollisionCollectable(s):
        "Sets the actor's collision properties to that suitable for a collectable."
        pass

    def setCollisionPlayerBullet(s):
        "Sets the actor's collision properties to that suitable for a player bullet."
        pass

    def setCollisionEnemyBullet(s):
        "Sets the actor's collision properties to that suitable for an enemy bullet."
        pass

    def setCollisionTerrain(s):
        pass

    def setCollisionDoor(s):
        pass

    def setFriction(s, f):
        pass

    def setElasticity(s, e):
        pass


            
class PlayerPhysicsObj(PhysicsObj):
    def __init__(s, owner):
        PhysicsObj.__init__(s, owner, 1, 200)
        s.addShapes(pymunk.Circle(s.body, radius=s.owner.radius))
        s.setFriction(6.0)
        s.setCollisionPlayer()
        s.velocity_limit = (400)

class DoorPhysicsObj(PhysicsObj):
    def __init__(s, owner, position=(0, 0)):
        PhysicsObj.__init__(s, owner)
        s.body.position = position
        poly = pymunk.Poly(s.body, rectCornersCenter(0, 0, 40, 40))
        # Sensors call collision callbacks but don't actually do any physics.
        # I think.
        poly.sensor = True
        s.addShapes(poly)
        s.setCollisionDoor()
        
class PlayerBulletPhysicsObj(PhysicsObj):
    """A generic physics object for small round things that hit stuff."""
    def __init__(s, owner, position=(0, 0)):
        PhysicsObj.__init__(s, owner, 1, 10)
        s.body.position = position
        s.addShapes(pymunk.Circle(s.body, radius=2))
        s.setCollisionPlayerBullet()

class AirP1PhysicsObjAir(PhysicsObj):
    def __init__(s, owner, position=(0, 0)):
        PhysicsObj.__init__(s, owner, 1, 10)
        poly = pymunk.Poly(s.body, rectCornersCenter(0, 0, 10, 80))
        poly.sensor = True
        s.addShapes(poly)
        s.position = position
        s.setCollisionPlayerBullet()

class AirP1PhysicsObjGround(PhysicsObj):
    def __init__(s, owner, position=(0, 0)):
        PhysicsObj.__init__(s, owner, 1, 10)
        poly = pymunk.Poly(s.body, rectCornersCenter(0, 0, 10, 30))
        poly.sensor = True
        s.addShapes(poly)
        s.position = position
        s.setCollisionPlayerBullet()

        
class BlockPhysicsObj(PhysicsObj):
    """Generic immobile rectangle physics object"""
    def __init__(s, owner):
        # Static body 
        PhysicsObj.__init__(s, owner)
        s.addShapes(pymunk.Poly(s.body, owner.corners))
        #print owner.corners
        s.setFriction(0.8)
        s.setElasticity(0.8)
        s.setCollisionTerrain()

class FallingBlockPhysicsObj(PhysicsObj):
    """Like a BlockPhysicsObject but...

BUGGO: A BlockPhyicsObj's body-center is actually not
in the center of the shape, and that's terrible.

This is because the shape is specified by the corners,
and life just gets shitty and weird.  idfk"""
    def __init__(s, owner):
        PhysicsObj.__init__(s, owner, mass=20, moment=pymunk.inf)
        constraintBody = pymunk.Body()
        s.addConstraints(pymunk.constraint.GrooveJoint(constraintBody, s.body,
                                                    (0, -50), (0, -150),
                                                     (100, 0)))
        s.constraintBody.position = (-200, 100)
        s.addAuxBodys(constraintBody)
        s.addShapes(pymunk.Poly(s.body, owner.corners))
        s.setFriction(0.1)
        s.setElasticity(0.8)
        s.setCollisionTerrain()
        s.body.apply_force((0, 400 * s.body.mass + 100))
        
    def _set_position(s, pos):
        s.body.position = pos
        for aux in s.auxBodys:
            aux.position = pos

    # Gotta redefine this 'cause constraints...
    position = property(lambda s: s.body.position, _set_position)

class CollectablePhysicsObj(PhysicsObj):
    def __init__(s, owner):
        PhysicsObj.__init__(s, owner, 1, 200)
        corners = []
        corners.append(rectCornersCenter(0, 0, 20, 10))
        corners.append(rectCornersCenter(0, 0, 10, 20))

        s.addShapes(*[
            pymunk.Poly(s.body, c)
            for c in corners
            ])
        s.setElasticity(0.8)
        s.setFriction(2.0)
        s.setCollisionCollectable()

class PowerupPhysicsObj(PhysicsObj):
    def __init__(s, owner):
        PhysicsObj.__init__(s, owner) # Static physics object
        corners = rectCornersCenter(0, 0, 10, 10)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionCollectable()

class CrawlerPhysicsObj(PhysicsObj):
    def __init__(s, owner):
        PhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf)
        corners = rectCornersCenter(0, 0, 25, 20)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()


class LineSprite(Component):
    """A class that draws positioned, scaled, rotated
and maybe someday animated `LineImage`s.

Just inherit from it or make a function to pass it a
LineImage.

TODO: The question is, how do we animate these things...

Apart from animations, this is mostly API-compatible with
`pyglet.sprite.Sprite`.  Huzzah~

Except I took out all the group stuff so I can not worry
about it until I need to.  Though it looks like it might
be ideal for shaders and ordering maybe...  So I might need
to make that work.

TODO: Glow layer???
"""

    def __init__(s, owner, lineimage, x=0, y=0, batch=None, group=None):
        Component.__init__(s, owner)
        s._image = lineimage
        s._x = x
        s._y = y
        # BUGGO: Actually, we have no way of moving a lineimage
        # into a new batch, so if we DO provide a batch for this sprite,
        # it won't actually draw anything...
        s._batch = batch or lineimage.batch
        s._rotation = 0.0
        s._scale = 1.0


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


class ImgSprite(Component):
    """A sprite that displays a bitmap image.
We use Pyglet's Sprite class internally, but have to wrap it
to play nice with how we handle coordinates...

ie, _not_ recreating the whole shape from scratch whenever
it moves.

BUGGO: Doesn't work correctly with shaders.  If we have textured
quads, then we need texture colors, if we have untextured quads,
then we need non-texture colors.  Either use different shaders for
the two, or have all textured images have a non-texture color of 0 and
all non-textured shapes bind to a single-pixel black texture, then the
shader just adds the two together...

XXX: The existence of this feels like a kludge, though I'm not sure why.
Pyglet's whole drawing system makes me vaguely unhappy for some reason,
I guess.
Could trivially replicated, but I'm not sure that'd be less of a kludge.
I dunno man."""
    def __init__(s, owner, image, x=0, y=0, batch=None, group=None):
        Component.__init__(s, owner)
        s._image = image
        s._x = x
        s._y = y
        s._batch = batch or pyglet.graphics.Batch()
        s._sprite = pyglet.sprite.Sprite(image, batch=s._batch)
        print "Batch", s._batch
        s._rotation = 0.0
        s._scale = 1.0


    def draw(s):
        glPushAttrib(GL_COLOR_BUFFER_BIT)
        glEnable(GL_BLEND)
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
        with Affine((s._x, s._y), s.rotation, (s._scale, s._scale)):
            s._batch.draw()
        glPopAttrib()


    #def _set_image(s, image):
    #    s._image = image
    #image = property(lambda s: s._image, _set_image)
    
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


class BlockSprite(LineSprite):
    def __init__(s, owner, corners, color, batch=None):
        verts = [Vertex(x, y, color) for (x,y) in corners]
        poly = Polygon(verts)
        image = LineImage([poly], batch=batch)
        LineSprite.__init__(s, owner, image)


class Life(Component):
    """A component that keeps track of life and kills
its owner when it runs out."""
    def __init__(s, owner, hps, attenuation = 1.0, reduction = 0):
        Component.__init__(s, owner)
        s.life = hps
        
        # A multiplier to the damage taken
        s.damageAttenuation = attenuation

        # A subtractor to the damage taken
        # Happens BEFORE attenuation
        s.damageReduction = reduction

    def takeDamage(s, damager, damage):
        reducedDamage = max(0, damage - s.damageReduction)
        attenuatedDamage = reducedDamage * s.damageAttenuation
        s.life -= attenuatedDamage
        if s.life <= 0:
            s.owner.alive = False
        print "Took {} out of {} damage, life is now {}".format(attenuatedDamage, damage, s.life)

class TimedLife(Component):
    """A component that kills the owner after
a certain timeout."""
    def __init__(s, owner, time):
        Component.__init__(s, owner)
        s.time = float(time)

    def update(s, dt):
        s.time -= dt
        if s.time <= 0:
            s.owner.alive = False

# TODO:
# Hitboxes????


class Timer(object):
    """Not really a component, but handy.  A simple timer.

XXX: 
An intriguing idea is to add them to a global list somewhere
and have one function call that updates all the timers in the
list run by the game loop...
But then we'd have to make sure to remove them when we're done...
That becomes non-thread-safe, but is tempting.

What is also tempting is the idea of doing the same thing to
all Components, so that each Actor doesn't have to update
a particular set of them itself...

Something to think about, maybe.   ORDERING becomes an issue!

It already is an issue, given that each Actor updates its Components
more or less ad-hoc'ly.  On the one hand that's good because an Actor
presumably knows what order its shit should be updated in.  On the
other hand it can make life difficult, and update order of actors can
become significant if they do things that directly affect other Actors."""
    def __init__(s, time=0.0, defaultTime = 0.0):
        s.time = float(time)
        s.defaultTime = defaultTime

    def reset(s):
        """Resets the timer to the default time."""
        s.time = s.defaultTime
        
    def update(s, dt):
        s.time -= dt

    def expired(s):
        return s.time <= 0.0

class ParticleSystem(Component):
    """A test component that emits particles."""
    def __init__(s, owner):
        Component.__init__(s, owner)
        s.tex = rcache.get_image("playertest").get_texture()
        s.sparkGroup = ParticleGroup(
            controllers=[
                Lifetime(3),
                Movement(damping=0.93),
                Fader(fade_out_start=0.75, fade_out_end=3.0)
                ],
            renderer=BillboardRenderer(SpriteTexturizer(s.tex.id))
            )
        s.sparkEmitter = StaticEmitter(
            template=Particle(
                position = (100, 0, -100),
                color = (1,1,1),
                size=(20,20,0)),
            deviation=Particle(
                position=(2,2,0),
                velocity=(75,75,0),
                size=(0.2,0.2,0),
                age=15))

    def update(s,dt):
        x,y = s.owner.physicsObj.position
        s.sparkEmitter.template.position.x = x
        s.sparkEmitter.template.position.y = y
        #s.sparkEmitter.position = 
        s.sparkEmitter.emit(int(100*dt), s.sparkGroup)
        
