using ScreenMonAPI.Messages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ScreenMonServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Server _server = ((App)Application.Current).server;
        private readonly AppDbContext _context = new();
        private readonly ObservableCollection<User> _users;
        public MainWindow()
        {
            InitializeComponent();
            _users = new ObservableCollection<User>(_context.Users);
        }


        private void ExitApplication(object? sender, EventArgs args)
        {
            Environment.Exit(0);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            _server.NewUser += (_, user) =>
            {
                Dispatcher.Invoke( ()=> _users.Add(user));
            };
            _server.ClientLoggedIn += (_, user) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var element = _users.First(u => u.Id == user.Id);
                    element.IsOnline = true;
                    element.CurrentSession = user.CurrentSession;
                    element.LastLoginTime = user.LastLoginTime;
                    element.Sessions = user.Sessions;
                    CollectionViewSource.GetDefaultView(_users).Refresh();
                });
            };
            _server.ClientDisconnected += (_, user) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var element = _users.First(u => u.Id == user.Id);
                    element.IsOnline = false;
                    element.CurrentSession = null;
                    CollectionViewSource.GetDefaultView(_users).Refresh();
                });
            };
            ClientListView.ItemsSource = _users;
        }


        private void ClientListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ClientListView.SelectedItem is not User user || user.IsOnline == false) return;
            Debug.Assert(user.CurrentSession!=null, "user.CurrentSession!=null");
            var viewWindow = new ScreenViewer(user.CurrentSession);
            viewWindow.Show();
            _server.ClientDisconnected += (_, u) =>
            {
                if (u.Id != user.Id) return;
                Dispatcher.Invoke(() =>
                {
                    viewWindow.Close();
                });
            };
        }

        private void KickClient_Click(object sender, RoutedEventArgs e)
        {
            if (ClientListView.SelectedItem is not User user || user.IsOnline == false) return;
            Debug.Assert(user.CurrentSession != null, "user.CurrentSession != null");
            _server.Clients[user.CurrentSession.Id].Dispose();
        }

        private void ChangeFrequency_Click(object sender, RoutedEventArgs e)
        {
            if (ClientListView.SelectedItem is not User user || user.IsOnline == false) return;
            var dialog = new ChangeFrequencyDialog();
            var result = dialog.ShowDialog();
            if (result is null or false) return;
            var frequency = dialog.Frequency;
            Debug.Assert(user.CurrentSession != null, "user.CurrentSession != null");
            _server.Clients.GetValueOrDefault(user.CurrentSession.Id)
                ?.SendMessage(new FrequencyChangeMessage { NewFrequency = frequency });
        }

        private void ViewHistory_OnClick(object sender, RoutedEventArgs e)
        {
            if (ClientListView.SelectedItem is not User user) return;
            var dialog = new ManageHistoryDialog(user, _server.ImageManager);
            dialog.ShowDialog();
        }

    }
}