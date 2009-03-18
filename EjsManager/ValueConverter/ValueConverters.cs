using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace SiliconStudio.Meet.EjsManager
{
    public class CourseIDToNameConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                ObservableCourseList l =
                        App.Current.Resources["CompleteCoursesList"] as ObservableCourseList;

                foreach (ejsServiceReference.ejsCourse course in l)
                {
                    if (course._id == (int)value)
                        return course._name;
                }
                return "Unknown Course Name";
            }
            catch (Exception)
            {
                return "Unknown Course Name";
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class CourseIDToObject : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                ObservableCourseList l =
                          App.Current.Resources["CompleteCoursesList"] as ObservableCourseList;

                foreach (ejsServiceReference.ejsCourse course in l)
                {
                    if (course._id == (int)value)
                        return course;
                }
                return "null";

            }
            catch (Exception)
            {
                return "Unknown Course Name";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value is ejsServiceReference.ejsCourse)
                {
                    return (((ejsServiceReference.ejsCourse)value)._id);
                }
                else
                    throw new ApplicationException("Unknown value type...");
            }
            catch (Exception)
            {
                return -1;
            }
        }
        #endregion
    }

	public class UserIDToObject : IValueConverter
	{
		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			try
			{
				ObservableUserList l =
						  App.Current.Resources["CompleteUsersList"] as ObservableUserList;

				foreach (ejsServiceReference.ejsUserInfo user in l)
				{
					if (user.Id == (string)value)
						return user;
				}
				return "null";

			}
			catch (Exception)
			{
				return "Unknown User Name";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		#endregion
	}

	public class BoolFlip : IValueConverter
	{
		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			try
			{
				if ((bool)value == true)
					return false;
				else
					return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		#endregion
	}

	public class DateTimeToStatus : IValueConverter
	{
		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			try
			{
				DateTime dt = (DateTime)value;
				if (dt != null)
				{
					TimeSpan ts = dt.Subtract(DateTime.Now);
					if (ts.TotalSeconds < 0)
						return "Expired (Zombie Token)";
					else
						return dt;
				}

				return "Unknown Date / Time";
			}
			catch (Exception)
			{
				return "Unknown Date / Time";
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		#endregion
	}

    public class UserGroupIdToGroupName : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((int)value == 1)
                    return "Administrator";
                else if((int)value == 2)
                    return "Teacher";
                else if((int)value == 3)
                    return "Student";
                else
                    return "Unkknown Group";
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    public class UserGroupIdToTeacherStatus : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((int)value == 2)
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((bool)value == true)
                    return 2;
                else
                    return 3;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
