using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;

namespace SiliconStudio.Meet.EjsManager
{
	public class ejsManagerStage : UserControl
	{

		#region Protected Properties

		private ejsServiceReference.ejsSessionToken _currentUserToken = null;
		public ejsServiceReference.ejsSessionToken CurrentUserToken
		{
			get { return _currentUserToken; }
			set { _currentUserToken = value; }
		}

		protected bool _isStageReady = false;
		public bool IsStageReady
		{
			get { return _isStageReady; }
			set { _isStageReady = value; }
		}

        protected bool _isStageBusy = false;
        protected object threadLock = new object();

		#endregion

		#region RoutedEvents

		public static readonly RoutedEvent RequestGoToStageEvent =
			EventManager.RegisterRoutedEvent("RequestGoToStage",
			RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ejsManagerStage));

		public static readonly RoutedEvent AsyncOperationStartedEvent =
			EventManager.RegisterRoutedEvent("AsyncOperationStarted",
			RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ejsManagerStage));

		public static readonly RoutedEvent AsyncOperationCompletedEvent =
			EventManager.RegisterRoutedEvent("AsyncOperationCompleted",
			RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ejsManagerStage));

		public event RoutedEventHandler RequestGoToStage
		{
			add { AddHandler(RequestGoToStageEvent, value); }
			remove { RemoveHandler(RequestGoToStageEvent, value); }
		}

		public event RoutedEventHandler AsyncOperationStarted
		{
			add { AddHandler(AsyncOperationStartedEvent, value); }
			remove { RemoveHandler(AsyncOperationStartedEvent, value); }
		}

		public event RoutedEventHandler AsyncOperationCompleted
		{
			add { AddHandler(AsyncOperationCompletedEvent, value); }
			remove { RemoveHandler(AsyncOperationCompletedEvent, value); }
		}

		protected void RaiseRequestGoToStageEvent(string DestinationStageName)
		{
			RequestGoToStageEventArgs e =
				new RequestGoToStageEventArgs(
					ejsManagerStage.RequestGoToStageEvent, DestinationStageName);
			this.RaiseEvent(e);
		}

		public void RaiseAsyncOperationStartedEvent(string Message)
		{
			AsyncOperationStartedEventArgs e =
				new AsyncOperationStartedEventArgs(
					ejsManagerStage.AsyncOperationStartedEvent, Message);
			this.RaiseEvent(e);
		}

		public void RaiseAsyncOperationCompletedEvent()
		{
			RoutedEventArgs e =
				new RoutedEventArgs(
					ejsManagerStage.AsyncOperationCompletedEvent);
			this.RaiseEvent(e);
		}

		#endregion

		public void Activate(ejsServiceReference.ejsSessionToken UserToken)
		{
			this._currentUserToken = UserToken;
			if (this._isStageReady == false)
				this.PrepareStage();
			this.Visibility = System.Windows.Visibility.Visible;
		}

		public virtual void PrepareStage()
		{
			this._isStageReady = true;
		}

		public void DeActivate()
		{
			this.Visibility = System.Windows.Visibility.Collapsed;
		}

        #region Helpers and Shared Methods

        protected bool GetDeleteConfirmation()
        {
            if (
                MessageBox.Show("You are about to delete the selected item.\n",
                "Delete Item", MessageBoxButton.OKCancel, MessageBoxImage.Warning)
                == MessageBoxResult.OK
                )
                return true;
            else
                return false;
        }

        protected bool GetUpdateConfirmation()
        {
            if (
                MessageBox.Show("You are about to update the selected item.\n",
                "Update Item", MessageBoxButton.OKCancel, MessageBoxImage.Warning)
                == MessageBoxResult.OK
                )
                return true;
            else
                return false;
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

        #endregion

	}

	public class RequestGoToStageEventArgs : RoutedEventArgs
	{
		public string DestinationStageName { get; set; }

		public RequestGoToStageEventArgs(RoutedEvent SourceEvent, string DestinationStageName)
		{
			this.RoutedEvent = SourceEvent;
			this.DestinationStageName = DestinationStageName;
		}
	}

	public class AsyncOperationStartedEventArgs : RoutedEventArgs
	{
		public string Message { get; set; }

		public AsyncOperationStartedEventArgs(RoutedEvent SourceEvent, string Message)
		{
			this.RoutedEvent = SourceEvent;
			this.Message = Message;
		}
	}
}
