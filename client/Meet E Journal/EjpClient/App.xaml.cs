using System;
using System.Text.RegularExpressions;
using System.Windows;
using SiliconStudio.Meet.EjpLib.BaseClasses;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Configuration;

namespace ejpClient
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>

	public partial class App : System.Windows.Application
	{
		/// <summary>
		/// Application wide static user object.
		/// </summary>
		internal static ejpStudent _currentEjpStudent = new ejpStudent(
			new EjsBridge.ejsService.ejsSessionToken
			{
				_creationTimeStamp = DateTime.Now,
				_expireTimeStamp = DateTime.Now.AddHours(12),
				_sourceHostId = Guid.NewGuid(),
				_userId = Guid.NewGuid()
			});

		/// <summary>
		/// Application wide static settings object.
		/// </summary>
		internal static ejpClient.EJPSettings _ejpSettings = new ejpClient.EJPSettings
		{
			EjsAddress = "",
			LiveSpaceUri = "",
			SaveUserSettings = false,
			ShowMapLock = true,
			UndoCount = 50,
			UserName = "UnknownUser",
			VersionString = "Version 1-8-3"
		};

		public static bool _hasDefaultDocumentOnLoad;
		public static bool _requestedOpenFromEjsOnLoad;
		public static bool _requestedNewEmptyAssignmentOnLoad;
		public static bool _requestedNewAssignmentOnLoad;
		public static bool _requestedDocumentIsCA;
		public static string _defaultDocumentPath;

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);
			if (App._currentEjpStudent.SessionToken._isAuthenticated == true)
			{
				EjsBridge.ejsBridgeManager.LogOutUser(
					App._currentEjpStudent.SessionToken);
			}
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			try
			{
				string uiCultureSetting = ConfigurationManager.AppSettings.Get("UICulture");
				if (!string.IsNullOrEmpty(uiCultureSetting))
				{
					CultureInfo culture = new CultureInfo(uiCultureSetting);
					Thread.CurrentThread.CurrentUICulture = culture;
				}
			}
			catch(ArgumentException ae)
			{
				//	OK, the specified culture is invalid. Let's ignore, and rely on current user UI culture.
				System.Diagnostics.Debug.WriteLine(ae.Message);
			}
			catch(ConfigurationException ce)
			{
				System.Diagnostics.Debug.WriteLine(ce.Message);
			}
			base.OnStartup(e);

			if (e.Args.Length == 0)
				return;

			//only one args usually means the application
			//was launched by doubleclicking a file...
			if (e.Args.Length == 1)
			{
				if (File.Exists(e.Args[0]))
				{
					App._defaultDocumentPath = e.Args[0];
					App._hasDefaultDocumentOnLoad = true;
				}
			}

			string argPat = @"(?<argname>/\w+):(?<argvalue>.+)";
			foreach (string arg in e.Args)
			{
				Match m = Regex.Match(arg, argPat);
				if (m.Success)
				{
					Console.WriteLine(m.Groups["argname"].Value + "->" + m.Groups["argname"].Value);
					switch (m.Groups["argname"].Value)
					{
						case @"/m": //Added 080714
							if (m.Groups["argvalue"].Value == "CA")
							{
								App._requestedDocumentIsCA = true;
							}
							else if (m.Groups["argvalue"].Value == "A")
							{
								//Default...
								App._requestedDocumentIsCA = false;
							}
							break;
						case @"/l":
							if (m.Groups["argvalue"].Value.EndsWith(".ejp")
								|| m.Groups["argvalue"].Value.EndsWith(".cejp"))
							{
								App._defaultDocumentPath = m.Groups["argvalue"].Value;
								App._hasDefaultDocumentOnLoad = true;
							}
							else if (m.Groups["argvalue"].Value == "ejs")
							{
								App._hasDefaultDocumentOnLoad = true;
								App._requestedOpenFromEjsOnLoad = true;
							}
							break;
						case @"/n":
							if (m.Groups["argvalue"].Value == "empty")
								App._requestedNewEmptyAssignmentOnLoad = true;
							else if (m.Groups["argvalue"].Value == "normal")
								App._requestedNewAssignmentOnLoad = true;
							break;
						default:
							break;
					}
				}
			}
		}

		public static bool IsCurrentUserEJSAuthenticated()
		{
			if (App._currentEjpStudent.SessionToken._isAuthenticated)
			{
				if (App._currentEjpStudent.SessionToken._expireTimeStamp
					> DateTime.Now)
				{
					return true;
				}
				else
					return false;
			}
			else
				return false;
		}
	}
}