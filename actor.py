import pyglet
import pyglet.window.key as key
import pymunk
import pymunk.pyglet_util


class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s):
        s.verts = [
            (-10, -10), (10, -10),
            (10, 10), (-10, 10)
        ]
        s.verts.reverse()
        s.body = pymunk.Body(1, 200)
        s.shape = pymunk.Circle(s.body, 10)
        #s.shape = pymunk.Poly(s.body, s.verts, radius=1)
        s.shape.friction = 5.8
        s.body.position = (0,0)
        s.moveForce = 400
        s.brakeForce = 400

        s.motionX = 0
        s.braking = False

    def _setPosition(s, pt):
        s.body.position = pt

    position = property(lambda s: s.body.position, _setPosition,
                        doc="The position of the Actor.")

    def draw(s):
        pymunk.pyglet_util.draw(s.shape)

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


class Player(Actor):
    """The player object."""
    def __init__(s, keyboard):
        super(s.__class__, s).__init__()
        s.keyboard = keyboard

    def update(s, dt):
        s.handleInputState()
        super(s.__class__, s).update(dt)

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
            print 'l'
            s.moveLeft()
        elif s.keyboard[key.RIGHT]:
            print 'r'
            s.moveRight()

        # Jump, maybe
        if s.keyboard[key.UP]:
            pass

        # Powers
        if s.keyboard[key.A]:
            pass
        if s.keyboard[key.S]:
            pass
        if s.keyboard[key.D]:
            pass
        if s.keyboard[key.W]:
            pass


    def handleInputEvent(s, k, mod):
        """Handles edge-triggered keyboard actions (key presses, not holds)"""
        # Switch powers
        if k == key.Q:
            print 'foo'
        elif k == key.E:
            print 'bar'
