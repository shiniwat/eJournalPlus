using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using EjsBridge.ejsService;

namespace ejpClient.ejpWindows
{
	/// <summary>
	/// Interaction logic for PublishAssignmentWindow.xaml
	/// </summary>
	public partial class PublishAssignmentWindow : Window
	{
		private EjsBridge.ejsService.ejsAssignment[] _assignments;

		/// <summary>
		/// Tells what course to ascociate the ass with
		/// in case it is published to EJS.
		/// </summary>
		private int _registerToCourseId;
		public int RegisterToCourseId
		{
			get { return _registerToCourseId; }
			set { _registerToCourseId = value; }
		}

		/// <summary>
		/// Even if the document is coming from the
		/// E Journal Server, it will still be downloaded
		/// to the local disk before it is added to the application.
		/// </summary>
		private string _documentLocalPath;
		public string DocumentLocalPath
		{
			get { return _documentLocalPath; }
			set { _documentLocalPath = value; }
		}

		/// <summary>
		/// This property can be examined by instantiators
		/// when the window is closed, to see if the operation
		/// was cancelled.
		/// </summary>
		private bool _cancelled;
		public bool Cancelled
		{
			get { return _cancelled; }
			set { _cancelled = value; }
		}

		//removed 080617
		//private string _currentFileName;
		//private bool _needsCourseList = true;

		public PublishAssignmentWindow()
		{
			InitializeComponent();
		}

		protected override void OnContentRendered(EventArgs e)
		{
			if (App.IsCurrentUserEJSAuthenticated() == false)
			{
				ejpWindows.EjsLoginWindow loginWindow = new EjsLoginWindow();
				loginWindow.ShowDialog();
			}
			if (App.IsCurrentUserEJSAuthenticated() == false)
			{
				this._cancelled = true;
				this.Close();
			}
			else
			{
				this._tb_LoginName.Text =
					App._currentEjpStudent.LastName + " " + App._currentEjpStudent.FirstName;

				this.LoadDataFromEjs();
			}

			if (this._cb_Courses.Items.Count > 0)
				this._cb_Courses.SelectedIndex = 0;
		}

		private void LoadDataFromEjs()
		{
			try
			{
				ejpWindows.LoadingMessageWindow lmw =
						new ejpClient.ejpWindows.LoadingMessageWindow();

				EjsBridge.ejsService.ejsCourse[] courses = null;

				BackgroundWorker bgw = new BackgroundWorker();
				bgw.DoWork += delegate(object s3, DoWorkEventArgs doWorkArgs)
				{
					courses =
					EjsBridge.ejsBridgeManager.GetRegisteredCoursesForUser(
					App._currentEjpStudent.SessionToken, true);

					this._assignments = EjsBridge.ejsBridgeManager.GetAllPublishedAssignments(
						App._currentEjpStudent.SessionToken, false);
				};

				bgw.RunWorkerCompleted += delegate(object s4, RunWorkerCompletedEventArgs workerCompletedArgs)
				{
					lmw.Close();
					bgw.Dispose();
				};

				bgw.RunWorkerAsync();
				lmw.ShowDialog();

				ObservableAssignmentList aList = this.Resources["AssignmentList"] as ObservableAssignmentList;
				aList.Clear();

				ObservableCourseList cList = this.Resources["CourseList"] as ObservableCourseList;
				cList.Clear();

				foreach (ejsCourse course in courses)
				{
					cList.Add(course);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void OnPublishAssignment(object sender, RoutedEventArgs e)
		{
			if (App.IsCurrentUserEJSAuthenticated() == false)
			{
				ejpWindows.EjsLoginWindow loginWindow = new EjsLoginWindow();
				loginWindow.ShowDialog();
			}
			if (App.IsCurrentUserEJSAuthenticated() == false)
			{
				MessageBox.Show(Application.Current.Resources["ERR_MustLogInBeforePublishAsg"] as string);//Properties.Resources.ERR_MustLogInBeforePublishAsg);
			}
			else
			{
				if (this._l_SaveFileName.Text.Length == 0)
				{
					MessageBox.Show(Application.Current.Resources["ERR_NameNeededToPusblishAsg"] as string);//Properties.Resources.ERR_NameNeededToPusblishAsg);
					return;
				}
				this.DocumentLocalPath = this._l_SaveFileName.Text;
				this.RegisterToCourseId = ((ejsCourse)this._cb_Courses.SelectedValue)._id;
				this.Cancelled = false;
				this.Close();
			}
		}

		private void OnCourseListSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (this._cb_Courses.SelectedValue != null)
			{
				ObservableAssignmentList cList =
					this.Resources["AssignmentList"] as ObservableAssignmentList;
				ejsCourse course = this._cb_Courses.SelectedValue as ejsCourse;

				cList.Clear();

				foreach (ejsAssignment ass in this._assignments)
				{
					if (ass.CourseId == course._id)
						cList.Add(ass);
				}
			}
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			this._cancelled = true;
			this.Close();
		}
	}
}
