using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.EventArguments
{
    public class BackgroundImageEventArgs : EventArgs
    {
        public string ImageFileName { get; set; }

        public BackgroundImageEventArgs() : base()
        {

        }

        public BackgroundImageEventArgs(string imageFileName) : this()
        {
            ImageFileName = imageFileName;
        }

        public override string ToString()
        {
            var message = string.Format("Background image file: {0}", ImageFileName);
            return message;
        }
    }

    public class ArgumentsForSendingToShowFormEventArgs : EventArgs
    {
        public bool closeFormWithImageOrText;

        public ArgumentsForSendingToShowFormEventArgs() : base()
        {

        }

        public ArgumentsForSendingToShowFormEventArgs(bool mustClose) : this()
        {
            closeFormWithImageOrText = mustClose;
        }
    }
}
