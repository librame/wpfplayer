using Librame.Extensions;
using MaterialDesignThemes.Wpf;
using System;
using System.IO;

namespace LibramePlayer.WpfApp.Models
{
    public class MainWindowViewModel
    {
        private static readonly string _playbackFileName
            = PlaylistHelper.CurrentDirectory.CombinePath(AppHelper.Options.PlaybackFileName);

        private PlaylistOptions _playlist;


        public MainWindowViewModel(ISnackbarMessageQueue snackbarMessageQueue)
        {
            snackbarMessageQueue.NotNull(nameof(snackbarMessageQueue));

            PlayerWrapper = new MediaPlayerWrapper();

            if (!File.Exists(_playbackFileName))
                PlaybackHelper.SaveOptions(new PlaybackOptions(), _playbackFileName);

            Playback = PlaybackHelper.LoadOptions(_playbackFileName);
        }


        public MediaPlayerWrapper PlayerWrapper { get; }

        public PlaybackOptions Playback { get; }


        public Action<MediaPlayerWrapper, PlaylistOptions> PlaylistChangeAction { get; set; }

        /// <summary>
        /// 由改变事件完成绑定。
        /// </summary>
        public PlaylistOptions Playlist
        {
            get => _playlist;
            set
            {
                _playlist = value.NotNull(nameof(value));
                PlaylistChangeAction?.Invoke(PlayerWrapper, _playlist);
            }
        }


        public void SavePlayback()
            => PlaybackHelper.SaveOptions(Playback, _playbackFileName);
    }
}
