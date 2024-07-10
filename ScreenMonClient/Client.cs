using ScreenMonAPI;
using ScreenMonAPI.Messages;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;

namespace ScreenMonClient
{
    // 维护连接、接收命令
    internal class Client : IDisposable
    {
        private readonly AppClient _client;
        public Client(AppClient client)//构造函数
        {
            _client = client;
        }

        public Client(string host, int port)
        {
            _client = AppClient.ConnectToServer(host, port);
        }
        public void Dispose() => _client.Dispose();

        public ResponseMessage Register(string username, string password)
        {
            var registerMessage = new RegisterMessage { Username = username, Password = password };
            _client.SendMessage(registerMessage);

            var responsePacket = _client.RecvPacket();
            return (ResponseMessage)responsePacket.Message;
        }

        public ResponseMessage Authenticate(string username, string password, string clientMac)
        {
            var authMessage = new AuthMessage { Username = username, Password = password, ClientMac = clientMac };
            _client.SendMessage(authMessage);

            var responsePacket = _client.RecvPacket();
            return (ResponseMessage)responsePacket.Message;
        }

        public void SendImage(Bitmap image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Png);
            var imageMessage = new ImageMessage { ImageData = ms.ToArray(), Timestamp = DateTime.Now };
            var packet = new AppPacket { Message = imageMessage };
            _client.SendPacket(packet);
        }

        public TimeSpan RecvFrequency()
        {
            var responsePacket = _client.RecvPacket();
            var responseMessage = (FrequencyChangeMessage)responsePacket.Message;
            return TimeSpan.FromSeconds(responseMessage.NewFrequency);
        }
    }
}
