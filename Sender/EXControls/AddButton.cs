using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sender.EXControls
{
    public class AddButton : Button
    {
        public int NumberButtonInList { get; set; }

        public AddButton() : base()
        {

        }

        public AddButton(int number) : this()
        {
            NumberButtonInList = 0;
        }
    }
}
