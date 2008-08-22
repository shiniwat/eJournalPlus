using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SiliconStudio.Meet.EjpControls
{
    /// <summary>
    /// Interaction logic for KnowledgeMapEntityComment.xaml
    /// </summary>
    public partial class KnowledgeMapEntityCommentNote : UserControl
    {
        private string _commentText = "";
        public string CommentText
        {
            get { return this._tb_NoteArea.Text; }
            set { this._tb_NoteArea.Text = value; }
        }

        private KnowledgeMapEntityBase _parentEntity;
        public KnowledgeMapEntityBase ParentEntity
        {
            get { return _parentEntity; }
            set { _parentEntity = value; }
        }

        public KnowledgeMapEntityCommentNote()
        {
            InitializeComponent();
            this._r_CloseGhost.MouseLeftButtonUp += new MouseButtonEventHandler(CloseGhost_MouseLeftButtonUp);
        }

        private void CloseGhost_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            this.ParentEntity.DrawCommentConnectorLine();
        }
    }
}
