import collections
import math

import pyglet
from pyglet.gl import *

import rcache
import images
import shader
import graphics

# So the world is going to have a number of renderers

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
        s.layer = 0
        s.shader = shader.Shader([shader.vprog], [shader.fprog])
		
    def __lt__(s, other):
        return s.layer < other.layer

    def renderStart(s):
        glPushAttrib(GL_COLOR_BUFFER_BIT)
        glEnable(GL_BLEND)
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
        s.shader.bind()
        
    def renderFinish(s):
        s.shader.unbind()
        glPopAttrib()

    # XXX: For now this assumes we're using the sorta default-ish shader...
    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)
            s.shader.uniformf("alpha", 1.0)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            s.shader.uniformf("colorDiff", 0, 0, 0, 0)
            s.img.batch.draw()
        
    def renderAll(s, actors):
        s.renderStart()
        for act in actors:
            s.renderActor(act)
        s.renderFinish()

class LineSpriteRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.shader = shader.DummyShader()

    def renderActor(s, actor):
        sp = actor.sprite
        actor.draw(s.shader)
        with graphics.Affine((sp._x, sp._y), sp.rotation, (sp._scale, sp._scale)):
            # For now, this updates the sprite's position and shader props and such
            sp._batch.draw()



class PlayerRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)

        s.img = rcache.getLineImage(images.playerImage)
        
        # Experimental glow effect, just overlay the sprite
        # with a diffuse, alpha-blended sprite.  Works surprisingly well.
        s.glowImage = rcache.getLineImage(images.playerImageGlow)


    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        with graphics.Affine(pos, rot):
            s.shader.uniformi("facing", actor.facing)
            s.shader.uniformf("alpha", 1.0)
            s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
            s.shader.uniformf("colorDiff", 0, 0, 0, 0)
            s.img.batch.draw()
            
            glow = -0.3 * abs(math.sin(actor.glow))
            s.shader.uniformf("vertexDiff", 0, 0, 0.0, glow)
            s.shader.uniformf("colorDiff", 0, 0, 0, glow)

            # XXX
            actor.powers.draw(s.shader)
            
            s.shader.uniformf("alpha", 0.2)
            #s.glowImage.position = s.physicsObj.position
            s.glowImage.batch.draw()

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

class BeginningsP1BulletRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.beginningsP1Bullet)

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
            s.shader.uniformf("colorDiff", 0, 0, 0, 0)
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
            s.shader.uniformf("colorDiff", 0, 0, 0, 0)
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
            s.shader.uniformf("colorDiff", 0, 0, 0, 0)

            imagenum = actor.animationCount % s.numImages
            s.images[imagenum].batch.draw()
        
class RenderManager(object):
    """A class that manages rendering of a set of Renderers."""
    def __init__(s):
        # A map of Renderers -> RenderComponents, or something
        s.renderers = collections.defaultdict(set)
        # __missing__ is a function that gets called without arguments
        # to provide a default value for a missing key; this default value
        # is then inserted as the value for said key.
        # So if we look up a renderer that doesn't exist, it automatically
        # gets added to the dict with an empty set as the value.
        #s.renderers.__missing__ = set

    def add(s, renderer, actor):
        s.renderers[renderer].add(actor)
		
    def remove(s, renderer, actor):
        s.renderers[renderer].remove(actor)
		
    def render(s):
        # Oops, well, we'll deal with layers some other way then.
        #s.renderers.sort()
        for r, actors in s.renderers.iteritems():
            r.renderAll(actors)


def preloadRenderers():
    """Instantiates all renderers into the rcache to prevent the first lookup from lagging.
For instance AirP2BulletRenderer does a lot of setup work."""
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
