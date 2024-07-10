using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace ScreenMonAPI
{
    public class AppServer: IDisposable
    {
        private readonly TcpListener _listener; // 用于监听传入的TCP连接
        private int _disposed = 0; // 标志是否已释放资源
        private readonly RSA _rsa = RSA.Create(); // 创建一个RSA实例，用于加密/解密
        private readonly BlockingCollection<AppClient> _clients = []; // 用于存储已连接的客户端

        public void Dispose()
        {
            Dispose(true); // 释放托管资源
            GC.SuppressFinalize(this); // 阻止终结器调用
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1) return; // 防止多次释放

            if (disposing)
            {
                _listener.Dispose(); // 释放TcpListener资源
                _rsa.Dispose(); // 释放RSA实例资源
            }

        }

        ~AppServer()
        {
            Dispose(false); // 在终结器中调用Dispose方法
        }

        public AppServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port); // 初始化TcpListener以监听指定端口
            _listener.Start(); // 启动监听
            new Thread(ListenForNewClients).Start(); // 启动一个新线程以监听新的客户端连接
        }

        private void ListenForNewClients()
        {
            while (true)
            {
                var client = _listener.AcceptTcpClient(); // 接受传入的客户端连接
                new Thread(() =>
                {
                    var appClient = AppClient.FromNewTcpClient(client, _rsa); // 使用传入的客户端连接和RSA实例创建一个AppClient
                    _clients.Add(appClient); // 将新的AppClient添加到客户端集合中
                }).Start(); // 启动一个新线程以处理新的客户端连接
            }
        }

        public AppClient AcceptClient()
        {
            return _clients.Take(); // 从客户端集合中获取一个已连接的客户端
        }
    }
}
