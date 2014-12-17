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
    def __init__(s):
        s.layer = LAYER_FG
        s.shader = rcache.getShader('default')

    def __lt__(s, other):
        return s.layer < other.layer

    # These are usually-sane defaults for renderStart, renderFinish and renderActor,
    # but feel free to override them if you want.
    def renderStart(s):
        glPushAttrib(GL_COLOR_BUFFER_BIT)
        glEnable(GL_BLEND)
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
        s.shader.bind()
        
    def renderFinish(s):
        s.shader.unbind()
        glPopAttrib()

    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)
            s.shader.uniformf("alpha", 1.0)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            s.img.batch.draw()
        
    def renderAll(s, actors):
        s.renderStart()
        for act in actors:
            s.renderActor(act)
        s.renderFinish()

class GUIRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.layer = LAYER_GUI
        s.lifeBarImage = rcache.getLineImage(images.lifeBar)
        s.energyBarImage = rcache.getLineImage(images.energyBar)

    def renderActor(s, actor):
        player = actor.player
        lifeFraction = player.life.life / player.life.maxLife
        # TODO: Energy doesn't exist yet.
        energyFraction = player.energy.energy / player.energy.maxEnergy
        
        s.shader.uniformi("facing", 1)
        s.shader.uniformf("alpha", 1.0)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)


        x = actor.world.camera.currentX
        y = actor.world.camera.currentY
        
        with graphics.Affine((x + -100, y - 378), 0.0, (lifeFraction, 1.0)):
            s.lifeBarImage.batch.draw()

        with graphics.Affine((x + 100, y - 378), 0.0, (energyFraction, 1.0)):
            s.energyBarImage.batch.draw()

        
class SpriteRenderer(Renderer):
    """A renderer that just draws a bitmap sprite.

CURRENTLY EXPERIMENTAL."""
    def __init__(s):
        Renderer.__init__(s)
        s.image = rcache.get_image("playertest")
        s.batch = pyglet.graphics.Batch()
        s.sprite = pyglet.sprite.Sprite(s.image, batch=s.batch)
        s.shader = rcache.getShader('texture')
        s.shader = rcache.getShader('bloom')

    def renderActor(s, actor):
        s.shader.uniformi("facing", 1)
        s.shader.uniformf("alpha", 1.0)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            s.batch.draw()


class PlayerRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)

        s.img = rcache.getLineImage(images.player)
        
        # Experimental glow effect, just overlay the sprite
        # with a diffuse, alpha-blended sprite.  Works surprisingly well.
        s.glowImage = rcache.getLineImage(images.playerGlow)


    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)
            s.shader.uniformf("alpha", 1.0)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            s.img.batch.draw()
            
            # XXX
            actor.powers.draw(s.shader)
            
            glow = -0.3 * abs(math.sin(actor.glow))
            s.shader.uniformf("vertexDiff", 0, 0, 0.0, glow)

            s.shader.uniformf("alpha", 0.2)
            s.glowImage.batch.draw()

# Having a renderer that does nothing but draw a particular image seems a bit lame
# But it's certainly the most flexible method, so we're sticking with it.
class CollectableRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.collectable)

class PowerupRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.powerup)

class CrawlerRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.crawler)
        
class TrooperRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.trooper)

class TrooperBulletRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.trooperBullet)

class ArcherRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.archer)
        
class FloaterRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.floater)
        
class EliteRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.crawler)
        
class HeavyRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.crawler)
        
class DragonRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.crawler)

class AnnihilatorRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.crawler)

class BeginningsP1BulletRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.beginningsP1Bullet)
        s.shader = rcache.getShader('colorshift')
        
    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        amount = float(actor.life.time) / float(actor.life.maxTime)
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)
            s.shader.uniformf("alpha", 1.0)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            s.shader.uniformf("colorTo", 1.0, 1.0, 0.0, 1.0)
            s.shader.uniformf("amount", amount)
            s.img.batch.draw()


class IndicatorRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.crosshair)
        s.layer = LAYER_GUI

    def renderActor(s, actor):
        targetBB = actor.target.physicsObj.getBB()

        spacing = 4
        bottomLeft = (targetBB.left - spacing, targetBB.bottom - spacing)
        bottomRight = (targetBB.right + spacing, targetBB.bottom - spacing)
        topLeft = (targetBB.left - spacing, targetBB.top + spacing)
        topRight = (targetBB.right + spacing, targetBB.top + spacing)

        s.shader.uniformi("facing", actor.facing)
        s.shader.uniformf("alpha", 1.0)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)

        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(bottomLeft, rot):
            s.img.batch.draw()
        with graphics.Affine(bottomRight, rot):
            s.img.batch.draw()
        with graphics.Affine(topLeft, rot):
            s.img.batch.draw()
        with graphics.Affine(topRight, rot):
            s.img.batch.draw()
    

class BackgroundRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.layer = LAYER_BG
        s.parallaxFactor = 1.3

    def renderActor(s, actor):
        rot = math.degrees(actor.physicsObj.angle)
        img = actor.img

        x = actor.world.camera.currentX
        y = actor.world.camera.currentY
        pos1 = (x / s.parallaxFactor, y / s.parallaxFactor)

        s.shader.uniformi("facing", actor.facing)
        s.shader.uniformf("alpha", 1.0)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        for thing in [0, 90, 180, 270]:
            with graphics.Affine(pos1, rot + thing):
                img.batch.draw()
        # Okay this particular background effects makes your eyeballs
        # strip gears...
        #pos2 = (x / (s.parallaxFactor*1.1), y / (s.parallaxFactor*1.1))
        #for thing in [0, 90, 180, 270]:
        #    with graphics.Affine(pos2, rot + thing):
        #        img.batch.draw()


        
class AirP1BulletAirRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.airP1BulletAir)

    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)

            lifePercentage = actor.life.time / actor.maxTime
                        
            s.shader.uniformf("alpha", lifePercentage)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            s.img.batch.draw()


class AirP1BulletGroundRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.airP1BulletGround)

    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)

            lifePercentage = actor.life.time / actor.maxTime
                        
            s.shader.uniformf("alpha", lifePercentage)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            s.img.batch.draw()

        
class AirP2BulletRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.numImages = 20
        s.images = [images.airP2Bullet() for _ in xrange(s.numImages)]


    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)
            s.shader.uniformf("alpha", 1.0)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)

            imagenum = actor.animationCount % s.numImages
            s.images[imagenum].batch.draw()


class BlockRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)

    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        img = actor.img
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)
            s.shader.uniformf("alpha", 1.0)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            img.batch.draw()

class GateRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.gate)

    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.rotation)
        s.shader.uniformi("facing", actor.facing)
        s.shader.uniformf("alpha", 1.0)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        with graphics.Affine(pos, rot):
            s.img.batch.draw()
        with graphics.Affine(pos, -rot):
            s.img.batch.draw()

class TreeRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)

    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        img = actor.img
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)
            s.shader.uniformf("alpha", 1.0)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            img.batch.draw()


class BBRenderer(Renderer):
    """Draws the bounding box of the given object.  Crude, but useful for debugging.

TODO: Make it draw together with the object's usual renderer, rather than replacing it?
Somehow.  Maybe it wraps another renderer or something.  Or it can just be a special mode
used by the main rendering loop."""
    def __init__(s):
        Renderer.__init__(s)

    def renderActor(s, actor):
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
        s.shader.uniformi("facing", 1)
        s.shader.uniformf("alpha", 1.0)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        img.batch.draw()
        #pos = actor.physicsObj.position
        #rot = math.degrees(actor.physicsObj.angle)
        #img = actor.img
        #with graphics.Affine(pos, rot):
        #    s.shader.uniformi("facing", actor.facing)
        #    s.shader.uniformf("alpha", 1.0)
        #    s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        #    img.batch.draw()
        

class RenderManager(object):
    """A class that manages rendering of a set of Renderers."""
    def __init__(s, screenw, screenh):
        # A list of layers
        # Each layer contains a dict: Renderers -> RenderComponents
        # We use defaultdict to create a new empty set of RenderComponents
        # if you look up a non-existent Renderers
        s.renderers = [collections.defaultdict(set) for _ in LAYERS]
        s.screenw = screenw
        s.screenh = screenh

        s.postprocPipelineSetup(screenw, screenh)

        s.offset = 0.0

    def add(s, renderer, actor):
        layer = s.renderers[renderer.layer]
        layer[renderer].add(actor)

    def addActorIfPossible(s, actor):
        if actor.renderer is not None:
            s.add(actor.renderer, actor)
    
    def remove(s, renderer, actor):
        layer = s.renderers[renderer.layer]
        layer[renderer].remove(actor)

    def removeActorIfPossible(s, actor):
        if actor.renderer is not None:
            layer = s.renderers[actor.renderer.layer]
            if actor in layer[actor.renderer]:
                s.remove(actor.renderer, actor)
		
    def renderActors(s):
        for layer in s.renderers:
            for r, actors in layer.iteritems():
                r.renderAll(actors)

    def __del__(s):
        return
        glDeleteTextures(1, byref(s.fbo_texture))
        glDeleteFramebuffers(1, byref(s.fbo))

    def postprocPipelineSetup(s, screenx, screeny):
        s.renderSteps = []
        shader1 = rcache.getShader('postproc')
        #shader2 = rcache.getShader('postproc2')
        newStep1 = PostprocStep(shader1, s.screenw, s.screenh)
        s.renderSteps.append(newStep1)
        #newStep2 = PostprocStep(shader2, s.screenw, s.screenh)
        #s.renderSteps.append(newStep2)
        #s.ppSetup()

        # Now we set up the initial framebuffer
        # Back-buffer
        s.fbo_texture = c_uint(0)
        glActiveTexture(GL_TEXTURE0)
        glGenTextures(1, byref(s.fbo_texture))
        glBindTexture(GL_TEXTURE_2D, s.fbo_texture)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        # XXX: power-of-two textures here!
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, s.screenw, s.screenw, 0, GL_RGBA, GL_UNSIGNED_BYTE, None);
        glBindTexture(GL_TEXTURE_2D, 0);

        # Frame buffer
        s.fbo = c_uint(0)
        glGenFramebuffers(1, byref(s.fbo))
        glBindFramebuffer(GL_FRAMEBUFFER, s.fbo)
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, s.fbo_texture, 0)
        status = glCheckFramebufferStatus(GL_FRAMEBUFFER)
        if status != GL_FRAMEBUFFER_COMPLETE:
            raise Exception("Something went wrong with glCheckFramebufferStatus: {}".format(status))
        glBindFramebuffer(GL_FRAMEBUFFER, 0)

        
    def render(s, camera):
        # Render to fbo
        glBindFramebuffer(GL_FRAMEBUFFER, s.fbo)
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        # We only render with the camera here;
        # If the camera is in effect when we do all the stuff below,
        # it'll apply twice; once to the render-to-backbuffer, once to
        # the actual rendering!
        with camera:
            s.renderActors()
        glBindFramebuffer(GL_FRAMEBUFFER, 0)
        
        fromTexture = s.fbo_texture
        for step in s.renderSteps[:-1]:
            step.render(fromTexture)
            fromTexture = step.toTexture
        s.renderSteps[-1].render(fromTexture, final=True)

class PostprocStep(object):
    """A class that represents a single step in a post-processing pipeline.
It takes a texture and renders it to a new texture with a particular shader."""
    def __init__(s, shader, screenw, screenh):
        s.screenw = screenw
        s.screenh = screenh
        s.toTexture = c_uint(0)
        s.fbo = c_uint(0)
        s.shader = shader

        # Create back-buffer
        glActiveTexture(GL_TEXTURE0)
        glGenTextures(1, byref(s.toTexture))
        glBindTexture(GL_TEXTURE_2D, s.toTexture)
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
        # XXX: power-of-two textures here!
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, s.screenw, s.screenw, 0, GL_RGBA, GL_UNSIGNED_BYTE, None);
        glBindTexture(GL_TEXTURE_2D, 0);

        # Create frame buffer
        s.fbo = c_uint(0)
        glGenFramebuffers(1, byref(s.fbo))
        glBindFramebuffer(GL_FRAMEBUFFER, s.fbo)
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, s.toTexture, 0)
        status = glCheckFramebufferStatus(GL_FRAMEBUFFER)
        if status != GL_FRAMEBUFFER_COMPLETE:
            raise Exception("Something went wrong with glCheckFramebufferStatus: {}".format(status))
        glBindFramebuffer(GL_FRAMEBUFFER, 0)

        
        # Make a billboard to render to.
        xoff = s.screenw
        yoff = s.screenh
        bbVertsArray = c_float * 8
        s.bbVerts = bbVertsArray(
            0, 0,
            0, yoff,
            xoff, yoff,
            xoff, 0
            )
        s.bbTexCoords = bbVertsArray(
            0, 0,
            0, 1 / (4.0/3.0),
            1, 1 / (4.0/3.0),
            1, 0
            )

    def __del__(s):
        glDeleteTextures(1, byref(s.toTexture))
        glDeleteFramebuffers(1, byref(s.fbo))

    def reshape(s, screenw, screenh):
        "Rescale FBO."
        s.screenw = screenw
        s.screenh = screenh
        glBindTexture(GL_TEXTURE_2D, s.toTexture)
        glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, s.screenw, s.screenh, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL)
        glBindTexture(GL_TEXTURE_2D, 0)
 
    def render(s, fromTexture, final=False):
        # If not the last step, render to fbo
        if not final:
            glBindFramebuffer(GL_FRAMEBUFFER, s.fbo)
        s.shader.bind()
        glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT)
        #glBindTexture(GL_TEXTURE_2D, fromTexture)

        glEnableClientState(GL_VERTEX_ARRAY)
        glVertexPointer(2, GL_FLOAT, 0, byref(s.bbVerts))
        glBindTexture(GL_TEXTURE_2D, fromTexture)
        glEnableClientState(GL_TEXTURE_COORD_ARRAY)
        glTexCoordPointer(2, GL_FLOAT, 0, byref(s.bbTexCoords))
        glDrawArrays(GL_TRIANGLE_FAN, 0, 4)

        s.shader.unbind()
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
