using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sender.EXControls
{
    public class DeleteButton : Button
    {
        public int NumberRowDeletedItem { get; set; }

        public MediaDataType ContentType { get; set; }
    }
}
