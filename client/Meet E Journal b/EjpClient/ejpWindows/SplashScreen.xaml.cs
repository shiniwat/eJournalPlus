using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;

namespace ejpClient.ejpWindows
{
    public delegate void ao_AutoClose();

    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        System.Timers.Timer tm;
        public SplashScreen(int seconds, bool runAsAbout)
        {
            InitializeComponent();
            if (runAsAbout)
            {
                this.WindowStyle = WindowStyle.ToolWindow;
                //Always collapsed for now...
                this._l_reportBugLink.Visibility = Visibility.Collapsed;
            }
            else
            {
                this._l_reportBugLink.Visibility = Visibility.Collapsed;

                this.WindowStyle = WindowStyle.None;
                this.tm = new System.Timers.Timer(seconds * 1000);
                this.tm.Elapsed += new ElapsedEventHandler(tm_Elapsed);
                this.tm.Start();
            }
        }

        private void AutoClose()
        {
            this.Close();
        }

        private void tm_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ao_AutoClose(this.AutoClose));
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if(tm != null)
                tm.Dispose();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(((Hyperlink)e.Source).NavigateUri.ToString(), string.Empty);
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("只今報告のサイトに接続出来ません。\nネットワークの接続を確認して下さい。", 
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
