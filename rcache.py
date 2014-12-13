import pyglet

import os

import shader

# Doesn't search recursively.
pyglet.resource.path = ['images', 'sounds', 'shaders']
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



# collections.defaultdict might be useful here.
# Actually, it doesn't have quite the right semantics, since the default
# function can't know what the key being initialized is.  :-(
LINEIMAGECACHE = {}
def getLineImage(linefunc):
    try:
        return LINEIMAGECACHE[linefunc]
    except KeyError:
        img = linefunc()
        LINEIMAGECACHE[linefunc] = img
        return img

RENDERERCACHE = {}
def getRenderer(renderer):
    try:
        return RENDERERCACHE[renderer]
    except KeyError:
        r = renderer()
        RENDERERCACHE[renderer] = r
        return r

SHADERCACHE = {}
def getShader(name):
    try:
        return SHADERCACHE[name]
    except KeyError:
        vertex_program = pyglet.resource.text(name + '.vert')
        fragment_program = pyglet.resource.text(name + '.frag')
        shaderObj = shader.Shader([vertex_program.text], [fragment_program.text])
        SHADERCACHE[name] = shaderObj
        return shaderObj
