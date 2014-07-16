#!/usr/bin/env python

import os
import random
import time

import pyglet
import pyglet.window.key as key
from pyglet.gl import *

from graphics import *
from terrain import *
import starmaze


class LevelEditor(object):
    def __init__(s, screenw, screenh):
        s.window = pyglet.window.Window(width=screenw, height=screenh)
        s.window.set_vsync(True)
        s.screenw = screenw
        s.screenh = screenh

        s.fps_display = pyglet.clock.ClockDisplay()

        s.keyboard = key.KeyStateHandler()
        s.window.push_handlers(s.keyboard)
        
        s.window.push_handlers(
            on_draw = lambda: s.on_draw(),
            on_key_press = lambda k, mods: s.on_key_press(k, mods),
            on_mouse_press = lambda x, y, b, m: s.on_mouse_press(x, y, b, m),
            on_mouse_release = lambda x, y, b, m: s.on_mouse_release(x, y, b, m),
            on_mouse_scroll = lambda x, y, scroll_x, scroll_y: s.on_mouse_scroll(x, y, scroll_x, scroll_y),
            on_mouse_drag = lambda x, y, dx, dy, b, m: s.on_mouse_drag(x, y, dx, dy, b, m)
        )

        s.actors = set()

        s.currentTarget = None
        s.startDrag = (0.0, 0.0)
        
        s.cameraTarget = pymunk.Body()
        s.camera = Camera(s.cameraTarget, s.screenw, s.screenh)

        s.world = None
        s.worldUpdateFunc = None

    def update(s, dt):
        s.handleInputState()
        s.camera.update(dt)

    def on_draw(s):
        s.window.clear()
        with s.camera:
            if s.currentTarget is not None:
                s.currentTarget.draw()
            for act in s.actors:
                act.draw()

        s.fps_display.draw()
        #print s.cameraTarget.position

    def startGameInstance(s):
        if s.world is None:
            print 'Starting new game instance'
            s.world = starmaze.World(s.screenw, s.screenh)

            descrs = [BlockDescription.fromObject(block) for block in s.actors]
            newobjs = set([block.create() for block in descrs])
            for obj in newobjs:
                s.world.birthActor(obj)
            
            s.worldUpdateFunc = lambda dt: s.world.update(dt)
            pyglet.clock.schedule_interval(s.worldUpdateFunc, 1.0/starmaze.PHYSICS_FPS)

            s.world.window.push_handlers(
                on_close = lambda: s.killGameInstance()
                )
        else:
            print "Game instance already running, should prolly close that first."

    def outputGameInstance(s):
        descrs = [BlockDescription.fromObject(block) for block in s.actors]
        print "["
        for d in descrs:
            print d, ","
        print "]"

    def killGameInstance(s):
        print 'killing game instance'
        if s.world is not None:
            pyglet.clock.unschedule(s.worldUpdateFunc)
            s.worldUpdateFunc = None
            s.world = None

    def on_mouse_press(s, x, y, button, modifiers):
        #print "Mouse press:", x, y, button, modifiers
        s.currentTarget = createBlockCorner(x-s.camera.x, y-s.camera.y, 1, 1)
        s.startDrag = (x, y)

    def on_mouse_drag(s, x, y, dx, dy, button, modifiers):
        #print "Mouse drag:", x, y, dx, dy, button, modifiers
        #print "Start drag", s.startDrag
        if s.currentTarget is not None:
            # BUGGO: click-dragging to define a new shape
            # doesn't handle winding order right.
            tx, ty = s.startDrag
            #topleftx = tx - s.camera.x
            #toplefty = tx - s.camera.y
            snapToSize = 10
            tx = tx - (tx % snapToSize)
            ty = ty - (ty % snapToSize)
            camerax = s.camera.x - (s.camera.x % snapToSize)
            cameray = s.camera.y - (s.camera.y % snapToSize)
            x = x - (x % snapToSize)
            y = y - (y % snapToSize)

            #s.currentTarget = createBlock(tx, ty, tx-x, ty-y)
            s.currentTarget = createBlockCorner(tx - s.camera.x, ty - s.camera.y, -(tx-x), -(ty-y))

    def on_mouse_release(s, x, y, button, modifiers):
        #print "Mouse release:", x, y, button, modifiers
        print "Obj created:", s.currentTarget.corners
        s.actors.add(s.currentTarget)
        s.currentTarget = None
            
    def on_mouse_scroll(s, x, y, scroll_x, scroll_y):
        print "Mouse scroll:", x, y, scroll_x, scroll_y
            
    def on_key_press(s, k, modifiers):
        if k == key.P:
            s.startGameInstance()
        if k == key.O:
            s.outputGameInstance()

    def handleInputState(s):
        x, y = s.cameraTarget.position
        if s.keyboard[key.LEFT]:
            s.cameraTarget.position = (x-10, y)
        elif s.keyboard[key.RIGHT]:
            s.cameraTarget.position = (x+10, y)
        x, y = s.cameraTarget.position
        if s.keyboard[key.UP]:
            s.cameraTarget.position = (x, y+10)
        elif s.keyboard[key.DOWN]:
            s.cameraTarget.position = (x, y-10)
            




def main():
    screenw = 1024
    screenh = 768

    editor = LevelEditor(screenw, screenh)

    pyglet.clock.schedule_interval(lambda dt: editor.update(dt), 1.0/60.0)
    pyglet.app.run()

if __name__ == '__main__':
    main()
