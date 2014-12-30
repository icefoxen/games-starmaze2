# README #

Starmaze.  It's a game.

# Reading the source

Flags in comments:

* BUGGO means known not-good behavior
* TODO means something incomplete but not immediately essential
* XXX marks a questionable design decision
* OPT marks something that might need optimization

# Background things that are always useful

If you don't have anything else to do, try these!

* Find an issue, fix it.
* Comb through code for BUGGO, XXX, etc flags and fix them.
* Comb through code, add assertions and input checks.  (Note that assertions * should be considered an immediate unconditional fail.)
* Write tests

# C# version

This is going to be the main version, but for the moment doesn't really exist.

## Dependencies

* OpenTK (DLL included in repo)
* Should work both on MS .NET and Mono, .NET version 4.5

## Setting up a new dev environment

Just installing Monodevelop/Xamarin Studio should be enough.  Should also work in Visual Studio but I haven't tested it.  Make sure you have the .NET 4.5 SDK or the latest available version of Mono.

Acquiring the CodeXL OpenGL debugger might also help.

## Coding guidelines

- Set the Monodevelop auto-formatter to the SharpDevelop (1TBS) style by going to Tools -> Formatting, down to Source Code -> Code Formatting -> C# Source code.  Also set line endings to unix.
- Go to Tools -> Formatting, down to Text Editor -> Behavior, enable "format document on save"

## OpenGL version notes

* All Mac's since 2008 support OpenGL 3.3 - http://support.apple.com/en-us/HT202823
* All NVidia graphics cards since the GeForce 8 series (~2005) support OpenGL "3". - https://developer.nvidia.com/opengl-driver
* All AMD graphics cards since Radeon 3000-ish series support OpenGL 3.3 - https://en.wikipedia.org/wiki/Radeon#Technology_Overview
* All Intel graphics cards >= HD 3000 support OpenGL 3.1, HD 4000 supports OpenGL 4.0 on Windows, but on Linux it uses Mesa which only supports up to 3.2.  3.3 support in Mesa sort of exists, but is essentially because not all backend drivers actually support it. - https://en.wikipedia.org/wiki/Intel_HD_and_Iris_Graphics#Capabilities , http://mesa3d.org/

Conclusion: Target OpenGL 3.1 if we care about Intel graphics cards, 3.3 otherwise.  And we DO care about Intel graphics cards because pretty much every non-gaming laptop has one.

# Python version

## Dependencies

* pyglet 1.2alpha (sigh)
* pymunk 4.0

## Setting up a new dev environment

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
