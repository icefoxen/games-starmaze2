import math
import random

from graphics import *

def player():
    """Returns a new LineImage with an image of the player."""
    polyList = []
    radius = 10
    pcol = (64, 224, 64, 255)
    polyList.append(Polygon.circle(0, 0, radius, pcol, numSegments=16))

    spokeLength = radius + 9
    spokeBase = 4
    polyList.append(Polygon.line(0, spokeBase, spokeLength, 0, pcol))
    polyList.append(Polygon.line(0, -spokeBase, spokeLength, 0, pcol))
    polyList.append(Polygon.line(spokeBase, 0, 0, spokeLength, pcol))
    polyList.append(Polygon.line(-spokeBase, 0, 0, spokeLength, pcol))
    polyList.append(Polygon.line(0, spokeBase, -spokeLength, 0, pcol))
    polyList.append(Polygon.line(0, -spokeBase, -spokeLength, 0, pcol))
    polyList.append(Polygon.line(spokeBase, 0, 0, -spokeLength, pcol))
    polyList.append(Polygon.line(-spokeBase, 0, 0, -spokeLength, pcol))

    image = LineImage(polyList)
    return image

def shieldImage():
    polys = []
    polys.append(Polygon.circle(0, 0, 15, (200, 200, 255, 172), strokeWidth=10))
    return LineImage(polys)

def playerGlow():

    polyList = []
    radius = 10
    pcol = (64, 224, 64, 255)
    polyList.append(Polygon.circle(0, 0, radius, pcol, numSegments=128, strokeWidth=30))

    #spokeLength = radius + 18
    #spokeBase = 8
    #polyList.append(Polygon.line(0, spokeBase, spokeLength, 0, pcol, strokeWidth=20))
    #polyList.append(Polygon.line(0, -spokeBase, spokeLength, 0, pcol, strokeWidth=20))
    #polyList.append(Polygon.line(spokeBase, 0, 0, spokeLength, pcol, strokeWidth=20))
    #polyList.append(Polygon.line(-spokeBase, 0, 0, spokeLength, pcol, strokeWidth=20))
    #polyList.append(Polygon.line(0, spokeBase, -spokeLength, 0, pcol, strokeWidth=20))
    #polyList.append(Polygon.line(0, -spokeBase, -spokeLength, 0, pcol, strokeWidth=20))
    #polyList.append(Polygon.line(spokeBase, 0, 0, -spokeLength, pcol, strokeWidth=20))
    #polyList.append(Polygon.line(-spokeBase, 0, 0, -spokeLength, pcol, strokeWidth=20))

    image = LineImage(polyList)
    return image

def beginningsP1Bullet():
    #p = Polygon.line(10, 0, -10, 0, (192, 0, 0, 255), strokeWidth=4)
    length = 5
    vs = [
        Vertex(length, 0, (255,0,0,255)),
        Vertex(-length, 0, (192,0,0,128))
        ]
    p = Polygon(vs, closed=False, strokeWidth=4)
    return LineImage([p])

def airP1BulletAir():
    color = (0, 0, 255, 255)
    shadeColor = (128, 128, 255, 128)
    radius = 40
    polyList = [Polygon.arc(-radius/2, 0, radius, 90, color, startAngle=135, closed=False),
                Polygon.arc(-radius/2, 0, radius, 90, shadeColor, startAngle=135, strokeWidth=15, closed=False),
                ]
    return LineImage(polyList)
                

def airP1BulletGround():
    color = (0, 0, 255, 255)
    shadeColor = (128, 128, 255, 128)
    radius = 40
    angle = 60
    polyList = [Polygon.arc(-radius/2, 0, radius, angle, color, startAngle=120, closed=False),
                Polygon.arc(-radius/2, 0, radius, angle, shadeColor, startAngle=120, strokeWidth=15, closed=False),
                ]
    return LineImage(polyList)

def airP2Bullet():
    color1 = (224, 224, 255, 255)
    color2 = (0, 0, 255, 128)
    shadeColor1 = (224, 225, 255, 128)
    shadeColor2 = (0, 0, 128, 32)
    length = 200
    yoff = 25
    xoff = 25
    xoff1 = random.random() * xoff - (xoff/2)
    xoff2 = random.random() * xoff - (xoff/2)
    yoff1 = random.random() * yoff - (yoff/2)
    yoff2 = random.random() * yoff - (yoff/2)
    v1 = Vertex(0, 0, color1)
    v2 = Vertex(length + xoff1, yoff1, color2)
    sv1 = Vertex(0, 0, shadeColor1)
    sv2 = Vertex(length + xoff2, yoff2, shadeColor2)
    polyList1 = jaggifyLine(v1, v2, 5, strokeWidth=2)
    polyList2 = jaggifyLine(v1, v2, 5, strokeWidth=2)
    polyList3 = jaggifyLine(sv1, sv2, 5, strokeWidth=5)
    polyList4 = jaggifyLine(sv1, sv2, 5, strokeWidth=5)
    return LineImage(polyList1 + polyList2 + polyList3 + polyList4)

def gate():
    poly = Polygon.rectCenter(0, 0, 20, 20, (128, 128, 255, 255))
    return LineImage([poly])

def powerup():
    poly = Polygon.rectCenter(0, 0, 5, 5, (192, 0, 0, 255))
    return LineImage([poly])

def crawler():
    polyList = []
    color = (192, 192, 192, 255)
    polyList.append(Polygon.arc(0, 0, 8, 180, color, startAngle=90.0, numSegments=6))
    #polyList.append(cornersToLines(circleCorners(0, 0, 15, numSegments=6, color)))
    polyList.append(Polygon.line(0, 0, 10, 10, color))
    polyList.append(Polygon.line(0, 0, -10, 10, color))
    polyList.append(Polygon.line(0, 0, 5, 12, color))
    polyList.append(Polygon.line(0, 0, -5, 12, color))
    polyList.append(Polygon.line(0, 0, 15, 5, color))
    polyList.append(Polygon.line(0, 0, -15, 5, color))

    return LineImage(polyList)

def trooper():
    polys = []
    color = (224, 192, 192, 255)
    polys.append(Polygon.rectCenter(0, 0, 15, 25, color))
    polys.append(Polygon.rectCenter(5, 0, 15, 5, color))
    polys.append(Polygon.line(-7, 15, -7, 22, color))
    polys.append(Polygon.line(-7, 22, 10, 12, color))
    polys.append(Polygon.line(10, 12, -7, 15, color))

    return LineImage(polys)

# BUGGO: I really want a solid rect here...
def trooperBullet():
    polys = []
    color = (255, 224, 0, 255)
    polys.append(Polygon.rectCenter(0, 0, 3, 3, color))

    return LineImage(polys)

def archer():
    polys = []
    color = (192, 224, 192, 255)
    polys.append(Polygon.rectCenter(0, 0, 25, 12, color))
    polys.append(Polygon.line(0, 0, 17, 17, color))
    polys.append(Polygon.line(0, 0, -17, 17, color))

    return LineImage(polys)

def floater():
    polys = []
    color = (192, 192, 224, 255)
    size = 15
    polys.append(Polygon.rectCenter(0, 0, size, size, color))
    polys.append(Polygon.circle(0, 0, size, color, numSegments=4))
    polys.append(Polygon.line(-size, -size,  size,  size, color))
    polys.append(Polygon.line(-size,  size,  size, -size, color))

    return LineImage(polys)


def collectable():
    color = (192, 0, 0, 255)
    polyList = []
    polyList.append(Polygon.rectCorner(-10, -5, 20, 10, color))
    polyList.append(Polygon.rectCorner(-5, -10, 10, 20, color))
    image = LineImage(polyList)
    return image

# OPT: This might be worth optimizing
def jaggifyLine(v1, v2, numSegments, **kwargs):
    """Returns a Polygon that's a randomly jaggy line connecting the two verts."""
    def divideLine(v1, v2, recursion = 0):
        jagAmount = 0.3 + random.random() * 0.4
        jagWidth = 0.3 * random.random()
        midpoint = lerpVertex(v1, v2, jagAmount)
        midpointXOff = midpoint.x - v1.x
        midpointYOff = midpoint.y - v1.y
        midpointNormalX, midpointNormalY = (midpointYOff, midpointXOff)
        midpointNormalX *= jagWidth
        midpointNormalY *= jagWidth
        offsetMidpointX = midpoint.x + (midpointNormalX * random.choice([-1, 1]))
        offsetMidpointY = midpoint.y + (midpointNormalY * random.choice([-1, 1]))
        # XXX Direction of the jag is constant here...

        midpointVert = Vertex(offsetMidpointX, offsetMidpointY, midpoint.color)
        if recursion == 0:
            l1 = Polygon([v1, midpointVert], closed=False, **kwargs)
            l2 = Polygon([midpointVert, v2], closed=False, **kwargs)
            return [l1, l2]
        else:
            l1s = divideLine(v1, midpointVert, recursion=recursion-1)
            l2s = divideLine(midpointVert, v2, recursion=recursion-1)
            return l1s + l2s
    return divideLine(v1, v2, numSegments)

def tree():
    # Not really a perfect tree-building algorithm...
    # Angle change should be a delta off of what the previous angle is
    # We might want a way to gradiate colors more finely, choose where
    # to break the line (instead of the midpoint), change the number
    # of branches, and possibly other things
    # But, not a bad start.
    # Actually looking up some fractal tree-building algorithms might
    # be useful, you know.
    def divideLine(v1, v2, anglechange, recursion = 0):
        lengthScale = 0.9
        finishColor = (255, 0, 0, 255)
        ranglechange = math.radians(anglechange)

        midpoint = lerpVertex(v1, v2, 0.5)
        
        angleChange1 = random.random() * ranglechange
        angleChange2 = random.random() * -ranglechange

        piover2 = math.pi / 2
        length = v1.distance(v2) * lengthScale
        x1angled = midpoint.x + math.cos(angleChange1 + piover2) * length
        y1angled = midpoint.y + math.sin(angleChange1 + piover2) * length
        x2angled = midpoint.x + math.cos(angleChange2 + piover2) * length
        y2angled = midpoint.y + math.sin(angleChange2 + piover2) * length

        v1angled = Vertex(x1angled, y1angled, v2.color)
        v2angled = Vertex(x2angled, y2angled, v2.color)
        
        l1 = Polygon([v1, midpoint], closed=False)
        if recursion == 0:
            #v1angled = Vertex(v1angled.x, v2angled.y, finishColor)
            #v2angled = Vertex(v2angled.x, v2angled.y, finishColor)
            flower1 = Polygon.circle(v1angled.x, v1angled.y, 3, finishColor, numSegments=3)
            flower2 = Polygon.circle(v2angled.x, v2angled.y, 3, finishColor, numSegments=3)
            l2 = Polygon([midpoint, v1angled], closed=False)
            l3 = Polygon([midpoint, v2angled], closed=False)
            return [l1, l2, l3, flower1, flower2]
        else:
            l2s = divideLine(midpoint, v1angled, anglechange, recursion-1)
            l3s = divideLine(midpoint, v2angled, anglechange, recursion-1)
            return [l1] + l2s + l3s
    
    color1 = (0, 192, 0, 255)
    color2 = (128, 192, 0, 255)
    v1 = Vertex(0, 0, color1)
    v2 = Vertex(0, 100, color2)
    polys = divideLine(v1, v2, 110, recursion=8)
    return LineImage(polys)

def backgroundSpiral():
    polys = []
    numPoints = 250
    colorBase = 192
    colorRange = 64
    length = 2
    width = 2

    spiralA = 10
    spiralB = 100
    numTurnings = 1
    deviationAvg = 0
    deviationStdDev = 25
    
    radius = 100
    
    def genPoint():
        theta = random.random() * (math.pi * (numTurnings * 2.0))
        radius = spiralA + spiralB*theta
        x = math.cos(theta) * radius + random.gauss(deviationAvg, deviationStdDev)
        y = math.sin(theta) * radius + random.gauss(deviationAvg, deviationStdDev)
        r = int(colorBase + (random.random() * colorRange))
        g = int(colorBase + (random.random() * colorRange))
        b = int(colorBase + (random.random() * colorRange))
        a = int((random.random() * colorRange))
        poly = Polygon.line(x, y, x+length, y+length, (r, g, b, a), strokeWidth=width)
        return poly
    
    polys = [genPoint() for _ in xrange(numPoints)]
    return LineImage(polys)

def lifeBar():
    polys = []
    color = (255, 0, 0, 255)
    polys.append(Polygon.rectCorner(0, 0, -250, 10, color))
    return LineImage(polys)

def energyBar():
    polys = []
    color = (0, 0, 255, 255)
    polys.append(Polygon.rectCorner(0, 0, 250, 10, color))
    return LineImage(polys)

def chunkGuide():
    """Crudely draws some guide lines for level chunk sizes.  Used in the level editor."""
    polys = []
    color = (32, 32, 32, 128)
    chunkSize = 1024
    # BUGGO: Oh gods horribly resolution-dependent
    startX = - (chunkSize / 2)
    startY = - (chunkSize / 2)
    for x in xrange(startX, startX + chunkSize + 1, 16):
        for y in xrange(startY, startY + chunkSize + 1, 16):
            polys.append(Polygon.line(x, startY, x, y, color, strokeWidth=1))
            polys.append(Polygon.line(startX, y, x, y, color, strokeWidth=1))
    return LineImage(polys)

def crosshair():
    polys = []
    color = (255, 0, 0, 255)
    size = 16
    start = size / 2
    polys.append(Polygon.line(-start, 0, start, 0, color, strokeWidth=2))
    polys.append(Polygon.line(0, -start, 0, start, color, strokeWidth=2))
    return LineImage(polys)
