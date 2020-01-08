using Librame.Extensions;
using System;
using System.Collections.Generic;

namespace LibramePlayer
{
    public class PlaylistOptions
    {
        public string Title { get; set; }

        public string Descr { get; set; }

        public string Author { get; set; }

        public string SinglePlaybackDuration { get; set; }

        public string SinglePlaybackInterval { get; set; }

        public string StopPlaybackDuration { get; set; }

        public LoopingState Looping { get; set; }

        public List<MediaOptions> Medias { get; }
            = new List<MediaOptions>();
    }


    public class MediaOptions : IEquatable<MediaOptions>
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Source { get; set; }

        public string Volume { get; set; }


        public override bool Equals(object obj)
            => obj is MediaOptions other ? Equals(other) : false;

        public bool Equals(MediaOptions other)
            => other?.Id == Id;

        public override int GetHashCode()
            => Id.GetHashCode();


        public Uri GetSourceUri()
        {
            var source = Source.NotEmpty(nameof(Source));

            if (source.Contains("\\"))
                source = source.Replace("\\", "/");

            // 支持相对路径（要求以“/”路径分隔符为前导符）
            if (source.StartsWith("./"))
                source = PlaylistHelper.CurrentDirectory.CombinePath(source);

            return new Uri(source.EnsureLeading("file:///"));
        }


        public static MediaOptions Create(int id, string source, string volume, string title = null)
        {
            source.NotEmpty(nameof(source));

            return new MediaOptions
            {
                Id = id,
                Source = source,
                Title = title,
                Volume = volume
            };
        }

    }
}
