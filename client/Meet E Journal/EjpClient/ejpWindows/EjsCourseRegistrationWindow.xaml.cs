using System;
using System.Windows;
using EjsBridge.ejsService;

namespace ejpClient.ejpWindows
{
	/// <summary>
	/// Interaction logic for EjsCourseRegistrationWindow.xaml
	/// </summary>
	public partial class EjsCourseRegistrationWindow : Window
	{
		public EjsCourseRegistrationWindow()
		{
			InitializeComponent();
		}

		protected override void OnContentRendered(EventArgs e)
		{
			base.OnContentRendered(e);
			if (App.IsCurrentUserEJSAuthenticated() == false)
			{
				ejpWindows.EjsLoginWindow loginWindow = new EjsLoginWindow();
				loginWindow.ShowDialog();
			}
			if (App.IsCurrentUserEJSAuthenticated() == false)
				this.Close();
			else
			{
				this._tb_LoginName.Text =
					App._currentEjpStudent.LastName + " " + App._currentEjpStudent.FirstName;

				this.LoadDataFromEjs();
			}
		}

		private void LoadDataFromEjs()
		{
			try
			{
				EjsBridge.ejsService.ejsCourse[] courses =
					EjsBridge.ejsBridgeManager.GetRegisteredCoursesForUser(
					App._currentEjpStudent.SessionToken, false);

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

		private void On_BtnRegisterNewClick(object sender, RoutedEventArgs e)
		{
			try
			{
				EjsRegisterToNewCourseWindow regNewCWin =
					new EjsRegisterToNewCourseWindow();
				regNewCWin.ShowDialog();

				this.LoadDataFromEjs();
			}
			catch (Exception)
			{
				MessageBox.Show(Application.Current.Resources["EX_CourseRegFailed"] as string);//Properties.Resources.EX_CourseRegFailed);
			}
		}

		private void On_BtnCancelClick(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
