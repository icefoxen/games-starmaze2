using System;
using System.IO;
using OpenTK;
using OpenTK.Input;
using Newtonsoft.Json;


namespace Starmaze.Engine
{
    /*If I were you I'd just make a new Engine/Input.cs file, 
     * make a class for key bindings, 
     * and then make an instance of that class 
     * get put somewhere globally accessible upon game load.
     */ 
    //(left/right/up//down), fire1, fire2, jump, defend.
    //should load and save the keybindings to the config file
    //settings.cfg
    /*The saving and loading of it should happen semi-automatically,
     * so we should be able to just stick a new property in the 
     * GameOptions class and have it get saved and loaded already.
     * 
     * Make code that prints the settings and copy and paste that into a file
     * 
     */

    public class KeyBindings
    {
       //store name and the key

        public Key MoveLeft, MoveRight, MoveUp, MoveDown;
        public Key Fire1, Fire2, Jump, Defend;

        public KeyBindings()
        {
            MoveLeft = Key.Left;
            MoveRight = Key.Right;
            MoveUp = Key.Up;
            MoveDown = Key.Down;
        }

        public static void KeysFromFile()
        {
        }
    }

}
