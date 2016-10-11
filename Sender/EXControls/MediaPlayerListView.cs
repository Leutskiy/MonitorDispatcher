using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Sender.EXControls
{
    using Sender.Entities;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;

    public class MediaPlayerListView : Player
    {
        public string DirAudioFiles { get; set; }

        public override Playlist Playlist { get; set; }

        public void Refresh(List<Track> addedTracks)
        {
            foreach (var track in addedTracks)
	        {
                this.AddRow(track.Name, "", "");
	        }
        }

        public void GetSourceListTracks()
        {

            Refresh(Playlist.Tracks);
        }

        public MediaPlayerListView() : base()
        {
            Playlist = new Playlist();
        }

        public MediaPlayerListView(string serializablePlaylistPath) : this()
        {
            if (!File.Exists(serializablePlaylistPath))
            {
                return;
            }

            using (Stream fs = new FileStream(serializablePlaylistPath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                if (fs.Length == 0)
                {
                    return;
                }

                BinaryFormatter formatter = new BinaryFormatter();

                List<Track> tracksObjectGraph = formatter.Deserialize(fs) as List<Track>;

                if (tracksObjectGraph != null)
                {
                    Playlist.Tracks = tracksObjectGraph;
                   
                    this.GetSourceListTracks();
                }
            }
        }
    }
}