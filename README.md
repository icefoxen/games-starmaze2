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
* Comb through code, add assertions and input checks.  (Note that assertions should be considered an immediate unconditional fail, they're for when the code is doing something Really Wrong.  Also be aware that they will not be present in Release builds, so don't worry about slowing things down with them!)
* Write tests
* Write doc comments

# C# version

This is the main branch.

## Dependencies

* OpenTK (DLL included in repo)
* NAudio
* Farseer physics engine
* Should work both on MS .NET and Mono, C# version >= 4.0, .NET version 4.5

## Setting up a new dev environment

Just installing Monodevelop/Xamarin Studio should be enough.  Should also work in Visual Studio but I haven't tested it.  Make sure you have the .NET 4.5 SDK or the latest available version of Mono.

Acquiring the CodeXL OpenGL debugger might also help.

## Coding guidelines

- Set the Monodevelop auto-formatter to the SharpDevelop (1TBS) style by going to Tools -> Formatting, down to Source Code -> Code Formatting -> C# Source code.  Also set line endings to unix.
- Go to Tools -> Formatting, down to Text Editor -> Behavior, enable "format document on save"
- THE CODE SHOULD IDEALLY COMPILE WITH NO WARNINGS.  Treat them as fatal errors unless they're for something you're going to fix soon.

## OpenGL version notes

* All Mac's since 2008 support OpenGL 3.3 - http://support.apple.com/en-us/HT202823
* All NVidia graphics cards since the GeForce 8 series (~2005) support OpenGL "3". - https://developer.nvidia.com/opengl-driver
* All AMD graphics cards since Radeon 3000-ish series support OpenGL 3.3 - https://en.wikipedia.org/wiki/Radeon#Technology_Overview
* All Intel graphics cards >= HD 3000 support OpenGL 3.1, HD 4000 supports OpenGL 4.0 on Windows, but on Linux it uses Mesa which only supports up to 3.2.  3.3 support in Mesa sort of exists, but is disabled by default because not all backend drivers actually support it. - https://en.wikipedia.org/wiki/Intel_HD_and_Iris_Graphics#Capabilities , http://mesa3d.org/

Conclusion: Target OpenGL 3.1 if we care about Intel graphics cards, 3.3 otherwise.  And we DO care about Intel graphics cards because pretty much every non-gaming laptop has one.  In the end though, we're targetting OpenGL 3.3, because it has features we want.

Targetting OpenGL ES 2.0 is necessary if we want to release on OUYA or be able to play it on Raspberry Pi (such as to put in an arcade box maybe), but apparently ES 2.0 is based vaguely off of OpenGL 2.0, so Eris only knows if that will be remotely feasible.

OpenGL ES 3.0 exists and is based off of OpenGL 4.3 which is awesome except nothing fucking supports it yet because graphics card/chip makers are the spawn of satan.
