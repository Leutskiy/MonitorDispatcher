using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Entities
{
    public enum PlaylistState : byte
    {
        Play  = 0,
        Pause = 10,
        Stop  = 20
    }
}
