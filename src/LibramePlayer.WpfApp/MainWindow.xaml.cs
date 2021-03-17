using Librame.Extensions;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LibramePlayer.WpfApp
{
    using Models;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string PlayerTitle
            = "Librame Player";

        private static readonly string TimeSpanEmpty
            = TimeSpan.Zero.ToStandardString();

        public static Snackbar Snackbar;

        private readonly double _defaultWidth;
        private readonly double _defaultHeight;

        private double _lastLeft;
        private double _lastTop;

        private RecordOptions _lastRecord;

        private TextBlock _lastPlaylistItem;

        private MainWindowViewModel _viewModel;


        public MainWindow()
        {
            InitializeComponent();

            InitializeBinding();

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2500);
            })
            .ContinueWith(t =>
            {
                MainSnackbar.MessageQueue.Enqueue(InternalResources.Welcome);
            },
            TaskScheduler.FromCurrentSynchronizationContext());

            _viewModel = new MainWindowViewModel(MainSnackbar.MessageQueue);

            _viewModel.PlaylistChangeAction = (wrapper, playList) =>
            {
                var defaultMedia = BindPlaylist(playList);
                wrapper.Load(defaultMedia);

                EnableControls();
            };

            _viewModel.PlayerWrapper.MediaOpenedAction = wrapper =>
            {
                if (_lastPlaylistItem.IsNotNull())
                    ChangeLastPlaylistItemState();

                // Reset LastPlaylistItem
                var mediaIndex = _viewModel.Playlist.Medias.IndexOf(wrapper.Media);
                _lastPlaylistItem = lbxPlaylist.Items[mediaIndex] as TextBlock;
                ChangeLastPlaylistItemState();

                if (_lastRecord.LastMediaId == wrapper.Media.Id
                    && _lastRecord.LastPosition.IsNotEmpty() && _lastRecord.LastPosition != TimeSpanEmpty)
                {
                    // 载入上次的进度
                    wrapper.Position = TimeSpan.Parse(_lastRecord.LastPosition);
                }

                pbrPlaying.Maximum = wrapper.DurationTime.TotalSeconds;
                sldVolume.Value = wrapper.Volume;
                tbkMediaTitle.Text = wrapper.Title;
                tbkCurrentTime.Text = wrapper.Position.ToStandardString();
                tbkDurationTime.Text = wrapper.DurationTime.ToStandardString();

                Snackbar.MessageQueue.Enqueue(InternalResources.MediaLoadedTextFormat.Format(wrapper.Media.Title));
            };

            _viewModel.PlayerWrapper.MediaFailedAction = (wrapper, e) =>
            {
                var textBlock = lbxPlaylist.SelectedItem as TextBlock;
                textBlock.Text += $" !{e.ErrorException.Message}";
                textBlock.Foreground = MaterialDesignHelper.ValidationErrorBrush;
                textBlock.FontWeight = FontWeights.Bold;

                Snackbar.MessageQueue.Enqueue(e.ErrorException.Message);
            };

            _viewModel.PlayerWrapper.MediaEndedAction = wrapper =>
            {
                PlayNext(wrapper);
            };

            _viewModel.PlayerWrapper.PlayingAction = wrapper =>
            {
                if (_isPlaylistFirstPlay && wrapper.Position > TimeSpan.Zero)
                {
                    // 首次播放需清除重复的当前定位时长
                    wrapper.TotalPlaybackDuration -= wrapper.Position;
                    _isPlaylistFirstPlay = false;
                }

                sldVolume.Value = wrapper.DynamicUpdateVolume();
                pbrPlaying.Value = wrapper.Position.TotalSeconds;
                tbkCurrentTime.Text = wrapper.Position.ToStandardString();
                tbxPlayerTitle.Text = $"{PlayerTitle} - Total: {wrapper.TotalPlaybackDuration.ToStandardString()}";

                _lastRecord.TotalPlaybackDuration = wrapper.TotalPlaybackDuration.ToStandardString();
                _lastRecord.LastMediaId = wrapper.Media.Id;
                _lastRecord.LastPosition = wrapper.Position.ToStandardString();
                _viewModel.SavePlayback();

                if (!btnStop.IsEnabled)
                    btnStop.IsEnabled = true;
            };

            _viewModel.PlayerWrapper.PlayAction = wrapper =>
            {
                var playIcon = btnPlay.Content as PackIcon;
                if (playIcon.Kind != PackIconKind.Pause)
                    playIcon.Kind = PackIconKind.Pause;
            };

            _viewModel.PlayerWrapper.PauseAction = wrapper =>
            {
                var playIcon = btnPlay.Content as PackIcon;
                if (playIcon.Kind != PackIconKind.Play)
                    playIcon.Kind = PackIconKind.Play;
            };

            _viewModel.PlayerWrapper.StopAction = wrapper =>
            {
                pbrPlaying.Value = 0;
                tbkCurrentTime.Text = TimeSpanEmpty;

                _lastRecord.TotalPlaybackDuration = TimeSpanEmpty;
                _lastRecord.LastMediaId = 0;
                _lastRecord.LastPosition = TimeSpanEmpty;
                _viewModel.SavePlayback();

                var playIcon = btnPlay.Content as PackIcon;
                if (playIcon.Kind != PackIconKind.Play)
                    playIcon.Kind = PackIconKind.Play;

                btnStop.IsEnabled = false;
            };

            EnableControls();

            DataContext = _viewModel;
            Snackbar = MainSnackbar;

            _defaultWidth = Width;
            _defaultHeight = Height;
        }


        private void InitializeBinding()
        {
            tbxPlayerTitle.Text = PlayerTitle;
            tbkMediaTitle.Text = InternalResources.PlaylistOpened;

            cbxLoopingState.ItemsSource = PlaylistHelper.GetLoopingStates().Select(pair => pair.Key);
            cbxLoopingState.SelectedIndex = 0;

            tbkCurrentTime.Text = TimeSpanEmpty;
            tbkDurationTime.Text = TimeSpanEmpty;

            tbxSinglePlayDurationTime.Text = TimeSpanEmpty;
            tbxSinglePlayIntervalTime.Text = TimeSpanEmpty;
            tbxTotalPlayDurationTime.Text = TimeSpanEmpty;
        }

        private void EnableControls()
        {
            var enabled = _viewModel.Playlist.IsNotNull();

            btnPlay.IsEnabled = enabled;
            btnStop.IsEnabled = enabled;
            //sldVolume.IsEnabled = enabled;

            btnSave.IsEnabled = false;
        }


        #region Player Controller

        private void PlayNext(MediaPlayerWrapper wrapper)
        {
            switch (_viewModel.Playlist.Looping)
            {
                case LoopingState.Single:
                    {
                        wrapper.Play(false);
                        break;
                    }

                case LoopingState.Playlist:
                    {
                        var mediaIndex = _viewModel.Playlist.Medias.IndexOf(wrapper.Media);
                        if (mediaIndex < _viewModel.Playlist.Medias.Count - 1)
                        {
                            wrapper.Load(_viewModel.Playlist.Medias[mediaIndex + 1]);
                        }
                        else
                        {
                            wrapper.Load(_viewModel.Playlist.Medias.First());
                        }

                        wrapper.Play(false);
                        break;
                    }

                case LoopingState.Random:
                    {
                        var media = GetMedia(new Random());
                        wrapper.Load(media);
                        wrapper.Play(false);

                        MediaOptions GetMedia(Random random)
                        {
                            var index = random.Next(0, _viewModel.Playlist.Medias.Count);
                            var media = _viewModel.Playlist.Medias[index];

                            if (_viewModel.PlayerWrapper.Media != media)
                                return media;

                            return GetMedia(random);
                        }
                        break;
                    }

                default:
                    break;
            }
        }


        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            var icon = btnPlay.Content as PackIcon;
            if (icon.Kind == PackIconKind.Play)
            {
                _viewModel.PlayerWrapper.Play(); //_viewModel.PlayerWrapper.State != PlayState.Paused
                btnPlay.ToolTip = InternalResources.Pause;
            }
            else
            {
                _viewModel.PlayerWrapper.Pause();
                btnPlay.ToolTip = InternalResources.Play;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            EnableControls();

            _viewModel.PlayerWrapper.Stop();
        }

        private void pbrPlaying_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _viewModel.PlayerWrapper.Position = TimeSpan.FromSeconds(pbrPlaying.Value);
        }

        private void sldVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _viewModel.PlayerWrapper.Volume = sldVolume.Value;
        }

        #endregion


        #region Playlist

        private void LoadPlaylist()
        {
            var dialog = new OpenFileDialog();
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.DefaultExt = AppHelper.Options.PlaylistExtension;
            dialog.Filter = AppHelper.Options.PlaylistFilter;

            if ((bool)dialog.ShowDialog())
                LoadPlaylist(dialog.FileName);
        }

        private void LoadPlaylist(string fileName)
        {
            _lastRecord = _viewModel.Playback.Records.SingleOrDefault(r => r.Playlist == fileName);
            if (_lastRecord.IsNull())
            {
                _lastRecord = new RecordOptions();
                _viewModel.Playback.Records.Add(_lastRecord);
            }

            if (_lastRecord.Playlist != fileName)
                _lastRecord.Playlist = fileName;

            _viewModel.Playlist = PlaylistHelper.LoadOptions(fileName);
            _viewModel.ClearPlayback();

            EnableControls();
        }


        private bool _isPlaylistFirstPlay = false;
        private MediaOptions BindPlaylist(PlaylistOptions options)
        {
            options.NotNull(nameof(options));

            if (options.Medias.IsEmpty())
            {
                Snackbar.MessageQueue.Enqueue("Playlist medias is empty.");
                return null;
            }

            if (_lastRecord.TotalPlaybackDuration.IsNotEmpty() && _lastRecord.TotalPlaybackDuration != TimeSpanEmpty)
            {
                _viewModel.PlayerWrapper.TotalPlaybackDuration = TimeSpan.Parse(_lastRecord.TotalPlaybackDuration);
                _isPlaylistFirstPlay = true;
            }

            var defaultSelectedIndex = 0;

            if (_lastRecord.LastMediaId > 0)
            {
                var media = options.Medias.SingleOrDefault(m => m.Id == _lastRecord.LastMediaId);
                if (media.IsNotNull())
                    defaultSelectedIndex = options.Medias.IndexOf(media);
            }
            else if (_viewModel.PlayerWrapper.State == PlayState.Playing)
            {
                defaultSelectedIndex = options.Medias.IndexOf(_viewModel.PlayerWrapper.Media);
            }

            if (defaultSelectedIndex < 0)
                defaultSelectedIndex = 0;

            lbxPlaylist.Items.Clear();

            var mediasCountLength = options.Medias.Count.ToString().Length;
            options.Medias.ForEach(media => AddPlaylist(media));

            _lastPlaylistItem = lbxPlaylist.Items[defaultSelectedIndex] as TextBlock;
            ChangeLastPlaylistItemState();

            tbkPlaylistTitle.Text = options.Title;
            tbkPlaylistAuthor.Text = $"by {options.Author}";
            tbkPlaylistDescr.Text = options.Descr;

            cbxLoopingState.SelectedIndex = PlaylistHelper.GetLoopingStatesIndex(options.Looping);

            tbxSinglePlayDurationTime.Text = options.SinglePlaybackDuration;
            tbxSinglePlayIntervalTime.Text = options.SinglePlaybackInterval;
            tbxTotalPlayDurationTime.Text = options.StopPlaybackDuration;

            if (options.SinglePlaybackDuration.IsNotEmpty() && options.SinglePlaybackDuration != TimeSpanEmpty)
                _viewModel.PlayerWrapper.SinglePlaybackDuration = TimeSpan.Parse(options.SinglePlaybackDuration);

            if (options.SinglePlaybackInterval.IsNotEmpty() && options.SinglePlaybackInterval != TimeSpanEmpty)
                _viewModel.PlayerWrapper.SinglePlaybackInterval = TimeSpan.Parse(options.SinglePlaybackInterval);

            if (options.StopPlaybackDuration.IsNotEmpty() && options.StopPlaybackDuration != TimeSpanEmpty)
                _viewModel.PlayerWrapper.StopPlaybackDuration = TimeSpan.Parse(options.StopPlaybackDuration);

            return _lastPlaylistItem.Tag as MediaOptions;


            void AddPlaylist(MediaOptions media)
            {
                if (media.Title.IsNull())
                {
                    media.Title = media.Source?.ToString().SplitPairByLastIndexOf('/').Value.Trim();

                    if (!btnSave.IsEnabled)
                        btnSave.IsEnabled = true;
                }

                var item = new TextBlock();
                item.Text = $"{media.Id.FormatString(mediasCountLength)}. {media.Title} [{media.Volume}]";
                item.Tag = media;

                lbxPlaylist.Items.Add(item);
            }
        }

        private void ChangeLastPlaylistItemState()
        {
            if (_lastPlaylistItem.Text.StartsWith(AppHelper.Options.PlayingMark))
            {
                _lastPlaylistItem.Text = _lastPlaylistItem.Text.TrimStart(AppHelper.Options.PlayingMark);
                _lastPlaylistItem.Foreground = tbkMediaTitle.Foreground;
                _lastPlaylistItem.FontWeight = FontWeights.Normal;
            }
            else
            {
                _lastPlaylistItem.Text = AppHelper.Options.PlayingMark + _lastPlaylistItem.Text;
                _lastPlaylistItem.Foreground = MaterialDesignHelper.PrimaryHueMidBrush;
                _lastPlaylistItem.FontWeight = FontWeights.Bold;
            }
        }


        private void mimRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lbxPlaylist.SelectedItem.IsNull())
                return;

            var item = lbxPlaylist.SelectedItem as TextBlock;
            var media = item.Tag as MediaOptions;

            var result = MessageBox.Show(InternalResources.MediaRemovedTextFormat.Format(media.Title),
                InternalResources.MediaRemovedCaption,
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.Playlist.Medias.Remove(media);

                _viewModel.Playlist.Medias.ForEach((media, index) =>
                {
                    // 重置 ID
                    media.Id = index + 1;
                });

                btnSave.IsEnabled = true;

                var defaultMedia = BindPlaylist(_viewModel.Playlist);
                _viewModel.PlayerWrapper.Load(defaultMedia);

                Snackbar.MessageQueue.Enqueue(InternalResources.MediaRemovedFormat.Format(media.Title));
            }
        }

        private void mimRefresh_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Playlist = PlaylistHelper.LoadOptions(_lastRecord.Playlist);
        }


        private void lbxPlaylist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EnableControls();

            if (_viewModel.Playlist.IsNull())
            {
                LoadPlaylist();
                return;
            }

            if (_lastPlaylistItem.IsNotNull())
                ChangeLastPlaylistItemState();

            // Reset LastPlaylistItem
            _lastPlaylistItem = lbxPlaylist.SelectedItem as TextBlock;
            ChangeLastPlaylistItemState();

            var media = _lastPlaylistItem.Tag as MediaOptions;
            tbkMediaTitle.Text = media.Title;

            _viewModel.PlayerWrapper.Load(media);
            _viewModel.PlayerWrapper.Play(); //_viewModel.PlayerWrapper.State == PlayState.Stoped
        }

        private void lbxPlaylist_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_lastRecord.IsNull() || _lastRecord.Playlist.IsEmpty())
                return;

            var meu = new ContextMenu();

            var mimRefresh = new MenuItem();
            mimRefresh.Header = InternalResources.MenuRefresh;
            mimRefresh.Click += mimRefresh_Click;
            meu.Items.Add(mimRefresh);

            if (lbxPlaylist.SelectedItems.IsNotEmpty())
            {
                var mimRemove = new MenuItem();
                mimRemove.Header = InternalResources.MenuRemoved;
                mimRemove.Click += mimRemove_Click;
                meu.Items.Add(mimRemove);
            }

            meu.IsOpen = true;
            lbxPlaylist.ContextMenu = meu;
        }

        private void lblPlaylist_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void lblPlaylist_PreviewDrop(object sender, DragEventArgs e)
        {
            var fileNames = e.Data.GetData(DataFormats.FileDrop) as string[];
            foreach (var file in fileNames)
            {
                var extension = Path.GetExtension(file);
                if (!AppHelper.MediaExtensions.Any(ext => ext == extension))
                {
                    if (file.EndsWith(AppHelper.Options.PlaylistExtension))
                    {
                        LoadPlaylist(file);
                        return;
                    }
                    else
                    {
                        Snackbar.MessageQueue.Enqueue(InternalResources.NotSupportedMediaExtensionFormat.Format(extension));
                        continue;
                    }
                }

                var media = MediaOptions.Create(lbxPlaylist.Items.Count + 1, file, AppHelper.Options.DefaultVolume);
                _viewModel.Playlist.Medias.Add(media);
                btnSave.IsEnabled = true;

                var defaultMedia = BindPlaylist(_viewModel.Playlist);
                _viewModel.PlayerWrapper.Load(defaultMedia);
            }
        }

        private void grdPlaylist_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lbxPlaylist.MaxHeight = grdPlaylist.MaxHeight;
        }

        #endregion


        #region Playlist Options

        private void tbkPlaylistTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btnSave.IsNotNull() && e.Changes.IsNotEmpty())
                btnSave.IsEnabled = true;
        }

        private void tbkPlaylistDescr_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btnSave.IsNotNull() && e.Changes.IsNotEmpty())
                btnSave.IsEnabled = true;
        }

        private void cbxLoopingState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (btnSave.IsNotNull())
                btnSave.IsEnabled = true;
        }

        private void tbxSinglePlayDurationTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btnSave.IsNotNull() && e.Changes.IsNotEmpty())
                btnSave.IsEnabled = true;
        }

        private void tbxSinglePlayIntervalTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btnSave.IsNotNull() && e.Changes.IsNotEmpty())
                btnSave.IsEnabled = true;
        }

        private void tbxTotalPlayDurationTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (btnSave.IsNotNull() && e.Changes.IsNotEmpty())
                btnSave.IsEnabled = true;
        }


        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadPlaylist();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Playlist.Title = tbkPlaylistTitle.Text;
            _viewModel.Playlist.Descr = tbkPlaylistDescr.Text;

            _viewModel.Playlist.Looping = PlaylistHelper.GetLoopingState((string)cbxLoopingState.SelectedItem);

            _viewModel.Playlist.SinglePlaybackDuration = tbxSinglePlayDurationTime.Text;
            _viewModel.Playlist.SinglePlaybackInterval = tbxSinglePlayIntervalTime.Text;
            _viewModel.Playlist.StopPlaybackDuration = tbxTotalPlayDurationTime.Text;

            PlaylistHelper.SaveOptions(_viewModel.Playlist, _lastRecord.Playlist);
            Snackbar.MessageQueue.Enqueue(InternalResources.PlaylistSaved);

            btnSave.IsEnabled = false;
        }

        #endregion


        #region Grid Wrapper

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed)
                return;

            DragMove();

            _lastLeft = Left;
            _lastTop = Top;
        }

        #endregion


        #region Window

        private Action WindowMaximizeAction { get; set; }

        private Action WindowRestoreAction { get; set; }


        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            var icon = btnMaximize.Content as PackIcon;

            if (WindowState == WindowState.Maximized)
            {
                Left = _lastLeft;
                Top = _lastTop;
                //ResizeMode = ResizeMode.CanResizeWithGrip;

                Width = _defaultWidth;
                Height = _defaultHeight;

                WindowState = WindowState.Normal;
                Topmost = false;

                icon.Kind = PackIconKind.WindowMaximize;
                btnMaximize.ToolTip = InternalResources.WindowMaximize;

                WindowRestoreAction?.Invoke();
                return;
            }

            if (WindowState == WindowState.Normal)
            {
                MaxWidth = SystemParameters.PrimaryScreenWidth;
                MaxHeight = SystemParameters.PrimaryScreenHeight;

                WindowState = WindowState.Maximized;
                Topmost = true;

                icon.Kind = PackIconKind.WindowRestore;
                btnMaximize.ToolTip = InternalResources.WindowRestore;

                WindowMaximizeAction?.Invoke();

                //Activated += new EventHandler(window_Activated);
                //Deactivated += new EventHandler(window_Deactivated);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PlayerWrapper.Dispose();

            _viewModel.ClearPlayback();

            Close();
        }

        #endregion

    }
}
