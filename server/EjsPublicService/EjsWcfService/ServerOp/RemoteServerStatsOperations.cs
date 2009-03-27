/// -----------------------------------------------------------------
/// RemoteServerStatsOperations.cs: Server Operations stuff
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace EjsWcfService.ServerOp
{
	public class RemoteServerStatsOperations
	{
		/// <summary>
		/// Returns all the courses that a particular user has registered to.
		/// </summary>
		internal static void GetServerStats(ref ejsServerStats result)
		{
			result = new ejsServerStats();
			result.ServerName = (string)ConfigurationManager.AppSettings["serverName"];
			result.CurrentSessions = sessionManager.TokenPool.GetSafePoolCopy();
			result.CurrentLiveConnectionCount = sessionManager.connectionCount;
		}
	}
}
