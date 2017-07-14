using Microsoft.Win32;
using System;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Input;

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
        public DelegateCommand SeekForwardXSeconds { get; set; }
        public DelegateCommand SeekBackwardXSeconds { get; set; }

        private Timer hideTimer;
        private bool vlcPlayerStopped = false;

        public MainWindow()
        {
            InitializeComponent();

            volume.Value = vlcPlayer.Volume;

            hideTimer = new Timer(2000)
            {
                AutoReset = false
            };

            hideTimer.Elapsed += delegate // Hide cursor & control bar
            {
                //if (Application.Current.MainWindow.WindowStyle == WindowStyle.None)
                //{
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    Mouse.OverrideCursor = Cursors.None;
                    controlBar.Visibility = Visibility.Collapsed;
                }));
                //}
            };

            OpenFileCommand = new DelegateCommand(ExecuteOpenFileCommand, (x) => true);
            PauseResumeCommand = new DelegateCommand(ExecutePauseResumeCommand, (x) => true);
            StopCommand = new DelegateCommand(ExecuteStopCommand, (x) => true);
            ToggleFullscreenCommand = new DelegateCommand(ExecuteToggleFullscreenCommand, (x) => true);
            EscapeFullscreenCommand = new DelegateCommand(ExecuteEscapeFullscreenCommand, (x) => true);
            SeekBackwardXSeconds = new DelegateCommand(ExecuteSeekBackwardXSeconds, (x) => true);
            SeekForwardXSeconds = new DelegateCommand(ExecuteSeekForwardXSeconds, (x) => true);

            DataContext = this;
        }

        private void ExecuteOpenFileCommand(object parameter)
        {
            OpenFile();
        }

        private void ExecutePauseResumeCommand(object parameter)
        {
            if (!vlcPlayerStopped) {
                vlcPlayer.PauseOrResume();
            }
            else
            {
                vlcPlayer.Play();
                vlcPlayerStopped = false;
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
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.CanResize;
            }
        }

        private void ExecuteSeekBackwardXSeconds(object seconds)
        {
            var currentPosition = vlcPlayer.Position * vlcPlayer.Length.TotalSeconds;
            var backXSecondsFromcurrentPosition = currentPosition - Int32.Parse((string)seconds);
            vlcPlayer.Position = (float)(backXSecondsFromcurrentPosition / vlcPlayer.Length.TotalSeconds);
            SeekUpdate();
        }

        private void ExecuteSeekForwardXSeconds(object seconds)
        {
            var currentPosition = vlcPlayer.Position * vlcPlayer.Length.TotalSeconds;
            var forwardXSecondsFromcurrentPosition = currentPosition + Int32.Parse((string)seconds);
            vlcPlayer.Position = (float)(forwardXSecondsFromcurrentPosition / vlcPlayer.Length.TotalSeconds);
            SeekUpdate();
        }

        private void Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vlcPlayer.Volume = (int)volume.Value;
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vlcPlayer.Position = (float)seekBar.Value;
            UpdatePlayerTimes();
        }

        private void VlcPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            // this event is called when vlcplayer has finished initialization not when the media is loaded
        }

        private void ToggleFullScreen()
        {
            // out of fullscreen
            if (WindowStyle == WindowStyle.None)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.CanResize;
            }
            else // into fullscreen
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;
            }
        }

        private void VlcPlayer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleFullScreen();
        }

        private void VlcPlayer_PositionChanged(object sender, EventArgs e) // will not be called when you set the position :| - it's only called when the media is playing
        {
            SeekUpdate();
        }

        private void SeekUpdate()
        {
            seekBar.Value = vlcPlayer.Position;
            UpdatePlayerTimes();
        }

        private void UpdatePlayerTimes()
        {
            var elapsedTimeSpan = TimeSpan.FromSeconds(vlcPlayer.Position * vlcPlayer.Length.TotalSeconds);
            var totalOrRemainingTimeSpan = TimeSpan.FromSeconds(vlcPlayer.Length.TotalSeconds);
            if (totalOrRemainingTimeSpan.Hours > 0)
            {
                elapsedTime.Text = elapsedTimeSpan.ToString(@"hh\:mm\:ss");
                totalOrRemainingTime.Text = totalOrRemainingTimeSpan.ToString(@"hh\:mm\:ss");
            }
            else
            {
                elapsedTime.Text = elapsedTimeSpan.ToString(@"mm\:ss");
                totalOrRemainingTime.Text = totalOrRemainingTimeSpan.ToString(@"mm\:ss");
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
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
                vlcPlayer.Play();
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            vlcPlayer.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            vlcPlayer.Pause();
        }

        private void StopPlayback()
        {
            if (!vlcPlayerStopped)
            {
                vlcPlayer.Stop();
                seekBar.Value = 0;
                vlcPlayerStopped = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopPlayback();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            hideTimer.Stop();
            Mouse.OverrideCursor = null; // Show cursor
            controlBar.Visibility = Visibility.Visible; // Show control bar
            hideTimer.Start();
        }

        private void VlcPlayer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > -1) // scroll up
            {
                if (volume.Value < 100)
                {
                    volume.Value = volume.Value + 5;
                }
            }
            else // scroll down
            {
                if (volume.Value > 0)
                {
                    volume.Value = volume.Value - 5;
                }
            }
        }

        private void MainWindow_Closing(object sender, EventArgs e)
        {
            vlcPlayer.Stop();
            vlcPlayer.Dispose();
        }
    }
}
