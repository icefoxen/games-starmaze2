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
import actor
#import physics




CGROUP_NONE = 0

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
LAYER_GATE         = 1 << 5

# These get a little weird because we're specifying what layers
# things occupy...
# Terrain occupies all layers, it touches everything.
LAYERSPEC_ALL          = 0xFFFFFFFF
# Players have their own layer
LAYERSPEC_PLAYER       = LAYER_PLAYER
# Enemies touch everything except enemy bullets
LAYERSPEC_ENEMY        = LAYERSPEC_ALL & ~LAYER_ENEMYBULLET & ~LAYER_ENEMY
# Collectables touch everything except enemies and bullets
LAYERSPEC_COLLECTABLE  = LAYERSPEC_ALL # & ~LAYER_ENEMY & ~LAYER_BULLET
# Player bullets only touch enemies and terrain
LAYERSPEC_PLAYERBULLET = LAYER_PLAYERBULLET
# Enemy bullets only touch players and terrain...
LAYERSPEC_ENEMYBULLET  = LAYER_PLAYER
# Gates only touch the player
LAYERSPEC_GATE         = LAYERSPEC_ALL

class Component(object):
    """Actors are made out of components.
We don't strictly need this class (yet), but having
all components inherit from it is good for clarity
and might be useful someday if we want a more generalized
component system."""
    def __init__(self, owner):
        self.owner = owner

######################################################################
## Controllers
######################################################################

# TODO:
# Be able to gauge jumping by how long you hold the button down
# Have a maximum speed (should that be on the PhysicsObj instead?)
FACING_LEFT = -1
FACING_RIGHT = 1
class KeyboardController(Component):
    """Lets a keyboard move an `Actor` around."""
    def __init__(self, owner, keyboard):
        Component.__init__(self, owner)
        self.keyboard = keyboard
        self.moveForce = 400
        self.brakeForce = 400
        self.motionX = 0
        self.braking = False
        # XXX: Circ. reference here I guess...
        self.owner = owner
        self.body = None  # BUGGO: Something weird with initialization order here, see below

    def stopMoving(self):
        self.motionX = 0
    
    def moveLeft(self):
        self.motionX = -1
        self.owner.facing = FACING_LEFT

    def moveRight(self):
        self.motionX = 1
        self.owner.facing = FACING_RIGHT

    def brake(self):
        self.braking = True
        
    def stopBrake(self):
        self.braking = False

    def update(self, dt):
        self.handleInputState()
        if self.braking:
            (vx, vy) = self.body.velocity
            if vx > 0:
                self.body.apply_impulse((-self.brakeForce * dt, 0))
            else:
                self.body.apply_impulse((self.brakeForce * dt, 0))
        else:
            xImpulse = self.moveForce * self.motionX * dt
            self.body.apply_impulse((xImpulse, 0))
    
    def handleInputState(self):
        """Handles level-triggered keyboard actions; ie
things that keep happening as long as you hold the button
down."""
        #print 'bop'
        # BUGGO: It works if we initialize s.body here but not
        # in the actual initializer.
        # "self" might be a little odd in initializers.
        self.body = self.owner.physicsObj
        self.stopBrake()
        self.stopMoving()
        if self.keyboard[key.DOWN]:
            self.brake()
        elif self.keyboard[key.LEFT]:
            self.moveLeft()
        elif self.keyboard[key.RIGHT]:
            self.moveRight()

        if self.keyboard[key.SPACE]:
            print "Enter room"
            if self.owner.gate is not None:
                self.owner.world.enterGate(self.owner.gate)

        # Powers
        if self.keyboard[key.Z]:
            self.owner.powers.defend()
        if self.keyboard[key.X]:
            self.owner.powers.attack2()
        if self.keyboard[key.C]:
            self.owner.powers.attack1()
        if self.keyboard[key.UP]:
            self.owner.powers.jump()

    def handleKeyPress(self, k, mod):
        """Handles edge-triggered keyboard actions (key presses, not holds)"""
        # Switch powers
        if k == key.Q:
            self.owner.powers.prevPower()
            print "Current power: ", self.owner.powers.currentPower
        elif k == key.E:
            self.owner.powers.nextPower()
            print "Current power: ", self.owner.powers.currentPower

        # Powers
        elif k == key.Z:
            self.owner.powers.startDefend()
        elif k == key.X:
            self.owner.powers.startAttack2()
        elif k == key.C:
            self.owner.powers.startAttack1()
        elif k == key.UP:
            self.owner.powers.startJump()


    def handleKeyRelease(self, k, mod):
        # Powers
        if k == key.Z:
            self.owner.powers.stopDefend()
        elif k == key.X:
            self.owner.powers.stopAttack2()
        elif k == key.C:
            self.owner.powers.stopAttack1()
        elif k == key.UP:
            self.owner.powers.stopJump()


class RoamAIController(Component):
    """A controller that sorta wanders at random..."""
    def __init__(self, owner):
        Component.__init__(self, owner)
        self.moveForce = 400

    def update(self, dt):
        self.owner.physicsObj.apply_impulse((self.moveForce * dt * self.owner.facing, 0))


STATE_WANDERING = 0
STATE_ATTACKING = 1
class TrooperAIController(Component):
    """Occasionally tests to see if the player is in front of it.
If it is, DESTROY"""
    def __init__(self, owner):
        Component.__init__(self, owner)
        self.moveForce = 400
        self.sightRange = 500
        self.sightHeight = 100
        self.sightCheck = Timer(defaultTime=1.0)
        self.lastSawPlayer = Timer(defaultTime=10.0)
        self.state = STATE_WANDERING

    def checkSight(self):
        """Check if we can see the player.
First we do a BB query in front of us.  If that
succeeds, we do a segment query that intersects
terrain and players, and if that succeeds, we can
see the player.

XXX: This method might best be refactored out into a superclass."""
        print "Checking sight..."
        space = self.owner.world.space
        selfPosition = self.owner.physicsObj.position
        selfFacing = self.owner.facing
        bottom = selfPosition.y - self.sightHeight/2
        top = selfPosition.y + self.sightHeight/2
        left = selfPosition.x
        right = selfPosition.x + self.sightRange
        if selfFacing == FACING_LEFT:
            right = left
            left = selfPosition.x - self.sightRange

        bb = pymunk.BB(left, bottom, right, top)
        shapes = space.bb_query(bb, LAYERSPEC_PLAYER)
        for shape in shapes:
            act = shape.body.component.owner
            if isinstance(act, actor.Player):
                # We've seen the player in the bounding box, now we do a segment
                # query to make sure there's nothing between us and it.
                # BUGGO: The layer collision stuff doesn't work right,
                # it just collides with itself
                # This is sticky because we have the 'enemy' layer collide with the
                # 'player' layer, so it's in the player layer...
                # start = selfPosition
                # end = act.physicsObj.position
                # segmentQueryInfo = space.segment_query_first(start, end, LAYERSPEC_PLAYER)
                # print "Segment test hit object:", segmentQueryInfo.shape.body.component.owner
                # if segmentQueryInfo.shape.body.component.owner == act:
                #     return True
                #     print 'trooper can REALLY see player'
                # print 'trooper can't actually see player'
                # return False

                return True
            
        return False
            
    def update(self, dt):
        self.owner.physicsObj.apply_impulse((self.moveForce * dt * self.owner.facing, 0))
        self.sightCheck.update(dt)
        if self.sightCheck.expired():
            self.sightCheck.reset()
            if self.checkSight():
                self.owner.fireBullet(actor.TrooperBullet)
                rcache.get_sound("EnemyShot1").play()
            
class ArcherAIController(Component):
    """Archers are immobile, they sit in one place and lob projectiles."""
    def __init__(self, owner):
        Component.__init__(self, owner)
        self.fireTimer = Timer(defaultTime=2.5)

    def update(self, dt):
        self.fireTimer.update(dt)
        if self.fireTimer.expired():
            self.fireTimer.reset()
            self.owner.fireBullet(actor.TrooperBullet, facing=FACING_LEFT)
            self.owner.fireBullet(actor.TrooperBullet, facing=FACING_RIGHT)
            rcache.get_sound("EnemyShot1").play()

class FloaterAIController(Component):
    """Floaters wander back and forth and fire at the player if they are close.
Their vision, and firing, is omnidirectional, and they can see through walls."""
    def __init__(self, owner, moveDirection=(1,0)):
        Component.__init__(self, owner)
        self.moveForce = 400
        self.moveDirection = moveDirection
        self.fireTimer = Timer(defaultTime=1.0)
        self.sightRadius = 100

    def update(self, dt):
        dirX, dirY = self.moveDirection
        xForce = self.moveForce * dirX * self.owner.facing
        yForce = self.moveForce * dirY * self.owner.facing
        self.owner.physicsObj.apply_impulse((xForce * dt, yForce * dt))
        self.fireTimer.update(dt)
        
        if self.fireTimer.expired() and self.checkVision():
            self.fireTimer.reset()
            self.fire()

    def checkVision(self):
        space = self.owner.world.space
        selfPosition = self.owner.physicsObj.position
        selfFacing = self.owner.facing
        bottom = selfPosition.y - self.sightRadius
        top = selfPosition.y + self.sightRadius
        left = selfPosition.x - self.sightRadius
        right = selfPosition.x + self.sightRadius

        bb = pymunk.BB(left, bottom, right, top)
        shapes = space.bb_query(bb, LAYERSPEC_PLAYER)
        for shape in shapes:
            act = shape.body.component.owner
            if isinstance(act, actor.Player):
                return True
            
        return False
        return True

    def fire(self):
        numBullets = 8
        angleIncrement = (math.pi * 2) / numBullets
        r = 20
        bulletForce = 300
        for i in xrange(numBullets):
            theta = angleIncrement * i
            xs = math.cos(theta)
            ys = math.sin(theta)
            xForce = xs * bulletForce
            yForce = ys * bulletForce
            xPos = xs * r
            yPos = ys * r
            self.owner.fireBulletAt(actor.FloaterBullet, (xPos, yPos), (xForce, yForce), facing=FACING_RIGHT)
        rcache.get_sound("EnemyShot2").play()

        
######################################################################
## Physics objects
######################################################################

class PhysicsObj(Component):
    """A component that handles an `Actor`'s position and movement
and physics interactions, all through pymunk.

Pretty much all `Actor`s will have one of these.  Inherit from it
and set the shapes and stuff in the initializer.
Call one of the setCollision*() methods too."""
    def __init__(self, owner, mass=None, moment=None, position=(0,0)):
        Component.__init__(self, owner)
        self.owner = owner
        
        self.body = pymunk.Body(mass=mass, moment=moment)
        self.body.position = position
        # Add a backlink to the Body so we can get this object
        # from collision handler callbacks
        self.body.component = self
        self.auxBodys = []
        # We have to hold on to a reference to the shapes
        # because it appears that the pymunk.Body doesn't do it
        # for us!  Per the pymunk dev, the body only holds weak
        # references to the shapes, the shapes hold strong refs
        # to the body.
        self.shapes = []
        self.constraints = []

        self.facing = 0

        # Set a reasonable default max velocity so things don't
        # get too crazy
        self.body.velocity_limit = 800

    def addAuxBodys(self, *bodys):
        self.auxBodys.extend(bodys)

    def addShapes(self, *shapes):
        self.shapes.extend(shapes)

    def addConstraints(self, *constraints):
        self.constraints.extend(constraints)
        
    def _set_position(self, pos):
        self.body.position = pos

    def _set_angle(self, ang):
        self.body.angle = ang

    def _set_velocity(self, vel):
        self.body.velocity = vel

    def _set_angular_velocity(self, vel):
        self.body.angular_velocity = vel

    def _set_velocity_limit(self, vel):
        self.body.velocity_limit = vel

    # XXX: This function is what can only be described
    # as a bad approximation... but it's also only used
    # in the level editor, which doesn't have an actual
    # running physics space in it.
    def getBB(self):
        for shape in self.shapes:
            shape.cache_bb()
            return shape.bb

    # It seems easiest to wrap these properties to expose them
    # rather than inheriting from Body or anything silly like that.
    angle = property(lambda self: self.body.angle, _set_angle)
    position = property(lambda self: self.body.position, _set_position)
    is_static = property(lambda self: self.body.is_static)
    velocity = property(lambda self: self.body.velocity, _set_velocity)
    angular_velocity = property(lambda self: self.body.angular_velocity, _set_angular_velocity)
    velocity_limit = property(lambda self: self.body.velocity_limit, _set_velocity_limit)
    torque = property(lambda self: self.body.torque)
    def apply_impulse(self, impulse, r=(0,0)):
        self.body.apply_impulse(impulse, r=r)
        
    def apply_force(self, force, r=(0,0)):
        self.body.apply_force(force, r=r)

    def negateGravity(self):
        force = 400 * self.body.mass
        self.apply_force((0, force))

    def setCollisionProperties(self, group, layerspec):
        "Set the actor's collision properties."
        for shape in self.shapes:
            shape.collision_type = group
            shape.layers = layerspec

    def setCollisionPlayer(self):
        "Sets the actor's collision properties to that suitable for a player."
        self.setCollisionProperties(CGROUP_NONE, LAYERSPEC_PLAYER)

    def setCollisionEnemy(self):
        "Sets the actor's collision properties to that suitable for an enemy."
        self.setCollisionProperties(CGROUP_NONE, LAYERSPEC_ENEMY)

    def setCollisionCollectable(self):
        "Sets the actor's collision properties to that suitable for a collectable."
        self.setCollisionProperties(CGROUP_NONE, LAYERSPEC_COLLECTABLE)

    def setCollisionPlayerBullet(self):
        "Sets the actor's collision properties to that suitable for a player bullet."
        self.setCollisionProperties(CGROUP_NONE, LAYERSPEC_PLAYERBULLET)

    def setCollisionEnemyBullet(self):
        "Sets the actor's collision properties to that suitable for an enemy bullet."
        self.setCollisionProperties(CGROUP_NONE, LAYERSPEC_ENEMYBULLET)

    def setCollisionTerrain(self):
        self.setCollisionProperties(CGROUP_NONE, LAYERSPEC_ALL)

    def setCollisionGate(self):
        self.setCollisionProperties(CGROUP_NONE, LAYERSPEC_GATE)

    def setFriction(self, f):
        for shape in self.shapes:
            shape.friction = f

    def setElasticity(self, e):
        for shape in self.shapes:
            shape.elasticity = e


    # Double-dispatch FTW
    # Note these are collision _handlers_, not detection.
    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithNone(self, arbiter)

    # Colliding with an object of no specific type.
    # This is basically here as a catch-all, just in case.
    def startCollisionWithNone(self, other, arbiter):
        return True

    def startCollisionWithTerrain(self, other, arbiter):
        return True

    def startCollisionWithPlayer(self, other, arbiter):
        return True

    def startCollisionWithCollectable(self, other, arbiter):
        return True

    def startCollisionWithPlayerBullet(self, other, arbiter):
        return True

    def startCollisionWithEnemyBullet(self, other, arbiter):
        return True

    def startCollisionWithEnemy(self, other, arbiter):
        return True

    def startCollisionWithGate(self, other, arbiter):
        return True

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithNone(self, arbiter)

    def endCollisionWithNone(self, other, arbiter):
        return False

    def endCollisionWithTerrain(self, other, arbiter):
        return True

    def endCollisionWithPlayer(self, other, arbiter):
        return True

    def endCollisionWithCollectable(self, other, arbiter):
        return True

    def endCollisionWithPlayerBullet(self, other, arbiter):
        return True

    def endCollisionWithEnemyBullet(self, other, arbiter):
        return True

    def endCollisionWithEnemy(self, other, arbiter):
        return True

    def endCollisionWithGate(self, other, arbiter):
        return True


            
class PlayerPhysicsObj(PhysicsObj):
    def __init__(self, owner, **kwargs):
        PhysicsObj.__init__(self, owner, mass=1, moment=200, **kwargs)
        self.addShapes(pymunk.Circle(self.body, radius=self.owner.radius))
        self.setFriction(6.0)
        self.setCollisionPlayer()
        self.velocity_limit = (400)

    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithPlayer(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithPlayer(self, arbiter)

    def startCollisionWithCollectable(self, other, arbiter):
        "The handler for a player collecting a Collectable."
        #print space, arbiter, args, kwargs
        collectable =  other.owner
        player = self.owner
        collectable.collect(player)
        collectable.alive = False
        return False

    def startCollisionWithTerrain(self, other, arbiter):
        for c in arbiter.contacts:
            normal = c.normal
            # This is not exactly 0 because floating point error
            # means a lot of the time a horizontal collision has
            # a vertical component of like -1.0e-15
            # But in general, if we hit something moving downward,
            # the y component of the normal is < 0

            if normal.y < -0.001:
                self.owner.onGround = True
        return True

    def endCollisionWithTerrain(self, other, arbiter):
        self.owner.onGround = False

    def startCollisionWithGate(self, other, arbiter):
        print 'starting collision with gate'
        gate = other.owner
        self.owner.gate = gate
        return False

    def endCollisionWithGate(self, other, arbiter):
        player = self.owner
        player.gate = None

    def startCollisionWithEnemyBullet(self, other, arbiter):
        print 'ow!'
        return False

class GatePhysicsObj(PhysicsObj):
    def __init__(self, owner, **kwargs):
        PhysicsObj.__init__(self, owner, **kwargs)
        poly = pymunk.Poly(self.body, rectCornersCenter(0, 0, 20, 20))
        # Sensors call collision callbacks but don't actually do any physics.
        poly.sensor = True
        self.addShapes(poly)
        self.setCollisionGate()
    
    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithGate(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithGate(self, arbiter)

        
class PlayerBulletPhysicsObj(PhysicsObj):
    """A generic physics object for small round things that hit stuff."""
    def __init__(self, owner, mass=1, moment=10, **kwargs):
        PhysicsObj.__init__(self, owner, mass=mass, moment=moment, **kwargs)

    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithPlayerBullet(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithPlayerBullet(self, arbiter)

    def startCollisionWithTerrain(self, other, arbiter):
        bullet = self.owner
        target = other.owner
        bullet.alive = False
        # XXX: This is a bit of a hack for DestroyableBlock's
        if target.life is not None:
            target.life.takeDamage(bullet, bullet.damage)
        return False

    def startCollisionWithEnemy(self, other, arbiter):
        bullet = self.owner
        enemy = other.owner
        bullet.alive = False
        enemy.life.takeDamage(bullet, bullet.damage)
        return False

class BeginningsBulletPhysicsObj(PlayerBulletPhysicsObj):
    def __init__(self, owner, **kwargs):
        PlayerBulletPhysicsObj.__init__(self, owner, **kwargs)
        self.addShapes(pymunk.Circle(self.body, radius=2))
        self.setCollisionPlayerBullet()

    
class AirP1PhysicsObjAir(PlayerBulletPhysicsObj):
    def __init__(self, owner, **kwargs):
        PlayerBulletPhysicsObj.__init__(self, owner, **kwargs)
        shape = pymunk.Poly(self.body, rectCornersCenter(0, 0, 5, 40))
        self.addShapes(shape)
        self.setCollisionPlayerBullet()

class AirP1PhysicsObjGround(PlayerBulletPhysicsObj):
    def __init__(self, owner, **kwargs):
        PlayerBulletPhysicsObj.__init__(self, owner, **kwargs)
        shape = pymunk.Poly(self.body, rectCornersCenter(0, 0, 5, 15))
        self.addShapes(shape)
        self.setCollisionPlayerBullet()

# XXX: 
# Maybe inherits from a BeamPhysicsObject or such...
# Though most beams are really made up of lots of bullet
# segments in games like this, see Viy's beam from La Mulana
class AirP2PhysicsObj(PhysicsObj):
    def __init__(self, owner, **kwargs):
        PhysicsObj.__init__(self, owner, mass=1, moment=100, **kwargs)
        self.body.position_func = AirP2PhysicsObj.position_func
        self.width = 200
        self.height = 10
        shape = pymunk.Poly(self.body, rectCornersCorner(0, -(self.height / 2), self.width * owner.facing, self.height))
        self.addShapes(shape)
        self.setCollisionPlayerBullet()

    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithPlayerBullet(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithPlayerBullet(self, arbiter)

    def startCollisionWithTerrain(self, other, arbiter):
        # XXX: This is a bit of a hack for DestroyableBlock's
        target = other.owner
        if target.life is not None:
            self.owner.enemiesTouching.add(target)
            
        return False

    def startCollisionWithEnemy(self, other, arbiter):
        self.owner.enemiesTouching.add(other.owner)
        return False

    def endCollisionWithEnemy(self, other, arbiter):
        self.owner.enemiesTouching.remove(other.owner)

    @staticmethod
    def position_func(body, dt):
        # As we yo-yo up and down the object tree to get the location of
        # the actor who actually fired the bullet...
        # Then we just make the bullet follow them along.
        x, y = body.component.owner.firer.physicsObj.position
        body.position = (x, y)
    
class EnemyBulletPhysicsObj(PhysicsObj):
    """A generic physics object for small round things that hit the player."""
    def __init__(self, owner, mass=1, moment=10, **kwargs):
        PhysicsObj.__init__(self, owner, mass=mass, moment=moment, **kwargs)
        # TODO: Get rid of these when you subclass this
        self.addShapes(pymunk.Circle(self.body, radius=2))
        self.setCollisionEnemyBullet()

    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithEnemyBullet(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithEnemyBullet(self, arbiter)

    def startCollisionWithTerrain(self, other, arbiter):
        self.owner.alive = False
        return False

    def startCollisionWithPlayer(self, other, arbiter):
        bullet = self.owner
        player = other.owner
        bullet.alive = False
        player.life.takeDamage(bullet, bullet.damage)
        return False

        
class BlockPhysicsObj(PhysicsObj):
    """Generic immobile rectangle physics object"""
    def __init__(self, owner, **kwargs):
        # Static body 
        PhysicsObj.__init__(self, owner, **kwargs)
        self.addShapes(pymunk.Poly(self.body, owner.corners))
        #print owner.corners
        self.setFriction(0.8)
        self.setElasticity(0.8)
        self.setCollisionTerrain()

    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithTerrain(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithTerrain(self, arbiter)


class FallingBlockPhysicsObj(PhysicsObj):
    """Like a BlockPhysicsObject but...

BUGGO: A BlockPhyicsObj's body-center is actually not
in the center of the shape, and that's terrible.

This is because the shape is specified by the corners,
and life just gets shitty and weird.  idfk"""
    def __init__(self, owner, **kwargs):
        PhysicsObj.__init__(self, owner, mass=20, moment=pymunk.inf, **kwargs)
        constraintBody = pymunk.Body()
        self.addConstraints(pymunk.constraint.GrooveJoint(constraintBody, self.body,
                                                    (0, -50), (0, -150),
                                                     (100, 0)))
        constraintBody.position = (-200, 100)
        self.addAuxBodys(constraintBody)
        self.addShapes(pymunk.Poly(self.body, owner.corners))
        self.setFriction(0.1)
        self.setElasticity(0.8)
        self.setCollisionTerrain()
        self.body.apply_force((0, 400 * self.body.mass + 100))

        
    # Gotta redefine this 'cause constraints
    # need to move along with the body
    # Wow, properties saved me trouble for once.
    def _set_position(self, pos):
        self.body.position = pos
        for aux in self.auxBodys:
            aux.position = pos
    position = property(lambda s: self.body.position, _set_position)

    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithTerrain(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithTerrain(self, arbiter)


# BUGGO: Why do these fall through floors?
class CollectablePhysicsObj(PhysicsObj):
    def __init__(self, owner, **kwargs):
        PhysicsObj.__init__(self, owner, 1, 200, **kwargs)
        corners = []
        corners.append(rectCornersCenter(0, 0, 10, 5))
        corners.append(rectCornersCenter(0, 0, 5, 10))

        self.addShapes(*[
            pymunk.Poly(self.body, c)
            for c in corners
            ])
        self.setElasticity(0.8)
        self.setFriction(2.0)
        self.setCollisionCollectable()

    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithCollectable(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithCollectable(self, arbiter)

        
class PowerupPhysicsObj(PhysicsObj):
    def __init__(self, owner, **kwargs):
        PhysicsObj.__init__(self, owner, **kwargs) # Static physics object
        corners = rectCornersCenter(0, 0, 5, 5)

        shape = pymunk.Poly(self.body, corners)
        shape.Sensor = True
        self.addShapes(shape)
        self.setCollisionCollectable()

    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithCollectable(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithCollectable(self, arbiter)

class EnemyPhysicsObj(PhysicsObj):
    def __init__(self, owner, mass=100, moment=None, **kwargs):
        PhysicsObj.__init__(self, owner, mass=mass, moment=moment, **kwargs)
        
    def startCollisionWith(self, other, arbiter):
        return other.startCollisionWithEnemy(self, arbiter)

    def endCollisionWith(self, other, arbiter):
        return other.endCollisionWithEnemy(self, arbiter)

    def startCollisionWithPlayer(self, other, arbiter):
        return False

    def startCollisionWithEnemyBullet(self, other, arbiter):
        return False

    def startCollisionWithEnemy(self, other, arbiter):
        return False


class CrawlerPhysicsObj(EnemyPhysicsObj):
    def __init__(self, owner, **kwargs):
        EnemyPhysicsObj.__init__(self, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        self.addShapes(pymunk.Poly(self.body, corners))
        self.setCollisionEnemy()

    # XXX: This feels awkward because reacting to collisions should
    # be the pervue of the Controller, not the physics obj.  Maybe
    # just send it a 'hit wall' message...
    def startCollisionWithTerrain(self, other, arbiter):
        for c in arbiter.contacts:
            normal = c.normal
            if abs(normal.y) < 0.0001:
                self.owner.facing *= -1
                break
        return True

class TrooperPhysicsObj(EnemyPhysicsObj):
    def __init__(self, owner, **kwargs):
        EnemyPhysicsObj.__init__(self, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 15, 30)

        self.addShapes(pymunk.Poly(self.body, corners))
        self.setCollisionEnemy()

    def startCollisionWithPlayer(self, other, arbiter):
        return False

    # XXX: See CrawlerPhysicsObj note
    def startCollisionWithTerrain(self, other, arbiter):
        for c in arbiter.contacts:
            normal = c.normal
            if abs(normal.y) < 0.0001:
                self.owner.facing *= -1
                break
        return True


class ArcherPhysicsObj(EnemyPhysicsObj):
    def __init__(self, owner, **kwargs):
        EnemyPhysicsObj.__init__(self, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 25, 12)

        self.addShapes(pymunk.Poly(self.body, corners))
        self.setCollisionEnemy()
    
class FloaterPhysicsObj(EnemyPhysicsObj):
    def __init__(self, owner, **kwargs):
        EnemyPhysicsObj.__init__(self, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 17, 17)
        self.negateGravity()

        self.addShapes(pymunk.Poly(self.body, corners))
        self.setCollisionEnemy()
    
    # XXX: See CrawlerPhysicsObj note
    def startCollisionWithTerrain(self, other, arbiter):
        self.owner.facing *= -1
        return True

    
class ElitePhysicsObj(EnemyPhysicsObj):
    def __init__(self, owner, **kwargs):
        EnemyPhysicsObj.__init__(self, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        self.addShapes(pymunk.Poly(self.body, corners))
        self.setCollisionEnemy()
    
class HeavyPhysicsObj(EnemyPhysicsObj):
    def __init__(self, owner, **kwargs):
        EnemyPhysicsObj.__init__(self, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        self.addShapes(pymunk.Poly(self.body, corners))
        self.setCollisionEnemy()

    
class DragonPhysicsObj(EnemyPhysicsObj):
    def __init__(self, owner, **kwargs):
        EnemyPhysicsObj.__init__(self, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        self.addShapes(pymunk.Poly(self.body, corners))
        self.setCollisionEnemy()


class AnnihilatorPhysicsObj(EnemyPhysicsObj):
    def __init__(self, owner, **kwargs):
        EnemyPhysicsObj.__init__(self, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        self.addShapes(pymunk.Poly(self.body, corners))
        self.setCollisionEnemy()


######################################################################
##  Misc components
######################################################################

class Life(Component):
    """A component that keeps track of life and kills
its owner when it runs out."""
    def __init__(self, owner, hps, maxLife=None, attenuation = 1.0, reduction = 0):
        Component.__init__(self, owner)
        self.life = hps
        self.maxLife = maxLife
        if self.maxLife is None:
            self.maxLife = hps
        
        # A multiplier to the damage taken
        self.damageAttenuation = attenuation

        # A subtractor to the damage taken
        # Happens BEFORE attenuation
        self.damageReduction = reduction

    def takeDamage(self, damager, damage):
        reducedDamage = max(0, damage - self.damageReduction)
        attenuatedDamage = reducedDamage * self.damageAttenuation
        #print "Damage: {}, reducedDamage: {}, attenuatedDamage: {}".format(damage, reducedDamage, attenuatedDamage)
        if attenuatedDamage >=4:
            rcache.get_sound("damage_4").play()
        if attenuatedDamage >=3:
            rcache.get_sound("damage_3").play()
        elif attenuatedDamage>0:
            rcache.get_sound("damage").play()
        else:
            rcache.get_sound("damage_0").play()
        self.life -= attenuatedDamage
        if self.life <= 0:
            self.owner.alive = False
        print "Took {} out of {} damage, life is now {}".format(attenuatedDamage, damage, self.life)


class Energy(Component):
    """mana for powers"""
    def __init__(self, owner, maxEnergy=100.0, regenRate=10.0):
        Component.__init__(self,owner)

        self.maxEnergy=maxEnergy
        self.energy=maxEnergy/2
        self.regenRate=regenRate

    def expend(self,amount):
        if(amount <=self.energy):
            self.energy=self.energy-amount
            return True
        else:
            return False

    def update(self,dt):
        if(self.energy<self.maxEnergy):
            self.energy=self.energy+self.regenRate*dt
        if(self.energy>self.maxEnergy):
            self.energy=self.maxEnergy
                        

class TimedLife(Component):
    """A component that kills the owner after
a certain timeout."""
    def __init__(self, owner, time):
        Component.__init__(self, owner)
        self.time = float(time)
        self.maxTime = time

    def update(self, dt):
        self.time -= dt
        if self.time <= 0:
            self.owner.alive = False

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
become significant if they do things that directly affect other Actor."""
    def __init__(self, time=0.0, defaultTime = 0.0):
        self.time = float(time)
        self.defaultTime = defaultTime

    def reset(self):
        """Resets the timer to the default time."""
        self.time = self.defaultTime
        
    def update(self, dt):
        self.time -= dt

    def expired(self):
        return self.time <= 0.0

class ParticleSystem(Component):
    """A test component that emits particles."""
    def __init__(self, owner):
        Component.__init__(self, owner)
        self.tex = rcache.get_image("playertest").get_texture()
        self.sparkGroup = ParticleGroup(
            controllers=[
                Lifetime(3),
                Movement(damping=0.93),
                Fader(fade_out_start=0.75, fade_out_end=3.0)
                ],
            renderer=BillboardRenderer(SpriteTexturizer(self.tex.id))
            )
        self.sparkEmitter = StaticEmitter(
            template=Particle(
                position = (100, 0, -100),
                color = (1,1,1),
                size=(20,20,0)),
            deviation=Particle(
                position=(2,2,0),
                velocity=(75,75,0),
                size=(0.2,0.2,0),
                age=15))

    def update(self,dt):
        x,y = self.owner.physicsObj.position
        self.sparkEmitter.template.position.x = x
        self.sparkEmitter.template.position.y = y
        #s.sparkEmitter.position = 
        self.sparkEmitter.emit(int(100*dt), s.sparkGroup)
        
