
namespace SiliconStudio.Meet.EjpControls.Enumerations
{
	/// <summary>
	/// The available editing modes for the Document Area.
	/// </summary>
	public enum DocumentAreaInputMehtod
	{
		/// <summary>
		/// The DocumentArea accepts no input.
		/// </summary>
		None,

		/// <summary>
		/// Any selectable enitity in the 
		/// DocumentArea is selected upon Mouse
		/// Click.
		/// </summary>
		Select,

		/// <summary>
		/// Any erasable entity in the DocumentArea
		/// is erased upon Mouse Click.
		/// </summary>
		Erase,

		/// <summary>
		/// The DocumentArea interprets all Mouse
		/// Interaction as Drawing commands based on 
		/// the currently set value from the 
		/// DocuemntAreaDrawingMode enumeration.
		/// </summary>
		Draw
	}//end: DocumentAreaInputMehtod
}