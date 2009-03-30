using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Windows.Input;

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
			if (tm != null)
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
				System.Windows.MessageBox.Show(Application.Current.Resources["ERR_ErrorReportNetworkFailed"] as string,//Properties.Resources.ERR_ErrorReportNetworkFailed,
					Application.Current.Resources["Str_ConnFailedTitle"] as string,//Properties.Resources.Str_ConnFailedTitle, 
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OnShowEgg(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.RightCtrl) == true)
				this._i_E.Visibility = Visibility.Visible;
		}

		private void OnHideEgg(object sender, MouseButtonEventArgs e)
		{
			this._i_E.Visibility = Visibility.Collapsed;
		}
	}
}
