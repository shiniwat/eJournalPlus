using System;

namespace SiliconStudio.Meet.EjpLib.Helpers
{
	/// <summary>
	/// Wrapper to allow for future ID manipulation.
	/// </summary>
	public static class IdManipulation
	{
		public static Guid GetNewGuid()
		{
			return Guid.NewGuid();
		}
	}
}
