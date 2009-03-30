using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace SiliconStudio.Meet.EjpControls
{
    public delegate void DeleteKnowledgeMapComment(KnowledgeMapComment comment);
    public delegate void KnowledgeMapCommentViewStateChanged(KnowledgeMapComment comment);

    public enum KnowledgeMapCommentViewState
    {
        Minimized,
        Maximized
    }

    /// <summary>
    /// Interaction logic for KnowledgeMapComment.xaml
    /// </summary>
    public partial class KnowledgeMapComment : UserControl
    {
        public event DeleteKnowledgeMapComment OnDeleteComment;
        public event KnowledgeMapCommentViewStateChanged OnMinimizeComment;
        public event KnowledgeMapCommentViewStateChanged OnMaximizeComment;

        ObservableCollection<CommentMessage>
            _messages = new ObservableCollection<CommentMessage>();

        public ObservableCollection<CommentMessage> Messages
        {
            get { return _messages; }
            set { _messages = value; }
        }

        public bool IsDragging { get; set; }

        public Rectangle BackgroundRectangle
        {
            get { return this._r_BG; }
        }

        private KnowledgeMapCommentViewState _currentViewState;
        public KnowledgeMapCommentViewState CurrentViewState
        {
            get { return _currentViewState; }
            set 
            {
                switch (value)
                {
                    case KnowledgeMapCommentViewState.Minimized:
                        this.MinimizeComment();
                        break;
                    case KnowledgeMapCommentViewState.Maximized:
                        this.MaximizeComment();
                        break;
                    default:
                        break;
                }
                _currentViewState = value; 
            }
        }

        public string OriginalAuthorName { get; set; }

        /// <summary>
        /// Original Creater of this comment tree
        /// </summary>
        private Guid _originalAuthorId;
        public Guid OriginalAuthorId
        {
            get { return _originalAuthorId; }
            set
            {
                _originalAuthorId = value;
                if (this._currentAuthorId != value)
                    this._b_Close.Visibility = Visibility.Collapsed;
                else
                    this._b_Close.Visibility = Visibility.Visible;
            }
        }

        private Guid _currentAuthorId;
        public Guid CurrentAuthorId
        {
            get { return _currentAuthorId; }
            set 
            { 
                _currentAuthorId = value;
                if (this._originalAuthorId != value)
                    this._b_Close.Visibility = Visibility.Collapsed;
                else
                    this._b_Close.Visibility = Visibility.Visible;
            }
        }


        public string CurrentAuthorName { get; set; }
        public Guid CommentId { get; set; }
        public Point Coordinates { get; set; }
        public Point OriginalCoordinates { get; set; }
        public string CommentTextInDocument { get; set; }

        private bool _isAnchorPointVisible;
        public bool IsAnchorPointVisible
        {
            get { return this._isAnchorPointVisible; }
            set
            {
                this._isAnchorPointVisible = value;
                this.UpdateAnchorVisibility();
            }
        }

        public KnowledgeMapComment()
        {
            InitializeComponent();
            this._currentViewState = KnowledgeMapCommentViewState.Maximized;
            this._lb_Messages.ItemsSource = this._messages;
        }

        private void MaximizeComment()
        {
            try
            {
                if (this._currentViewState == KnowledgeMapCommentViewState.Minimized)
                {
                    this.SetValue(Canvas.LeftProperty, (double)this.GetValue(Canvas.LeftProperty) - (175));
                    this.Width = 200;
                    this.Height = 300;
                    this._currentViewState = KnowledgeMapCommentViewState.Maximized;
                    if (this.OnMaximizeComment != null)
                        this.OnMaximizeComment(this);
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
							SiliconStudio.DebugManagers.MessageType.Error,
							"EjpControls - Knowledge Map Comment",
							"Maximize Failed" +
							"\nComment ID: " + this.CommentId.ToString() +
							"\nError: " + ex.Message);

                this.Height = 200;
            }
        }

        private void MinimizeComment()
        {
            this.SetValue(Canvas.LeftProperty, (double)this.GetValue(Canvas.LeftProperty) + (this.ActualWidth - 25));

            this.Height = 25;
            this.Width = 25;

            this._currentViewState = KnowledgeMapCommentViewState.Minimized;
            if (this.OnMinimizeComment != null)
                this.OnMinimizeComment(this);
        }

        private void UpdateAnchorVisibility()
        {
            if (this._isAnchorPointVisible)
            {
            }
            else
            {
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
           // e.Handled = true;
        }

        private void OnStylusUp(object sender, StylusEventArgs e)
        {
           // e.Handled = true;
        }

        private void _b_Enter_Click(object sender, RoutedEventArgs e)
        {
            if (this._tb_Message.Text.Length != 0)
            {
                CommentMessage c = new CommentMessage()
                {
                    Author = this.CurrentAuthorName,
                    Date = DateTime.Now,
                    Message = this._tb_Message.Text
                };

                this._messages.Add(c);
                this._tb_Message.Text = "";
            }
        }

        private void _b_Clear_Click(object sender, RoutedEventArgs e)
        {
            this._tb_Message.Text = "";
        }

        private void _b_Close_Click(object sender, RoutedEventArgs e)
        {
            this.DeleteComment();
        }

        private void _b_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.MinimizeComment();
        }

        private void _r_PushPin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.IsDragging == true)
                return;

            switch (this._currentViewState)
            {
                case KnowledgeMapCommentViewState.Minimized:
                    this.MaximizeComment();
                    break;
                case KnowledgeMapCommentViewState.Maximized:
                    this.MinimizeComment();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the ContextMenu for the PushPin.
        /// </summary>
        private void OnPushPinMenuOpen(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = sender as ContextMenu;

            if (this.OriginalAuthorId == this.CurrentAuthorId)
            {
                this._ppCMenu_RemoveComment.IsEnabled = true;
                this._ppCMenu_ReturnPosition.IsEnabled = true;
            }
            else
            {
                this._ppCMenu_RemoveComment.IsEnabled = false;
                this._ppCMenu_ReturnPosition.IsEnabled = false;
            }
        }

        private void OnReturnToOriginalPosition(object sender, RoutedEventArgs e)
        {
            this.SetValue(Canvas.LeftProperty, this.OriginalCoordinates.X);
            this.SetValue(Canvas.TopProperty, this.OriginalCoordinates.Y);
        }

        private void OnRemoveComment(object sender, RoutedEventArgs e)
        {
            this.DeleteComment();
        }

        /// <summary>
        /// Fire the events to delete this comment.
        /// </summary>
        private void DeleteComment()
        {
            if (this.OriginalAuthorId == this.CurrentAuthorId)
            {
                if (this.OnDeleteComment != null)
                    this.OnDeleteComment(this);
            }
        }
    }

    public class CommentMessage
    {
        public DateTime Date { get; set; }
        public String Message { get; set; }
        public String Author { get; set; }
        public Guid AuthorId { get; set; }
    }
}
