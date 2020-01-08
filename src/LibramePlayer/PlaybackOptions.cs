using System;
using System.Collections.Generic;

namespace LibramePlayer
{
    public class PlaybackOptions
    {
        public List<RecordOptions> Records { get; }
            = new List<RecordOptions>();
    }


    public class RecordOptions : IEquatable<RecordOptions>
    {
        public string Playlist { get; set; }

        public string TotalPlaybackDuration { get; set; }

        public int LastMediaId { get; set; }

        public string LastPosition { get; set; }


        public override bool Equals(object obj)
            => obj is RecordOptions other ? Equals(other) : false;

        public bool Equals(RecordOptions other)
            => other?.Playlist == Playlist;

        public override int GetHashCode()
            => Playlist.GetHashCode();
    }
}
