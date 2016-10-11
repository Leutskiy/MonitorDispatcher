using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.EXControls
{
    using Sender.Entities;

    public abstract class Player : ListViewEx
    {
        public abstract Playlist Playlist { get; set; }
    }
}
