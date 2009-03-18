using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using SiliconStudio.Meet.EjsManager.ejsServiceReference;
using SiliconStudio.Meet.EjsManager.ServiceOperations;
using System.ComponentModel;
using winForms = System.Windows.Forms;

namespace SiliconStudio.Meet.EjsManager
{
    /// <summary>
    /// Interaction logic for AddNewCourseDocument.xaml
    /// </summary>
    public partial class AddNewCourseDocumentWindow : AddNewItemWindow
    {
        public string _pathToDocumentFile = "";

        public AddNewCourseDocumentWindow(ejsSessionToken userEjsToken, ejsManagerStage parentStage)
            : base(userEjsToken, parentStage)
        {
            InitializeComponent();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.CancelOperation();
        }

        private void OnAddNewItem(object sender, RoutedEventArgs e)
        {
            this.AddNewItem();
        }

        protected override void AddNewItem()
        {
            if (StringValidation.ValidSqlInputVariable(this._tb_Title.Text)
            || StringValidation.ValidSqlInputVariable(this._tb_Description.Text)
                || StringValidation.ValidSqlInputVariable(this._pathToDocumentFile))
                return;
            else
            {
                try
                {

                    if (this._tb_Title.Text.Length == 0 ||
                        this._tb_Description.Text.Length == 0 ||
                        this._pathToDocumentFile.Length == 0)
                        return;

                    string title = this._tb_Title.Text;
                    string description = this._tb_Description.Text;
                    bool isActive = (bool)this._cb_IsAvailable.IsChecked;
                    int courseId = ((ejsCourse)this._cb_CourseList.SelectedItem)._id;

                    FileStream fs = new FileStream(this._pathToDocumentFile, FileMode.Open, FileAccess.Read);
                    BinaryReader br = new BinaryReader(fs);
                    long fileSize = fs.Length;
                    byte[] data = br.ReadBytes((int)fs.Length);
                    br.Close();
                    fs.Close();
                    fs.Dispose();

                    ejsCourseDocument document = new ejsCourseDocument();
                    document._name = title;
                    document._byteSize = data.Length;
                    document._isAvailable = isActive;
                    document._creationDate = DateTime.Now;
                    document._description = description;
                    document._documentId = Guid.NewGuid();

                    BackgroundWorker bgw = new BackgroundWorker();
                    bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(OperationCompleted);
                    bgw.WorkerSupportsCancellation = true;
                    bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
                    {
                        try
                        {
                            ejsBridgeManager.AddDocumentToCourse(this._currentUserToken, document, courseId, data);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            e.Cancel = true;
                        }
                    };

                    bgw.RunWorkerAsync();

                    this._parentStage.RaiseAsyncOperationStartedEvent("Uploading and Saving Course Document.");
                    
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private void OperationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this._parentStage.RaiseAsyncOperationCompletedEvent();
			if (e.Cancelled == false)
			{
				this.NeedsUpdate = true;
				this.Close();
			}
        }

        private void OnBrowseForFile(object sender, RoutedEventArgs e)
        {
            string path = this.GetOpenFileName("XPS Documents (*.xps)|*.xps", "XPS Document");
            if (path != "cancel")
            {
                FileInfo f = new FileInfo(path);
                this._l_CurrentFileName.Content = f.Name;
                this._pathToDocumentFile = path;
            }
            else
            {
                return;
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
    }
}
