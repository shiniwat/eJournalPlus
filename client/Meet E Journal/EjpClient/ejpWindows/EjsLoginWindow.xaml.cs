﻿using System;
using System.Windows;

namespace ejpClient.ejpWindows
{
	/// <summary>
	/// Interaction logic for EjsLoginWindow.xaml
	/// </summary>
	public partial class EjsLoginWindow : Window
	{
		public EjsLoginWindow()
		{
			InitializeComponent();
		}

		private void On_BtnOKClick(object sender, RoutedEventArgs e)
		{
			try
			{
				EjsBridge.ejsService.ejsSessionToken newToken =
				EjsBridge.ejsBridgeManager.AuthenticateUser(
					this._tb_UserName.Text,
					this._tbPw_Password.Password,
					SiliconStudio.Meet.EjpLib.Helpers.IdManipulation.GetNewGuid()
					);

				App._currentEjpStudent.SessionToken = newToken;
				this.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Resources.Str_AuthErrorTitle);
			}
			finally
			{
				this.Close();
			}
		}

		private void On_BtnCancelClick(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}