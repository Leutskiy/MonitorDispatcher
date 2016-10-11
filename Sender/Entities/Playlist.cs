using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sender.Entities
{
    /// <summary>
    /// Playlist's a media player 
    /// </summary>
    [Serializable]
    public class Playlist : IEnumerable<Track>, IDisposable
    {
        private int _playableTrackNumber;

        public string Name { get; set; }

        public PlaylistTrackStates State { get; set; }

        public int PlayableTrackNumber 
        {
            get 
            {
                return _playableTrackNumber;
            }
            set 
            { 
                _playableTrackNumber = value; 
            }
        }

        public int PreviousTrackNumber { get; set; }

        public int TracksCount 
        {
            get
            {
                return Tracks.Count;
            }
        }

        public List<Track> Tracks { get; set; }

        public Track this[int index]
        {
            get
            {
                if (index >= 0 && index < TracksCount )
                    return Tracks[index];

                throw new IndexOutOfRangeException("Трека, с указанным индексом, не существует");
            }
            set
            {
                if (index >= 0 && index < Tracks.Count)
                    Tracks[index] = value;

                throw new IndexOutOfRangeException("Трека, с указанным индексом, не существует");
            }
        }

        public IEnumerator<Track> GetEnumerator()
        {
            return Tracks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Playlist()
        {
            Tracks              = new List<Track>();
            Name                = this.GetType().ToString();

            PlayableTrackNumber = -1;
            PreviousTrackNumber = -1;
            State               = PlaylistTrackStates.Stop;
            
        }

        public Playlist(List<Track> playlistTraks, string name) : this()
        {
            Tracks              = playlistTraks;
            Name                = name;
        }

        public void AddTrack(Track track)
        {
            Tracks.Add(track);
        }

        public void AddRangeTracks(IEnumerable<Track> tracks)
        {
            Tracks.AddRange(tracks);
        }

        public void Dispose()
        {
            Tracks.Clear();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
