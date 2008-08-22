using System;
using System.Collections.Generic;
using System.Text;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
	/// <summary>
	/// Abstract Base class for all Assignment Parts.
	/// </summary>
	public abstract class ejpAssignmentPart
	{
		#region Properties with Accessors
		private Guid _parentAssignmentId;
		public Guid ParentAssignmentId
		{
			get { return _parentAssignmentId; }
			set { _parentAssignmentId = value; }
		}

		private Guid _id;
		public Guid Id
		{
			get { return _id; }
			set { _id = value; }
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Private default constructor
		/// </summary>
		private ejpAssignmentPart() 
		{
			throw new InvalidOperationException("Never instantiate this class directly!");
		}//end: Constructor

		/// <summary>
		/// Protected base constructor.
		/// </summary>
		/// <param name="parentAssignmentId">Guid of the Assignment to which this part belongs.</param>
		protected ejpAssignmentPart(Guid parentAssignmentId)
		{
			this._parentAssignmentId = parentAssignmentId;
		}//end: Constructor
		#endregion

	}//end: ejpAssignmentPart
}
