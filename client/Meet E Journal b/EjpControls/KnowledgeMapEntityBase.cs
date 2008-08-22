using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using SiliconStudio.Meet.EjpControls.Enumerations;

namespace SiliconStudio.Meet.EjpControls
{
	/// <summary>
	/// Interaction logic for KnowledgeMapEntityBase.xaml
	/// </summary>

    [Serializable]
    public abstract class KnowledgeMapEntityBase : System.Windows.Controls.UserControl
    {
        protected Guid _id;

        public Guid DragSourceId;

		/// <summary>
		/// Used only to lock the current object for
		/// thread safety.
		/// </summary>
		protected object _locker = new object();

        protected KnowledgeMapEntityCommentNote _commentNote;
        protected string _commentText;
        private bool _isCommentNoteVisible;
        public bool IsCommentNoteVisible
        {
            get { return (this._commentNote != null) ? this._isCommentNoteVisible : false; }
            set { _isCommentNoteVisible = value; }
        }

        protected Stroke _commentConnectorStroke;
        public Stroke CommentConnectorStroke
        {
            get { return _commentConnectorStroke; }
            set { _commentConnectorStroke = value; }
        }

        protected bool _expanded;
        protected double _previousHeight;
        protected List<Rect> _connectableRegions;
        protected List<UIElement> _currentlyDisplayedAnchorPoints;

        protected List<KnowledgeMapConnectedLine> _connectedLines;

        protected XpsDocumentReference _sourceReference;
        public XpsDocumentReference SourceReference
        {
            get { return _sourceReference; }
            set { _sourceReference = value; }
        }

        protected KnowledgeMapEntityType _entityType;
        public KnowledgeMapEntityType EntityType
        {
            get { return _entityType; }
            set { _entityType = value; }
        }

        public List<Rect> GetConnectableRegions(int Margin)
        {
            if (this._connectableRegions != null)
                this._connectableRegions.Clear();
            else
                this._connectableRegions = new List<Rect>();

            double centerHeight = this.ActualHeight * 0.5;
            double centerWidth = this.ActualWidth * 0.5;

            //Center
            this._connectableRegions.Add(
                new Rect(
                centerWidth - Margin,
                centerHeight - Margin,
                Margin * 2,
                Margin * 2
                )
            );

            #region Not Used Regions
            ////TopMiddle
            //this._connectableRegions.Add(
            //    new Rect(
            //        centerWidth - Margin,
            //        0,
            //        Margin * 2,
            //        Margin)
            //);

            ////RightMiddle
            //this._connectableRegions.Add(
            //    new Rect(
            //        this.ActualWidth - Margin,
            //        centerHeight - Margin,
            //        Margin,
            //        Margin * 2)
            //);

            ////BottomMiddle
            //this._connectableRegions.Add(
            //    new Rect(
            //        centerWidth - Margin,
            //        this.ActualHeight - Margin,
            //        Margin * 2,
            //        Margin)
            //);

            ////LeftMiddle
            //this._connectableRegions.Add(
            //    new Rect(
            //        0,
            //        centerHeight - Margin,
            //        Margin,
            //        Margin * 2)
            //);
            #endregion

            return this._connectableRegions;
        }

        public Rect GetBounds(int Margin)
        {
            Point p1 = new Point(
                (double)this.GetValue(InkCanvas.LeftProperty) - Margin,
                (double)this.GetValue(InkCanvas.TopProperty) - Margin
                );

            Point p2 = new Point(
                p1.X + (double)this.GetValue(FrameworkElement.ActualWidthProperty) + (Margin * 2),
                p1.Y + (double)this.GetValue(FrameworkElement.ActualHeightProperty) + (Margin * 2)
                );

            return new Rect(p1, p2);
        }

        public abstract void SetEntityColor(SolidColorBrush newColor);

        public abstract Color GetEntityColor();

        public abstract void DisplayAnchorPoint(Rect location);

        public abstract void HideAnchorPoints();

        public abstract void ShowCommentNote(object sender, RoutedEventArgs e);

        public abstract void DrawCommentConnectorLine();

		public abstract void Release();

        public Guid Id
        {
            get { return this._id; }
        }

        public void UpdateAllConnectedLines()
        {
            foreach (KnowledgeMapConnectedLine cl in this._connectedLines)
                cl.Update();
        }

        public void AddConnectedStrokes(KnowledgeMapConnectedLine newLine)
        {
            if (this._connectedLines == null)
                this._connectedLines = new List<KnowledgeMapConnectedLine>();

            this._connectedLines.Add(newLine);
            this.UpdateAllConnectedLines();
        }

        public void RemoveConnectedStrokes(KnowledgeMapConnectedLine line)
        {
            if (this._connectedLines != null)
                this._connectedLines.Remove(line);
            this.UpdateAllConnectedLines();
        }

        public KnowledgeMapEntityBase()
        {
        }
    }
}