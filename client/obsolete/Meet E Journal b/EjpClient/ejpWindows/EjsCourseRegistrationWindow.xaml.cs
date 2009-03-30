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
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					 SiliconStudio.DebugManagers.MessageType.Error,
					 "eJournalPlus Client - Course Registration Window",
					 "Loading data from Ejs Failed" +
					 "\nError: " + ex.Message);
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
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					 SiliconStudio.DebugManagers.MessageType.Error,
					 "eJournalPlus Client - Course Registration Window",
					 "Register to Course Failed" +
					 "\nError: " + ex.Message);

                MessageBox.Show("現時点では、新規コースに登録出来ません。しばらくしたら、もう一度試してみて下さい。");
            }
        }

        private void On_BtnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
