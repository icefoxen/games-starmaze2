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
        s.img = rcache.getLineImage(images.crawler)
        
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

class BackgroundRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)

    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        img = actor.img

        s.shader.uniformi("facing", actor.facing)
        s.shader.uniformf("alpha", 1.0)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        s.shader.uniformf("colorDiff", 0, 0, 0, 0)
        for thing in [0, 90, 180, 270]:
            with graphics.Affine(pos, rot + thing):
                img.batch.draw()


        
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
            s.shader.uniformf("colorDiff", 0, 0, 0, 0)
            img.batch.draw()

class DoorRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.img = rcache.getLineImage(images.door)

    def renderActor(s, actor):
        pos = actor.physicsObj.position
        rot = math.degrees(actor.physicsObj.angle)
        s.shader.uniformi("facing", actor.facing)
        s.shader.uniformf("alpha", 1.0)
        s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        s.shader.uniformf("colorDiff", 0, 0, 0, 0)
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
            s.shader.uniformf("colorDiff", 0, 0, 0, 0)
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
        s.shader.uniformf("colorDiff", 0, 0, 0, 0)
        img.batch.draw()
        #pos = actor.physicsObj.position
        #rot = math.degrees(actor.physicsObj.angle)
        #img = actor.img
        #with graphics.Affine(pos, rot):
        #    s.shader.uniformi("facing", actor.facing)
        #    s.shader.uniformf("alpha", 1.0)
        #    s.shader.uniformf("vertexDiff", 0, 0, 0, 0)
        #    s.shader.uniformf("colorDiff", 0, 0, 0, 0)
        #    img.batch.draw()
        
            
class RenderManager(object):
    """A class that manages rendering of a set of Renderers."""
    def __init__(s):
        # A map of Renderers -> RenderComponents, or something
        # We use defaultdict to create a new empty set of RenderComponents
        # if you look up a non-existent Renderers
        s.renderers = collections.defaultdict(set)

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
For instance AirP2BulletRenderer does relatively expensive setup work."""
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
