import math
import random

from graphics import *

def playerImage():
    """Returns a new LineImage with an image of the player."""
    polyList = []
    radius = 20
    pcol = (64, 224, 64, 255)
    polyList.append(Polygon.circle(0, 0, radius, pcol, numSegments=16))

    spokeLength = radius + 18
    spokeBase = 8
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

def playerImageGlow():

    polyList = []
    radius = 20
    pcol = (64, 224, 64, 32)
    polyList.append(Polygon.circle(0, 0, radius, pcol, numSegments=16, strokeWidth=20))

    spokeLength = radius + 18
    spokeBase = 8
    polyList.append(Polygon.line(0, spokeBase, spokeLength, 0, pcol, strokeWidth=20))
    polyList.append(Polygon.line(0, -spokeBase, spokeLength, 0, pcol, strokeWidth=20))
    polyList.append(Polygon.line(spokeBase, 0, 0, spokeLength, pcol, strokeWidth=20))
    polyList.append(Polygon.line(-spokeBase, 0, 0, spokeLength, pcol, strokeWidth=20))
    polyList.append(Polygon.line(0, spokeBase, -spokeLength, 0, pcol, strokeWidth=20))
    polyList.append(Polygon.line(0, -spokeBase, -spokeLength, 0, pcol, strokeWidth=20))
    polyList.append(Polygon.line(spokeBase, 0, 0, -spokeLength, pcol, strokeWidth=20))
    polyList.append(Polygon.line(-spokeBase, 0, 0, -spokeLength, pcol, strokeWidth=20))

    image = LineImage(polyList)
    return image

def beginningsP1Bullet():
    #p = Polygon.line(10, 0, -10, 0, (192, 0, 0, 255), strokeWidth=4)
    vs = [
        Vertex(10, 0, (255,0,0,255)),
        Vertex(-10, 0, (192,0,0,128))
        ]
    p = Polygon(vs, closed=False, strokeWidth=4)
    return LineImage([p])

def door():
    poly = Polygon.rectCenter(0, 0, 40, 40, (128, 128, 255, 255))
    return LineImage([poly])

def powerup():
    poly = Polygon.rectCenter(0, 0, 10, 10, (192, 0, 0, 255))
    return LineImage([poly])

def crawler():
    polyList = []
    color = (192, 192, 192, 255)
    polyList.append(Polygon.arc(0, 0, 15, 180, color, numSegments=6))
    #polyList.append(cornersToLines(circleCorners(0, 0, 15, numSegments=6, color)))
    polyList.append(Polygon.line(0, 0, 20, 20, color))
    polyList.append(Polygon.line(0, 0, -20, 20, color))
    polyList.append(Polygon.line(0, 0, 10, 25, color))
    polyList.append(Polygon.line(0, 0, -10, 25, color))
    polyList.append(Polygon.line(0, 0, 30, 10, color))
    polyList.append(Polygon.line(0, 0, -30, 10, color))

    return LineImage(polyList)

def collectable():
    color = (192, 0, 0, 255)
    polyList = []
    polyList.append(Polygon.rectCorner(-10, -5, 20, 10, color))
    polyList.append(Polygon.rectCorner(-5, -10, 10, 20, color))
    image = LineImage(polyList)
    return image

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
