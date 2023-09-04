using Mouse_Ghst_wpf.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mouse_Ghst_wpf.Classes
{

    public class KeyboardAction : BaseAction
    {
        public Key Key { get; }

        public KeyboardAction(Key key, TimeSpan time)
        {
            Key = key;
            Time = time;
            Type = ActionType.Keyboard;
        }
    }
}
