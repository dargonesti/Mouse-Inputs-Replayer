using Mouse_Ghst_wpf.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouse_Ghst_wpf.Classes
{
    public class InputAction : BaseAction
    {
        public int KeyCode { get; }
        public bool KeyDown { get; }

        public InputAction(ActionType type, int keyCode, bool keyDown, TimeSpan time)
        {
            Type = ActionType.Keyboard;
            KeyCode = keyCode;
            KeyDown = keyDown;
            Time = time;
        }
    }
}
