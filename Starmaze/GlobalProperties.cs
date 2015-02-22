using Starmaze.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starmaze
{
    public class GlobalProperties
    {
        private static GlobalProperties instance;
        public KeyBindings keys;

        private GlobalProperties()
        {
            keys = new KeyBindings();
        }

        public static GlobalProperties Instance
        {
            get
            {
                if (instance == null)
                    instance = new GlobalProperties();
                return instance; 
            }
        }

    }
    
}
