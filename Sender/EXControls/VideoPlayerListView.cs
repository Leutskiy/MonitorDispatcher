using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.EXControls
{
    using Sender.Entities;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;

    public class VideoPlayerListView : Player
    {
        public string DirVideoFiles { get; set; }

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

        public VideoPlayerListView() : base()
        {
            Playlist = new Playlist();
        }

        public VideoPlayerListView(string serializablePlaylistPath) : this()
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

                var formatter = new BinaryFormatter();

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
