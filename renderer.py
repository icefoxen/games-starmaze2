import collections

import pyglet
from pyglet.gl import *

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
		
    def __lt__(s, other):
        return s.layer < other.layer

    def renderStart(s):
        pass
    def renderFinish(s):
        pass

    def renderActor(s, actor):
        pass
		
    def renderAll(s, actors):
        s.renderStart()
        for act in actors:
            s.renderActor(act)
        s.renderFinish()

class LineSpriteRenderer(Renderer):
    def __init__(s):
        Renderer.__init__(s)
        s.shader = shader.DummyShader()

    def renderStart(s):
        glPushAttrib(GL_COLOR_BUFFER_BIT)
        glEnable(GL_BLEND)
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA)
        s.shader.bind()

    def renderActor(s, actor):
        sp = actor.sprite
        with graphics.Affine((sp._x, sp._y), sp.rotation, (sp._scale, sp._scale)):
            # For now, this updates the sprite's position and shader props and such
            actor.draw(s.shader)
            sp._batch.draw()

    def renderFinish(s):
        s.shader.unbind()
        glPopAttrib()

LINESPRITERENDERER = LineSpriteRenderer()
		
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
