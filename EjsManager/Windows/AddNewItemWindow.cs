using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SiliconStudio.Meet.EjsManager
{
    public class AddNewItemWindow : Window
    {
        protected ejsServiceReference.ejsSessionToken _currentUserToken;
        protected ejsManagerStage _parentStage;

        public bool NeedsUpdate = false;

        public AddNewItemWindow()
        {

        }

        public AddNewItemWindow(ejsServiceReference.ejsSessionToken userEjsToken, ejsManagerStage parentStage)
        {
            this._currentUserToken = userEjsToken;
            this._parentStage = parentStage;
        }

        protected void CancelOperation()
        {
            if (MessageBox.Show(
                    "Are you sure you wish to cancel?",
                    "Cancel",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        protected virtual void AddNewItem()
        {
            return;
        }

		protected void _dv_CheckStringInput(object sender, TextChangedEventArgs e)
		{
            return;

            //No longer used.
            //TextBox tb = sender as TextBox;
            //if (tb.Text == "" || tb.Text.Length == 0)
            //    tb.Background = Brushes.LightSalmon;
            //else
            //    tb.Background = Brushes.White;
		}
    }
}
