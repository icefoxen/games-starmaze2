import pyglet
from pyglet.gl import *
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
        s.brakeFactor = 400

        s.motionX = 0
        s.motionY = 0
        s.braking = False

    def _setPosition(s, pt):
        s.body.position = pt

    position = property(lambda s: s.body.position, _setPosition,
                        doc="The position of the Actor.")

    def draw(s):
        pymunk.pyglet_util.draw(s.shape)

    # XXX: Is it better to track key state?  I sort of think so...
    def stopMoving(s):
        s.motionX = 0
        s.motionY = 0
    
    def moveLeft(s):
        s.motionX = -1

    def moveRight(s):
        s.motionX = 1

    def brake(s):
        s.braking = True

    def update(s, dt):
        xImpulse = s.moveForce * s.motionX * dt
        yImpulse = 0.0
        s.body.apply_impulse((xImpulse, yImpulse))

        #fmtstr = "position: {} force: {} torque: {}"
        #print fmtstr.format(s.body.position, s.body.force, s.body.torque)


class Player(Actor):
    """The player object."""
    def __init__(s):
        super(s.__class__, s).__init__()
