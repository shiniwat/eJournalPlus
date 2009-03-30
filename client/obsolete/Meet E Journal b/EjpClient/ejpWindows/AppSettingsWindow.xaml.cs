#define INTERNAL_BUILD
//#define PUBLIC_BUILD

using System;
using System.Windows;

namespace ejpClient.ejpWindows
{
    /// <summary>
    /// Interaction logic for AppSettingsWindow.xaml
    /// </summary>
    public partial class AppSettingsWindow : Window
    {
        public AppSettingsWindow()
        {
            InitializeComponent();
            this.ImportSettings();
        }

        private void ImportSettings()
        {
            try
            {
                this._cb_DisplayMapLock.IsChecked = App._ejpSettings.ShowMapLock;
                this._cb_SavePass.IsChecked = App._ejpSettings.SaveUserSettings;
#if PUBLIC_BUILD
                this._cv_UserSettings.IsEnabled = false;
				this._tb_EJSAddress.IsEnabled = false;
				this._tb_LiveSpaceUrl.IsEnabled = false;
#endif
#if INTERNAL_BUILD
                this._cv_UserSettings.IsEnabled = true;
				this._tb_EJSAddress.IsEnabled = true;
				this._tb_LiveSpaceUrl.IsEnabled = true;
				this._tb_EJSAddress.Text = App._ejpSettings.EjsAddress;
                this._tb_LiveSpaceUrl.Text = App._ejpSettings.LiveSpaceUri;
				this._tb_UserName.Text = App._ejpSettings.UserName;
#endif
				this._tb_UndoCount.Text = App._ejpSettings.UndoCount.ToString();
                //if(App._ejpSettings.SaveUserSettings)
                //    this._tb_UserName.Text = App._ejpSettings.UserName;
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"eJournalPlus Client - Application Settings Window",
					"Importing Application Settings failed" +
					"\nError: " + ex.Message);
            }
        }


        private void On_BtnOKClick(object sender, RoutedEventArgs e)
        {
            bool cancel = this.SaveSettings();
            if(!cancel) this.Close();
        }

        private bool SaveSettings()
        {
            App._ejpSettings.SaveUserSettings = (bool)this._cb_SavePass.IsChecked;
            App._ejpSettings.ShowMapLock = (bool)this._cb_DisplayMapLock.IsChecked;

            if (this._tb_EJSAddress.Text.Length != 0)
            {
                if ((this._tb_EJSAddress.Text.StartsWith("http://")))
                {
                    App._ejpSettings.EjsAddress = this._tb_EJSAddress.Text;
                    App._ejpSettings.IsEjsConfigured = true;
                    EjsBridge.ejsBridgeManager.EjsAddress = App._ejpSettings.EjsAddress;
                }
                else
                {
                    this.ShowSettingsFormatError("The E Journal Server address is invald.\nMake sure you include the http:// part of the Url");
                    return true;
                }
            }
            else
            {
                App._ejpSettings.EjsAddress = "";
                App._ejpSettings.IsEjsConfigured = false;
            }

            if (this._tb_LiveSpaceUrl.Text.Length != 0)
            {
                if ((this._tb_LiveSpaceUrl.Text.StartsWith("http://")))
                    App._ejpSettings.LiveSpaceUri = this._tb_LiveSpaceUrl.Text;
                else
                {
                    this.ShowSettingsFormatError("The Live Space Url is invalid.\nMake sure you include the http:// part of the Url");
                    return true;
                }
            }

            int undoCount = -1;
            if (Int32.TryParse(this._tb_UndoCount.Text, out undoCount))
                App._ejpSettings.UndoCount = undoCount;
            else
            {
                this.ShowSettingsFormatError("Undo Count does not contain a valid number.");
                return true;
            }

            if (this._tb_UserName.Text.Length != 0)
            {
                if (App._ejpSettings.SaveUserSettings)
                    App._ejpSettings.UserName = this._tb_UserName.Text;
            }

            return false;
        }

        private void ShowSettingsFormatError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void On_BtnCancelClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void _cb_SavePass_Checked(object sender, RoutedEventArgs e)
        {
            //this._cv_UserSettings.IsEnabled = true;
        }

        private void _cb_SavePass_Unchecked(object sender, RoutedEventArgs e)
        {
            //this._cv_UserSettings.IsEnabled = false;
        }

        private void On_BtnRegisterToCourse(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SaveSettings();
                EjsCourseRegistrationWindow cRegWin =
                    new EjsCourseRegistrationWindow();
                cRegWin.ShowDialog();
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"eJournalPlus Client - Application Settings Window",
					"Register to Course Failed" +
					"\nError: " + ex.Message);

				//Todo: Fix Message.
                MessageBox.Show("コースに登録出来ませんでした。\n" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void On_BtnUpdatePasswordClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SaveSettings();
                string UserName = this._tb_UserName.Text;
                string OldPassword = this._tb_OldPassword.Password;
                string NewPassword = this._tb_NewPassword.Password;

                EjsBridge.ejsBridgeManager.UpdateUserPassword(
                    UserName, OldPassword, NewPassword);

                //Not sure about this...
                App._currentEjpStudent.SessionToken._isAuthenticated = false;

                this._tb_NewPassword.Password = "";
                this._tb_OldPassword.Password = "";

                MessageBox.Show("パスワードを更新しました。再ログインが必要となります。");

            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"eJournalPlus Client - Application Settings Window",
					"Failed to Update Password" +
					"\nError: " + ex.Message);

                MessageBox.Show("失敗しました。\n" + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
