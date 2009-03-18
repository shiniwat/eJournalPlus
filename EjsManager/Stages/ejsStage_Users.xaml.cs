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
using SiliconStudio.Meet.EjsManager.ServiceOperations;
using SiliconStudio.Meet.EjsManager.ejsServiceReference;
using System.ComponentModel;

namespace SiliconStudio.Meet.EjsManager
{
	/// <summary>
	/// Interaction logic for ejsStage_Users.xaml
	/// </summary>
	public partial class ejsStage_Users : ejsManagerStage
	{
		public ejsStage_Users()
		{
			InitializeComponent();
		}

        //This is not so nice... but lack of time is the mother
        //of all bad coding habits...
        private struct multiplAsyncResultStruct
        {
            public ejsUserInfo[] userInfoArray;
            public ejsCourseRegistration[] courseRegistrationsArray;
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

                        ejsUserInfo[] ui = ejsBridgeManager.GetAllUserRecords(this.CurrentUserToken);
                        ejsCourseRegistration[] cr = ejsBridgeManager.GetAllRegisteredCourseRegistrations(this.CurrentUserToken);

                        e.Result = new multiplAsyncResultStruct()
                        {
                            courseRegistrationsArray = cr,
                            userInfoArray = ui
                        };

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Getting All Assignments on eJournalServer...");
            }
		}

		private void PrepareOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled == false
				&& e.Error == null)
			{

                multiplAsyncResultStruct mr = (multiplAsyncResultStruct)e.Result;

				if (mr.courseRegistrationsArray != null 
                    && mr.userInfoArray != null)
				{
					ObservableUserList l =
						App.Current.Resources["CompleteUsersList"] as ObservableUserList;

					l.Clear();

                    ObservableCourseRegistrationList r =
                        App.Current.Resources["CompleteCourseRegistrationsList"] as ObservableCourseRegistrationList;

					r.Clear();

					for (int i = 0; i < mr.userInfoArray.Length; i++)
						l.Add(mr.userInfoArray[i]);

                    //Slow. Refactor...
                    for (int j = 0; j < mr.courseRegistrationsArray.Length; j++)
                    {
                        for (int i = 0; i < mr.userInfoArray.Length; i++)
                        {
                            if (mr.userInfoArray[i].Id == mr.courseRegistrationsArray[j]._userId)
                            {
                                mngCourseRegistration mrr = new mngCourseRegistration()
                                {
                                    EjsCourseRegistrationObject = mr.courseRegistrationsArray[j],
                                    UserInfoObject = mr.userInfoArray[i]
                                };

                                r.Add(mrr);
                            }
                        }
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
			if (this._lv_UserList.SelectedItem == null)
				return;

            if (this.GetUpdateConfirmation() == true)
            {
                ejsUserInfo u = this._lv_UserList.SelectedItem as
                    ejsUserInfo;

                this.UpdateUser(u, this._tb_Password.Text);
            }
        }

        private void UpdateUser(ejsUserInfo userToUpdate, string newPassword)
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
                        if(StringValidation.ValidSqlInputVariable(userToUpdate.Email) ||
                            StringValidation.ValidSqlInputVariable(userToUpdate.FirstName) ||
                            StringValidation.ValidSqlInputVariable(userToUpdate.LastName) ||
                            StringValidation.ValidSqlInputVariable(userToUpdate.UserName)
                            )
                        {
                            MessageBox.Show("User Info contains invalid data", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        if (newPassword != "")
                        {
                            if(StringValidation.ValidSqlInputVariable(newPassword))
                            {
                                MessageBox.Show("The specified Password is not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                        }

                        ejsBridgeManager.UpdateUser(this.CurrentUserToken, userToUpdate, newPassword);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Updating User Info on eJournalServer...");
            }
        }

        #endregion

        #region Delete Item

        private void OnDeleteCurrentItem(object sender, RoutedEventArgs e)
		{
			//TODO: Implement Delete
			if (this._lv_UserList.SelectedItem == null)
				return;

			ejsUserInfo u = this._lv_UserList.SelectedItem as
                    ejsUserInfo;

			if (this.ConfirmCanDelete(u) == true)
			{
				if (this.GetDeleteConfirmation() == true)
				{
					this.DeleteUser(u, false);
				}
			}
        }

		private bool ConfirmCanDelete(ejsUserInfo userToDelete)
		{
			if (userToDelete.IsAccountActive)
			{
				MessageBox.Show("Users can only be deleted when their accounts are not active.\n"
					+ "To delete a User, first uncheck the Can Login check box.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
				return false;
			}
			else
				return true;
		}

        private void DeleteUser(ejsUserInfo userToDelete, bool confirmCanDelete)
        {

			if (confirmCanDelete)
			{
				if (this.ConfirmCanDelete(userToDelete) == false)
					return;
			}

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
                        ejsBridgeManager.DeleteUser(this.CurrentUserToken, userToDelete);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Deleting User Record on eJournalServer...");
            }
        }
        
        #endregion

        #region Create New

        private void OnCreateNew(object sender, RoutedEventArgs e)
        {
            this.RegisterNewUser();
        }

        private void RegisterNewUser()
        {
            AddNewUserWindow w = new AddNewUserWindow(this.CurrentUserToken, this);
            w.ShowDialog();
            if (w.NeedsUpdate == true)
                this.UpdateData();
        }

        #endregion

        #region Update Data

        private void OnUpdateList(object sender, RoutedEventArgs e)
        {
            this.UpdateData();
        }

        private void UpdateData()
		{
			this.IsStageReady = false;
			ObservableUserList l =
                        App.Current.Resources["CompleteUsersList"] as ObservableUserList;
			l.Clear();
			this.PrepareStage();
        }

        #endregion

        #region Course Registrations

        private void OnSetCourseRegistrations(object sender, RoutedEventArgs e)
		{
			if (this._lv_UserList.SelectedItem == null)
				return;

			ejsUserInfo u = null;

			if(this._lv_UserList.SelectedItem is mngCourseRegistration)
			{
				mngCourseRegistration cr = this._lv_UserList.SelectedItem
					as mngCourseRegistration;

				u = cr.UserInfoObject;

			}
			else
				u = this._lv_UserList.SelectedItem as ejsUserInfo;
			
			if(u != null)
				this.SetCourseRegistrationsForUser(u);

        }

		private void SetCourseRegistrationsForUser(ejsUserInfo user)
		{
			CourseRegistrationsWindow w = 
				new CourseRegistrationsWindow(user, this.CurrentUserToken, this);
			w.ShowDialog();
			if (w.NeedsUpdate)
			{
				this.UpdateData();
			}

		}

        #endregion

        #region Helpers and Shared Methods

        private void UpdateItemOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.IsStageReady = true;
            this._isStageBusy = false;

			this._tb_Password.Text = "";

            this.RaiseAsyncOperationCompletedEvent();
            this.UpdateData();
        }

        private void OnListAllUsersUnGrouped(object sender, RoutedEventArgs e)
        {
            GridView gv =
                App.Current.Resources["views_UsersListView"] as GridView;

            ObservableUserList l =
                        App.Current.Resources["CompleteUsersList"] as ObservableUserList;

            this._lv_UserList.View = gv;
            this._lv_UserList.ItemsSource = l;

			this._b_Delete.IsEnabled = true;
			this._b_Update.IsEnabled = true;

			this._g_Details.IsEnabled = true;
        }

        private void OnGroupUserListByCourse(object sender, RoutedEventArgs e)
        {
            GridView gv = 
                App.Current.Resources["views_UsersListGroupedByCourseView"] as GridView;

            CollectionViewSource cvs =
                App.Current.Resources["views_UserCourseRegistrationsView"] as CollectionViewSource;

            this._lv_UserList.View = gv;
            this._lv_UserList.ItemsSource = cvs.View;

			this._b_Delete.IsEnabled = false;
			this._b_Update.IsEnabled = false;

			this._g_Details.IsEnabled = false;

        }

        #endregion
    }
}
