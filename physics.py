# First, vectors
from collections import namedtuple
import math

ZEROVEC = (0.0, 0.0)
I = (1.0, 0.0)
J = (0.0, 1.0)
PIOVER2 = math.pi / 2
PIOVER4 = math.pi / 4

class Vec(namedtuple("Vec", ["x", "y"])):
    "A vector."
    def __init__(s, x, y):
        super(Vec, s).__init__(float(x), float(y))
    
    def __add__(s, other):
        nx = s.x + other.x
        ny = s.y + other.y
        return Vec(nx,ny)

    def __sub__(s, other):
        nx = s.x - other.x
        ny = s.y - other.y
        return Vec(nx,ny)

    def __mul__(s, scale):
        nx = s.x * scale
        ny = s.y * scale
        return Vec(nx, ny)

    def __div__(s, scale):
        nx = s.x / scale
        ny = s.y / scale
        return Vec(nx, ny)

    def __invert__(s):
        return Vec(-s.x, -s.y)

    def magSquared(s):
        return s.x * s.x + s.y * s.y

    def mag(s):
        return math.sqrt(s.magSquared())

    def unit(s):
        m = s.mag()
        return s / m

    def dot(s, other):
        return (s.x * other.x) + (s.y * other.y)

    # Angle is in degrees, increasing cw,
    # with the origin facing up.
    @staticmethod
    def fromAngle(angle):
        rads = math.radians(angle)
        x = math.sin(rads)
        y = math.cos(rads)
        return Vec(x, y)

    def toAngle(s):
        return -math.degrees(math.atan2(s.y, s.x))+90

    def __int__(s):
        return Vec(int(s.x), int(s.y))

    # Constrains the vector's magnitude to the value given.
    def cap(s, magnitude):
        magSquared = magnitude * magnitude
        thisMagSquared = s.magSquared()
        if thisMagSquared > magSquared:
            v2 = s.unit()
            return v2 * magnitude
        else:
            return v

    # Returns true if the points v1 and v2 are within dist units of each other
    def within(s, v2, dist):
        d = s - v2
        return d.magSquared() < (dist * dist)

    # Returns a vector perpendicular to the given one
    # In no particular direction.
    def perpendicular(s):
        return Vec(s.y, -s.x)


    # Returns angle between two vectors.
    # Fails for zero vectors, but we shouldn't have those anyway.
    def angleBetween(s, v2):
        return math.degrees(math.atan2(v2.y, v2.x) - math.atan2(s.y, s.x))

    # Returns true if the two vectors are within the given number
    # of degrees from each other
    def angleWithin(s, v2, angle):
        angle /= 2
        ang = s.angleBetween(v2)
        return (ang <= angle) or ((360 - ang) <= angle)

    def rotate(v, angle):
        rads = math.radians(angle)
        ca = math.cos(rads)
        sa = math.sin(rads)
        x = v[0] * ca - v[1] * sa
        y = v[0] * sa + v[1] * ca
        return new(x, y)

class BBox(namedtuple("BBox", ["x", "y", "w", "h"])):
    "A bounding box."
    def overlapping(s, other):
        return (s.x < other.x < (s.x + s.w) or
                other.x < s.x < (other.x + other.w) or
                s.y < other.y < (s.y + s.h) or
                other.y < s.y < (other.y + other.h))

    # XXX: Incomplete.  Also, wrong.
    def intersection(s, other):
        if not s.overlapping(other):
            return None
        else:
            ix = other.x - s.x
            iy = other.y - s.y
            return BBox(ix, iy, 0, 0)

class Space(object):
    def __init__(s):
        s.physicsObjs = set()
        s.gravity = ZEROVEC

    def add(s, physicsObj):
        s.physicsObjs.add(physicsObj)

    def remove(s, physicsObj):
        s.physicsObjs.remove(physicsObj)

    def step(s, dt):
        for o in s.physicsObjs:
            if o.mass is not None:
                #print "BEFORE", o.velocity
                o.apply_impulse(s.gravity * dt)
                o.update(dt)
                #print "AFTER", o.velocity
        
    def add_collision_handler(group1, group2, begin=None, separate=None):
        pass
