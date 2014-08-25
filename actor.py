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

class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s, batch=None):
        s.batch = batch or pyglet.graphics.Batch()
        s.physicsObj = None #PhysicsObj(s)


        #lines = cornersToLines(s.corners)
        #colors = [(255, 255, 255, 255) for _ in lines]
        #image = LineImage(lines, colors)
        s.sprite = None #LineSprite(image)

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
        #s.glowSprite = ImgSprite(s, glowImage)

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


class AirPowerup(Actor):
    def __init__(s):
        Actor.__init__(s)
        s.physicsObj = PowerupPhysicsObj(s)
        img = rcache.getLineImage(images.powerup)
        s.sprite = LineSprite(s, img)

    def collect(s, player):
        print "Gained Air power!"
        player.powers.addPower(AirPower(player))

class AirPowerupDescription(object):
    def __init__(s, x, y):
        s.x = x
        s.y = y

    def create(s):
        p = AirPowerup()
        p.physicsObj.position = (s.x, s.y)
        return p

    @staticmethod
    def fromObject(powerup):
        x, y = powerup.position
        return AirPowerupDescription(x, y)

    def __repr__(s):
        return "AirPowerupDescription({}, {})".format(
            s.x, s.y
            )



class CrawlerEnemy(Actor):
    """An enemy that crawls along the ground and hurts when you touch it.

BUGGO: Doesn't currently hurt when you touch it; blocked on better collision
handling I think."""
    def __init__(s, batch=None):
        Actor.__init__(s, batch)
        s.controller = RoamAIController(s)
        s.physicsObj = CrawlerPhysicsObj(s)
        img = rcache.getLineImage(images.crawler)
        s.sprite = LineSprite(s, img)

        s.facing = FACING_RIGHT
        s.life = Life(s, 3, reduction=8)

    def update(s, dt):
        s.controller.update(dt)
        # If it flips off of upright, apply restoring force.
        # XXX: Can we make these beasties stick to walls?
        # In the end that will all be the job of a Controller
        # object; sticking to walls will be easy just by applying
        # a force toward the wall with some friction, and maybe
        # countering gravity if necessary
        # Also consider how to stick to walls when going on slopes...

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
        s.world.addActor(c)


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


    

# TODO: Bullet class?
# Should bullets keep a reference to their firer?
# Might be useful, I dunno.  No immediate need for
# it though.
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
    def __init__(s, x, y, direction, impulse=None, lifetime=None):
        Actor.__init__(s)
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)
        s.physicsObj = PlayerBulletPhysicsObj(s, position=(x, y+yOffset))

        image = rcache.getLineImage(images.beginningsP1Bullet)
        s.sprite = LineSprite(s, image)
        if impulse == None:
            xImpulse = 400 * direction
            yImpulse = yOffset * 10
            s.physicsObj.apply_impulse((xImpulse, yImpulse))
        else:
            s.physicsObj.apply_impulse(impulse)
        # Counteract gravity?
        # BUGGO: Gravity might vary...
        s.physicsObj.apply_force((0, 400))
        s.life = TimedLife(s, 0.4 + (random.random() / 3.0))

        s.facing = direction
        s.damage = 1

    def update(s, dt):
        s.life.update(dt)

    def onDeath(s):
        #print 'bullet died'
        pass

class BeginningP2Bullet(Actor):
    def __init__(s, x, y, direction):
        Actor.__init__(s)
        s.physicsObj = PlayerBulletPhysicsObj(s, position=(x, y))
        # TODO: Placeholder image
        image = rcache.getLineImage(images.powerup)
        s.sprite = LineSprite(s, image)
        xImpulse = 300 * direction
        yImpulse = 200
        s.physicsObj.apply_impulse((xImpulse, yImpulse))
        
        s.damage = 10

    def update(s, dt):
        pass

    def collideWithEnemy(s, enemy):
        enemy.takeDamage(s.damage)

    def onDeath(s):
        for angle in range(0, 360, 30):
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
            b = BeginningP1Bullet(newx, newy, FACING_RIGHT, impulse=(xForce, yForce))
            b.life = TimedLife(b, 0.15)
            b.physicsObj.body.angle = rangle
            s.world.addActor(b)

class AirP1BulletAir(Actor):
    def __init__(s, x, y, direction):
        Actor.__init__(s)
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)
        s.physicsObj = AirP1PhysicsObjAir(s, position=(x, y+yOffset))

        image = rcache.getLineImage(images.airP1BulletAir)
        s.sprite = LineSprite(s, image)
        xImpulse = 600 * direction
        yImpulse = yOffset * 10
        s.physicsObj.apply_impulse((xImpulse, yImpulse))
        # Counteract gravity?
        s.physicsObj.apply_force((0, 400))
        s.maxTime = 0.45
        s.life = TimedLife(s, s.maxTime)

        s.facing = direction
        s.damage = 3

    def update(s, dt):
        s.life.update(dt)

    def onDeath(s):
        #print 'bullet died'
        pass

    def draw(s, shader):
        if (s.sprite is not None) and (s.physicsObj is not None):
            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            lifePercentage = s.life.time / s.maxTime
            #s.sprite.scale = (1/lifePercentage)
            shader.uniformf("alpha", lifePercentage)
            s.sprite.draw()

class AirP1BulletGround(Actor):
    def __init__(s, x, y, direction):
        Actor.__init__(s)
        yVariance = 5
        yOffset = (random.random() * yVariance) - (yVariance / 2)
        s.physicsObj = AirP1PhysicsObjGround(s, position=(x, y+yOffset))

        image = rcache.getLineImage(images.airP1BulletGround)
        s.sprite = LineSprite(s, image)
        xImpulse = 800 * direction
        yImpulse = yOffset * 10
        s.physicsObj.apply_impulse((xImpulse, yImpulse))
        # Counteract gravity?
        s.physicsObj.apply_force((0, 400))
        s.maxTime = 0.12
        s.life = TimedLife(s, s.maxTime)
        s.facing = direction
        s.damage = 2

    def update(s, dt):
        s.life.update(dt)

    def onDeath(s):
        #print 'bullet died'
        pass


    def draw(s, shader):
        if (s.sprite is not None) and (s.physicsObj is not None):
            s.sprite.position = s.physicsObj.position
            s.sprite.rotation = math.degrees(s.physicsObj.angle)
            lifePercentage = s.life.time / s.maxTime
            shader.uniformf("alpha", lifePercentage)
            s.sprite.draw()


# BUGGO This needs fixing
# We don't want to create a new bullet each frame I guess
# So we need to move this along so it follows its parent
# But actually doing damage is also kinda fucked up, then.
# Bligher.


# We precalculate these because it's actually pretty intensive
LIGHTNINGIMAGES = [images.airP2Bullet() for _ in range(20)]
class AirP2Bullet(Actor):
    # _freelist = []
    # def __new__(cls, x, y, direction):
    #     print "New bullet, length of freelist is", len(AirP2Bullet._freelist)
    #     if len(AirP2Bullet._freelist) == 0:
    #         print "Allocating new bullet"
    #         return super(AirP2Bullet, cls).__new__(cls, x, y, direction)
    #     else:
    #         print "Geting bullet from free list"
    #         b = AirP2Bullet._freelist.pop()
    #         b.__init__(x, y, direction)
    #         return b

    # def __del__(s):
    #     pass
    #     #print "Adding bullet to free list"
    #     #print "Length of free list is now", len(AirP2Bullet._freelist)
    #     #AirP2Bullet._freelist.append(s)

    # def onDeath(s):
    #     print 'Finalized'
    #     AirP2Bullet._freelist.append(s)
    #     #s.physicsObj.owner = None
    #     #s.sprite.owner = None
    #     #s.life.owner = None
    #     #for spr in s.sprites:
    #     #    spr.owner = None
    

    
    def __init__(s, x, y, direction):
        Actor.__init__(s)
        s.physicsObj = AirP1PhysicsObjGround(s, position=(x, y))
        s.physicsObj.apply_force((0, 400))
        # Different image each time, not cached!
        image = images.airP2Bullet()
        s.sprite = LineSprite(s, image)
        s.maxTime = 0.6
        s.life = TimedLife(s, s.maxTime)
        s.facing = direction
        s.animationTimer = Timer(0.03)

        s.images = LIGHTNINGIMAGES
        s.sprites = [LineSprite(s, image) for image in LIGHTNINGIMAGES]
        s.spriteCount = random.randint(0, len(LIGHTNINGIMAGES))

    def update(s, dt):
        #s.physicsObj.position = s.owner.physicsObj.position
        # Only lasts one frame
        #s.alive = False
        s.life.update(dt)
        s.animationTimer.update(dt)
        if s.animationTimer.expired():
            s.spriteCount = (s.spriteCount + 1) % len(s.sprites)
            s.sprite = s.sprites[s.spriteCount]
            #image = images.airP2Bullet()
            #s.sprite = LineSprite(s, image)
        


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

    def fireBullet(s, bulletClass):
        x, y = s.owner.physicsObj.position
        direction = s.owner.facing
        bullet = bulletClass(x, y, direction)
        s.owner.world.addActor(bullet)

    def draw(s, shader):
        pass


# BUGGO:
# As the code stands, with Beginnings and Air powers both
# attacks can be used pretty much at the same time.  Do
# we want it to be this way?
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
        s.shieldSprite = LineSprite(s, s.shieldImage)

    def draw(s, shader):
        if s.defending:
            s.shieldSprite.position = s.owner.physicsObj.position
            s.shieldSprite.draw()

    def update(s, dt):
        s.attack1Refire.update(dt)
        s.attack2Refire.update(dt)
        s.jumpTimer.update(dt)
        if s.jumping and not s.jumpTimer.expired():
            s.owner.physicsObj.apply_impulse((0, 2000 * dt))

        if s.usingAttack1 and s.attack1Refire.expired():
            # BUGGI: Make the number of bullets fired
            # correct even at low framerates
            s.fireBullet(BeginningP1Bullet)
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
            s.attack2Refire.reset()
            s.fireBullet(BeginningP2Bullet)

    def startDefend(s):
        #print "Starting defend"
        s.defending = True
        s.owner.life.attenuation = 0.25

    def stopDefend(s):
        #print "Stopping defend"
        s.defending = False
        s.owner.life.attenuation = 1.0

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
        
    def startDefend(s):
        if s.defenseCooldownTimer.expired():
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
            s.owner.physicsObj.velocity = (-facing * 1000, 0)
            s.defenseTimer.update(dt)
            if s.defenseTimer.expired():
                s.defending = False
                s.owner.physicsObj.velocity = (0,0)
                s.owner.physicsObj.angular_velocity = s.defenseAngularVel
                s.owner.physicsObj.velocity_limit = s.defenseVelLimit

        if s.attack2Charging and s.attack2Timer.expired():
            s.fireBullet(AirP2Bullet)
            #s.attack2Charging = False
        
    def startJump(s):
        if s.owner.onGround:
            s.owner.physicsObj.apply_impulse((0, 300))
        # With the Air jump we can float down slowly even
        # if we didn't start off jumping
        s.jumping = True

    def stopJump(s):
        s.jumping = False

    def startAttack1(s):
        if s.attack1Timer.expired():
            s.attack1Timer.reset()
            if s.owner.onGround:
                s.fireBullet(AirP1BulletGround)
                print "Zang!"
            else:
                s.fireBullet(AirP1BulletAir)
                print "Zam!"

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
        s.currentPower = s.powers[s.powerIndex]

    def addPower(s, power):
        # Remove the null power if it exists before adding
        # the other power
        # Can't use isinstance() here 'cause power all inherit
        # from NullPower, so.
        if len(s.powers) == 1 and s.powers[0].__class__ == NullPower:
            s.powers = [power]
        else:
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
