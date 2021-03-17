using Librame.Extensions;
using Librame.Extensions.Core;
using System;
using System.Windows.Media;
using System.Windows.Threading;

namespace LibramePlayer.WpfApp
{
    public class MediaPlayerWrapper : AbstractDisposable
    {
        private DispatcherTimer _playerTimer = null;
        private DispatcherTimer _playingTimer = null;

        private MediaPlayer _player = null;


        public MediaPlayerWrapper(MediaOptions media = null)
        {
            _player = new MediaPlayer();
            _player.MediaOpened += _player_MediaOpened;
            _player.MediaFailed += _player_MediaFailed;
            _player.MediaEnded += _player_MediaEnded;

            _playerTimer = new DispatcherTimer();
            _playerTimer.Interval = TimeSpan.FromSeconds(1);
            _playerTimer.Tick += _playerTimer_Tick;
            _playerTimer.Start();

            _playingTimer = new DispatcherTimer();
            _playingTimer.Interval = TimeSpan.FromMilliseconds(100);
            _playingTimer.Tick += _playingTimer_Tick;

            if (media.IsNotNull())
                Load(media);
        }


        public TimeSpan DurationTime
            => _player.NaturalDuration.TimeSpan;

        public TimeSpan Position
        {
            get => _player.Position;
            set => _player.Position = value;
        }

        public Uri Source
            => _player.Source;

        public PlayState State { get; private set; }
            = PlayState.Stoped;

        public MediaOptions Media { get; private set; }

        public string Title
            => Media?.Title ?? Source?.ToString().SplitPairByLastIndexOf('/').Value.Trim();

        public double Volume
        {
            get => _player.Volume;
            set => _player.Volume = value;
        }

        public TimeSpan PlayingInterval
        {
            get => _playingTimer.Interval;
            set => _playingTimer.Interval = value;
        }


        public TimeSpan SinglePlaybackDuration { get; set; }

        public TimeSpan SinglePlaybackInterval { get; set; }

        public TimeSpan StopPlaybackDuration { get; set; }

        public TimeSpan TotalPlaybackDuration { get; internal set; }


        public Action<MediaPlayerWrapper> MediaOpenedAction { get; set; }

        public Action<MediaPlayerWrapper, ExceptionEventArgs> MediaFailedAction { get; set; }

        public Action<MediaPlayerWrapper> MediaEndedAction { get; set; }


        public Action<MediaPlayerWrapper> PlayingAction { get; set; }

        public Action<MediaPlayerWrapper> SinglePlayCompletedAction { get; set; }


        public Action<MediaPlayerWrapper> PlayAction { get; set; }

        public Action<MediaPlayerWrapper> PauseAction { get; set; }

        public Action<MediaPlayerWrapper> StopAction { get; set; }


        public void Load(MediaOptions media)
        {
            Media = media.NotNull(nameof(media));

            _player.Open(media.GetSourceUri());

            DynamicUpdateVolume();
        }


        public void Play(bool resetTotalPlaybackDuration = false)
        {
            if (State == PlayState.Playing)
                Stop(resetTotalPlaybackDuration);

            _player.Play();
            _playingTimer.Start();
            State = PlayState.Playing;

            PlayAction?.Invoke(this);
        }

        public void Pause()
        {
            if (_player.CanPause)
            {
                _player.Pause();
                _playingTimer.Stop();
                State = PlayState.Paused;

                PauseAction?.Invoke(this);
            }
        }

        public void Stop(bool resetTotalPlaybackDuration = false)
        {
            _player.Stop();
            _playingTimer.Stop();
            State = PlayState.Stoped;

            LastPosition = TimeSpan.Zero;

            if (resetTotalPlaybackDuration)
                TotalPlaybackDuration = TimeSpan.Zero;

            StopAction?.Invoke(this);
        }


        public double DynamicUpdateVolume()
        {
            if (Media.Volume.IsEmpty())
            {
                _player.Volume = AppHelper.DefaultVolume;
            }
            else if (!Media.Volume.Contains(AppHelper.Options.VolumeRangeSeparator))
            {
                _player.Volume = double.Parse(Media.Volume);
            }
            else
            {
                var range = Media.Volume.SplitPair(AppHelper.Options.VolumeRangeSeparator);
                var min = double.Parse(range.Key);

                if (_player.NaturalDuration.HasTimeSpan)
                {
                    var max = double.Parse(range.Value);
                    var quotient = Position.TotalSeconds / DurationTime.TotalSeconds;
                    _player.Volume = (max - min) * quotient + min;
                }
                else
                {
                    // 初始化时持续时间不可用，直接使用最小音量值
                    _player.Volume = min;
                }
            }

            return _player.Volume;
        }


        private void _player_MediaOpened(object sender, EventArgs e)
        {
            MediaOpenedAction?.Invoke(this);
        }

        private void _player_MediaFailed(object sender, ExceptionEventArgs e)
        {
            MediaFailedAction?.Invoke(this, e);
        }

        private void _player_MediaEnded(object sender, EventArgs e)
        {
            MediaEndedAction?.Invoke(this);
        }


        internal TimeSpan LastPlaybackDuration { get; private set; }

        internal TimeSpan LastPosition { get; private set; }


        private void UpdateTotalPlaybackDuration()
        {
            if (_player.Position > LastPosition)
            {
                TotalPlaybackDuration += _player.Position - LastPosition;
                LastPosition = _player.Position;
            }
        }

        private void _playerTimer_Tick(object sender, EventArgs e)
        {
            if (TimeSpan.Zero != SinglePlaybackDuration)
            {
                // 非首次播放
                if (TotalPlaybackDuration > SinglePlaybackDuration)
                {
                    var currentTotalSeconds = (int)Math.Round(TotalPlaybackDuration.TotalSeconds, 0);
                    var singleTotalSeconds = (int)Math.Round(SinglePlaybackDuration.TotalSeconds, 0);
                    var timeSpan = currentTotalSeconds % singleTotalSeconds;

                    // 达到单次播放持续时间
                    if (timeSpan == 0)
                    {
                        switch (State)
                        {
                            case PlayState.Playing:
                                Pause();
                                break;

                            case PlayState.Paused:
                                if (LastPlaybackDuration >= SinglePlaybackInterval)
                                {
                                    Play();
                                    LastPlaybackDuration = TimeSpan.Zero;
                                }
                                else
                                {
                                    LastPlaybackDuration += _playerTimer.Interval;
                                }
                                break;
                        }
                    }
                }
            }
            
            if (TimeSpan.Zero != StopPlaybackDuration)
            {
                if (TotalPlaybackDuration >= StopPlaybackDuration)
                {
                    if (State != PlayState.Stoped)
                        Stop();
                }
            }
        }

        private void _playingTimer_Tick(object sender, EventArgs e)
        {
            UpdateTotalPlaybackDuration();

            PlayingAction?.Invoke(this);
        }


        protected override void DisposeCore()
        {
            if (_playingTimer.IsEnabled)
                _playingTimer.Stop();

            if (_playerTimer.IsEnabled)
                _playerTimer.Stop();
        }

    }
}
