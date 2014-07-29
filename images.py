
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

    #polyList.append(cornersToLines(circleCorners(0, 0, 400, numSegments=100000)))

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

    #polyList.append(cornersToLines(circleCorners(0, 0, 400, numSegments=100000)))

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

    allLines = list(itertools.chain.from_iterable(lineList))
    colors = [(192, 192, 192, 255) for _ in allLines]
    image = LineImage(allLines, colors, lineWidth=2)
    return image

def collectable():
    color = (192, 0, 0, 255)
    polyList = []
    polyList.append(Polygon.rectCorner(-10, -5, 20, 10, color))
    polyList.append(Polygon.rectCorner(-5, -10, 10, 20, color))
    image = LineImage(polyList)
    return image

