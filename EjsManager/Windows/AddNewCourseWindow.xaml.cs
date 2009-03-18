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
	/// Interaction logic for AddNewCourseWindow.xaml
	/// </summary>
	public partial class AddNewCourseWindow : AddNewItemWindow
	{
		public AddNewCourseWindow(ejsSessionToken Token, ejsManagerStage parentStage)
			: base(Token, parentStage)
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
			if (StringValidation.ValidSqlInputVariable(this._tb_Description.Text)
			|| StringValidation.ValidSqlInputVariable(this._tb_Name.Text)
			|| StringValidation.ValidSqlInputVariable(this._tb_Owner.Text))
				return;
			else
			{
				try
				{

					if (this._tb_Description.Text.Length == 0 ||
						this._tb_Name.Text.Length == 0 ||
						this._tb_Owner.Text.Length == 0)
						return;

					bool isActive = (bool)this._cb_IsActive.IsChecked;

					ejsCourse course = new ejsCourse()
					{
						_creationDate = DateTime.Now,
						_description = this._tb_Description.Text,
						_isActive = isActive,
						_name = this._tb_Name.Text,
						_owner = this._tb_Owner.Text
					};

					BackgroundWorker bgw = new BackgroundWorker();
					bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OperationCompleted);
					bgw.WorkerSupportsCancellation = true;
					bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
					{
						try
						{
							ejsBridgeManager.AddNewCourse(this._currentUserToken, course);
						}
						catch (Exception ex)
						{
							MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
							e.Cancel = true;
						}
					};

					bgw.RunWorkerAsync();

					this._parentStage.RaiseAsyncOperationStartedEvent("Uploading and Saving Course Document.");

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
