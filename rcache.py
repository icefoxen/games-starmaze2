import pyglet

import os

# Doesn't search recursively.
pyglet.resource.path = ['images', 'sound']
pyglet.resource.reindex()

# TODO: Sort out AVbin and make oggs work
def get_sound(name):
    sound = pyglet.resource.media(name + ".wav", streaming=False)
    return sound

def get_image(name):
    # Pyglet already caches images
    img = pyglet.resource.image(name + '.png')
    # Set the point the image gets rotated around
    # to the image's center.
    img.anchor_x = int(img.width // 2)
    img.anchor_y = int(img.height // 2)
    return img

def get_sprite(name):
    img = get_image(name)
    return pyglet.sprite.Sprite(img)


# TODO: collections.defaultdict might be useful here.

LINEIMAGECACHE = {}
def getLineImage(linefunc):
    try:
        return LINEIMAGECACHE[linefunc]
    except KeyError:
        img = linefunc()
        LINEIMAGECACHE[linefunc] = img
        return img
