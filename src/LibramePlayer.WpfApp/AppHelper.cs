using Librame.Extensions;
using Newtonsoft.Json;
using System.IO;

namespace LibramePlayer.WpfApp
{
    public static class AppHelper
    {
        private static readonly string _optionsFileName
            = $"{typeof(AppHelper).Assembly.GetDisplayName()}.json";


        static AppHelper()
        {
            if (!File.Exists(_optionsFileName))
                File.WriteAllText(_optionsFileName, JsonConvert.SerializeObject(new AppOptions(), Formatting.Indented));

            Options = JsonConvert.DeserializeObject<AppOptions>(File.ReadAllText(_optionsFileName));
        }


        public static AppOptions Options { get; private set; }


        public static double DefaultVolume
        {
            get
            {
                if (Options.DefaultVolume.IsEmpty())
                    return 0.7;

                if (Options.DefaultVolume.Contains(Options.VolumeRangeSeparator))
                    return double.Parse(Options.DefaultVolume.SplitPair(Options.VolumeRangeSeparator).Key);

                return double.Parse(Options.DefaultVolume);
            }
        }

        public static string[] MediaExtensions
            => Options.MediaExtensions.Split(',');
    }
}
