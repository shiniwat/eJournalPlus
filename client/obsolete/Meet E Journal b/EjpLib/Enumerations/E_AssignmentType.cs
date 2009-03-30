
namespace SiliconStudio.Meet.EjpLib.Enumerations
{
	/// <summary>
	/// Available types of assignments.
	/// </summary>
	public enum AssignmentType
	{
		/// <summary>
		/// The assignment is still being edited and contains no
		/// comments from any other user. The assignment has never
		/// been saved by any other user than the Original Creator.
		/// </summary>
		WorkingAssignment = 0,

		/// <summary>
		/// The assignment contains comments from a user other
		/// than the Original Creator. Parts of the assignment
		/// has been locked and can no longer be edited.
		/// </summary>
		CommentedAssignment = 1
	}//end: AssignmentType
}