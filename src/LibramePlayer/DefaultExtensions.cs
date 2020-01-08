using System;

namespace LibramePlayer
{
    public static class DefaultExtensions
    {
        public static string ToStandardString(this TimeSpan timeSpan)
            => timeSpan.ToString(@"hh\:mm\:ss");
    }
}
