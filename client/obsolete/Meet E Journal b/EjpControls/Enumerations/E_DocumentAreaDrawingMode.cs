
namespace SiliconStudio.Meet.EjpControls.Enumerations
{
	/// <summary>
	/// The available editing modes for the Document Area.
	/// </summary>
	public enum DocumentAreaDrawingMode
	{
		/// <summary>
		/// Drawing is disabled, the DocumentArea
		/// cannot be drawed upon.
		/// </summary>
		None,

		/// <summary>
		/// All drawing commands are interpreted as
		/// Pen lines.
		/// </summary>
		PenLine,

		/// <summary>
		/// All drawing commands are interpreted as
		/// Marker lines.
		/// </summary>
		MarkerLine,

		/// <summary>
		/// Drawing commands are not interpreted.
		/// </summary>
		Freehand,

	}//end: DocumentAreaDrawingMode
}