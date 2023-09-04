using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mouse_Ghst_wpf.Enums
{

        [StructLayout(LayoutKind.Sequential)]
        public struct HookStruct
    {
            public Point pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    
}
