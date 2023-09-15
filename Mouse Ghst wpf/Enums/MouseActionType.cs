using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouse_Ghst_wpf.Enums
{
    public enum MouseActionType
    {
        Unknown = 9876,
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
    }//*/
    /*
        Absolute = 0x8000,
        HWheel = 0x01000,
        Move = 0x0001,
        MoveNoCoalesce = 0x2000,
        LeftDown = 0x0002,
        LeftUp = 0x0004,
        RightDown = 0x0008,
        RightUp = 0x0010,
        MiddleDown = 0x0020,
        MiddleUp = 0x0040,
        VirtualDesk = 0x4000,
        Wheel = 0x0800,
        XDown = 0x0080,
        XUp = 0x0100,*/
}
