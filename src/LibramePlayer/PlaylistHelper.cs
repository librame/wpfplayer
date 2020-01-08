using Librame.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibramePlayer
{
    using Resources;

    public static class PlaylistHelper
    {
        public static readonly string CurrentDirectory
            = Directory.GetCurrentDirectory().WithoutDevelopmentRelativePath();

        public static PlaylistOptions LoadOptions(string fileName)
            => JsonConvert.DeserializeObject<PlaylistOptions>(File.ReadAllText(fileName));

        public static void SaveOptions(PlaylistOptions options, string fileName)
            => File.WriteAllText(fileName, JsonConvert.SerializeObject(options, Formatting.Indented));


        private static IEnumerable<KeyValuePair<string, int>> _loopingStates;
        public static IEnumerable<KeyValuePair<string, int>> GetLoopingStates()
        {
            if (_loopingStates.IsEmpty())
            {
                var localizer = ServiceHelper.Provider.GetRequiredService<IStringLocalizer<LoopingStateResource>>();
                _loopingStates = typeof(LoopingState).AsEnumResults(fi => new KeyValuePair<string, int>(localizer[fi.Name], (int)fi.GetValue(null)));
            }

            return _loopingStates;
        }

        public static int GetLoopingStatesIndex(LoopingState state)
        {
            var value = (int)state;

            var index = 0;
            foreach (var pair in GetLoopingStates())
            {
                if (pair.Value == value)
                    return index;

                index++;
            }

            return index;
        }

        public static LoopingState GetLoopingState(string text)
            => (LoopingState)GetLoopingStates().First(pair => pair.Key == text).Value;
    }
}
