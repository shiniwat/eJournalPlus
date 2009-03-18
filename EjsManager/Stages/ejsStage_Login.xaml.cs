using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO;

namespace SiliconStudio.Meet.EjsManager
{
	/// <summary>
	/// Interaction logic for ejsStage_Login.xaml
	/// </summary>
	public partial class ejsStage_Login : ejsManagerStage
	{

		public static readonly RoutedEvent UserAuthenticatedEvent =
			EventManager.RegisterRoutedEvent("UserAuthenticated",
			RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ejsManagerStage));

		public event RoutedEventHandler UserAuthenticated
		{
			add { AddHandler(UserAuthenticatedEvent, value); }
			remove { RemoveHandler(UserAuthenticatedEvent, value); }
		}

		protected void RaiseUserAuthenticatedEvent(string UserName, string ServerAddress, 
			ejsServiceReference.ejsSessionToken Token)
		{
			UserAuthenticatedEventArgs e =
				new UserAuthenticatedEventArgs(
					ejsStage_Login.UserAuthenticatedEvent,
					UserName, ServerAddress, Token);
			this.RaiseEvent(e);
		}

		private bool _canSaveLoadSettings;
		private string _appDataDir;

		public ejsStage_Login()
		{
			InitializeComponent();
			this.ConfirmAppStorage();
			this.LoadSettings();
		}

		private void ConfirmAppStorage()
		{
			string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			baseDir += @"\Meet\eJournalServerManager";

			if (!Directory.Exists(baseDir))
			{
				try
				{
					Directory.CreateDirectory(baseDir);
				}
				catch (Exception)
				{
					MessageBox.Show("A required directory could not be created.\nApplication Settings cannot be saved.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					this._canSaveLoadSettings = false;
					this._appDataDir = "";
					return;
				}
			}

			this._appDataDir = baseDir;
			this._canSaveLoadSettings = true;
		}

		private void LoadSettings()
		{
			if (this._canSaveLoadSettings == false)
				return;

			string path = this._appDataDir + @"\settings.txt";

			if (File.Exists(path) == false)
				return;

			using (StreamReader tr = new StreamReader(path))
			{
				string line = "";
				string NextSetting = "";
				while ((line = tr.ReadLine()) != null)
				{
					switch (line)
					{
						case "ServerAddress":
							NextSetting = "ServerAddress";
							continue;
						case "UserName":
							NextSetting = "UserName";
							continue;
						default:
							break;
					}

					switch (NextSetting)
					{
						case "ServerAddress":
							if(line != "-")
								this._tb_ServerAddress.Text = line;
							break;
						case "UserName":
							if (line != "-")
								this._tb_UserName.Text = line;
							break;
						default:
							break;
					}
				}
			}
		}

		private void SaveSettings()
		{
			if (this._canSaveLoadSettings == false)
				return;

			//This is done as simple as possible to make it easier for anyone else
			//to extend this with their own settings, or to rebuild this using
			//the ConfigurationManager/Configuration namespace...
			using (StreamWriter tw = new StreamWriter(this._appDataDir + @"\settings.txt", false))
			{
				if ((bool)this._cb_RememberServerAddress.IsChecked)
				{
					tw.WriteLine("ServerAddress");
					tw.WriteLine(this._tb_ServerAddress.Text);
				}
				else
				{
					tw.WriteLine("ServerAddress");
					tw.WriteLine("-");
				}
				if ((bool)this._cb_RememberUserName.IsChecked)
				{
					tw.WriteLine("UserName");
					tw.WriteLine(this._tb_UserName.Text);
				}
				else
				{
					tw.WriteLine("UserName");
					tw.WriteLine("-");
				}
			}
		}

		private void OnAuthenticateLogin(object sender, RoutedEventArgs e)
		{
			this.SaveSettings();
			this.AuthenticateUserAsync();
		}

		private void AuthenticateUserAsync()
		{
			if (this._tb_UserName.Text == "" ||
				this._tb_ServerAddress.Text == "" ||
				this._pwb_Password.Password.Length == 0)
				return;

			if (StringValidation.ValidSqlInputVariable(this._tb_UserName.Text) ||
				StringValidation.ValidSqlInputVariable(this._tb_ServerAddress.Text) ||
				StringValidation.ValidSqlInputVariable(this._pwb_Password.Password))
				return;

			string userName = this._tb_UserName.Text;
			string serverAddress = this._tb_ServerAddress.Text;
			string password = this._pwb_Password.Password;

			ServiceOperations.ejsBridgeManager.EjsAddress = serverAddress;

			BackgroundWorker bgw = new BackgroundWorker();
			bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LoginOperationCompleted);
			bgw.WorkerSupportsCancellation = true;
			bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
			{
				try
				{
					e.Result = ServiceOperations.ejsBridgeManager.AuthenticateUser(
						userName, password, Guid.NewGuid());
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					e.Cancel = true;
				}
			};
			bgw.RunWorkerAsync();

			this.RaiseAsyncOperationStartedEvent("Authenticating...");
		}

		private void LoginOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled == false
				&& e.Error == null)
			{
				ejsServiceReference.ejsSessionToken t =
					e.Result as ejsServiceReference.ejsSessionToken;
				if (t != null)
				{
					this.RaiseUserAuthenticatedEvent(
						t._lastName + ", " + t._firstName,
						"eJournalServer",
						t);

					this.RaiseRequestGoToStageEvent("Courses");
				}
			}

			this.RaiseAsyncOperationCompletedEvent();
		}
	}



	public class UserAuthenticatedEventArgs : RoutedEventArgs
	{
		public string LoginName { get; set; }
		public string ServerAddress { get; set; }
		public ejsServiceReference.ejsSessionToken Token { get; set; }

		public UserAuthenticatedEventArgs(RoutedEvent SourceEvent, 
			string LoginName, string ServerAddress, 
			ejsServiceReference.ejsSessionToken Token)
		{
			this.RoutedEvent = SourceEvent;
			this.ServerAddress = ServerAddress;
			this.LoginName = LoginName;
			this.Token = Token;
		}
	}

}
