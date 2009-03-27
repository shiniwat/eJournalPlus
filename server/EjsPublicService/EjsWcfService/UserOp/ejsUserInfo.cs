/// -----------------------------------------------------------------
/// ejsUserInfo.cs: data contract of session user data
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace EjsWcfService.UserOp
{
	[DataContract]
	public class ejsUserInfo
	{
		[DataMember]
		public string FirstName;
		[DataMember]
		public string LastName;
		[DataMember]
		public string Email;
		[DataMember]
		public string Id;

		[DataMember]
		public int UserGroupId;

		[DataMember]
		public string DatabaseName;
		[DataMember]
		public bool IsAccountActive;
		[DataMember]
		public bool IsLoggedIn;
		[DataMember]
		public string UserName;
	}
}
