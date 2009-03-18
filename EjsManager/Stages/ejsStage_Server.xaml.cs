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
using SiliconStudio.Meet.EjsManager.ServiceOperations;
using SiliconStudio.Meet.EjsManager.ejsServiceReference;

namespace SiliconStudio.Meet.EjsManager
{
	/// <summary>
	/// Interaction logic for ejsStage_Server.xaml
	/// </summary>
	public partial class ejsStage_Server : ejsManagerStage
	{
		public ejsStage_Server()
		{
			InitializeComponent();
		}

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
                        e.Result = ejsBridgeManager.GetServerStats(this.CurrentUserToken);
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
                ejsServerStats stats =
                    e.Result as ejsServerStats;
                if (stats != null)
                {
                    this._lv_SessionsList.ItemsSource = stats._currentSessions;
                    this._l_ServerName.Content = stats._serverName;
                    this._l_ServerAddress.Content = ejsBridgeManager.EjsAddress;
                }
            }

            this.IsStageReady = true;
            this._isStageBusy = false;

            this.RaiseAsyncOperationCompletedEvent();

        }

        private void OnUpdateList(object sender, RoutedEventArgs e)
        {
            this.UpdateData();
        }

        private void UpdateData()
        {
            this.IsStageReady = false;
            this.PrepareStage();
        }
	}
}
