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
	/// Interaction logic for ejsStage_Documents.xaml
	/// </summary>
	public partial class ejsStage_Documents : ejsManagerStage
	{
		public ejsStage_Documents()
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
                        e.Result = ejsBridgeManager.GetAllCourseDocuments(
                            this.CurrentUserToken, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Getting All Course Document Records on eJournalServer...");
            }
        }

        private void PrepareOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled == false
				&& e.Error == null)
			{
				ejsCourseDocument[] documents =
                    e.Result as ejsCourseDocument[];
				if (documents != null)
				{

					ObservableCourseDocumentList l =
						App.Current.Resources["CompleteCourseDocumentsList"] as ObservableCourseDocumentList;

					l.Clear();

					for (int i = 0; i < documents.Length; i++)
					{
					    l.Add(documents[i]);
					}
				}
			}

			this.IsStageReady = true;
            this._isStageBusy = false;

			this.RaiseAsyncOperationCompletedEvent();

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
            ObservableCourseDocumentList l =
                        App.Current.Resources["CompleteCourseDocumentsList"] as ObservableCourseDocumentList;
            l.Clear();
            this.PrepareStage();
        }

        #endregion

        #region Update Item

        private void OnUpdateCurrentItem(object sender, RoutedEventArgs e)
		{
			//TODO: Implement Update
			if (this._lv_DocumentsList.SelectedItem == null)
				return;

            if (this.GetUpdateConfirmation() == true)
            {
                ejsCourseDocument d = this._lv_DocumentsList.SelectedItem as
                    ejsCourseDocument;

                this.UpdateCourseDocument(d);
            }

			this.UpdateData();
        }

        private void UpdateCourseDocument(ejsCourseDocument documentToUpdate)
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
                        if (StringValidation.ValidSqlInputVariable(documentToUpdate._description)
                            || StringValidation.ValidSqlInputVariable(documentToUpdate._name))
                        {
                            MessageBox.Show("Invalid Document Title / Description", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        ejsBridgeManager.UpdateCourseDocument(this.CurrentUserToken, documentToUpdate);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Updating Course Document on eJournalServer...");
            }
        }

        #endregion

        #region Delete Item

        private void OnDeleteCurrentItem(object sender, RoutedEventArgs e)
		{
			if (this._lv_DocumentsList.SelectedItem == null)
				return;

            if (this.GetDeleteConfirmation() == true)
            {
                ejsCourseDocument d = this._lv_DocumentsList.SelectedItem as
                    ejsCourseDocument;

                this.DeleteCourseDocument(d);
            }
        }

        private void DeleteCourseDocument(ejsCourseDocument documentToDelete)
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
                        ejsBridgeManager.DeleteCourseDocument(this.CurrentUserToken, documentToDelete);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Deleting Course Document on eJournalServer...");
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
        }

        #endregion

        #region Create New
        
        private void OnCreateNew(object sender, RoutedEventArgs e)
        {
            this.RegisterNewCourseDocument();
        }

        private void RegisterNewCourseDocument()
        {
            AddNewCourseDocumentWindow w = new AddNewCourseDocumentWindow(this.CurrentUserToken, this);
            w.ShowDialog();
            if (w.NeedsUpdate == true)
                this.UpdateData();
        }

        #endregion
    }
}
