using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SiliconStudio.DebugManagers
{
	#region Console Window Handling
	enum CtrlMsgTypes : uint
	{
		_C_EVENT = 0,
		_BREAK_EVENT,
		_CLOSE_EVENT,
		_LOGOFF_EVENT = 5,
		_SHUTDOWN_EVENT
	}

	delegate bool ConsoleControlDelegate(CtrlMsgTypes msg);
	#endregion

	public static class DebugReporter
	{
		#region Console Window Handling
		//This is to open and run the output console.
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool FreeConsole();

		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool SetConsoleCtrlHandler(ConsoleControlDelegate ctrlHandler, bool Add);

		private static bool ConsoleCtrlHandler(CtrlMsgTypes msg)
		{
			switch (msg)
			{
				case CtrlMsgTypes._C_EVENT:
					FreeConsole();
					return true;
				case CtrlMsgTypes._BREAK_EVENT:
					FreeConsole();
					return true;
				case CtrlMsgTypes._CLOSE_EVENT:
					return true;
				case CtrlMsgTypes._LOGOFF_EVENT:
					return false;
				case CtrlMsgTypes._SHUTDOWN_EVENT:
					return false;
				default:
					return false;
			}
		}

		/// <summary>
		/// Need to hold on to that delegate in order to avoid 
		/// a CallbackOnCollectedDelegate exception.
		/// </summary>
		private static ConsoleControlDelegate _consoleCtrlDel = 
			new ConsoleControlDelegate(DebugReporter.ConsoleCtrlHandler);

		/// <summary>
		/// Tells wheter a console window has been allocated or not.
		/// </summary>
		private static bool _isConsoleOpen = false;

		/// <summary>
		/// Opens a console window for debug output
		/// </summary>
		private static void OpenDebugConsole()
		{
			try
			{
				if (DebugReporter._isConsoleOpen == false)
				{
					DebugReporter.AllocConsole();
					//Console.SetWindowSize(120, 60);
					//Console.SetBufferSize(120, 80);
					Console.Title = DebugReporter.Name + " Debug Output Console";
					Console.WriteLine("##########################################");
					Console.WriteLine(DebugReporter.Name + " Debug Output Console.");
					Console.WriteLine("DO NOT close this console by any other means than CONTROL-C!");
					Console.WriteLine("Closing this console with the Close Button will exit the application.");
					Console.WriteLine("##########################################");
					Console.WriteLine("");
					DebugReporter.SetConsoleCtrlHandler(DebugReporter._consoleCtrlDel, true);
					DebugReporter._isConsoleOpen = true;
				}
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Failed to open the debug console for output:\n" + ex.Message);
			}
		}


		#endregion

		#region Private properties

		/// <summary>
		/// Name of this reporter. Used to name files and
		/// give informative comments in the output file.
		/// </summary>
		public static string Name { get; set; }

		/// <summary>
		/// Status of the reporter.
		/// </summary>
		private static ReporterStatus _dManagerStatus = ReporterStatus.OK;

		/// <summary>
		/// Path to the file used to write and store debug output. Used
		/// only when debug output mode is set to File.
		/// </summary>
		private static string _outputFilePath =
			AppDomain.CurrentDomain.BaseDirectory +
								  @"TemporaryFiles\Debug\" + DebugReporter.Name + 
								  "_Execution_Log_" + Guid.NewGuid().ToString() + ".txt";

		/// <summary>
		/// Sets a filter to discard messages that are not needed.
		/// </summary>
		private static DebugLevels _outPutFilter = DebugLevels.All;

		/// <summary>
		/// Tells the method used to provide debug output.
		/// Setting the debug mode through the Accessor is
		/// required for the necessary initalization to run for each mode.
		/// </summary>
		private static DebugMode _debugMode = DebugMode.Output;

		#endregion

		#region Public Properties

		/// <summary>
		/// Tells the method used to provide debug output.
		/// Always set the debug mode through the Accessor.
		/// </summary>
		public static DebugMode DebugMode
		{
			get { return DebugReporter._debugMode; }
			set
			{
				if (value != DebugReporter._debugMode)
				{
					DebugReporter.SetDebugMode(value);
				}
			}
		}

		/// <summary>
		/// Use this mask setting to filter the messages being
		/// reported by the debugger.
		/// </summary>
		public static DebugLevels OutputFilter
		{
			set 
			{
				DebugReporter._outPutFilter = value;
				DebugReporter.Report(MessageType.Internal,
				"Debug Level changed.",
					"DebugReporter Debug Level changed to:\n" +
					DebugReporter._outPutFilter.ToString());
			}
		}

		#endregion

		#region Constructors
		static DebugReporter()
		{
			DebugReporter.Name = "Project Debug Reporter";
		}
		#endregion

		#region Reporter Setup and Management

		/// <summary>
		/// Prepares the necessary files and tools for the given
		/// debug mode...
		/// </summary>
		[Conditional("DEBUG")]
		private static void SetDebugMode(DebugMode mode)
		{
			//Perform Setup for the new mode...
			switch (mode)
			{
				case DebugMode.File:
					try
					{
						FileInfo fi = new FileInfo(DebugReporter._outputFilePath);

						if (!Directory.Exists(fi.Directory.FullName))
							Directory.CreateDirectory(fi.Directory.FullName);

						if (File.Exists(DebugReporter._outputFilePath))
							break;
						else
						{
							File.CreateText(DebugReporter._outputFilePath).Close();
						}

						DebugReporter._debugMode = mode;
					}
					catch (Exception ex)
					{
						DebugReporter.KillDebugReporter("Failed to set DebugMode File.\nException:\n" + ex.Message);
						return;
					}
					break;
				case DebugMode.Email:
					throw new NotImplementedException("This feature is not available in this project...");
				case DebugMode.Console:
					try
					{
						DebugReporter.OpenDebugConsole();
						DebugReporter._debugMode = mode;
					}
					catch (Exception ex)
					{
						DebugReporter.KillDebugReporter("Failed to set DebugMode Console.\nException:\n" + ex.Message);
						return;
					}

					break;
				case DebugMode.Wreq_v1:
					throw new NotImplementedException("This feature is not available in this project...");
				case DebugMode.None:
					DebugReporter._debugMode = mode;
					break;
				default:
					break;
			}

			DebugReporter.Report(MessageType.Internal,
				"Debug mode changed.",
					"DebugReporter Debug Mode changed to:\n" +
					DebugReporter._debugMode.ToString());
		}

		/// <summary>
		/// Kills the debug reporter, stops all reporting
		/// </summary>
		/// <param name="reason">Reason to kill the reporter.</param>
		private static void KillDebugReporter(string reason)
		{
			if (DebugReporter._dManagerStatus == ReporterStatus.OK)
			{
				DebugReporter.DebugMode = DebugMode.Output;
				DebugReporter.Report(MessageType.Internal,
					"Error", reason);
				DebugReporter._dManagerStatus = ReporterStatus.Dead;
			}
			else
			{
				DebugReporter._dManagerStatus = ReporterStatus.Dead;
				return;
			}
		}

		/// <summary>
		/// Restarts the reporter and resets all values to their defauls
		/// </summary>
		/// <param name="reason">Reason to restart the reporter.</param>
		private static void RestartReporter(string reason)
		{

		}

		#endregion

		#region Reporting

		/// <summary>
		/// Reports entering into a method.
		/// </summary>
		[Conditional("DEBUG")]
		public static void ReportMethodEnter() 
		{
			DebugReporter.WriteReport(MessageType.Information,
				"Entering Method", "Execution entering method.");
		}

		/// <summary>
		/// Reports leaving a method.
		/// </summary>
		[Conditional("DEBUG")]
		public static void ReportMethodLeave() 
		{
			DebugReporter.WriteReport(MessageType.Information,
				"Leaving Method", "Execution leaving method.");
		}

		/// <summary>
		/// Report a message through the Debug Manager. Time stamps and other
		/// info is addded automatically.
		/// </summary>
		/// <param name="Source">An identifier of the source of the message. Method name, parameter or sim.</param>
		/// <param name="Title">Title of the message.</param>
		/// <param name="Body">Message body.</param>
		[Conditional("DEBUG")]
		public static void Report(MessageType DebugMessageType, string Title, string Body)
		{
			DebugReporter.WriteReport(DebugMessageType,
				Title, Body);
		}

		/// <summary>
		/// Writes the report to the designated receiver. 
		/// </summary>
		private static void WriteReport(MessageType DebugMessageType, string Title, string Body)
		{
			//Test for the current Filter.
			bool shouldReport = false;

			//We always report internal messages...
			if (DebugMessageType == MessageType.Internal)
				shouldReport = true;
			else
			{
				switch (DebugMessageType)
				{
					case MessageType.Information:
						if ((DebugLevels.Information & DebugReporter._outPutFilter) > 0)
							shouldReport = true;
						break;
					case MessageType.Warning:
						if ((DebugLevels.Warning & DebugReporter._outPutFilter) > 0)
							shouldReport = true;
						break;
					case MessageType.Error:
						if ((DebugLevels.Error & DebugReporter._outPutFilter) > 0)
							shouldReport = true;
						break;
					default:
						break;
				}
			}

			if (!shouldReport)
				return;

			if (DebugReporter._dManagerStatus != ReporterStatus.OK)
			{
				throw new ApplicationException(DebugReporter.Name +
					" - The Debug Reporter is no longer alive. Cannot Write Report.");
			}

			//Get some additional data on the current stack frame.
			StackFrame frame = new StackFrame(2, true);
			string method = frame.GetMethod().Name;
			string file = frame.GetFileName();
			string line = frame.GetFileLineNumber().ToString();
			string typename = frame.GetMethod().DeclaringType.FullName;

			string TimeStamp = DateTimeStringBuilder.GetDateTimeString();

			try
			{
				switch (DebugReporter._debugMode)
				{
					case DebugMode.None:
						break;
					case DebugMode.File:
						DebugReporter.ReportToFile(DebugMessageType, TimeStamp, file, method, line, typename, Title, Body);
						break;
					case DebugMode.Email:
						DebugReporter.ReportToEmail(DebugMessageType, TimeStamp, file, method, line, typename, Title, Body);
						break;
					case DebugMode.Console:
						DebugReporter.ReportToConsole(DebugMessageType, TimeStamp, file, method, line, typename, Title, Body);
						break;
					case DebugMode.Output:
						DebugReporter.ReportToOutput(DebugMessageType, TimeStamp, file, method, line, typename, Title, Body);
						break;
					case DebugMode.Wreq_v1:
						DebugReporter.ReportToWreq_v1(DebugMessageType, TimeStamp, file, method, line, typename, Title, Body);
						break;
					default:
						break;
				}
			}
			catch (Exception ex)
			{
				DebugReporter.KillDebugReporter("Failed to Write Report.\nException:\n" + ex.Message);

				throw new ApplicationException(TimeStamp + DebugReporter.Name +
					"Unhandled Excpetion:\n" + ex.Message);
			}
		}

		private static void ReportToFile(MessageType messageType, string timestamp, string File, string Method, string Linenumber, string TypeName, string title, string body)
		{
			try
			{
				using (TextWriter tw = new StreamWriter(DebugReporter._outputFilePath, true))
				{
					if (messageType == MessageType.Error)
						tw.WriteLine("*****************");
					tw.WriteLine(messageType.ToString());
					tw.WriteLine(timestamp);
					tw.WriteLine("File name: " + File);
					tw.WriteLine("Type name: " + TypeName);
					tw.WriteLine("Method name: " + Method);
					tw.WriteLine("Line : " + Linenumber);
					tw.WriteLine(title);
					tw.WriteLine(body);
					if (messageType == MessageType.Error)
						tw.WriteLine("*****************");
					tw.WriteLine("");
					tw.Flush();
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		private static void ReportToWreq_v1(MessageType messageType, string timestamp, string File, string Method, string Linenumber, string TypeName, string title, string body)
		{
			throw new NotImplementedException(
				"Report to Wreq (v1): This feature is not available in this project...");
		}

		private static void ReportToConsole(MessageType messageType, string timestamp, string File, string Method, string Linenumber, string TypeName, string title, string body)
		{
			try
			{
				if (DebugReporter._isConsoleOpen == false)
					DebugReporter.OpenDebugConsole();

				if (messageType == MessageType.Error)
					Console.WriteLine("*****************");
				Console.WriteLine(messageType.ToString());
				Console.WriteLine(timestamp);
				Console.WriteLine("File: " + File);
				Console.WriteLine("Type: " + TypeName);
				Console.WriteLine("Method: " + Method);
				Console.WriteLine("Line: " + Linenumber);
				Console.WriteLine(title);
				Console.WriteLine(body);
				if (messageType == MessageType.Error)
					Console.WriteLine("*****************");
				Console.WriteLine("");
			}
			catch (Exception)
			{
				throw;
			}
		}

		private static void ReportToEmail(MessageType messageType, string timestamp, string File, string Method, string Linenumber, string TypeName, string title, string body)
		{
			throw new NotImplementedException(
				"Report to Email: This feature is not available in this project...");
		}

		/// <summary>
		/// Primitive output to the Visual Studio Output window...
		/// </summary>
		private static void ReportToOutput(MessageType messageType, string timestamp, string File, string Method, string Linenumber, string TypeName, string title, string body)
		{
			try
			{
				Debug.Print("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}", messageType, timestamp, File, Method, "Line: " + Linenumber, title, body);
			}
			catch (Exception) { /*Fail Silently for now...*/ }
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Helper to build a time date string.
		/// </summary>
		private class DateTimeStringBuilder
		{
			public static string GetDateTimeString()
			{
				string result = DateTime.Today.Year.ToString() +
								DateTime.Today.Month.ToString() +
								DateTime.Today.Day.ToString() +
								"-" +
								DateTime.Now.Hour.ToString() + ":" +
								DateTime.Now.Minute.ToString() + ":" +
								DateTime.Now.Second.ToString() + ":" +
								DateTime.Now.Millisecond.ToString() + ":";

				return result;
			}
		}

		#endregion
	}
}
