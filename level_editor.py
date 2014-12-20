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
    """Level editor!  For now, just click and drag to draw platformself,
P starts playing the game, O outputs the level description in a form
suitable for copy-pasting into a Python file."""
    def __init__(self, screenw, screenh):
        self.window = pyglet.window.Window(width=screenw, height=screenh)
        self.window.set_vsync(True)
        self.screenw = screenw
        self.screenh = screenh

        self.fps_display = pyglet.clock.ClockDisplay()
        self.renderManager = renderer.RenderManager()
        renderer.preloadRenderers()
        self.keyboard = key.KeyStateHandler()
        self.window.push_handlers(self.keyboard)
        
        self.window.push_handlers(
            on_draw = lambda: self.on_draw(),
            on_key_press = lambda k, mods: self.on_key_press(k, mods),
            on_mouse_press = lambda x, y, b, m: self.on_mouse_press(x, y, b, m),
            on_mouse_release = lambda x, y, b, m: self.on_mouse_release(x, y, b, m),
            on_mouse_scroll = lambda x, y, scroll_x, scroll_y: self.on_mouse_scroll(x, y, scroll_x, scroll_y),
            on_mouse_drag = lambda x, y, dx, dy, b, m: self.on_mouse_drag(x, y, dx, dy, b, m)
        )

        self.actors = set()

        self.currentTarget = None
        self.indicator = None
        self.startDrag = (0.0, 0.0)
        
        self.cameraTarget = pymunk.Body()
        self.camera = Camera(self.cameraTarget, self.screenw, self.screenh)

        self.world = None
        self.worldUpdateFunc = None

        self.chunkGuide = images.chunkGuide()

    def update(self, dt):
        self.handleInputState()
        self.updateIndicator()
        self.camera.update(dt)

    def updateIndicator(self):
        if self.currentTarget is not None:
            if self.indicator is None:
                self.indicator = Indicator(self.currentTarget)
                self.actors.add(self.indicator)
                self.renderManager.addActorIfPossible(self.indicator)
            else:
                self.indicator.target = self.currentTarget
        elif self.currentTarget is None:
            if self.indicator is not None:
                self.renderManager.removeActorIfPossible(self.indicator)
                self.actors.remove(self.indicator)
                self.indicator = None

    def on_draw(self):
        self.window.clear()
        with self.camera:
            self.chunkGuide.batch.draw()
            self.renderManager.render()
        #self.fps_display.draw()

    def startGameInstance(self):
        """Starts a new World with the objects specified in the level editor."""
        if self.world is None:
            print 'Starting new game instance'
            self.world = starmaze.World(self.screenw, self.screenh)

            descrs = [block.describe() for block in self.actors]
            newobjs = set([blockDescr() for blockDescr in descrs])
            for obj in newobjs:
                self.world.addActor(obj)
            
            self.worldUpdateFunc = lambda dt: self.world.update(dt)
            pyglet.clock.schedule_interval(self.worldUpdateFunc, 1.0/starmaze.PHYSICS_FPS)

            self.world.window.push_handlers(
                on_close = lambda: self.killGameInstance()
                )
        else:
            print "Game instance already running, should prolly close that first."

    def outputGameInstance(self):
        """Prints out all the objects in the level, in a state suitable for copy-pasting
into a python file"""
        descrs = [block.describeString() for block in self.actors]
        print "["
        for d in descrs:
            print d, ","
        print "]"

    def killGameInstance(self):
        print 'killing game instance'
        if self.world is not None:
            pyglet.clock.unschedule(self.worldUpdateFunc)
            self.worldUpdateFunc = None
            self.world = None
            
    def on_mouse_press(self, x, y, button, modifiers):
        #print "Mouse press:", x, y, button, modifiers
        if button == 1:
            self.currentTarget = createBlockCorner(x-self.camera.x, y-self.camera.y, 1, 1)
            self.startDrag = (x, y)
        elif button == 4:
            # Check if a block is under the click location
            # If so, select it
            for act in self.actors:
                bb = act.physicsObj.getBB()
                # Adjust for camera position
                cx = x - self.camera.x
                cy = y - self.camera.y
                if bb is not None and bb.contains_vect((cx,cy)):
                    self.currentTarget = act
                    print 'Selected', act
                    return
            self.currentTarget = None
            print 'Selected nothing.'
            
    def on_mouse_drag(self, x, y, dx, dy, button, modifiers):
        #print "Mouse drag:", x, y, dx, dy, button, modifiers
        #print "Start drag", self.startDrag
        if self.currentTarget is not None:
            if button == 1:
                # Left-click creates new thing
                # We have to make it so you can start dragging from
                # any corner, but it will adjusted it to the bottom
                # left corner that is the reference point for our
                # box-creation code.
                startx, starty = self.startDrag
                snapToSize = 16
                correctedStartX = (startx + snapToSize) - (startx % snapToSize)
                correctedStartY = (starty + snapToSize) - (starty % snapToSize)

                correctedCurrentX = (x + snapToSize) - (x % snapToSize)
                correctedCurrentY = (y + snapToSize) - (y % snapToSize)

                width = correctedCurrentX - correctedStartX
                height = correctedCurrentY - correctedStartY
            
                cameraAdjustedX = correctedStartX - self.camera.x
                cameraAdjustedY = correctedStartY - self.camera.y

                # switching out the renderer shouldn't really be ncessary but it is...
                # Because self.currentTarget isn't really in the list of actors and so
                # then things get a little squirrelly.
                self.renderManager.removeActorIfPossible(self.currentTarget)
                self.currentTarget = createBlockCorner(cameraAdjustedX, cameraAdjustedY,
                                                    width, height)
                self.renderManager.addActorIfPossible(self.currentTarget)
            #elif button == 4:
                # Right-click moves thing


    def on_mouse_release(self, x, y, button, modifiers):
        #print "Mouse release:", x, y, button, modifiers
        if self.currentTarget is not None:
            if button == 1:
                #print "Obj created:", self.currentTarget.corners
                self.actors.add(self.currentTarget)
            
    def on_mouse_scroll(self, x, y, scroll_x, scroll_y):
        #print "Mouse scroll:", x, y, scroll_x, scroll_y
        pass
            
    def on_key_press(self, k, modifiers):
        moveDistance = 16
        if modifiers & key.MOD_SHIFT:
            moveDistance *= 8
        if k == key.P:
            self.startGameInstance()
        elif k == key.O:
            self.outputGameInstance()
        elif k == key.BACKSPACE and modifiers & key.MOD_CTRL:
            for act in self.actors:
                self.renderManager.removeActorIfPossible(act)
            self.actors = set()
        elif self.currentTarget is not None:
            x, y = self.currentTarget.physicsObj.position
            if k == key.BACKSPACE or k == key.DELETE:
                self.actors.remove(self.currentTarget)
                self.renderManager.removeActorIfPossible(self.currentTarget)
                self.currentTarget = None
            elif k == key.LEFT:
                self.currentTarget.physicsObj.position = (x - moveDistance, y)
            elif k == key.RIGHT:
                self.currentTarget.physicsObj.position = (x + moveDistance, y)
            elif k == key.UP:
                self.currentTarget.physicsObj.position = (x, y + moveDistance)
            elif k == key.DOWN:
                self.currentTarget.physicsObj.position = (x, y - moveDistance)
        # else:
        #     print self.camera.aimedAtX, self.camera.aimedAtY
        #     if k == key.LEFT:
        #         print 'rar', moveDistance, self.camera.aimedAtX
        #         self.cameraTarget.position[0] -= moveDistance
        #         print 'roar', self.camera.aimedAtX
        #     elif k == key.RIGHT:
        #         self.camera.aimedAtX -= moveDistance
        #     elif k == key.UP:
        #         self.camera.aimedAtY += moveDistance
        #     elif k == key.DOWN:
        #         self.camera.aimedAtY -= moveDistance

    def handleInputState(self):
        if self.currentTarget is None:
            x, y = self.cameraTarget.position
            if self.keyboard[key.LEFT]:
                self.cameraTarget.position = (x-16, y)
            elif self.keyboard[key.RIGHT]:
                self.cameraTarget.position = (x+16, y)
            if self.keyboard[key.UP]:
                self.cameraTarget.position = (x, y+16)
            elif self.keyboard[key.DOWN]:
                self.cameraTarget.position = (x, y-16)
            




def main():
    screenw = 1050
    screenh = 1050

    editor = LevelEditor(screenw, screenh)

    pyglet.clock.schedule_interval(lambda dt: editor.update(dt), 1.0/60.0)
    pyglet.app.run()

if __name__ == '__main__':
    main()
