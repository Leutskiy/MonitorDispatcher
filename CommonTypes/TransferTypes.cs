using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes
{
    public class MediaData
    {
        public string  Message           { get; set; }
        public string  ImageFileName     { get; set; }
        public string  MediaDataFileName { get; set; }
        public Command Command           { get; set; }

        public MediaData()
            : this(string.Empty, string.Empty, string.Empty, Command.Text)
        {

        }

        public MediaData(string m, string ifilename, string mfilename, Command cmd)
        {
            Message       = m;
            ImageFileName = ifilename;
            MediaDataFileName = mfilename;
            Command       = cmd;
        }
    }
}
