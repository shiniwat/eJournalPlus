/// -----------------------------------------------------------------
/// ejsCourseRegistration.cs: data contract of Course data
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace EjsWcfService.CourseOp
{
	[DataContract]
	public class ejsCourseRegistration
	{
		[DataMember]
		private int _courseId;
		public int CourseId
		{
			get { return _courseId; }
			set { _courseId = value; }
		}

		[DataMember]
		private string _userId;
		public string UserId
		{
			get { return _userId; }
			set { _userId = value; }
		}

		[DataMember]
		private DateTime _registerDate;
		public DateTime RegisterDate
		{
			get { return _registerDate; }
			set { _registerDate = value; }
		}

		public ejsCourseRegistration() { }
	}
}
