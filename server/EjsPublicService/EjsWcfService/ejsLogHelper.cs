/// -----------------------------------------------------------------
/// ejsLoghelper.cs: A helper class for logging
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace EjsWcfService
{
	public enum LoggingModes
	{
		None,
		//Console,
		System,
		Sql,
		//TextFile
	}
	
	internal static class ejsLogHelper
	{
		internal static string _connectionString = string.Empty;
		internal static LoggingModes LoggingMode = (ConfigurationManager.AppSettings["loggingMode"] == "Sql")
			? LoggingModes.Sql
			: LoggingModes.None;

		//	do nothing on this at this moment.,
		//	@todo: use eventlog for logging critical errors.
        internal static void LogMessage(string Message, bool AddLineBreak)
        {
            switch (ejsLogHelper.LoggingMode)
            {
                case LoggingModes.None:
                //case LoggingModes.Console:
                case LoggingModes.System:
                default:
                    break;
                    
                case LoggingModes.Sql:
					SqlLoggingSynchronous(Message);
					break;
            }
		}
		
		internal static void SqlLoggingSynchronous(string message)
		{
			if (string.IsNullOrEmpty(_connectionString))
			{
				_connectionString = ConfigurationManager.AppSettings["connectionString"];
			}
			
			SqlConnection connection = new SqlConnection(_connectionString);
			try
			{
				if (connection != null)
				{
					connection.Open();
					SqlCommand command = new SqlCommand("AddToLog", connection);
					command.CommandType = CommandType.StoredProcedure;
					command.Parameters.Add("@Text", SqlDbType.NVarChar).Value = message;
					command.Parameters.Add("@Cat", SqlDbType.Int).Value = 0;	//	@todo: unused so far; but we may use it in the future.
					int nres = command.ExecuteNonQuery();
				}
			}
			catch(SqlException se)
			{
				System.Diagnostics.Debug.WriteLine(se.Message);
			}
			catch(Exception aex)
			{
				System.Diagnostics.Debug.WriteLine(aex.Message);
			}
			finally
			{
				connection.Close();
			}
		}
		
	}
}
