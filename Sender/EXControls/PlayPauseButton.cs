using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sender.EXControls
{
    using Sender.Entities;

    public enum MediaDataType : byte
    {
        None,
        Video,
        Music,
        Sound,
        Image,
        Text,
        Parameters
    }

    public class StartPlayMediaDataButton : Button
    {
        public delegate void EventHandlerClick (object sender, EventArgs  eventArgs); 

        public enum ButtonStates : byte
        {
            Pause    = 0,
            Play     = 1,
            Continue = 2
        }

        public int NumberRowItem  { get; set; }

        public ButtonStates ButtonState  { get; set; }

        public MediaDataType ContentType { get; set; }

        public EventHandlerClick HandlerClick { get; set; }

        public StartPlayMediaDataButton()
        {
            ContentType = MediaDataType.None;
            ButtonState = ButtonStates.Play;
        }

        public StartPlayMediaDataButton(MediaDataType type) : this()
        {
            ContentType = type;
        }
    }
}
