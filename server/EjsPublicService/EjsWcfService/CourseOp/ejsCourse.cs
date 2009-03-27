/// -----------------------------------------------------------------
/// ejsCourse.cs: data contract of Course data
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
	public class ejsCourse
	{
		[DataMember]
		private int _id;
		public int Id
		{
			get { return _id; }
			set { _id = value; }
		}

		[DataMember]
		private string _name;
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		[DataMember]
		private string _description;
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		[DataMember]
		private string _owner;
		public string Owner
		{
			get { return _owner; }
			set { _owner = value; }
		}

		[DataMember]
		private DateTime _creationDate;
		public DateTime CreationDate
		{
			get { return _creationDate; }
			set { _creationDate = value; }
		}

		[DataMember]
		private List<ejsCourseDocument> _documents;
		public List<ejsCourseDocument> Documents
		{
			get { return _documents; }
			set { _documents = value; }
		}

		[DataMember]
		private bool _isActive;
		public bool IsActive
		{
			get { return _isActive; }
			set { _isActive = value; }
		}

		public ejsCourse()
		{
			this.Documents = new List<ejsCourseDocument>();
		}
	}
}
