using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouse_Ghst_wpf.Enums
{
    public enum MouseActionType
    {
        Move = 0x00000001,
        Move2 = 0,
        Down = 0x00000002,
        Up = 0x00000004,
        Down2 = 513,
        Up2 = 514,
        MiddleDown = 0x00000020,
        MiddleUp = 0x00000040,
        Absolute = 0x00008000,
        RightDown = 0x00000008,
        RightUp = 0x00000010
    }
}
