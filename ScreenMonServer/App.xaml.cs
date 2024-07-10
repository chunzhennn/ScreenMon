using System.Windows;

namespace ScreenMonServer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal readonly Server server = new(12346);

        public App()
        {
            server.Start();
        }
    }

}
