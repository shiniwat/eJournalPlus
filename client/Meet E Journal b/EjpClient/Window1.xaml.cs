#define INTERNAL_BUILD
//#define PUBLIC_BUILD

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ejpClient.DebugTools;
using ejpClient.Enumerations;
using Microsoft.Win32;
using SiliconStudio.Meet.EjpControls;
using SiliconStudio.Meet.EjpLib.BaseClasses;
using SiliconStudio.Meet.EjpLib.Helpers;
using WlsBridge;
using winForms = System.Windows.Forms;

namespace ejpClient
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>

    public delegate void ao_AutoSave(bool GenerateNewFile);

    public enum ToolBoxState
    {
        None,
        Report,
        KnowledgeMap,
        Document
    }

    public enum Tool
    {
        Pen,
        Marker,
        Eraser,
        Label,
        Freehand,
        Line,
        SingleArrow,
        DoubleArrow,
        Square,
        Circle,
        Note,
        Select
    }



	public partial class Window1 : System.Windows.Window
	{
        private ApplicationState _currentAppState = ApplicationState.Cold;
		private ejpAssignment _currentWorkingAssingment = null;
		private Binding _bindings_StudiesBinding = null;
        private KnowledgeMap _currentKnowledgeMapControl;
        private XpsDocumentViewer _currentXpsDocumentViewer;
        private ReportEditor _currentReportEditor;
        private ejpStudy _currentWorkingStudy = null;
		private List<KnowledgeMap> _openknowledgeMaps;
		private List<XpsDocumentViewer> _openXpsDocumentViewers;
		private List<ReportEditor> _openReports;

        private Dictionary<string, SolidColorBrush> _documentAreaPenColorsList;
        private Dictionary<string, SolidColorBrush> _documentAreaMarkerColorsList;
        private Dictionary<string, SolidColorBrush> _kmAreaColorsList;

        private Tool _previousKmTool = Tool.Select;
        private ToolBoxState _previousToolBoxState;

		private string _localAppDataPath;
        private string _localAppSettingsaPath;

        private System.Timers.Timer _autoSaveTimer;
        private string _autoSaveFilePath;
        private bool _canAutoSave;

		public Window1()
		{
			InitializeComponent();

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(currentDomain_AssemblyResolve);

            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);

            //Set the debug mode 
#if PUBLIC_BUILD
			SiliconStudio.DebugManagers.DebugReporter.DebugMode = 
				SiliconStudio.DebugManagers.DebugMode.None;
#endif
#if INTERNAL_BUILD
            SiliconStudio.DebugManagers.DebugReporter.DebugMode = 
				SiliconStudio.DebugManagers.DebugMode.None;
#endif
            SiliconStudio.DebugManagers.DebugReporter.OutputFilter = 
				SiliconStudio.DebugManagers.DebugLevels.Warning | SiliconStudio.DebugManagers.DebugLevels.Error;

			SiliconStudio.DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Information,
				"eJournalPlus Client", "Application Started.");

            this.SetApplicationWidePaths();
            this.LoadApplicationSettings(this._localAppSettingsaPath + "tempSettings.bej");
            this.SetApplicationState(ApplicationState.Cold);
            this._previousToolBoxState = ToolBoxState.None;
			this._openknowledgeMaps = new List<KnowledgeMap>();
			this._openXpsDocumentViewers = new List<XpsDocumentViewer>();
			this._openReports = new List<ReportEditor>();

            this._M_km_ColorSwatchButton.ActiveTextColorChanged += 
                new ActiveColorChanged(_M_km_ColorSwatchButton_ActiveColorChanged);

            this._M_PL_ColorSwatchButton.ActiveTextColorChanged +=
                new ActiveColorChanged(_M_pl_ColorSwatchButton_ActiveColorChanged);
            this._M_ML_ColorSwatchButton.ActiveTextColorChanged +=
                            new ActiveColorChanged(_M_ml_ColorSwatchButton_ActiveColorChanged);

            //Make sure the pen/marker line is disabled if the user depresses a
            //doc line button.
            this._M_ML_ColorSwatchButton.OnSwitchButtonUnChecked += 
                new SwitchButtonUnChecked(_M_MarkerLineButtonUncked);
            this._M_PL_ColorSwatchButton.OnSwitchButtonUnChecked +=
                new SwitchButtonUnChecked(_M_PenLineButtonUncked);

            this._gS_MainDivider.DragCompleted += 
                new System.Windows.Controls.Primitives.DragCompletedEventHandler(_gS_MainDivider_DragCompleted);

            this.LoadColorSwatches();
            this.UpdateWindowTitle();

            //We need to hook up some special handlers to deal
            //with km entity copy, paste and delete.
            //it is best to hook them up here since they are 
            //global and tied to the type.
            //At this point these handlers do nothing but prevent the application
            //from crashing.
            CommandManager.RegisterClassCommandBinding(typeof(InkCanvas),
                new CommandBinding(ApplicationCommands.Paste, HandleKMPasteCommandExecuted));
            CommandManager.RegisterClassCommandBinding(typeof(InkCanvas),
                new CommandBinding(ApplicationCommands.Copy, HandleKMCopyCommandExecuted));
            CommandManager.RegisterClassCommandBinding(typeof(InkCanvas),
               new CommandBinding(ApplicationCommands.Delete, HandleKMDelete));

            CommandManager.RegisterClassCommandBinding(typeof(XpsDocumentViewer),
                new CommandBinding(ApplicationCommands.Undo, HandleKMDMUndo));
            CommandManager.RegisterClassCommandBinding(typeof(XpsDocumentViewer),
                new CommandBinding(ApplicationCommands.Redo, HandleKMDMRedo));

            CommandManager.RegisterClassCommandBinding(typeof(KnowledgeMap),
                new CommandBinding(ApplicationCommands.Undo, HandleKMDMUndo));
            CommandManager.RegisterClassCommandBinding(typeof(KnowledgeMap),
                new CommandBinding(ApplicationCommands.Redo, HandleKMDMRedo));

            //this.SetApplicationWidePaths();
            this._autoSaveTimer = new System.Timers.Timer(180000);
            this._autoSaveTimer.Elapsed += new ElapsedEventHandler(_autoSaveTimer_Elapsed);
            this._autoSaveTimer.Start();

            this._canAutoSave = true;

#if INTERNAL_BUILD
            Application.Current.DispatcherUnhandledException +=
                new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(
                Current_DispatcherUnhandledException);
#endif

			//Magnifier Stuff
			VisualBrush mg_b = (VisualBrush)mg_Visual.Fill;
			mg_b.Visual = this.LayoutRoot;

			Rect viewBox = mg_b.Viewbox;
			viewBox.Width = 20;
			viewBox.Height = 20;
			mg_b.Viewbox = viewBox;
        }

        /// <summary>
        /// Need to keep track of when the machine sleeps to make sure that 
        /// it does not try to pile up AutoSave events...
        /// </summary>
        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    this._autoSaveTimer.Start();
                    break;
                case PowerModes.StatusChange:
                    break;
                case PowerModes.Suspend:
                    this._autoSaveTimer.Stop();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Since we are not allowed to redistribute the Winwdows Live Client SDK dlls as
        /// stand alone files we have to hook up this event to tell the application where
        /// to find the appropriate files...
        /// </summary>
        private Assembly currentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly loadedAssembly = null;
            string strTempAssmbPath = "";

            if (args.Name.Substring(0, args.Name.IndexOf(",")).Contains("WindowsLive"))
            {
                string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                strTempAssmbPath = programFilesPath + "\\Common Files\\Microsoft Shared\\WLIDClient\\"
                        + args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                try
                {
                    loadedAssembly = Assembly.LoadFrom(strTempAssmbPath);
                }
                catch (FileNotFoundException)
                {
                    this._menuI_PublishToWLS.IsEnabled = false;
                    //assembly could not be found...
                    throw new ApplicationException("Windows Live Spacesと接続するための必要なファイルが見つかりませんでした。\n" +
                                        "Windows Live Spacesに公開することが出来ません。eJournalPlusを再インストールして下さい。\n" +
                                        "(Windows Live Spaces以外の機能には影響がありません。)");
                }
            }

            return loadedAssembly;
        }

        /// <summary>
        /// Sets paths to temp folders and save locations, creating any
        /// missing directories that the user migh have deleted.
        /// </summary>
        private void SetApplicationWidePaths()
        {
			string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			baseDir += @"\Meet\eJournalPlus";

			if (!Directory.Exists(baseDir))
			{
				try
				{
					Directory.CreateDirectory(baseDir);
				}
				catch (Exception ex)
				{
					SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Creating and setting AppData path Failed" +
						 "\nError: " + ex.Message);

                    MessageBox.Show("アプリケーションのデータを保存するフォルダを作成できません。\n"+
                        "このため，自動保存をすることはできません。", "エラー",
						MessageBoxButton.OK, MessageBoxImage.Warning);
					this._canAutoSave = false;
				}
			}

            //string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            //Create AutoSave dir
            this._autoSaveFilePath = baseDir + @"\AutoSave\";
            if (!Directory.Exists(this._autoSaveFilePath))
            {
                try
                {
                    Directory.CreateDirectory(this._autoSaveFilePath);
                }
                catch (Exception ex)
                {
					SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Creating and setting Auto Save path Failed" +
						 "\nError: " + ex.Message);

                    MessageBox.Show("自動保存のためのフォルダを作成できません。\n" +
                        "このため，自動保存をすることはできません。", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    this._canAutoSave = false;
                }
            }
            else
            {
                if(this._currentAppState == ApplicationState.ComAssignmentLoaded)
                    this._autoSaveFilePath += "AutoSaveEjp" + Guid.NewGuid().ToString() + ".cejp";
                else
                    this._autoSaveFilePath += "AutoSaveEjp" + Guid.NewGuid().ToString() + ".ejp";
                this._canAutoSave = true;
            }

            //Create AutoSave dir
            this._localAppSettingsaPath = baseDir + @"\Settings\";
            if (!Directory.Exists(this._localAppSettingsaPath))
            {
                try
                {
                    Directory.CreateDirectory(this._localAppSettingsaPath);
                }
                catch (Exception ex)
                {
                    SiliconStudio.DebugManagers.DebugReporter.Report(
                         SiliconStudio.DebugManagers.MessageType.Error,
                         "eJournalPlus Client",
                         "Creating and setting App Settings path Failed" +
                         "\nError: " + ex.Message);

                    MessageBox.Show("アプリケーション設定のフォルダを作成できません。", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            
            //Create Temp dir
            string tempAssSavePath = baseDir + "\\TemporaryFiles\\";
            if (!Directory.Exists(tempAssSavePath))
            {
                try
                {
                    Directory.CreateDirectory(tempAssSavePath);
                }
                catch (Exception ex)
                {
					SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Creating and setting Temporary Assignment path Failed" +
						 "\nError: " + ex.Message);

                    MessageBox.Show("一時保存のためのフォルダを作成できません。\n"+
                        "作業を続けることができません。", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }

            //Create Temp dir
            string tempCDPath = baseDir + "\\TemporaryFiles\\DownloadedFiles\\";
            if (!Directory.Exists(tempCDPath))
            {
                try
                {
                    Directory.CreateDirectory(tempCDPath);
                }
                catch (Exception ex)
                {
					SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Creating and setting Downloaded files path Failed" +
						 "\nError: " + ex.Message);

                    MessageBox.Show("ダウンロードするためのフォルダを作成できません。\n" +
                        "作業を続けることができません。", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }

			//Store the path if we made it here.
			this._localAppDataPath = baseDir;
        }

        /// <summary>
        /// Catch all exceptions safety net. NOT good practice but necessary for now...
        /// </summary>
        private void Current_DispatcherUnhandledException(
            object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            return;

			SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Unhandled Application Wide Exception Occured" +
						 "\nError: " + e.Exception.Message);

            MessageBox.Show("原因が不明な問題が発生しました。\nメッセージは:\n" + e.Exception.Message
                + "\n\neJournalPlusを閉じます。");
            e.Handled = true;

			//Create and store Minidump.
			string dumpFile = DateTimeStringBuilder.GetDateTimeString() + "-" + "crashDump.dmp";
			ejpMinidumper.MiniDumpToFile(dumpFile);
            
            if (this._currentWorkingAssingment != null)
            {
                //close the app.
                try
                {
                    this.CloseCurrentAssignment();
                }
                catch (Exception ex)
                {
					SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Due to an unhandled application wide exception I have tried to close the current assignement. This operation failed." +
						 "\nError: " + ex.Message);

                    MessageBox.Show("保存に失敗した場合は、\n" + this._autoSaveFilePath + "\nを開いてみて下さい。");
                }
                finally
                {
                    Application.Current.Shutdown();
                }
            }
            else
                try
                {
                    Application.Current.Shutdown();
                }
                catch (Exception) { }
        }

        #region AutoSave

        private void AutoSaveCurrentAssignment(bool GenerateNewFile)
        {
			SiliconStudio.DebugManagers.DebugReporter.ReportMethodEnter();
            string prevAutoSavePath = this._autoSaveFilePath;
            bool prevState = false;
            try
            {
                if (this._currentWorkingAssingment != null && this._canAutoSave != false)
                {
                    prevState = this._currentWorkingAssingment.IsPersisted;

                    foreach (KnowledgeMap km in this._openknowledgeMaps)
                        km.ExportMapObject();

                    foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                        xpsdv.ExportDocumentExtras();

                    foreach (ReportEditor repe in this._openReports)
                        repe.ExportReportComments();

                    this.SetApplicationWidePaths();

                    this._currentWorkingAssingment.IsPersisted = false;
                    this._currentWorkingAssingment.Export(this._autoSaveFilePath);
                    this._currentWorkingAssingment.IsPersisted = prevState;
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
							 SiliconStudio.DebugManagers.MessageType.Error,
							 "eJournalPlus Client - Auto Saving",
							 "Unhandled Exception" +
							 "\nError: " + ex.Message);

                MessageBox.Show("このアサインメントのファイルに\n問題を発見したため、オートセーブが出来ません。\n"
                    + "改めて別の名前でファイルを保存して下さい。");
                this._canAutoSave = false;
            }
            finally
            {
                if(this._currentWorkingAssingment != null)
                    this._currentWorkingAssingment.IsPersisted = prevState;
            }

            //Delete the previous autosave file.
            try
            {
                if (this._currentWorkingAssingment != null && this._canAutoSave != false)
                {
                    File.Delete(prevAutoSavePath);
                }
            }
            catch (Exception) { }

			SiliconStudio.DebugManagers.DebugReporter.ReportMethodLeave();
        }

        private void _autoSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ao_AutoSave(this.AutoSaveCurrentAssignment), false);
        }

        #endregion

        #region Hooked up dummy handlers
        private void HandleKMDelete(object sender, ExecutedRoutedEventArgs e) { }

        private void HandleKMCopyCommandExecuted(object sender, ExecutedRoutedEventArgs e) { }

        private void HandleKMPasteCommandExecuted(object sender, ExecutedRoutedEventArgs e) { }

        #endregion

        #region Handle Undo / Redo

        private void HandleKMDMUndo(object sender, ExecutedRoutedEventArgs e)
        {
            if (this._previousToolBoxState == ToolBoxState.KnowledgeMap)
                this._currentKnowledgeMapControl.PropagateUndo();
            else if (this._previousToolBoxState == ToolBoxState.Document)
                this._currentXpsDocumentViewer.PropagateUndo();
        }

        private void HandleKMDMRedo(object sender, ExecutedRoutedEventArgs e)
        {
            if (this._previousToolBoxState == ToolBoxState.KnowledgeMap)
                this._currentKnowledgeMapControl.PropagateRedo();
            else if (this._previousToolBoxState == ToolBoxState.Document)
                this._currentXpsDocumentViewer.PropagateRedo();
        }

        #endregion

        #region Process loaded event handler
        /// <summary>
        /// Once the window is rendered, we invoke and act on any 
        /// command line args that might have been passed to the 
        /// application.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (App._hasDefaultDocumentOnLoad)
            {
                if (App._requestedOpenFromEjsOnLoad)
                {
                    this._M_meet_OnOpenEjsAssignment(null, null);
                }
                else if (File.Exists(App._defaultDocumentPath))
                {
                    if (App._requestedDocumentIsCA == false)
                    {
                        this._M_meet_OnOpenLocalAssignment(null, null);
                        App._defaultDocumentPath = "";
                    }
                    else if (App._requestedDocumentIsCA == true)
                    {
                        this._M_meet_OnOpenLocalCommentedAssignmentEX(null, null);
                        App._defaultDocumentPath = "";
                    }
                }
            }
            else if (App._requestedNewAssignmentOnLoad)
            {
                try
                {
                    this._M_meet_OnStartNewAssignment(null, null);
                }
                catch (Exception ex)
                {
					SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Attempting to start new assignment for new application instance failed" +
						 "\nError: " + ex.Message);

                    MessageBox.Show("失敗しました.\n" + ex.Message);
                    this.SetApplicationState(ApplicationState.Cold);
                }
            }
            else if (App._requestedNewEmptyAssignmentOnLoad)
            {
                try
                {
                    this.CreateNewEmptyAssignment(App._currentEjpStudent);
                    this.SetApplicationState(ApplicationState.AssignmentLoaded);
                }
                catch (Exception ex)
                {
					SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Attempting to start new empty assignment for new application instance failed" +
						 "\nError: " + ex.Message);

                    MessageBox.Show("失敗しました.\n" + ex.Message);
                    this.SetApplicationState(ApplicationState.Cold);
                }
            }
            else
            {
                ejpWindows.SplashScreen spl = new ejpClient.ejpWindows.SplashScreen(3, false);
                spl.ShowDialog();
            }
        }
        #endregion 

        #region User Control Specific Eventhandlers

        private void _gS_MainDivider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
            {
                if (xpsdv.IsScaleLockActive == true)
                    xpsdv.UpdateCurrentlyDisplayedPageScale();
            }

            foreach (KnowledgeMap km in this._openknowledgeMaps)
                km.UpdateScale();
        }

        private void _tb_XpsDocumentsAndReports_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this._currentWorkingAssingment != null)
                this.SetToolBoxState(ToolBoxState.Document);
        }

        private void _tb_KnowledgeMaps_GotFocus(object sender, RoutedEventArgs e)
        {
            if (this._currentWorkingAssingment != null)
                this.SetToolBoxState(ToolBoxState.KnowledgeMap);
        }

        private void _M_MarkerLineButtonUncked()
        {
            this._M_PL_ColorSwatchButton.Press();
        }

        private void _M_PenLineButtonUncked()
        {
            this._M_ML_ColorSwatchButton.Press();
        }

        private void _M_pl_ColorSwatchButton_ActiveColorChanged(SolidColorBrush newColor)
        {
            this._mtb_Eraser.IsChecked = false;
            this._M_ML_ColorSwatchButton.Depress();
            
            if (this._currentAppState == ApplicationState.ComAssignmentLoaded)
                return;

            foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
            {
                dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
                dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.PenLine;
                if (this._currentXpsDocumentViewer != null)
                {
                    if (dv != this._currentXpsDocumentViewer)
                        dv.PropagatePenColorChange(newColor, false);
                }
            }
            if (this._currentXpsDocumentViewer != null)
            {
                this._currentXpsDocumentViewer.PropagatePenColorChange(newColor, true);
            }
        }

        private void _M_ml_ColorSwatchButton_ActiveColorChanged(SolidColorBrush newColor)
        {
            this._mtb_Eraser.IsChecked = false;
            this._M_PL_ColorSwatchButton.Depress();

            if (this._currentAppState == ApplicationState.ComAssignmentLoaded)
                return;

            foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
            {
                dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
                dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.MarkerLine;
                if (this._currentXpsDocumentViewer != null)
                {
                    if (dv != this._currentXpsDocumentViewer)
                        dv.PropagateMarkerColorChange(newColor, false);
                }
            }
			if (this._currentXpsDocumentViewer != null)
			{
				this._currentXpsDocumentViewer.PropagateMarkerColorChange(newColor, true);
			}
        }

        private void _M_km_ColorSwatchButton_ActiveColorChanged(SolidColorBrush newColor)
        {
            //We need to call this method twice since we only want to update
            //selected entities on the currently select km.
            //For the rest we only want to set the drawing attributes.
            foreach (KnowledgeMap km in this._openknowledgeMaps)
                km.PropagateColorSelectionEvent(newColor, false);

            if(this._currentKnowledgeMapControl != null)
                this._currentKnowledgeMapControl.PropagateColorSelectionEvent(newColor, true);
        }

        #endregion

        private void OnApplicationClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"eJournalPlus Client", "Appliction Shutdown Started.");

            this.SaveApplicationSettings(this._localAppSettingsaPath + "tempSettings.bej");

			//If the application state is cold there should be no need to 
			//close the current assignment...
			if (this._currentAppState != ApplicationState.Cold)
			{
				if (this._currentWorkingAssingment != null)
				{
					if (this.CloseCurrentAssignment() == false)
					{
						e.Cancel = true;
					}
				}
			}

            if (e.Cancel != true)
            {
                try
                {
                    if (App.IsCurrentUserEJSAuthenticated())
                    {
                        EjsBridge.ejsBridgeManager.LogOutUser(App._currentEjpStudent.SessionToken);
                    }
                }
                catch (ApplicationException ex)
                {
					SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Attempting to log out the current user from EJS, while closing application, failed" +
						 "\nError: " + ex.Message);
                }
            }

            //Clear the temporaryFiles Folder
            this.ClearTempFiles();

			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"eJournalPlus Client", "Appliction Closing.");
        }

        /// <summary>
        /// Clears the directory that contains the Auto saved files, BUT
        /// keeps the latest auto save file just in case...
        /// </summary>
		private void ClearTempFiles()
		{
			try
			{
				string asDir = this._localAppDataPath + @"\AutoSave\";
				DirectoryInfo asDi = new DirectoryInfo(asDir);

				string dlDir = this._localAppDataPath + @"\TemporaryFiles\DownloadedFiles\";
				DirectoryInfo dlDi = new DirectoryInfo(dlDir);

				string tmpDir = this._localAppDataPath + @"\TemporaryFiles\";
				DirectoryInfo tmpDi = new DirectoryInfo(tmpDir);

				//clear the auto saves
				FileInfo asfi = new FileInfo(this._autoSaveFilePath);
				string autoSaveName = asfi.Name;
				foreach (FileInfo fi in asDi.GetFiles())
				{
					if (fi.Name == autoSaveName)
						continue;

					try
					{
						fi.Delete();
					}
					catch (Exception ex)
					{
						SiliconStudio.DebugManagers.DebugReporter.Report(
							SiliconStudio.DebugManagers.MessageType.Error,
							"eJournalPlus Client",
							"Exception was thrown while deleting the file " + fi.Name + ":\n" + ex.Message);
					}
				}

				//clear the downloaded files
				foreach (FileInfo fi in dlDi.GetFiles())
				{
					if (fi.Name == autoSaveName)
						continue;

					try
					{
						fi.Delete();
					}
					catch (Exception ex)
					{
						SiliconStudio.DebugManagers.DebugReporter.Report(
							SiliconStudio.DebugManagers.MessageType.Error,
							"eJournalPlus Client",
							"Exception was thrown while deleting the file " + fi.Name + ":\n" + ex.Message);
					}
				}

				//clear the temp files
				foreach (FileInfo fi in tmpDi.GetFiles())
				{
					if (fi.Name == autoSaveName)
						continue;

					try
					{
						fi.Delete();
					}
					catch (Exception ex)
					{
						SiliconStudio.DebugManagers.DebugReporter.Report(
							SiliconStudio.DebugManagers.MessageType.Error,
							"eJournalPlus Client",
							"Exception was thrown while deleting the file " + fi.Name + ":\n" + ex.Message);
					}
				}
			}
			catch (Exception ex)
			{
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"eJournalPlus Client",
					"Exception was thrown while deleting temporary files:\n" + ex.Message);
			}
		}

        #region Commented Assignment Related Methods

        /// <summary>
        /// Open a local Commented Assignment.
        /// Updated on 080714
        /// </summary>
        private void _M_meet_OnOpenLocalCommentedAssignmentEX(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            bool clearAssignmentOnFail = false;
            
            try
            {
                string path = "";
                if (App._defaultDocumentPath == "" ||
                    App._defaultDocumentPath == null)
                {
                    path = this.GetOpenFileName("ejpファイル (*.ejp;*.cejp)|*.ejp;*.cejp|コメント付きejpファイル (*.cejp)|*.cejp",
                        "コメントモードで開くファイルを選択");
                }
                else
                {
                    path = App._defaultDocumentPath;
                }

                if (path != "cancel")
                {
                    if (this._currentWorkingAssingment == null)
                    {
                        clearAssignmentOnFail = true;

                        this.OpenLocalAssignment(path);

                        //if this is not already a CA, update the necessary info to make it one.
                        if (this._currentWorkingAssingment.MetaData.AssignmentContentType !=
                            SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment)
                        {
                            this._currentWorkingAssingment.MetaData.AssignmentContentType =
                                SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment;
                            this._currentWorkingAssingment.MetaData.OwnerUserId = Guid.Empty;
                            this._currentWorkingAssingment.MetaData.Id = IdManipulation.GetNewGuid();
                            this._currentWorkingAssingment.IsPersisted = false;
                            this._currentWorkingAssingment.EjsDatabaseId = -1;
                            this._currentWorkingAssingment.MetaData.CreationDate = DateTime.Now;
                            this._currentWorkingAssingment.MetaData.EJSDatabaseId = -1;
                            this._currentWorkingAssingment.MetaData.IsManagedByEJournalServer = false;
                            this._currentWorkingAssingment.MetaData.Revision = 0;
                            this._currentWorkingAssingment.MetaData.Title = this._currentWorkingAssingment.MetaData.Title + " - Commented";
                            this._currentWorkingAssingment.MetaData.Version = 1;
                            this._menuI_Publish.IsEnabled = false;
                        }

                        List<TabItem> delTI = new List<TabItem>();
                        for (int i = 0; i < this._tb_KnowledgeMaps.Items.Count; i++)
                        {
                            if(((TabItem)this._tb_KnowledgeMaps.Items[i]).Name == "newKMTab")
                                delTI.Add((TabItem)this._tb_KnowledgeMaps.Items[i]);
                        }
                        foreach (TabItem ti in delTI)
                            this._tb_KnowledgeMaps.Items.Remove(ti);

                        List<TabItem> delTIR = new List<TabItem>();
                        for (int i = 0; i < this._tb_XpsDocumentsAndReports.Items.Count; i++)
                        {
                            if(((TabItem)this._tb_XpsDocumentsAndReports.Items[i]).Name == "newReportTab")
                                delTIR.Add((TabItem)this._tb_XpsDocumentsAndReports.Items[i]);
                        }
                        foreach (TabItem ti in delTIR)
                            this._tb_XpsDocumentsAndReports.Items.Remove(ti);

                        this.SetApplicationState(ApplicationState.ComAssignmentLoaded);
                        this.AutoSaveCurrentAssignment(true);
                    }
                    else
                    {
                        this.SaveApplicationSettings(this._localAppSettingsaPath + "tempSettings.bej");
                        string appFileName = Process.GetCurrentProcess().MainModule.FileName;
                        Process.Start(appFileName, " /m:CA /l:" + path + "\"");
                    }
                }
            }
            catch (Exception ex)
            {
                SiliconStudio.DebugManagers.DebugReporter.Report(
                         SiliconStudio.DebugManagers.MessageType.Error,
                         "eJournalPlus Client",
                         "Failed to open local commented assignment" +
                         "\nError: " + ex.Message);

                MessageBox.Show("失敗しました.\n" + ex.Message);
                this.ClearUi();

                //make sure the current working assignment is unloaded. 080603
                if (this._currentWorkingAssingment != null && clearAssignmentOnFail == true)
                    this._currentWorkingAssingment.Close(false);

                this.SetApplicationState(ApplicationState.Cold);
            }
            finally
            {
                //Need to set this to handled to prevent
                //menu group default action from being invoked.
                if (e != null)
                    e.Handled = true;
            }
        }

        private void _M_meet_OnSwitchToNormalAssignmentMode(object sender, RoutedEventArgs e)
        {
            if (this.ReqConsent_SwitchToNormalAMode())
            {
                this.SetApplicationState(ApplicationState.AssignmentLoaded);
                this._currentWorkingAssingment.MetaData.AssignmentContentType = 
                    SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.WorkingAssignment;
                this.ShowMainToolBox();
            }
        }

        #endregion

        #region Inital Setup and Settings

        /// <summary>
        /// Loads the specified settings file.
        /// </summary>
        /// <param name="path">Path to the settings file to read.</param>
        private void LoadApplicationSettings(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (FileStream settingsFileStream = new FileStream(path, FileMode.Open))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        App._ejpSettings = (ejpClient.EJPSettings)bf.Deserialize(settingsFileStream);
                        if (App._ejpSettings.IsEjsConfigured)
                        {
                            EjsBridge.ejsBridgeManager.EjsAddress = App._ejpSettings.EjsAddress;
                            this._menuI_OpenAssignmentOnline.IsEnabled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to load application settings" +
						 "\nPath: " + path +
						 "\nError: " + ex.Message);

                MessageBox.Show("設定ファイルがありません。\nデフォルト設定で実行します。");
            }
        }

        private void SaveApplicationSettings(string path)
        {
            try
            {
                using (FileStream settingsFileStream = new FileStream(path, FileMode.Create))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(settingsFileStream, App._ejpSettings);
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to save application settings" +
						 "\nPath: " + path +
						 "\nError: " + ex.Message);

                //MessageBox.Show("設定ファイルを保存出来ませんでした。");
            }
        }

        private void LoadColorSwatches()
        {
            this._kmAreaColorsList = new Dictionary<string, SolidColorBrush>
            {
                {"赤", new SolidColorBrush(Color.FromArgb(255,255,0,0))},
                {"黒", new SolidColorBrush(Color.FromArgb(255,0,0,0))},
                {"黄", new SolidColorBrush(Color.FromArgb(255,255,255,0))},
                {"緑", new SolidColorBrush(Color.FromArgb(255,0,255,0))},
                {"青", new SolidColorBrush(Color.FromArgb(255,0,0,255))},
            };

            this._documentAreaPenColorsList = new Dictionary<string, SolidColorBrush>
            {
                {"赤", new SolidColorBrush(Color.FromArgb(255,255,0,0))},
                {"黄", new SolidColorBrush(Color.FromArgb(255,255,255,0))},
                {"緑", new SolidColorBrush(Color.FromArgb(255,0,255,0))},
                {"青", new SolidColorBrush(Color.FromArgb(255,0,0,255))},
            };

            this._documentAreaMarkerColorsList = new Dictionary<string, SolidColorBrush>
            {
                {"赤", new SolidColorBrush(Color.FromArgb(100,255,0,0))},
                {"黄", new SolidColorBrush(Color.FromArgb(100,255,255,0))},
                {"緑", new SolidColorBrush(Color.FromArgb(100,0,255,0))},
                {"青", new SolidColorBrush(Color.FromArgb(100,0,0,255))},
            };

            this._M_km_ColorSwatchButton.Items = this._kmAreaColorsList;
            this._M_ML_ColorSwatchButton.Items = this._documentAreaMarkerColorsList;
            this._M_PL_ColorSwatchButton.Items = this._documentAreaPenColorsList;

            this._M_km_ColorSwatchButton.SetCurrentColor(this._kmAreaColorsList["赤"]);
            this._M_ML_ColorSwatchButton.SetCurrentColor(this._documentAreaMarkerColorsList["赤"]);
            this._M_PL_ColorSwatchButton.SetCurrentColor(this._documentAreaPenColorsList["赤"]);
        }

        /// <summary>
        /// Display the settings window where the user can update application and user settings.
        /// </summary>
        private void _M_meet_OnOpenSettingsWindow(object sender, RoutedEventArgs e)
        {
            ejpClient.ejpWindows.AppSettingsWindow appSetWin = new ejpClient.ejpWindows.AppSettingsWindow();
            appSetWin.Closing += new CancelEventHandler(appSetWin_Closing);
            appSetWin.ShowDialog();
        }

        /// <summary>
        /// Make sure the new settings take effect.
        /// </summary>
        private void appSetWin_Closing(object sender, CancelEventArgs e)
        {
            this.SetApplicationState(this._currentAppState);
        }

        /// <summary>
        /// Display the About Dialog.
        /// </summary>
        private void _M_meet_OnShowAboutSplash(object sender, RoutedEventArgs e)
        {
            ejpWindows.SplashScreen spl = new ejpClient.ejpWindows.SplashScreen(3, true);
            spl.ShowDialog();
        }

        #endregion

        #region Menu Callbacks

        /// <summary>
        /// Handles the ContextMenu for the study list itself.
        /// </summary>
        private void OnStudyListContextMenuOpen(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = sender as ContextMenu;
            if(this._currentWorkingAssingment == null)
                ((MenuItem)cm.Items[0]).IsEnabled = false;
            else
                ((MenuItem)cm.Items[0]).IsEnabled = true;
        }

        /// <summary>
        /// Handles the ContextMenu for each study item.
        /// </summary>
        private void OnStudyItemContextMenuOpen(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = sender as ContextMenu;

            if (this._currentWorkingAssingment.Studies.Count < 2)
                ((MenuItem)cm.Items[2]).IsEnabled = false;
            else
                ((MenuItem)cm.Items[2]).IsEnabled = true;
        }

        private void _M_meet_DefaultExportCommand(object sender, MouseButtonEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            this._M_meet_OnExportReport(null, null);
        }

        private void _M_meet_OnExportKnowledgeMap(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

			try
			{
				winForms.SaveFileDialog exportMapDialog = new winForms.SaveFileDialog();
				exportMapDialog.Filter = "Png Images (*.png)|*.png";

				if (exportMapDialog.ShowDialog() == winForms.DialogResult.OK)
				{
					this._currentKnowledgeMapControl.ExportMapToImage(exportMapDialog.FileName);
				}
			}
			catch (Exception ex)
			{
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to export Knowledge Map" +
						 "\nError: " + ex.Message);

				MessageBox.Show("ナレッジマップを書き出しする際に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}
        }

        private void _M_meet_OnExportReport(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                bool foundReportToExport = false;
                foreach (TabItem ti in this._tb_XpsDocumentsAndReports.Items)
                {
                    if (ti.Content is ReportEditor && ti.Visibility == Visibility.Visible)
                    {
                        if (ti == this._tb_XpsDocumentsAndReports.SelectedItem)
                        {
                            foundReportToExport = true;
                            winForms.SaveFileDialog exportReportDialog = new winForms.SaveFileDialog();
                            exportReportDialog.Filter = "RichTextFiles (*.rtf)|*.rtf";

                            if (exportReportDialog.ShowDialog() == winForms.DialogResult.OK)
                            {
                                ((ReportEditor)ti.Content).ExportReportToRtf(exportReportDialog.FileName);
                                //foundReportToExport = true;
                            }
                        }
                    }
                }
                if (!foundReportToExport)
                    MessageBox.Show("書き出したいレポートを選択して下さい。", "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) 
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to export Report" +
						 "\nError: " + ex.Message);

                MessageBox.Show("レポートを書き出しする際に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _M_meet_OnStartNewAssignment(object sender, RoutedEventArgs e)
		{
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

			SiliconStudio.DebugManagers.DebugReporter.ReportMethodEnter();

            try
            {
                if (this._currentWorkingAssingment == null)
                {
                    EjsBridge.ejsBridgeManager.EjsAddress = App._ejpSettings.EjsAddress;
                    ejpWindows.NewAssignmentWindow newAW = new ejpWindows.NewAssignmentWindow();
                    newAW.WindowHeadline = "新規作成";
                    newAW.Closed += delegate(object ws, EventArgs we)
                    {
                        if (newAW.CreateLocation ==
                            ejpClient.ejpWindows.CreateAssignmentStartLocation.Local)
                        {
                            FileInfo fi = new FileInfo(newAW.FirstDocumentLocalPath);
                            this.CreateNewAssignment(newAW.FirstDocumentLocalPath, fi.Name, App._currentEjpStudent,
                                false, IdManipulation.GetNewGuid());
                            this.SetApplicationState(ApplicationState.AssignmentLoaded);
                            this.SetToolBoxState(ToolBoxState.Document);
                            this._M_PL_ColorSwatchButton.Press();
                            this.AutoSaveCurrentAssignment(true);
                        }
                        else if (newAW.CreateLocation ==
                            ejpClient.ejpWindows.CreateAssignmentStartLocation.None)
                        {
                            this.CreateNewEmptyAssignment(App._currentEjpStudent);
                            this.SetApplicationState(ApplicationState.AssignmentLoaded);
                            this.AutoSaveCurrentAssignment(true);
                        }
                        else if (newAW.CreateLocation ==
                            ejpClient.ejpWindows.CreateAssignmentStartLocation.EJournalServer)
                        {
                            this.CreateNewAssignment(newAW.FirstDocumentLocalPath, newAW.EjsDocumentToDownload._name,
                                App._currentEjpStudent, false, IdManipulation.GetNewGuid());
                            this.SetApplicationState(ApplicationState.AssignmentLoaded);
                            this.SetToolBoxState(ToolBoxState.Document);
                            this._M_PL_ColorSwatchButton.Press();
                            this.AutoSaveCurrentAssignment(true);
                        }
                    };
                    newAW.ShowDialog();
                }
                else
                {
                    string appFileName = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appFileName, @"/n:normal");
                }

				SiliconStudio.DebugManagers.DebugReporter.ReportMethodLeave();
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to Start new Assignment" +
						 "\nError: " + ex.Message);

                MessageBox.Show("失敗しました.\n" + ex.Message);
                this.SetApplicationState(ApplicationState.Cold);
            }
		}

		private void _M_meet_OnCloseAssignment(object sender, RoutedEventArgs e)
		{
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

			SiliconStudio.DebugManagers.DebugReporter.ReportMethodEnter();

            if (this.CloseCurrentAssignment() == true)
            {
                BindingOperations.ClearBinding(this._LB_StudyList, ListBox.ItemsSourceProperty);
                this.ClearUi();
                this.SetApplicationState(ApplicationState.Cold);
                this._canAutoSave = true;
            }

			SiliconStudio.DebugManagers.DebugReporter.ReportMethodLeave();
		}

        private void _M_meet_OnExportAssignment(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                winForms.SaveFileDialog saveAssignmentDialog = new winForms.SaveFileDialog();
                saveAssignmentDialog.Filter = "Assignments (*.ejp)|*.ejp";

                if (saveAssignmentDialog.ShowDialog() == winForms.DialogResult.OK)
                {
                    foreach (KnowledgeMap km in this._openknowledgeMaps)
                        km.ExportMapObject();

                    foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                        xpsdv.ExportDocumentExtras();

                    foreach (ReportEditor repe in this._openReports)
                        repe.ExportReportComments();

                    bool prevState = this._currentWorkingAssingment.IsPersisted;
                    this._currentWorkingAssingment.IsPersisted = false;
                    this._currentWorkingAssingment.Export(saveAssignmentDialog.FileName);
                    this._currentWorkingAssingment.IsPersisted = prevState;
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to export Assignment" +
						 "\nError: " + ex.Message);

                MessageBox.Show("アサインメントを書き出しする際に失敗しました。", "書き出し失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _M_meet_OnImportAssignmentFromEjs(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                if (this._currentWorkingAssingment == null)
                    return;

                if(this._currentWorkingAssingment.IsPersisted == false)
                {
                    MessageBox.Show("別のアサインメントをマージする前に，アサインメントを保存してください。",
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ejpWindows.EjsOpenAssignmentWindowEX ejsMAw =
                    new ejpClient.ejpWindows.EjsOpenAssignmentWindowEX(true);

                ejpWindows.LoadingMessageWindow lmw =
                    new ejpClient.ejpWindows.LoadingMessageWindow();

                ejsMAw.Closed += delegate(object s2, EventArgs e2)
                {
                    if (ejsMAw.Cancelled == false && ejsMAw.OpenForMerge == true)
                    {
                        string tempPath = this._localAppDataPath +
                        "\\TemporaryFiles\\DownloadedFiles\\" +
                        "TempEjpPackage" + DateTime.Now.Ticks.ToString() + ".ejp";

                        BackgroundWorker bgw = new BackgroundWorker();
                        bgw.DoWork += delegate(object s3, DoWorkEventArgs doWorkArgs)
                        {
                            EjsBridge.ejsBridgeManager.DownloadAssignment(
                                App._currentEjpStudent.SessionToken, tempPath,
                                ejsMAw.AssignmentToOpen);
                        };

                        bgw.RunWorkerCompleted += delegate(object s4, RunWorkerCompletedEventArgs workerCompletedArgs)
                        {
                            lmw.Close();
                            this._l_AppStatusLabel.Text = "Idle";
                            bgw.Dispose();
                        };

                        bgw.RunWorkerAsync();
                        this._l_AppStatusLabel.Text = "アサインメントをダウンロード中。";
                        lmw.ShowDialog();

                        this.ImportAssignment(tempPath);

                        if (this._currentWorkingAssingment.IsPersisted)
                            this._M_meet_OnSaveAssignment(null, null);

                        this.AutoSaveCurrentAssignment(true);
                    }
                };
                ejsMAw.ShowDialog();

            }
            catch (Exception ex)
            {
                SiliconStudio.DebugManagers.DebugReporter.Report(
                         SiliconStudio.DebugManagers.MessageType.Error,
                         "eJournalPlus Client",
                         "Failed to merge Ejs Assignment" +
                         "\nError: " + ex.Message);

                MessageBox.Show("失敗しました。\n" + ex.Message);
                this.SetApplicationState(ApplicationState.Cold);
            }
        }

        private void _M_meet_OnImportAssignment(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                string path = this.GetOpenFileName("Assignments (*.ejp)|*.ejp", "ejpファイルを選択");
                if (path != "cancel")
                {
                    this.ImportAssignment(path);

                    if (this._currentWorkingAssingment.IsPersisted)
                        this._M_meet_OnSaveAssignment(null, null);

                    this.AutoSaveCurrentAssignment(true);
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to import Assignment" +
						 "\nError: " + ex.Message);

                MessageBox.Show("失敗しました。\n" + ex.Message);
                this.SetApplicationState(ApplicationState.AssignmentLoaded);
            }
        }

        private void _M_meet_OnOpenEjsAssignment(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if(e != null)
                e.Handled = true;

            try
            {
                if (this._currentWorkingAssingment == null)
                {
                    ejpWindows.EjsOpenAssignmentWindowEX ejsOAw =
                        new ejpClient.ejpWindows.EjsOpenAssignmentWindowEX(false);

                    ejpWindows.LoadingMessageWindow lmw = 
                        new ejpClient.ejpWindows.LoadingMessageWindow();

                    ejsOAw.Closed += delegate(object s2, EventArgs e2)
                        {
                            if (ejsOAw.Cancelled == false)
                            {
								string tempPath = this._localAppDataPath +
                                "\\TemporaryFiles\\DownloadedFiles\\" +
                                "TempEjpPackage" + DateTime.Now.Ticks.ToString() + ".ejp";
                                //string tempPath = @"c:\Windows\Temp\TempEjpPackage" + DateTime.Now.Ticks.ToString() + ".ejp";

                                BackgroundWorker bgw = new BackgroundWorker();
                                bgw.DoWork += delegate(object s3, DoWorkEventArgs doWorkArgs)
                                {
                                    EjsBridge.ejsBridgeManager.DownloadAssignment(
                                        App._currentEjpStudent.SessionToken, tempPath,
                                        ejsOAw.AssignmentToOpen);
                                };

                                bgw.RunWorkerCompleted += delegate(object s4, RunWorkerCompletedEventArgs workerCompletedArgs)
                                {
                                    lmw.Close();
                                    this._l_AppStatusLabel.Text = "Idle";
                                    bgw.Dispose();
                                };

                                bgw.RunWorkerAsync();
                                this._l_AppStatusLabel.Text = "アサインメントをダウンロード中。";
                                lmw.ShowDialog();

                                //While not good practice, the order of these
                                //operations matter...
                                this.OpenLocalAssignment(tempPath);
                                this._currentWorkingAssingment.IsPersisted = false;

                                if (this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                                    SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment
                                    || ejsOAw.OpenSelectedAssignmentAsCommented == true)
                                {
                                    this.SetApplicationState(ApplicationState.ComAssignmentLoaded);
                                    this._currentWorkingAssingment.MetaData.AssignmentContentType =
                                        SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment;
                                }
                                else
                                    this.SetApplicationState(ApplicationState.AssignmentLoaded);

                                
                                this.UpdateWindowTitle();
                                this.SetToolBoxState(ToolBoxState.Document);
                                this._M_PL_ColorSwatchButton.Press();
                                this.AutoSaveCurrentAssignment(true);
                                
                            }
                        };
                    ejsOAw.ShowDialog();
                }
                else
                {
                    this.SaveApplicationSettings("tempSettings.bej");
                    string appFileName = Process.GetCurrentProcess().MainModule.FileName;
                    Process.Start(appFileName, @"/l:ejs");
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to open Ejs Assignment" +
						 "\nError: " + ex.Message);

                MessageBox.Show("失敗しました。\n" + ex.Message);
                this.SetApplicationState(ApplicationState.Cold);
            }
        }

        private void _M_meet_DefaultOpenCommand(object sender, MouseButtonEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            this._M_meet_OnOpenLocalAssignment(null, null);
        }

        /// <summary>
        /// Open a Local Assignment.
        /// </summary>
        private void _M_meet_OnOpenLocalAssignment(object sender, RoutedEventArgs e)
		{
			bool clearAssignmentOnFail = false;
            try
            {
                string path = "";
                if (App._defaultDocumentPath == "" ||
                    App._defaultDocumentPath == null)
                {
                    path = this.GetOpenFileName("ejpファイル (*.ejp;*.cejp)|*.ejp;*.cejp|コメント付きejpファイル (*.cejp)|*.cejp", 
                        "ふつうモードで開くファイルを選択");
                }
                else
                {
                    path = App._defaultDocumentPath;
                }
                if (path != "cancel")
                {
                    if (this._currentWorkingAssingment == null)
                    {
						clearAssignmentOnFail = true;

                        this.OpenLocalAssignment(path);

                        if (this._M_ML_ColorSwatchButton.IsActive)
                        {
                            foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
                            {
                                dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
                                dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.MarkerLine;
                            }
                        }
                        else if (this._M_PL_ColorSwatchButton.IsActive)
                        {
                            foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
                            {
                                dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
                                dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.PenLine;
                            }
                        }

                        if (
                             this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                             SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment)
                        {
                            /*here we need to convert the CA to a normal assignment */

                            this._currentWorkingAssingment.MetaData.AssignmentContentType =
                                 SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.WorkingAssignment;
                            this._currentWorkingAssingment.MetaData.OwnerUserId = App._currentEjpStudent.Id;
                            this._currentWorkingAssingment.MetaData.Id = IdManipulation.GetNewGuid();
                            this._currentWorkingAssingment.IsPersisted = false;
                            this._currentWorkingAssingment.EjsDatabaseId = -1;
                            this._currentWorkingAssingment.MetaData.CreationDate = DateTime.Now;
                            this._currentWorkingAssingment.MetaData.EJSDatabaseId = -1;
                            this._currentWorkingAssingment.MetaData.IsManagedByEJournalServer = false;
                            this._currentWorkingAssingment.MetaData.Revision = 0;
                            this._currentWorkingAssingment.MetaData.Title = this._currentWorkingAssingment.MetaData.Title + " - Comments (Converted)";
                            this._currentWorkingAssingment.MetaData.Version = 1;

                            //Remove the comments from all the reports
                            foreach (ReportEditor repEditor in this._openReports)
                                repEditor.IsEditingLocked = false;

                            //Remove the comments from all the KMs //CO on 080714
                            foreach (KnowledgeMap km in this._openknowledgeMaps)
                                km.SetNormalMode();

                        }

                        this.SetApplicationState(ApplicationState.AssignmentLoaded);
                        this.SetToolBoxState(ToolBoxState.Document);
                        this._currentXpsDocumentViewer.Focus();
                        this._M_PL_ColorSwatchButton.Press();

                        this.AutoSaveCurrentAssignment(true);
                    }
                    else
                    {
                        this.SaveApplicationSettings(this._localAppSettingsaPath + "tempSettings.bej");
                        string appFileName = Process.GetCurrentProcess().MainModule.FileName;
                        Process.Start(appFileName, "\"/l:" + path + "\"");
                    }
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to open local assignment" +
						 "\nError: " + ex.Message);

                MessageBox.Show("失敗しました。\n" + ex.Message);
                this.ClearUi();
	
				//make sure the current working assignment is unloaded. 080603
				if(this._currentWorkingAssingment != null && clearAssignmentOnFail == true)
					this._currentWorkingAssingment.Close(false);

                this.SetApplicationState(ApplicationState.Cold);
            }
            finally
            {
                //Need to set this to handled to prevent
                //menu group default action from being invoked.
                if(e != null)
                    e.Handled = true;
            }
		}

        private void _M_meet_DefaultPublishCommand(object sender, MouseButtonEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            this._M_meet_OnPublishToWLS(null, null);
        }

        private void _M_meet_OnPublishToEjs(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if(e != null)
                e.Handled = true;

            try
            {
                /* 080723
                 * Once we have decided how to deal with local assignments
                 * that have no parent and such, this part will be
                 * necessary. For now it is commented out...
                 */
                //if (App.IsCurrentUserEJSAuthenticated() == false)
                //{
                //    EjsLoginWindow loginWindow = new EjsLoginWindow();
                //    loginWindow.ShowDialog();
                //}
                //if (App.IsCurrentUserEJSAuthenticated() == false)
                //{
                //    return;
                //}

                if (this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                    SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.WorkingAssignment)
                {
                    ejpWindows.PublishAssignmentWindow pubaw =
                        new ejpClient.ejpWindows.PublishAssignmentWindow();
                    pubaw.Closed += delegate(object ws, EventArgs we)
                    {
                        if (pubaw.Cancelled == true)
                            return;
                        this.DoPublishToEJS(pubaw.DocumentLocalPath, pubaw.RegisterToCourseId);
                    };
                    pubaw.ShowDialog();
                }
                else
                {
                    this.DoPublishToEJS("Commented Assignment", -1); //-1 = commented assignments do not belong to courses
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to publish assignment to Ejs" +
						 "\nError: " + ex.Message);

                MessageBox.Show("アサインメントを公開する際に失敗しました。", "保存の失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Do the actual publishing to EJS. Never call this method directly.
        /// </summary>
        /// <param name="title">Title to be set for the new assignment.</param>
        /// <param name="coursId">Course Id to register to. -1 for Commented Assignments.</param>
        private void DoPublishToEJS(string title, int coursId)
        {
            foreach (KnowledgeMap km in this._openknowledgeMaps)
                km.ExportMapObject();

            foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                xpsdv.ExportDocumentExtras();

            foreach (ReportEditor repe in this._openReports)
                repe.ExportReportComments();

            bool prevState = this._currentWorkingAssingment.IsPersisted;
            string prevTitle = this._currentWorkingAssingment.MetaData.Title;
            bool prevManagementState = this._currentWorkingAssingment.MetaData.IsManagedByEJournalServer;
            Guid prevExternalAssignmentId = this._currentWorkingAssingment.MetaData.Id;
            this._currentWorkingAssingment.MetaData.Title = title;
            this._currentWorkingAssingment.IsPersisted = false;
            this._currentWorkingAssingment.MetaData.IsManagedByEJournalServer = true;
            this._currentWorkingAssingment.MetaData.Id = SiliconStudio.Meet.EjpLib.Helpers.IdManipulation.GetNewGuid();

			string tempPath = this._localAppDataPath +
                                "\\TemporaryFiles\\" +
                                "TempEjpPackage" + DateTime.Now.Ticks.ToString() + ".ejp";

            //string tempPath = @"c:\Windows\Temp\TempEjpPackage" + DateTime.Now.Ticks.ToString() + ".ejp";
            this._currentWorkingAssingment.Export(tempPath);

            int commentCount = this.GetCurrentAssignmentCommentCount();

            EjsBridge.ejsService.ejsAssignment dbAssignMent =
                new EjsBridge.ejsService.ejsAssignment
                {
                    AssignmentContentType = (int)this._currentWorkingAssingment.MetaData.AssignmentContentType,
                    CommentCount = commentCount,
                    CreationDate = DateTime.Now,
                    Description = "",
                    EJSDatabaseId = -1,
                    IsManagedByEJournalServer = true,
                    LastModifiedDate = DateTime.Now,
                    Title = this._currentWorkingAssingment.MetaData.Title,
                    StudyCount = this._currentWorkingAssingment.Studies.Count,
                    OwnerUserId = App._currentEjpStudent.Id,
                    Version = 1,
                    CourseId = coursId,
                    ExternalAssignmentId = this._currentWorkingAssingment.MetaData.Id
                };
            if (dbAssignMent.AssignmentContentType == 1) //1 = Commented Assignments
                dbAssignMent.ParentAssignmentId = prevExternalAssignmentId;
            else
                dbAssignMent.ParentAssignmentId = Guid.Empty;

            this._currentWorkingAssingment.IsPersisted = prevState;
            this._currentWorkingAssingment.MetaData.Title = prevTitle;
            this._currentWorkingAssingment.MetaData.IsManagedByEJournalServer = prevManagementState;
            this._currentWorkingAssingment.MetaData.Id = prevExternalAssignmentId;

            ejpWindows.LoadingMessageWindow lmw =
                new ejpClient.ejpWindows.LoadingMessageWindow();

            int Id = -1;
            BackgroundWorker bgw = new BackgroundWorker();
            bgw.DoWork += delegate(object s3, DoWorkEventArgs doWorkArgs)
            {
                Id =
                EjsBridge.ejsBridgeManager.SaveAndUploadNewAssignment(App._currentEjpStudent.SessionToken,
                   tempPath, dbAssignMent);

                this._currentWorkingAssingment.MetaData.EJSDatabaseId = Id;

                foreach (ejpStudy study in this._currentWorkingAssingment.Studies)
                {
                    EjsBridge.ejsBridgeManager.SaveAndUploadStudyMetaData(
                    App._currentEjpStudent.SessionToken,
                    new EjsBridge.ejsService.ejsStudyMetaData
                    {
                        CommentCount = 0,
                        CreationDate = DateTime.Now,
                        Description = "",
                        IsAvailable = true,
                        LastModifiedDate = DateTime.Now,
                        OwnerId = App._currentEjpStudent.Id,
                        ParentAssignmentId = Id,
                        Title = study.MetaData.Title
                    }, this._currentWorkingAssingment.MetaData.EJSDatabaseId
                    );
                }
            };

            bgw.RunWorkerCompleted += delegate(object s4, RunWorkerCompletedEventArgs workerCompletedArgs)
            {
                if(workerCompletedArgs.Error != null)
                {
                    if (workerCompletedArgs.Error.InnerException != null)
                        MessageBox.Show(
                            "公開するための処理が失敗しました。\nメッセージは：\n\n" +
                            workerCompletedArgs.Error.InnerException.Message, "エラー", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    else
                        MessageBox.Show(
                            "公開するための処理が失敗しました。", "エラー", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                }
                
                lmw.Close();
                this._l_AppStatusLabel.Text = "Idle";
                bgw.Dispose();
            };

            bgw.RunWorkerAsync();
            this._l_AppStatusLabel.Text = "アサインメント公開中。";
            lmw.ShowDialog();
            MessageBox.Show("公開が無事終わりました。", "公開完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void _M_meet_OnPublishToWLS(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                bool foundReportToPublish = false;
                foreach (TabItem ti in this._tb_XpsDocumentsAndReports.Items)
                {
                    if (ti.Content is ReportEditor && ti.Visibility == Visibility.Visible)
                    {
                        if (ti == this._tb_XpsDocumentsAndReports.SelectedItem)
                        {
                            string htmlRep = ((ReportEditor)ti.Content).ExportReportToHTMLString();
                            if(htmlRep == "")
                                return;

                            string postTitle = "eJournalPlus - レポート" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();

                            byte[] kmByteArray = this._currentKnowledgeMapControl.ExportMapToByteArray();
                            foundReportToPublish = true;

                            WlsBridge.WLSBridgeHelper wlsbh = new WLSBridgeHelper();

                            //push the KM Image
                            wlsbh.EnsureAlbumExist();

                            //make sure the user is authenticated
                            if (wlsbh.LastStatusCode == WlsBridgeStatusCode.AuthenticationError)
                                return;

                            string kmImageLocation = null;
                            string kmImageName = string.Format("KnowledgeMap_{0}", DateTime.Now.ToString("yy_MMMM_d-HH_MM_ss"));

                            kmImageLocation = wlsbh.PutPhoto(kmImageName, kmByteArray, "image/png");

                            //Wrap up the post in a complete HTML envelope and add the 
                            //image with the location we got above...
                            StringBuilder builder = new StringBuilder();
                            builder.Append("<html><head><title>");
                            builder.Append(postTitle);
                            builder.AppendLine("</title></head><body>");
                            builder.Append(htmlRep);

                            if (kmImageLocation != null)
                            {
                                builder.AppendFormat("<p><a href=\"{0}\"><img width=\"300\" border=\"1\" src=\"{1}\"/></a></p>", kmImageLocation, kmImageLocation);
                            }
                            builder.AppendLine("</body></html>");

                            wlsbh.PostBlog(postTitle, builder.ToString());

                            MessageBox.Show("公開が無事終わりました。", "公開完了", MessageBoxButton.OK, MessageBoxImage.Information);

                            break;
                        }
                    }
                }
                if (!foundReportToPublish)
                    MessageBox.Show("公開したいレポートを選択して下さい。", "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (ApplicationException ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to publish report to Windows Live Spaces" +
						 "\nError: " + ex.Message);

                MessageBox.Show("アサインメントを公開する際に失敗しました。\n" + ex.Message,
                    "失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _M_meet_DefaultPrintCommand(object sender, MouseButtonEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            this._M_meet_OnPrintReport(null, null);
        }

        private void _M_meet_OnPrintReport(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                bool foundReportToExport = false;
                foreach (TabItem ti in this._tb_XpsDocumentsAndReports.Items)
                {
                    if (ti.Content is ReportEditor && ti.Visibility == Visibility.Visible)
                    {
                        if (ti == this._tb_XpsDocumentsAndReports.SelectedItem)
                        {
                            ((ReportEditor)ti.Content).PrintReport();
                            foundReportToExport = true;
                        }
                    }
                }
                if (!foundReportToExport)
                    MessageBox.Show("印刷したいレポートを選択して下さい。", "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to pring Report" +
						 "\nError: " + ex.Message);

                MessageBox.Show("レポートを印刷する際に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _M_meet_OnPrintKnowledgeMap(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                this._currentKnowledgeMapControl.PrintKnowledgeMap();
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to print Knowledge Map" +
						 "\nError: " + ex.Message);

                MessageBox.Show("ナレッジマップを印刷する際に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //ToDo: Test / refactor
        private void _M_meet_OnSaveAssignmentAs(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            //this._currentWorkingAssingment.IsPersisted = false;
            //this._M_meet_OnSaveAssignment(null, null);
            this.SaveAssignment(true);
        }

		private void _M_meet_OnSaveAssignment(object sender, RoutedEventArgs e)
		{
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if(e != null)
                e.Handled = true;

            this.SaveAssignment(false);
            
		}

		private void _km_OnSelectedKmChanged(object sender, RoutedEventArgs e)
		{
			//TODO: Implement 080410: depr...
            //TabItem t = this._tb_KnowledgeMaps.SelectedValue as TabItem;
            //if(t != null)
            //    this._currentKnowledgeMapControl = t.Content as KnowledgeMap;
		}

        private void _M_meet_DefaultImportCommand(object sender, MouseButtonEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            this._M_meet_OnAddXpsDocumentToStudy(null, null);
        }

        private void _M_meet_OnAddNewStudy(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {

                if (this._currentWorkingAssingment != null)
                {
                    EjsBridge.ejsBridgeManager.EjsAddress = App._ejpSettings.EjsAddress;
                    ejpWindows.NewAssignmentWindow newStudyW = new ejpWindows.NewAssignmentWindow();
                    newStudyW.WindowHeadline = "スタディの追加";
                    newStudyW.Closed += delegate(object ws, EventArgs we)
                    {
                        ejpStudy newStudy = null;
                        if (newStudyW.CreateLocation != ejpClient.ejpWindows.CreateAssignmentStartLocation.NotSet)
                        {
                            newStudy = this._currentWorkingAssingment.CreateNewStudy();
                            this.LoadStudyIntoUi(newStudy);
                            this._currentWorkingStudy = newStudy;
                            this.SetToolBoxState(ToolBoxState.Document);
                        }

                        if (newStudyW.CreateLocation ==
                            ejpClient.ejpWindows.CreateAssignmentStartLocation.Local)
                        {
                            FileInfo fi = new FileInfo(newStudyW.FirstDocumentLocalPath);
                            ejpXpsDocument d = 
                                newStudy.ImportXpsDocument(newStudyW.FirstDocumentLocalPath,
                                fi.Name, true, IdManipulation.GetNewGuid());
                            this.AddNewXpsDocumentToUi(d);
                            this._LB_StudyList.SelectedIndex = this._LB_StudyList.Items.Count - 1;

                            if (this._currentWorkingAssingment.IsPersisted)
                                this._currentWorkingAssingment.Save();

                            this.AutoSaveCurrentAssignment(true);
                        }
                        else if (newStudyW.CreateLocation ==
                            ejpClient.ejpWindows.CreateAssignmentStartLocation.None)
                        {
                            this._LB_StudyList.SelectedIndex = this._LB_StudyList.Items.Count - 1;

                            if (this._currentWorkingAssingment.IsPersisted)
                                this._currentWorkingAssingment.Save();
                        }
                        else if (newStudyW.CreateLocation ==
                            ejpClient.ejpWindows.CreateAssignmentStartLocation.EJournalServer)
                        {
                            ejpXpsDocument d =
                                newStudy.ImportXpsDocument(newStudyW.FirstDocumentLocalPath, 
                                newStudyW.EjsDocumentToDownload._name, true,
                                IdManipulation.GetNewGuid());
                            this.AddNewXpsDocumentToUi(d);
                            this._LB_StudyList.SelectedIndex = this._LB_StudyList.Items.Count - 1;
                            this.AutoSaveCurrentAssignment(true);

                            if (this._currentWorkingAssingment.IsPersisted)
                                this._currentWorkingAssingment.Save();
                        }

						if (newStudy != null)
						{
							if(this._LB_StudyList.Items.Contains(newStudy))
								this._LB_StudyList.ScrollIntoView(newStudy);
						}
                    };
                    newStudyW.ShowDialog();
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to add new study" +
						 "\nError: " + ex.Message);

                MessageBox.Show("失敗しました。\n" + ex.Message);
            }
        }

        private void _M_meet_OnAddNewReportToStudy(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if(e != null)
                e.Handled = true;

            //We do not allow adding Reports to Commented Assignments
            if (this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment)
                return;

            try
            {
                //FIX: Load this using the same function as when displaying the reports
                //when loading from the package.
                ejpReport r = this._currentWorkingStudy.AddReport();
                ReportEditor re = new ReportEditor();
                re.ImportReport(r, this._currentWorkingStudy.MetaData.Id);
                TabItem t = new TabItem();
                t.Tag = this._currentWorkingStudy.MetaData.Id;
                TextBlock headerT = new TextBlock
                {
                    Text = "レポート " +
                    this._currentWorkingStudy.Reports.Count.ToString(),
                    Tag = t,
                    AllowDrop = true
                };

                headerT.DragEnter += new DragEventHandler(headerT_PreviewDragEnter);
                headerT.MouseLeftButtonUp += new MouseButtonEventHandler(Report_GotFocus);
                t.Header = headerT;

                t.Content = re;
                t.MouseLeftButtonUp += new MouseButtonEventHandler(Report_GotFocus);
                //t.MouseMove +=new MouseEventHandler(ReportTabItem_MouseOver);
                //re.LoadTextColors(this._reportAreaColorsList);
                this._tb_XpsDocumentsAndReports.Items.Add(t);
                t.Focus();
                this._openReports.Add(re);
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to add new report to study" +
						 "\nError: " + ex.Message);

                MessageBox.Show("レポートは二枚より作成出来ません");
            }
        }

        private void _M_meet_OnAddNewKnowledgeMapToStudy(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if(e != null)
                e.Handled = true;

            //We do not allow adding Knowledge Maps to Commented Assignments
            if (this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment)
                return;

            bool addKM = true;
            if (this._currentWorkingAssingment.IsPersisted == false)
            {
                addKM = false;
                if (MessageBox.Show("ナレッジマップを追加するため\n" +
                    "ファイルを保存しなければなりません。\n\n" +
                    "ファイルを保存して、ナレッジマップを追加しますか？",
                    "ナレッジマップの追加", MessageBoxButton.YesNo, MessageBoxImage.Question)
                    == MessageBoxResult.Yes)
                    addKM = true;
                else
                    return;
            }

            if (addKM == false)
                return;


            //FIX: Load this using the same function as when displaying the maps
            //when loading from the package.
            try
            {
                ejpKnowledgeMap ejpMap = this._currentWorkingStudy.AddKnowledgeMap();
                KnowledgeMap m = new KnowledgeMap();
                m.ImportMapObject(ejpMap);
                m.OnEntityRequestedReferenceNavigate += new EntityRequestedReferenceNavigate(m_OnEntityRequestedReferenceNavigate);
                m.OnLinkedEntityColorChanged += new LinkedEntityColorChanged(m_OnLinkedEntityColorChanged);
                m.OnMapLockStatusChanged += new MapLockStatusChanged(m_OnMapLockStatusChanged);
                m.OnKnowledgeMapRequestToolUpdate +=new KnowledgeMapRequestToolUpdate(m_OnKnowledgeMapRequestToolUpdate);
                this._openknowledgeMaps.Add(m);

                //Make sure the assienmnt is saved...
                this._M_meet_OnSaveAssignment(null, null);
                if (this._currentWorkingAssingment.IsPersisted == false)
                {
                    //if the assigment is still not saved, rewind...
                    this._openknowledgeMaps.Remove(m);
                    this._currentWorkingStudy.KnowledgeMaps.Remove(ejpMap);
                    return;
                }

                TabItem t = new TabItem();
                t.Tag = this._currentWorkingStudy.MetaData.Id;

                TextBlock headerT = new TextBlock
                {
                    Text = "ナレッジマップ " +
                    this._currentWorkingStudy.KnowledgeMaps.Count.ToString(),
                    Tag = t,
                    AllowDrop = true
                };

                headerT.DragEnter += new DragEventHandler(headerT_PreviewDragEnter);
                headerT.MouseLeftButtonUp += new MouseButtonEventHandler(KM_GotFocus);
                t.Header = headerT;

                t.Content = m;
                t.MouseLeftButtonUp += new MouseButtonEventHandler(KM_GotFocus);
                //t.MouseMove +=new MouseEventHandler(KnowledgeMapTabItem_MouseOver);
                this._tb_KnowledgeMaps.Items.Add(t);
                t.Focus();
                this._currentKnowledgeMapControl = m;
                
                //080409
                //Give the new KM the same tool and focus as all other
                //currently open KMs
                m.HasInputFocus = true;

                

                switch (this._previousKmTool)
                {
                    case Tool.Pen:
                        m.InputState = KnowledgeMapInputState.None;
                        break;
                    case Tool.Marker:
                        m.InputState = KnowledgeMapInputState.None;
                        break;
                    case Tool.Eraser:
                        m.InputState = KnowledgeMapInputState.None;
                        break;
                    case Tool.Label:
                        m.InputState = KnowledgeMapInputState.Label;
                        break;
                    case Tool.Freehand:
                        m.InputState = KnowledgeMapInputState.Freehand;
                        break;
                    case Tool.Line:
                        m.InputState = KnowledgeMapInputState.Line;
                        break;
                    case Tool.SingleArrow:
                        m.InputState = KnowledgeMapInputState.SingleArrow;
                        break;
                    case Tool.DoubleArrow:
                        m.InputState = KnowledgeMapInputState.DoubleArrow;
                        break;
                    case Tool.Square:
                        m.InputState = KnowledgeMapInputState.Square;
                        break;
                    case Tool.Circle:
                        m.InputState = KnowledgeMapInputState.Circle;
                        break;
                    case Tool.Note:
                        m.InputState = KnowledgeMapInputState.Note;
                        break;
                    case Tool.Select:
                        m.InputState = KnowledgeMapInputState.Select;
                        break;
                    default:
                        m.InputState = KnowledgeMapInputState.None;
                        break;
                }
                
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to add new Knowledge Map to study" +
						 "\nError: " + ex.Message);

                MessageBox.Show("ナレッジマップは二枚より作成出来ません");
            }
        }

        /// <summary>
        /// Imports an image to set as KnowledgeMap Guide (Background)
        /// </summary>
        private void _M_meet_OnSetKnowledgeMapGuide(object sender, RoutedEventArgs e)
        {
            SiliconStudio.DebugManagers.DebugReporter.ReportMethodEnter();
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                bool deleteCurrentGuide = false;
                if (this._currentKnowledgeMapControl == null)
                {
                    MessageBox.Show("ガイドを設定たいナレッジマップを選択して下さい。",
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (this._currentKnowledgeMapControl.HasGuide)
                {
                    if (MessageBox.Show(
                        "\nガイドはすでに設定してあります。新しいガイドを設定するため\n" +
                        "このガイドを削除しなければなりません。\n\n" +
                        "現在のガイドを削除して、新しいガイドを設定しますか？",
                        "ガイドの設定", MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.Yes)
                        deleteCurrentGuide = true;
                    else
                        return;
                }

                winForms.OpenFileDialog imageFileDialog = new System.Windows.Forms.OpenFileDialog();

                imageFileDialog.Filter = "Image Files|*.png;*.bmp;";
                //imageFileDialog.Filter = "Png Images (*.png)|*.png";

                if (imageFileDialog.ShowDialog() == winForms.DialogResult.OK)
                {
                    if(deleteCurrentGuide)
                        this._currentKnowledgeMapControl.ClearKnowledgeMapGuide();

                    ejpExternalImageEntityWrapper wrapper = this._currentWorkingStudy.ImportKnowledgeMapGuideToStudy(
                        imageFileDialog.FileName, this._currentKnowledgeMapControl.LocalMapObject);

                    //Pass negative values for defaults.
                    this._currentKnowledgeMapControl.SetKnowledgeMapGuide("", wrapper.Source, -1, -1, -1, -1);
                }
            }
            catch (Exception ex)
            {
                SiliconStudio.DebugManagers.DebugReporter.Report(
                         SiliconStudio.DebugManagers.MessageType.Error,
                         "eJournalPlus Client",
                         "Failed to Import Guide to KnowledgeMap" +
                         "\nError: " + ex.Message);

                MessageBox.Show("画像の読み込みに失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SiliconStudio.DebugManagers.DebugReporter.ReportMethodLeave();
        }

        private void _M_meet_OnClearKnowledgeMapGuide(object sender, RoutedEventArgs e)
        {
            SiliconStudio.DebugManagers.DebugReporter.ReportMethodEnter();
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            try
            {
                bool deleteCurrentGuide = false;
                if (this._currentKnowledgeMapControl == null)
                {
                    MessageBox.Show("ガイドを設定たいナレッジマップを選択して下さい。",
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (this._currentKnowledgeMapControl.HasGuide)
                {
                    if (MessageBox.Show(
                        "ガイドは削除しますか？", "ガイド削除", MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.Yes)
                        deleteCurrentGuide = true;
                    else
                        return;
                }
                else
                {
                    MessageBox.Show(
                        "選択してあるナレッジマップはまだガイド設定されていません。",　
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                }

                if(deleteCurrentGuide)
                    this._currentKnowledgeMapControl.ClearKnowledgeMapGuide();
            }
            catch (Exception ex)
            {
                SiliconStudio.DebugManagers.DebugReporter.Report(
                         SiliconStudio.DebugManagers.MessageType.Error,
                         "eJournalPlus Client",
                         "Failed to Delete KnowledgeMap Guide" +
                         "\nError: " + ex.Message);

                MessageBox.Show("ガイドの削除に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SiliconStudio.DebugManagers.DebugReporter.ReportMethodLeave();
        }

        /// <summary>
        /// Import an Image file to the current KM
        /// </summary>
        private void _M_meet_OnImportImage(object sender, RoutedEventArgs e)
        {
			SiliconStudio.DebugManagers.DebugReporter.ReportMethodEnter();
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
			if (e != null)
				e.Handled = true;

			try
			{
				winForms.OpenFileDialog imageFileDialog = new System.Windows.Forms.OpenFileDialog();
                imageFileDialog.Filter = "Image Files|*.png;*.bmp;";
				//imageFileDialog.Filter = "Png Images (*.png)|*.png";

				if (imageFileDialog.ShowDialog() == winForms.DialogResult.OK)
				{
					ejpExternalImageEntityWrapper wrapper = this._currentWorkingStudy.ImportImageFileToStudy(
						imageFileDialog.FileName, this._currentKnowledgeMapControl.LocalMapObject);

					//passing the source Uri as the target path ... NOT a good solution...
					this._currentKnowledgeMapControl.AddImageEntity(
						System.IO.Path.GetFileNameWithoutExtension(imageFileDialog.FileName),
						wrapper.Source, Brushes.Black, SiliconStudio.Meet.EjpControls.Enumerations.KnowledgeMapEntityType.OriginalToMap,
						wrapper.SourceUri, "", 20, 20, 100, 100,
							new XpsDocumentReference()
							{
								AnchorX = 0,
								AnchorY = 0,
								Content = "",
								DocumentId =
								Guid.Empty,
								DocumentParentStudyId = Guid.Empty,
								PageNumber = -1,
								ParentPathData = "",
								TargetLineId = Guid.Empty
							});
				}
			}
			catch (Exception ex)
			{
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to Import Image to KnowledgeMap" +
						 "\nError: " + ex.Message);

				MessageBox.Show("画像の読む込みに失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
			}


			SiliconStudio.DebugManagers.DebugReporter.ReportMethodLeave();
        }

        private void _M_meet_OnAddXpsDocumentToStudy(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            if (this._currentWorkingStudy == null)
            {
                MessageBox.Show("まず最初にXPSドキュメントを追加したいスタディを選んでください。");
                return;
            }

            try
            {
                EjsBridge.ejsBridgeManager.EjsAddress = App._ejpSettings.EjsAddress;
                ejpWindows.NewAssignmentWindow newDocuementW = new ejpWindows.NewAssignmentWindow();
                newDocuementW.WindowHeadline = "スタディにドキュメントの追加";
                newDocuementW.DisableNoSelect();
                newDocuementW.Closed += delegate(object ws, EventArgs we)
                {
                    if (newDocuementW.CreateLocation ==
                        ejpClient.ejpWindows.CreateAssignmentStartLocation.Local)
                    {
                        FileInfo fi = new FileInfo(newDocuementW.FirstDocumentLocalPath);

                        ejpXpsDocument d =
                            this._currentWorkingStudy.ImportXpsDocument(
                            newDocuementW.FirstDocumentLocalPath, fi.Name, true, IdManipulation.GetNewGuid());
                        this.AddNewXpsDocumentToUi(d);
                        this.AutoSaveCurrentAssignment(true);
                        this._mtb_Eraser.IsChecked = false;
                    }
                    else if (newDocuementW.CreateLocation ==
                        ejpClient.ejpWindows.CreateAssignmentStartLocation.EJournalServer)
                    {
                        ejpXpsDocument d =
                            this._currentWorkingStudy.ImportXpsDocument(newDocuementW.FirstDocumentLocalPath, 
                            newDocuementW.EjsDocumentToDownload._name, true,
                            IdManipulation.GetNewGuid());
                        this.AddNewXpsDocumentToUi(d);
                        this.AutoSaveCurrentAssignment(true);
                        this._mtb_Eraser.IsChecked = false;
                    }
                };
                newDocuementW.ShowDialog();
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to Add XPS Document to study" +
						 "\nError: " + ex.Message);

                MessageBox.Show("失敗しました。\n" + ex.Message);
            }
          
        }

        /// <summary>
        /// When a new Xps document is added to a study that already exists in an assignment,
        /// this method adds all the necessary callbacks and Ui elements to get the new
        /// XPS document representeted in the Ui.
        /// </summary>
        /// <param name="document"></param>
        private void AddNewXpsDocumentToUi(ejpXpsDocument document)
        {
            //If the package is saved temporary that means we need to 
            //save the new XpsDocument by copying it not from disk but from
            //the current temp assignment file.
            if (!this._currentWorkingAssingment.IsPersisted)
                document.IsExternalToAssignment = false;

            //FIX: move this code out of here to its own function
            XpsDocumentViewer dv = new XpsDocumentViewer();

            dv.OnDocumentLineDeleted +=
                new DocumentLineDeleted(xpsDocumentViewer_OnDocumentLineDeleted);

            dv.OnDocumentLineContentsChanged +=
                new DocumentLineContentsChanged(xpsDocumentViewer_OnDocumentLineContentsChanged);

            dv.OnImageLineColorChanged +=
                new ImageLineColorChanged(xpsDocumentViewer_OnImageLineColorChanged);

            dv.OnImageLineDeleted +=
                new ImageLineDeleted(xpsDocumentViewer_OnImageLineDeleted);

            dv.OnQueryDocumentLineConnections += new QueryDocumentLineConnections(dv_OnQueryDocumentLineConnections);

            //080731
            dv.PropagateMarkerColorChange(this._M_ML_ColorSwatchButton.GetCurrentColor(), false);
            dv.PropagatePenColorChange(this._M_PL_ColorSwatchButton.GetCurrentColor(), false);

			try
			{
				dv.LoadXpsDocument(document, this._currentWorkingStudy.MetaData.Id);
			}
			catch (ApplicationException ex)
			{
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"eJournalPlus - Client",
					"Load Xps Document into Ui Failed" +
					"\nError: " + ex.Message);

				//TODO: SHow a message...
				throw;
			}

            TabItem t = new TabItem();
            t.Tag = this._currentWorkingStudy.MetaData.Id;

            TextBlock headerT = new TextBlock
            {
                Text = document.XpsDocument.CoreDocumentProperties.Title,
                Tag = t
            };

            headerT.DragEnter += new DragEventHandler(headerT_PreviewDragEnter);
            headerT.MouseLeftButtonUp += new MouseButtonEventHandler(XPSDocument_GotFocus);
            t.Header = headerT;

            t.Content = dv;

            this._currentXpsDocumentViewer = dv;
            
            if(this._M_PL_ColorSwatchButton.IsActive)
                dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.PenLine;
            else if(this._M_ML_ColorSwatchButton.IsActive)
                dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.MarkerLine;

            dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
            
            dv.HasInputFocus = true;
            
            //t.MouseMove += new MouseEventHandler(XpsTabItem_MouseOver);
            t.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(XPSDocument_GotFocus);
            this._tb_XpsDocumentsAndReports.Items.Add(t);
            t.Focus();
            this._openXpsDocumentViewers.Add(dv);

            //080731
            this._currentWorkingStudy.LastDispplayedReportXPSIndex = this._tb_XpsDocumentsAndReports.Items.IndexOf(t);

        }

        private void _M_meet_OnStartQuit(object sender, RoutedEventArgs e)
        {
            //Need to set this to handled to prevent
            //menu group default action from being invoked.
            if (e != null)
                e.Handled = true;

            this.ClearTempFiles();

            this.Close();
        }

		#region ToolBox

		private void _M_da_OnSetInputSelect(object sender, RoutedEventArgs e)
		{
			foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
			{
				dv.Cursor = Cursors.Arrow;
				dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Select;
				dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.None;
			}
            this._previousKmTool = Tool.Select;
		}

        private void _M_da_OnSetInputErase(object sender, RoutedEventArgs e)
        {
            if (this._previousToolBoxState == ToolBoxState.KnowledgeMap)
            {
                this._currentKnowledgeMapControl.DeleteSelection();
            }
            else if (this._previousToolBoxState == ToolBoxState.Document)
            {
				if(this._currentXpsDocumentViewer != null)
					this._currentXpsDocumentViewer.DeleteSelection();
            }

            this._mtb_Curve.IsChecked = false;
            this._mtb_Circle.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_Select.IsChecked = false;

            /* 080723
             * Moved into the If statement to prevent BUG 223:
             * When a line is selected in teh DA and the eraser is
             * activated, the line is deleted and the DA input mode is
             * reset, but the KM remains in eraser mode...
             */
            //foreach (KnowledgeMap km in this._openknowledgeMaps)
             //   km.InputState = KnowledgeMapInputState.EraseByStroke;

            //There are some event fired on the way down here so we need to double check...
            if ((bool)this._mtb_Eraser.IsChecked)
            {
                foreach (KnowledgeMap km in this._openknowledgeMaps)
                {
                    km.InputState = KnowledgeMapInputState.EraseByStroke;
                }
                foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
                {
                    dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Erase;
                }
            }
            else
            {
                foreach (KnowledgeMap km in this._openknowledgeMaps)
                {
                    km.InputState = KnowledgeMapInputState.None;
                }
                foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
                {
                    dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
                }
            }

        }

        private void _M_da_OnUnSetInputErase(object sender, RoutedEventArgs e)
        {
            foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
            {
                dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
            }
            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.None;
            }
        }

		private void _M_da_OnSetInputPenLine(object sender, RoutedEventArgs e)
		{
            this._mtb_Eraser.IsChecked = false;
			foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
			{
				dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
				dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.PenLine;
			}
		}

		private void _M_da_OnSetInputMarkerLine(object sender, RoutedEventArgs e)
		{
			foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
			{
				dv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
				dv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.MarkerLine;
			}
		}

		private void _M_km_OnSetModeFreehand(object sender, RoutedEventArgs e)
		{
            this._mtb_Circle.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.Freehand;
            }
            this._previousKmTool = Tool.Freehand;
		}

        private void _M_km_OnSetMode_None(object sender, RoutedEventArgs e)
        {
            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.None;
            }
        }

		private void _M_km_OnSetModeSelect(object sender, RoutedEventArgs e)
		{
            this._mtb_Curve.IsChecked = false;
            this._mtb_Circle.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.Select;
            }
            this._previousKmTool = Tool.Select;
		}

        private void _M_da_OnDecreaseEntityFontSize(object sender, RoutedEventArgs e)
        {
            if (this._currentKnowledgeMapControl == null)
                return;
            this._currentKnowledgeMapControl.PropagateDecreaseFontSize();
        }

        private void _M_da_OnIncreaseEntityFontSize(object sender, RoutedEventArgs e)
        {
            if (this._currentKnowledgeMapControl == null)
                return;
            this._currentKnowledgeMapControl.PropagateIncreaseFontSize();
        }

		private void _M_km_OnSetModeSimpleArrow(object sender, RoutedEventArgs e)
		{
            this._mtb_Curve.IsChecked = false;
            this._mtb_Circle.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.SingleArrow;
            }
            this._previousKmTool = Tool.SingleArrow;
		}

		private void _M_km_OnSetModeDoubleArrow(object sender, RoutedEventArgs e)
		{
            this._mtb_Curve.IsChecked = false;
            this._mtb_Circle.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.DoubleArrow;
            }
            this._previousKmTool = Tool.DoubleArrow;
		}

        private void _M_da_OnSetModeLabel(object sender, RoutedEventArgs e)
        {
            this._mtb_Curve.IsChecked = false;
            this._mtb_Circle.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.Label;
            }
            this._previousKmTool = Tool.Label;
        }

		private void _M_km_OnSetModeLine(object sender, RoutedEventArgs e)
		{
            this._mtb_Curve.IsChecked = false;
            this._mtb_Circle.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.Line;
            }
            this._previousKmTool = Tool.Line;
		}

        private void _M_km_OnUnSetModeLine(object sender, RoutedEventArgs e)
        {
            this._mtb_Select.IsChecked = true;
        }

		private void _M_km_OnSetModeSquare(object sender, RoutedEventArgs e)
		{
            this._mtb_Curve.IsChecked = false;
            this._mtb_Circle.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.Square;
            }
            this._previousKmTool = Tool.Square;
		}

		private void _M_km_OnSetModeCircle(object sender, RoutedEventArgs e)
		{
            this._mtb_Curve.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.Circle;
            }
            this._previousKmTool = Tool.Circle;
		}

		private void _M_km_OnSetModeNote(object sender, RoutedEventArgs e)
		{
            this._mtb_Curve.IsChecked = false;
            this._mtb_Square.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Circle.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.InputState = KnowledgeMapInputState.Note;
            }
            this._previousKmTool = Tool.Note;
		}

		#endregion

		#endregion

        #region Other Callbacks

		private void OnMagnifierMove(object sender, MouseEventArgs e)
		{
            //TODO: Solve the constant update of the 
            //      size of the magnifier...

                VisualBrush mg_B = (VisualBrush)mg_Visual.Fill;
                Rect viewBox = mg_B.Viewbox;
                viewBox.Width = this._sl_MagnifierFactor.Value;
                viewBox.Height = this._sl_MagnifierFactor.Value;

			Point mousePos = e.MouseDevice.GetPosition(this.LayoutRoot);
			double xoffset = viewBox.Width / 2.0;
			double yoffset = viewBox.Height / 2.0;
			viewBox.X = mousePos.X - xoffset;
			viewBox.Y = mousePos.Y - yoffset;
			mg_B.Viewbox = viewBox;
			Canvas.SetLeft(mg_bgCanvas, mousePos.X - mg_Visual.Width / 2);
			Canvas.SetTop(mg_bgCanvas, mousePos.Y - mg_Visual.Height / 2);
		}

		private void OnMagnifierFactorChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (mg_Visual != null)
			{
				VisualBrush mg_B = (VisualBrush)mg_Visual.Fill;
				Rect viewBox = mg_B.Viewbox;
				viewBox.Width = this._sl_MagnifierFactor.Value;
				viewBox.Height = this._sl_MagnifierFactor.Value;
				mg_B.Viewbox = viewBox;
			}
		}

        private void OnSelectedStudyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._LB_StudyList.SelectedItem != null)
            {
                ejpStudy s = this._LB_StudyList.SelectedItem as ejpStudy;
                this.DisplayStudy(s.MetaData.Id);
                this._currentWorkingStudy = s;
            }
        }

        #endregion

        #region Open / Close / Save / Create Assignment
        private void SaveAssignment(bool runSaveAs)
		{

            if (this._currentWorkingAssingment == null)
                return;

            if (!File.Exists(this._currentWorkingAssingment.FilePackagePath) ||
                this._currentWorkingAssingment.IsPersisted == false ||
                runSaveAs)
            {
                try
                {
                    bool prevPerStatus = this._currentWorkingAssingment.IsPersisted;
                    
                    this._currentWorkingAssingment.IsPersisted = false;

                    winForms.SaveFileDialog saveAssignmentDialog = new winForms.SaveFileDialog();
                    if (this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                        SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.WorkingAssignment)
                        saveAssignmentDialog.Filter = "Assignments (*.ejp)|*.ejp";
                    else
                        if (this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                            SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment)
                            saveAssignmentDialog.Filter = "Commented Assignments (*.cejp)|*.cejp";

                    if (saveAssignmentDialog.ShowDialog() == winForms.DialogResult.OK)
                    {
                        foreach (KnowledgeMap km in this._openknowledgeMaps)
                            km.ExportMapObject();

                        foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                            xpsdv.ExportDocumentExtras();

                        foreach (ReportEditor repe in this._openReports)
                            repe.ExportReportComments();

                        this._currentWorkingAssingment.FilePackagePath = saveAssignmentDialog.FileName;
                        this._currentWorkingAssingment.SaveAs();

                        foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                            xpsdv.LoadXpsDocument(null, Guid.Empty);

                        this.UpdateWindowTitle();
                    }
                    else
                        this._currentWorkingAssingment.IsPersisted = prevPerStatus;
                }
                catch (Exception ex)
                {
                    SiliconStudio.DebugManagers.DebugReporter.Report(
                         SiliconStudio.DebugManagers.MessageType.Error,
                         "eJournalPlus Client",
                         "Failed to Save assignment" +
                         "\nError: " + ex.Message);

                    MessageBox.Show("アサインメントを保存する際に失敗しました。", "保存の失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                    this._canAutoSave = false;
                }
            }
            else
            {
                try
                {
                    foreach (KnowledgeMap km in this._openknowledgeMaps)
                        km.ExportMapObject();

                    foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                        xpsdv.ExportDocumentExtras();

                    foreach (ReportEditor repe in this._openReports)
                        repe.ExportReportComments();

                    this._currentWorkingAssingment.Save();
                }
                catch (Exception ex)
                {
                    SiliconStudio.DebugManagers.DebugReporter.Report(
                         SiliconStudio.DebugManagers.MessageType.Error,
                         "eJournalPlus Client",
                         "Failed to Save assignment" +
                         "\nError: " + ex.Message);

                    MessageBox.Show("アサインメントを保存する際に失敗しました。", "保存の失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                    this._canAutoSave = false;
                }
            }

            //if(this._currentWorkingAssingment != null)
            //    this._currentWorkingAssingment.Save();
            //this.UpdateWindowTitle();
		}

        private bool CloseCurrentAssignment()
        {
            MessageBoxResult respons = MessageBox.Show("編集された項目を保存しますか？",
                "保存しますか？", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            bool saveOnClose = false;

            switch (respons)
            {
                case MessageBoxResult.Cancel:
                    return false;
                case MessageBoxResult.No:
                    saveOnClose = false;
                    break;
                case MessageBoxResult.None:
                    return false;
                case MessageBoxResult.Yes:
                    saveOnClose = true;
                    break;
                default:
                    break;
            }

            try
            {
				if (saveOnClose) //080520 Need to not run this in case the user answered No.. duh... :)
				{
					foreach (KnowledgeMap km in this._openknowledgeMaps)
						km.ExportMapObject();

					foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
						xpsdv.ExportDocumentExtras();

					foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
						xpsdv.Close();

					foreach (ReportEditor repe in this._openReports)
						repe.ExportReportComments();
				}

                if (this._currentWorkingAssingment != null)
                {
                    if (this._currentWorkingAssingment.IsPersisted)
                        this._currentWorkingAssingment.Close(saveOnClose);
                    else
                    {
                        if (saveOnClose)
                            this._M_meet_OnSaveAssignment(null, null);
                        this._currentWorkingAssingment.Close(false);
                    }

                    this._currentWorkingAssingment = null;
                    ejpAssignment.ReleaseAllImportedAssignments();
                }
                this.UpdateWindowTitle();

                return true;
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to close the current assignment" +
						 "\nError: " + ex.Message);

                if(saveOnClose)
                    MessageBox.Show("アサインメントを保存する際に失敗しました。", "保存の失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("アプリケーションを閉じる際に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                
                return false;
            }
        }

        private void CreateNewEmptyAssignment(ejpStudent owner)
        {
			string saveDir = this._localAppDataPath +
                "\\TemporaryFiles\\" +
                "TempEjpPackage" + DateTime.Now.Ticks.ToString() + ".ejp";

            this._currentWorkingAssingment = 
                ejpAssignment.CreateEmpty("No Name", owner, false, saveDir);
            this.LoadCurrentAssignment();
            this.UpdateWindowTitle();
        }

		private void CreateNewAssignment(string firstXpsPath, string firstXpsTitle, ejpStudent owner, bool isManagedByEJournalServer, Guid xpsDocumentId)
		{
			string saveDir = this._localAppDataPath +
                "\\TemporaryFiles\\" +
                "TempEjpPackage" + DateTime.Now.Ticks.ToString() + ".ejp";

			 this._currentWorkingAssingment = ejpAssignment.Create(
                 "No Name", firstXpsPath, firstXpsTitle, owner,
                 isManagedByEJournalServer, xpsDocumentId, saveDir);
			 this.LoadCurrentAssignment();
             this.UpdateWindowTitle();
		}

        private void OpenLocalAssignment(string targetPath)
        {
            this.ClearUi();
            this._currentWorkingAssingment = ejpAssignment.Open(targetPath);
            this.LoadCurrentAssignment();
            this.UpdateWindowTitle();
            this._currentWorkingAssingment.IsPersisted = true;
        }

        private void ImportAssignment(string targetPath)
        {
            try
            {
                if (this._currentWorkingAssingment.IsPersisted == false)
                {
                    MessageBox.Show("別のアサインメントをマージする前に，アサインメントを保存してください。",
                        "エラー",MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                List<ejpStudy> importedStudies = this._currentWorkingAssingment.Import(targetPath);
                foreach (ejpStudy newStudy in importedStudies)
                {
                    this.LoadStudyIntoUi(newStudy);
                }

                if (importedStudies.Count != 0)
                {
                    this.DisplayStudy(importedStudies[0].MetaData.Id);
                    this._LB_StudyList.SelectedItem = importedStudies[0];

                    if (this._M_PL_ColorSwatchButton.IsActive)
                    {
                        this._currentXpsDocumentViewer.InputMethod = 
                            SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
                        this._currentXpsDocumentViewer.DrawingMode = 
                            SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.PenLine;
                    }
                    if (this._M_ML_ColorSwatchButton.IsActive)
                    {
                        this._currentXpsDocumentViewer.InputMethod =
                            SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
                        this._currentXpsDocumentViewer.DrawingMode =
                            SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.MarkerLine;
                    }
                    this._currentXpsDocumentViewer.HasInputFocus = true;
                }
                else
                    MessageBox.Show("読み込もうとしたアサインメントは以前からこのアサインメントに含まれている。", 
                        "失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to import assignment" +
						 "\nTarget Path: " + targetPath +
						 "\nError: " + ex.Message);

                MessageBox.Show("アサインメントを読み込もうとしたら失敗しました。\n"+ex.Message, "失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

		private void ExportAssignment(string targetPath)
		{
			//Persist the KMs to the assignment
			foreach (KnowledgeMap km in this._openknowledgeMaps)
				km.ExportMapObject();

            foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                xpsdv.ExportDocumentExtras();

            foreach (ReportEditor repe in this._openReports)
                repe.ExportReportComments();

			this._currentWorkingAssingment.Export(targetPath);
		}

		private void LoadCurrentAssignment()
		{
			if (this._currentWorkingAssingment == null)
				throw new ApplicationException("The Current Working Assignment is not set.");

			this.ClearUi();

			this._bindings_StudiesBinding = new Binding();
			this._bindings_StudiesBinding.Source = this._currentWorkingAssingment;
			this._bindings_StudiesBinding.Path = new PropertyPath("Studies");
			this._LB_StudyList.SetBinding(ListBox.ItemsSourceProperty, this._bindings_StudiesBinding);

            foreach (ejpStudy s in this._currentWorkingAssingment.Studies)
            {
                this.LoadStudyIntoUi(s);
            }

            if (this._currentWorkingAssingment.Studies.Count != 0)
            {
                this._currentWorkingStudy = this._currentWorkingAssingment.Studies[0];
                this.DisplayStudy(this._currentWorkingStudy.MetaData.Id);
            }
            this.SetTabShareReportXPS();
            this._LB_StudyList.SelectedIndex = 0;

            foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
                dv.HasInputFocus = true;

		}
		
        #endregion

        private void DisplayStudy(Guid id)
        {
            ejpStudy dispStudy = null;
            foreach (ejpStudy std in this._currentWorkingAssingment.Studies)
            {
                if (std.MetaData.Id == id)
                    dispStudy = std;
            }

            foreach (TabItem ti in this._tb_KnowledgeMaps.Items)
            {
                if (ti.Tag == null)
                    continue;

                if ((Guid)ti.Tag == id)
                {
                    ti.Width = double.NaN;
                    ti.Visibility = Visibility.Visible;
                    ti.IsHitTestVisible = true;
                    if ((UIElement)ti.Content != null)
                    {
                        ((UIElement)ti.Content).Visibility = Visibility.Visible;
                    }
                    if (this._tb_KnowledgeMaps.Items.IndexOf(ti)
                        == dispStudy.LastDispplayedKMIndex)
                    {
                        ti.Focus();
                        this._currentKnowledgeMapControl = ti.Content as KnowledgeMap;
                    }
                }
                else
                {
                    ti.Width = 0.1;
                    ti.Visibility = Visibility.Hidden;
                    ti.IsHitTestVisible = false;
                    if ((UIElement)ti.Content != null)
                    {
                        ((UIElement)ti.Content).Visibility = Visibility.Hidden;
                    }
                    ti.Width = 0.1;
                }
            }

            foreach (TabItem ti in this._tb_XpsDocumentsAndReports.Items)
            {
                if (ti.Tag == null)
                    continue;

                if ((Guid)ti.Tag == id)
                {
                    ti.Width = double.NaN;
                    ti.Visibility = Visibility.Visible;
                    ti.IsHitTestVisible = true;
                    if ((UIElement)ti.Content != null)
                    {
                        ((UIElement)ti.Content).Visibility = Visibility.Visible;
                    }
                    if (this._tb_XpsDocumentsAndReports.Items.IndexOf(ti)
                        == dispStudy.LastDispplayedReportXPSIndex)
                    {
                        ti.Focus();
                        //080731
                        //if the selected index is a dv, set the current dv -> dv
                        XpsDocumentViewer x = 
                            ti.Content as XpsDocumentViewer;
                        if (x != null)
                        {
                            this._currentXpsDocumentViewer = x;
                        }
                    }
                }
                else
                {
                    ti.Width = 0.1;
                    ti.Visibility = Visibility.Hidden;
                    ti.IsHitTestVisible = false;
                    if ((UIElement)ti.Content != null)
                    {
                        ((UIElement)ti.Content).Visibility = Visibility.Hidden;
                    }
                    ti.Width = 0.1;
                }
            }
            this._currentWorkingStudy = dispStudy;
            
            this._LB_StudyList.SelectedValue = dispStudy;
        }

		#region Load Study Into Gui

		private void LoadStudyIntoUi(ejpStudy study)
		{
			this.LoadXpsDocumentsFromStudy(study);
			this.LoadReportsFromStudy(study);
			this.LoadKnowledgeMapsFromStudy(study);

            if (this._currentWorkingAssingment.MetaData.AssignmentContentType !=
                 SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment)
            {
                if (study.KnowledgeMaps.Count < 2)
                {
                    TabItem t = new TabItem();
                    t.ToolTip = "新しいナレッジマップ";
                    t.Name = "newKMTab";
                    t.Tag = study.MetaData.Id;
                    t.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
                    {
                        //We do not allow adding KMs to Commented Assignments
                        //if (this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                        //    SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment)
                        //    return;
                        
                        this._M_meet_OnAddNewKnowledgeMapToStudy(null, null);
                        //if true, the assignmen was/is saved and the new KM was added...
                        if(this._currentWorkingAssingment.IsPersisted == true)
                            this._tb_KnowledgeMaps.Items.Remove(t);
                    };
                    this._tb_KnowledgeMaps.Items.Add(t);
                }


                if (study.Reports.Count < 2)
                {
                    TabItem t = new TabItem();
                    t.ToolTip = "新しいレポート";
                    t.Name = "newReportTab";
                    t.Tag = study.MetaData.Id;
                    t.MouseLeftButtonUp += delegate(object sender, MouseButtonEventArgs e)
                    {
                        //We do not allow adding Reports to Commented Assignments
                        //if (this._currentWorkingAssingment.MetaData.AssignmentContentType ==
                        //    SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.CommentedAssignment)
                        //    return;

                        this._tb_XpsDocumentsAndReports.Items.Remove(t);
                        this._M_meet_OnAddNewReportToStudy(null, null);
                    };
                    this._tb_XpsDocumentsAndReports.Items.Add(t);
                }
            }
		}

		private void LoadKnowledgeMapsFromStudy(ejpStudy study)
		{
            int kmCounter = 1;
			foreach (ejpKnowledgeMap ejpMap in study.KnowledgeMaps)
			{
				KnowledgeMap m = new KnowledgeMap();

                if (App._currentEjpStudent != null)
                {
                    m.CurrentOwnerName = App._currentEjpStudent.LastName + " " + App._currentEjpStudent.FirstName;
                    m.CurrentOwnerId = App._currentEjpStudent.Id;
                }

				m.ImportMapObject(ejpMap);
				this._openknowledgeMaps.Add(m);

				TabItem t = new TabItem();
                t.Tag = study.MetaData.Id;

                TextBlock headerT = new TextBlock { Text = "ナレッジマップ " + kmCounter.ToString(), Tag = t, AllowDrop=true };
                headerT.DragEnter += new DragEventHandler(headerT_PreviewDragEnter);
                headerT.MouseLeftButtonUp += new MouseButtonEventHandler(KM_GotFocus);
                t.Header = headerT;
                t.Content = m;
                t.MouseLeftButtonUp += new MouseButtonEventHandler(KM_GotFocus);
				this._tb_KnowledgeMaps.Items.Add(t);
                this._currentKnowledgeMapControl = m;

                m.OnEntityRequestedReferenceNavigate += new EntityRequestedReferenceNavigate(m_OnEntityRequestedReferenceNavigate);
                m.OnLinkedEntityColorChanged += new LinkedEntityColorChanged(m_OnLinkedEntityColorChanged);
                m.OnMapLockStatusChanged +=new MapLockStatusChanged(m_OnMapLockStatusChanged);
                m.OnKnowledgeMapRequestToolUpdate += new KnowledgeMapRequestToolUpdate(m_OnKnowledgeMapRequestToolUpdate);
                study.LastDispplayedKMIndex = this._tb_KnowledgeMaps.Items.IndexOf(t);

                if (this._currentWorkingAssingment.MetaData.IsManagedByEJournalServer)
                    m.IsMapCommentLayerUserLocked = true;
                else
                    m.IsMapCommentLayerUserLocked = false;
                kmCounter += 1;
			}
            this._tb_KnowledgeMaps.SelectedIndex = 0;
		}

        private void m_OnKnowledgeMapRequestToolUpdate(KnowledgeMap sender)
        {
            //TODO: Evaluate this method.
            switch (sender.InputState)
            {
                case KnowledgeMapInputState.None:
                    //Not yet implemented
                    break;
                case KnowledgeMapInputState.Select:
                    this._mtb_Select.IsChecked = true;
                    break;
                case KnowledgeMapInputState.Freehand:
                    break;
                case KnowledgeMapInputState.Label:
                    break;
                case KnowledgeMapInputState.EraseByStroke:
                    break;
                case KnowledgeMapInputState.Line:
                    break;
                case KnowledgeMapInputState.Square:
                    break;
                case KnowledgeMapInputState.Circle:
                    break;
                case KnowledgeMapInputState.SingleArrow:
                    break;
                case KnowledgeMapInputState.DoubleArrow:
                    break;
                case KnowledgeMapInputState.Note:
                    break;
                default:
                    break;
            }
        }

        private void KnowledgeMapTabItem_MouseOver(object sender, MouseEventArgs e)
        {
            if (this._previousToolBoxState != ToolBoxState.KnowledgeMap)
            {
                if (!(e.Source is TabItem) && !(e.Source is TextBlock))
                {
                    this.SetToolBoxState(ToolBoxState.KnowledgeMap);
                }
            }
        }

        /// <summary>
        /// Called by all tabs on drag enter
        /// </summary>
        private void headerT_PreviewDragEnter(object sender, DragEventArgs e)
        {
            TextBlock tb = sender as TextBlock;
            if (tb != null)
            {
                ((TabItem)tb.Tag).Focus();
                if(((TabItem)tb.Tag).Content is KnowledgeMap)
                    this._currentKnowledgeMapControl = (KnowledgeMap)((TabItem)tb.Tag).Content;
            }
        }

        private void KM_GotFocus(object sender, RoutedEventArgs e)
        {
            this._currentWorkingStudy.LastDispplayedKMIndex =
                this._tb_KnowledgeMaps.SelectedIndex;

            //moved here on 080730
            //still remains down under, commented out :)
            this._currentKnowledgeMapControl = ((KnowledgeMap)this._tb_KnowledgeMaps.SelectedContent);

            if (this._previousToolBoxState == ToolBoxState.KnowledgeMap)
                return;

            if ((e.Source is TabItem) || (e.Source is TextBlock))
                this.SetTabShareKnowledgeMap();

            this.SetToolBoxState(ToolBoxState.KnowledgeMap);
            this.SetTool(this._previousKmTool);

            foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
                dv.HasInputFocus = false;

            foreach (KnowledgeMap km in this._openknowledgeMaps)
                km.HasInputFocus = true;

            //this._currentWorkingStudy.LastDispplayedKMIndex =
              //  this._tb_KnowledgeMaps.SelectedIndex;

            //this._currentKnowledgeMapControl = ((KnowledgeMap)this._tb_KnowledgeMaps.SelectedContent);
            //Debug.Print(this._currentKnowledgeMapControl.LocalMapObject.PackageRelationshipIDString);
            
            /* The Editingmode of the InkCanvas is set to Select when the 
             * Children Collection of the InkCanvas is updated manually...
             * Thus we have to make sure the correct tool is still set 
             */
            this._currentKnowledgeMapControl.InputState = this._currentKnowledgeMapControl.InputState;

        }

        private void m_OnLinkedEntityColorChanged(Guid linkTargetId, string targetPathData, SolidColorBrush newColor)
        {
            foreach (TabItem ti in this._tb_XpsDocumentsAndReports.Items)
            {
                XpsDocumentViewer dv = ti.Content as XpsDocumentViewer;
                if (dv != null)
                    dv.PropagateLinkedEntityColorChanged(linkTargetId, targetPathData, newColor);
            }
        }

        private void m_OnEntityRequestedReferenceNavigate(XpsDocumentReference reference)
        {
            if(this._currentWorkingStudy.MetaData.Id != reference.DocumentParentStudyId)
                this.DisplayStudy(reference.DocumentParentStudyId);

            foreach (TabItem ti in this._tb_XpsDocumentsAndReports.Items)
            {
                XpsDocumentViewer dv = ti.Content as XpsDocumentViewer;
                if (dv != null)
                {
                    if (dv.Document.InternalDocumentId == reference.DocumentId)
                    {
                        this._tb_XpsDocumentsAndReports.SelectedValue = ti;
                        dv.FollowReference(reference);
                    }
                }
            }

        }

        private void m_OnMapLockStatusChanged(bool IsMaplockOn)
        {
            if (IsMaplockOn == true)
                this.UnCheckEntireToolbar();
        }

		private void LoadReportsFromStudy(ejpStudy study)
		{
            int repCounter = 1;
			foreach (ejpReport report in study.Reports)
			{
				ReportEditor re = new ReportEditor();

                if (App._currentEjpStudent != null)
                {
                    re.CurrentOwnerId = App._currentEjpStudent.Id;
                    re.CurrentOwnerName = App._currentEjpStudent.LastName + " " + App._currentEjpStudent.FirstName;
                }

				re.ImportReport(report, study.MetaData.Id);
				TabItem t = new TabItem();
                t.Tag = study.MetaData.Id;

                TextBlock headerT = new TextBlock { Text = "レポート " + repCounter.ToString(), Tag = t, AllowDrop = true };
                headerT.DragEnter += new DragEventHandler(headerT_PreviewDragEnter);
                headerT.MouseLeftButtonUp += new MouseButtonEventHandler(Report_GotFocus);
                t.Header = headerT;

				t.Content = re;
                t.MouseLeftButtonUp += new MouseButtonEventHandler(Report_GotFocus);
				this._tb_XpsDocumentsAndReports.Items.Add(t);
				this._openReports.Add(re);
                repCounter += 1;
			}
		}

        private void ReportTabItem_MouseOver(object sender, MouseEventArgs e)
        {
            if (!(e.Source is TabItem) && !(e.Source is TextBlock))
            {
                this.SetToolBoxState(ToolBoxState.Report);
            }
        }

        private void Report_GotFocus(object sender, RoutedEventArgs e)
        {
            if ((e.Source is TabItem) || (e.Source is TextBlock))
            {
                if (this._tb_XpsDocumentsAndReports.SelectedContent is ReportEditor == false)
                        return;

                this.SetTabShareReportXPS();
                ReportEditor re =
                    this._tb_XpsDocumentsAndReports.SelectedContent as ReportEditor;
                if (re != null)
                {
                    re.SetFocusToInputArea();
                    this.SetToolBoxState(ToolBoxState.Report);
                }
            }
            this._currentWorkingStudy.LastDispplayedReportXPSIndex =
                this._tb_XpsDocumentsAndReports.SelectedIndex;
        }

		private void LoadXpsDocumentsFromStudy(ejpStudy study)
		{
			foreach (ejpXpsDocument xpsD in study.XpsDocuments)
			{
				XpsDocumentViewer dv = new XpsDocumentViewer();
                
                dv.OnDocumentLineDeleted += 
                    new DocumentLineDeleted(xpsDocumentViewer_OnDocumentLineDeleted);

                dv.OnDocumentLineContentsChanged +=
                    new DocumentLineContentsChanged(xpsDocumentViewer_OnDocumentLineContentsChanged);

                dv.OnImageLineColorChanged += 
                    new ImageLineColorChanged(xpsDocumentViewer_OnImageLineColorChanged);

                dv.OnImageLineDeleted += 
                    new ImageLineDeleted(xpsDocumentViewer_OnImageLineDeleted);

                dv.OnQueryDocumentLineConnections += new QueryDocumentLineConnections(dv_OnQueryDocumentLineConnections);

				dv.LoadXpsDocument(xpsD, study.MetaData.Id);
				TabItem t = new TabItem();
                t.Tag = study.MetaData.Id;
				t.Header = xpsD.XpsDocument.CoreDocumentProperties.Title;
				t.Content = dv;
                t.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(XPSDocument_GotFocus);
				this._tb_XpsDocumentsAndReports.Items.Add(t);
				this._openXpsDocumentViewers.Add(dv);
                this._currentXpsDocumentViewer = dv;
                study.LastDispplayedReportXPSIndex = this._tb_XpsDocumentsAndReports.Items.IndexOf(t);
			}
		}

        private void XpsTabItem_MouseOver(object sender, MouseEventArgs e)
        {
            if (!(e.Source is TabItem) && !(e.Source is TextBlock))
            {
                this.SetToolBoxState(ToolBoxState.Document);
            }
        }

        private int dv_OnQueryDocumentLineConnections(Guid LineId, string TargetPathData, Guid documentId)
        {
            int result = 0;
            foreach (KnowledgeMap km in this._openknowledgeMaps)
                result += km.FindConnectedEntities(LineId, TargetPathData, documentId).Count;

            return result;
        }

        private void XPSDocument_GotFocus(object sender, RoutedEventArgs e)
        {
            //Used to update the current xpsdocument viewer even if the 
            //user has not set actual input focus to be in the document area.
            //Useful for letting the user switch tab, change the color of the selected
            //line in that tab and then switch back.
            //080329
            if ((e.Source is TabItem) || (e.Source is TextBlock))
            {
                TabItem ti = null;
                if (e.Source is TabItem)
                    ti = e.Source as TabItem;
                else if (e.Source is TextBlock)
                    ti = ((TextBlock)e.Source).Parent as TabItem;

                //This check is to cover for when the user drags the mouse with the button down,
                //from a report to a DV. The event will fire but the selected content in the 
                //DVR area is still a report.
                //TODO: Set the focus to the selected dv programmatically
                //080404
                XpsDocumentViewer x = 
                    ti.Content as XpsDocumentViewer;
                if (x != null)
                {
                    if (this._tb_XpsDocumentsAndReports.SelectedContent is XpsDocumentViewer == false)
                        return;
                }
                else
                    return;
            }

            if (this._previousToolBoxState != ToolBoxState.Document ||
                this._currentXpsDocumentViewer != (XpsDocumentViewer)this._tb_XpsDocumentsAndReports.SelectedContent)
            {
                if (this._tb_XpsDocumentsAndReports.SelectedContent is XpsDocumentViewer)
                {
                    if ((e.Source is TabItem) || (e.Source is TextBlock))
                    {
                        this.SetTabShareReportXPS();
                    }
                    this.SetToolBoxState(ToolBoxState.Document);

                    this._currentXpsDocumentViewer = (XpsDocumentViewer)this._tb_XpsDocumentsAndReports.SelectedContent;

                    foreach (KnowledgeMap km in this._openknowledgeMaps)
                        km.HasInputFocus = false;

                    foreach (XpsDocumentViewer dv in this._openXpsDocumentViewers)
                        dv.HasInputFocus = true;

                    this._currentWorkingStudy.LastDispplayedReportXPSIndex =
                        this._tb_XpsDocumentsAndReports.SelectedIndex;
                }
            }
        }

        private void xpsDocumentViewer_OnImageLineDeleted(string TargetPathData, Guid lineParentDocumentId)
        {
            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.PropagateImageLineDeleted(TargetPathData, lineParentDocumentId);
            }
            this._mtb_Eraser.IsChecked = false;
        }

        private void xpsDocumentViewer_OnImageLineColorChanged(Color newColor, string TargetPathData)
        {
            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.PropagateImageLineColorChanged(newColor, TargetPathData);
            }
        }

        private void xpsDocumentViewer_OnDocumentLineDeleted(Guid LineId)
        {
            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.PropagateDocumentLineDeleted(LineId);
            }
            this._mtb_Eraser.IsChecked = false;
        }

        private void xpsDocumentViewer_OnDocumentLineContentsChanged(
            SiliconStudio.Meet.EjpControls.Helpers.DragDropQuote Data)
        {
            foreach (KnowledgeMap km in this._openknowledgeMaps)
            {
                km.PropagateDocumentLineContentsChanged(Data);
            }
        }

		#endregion

		#region Global Helpers

        private void SetTabShareKnowledgeMap()
        {
            double w = this._g_MainTabGrid.ActualWidth;
            this._g_MainTabGrid.ColumnDefinitions[0].Width = new GridLength(w*0.35, GridUnitType.Pixel);
        }

        private void SetTabShareReportXPS()
        {
            double w = this._g_MainTabGrid.ActualWidth;
            this._g_MainTabGrid.ColumnDefinitions[0].Width = new GridLength(w*0.65, GridUnitType.Pixel);
        }

		private void ClearUi()
		{
			foreach (KnowledgeMap km in this._openknowledgeMaps)
				km.Release();

			this._openReports.Clear(); //080329
			this._openknowledgeMaps.Clear();
			this._openXpsDocumentViewers.Clear();
			this._tb_KnowledgeMaps.Items.Clear();
			this._tb_XpsDocumentsAndReports.Items.Clear();
            this.UnCheckEntireToolbar();
            this._M_ML_ColorSwatchButton.SetCurrentColor(this._documentAreaMarkerColorsList["赤"]);
            this._M_PL_ColorSwatchButton.SetCurrentColor(this._documentAreaPenColorsList["赤"]);
            this._M_km_ColorSwatchButton.SetCurrentColor(this._kmAreaColorsList["赤"]);
            this._previousKmTool = Tool.Select;

            this.SetToolBoxState(ToolBoxState.None);
			BindingOperations.ClearBinding(this._LB_StudyList, ListBox.ItemsSourceProperty);

            double w = this._g_MainTabGrid.ActualWidth;
            this._g_MainTabGrid.ColumnDefinitions[0].Width = new GridLength(w * 0.5, GridUnitType.Pixel);
		}

        private void UpdateWindowTitle()
        {
            string versionPart = "MEET eJournalPlus 1.0.0 ";
            if (this._currentWorkingAssingment == null)
                this.Title = versionPart + " - ファイル未作成";
            else if (this._currentWorkingAssingment.IsPersisted)
                this.Title = versionPart + this._currentWorkingAssingment.FilePackagePath;
            else
                this.Title = versionPart + "注意：まだ1度も保存されていません";
        }

		private string GetOpenFileName(string extensionMask, string dialogTitle)
		{
			winForms.OpenFileDialog openXpsDialog = new winForms.OpenFileDialog();
			openXpsDialog.Title = dialogTitle;
			openXpsDialog.CheckFileExists = true;
			openXpsDialog.Filter = extensionMask;

			if (openXpsDialog.ShowDialog() == winForms.DialogResult.OK)
				return (openXpsDialog.FileName);
			else
				return "cancel";
		}

        private void SetApplicationState(ApplicationState newState)
        {
            switch (newState)
            {
                case ApplicationState.Cold:
                    //this.ClearUi();
                    this.UnCheckEntireToolbar();
                    this._meet_MainToolBox.IsEnabled = false;
                    this._menuH_Export.IsEnabled = false;
                    this._menuH_Import.IsEnabled = false;
                    this._menuI_SaveAs.IsEnabled = false;
                    this._menuI_Save.IsEnabled = false;
                    this._menuI_Publish.IsEnabled = false;
                    this._menuI_Close.IsEnabled = false;
                    this._menuI_ExportKM.IsEnabled = false;
                    this._menuI_ExportReport.IsEnabled = false;
                    this._menuI_Print.IsEnabled = false;
                    this._menuI_Close.IsEnabled = true;
                    this._menuI_CloseCurrentFile.IsEnabled = false;

#if INTERNAL_BUILD
                    if (App._ejpSettings.IsEjsConfigured)
                        this._menuI_OpenAssignmentOnline.IsEnabled = true;
                    else
                        this._menuI_OpenAssignmentOnline.IsEnabled = false;
					this._menuI_OpenLocalCA.IsEnabled = true;
					this._menuH_Settings.IsEnabled = true;
#endif

#if PUBLIC_BUILD
					this._menuI_OpenAssignmentOnline.IsEnabled = false;
					this._menuI_OpenLocalCA.IsEnabled = true;
					this._menuH_Settings.IsEnabled = false;
					this._menuI_Publish.IsEnabled = false;
#endif
					this._menuI_PublishToEJS.IsEnabled = false;

                    this.ShowMainToolBox();
                    break;
                case ApplicationState.AssignmentLoaded:
                    this._meet_MainToolBox.IsEnabled = true;
                    this._menuH_Import.IsEnabled = true;
                    this._menuI_SaveAs.IsEnabled = true;
                    this._menuH_Export.IsEnabled = true;
                    this._menuI_ExportKM.IsEnabled = true;
                    this._menuI_ExportReport.IsEnabled = true;
                    this._menuI_Save.IsEnabled = true;
                    this._menuI_Publish.IsEnabled = true;
                    this._menuI_Close.IsEnabled = true;
                    this._menuI_Print.IsEnabled = true;
                    this._menuI_PublishToWLS.IsEnabled = true;
                    this._menuI_CloseCurrentFile.IsEnabled = true;

                    this._LB_StudyList.ContextMenu.IsEnabled = true;

#if INTERNAL_BUILD
                    if (App._ejpSettings.IsEjsConfigured)
                        this._menuI_OpenAssignmentOnline.IsEnabled = true;
                    else
                        this._menuI_OpenAssignmentOnline.IsEnabled = false;

					this._menuH_Settings.IsEnabled = true;
                    this._menuI_OpenLocalCA.IsEnabled = true;
                    this._menuI_MergeFromEjs.IsEnabled = true;

                    if (App._ejpSettings.IsEjsConfigured)
                        this._menuI_PublishToEJS.IsEnabled = true;
                    else
                        this._menuI_PublishToEJS.IsEnabled = false;
#endif

#if PUBLIC_BUILD
					this._menuI_OpenAssignmentOnline.IsEnabled = false;
                    this._menuI_OpenLocalCA.IsEnabled = true;
                    this._menuI_PublishToEJS.IsEnabled = false;
					this._menuH_Settings.IsEnabled = false;
					this._menuI_Publish.IsEnabled = true;
                    this._menuI_MergeFromEjs.IsEnabled = false;
#endif

                    //Open all the Documents
                    foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                    {
                        xpsdv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.Draw;
                        xpsdv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.PenLine;
						xpsdv.IsCommentingEnabled = true;
                    }

                    //Open all the reports
                    foreach (ReportEditor repEditor in this._openReports)
                        repEditor.IsEditingLocked = false;

                    //Open all the KMs
                    foreach (KnowledgeMap km in this._openknowledgeMaps)
                        km.SetNormalMode();

                    this.EnableAllStudyContextMenus();
                    
                    this.ShowMainToolBox();
                    break;
                case ApplicationState.ComAssignmentLoaded:
                    this._meet_MainToolBox.IsEnabled = true;
                    this._menuH_Import.IsEnabled = false;
                    this._menuI_SaveAs.IsEnabled = true;
                    this._menuH_Export.IsEnabled = false;
                    this._menuI_ExportKM.IsEnabled = false;
                    this._menuI_ExportReport.IsEnabled = false;
                    this._menuI_Save.IsEnabled = true;
                    this._menuI_Publish.IsEnabled = true;
                    this._menuI_PublishToWLS.IsEnabled = false;
                    this._menuI_Close.IsEnabled = true;
                    this._menuI_Print.IsEnabled = true;
                    this._menuI_CloseCurrentFile.IsEnabled = true;
                    this._LB_StudyList.ContextMenu.IsEnabled = false;

                    //Always Disable for public release:
                    if (App._ejpSettings.IsEjsConfigured)
                        this._menuI_OpenAssignmentOnline.IsEnabled = true;
                    else
                        this._menuI_OpenAssignmentOnline.IsEnabled = false;

                    this._menuI_OpenLocalCA.IsEnabled = true;

                    if (App._ejpSettings.IsEjsConfigured)
                        this._menuI_PublishToEJS.IsEnabled = true;
                    else
                        this._menuI_PublishToEJS.IsEnabled = false;

                    //Lock down all the Documents
                    foreach (XpsDocumentViewer xpsdv in this._openXpsDocumentViewers)
                    {
                        xpsdv.InputMethod = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaInputMehtod.None;
                        xpsdv.DrawingMode = SiliconStudio.Meet.EjpControls.Enumerations.DocumentAreaDrawingMode.None;
						xpsdv.IsCommentingEnabled = false;
                    }

                    //Lock down all the reports
                    foreach (ReportEditor repEditor in this._openReports)
                        repEditor.IsEditingLocked = true;

                    //Lock down all the KMs
                    foreach (KnowledgeMap km in this._openknowledgeMaps)
                        km.SetCommentMode();

                    this.DisableAllStudyContextMenus();

                    this.ShowCommentToolBox();
                    break;
                default:
                    break;
            }
            this._currentAppState = newState;
        }

        private void DisableAllStudyContextMenus()
        {
            // Disable the contextmenu for all the studies in the studylist
            // Getting the currently selected ListBoxItem
            // Note that the ListBox must have
            // IsSynchronizedWithCurrentItem set to True for this to work
            foreach (object study in this._LB_StudyList.Items)
            {
                ListBoxItem myListBoxItem =
                    (ListBoxItem)
                    (this._LB_StudyList.ItemContainerGenerator.ContainerFromItem
                    (study));

                // Getting the ContentPresenter of myListBoxItem
                ContentPresenter cp = FindVisualChild<ContentPresenter>(myListBoxItem);

                // Finding textBlock from the DataTemplate that is set on that ContentPresenter
                DataTemplate template = cp.ContentTemplate;
                ContextMenu cm = (ContextMenu)template.FindName("dt_mi_ContextMenu", cp);
                cm.IsEnabled = false;
            }
        }

        private void EnableAllStudyContextMenus()
        {
            // Enable the contextmenu for all the studies in the studylist
            // Getting the currently selected ListBoxItem
            // Note that the ListBox must have
            // IsSynchronizedWithCurrentItem set to True for this to work
            foreach (object study in this._LB_StudyList.Items)
            {
                ListBoxItem myListBoxItem =
                    (ListBoxItem)
                    (this._LB_StudyList.ItemContainerGenerator.ContainerFromItem
                    (study));

                // Getting the ContentPresenter of myListBoxItem
                ContentPresenter cp = FindVisualChild<ContentPresenter>(myListBoxItem);

                // Finding textBlock from the DataTemplate that is set on that ContentPresenter
                DataTemplate template = cp.ContentTemplate;
                ContextMenu cm = (ContextMenu)template.FindName("dt_mi_ContextMenu", cp);
                cm.IsEnabled = true;
            }
        }

        /// <summary>
        /// Helper method to resolve named elements of a templated item.
        /// http://msdn2.microsoft.com/en-us/library/bb613579.aspx
        /// </summary>
        private childItem FindVisualChild<childItem>(DependencyObject obj)
                    where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        /// <summary>
        /// Get the users consent to switch the application into Commented
        /// Assignment mode.
        /// </summary>
        /// <returns>True if the user gives consent.</returns>
        private bool ReqConsent_SwitchToCommentAMode()
        {
            if (MessageBox.Show(
                    "コメントモードに切り替えます。\n\n宜しいですか？", 
                    "モードが変わる", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Get the users consent to switch the application into Normal
        /// Assignment mode.
        /// </summary>
        /// <returns>True if the user gives consent.</returns>
        private bool ReqConsent_SwitchToNormalAMode()
        {
            if (MessageBox.Show(
                    "普通のモードに切り替えると、\nコメントは全てなくなります！\n\n宜しいですか？",
                    "モードが変わる",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                return true;
            else
                return false;
        }

        private int GetCurrentAssignmentCommentCount()
        {
            int totalCommentCount = 0;
            try
            {
                foreach (KnowledgeMap km in this._openknowledgeMaps)
                    totalCommentCount += km.CommentCount;

                foreach (ReportEditor re in this._openReports)
                    totalCommentCount += re.Comments.Count;
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Error,
						 "eJournalPlus Client",
						 "Failed to Comment Count for the current assignment" +
						 "\nError: " + ex.Message);

                //TODO: something
                throw new ApplicationException("Failed to get the comment count for the current working assignment.");
            }
            return totalCommentCount;
        }

        private void ShowCommentToolBox()
        {
            ThicknessAnimation toolBoxOutAnim = new ThicknessAnimation(
                new Thickness(140, -600, 0, 400), new Duration(new TimeSpan(0, 0, 0, 1)));
            this._meet_MainToolBox.BeginAnimation(ToolBar.MarginProperty, toolBoxOutAnim);

            ThicknessAnimation toolBoxInAnim = new ThicknessAnimation(
                new Thickness(140, 0, 0, 1), new Duration(new TimeSpan(0, 0, 0, 0, 500)));
            this._meet_CommentToolBox.BeginAnimation(ToolBar.MarginProperty, toolBoxInAnim);
        }

        private void ShowMainToolBox()
        {
            ThicknessAnimation toolBoxOutAnim = new ThicknessAnimation(
                new Thickness(140, -600, 0, 400), new Duration(new TimeSpan(0, 0, 0, 1)));
            this._meet_CommentToolBox.BeginAnimation(ToolBar.MarginProperty, toolBoxOutAnim);

            ThicknessAnimation toolBoxInAnim = new ThicknessAnimation(
                new Thickness(140, 0, 0, 1), new Duration(new TimeSpan(0, 0, 0, 0, 500)));
            this._meet_MainToolBox.BeginAnimation(ToolBar.MarginProperty, toolBoxInAnim);
        }

        private void UnCheckEntireToolbar()
        {
            this._mtb_Circle.IsChecked = false;
            this._mtb_Curve.IsChecked = false;
            this._mtb_DoubleArrow.IsChecked = false;
            this._mtb_Eraser.IsChecked = false;
            this._mtb_Line.IsChecked = false;
            this._mtb_Moji.IsChecked = false;
            this._mtb_Note.IsChecked = false;
            this._mtb_Select.IsChecked = false;
            this._mtb_SingleArrow.IsChecked = false;
            this._mtb_Square.IsChecked = false;
        }

        private void SetToolBoxState(ToolBoxState state)
        {
            if (state == this._previousToolBoxState)
                return;
            else
                _previousToolBoxState = state;

            if (this._currentAppState == ApplicationState.AssignmentLoaded)
            {
                switch (state)
                {
                    case ToolBoxState.None:
                        this.UnCheckEntireToolbar();
                        this._meet_MainToolBox.IsEnabled = false;
                        this._meet_CommentToolBox.IsEnabled = false;
                        break;
                    case ToolBoxState.Report:
                        this._meet_MainToolBox.IsEnabled = true;
                        this._meet_CommentToolBox.IsEnabled = false;
                        this._mtb_Circle.IsEnabled = false;
                        this._mtb_Curve.IsEnabled = false;
                        this._mtb_Square.IsEnabled = false;
                        this._mtb_Line.IsEnabled = false;
                        this._mtb_Moji.IsEnabled = false;
                        this._mtb_Moji_L.IsEnabled = false;
                        this._mtb_Moji_S.IsEnabled = false;
                        this._mtb_Note.IsEnabled = false;
                        this._mtb_Select.IsEnabled = false;
                        this._mtb_SingleArrow.IsEnabled = false;
                        this._mtb_DoubleArrow.IsEnabled = false;
                        this._mtb_Eraser.IsEnabled = false;
                        this._mtb_Eraser.IsChecked = false;
                        this._M_ML_ColorSwatchButton.IsEnabled = false;
                        this._M_PL_ColorSwatchButton.IsEnabled = false;
                        this._M_km_ColorSwatchButton.IsEnabled = false;
                        this._mtb_Redo.IsEnabled = true;
                        this._mtb_Undo.IsEnabled = true;
                        break;
                    case ToolBoxState.KnowledgeMap:
                        this._meet_MainToolBox.IsEnabled = true;
                        this._meet_CommentToolBox.IsEnabled = false;
                        this._mtb_Circle.IsEnabled = true;
                        this._mtb_Curve.IsEnabled = true;
                        this._mtb_Square.IsEnabled = true;
                        this._mtb_Line.IsEnabled = true;
                        this._mtb_Moji.IsEnabled = true;
                        this._mtb_Note.IsEnabled = true;
                        this._mtb_Select.IsEnabled = true;
                        this._mtb_Moji_L.IsEnabled = true;
                        this._mtb_Moji_S.IsEnabled = true;
                        this._mtb_SingleArrow.IsEnabled = true;
                        this._mtb_DoubleArrow.IsEnabled = true;
                        this._mtb_Eraser.IsEnabled = true;
                        this._mtb_Eraser.IsChecked = false;
                        this._M_ML_ColorSwatchButton.IsEnabled = false;
                        this._M_PL_ColorSwatchButton.IsEnabled = false;
                        this._M_km_ColorSwatchButton.IsEnabled = true;
                        this._mtb_Redo.IsEnabled = true;
                        this._mtb_Undo.IsEnabled = true;
                        break;
                    case ToolBoxState.Document:
                        this._meet_MainToolBox.IsEnabled = true;
                        this._meet_CommentToolBox.IsEnabled = false;
                        this._mtb_Circle.IsEnabled = false;
                        this._mtb_Curve.IsEnabled = false;
                        this._mtb_Square.IsEnabled = false;
                        this._mtb_Line.IsEnabled = false;
                        this._mtb_Moji.IsEnabled = false;
                        this._mtb_Moji_L.IsEnabled = false;
                        this._mtb_Moji_S.IsEnabled = false;
                        this._mtb_Note.IsEnabled = false;
                        this._mtb_Select.IsEnabled = false;
                        this._mtb_SingleArrow.IsEnabled = false;
                        this._mtb_DoubleArrow.IsEnabled = false;
                        this._mtb_Eraser.IsEnabled = true;
                        this._mtb_Eraser.IsChecked = false;
                        this._M_ML_ColorSwatchButton.IsEnabled = true;
                        this._M_PL_ColorSwatchButton.IsEnabled = true;
                        this._M_km_ColorSwatchButton.IsEnabled = false;
                        this._mtb_Redo.IsEnabled = true;
                        this._mtb_Undo.IsEnabled = true;
                        break;
                    default:
                        break;
                }
            }
            else if (this._currentAppState == ApplicationState.ComAssignmentLoaded)
            {
                switch (state)
                {
                    case ToolBoxState.None:
                        this.UnCheckEntireToolbar();
                        this._meet_MainToolBox.IsEnabled = false;
                        this._meet_CommentToolBox.IsEnabled = false;
                        break;
                    case ToolBoxState.Report:
                        this._meet_MainToolBox.IsEnabled = false;
                        this._meet_CommentToolBox.IsEnabled = true;
                        break;
                    case ToolBoxState.KnowledgeMap:
                        this._meet_MainToolBox.IsEnabled = false;
                        this._meet_CommentToolBox.IsEnabled = true;

                        //this._mtb_Select.IsEnabled = true;
                        
                        //this._mtb_SingleArrow.IsEnabled = true;
                        //this._mtb_DoubleArrow.IsEnabled = true;
                        //this._mtb_Eraser.IsEnabled = true;
                        //this._mtb_Eraser.IsChecked = false;
                        
                        break;
                    case ToolBoxState.Document:
                        this._meet_MainToolBox.IsEnabled = false;
                        this._meet_CommentToolBox.IsEnabled = false;
                        break;
                    default:
                        break;
                }
            }
        }

        private void SetTool(Tool tool)
        {
            switch (tool)
            {
                case Tool.Eraser:
                    this._mtb_Eraser.IsChecked = true;
                    break;
                case Tool.Label:
                    this._mtb_Moji.IsChecked = true;
                    break;
                case Tool.Freehand:
                    this._mtb_Curve.IsChecked = true;
                    break;
                case Tool.Line:
                    this._mtb_Line.IsChecked = true;
                    break;
                case Tool.SingleArrow:
                    this._mtb_SingleArrow.IsChecked = true;
                    break;
                case Tool.DoubleArrow:
                    this._mtb_DoubleArrow.IsChecked = true;
                    break;
                case Tool.Square:
                    this._mtb_Square.IsChecked = true;
                    break;
                case Tool.Circle:
                    this._mtb_Circle.IsChecked = true;
                    break;
                case Tool.Note:
                    this._mtb_Note.IsChecked = true;
                    break;
                case Tool.Select:
                    this._mtb_Select.IsChecked = true;
                    break;
                default:
                    break;
            }
        }

		private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.topmenuItem.IsSubmenuOpen = true;
		}

        private void OnRemoveStudyFromAssignment(object sender, RoutedEventArgs e)
        {
            if (
                MessageBox.Show("スタッディを削除しますか？",
                "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                this._currentWorkingAssingment.RemoveStudy(
                    ((ejpStudy)this._LB_StudyList.SelectedItem).MetaData.Id);
                this._LB_StudyList.SelectedIndex = 0;
            }
        }

        private void OnRenameStudy(object sender, RoutedEventArgs e)
        {
            Grid g = sender as Grid;
            if (g != null)
            {
                try
                {
                    Guid guid = new Guid((string)g.Tag);
                    foreach (ejpStudy std in this._LB_StudyList.Items)
                    {
                        if (std.MetaData.Id == guid)
                        {
                            TextBox tb = (TextBox)g.FindName("dt_StudyNameTB");
                            tb.Visibility = Visibility.Visible;
                        }
                    }
                }
                catch { }
            }
        }

        private ScrollViewer GetScrollViewer()
        {
            Border scBorder = VisualTreeHelper.GetChild(this._LB_StudyList, 0) as Border;
            if (scBorder is Border)
            {
                ScrollViewer sv = scBorder.Child as ScrollViewer;
                if (sv is ScrollViewer)
                    return sv;
                else
                    return null;
            }
            else
                return null;
        }

        private void OnScrollStudyListRight(object sender, RoutedEventArgs e)
        {
            ScrollViewer sv = GetScrollViewer();
            if (sv != null)
                sv.LineRight();
        }

        private void OnScrollStudyListLeft(object sender, RoutedEventArgs e)
        {
            ScrollViewer sv = GetScrollViewer();
            if (sv != null)
                sv.LineLeft();
        }

        private void _LB_StudyList_DragOver(object sender, DragEventArgs e)
        {
            Grid g = sender as Grid;
            if (g != null)
            {
                try
                {
                    Guid guid = new Guid((string)g.Tag);
                    this.DisplayStudy(guid);

                    foreach (ejpStudy std in this._LB_StudyList.Items)
                    {
                        if (std.MetaData.Id == guid)
                        {
                            this._currentWorkingStudy = std;

                            this._LB_StudyList.SelectedIndex = 
                                this._LB_StudyList.Items.IndexOf(std);
                        }
                    }
                }
                catch{}
            }
        }

        #endregion

		private void _LB_StudyList_LayoutUpdated(object sender, EventArgs e)
		{
			if (this._LB_StudyList.Items != null && 
				this._LB_StudyList.Items.Count != 0)
			{
				//Left Button
				if (this.IsStudyVisibleInList(0) == true)
					this._ScrollStudyLeft.Visibility = Visibility.Collapsed;
				else
					this._ScrollStudyLeft.Visibility = Visibility.Visible;

				////Right Button
				if (this.IsStudyVisibleInList(this._LB_StudyList.Items.Count - 1) == true)
				    this._ScrollStudyRight.Visibility = Visibility.Collapsed;
				else
				    this._ScrollStudyRight.Visibility = Visibility.Visible;
			}
		}

        /// <summary>
        /// Test for item visibility in the studylist. 
        /// An item is considered visible if the item falls within the visible bounds of the 
        /// listbox. That is, the item can be seen within the Ui.
        /// </summary>
		public bool IsStudyVisibleInList(int index)
		{
			if (this._LB_StudyList.Items == null || 
				this._LB_StudyList.Items.Count == 0)
				return false;

			ListBoxItem item = this._LB_StudyList.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
			VirtualizingStackPanel vPanel = VisualTreeHelper.GetParent(item) as VirtualizingStackPanel;
			int FirstVI = (int)vPanel.HorizontalOffset;
			int VICount = (int)vPanel.ViewportWidth;
			if (index >= FirstVI && index <= FirstVI + VICount)
				return true;

			return false;

		}

        #region EJS Related methods

        #endregion
    }
}