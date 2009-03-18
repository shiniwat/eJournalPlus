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
using System.ComponentModel;
using SiliconStudio.Meet.EjsManager.ServiceOperations;

namespace SiliconStudio.Meet.EjsManager
{
    /// <summary>
    /// Interaction logic for AddNewUserWindow.xaml
    /// </summary>
    public partial class AddNewUserWindow : AddNewItemWindow
    {
        public AddNewUserWindow(ejsSessionToken userEjsToken, ejsManagerStage parentStage)
            : base(userEjsToken, parentStage)
        {
            InitializeComponent();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.CancelOperation();
        }

        private void OnAddNewItem(object sender, RoutedEventArgs e)
        {
            this.AddNewItem();
        }

        protected override void AddNewItem()
        {
            if (StringValidation.ValidSqlInputVariable(this._tb_Email.Text)
            || StringValidation.ValidSqlInputVariable(this._tb_FirstName.Text)
            || StringValidation.ValidSqlInputVariable(this._tb_Password.Text)
            || StringValidation.ValidSqlInputVariable(this._tb_UserName.Text)
                || StringValidation.ValidSqlInputVariable(this._tb_LastName.Text))
                return;
            else
            {
                try
                {

                    if (this._tb_Email.Text.Length == 0 ||
                        this._tb_FirstName.Text.Length == 0 ||
                        this._tb_UserName.Text.Length == 0 ||
                        this._tb_LastName.Text.Length == 0 ||
                        this._tb_Password.Text.Length == 0)
                        return;

                    bool isActive = (bool)this._cb_CanLogin.IsChecked;

                    ejsUserInfo uInfo = new ejsUserInfo()
                    {
                        FirstName = this._tb_FirstName.Text,
                        LastName = this._tb_LastName.Text,
                        Email = this._tb_Email.Text,
                        IsAccountActive = isActive,
                        UserName = this._tb_UserName.Text,
                        Id = Guid.NewGuid().ToString()
                    };

                    int groupid = 3;
                    if (this._cb_IsTeacher.IsChecked == true)
                        groupid = 2;

                    string password = this._tb_Password.Text;

                    BackgroundWorker bgw = new BackgroundWorker();
                    bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OperationCompleted);
                    bgw.WorkerSupportsCancellation = true;
                    bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
                    {
                        try
                        {
                            ejsBridgeManager.AddNewUser(this._currentUserToken, uInfo, groupid, password);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            e.Cancel = true;
                        }
                    };

                    bgw.RunWorkerAsync();

                    this._parentStage.RaiseAsyncOperationStartedEvent("Creating New User Record.");

                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private void OperationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this._parentStage.RaiseAsyncOperationCompletedEvent();

			if (e.Cancelled == false)
			{
				this.NeedsUpdate = true;
				this.Close();
			}
        }

    }
}
