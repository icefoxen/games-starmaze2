import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util


class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s, x, y):
        s.verts = [
            (-10, -10), (-10, 10),
            (10, 10), (10, -10)
        ]
        s.verts.reverse()
        s.body = pymunk.Body(1, 200)
        #s.shape = pymunk.Circle(s.body, 10)
        s.shape = pymunk.Poly(s.body, s.verts, radius=1)
        s.shape.friction = 0.8
        s.body.position = (x,y)

    def draw(s):
        pymunk.pyglet_util.draw(s.shape)

    def stopMoving(s):
        s.body.force = pymunk.Vec2d(0,0)

    
    def moveLeft(s):
        s.body.apply_force((-100, 0))

    def moveRight(s):
        s.body.apply_force((100, 0))

    def moveUp(s):
        s.body.apply_force((0, 600))


    def update(s, dt):
        fmtstr = "position: {} force: {} torque: {}"
        print fmtstr.format(s.body.position, s.body.force, s.body.torque)
