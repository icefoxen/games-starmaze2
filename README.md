# README #

Starmaze.  It's a game.

# Dependencies

* pyglet 1.2alpha (sigh)
* pymunk 4.0

# Tests

Oh gods tests ahahahaha

# Reading the source

Flags in comments:

* BUGGO means known not-good behavior
* TODO means something incomplete but not immediately essential
* XXX marks a questionable design decision
* OPT marks something that might need optimization

# Setting up a new dev environment

On Linux:

```
hg clone ssh://hg@bitbucket.org/icefox/starmaze
cd starmaze
virtualenv env
source env/bin/activate
pip install --upgrade http://pyglet.googlecode.com/archive/tip.zip
pip install pymunk
Probably need to install AVBin somewhere somehow.
echo 'default-push = ssh://hg@bitbucket.org/icefox/starmaze' >> .hg/hgrc
```

On Windows it's pretty much the same.  BUT:

- You have to use 32 bit Python, since pymunk on Windows is 32-bit only
- Gotta install AVBin 32-bit version, link here: https://avbin.github.io/AVbin/Download.html
- Then gotta take the avbin.dll file out of c:\windows\system32 and put it in the Starmaze directory (if it's not already there).