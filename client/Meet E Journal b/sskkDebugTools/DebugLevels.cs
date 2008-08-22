using System;

namespace SiliconStudio.DebugManagers
{
	[Flags]
	public enum DebugLevels
	{
		None = 0,
		Information = 1,
		Warning = 2,
		Error = 4,
		All = Information | Warning | Error
	}
}