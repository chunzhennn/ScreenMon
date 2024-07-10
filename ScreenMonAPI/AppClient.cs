using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ScreenMonAPI.Messages;

namespace ScreenMonAPI
{
    public class AppClient : IDisposable
    {
        private readonly TcpClient _tcpClient; // 用于管理TCP连接的客户端
        private readonly NetworkStream _stream; // 网络流，用于数据读写
        private readonly StreamReader _streamReader; // 读取网络流中的数据
        private Aes? _aes = null; // AES加密算法的实例

        private readonly JsonSerializerOptions _rawJsonSerializerOptions =
            new() { Converters = { new MessageConverter() }, WriteIndented = false }; // JSON序列化选项，用于未加密数据

        private JsonSerializerOptions? _encryptedJsonSerializerOptions = null; // JSON序列化选项，用于加密数据
        private int _disposed = 0; // 标志是否已释放资源

        public string Ip => (_tcpClient.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ??
                            throw new InvalidOperationException("RemoteEndPoint is not IPEndpoint"); // 获取远程IP地址

        private AppClient(TcpClient client)
        {
            _tcpClient = client; // 初始化TCP客户端
            _stream = _tcpClient.GetStream(); // 获取网络流
            _streamReader = new StreamReader(_stream); // 初始化StreamReader
        }

        private AppClient(string host, int port)
        {
            _tcpClient = new TcpClient(host, port); // 连接到指定主机和端口的TCP服务器
            _stream = _tcpClient.GetStream(); // 获取网络流
            _streamReader = new StreamReader(_stream); // 初始化StreamReader
        }

        public static AppClient ConnectToServer(string host, int port)
        {
            var client = new AppClient(host, port); // 创建一个新的AppClient实例并连接到服务器

            var packet = client.RecvRawPacket(); // 接收服务器发送的原始数据包
            if (packet.Type != AppPacketType.RsaKey || packet.Message is not RsaKeyMessage rsaMessage)
            {
                throw new InvalidPacketException("Server responds with weird packet"); // 如果数据包类型不是RsaKey或消息类型不正确，抛出异常
            }

            var rsa = RSA.Create(); // 创建一个新的RSA实例
            rsa.ImportRSAPublicKey(rsaMessage.RsaPublicKeyBytes, out _); // 导入服务器的RSA公钥
            client._aes = Aes.Create(); // 创建一个新的AES实例
            var messageString = JsonSerializer.Serialize(new AppPacket
            {
                Message = new AesKeyMessage
                {
                    AesIvBytes = client._aes.IV, // 包含AES IV的消息
                    AesKeyBytes = client._aes.Key // 包含AES密钥的消息
                }
            }, client._rawJsonSerializerOptions); // 序列化AES密钥消息
            client.SendRawPacket(new AppPacket
            {
                Message = new EncryptedPacketMessage{
                    EncryptedBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(messageString), RSAEncryptionPadding.Pkcs1) // 使用RSA加密AES密钥消息
                }
            }); // 发送加密的AES密钥消息
            client._encryptedJsonSerializerOptions = new JsonSerializerOptions()
            {
                Converters = { new EncryptedMessageConverter(client._aes) }, // 使用AES加密消息的JSON序列化选项
                WriteIndented = false
            };
            return client; // 返回已连接的AppClient实例
        }

        public static AppClient FromNewTcpClient(TcpClient tcpClient, RSA rsa)
        {
            var client = new AppClient(tcpClient); // 创建一个新的AppClient实例并初始化TCP客户端

            client.SendRawPacket(new AppPacket
            {
                Message = new RsaKeyMessage
                {
                    RsaPublicKeyBytes = rsa.ExportRSAPublicKey() // 发送包含RSA公钥的消息
                }
            });
            var packet = client.RecvRawPacket(); // 接收客户端发送的原始数据包
            if (packet.Type != AppPacketType.EncryptedPacket ||
                packet.Message is not EncryptedPacketMessage encryptedMessage)
            {
                throw new InvalidPacketException("Client responds with weird packet"); // 如果数据包类型不是EncryptedPacket或消息类型不正确，抛出异常
            }

            var decryptedPacket = rsa.Decrypt(encryptedMessage.EncryptedBytes, RSAEncryptionPadding.Pkcs1); // 使用RSA解密数据包
            var aesPacket = JsonSerializer.Deserialize<AppPacket>(Encoding.UTF8.GetString(decryptedPacket), client._rawJsonSerializerOptions) ??
                         throw new InvalidPacketException("Client responds with weird packet"); // 反序列化AES密钥消息
            if (aesPacket.Message is not AesKeyMessage aesMessage)
            {
                throw new InvalidPacketException("Client responds with weird packet"); // 如果消息类型不正确，抛出异常
            }
            client._aes = Aes.Create(); // 创建一个新的AES实例
            client._aes.IV = aesMessage.AesIvBytes; // 设置AES IV
            client._aes.Key = aesMessage.AesKeyBytes; // 设置AES密钥
            client._encryptedJsonSerializerOptions = new JsonSerializerOptions()
            {
                Converters = { new EncryptedMessageConverter(client._aes) }, // 使用AES加密消息的JSON序列化选项
                WriteIndented = false
            };
            return client; // 返回已初始化的AppClient实例
        }
        public void Dispose()
        {
            Dispose(true); // 释放托管资源
            GC.SuppressFinalize(this); // 阻止终结器调用
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return; // 防止多次释放
            }

            if (disposing)
            {
                // 释放托管资源
                _streamReader.Dispose();
                _stream.Dispose();
                _tcpClient.Dispose();
                _aes?.Dispose();
            }
        }

        ~AppClient()
        {
            Dispose(false); // 在终结器中调用Dispose方法
        }

        public void SendMessage(IAppMessage message)
        {
            SendPacket(new AppPacket { Message = message }); // 发送消息
        }

        private void SendRawPacket(AppPacket packet)
        {
            //序列化packet为JSON字符串
            var jsonString = JsonSerializer.Serialize(packet, _rawJsonSerializerOptions) + '\n'; //添加换行符作为分隔符

            //编码JSON字符串为字节数组
            byte[] data = Encoding.UTF8.GetBytes(jsonString);

            //发送数据
            _stream.Write(data, 0, data.Length);
        }

        public void SendPacket(AppPacket packet)
        {
            //序列化packet为JSON字符串
            var jsonString = JsonSerializer.Serialize(packet, _encryptedJsonSerializerOptions) + '\n'; //添加换行符作为分隔符

            //编码JSON字符串为字节数组
            byte[] data = Encoding.UTF8.GetBytes(jsonString);

            //发送数据
            _stream.Write(data, 0, data.Length);
        }

        private AppPacket RecvRawPacket()
        {
            var message = _streamReader.ReadLine() ?? throw new IOException("Connection EOF"); // 从网络流中读取一行数据

            //反序列化JSON字符串为AppPacket实例
            var packet = JsonSerializer.Deserialize<AppPacket>(message, _rawJsonSerializerOptions) ??
                         throw new InvalidPacketException("JSON deserialization failed");
            return packet;
        }

        public AppPacket RecvPacket()
        {
            var message = _streamReader.ReadLine() ?? throw new IOException("Connection EOF"); // 从网络流中读取一行数据

            //反序列化JSON字符串为AppPacket实例
            var packet = JsonSerializer.Deserialize<AppPacket>(message, _encryptedJsonSerializerOptions) ??
                               throw new InvalidPacketException("JSON deserialization failed");
            return packet;
        }
    }
}
