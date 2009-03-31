/// -----------------------------------------------------------------
/// sessionManager.cs: static class that manages EJS user sessions.
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.IO.IsolatedStorage;

namespace EjsWcfService
{
	internal static class sessionManager
	{
		internal static ejsSessionPool TokenPool = new ejsSessionPool();
		internal static int connectionCount = 0;
		//internal static string TemporaryStorageLocation;	//	unused.
		internal static TimeoutException	_sessionPoolCycleTimer;
		internal static IsolatedStorageFile _isoStore;// = IsolatedStorageFile.GetStore(IsolatedStorageScope.Application, null, null);
		
		public static void ClearTempFiles()
		{
			if (_isoStore == null)
			{
				// user store doesn't always work especially if it runs under ASP.NET context.
				// Should use GetMachineStoreForDomain() instead.
				//sessionManager._isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.User, null, null);
				sessionManager._isoStore = IsolatedStorageFile.GetMachineStoreForDomain();
			}
			try
			{
				string[] fileNames = _isoStore.GetFileNames("*");
				foreach(string name in fileNames)
				{
					File.Delete(name);
				}
			}
			catch(IOException ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}
		}
	}
}
