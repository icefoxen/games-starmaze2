
from graphics import *

def playerImage():
    """Returns a new LineImage with an image of the player."""
    lineList = []
    radius = 20
    corners1 = circleCorners(0, 0, radius)
    lineList.append(cornersToLines(corners1))

    spokeLength = radius + 18
    spokeBase = 8
    lineList.append(lineCorners(0, spokeBase, spokeLength, 0))
    lineList.append(lineCorners(0, -spokeBase, spokeLength, 0))
    lineList.append(lineCorners(spokeBase, 0, 0, spokeLength))
    lineList.append(lineCorners(-spokeBase, 0, 0, spokeLength))
    lineList.append(lineCorners(0, spokeBase, -spokeLength, 0))
    lineList.append(lineCorners(0, -spokeBase, -spokeLength, 0))
    lineList.append(lineCorners(spokeBase, 0, 0, -spokeLength))
    lineList.append(lineCorners(-spokeBase, 0, 0, -spokeLength))

    allLines = list(itertools.chain.from_iterable(lineList))
    colors = [(64, 224, 64, 255) for _ in allLines]

    image = LineImage(allLines, colors)
    return image

def playerImageGlow():
    lineList = []
    radius = 20
    corners1 = circleCorners(0, 0, radius)
    lineList.append(cornersToLines(corners1))

    spokeLength = radius + 18
    spokeBase = 8
    lineList.append(lineCorners(0, spokeBase, spokeLength, 0))
    lineList.append(lineCorners(0, -spokeBase, spokeLength, 0))
    lineList.append(lineCorners(spokeBase, 0, 0, spokeLength))
    lineList.append(lineCorners(-spokeBase, 0, 0, spokeLength))
    lineList.append(lineCorners(0, spokeBase, -spokeLength, 0))
    lineList.append(lineCorners(0, -spokeBase, -spokeLength, 0))
    lineList.append(lineCorners(spokeBase, 0, 0, -spokeLength))
    lineList.append(lineCorners(-spokeBase, 0, 0, -spokeLength))

    allLines = list(itertools.chain.from_iterable(lineList))
    colors = [(128, 224, 128, 32) for _ in allLines]

    image = LineImage(allLines, colors, lineWidth=20)
    return image

def beginningsP1Bullet():
    lines = [
        (10, 0), (-10, 0)
        ]
    colors = [(192, 0, 0, 255), (128, 0, 0, 192)]
    return LineImage(lines, colors, lineWidth=4)

def powerup():
    corners = rectCornersCenter(0, 0, 10, 10)
    lineList = cornersToLines(corners)
    
    #allLines = list(itertools.chain.from_iterable(lineList))
    colors = [(192, 0, 0, 255) for _ in lineList]
    return LineImage(lineList, colors)

def crawler():
    lineList = []
    lineList.append(cornersToLines(circleCorners(0, 0, 15, numSegments=6)))
    lineList.append(cornersToLines(lineCorners(0, 0, 20, 20)))
    lineList.append(cornersToLines(lineCorners(0, 0, -20, 20)))
    lineList.append(cornersToLines(lineCorners(0, 0, 10, 25)))
    lineList.append(cornersToLines(lineCorners(0, 0, -10, 25)))
    lineList.append(cornersToLines(lineCorners(0, 0, 30, 10)))
    lineList.append(cornersToLines(lineCorners(0, 0, -30, 10)))

    allLines = list(itertools.chain.from_iterable(lineList))
    colors = [(192, 192, 192, 255) for _ in allLines]
    image = LineImage(allLines, colors, lineWidth=2)
    return image
