using System;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
	/// <summary>
	/// Abstract Base class for all Assignment Parts.
	/// </summary>
	[Serializable]
	public abstract class ejpStudyPart
	{
		#region Properties with Accessors
		private Guid _parentStudyId;
		public Guid ParentStudyId
		{
			get { return _parentStudyId; }
			set { _parentStudyId = value; }
		}

		private Guid _id;
		public Guid Id
		{
			get { return _id; }
			set { _id = value; }
		}

		/// <summary>
		/// The string that identifies this studyPart within its parent Assignment Package.
		/// This is a package Id and not the ID of the study itself.
		/// </summary>
		public string PackageRelationshipIDString { get; set; }

		#endregion

		#region Constructors
		/// <summary>
		/// Private default constructor
		/// </summary>
		private ejpStudyPart()
		{
			throw new InvalidOperationException("Never instantiate this class directly!");
		}//end: Constructor

		/// <summary>
		/// Protected base constructor.
		/// </summary>
		/// <param name="parentAssignmentId">Guid of the Assignment to which this part belongs.</param>
		protected ejpStudyPart(Guid parentStudyId)
		{
			this._parentStudyId = parentStudyId;
		}//end: Constructor
		#endregion

	}//end: ejpStudyPart
}
