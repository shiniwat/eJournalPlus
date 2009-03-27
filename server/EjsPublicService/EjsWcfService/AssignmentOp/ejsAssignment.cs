/// -----------------------------------------------------------------
/// ejsAssignment.cs: Assignment operation stuff
/// connections to the Database.
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace EjsWcfService.AssignmentOp
{
	/// <summary>
	/// Data Contract for assignment operation.
	/// </summary>
	[DataContract]
	public class ejsAssignment
	{
		[DataMember]
		public int EJSDatabaseId;
		[DataMember]
		public string Title;
		[DataMember]
		public DateTime CreationDate;
		[DataMember]
		public DateTime LastModifiedDate;
		[DataMember]
		public int Version;
		[DataMember]
		public int AssignmentContentType;
		[DataMember]
		public bool IsManagedByEJournalServer;
		[DataMember]
		public bool IsAvailable;
		[DataMember]
		public Guid OwnerUserId;
		[DataMember]
		public int OriginalOwnerDbId;
		[DataMember]
		public int CommentCount;
		[DataMember]
		public int StudyCount;
		[DataMember]
		public string Description;
		[DataMember]
		public long DataSize;
		[DataMember]
		public long CourseId;
		[DataMember]
		public string OwnerName;
		[DataMember]
		public Guid ExternalAssignmentId;
		[DataMember]
		public Guid ParentAssignmentId;
		[DataMember]
		public ejsStudyMetaData[] studies;
	}
}
