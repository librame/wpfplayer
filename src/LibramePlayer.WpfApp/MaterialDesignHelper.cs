using Librame.Extensions;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Media;

namespace LibramePlayer.WpfApp
{
    public static class MaterialDesignHelper
    {
        private static BundledTheme _bundledTheme = null;


        static MaterialDesignHelper()
        {
            if (_bundledTheme.IsNull())
            {
                foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
                {
                    if (dictionary is BundledTheme bundledTheme)
                    {
                        _bundledTheme = bundledTheme;
                        break;
                    }
                }
            }
        }


        public static ITheme Theme
            => _bundledTheme.GetTheme();


        public static SolidColorBrush PrimaryHueLightBrush
            => _bundledTheme[nameof(PrimaryHueLightBrush)] as SolidColorBrush;

        public static SolidColorBrush PrimaryHueLightForegroundBrush
            => _bundledTheme[nameof(PrimaryHueLightForegroundBrush)] as SolidColorBrush;

        public static SolidColorBrush PrimaryHueMidBrush
            => _bundledTheme[nameof(PrimaryHueMidBrush)] as SolidColorBrush;

        public static SolidColorBrush PrimaryHueMidForegroundBrush
            => _bundledTheme[nameof(PrimaryHueMidForegroundBrush)] as SolidColorBrush;

        public static SolidColorBrush PrimaryHueDarkBrush
            => _bundledTheme[nameof(PrimaryHueDarkBrush)] as SolidColorBrush;

        public static SolidColorBrush PrimaryHueDarkForegroundBrush
            => _bundledTheme[nameof(PrimaryHueDarkForegroundBrush)] as SolidColorBrush;


        public static SolidColorBrush SecondaryHueLightBrush
            => _bundledTheme[nameof(SecondaryHueLightBrush)] as SolidColorBrush;

        public static SolidColorBrush SecondaryHueLightForegroundBrush
            => _bundledTheme[nameof(SecondaryHueLightForegroundBrush)] as SolidColorBrush;

        public static SolidColorBrush SecondaryHueMidBrush
            => _bundledTheme[nameof(SecondaryHueMidBrush)] as SolidColorBrush;

        public static SolidColorBrush SecondaryHueMidForegroundBrush
            => _bundledTheme[nameof(SecondaryHueMidForegroundBrush)] as SolidColorBrush;

        public static SolidColorBrush SecondaryHueDarkBrush
            => _bundledTheme[nameof(SecondaryHueDarkBrush)] as SolidColorBrush;

        public static SolidColorBrush SecondaryHueDarkForegroundBrush
            => _bundledTheme[nameof(SecondaryHueDarkForegroundBrush)] as SolidColorBrush;


        public static SolidColorBrush SecondaryAccentBrush
            => _bundledTheme[nameof(SecondaryAccentBrush)] as SolidColorBrush;

        public static SolidColorBrush SecondaryAccentForegroundBrush
            => _bundledTheme[nameof(SecondaryAccentForegroundBrush)] as SolidColorBrush;


        public static SolidColorBrush ValidationErrorBrush
            => _bundledTheme[nameof(ValidationErrorBrush)] as SolidColorBrush;


        public static SolidColorBrush MaterialDesignBackground
            => _bundledTheme[nameof(MaterialDesignBackground)] as SolidColorBrush;
    }
}
