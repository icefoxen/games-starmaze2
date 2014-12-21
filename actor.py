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
    def __init__(self):
        self.physicsObj = None
        self.renderer = None

        self.alive = True

        self.motionX = 0
        self.braking = False
        self.world = None
        self.life = None
        self.facing = FACING_RIGHT
        self.onGround = False
        self.bulletOffset = (0,0)

    def onDeath(self):
        pass

    def update(self, dt):
        pass

    def fireBullet(self, bulletClass, facing=None):
        if facing is None:
            facing = self.facing
        posx, posy = self.physicsObj.position
        offsetx, offsety = self.bulletOffset
        bulletpos = (posx + (offsetx * facing), posy + offsety)
        
        bullet = bulletClass(self, bulletpos, facing)
        self.world.addActor(bullet)

    def fireBulletAt(self, bulletClass, position, initialImpulse, facing=None, gravity=False):
        """Fires a bullet in a specific direction.  Position is an offset from the actor
doing the firing."""
        if facing is None:
            facing = self.facing
        posx, posy = self.physicsObj.position
        offsetx, offsety = position
        bulletpos = (posx + (offsetx * facing), posy + offsety)
        
        bullet = bulletClass(self, bulletpos, facing)
        bullet.physicsObj.body.reset_forces()
        if not gravity:
            bullet.physicsObj.negateGravity()
        bullet.physicsObj.apply_impulse(initialImpulse)
            
        self.world.addActor(bullet)


# XXX: This being an actor little weird, but hey, it ties fairly nicely into the
# update and rendering systems.
class GUI(Actor):
    def __init__(self, player):
        Actor.__init__(self)
        self.renderer = rcache.getRenderer(GUIRenderer)
        self.player = player
        # Awwwwwkward...
        self.physicsObj = PhysicsObj(self)

    def update(self, dt):
        pass
    
class Indicator(Actor):
    """This is an Actor that just sticks to a given actor and
moves around with it.  Initially for the level editor, but it
might eventually be useful for a crosshairs or something."""
    def __init__(self, target):
        self.renderer = rcache.getRenderer(IndicatorRenderer)
        self.target = target
        self.physicsObj = PhysicsObj(self)
        self.facing = FACING_LEFT

    def update(self, dt):
        self.physicsObj.position = self.target.position
        print 'indicator exists'
    
# No description; the keyboard object can't be readily printed.
class Player(Actor):
    """The player object."""
    def __init__(self, keyboard, position=(0,0)):
        print self, keyboard
        self.radius = 10
        Actor.__init__(self)
        self.controller = KeyboardController(self, keyboard)
        self.physicsObj = PlayerPhysicsObj(self, position=position)

        self.renderer = rcache.getRenderer(PlayerRenderer)
        #self.renderer = rcache.getRenderer(SpriteRenderer)
        self.bulletOffset = (self.radius,0)

        self.powers = PowerSet(self)
        self.facing = FACING_RIGHT
        self.glow = 0.0

        self.gate = None

        self.life = Life(self, 100)
        self.energy = Energy(self)
    def update(self, dt):
        self.controller.update(dt)
        self.powers.update(dt)
        self.glow += 0.05
        self.energy.update(dt)

    def onDeath(self):
        print "oh noez, player died!"

@described
class Collectable(Actor):
    """Something you can collect which does things to you,
whether restoring your health or unlocking a new Power or whatever."""

    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.physicsObj = CollectablePhysicsObj(self, position=position)
        self.life = TimedLife(self, 15)
        self.renderer = rcache.getRenderer(CollectableRenderer)

    def collect(self, player):
        print "Collected collectable!"

    def update(self, dt):
        self.life.update(dt)

@described
class BeginningsPowerup(Actor):
    "Powerups don't time out and don't move."
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.physicsObj = PowerupPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(PowerupRenderer)
        
    def collect(self, player):
        print "Gained Beginnings power!"
        player.powers.addPower(BeginningsPower(player))

@described
class AirPowerup(Actor):
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.physicsObj = PowerupPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(PowerupRenderer)
        
    def collect(self, player):
        print "Gained Air power!"
        player.powers.addPower(AirPower(player))


@described
class CrawlerEnemy(Actor):
    """An enemy that crawls along the ground and hurts when you touch it.

BUGGO: Doesn't currently hurt when you touch it; blocked on better collision
handling I think."""
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.controller = RoamAIController(self)
        self.physicsObj = CrawlerPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(CrawlerRenderer)
        
        self.facing = FACING_RIGHT
        self.life = Life(self, 3, reduction=8)

    def update(self, dt):
        self.controller.update(dt)

    def onDeath(self):
        c = Collectable()
        c.physicsObj.position = self.physicsObj.position
        yForce = 350
        xForce = (random.random() * 150) - 75
        c.physicsObj.apply_impulse((xForce, yForce))
        self.world.addActor(c)

@described
class TrooperEnemy(Actor):
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.controller = TrooperAIController(self)
        self.physicsObj = TrooperPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(TrooperRenderer)
        self.bulletOffset = (30,0)
        self.facing = FACING_LEFT
        self.life = Life(self, 100)

    def update(self, dt):
        self.controller.update(dt)

@described
class ArcherEnemy(Actor):
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.controller = ArcherAIController(self)
        self.physicsObj = ArcherPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(ArcherRenderer)
        
        self.facing = FACING_RIGHT
        self.life = Life(self, 20)
        self.bulletOffset = (25, 0)

    def update(self, dt):
        self.controller.update(dt)

@described
class FloaterEnemy(Actor):
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.controller = FloaterAIController(self)
        self.physicsObj = FloaterPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(FloaterRenderer)
        
        self.facing = FACING_RIGHT
        self.life = Life(self, 30)

    def update(self, dt):
        self.controller.update(dt)

@described
class EliteEnemy(Actor):
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.controller = RoamAIController(self)
        self.physicsObj = ElitePhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(EliteRenderer)
        
        self.facing = FACING_RIGHT
        self.life = Life(self, 10)

    def update(self, dt):
        self.controller.update(dt)

@described
class HeavyEnemy(Actor):
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.controller = RoamAIController(self)
        self.physicsObj = HeavyPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(HeavyRenderer)
        
        self.facing = FACING_RIGHT
        self.life = Life(self, 10)

    def update(self, dt):
        self.controller.update(dt)

@described
class DragonEnemy(Actor):
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.controller = RoamAIController(self)
        self.physicsObj = DragonPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(DragonRenderer)
        
        self.facing = FACING_RIGHT
        self.life = Life(self, 10)

    def update(self, dt):
        self.controller.update(dt)

@described
class AnnihilatorEnemy(Actor):
    def __init__(self, position=(0,0)):
        Actor.__init__(self)
        self.controller = RoamAIController(self)
        self.physicsObj = AnnihilatorPhysicsObj(self, position=position)
        self.renderer = rcache.getRenderer(AnnihilatorRenderer)
        
        self.facing = FACING_RIGHT
        self.life = Life(self, 10)

    def update(self, dt):
        self.controller.update(dt)

class TrooperBullet(Actor):
    def __init__(self, firer, position, facing):
        Actor.__init__(self)
        self.firer = firer
        self.facing = facing

        self.physicsObj = EnemyBulletPhysicsObj(self, position=position)
        self.physicsObj.negateGravity()
        self.renderer = rcache.getRenderer(TrooperBulletRenderer)
        self.life = TimedLife(self, 1.0)

        xImpulse = 300 * facing
        yImpulse = 0
        self.physicsObj.apply_impulse((xImpulse, yImpulse))

        self.damage = 6
        self.rotateSpeed = 10

    def update(self, dt):
        self.life.update(dt)
        self.physicsObj.angle += dt * self.rotateSpeed
                

class FloaterBullet(Actor):
    def __init__(self, firer, position, facing):
        Actor.__init__(self)
        self.firer = firer
        self.facing = facing

        self.physicsObj = EnemyBulletPhysicsObj(self, position=position)
        self.physicsObj.negateGravity()
        self.renderer = rcache.getRenderer(TrooperBulletRenderer)
        self.life = TimedLife(self, 0.3)

        self.damage = 3

    def update(self, dt):
        self.life.update(dt)

        
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
    def __init__(self, firer, position, facing, impulse=None, lifetime=None):
        Actor.__init__(self)
        self.firer = firer
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)

        x,y = position
        self.physicsObj = BeginningsBulletPhysicsObj(self, position=(x, y+yOffset))
        
        self.renderer = rcache.getRenderer(BeginningsP1BulletRenderer)

        if impulse == None:
            xImpulse = 400 * facing
            yImpulse = yOffset * 10
            self.physicsObj.apply_impulse((xImpulse, yImpulse))
        else:
            self.physicsObj.apply_impulse(impulse)
        # Counteract gravity?
        # BUGGO: Gravity might vary...
        self.physicsObj.negateGravity()
        self.life = TimedLife(self, 0.4 + (random.random() / 3.0))

        self.facing = facing
        self.damage = 1

    def update(self, dt):
        self.life.update(dt)

    def onDeath(self):
        #print 'bullet died'
        pass

class BeginningP2Bullet(Actor):
    def __init__(self, firer, position, direction):
        Actor.__init__(self)
        self.firer = firer
        x,y = position
        self.physicsObj = BeginningsBulletPhysicsObj(self, position=(x, y))
        # TODO: Placeholder image
        self.renderer = rcache.getRenderer(PowerupRenderer)
        xImpulse = 300 * direction
        yImpulse = 200
        self.physicsObj.apply_impulse((xImpulse, yImpulse))
        
        self.damage = 10

    def update(self, dt):
        pass

    def collideWithEnemy(self, enemy):
        enemy.takeDamage(self.damage)

    def onDeath(self):
        for angle in xrange(0, 360, 30):
            rangle = math.radians(angle)
            force = 1000
            xForce = math.cos(rangle) * force
            yForce = math.sin(rangle) * force
            x,y = self.physicsObj.position
            # We start the fragments a bit back from the
            # current bullet position, so they don't immediately
            # hit whatever it hit
            vx, vy = self.physicsObj.velocity
            newx = x - (vx / 20.0)
            newy = y - (vy / 20.0)
            # TODO: Placeholder bullet
            b = BeginningP1Bullet(self.firer, (newx, newy), FACING_RIGHT, impulse=(xForce, yForce))
            b.life = TimedLife(b, 0.15)
            b.physicsObj.body.angle = rangle
            self.world.addActor(b)
        rcache.get_sound("Powers_Begining_Grenade_Hit").play()

class AirP1BulletAir(Actor):
    def __init__(self, firer, position, direction):
        Actor.__init__(self)
        self.firer = firer
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)
        x,y = position
        self.physicsObj = AirP1PhysicsObjAir(self, position=(x, y+yOffset))

        self.renderer = rcache.getRenderer(AirP1BulletAirRenderer)

        xImpulse = 600 * direction
        yImpulse = yOffset * 10
        self.physicsObj.apply_impulse((xImpulse, yImpulse))
        self.physicsObj.negateGravity()
        self.maxTime = 0.45
        self.life = TimedLife(self, self.maxTime)

        self.facing = direction
        self.damage = 3

    def update(self, dt):
        self.life.update(dt)

    def onDeath(self):
        #print 'bullet died'
        pass

class AirP1BulletGround(Actor):
    def __init__(self, firer, position, direction):
        Actor.__init__(self)
        self.firer = firer
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)
        x,y = position
        self.physicsObj = AirP1PhysicsObjGround(self, position=(x, y+yOffset))

        self.renderer = rcache.getRenderer(AirP1BulletGroundRenderer)
        
        xImpulse = 800 * direction
        yImpulse = yOffset * 10
        self.physicsObj.apply_impulse((xImpulse, yImpulse))
        self.physicsObj.negateGravity()
        self.maxTime = 0.12
        self.life = TimedLife(self, self.maxTime)
        self.facing = direction
        self.damage = 2

    def update(self, dt):
        self.life.update(dt)

    def onDeath(self):
        #print 'bullet died'
        pass

# BUGGO This needs fixing
# We don't want to create a new bullet each frame I guess
# So we need to move this along so it follows its parent
# But actually doing damage is also kinda fucked up, then.
# Bligher.


class AirP2Bullet(Actor):
    def __init__(self, firer, position, direction):
        Actor.__init__(self)
        self.firer = firer
        # This has to be set before the physicsobj is created
        self.facing = direction
        x,y = position

        self.physicsObj = AirP2PhysicsObj(self, position=(x, y))
        
        self.maxTime = 0.6
        self.life = TimedLife(self, self.maxTime)
        self.animationTimer = Timer(0.03)
        
        #self.renderer = rcache.getRenderer(BBRenderer)
        self.renderer = rcache.getRenderer(AirP2BulletRenderer)

        # We start at a random place in the animation.
        # XXX: This should probably be a matter for the renderer, not the actor
        self.animationCount = random.randint(0, 100)

        self.damagePerSecond = 100
        self.enemiesTouching = set()


    def update(self, dt):
        self.life.update(dt)
        self.animationTimer.update(dt)
        if self.animationTimer.expired():
            self.animationCount += 1

        for e in self.enemiesTouching:
            e.life.takeDamage(self, self.damagePerSecond * dt)


class NullPower(object):
    "A power set that does nothing."
    def __init__(self, owner):
        self.owner = owner

    def __eq__(self, other):
        return self.__class__ == other.__class__

    def update(self, dt):
        pass

    def attack1(self):
        pass

    def attack2(self):
        pass

    def defend(self):
        pass

    def jump(self):
        pass

    def startAttack1(self):
        pass

    def startAttack2(self):
        pass

    def startDefend(self):
        pass

    def startJump(self):
        pass


    def stopAttack1(self):
        pass

    def stopAttack2(self):
        pass

    def stopDefend(self):
        pass

    def stopJump(self):
        pass

    def draw(self, shader):
        pass


# BUGGO:
# As the code stands, with Beginnings and Air powers both
# attacks can be used pretty much at the same time.  Do
# we want it to be this way?  Probably not!
class BeginningsPower(NullPower):
    """The Beginnings elemental power set."""
    def __init__(self, owner):
        NullPower.__init__(self, owner)

        self.usingAttack1 = False
        self.attack1Refire = Timer(defaultTime = 0.05)
        
        self.attack2Refire = Timer(defaultTime = 1.5)
        
        self.jumpTimer = Timer(defaultTime = 0.20)
        self.jumping = False

        self.defending = False
        self.shieldImage = rcache.getLineImage(images.shieldImage)

        self.attack1Cost = 1.0
        self.attack2Cost = 10.0
        self.defendCost = 25.0
        self.jumpCost = 0.0

    # XXX: This is a little awkward.
    def draw(self, shader):
        if self.defending:
            self.shieldImage.batch.draw()

    def update(self, dt):
        self.attack1Refire.update(dt)
        self.attack2Refire.update(dt)
        self.jumpTimer.update(dt)
        if self.defending:
            if self.owner.energy.expend(self.defendCost*dt):
                self.owner.life.damageAttenuation = 0.25
            else:
                self.defending=False
                self.owner.life.damageAttenuation = 1.0
        if self.jumping and not self.jumpTimer.expired():
            self.owner.physicsObj.apply_impulse((0, 2000 * dt))

        if self.usingAttack1 and self.attack1Refire.expired():
            # BUGGO: Make the number of bullets fired
            # correct even at low framerates
            if self.owner.energy.expend(self.attack1Cost):
                self.owner.fireBullet(BeginningP1Bullet)
                rcache.get_sound("Powers_Begining_Gun").play()
                self.attack1Refire.reset()

    # BUGGO: It's concievable we'd have to fire multiple shots in the same frame...
    # If we lag real bad at least.
    # But since that'd currently involve going 20 FPself...
    def startAttack1(self):
        print 'firing'
        self.usingAttack1 = True

    def stopAttack1(self):
        self.usingAttack1 = False
        
    def attack2(self):
        if self.attack2Refire.expired():
            if self.owner.energy.expend(self.attack2Cost):
                self.attack2Refire.reset()
                self.owner.fireBullet(BeginningP2Bullet)
                rcache.get_sound("Powers_Begining_Grenade_Launch").play()

    def startDefend(self):
        #print "Starting defend"
        self.defending = True
        

    def stopDefend(self):
        #print "Stopping defend"
        self.defending = False
        self.owner.life.damageAttenuation = 1.0

    def jump(self):
        pass

    def startJump(self):
        if self.owner.onGround:
            self.owner.physicsObj.apply_impulse((0, 100))
            self.owner.onGround = False
            self.jumpTimer.reset()
            self.jumping = True

    def stopJump(self):
        self.jumping = False


class AirPower(NullPower):
    """The Air elemental power set."""
    def __init__(self, owner):
        NullPower.__init__(self, owner)
        self.timer = 0.0

        self.attack1Timer = Timer(defaultTime = 0.1)
        self.attack2Timer = Timer(defaultTime = 0.8)
        self.attack2FireTimer = Timer(defaultTime = 0.3)
        self.attack2Charging = False
        
        self.jumping = False

        self.defending = False
        self.defenseAngularVel = 0.0
        self.defenseVelLimit = 0.0
        self.defenseTimer = Timer(defaultTime = 0.3)
        self.defenseCooldownTimer = Timer(defaultTime = 1.25)

        self.attack1Cost = 5.0
        self.attack2Cost = 40.0
        self.defendCost = 5.0
        self.jumpCost = 0.0
        
    def startDefend(self):
        if self.defenseCooldownTimer.expired():
            if self.owner.energy.expend(self.defendCost):
                self.defending = True
                self.defenseCooldownTimer.reset()
                self.defenseTimer.reset()
                self.defenseAngularVel = self.owner.physicsObj.angular_velocity
                self.defenseVelLimit = self.owner.physicsObj.velocity_limit
                self.owner.physicsObj.velocity_limit = 1000.0

    def update(self, dt):
        self.attack1Timer.update(dt)
        self.attack2Timer.update(dt)
        self.defenseCooldownTimer.update(dt)
        if self.jumping:
            self.owner.physicsObj.apply_impulse((0, 300*dt))
        if self.defending:
            facing = self.owner.facing
            #self.owner.physicsObj.apply_impulse((-facing * 3000 * dt, 0))
            self.owner.physicsObj.velocity = (-facing * 500, 0)
            self.defenseTimer.update(dt)
            if self.defenseTimer.expired():
                self.defending = False
                self.owner.physicsObj.velocity = (0,0)
                self.owner.physicsObj.angular_velocity = self.defenseAngularVel
                self.owner.physicsObj.velocity_limit = self.defenseVelLimit

        if self.attack2Charging and self.attack2Timer.expired():
            if self.owner.energy.expend(self.attack2Cost):
                self.owner.fireBullet(AirP2Bullet)
                rcache.get_sound("Powers_Air_Lit").play()
            self.attack2Charging = False
        
    def startJump(self):
        if self.owner.onGround:
            if self.owner.energy.expend(self.jumpCost):
                self.owner.physicsObj.apply_impulse((0, 300))
        # With the Air jump we can float down slowly even
        # if we didn't start off jumping
        self.jumping = True

    def stopJump(self):
        self.jumping = False

    def startAttack1(self):
        if self.attack1Timer.expired():
            self.attack1Timer.reset()
            if self.owner.energy.expend(self.attack1Cost)==True:
                if self.owner.onGround:
                    self.owner.fireBullet(AirP1BulletGround)
                    rcache.get_sound("Powers_Air_Wave_Small").play()
                else:
                    self.owner.fireBullet(AirP1BulletAir)
                    rcache.get_sound("Powers_Air_Wave_Large").play()

    def startAttack2(self):
        self.attack2Timer.reset()
        self.attack2Charging = True

    def stopAttack2(self):
        self.attack2Charging = False


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
    def __init__(self):
        self.sequence = 0
        self.time = 0.0
        self.maxSequence = 3
        self.maxTime = 0.2

    def update(self, dt):
        self.time -= dt

    def attack(self):
        if self.sequence == 0:
            self.attack1()
            self.time = self.maxTime
            self.sequence += 1
        elif self.sequence == 1 and self.time > 0.0:
            self.attack2()
            self.time = self.maxTime
            self.sequence += 1
        elif self.sequence == 2 and self.time > 0.0:
            self.attack3()
            #self.time = self.maxTime
            self.sequence = 0
            

            
class PowerSet(Component):
    def __init__(self, owner):
        Component.__init__(self, owner)
        self.powerIndex = 0
        self.powers = [NullPower(owner)]
        self.powers = [AirPower(owner),BeginningsPower(owner)]
        self.currentPower = self.powers[self.powerIndex]

    def addPower(self, power):
        # Remove the null power if it exists before adding
        # the other power
        # Can't use isinstance() here 'cause power all inherit
        # from NullPower, so.
        if len(self.powers) == 1 and self.powers[0].__class__ == NullPower:
            self.powers = [power]
        elif power not in self.powers:
            self.powers.append(power)
            self.powers.sort()
        self.currentPower = power
        self.powerIndex = self.powers.index(power)
        print "Added power:", power
        print self.powers
        
    def update(self, dt):
        self.currentPower.update(dt)

    def attack1(self):
        self.currentPower.attack1()

    def attack2(self):
        self.currentPower.attack2()

    def defend(self):
        self.currentPower.defend()

    def jump(self):
        self.currentPower.jump()

    def startAttack1(self):
        self.currentPower.startAttack1()

    def startAttack2(self):
        self.currentPower.startAttack2()

    def startDefend(self):
        self.currentPower.startDefend()

    def startJump(self):
        self.currentPower.startJump()

    def stopAttack1(self):
        self.currentPower.stopAttack1()

    def stopAttack2(self):
        self.currentPower.stopAttack2()

    def stopDefend(self):
        self.currentPower.stopDefend()

    def stopJump(self):
        self.currentPower.stopJump()

    def draw(self, shader):
        self.currentPower.draw(shader)

    def nextPower(self):
        self.powerIndex = (self.powerIndex + 1) % len(self.powers)
        self.currentPower = self.powers[self.powerIndex]

    def prevPower(self):
        self.powerIndex = (self.powerIndex - 1) % len(self.powers)
        self.currentPower = self.powers[self.powerIndex]
