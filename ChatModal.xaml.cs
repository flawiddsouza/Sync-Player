using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace Synced_Player
{
    /// <summary>
    /// Interaction logic for ChatModal.xaml
    /// </summary>
    public partial class ChatModal : Window
    {
        public ChatModal()
        {
            InitializeComponent();
        }

        private void ChatInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Enter:
                    if (!string.IsNullOrWhiteSpace(chatInput.Text))
                    {
                        ((MainWindow)Owner).SendChat(chatInput.Text);
                        ((MainWindow)Owner).Messages.Add("You [" + ((MainWindow)Owner).StringifyElapsedTime() + "]: " + chatInput.Text);
                        chatInput.Clear();
                        ListBoxScrollToBottom(chatMessages);
                        e.Handled = true;
                    }
                    else
                    {
                        Close();
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void ChatMessages_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.E:
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Text file (*.txt)|*.txt";
                    saveFileDialog.FileName = "Synced-Player_ChatLog_" + DateTime.Now.ToString("dd-MMM-yy_hh-mm-ss-tt");
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        File.WriteAllLines(saveFileDialog.FileName, ((MainWindow)Owner).Messages.ToList());
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void PreferencesWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        // from https://stackoverflow.com/a/18305272
        private void ListBoxScrollToBottom(ListBox listBox)
        {
            ListBoxAutomationPeer svAutomation = (ListBoxAutomationPeer)UIElementAutomationPeer.CreatePeerForElement(chatMessages);
            IScrollProvider scrollInterface = (IScrollProvider)svAutomation.GetPattern(PatternInterface.Scroll);
            System.Windows.Automation.ScrollAmount scrollVertical = System.Windows.Automation.ScrollAmount.LargeIncrement;
            System.Windows.Automation.ScrollAmount scrollHorizontal = System.Windows.Automation.ScrollAmount.NoAmount;
            if (scrollInterface.VerticallyScrollable)
            {
                scrollInterface.Scroll(scrollHorizontal, scrollVertical);
            }
        }

        private void ChatModal_Loaded(object sender, RoutedEventArgs e)
        {
            ListBoxScrollToBottom(chatMessages);
            chatInput.Focus();
        }
    }
}
