#!/usr/bin/env python

import os
import random
import time

import pyglet
import pyglet.window.key as key
from pyglet.gl import *

from graphics import *
import renderer
import images
from terrain import *
import starmaze


class LevelEditor(object):
    """Level editor!  For now, just click and drag to draw platforms,
P starts playing the game, O outputs the level description in a form
suitable for copy-pasting into a Python file."""
    def __init__(s, screenw, screenh):
        s.window = pyglet.window.Window(width=screenw, height=screenh)
        s.window.set_vsync(True)
        s.screenw = screenw
        s.screenh = screenh

        s.fps_display = pyglet.clock.ClockDisplay()
        s.renderManager = renderer.RenderManager()
        renderer.preloadRenderers()
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

        s.chunkGuide = images.chunkGuide()

    def update(s, dt):
        s.handleInputState()
        s.camera.update(dt)
        
    def on_draw(s):
        s.window.clear()
        with s.camera:
            s.chunkGuide.batch.draw()
            s.renderManager.render()
        #s.fps_display.draw()

    def startGameInstance(s):
        """Starts a new World with the objects specified in the level editor."""
        if s.world is None:
            print 'Starting new game instance'
            s.world = starmaze.World(s.screenw, s.screenh)

            descrs = [block.describe() for block in s.actors]
            newobjs = set([blockDescr() for blockDescr in descrs])
            for obj in newobjs:
                s.world.addActor(obj)
            
            s.worldUpdateFunc = lambda dt: s.world.update(dt)
            pyglet.clock.schedule_interval(s.worldUpdateFunc, 1.0/starmaze.PHYSICS_FPS)

            s.world.window.push_handlers(
                on_close = lambda: s.killGameInstance()
                )
        else:
            print "Game instance already running, should prolly close that first."

    def outputGameInstance(s):
        """Prints out all the objects in the level, in a state suitable for copy-pasting
into a python file"""
        descrs = [block.describeString() for block in s.actors]
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
        if button == 1:
            s.currentTarget = createBlockCorner(x-s.camera.x, y-s.camera.y, 1, 1)
            s.startDrag = (x, y)
        elif button == 4:
            # Check if a block is under the click location
            # If so, select it
            for act in s.actors:
                bb = act.physicsObj.getBB()
                # Adjust for camera position
                cx = x - s.camera.x
                cy = y - s.camera.y
                if bb.contains_vect((cx,cy)):
                    s.currentTarget = act
                    print 'Selected', act
                    return
            s.currentTarget = None
            print 'Selected nothing.'
            
    def on_mouse_drag(s, x, y, dx, dy, button, modifiers):
        #print "Mouse drag:", x, y, dx, dy, button, modifiers
        #print "Start drag", s.startDrag
        if s.currentTarget is not None:
            if button == 1:
                # Left-click creates new thing
                # We have to make it so you can start dragging from
                # any corner, but it will adjusted it to the bottom
                # left corner that is the reference point for our
                # box-creation code.
                startx, starty = s.startDrag
                snapToSize = 16
                correctedStartX = (startx + snapToSize) - (startx % snapToSize)
                correctedStartY = (starty + snapToSize) - (starty % snapToSize)

                correctedCurrentX = (x + snapToSize) - (x % snapToSize)
                correctedCurrentY = (y + snapToSize) - (y % snapToSize)

                width = correctedCurrentX - correctedStartX
                height = correctedCurrentY - correctedStartY
            
                cameraAdjustedX = correctedStartX - s.camera.x
                cameraAdjustedY = correctedStartY - s.camera.y

                # switching out the renderer shouldn't really be ncessary but it is...
                # Because s.currentTarget isn't really in the list of actors and so
                # then things get a little squirrelly.
                s.renderManager.removeActorIfPossible(s.currentTarget)
                s.currentTarget = createBlockCorner(cameraAdjustedX, cameraAdjustedY,
                                                    width, height)
                s.renderManager.addActorIfPossible(s.currentTarget)
            #elif button == 4:
                # Right-click moves thing


    def on_mouse_release(s, x, y, button, modifiers):
        #print "Mouse release:", x, y, button, modifiers
        if s.currentTarget is not None:
            if button == 1:
                #print "Obj created:", s.currentTarget.corners
                s.actors.add(s.currentTarget)
            
    def on_mouse_scroll(s, x, y, scroll_x, scroll_y):
        #print "Mouse scroll:", x, y, scroll_x, scroll_y
        pass
            
    def on_key_press(s, k, modifiers):
        moveDistance = 16
        if modifiers & key.MOD_SHIFT:
            moveDistance *= 8
        if k == key.P:
            s.startGameInstance()
        elif k == key.O:
            s.outputGameInstance()
        elif k == key.BACKSPACE and modifiers & key.MOD_CTRL:
            for act in s.actors:
                s.renderManager.removeActorIfPossible(act)
            s.actors = set()
        elif s.currentTarget is not None:
            x, y = s.currentTarget.physicsObj.position
            if k == key.BACKSPACE or k == key.DELETE:
                s.actors.remove(s.currentTarget)
                s.renderManager.removeActorIfPossible(s.currentTarget)
                s.currentTarget = None
            elif k == key.LEFT:
                s.currentTarget.physicsObj.position = (x - moveDistance, y)
            elif k == key.RIGHT:
                s.currentTarget.physicsObj.position = (x + moveDistance, y)
            elif k == key.UP:
                s.currentTarget.physicsObj.position = (x, y + moveDistance)
            elif k == key.DOWN:
                s.currentTarget.physicsObj.position = (x, y - moveDistance)
        # else:
        #     print s.camera.aimedAtX, s.camera.aimedAtY
        #     if k == key.LEFT:
        #         print 'rar', moveDistance, s.camera.aimedAtX
        #         s.cameraTarget.position[0] -= moveDistance
        #         print 'roar', s.camera.aimedAtX
        #     elif k == key.RIGHT:
        #         s.camera.aimedAtX -= moveDistance
        #     elif k == key.UP:
        #         s.camera.aimedAtY += moveDistance
        #     elif k == key.DOWN:
        #         s.camera.aimedAtY -= moveDistance

    def handleInputState(s):
        if s.currentTarget is None:
            x, y = s.cameraTarget.position
            if s.keyboard[key.LEFT]:
                s.cameraTarget.position = (x-10, y)
            elif s.keyboard[key.RIGHT]:
                s.cameraTarget.position = (x+10, y)
            if s.keyboard[key.UP]:
                s.cameraTarget.position = (x, y+10)
            elif s.keyboard[key.DOWN]:
                s.cameraTarget.position = (x, y-10)
            




def main():
    screenw = 1050
    screenh = 1050

    editor = LevelEditor(screenw, screenh)

    pyglet.clock.schedule_interval(lambda dt: editor.update(dt), 1.0/60.0)
    pyglet.app.run()

if __name__ == '__main__':
    main()
