using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Entities
{
    /// <summary>
    /// Track's a playlist
    /// </summary>
    [Serializable]
    public class Track
    {
        private int _id;

        public int Id
        {
            get
            {
                return _id;
            }
            private set
            {
                if (value >= 0)
                {
                    _id = value;
                }
            }
        }

        public string Name { get; set; }

        public string FullPath { get; set; }

        private Track()
        {
            Id = 0;
            Name = String.Empty;
            FullPath = String.Empty;
        }

        public Track(int id, string name, string fullPath)
            : this()
        {
            Id = id;
            Name = name;
            FullPath = fullPath;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
