using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using SiliconStudio.Meet.EjpControls.Helpers;

namespace SiliconStudio.Meet.EjpControls
{
    public delegate void DocumentLineNoteRequestedClose(DocumentLineCommentNote sender, DocumentLine ParentDocumentLine);


    /// <summary>
    /// Interaction logic for DocumentLineCommentNote.xaml
    /// </summary>
    public partial class DocumentLineCommentNote : UserControl
    {
        public event DocumentLineNoteRequestedClose OnClosing;

        private DocumentLine _documentLineParent;
        public DocumentLine DocumentLineParent
        {
            get { return _documentLineParent; }
            set { _documentLineParent = value; }
        }

        private bool _isResizing;
        private Point _previousRecMPoint;

        private bool _isMoving;
        public bool IsMoving
        {
            get { return _isMoving; }
            set { _isMoving = value; }
        }

        public Point InitialMoveOffset { get; set; }

        public DocumentLineCommentNote()
        {
            InitializeComponent();
            this._tb_NoteArea.TextChanged += new TextChangedEventHandler(_tb_NoteArea_TextChanged);
            this._r_ResizeGhost.MouseLeftButtonDown += new MouseButtonEventHandler(_g_ResizeGrid_MouseLeftButtonDown);
            this._r_MoveGhost.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_r_MoveGhost_PreviewMouseLeftButtonDown);
            this._r_CloseGhost.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_r_CloseGhost_PreviewMouseLeftButtonUp);
        }

        private void _r_CloseGhost_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void _r_MoveGhost_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.Print("Moving");
            this._isMoving = true;
            this.InitialMoveOffset = e.GetPosition(this);
        }

        private void _g_ResizeGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this._isResizing = true;
            this._previousRecMPoint = e.GetPosition(this._g_ResizeGrid);
        }

        private void _tb_NoteArea_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this._documentLineParent != null)
            {
                this._documentLineParent.Comment.Content = this._tb_NoteArea.Text;
            }
        }

        public void Close()
        {
            this._documentLineParent.Comment.Content = this._tb_NoteArea.Text;
            this._documentLineParent.IsCommentVisualOpen = false;

            Panel p = this.Parent as Panel;

            if (p == null)
            {
                FixedPage fp = this.Parent as FixedPage;
                fp.Children.Remove(this);
            }
            else
                p.Children.Remove(this);

            if (this.OnClosing != null)
                this.OnClosing(this, this._documentLineParent);
        }

    }
}
