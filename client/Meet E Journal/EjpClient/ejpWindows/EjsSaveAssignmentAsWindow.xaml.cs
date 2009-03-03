using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using EjsBridge.ejsService;
using winForms = System.Windows.Forms;

namespace ejpClient.ejpWindows
{
	/// <summary>
	/// Interaction logic for EjsSaveAssignmentAsWindow.xaml
	/// </summary>
	public partial class SaveAssignmentAsWindow : Window
	{
		/// <summary>
		/// Tells from where the first XPS document of the
		/// first study is loaded from.
		/// </summary>
		private CreateAssignmentStartLocation _saveLocation
			= CreateAssignmentStartLocation.NotSet;
		public CreateAssignmentStartLocation SaveLocation
		{
			get { return _saveLocation; }
			set { _saveLocation = value; }
		}

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

		private string _currentFileName;
		private bool _needsCourseList = true;

		public SaveAssignmentAsWindow(string FileName)
		{
			InitializeComponent();
			if (App._ejpSettings.IsEjsConfigured == false)
			{
				this._rb_DocLocEjs.IsEnabled = false;
			}

			this._currentFileName = FileName;

		}

		private void On_RbDocLocLocalChecked(object sender, RoutedEventArgs e)
		{
			string path = this.GetSaveFileName("EJP Assignments (*.EJP)|*.ejp", "保存先");
			if (path != "cancel")
			{
				this._b_Ok.IsEnabled = true;
				FileInfo f = new FileInfo(path);
				this._l_SaveFileName.Text = f.Name;
				this._documentLocalPath = path;
				this._saveLocation = CreateAssignmentStartLocation.Local;
			}
			else
			{
				this._rb_DocLocLocal.IsChecked = false;
				this._documentLocalPath = "";
				this._l_SaveFileName.Text = this._currentFileName;
				this.EnableDisableOKButton();

			}
			this._tb_EjsSaveInstructions.Visibility = Visibility.Hidden;
			this._tb_EjsSaveInstructions.Visibility = Visibility.Hidden;
			this._tb_EjsSaveToCourseInstructions.Visibility = Visibility.Hidden;
			this._cb_Courses.Visibility = Visibility.Hidden;
		}

		private void On_RbDocLocEjsChecked(object sender, RoutedEventArgs e)
		{
			if (App.IsCurrentUserEJSAuthenticated() == false)
			{
				ejpWindows.EjsLoginWindow loginWindow = new EjsLoginWindow();
				loginWindow.ShowDialog();
			}
			if (App.IsCurrentUserEJSAuthenticated() == false)
			{
				MessageBox.Show("公開するためログインが必要です。");
				this._rb_DocLocEjs.IsChecked = false;
				this._l_SaveFileName.Text = "";
			}
			else
			{
				this._tb_EjsSaveInstructions.Visibility = Visibility.Visible;
				this._tb_EjsSaveToCourseInstructions.Visibility = Visibility.Visible;
				this._cb_Courses.Visibility = Visibility.Visible;

				this._saveLocation = CreateAssignmentStartLocation.EJournalServer;
				this.EnableDisableOKButton();
				if (this._needsCourseList)
				{
					this.LoadDataFromEjs();
					this._needsCourseList = false;
				}
			}
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
				};

				bgw.RunWorkerCompleted += delegate(object s4, RunWorkerCompletedEventArgs workerCompletedArgs)
				{
					lmw.Close();
					bgw.Dispose();
				};

				bgw.RunWorkerAsync();
				lmw.ShowDialog();

				ObservableCourseList cList = this.Resources["CourseList"] as ObservableCourseList;

				foreach (ejsCourse course in courses)
				{
					cList.Add(course);
				}
				if (this._cb_Courses.Items.Count > 0)
					this._cb_Courses.SelectedIndex = 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void On_BtnOKClick(object sender, RoutedEventArgs e)
		{
			if (this.SaveLocation == CreateAssignmentStartLocation.EJournalServer)
			{
				if (App.IsCurrentUserEJSAuthenticated() == false)
				{
					ejpWindows.EjsLoginWindow loginWindow = new EjsLoginWindow();
					loginWindow.ShowDialog();
				}
				if (App.IsCurrentUserEJSAuthenticated() == false)
				{
					MessageBox.Show("公開するためログインが必要です。");
					this._rb_DocLocEjs.IsChecked = false;
					this._l_SaveFileName.Text = "";
				}
				else
				{
					if (this._l_SaveFileName.Text.Length == 0)
					{
						MessageBox.Show("公開するアサインメントの名前を必ず入力して下さい。");
						return;
					}
					this.DocumentLocalPath = this._l_SaveFileName.Text;
					this.RegisterToCourseId = ((ejsCourse)this._cb_Courses.SelectedValue)._id;
					this.Close();
				}
			}
			else
			{
				//this.DocumentLocalPath = this._l_SaveFileName.Text;
				this.Close();
			}
		}

		private void On_BtnCancelClick(object sender, RoutedEventArgs e)
		{
			this._saveLocation = CreateAssignmentStartLocation.NotSet;
			this.Close();
		}

		private void EnableDisableOKButton()
		{
			if ((bool)this._rb_DocLocEjs.IsChecked
				|| (bool)this._rb_DocLocLocal.IsChecked)
				this._b_Ok.IsEnabled = true;
			else
			{
				this._l_SaveFileName.Text = this._currentFileName;
				this._saveLocation = CreateAssignmentStartLocation.NotSet;
				this._b_Ok.IsEnabled = false;
			}
		}

		private string GetSaveFileName(string extensionMask, string dialogTitle)
		{
			winForms.SaveFileDialog saveDialog = new winForms.SaveFileDialog();
			saveDialog.Title = dialogTitle;
			saveDialog.Filter = extensionMask;

			if (saveDialog.ShowDialog() == winForms.DialogResult.OK)
				return (saveDialog.FileName);
			else
				return "cancel";
		}
	}
}
