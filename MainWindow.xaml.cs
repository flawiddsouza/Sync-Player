using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Synced_Player
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public DelegateCommand OpenFileCommand { get; set; }
        public DelegateCommand PauseResumeCommand { get; set; }
        public DelegateCommand StopCommand { get; set; }
        public DelegateCommand ToggleFullscreenCommand { get; set; }
        public DelegateCommand EscapeFullscreenCommand { get; set; }
        public DelegateCommand SeekForwardXSecondsCommand { get; set; }
        public DelegateCommand SeekBackwardXSecondsCommand { get; set; }
        public DelegateCommand OpenPreferencesCommand { get; set; }
        public DelegateCommand OpenChatCommand { get; set; }
        public ObservableCollection<String> Messages { get; set; }
        public bool ChatOpen = false;

        private Timer hideTimer;
        private bool vlcPlayerPausedOrStopped = false;
        private SyncClient syncClient;
        private SoundPlayer messageAlert;
        private WindowState windowStateBeforeFullScreen;

        public MainWindow()
        {
            InitializeComponent();

            Left = Properties.Settings.Default.MainWindowLeft;
            Top = Properties.Settings.Default.MainWindowTop;
            Width = Properties.Settings.Default.MainWindowWidth;
            Height = Properties.Settings.Default.MainWindowHeight;
            WindowState = Properties.Settings.Default.MainWindowWindowState;

            volume.Value = vlcPlayer.Volume;

            hideTimer = new Timer(1000)
            {
                AutoReset = false
            };

            hideTimer.Elapsed += delegate // Hide cursor & control bar
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (WindowStyle == WindowStyle.None)
                    {
                        Mouse.OverrideCursor = Cursors.None;
                        controlBar.Visibility = Visibility.Collapsed;
                    }
                }));
            };

            OpenFileCommand = new DelegateCommand(ExecuteOpenFileCommand, (x) => true);
            PauseResumeCommand = new DelegateCommand(ExecutePauseResumeCommand, (x) => true);
            StopCommand = new DelegateCommand(ExecuteStopCommand, (x) => true);
            ToggleFullscreenCommand = new DelegateCommand(ExecuteToggleFullscreenCommand, (x) => true);
            EscapeFullscreenCommand = new DelegateCommand(ExecuteEscapeFullscreenCommand, (x) => true);
            SeekBackwardXSecondsCommand = new DelegateCommand(ExecuteSeekBackwardXSecondsCommand, (x) => true);
            SeekForwardXSecondsCommand = new DelegateCommand(ExecuteSeekForwardXSecondsCommand, (x) => true);
            OpenPreferencesCommand = new DelegateCommand(ExecuteOpenPreferencesCommand, (x) => true);
            OpenChatCommand = new DelegateCommand(ExecuteOpenChatCommand, (x) => true);

            Messages = new ObservableCollection<string>();

            DataContext = this;

            messageAlert = new SoundPlayer(@"Sounds\Received.wav");

            JoinRoom();
        }

        // Event Handlers

        // MainWindow Event Handlers

        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            CenterAllOwnedWindowsToSelf();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterAllOwnedWindowsToSelf();
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (WindowStyle == WindowStyle.None)
            {
                UndoHideCursor();
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.I)
            {
                infoPanel.Visibility = Visibility.Visible;
            }
        }

        private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.I)
            {
                infoPanel.Visibility = Visibility.Hidden;
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            syncClient?.Close();

            vlcPlayer.Stop();
            vlcPlayer.Dispose();

            Properties.Settings.Default.Save();
        }

        // SyncClient Event Handlers

        private void SyncClient_ConnectionChanged(object sender, ConnectionEventArgs e)
        {
            switch (e.Status)
            {
                case ConnectionStatus.Connected:
                    Dispatcher.Invoke(() =>
                    {
                        ShowNotification("Connected");
                        connectionStatus.Text = "Connected";
                    });
                    connectionStatus.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#98ff59");
                    break;
                case ConnectionStatus.Disconnected:
                    Dispatcher.Invoke(() =>
                    {
                        connectionStatus.Text = "Disconnected";
                        connectionStatus.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#ff5e59");
                    });
                    ShowNotification("Disconnected");
                    break;
            }
        }

        private void SyncClient_SeekToReceived(object sender, SyncEventArgs e)
        {
            float seekTime = float.Parse(e.SeekTime);
            vlcPlayer.Position = seekTime;
            SeekUpdate();
            ShowNotification(e.User + " seeked the video to " + StringifyElapsedTime(seekTime));
        }

        private void SyncClient_PauseReceived(object sender, SyncEventArgs e)
        {
            float seekTime = float.Parse(e.SeekTime);
            vlcPlayer.Pause();
            vlcPlayer.Position = seekTime;
            SeekUpdate();
            ShowNotification(e.User + " paused the video at " + StringifyElapsedTime(seekTime));
        }

        private void SyncClient_PlayReceived(object sender, SyncEventArgs e)
        {
            float seekTime = float.Parse(e.SeekTime);
            vlcPlayer.Position = seekTime;
            SeekUpdate();
            vlcPlayer.Play();
            ShowNotification(e.User + " played the video from " + StringifyElapsedTime(seekTime));
        }

        private void SyncClient_ChatReceived(object sender, ChatEventArgs e)
        {
            float seekTime = float.Parse(e.SeekTime);
            Dispatcher.Invoke(() =>
            {
                Messages.Add(e.User + " [" + StringifyElapsedTime(seekTime) + "]: " + e.ChatMessage);
                messageAlert.Play();
                if (Properties.Settings.Default.PauseAndOpenChatOnMessageReceived)
                {
                    if (!vlcPlayerPausedOrStopped)
                    {
                        Pause();
                    }
                    if (!ChatOpen)
                    {
                        OpenChat();
                    }
                }
            });
        }

        // VlcPlayer Event Handlers

        private void VlcPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            // this event is called when vlcplayer has finished initialization - not when the media is loaded
        }

        private void VlcPlayer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleFullScreen();
        }

        // will not be called when you set vlcPlayer.Position :| - it's only called when the media is playing
        // that means we'll have to call SeekUpdate() manually whenever we change vlcPlayer.Position
        private void VlcPlayer_PositionChanged(object sender, EventArgs e)
        {
            SeekUpdate();
        }

        private void VlcPlayer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > -1) // scroll up
            {
                if (volume.Value <= 95)
                {
                    volume.Value = volume.Value + 5;
                }
                else
                {
                    volume.Value = 100;
                }
            }
            else // scroll down
            {
                if (volume.Value >= 5)
                {
                    volume.Value = volume.Value - 5;
                }
                else
                {
                    volume.Value = 0;
                }
            }
        }

        // ControlBar Event Handlers

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            Pause();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopPlayback();
        }

        private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vlcPlayer.Volume = (int)volume.Value;
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //syncClient?.SendSeekTo(vlcPlayer.Position.ToString());
            vlcPlayer.Position = (float)seekBar.Value;
            UpdatePlayerTimes();
        }

        // Commands

        private void ExecuteOpenFileCommand(object parameter)
        {
            OpenFile();
        }

        private void ExecutePauseResumeCommand(object parameter)
        {
            if (!vlcPlayerPausedOrStopped)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }

        private void ExecuteStopCommand(object parameter)
        {
            StopPlayback();
        }

        private void ExecuteToggleFullscreenCommand(object parameter)
        {
            ToggleFullScreen();
        }

        private void ExecuteEscapeFullscreenCommand(object parameter)
        {
            if (WindowStyle == WindowStyle.None)
            {
                ExitFullScreen();
            }
        }

        private void ExecuteSeekBackwardXSecondsCommand(object seconds)
        {
            var currentPosition = vlcPlayer.Position * vlcPlayer.Length.TotalSeconds;
            var backXSecondsFromcurrentPosition = currentPosition - Int32.Parse((string)seconds);
            syncClient?.SendSeekTo(vlcPlayer.Position.ToString());
            vlcPlayer.Position = (float)(backXSecondsFromcurrentPosition / vlcPlayer.Length.TotalSeconds);
            SeekUpdate();
        }

        private void ExecuteSeekForwardXSecondsCommand(object seconds)
        {
            var currentPosition = vlcPlayer.Position * vlcPlayer.Length.TotalSeconds;
            var forwardXSecondsFromcurrentPosition = currentPosition + Int32.Parse((string)seconds);
            syncClient?.SendSeekTo(vlcPlayer.Position.ToString());
            vlcPlayer.Position = (float)(forwardXSecondsFromcurrentPosition / vlcPlayer.Length.TotalSeconds);
            SeekUpdate();
        }

        private void ExecuteOpenPreferencesCommand(object parameter)
        {
            Window preferences = new PreferencesWindow();
            preferences.Owner = this;
            preferences.Show();
        }

        private void ExecuteOpenChatCommand(object parameter)
        {
            OpenChat();
        }

        // Plain Methods

        public bool JoinRoom()
        {
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.ServerAddress) && !string.IsNullOrWhiteSpace(Properties.Settings.Default.RoomName) && !string.IsNullOrWhiteSpace(Properties.Settings.Default.Username) && syncClient == null)
            {
                syncClient = new SyncClient(Properties.Settings.Default.ServerAddress, Properties.Settings.Default.RoomName, Properties.Settings.Default.Username);
                syncClient.ConnectionChanged += SyncClient_ConnectionChanged;
                syncClient.SeekToReceived += SyncClient_SeekToReceived;
                syncClient.PauseReceived += SyncClient_PauseReceived;
                syncClient.PlayReceived += SyncClient_PlayReceived;
                syncClient.ChatReceived += SyncClient_ChatReceived;
                if (syncClient.Connect() == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void LeaveRoom()
        {
            syncClient.Close();
            syncClient.ConnectionChanged -= SyncClient_ConnectionChanged;
            syncClient.SeekToReceived -= SyncClient_SeekToReceived;
            syncClient.PauseReceived -= SyncClient_PauseReceived;
            syncClient.PlayReceived -= SyncClient_PlayReceived;
            syncClient.ChatReceived -= SyncClient_ChatReceived;
            syncClient = null;
        }

        public void SendChat(String message)
        {
            syncClient?.SendChatMessage(vlcPlayer.Position.ToString(), message);
        }

        private void OpenChat()
        {
            ChatModal chatModal = new ChatModal();
            chatModal.Owner = this;
            chatModal.DataContext = this;
            chatModal.Show();
            ChatOpen = true;
        }

        private void CenterAllOwnedWindowsToSelf()
        {
            foreach (Window win in OwnedWindows)
            {
                win.Left = Left + (Width - win.ActualWidth) / 2;
                win.Top = Top + (Height - win.ActualHeight) / 2;
            }
        }

        private void ToggleFullScreen()
        {
            // out of fullscreen
            if (WindowStyle == WindowStyle.None)
            {
                ExitFullScreen();
            }
            else // into fullscreen
            {
                EnterFullScreen();
            }
        }

        private void UpdatePlayerTimes()
        {
            elapsedTime.Text = StringifyElapsedTime();
            totalOrRemainingTime.Text = StringifyTotalDuration();
        }

        private TimeSpan GetElapsedTime()
        {
            return TimeSpan.FromSeconds(vlcPlayer.Position * vlcPlayer.Length.TotalSeconds);
        }

        /// <param name="elapsedSeekTimeBetween0to1">This will usually be vlcPlayer.Position from another player</param>
        private TimeSpan GetElapsedTime(double elapsedSeekTimeBetween0to1)
        {
            return TimeSpan.FromSeconds(elapsedSeekTimeBetween0to1 * vlcPlayer.Length.TotalSeconds);
        }

        private TimeSpan GetTotalDuration()
        {
            return TimeSpan.FromSeconds(vlcPlayer.Length.TotalSeconds);
        }

        public String StringifyElapsedTime()
        {
            TimeSpan elapsedTime = GetElapsedTime();
            TimeSpan totalDuration = GetTotalDuration();
            if (totalDuration.Hours > 0)
            {
                return elapsedTime.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return elapsedTime.ToString(@"mm\:ss");
            }
        }

        /// <param name="elapsedSeekTimeBetween0to1">This will usually be vlcPlayer.Position from another player</param>
        public String StringifyElapsedTime(double elapsedSeekTimeBetween0to1)
        {
            TimeSpan elapsedTime = GetElapsedTime(elapsedSeekTimeBetween0to1);
            TimeSpan totalDuration = GetTotalDuration();
            if (totalDuration.Hours > 0)
            {
                return elapsedTime.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return elapsedTime.ToString(@"mm\:ss");
            }
        }

        private String StringifyTotalDuration()
        {
            TimeSpan totalDuration = GetTotalDuration();
            if (totalDuration.Hours > 0)
            {
                return totalDuration.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return totalDuration.ToString(@"mm\:ss");
            }
        }

        private void SeekUpdate()
        {
            Dispatcher.Invoke(() =>
            {
                seekBar.Value = vlcPlayer.Position;
                UpdatePlayerTimes();
            });
        }

        private void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video files (*.mp4;*.mkv)|*.mp4;*.mkv|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                vlcPlayer.Stop();
                vlcPlayer.LoadMedia(openFileDialog.FileName);
                Title = Path.GetFileName(openFileDialog.FileName) + " - " + Title;
                seekBar.IsEnabled = true;
                if (Properties.Settings.Default.DontPlayOnMediaLoad == false)
                {
                    Play();
                }
                else
                {
                    // the thing is, we can't access the duration of the clip without playing it even once
                    // since DontPlayOnMediaLoad flag is true, the below method will just set both player times to 00:00
                    // until the video is played - don't blame me, it's LibVLC's fault
                    UpdatePlayerTimes(); // if we don't run this the player times will stay as --:--
                }
            }
        }

        private void Play()
        {
            syncClient?.SendPlay(vlcPlayer.Position.ToString());
            vlcPlayer.Play();
            vlcPlayerPausedOrStopped = false;
        }

        private void Pause()
        {
            syncClient?.SendPause(vlcPlayer.Position.ToString());
            vlcPlayer.Pause();
            vlcPlayerPausedOrStopped = true;
        }

        private void StopPlayback()
        {
            syncClient?.SendPause("0");
            vlcPlayer.Stop();
            seekBar.Value = 0;
            vlcPlayerPausedOrStopped = true;
        }

        private void UndoHideCursor()
        {
            hideTimer.Stop();
            Mouse.OverrideCursor = null; // Show cursor
            controlBar.Visibility = Visibility.Visible; // Show control bar
            hideTimer.Start();
        }

        private void EnterFullScreen()
        {
            Grid.SetRow(controlBar, 0);
            windowStateBeforeFullScreen = WindowState;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
        }

        private void ExitFullScreen()
        {
            UndoHideCursor();
            Grid.SetRow(controlBar, 1);
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = windowStateBeforeFullScreen;
            ResizeMode = ResizeMode.CanResize;
        }

        private void ShowNotification(String notification)
        {
            Timer timer = new Timer(3000);
            Dispatcher.Invoke(() =>
            {
                notificationBlock.Text = notification;
                notificationBlock.Visibility = Visibility.Visible;
            });
            timer.Start();
            timer.Elapsed += delegate
            {
                Dispatcher.Invoke(() =>
                {
                    // this ensures that the notificationBlock isn't hidden if a new notification has arrived within the elapsed time
                    if (notificationBlock.Text == notification)
                    {
                        notificationBlock.Visibility = Visibility.Hidden;
                    }
                });
                timer.Stop();
            };
        }
    }
}
