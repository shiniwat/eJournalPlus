/// -----------------------------------------------------------------
/// ejsServerStats.cs: data contract of Server operation stuff
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace EjsWcfService.ServerOp
{
	[DataContract]
	public class ejsServerStats
	{
		[DataMember]
		private string _serverName;
		public string ServerName
		{
			get { return _serverName; }
			set { _serverName = value; }
		}

		[DataMember]
		private ejsSessionToken[] _currentSessions;
		public ejsSessionToken[] CurrentSessions
		{
			get { return _currentSessions; }
			set { _currentSessions = value; }
		}

		[DataMember]
		private int _currentLiveConnectionCount;
		public int CurrentLiveConnectionCount
		{
			get { return _currentLiveConnectionCount; }
			set { _currentLiveConnectionCount = value; }
		}
	}
}
