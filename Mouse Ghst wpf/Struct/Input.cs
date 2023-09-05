using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mouse_Ghst_wpf.Struct
{

    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        public int type;
        public InputUnion u;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
