using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EjsBridge.ejsService;

namespace ejpClient.ejpWindows
{
	/// <summary>
	/// Interaction logic for EjsOpenAssignmentWindow.xaml
	/// </summary>
	public partial class EjsOpenAssignmentWindow : Window
	{
		/// <summary>
		/// This property can be examined by instantiators
		/// when the window is closed, to see if the operation
		/// was cancelled.
		/// </summary>
		private bool _cancelled = true;
		public bool Cancelled
		{
			get { return _cancelled; }
			set { _cancelled = value; }
		}

		/// <summary>
		/// The Online assignment to open.
		/// </summary>
		private ejsAssignment _assignmentToOpen;
		public ejsAssignment AssignmentToOpen
		{
			get { return _assignmentToOpen; }
			set { _assignmentToOpen = value; }
		}

		private EjsBridge.ejsService.ejsAssignment[] _assignments;

		public EjsOpenAssignmentWindow()
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

			//Set the combobox to display the documents
			//of the first course in the list, if there are
			//any courses in the list.
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
			catch (Exception)
			{
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

		private void OnDeleteAssignment(object sender, RoutedEventArgs e)
		{
			if (this._lv_Assignments.SelectedItem != null)
			{
				ejsAssignment assToDelete = this._lv_Assignments.SelectedItem as ejsAssignment;
				if (assToDelete.OwnerUserId != App._currentEjpStudent.Id)
					MessageBox.Show("他人のアサインメントは削除出来ません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Stop);
				else
				{
					try
					{
						int selectedCoursItemId = this._cb_Courses.SelectedIndex;
						if (MessageBox.Show("選択されたアサインメントを削除しますか？", "削除",
							MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
						{
							EjsBridge.ejsBridgeManager.HideAssignment(
							App._currentEjpStudent.SessionToken, assToDelete);
							this.LoadDataFromEjs();

							//Set the combobox to display the documents
							//of the prev selected course in the list, if there are
							//any courses in the list.
							if (this._cb_Courses.Items.Count != 0)
								this._cb_Courses.SelectedIndex = selectedCoursItemId;
						}
					}
					catch (ApplicationException)
					{
						MessageBox.Show("選択されたアサインメントは削除できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
		}

		private void OnOpenAssignment(object sender, RoutedEventArgs e)
		{
			if (this._lv_Assignments.SelectedItem != null)
			{
				this._assignmentToOpen = this._lv_Assignments.SelectedItem as ejsAssignment;
				if (this._assignmentToOpen != null)
				{
					this._cancelled = false;
					this.Close();
				}
				else
					MessageBox.Show("選択されたアサインメントが開けません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else
			{
				this._cancelled = true;
				this.Close();
			}
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			this._cancelled = true;
			this.Close();
		}
	}

	internal class ObservableAssignmentList : ObservableCollection<ejsAssignment> { }

	internal class ejsAssignmentMetaDataToDisplayFormat : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			ejsAssignment ass = value as ejsAssignment;
			if (ass == null)
				throw new ApplicationException("Attempt to display non EJS Assignment Object in Assignment List.");

			return ass.Title +
				" " +
				"(" + ass.OwnerName +
				" " +
				" / " + ass.CreationDate.ToShortDateString() + " " + ass.CreationDate.ToLongTimeString() +
				")";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}


	internal class StudyListToStudyCount : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return ((ejsStudyMetaData[])value).Length.ToString();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

}
