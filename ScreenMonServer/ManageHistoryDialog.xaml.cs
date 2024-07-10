using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using Path = System.IO.Path;

namespace ScreenMonServer
{
    /// <summary>
    /// ManageHistoryDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ManageHistoryDialog : Window
    {
        private readonly User _user;
        private readonly ImageManager _imageManager;
        public ManageHistoryDialog(User user, ImageManager imageManager)
        {
            _user = user;
            _imageManager = imageManager;
            InitializeComponent();
        }

        private void DeleteHistory_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private static void OpenFile(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }

        private static void OpenDirectory(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void OpenHistoryImages(object sender, RoutedEventArgs e)
        {
            if (HistoryListView.SelectedItem is not Session selectedClient) return;
            OpenDirectory(Path.Combine(_imageManager.GetImageDirectory(selectedClient.Id)));
            DialogResult = true;
        }

        private void ManageHistoryDialog_OnLoaded(object sender, RoutedEventArgs e)
        {
            HistoryListView.ItemsSource = _user.Sessions;
            var view = CollectionViewSource.GetDefaultView(HistoryListView.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("LoginTime", ListSortDirection.Ascending));
        }
    }
}
