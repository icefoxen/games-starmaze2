import collections
import math
from ctypes import *

import pyglet
from pyglet.gl import *

import rcache
import images
import shader
import graphics

# So the world is going to have a number of renderers
LAYER_BG = 0
LAYER_FG  = 1
LAYER_GUI = 2
LAYERS = [LAYER_BG, LAYER_FG, LAYER_GUI]

# XXX: Components should have update and onDeath methods that always get called, I guess...
# Miiiiight slow shit down some, but oh well.
# EXCEPT, update matter orders dammit, so suddenly we need to order components before updating...

# XXX: Except we could remove RenderComponents entirely, I think, and have each Renderer just pluck
# out the required properties from the Actors itself.
#
# Except that means all actors have to remove themselves from the render manager on death
# Which... is doable, really.  But maybe not the best way.  Hm.
# Actually, the world should do it.  The actor has to know what renderer it's using, so it just
# has a reference to a particular renderer.  Then the world knows what renderer to remove it
# from on death.

class Renderer(object):
    """A class that draws a particular type of thing."""
    def __init__(self):
        self.layer = LAYER_FG
        self.shader = rcache.getShader('default')

    def __lt__(self, other):
        return self.layer < other.layer

    # These are usually-sane defaults for renderStart, renderFinish and renderActor,
    # but feel free to override them if you want.
    def renderStart(self):
        glPushAttrib(GL_COLOR_BUFFER_BIT)
        glEnable(GL_BLEND)
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
        self.shader.bind()
        
    def renderFinish(self):
        self.shader.unbind()
        glPopAttrib()

    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            self.shader.uniformi("facing", actor.facing)
            self.shader.uniformf("alpha", 1.0)
            self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            self.img.batch.draw()
        
    def renderAll(self, actors):
        self.renderStart()
        for act in actors:
            self.renderActor(act)
        self.renderFinish()

class GUIRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.layer = LAYER_GUI
        self.lifeBarImage = rcache.getLineImage(images.lifeBar)
        self.energyBarImage = rcache.getLineImage(images.energyBar)

    def renderActor(self, actor):
        player = actor.player
        lifeFraction = player.life.life / player.life.maxLife
        # TODO: Energy doesn't exist yet.
        energyFraction = player.energy.energy / player.energy.maxEnergy
        
        self.shader.uniformi("facing", 1)
        self.shader.uniformf("alpha", 1.0)
        self.shader.uniformf("vertexDiff", 0, 0, 0, 0)


        x = actor.world.camera.currentX
        y = actor.world.camera.currentY
        
        with graphics.Affine((x + -100, y - 378), 0.0, (lifeFraction, 1.0)):
            self.lifeBarImage.batch.draw()

        with graphics.Affine((x + 100, y - 378), 0.0, (energyFraction, 1.0)):
            self.energyBarImage.batch.draw()

        
class SpriteRenderer(Renderer):
    """A renderer that just draws a bitmap sprite.

CURRENTLY EXPERIMENTAL."""
    def __init__(self):
        Renderer.__init__(self)
        self.image = rcache.get_image("playertest")
        self.batch = pyglet.graphics.Batch()
        self.sprite = pyglet.sprite.Sprite(self.image, batch=self.batch)
        self.shader = rcache.getShader('texture')
        self.shader = rcache.getShader('bloom')

    def renderActor(self, actor):
        self.shader.uniformi("facing", 1)
        self.shader.uniformf("alpha", 1.0)
        self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            self.batch.draw()


class PlayerRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)

        self.img = rcache.getLineImage(images.player)
        
        # Experimental glow effect, just overlay the sprite
        # with a diffuse, alpha-blended sprite.  Works surprisingly well.
        self.glowImage = rcache.getLineImage(images.playerGlow)


    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            self.shader.uniformi("facing", actor.facing)
            self.shader.uniformf("alpha", 1.0)
            self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            self.img.batch.draw()
            
            # XXX
            actor.powers.draw(self.shader)
            
            glow = -0.3 * abs(math.sin(actor.glow))
            self.shader.uniformf("vertexDiff", 0, 0, 0.0, glow)

            self.shader.uniformf("alpha", 0.2)
            self.glowImage.batch.draw()

# Having a renderer that does nothing but draw a particular image seems a bit lame
# But it's certainly the most flexible method, so we're sticking with it.
class CollectableRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.collectable)

class PowerupRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.powerup)

class CrawlerRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.crawler)
        
class TrooperRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.trooper)

class TrooperBulletRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.trooperBullet)

class ArcherRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.archer)
        
class FloaterRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.floater)
        
class EliteRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.crawler)
        
class HeavyRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.crawler)
        
class DragonRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.crawler)

class AnnihilatorRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.crawler)

class BeginningsP1BulletRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.beginningsP1Bullet)
        self.shader = rcache.getShader('colorshift')
        
    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        amount = float(actor.life.time) / float(actor.life.maxTime)
        with graphics.Affine(pos, rot):
            self.shader.uniformi("facing", actor.facing)
            self.shader.uniformf("alpha", 1.0)
            self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            self.shader.uniformf("colorTo", 1.0, 1.0, 0.0, 1.0)
            self.shader.uniformf("amount", amount)
            self.img.batch.draw()


class IndicatorRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.crosshair)
        self.layer = LAYER_GUI

    def renderActor(self, actor):
        targetBB = actor.target.physicsObj.getBB()

        spacing = 4
        bottomLeft = (targetBB.left - spacing, targetBB.bottom - spacing)
        bottomRight = (targetBB.right + spacing, targetBB.bottom - spacing)
        topLeft = (targetBB.left - spacing, targetBB.top + spacing)
        topRight = (targetBB.right + spacing, targetBB.top + spacing)

        self.shader.uniformi("facing", actor.facing)
        self.shader.uniformf("alpha", 1.0)
        self.shader.uniformf("vertexDiff", 0, 0, 0, 0)

        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(bottomLeft, rot):
            self.img.batch.draw()
        with graphics.Affine(bottomRight, rot):
            self.img.batch.draw()
        with graphics.Affine(topLeft, rot):
            self.img.batch.draw()
        with graphics.Affine(topRight, rot):
            self.img.batch.draw()
    

class BackgroundRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.layer = LAYER_BG
        self.parallaxFactor = 1.3

    def renderActor(self, actor):
        rot = math.degrees(actor.physicsObj.angle)
        img = actor.img

        x = actor.world.camera.currentX
        y = actor.world.camera.currentY
        pos1 = (x / self.parallaxFactor, y / self.parallaxFactor)

        self.shader.uniformi("facing", actor.facing)
        self.shader.uniformf("alpha", 1.0)
        self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        for thing in [0, 90, 180, 270]:
            with graphics.Affine(pos1, rot + thing):
                img.batch.draw()
        # Okay this particular background effects makes your eyeballs
        # strip gears...
        #pos2 = (x / (self.parallaxFactor*1.1), y / (self.parallaxFactor*1.1))
        #for thing in [0, 90, 180, 270]:
        #    with graphics.Affine(pos2, rot + thing):
        #        img.batch.draw()


        
class AirP1BulletAirRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.airP1BulletAir)

    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            self.shader.uniformi("facing", actor.facing)

            lifePercentage = actor.life.time / actor.maxTime
                        
            self.shader.uniformf("alpha", lifePercentage)
            self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            self.img.batch.draw()


class AirP1BulletGroundRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.airP1BulletGround)

    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            self.shader.uniformi("facing", actor.facing)

            lifePercentage = actor.life.time / actor.maxTime
                        
            self.shader.uniformf("alpha", lifePercentage)
            self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            self.img.batch.draw()

        
class AirP2BulletRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.numImages = 20
        self.images = [images.airP2Bullet() for _ in xrange(self.numImages)]


    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            self.shader.uniformi("facing", actor.facing)
            self.shader.uniformf("alpha", 1.0)
            self.shader.uniformf("vertexDiff", 0, 0, 0, 0)

            imagenum = actor.animationCount % self.numImages
            self.images[imagenum].batch.draw()


class BlockRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)

    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        img = actor.img
        with graphics.Affine(pos, rot):
            self.shader.uniformi("facing", actor.facing)
            self.shader.uniformf("alpha", 1.0)
            self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            img.batch.draw()

class GateRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)
        self.img = rcache.getLineImage(images.gate)

    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.rotation)
        self.shader.uniformi("facing", actor.facing)
        self.shader.uniformf("alpha", 1.0)
        self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        with graphics.Affine(pos, rot):
            self.img.batch.draw()
        with graphics.Affine(pos, -rot):
            self.img.batch.draw()

class TreeRenderer(Renderer):
    def __init__(self):
        Renderer.__init__(self)

    def renderActor(self, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        img = actor.img
        with graphics.Affine(pos, rot):
            self.shader.uniformi("facing", actor.facing)
            self.shader.uniformf("alpha", 1.0)
            self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            img.batch.draw()


class BBRenderer(Renderer):
    """Draws the bounding box of the given object.  Crude, but useful for debugging.

TODO: Make it draw together with the object's usual renderer, rather than replacing it?
Somehow.  Maybe it wraps another renderer or something.  Or it can just be a special mode
used by the main rendering loop."""
    def __init__(self):
        Renderer.__init__(self)

    def renderActor(self, actor):
        bbs = [shape.bb for shape in actor.physicsObj.shapes]
        polys = []
        for bb in bbs:
            x0 = bb.left
            x1 = bb.right
            y0 = bb.bottom
            y1 = bb.top
            x0 = min(x0, x1)
            x1 = max(x0, x1)
            y0 = min(y0, y1)
            y1 = max(y0, y1)
            p = graphics.Polygon.rectCorner(x0, y0, (x1 - x0), (y1 - y0), (255, 0, 255, 255))
            polys.append(p)
        img = graphics.LineImage(polys)

        # No affine here, the image is created with the object's
        # gameworld coordinates.
        self.shader.uniformi("facing", 1)
        self.shader.uniformf("alpha", 1.0)
        self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        img.batch.draw()
        #pos = actor.physicsObj.position
        #rot = math.degrees(actor.physicsObj.angle)
        #img = actor.img
        #with graphics.Affine(pos, rot):
        #    self.shader.uniformi("facing", actor.facing)
        #    self.shader.uniformf("alpha", 1.0)
        #    self.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        #    img.batch.draw()
        

class RenderManager(object):
    """A class that manages rendering of a set of Renderers."""
    def __init__(self, screenw, screenh):
        # A list of layers
        # Each layer contains a dict: Renderers -> RenderComponents
        # We use defaultdict to create a new empty set of RenderComponents
        # if you look up a non-existent Renderers
        self.renderers = [collections.defaultdict(set) for _ in LAYERS]
        self.screenw = screenw
        self.screenh = screenh

        self.postprocPipelineSetup(screenw, screenh)

        self.offset = 0.0

    def add(self, renderer, actor):
        layer = self.renderers[renderer.layer]
        layer[renderer].add(actor)

    def addActorIfPossible(self, actor):
        if actor.renderer is not None:
            self.add(actor.renderer, actor)
    
    def remove(self, renderer, actor):
        layer = self.renderers[renderer.layer]
        layer[renderer].remove(actor)

    def removeActorIfPossible(self, actor):
        if actor.renderer is not None:
            layer = self.renderers[actor.renderer.layer]
            if actor in layer[actor.renderer]:
                self.remove(actor.renderer, actor)
		
    def renderActors(self):
        for layer in self.renderers:
            for r, actors in layer.iteritems():
                r.renderAll(actors)

    def __del__(self):
        return
        glDeleteTextures(1, byref(self.fbo_texture))
        glDeleteFramebuffers(1, byref(self.fbo))

    def postprocPipelineSetup(self, screenx, screeny):
        self.renderSteps = []
        shader1 = rcache.getShader('postproc')
        #shader2 = rcache.getShader('postproc2')
        newStep1 = PostprocStep(shader1, self.screenw, self.screenh)
        self.renderSteps.append(newStep1)
        #newStep2 = PostprocStep(shader2, self.screenw, self.screenh)
        #self.renderSteps.append(newStep2)
        #self.ppSetup()

        # Now we set up the initial framebuffer
        # Back-buffer
        self.fbo_texture = c_uint(0)
        glActiveTexture(GL_TEXTURE0)
        glGenTextures(1, byref(self.fbo_texture))
        glBindTexture(GL_TEXTURE_2D, self.fbo_texture)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        # XXX: power-of-two textures here!
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, self.screenw, self.screenw, 0, GL_RGBA, GL_UNSIGNED_BYTE, None);
        glBindTexture(GL_TEXTURE_2D, 0);

        # Frame buffer
        self.fbo = c_uint(0)
        glGenFramebuffers(1, byref(self.fbo))
        glBindFramebuffer(GL_FRAMEBUFFER, self.fbo)
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, self.fbo_texture, 0)
        status = glCheckFramebufferStatus(GL_FRAMEBUFFER)
        if status != GL_FRAMEBUFFER_COMPLETE:
            raise Exception("Something went wrong with glCheckFramebufferStatus: {}".format(status))
        glBindFramebuffer(GL_FRAMEBUFFER, 0)

        
    def render(self, camera):
        # Render to fbo
        glBindFramebuffer(GL_FRAMEBUFFER, self.fbo)
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        # We only render with the camera here;
        # If the camera is in effect when we do all the stuff below,
        # it'll apply twice; once to the render-to-backbuffer, once to
        # the actual rendering!
        with camera:
            self.renderActors()
        glBindFramebuffer(GL_FRAMEBUFFER, 0)
        
        fromTexture = self.fbo_texture
        for step in self.renderSteps[:-1]:
            step.render(fromTexture)
            fromTexture = step.toTexture
        self.renderSteps[-1].render(fromTexture, final=True)

class PostprocStep(object):
    """A class that represents a single step in a post-processing pipeline.
It takes a texture and renders it to a new texture with a particular shader."""
    def __init__(self, shader, screenw, screenh):
        self.screenw = screenw
        self.screenh = screenh
        self.toTexture = c_uint(0)
        self.fbo = c_uint(0)
        self.shader = shader

        # Create back-buffer
        glActiveTexture(GL_TEXTURE0)
        glGenTextures(1, byref(self.toTexture))
        glBindTexture(GL_TEXTURE_2D, self.toTexture)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        # XXX: power-of-two textures here!
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, self.screenw, self.screenw, 0, GL_RGBA, GL_UNSIGNED_BYTE, None);
        glBindTexture(GL_TEXTURE_2D, 0);

        # Create frame buffer
        self.fbo = c_uint(0)
        glGenFramebuffers(1, byref(self.fbo))
        glBindFramebuffer(GL_FRAMEBUFFER, self.fbo)
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, self.toTexture, 0)
        status = glCheckFramebufferStatus(GL_FRAMEBUFFER)
        if status != GL_FRAMEBUFFER_COMPLETE:
            raise Exception("Something went wrong with glCheckFramebufferStatus: {}".format(status))
        glBindFramebuffer(GL_FRAMEBUFFER, 0)

        
        # Make a billboard to render to.
        xoff = self.screenw
        yoff = self.screenh
        bbVertsArray = c_float * 8
        self.bbVerts = bbVertsArray(
            0, 0,
            0, yoff,
            xoff, yoff,
            xoff, 0
            )
        self.bbTexCoords = bbVertsArray(
            0, 0,
            0, 1 / (4.0/3.0),
            1, 1 / (4.0/3.0),
            1, 0
            )

    def __del__(self):
        glDeleteTextures(1, byref(self.toTexture))
        glDeleteFramebuffers(1, byref(self.fbo))

    def reshape(self, screenw, screenh):
        "Rescale FBO."
        self.screenw = screenw
        self.screenh = screenh
        glBindTexture(GL_TEXTURE_2D, self.toTexture)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, self.screenw, self.screenh, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL)
        glBindTexture(GL_TEXTURE_2D, 0)
 
    def render(self, fromTexture, final=False):
        # If not the last step, render to fbo
        if not final:
            glBindFramebuffer(GL_FRAMEBUFFER, self.fbo)
        self.shader.bind()
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        #glBindTexture(GL_TEXTURE_2D, fromTexture)

        glEnableClientState(GL_VERTEX_ARRAY)
        glVertexPointer(2, GL_FLOAT, 0, byref(self.bbVerts))
        glBindTexture(GL_TEXTURE_2D, fromTexture)
        glEnableClientState(GL_TEXTURE_COORD_ARRAY)
        glTexCoordPointer(2, GL_FLOAT, 0, byref(self.bbTexCoords))
        glDrawArrays(GL_TRIANGLE_FAN, 0, 4)

        self.shader.unbind()
        if not final:
            glBindFramebuffer(GL_FRAMEBUFFER, 0)
    

def preloadRenderers():
    """Instantiates all renderers into the rcache to prevent the first lookup from lagging.
For instance AirP2BulletRenderer does relatively expensive setup work."""
    # Operates by walking down all the subclasses of Renderer and
    # adding them to the rcache.
    # Note that __subclasses__() only gets direct sublcasses, see:
    # http://stackoverflow.com/questions/5881873/python-find-all-classes-which-inherit-from-this-one

    subclasses = set()
    work = [Renderer]
    while work:
        parent = work.pop()
        for child in parent.__subclasses__():
            subclasses.add(child)
            work.append(child)

    for rendererClass in subclasses:
        rcache.getRenderer(rendererClass)
