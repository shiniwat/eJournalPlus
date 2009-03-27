/// -----------------------------------------------------------------
/// EjsConnectionHandler.cs: helper class that handles database connection.
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Linq;
using System.Web;
using System.Data.SqlClient;

namespace EjsWcfService
{
	internal static class EjsConnectionHandler
	{
		internal static SqlConnection OpenDBConnection(string connectionString)
		{
			try
			{
				//if (EjsConnectionHandler.ConnectionObject == null
				//    || EjsConnectionHandler.ConnectionObject.State == System.Data.ConnectionState.Broken
				//    || EjsConnectionHandler.ConnectionObject.State == System.Data.ConnectionState.Closed
				//)
				//{
				//    EjsConnectionHandler.ConnectionObject = new SqlConnection(connectionString);
				//    EjsConnectionHandler.ConnectionObject.Open();
				//}

				SqlConnection connection = new SqlConnection(connectionString);
				connection.Open();

				return connection;
			}
			catch (Exception ex)
			{
				//TODO: Add Logging code to event log
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.OpenDatabaseConnectionFailed,
					"Open Database Connection Failed",
					"The service cannot connect to the E Journal Server database at this moment.",
					ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
		}

		internal static void CloseDBConnection(SqlConnection connection)
		{
			try
			{

				connection.Close();
				connection.Dispose();

			}
			catch (Exception ex)
			{
				//TODO: Add Logging code to event log
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.CloseDatabaseConnectionFailed,
					"Close Database Connection Failed",
					"The service cannot disconnect from the E Journal Server database at this moment.",
					ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
		}
	}
}
