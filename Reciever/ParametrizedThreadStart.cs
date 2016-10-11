using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reciever
{
    class ParametrizedThreadStart
    {
        private   Action<System.Windows.Forms.Form> method;

        public ParametrizedThreadStart(Action<System.Windows.Forms.Form> method)
        {
            // TODO: Complete member initialization
            this.method = method;
        }
    }
}
