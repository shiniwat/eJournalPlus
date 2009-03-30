using System.Windows;
using System.Windows.Input;

namespace SiliconStudio.Meet.EjpControls.Windows
{
    /// <summary>
    /// Interaction logic for ReportUrlInputWindow.xaml
    /// </summary>
    public partial class ReportUrlInputWindow : Window
    {
        private bool _cancelled;
        public bool Cancelled
        {
            get
            {
                return this._cancelled;
            }
        }

        public string Explanation { get { return this._tb_Explanation.Text; } }
        public string Url { get { return this._tb_Url.Text; } }

        public ReportUrlInputWindow(string linkExplanation, string url)
        {
            InitializeComponent();
            this._tb_Explanation.Text = linkExplanation;
            //this._tb_Url.Text = ;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this._cancelled = true;
            this.Close();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            bool doIt = true;
            if (this._tb_Explanation.Text.Length == 0)
            {
                this._tb_Explanation.Text = "説明を入力して下さい!";
                doIt = false;
            }
            if (this._tb_Url.Text.Length == 0 || this._tb_Url.Text.StartsWith(@"http://") == false)
            {
                MessageBox.Show("正しいUrlを入力して下さい\n\n必ず「http://」から入力して下さい。", "", MessageBoxButton.OK, MessageBoxImage.Error);
                doIt = false;
            }
            if (doIt)
            {
                this._cancelled = false;
                this.Close();
            }
        }

        private void Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
