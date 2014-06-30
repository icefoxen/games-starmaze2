import pyglet
from pyglet.gl import *
import pymunk
import pymunk.pyglet_util


class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s, x, y):
        s.body = pymunk.Body(1, 2000)
        s.shape = pymunk.Circle(s.body, 10)
        s.shape.friction = 5.8
        s.body.position = (x,y)

    def draw(s):
        pymunk.pyglet_util.draw(s.shape)
