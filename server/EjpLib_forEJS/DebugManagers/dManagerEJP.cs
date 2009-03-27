using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace SiliconStudio.Meet.EjpLib.DebugManagers
{
    public enum DebugModeEJP
    {
        None,
        File,
        Email,
        Console,
        Output
    }

    public enum dManagerStatus
    {
        OK,
        Dead,
        Zombie
    }

    public static class dManagerEJP
    {
        private static string _name = "eJournalPlus Debug Manager";

        private static dManagerStatus _dManagerStatus = dManagerStatus.OK;

        private static string _outputFilePath =
            AppDomain.CurrentDomain.BaseDirectory +
                                  @"TemporaryFiles\Debug\" + "Execution_Log_" +
                                  Guid.NewGuid().ToString() + ".txt";

        private static DebugModeEJP _debugMode = DebugModeEJP.Output;
        public static DebugModeEJP DebugMode
        {
            get { return dManagerEJP._debugMode; }
            set
            {
                if (value != dManagerEJP._debugMode)
                {
                    dManagerEJP.SetDebugMode(value);
                }
            }
        }

        /// <summary>
        /// Prepares the necessary files and tools for the given
        /// debug mode...
        /// </summary>
        private static void SetDebugMode(DebugModeEJP mode)
        {
            dManagerEJP._debugMode = mode;
            //Perform Setup for the new mode...
            switch (mode)
            {
                case DebugModeEJP.File:
                    try
                    {
                        FileInfo fi = new FileInfo(dManagerEJP._outputFilePath);

                        if (!Directory.Exists(fi.Directory.FullName))
                            Directory.CreateDirectory(fi.Directory.FullName);

                        if (File.Exists(dManagerEJP._outputFilePath))
                            break;
                        else
                            File.CreateText(dManagerEJP._outputFilePath);
                    }
                    catch (Exception ex)
                    {
                        if (dManagerEJP._dManagerStatus == dManagerStatus.OK)
                        {
                            dManagerEJP.DebugMode = DebugModeEJP.Output;
                            dManagerEJP.Report("dManager Initialization Error",
                                "Failed to set DebugMode File.\nException:\n{0}",
                                ex.Message);
                            dManagerEJP._dManagerStatus = dManagerStatus.Dead;
                        }
                        else
                        {
                            dManagerEJP._dManagerStatus = dManagerStatus.Dead;
                            return;
                        }
                    }
                    break;
                case DebugModeEJP.Email:
                    throw new NotImplementedException("This feature is not available in this project...");
                    break;
                case DebugModeEJP.Console:
                    throw new NotImplementedException("This feature is not available in this project...");
                    break;
                default:
                    break;
            }

            dManagerEJP.Report(dManagerEJP._name, "Debug mode changed.",
                    "Debug Manager Debug Mode changed to:\n" + 
                    dManagerEJP._debugMode.ToString());
        }

        /// <summary>
        /// Report a message through the Debug Manager. Time stamps and other
        /// info is addded automatically.
        /// </summary>
        /// <param name="Source">An identifier of the source of the message. Method name, parameter or sim.</param>
        /// <param name="Title">Title of the message.</param>
        /// <param name="Body">Message body.</param>
        internal static void Report(string Source, string Title, string Body)
        {
            string TimeStamp = DateTimeStringBuilder.GetDateTimeString();
            try
            {
                switch (dManagerEJP._debugMode)
                {
                    case DebugModeEJP.None:
                        break;
                    case DebugModeEJP.File:
                        dManagerEJP.ReportToFile(TimeStamp, Source, Title, Body);
                        break;
                    case DebugModeEJP.Email:
                        dManagerEJP.ReportToEmail(TimeStamp, Source, Title, Body);
                        break;
                    case DebugModeEJP.Console:
                        dManagerEJP.ReportToConsole(TimeStamp, Source, Title, Body);
                        break;
                    case DebugModeEJP.Output:
                        dManagerEJP.ReportToOutput(TimeStamp, Source, Title, Body);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(TimeStamp + dManagerEJP._name +
                    "Debug Manager Excpetion:\n" + ex.Message);
            }
        }

        private static void ReportToFile(string timestamp, string source, string title, string body)
        {
            try
            {
                using (TextWriter tw = new StreamWriter(dManagerEJP._outputFilePath))
                {
                    tw.Write(timestamp + "\n" + source + "\n" + title + "\n" + body + "\n");
                    tw.Flush();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static void ReportToConsole(string timestamp, string source, string title, string body)
        {
            throw new NotImplementedException(
                "Report to Console: This feature is not available in this project...");
        }

        private static void ReportToEmail(string timestamp, string source, string title, string body)
        {
            throw new NotImplementedException(
                "Report to Email: This feature is not available in this project...");
        }

        /// <summary>
        /// Primitive output to the Output window...
        /// </summary>
        private static void ReportToOutput(string timestamp, string source, string title, string body)
        {
            try
            {
                Debug.Print("{0}\n{1}\n{2}\n{3}", timestamp, source, title, body);
            }
            catch (Exception) { /*Fail Silently for now...*/ }
        }

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
                                DateTime.Now.Hour.ToString() +
                                DateTime.Now.Minute.ToString() +
                                DateTime.Now.Second.ToString() +
                                DateTime.Now.Millisecond.ToString();

                return result;
            }
        }
    }
}
