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
using SiliconStudio.Meet.EjsManager.ejsServiceReference;
using SiliconStudio.Meet.EjsManager.ServiceOperations;

namespace SiliconStudio.Meet.EjsManager
{
	/// <summary>
	/// Interaction logic for ejsStage_Courses.xaml
	/// </summary>
	public partial class ejsStage_Courses : ejsManagerStage
	{

		public static readonly RoutedEvent CourseDataUpdatedEvent =
			EventManager.RegisterRoutedEvent("CourseDataUpdated",
			RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ejsStage_Courses));

		public event RoutedEventHandler CourseDataUpdated
		{
			add { AddHandler(CourseDataUpdatedEvent, value); }
			remove { RemoveHandler(CourseDataUpdatedEvent, value); }
		}

		protected void RaiseCourseDataUpdatedEvent()
		{
			RoutedEventArgs e =
				new RoutedEventArgs(ejsStage_Courses.CourseDataUpdatedEvent);
			this.RaiseEvent(e);
		}

		public ejsStage_Courses()
		{
			InitializeComponent();
		}

		#region Prepare Stage

		public override void PrepareStage()
		{

            lock (this.threadLock)
            {
                if (this._isStageBusy)
                    return;

                if (this.IsStageReady == true)
                    return;

                this._isStageBusy = true;

                BackgroundWorker bgw = new BackgroundWorker();
                bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PrepareOperationCompleted);
                bgw.WorkerSupportsCancellation = true;
                bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
                {
                    try
                    {
                        e.Result = ejsBridgeManager.GetAllRegisteredCourses(
                            this.CurrentUserToken, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Getting All Courses on eJournalServer...");
            }
		}

		private void PrepareOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled == false
				&& e.Error == null)
			{
				ejsCourse[] courses =
					e.Result as ejsCourse[];
				if (courses != null)
				{
					ObservableCourseList l = 
						App.Current.Resources["CompleteCoursesList"] as ObservableCourseList;

					l.Clear();

                    //We have to keep them seperate to make sure they do not update
                    //eachothers current item...
                    ObservableCourseList ln =
                        App.Current.Resources["CompleteCoursesForNewDocumentList"] as ObservableCourseList;

					ln.Clear();

                    for (int i = 0; i < courses.Length; i++)
                    {
                        l.Add(courses[i]);
                        ln.Add(courses[i]);
                    }
				}
			}

			this.IsStageReady = true;
            this._isStageBusy = false;

			this.RaiseAsyncOperationCompletedEvent();

		}

		#endregion

		#region Update Item

		private void OnUpdateCurrentItem(object sender, RoutedEventArgs e)
		{
			//TODO: Implement Update
			if (this._lv_CourseList.SelectedItem == null)
				return;

			if (this.GetUpdateConfirmation() == true)
			{
				ejsCourse c = this._lv_CourseList.SelectedItem as
					ejsCourse;

				this.UpdateCourse(c);
			}
		}

		private void UpdateCourse(ejsCourse courseToUpdate)
		{
			lock (this.threadLock)
			{
				if (this._isStageBusy)
					return;

				this._isStageBusy = true;

				BackgroundWorker bgw = new BackgroundWorker();
				bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(UpdateItemOperationCompleted);
				bgw.WorkerSupportsCancellation = true;
				bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
				{
					try
					{
						if (StringValidation.ValidSqlInputVariable(courseToUpdate._description)
							|| StringValidation.ValidSqlInputVariable(courseToUpdate._name)
							|| StringValidation.ValidSqlInputVariable(courseToUpdate._owner))
						{
							MessageBox.Show("Invalid Course Data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
							return;
						}

						ejsBridgeManager.UpdateCourse(this.CurrentUserToken, courseToUpdate);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						e.Cancel = true;
					}
				};

				bgw.RunWorkerAsync();

				this.RaiseAsyncOperationStartedEvent("Updating Course on eJournalServer...");
			}
		}

		#endregion

		#region Delete Item

		private void OnDeleteCurrentItem(object sender, RoutedEventArgs e)
		{
			if (this._lv_CourseList.SelectedItem == null)
				return;

			if (this.GetDeleteConfirmation() == true)
			{
				ejsCourse c = this._lv_CourseList.SelectedItem as
					ejsCourse;

				this.DeleteCourse(c);
			}
		}

		private void DeleteCourse(ejsCourse courseToDelete)
		{
			lock (this.threadLock)
			{
				if (this._isStageBusy)
					return;

				this._isStageBusy = true;

				BackgroundWorker bgw = new BackgroundWorker();
				bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(UpdateItemOperationCompleted);
				bgw.WorkerSupportsCancellation = true;
				bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
				{
					try
					{
						ejsBridgeManager.DeleteCourse(this.CurrentUserToken, courseToDelete);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						e.Cancel = true;
					}
				};

				bgw.RunWorkerAsync();

				this.RaiseAsyncOperationStartedEvent("Deleting Course on eJournalServer...");
			}
		}

		#endregion 

		#region Helpers and Shared Methods

		private void UpdateItemOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.IsStageReady = true;
			this._isStageBusy = false;
			this.RaiseAsyncOperationCompletedEvent();
			this.UpdateData();
			this.RaiseCourseDataUpdatedEvent();
		}

		#endregion

		#region Update Data

		private void UpdateData()
		{
			this.IsStageReady = false;
			ObservableCourseList l =
						App.Current.Resources["CompleteCoursesList"] as ObservableCourseList;
			l.Clear();
			this.PrepareStage();
		}

		private void OnUpdateList(object sender, RoutedEventArgs e)
		{
			this.UpdateData();
		}

		#endregion

		#region Create New

		private void OnCreateNew(object sender, RoutedEventArgs e)
		{
			this.RegisterNewCourse();
		}

		private void RegisterNewCourse()
		{
			AddNewCourseWindow w = new AddNewCourseWindow(this.CurrentUserToken, this);
			w.ShowDialog();
			if (w.NeedsUpdate == true)
			{
				this.UpdateData();
				this.RaiseCourseDataUpdatedEvent();
			}

		}

		#endregion

	}
}
