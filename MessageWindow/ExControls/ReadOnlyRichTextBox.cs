using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MessageWindow.ExControls
{
    public class ReadOnlyRichTextBox : RichTextBox
    {
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);

        public ReadOnlyRichTextBox()
        {
            this.ReadOnly = true;
            this.GotFocus += TextBoxGotFocus;
            this.Cursor   = Cursors.Arrow; // mouse cursor like in other controls
        }

        private void TextBoxGotFocus(object sender, EventArgs args)
        {
            HideCaret(this.Handle);
        }
    }
}
