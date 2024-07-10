using ScreenMonAPI;
using System.Net.NetworkInformation;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Threading.Timer;

namespace ScreenMonClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NotifyIcon _trayIcon;
        private Client? _client;
        private Timer? _timer;
        private bool _running = false;

        public MainWindow()
        {
            InitializeComponent();

            _trayIcon = new NotifyIcon();

            using var iconStream = Application.GetResourceStream(new Uri("Resources/monitor.ico", UriKind.Relative))
                ?.Stream;
            _trayIcon.Icon = new Icon(iconStream ?? throw new ArgumentNullException());

            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("退出", null, ExitApplication);

            // 将上下文菜单添加到NotifyIcon
            _trayIcon.ContextMenuStrip = trayMenu;

            _trayIcon.Visible = false;
        }

        private void ExitApplication(object? sender, EventArgs args)
        {
            Environment.Exit(0);
        }

        private static string? GetMacAddress()
        {
            var macAddr =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault();
            return macAddr;
        }

        private void OnServerDisconnected()
        {
            _trayIcon.ShowBalloonTip(1, "ScreenMon", "连接断开", ToolTipIcon.Warning);
            _trayIcon.Visible = false;
            StatusLabel.Content = "未连接";
            Visibility = Visibility.Visible;
            WindowState = WindowState.Normal;
            RegisterButton.IsEnabled = true;
            LoginButton.IsEnabled = true;
            Show();
        }

        private void Register_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                if (_client == null)
                {
                    _client = new Client(IPTextBox.Text, int.Parse(PortTextBox.Text));
                    StatusLabel.Content = "已连接 未登录";
                }
            }
            catch
            {
                MessageBox.Show("无效的服务器地址或端口", "错误");
                return;
            }

            try
            {
                var response = _client.Register(UsernameTextBox.Text, PasswordTextBox.Text);
                if (!response.Success)
                {
                    MessageBox.Show(response.Message, "注册失败");
                    return;
                }

                MessageBox.Show("注册成功");
            }
            catch (Exception e)
            {
                _client?.Dispose();
                _client = null;
                StatusLabel.Content = "未连接";
                MessageBox.Show(e.Message, "注册失败");
            }
        }

        private void Login_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                if (_client == null)
                {
                    _client = new Client(IPTextBox.Text, int.Parse(PortTextBox.Text));
                    StatusLabel.Content = "已连接 未登录";
                }
            }
            catch
            {
                MessageBox.Show("无效的服务器地址或端口", "错误");
                return;
            }

            try
            {
                var response = _client.Authenticate(UsernameTextBox.Text, PasswordTextBox.Text,
                    GetMacAddress() ?? throw new InvalidOperationException("无法获取MAC地址，未联网？"));
                if (!response.Success)
                {
                    MessageBox.Show(response.Message, "登录失败");
                    return;
                }

                _running = true;
                StatusLabel.Content = "已连接 已登录";
                LoginButton.IsEnabled = false;
                RegisterButton.IsEnabled = false;
                _timer = new Timer(SendScreenShot, null, TimeSpan.Zero, TimeSpan.FromSeconds(5.0));
                new Thread(ListenFrequencyChange).Start();
                _trayIcon.Visible = true;
                _trayIcon.ShowBalloonTip(1, "ScreenMon", "已最小化到系统托盘", ToolTipIcon.Info);
                Hide();
            }
            catch (Exception e)
            {
                _client?.Dispose();
                _client = null;
                StatusLabel.Content = "未连接";
                MessageBox.Show(e.Message, "登录失败");
            }

        }

        private void SendScreenShot(object? _)
        {
            if (!_running)
            {
                return;
            }
            var image = ScreenShot.CaptureScreen();
            try
            {
                _client?.SendImage(image);
            }
            catch (System.IO.IOException)
            {
                _running = false;
                _timer?.Dispose();
                _timer = null;
                _client?.Dispose();
                _client = null;
            }

        }

        private void ListenFrequencyChange(object? _)
        {
            try
            {
                while (_running)
                {
                    var newSpan = _client?.RecvFrequency();
                    _timer?.Change(TimeSpan.Zero, newSpan ?? throw new InvalidPacketException("new frequency is null"));
                }
            }
            catch (System.IO.IOException)
            {
                _running = false;
                _timer?.Dispose();
                _timer = null;
                _client?.Dispose();
                _client = null;
            }
            finally
            {
                Dispatcher.Invoke(OnServerDisconnected);
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
