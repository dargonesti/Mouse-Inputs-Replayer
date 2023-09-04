using Mouse_Ghst_wpf.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouse_Ghst_wpf.Classes
{
    public class MouseAction :BaseAction
    {
        public MouseActionType MouseType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEMOVE = 0x00000001;


        public MouseAction(IntPtr wParam, int x, int y, TimeSpan time)
        {
            if (wParam == (IntPtr)WM_MOUSEMOVE)
            {
                MouseType = MouseActionType.Move;
            }
            else if (wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                MouseType = MouseActionType.Down;
            }
            else if (wParam == (IntPtr)WM_LBUTTONUP)
            {
                MouseType = MouseActionType.Up;
            }
            else if (wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                //MouseType = MouseActionType.Wheel;
                Console.WriteLine("TODO : Mouse Wheel");
            }

            X = x;
            Y = y;
            Time = time;
            Type=ActionType.Mouse;
        }
    }
}
