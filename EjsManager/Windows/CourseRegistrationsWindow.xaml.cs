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
using System.Windows.Shapes;
using SiliconStudio.Meet.EjsManager.ejsServiceReference;
using SiliconStudio.Meet.EjsManager.ServiceOperations;
using System.ComponentModel;

namespace SiliconStudio.Meet.EjsManager
{
	/// <summary>
	/// Interaction logic for CourseRegistrationsWindow.xaml
	/// </summary>
	public partial class CourseRegistrationsWindow : Window
	{
		private ejsUserInfo _currentUserInfo = null;
		private ejsSessionToken _currentUserToken = null;
		private ejsManagerStage _parentStage = null;

		private bool _needsUpdate = false;
		public bool NeedsUpdate
		{
			get { return _needsUpdate; }
			set { _needsUpdate = value; }
		}

		public CourseRegistrationsWindow(ejsUserInfo user,
			ejsSessionToken userToken, ejsManagerStage parentStage)
		{
			InitializeComponent();

			this._currentUserToken = userToken;
			this._currentUserInfo = user;
			this._parentStage = parentStage;

			this.BuildCourseLists();

			this._l_CurrentUserName.Content = user.LastName + ", " + user.FirstName;

		}

		private void BuildCourseLists()
		{
			ObservableCourseRegistrationList r =
				App.Current.Resources["CompleteCourseRegistrationsList"] as ObservableCourseRegistrationList;

			ObservableCourseList l =
						App.Current.Resources["CompleteCoursesList"] as ObservableCourseList;

			foreach (mngCourseRegistration ejscr in r)
			{
				if (ejscr.UserInfoObject.Id == this._currentUserInfo.Id)
				{
					foreach (ejsCourse course in l)
					{
						if (course._id == ejscr.CourseId)
							this._lv_RegisteredCourses.Items.Add(course._name);
					}
				}
			}
			
			foreach (ejsCourse course in l)
			{
				if (this._lv_RegisteredCourses.Items.Contains(course._name) == false)
					this._lv_AvailableCourses.Items.Add(course._name);
			}

		}

		private void OnClose(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		#region Register

		private void OnRegisterUserToCourse(object sender, RoutedEventArgs e)
		{
			if (this._lv_AvailableCourses.SelectedItem == null)
				return;

			string newCourseName = (string)this._lv_AvailableCourses.SelectedItem;

			this.RegisterUserToCourse(newCourseName);

		}

		private void RegisterUserToCourse(string newCourseName)
		{
			ejsCourse rCourse = this.GetCourseByName(newCourseName);

			if (rCourse != null)
			{
				try
				{
					BackgroundWorker bgw = new BackgroundWorker();
					bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(AddOperationCompleted);
					bgw.WorkerSupportsCancellation = true;
					bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
					{
						try
						{
							ejsBridgeManager.RegisterUserToCourse_adm(this._currentUserToken, this._currentUserInfo, rCourse);
							e.Result = newCourseName;
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
							e.Cancel = true;
						}
					};

					bgw.RunWorkerAsync();
					this._parentStage.RaiseAsyncOperationStartedEvent("Registering User to Course.");
                    this.BringIntoView();
				}
				catch (Exception)
				{
					throw;
				}
			}
		}

		private void AddOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this._parentStage.RaiseAsyncOperationCompletedEvent();

			string CourseName = (string)e.Result;
			this._lv_AvailableCourses.Items.Remove(CourseName);
			this._lv_RegisteredCourses.Items.Add(CourseName);

			this.NeedsUpdate = true;
		}

		#endregion

		private ejsCourse GetCourseByName(string CourseName)
		{
			ObservableCourseList l =
						App.Current.Resources["CompleteCoursesList"] as ObservableCourseList;

			foreach (ejsCourse course in l)
			{
				if (course._name == CourseName)
					return course;
			}

			return null;
		}

		#region Remove

		private void OnRemoveUserFromCourse(object sender, RoutedEventArgs e)
		{
			if (this._lv_RegisteredCourses.SelectedItem == null)
				return;

			if (
				MessageBox.Show(
				"\n\nRemoving a User from a Course will make all the assignments saved to that Course unavailable to the User.\n\n" +
				"The assignments are not deleted, but they will no longer show up in the list of assignments available in the eJournalPlus Client.\n\n" +
				"Are you sure you want to continue?",
				"Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning)
				== MessageBoxResult.Yes)
			{
				string courseName = (string)this._lv_RegisteredCourses.SelectedItem;
				this.RemoveUserFromCourse(courseName);
			}
		}

		private void RemoveUserFromCourse(string courseName)
		{
			ejsCourse rCourse = this.GetCourseByName(courseName);

			if (rCourse != null)
			{
				try
				{
					BackgroundWorker bgw = new BackgroundWorker();
					bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RemoveOperationCompleted);
					bgw.WorkerSupportsCancellation = true;
					bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
					{
						try
						{
							ejsBridgeManager.RemoveUserFromCourse(this._currentUserToken, this._currentUserInfo, rCourse);
							e.Result = courseName;
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
							e.Cancel = true;
						}
					};

					bgw.RunWorkerAsync();
					this._parentStage.RaiseAsyncOperationStartedEvent("Removing User from Course.");
                    this.BringIntoView();
				}
				catch (Exception)
				{
					throw;
				}
			}
		}

		private void RemoveOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this._parentStage.RaiseAsyncOperationCompletedEvent();

			if (e.Cancelled)
				return;

			string CourseName = (string)e.Result;
			this._lv_AvailableCourses.Items.Add(CourseName);
			this._lv_RegisteredCourses.Items.Remove(CourseName);

			this.NeedsUpdate = true;
		}

		#endregion
	}
}
