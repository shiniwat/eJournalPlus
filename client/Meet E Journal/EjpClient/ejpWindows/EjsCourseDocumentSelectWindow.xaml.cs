using System;
using System.Windows;
using System.Windows.Data;
using System.Collections.ObjectModel;
using EjsBridge.ejsService;

namespace ejpClient.ejpWindows
{
	/// <summary>
	/// Interaction logic for EjsCourseDocumentSelectWindow.xaml
	/// </summary>
	public partial class EjsCourseDocumentSelectWindow : Window
	{
		/// <summary>
		/// The user selected ejs Course Document. We
		/// This property can be used on window close
		/// to get the document meta data that the user
		/// selected.
		/// </summary>
		private ejsCourseDocument _selectedDocument;
		public ejsCourseDocument SelectedDocument
		{
			get { return _selectedDocument; }
			set { _selectedDocument = value; }
		}

		/// <summary>
		/// This property can be examined by instantiators
		/// when the window is closed, to see if the operation
		/// was cancelled.
		/// </summary>
		private bool _cancelled;
		public bool Cancelled
		{
			get { return _cancelled; }
			set { _cancelled = value; }
		}

		public EjsCourseDocumentSelectWindow()
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
			{
				this._cancelled = true;
				this.Close();
			}
			else
			{
				this._tb_LoginName.Text =
					App._currentEjpStudent.LastName + " " + App._currentEjpStudent.FirstName;

				this.LoadDataFromEjs();
			}

			//Set the combobox to display the documents
			//of the first course in the list, if there are
			//any courses in the list.
			if (this._cb_Courses.Items.Count > 0)
				this._cb_Courses.SelectedIndex = 0;
		}

		private void LoadDataFromEjs()
		{
			try
			{
				EjsBridge.ejsService.ejsCourse[] courses =
					EjsBridge.ejsBridgeManager.GetRegisteredCoursesForUser(
					App._currentEjpStudent.SessionToken, true);

				ObservableCourseList cList = this.Resources["CourseList"] as ObservableCourseList;

				foreach (ejsCourse course in courses)
				{
					cList.Add(course);
				}
			}
			catch (Exception ex)
			{
				//Todo: Fix Message
				MessageBox.Show(ex.Message);
			}
		}

		private void On_BtnOKClick(object sender, RoutedEventArgs e)
		{

			try
			{
				ejsCourseDocument doc =
					this._lv_Documents.SelectedValue as ejsCourseDocument;

				if (doc != null)
				{
					this._selectedDocument = doc;
					this._cancelled = false;
				}
			}
			finally
			{
				this.Close();
			}
		}

		private void On_BtnCancelClick(object sender, RoutedEventArgs e)
		{
			this._cancelled = true;
			this.Close();
		}
	}

	internal class ObservableCourseList : ObservableCollection<EjsBridge.ejsService.ejsCourse> { }

	internal class ejsDocumentNameConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			EjsBridge.ejsService.ejsCourseDocument d = value as EjsBridge.ejsService.ejsCourseDocument;
			if (d != null)
			{
				return d._name;
			}
			else
				return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	internal class ejsDocumentDescriptionConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			EjsBridge.ejsService.ejsCourseDocument d = value as EjsBridge.ejsService.ejsCourseDocument;
			if (d != null)
			{
				return d._description;
			}
			else
				return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}


	internal class CourseSqlDateConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			try
			{
				if (value is DateTime)
					return ((DateTime)value).ToShortDateString() + " " + ((DateTime)value).ToLongTimeString();
				else
					throw new ArgumentException();
			}
			catch (Exception)
			{
				return "Unknown";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
