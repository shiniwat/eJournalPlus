using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace SiliconStudio.Meet.EjsManager
{
	public partial class MainWindow
	{
		private ejsManagerStage _currentStage;
		private Ellipse _currentStageMarker;
		private ejsServiceReference.ejsSessionToken _currentUserToken;

		private Dictionary<string, ejsManagerStage> _stages =
			new Dictionary<string, ejsManagerStage>();

		private BusyDialog _busyDialog;

		private int _currentlyRunningOperationsCount = 0;

		public MainWindow()
		{
			this.InitializeComponent();

			this._stages.Add("Login", this._stage_Login);
			this._stages.Add("Courses", this._stage_Courses);
			this._stages.Add("Documents", this._stage_Documents);
			this._stages.Add("Users", this._stage_Users);
			this._stages.Add("Assignments", this._stage_Assignments);
			this._stages.Add("Server", this._stage_Server);

			this.SetApplicationState(ApplicationState.Cold);

		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (this._currentlyRunningOperationsCount > 0)
			{
				e.Cancel = true;
				base.OnClosing(e);
				return;
			}

			if (MessageBox.Show("Are you sure you want to quit?", "Quit application", MessageBoxButton.YesNo, MessageBoxImage.Question)
				== MessageBoxResult.Yes)
			{
				if (this._currentUserToken != null && this._currentUserToken._isAuthenticated == true)
					ServiceOperations.ejsBridgeManager.LogOutUser(this._currentUserToken);
				base.OnClosing(e);
			}
			else
			{
				e.Cancel = true;
				base.OnClosing(e);
				return;
			}
		}

		public void OnGoToStage(object sender, RoutedEventArgs e)
		{
			Button sButton = sender as Button;
			string stageName = sButton.Tag as string;

			if (stageName != null && stageName != "")
				this.GoToStage(stageName);
		}

		private void GoToStage(string StageName)
		{
			try
			{
				if (this._stages.ContainsKey(StageName) == false)
					throw new ApplicationException("Attempted to go to Stage that does not exist:\n" + StageName);

				Ellipse ellipseToActivate =
					this.FindName("_be_" + StageName) as Ellipse;

				if (this._currentStage != null)
					this._currentStage.DeActivate();

				if (this._currentStageMarker != null)
					this._currentStageMarker.Visibility = Visibility.Collapsed;

				this._stages[StageName].Activate(this._currentUserToken);

				if (ellipseToActivate != null)
					ellipseToActivate.Visibility = Visibility.Visible;

				this._currentStage = this._stages[StageName];
				this._currentStageMarker = ellipseToActivate;
			}
			catch (ApplicationException ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OnExit(object sender, RoutedEventArgs e)
		{
			//TODO: Implement Proper Exit 
			//if(this._currentUserToken != null && this._currentUserToken._isAuthenticated == true)
			//	ServiceOperations.ejsBridgeManager.LogOutUser(this._currentUserToken);

			this.Close();
		}

		private void OnRequestGoToStage(object sender, RoutedEventArgs e)
		{
			RequestGoToStageEventArgs dse =
				e as RequestGoToStageEventArgs;
			this.GoToStage(dse.DestinationStageName);
		}

		private void OnUserAuthenticated(object sender, RoutedEventArgs e)
		{
			UserAuthenticatedEventArgs dse =
				e as UserAuthenticatedEventArgs;
			this._l_LoginStatus.Content = "Logged In: " + dse.LoginName + " / " + dse.ServerAddress;
			this._currentUserToken = dse.Token;

			foreach (KeyValuePair<string, ejsManagerStage> stagePair in this._stages)
			{
				stagePair.Value.CurrentUserToken = dse.Token;
				stagePair.Value.PrepareStage();
			}

			this.SetApplicationState(ApplicationState.Ready);	
		}

		private void OnCourseDataUpdated(object sender, RoutedEventArgs e)
		{
			foreach (KeyValuePair<string, ejsManagerStage> stagePair in this._stages)
			{
				if (stagePair.Key != "Courses")
				{
					stagePair.Value.IsStageReady = false;
					stagePair.Value.CurrentUserToken = this._currentUserToken;
					stagePair.Value.PrepareStage();
				}
			}
		}

		private void OnStageAsyncOperationStarted(object sender, RoutedEventArgs e)
		{
			AsyncOperationStartedEventArgs ase =
				e as AsyncOperationStartedEventArgs;
			this._currentlyRunningOperationsCount += 1;
			this.LockApplicationForAsyncOperation(ase.Message);
		}

		private void OnStageAsyncOperationCompleted(object sender, RoutedEventArgs e)
		{
			this._currentlyRunningOperationsCount -= 1;
			this.FreeApplicationOnOperationCompleted();
		}

		private void LockApplicationForAsyncOperation(string Message)
		{
			//TODO: Fix Busy Dialog
			this._l_ApplicationStatus.Content = Message;
			this.SetApplicationState(ApplicationState.Busy);
			if (this._busyDialog == null)
			{
				this._busyDialog = new BusyDialog();
				this._busyDialog.Owner = this;
			}

			if (this._busyDialog.IsActive == true ||
				this._busyDialog.IsVisible == true)
				return;
			else
				this._busyDialog.ShowDialog();
		}

		private void FreeApplicationOnOperationCompleted()
		{
			//TODO: Fix Clear Busy Dialog
			if(this._busyDialog != null &&
				this._currentlyRunningOperationsCount == 0)
				this._busyDialog.Hide();

			this._l_ApplicationStatus.Content = "Idle";
		}

		private void SetApplicationState(ApplicationState state)
		{
			switch (state)
			{
				case ApplicationState.Cold:
					this.GoToStage("Login");
					this._mtb_Assignments.IsEnabled = false;
					this._mtb_Courses.IsEnabled = false;
					this._mtb_Documents.IsEnabled = false;
					this._mtb_Exit.IsEnabled = true;
					this._mtb_Server.IsEnabled = false;
					this._mtb_Users.IsEnabled = false;
					break;
				case ApplicationState.Ready:
					this._mtb_Assignments.IsEnabled = true;
					this._mtb_Courses.IsEnabled = true;
					this._mtb_Documents.IsEnabled = true;
					this._mtb_Exit.IsEnabled = true;
					this._mtb_Server.IsEnabled = true;
					this._mtb_Users.IsEnabled = true;
					break;
				case ApplicationState.Busy:
					//TODO: Implement Busy AppState if Needed
					break;
				case ApplicationState.Broken:
					this._mtb_Assignments.IsEnabled = false;
					this._mtb_Courses.IsEnabled = false;
					this._mtb_Documents.IsEnabled = false;
					this._mtb_Exit.IsEnabled = true;
					this._mtb_Server.IsEnabled = false;
					this._mtb_Users.IsEnabled = false;
					break;
				default:
					break;
			}
		}

		
	}

	public enum ApplicationState
	{
		Cold,
		Ready,
		Busy,
		Broken
	}
}