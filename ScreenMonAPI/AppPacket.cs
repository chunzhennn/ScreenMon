using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ScreenMonAPI.Messages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenMonAPI
{
    public enum AppPacketType
    {
        Response,
        Register,
        Auth,
        FrequencyChange,
        Image,
        EncryptedPacket,
        RsaKey,
        AesKey
    }

    public class AppPacket
    {
        public AppPacketType Type { get; private init; } // 数据包类型

        private readonly IAppMessage _message = null!; //消息接口

        public required IAppMessage Message
        {
            get => _message;
            init
            {
                ArgumentNullException.ThrowIfNull(value); // 检查消息是否为空
                Type = value switch // 根据消息类型设置数据包类型
                {
                    ResponseMessage => AppPacketType.Response,
                    RegisterMessage => AppPacketType.Register,
                    AuthMessage => AppPacketType.Auth,
                    FrequencyChangeMessage => AppPacketType.FrequencyChange,
                    ImageMessage => AppPacketType.Image,
                    EncryptedPacketMessage => AppPacketType.EncryptedPacket,
                    RsaKeyMessage => AppPacketType.RsaKey,
                    AesKeyMessage => AppPacketType.AesKey,
                    _ => throw new InvalidPacketException("Invalid message type") //异常处理
                };
                _message = value;
            }
        }
    }

    // 自定义的JsonConverter，用于将AppPacket转换为JSON字符串
    public class MessageConverter : JsonConverter<AppPacket>
    {
        protected virtual AppPacket ReadPacket(JsonElement root)
        {
            var packetType = root.TryGetProperty("Type", out var element)
                ? (AppPacketType)element.GetUInt32()
                : throw new InvalidPacketException("Packet missing type field");
            if (root.TryGetProperty("Message", out var message) == false)
            {
                throw new InvalidPacketException("Packet missing message field");
            }
            return packetType switch //根据数据包类型反序列化消息
            {
                AppPacketType.Response => new AppPacket
                {
                    Message = message.Deserialize<ResponseMessage>() ??
                              throw new InvalidPacketException("Malformed message")
                },
                AppPacketType.Register => new AppPacket
                {
                    Message = message.Deserialize<RegisterMessage>() ??
                              throw new InvalidPacketException("Malformed message")
                },
                AppPacketType.Auth => new AppPacket
                {
                    Message = message.Deserialize<AuthMessage>() ??
                              throw new InvalidPacketException("Malformed message")
                },
                AppPacketType.FrequencyChange => new AppPacket
                {
                    Message = message.Deserialize<FrequencyChangeMessage>() ??
                              throw new InvalidPacketException("Malformed message")
                },
                AppPacketType.Image => new AppPacket
                {
                    Message = message.Deserialize<ImageMessage>() ??
                              throw new InvalidPacketException("Malformed message")
                },
                AppPacketType.EncryptedPacket => new AppPacket
                {
                    Message  = message.Deserialize<EncryptedPacketMessage>() ??
                           throw new InvalidPacketException("Malformed message")
                },
                AppPacketType.RsaKey => new AppPacket
                {
                    Message = message.Deserialize<RsaKeyMessage>() ??
                              throw new InvalidPacketException("Malformed message")
                },
                AppPacketType.AesKey => new AppPacket
                {
                    Message = message.Deserialize<AesKeyMessage>() ??
                              throw new InvalidPacketException("Malformed message")
                },
                _ => throw new InvalidPacketException($"Unknown type: {packetType}"),
            };
        }
        public override AppPacket Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return ReadPacket(doc.RootElement);
        }

        public override void Write(Utf8JsonWriter writer, AppPacket value, JsonSerializerOptions options)
        {
            writer.WriteStartObject(); //开始写入JSON对象
            writer.WriteNumber("Type", (int)value.Type); // 写入数据包类型

            writer.WritePropertyName("Message");
            var messageString = JsonSerializer.Serialize(value.Message, value.Message.GetType());
            writer.WriteRawValue(messageString);

            writer.WriteEndObject(); // 结束写入JSON对象
        }
    }

    // 自定义加密消息转换器
    public class EncryptedMessageConverter(Aes aes) : MessageConverter
    {
        private readonly JsonSerializerOptions _rawMessageJsonSerializerOptions =
            new() { Converters = { new MessageConverter() }, WriteIndented = false };

        protected override AppPacket ReadPacket(JsonElement root)
        {
            var packetType = root.TryGetProperty("Type", out var element)
                ? (AppPacketType)element.GetUInt32()
                : throw new InvalidPacketException("Packet missing type field");
            if (packetType != AppPacketType.EncryptedPacket)
                return base.ReadPacket(root); // 如果不是加密数据包，调用基类方法
            if (root.TryGetProperty("Message", out var message) == false)
            {
                throw new InvalidPacketException("Packet missing message field");
            }

            var encryptedMessage = message.Deserialize<EncryptedPacketMessage>() ??
                                   throw new InvalidPacketException("Malformed message");
            var encryptedBytes = encryptedMessage.EncryptedBytes; // 获取加密字节
            var decryptedBytes = aes.DecryptCfb(encryptedBytes, aes.IV); // 解密字节
            using var ms = new MemoryStream(decryptedBytes);
            using var gzipStream = new GZipStream(ms, CompressionMode.Decompress); // 解压缩数据
            using var resultStream = new MemoryStream();
            gzipStream.CopyTo(resultStream);
            var decompressedBytes = resultStream.ToArray();

            var packetString = Encoding.UTF8.GetString(decompressedBytes);
            return base.ReadPacket(JsonDocument.Parse(packetString).RootElement); // 反序列化解压缩后的数据包
        }

        public override void Write(Utf8JsonWriter writer, AppPacket value, JsonSerializerOptions options)
        {
            writer.WriteStartObject(); // 开始写入JSON对象
            writer.WriteNumber("Type", (int)AppPacketType.EncryptedPacket); // 写入加密数据包类型

            writer.WritePropertyName("Message"); // 写入消息属性
            var packetString = JsonSerializer.Serialize(value, value.GetType(), _rawMessageJsonSerializerOptions);
            var rawBytes = Encoding.UTF8.GetBytes(packetString); // 转换为字节数组

            using var ms = new MemoryStream();
            using var compressor = new GZipStream(ms, CompressionLevel.SmallestSize); // 压缩数据
            compressor.Write(rawBytes, 0, rawBytes.Length);
            compressor.Close();
            var compressedBytes = ms.ToArray();
            var encryptedBytes = aes.EncryptCfb(compressedBytes, aes.IV); // 加密压缩后的数据
            var message = new EncryptedPacketMessage
            {
                EncryptedBytes = encryptedBytes
            };
            var messageString = JsonSerializer.Serialize(message, message.GetType());
            writer.WriteRawValue(messageString); // 写入加密消息

            writer.WriteEndObject(); // 结束写入JSON对象
        }
    }
}