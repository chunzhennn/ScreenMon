using ScreenMonAPI.Messages;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenMonServer
{
    /// <summary>
    /// ScreenViewer.xaml 的交互逻辑
    /// </summary>
    public partial class ScreenViewer : Window
    {
        private readonly Session _session;
        private readonly Server _server = ((App)Application.Current).server;
        internal ScreenViewer(Session session)
        {
            _session = session;
            InitializeComponent();
            Title = $"{_session.User.Name}@{_session.Ip}";
        }

        public void ImagePacketHandler(object sender, byte[] imageDataBytes, Session session)
        {
            if (session.Id != _session.Id)
            {
                return;
            }
            ScreenImage.Dispatcher.Invoke(() =>
            {
                using var stream = new MemoryStream(imageDataBytes, false);
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = stream;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                ScreenImage.Source = image;
            });
        }

        private void ScreenViewer_OnLoaded(object sender, RoutedEventArgs e)
        {
            _server.ImageReceived += ImagePacketHandler;
        }

        private void ScreenViewer_OnClosing(object? sender, CancelEventArgs e)
        {
            _server.ImageReceived -= ImagePacketHandler;
        }

    }
}
