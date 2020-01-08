using Librame.Extensions;
using System.Configuration;

namespace LibramePlayer.WpfApp
{
    public static class AppHelper
    {
        public static string DefaultVolume
            => ConfigurationManager.AppSettings[nameof(DefaultVolume)];

        public static string[] MediaExtensions
            => ConfigurationManager.AppSettings[nameof(MediaExtensions)].Split(',');

        public static string PlayingMark
            => ConfigurationManager.AppSettings[nameof(PlayingMark)];

        public static string PlaylistExtension
            => ConfigurationManager.AppSettings[nameof(PlaylistExtension)];

        public static string PlaylistFilter
            => ConfigurationManager.AppSettings[nameof(PlaylistFilter)];

        public static string VolumeRangeSeparator
            => ConfigurationManager.AppSettings[nameof(VolumeRangeSeparator)];


        public static double DefaultVolumeForPlayer
        {
            get
            {
                if (DefaultVolume.IsEmpty())
                    return 0.7;

                if (DefaultVolume.Contains(VolumeRangeSeparator))
                {
                    // 默认使用最小音量
                    return double.Parse(DefaultVolume.SplitPair(VolumeRangeSeparator).Key);
                }

                return double.Parse(DefaultVolume);
            }
        }

    }
}
