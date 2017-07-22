using System;
using WebSocketSharp;
using Newtonsoft.Json.Linq;

namespace Synced_Player
{
    class SyncClient
    {
        public event EventHandler<ConnectionEventArgs> ConnectionChanged;
        public event EventHandler<SyncEventArgs> SeekToReceived;
        public event EventHandler<SyncEventArgs> PauseReceived;
        public event EventHandler<SyncEventArgs> PlayReceived;
        public event EventHandler<ChatEventArgs> ChatReceived;

        private String server;
        private String room;
        private String user;
        private WebSocket ws;

        public SyncClient(String server, String room, String user)
        {
            this.server = server;
            this.room = room;
            this.user = user;
        }

        public bool Connect()
        {
            try
            {
                ws = new WebSocket(server);
                ws.OnOpen += WS_OnOpen;
                ws.OnMessage += WS_OnMessage;
                ws.OnClose += WS_OnClose;
                ws.Connect();
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private void WS_OnOpen(object sender, EventArgs e)
        {
            OnConnectionChanged(new ConnectionEventArgs(ConnectionStatus.Connected));
            // join room
            JObject JsonObj = new JObject();
            JsonObj["room"] = room;
            JsonObj["user"] = user;
            ws.Send(JsonObj.ToString());
        }

        private void WS_OnMessage(object sender, MessageEventArgs e)
        {
            if(!e.Data.IsNullOrEmpty())
            {
                JObject receivedMessage = JObject.Parse(e.Data);
                //Console.WriteLine(receivedMessage);
                String receivedMessageAction = receivedMessage["action"].ToObject<String>();
                String receivedMessageUser = receivedMessage["user"].ToObject<String>();
                String receivedMessageSeekTime = receivedMessage["seekTime"].ToObject<String>();
                switch (receivedMessageAction)
                {
                    case "seekTo":
                        OnSeekToReceived(new SyncEventArgs(receivedMessageUser, receivedMessageSeekTime));
                        break;
                    case "pause":
                        OnPauseReceived(new SyncEventArgs(receivedMessageUser, receivedMessageSeekTime));
                        break;
                    case "play":
                        OnPlayReceived(new SyncEventArgs(receivedMessageUser, receivedMessageSeekTime));
                        break;
                    case "chat":
                        String chatMessage = receivedMessage["message"].ToObject<String>();
                        OnChatReceived(new ChatEventArgs(receivedMessageUser, receivedMessageSeekTime, chatMessage));
                        break;
                }
            }
        }

        private void WS_OnClose(object sender, CloseEventArgs e)
        {
            OnConnectionChanged(new ConnectionEventArgs(ConnectionStatus.Disconnected));
        }

        public void SendSeekTo(String seekTime)
        {
            if (ws.ReadyState == WebSocketState.Open)
            {
                JObject JsonObj = new JObject();
                JsonObj["action"] = "seekTo";
                JsonObj["seekTime"] = seekTime;
                ws.Send(JsonObj.ToString());
            }
            else if (ws.ReadyState == WebSocketState.Connecting)
            {
                Console.WriteLine("Couldn't SendSeekTo because the socket is still connecting");
            }
        }

        public void SendPause(String seekTime)
        {
            if (ws.ReadyState == WebSocketState.Open)
            {
                JObject JsonObj = new JObject();
                JsonObj["action"] = "pause";
                JsonObj["seekTime"] = seekTime;
                ws.Send(JsonObj.ToString());
            }
            else if (ws.ReadyState == WebSocketState.Connecting)
            {
                Console.WriteLine("Couldn't SendPause because the socket is still connecting");
            }
        }

        public void SendPlay(String seekTime)
        {
            if (ws.ReadyState == WebSocketState.Open)
            {
                JObject JsonObj = new JObject();
                JsonObj["action"] = "play";
                JsonObj["seekTime"] = seekTime;
                ws.Send(JsonObj.ToString());
            }
            else if (ws.ReadyState == WebSocketState.Connecting)
            {
                Console.WriteLine("Couldn't SendPlay because the socket is still connecting");
            }
        }

        public void SendChatMessage(String seekTime, String message)
        {
            if (ws.ReadyState == WebSocketState.Open)
            {
                JObject JsonObj = new JObject();
                JsonObj["action"] = "chat";
                JsonObj["seekTime"] = seekTime;
                JsonObj["message"] = message;
                ws.Send(JsonObj.ToString());
            }
            else if (ws.ReadyState == WebSocketState.Connecting)
            {
                Console.WriteLine("Couldn't SendChatMessage because the socket is still connecting");
            }
        }

        public void Close()
        {
            ws.Close();
        }

        protected virtual void OnConnectionChanged(ConnectionEventArgs e)
        {
            ConnectionChanged?.Invoke(this, e);
        }

        protected virtual void OnSeekToReceived(SyncEventArgs e)
        {
            SeekToReceived?.Invoke(this, e);
            // equivalent to:
            //if (SeekToReceived != null)
            //{
            //    SeekToReceived(this, e);
            //}
            // just a learning note
        }

        protected virtual void OnPauseReceived(SyncEventArgs e)
        {
            PauseReceived?.Invoke(this, e);
        }

        protected virtual void OnPlayReceived(SyncEventArgs e)
        {
            PlayReceived?.Invoke(this, e);
        }

        protected virtual void OnChatReceived(ChatEventArgs e)
        {
            ChatReceived?.Invoke(this, e);
        }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public ConnectionStatus Status { get; private set; }

        public ConnectionEventArgs(ConnectionStatus status)
        {
            Status = status;
        }
    }

    public class SyncEventArgs : EventArgs
    {
        public string User { get; private set; }
        public string SeekTime { get; private set; }

        public SyncEventArgs(String user, String seekTime)
        {
            User = user;
            SeekTime = seekTime;
        }
    }

    public class ChatEventArgs : SyncEventArgs
    {
        public string ChatMessage { get; private set; }

        public ChatEventArgs(string user, string seekTime, string chatMessage) : base(user, seekTime)
        {
            ChatMessage = chatMessage;
        }
    }
}
