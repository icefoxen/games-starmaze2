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


# TODO: Bullet class?
# Should bullets keep a reference to their firer?
# Might be useful, I dunno.  No immediate need for
# it though.

# TODO: WE MIGHT NEED SOME PROPER EVENTS TO OCCUR FOR ACTORS
# Hit ground, leave ground
# Hit terrain, leave contact with terrain
# Hit with attack, touch enemy (useful for bullets)
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
            s.world.birthActor(b)

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
        s.owner.world.birthActor(bullet)

    def draw(s, shader):
        pass
    
class BeginningsPower(NullPower):
    """The Beginnings elemental power set."""
    def __init__(s, owner):
        NullPower.__init__(s, owner)
        s.timer = 0.0

        s.timer1 = 0.0
        s.usingAttack1 = False
        s.refireTime1 = 0.05
        
        s.refireTime2 = 1.5
        
        s.jumpTimerTime = 0.20
        s.jumpTimer = 0.0
        s.jumping = False

        s.defending = False
        s.shieldImage = rcache.getLineImage(images.shieldImage)
        s.shieldSprite = LineSprite(s, s.shieldImage)

    def draw(s, shader):
        if s.defending:
            s.shieldSprite.position = s.owner.physicsObj.position
            s.shieldSprite.draw()

    def update(s, dt):
        s.timer -= dt
        s.timer1 -= dt
        s.jumpTimer -= dt
        if s.jumping and s.jumpTimer > 0:
            s.owner.physicsObj.apply_impulse((0, 2000 * dt))

        if s.usingAttack1 and s.timer1 < 0:
            dtCopy = dt
            # Make the number of bullets fired
            # correct even at low framerates
            while dtCopy > 0.0:
                s.timer1 = s.refireTime1
                s.fireBullet(BeginningP1Bullet)
                dtCopy -= s.refireTime1

    # BUGGO: It's concievable we'd have to fire multiple shots in the same frame...
    # If we lag real bad at least.
    # But since that'd currently involve going 20 FPS...
    def startAttack1(s):
        s.usingAttack1 = True

    def stopAttack1(s):
        s.usingAttack1 = False
        
    def attack2(s):
        if s.timer < 0.0:
            s.timer = s.refireTime2
            x, y = s.owner.physicsObj.position
            direction = s.owner.facing
            bullet = BeginningP2Bullet(x, y, direction)
            s.owner.world.birthActor(bullet)

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
            s.jumpTimer = s.jumpTimerTime
            s.jumping = True

    def stopJump(s):
        s.jumping = False

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
            s.powers.add(power)
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
