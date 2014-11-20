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
LAYER_DOOR         = 1 << 5

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
        s.moveForce = 400

    def update(s, dt):
        s.owner.physicsObj.apply_impulse((s.moveForce * dt * s.owner.facing, 0))


STATE_WANDERING = 0
STATE_ATTACKING = 1
class TrooperAIController(Component):
    """Occasionally tests to see if the player is in front of it.
If it is, DESTROY"""
    def __init__(s, owner):
        Component.__init__(s, owner)
        s.moveForce = 400
        s.sightRange = 500
        s.sightHeight = 100
        s.sightCheck = Timer(defaultTime=1.0)
        s.lastSawPlayer = Timer(defaultTime=10.0)
        s.state = STATE_WANDERING

    def checkSight(s):
        """Check if we can see the player.
First we do a BB query in front of us.  If that
succeeds, we do a segment query that intersects
terrain and players, and if that succeeds, we can
see the player.

XXX: This method might best be refactored out into a superclass."""
        print "Checking sight..."
        space = s.owner.world.space
        selfPosition = s.owner.physicsObj.position
        selfFacing = s.owner.facing
        bottom = selfPosition.y - s.sightHeight/2
        top = selfPosition.y + s.sightHeight/2
        left = selfPosition.x
        right = selfPosition.x + s.sightRange
        if selfFacing == FACING_LEFT:
            right = left
            left = selfPosition.x - s.sightRange

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
            
    def update(s, dt):
        s.owner.physicsObj.apply_impulse((s.moveForce * dt * s.owner.facing, 0))
        s.sightCheck.update(dt)
        if s.sightCheck.expired():
            s.sightCheck.reset()
            if s.checkSight():
                s.owner.fireBullet(actor.TrooperBullet)
                rcache.get_sound("EnemyShot1").play()
            
class ArcherAIController(Component):
    """Archers are immobile, they sit in one place and lob projectiles."""
    def __init__(s, owner):
        Component.__init__(s, owner)
        s.fireTimer = Timer(defaultTime=2.5)

    def update(s, dt):
        s.fireTimer.update(dt)
        if s.fireTimer.expired():
            s.fireTimer.reset()
            s.owner.fireBullet(actor.TrooperBullet, facing=FACING_LEFT)
            s.owner.fireBullet(actor.TrooperBullet, facing=FACING_RIGHT)
            rcache.get_sound("EnemyShot1").play()

class FloaterAIController(Component):
    """Floaters wander back and forth and fire at the player if they are close.
Their vision, and firing, is omnidirectional, and they can see through walls."""
    def __init__(s, owner, moveDirection=(1,0)):
        Component.__init__(s, owner)
        s.moveForce = 400
        s.moveDirection = moveDirection
        s.fireTimer = Timer(defaultTime=1.0)
        s.sightRadius = 100

    def update(s, dt):
        dirX, dirY = s.moveDirection
        xForce = s.moveForce * dirX * s.owner.facing
        yForce = s.moveForce * dirY * s.owner.facing
        s.owner.physicsObj.apply_impulse((xForce * dt, yForce * dt))
        s.fireTimer.update(dt)
        
        if s.fireTimer.expired() and s.checkVision():
            s.fireTimer.reset()
            s.fire()

    def checkVision(s):
        space = s.owner.world.space
        selfPosition = s.owner.physicsObj.position
        selfFacing = s.owner.facing
        bottom = selfPosition.y - s.sightRadius
        top = selfPosition.y + s.sightRadius
        left = selfPosition.x - s.sightRadius
        right = selfPosition.x + s.sightRadius

        bb = pymunk.BB(left, bottom, right, top)
        shapes = space.bb_query(bb, LAYERSPEC_PLAYER)
        for shape in shapes:
            act = shape.body.component.owner
            if isinstance(act, actor.Player):
                return True
            
        return False
        return True

    def fire(s):
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
            s.owner.fireBulletAt(actor.FloaterBullet, (xPos, yPos), (xForce, yForce), facing=FACING_RIGHT)
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
    def __init__(s, owner, mass=None, moment=None, position=(0,0)):
        Component.__init__(s, owner)
        s.owner = owner
        
        s.body = pymunk.Body(mass=mass, moment=moment)
        s.body.position = position
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

    # XXX: This function is what can only be described
    # as a bad approximation... but it's also only used
    # in the level editor, which doesn't have an actual
    # running physics space in it.
    def getBB(s):
        for shape in s.shapes:
            shape.cache_bb()
            return shape.bb

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

    def negateGravity(s):
        force = 400 * s.body.mass
        s.apply_force((0, force))

    def setCollisionProperties(s, group, layerspec):
        "Set the actor's collision properties."
        for shape in s.shapes:
            shape.collision_type = group
            shape.layers = layerspec

    def setCollisionPlayer(s):
        "Sets the actor's collision properties to that suitable for a player."
        s.setCollisionProperties(CGROUP_NONE, LAYERSPEC_PLAYER)

    def setCollisionEnemy(s):
        "Sets the actor's collision properties to that suitable for an enemy."
        s.setCollisionProperties(CGROUP_NONE, LAYERSPEC_ENEMY)

    def setCollisionCollectable(s):
        "Sets the actor's collision properties to that suitable for a collectable."
        s.setCollisionProperties(CGROUP_NONE, LAYERSPEC_COLLECTABLE)

    def setCollisionPlayerBullet(s):
        "Sets the actor's collision properties to that suitable for a player bullet."
        s.setCollisionProperties(CGROUP_NONE, LAYERSPEC_PLAYERBULLET)

    def setCollisionEnemyBullet(s):
        "Sets the actor's collision properties to that suitable for an enemy bullet."
        s.setCollisionProperties(CGROUP_NONE, LAYERSPEC_ENEMYBULLET)

    def setCollisionTerrain(s):
        s.setCollisionProperties(CGROUP_NONE, LAYERSPEC_ALL)

    def setCollisionDoor(s):
        s.setCollisionProperties(CGROUP_NONE, LAYERSPEC_DOOR)

    def setFriction(s, f):
        for shape in s.shapes:
            shape.friction = f

    def setElasticity(s, e):
        for shape in s.shapes:
            shape.elasticity = e


    # Double-dispatch FTW
    # Note these are collision _handlers_, not detection.
    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithNone(s, arbiter)

    # Colliding with an object of no specific type.
    # This is basically here as a catch-all, just in case.
    def startCollisionWithNone(s, other, arbiter):
        return True

    def startCollisionWithTerrain(s, other, arbiter):
        return True

    def startCollisionWithPlayer(s, other, arbiter):
        return True

    def startCollisionWithCollectable(s, other, arbiter):
        return True

    def startCollisionWithPlayerBullet(s, other, arbiter):
        return True

    def startCollisionWithEnemyBullet(s, other, arbiter):
        return True

    def startCollisionWithEnemy(s, other, arbiter):
        return True

    def startCollisionWithDoor(s, other, arbiter):
        return True

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithNone(s, arbiter)

    def endCollisionWithNone(s, other, arbiter):
        return False

    def endCollisionWithTerrain(s, other, arbiter):
        return True

    def endCollisionWithPlayer(s, other, arbiter):
        return True

    def endCollisionWithCollectable(s, other, arbiter):
        return True

    def endCollisionWithPlayerBullet(s, other, arbiter):
        return True

    def endCollisionWithEnemyBullet(s, other, arbiter):
        return True

    def endCollisionWithEnemy(s, other, arbiter):
        return True

    def endCollisionWithDoor(s, other, arbiter):
        return True



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
    def __init__(s, owner, **kwargs):
        PhysicsObj.__init__(s, owner, mass=1, moment=200, **kwargs)
        s.addShapes(pymunk.Circle(s.body, radius=s.owner.radius))
        s.setFriction(6.0)
        s.setCollisionPlayer()
        s.velocity_limit = (400)

    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithPlayer(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithPlayer(s, arbiter)

    def startCollisionWithCollectable(s, other, arbiter):
        "The handler for a player collecting a Collectable."
        #print space, arbiter, args, kwargs
        collectable =  other.owner
        player = s.owner
        collectable.collect(player)
        collectable.alive = False
        return False

    def startCollisionWithTerrain(s, other, arbiter):
        for c in arbiter.contacts:
            normal = c.normal
            # This is not exactly 0 because floating point error
            # means a lot of the time a horizontal collision has
            # a vertical component of like -1.0e-15
            # But in general, if we hit something moving downward,
            # the y component of the normal is < 0
            #
            # TODO: Oooh, we should probably see if there's a
            # callback for when two things _stop_ colliding with each other,
            # I think there is.  Setting onGround to false in such a callback
            # would be a good thing

            if normal.y < -0.001:
                s.owner.onGround = True
        return True

    def endCollisionWithTerrain(s, other, arbiter):
        s.owner.onGround = False

    def startCollisionWithDoor(s, other, arbiter):
        print 'starting collision with door'
        door = other.owner
        s.owner.door = door
        return False

    def endCollisionWithDoor(s, other, arbiter):
        player = s.owner
        player.door = None

    def startCollisionWithEnemyBullet(s, other, arbiter):
        print 'ow!'
        return False

class DoorPhysicsObj(PhysicsObj):
    def __init__(s, owner, **kwargs):
        PhysicsObj.__init__(s, owner, **kwargs)
        poly = pymunk.Poly(s.body, rectCornersCenter(0, 0, 20, 20))
        # Sensors call collision callbacks but don't actually do any physics.
        poly.sensor = True
        s.addShapes(poly)
        s.setCollisionDoor()
    
    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithDoor(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithDoor(s, arbiter)

        
class PlayerBulletPhysicsObj(PhysicsObj):
    """A generic physics object for small round things that hit stuff."""
    def __init__(s, owner, mass=1, moment=10, **kwargs):
        PhysicsObj.__init__(s, owner, mass=mass, moment=moment, **kwargs)

    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithPlayerBullet(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithPlayerBullet(s, arbiter)

    def startCollisionWithTerrain(s, other, arbiter):
        s.owner.alive = False
        return False

    def startCollisionWithEnemy(s, other, arbiter):
        bullet = s.owner
        enemy = other.owner
        bullet.alive = False
        enemy.life.takeDamage(bullet, bullet.damage)
        return False

class BeginningsBulletPhysicsObj(PlayerBulletPhysicsObj):
    def __init__(s, owner, **kwargs):
        PlayerBulletPhysicsObj.__init__(s, owner, **kwargs)
        s.addShapes(pymunk.Circle(s.body, radius=2))
        s.setCollisionPlayerBullet()

    
class AirP1PhysicsObjAir(PlayerBulletPhysicsObj):
    def __init__(s, owner, **kwargs):
        PlayerBulletPhysicsObj.__init__(s, owner, **kwargs)
        shape = pymunk.Poly(s.body, rectCornersCenter(0, 0, 5, 40))
        s.addShapes(shape)
        s.setCollisionPlayerBullet()

class AirP1PhysicsObjGround(PlayerBulletPhysicsObj):
    def __init__(s, owner, **kwargs):
        PlayerBulletPhysicsObj.__init__(s, owner, **kwargs)
        shape = pymunk.Poly(s.body, rectCornersCenter(0, 0, 5, 15))
        s.addShapes(shape)
        s.setCollisionPlayerBullet()

# XXX: 
# Maybe inherits from a BeamPhysicsObject or such...
# Though most beams are really made up of lots of bullet
# segments in games like this, see Viy's beam from La Mulana
class AirP2PhysicsObj(PhysicsObj):
    def __init__(s, owner, **kwargs):
        PhysicsObj.__init__(s, owner, mass=1, moment=100, **kwargs)
        s.body.position_func = AirP2PhysicsObj.position_func
        s.width = 200
        s.height = 10
        shape = pymunk.Poly(s.body, rectCornersCorner(0, -(s.height / 2), s.width * owner.facing, s.height))
        s.addShapes(shape)
        s.setCollisionPlayerBullet()

    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithPlayerBullet(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithPlayerBullet(s, arbiter)

    def startCollisionWithTerrain(s, other, arbiter):
        return False

    def startCollisionWithEnemy(s, other, arbiter):
        s.owner.enemiesTouching.add(other.owner)
        return False

    def endCollisionWithEnemy(s, other, arbiter):
        s.owner.enemiesTouching.remove(other.owner)

    @staticmethod
    def position_func(body, dt):
        # As we yo-yo up and down the object tree to get the location of
        # the actor who actually fired the bullet...
        # Then we just make the bullet follow them along.
        x, y = body.component.owner.firer.physicsObj.position
        body.position = (x, y)
    
class EnemyBulletPhysicsObj(PhysicsObj):
    """A generic physics object for small round things that hit the player."""
    def __init__(s, owner, mass=1, moment=10, **kwargs):
        PhysicsObj.__init__(s, owner, mass=mass, moment=moment, **kwargs)
        # TODO: Get rid of these when you subclass this
        s.addShapes(pymunk.Circle(s.body, radius=2))
        s.setCollisionEnemyBullet()

    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithEnemyBullet(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithEnemyBullet(s, arbiter)

    def startCollisionWithTerrain(s, other, arbiter):
        s.owner.alive = False
        return False

    def startCollisionWithPlayer(s, other, arbiter):
        bullet = s.owner
        player = other.owner
        bullet.alive = False
        player.life.takeDamage(bullet, bullet.damage)
        return False

        
class BlockPhysicsObj(PhysicsObj):
    """Generic immobile rectangle physics object"""
    def __init__(s, owner, **kwargs):
        # Static body 
        PhysicsObj.__init__(s, owner, **kwargs)
        s.addShapes(pymunk.Poly(s.body, owner.corners))
        #print owner.corners
        s.setFriction(0.8)
        s.setElasticity(0.8)
        s.setCollisionTerrain()

    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithTerrain(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithTerrain(s, arbiter)

        
class FallingBlockPhysicsObj(PhysicsObj):
    """Like a BlockPhysicsObject but...

BUGGO: A BlockPhyicsObj's body-center is actually not
in the center of the shape, and that's terrible.

This is because the shape is specified by the corners,
and life just gets shitty and weird.  idfk"""
    def __init__(s, owner, **kwargs):
        PhysicsObj.__init__(s, owner, mass=20, moment=pymunk.inf, **kwargs)
        constraintBody = pymunk.Body()
        s.addConstraints(pymunk.constraint.GrooveJoint(constraintBody, s.body,
                                                    (0, -50), (0, -150),
                                                     (100, 0)))
        constraintBody.position = (-200, 100)
        s.addAuxBodys(constraintBody)
        s.addShapes(pymunk.Poly(s.body, owner.corners))
        s.setFriction(0.1)
        s.setElasticity(0.8)
        s.setCollisionTerrain()
        s.body.apply_force((0, 400 * s.body.mass + 100))

        
    # Gotta redefine this 'cause constraints
    # need to move along with the body
    # Wow, properties saved me trouble for once.
    def _set_position(s, pos):
        s.body.position = pos
        for aux in s.auxBodys:
            aux.position = pos
    position = property(lambda s: s.body.position, _set_position)

    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithTerrain(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithTerrain(s, arbiter)


# BUGGO: Why do these fall through floors?
class CollectablePhysicsObj(PhysicsObj):
    def __init__(s, owner, **kwargs):
        PhysicsObj.__init__(s, owner, 1, 200, **kwargs)
        corners = []
        corners.append(rectCornersCenter(0, 0, 10, 5))
        corners.append(rectCornersCenter(0, 0, 5, 10))

        s.addShapes(*[
            pymunk.Poly(s.body, c)
            for c in corners
            ])
        s.setElasticity(0.8)
        s.setFriction(2.0)
        s.setCollisionCollectable()

    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithCollectable(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithCollectable(s, arbiter)

        
class PowerupPhysicsObj(PhysicsObj):
    def __init__(s, owner, **kwargs):
        PhysicsObj.__init__(s, owner, **kwargs) # Static physics object
        corners = rectCornersCenter(0, 0, 5, 5)

        shape = pymunk.Poly(s.body, corners)
        shape.Sensor = True
        s.addShapes(shape)
        s.setCollisionCollectable()

    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithCollectable(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithCollectable(s, arbiter)

class EnemyPhysicsObj(PhysicsObj):
    def __init__(s, owner, mass=100, moment=None, **kwargs):
        PhysicsObj.__init__(s, owner, mass=mass, moment=moment, **kwargs)
        
    def startCollisionWith(s, other, arbiter):
        return other.startCollisionWithEnemy(s, arbiter)

    def endCollisionWith(s, other, arbiter):
        return other.endCollisionWithEnemy(s, arbiter)

    def startCollisionWithPlayer(s, other, arbiter):
        return False

    def startCollisionWithEnemyBullet(s, other, arbiter):
        return False

    def startCollisionWithEnemy(s, other, arbiter):
        return False


class CrawlerPhysicsObj(EnemyPhysicsObj):
    def __init__(s, owner, **kwargs):
        EnemyPhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()

    # XXX: This feels awkward because reacting to collisions should
    # be the pervue of the Controller, not the physics obj.  Maybe
    # just send it a 'hit wall' message...
    def startCollisionWithTerrain(s, other, arbiter):
        for c in arbiter.contacts:
            normal = c.normal
            if abs(normal.y) < 0.0001:
                s.owner.facing *= -1
                break
        return True

class TrooperPhysicsObj(EnemyPhysicsObj):
    def __init__(s, owner, **kwargs):
        EnemyPhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 15, 30)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()

    def startCollisionWithPlayer(s, other, arbiter):
        return False

    # XXX: See CrawlerPhysicsObj note
    def startCollisionWithTerrain(s, other, arbiter):
        for c in arbiter.contacts:
            normal = c.normal
            if abs(normal.y) < 0.0001:
                s.owner.facing *= -1
                break
        return True


class ArcherPhysicsObj(EnemyPhysicsObj):
    def __init__(s, owner, **kwargs):
        EnemyPhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 25, 12)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()
    
class FloaterPhysicsObj(EnemyPhysicsObj):
    def __init__(s, owner, **kwargs):
        EnemyPhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 17, 17)
        s.negateGravity()

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()
    
    # XXX: See CrawlerPhysicsObj note
    def startCollisionWithTerrain(s, other, arbiter):
        s.owner.facing *= -1
        return True

    
class ElitePhysicsObj(EnemyPhysicsObj):
    def __init__(s, owner, **kwargs):
        EnemyPhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()
    
class HeavyPhysicsObj(EnemyPhysicsObj):
    def __init__(s, owner, **kwargs):
        EnemyPhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()

    
class DragonPhysicsObj(EnemyPhysicsObj):
    def __init__(s, owner, **kwargs):
        EnemyPhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()


class AnnihilatorPhysicsObj(EnemyPhysicsObj):
    def __init__(s, owner, **kwargs):
        EnemyPhysicsObj.__init__(s, owner, mass=100, moment=pymunk.inf, **kwargs)
        corners = rectCornersCenter(0, 0, 12, 10)

        s.addShapes(pymunk.Poly(s.body, corners))
        s.setCollisionEnemy()


######################################################################
##  Misc components
######################################################################

class Life(Component):
    """A component that keeps track of life and kills
its owner when it runs out."""
    def __init__(s, owner, hps, maxLife=None, attenuation = 1.0, reduction = 0):
        Component.__init__(s, owner)
        s.life = hps
        s.maxLife = maxLife
        if s.maxLife is None:
            s.maxLife = hps
        
        # A multiplier to the damage taken
        s.damageAttenuation = attenuation

        # A subtractor to the damage taken
        # Happens BEFORE attenuation
        s.damageReduction = reduction

    def takeDamage(s, damager, damage):
        reducedDamage = max(0, damage - s.damageReduction)
        attenuatedDamage = reducedDamage * s.damageAttenuation
        #print "Damage: {}, reducedDamage: {}, attenuatedDamage: {}".format(damage, reducedDamage, attenuatedDamage)
        if attenuatedDamage >=4:
            rcache.get_sound("damage_4").play()
        if attenuatedDamage >=3:
            rcache.get_sound("damage_3").play()
        elif attenuatedDamage>0:
            rcache.get_sound("damage").play()
        else:
            rcache.get_sound("damage_0").play()
        s.life -= attenuatedDamage
        if s.life <= 0:
            s.owner.alive = False
        print "Took {} out of {} damage, life is now {}".format(attenuatedDamage, damage, s.life)


class Energy(Component):
    """mana for powers"""
    def __init__(s, owner, maxEnergy=100.0, regenRate=10.0):
        Component.__init__(s,owner)

        s.maxEnergy=maxEnergy
        s.energy=maxEnergy/2
        s.regenRate=regenRate

    def expend(s,amount):
        if(amount <=s.energy):
            s.energy=s.energy-amount
            return True
        else:
            return False

    def update(s,dt):
        if(s.energy<s.maxEnergy):
            s.energy=s.energy+s.regenRate*dt
        if(s.energy>s.maxEnergy):
            s.energy=s.maxEnergy
                        

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
        
