namespace LibramePlayer.WpfApp
{
    public class AppOptions
    {
        public string MediaExtensions { get; set; }
            = ".aac,.flac,.m4a,.mp3,.wav";

        public string PlayingMark { get; set; }
            = "♪ ";


        public string PlaybackFileName { get; set; }
            = "_playback.lppb";

        public string PlaylistExtension { get; set; }
            = ".lppl.json";

        public string PlaylistFilter { get; set; }
            = "Librame Player Playlist JSON (.lppl.json)|*.lppl.json";


        public string DefaultVolume { get; set; }
            = "0.3-0.7";

        public string VolumeRangeSeparator { get; set; }
            = "-";
    }
}
