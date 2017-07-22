using System;
using System.Windows;
using System.Windows.Controls;

namespace Synced_Player
{
    /// <summary>
    /// Interaction logic for PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        public PreferencesWindow()
        {
            InitializeComponent();

            serverAddress.Text = Properties.Settings.Default.ServerAddress;
            roomName.Text = Properties.Settings.Default.RoomName;
            username.Text = Properties.Settings.Default.Username;
            pauseAndOpenChatOnMessageReceived.IsChecked = Properties.Settings.Default.PauseAndOpenChatOnMessageReceived;
            dontPlayOnMediaLoad.IsChecked = Properties.Settings.Default.DontPlayOnMediaLoad;

            // since we don't want the TextChanged events while running the above statements, event handlers are placed here:
            serverAddress.TextChanged += ServerAddress_TextChanged;
            roomName.TextChanged += RoomName_TextChanged;
            username.TextChanged += Username_TextChanged;

            if (string.IsNullOrWhiteSpace(serverAddress.Text) || string.IsNullOrWhiteSpace(roomName.Text) || string.IsNullOrWhiteSpace(username.Text))
            {
                joinRoom.IsEnabled = true;
            }
        }

        private void ServerAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.ServerAddress = serverAddress.Text;
            Properties.Settings.Default.Save();

            JoinRoomButtonToggler(serverAddress);
        }

        private void RoomName_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.RoomName = roomName.Text;
            Properties.Settings.Default.Save();

            JoinRoomButtonToggler(roomName);
        }

        private void Username_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.Username = username.Text;
            Properties.Settings.Default.Save();

            JoinRoomButtonToggler(username);
        }

        private void JoinRoom_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(serverAddress.Text) && !string.IsNullOrWhiteSpace(roomName.Text) && !string.IsNullOrWhiteSpace(username.Text))
            {
                joinRoom.IsEnabled = false;
                ((MainWindow)Owner).LeaveRoom(); // just in case if we're already connected
                ((MainWindow)Owner).JoinRoom();
            }
        }

        private void JoinRoomButtonToggler(TextBox targetTextBox)
        {
            if (string.IsNullOrWhiteSpace(targetTextBox.Text))
            {
                joinRoom.IsEnabled = false;
            }
            else if (!string.IsNullOrWhiteSpace(serverAddress.Text) && !string.IsNullOrWhiteSpace(roomName.Text) && !string.IsNullOrWhiteSpace(username.Text))
            {
                joinRoom.IsEnabled = true;
            }
        }

        private void PreferencesWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }

        private void PauseAndOpenChatOnMessageReceived_Click(object sender, RoutedEventArgs e)
        {
            if (pauseAndOpenChatOnMessageReceived.IsChecked == true)
            {
                Properties.Settings.Default.PauseAndOpenChatOnMessageReceived = true;
            }
            else
            {
                Properties.Settings.Default.PauseAndOpenChatOnMessageReceived = false;
            }
            Properties.Settings.Default.Save();
        }

        private void DontPlayOnMediaLoad_Click(object sender, RoutedEventArgs e)
        {
            if (dontPlayOnMediaLoad.IsChecked == true)
            {
                Properties.Settings.Default.DontPlayOnMediaLoad = true;
            }
            else
            {
                Properties.Settings.Default.DontPlayOnMediaLoad = false;
            }
            Properties.Settings.Default.Save();
        }
    }
}
