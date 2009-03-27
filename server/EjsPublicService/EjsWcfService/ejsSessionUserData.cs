/// -----------------------------------------------------------------
/// ejsSessionUserData.cs: A helper class that maintains session user data
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EjsWcfService
{
	/// <summary>
	/// Built to represent a user object whithin the service.
	/// </summary>
	public class ejsSessionUserData
	{
		private string _userName;
		public string UserName
		{
			get { return _userName; }
			set { _userName = value; }
		}

		private ejsSessionToken _sessionToken;
		public ejsSessionToken SessionToken
		{
			get { return _sessionToken; }
			set { _sessionToken = value; }
		}

		public ejsSessionUserData(string UserName, ejsSessionToken SessionToken)
		{
			this._userName = UserName;
			this._sessionToken = SessionToken;
		}
	}
}
