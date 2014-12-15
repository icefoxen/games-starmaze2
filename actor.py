import itertools
import math
import random

import pyglet
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util

from component import *
import rcache
import images
from renderer import *

def described(cls):
    """MAGIC!
A decorator that takes a class and makes some changes to make it effectively
able to serialize and de-serialize instances of itself:

* Alters its __init__ method to save copies of its args
* Adds a method 'describe' to return a function that, when called, will re-instantiate
  the object with its original args
* Adds a method 'describeString' to return a string that is valid Python code to
  return a description that creates the Actor.

Thus to turn an Actor into a described form, one only to call the Actor's describe()
method, which returns a function.  Call said function to produce an Actor.

You can also call the Actor's describeString() method to get a string that can be,
say, put into a level's definition file and create said Actor when called.

XXX: This means that everything passed to an Actor has to be able to be reasonably
printed out; for instance, if you give it a Batch, well you're out of luck if that
gets printed out into a level spec file and then re-loaded.
"""
    clsinit = cls.__init__
    def newinit(self, *args, **kwargs):
        self.__args = (args, kwargs)
        return clsinit(self, *args, **kwargs)

    def describe(self):
        def describeFunc():
            args, kwargs = self.__args
            return cls(*args, **kwargs)
        return describeFunc

    def describeString(self):
        name = self.__class__.__name__
        args, kwargs = self.__args
        # Positional arguments must naturally come before keyword arguments.
        argReprs = [repr(arg) for arg in args] + ["{}={}".format(ky, repr(vl)) for ky,vl in kwargs.iteritems()]
        return "(lambda: {}({}))".format(name, ", ".join(argReprs))
    
    cls.__init__ = newinit
    cls.describe = describe
    cls.describeString = describeString

    return cls

class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s):
        s.physicsObj = None
        s.renderer = None

        s.alive = True

        s.motionX = 0
        s.braking = False
        s.world = None
        s.life = None
        s.facing = FACING_RIGHT
        s.onGround = False
        s.bulletOffset = (0,0)

    def onDeath(s):
        pass

    def update(s, dt):
        pass

    def fireBullet(s, bulletClass, facing=None):
        if facing is None:
            facing = s.facing
        posx, posy = s.physicsObj.position
        offsetx, offsety = s.bulletOffset
        bulletpos = (posx + (offsetx * facing), posy + offsety)
        
        bullet = bulletClass(s, bulletpos, facing)
        s.world.addActor(bullet)

    def fireBulletAt(s, bulletClass, position, initialImpulse, facing=None, gravity=False):
        """Fires a bullet in a specific direction.  Position is an offset from the actor
doing the firing."""
        if facing is None:
            facing = s.facing
        posx, posy = s.physicsObj.position
        offsetx, offsety = position
        bulletpos = (posx + (offsetx * facing), posy + offsety)
        
        bullet = bulletClass(s, bulletpos, facing)
        bullet.physicsObj.body.reset_forces()
        if not gravity:
            bullet.physicsObj.negateGravity()
        bullet.physicsObj.apply_impulse(initialImpulse)
            
        s.world.addActor(bullet)


# XXX: This being an actor little weird, but hey, it ties fairly nicely into the
# update and rendering systems.
class GUI(Actor):
    def __init__(s, player):
        Actor.__init__(s)
        s.renderer = rcache.getRenderer(GUIRenderer)
        s.player = player
        # Awwwwwkward...
        s.physicsObj = PhysicsObj(s)

    def update(s, dt):
        pass
    
class Indicator(Actor):
    """This is an Actor that just sticks to a given actor and
moves around with it.  Initially for the level editor, but it
might eventually be useful for a crosshairs or something."""
    def __init__(s, target):
        s.renderer = rcache.getRenderer(IndicatorRenderer)
        s.target = target
        s.physicsObj = PhysicsObj(s)
        s.facing = FACING_LEFT

    def update(s, dt):
        s.physicsObj.position = s.target.position
        print 'indicator exists'
    
# No description; the keyboard object can't be readily printed.
class Player(Actor):
    """The player object."""
    def __init__(s, keyboard, position=(0,0)):
        print s, keyboard
        s.radius = 10
        Actor.__init__(s)
        s.controller = KeyboardController(s, keyboard)
        s.physicsObj = PlayerPhysicsObj(s, position=position)

        s.renderer = rcache.getRenderer(PlayerRenderer)
        #s.renderer = rcache.getRenderer(SpriteRenderer)
        s.bulletOffset = (s.radius,0)

        s.powers = PowerSet(s)
        s.facing = FACING_RIGHT
        s.glow = 0.0

        s.gate = None

        s.life = Life(s, 100)
        s.energy = Energy(s)
    def update(s, dt):
        s.controller.update(dt)
        s.powers.update(dt)
        s.glow += 0.05
        s.energy.update(dt)

    def onDeath(s):
        print "oh noez, player died!"

@described
class Collectable(Actor):
    """Something you can collect which does things to you,
whether restoring your health or unlocking a new Power or whatever."""

    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.physicsObj = CollectablePhysicsObj(s, position=position)
        s.life = TimedLife(s, 15)
        s.renderer = rcache.getRenderer(CollectableRenderer)

    def collect(s, player):
        print "Collected collectable!"

    def update(s, dt):
        s.life.update(dt)

@described
class BeginningsPowerup(Actor):
    "Powerups don't time out and don't move."
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.physicsObj = PowerupPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(PowerupRenderer)
        
    def collect(s, player):
        print "Gained Beginnings power!"
        player.powers.addPower(BeginningsPower(player))

@described
class AirPowerup(Actor):
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.physicsObj = PowerupPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(PowerupRenderer)
        
    def collect(s, player):
        print "Gained Air power!"
        player.powers.addPower(AirPower(player))


@described
class CrawlerEnemy(Actor):
    """An enemy that crawls along the ground and hurts when you touch it.

BUGGO: Doesn't currently hurt when you touch it; blocked on better collision
handling I think."""
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.controller = RoamAIController(s)
        s.physicsObj = CrawlerPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(CrawlerRenderer)
        
        s.facing = FACING_RIGHT
        s.life = Life(s, 3, reduction=8)

    def update(s, dt):
        s.controller.update(dt)

    def onDeath(s):
        c = Collectable()
        c.physicsObj.position = s.physicsObj.position
        yForce = 350
        xForce = (random.random() * 150) - 75
        c.physicsObj.apply_impulse((xForce, yForce))
        s.world.addActor(c)

@described
class TrooperEnemy(Actor):
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.controller = TrooperAIController(s)
        s.physicsObj = TrooperPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(TrooperRenderer)
        s.bulletOffset = (30,0)
        s.facing = FACING_LEFT
        s.life = Life(s, 100)

    def update(s, dt):
        s.controller.update(dt)

@described
class ArcherEnemy(Actor):
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.controller = ArcherAIController(s)
        s.physicsObj = ArcherPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(ArcherRenderer)
        
        s.facing = FACING_RIGHT
        s.life = Life(s, 20)
        s.bulletOffset = (25, 0)

    def update(s, dt):
        s.controller.update(dt)

@described
class FloaterEnemy(Actor):
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.controller = FloaterAIController(s)
        s.physicsObj = FloaterPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(FloaterRenderer)
        
        s.facing = FACING_RIGHT
        s.life = Life(s, 30)

    def update(s, dt):
        s.controller.update(dt)

@described
class EliteEnemy(Actor):
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.controller = RoamAIController(s)
        s.physicsObj = ElitePhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(EliteRenderer)
        
        s.facing = FACING_RIGHT
        s.life = Life(s, 10)

    def update(s, dt):
        s.controller.update(dt)

@described
class HeavyEnemy(Actor):
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.controller = RoamAIController(s)
        s.physicsObj = HeavyPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(HeavyRenderer)
        
        s.facing = FACING_RIGHT
        s.life = Life(s, 10)

    def update(s, dt):
        s.controller.update(dt)

@described
class DragonEnemy(Actor):
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.controller = RoamAIController(s)
        s.physicsObj = DragonPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(DragonRenderer)
        
        s.facing = FACING_RIGHT
        s.life = Life(s, 10)

    def update(s, dt):
        s.controller.update(dt)

@described
class AnnihilatorEnemy(Actor):
    def __init__(s, position=(0,0)):
        Actor.__init__(s)
        s.controller = RoamAIController(s)
        s.physicsObj = AnnihilatorPhysicsObj(s, position=position)
        s.renderer = rcache.getRenderer(AnnihilatorRenderer)
        
        s.facing = FACING_RIGHT
        s.life = Life(s, 10)

    def update(s, dt):
        s.controller.update(dt)

class TrooperBullet(Actor):
    def __init__(s, firer, position, facing):
        Actor.__init__(s)
        s.firer = firer
        s.facing = facing

        s.physicsObj = EnemyBulletPhysicsObj(s, position=position)
        s.physicsObj.negateGravity()
        s.renderer = rcache.getRenderer(TrooperBulletRenderer)
        s.life = TimedLife(s, 1.0)

        xImpulse = 300 * facing
        yImpulse = 0
        s.physicsObj.apply_impulse((xImpulse, yImpulse))

        s.damage = 6
        s.rotateSpeed = 10

    def update(s, dt):
        s.life.update(dt)
        s.physicsObj.angle += dt * s.rotateSpeed
                

class FloaterBullet(Actor):
    def __init__(s, firer, position, facing):
        Actor.__init__(s)
        s.firer = firer
        s.facing = facing

        s.physicsObj = EnemyBulletPhysicsObj(s, position=position)
        s.physicsObj.negateGravity()
        s.renderer = rcache.getRenderer(TrooperBulletRenderer)
        s.life = TimedLife(s, 0.3)

        s.damage = 3

    def update(s, dt):
        s.life.update(dt)

        
# TODO: Bullet class?
# It's a bit hard to make one that's generic.
# Bullets probably SHOULD have controllers...

# TODO: WE MIGHT NEED SOME PROPER EVENTS TO OCCUR FOR ACTORS
# Hit ground, leave ground
# Hit terrain, leave contact with terrain
# Hit with attack, touch enemy (useful for bullets)
# Actually just general collision 
# Take damage, as well.
# onDeath is already the start of this.
# Other stuff maybe.  Hmmmm.
# We could even consider the more fundamental methods to be
# onDraw and onUpdate, really

class BeginningP1Bullet(Actor):
    def __init__(s, firer, position, facing, impulse=None, lifetime=None):
        Actor.__init__(s)
        s.firer = firer
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)

        x,y = position
        s.physicsObj = BeginningsBulletPhysicsObj(s, position=(x, y+yOffset))
        
        s.renderer = rcache.getRenderer(BeginningsP1BulletRenderer)

        if impulse == None:
            xImpulse = 400 * facing
            yImpulse = yOffset * 10
            s.physicsObj.apply_impulse((xImpulse, yImpulse))
        else:
            s.physicsObj.apply_impulse(impulse)
        # Counteract gravity?
        # BUGGO: Gravity might vary...
        s.physicsObj.negateGravity()
        s.life = TimedLife(s, 0.4 + (random.random() / 3.0))

        s.facing = facing
        s.damage = 1

    def update(s, dt):
        s.life.update(dt)

    def onDeath(s):
        #print 'bullet died'
        pass

class BeginningP2Bullet(Actor):
    def __init__(s, firer, position, direction):
        Actor.__init__(s)
        s.firer = firer
        x,y = position
        s.physicsObj = BeginningsBulletPhysicsObj(s, position=(x, y))
        # TODO: Placeholder image
        s.renderer = rcache.getRenderer(PowerupRenderer)
        xImpulse = 300 * direction
        yImpulse = 200
        s.physicsObj.apply_impulse((xImpulse, yImpulse))
        
        s.damage = 10

    def update(s, dt):
        pass

    def collideWithEnemy(s, enemy):
        enemy.takeDamage(s.damage)

    def onDeath(s):
        for angle in xrange(0, 360, 30):
            rangle = math.radians(angle)
            force = 1000
            xForce = math.cos(rangle) * force
            yForce = math.sin(rangle) * force
            x,y = s.physicsObj.position
            # We start the fragments a bit back from the
            # current bullet position, so they don't immediately
            # hit whatever it hit
            vx, vy = s.physicsObj.velocity
            newx = x - (vx / 20.0)
            newy = y - (vy / 20.0)
            # TODO: Placeholder bullet
            b = BeginningP1Bullet(s.firer, (newx, newy), FACING_RIGHT, impulse=(xForce, yForce))
            b.life = TimedLife(b, 0.15)
            b.physicsObj.body.angle = rangle
            s.world.addActor(b)
        rcache.get_sound("Powers_Begining_Grenade_Hit").play()

class AirP1BulletAir(Actor):
    def __init__(s, firer, position, direction):
        Actor.__init__(s)
        s.firer = firer
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)
        x,y = position
        s.physicsObj = AirP1PhysicsObjAir(s, position=(x, y+yOffset))

        s.renderer = rcache.getRenderer(AirP1BulletAirRenderer)

        xImpulse = 600 * direction
        yImpulse = yOffset * 10
        s.physicsObj.apply_impulse((xImpulse, yImpulse))
        s.physicsObj.negateGravity()
        s.maxTime = 0.45
        s.life = TimedLife(s, s.maxTime)

        s.facing = direction
        s.damage = 3

    def update(s, dt):
        s.life.update(dt)

    def onDeath(s):
        #print 'bullet died'
        pass

class AirP1BulletGround(Actor):
    def __init__(s, firer, position, direction):
        Actor.__init__(s)
        s.firer = firer
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)
        x,y = position
        s.physicsObj = AirP1PhysicsObjGround(s, position=(x, y+yOffset))

        s.renderer = rcache.getRenderer(AirP1BulletGroundRenderer)
        
        xImpulse = 800 * direction
        yImpulse = yOffset * 10
        s.physicsObj.apply_impulse((xImpulse, yImpulse))
        s.physicsObj.negateGravity()
        s.maxTime = 0.12
        s.life = TimedLife(s, s.maxTime)
        s.facing = direction
        s.damage = 2

    def update(s, dt):
        s.life.update(dt)

    def onDeath(s):
        #print 'bullet died'
        pass

# BUGGO This needs fixing
# We don't want to create a new bullet each frame I guess
# So we need to move this along so it follows its parent
# But actually doing damage is also kinda fucked up, then.
# Bligher.


class AirP2Bullet(Actor):
    def __init__(s, firer, position, direction):
        Actor.__init__(s)
        s.firer = firer
        # This has to be set before the physicsobj is created
        s.facing = direction
        x,y = position

        s.physicsObj = AirP2PhysicsObj(s, position=(x, y))
        
        s.maxTime = 0.6
        s.life = TimedLife(s, s.maxTime)
        s.animationTimer = Timer(0.03)
        
        #s.renderer = rcache.getRenderer(BBRenderer)
        s.renderer = rcache.getRenderer(AirP2BulletRenderer)

        # We start at a random place in the animation.
        # XXX: This should probably be a matter for the renderer, not the actor
        s.animationCount = random.randint(0, 100)

        s.damagePerSecond = 100
        s.enemiesTouching = set()


    def update(s, dt):
        s.life.update(dt)
        s.animationTimer.update(dt)
        if s.animationTimer.expired():
            s.animationCount += 1

        for e in s.enemiesTouching:
            e.life.takeDamage(s, s.damagePerSecond * dt)


class NullPower(object):
    "A power set that does nothing."
    def __init__(s, owner):
        s.owner = owner

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

    def startAttack1(s):
        pass

    def startAttack2(s):
        pass

    def startDefend(s):
        pass

    def startJump(s):
        pass


    def stopAttack1(s):
        pass

    def stopAttack2(s):
        pass

    def stopDefend(s):
        pass

    def stopJump(s):
        pass

    def draw(s, shader):
        pass


# BUGGO:
# As the code stands, with Beginnings and Air powers both
# attacks can be used pretty much at the same time.  Do
# we want it to be this way?  Probably not!
class BeginningsPower(NullPower):
    """The Beginnings elemental power set."""
    def __init__(s, owner):
        NullPower.__init__(s, owner)

        s.usingAttack1 = False
        s.attack1Refire = Timer(defaultTime = 0.05)
        
        s.attack2Refire = Timer(defaultTime = 1.5)
        
        s.jumpTimer = Timer(defaultTime = 0.20)
        s.jumping = False

        s.defending = False
        s.shieldImage = rcache.getLineImage(images.shieldImage)

        s.attack1Cost = 1.0
        s.attack2Cost = 10.0
        s.defendCost = 25.0
        s.jumpCost = 0.0

    # XXX: This is a little awkward.
    def draw(s, shader):
        if s.defending:
            s.shieldImage.batch.draw()

    def update(s, dt):
        s.attack1Refire.update(dt)
        s.attack2Refire.update(dt)
        s.jumpTimer.update(dt)
        if s.defending:
            if s.owner.energy.expend(s.defendCost*dt):
                s.owner.life.damageAttenuation = 0.25
            else:
                s.defending=False
                s.owner.life.damageAttenuation = 1.0
        if s.jumping and not s.jumpTimer.expired():
            s.owner.physicsObj.apply_impulse((0, 2000 * dt))

        if s.usingAttack1 and s.attack1Refire.expired():
            # BUGGO: Make the number of bullets fired
            # correct even at low framerates
            if s.owner.energy.expend(s.attack1Cost):
                s.owner.fireBullet(BeginningP1Bullet)
                rcache.get_sound("Powers_Begining_Gun").play()
                s.attack1Refire.reset()

    # BUGGO: It's concievable we'd have to fire multiple shots in the same frame...
    # If we lag real bad at least.
    # But since that'd currently involve going 20 FPS...
    def startAttack1(s):
        print 'firing'
        s.usingAttack1 = True

    def stopAttack1(s):
        s.usingAttack1 = False
        
    def attack2(s):
        if s.attack2Refire.expired():
            if s.owner.energy.expend(s.attack2Cost):
                s.attack2Refire.reset()
                s.owner.fireBullet(BeginningP2Bullet)
                rcache.get_sound("Powers_Begining_Grenade_Launch").play()

    def startDefend(s):
        #print "Starting defend"
        s.defending = True
        

    def stopDefend(s):
        #print "Stopping defend"
        s.defending = False
        s.owner.life.damageAttenuation = 1.0

    def jump(s):
        pass

    def startJump(s):
        if s.owner.onGround:
            s.owner.physicsObj.apply_impulse((0, 100))
            s.owner.onGround = False
            s.jumpTimer.reset()
            s.jumping = True

    def stopJump(s):
        s.jumping = False


class AirPower(NullPower):
    """The Air elemental power set."""
    def __init__(s, owner):
        NullPower.__init__(s, owner)
        s.timer = 0.0

        s.attack1Timer = Timer(defaultTime = 0.1)
        s.attack2Timer = Timer(defaultTime = 0.8)
        s.attack2FireTimer = Timer(defaultTime = 0.3)
        s.attack2Charging = False
        
        s.jumping = False

        s.defending = False
        s.defenseAngularVel = 0.0
        s.defenseVelLimit = 0.0
        s.defenseTimer = Timer(defaultTime = 0.3)
        s.defenseCooldownTimer = Timer(defaultTime = 1.25)

        s.attack1Cost = 5.0
        s.attack2Cost = 40.0
        s.defendCost = 5.0
        s.jumpCost = 0.0
        
    def startDefend(s):
        if s.defenseCooldownTimer.expired():
            if s.owner.energy.expend(s.defendCost):
                s.defending = True
                s.defenseCooldownTimer.reset()
                s.defenseTimer.reset()
                s.defenseAngularVel = s.owner.physicsObj.angular_velocity
                s.defenseVelLimit = s.owner.physicsObj.velocity_limit
                s.owner.physicsObj.velocity_limit = 1000.0

    def update(s, dt):
        s.attack1Timer.update(dt)
        s.attack2Timer.update(dt)
        s.defenseCooldownTimer.update(dt)
        if s.jumping:
            s.owner.physicsObj.apply_impulse((0, 300*dt))
        if s.defending:
            facing = s.owner.facing
            #s.owner.physicsObj.apply_impulse((-facing * 3000 * dt, 0))
            s.owner.physicsObj.velocity = (-facing * 500, 0)
            s.defenseTimer.update(dt)
            if s.defenseTimer.expired():
                s.defending = False
                s.owner.physicsObj.velocity = (0,0)
                s.owner.physicsObj.angular_velocity = s.defenseAngularVel
                s.owner.physicsObj.velocity_limit = s.defenseVelLimit

        if s.attack2Charging and s.attack2Timer.expired():
            if s.owner.energy.expend(s.attack2Cost):
                s.owner.fireBullet(AirP2Bullet)
                rcache.get_sound("Powers_Air_Lit").play()
            s.attack2Charging = False
        
    def startJump(s):
        if s.owner.onGround:
            if s.owner.energy.expend(s.jumpCost):
                s.owner.physicsObj.apply_impulse((0, 300))
        # With the Air jump we can float down slowly even
        # if we didn't start off jumping
        s.jumping = True

    def stopJump(s):
        s.jumping = False

    def startAttack1(s):
        if s.attack1Timer.expired():
            s.attack1Timer.reset()
            if s.owner.energy.expend(s.attack1Cost)==True:
                if s.owner.onGround:
                    s.owner.fireBullet(AirP1BulletGround)
                    rcache.get_sound("Powers_Air_Wave_Small").play()
                else:
                    s.owner.fireBullet(AirP1BulletAir)
                    rcache.get_sound("Powers_Air_Wave_Large").play()

    def startAttack2(s):
        s.attack2Timer.reset()
        s.attack2Charging = True

    def stopAttack2(s):
        s.attack2Charging = False


class Combo(object):
    """Just some thoughts on how to implement combos.

Easily improvable by having a list (or perhaps tree)
of ComboMoves, each of which has an attack and a max time,
or maybe a max and min time interval during which you
have to hit the button.

Linear combos (3-attack combo mashing the same button)
are very easy.

Combinations of moves in the same Power set are also
easy (attack1 -> attack1 -> attack2), we just have
an attack method for each attack and they chain off
each other.  We still have all the state we need.

Complex combinations such as a Dragon Punch or a
combo that spreads across Powers would require
a fair bit of reworking of how input events flow, but
we should be able to do it if we really want to.

An easier way to make such things happen might perhaps
be to have certain combos set a state on the player, so,
a Water combo might result in the player surrounded by
hovering ice spears for half a second.  If you then
switched to Fire power and used one of its attacks,
it could easily detect whether the hovering ice spears
were there or not, and if so do a different attack
(say turning them into an omnidirectional blast of steam).

I don't really want to go that far down the rabbit hole
for this game, though."""
    def __init__(s):
        s.sequence = 0
        s.time = 0.0
        s.maxSequence = 3
        s.maxTime = 0.2

    def update(s, dt):
        s.time -= dt

    def attack(s):
        if s.sequence == 0:
            s.attack1()
            s.time = s.maxTime
            s.sequence += 1
        elif s.sequence == 1 and s.time > 0.0:
            s.attack2()
            s.time = s.maxTime
            s.sequence += 1
        elif s.sequence == 2 and s.time > 0.0:
            s.attack3()
            #s.time = s.maxTime
            s.sequence = 0
            

            
class PowerSet(Component):
    def __init__(s, owner):
        Component.__init__(s, owner)
        s.powerIndex = 0
        s.powers = [NullPower(owner)]
        s.powers = [AirPower(owner),BeginningsPower(owner)]
        s.currentPower = s.powers[s.powerIndex]

    def addPower(s, power):
        # Remove the null power if it exists before adding
        # the other power
        # Can't use isinstance() here 'cause power all inherit
        # from NullPower, so.
        if len(s.powers) == 1 and s.powers[0].__class__ == NullPower:
            s.powers = [power]
        elif power not in s.powers:
            s.powers.append(power)
            s.powers.sort()
        s.currentPower = power
        s.powerIndex = s.powers.index(power)
        print "Added power:", power
        print s.powers
        
    def update(s, dt):
        s.currentPower.update(dt)

    def attack1(s):
        s.currentPower.attack1()

    def attack2(s):
        s.currentPower.attack2()

    def defend(s):
        s.currentPower.defend()

    def jump(s):
        s.currentPower.jump()

    def startAttack1(s):
        s.currentPower.startAttack1()

    def startAttack2(s):
        s.currentPower.startAttack2()

    def startDefend(s):
        s.currentPower.startDefend()

    def startJump(s):
        s.currentPower.startJump()

    def stopAttack1(s):
        s.currentPower.stopAttack1()

    def stopAttack2(s):
        s.currentPower.stopAttack2()

    def stopDefend(s):
        s.currentPower.stopDefend()

    def stopJump(s):
        s.currentPower.stopJump()

    def draw(s, shader):
        s.currentPower.draw(shader)

    def nextPower(s):
        s.powerIndex = (s.powerIndex + 1) % len(s.powers)
        s.currentPower = s.powers[s.powerIndex]

    def prevPower(s):
        s.powerIndex = (s.powerIndex - 1) % len(s.powers)
        s.currentPower = s.powers[s.powerIndex]
