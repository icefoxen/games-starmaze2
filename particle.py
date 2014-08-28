# Testing out how well a native Python particle system works.
# Conclusion: It works more or less okay, I guess.  <500 particles
# is no problem... on my Intel i5.
# The major performance hits seem to be the sheer number of draw and
# update calls, alas.
import itertools
import random

import pyglet

import component
import graphics
import images
import rcache
import physics

class ParticleGroup(object):
    """A set of particles that tracks and updates itself.  All particles in the system obey the same rules,
given by a Controller."""
    def __init__(s):
        s.particles = []

    def addParticle(s, pos, color, vel, rot, age):
        # We represent particles as just tuples.
        # Tuples are pretty lightweight in Python, and are recycled on a freelist
        # internally (though the max freelist size for each tuple size is 2000 by
        # default, unless it's changed in a newer version).
        s.particles.append((pos, color, vel, rot, age))

class ParticleController(object):
    """A thing that updates a ParticleGroup."""
    def __init__(s):
        pass

    def updateParticle(s, pos, color, vel, rot, age, dt):
        px, py = pos
        vx, vy = vel
        newPos = physics.Vec(px + vx, py + vy)
        return (newPos, color, vel, rot, age+dt)
    
    def update(s, group, dt):
        group.particles = [s.updateParticle(pos, color, vel, rot, age, dt)
                           for (pos, color, vel, rot, age) in group.particles]

class ParticleRenderer(object):
    """A thing that draws a ParticleGroup."""
    def __init__(s):
        s.pimage = rcache.get_image("playertest")
        s.batch = pyglet.graphics.Batch()
        s.psprites = []

    def draw(s, group):
        #print(len(s.psprites))
        while len(s.psprites) < len(group.particles):
            s.psprites.append(pyglet.sprite.Sprite(s.pimage, batch=s.batch))
        for particle, sprite in itertools.izip(group.particles, s.psprites):
            (pos, color, vel, rot, age) = particle
            sprite.position = pos
        s.batch.draw()

class ParticleEmitter(object):
    """A thing that regularly adds particles to a ParticleGroup."""
    def __init__(s):
        pass

    def update(s, group, dt):
        if random.random() < 1.0:
            pos = physics.Vec(400, 400)
            vel = physics.Vec(random.random(), random.random())
            group.addParticle(pos, (255,255,0,255), vel, 0, 0.0)
