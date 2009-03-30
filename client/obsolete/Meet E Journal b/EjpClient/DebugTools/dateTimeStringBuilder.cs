using System;

namespace ejpClient.DebugTools
{
	class DateTimeStringBuilder
	{
		public static string GetDateTimeString()
		{
			string result = DateTime.Today.Year.ToString() +
							DateTime.Today.Month.ToString() +
							DateTime.Today.Day.ToString() +
							"-" +
							DateTime.Now.Hour.ToString() +
							DateTime.Now.Minute.ToString() +
							DateTime.Now.Second.ToString() +
							DateTime.Now.Millisecond.ToString();

			return result;
		}
	}
}
