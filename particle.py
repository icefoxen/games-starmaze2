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
        self.particles = []

    def addParticle(self, pos, color, vel, rot, age):
        # We represent particles as just tuples.
        # Tuples are pretty lightweight in Python, and are recycled on a freelist
        # internally (though the max freelist size for each tuple size is 2000 by
        # default, unless it's changed in a newer version).
        self.particles.append((pos, color, vel, rot, age))

class ParticleController(object):
    """A thing that updates a ParticleGroup."""
    def __init__(s):
        pass

    def updateParticle(self, pos, color, vel, rot, age, dt):
        px, py = pos
        vx, vy = vel
        newPos = physics.Vec(px + vx, py + vy)
        return (newPos, color, vel, rot, age+dt)
    
    def update(self, group, dt):
        group.particles = [self.updateParticle(pos, color, vel, rot, age, dt)
                           for (pos, color, vel, rot, age) in group.particles]
        if random.random() < 0.01:
            print 'Number of particles: ', len(group.particles)
            
class ParticleRenderer(object):
    """A thing that draws a ParticleGroup."""
    def __init__(s):
        self.pimage = rcache.get_image("playertest")
        self.batch = pyglet.graphics.Batch()
        # We generate more sprites as necessary and reuse them.
        # That way (ideally) Pyglet draws 'em all with one draw call.
        self.psprites = []

    def draw(self, group):
        #print(len(s.psprites))
        while len(self.psprites) < len(group.particles):
            self.psprites.append(pyglet.sprite.Sprite(self.pimage, batch=self.batch))
        for particle, sprite in itertools.izip(group.particles, self.psprites):
            (pos, color, vel, rot, age) = particle
            sprite.position = pos
        self.batch.draw()



        
class ParticleEmitter(object):
    """A thing that regularly adds particles to a ParticleGroup."""
    def __init__(s):
        pass

    def update(self, group, dt):
        if random.random() < 1.0:
            pos = physics.Vec(400, 400)
            vel = physics.Vec(random.random(), random.random())
            group.addParticle(pos, (255,255,0,255), vel, 0, 0.0)
