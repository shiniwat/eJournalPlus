using System;
using System.Collections.Generic;
using System.Text;

namespace SiliconStudio.Meet.EjpControls.Enumerations
{
	/// <summary>
	/// The available editing modes for the Document Area.
	/// </summary>
	[Serializable]
	public enum KnowledgeMapEntityType
	{
		/// <summary>
		/// The contents of the text entity were copied from a 
		/// document in the document area and the Entity retains
		/// the link back to the location in the document where the
		/// original contents reside.
		/// </summary>
		ConnectedToDocument,

		/// <summary>
		/// The Entity was created by the user inside the map. Its
		/// contents were added by the user and it has no link to 
		/// any Document.
		/// </summary>
		OriginalToMap

	}//end: KnowledgeMapTextEntityType
}