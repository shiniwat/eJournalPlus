using System;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
	/// <summary>
	/// Holds Meta data for an Assignment
	/// </summary>
	[Serializable]
	public class ejpAssignmentMetaData
	{
		public int EJSDatabaseId;
		public string Title;
		public Guid Id;
		public DateTime CreationDate;
		public DateTime LastModifiedDate;
		public int Revision;
		public int Version;
		public Enumerations.AssignmentType AssignmentContentType;
		public bool IsManagedByEJournalServer;
        public Guid OwnerUserId;
  
		public ejpAssignmentMetaData(Guid id, string title, int ejsDatabaseId, DateTime creationDate, 
			DateTime lastModifiedDate, int version, int revision, Enumerations.AssignmentType assignmentContentType, 
			bool isManagedByEJournalServer, Guid ownerUserId)
		{
            this.OwnerUserId = ownerUserId;
			this.Id = id;
			this.Title = title;
			this.EJSDatabaseId = ejsDatabaseId;
			this.CreationDate = creationDate;
			this.LastModifiedDate = lastModifiedDate;
			this.Revision = revision;
			this.Version = version;
			this.AssignmentContentType = assignmentContentType;
			this.IsManagedByEJournalServer = isManagedByEJournalServer;
		}

		public ejpAssignmentMetaData() { }

	}//end: ejpAssignmentMetaData

	/// <summary>
	/// Holds Meta data for a study within an Assignment
	/// </summary>
	[Serializable]
	public class ejpStudyMetaData
	{
		public int EJSDatabaseId;
		public Guid ParentAssignmentId;
		public Guid Id;
		public String Title;

		public ejpStudyMetaData(Guid parentAssignmentId, Guid id, string title, int ejsDatabaseId)
		{
			this.ParentAssignmentId = parentAssignmentId;
			this.Title = title;
			this.EJSDatabaseId = ejsDatabaseId;
			this.Id = id;
		}

		public ejpStudyMetaData() { }

	}//end: ejpAssignmentMetaData

}
