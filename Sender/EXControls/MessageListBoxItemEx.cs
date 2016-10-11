using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.EXControls
{
    public class MessageListBoxItemEx
    {
        public MessageListBoxItemEx(Color c, string m)
        {
            ItemColor = c;
            Message = m;
        }

        public Color ItemColor { get; set; }
        public string Message { get; set; }
    }
}