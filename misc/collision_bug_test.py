import pyglet
import pymunk
import pymunk.pyglet_util

import os


class Actor(object):
    """The basic thing-that-moves-and-does-stuff in a `Room`."""
    def __init__(s, x, y):
        s.verts = [
            (-10, -10), (-10, 10),
            (10, 10), (10, -10)
        ]
        s.body = pymunk.Body(1, 200)
        #s.shape = pymunk.Circle(s.body, 10)
        s.shape = pymunk.Poly(s.body, s.verts, radius=1)
        s.shape.friction = 0.8
        s.body.position = (x,y)

    def draw(s):
        pymunk.pyglet_util.draw(s.shape)

STATIC_BODY = pymunk.Body()

class Terrain(object):
    def __init__(s, verts, colors, batch=None):
        "Create a `Terrain` object."
        s.verts = verts
        s.colors = colors
        s.body = STATIC_BODY
        poly = pymunk.Poly(s.body, verts)
        poly.friction = 0.8
        line = pymunk.Segment(s.body, verts[0], verts[1], 2)
        s.shapes = [poly]

        s.batch = None or pyglet.graphics.Batch()

    def addToSpace(s, space):
        space.add(s.shapes)
        if s.body != STATIC_BODY:
            space.add(s.body)

def createBlock(x, y, w, h, color=(255, 255, 255, 255), batch=None):
    "Creates a `Terrain` object representing a block of the given size."
    xf = float(x)
    yf = float(y)
    wf = float(w)
    hf = float(h)
    verts = [
        (xf, yf), (xf+wf, yf), 
        (xf+wf, yf), (xf+wf, yf+hf),
        (xf+wf, yf+hf), (xf, yf+hf),
        (xf, yf+hf), (xf, yf)
    ]

    verts = [
        (xf, yf), 
        (xf+wf, yf), 
        (xf+wf, yf+hf),
        (xf, yf+hf),
        (xf, yf)
    ]
    colors = []
    if isinstance(color, list):
        if len(color) != len(verts):
            raise Exception("Color array not right size, expected {}, got {}: {}".format(len(verts), len(color), color))
        else:
            colors = color
    else:
        if len(color) != 4:
            raise Exception("color is not a 4-tuple: {}".format(color))
        else:
            colors = [color] * len(verts)

    t = Terrain(verts, colors, batch)
    return t

class Room(object):
    """A collection of `Terrain` objects and environmental data.
Also handles physics.  There's only ever one `Room` on screen
at a time."""
    def __init__(s):
        s.terrain = set()
        s.space = pymunk.Space()
        s.space.gravity = (0.0, -400.0)

        s.actors = set()

        s.name = ""

    def addTerrain(s, t):
        "Adds a `Terrain` object to the room."
        s.terrain.add(t)
        s.space.add(t.shapes)

    def addActor(s, a):
        "Adds the given `Actor` to the room."
        s.actors.add(a)
        s.space.add(a.shape, a.body)

    def update(s,dt):
        "Updates all the physics objects in the room."
        s.space.step(dt)


    def draw(s):
        "Draws the room and all its contents."
        for ter in s.terrain:
            ter.draw()
        for act in s.actors:
            act.draw()

class World(object):
    """Contains all the state for the game."""
    def __init__(s, screenw, screenh):
        s.window = pyglet.window.Window(width=screenw, height=screenh)
        s.screenw = screenw
        s.screenh = screenh

        s.setupWorld()

        s.physicsSteps = 30.0

        s.window.push_handlers(
            on_draw = lambda: s.on_draw(),
        )
        
    def setupWorld(s):
        s.room = Room()
        b1 = createBlock(330, 100, 570, 30)
        b2 = createBlock(300, 100, 30, 300)
        b3 = createBlock(800, 100, 30, 300)
        b4 = createBlock(300, 200, 270, 30)
        s.room.addTerrain(b1)
        s.room.addTerrain(b2)
        s.room.addTerrain(b3)
        s.room.addTerrain(b4)

        s.player = Actor(s.screenw / 2, s.screenh / 2)
        s.room.addActor(s.player)


    def update(s, dt):
        step = dt / s.physicsSteps
        for _ in range(int(s.physicsSteps)):
            s.room.update(step)

    def on_draw(s):
        s.window.clear()
        pymunk.pyglet_util.draw(s.room.space)
   

PHYSICS_FPS = 60.0
def main():
    screenw = 1024
    screenh = 768

    world = World(screenw, screenh)

    pyglet.clock.schedule_interval(lambda dt: world.update(dt), 1.0/PHYSICS_FPS)
    pyglet.app.run()


if __name__ == '__main__':
    main()
