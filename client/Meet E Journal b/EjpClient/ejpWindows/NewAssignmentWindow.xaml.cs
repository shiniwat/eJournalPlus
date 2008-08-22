//#define INTERNAL_BUILD
#define PUBLIC_BUILD

using System;
using System.IO;
using System.Windows;
using winForms = System.Windows.Forms;

namespace ejpClient.ejpWindows
{
    public enum CreateAssignmentStartLocation
    {
        NotSet,
        None,
        Local,
        EJournalServer
    }

    /// <summary>
    /// Interaction logic for NewAssignmentWindow.xaml
    /// </summary>
    public partial class NewAssignmentWindow : Window
    {
        /// <summary>
        /// Tells from where the first XPS document of the
        /// first study is loaded from.
        /// </summary>
        private CreateAssignmentStartLocation _createLocation 
            = CreateAssignmentStartLocation.NotSet;
        public CreateAssignmentStartLocation CreateLocation
        {
            get { return _createLocation; }
            set { _createLocation = value; }
        }

        /// <summary>
        /// Even if the document is coming from the
        /// E Journal Server, it will still be downloaded
        /// to the local disk before it is added to the application.
        /// </summary>
        private string _firstDocumentLocalPath;
        public string FirstDocumentLocalPath
        {
            get { return _firstDocumentLocalPath; }
            set { _firstDocumentLocalPath = value; }
        }

        /// <summary>
        /// Sets the title of the Window and the main Headline
        /// </summary>
        public string WindowHeadline
        {
            get { return this._l_WindowTitle.Text; }
            set
            {
                this._l_WindowTitle.Text = value;
                this.Title = value;
            }
        }

        /// <summary>
        /// When the window is closing, if the createLocation is set to Ejs,
        /// this is the document that the app needs to download from the Ejs.
        /// </summary>
        private EjsBridge.ejsService.ejsCourseDocument _ejsDocumentToDownload;
        public EjsBridge.ejsService.ejsCourseDocument EjsDocumentToDownload
        {
            get { return _ejsDocumentToDownload; }
            set { _ejsDocumentToDownload = value; }
        }

        public NewAssignmentWindow()
        {
            InitializeComponent();
            
            if (App._ejpSettings.IsEjsConfigured == false)
            {
                this._rb_DocLocEjs.IsEnabled = false;
            }

            
#if PUBLIC_BUILD
            this._rb_DocLocEjs.IsEnabled = false;
#endif
#if INTERNAL_BUILD
			this._rb_DocLocEjs.IsEnabled = true;
#endif

        }

        private void On_RbDocLocNoneChecked(object sender, RoutedEventArgs e)
        {
            this._l_FirstXpsName.Text = "なし";
            this._createLocation = CreateAssignmentStartLocation.None;
            this.EnableDisableOKButton();
        }

        private void On_RbDocLocLocalChecked(object sender, RoutedEventArgs e)
        {
            string path = this.GetOpenFileName("XPS Documents (*.xps)|*.xps", "XPSドキュメントを選択");
            if (path != "cancel")
            {
                this._b_Ok.IsEnabled = true;
                FileInfo f = new FileInfo(path);
                this._l_FirstXpsName.Text = f.Name;
                this._firstDocumentLocalPath = path;
                this._createLocation = CreateAssignmentStartLocation.Local;
            }
            else
            {
                this._rb_DocLocLocal.IsChecked = false;
                this._firstDocumentLocalPath = "";
                this.EnableDisableOKButton();
                return;
            }
        }

        private void On_RbDocLocEjsChecked(object sender, RoutedEventArgs e)
        {

            ejpWindows.EjsCourseDocumentSelectWindow docSelectWindow =
                new EjsCourseDocumentSelectWindow();

            docSelectWindow.Closing += delegate(object ws, System.ComponentModel.CancelEventArgs we)
            {
                if (docSelectWindow.Cancelled == false && docSelectWindow.SelectedDocument != null)
                {
					string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
					baseDir += @"\Meet\eJournalPlus\";
                    string path = baseDir +
                                  @"TemporaryFiles\DownloadedFiles\" +
                                  Guid.NewGuid().ToString() + ".xps";

                    this._firstDocumentLocalPath = path;
                    this._createLocation = CreateAssignmentStartLocation.EJournalServer;
                    this._l_FirstXpsName.Text = docSelectWindow.SelectedDocument._name;
                    this._ejsDocumentToDownload = docSelectWindow.SelectedDocument;
                }
                else
                {
                    this._rb_DocLocEjs.IsChecked = false;
                    this._createLocation = CreateAssignmentStartLocation.NotSet;
                }
            };

            docSelectWindow.ShowDialog();

            this._createLocation = CreateAssignmentStartLocation.EJournalServer;
            this.EnableDisableOKButton();
        }

        private void On_BtnOKClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this._createLocation == CreateAssignmentStartLocation.EJournalServer)
                {
                    this.DownloadEJSCourseDocument_Async(this._firstDocumentLocalPath,
                        this._ejsDocumentToDownload);
                }
                this.Close();
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					 SiliconStudio.DebugManagers.MessageType.Error,
					 "eJournalPlus Client - New Assignment Window",
					 "Creating new Assignment Failed" +
					 "\nError: " + ex.Message);

                throw new ApplicationException("Could not Create new Assignment from the given document.");
            }
        }

        private void On_BtnCancelClick(object sender, RoutedEventArgs e)
        {
            this._createLocation = CreateAssignmentStartLocation.NotSet;
            this.Close();
        }

        private void EnableDisableOKButton()
        {
            if ((bool)this._rb_DocLocEjs.IsChecked
                || (bool)this._rb_DocLocNone.IsChecked
                || (bool)this._rb_DocLocLocal.IsChecked)
                this._b_Ok.IsEnabled = true;
            else
            {
                this._l_FirstXpsName.Text = "ドキュメントを選択して下さい。。。";
                this._createLocation = CreateAssignmentStartLocation.NotSet;
                this._b_Ok.IsEnabled = false;
            }
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

        private void DownloadEJSCourseDocument_Async(string path, EjsBridge.ejsService.ejsCourseDocument document)
        {
            //BackgroundWorker bgw = new BackgroundWorker();
            EjsBridge.ejsBridgeManager.DownloadCourseDocument(
                       App._currentEjpStudent.SessionToken, path,
                       document);
        }

        public void DisableNoSelect()
        {
            this._rb_DocLocNone.IsEnabled = false;
        }
    }
}
