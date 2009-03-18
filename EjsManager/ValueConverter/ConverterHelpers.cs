using System;
using System.Collections.Generic;
using System.Text;

namespace SiliconStudio.Meet.EjsManager
{
	public static class ConverterHelpers
	{
		public static ejsServiceReference.ejsUserInfo GetUserByIdString(string Id)
		{
			try
			{
				ObservableUserList l =
						  App.Current.Resources["CompleteUsersList"] as ObservableUserList;

				foreach (ejsServiceReference.ejsUserInfo user in l)
				{
					if (user.Id == Id)
						return user;
				}
				return null;

			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
