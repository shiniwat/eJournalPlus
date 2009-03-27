/// -----------------------------------------------------------------
/// ejsStudyMetadata.cs: Assignment operation stuff
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
	[DataContract]
	public class ejsStudyMetaData
	{
		[DataMember]
		public Guid UserId;
		[DataMember]
		public string Title;
		[DataMember]
		public string Description;
		[DataMember]
		public Guid OwnerId;
		[DataMember]
		public int ParentAssignmentId;
		[DataMember]
		public DateTime CreationDate;
		[DataMember]
		public DateTime LastModifiedDate;
		[DataMember]
		public bool IsAvailable;
		[DataMember]
		public int CommentCount;
	}
}
