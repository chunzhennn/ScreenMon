using System.Windows;

namespace ScreenMonServer
{
    /// <summary>
    /// ChangeFrequencyDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ChangeFrequencyDialog : Window
    {
        public int Frequency;
        public ChangeFrequencyDialog()
        {
            InitializeComponent();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var isValid = int.TryParse(FrequencyTextBox.Text, out Frequency) && Frequency > 0;
            if (!isValid)
            {
                MessageBox.Show("无效的频率");
                return;
            }

            DialogResult = true;
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
