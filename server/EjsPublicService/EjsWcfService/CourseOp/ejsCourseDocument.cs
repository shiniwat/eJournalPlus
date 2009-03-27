/// -----------------------------------------------------------------
/// ejsCourseDocument.cs: data contract of Course data
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
	public class ejsCourseDocument
	{
		[DataMember]
		private Guid _documentId;
		public Guid DocumentId
		{
			get { return _documentId; }
			set { _documentId = value; }
		}

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
		private DateTime _creationDate;
		public DateTime CreationDate
		{
			get { return _creationDate; }
			set { _creationDate = value; }
		}

		[DataMember]
		private long _byteSize;
		public long ByteSize
		{
			get { return _byteSize; }
			set { _byteSize = value; }
		}

		[DataMember]
		private int _courseId;
		public int CourseId
		{
			get { return _courseId; }
			set { _courseId = value; }
		}

		[DataMember]
		private bool _isAvailable;
		public bool IsAvailable
		{
			get { return _isAvailable; }
			set { _isAvailable = value; }
		}

		public ejsCourseDocument()
		{
		}
	}
}
