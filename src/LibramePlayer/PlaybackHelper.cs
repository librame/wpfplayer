using Newtonsoft.Json;
using System.IO;

namespace LibramePlayer
{
    public static class PlaybackHelper
    {
        public static PlaybackOptions LoadOptions(string fileName)
            => JsonConvert.DeserializeObject<PlaybackOptions>(File.ReadAllText(fileName));

        public static void SaveOptions(PlaybackOptions options, string fileName)
            => File.WriteAllText(fileName, JsonConvert.SerializeObject(options, Formatting.Indented));
    }
}
