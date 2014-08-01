"""Each zone is just a module that exports a single relevant function,
`generateZone`.

When called, it returns a list of Rooms, already interconnected.
In the list of rooms is one room with the entryPoint property set
to true, and one with the bossRoom property set to something...

XXX
Well, let's not get into that quite yet, until we have multiple
zones.  The general thought I'm working with right now is that
each zone is generated as a pile of `Room`s, then we go through
later and wire the zones in to the nexus and such.

Ooooh, since each room is named, we can make the entry and boss
rooms of each Zone have specific names, and just look for those
when doing final connecting-up's.  I LIKE IT.  INDIRECTION FTW.

An open question is how we determine that a boss is defeated
and a Power or Powerup is collected.  These are probably kept
track of by a global flags dict, kept by the World object.

"""

from terrain import *


cathedral = Room("Cathedral", [
BlockDescription(344.997236241, -629.999187503, [(0, 0), (528, 0), (528, 110), (0, 110)], (255, 255, 255, 255)) ,
BlockDescription(-419.0, -75.2722326316, [(0, 0), (104, 0), (104, 100), (0, 100)], (255, 255, 255, 255)) ,
BlockDescription(-301.020837096, -568.991405161, [(0, 0), (643, 0), (643, 80), (0, 80)], (255, 255, 255, 255)) ,
BlockDescription(-1064.15247439, -601.999997618, [(0, 0), (495, 0), (495, 82), (0, 82)], (255, 255, 255, 255)) ,
BlockDescription(-535.0, -40.0003785406, [(0, 0), (116, 0), (116, 106), (0, 106)], (255, 255, 255, 255)) ,
BlockDescription(-838.0, -12.0000000862, [(0, 0), (302, 0), (302, 84), (0, 84)], (255, 255, 255, 255)) ,
BlockDescription(408.999597217, -46.0002567557, [(0, 0), (121, 0), (121, 107), (0, 107)], (255, 255, 255, 255)) ,
BlockDescription(-314.0, -195.0, [(0, 0), (115, 0), (115, 488), (0, 488)], (255, 255, 255, 255)) ,
BlockDescription(-1042.0, -98.0, [(0, 0), (105, 0), (105, 100), (0, 100)], (255, 255, 255, 255)) ,
BlockDescription(-566.999999802, -625.0, [(0, 0), (267, 0), (267, 80), (0, 80)], (255, 255, 255, 255)) ,
BlockDescription(784.99999999, -55.0000000065, [(0, 0), (106, 0), (106, 108), (0, 108)], (255, 255, 255, 255)) ,
BlockDescription(324.931014162, -81.0439753371, [(0, 0), (87, 0), (87, 95), (0, 95)], (255, 255, 255, 255)) ,
BlockDescription(891.0, -111.0, [(0, 0), (86, 0), (86, 106), (0, 106)], (255, 255, 255, 255)) ,
BlockDescription(211.0, -174.0, [(0, 0), (111, 0), (111, 447), (0, 447)], (255, 255, 255, 255)) ,
BlockDescription(528.999999733, 1.99999982987, [(0, 0), (252, 0), (252, 86), (0, 86)], (255, 255, 255, 255)) ,
BlockDescription(-938.0, -59.0000000019, [(0, 0), (100, 0), (100, 106), (0, 106)], (255, 255, 255, 255)) ,
BlockDescription(-77.0, -479.981517977, [(0, 0), (178, 0), (178, 102), (0, 102)], (255, 255, 255, 255)) ,
])


entryway = Room("Entryway", [
BlockDescription(595.999999876, -409.999999866, [(0, 0), (121, 0), (121, 122), (0, 122)], (255, 255, 255, 255)) ,
BlockDescription(410.931422407, -326.0, [(0, 0), (92, 0), (92, 92), (0, 92)], (255, 255, 255, 255)) ,
BlockDescription(880.999153668, -471.999943878, [(0, 0), (501, 0), (501, 49), (0, 49)], (255, 255, 255, 255)) ,
BlockDescription(723.958693549, -493.0, [(0, 0), (155, 0), (155, 154), (0, 154)], (255, 255, 255, 255)) ,
BlockDescription(502.999794268, -364.999778044, [(0, 0), (91, 0), (91, 102), (0, 102)], (255, 255, 255, 255)) ,
BlockDescription(1061.99737716, -440.999999999, [(0, 0), (151, 0), (151, 20), (0, 20)], (255, 255, 255, 255)) ,
BlockDescription(-419.0, -276.0, [(0, 0), (826, 0), (826, 60), (0, 60)], (255, 255, 255, 255)) ,
BlockDescription(-420.0, -215.0, [(0, 0), (86, 0), (86, 445), (0, 445)], (255, 255, 255, 255)) ,
BlockDescription(1388.94483975, -471.0, [(0, 0), (119, 0), (119, 542), (0, 542)], (255, 255, 255, 255)) ,
BeginningsPowerupDescription(300, -200),
CrawlerEnemyDescription(400, -200),
DoorDescription(1140, -400, "Cathedral", 0, 0),
TreeDescription(1080, -420),
#TreeDescription(1200, -420),
FallingBlockDescription(200.0, 140.0, [(0, 0), (106, 0), (106, 37), (0, 37)], (255, 255, 255, 255)) ,
])

arena = Room("Arena", [
BlockDescription(-680.914246829, 626.999999989, [(0, 0), (102, 0), (102, 252), (0, 252)], (255, 255, 255, 255)) ,
BlockDescription(347.749025925, 69.9999993128, [(0, 0), (210, 0), (210, 77), (0, 77)], (255, 255, 255, 255)) ,
BlockDescription(-142.704277382, 77.9999999984, [(0, 0), (253, 0), (253, 67), (0, 67)], (255, 255, 255, 255)) ,
BlockDescription(637.995782926, 350.979433455, [(0, 0), (105, 0), (105, 450), (0, 450)], (255, 255, 255, 255)) ,
BlockDescription(-361.054117293, -136.961532784, [(0, 0), (298, 0), (298, 54), (0, 54)], (255, 255, 255, 255)) ,
BlockDescription(-681.881362414, 254.932783673, [(0, 0), (104, 0), (104, 373), (0, 373)], (255, 255, 255, 255)) ,
BlockDescription(-679.999849926, -384.999997359, [(0, 0), (217, 0), (217, 218), (0, 218)], (255, 255, 255, 255)) ,
BlockDescription(-463.0, -384.991739445, [(0, 0), (953, 0), (953, 65), (0, 65)], (255, 255, 255, 255)) ,
BlockDescription(494.965215094, -386.999999975, [(0, 0), (240, 0), (240, 195), (0, 195)], (255, 255, 255, 255)) ,
BlockDescription(-501.9549123, 84.969610862, [(0, 0), (156, 0), (156, 66), (0, 66)], (255, 255, 255, 255)) ,
BlockDescription(126.093160135, -142.999993111, [(0, 0), (326, 0), (326, 51), (0, 51)], (255, 255, 255, 255)) ,
BlockDescription(640.999999998, -190.9999413, [(0, 0), (94, 0), (94, 538), (0, 538)], (255, 255, 255, 255)) ,
BlockDescription(-679.0, -168.00667689, [(0, 0), (89, 0), (89, 418), (0, 418)], (255, 255, 255, 255)) ,
BlockDescription(-186.999968146, 289.0, [(0, 0), (359, 0), (359, 64), (0, 64)], (255, 255, 255, 255)) ,
BeginningsPowerupDescription(000, -200),
])


def generateZone():
    return [entryway, cathedral, arena]
