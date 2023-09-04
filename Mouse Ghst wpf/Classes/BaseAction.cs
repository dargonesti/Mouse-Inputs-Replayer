using Mouse_Ghst_wpf.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mouse_Ghst_wpf.Classes
{
    public abstract class BaseAction
    {
        public ActionType Type { get; set; }
        public TimeSpan Time { get; set; }
    }
}
