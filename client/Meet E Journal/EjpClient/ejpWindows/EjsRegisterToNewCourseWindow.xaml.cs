using System;
using System.Windows;
using EjsBridge.ejsService;

namespace ejpClient.ejpWindows
{
	/// <summary>
	/// Interaction logic for EjsRegisterToNewCourseWindow.xaml
	/// </summary>
	public partial class EjsRegisterToNewCourseWindow : Window
	{
		public EjsRegisterToNewCourseWindow()
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
				this.LoadDataFromEjs();

			this._l_Date.Text = DateTime.Today.ToShortDateString();
		}

		private void LoadDataFromEjs()
		{
			try
			{
				EjsBridge.ejsService.ejsCourse[] courses =
					EjsBridge.ejsBridgeManager.GetAllRegisteredCourses(
					App._currentEjpStudent.SessionToken, false);

				ObservableCourseList cList = this.Resources["CourseList"] as ObservableCourseList;

				foreach (ejsCourse course in courses)
				{
					cList.Add(course);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void On_BtnRegisterClick(object sender, RoutedEventArgs e)
		{
			try
			{
				EjsBridge.ejsBridgeManager.RegisterUserToCourse(
					App._currentEjpStudent.SessionToken, (ejsCourse)this._cb_Courses.SelectedValue);

				this.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void On_BtnCancelClick(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
