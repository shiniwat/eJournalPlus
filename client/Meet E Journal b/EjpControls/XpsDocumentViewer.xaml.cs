using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Annotations.Storage;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SiliconStudio.Meet.EjpControls.Enumerations;
using SiliconStudio.Meet.EjpControls.Helpers;
using SiliconStudio.Meet.EjpLib.Enumerations;

namespace SiliconStudio.Meet.EjpControls
{
    public delegate void DocumentLineContentsChanged(Helpers.DragDropQuote Data);
    public delegate void DocumentLineDeleted(Guid LineId);

    public delegate void ImageLineDeleted(string TargetLineData, Guid LineParentDocumentId);
    public delegate void ImageLineColorChanged(Color newColor, string TargetPathData);

    public delegate int QueryDocumentLineConnections(Guid LineId, string TargetPathData, Guid documentId);

	/// <summary>
	/// Interaction logic for XpsDocumentViewer.xaml
	/// </summary>
	public partial class XpsDocumentViewer : System.Windows.Controls.UserControl
	{

        public event DocumentLineContentsChanged OnDocumentLineContentsChanged;
        public event DocumentLineDeleted OnDocumentLineDeleted;

        public event ImageLineColorChanged OnImageLineColorChanged;
        public event ImageLineDeleted OnImageLineDeleted;

        public event QueryDocumentLineConnections OnQueryDocumentLineConnections;

        private bool _hasInputFocus = false;
        public bool HasInputFocus
        {
            get { return _hasInputFocus; }
            set { _hasInputFocus = value; }
        }

        private List<Border> _imageBorders = 
            new List<Border>();
        public List<Border> ImageBorders
        {
            get { return _imageBorders; }
            set { _imageBorders = value; }
        }

        private Guid _parentStudyId;
        public Guid ParentStudyId
        {
            get { return _parentStudyId; }
            set { _parentStudyId = value; }
        }

        /// <summary>
        /// The base document object from the loaded assignment package.
        /// </summary>
        private EjpLib.BaseClasses.ejpXpsDocument _document;
        public EjpLib.BaseClasses.ejpXpsDocument Document
        {
            get { return _document; }
        }

		private Stream _annotationStream = null;
		private AnnotationService _annotationService;

		private DocumentAreaInputMehtod _inputMethod;
		public DocumentAreaInputMehtod InputMethod
		{
			get { return _inputMethod; }
			set
            { 
                _inputMethod = value;
                this.SetInputMethod(value);
            }
		}

		private DocumentAreaDrawingMode _drawingMode;
		public DocumentAreaDrawingMode DrawingMode
		{
			get { return _drawingMode; }
			set { _drawingMode = value; }
		}

        /// <summary>
        /// Holds all the glyphs in the currently lodade document.
        /// </summary>
        private List<Glyphs> _currentDocumentGlyphs = 
            new List<Glyphs>();

        /// <summary>
        /// Holds a list of all the Paths in this document
        /// that can be decorated with Borders.
        /// </summary>
        private List<System.Windows.Shapes.Path> _currentDocumentMarkablePaths =
            new List<System.Windows.Shapes.Path>();

        /// <summary>
        /// Holds a list of all the Pen Lines added to the current
        /// document. 
        /// </summary>
        private List<Helpers.DocumentLine> _currentDocumentLines = 
            new List<SiliconStudio.Meet.EjpControls.Helpers.DocumentLine>();

        private Point _contextMenuOpenedPoint;
        private Point _lastRecordedMousePos;
        private FixedPage _currentFixedPage;

		/// <summary>
		/// Are comments enabled for document lines?
		/// </summary>
		public bool IsCommentingEnabled { get; set; }

        /// <summary>
        /// Determines if the scaleLock is active.
        /// If so, we need to adjust the size of the 
        /// page in the documentviewer to fit the size
        /// when the user drags the scale bar.
        /// </summary>
        private bool _isScaleLockActive;
        public bool IsScaleLockActive
        {
            get { return _isScaleLockActive; }
            set { _isScaleLockActive = value; }
        }

        /// <summary>
        /// The 1-Based page number of the current page.
        /// </summary>
        private int _currentPageNumber;

        //DocLine related stuff
        private bool _isPaintingDocumentLine = false;
        private bool _isResizingDocumentLine = false;
        private bool _isResizingDocumentLineStart = false;
        private Glyphs _currentDocumentLineStart; // The start of the new line being added
        private Glyphs _currentDocumentLineEnd; // The end of the new line being added
        private double _currentDocumentLineOffsetStart;
        private double _currentDocumentLineOffsetEnd;
        private DocumentLine _currentlyTouchingDocumentLine;  //Currently mouse over
        private DocumentLine _currentlyEditingDocumentLine;  //Currently being edited or manipulated
        private DocumentLine _currentlySelectedDocumentLine; //Has been visually selected.
        private SolidColorBrush _currentPenlineBrush;
        private SolidColorBrush _currentMarkerlineBrush;
        private Border _currentlySelectedImageBorder;
        //private bool _isAddingNewLineManually; //Tells wether the user is just adding a new line manually

        //Comments related stuff
        private DocumentLineCommentNote _currentOpenCommentNote;

        //Undo/Redo
        private Stack<UndoRedoAction> UndoStack = new Stack<UndoRedoAction>(25);
        private Stack<UndoRedoAction> RedoStack = new Stack<UndoRedoAction>(25);

		public XpsDocumentViewer()
		{
			InitializeComponent();
			this._inputMethod = DocumentAreaInputMehtod.Select;
			this._drawingMode = DocumentAreaDrawingMode.None;
			this._currentMarkerlineBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
            this._currentPenlineBrush = Brushes.Red;
            this._dA_DV_DocumentViewer.AllowDrop = true;
            this._tb_ScaleLock.IsChecked = true;

            this._dA_DV_DocumentViewer.PreviewMouseRightButtonUp += 
                new MouseButtonEventHandler(_dA_DV_DocumentViewer_PreviewMouseRightButtonUp);

            this._dA_DV_DocumentViewer.PreviewDragLeave += 
                new DragEventHandler(_dA_DV_DocumentViewer_DragLeave);
            this._dA_DV_DocumentViewer.PreviewMouseLeftButtonDown += 
                new MouseButtonEventHandler(_dA_DV_DocumentViewer_PreviewMouseLeftButtonDown);
            this._dA_DV_DocumentViewer.PreviewMouseMove += 
                new MouseEventHandler(_dA_DV_DocumentViewer_PreviewMouseMove);
            this._dA_DV_DocumentViewer.PreviewMouseLeftButtonUp += 
                new MouseButtonEventHandler(_dA_DV_DocumentViewer_PreviewMouseLeftButtonUp);

            this._sl_ZoomSlider.ToolTip = "スケールバー";
        }

        #region Document Line Note Related

        private void _dA_DV_DocumentViewer_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
			DebugManagers.DebugReporter.ReportMethodEnter();

            if (this.HasInputFocus == false || this.IsCommentingEnabled == false)
            {
                e.Handled = true;
				DebugManagers.DebugReporter.ReportMethodLeave();
                return;
            }

            bool result = false;
            DocumentLine c_dl = this.HittestDocumentLineRegions(
                this.UpdateLastRecordedMousePosition(), 
                E_DocumentEntityAdornerType.None);
            if (c_dl == null)
            {
                result = true;
            }
            else
                result = false;
            
            //To hittest Image Borders for the contextMenu
            //Border b = this.GetMouseHitImageBorder();
            //if (result == true && b == null)
            //{
            //    result = true;
            //}
            //else
            //{
            //    this._lastHittestedImageBorder = b;
            //    result = false;
            //}

            if (result == false)
            {
                if (c_dl.HasComment == false)
                    ((MenuItem)this._cm_DocumentLineContextMenu.Items[1]).IsEnabled = false;
                else
                    ((MenuItem)this._cm_DocumentLineContextMenu.Items[1]).IsEnabled = true;

                this._contextMenuOpenedPoint = this.UpdateLastRecordedMousePosition();
            }

            e.Handled = result;
			DebugManagers.DebugReporter.ReportMethodLeave();
        }

        private void OnAddNoteToDocumentLine(object sender, RoutedEventArgs e)
        {
            DocumentLine c_dl = this.HittestDocumentLineRegions(this._contextMenuOpenedPoint, E_DocumentEntityAdornerType.None);
            if (c_dl != null)
            {
                if (c_dl.IsCommentVisualOpen != true)
                {
                    if (this._currentOpenCommentNote != null)
                        this.CloseOpenCommentNote();

                    DocumentLineCommentNote dlcn = new DocumentLineCommentNote();
                    dlcn.DocumentLineParent = c_dl;
                    dlcn.OnClosing +=
                        new DocumentLineNoteRequestedClose(currentOpenDocumentLineNote_OnClosing);

                    FixedPage.SetLeft(dlcn, c_dl.End.BoundingBox.Right - 10);
                    FixedPage.SetTop(dlcn, c_dl.End.BoundingBox.Top - 10);
                    this._currentFixedPage.Children.Add(dlcn);
                    c_dl.IsCommentVisualOpen = true;

                    this._currentOpenCommentNote = dlcn;

                    if (c_dl.HasComment == false)
                    {
                        this.SetupCommentInDocumentLine(c_dl, new DocumentLineComment
                            {
                                Content = "",
                                DateAdded = DateTime.Now.ToShortDateString(),
                                ParentDocumentId = this._document.InternalDocumentId,
                                ParentStudyId = this._parentStudyId,
                                ParentDocumentLineId = c_dl.Id
                            });
                    }
                    else
                    {
                        dlcn._tb_NoteArea.Text = c_dl.Comment.Content;
                    }
                }
            }
        }

        /// <summary>
        /// Sets up a comment in a parent document line.
        /// The parent line must be fully initialized.
        /// </summary>
        private void SetupCommentInDocumentLine(DocumentLine parentLine, DocumentLineComment comment)
        {
            parentLine.CommentIcon = new Rectangle();
            parentLine.CommentIcon.Fill = (DrawingBrush)this.Resources["commentBubbleDrawing"];
            parentLine.HasComment = true;
            parentLine.AlignCommentVisual();
            ((Canvas)parentLine.Start.Glyphs.Parent).Children.Add(parentLine.CommentIcon);
            parentLine.Comment = comment;
            parentLine.CommentIcon.Tag = parentLine;
            parentLine.CommentIcon.PreviewMouseLeftButtonDown += 
                new MouseButtonEventHandler(CommentIcon_PreviewMouseLeftButtonUp);
        }

        private void CommentIcon_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Rectangle r = sender as Rectangle;
            DocumentLine c_dl = r.Tag as DocumentLine;

            if (c_dl.IsCommentVisualOpen != true)
            {
                if (this._currentOpenCommentNote != null)
                {
                    this.CloseOpenCommentNote();
                }

                DocumentLineCommentNote dlcn = new DocumentLineCommentNote();
                dlcn.DocumentLineParent = c_dl;
                dlcn.OnClosing +=
                    new DocumentLineNoteRequestedClose(currentOpenDocumentLineNote_OnClosing);

                FixedPage.SetLeft(dlcn, c_dl.End.BoundingBox.Right - 10);
                FixedPage.SetTop(dlcn, c_dl.End.BoundingBox.Top - 10);
                this._currentFixedPage.Children.Add(dlcn);
                c_dl.IsCommentVisualOpen = true;
                dlcn._tb_NoteArea.Text = c_dl.Comment.Content;

                this._currentOpenCommentNote = dlcn;
            }
        }

        private void currentOpenDocumentLineNote_OnClosing(
            DocumentLineCommentNote sender, DocumentLine ParentDocumentLine)
        {
            this._currentOpenCommentNote = null;
            ParentDocumentLine.IsCommentVisualOpen = false;
        }

        private void OnRemoveNoteFromDocumentLine(object sender, RoutedEventArgs e)
        {
            DocumentLine c_dl = this.HittestDocumentLineRegions(this._contextMenuOpenedPoint, E_DocumentEntityAdornerType.None);
            if (c_dl != null)
            {
                ((Canvas)c_dl.Start.Glyphs.Parent).Children.Remove(c_dl.CommentIcon);
                c_dl.DeleteComment();
            }
        }

        private void CloseOpenCommentNote()
        {
            if(this._currentOpenCommentNote != null)
            {
                this._currentOpenCommentNote.Close();
                //FixedPage p_fp = this._currentOpenCommentNote.Parent as FixedPage;
                //p_fp.Children.Remove(this._currentOpenCommentNote);
                //this._documentLineParent.Comment.Content = this._tb_NoteArea.Text;
                //this._documentLineParent.IsCommentVisualOpen = false;
                //this._currentOpenCommentNote = null;
            }
        }

        #endregion

        private void _dA_DV_DocumentViewer_DragLeave(object sender, DragEventArgs e)
        {
            this.DeSelectCurrentlySelectedGFXObject();
            this.DeSelectCurrentlySelectedDocLine();
        }

        /// <summary>
        /// If we are painting a Document line, this is where 
        /// it is finalized and painted into the document
        /// </summary>
        private void _dA_DV_DocumentViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.HasInputFocus == false)
            {
                e.Handled = true;
                return;
            }

            if (this._inputMethod == DocumentAreaInputMehtod.Draw ||
                this._inputMethod == DocumentAreaInputMehtod.Select)
            {
				if (this._isResizingDocumentLine)
				{
					this._isResizingDocumentLine = false;

					if (this._currentlySelectedDocumentLine == null)
						Debug.Print("Error #1: Currently Selected Documentline is not set.");
					else
					{
						Debug.Print("Line width:{0}", this._currentlyEditingDocumentLine.BoundingBox.Width);

						if (this._currentlyEditingDocumentLine.BoundingBox.Width < 12)
						{
							Debug.Print("Line Broken:{0}", this._currentlyEditingDocumentLine.BoundingBox.Width);
							this.DeleteDocumentLineFromDocument(this._currentlyEditingDocumentLine);

							return;
						}

                        //try this...
                        if (this._isPaintingDocumentLine)
                        {
                            this.DeSelectCurrentlySelectedDocLine();
                            this._isPaintingDocumentLine = false;
                        }

                        if (this._currentlySelectedDocumentLine != null)
                        {
                            this._currentlySelectedDocumentLine.MarkAsSelected(true,
                                                this.Resources["lineHandleS_selected"] as Brush,
                                                this.Resources["lineHandleE_selected"] as Brush);
                        }
					}
					//Align the handles to fit in between the nearest glyphs.
                    if (this._currentlyEditingDocumentLine != null)
                    {
                        if (this._isResizingDocumentLineStart)
                            this._currentlyEditingDocumentLine.AlignHandlesToGlyphs(Double.NaN, Double.NaN, true, false);
                        else
                            this._currentlyEditingDocumentLine.AlignHandlesToGlyphs(Double.NaN, Double.NaN, false, true);
                    }

					this.FireDocumentLineContentsChangedEvent(this._currentlyEditingDocumentLine);
					this._currentlyEditingDocumentLine = null;         
                }
            }
            
            if (this._currentOpenCommentNote != null)
            {
                if (this._currentOpenCommentNote.IsMoving)
                {
                    this._currentOpenCommentNote.IsMoving = false;
                }
            }
        }

        /// <summary>
        /// Takes care of most operations that need to run on a Mouse Down hittest
        /// </summary>
        private void _dA_DV_DocumentViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this.HasInputFocus == false)
            {
                e.Handled = true;
                return;
            }

            this.UpdateLastRecordedMousePosition();

            if (this._currentOpenCommentNote != null)
            {
                double commentLeft = (double)this._currentOpenCommentNote.GetValue(FixedPage.LeftProperty);
                double commentTop = (double)this._currentOpenCommentNote.GetValue(FixedPage.TopProperty);
                double commentHeight = this._currentOpenCommentNote.Height;
                double commentWidth = this._currentOpenCommentNote.Width;

                Point mousePos = e.GetPosition(this._currentFixedPage);
                if (mousePos.X > commentLeft && mousePos.X < (commentLeft + commentWidth)
                    && mousePos.Y > commentTop && mousePos.Y < (commentTop + commentHeight))
                {
                    return;
                }
            }

            if (this.IsMouseOverDocumentLineRegion(this.UpdateLastRecordedMousePosition()))
            {
                if (this._inputMethod == DocumentAreaInputMehtod.Erase)
                {
                    DocumentLine dl = null;
                    if(this._drawingMode == DocumentAreaDrawingMode.MarkerLine)
                        dl = this.HittestDocumentLineRegions(this._lastRecordedMousePos, E_DocumentEntityAdornerType.MarkerLine);
                    else if(this._drawingMode == DocumentAreaDrawingMode.PenLine)
                       dl = this.HittestDocumentLineRegions(this._lastRecordedMousePos, E_DocumentEntityAdornerType.None);

                    if (dl == null)
                        return;

                    bool doIt = false;
                    if (this._drawingMode == DocumentAreaDrawingMode.MarkerLine && 
                        dl.LineType == E_DocumentEntityAdornerType.MarkerLine)
                            doIt = true;
                    else if(this._drawingMode == DocumentAreaDrawingMode.PenLine && 
                        dl.LineType == E_DocumentEntityAdornerType.PenLine)
                            doIt = true;

                    if (doIt == false)
                        return;

                    if(this.OnQueryDocumentLineConnections != null)
                    {
                        int connectedElements = this.OnQueryDocumentLineConnections(dl.Id, "", this._document.InternalDocumentId);
                        if (connectedElements > 0)
                        {
                            if (
                            MessageBox.Show(
                            "\n\n" +
                            "いま、あなたが消そうとしている線は、ナレッジマップ上にある \n" +
                            "１つ以上のオブジェクトと関連しています。\n\n" +
                            "この線を消すと、関連するすべてのオブジェクトがナレッジマップ\n" +
                            "からも削除されます。\n\n" +
                            "この動作は取り消せません。それでもこの線を消しますか？" +
                            "\n\n", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                            == MessageBoxResult.No)
                                return;
                        }
                    }

                    this.DeleteDocumentLineFromDocument(dl);
                    this.AddActionToUndoList(dl, null, UndoRedoActions.DeleteDocumentLine, Colors.Black, null);
                    if(e!= null)    //080407
                        e.Handled = true;
                }
                else
                {
                    DocumentLine dl = this.HittestDocumentLineRegions(this._lastRecordedMousePos, E_DocumentEntityAdornerType.None);
                    //Final temp solution to solve the odd
                    //handle offsets...
                    if (e.OriginalSource is Rectangle)
                    {
                        if(((Rectangle)e.OriginalSource).Tag is DocumentLine)
                            return;
                    }
                    this.DeSelectCurrentlySelectedDocLine();
                    this._currentlySelectedDocumentLine = dl;
					dl.MarkAsSelected(true, this.Resources["lineHandleS_selected"] as Brush, this.Resources["lineHandleE_selected"] as Brush);
                    this._isResizingDocumentLine = false;
                    this._isPaintingDocumentLine = false;
                    Helpers.DragDropQuote q = new Helpers.DragDropQuote
                    {
                        Color = dl.GetColor(),
                        UnicodeString = dl.Contents,
                        Reference = new XpsDocumentReference
                        {
                            TargetLineId = dl.Id,
                            AnchorX = (int)dl.Anchor.X,
                            AnchorY = (int)dl.Anchor.Y,
                            DocumentId = this._document.InternalDocumentId,
                            DocumentParentStudyId = this._parentStudyId,
                            PageNumber = this._currentPageNumber,
                            Content = dl.Contents
                        }
                    };

                    if (dl.HasComment)
                        q.CommentString = dl.Comment.Content;

                    DragDrop.DoDragDrop(this, q, DragDropEffects.All);
                }

                this.DeSelectCurrentlySelectedGFXObject();

            }
            else
                this.DeSelectCurrentlySelectedDocLine();

            Border hitB = this.GetMouseHitImageBorder();
            if (hitB == null)
            {
                this.DeSelectCurrentlySelectedGFXObject();
            }
            else //we have to do this here because this is the only event we get...
            {
                if (this._inputMethod == DocumentAreaInputMehtod.Erase)
                {
                    this.DeleteHittestedImageBorders(e);
                }
            }
        }

        private void _dA_DV_DocumentViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (this.HasInputFocus == false)
            {
                return;
            }

            if (this._isResizingDocumentLine)
            {
                if (this._isResizingDocumentLineStart)
                    this.DragDocumentLineStart(e);
                else
                    this.DragDocumentLineEnd(e);

                //uncomment for live content update.
                //this.FireDocumentLineContentsChangedEvent(this._currentlyEditingDocumentLine);
                
                if (this._currentlyEditingDocumentLine.HasComment)
                {
                    this._currentlyEditingDocumentLine.AlignCommentVisual();
                }
            }
            else if (this._currentOpenCommentNote != null)
            {
                if (this._currentOpenCommentNote.IsMoving)
                {
                    e.Handled = true;
                 
                    Point cP = e.GetPosition(this._currentFixedPage);

                    if (cP.X < 5 || cP.X > (this._currentFixedPage.ActualWidth - 10))
                        return;
                    else if (cP.Y < 5 || cP.Y > (this._currentFixedPage.ActualHeight - 10))
                        return;

                    FixedPage.SetLeft(this._currentOpenCommentNote, cP.X - this._currentOpenCommentNote.InitialMoveOffset.X);
                    FixedPage.SetTop(this._currentOpenCommentNote, cP.Y - this._currentOpenCommentNote.InitialMoveOffset.Y);

                    this.UpdateLastRecordedMousePosition();
                }
            }
            else
            {
                if (this._inputMethod != DocumentAreaInputMehtod.Erase)
                {
                    if (this.IsMouseOverDocumentLineRegion(this.UpdateLastRecordedMousePosition()))
                        this._currentFixedPage.Cursor = Cursors.Hand;
                    else 
                        this._currentFixedPage.Cursor = Cursors.Pen;
                }

                if (this._currentlyTouchingDocumentLine != null)
                {
                    this._currentlyTouchingDocumentLine.RunTuchedFeedback(false);
                    this._currentlyTouchingDocumentLine = null;
                }

                DocumentLine hitDl = this.HittestDocumentLineRegions(this.UpdateLastRecordedMousePosition(),
                    E_DocumentEntityAdornerType.None);
                if (hitDl != null)
                {
                    hitDl.RunTuchedFeedback(true);
                    this._currentlyTouchingDocumentLine = hitDl;
                }

                //disable selection.
                if (e.OriginalSource is DocumentViewer)
                    e.Handled = true;
            }
        }

        /// <summary>
        /// Deselect the currently selected Graphics Object
        /// </summary>
        private void DeSelectCurrentlySelectedGFXObject()
        {
            if (this._currentlySelectedImageBorder != null)
            {
                this._currentlySelectedImageBorder.Child = null;
                this._currentlySelectedImageBorder = null;
            }
        }

        /// <summary>
        /// Check to see if the given point is intersecting any 
        /// Document lines. If so, return the hit line.
        /// </summary>
        private DocumentLine HittestDocumentLineRegions(Point mousePos, E_DocumentEntityAdornerType preferedLineType)
        {
            foreach (DocumentLine pl in this._currentDocumentLines)
            {
                if (pl.HitTest(mousePos, this._currentPageNumber) == true)
                {
                    switch (preferedLineType)
                    {
                        case E_DocumentEntityAdornerType.None:
                            return pl;
                        case E_DocumentEntityAdornerType.PenLine:
                            if (pl.LineType == preferedLineType)
                                return pl;
                            break;
                        case E_DocumentEntityAdornerType.MarkerLine:
                            if (pl.LineType == preferedLineType)
                                return pl;
                            break;
                        default:
                            break;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Deselect the currently selected Document Line
        /// </summary>
        private void DeSelectCurrentlySelectedDocLine()
        {
            if (this._currentlySelectedDocumentLine != null)
            {
				this._currentlySelectedDocumentLine.MarkAsSelected(false, 
					this.Resources["lineHandleS_default"] as Brush,
					this.Resources["lineHandleE_default"] as Brush);
                this._currentlySelectedDocumentLine = null;
            }
        }

        /// <summary>
        /// Check to see if the Mouse is over a line in the document.
        /// </summary>
        private bool IsMouseOverDocumentLineRegion(Point mousePos)
        {
            foreach (DocumentLine pl in this._currentDocumentLines)
            {
                if (pl.HitTest(mousePos, this._currentPageNumber) == true)
                    return true;
            }
            return false;
        }

        private DocumentLine FindPreviousDocumentLine(DocumentLine currentLine)
        {
            if (this._currentDocumentLines.Count < 2)
                return null;

            int cLineStartGlyphsIndex = this._currentDocumentGlyphs.IndexOf(currentLine.Start.Glyphs);
            int closestGlyphId = 0;
            double closestGlyphLeftPos = 0;

            DocumentLine result = null;
            foreach (DocumentLine dl in this._currentDocumentLines)
            {
                if (dl == this._currentlyEditingDocumentLine)
                    continue;

                if (dl.LineType == this._currentlyEditingDocumentLine.LineType)
                {
                    int lEndGlyphsIndex = this._currentDocumentGlyphs.IndexOf(dl.End.Glyphs);
                    if (lEndGlyphsIndex <= cLineStartGlyphsIndex)
                    {
                        if(lEndGlyphsIndex > closestGlyphId)
                        {
                            //If the found line is on the same glyphs but starts before the line we
                            //we are testing from, we need to discard it as a candidate.
                            if (lEndGlyphsIndex == this._currentDocumentGlyphs.IndexOf(dl.Start.Glyphs))
                            {
                                if (dl.Start.BoxLeft > currentLine.End.BoundingBoxWithHandles.Right)
                                    continue;
                            }

                            closestGlyphId = lEndGlyphsIndex;
                            closestGlyphLeftPos = (double)dl.End.Line.GetValue(Canvas.LeftProperty)+dl.End.Width;
                            result = dl;
                        }
                        else if (lEndGlyphsIndex == closestGlyphId)
                        {
                            double cPos = (double)dl.End.Line.GetValue(Canvas.LeftProperty) + dl.End.Width;
                            if (cPos > closestGlyphLeftPos)
                            {
                                closestGlyphId = lEndGlyphsIndex;
                                closestGlyphLeftPos = cPos;
                                result = dl;
                            }
                        }
                    }
                }
            }
            return result;
        }

        private void DragDocumentLineStart(MouseEventArgs e)
        {
            //This is a silly test, but the way the PreviewMouseMove event
            //is fired forces us to check here so that this function does not get called
            //several times even though the mouse has not moved...
            if (this._lastRecordedMousePos == this.UpdateLastRecordedMousePosition())
                return;

            int startIndex = this._currentDocumentGlyphs.IndexOf(this._currentlyEditingDocumentLine.Start.Glyphs);
            int endIndex = this._currentDocumentGlyphs.IndexOf(this._currentDocumentLineEnd);
            int fullLineEndIndex = this._currentDocumentGlyphs.IndexOf(this._currentlyEditingDocumentLine.End.Glyphs);

            if (endIndex > fullLineEndIndex)
                return;

            //Adding parts above.
            if (endIndex < startIndex)
            {

                //Check to see that we are not 
                //Dragging 2 lines on top over eachother.
                DocumentLine prevLine = this.FindPreviousDocumentLine(this._currentlyEditingDocumentLine);
                if (prevLine != null)
                {
                    int ceDlSIndex = this._currentDocumentGlyphs.IndexOf(this._currentDocumentLineEnd);
                    int pDlEIndex = this._currentDocumentGlyphs.IndexOf(prevLine.End.Glyphs);

                    Point currMousePoint = e.GetPosition((Canvas)this._currentlyEditingDocumentLine.Start.Glyphs.Parent);
                    if (ceDlSIndex == pDlEIndex)
                    {
                        //Replaced the commented out section to accomodate for a calc error 080329
                        if (((double)prevLine.End.Line.GetValue(Canvas.LeftProperty) + prevLine.End.Width) >= currMousePoint.X)
                            return;

                        //if ((prevLine.End.Glyphs.OriginX + prevLine.End.Width) >= currMousePoint.X)
                        //    return;
                    }
                    else if (ceDlSIndex < pDlEIndex)
                        return;
                }

                this._currentlyEditingDocumentLine.Start.StartHandle.Width = 0;

                ((Canvas)this._currentlyEditingDocumentLine.Start.Glyphs.Parent).Children.Remove(
                        this._currentlyEditingDocumentLine.Start.StartHandle);

                if (this._currentlyEditingDocumentLine.Start == this._currentlyEditingDocumentLine.End)
                {
                    double currentDiff =
                        ((double)this._currentlyEditingDocumentLine.End.Line.GetValue(Canvas.LeftProperty))
                        - this._currentlyEditingDocumentLine.End.Glyphs.OriginX;
                    this._currentlyEditingDocumentLine.End.Line.Width += currentDiff;
                    Canvas.SetLeft(this._currentlyEditingDocumentLine.End.Line, this._currentlyEditingDocumentLine.End.Glyphs.OriginX);

                    this._currentlyEditingDocumentLine.End.BoxLeft = this._currentlyEditingDocumentLine.End.Glyphs.OriginX;
                    this._currentlyEditingDocumentLine.End.OffsetStart = 0;
                    startIndex -= 1;
                }
                else
                {
                    this._currentlyEditingDocumentLine.Start.Line.Width = this._currentlyEditingDocumentLine.Start.Glyphs.ToGlyphRun().ComputeAlignmentBox().Width;
                    Canvas.SetLeft(this._currentlyEditingDocumentLine.Start.Line, this._currentlyEditingDocumentLine.Start.Glyphs.OriginX);
                    startIndex -= 1;
                }

                this._currentlyEditingDocumentLine.Start.BoxLeft = this._currentlyEditingDocumentLine.Start.Glyphs.OriginX;
                this._currentlyEditingDocumentLine.Start.OffsetStart = 0;

                double width;

                if ((startIndex - endIndex) >= 0) //Add the lines in between
                {
                    for (int i = startIndex; i > endIndex; --i)
                    {
                        width = this._currentDocumentGlyphs[i].ToGlyphRun().ComputeAlignmentBox().Width;
                        PenLinePart lPartM = this.DrawDocumentLineForGlyphs(this._currentDocumentGlyphs[i],
                            Enumerations.PenLineGlyphsPosition.Intermediate, 0,
                            width, false, false, this._currentlyEditingDocumentLine.LineType,
                            this._currentlyEditingDocumentLine.GetColor());

                        lPartM.ParentPenLineId = this._currentlyEditingDocumentLine.Id;
                        this._currentlyEditingDocumentLine.LineParts.Insert(0, lPartM);
                    }
                }

                width = 3;
                PenLinePart lPartE = this.DrawDocumentLineForGlyphs(this._currentDocumentGlyphs[endIndex],
                    Enumerations.PenLineGlyphsPosition.Start,
                    this._currentDocumentLineOffsetEnd, width, true, false, this._currentlyEditingDocumentLine.LineType,
                    this._currentlyEditingDocumentLine.GetColor());

                //Wrap the new PenLinePart into the new PenLine
                lPartE.ParentPenLineId = this._currentlyEditingDocumentLine.Id;
                this._currentlyEditingDocumentLine.Start = lPartE;
                this._currentlyEditingDocumentLine.LineParts.Insert(0, lPartE);

                //Tag the start handle with the pen line
                //for backwards referencing while editing.
                lPartE.StartHandle.Tag = this._currentlyEditingDocumentLine;
            }
            else if (startIndex < endIndex)
            {
                if ((endIndex - startIndex) >= 1) //Remove the lines in between
                {
                    int diff = endIndex - startIndex;
                    for (int i = 0; i < diff; i++)
                    {
                        if (this._currentlyEditingDocumentLine.LineParts.Count == 0)
                            break;

                        PenLinePart mPart = this._currentlyEditingDocumentLine.LineParts[0];
                        Canvas parentCanvas = mPart.Glyphs.Parent as Canvas;
                        if (parentCanvas.Children.Contains(mPart.Line))
                            parentCanvas.Children.Remove(mPart.Line);
                        if (parentCanvas.Children.Contains(mPart.StartHandle))
                            parentCanvas.Children.Remove(mPart.StartHandle);
                        this._currentlyEditingDocumentLine.LineParts.Remove(mPart);
                    }
                }

                //Remove one more part to make room for the new endPart.
                if (this._currentlyEditingDocumentLine.LineParts.Count > 1)
                {
                    PenLinePart lastPart =
                        this._currentlyEditingDocumentLine.LineParts[0];
                    Canvas parentCanvas = lastPart.Glyphs.Parent as Canvas;
                    parentCanvas.Children.Remove(lastPart.Line);
                    this._currentlyEditingDocumentLine.LineParts.Remove(lastPart);
                }

                //Need to solve for when this is the last segment of the line
                if (this._currentlyEditingDocumentLine.LineParts.Count == 1)
                {
                    /*
                     * here we need to remove all remaints of the line and add a new piece that contains
                     * both the beginning and the end, just as we did when we added the line in the 
                     * first place.
                     */

                    PenLinePart p = this._currentlyEditingDocumentLine.LineParts[0];
                    Canvas parentCanvas = p.Glyphs.Parent as Canvas;
                    if (parentCanvas.Children.Contains(p.Line))
                        parentCanvas.Children.Remove(p.Line);
                    if (parentCanvas.Children.Contains(p.StartHandle))
                        parentCanvas.Children.Remove(p.StartHandle);
                    if (parentCanvas.Children.Contains(p.EndHandle))
                        parentCanvas.Children.Remove(p.EndHandle);

                    Point currMousePoint = e.GetPosition((Canvas)p.Glyphs.Parent);
                    Glyphs c_glyphs = this._currentlyEditingDocumentLine.LineParts[0].Glyphs;
                    double glyphStartX = c_glyphs.OriginX;
                    double glyphWidth = c_glyphs.ToGlyphRun().ComputeAlignmentBox().Width;
                    double glyphEndX = glyphStartX + glyphWidth;
                    double currentMousePosWithinGlyph = currMousePoint.X - glyphStartX;

                    double newLineWidth = glyphWidth - currentMousePosWithinGlyph;

                    double endHandleStartX = (double)this._currentlyEditingDocumentLine.End.EndHandle.GetValue(Canvas.LeftProperty);
                    double endToWidthDiff = glyphEndX - endHandleStartX;
                    newLineWidth -= (endToWidthDiff - 9);
                    if (newLineWidth < 10)
                        return;

                    PenLinePart lPartS = this.DrawDocumentLineForGlyphs(p.Glyphs,
                        Enumerations.PenLineGlyphsPosition.Start,
                        currentMousePosWithinGlyph, 
                        newLineWidth,
                        true, true,
                        this._currentlyEditingDocumentLine.LineType,
                        this._currentlyEditingDocumentLine.GetColor());

                    lPartS.EndHandle.Tag = this._currentlyEditingDocumentLine;
                    lPartS.StartHandle.Tag = this._currentlyEditingDocumentLine;
                    this._currentlyEditingDocumentLine.Start = lPartS;
                    this._currentlyEditingDocumentLine.End = lPartS;
                    this._currentlyEditingDocumentLine.AddPart(lPartS);

                    this._currentlyEditingDocumentLine.LineParts.Remove(p);
                }
                else 
                {
                    double width = 
                        this._currentDocumentLineEnd.ToGlyphRun().ComputeAlignmentBox().Width - 
                        this._currentDocumentLineOffsetEnd;
                    if (width < 0.000)
                        return;

                    PenLinePart lPartS = this.DrawDocumentLineForGlyphs(this._currentDocumentGlyphs[endIndex],
                        Enumerations.PenLineGlyphsPosition.Start,
                        this._currentDocumentLineOffsetEnd, width, true, false,
                        this._currentlyEditingDocumentLine.LineType,
                        this._currentlyEditingDocumentLine.GetColor());

                    //Wrap the new PenLinePart into the PenLine
                    lPartS.ParentPenLineId = this._currentlyEditingDocumentLine.Id;
                    lPartS.StartHandle.Tag = this._currentlyEditingDocumentLine;
                    this._currentlyEditingDocumentLine.Start = lPartS;
                    this._currentlyEditingDocumentLine.LineParts.Insert(0, lPartS);
                }
            }
            else if (startIndex == endIndex)
            {
                Point currMousePoint = e.GetPosition((Canvas)this._currentlyEditingDocumentLine.Start.Glyphs.Parent);
                if (currMousePoint.X <= (this._currentlyEditingDocumentLine.Start.Glyphs.OriginX + 1))
                    return;

                //Check to see that we are not 
                //Dragging 2 lines on top over eachother.
                DocumentLine prevLine = this.FindPreviousDocumentLine(this._currentlyEditingDocumentLine);
                if (prevLine != null)
                {
                    if (this._currentlyEditingDocumentLine.Start.Glyphs == prevLine.End.Glyphs)
                    {
                        if(((double)prevLine.End.Line.GetValue(Canvas.LeftProperty) + prevLine.End.Width) >= currMousePoint.X)
                            return;
                    }
                }

                Glyphs c_glyphs = this._currentlyEditingDocumentLine.Start.Glyphs;
                double glyphStartX = c_glyphs.OriginX;
                double glyphWidth = c_glyphs.ToGlyphRun().ComputeAlignmentBox().Width;
                double glyphEndX = glyphStartX + glyphWidth;
                double currentMousePosWithinGlyph = currMousePoint.X - glyphStartX;

                double newLineWidth = glyphWidth - currentMousePosWithinGlyph;

                if (this._currentlyEditingDocumentLine.Start == this._currentlyEditingDocumentLine.End)
                {
                    double endHandleStartX = (double)this._currentlyEditingDocumentLine.End.EndHandle.GetValue(Canvas.LeftProperty);
                    double endToWidthDiff = glyphEndX - endHandleStartX;
                    newLineWidth -= (endToWidthDiff - 9);
                    if (newLineWidth < 10)
                        return;
                }

                if (newLineWidth < 5.0000
                    || newLineWidth > this._currentlyEditingDocumentLine.Start.Glyphs.ToGlyphRun().ComputeAlignmentBox().Width)
                {
                    return;
                }

                this._currentlyEditingDocumentLine.Start.OffsetStart = currentMousePosWithinGlyph;
                this._currentlyEditingDocumentLine.Start.Line.Width = newLineWidth;
                this._currentlyEditingDocumentLine.Start.BoxLeft = currMousePoint.X;
                Canvas.SetLeft(this._currentlyEditingDocumentLine.Start.Line, currMousePoint.X);
                Canvas.SetLeft(this._currentlyEditingDocumentLine.Start.StartHandle, currMousePoint.X);
            }
            this.UpdateLastRecordedMousePosition();
        }

        private DocumentLine FindNextDocumentLine(DocumentLine currentLine)
        {
            if (this._currentDocumentLines.Count < 2)
                return null;

            int cLineEndGlyphsIndex = this._currentDocumentGlyphs.IndexOf(currentLine.End.Glyphs);

            int closestGlyphId = this._currentDocumentGlyphs.Count - 1;
            double closestGlyphLeftPos = this._currentFixedPage.Width;

            DocumentLine result = null;
            foreach (DocumentLine dl in this._currentDocumentLines)
            {
                if (dl == this._currentlyEditingDocumentLine)
                    continue;

                if (dl.LineType == this._currentlyEditingDocumentLine.LineType)
                {
                    int lStartGlyphsIndex = this._currentDocumentGlyphs.IndexOf(dl.Start.Glyphs);
                    if (lStartGlyphsIndex >= cLineEndGlyphsIndex) // was >=
                    {
                        if (lStartGlyphsIndex < closestGlyphId)
                        {
                            //If the found line is on the same glyphs but ends before the line we
                            //we are testing from, we need to discard it as a candidate.
                            if (lStartGlyphsIndex == this._currentDocumentGlyphs.IndexOf(dl.End.Glyphs))
                            {
                                if (dl.End.BoundingBoxWithHandles.Right < currentLine.Start.BoxLeft)
                                    continue;
                            }

                            closestGlyphId = lStartGlyphsIndex;
                            closestGlyphLeftPos = (double)dl.Start.Line.GetValue(Canvas.LeftProperty);
                            result = dl;
                        }
                        else if (lStartGlyphsIndex == closestGlyphId)
                        {
                            double cPos = (double)dl.Start.Line.GetValue(Canvas.LeftProperty);
                            if (cPos <= closestGlyphLeftPos)
                            {
                                closestGlyphLeftPos = cPos;
                                closestGlyphId = lStartGlyphsIndex;
                                result = dl;
                            }
                        }
                    }
                    else if (lStartGlyphsIndex == cLineEndGlyphsIndex)
                    {
                        //If the found line is on the same glyphs but ends before the line we
                        //we are testing from, we need to discard it as a candidate.
                        if (lStartGlyphsIndex == cLineEndGlyphsIndex)
                        {
                            if (dl.End.BoundingBoxWithHandles.Right < currentLine.Start.BoxLeft)
                                continue;
                        }
                    }
                }
            }
            return result;
        }

        private void DragDocumentLineEnd(MouseEventArgs e)
        {
            int startIndex = this._currentDocumentGlyphs.IndexOf(this._currentlyEditingDocumentLine.End.Glyphs);
            int endIndex = this._currentDocumentGlyphs.IndexOf(this._currentDocumentLineEnd);
            int fullLineStartIndex = this._currentDocumentGlyphs.IndexOf(this._currentlyEditingDocumentLine.Start.Glyphs);

            if (endIndex < fullLineStartIndex)
                return;

            //Adding parts below.
            if (endIndex > startIndex)
            {
                //Check to see that we are not 
                //Dragging 2 lines on top over eachother.
                DocumentLine nextLine = this.FindNextDocumentLine(this._currentlyEditingDocumentLine);
                if (nextLine != null)
                {
                    int ceDlEIndex = this._currentDocumentGlyphs.IndexOf(this._currentDocumentLineEnd);
                    int nDlSIndex = this._currentDocumentGlyphs.IndexOf(nextLine.Start.Glyphs);
                    Point currMousePoint = e.GetPosition((Canvas)this._currentlyEditingDocumentLine.End.Glyphs.Parent);
                    if (ceDlEIndex == nDlSIndex)
                    {
                        if ((currMousePoint.X - nextLine.Start.Glyphs.OriginX) > nextLine.Start.OffsetStart)
                            return;
                    }
                    else if (ceDlEIndex > nDlSIndex)
                        return;
                }


                if (this._currentlyEditingDocumentLine.End == this._currentlyEditingDocumentLine.Start)
                {
                    this._currentlyEditingDocumentLine.End.Line.Width =
                            this._currentlyEditingDocumentLine.End.Glyphs.ToGlyphRun().ComputeAlignmentBox().Width -
                            this._currentlyEditingDocumentLine.Start.OffsetStart;
                }
                else
                {
                    this._currentlyEditingDocumentLine.End.Line.Width =
                            this._currentlyEditingDocumentLine.End.Glyphs.ToGlyphRun().ComputeAlignmentBox().Width;
                }

                ((Canvas)this._currentlyEditingDocumentLine.End.Glyphs.Parent).Children.Remove(
                        this._currentlyEditingDocumentLine.End.EndHandle);
                this._currentlyEditingDocumentLine.End.EndHandle.Width = 0;
                double width;

                //Compensate for the line we edited above
                startIndex += 1;

                if ((endIndex - startIndex) >= 1) //Add the lines in between
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        width = this._currentDocumentGlyphs[i].ToGlyphRun().ComputeAlignmentBox().Width;
                        PenLinePart lPartM = this.DrawDocumentLineForGlyphs(this._currentDocumentGlyphs[i],
                            Enumerations.PenLineGlyphsPosition.Intermediate,
                            0, width, false, false, this._currentlyEditingDocumentLine.LineType,
                            this._currentlyEditingDocumentLine.GetColor());

                        lPartM.ParentPenLineId = this._currentlyEditingDocumentLine.Id;
                        this._currentlyEditingDocumentLine.AddPart(lPartM);
                    }
                }
                width = this._currentDocumentLineOffsetEnd;
                PenLinePart lPartE = this.DrawDocumentLineForGlyphs(this._currentDocumentGlyphs[endIndex],
                    Enumerations.PenLineGlyphsPosition.End,
                    0, width, false, true, this._currentlyEditingDocumentLine.LineType,
                    this._currentlyEditingDocumentLine.GetColor());

                //Wrap the new PenLinePart into the new PenLine
                lPartE.ParentPenLineId = this._currentlyEditingDocumentLine.Id;
                this._currentlyEditingDocumentLine.End = lPartE;
                this._currentlyEditingDocumentLine.AddPart(lPartE);

                //Tag the start handle with the pen line
                //for backwards referencing while editing.
                lPartE.EndHandle.Tag = this._currentlyEditingDocumentLine;
            }
            else if (startIndex > endIndex) //Removing parts Above.
            {
                //Remove the previous endPart
                Canvas lineParent = (Canvas)this._currentlyEditingDocumentLine.End.Glyphs.Parent;
                lineParent.Children.Remove(this._currentlyEditingDocumentLine.End.Line);
                lineParent.Children.Remove(this._currentlyEditingDocumentLine.End.EndHandle);
                this._currentlyEditingDocumentLine.LineParts.Remove(this._currentlyEditingDocumentLine.End);

                //Compesate for the line we removed above.
                startIndex -= 1;

                if ((startIndex - endIndex) >= 1) //Remove the lines in between
                {
                    int diff = startIndex - endIndex;
                    for (int i = 0; i < diff; i++)
                    {
                        int lastIndex = this._currentlyEditingDocumentLine.LineParts.Count - 1;
                        PenLinePart mPart = this._currentlyEditingDocumentLine.LineParts[lastIndex];
                        Canvas parentCanvas = mPart.Glyphs.Parent as Canvas;
                        if (parentCanvas.Children.Contains(mPart.Line))
                            parentCanvas.Children.Remove(mPart.Line);
                        if (parentCanvas.Children.Contains(mPart.EndHandle))
                            parentCanvas.Children.Remove(mPart.EndHandle);
                        this._currentlyEditingDocumentLine.LineParts.RemoveAt(lastIndex);
                    }
                }

                //Remove one more part to make room for the new endPart.
                    PenLinePart lastPart =
                        this._currentlyEditingDocumentLine.LineParts[this._currentlyEditingDocumentLine.LineParts.Count - 1];

                    lineParent = (Canvas)lastPart.Glyphs.Parent;
                    lineParent.Children.Remove(lastPart.Line);
                
                //Need to solve for when this is the last segment of the line
                if (this._currentlyEditingDocumentLine.LineParts.Count == 1)
                {
                    Glyphs startGlyphs = this._currentlyEditingDocumentLine.LineParts[0].Glyphs;
                    double startOffset = this._currentlyEditingDocumentLine.LineParts[0].OffsetStart;

                    if (lineParent.Children.Contains(this._currentlyEditingDocumentLine.Start.StartHandle))
                        lineParent.Children.Remove(this._currentlyEditingDocumentLine.Start.StartHandle);

                    this._currentlyEditingDocumentLine.LineParts.RemoveAt(0);

                    double width = this._currentDocumentLineOffsetEnd - startOffset;

                    PenLinePart lPart = this.DrawDocumentLineForGlyphs(startGlyphs,
                        Enumerations.PenLineGlyphsPosition.Start,
                        startOffset, width, true,
                        true, this._currentlyEditingDocumentLine.LineType,
                        this._currentlyEditingDocumentLine.GetColor());

                    lPart.EndHandle.Tag = this._currentlyEditingDocumentLine;
                    lPart.StartHandle.Tag = this._currentlyEditingDocumentLine;
                    this._currentlyEditingDocumentLine.Start = lPart;
                    this._currentlyEditingDocumentLine.End = lPart;
                    this._currentlyEditingDocumentLine.AddPart(lPart);
                }
                else
                {

                    this._currentlyEditingDocumentLine.LineParts.Remove(lastPart);

                    //Add the new endPart.
                    Double width = this._currentDocumentLineOffsetEnd;
                    PenLinePart lPartE = this.DrawDocumentLineForGlyphs(this._currentDocumentGlyphs[endIndex], Enumerations.PenLineGlyphsPosition.End,
                        0, width, false, true, this._currentlyEditingDocumentLine.LineType,
                        this._currentlyEditingDocumentLine.GetColor());

                    //Wrap the new PenLinePart into the new PenLine
                    lPartE.ParentPenLineId = this._currentlyEditingDocumentLine.Id;
                    this._currentlyEditingDocumentLine.End = lPartE;
                    this._currentlyEditingDocumentLine.AddPart(lPartE);

                    //Tag the start handle with the pen line
                    //for backwards referencing while editing.
                    lPartE.EndHandle.Tag = this._currentlyEditingDocumentLine;
                }
            }
            else if (startIndex == endIndex) //Manipulating the same part.
            {
                //Check to make sure that the new length of the current line is within the
                //valid bounds.
                Point currMousePoint = e.GetPosition((Canvas)this._currentlyEditingDocumentLine.End.Glyphs.Parent);
                double newWidth = this._currentlyEditingDocumentLine.End.Line.Width +
                    (currMousePoint.X - this._lastRecordedMousePos.X);

                //Check to see that we are not 
                //Dragging 2 lines on top over eachother.
                DocumentLine nextLine = this.FindNextDocumentLine(this._currentlyEditingDocumentLine);
                if (nextLine != null)
                {
                    if (this._currentlyEditingDocumentLine.End.Glyphs == nextLine.Start.Glyphs)
                    {
                        if ((currMousePoint.X - nextLine.Start.Glyphs.OriginX) > nextLine.Start.OffsetStart)
                            return;
                    }
                }

                if (newWidth < 5.0000
                    || newWidth > 
                    (this._currentlyEditingDocumentLine.End.Glyphs.ToGlyphRun().ComputeAlignmentBox().Width - 
                    this._currentlyEditingDocumentLine.End.OffsetStart))
                    return;

                this._currentlyEditingDocumentLine.End.Line.Width +=
                    currMousePoint.X - this._lastRecordedMousePos.X;

                if (this._currentlyEditingDocumentLine.End.OffsetStart != 0)
                {
                    Canvas.SetLeft(this._currentlyEditingDocumentLine.End.EndHandle,
                        this._currentlyEditingDocumentLine.End.Glyphs.OriginX +
                        this._currentlyEditingDocumentLine.End.OffsetStart +
                        (this._currentlyEditingDocumentLine.End.Line.Width - 9));
                }
                else
                {
                    Canvas.SetLeft(this._currentlyEditingDocumentLine.End.EndHandle,
                    this._currentlyEditingDocumentLine.End.Glyphs.OriginX +
                    (this._currentlyEditingDocumentLine.End.Line.Width - 9));
                }
            }
            this.UpdateLastRecordedMousePosition();
        }

        /// <summary>
        /// While we are drawing a Document line we need to keep
        /// track of where we are.
        /// </summary>
        void glyph_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (this._isResizingDocumentLine)
            {
                Glyphs gl = sender as Glyphs;
                this._currentDocumentLineEnd = gl;
                this.SetCurrentDocumentLineOffsetEnd();
                this.SetCurrentDocumentLineOffsetStart();
                e.Handled = true;
            }
        }

        /// <summary>
        /// If we are currently drawing a Document line, this is where the
        /// last glyph of the line is identified.
        /// </summary>
        private void glyph_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this._isResizingDocumentLine = false;
            e.Handled = true;
        }

        /// <summary>
        /// Initiate a Pen Line add operation on the currently 
        /// clicked Glyph
        /// </summary>
        private void glyph_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (this._inputMethod == DocumentAreaInputMehtod.Draw)
            {
                if (this.IsMouseOverDocumentLineRegion(this._lastRecordedMousePos) == true)
                    return;

                //Make sure we are not trying to add a line on an already marked glyph
                if (this._drawingMode == DocumentAreaDrawingMode.PenLine
                    || this._drawingMode == DocumentAreaDrawingMode.MarkerLine)
                {
                    this._isPaintingDocumentLine = true;
                    Glyphs gl = sender as Glyphs;
                    this._currentDocumentLineStart = gl;
                    this.SetCurrentDocumentLineOffsetStart();

                    
                    this._currentDocumentLineEnd = gl;
                    this.SetCurrentDocumentLineOffsetEnd();
                    this._currentlyEditingDocumentLine = this.DocumentLineCollected();

                    this._currentDocumentLineStart = this._currentlyEditingDocumentLine.Start.Glyphs;
                    this._currentDocumentLineEnd = this._currentlyEditingDocumentLine.End.Glyphs;
                    this._currentlySelectedDocumentLine = this._currentlyEditingDocumentLine;

                    this._isResizingDocumentLineStart = false;
                    this._isResizingDocumentLine = true;

					e.Handled = true;
                }
            }
            
        }

        /// <summary>
        /// Calculates the offset in X for the start postion of
        /// the current Pen line.
        /// </summary>
        private void SetCurrentDocumentLineOffsetStart()
        {
            //Set to operate on parent as a fixed page to work with Printed Xps
            if (this._currentDocumentLineStart == null)
                throw new ApplicationException("The current pen line start glyph has not been set");

            Canvas fp = this._currentDocumentLineStart.Parent as Canvas;
            Point p;

            if (fp == null)
                p = Mouse.GetPosition((Panel)this._currentDocumentLineStart.Parent);
            else
                p = Mouse.GetPosition(fp);

            this._currentDocumentLineOffsetStart = p.X - this._currentDocumentLineStart.OriginX;
        }

        /// <summary>
        /// Calculates the offset in X for the end postion of
        /// the current Pen line.
        /// </summary>
        private void SetCurrentDocumentLineOffsetEnd()
        {
            //Set to operate on parent as a fixed page to work with Printed Xps
            if (this._currentDocumentLineEnd == null)
                throw new ApplicationException("The current pen line end glyph has not been set");

            Canvas fp = this._currentDocumentLineEnd.Parent as Canvas;
            Point p;

            if (fp == null)
                p = Mouse.GetPosition((Panel)this._currentDocumentLineEnd.Parent);
            else
                p = Mouse.GetPosition(fp);

            this._currentDocumentLineOffsetEnd = p.X - this._currentDocumentLineEnd.OriginX;

        }

        /// <summary>
        /// When all the necessary data for a new Pen line has been addded,
        /// this method will add the actual UiElements to the Page that contains
        /// the glyphs affected by the new pen line.
        /// </summary>
        private DocumentLine DocumentLineCollected()
        {
            E_DocumentEntityAdornerType lineType = E_DocumentEntityAdornerType.None;
            if (this._drawingMode == DocumentAreaDrawingMode.PenLine)
                lineType = E_DocumentEntityAdornerType.PenLine;
            else if (this._drawingMode == DocumentAreaDrawingMode.MarkerLine)
                lineType = E_DocumentEntityAdornerType.MarkerLine;

            Helpers.DocumentLine newPenLine =
                new Helpers.DocumentLine
                {
                    Id = EjpLib.Helpers.IdManipulation.GetNewGuid(),
                    StartPageNumber = this._dA_DV_DocumentViewer.MasterPageNumber,
                    ParentDocumentId = this._document.InternalDocumentId,
                    ParentStudyId = this._document.ParentStudyId,
                    LineType = lineType
                };

            if (this._currentDocumentLineStart == this._currentDocumentLineEnd)
            {
                double width = this._currentDocumentLineOffsetEnd - this._currentDocumentLineOffsetStart;

				//Set the minimum length of the line.
				width = (width > 1) ? width : 1;

                //if width is negative that means the user has dragged a line backwards on the same set of glyphs.
                if (width < 0.00000)
                {
                    width = width - (width * 2);
                    this._currentDocumentLineOffsetStart = this._currentDocumentLineOffsetEnd;
                }

                PenLinePart lPart = new PenLinePart();
                if (this._drawingMode == DocumentAreaDrawingMode.MarkerLine)
                {
                    lPart = this.DrawDocumentLineForGlyphs(
                        this._currentDocumentLineStart, Enumerations.PenLineGlyphsPosition.Start,
                        this._currentDocumentLineOffsetStart, width, true, true, lineType,
                        this._currentMarkerlineBrush.Color);
                }
                else if (this._drawingMode == DocumentAreaDrawingMode.PenLine)
                {
                    lPart = this.DrawDocumentLineForGlyphs(
                        this._currentDocumentLineStart, Enumerations.PenLineGlyphsPosition.Start,
                        this._currentDocumentLineOffsetStart, width, true, true, lineType,
                        this._currentPenlineBrush.Color);
                }

                //Wrap the new PenLinePart into the new PenLine and add this new
                //Pen line to the list of Pen Lines asc. with the current document.
                lPart.ParentPenLineId = newPenLine.Id;
                newPenLine.Start = lPart;
                newPenLine.End = lPart;
                newPenLine.AddPart(lPart);

                //Tag the start and end handles with the pen line
                //for backwards referencing while editing.
                lPart.EndHandle.Tag = newPenLine;
                lPart.StartHandle.Tag = newPenLine;

                this._currentDocumentLines.Add(newPenLine);

                this.AddActionToUndoList(newPenLine, null, UndoRedoActions.AddDocumentLine, new Color(), null);
                this.RedoStack.Clear();
            }
            else
            {
                int startIndex = this._currentDocumentGlyphs.IndexOf(this._currentDocumentLineStart) + 1;
                int endIndex = this._currentDocumentGlyphs.IndexOf(this._currentDocumentLineEnd);

                //if the startindex is greater than the endindex, the line has been drawn from 
                //bottom to top. In this case we need to reverse the line.
                if (startIndex > endIndex)
                {
                    double tempStart = this._currentDocumentLineOffsetStart;
                    this._currentDocumentLineOffsetStart = this._currentDocumentLineOffsetEnd;
                    this._currentDocumentLineOffsetEnd = tempStart;

                    Glyphs tempStartGlyphs = this._currentDocumentLineStart;
                    this._currentDocumentLineStart = this._currentDocumentLineEnd;
                    this._currentDocumentLineEnd = tempStartGlyphs;

                    startIndex = this._currentDocumentGlyphs.IndexOf(this._currentDocumentLineStart) + 1;
                    endIndex = this._currentDocumentGlyphs.IndexOf(this._currentDocumentLineEnd);

                }

                PenLinePart lPart = new PenLinePart();
                double width = this._currentDocumentLineStart.ToGlyphRun().ComputeAlignmentBox().Width - this._currentDocumentLineOffsetStart;
                if (this._drawingMode == DocumentAreaDrawingMode.MarkerLine)
                {
                    lPart = this.DrawDocumentLineForGlyphs(
                        this._currentDocumentLineStart, Enumerations.PenLineGlyphsPosition.Start,
                    this._currentDocumentLineOffsetStart, width, true, false, lineType,
                    this._currentMarkerlineBrush.Color);

                }
                else if (this._drawingMode == DocumentAreaDrawingMode.PenLine)
                {
                    lPart = this.DrawDocumentLineForGlyphs(
                        this._currentDocumentLineStart, Enumerations.PenLineGlyphsPosition.Start,
                    this._currentDocumentLineOffsetStart, width, true, false, lineType,
                    this._currentPenlineBrush.Color);
                }
                
                //Wrap the new PenLinePart into the new PenLine
                lPart.ParentPenLineId = newPenLine.Id;
                newPenLine.Start = lPart;
                newPenLine.AddPart(lPart);

                //Tag the start handle with the pen line
                //for backwards referencing while editing.
                lPart.StartHandle.Tag = newPenLine;

                for (int i = startIndex; i < endIndex; i++)
                {
                    PenLinePart lPartM = new PenLinePart();
                    width = this._currentDocumentGlyphs[i].ToGlyphRun().ComputeAlignmentBox().Width;
                    if (this._drawingMode == DocumentAreaDrawingMode.MarkerLine)
                    {
                        lPartM = this.DrawDocumentLineForGlyphs(
                        this._currentDocumentGlyphs[i], Enumerations.PenLineGlyphsPosition.Intermediate,
                        0, width, false, false, lineType, this._currentMarkerlineBrush.Color);
                    }
                    else if (this._drawingMode == DocumentAreaDrawingMode.PenLine)
                    {
                        lPartM = this.DrawDocumentLineForGlyphs(
                        this._currentDocumentGlyphs[i], Enumerations.PenLineGlyphsPosition.Intermediate,
                        0, width, false, false, lineType, this._currentPenlineBrush.Color);
                    }

                    //Wrap the new PenLinePart into the new PenLine
                    lPartM.ParentPenLineId = newPenLine.Id;
                    newPenLine.AddPart(lPartM);
                }

                PenLinePart lPartE = new PenLinePart(); 
                width = this._currentDocumentLineOffsetEnd;
                if (this._drawingMode == DocumentAreaDrawingMode.MarkerLine)
                {
                    lPartE = this.DrawDocumentLineForGlyphs(
                    this._currentDocumentLineEnd, Enumerations.PenLineGlyphsPosition.End,
                    0, width, false, true, lineType, this._currentMarkerlineBrush.Color);
                }
                else if (this._drawingMode == DocumentAreaDrawingMode.PenLine)
                {
                    lPartE = this.DrawDocumentLineForGlyphs(
                    this._currentDocumentLineEnd, Enumerations.PenLineGlyphsPosition.End,
                    0, width, false, true, lineType, this._currentPenlineBrush.Color);
                }
               

                //Wrap the new PenLinePart into the new PenLine
                lPartE.ParentPenLineId = newPenLine.Id;
                newPenLine.End = lPartE;
                newPenLine.AddPart(lPartE);

                //Tag the start handle with the pen line
                //for backwards referencing while editing.
                lPartE.EndHandle.Tag = newPenLine;

                //Add the new Pen Line into the list of Pen Lines asc.
                //with the current document.
                this._currentDocumentLines.Add(newPenLine);

                this.AddActionToUndoList(newPenLine, null, UndoRedoActions.AddDocumentLine, new Color(), null);
                this.RedoStack.Clear();
            }

            return newPenLine;
        }

        /// <summary>
        /// Draw an actual Pen Line into the current document.
        /// </summary>
        private Helpers.PenLinePart DrawDocumentLineForGlyphs(Glyphs glyphs, Enumerations.PenLineGlyphsPosition position, 
            double offsetStart, double width, bool drawStartHandle, bool drawEndHandle, 
            E_DocumentEntityAdornerType lineType, Color color)
        {

            if (width < 0.000)
                width = 1;

            //Set to operate on parent as a fixed page to work with Printed Xps
            Canvas parentContainer = glyphs.Parent as Canvas;

            Rectangle docLine = new Rectangle();
            Rectangle penLineStartHandle = new Rectangle { Width = 0 };
            Rectangle penLineEndHandle = new Rectangle { Width = 0 };
			
			DrawingBrush lineHandleSBrush = this.Resources["lineHandleS_default"] as DrawingBrush;
			DrawingBrush lineHandleEBrush = this.Resources["lineHandleE_default"] as DrawingBrush;

            Helpers.PenLinePart newPart = new Helpers.PenLinePart
            {
                Line = docLine,
                EndHandle = penLineEndHandle,
                StartHandle = penLineStartHandle,
                Glyphs = glyphs,
                Position = position,
                OffsetStart = offsetStart,
                PageNumber = this._currentPageNumber
            };

            double lineHeight = 0;
            double handlHeight = 0;
            double lineTop = 0;
            double handleTop = 0;

            if (lineType == E_DocumentEntityAdornerType.PenLine)
            {
                lineHeight = 1.2;
                lineTop = glyphs.OriginY + (3 - lineHeight);
                handleTop = (glyphs.OriginY + 3) - glyphs.FontRenderingEmSize;
                handlHeight = glyphs.FontRenderingEmSize;
            }
            else if (lineType == E_DocumentEntityAdornerType.MarkerLine)
            {
                lineHeight = glyphs.FontRenderingEmSize - 2;
                lineTop = glyphs.OriginY - lineHeight;
                handleTop = glyphs.OriginY - (lineHeight);
                handlHeight = glyphs.FontRenderingEmSize -2;
            }

            switch (position)
            {
                case PenLineGlyphsPosition.Start:
                    docLine.Height = lineHeight;
                    docLine.Width = width;
                    Canvas.SetTop(docLine, lineTop);
                    Canvas.SetLeft(docLine, glyphs.OriginX + offsetStart);
                    parentContainer.Children.Add(docLine);

                    break;
                case PenLineGlyphsPosition.Intermediate:
                    docLine.Height = lineHeight;
                    docLine.Width = width;
                    Canvas.SetTop(docLine, lineTop);
                    Canvas.SetLeft(docLine, glyphs.OriginX);
                    parentContainer.Children.Add(docLine);

                    break;
                case PenLineGlyphsPosition.End:
                    docLine.Height = lineHeight;
                    docLine.Width = width;
                    Canvas.SetTop(docLine, lineTop);
                    Canvas.SetLeft(docLine, glyphs.OriginX);
                    parentContainer.Children.Add(docLine);

                    break;
                default:
                    break;
            }

            if (drawStartHandle)
            {
                penLineStartHandle.Fill = lineHandleSBrush;
                penLineStartHandle.Height = handlHeight;
                penLineStartHandle.Width = 9;
                penLineStartHandle.Cursor = Cursors.Arrow;
                penLineStartHandle.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(DocLineHandleStart_MLB_Down);
                Canvas.SetTop(penLineStartHandle, handleTop);
                Canvas.SetLeft(penLineStartHandle, (glyphs.OriginX + offsetStart));
                parentContainer.Children.Add(penLineStartHandle);
            }

            if (drawEndHandle)
            {
                penLineEndHandle.Fill = lineHandleEBrush;
                penLineEndHandle.Height = handlHeight;
                penLineEndHandle.Width = 9;
                penLineEndHandle.Cursor = Cursors.Arrow;
                penLineEndHandle.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(DocLineHandleEnd_MLB_Down);
                Canvas.SetTop(penLineEndHandle, handleTop);
                Canvas.SetLeft(penLineEndHandle, glyphs.OriginX + offsetStart + (width - 9));
                parentContainer.Children.Add(penLineEndHandle);
            }

            docLine.Fill = new SolidColorBrush(color);

            if (lineType == E_DocumentEntityAdornerType.MarkerLine)
            {
                docLine.PreviewMouseMove += new MouseEventHandler(docMarkerLine_PreviewMouseMove);
            }

            //Set the coordinate of the new line.
            newPart.LineBrush = new SolidColorBrush(color);
            newPart.LineTop = (double)docLine.GetValue(Canvas.TopProperty);
            newPart.BoxHeight = glyphs.FontRenderingEmSize-2;
            newPart.BoxTop = glyphs.OriginY - (glyphs.FontRenderingEmSize);//-2);
            newPart.BoxLeft = (double)docLine.GetValue(Canvas.LeftProperty);
            return newPart;
        }

        /// <summary>
        /// Deletes the given documentline from the document.
        /// </summary>
        /// <param name="dl"></param>
        private void DeleteDocumentLineFromDocument(DocumentLine dl)
        {
            if (dl.HasComment)
            {
                this.CloseOpenCommentNote();
                Panel commentParentP = dl.End.Glyphs.Parent as Panel;
                if (commentParentP.Children.Contains(dl.CommentIcon))
                    commentParentP.Children.Remove(dl.CommentIcon);
            }

            foreach (PenLinePart plp in dl.LineParts)
            {
                Panel parentP = plp.Glyphs.Parent as Panel;
                parentP.Children.Remove(plp.Line);
                parentP.Children.Remove(plp.StartHandle);
                parentP.Children.Remove(plp.EndHandle);
            }

            this.FireDocumentLineDeletedEvent(dl);
            this._currentDocumentLines.Remove(dl);
        }

        /// <summary>
        /// Deletes the given imageborder from the document.
        /// </summary>
        /// <param name="ib"></param>
        private void DeleteImageBorderFromDocument(Border ib)
        {
            this.DeSelectCurrentlySelectedGFXObject();
            Panel parent = ib.Parent as Panel;
            parent.Children.Remove(ib);
            this.FireImageLineDeleted((string)ib.Tag, this._document.InternalDocumentId);
            this._imageBorders.Remove(ib);
        }

        private void docMarkerLine_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (this._isResizingDocumentLine)
            {
                try
                {
                    DocumentLine dl = this.HittestDocumentLineRegions(this.UpdateLastRecordedMousePosition(), E_DocumentEntityAdornerType.None);
                    if (dl != null)
                    {
                        this._currentDocumentLineEnd =
                            this._currentlyEditingDocumentLine.GetMouseOverLinePart(this._lastRecordedMousePos, this._currentPageNumber).Glyphs;
                        this.SetCurrentDocumentLineOffsetEnd();
                        this.SetCurrentDocumentLineOffsetStart();
                        e.Handled = true;
                    }
                }
                catch (Exception ex)
                {
					SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - XPS Document Viewer",
								   "Unknwon Error" +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nDocument: " + this._document.XpsDocument.CoreDocumentProperties.Title +
								   "\nError: " + ex.Message);
                }
            }
        }

        private void DocLineHandleEnd_MLB_Down(object sender, MouseButtonEventArgs e)
        {
            if (this._inputMethod == DocumentAreaInputMehtod.Draw ||
                this._inputMethod == DocumentAreaInputMehtod.Select)
            {
                this._isResizingDocumentLineStart = false;
                this._isResizingDocumentLine = true;
                this._currentlyEditingDocumentLine = ((Rectangle)sender).Tag as DocumentLine;

                this._currentDocumentLineStart = this._currentlyEditingDocumentLine.Start.Glyphs;
                this._currentDocumentLineEnd = this._currentlyEditingDocumentLine.End.Glyphs;

                this._currentlySelectedDocumentLine = this._currentlyEditingDocumentLine;

                e.Handled = true;
            }
        }

        private void DocLineHandleStart_MLB_Down(object sender, MouseButtonEventArgs e)
        {
            if (this._inputMethod == DocumentAreaInputMehtod.Draw ||
                this._inputMethod == DocumentAreaInputMehtod.Select)
            {
                this._isResizingDocumentLineStart = true;
                this._isResizingDocumentLine = true;
                this._currentlyEditingDocumentLine = ((Rectangle)sender).Tag as DocumentLine;

                this._currentDocumentLineStart = this._currentlyEditingDocumentLine.Start.Glyphs;
                this._currentDocumentLineEnd = this._currentlyEditingDocumentLine.Start.Glyphs;

                this._currentlySelectedDocumentLine = this._currentlyEditingDocumentLine;

                e.Handled = true;
            }
        }

        private void DeleteHittestedImageBorders(MouseButtonEventArgs e)
        {
            //Find all the borders that the user clicked within and
            //Remove them from the visual tree
            List<int> indicesToRemove = new List<int>();
            for (int i = this._imageBorders.Count - 1; i >= 0; i--)
            {

                Panel parent = this._imageBorders[i].Parent as Panel;
                if (parent.IsMouseOver == false)
                    continue;

                Point mousePoint = Mouse.GetPosition(this._imageBorders[i]);
                if (mousePoint.X >= 0 && mousePoint.X < this._imageBorders[i].RenderSize.Width 
                    && mousePoint.Y >= 0 && mousePoint.Y < this._imageBorders[i].RenderSize.Height)
                {
                    if (this.OnQueryDocumentLineConnections != null)
                    {
                        int connectedElements =
                            this.OnQueryDocumentLineConnections(Guid.Empty, (string)this._imageBorders[i].Tag, this._document.InternalDocumentId);
                        if (connectedElements > 0)
                        {
                            if (
                            MessageBox.Show(
                            "\n\n" +
                            "いま、あなたが消そうとしている線は、ナレッジマップ上にある \n" +
                            "１つ以上のオブジェクトと関連しています。\n\n" +
                            "この線を消すと、関連するすべてのオブジェクトがナレッジマップ\n" +
                            "からも削除されます。\n\n" +
                            "この動作は取り消せません。それでもこの線を消しますか？" +
                            "\n\n", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                            == MessageBoxResult.No)
                                return;
                        }
                    }
                    parent.Children.Remove(this._imageBorders[i]);
                    indicesToRemove.Add(i);

                    //At this point we only allow removing one border
                    //at the time, so as not remove overlapping ones.
                    //The enumeration walks backwards for automatic
                    //z ordering...
                    break;
                }
            }

            if (indicesToRemove.Count != 0)
                this.FireImageLineDeleted((string)this._imageBorders[indicesToRemove[0]].Tag, this._document.InternalDocumentId);

            //Delete the deleted Borders from the internal list.
            //This wont work with more than 1 index.
            for (int j = 0; j < indicesToRemove.Count; j++)
            {
                this.AddActionToUndoList(null, this._imageBorders[indicesToRemove[j]], 
                    UndoRedoActions.DeleteImageBorder, Colors.Black, null);
                this._imageBorders.RemoveAt(indicesToRemove[j]);
            }

            this.DeSelectCurrentlySelectedGFXObject();

            
        }

        /// <summary>
        /// Callback for mouse clicks on any panel object within 
        /// the document that contains graphics.
        /// </summary>
        private void gfxPanel_MLB_Down(object sender, MouseButtonEventArgs e)
        {
            if (this._inputMethod == DocumentAreaInputMehtod.Erase)
            {
                this.DeleteHittestedImageBorders(e);
            }
            else if (this._inputMethod == DocumentAreaInputMehtod.Draw || 
                this._inputMethod == DocumentAreaInputMehtod.Select)
            {
                System.Windows.Shapes.Path eventOriginPath = 
                    (System.Windows.Shapes.Path)sender;

                bool doDragDrop = false;
                Border hitB = this.GetMouseHitImageBorder();
                if (hitB != null)
                {
                    this.DeSelectCurrentlySelectedGFXObject();
                    this._currentlySelectedImageBorder = hitB;

                    Rectangle r = new Rectangle();
                    r.Fill = new SolidColorBrush(Color.FromArgb(50, 50, 50, 255));
                    this._currentlySelectedImageBorder.Child = r;
                    
                    doDragDrop = true;
                }
                else
                {
                    switch (this._drawingMode)
                    {
                        case DocumentAreaDrawingMode.PenLine:
                            Border pbo = new Border();
                            pbo.Cursor = Cursors.Arrow;
                            pbo.BorderBrush = this._currentPenlineBrush;
                            pbo.BorderThickness = new Thickness(2);
                            pbo.Margin = new Thickness(
                                eventOriginPath.RenderedGeometry.Bounds.Left - 2,
                                eventOriginPath.RenderedGeometry.Bounds.Top - 2,
                                0, 0);
                            pbo.Width = eventOriginPath.RenderedGeometry.Bounds.Width + 4;
                            pbo.Height = eventOriginPath.RenderedGeometry.Bounds.Height + 4;
                            Panel parPp = eventOriginPath.Parent as Panel;
                            parPp.Children.Add(pbo);
                            pbo.Tag = eventOriginPath.Data.ToString();
                            this._imageBorders.Add(pbo);

                            this.AddActionToUndoList(null, pbo, UndoRedoActions.AddImageBorder, Colors.Black, null);
                            this.RedoStack.Clear();
                            break;
                        case DocumentAreaDrawingMode.MarkerLine:
                            Border bo = new Border();
                            bo.Cursor = Cursors.Arrow;
                            bo.BorderBrush = this._currentMarkerlineBrush;
                            bo.BorderThickness = new Thickness(5);
                            bo.Margin = new Thickness(
                                eventOriginPath.RenderedGeometry.Bounds.Left - 5,
                                eventOriginPath.RenderedGeometry.Bounds.Top - 5,
                                0, 0);
                            bo.Width = eventOriginPath.RenderedGeometry.Bounds.Width + 10;
                            bo.Height = eventOriginPath.RenderedGeometry.Bounds.Height + 10;
                            Panel parP = eventOriginPath.Parent as Panel;
                            parP.Children.Add(bo);
                            bo.Tag = eventOriginPath.Data.ToString();
                            this._imageBorders.Add(bo);

                            this.AddActionToUndoList(null, bo, UndoRedoActions.AddImageBorder, Colors.Black, null);
                            this.RedoStack.Clear();
                            break;
                        case DocumentAreaDrawingMode.Freehand:
                            break;
                        default:
                            break;
                    }
                    
                }
                
                if (doDragDrop)
                {

                    Color c = ((SolidColorBrush)hitB.BorderBrush).Color;
                    ImageBrush i = eventOriginPath.Fill as ImageBrush;
                    Helpers.DragDropImage dropImage = new Helpers.DragDropImage
                    {
                        Color = c,
                        TargetPathData = eventOriginPath.Data.ToString(),
                        Reference = new XpsDocumentReference
                        {
                            ParentPathData = eventOriginPath.Data.ToString(),
                            AnchorX = (int)eventOriginPath.Margin.Left,
                            AnchorY = (int)eventOriginPath.Margin.Top,
                            Content = "",
                            PageNumber = this._currentPageNumber,
                            DocumentId = this._document.InternalDocumentId,
                            DocumentParentStudyId = this._parentStudyId
                        },
                        SourceUri = "",
                        IBrush = i
                    };

                    DragDrop.DoDragDrop(this, dropImage, DragDropEffects.All);
                }
            }
        }

        /// <summary>
        /// Searches through all the image borders in the current document 
        /// and returns the first one that contains the current mouse position.
        /// </summary>
        /// <returns>Hittested Border. Null if no matching border found.</returns>
        private Border GetMouseHitImageBorder()
        {
            for (int i = this._imageBorders.Count - 1; i >= 0; i--)
            {
                //This is to make sure we only search the current page.
                Panel parent = this._imageBorders[i].Parent as Panel;
                if (parent.IsMouseOver == false)
                    continue;

                Point mousePoint = Mouse.GetPosition(this._imageBorders[i]);
                if (mousePoint.X >= 0 && mousePoint.Y >= 0
                    && mousePoint.X < this._imageBorders[i].Width
                    && mousePoint.Y < this._imageBorders[i].Height)
                    return this.ImageBorders[i];
            }
            return null;
        }

        #region Internal Helpers

        private void SetInputMethod(DocumentAreaInputMehtod inputMethod)
        {
            switch (inputMethod)
            {
                case DocumentAreaInputMehtod.None:
                    this.SetDocumentWideMouseCursor(Cursors.Arrow);
                    break;
                case DocumentAreaInputMehtod.Select:
                    this.SetDocumentWideMouseCursor(Cursors.Arrow);
                    break;
                case DocumentAreaInputMehtod.Erase:
                    this.SetDocumentWideMouseCursor(Cursors.Cross);
                    break;
                case DocumentAreaInputMehtod.Draw:
                    this.SetDocumentWideMouseCursor(Cursors.Pen);
                    break;
                default:
                    break;
            }
        }

        private void SetDocumentWideMouseCursor(Cursor cursor)
        {
            FixedDocumentSequence seq = (FixedDocumentSequence)this._dA_DV_DocumentViewer.Document;
            foreach (DocumentReference dref in seq.References)
            {
                FixedDocument fd = dref.GetDocument(false);
                foreach (PageContent pc in fd.Pages)
                {
                    FixedPage fp = pc.GetPageRoot(false);
                    fp.Cursor = cursor;
                }
            }
        }

        private Point UpdateLastRecordedMousePosition()
        {
			try
			{
				this._lastRecordedMousePos = Mouse.GetPosition((Canvas)this._currentFixedPage.Children[0]);
				return this._lastRecordedMousePos;
			}
			catch (InvalidCastException ex)
			{
				SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - XPS Document Viewer",
								   "Failed to set last known Mouse Position, the XPS Document is likely not complient with the EJP expected format." +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nDocument: " + this._document.XpsDocument.CoreDocumentProperties.Title +
								   "\nError: " + ex.Message);

				return this._lastRecordedMousePos;
			}
        }

        public void FollowReference(XpsDocumentReference reference)
        {
            //One alternative way to go would be to replicate OnFindInvoked from
            //from the documentviewer control. But then we need to include a 
            //this._findToolbar into the control because that is the mechanism
            //that returns the location of the sought after text.

            Rect target = new Rect();

            if (reference.TargetLineId == null)
            {
                target = new Rect(reference.AnchorX, reference.AnchorY,
                            this._dA_DV_DocumentViewer.Document.DocumentPaginator.GetPage(0).ContentBox.Width, 30);
            }
            else if (reference.TargetLineId != Guid.Empty)
            {
                foreach (DocumentLine dl in this._currentDocumentLines)
                {
                    if (dl.Id == reference.TargetLineId)
                    {
                        target = new Rect(dl.Anchor.X, dl.Anchor.Y,
                            this._dA_DV_DocumentViewer.Document.DocumentPaginator.GetPage(0).ContentBox.Width, 30);
                    }
                }
            }
            else if (reference.ParentPathData != "")
            {
                foreach (System.Windows.Shapes.Path dPath in this._currentDocumentMarkablePaths)
                {
                    if (dPath.Data.ToString() == reference.ParentPathData)
                    {
                        target = dPath.RenderedGeometry.Bounds;
                    }
                }
            }

            //OnBringIntoView takes a Visual as its argument.
            Type t = this._dA_DV_DocumentViewer.GetType();
            MethodInfo inf = t.GetMethod("OnBringIntoView", BindingFlags.NonPublic | BindingFlags.Instance);
            inf.Invoke(this._dA_DV_DocumentViewer,
                new Object[] 
                    {
                        this._dA_DV_DocumentViewer.Document.DocumentPaginator.GetPage(reference.PageNumber-1).Visual,
                        target,
                        reference.PageNumber
                    }
                );
        }

        #region Undo Redo

        /// <summary>
        /// Adds a new Undo Redo action to the Undo Stack. Only pass in the 
        /// parameters that are relevent to the designated action.
        /// </summary>
        /// <param name="line">The line that this operation operates on.</param>
        /// <param name="action">The action to add.</param>
        /// <param name="color">The previous color, null if this does not apply.</param>
        /// <param name="comment">The precious comment, null if this does not apply.</param>
        private void AddActionToUndoList(DocumentLine line, Border imageBorder, UndoRedoActions action, 
            Color color, DocumentLineComment comment)
        {
            try
            {
                UndoRedoAction ura = new UndoRedoAction { Action = action, DocumentLine = line, ImageBorder = imageBorder };

                switch (action)
                {
                    case UndoRedoActions.SetColor:
                        ura.LineColor = color;
                        break;
                    case UndoRedoActions.RemoveComment:
                        ura.Comment = comment;
                        break;
                    default:
                        break;
                }

                this.UndoStack.Push(ura);
                Debug.Print("UndoStack no contains: " + this.UndoStack.Count.ToString());
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - XPS Document Viewer",
								   "Set Undo Action Failed" +
								   "\nAction: " + action.ToString() +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nDocument: " + this._document.XpsDocument.CoreDocumentProperties.Title +
								   "\nError: " + ex.Message);
            }
        }

        /// <summary>
        /// Propagates the Undo command causing it to be invoked on this Control.
        /// </summary>
        public void PropagateUndo()
        {
            try
            {
                UndoRedoAction ura = this.UndoStack.Pop();
                switch (ura.Action)
                {
                    case UndoRedoActions.DeleteDocumentLine:
                        this.ReInsertDocumentLineIntoDocument(ura.DocumentLine);
                        break;
                    case UndoRedoActions.AddDocumentLine:

                        if (this.OnQueryDocumentLineConnections != null)
                        {
                            int connectedElements = this.OnQueryDocumentLineConnections(ura.DocumentLine.Id, "", this._document.InternalDocumentId);
                            if (connectedElements > 0)
                            {
                                if (
                                   MessageBox.Show(
                                    "\n\n" +
                                    "元に戻せば線が消えます。その線は、ナレッジマップ上にある \n" +
                                    "１つ以上のオブジェクトと関連しています。\n\n" +
                                    "この線を消すと、関連するすべてのオブジェクトがナレッジマップ\n" +
                                    "からも削除されます。\n\n" +
                                    "それでもこの線を消しますか？" +
                                    "\n\n", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                                    == MessageBoxResult.No)
                                {
                                    this.UndoStack.Push(ura);
                                    return;
                                }
                            }
                        }

                        this.DeleteDocumentLineFromDocument(ura.DocumentLine);
                        break;
                    case UndoRedoActions.DeleteImageBorder:
                        this.ReInsertImageBorderIntoDocument(ura.ImageBorder);
                        break;
                    case UndoRedoActions.AddImageBorder:

                        if (this.OnQueryDocumentLineConnections != null)
                        {
                            int connectedElements = this.OnQueryDocumentLineConnections(Guid.Empty, (string)ura.ImageBorder.Tag, this._document.InternalDocumentId);
                            if (connectedElements > 0)
                            {
                                if (
                                   MessageBox.Show(
                                    "\n\n" +
                                    "元に戻せば線が消えます。その線は、ナレッジマップ上にある \n" +
                                    "１つ以上のオブジェクトと関連しています。\n\n" +
                                    "この線を消すと、関連するすべてのオブジェクトがナレッジマップ\n" +
                                    "からも削除されます。\n\n" +
                                    "それでもこの線を消しますか？" +
                                    "\n\n", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                                    == MessageBoxResult.No)
                                {
                                    this.UndoStack.Push(ura);
                                    return;
                                }
                            }
                        }

                        this.DeleteImageBorderFromDocument(ura.ImageBorder);
                        break;
                    case UndoRedoActions.SetColor:
                        break;
                    case UndoRedoActions.AddComment:
                        break;
                    case UndoRedoActions.RemoveComment:
                        break;
                    default:
                        break;
                }
                this.RedoStack.Push(ura);
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - XPS Document Viewer",
								   "Failed to propagate Undo Action" +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nDocument: " + this._document.XpsDocument.CoreDocumentProperties.Title +
								   "\nError: " + ex.Message);
            }
        }

        public void PropagateRedo()
        {
            try
            {
                UndoRedoAction rua = this.RedoStack.Pop();
                switch (rua.Action)
                {
                    case UndoRedoActions.DeleteDocumentLine:
                        this.DeleteDocumentLineFromDocument(rua.DocumentLine);
                        break;
                    case UndoRedoActions.AddDocumentLine:
                        ReInsertDocumentLineIntoDocument(rua.DocumentLine);
                        break;
                    case UndoRedoActions.DeleteImageBorder:
                        this.DeleteImageBorderFromDocument(rua.ImageBorder);
                        break;
                    case UndoRedoActions.AddImageBorder:
                        this.ReInsertImageBorderIntoDocument(rua.ImageBorder);
                        break;
                    case UndoRedoActions.SetColor:
                        break;
                    case UndoRedoActions.AddComment:
                        break;
                    case UndoRedoActions.RemoveComment:
                        break;
                    default:
                        break;
                }
                this.UndoStack.Push(rua);
                Debug.Print("REDO: UndoStack no contains: " + this.UndoStack.Count.ToString());
                Debug.Print("REDO: RedoStack no contains: " + this.RedoStack.Count.ToString());
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - XPS Document Viewer",
								   "Failed to propagate Redo Action" +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nDocument: " + this._document.XpsDocument.CoreDocumentProperties.Title +
								   "\nError: " + ex.Message);
            }
        }

        /// <summary>
        /// This method is part of the Undo/Redo system.
        /// </summary>
        /// <param name="dl">Border to be RE-Inserted into the document.</param>
        private void ReInsertImageBorderIntoDocument(Border ib)
        {
            foreach (System.Windows.Shapes.Path parP in this._currentDocumentMarkablePaths)
            {
                if (parP.Data.ToString() == (string)ib.Tag)
                {
                    Panel parPp = parP.Parent as Panel;
                    parPp.Children.Add(ib);
                    this._imageBorders.Add(ib);
                    break;
                }
            }
        }

        /// <summary>
        /// This method is part of the Undo/Redo system.
        /// </summary>
        /// <param name="dl">Line to be RE-Inserted into the document.</param>
        private void ReInsertDocumentLineIntoDocument(DocumentLine dl)
        {
            List<PenLinePart> newLineParts = new List<PenLinePart>();
            PenLinePart newStart = null;
            PenLinePart newEnd = null;
            foreach (PenLinePart plp in dl.LineParts)
            {
                bool drawStart = false;
                bool drawEnd = false;

                if (plp == dl.Start)
                    drawStart = true;
                if (plp == dl.End)
                    drawEnd = true;

                PenLinePart newPart = this.DrawDocumentLineForGlyphs(plp.Glyphs, PenLineGlyphsPosition.Start,
                    plp.OffsetStart, plp.Line.Width, drawStart, drawEnd, dl.LineType,
                    plp.LineBrush.Color);

                if (drawStart)
                {
                    newPart.StartHandle.Tag = dl;
                    newStart = newPart;
                }
                if (drawEnd)
                {
                    newPart.EndHandle.Tag = dl;
                    newEnd = newPart;
                }
                newLineParts.Add(newPart);
            }

            dl.LineParts.Clear();
            foreach (PenLinePart newPlp in newLineParts)
                dl.LineParts.Add(newPlp);

            dl.Start = newStart;
            dl.End = newEnd;

            this._currentDocumentLines.Add(dl);
            if (dl.HasComment)
            {
                ((Canvas)dl.Start.Glyphs.Parent).Children.Add(dl.CommentIcon);
            }
        }

        #endregion


        /// <summary>
        /// Sets the current pen line color and optionally updates any selected
        /// pen line in the document.
        /// </summary>
        public void PropagatePenColorChange(SolidColorBrush newColor, bool updateSelectedLine)
        {
            this._currentPenlineBrush = newColor;
            if (updateSelectedLine)
            {
                if (this._currentlySelectedDocumentLine != null)
                {
                    if (this._currentlySelectedDocumentLine.LineType == E_DocumentEntityAdornerType.PenLine)
                    {
                        this._currentlySelectedDocumentLine.SetColor(newColor);
                        this.FireDocumentLineContentsChangedEvent(this._currentlySelectedDocumentLine);
                    }
                }


                if (this._currentlySelectedImageBorder != null)
                {
                    //Ugly way to tell the difference between Pen and Marker
                    if (this._currentlySelectedImageBorder.BorderThickness.Left == 2)
                    {
                        this._currentlySelectedImageBorder.BorderBrush = newColor;
                        this.FireImageLineColorChanged(newColor.Color, (string)this._currentlySelectedImageBorder.Tag);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the current marker line color and optionally updates any selected
        /// marker line in the document.
        /// </summary>
        public void PropagateMarkerColorChange(SolidColorBrush newColor, bool updateSelectedLine)
        {
            this._currentMarkerlineBrush = newColor;
            if (updateSelectedLine)
            {
                if (this._currentlySelectedDocumentLine != null)
                {
                    if (this._currentlySelectedDocumentLine.LineType == E_DocumentEntityAdornerType.MarkerLine)
                    {
                        this._currentlySelectedDocumentLine.SetColor(newColor);
                        this.FireDocumentLineContentsChangedEvent(this._currentlySelectedDocumentLine);
                    }
                }
                if (this._currentlySelectedImageBorder != null)
                {
                    //Ugly way to tell the difference between Pen and Marker
                    if (this._currentlySelectedImageBorder.BorderThickness.Left == 5)
                    {
                        this._currentlySelectedImageBorder.BorderBrush = newColor;
                        this.FireImageLineColorChanged(newColor.Color, (string)this._currentlySelectedImageBorder.Tag);
                    }
                }
            }
        }

        public void PropagateLinkedEntityColorChanged(Guid linkedLineId, string targetPathData, SolidColorBrush newColor)
        {
            foreach (DocumentLine dl in this._currentDocumentLines)
            {
                if (dl.Id == linkedLineId)
                {
                    if (dl.LineType == E_DocumentEntityAdornerType.MarkerLine)
                    {
                        dl.SetColor(new SolidColorBrush(Color.FromArgb(128, newColor.Color.R, newColor.Color.G, newColor.Color.B)));
                    }
                    else
                        dl.SetColor(newColor);
                }
            }
            foreach (Border b in this._imageBorders)
            {
                if ((string)b.Tag == targetPathData)
                    b.BorderBrush = newColor;
            }
        }

		public void Close()
		{
			this._dA_DV_DocumentViewer.Document = null;
        }

        #endregion

        #region Document Loading and Document Setup

        /// <summary>
        /// Loads an XPS Document into the control.
        /// This method also hooks up the annotation service 
        /// and makes all the document parts interactive.
        /// </summary>
        public void LoadXpsDocument(EjpLib.BaseClasses.ejpXpsDocument document, Guid parentStudyId)
        {
			try
			{

				//TODO: On enable document parts, perform more strict check for 
                //      complience with expected XPS format,
				//		if fail, return false here and display error message.

				if (this._dA_DV_DocumentViewer.Document != null 
					&& this._document != null)
				{
					/*
					 * 080617
					 * A save as operation has occured and we need to reload the Xps Document displayed
					 * in the DocumentViewer Control.
					 * This is done because the Document currently displayed in the viewer has been 
					 * removed from the PackageStore and thus any images in the Document are no longer valid.
					 * 
					 * The local _document member already holds a reference to the correct ejpXpsDocument, and 
					 * the values of its fields were updated during the save operation, so basically we only
					 * need to reload it using the existing _document member.
					 * 
					 * Includes ugly coding and bad jokes...
					 * Sorry folks...
					 */

					document = this._document;
					parentStudyId = this._parentStudyId;

					this._currentDocumentMarkablePaths.Clear();
					this._currentDocumentGlyphs.Clear();
					this._imageBorders.Clear();
					this._currentDocumentLines.Clear();

					this._dA_DV_DocumentViewer.Document = null;
				}

				this._parentStudyId = parentStudyId;
				this._dA_DV_DocumentViewer.Document = document.XpsDocument.GetFixedDocumentSequence();
				this._document = document;

				FixedDocumentSequence seq = (FixedDocumentSequence)this._dA_DV_DocumentViewer.Document;

				//Set the first page to be the current page on loading a new document.
				DocumentReference dref_0 = seq.References[0];
				FixedDocument fd_0 = dref_0.GetDocument(false);
				PageContent pc_0 = fd_0.Pages[0];
				this._currentFixedPage = pc_0.GetPageRoot(false);
				this._currentPageNumber = 1;

				foreach (DocumentReference dref in seq.References)
				{
					FixedDocument fd = dref.GetDocument(false);
					foreach (PageContent pc in fd.Pages)
					{
						FixedPage fp = pc.GetPageRoot(false);
						fp.MouseEnter += new MouseEventHandler(FixedPage_MouseEnter);

						foreach (UIElement uiel in fp.Children)
						{
							this.EnablePagePart(uiel);
						}
					}
				}

				this.LoadDocumentExtras();
				this._dA_DV_DocumentViewer.FitToWidth();
			}
			catch (Exception ex)
			{
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"EjpControls - XpsDocumentViewer - Control Setup",
					"Loading Xps Document into Viewer Failed" +
					"\nDocument: " + this._document.XpsDocument.CoreDocumentProperties.Title +
					"\nError: " + ex.Message);
				throw new ApplicationException("The XPS Document you tried to load has an unsupported Document Structure. This document cannot be displayed by eJournalPlus.");
			}
        }

        /// <summary>
        /// Makes a UiElement withing a Fixed Page in the document
        /// interactive. Currently only tracks panels containing 
        /// graphics, and glyphs containing text.
        /// </summary>
        private void EnablePagePart(UIElement rootElement)
        {
            if (rootElement is Panel)
            {
                Panel pRoot = rootElement as Panel;
                //We need to check if the panel contains more than one Path with an ImageBrush.
                //If so we have to make the Draggable Image object not the path itself but the
                //parent canvas that holds it. That is why we are both checking it here and in
                //the Else statement below.
                try
                {
                    if (pRoot.Children[0] is System.Windows.Shapes.Path)
                    {
                        System.Windows.Shapes.Path pHolder = pRoot.Children[0] as System.Windows.Shapes.Path;
                        pHolder.Tag = Enumerations.E_DocumentEntityAdornerType.None;
                        if (pHolder.Fill is ImageBrush)
                        {
                            pHolder.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(gfxPanel_MLB_Down);
                            this._currentDocumentMarkablePaths.Add(pHolder);
                        }
                    }
                    foreach (UIElement child in pRoot.Children)
                    {
                        this.EnablePagePart(child);
                    }
                }
                catch (ArgumentOutOfRangeException aorEx)
                {
                    //Empty Page?
                }
            }
            else
            {
                //Found the bottom of this tree.
                if (rootElement is System.Windows.Shapes.Path)
                {
                    System.Windows.Shapes.Path pHolder = rootElement as System.Windows.Shapes.Path;
                    pHolder.Tag = Enumerations.E_DocumentEntityAdornerType.None;
                    if (pHolder.Fill is ImageBrush)
                    {
                        pHolder.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(gfxPanel_MLB_Down);
                        this._currentDocumentMarkablePaths.Add(pHolder);
                    }
                }
                else if (rootElement is Glyphs)
                {
                    Glyphs gl = rootElement as Glyphs;
                    gl.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(glyph_PreviewMouseLeftButtonDown);
                    gl.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(glyph_PreviewMouseLeftButtonUp);
                    gl.PreviewMouseMove += new MouseEventHandler(glyph_PreviewMouseMove);
                    this._currentDocumentGlyphs.Add(gl);
                }
            }
        }

        /// <summary>
        /// Callbacl to keep the reference to the current Fixed Page updated
        /// Also sets the current page number.
        /// </summary>
        private void FixedPage_MouseEnter(object sender, MouseEventArgs e)
        {
            this._currentFixedPage = sender as FixedPage;
            
            FixedDocumentSequence seq = (FixedDocumentSequence)this._dA_DV_DocumentViewer.Document;
            foreach (DocumentReference dref in seq.References)
            {
                int pageCounter = 1;
                FixedDocument fd = dref.GetDocument(false);
                foreach (PageContent pc in fd.Pages)
                {
                    FixedPage fp = pc.GetPageRoot(false);
                    if (fp == this._currentFixedPage)
                        this._currentPageNumber = pageCounter;
                    
                    pageCounter += 1;
                }
            }
        }

        /// <summary>
        /// Push all the document extras into the base lib
        /// representation of the document so that they can
        /// be saved into the package.
        /// </summary>
        public void ExportDocumentExtras()
        {
            //clear out all the lines and borders
            //to make sure we don't save them twice...
            this._document.DocumentLines.Clear();
            this._document.DocumentImageBorders.Clear();

            foreach (DocumentLine dl in this._currentDocumentLines)
            {
                EjpLib.BaseClasses.ejpDocumentLine ejpDl =
                    new SiliconStudio.Meet.EjpLib.BaseClasses.ejpDocumentLine
                        {
                            Id = dl.Id,
                            LineType = (int)dl.LineType,
                            ParentDocumentId = dl.ParentDocumentId,
                            ParentStudyId = dl.ParentStudyId,
                            StartPageNumber = dl.StartPageNumber,
                            Color =
                                new SiliconStudio.Meet.EjpLib.BaseClasses.ejpSolidColor
                                {
                                    _a = dl.GetColor().A,
                                    _b = dl.GetColor().B,
                                    _g = dl.GetColor().G,
                                    _r = dl.GetColor().R
                                }
                        };

                foreach (PenLinePart plp in dl.LineParts)
                {
                    EjpLib.BaseClasses.ejpDocumentLinePart ejpPlp =
                        new SiliconStudio.Meet.EjpLib.BaseClasses.ejpDocumentLinePart
                        {
                            BoxHeight = plp.BoxHeight,
                            BoxLeft = plp.BoxLeft,
                            BoxTop = plp.BoxTop,
                            LineLeft = plp.LineLeft,
                            LineTop = plp.LineTop,
                            OffsetStart = plp.OffsetStart,
                            PageNumber = plp.PageNumber,
                            ParentGlyphName = plp.ParentGlyphName,
                            ParentPenLineId = plp.ParentPenLineId,
                            Position = (int)plp.Position,
                            Width = plp.Width
                        };

                    ejpDl.LineParts.Add(ejpPlp);
                    if (plp == dl.Start)
                        ejpDl.Start = ejpPlp;
                    else if (plp == dl.End)
                        ejpDl.End = ejpPlp;
                }

                ejpDl.HasComment = dl.HasComment;
                if (dl.HasComment == true)
                {
                    ejpDl.LineComment = new EjpLib.BaseClasses.ejpDocumentLineComment
                    {
                        Author = dl.Comment.Author,
                        AuthorId = dl.Comment.AuthorId,
                        Content = dl.Comment.Content,
                        DateAdded = dl.Comment.DateAdded,
                        ParentDocumentId = dl.Comment.ParentDocumentId,
                        ParentDocumentLineId = dl.Comment.ParentDocumentLineId,
                        ParentStudyId = dl.Comment.ParentStudyId
                    };
                }

                this._document.DocumentLines.Add(ejpDl);
            }

            foreach (Border diBorder in this._imageBorders)
            {
                int lineType = 0;
                if (diBorder.BorderThickness.Left == 5)
                    lineType = (int)Enumerations.E_DocumentEntityAdornerType.MarkerLine;
                else
                    lineType = (int)Enumerations.E_DocumentEntityAdornerType.PenLine;

                EjpLib.BaseClasses.ejpDocumentImageBorder ejpDiBorder =
                    new SiliconStudio.Meet.EjpLib.BaseClasses.ejpDocumentImageBorder
                    {
                        Height = diBorder.Height,
                        Width = diBorder.Width,
                        MarginLeft = diBorder.Margin.Left,
                        MarginTop = diBorder.Margin.Top,
                        ParentDocumentId = this._document.InternalDocumentId,
                        ParentStudyId = this._document.ParentStudyId,
                        PathData = (string)diBorder.Tag,
                        LineType = lineType,
                        Id = EjpLib.Helpers.IdManipulation.GetNewGuid(),
                        Color =
                            new SiliconStudio.Meet.EjpLib.BaseClasses.ejpSolidColor
                            {
                                _a = ((SolidColorBrush)diBorder.BorderBrush).Color.A,
                                _r = ((SolidColorBrush)diBorder.BorderBrush).Color.R,
                                _g = ((SolidColorBrush)diBorder.BorderBrush).Color.G,
                                _b = ((SolidColorBrush)diBorder.BorderBrush).Color.B
                            }
                    };
                this._document.DocumentImageBorders.Add(ejpDiBorder);
            }
        }

        private void LoadDocumentExtras()
        {
            if (this._document == null)
                return;

            foreach (EjpLib.BaseClasses.ejpDocumentLine ejpDl in this._document.DocumentLines)
            {
                DocumentLine dl = new DocumentLine
                {
                    ParentDocumentId = ejpDl.ParentDocumentId,
                    ParentStudyId = ejpDl.ParentStudyId,
                    Id = ejpDl.Id,
                    StartPageNumber = ejpDl.StartPageNumber,
                    LineType = (E_DocumentEntityAdornerType)Enum.Parse(typeof(E_DocumentEntityAdornerType), ejpDl.LineType.ToString())
                };

                foreach (EjpLib.BaseClasses.ejpDocumentLinePart ejpDlP in ejpDl.LineParts)
                {
                    Glyphs parentGlyphs = null;
                    foreach (Glyphs g in this._currentDocumentGlyphs)
                    {
                        if (g.Name == ejpDlP.ParentGlyphName)
                            parentGlyphs = g;
                    }

                    //if we cannot find the parent glyphs, ignore this part of the line...
                    if (parentGlyphs == null)
                        continue;

                    bool drawStart = false;
                    bool drawEnd = false;
                    bool setStart = false;
                    bool setEnd = false;

                    if (ejpDlP == ejpDl.Start || ejpDl.LineParts.Count == 1)
                    {
                        drawStart = true;
                        setStart = true;
                    }

                    if ((ejpDlP == ejpDl.End) || ejpDl.LineParts.Count == 1)
                    {
                        drawEnd = true;
                        setEnd = true;
                    }

                    PenLinePart newPart =
                        this.DrawDocumentLineForGlyphs(
                        parentGlyphs,
                        (PenLineGlyphsPosition)Enum.Parse(typeof(PenLineGlyphsPosition), ejpDlP.Position.ToString()),
                        ejpDlP.OffsetStart,
                        ejpDlP.Width,
                        drawStart,
                        drawEnd,
                        dl.LineType,
                        Color.FromArgb(ejpDl.Color._a, ejpDl.Color._r, ejpDl.Color._g, ejpDl.Color._b)
                        );
                    
                    //When loading the page number must be 
                    //read from the saved object.
                    newPart.PageNumber = ejpDlP.PageNumber;

                    if (setStart == true)
                    {
                        dl.Start = newPart;
                        dl.Start.StartHandle.Tag = dl;
                    }

                    if (setEnd == true)
                    {
                        dl.End = newPart;
                        dl.End.EndHandle.Tag = dl;
                    }

                    dl.LineParts.Add(newPart);
                }

                dl.HasComment = ejpDl.HasComment;
                if (ejpDl.HasComment == true)
                {
                    this.SetupCommentInDocumentLine(dl, new DocumentLineComment
                    {
                        Author = ejpDl.LineComment.Author,
                        AuthorId = ejpDl.LineComment.AuthorId,
                        Content = ejpDl.LineComment.Content,
                        DateAdded = ejpDl.LineComment.DateAdded,
                        ParentDocumentId = ejpDl.LineComment.ParentDocumentId,
                        ParentDocumentLineId = ejpDl.LineComment.ParentDocumentLineId,
                        ParentStudyId = ejpDl.LineComment.ParentStudyId
                    });
                }

                this._currentDocumentLines.Add(dl);
              
            }

            foreach (EjpLib.BaseClasses.ejpDocumentImageBorder ejpDiB in this._document.DocumentImageBorders)
            {
                foreach (System.Windows.Shapes.Path dPath in this._currentDocumentMarkablePaths)
                {
                    if (dPath.Data.ToString() == ejpDiB.PathData)
                    {
                        E_DocumentEntityAdornerType LineType =
                            (E_DocumentEntityAdornerType)Enum.Parse(typeof(E_DocumentEntityAdornerType), ejpDiB.LineType.ToString());

                        switch (LineType)
                        {
                            case E_DocumentEntityAdornerType.PenLine:
                                Border pbo = new Border();
                                pbo.Cursor = Cursors.Arrow;
                                pbo.BorderBrush = new SolidColorBrush(
                                    Color.FromArgb(ejpDiB.Color._a, 
                                                    ejpDiB.Color._r, 
                                                    ejpDiB.Color._g, 
                                                    ejpDiB.Color._b)
                                                    );
                                pbo.BorderThickness = new Thickness(2);
                                pbo.Margin = new Thickness(
                                    ejpDiB.MarginLeft,
                                    ejpDiB.MarginTop,
                                    0, 0);
                                pbo.Width = ejpDiB.Width;
                                pbo.Height = ejpDiB.Height;
                                Panel parPp = dPath.Parent as Panel;
                                parPp.Children.Add(pbo);
                                pbo.Tag = dPath.Data.ToString();
                                this._imageBorders.Add(pbo);
                                break;
                            case E_DocumentEntityAdornerType.MarkerLine:
                                Border bo = new Border();
                                bo.Cursor = Cursors.Arrow;
                                bo.BorderBrush = new SolidColorBrush(
                                    Color.FromArgb(ejpDiB.Color._a,
                                                    ejpDiB.Color._r,
                                                    ejpDiB.Color._g,
                                                    ejpDiB.Color._b)
                                                    );
                                bo.BorderThickness = new Thickness(5);
                                bo.Margin = new Thickness(
                                    ejpDiB.MarginLeft,
                                    ejpDiB.MarginTop,
                                    0, 0);
                                bo.Width = ejpDiB.Width;
                                bo.Height = ejpDiB.Height;
                                Panel parP = dPath.Parent as Panel;
                                parP.Children.Add(bo);
                                bo.Tag = dPath.Data.ToString();
                                this._imageBorders.Add(bo);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

        }

        internal void FireDocumentLineDeletedEvent(DocumentLine deletedLine)
        {
            if (this.OnDocumentLineDeleted != null)
                this.OnDocumentLineDeleted(deletedLine.Id);
        }

        internal void FireDocumentLineContentsChangedEvent(DocumentLine updatedLine)
        {
            if (this.OnDocumentLineContentsChanged != null)
            {
                Helpers.DragDropQuote q = new Helpers.DragDropQuote
                {
                    Color = updatedLine.GetColor(),
                    UnicodeString = updatedLine.Contents,
                    Reference = new XpsDocumentReference
                    {
                        TargetLineId = updatedLine.Id,
                        AnchorX = (int)updatedLine.Anchor.X,
                        AnchorY = (int)updatedLine.Anchor.Y,
                        DocumentId = this._document.InternalDocumentId,
                        DocumentParentStudyId = this._parentStudyId,
                        PageNumber = this._currentPageNumber,
                        Content = updatedLine.Contents
                    }
                };
                this.OnDocumentLineContentsChanged(q);
            }
        }

        internal void FireImageLineColorChanged(Color newColor, string targetPathData)
        {
            if(this.OnImageLineColorChanged != null)
                this.OnImageLineColorChanged(newColor, targetPathData);
        }

        internal void FireImageLineDeleted(string targetPathData, Guid lineParentDocumentId)
        {
            if (this.OnImageLineDeleted != null)
                this.OnImageLineDeleted(targetPathData, lineParentDocumentId);
        }

        private void OnToggleScaleLock(object sender, RoutedEventArgs e)
        {
            this._isScaleLockActive = !this._isScaleLockActive;
            if (this._isScaleLockActive == true)
                this.UpdateCurrentlyDisplayedPageScale();
        }

        private void OnZoomDocumentOut(object sender, RoutedEventArgs e)
        {
			DebugManagers.DebugReporter.ReportMethodEnter();

            if((bool)this._tb_ScaleLock.IsChecked)
                this._tb_ScaleLock.IsChecked = false;

			this._sl_ZoomSlider.Value = this._dA_DV_DocumentViewer.Zoom;

			DebugManagers.DebugReporter.ReportMethodLeave();
        }

        private void OnZoomDocumentIn(object sender, RoutedEventArgs e)
        {
            if ((bool)this._tb_ScaleLock.IsChecked)
                this._tb_ScaleLock.IsChecked = false;

			this._sl_ZoomSlider.Value = this._dA_DV_DocumentViewer.Zoom;

        }

		private bool t_IsColdZoomUpdate;

		public void UpdateCurrentlyDisplayedPageScale()
		{
			DebugManagers.DebugReporter.ReportMethodEnter();

			t_IsColdZoomUpdate = true;
			DocumentViewer.FitToWidthCommand.Execute(1, this._dA_DV_DocumentViewer);
			this.SetZoomSliderValue(false);
			t_IsColdZoomUpdate = false;

			DebugManagers.DebugReporter.ReportMethodLeave();
		}

		private void OnKnowledgeMapZoomChanged(object sender, RoutedEventArgs e)
		{
			DebugManagers.DebugReporter.ReportMethodEnter();

			if (!t_IsColdZoomUpdate)
			{
				this._dA_DV_DocumentViewer.Zoom = this._sl_ZoomSlider.Value;
				this.SetZoomSliderValue(true);
			}

			DebugManagers.DebugReporter.ReportMethodLeave();
		}

		private void SetZoomSliderValue(bool UncheckScaleLock)
		{
			DebugManagers.DebugReporter.ReportMethodEnter();

			if (UncheckScaleLock)
			{
				if ((bool)this._tb_ScaleLock.IsChecked)
					this._tb_ScaleLock.IsChecked = false;
			}

			this._sl_ZoomSlider.Value = this._dA_DV_DocumentViewer.Zoom;

			

			DebugManagers.DebugReporter.ReportMethodLeave();
		}

        #endregion

        #region Annotations Related

        public void SaveAnnotations()
		{
			if (this._annotationService != null)
			{
				this._annotationService.Store.Flush();
				this._annotationStream.Flush();
			}
		}

		private void StartAnnotationsService()
		{
			if (this._annotationService == null)
			{
				this._annotationStream = GetAnnotationPart(this._document.FixedDocSeqUri).GetStream();
				this._annotationService = new AnnotationService(this._dA_DV_DocumentViewer);
				this._annotationService.Enable(new XmlStreamStore(this._annotationStream));
			}
			else if (!this._annotationService.IsEnabled)
				this._annotationService.Enable(this._annotationService.Store);
		}

		private void StopAnnotationsService()
		{
			if ((this._annotationService != null) && this._annotationService.IsEnabled)
			{
				this._annotationService.Store.Flush();
				this._annotationStream.Flush();
				this._annotationStream.Close();
			}

			if (this._annotationService != null)
			{
				if (this._annotationService.IsEnabled)
					this._annotationService.Disable();
				this._annotationService = null;
			}
		}

		private PackagePart GetAnnotationPart(Uri documentUri)
		{
			Package package = PackageStore.GetPackage(this._document.PackageUri);
			if (package == null)
			{
				throw new InvalidOperationException(
					"The document package '" + this._document.PackageUri.ToString() + "' does not exist.");
			}

			PackagePart docPart = package.GetPart(documentUri);

			PackageRelationship annotRel = null;
			foreach (PackageRelationship rel in
				docPart.GetRelationshipsByType(AssignmentPackagePartRelationship.Annotations_v1))
			{
				annotRel = rel;
			}

			PackagePart annotPart = null;
			if (annotRel == null)
			{
				annotPart = package.CreatePart(PackUriHelper.CreatePartUri(
					new Uri("/Annotations.xml", UriKind.Relative)), "application/vnd.ms-package.annotations+xml");
				docPart.CreateRelationship(
					annotPart.Uri, TargetMode.Internal, AssignmentPackagePartRelationship.Annotations_v1);
			}

			else
			{   
				annotPart = package.GetPart(annotRel.TargetUri);
				if (annotPart == null)
				{
					throw new InvalidOperationException(
						"The Annotations part referenced by the Annotations " +
						"Relationship TargetURI '" + annotRel.TargetUri +
						"' could not be found.");
				}
			}

			return annotPart;
        }

        #endregion

        #region Undo Redo

        private enum UndoRedoActions
        {
            DeleteDocumentLine,
            AddDocumentLine,
            DeleteImageBorder,
            AddImageBorder,
            SetColor,
            AddComment,
            RemoveComment,
        }

        private class UndoRedoAction
        {
            public Border ImageBorder { get; set; }
            public DocumentLine DocumentLine { get; set; }
            public UndoRedoActions Action { get; set; }
            public DocumentLineComment Comment { get; set; }
            public Color LineColor { get; set; }

            public UndoRedoAction()
            {
            }
        }

        #endregion

        public void DeleteSelection()
        {
            if (this._currentlySelectedDocumentLine != null)
            {
                if (this.OnQueryDocumentLineConnections != null)
                {
                    int connectedElements = this.OnQueryDocumentLineConnections(
                        this._currentlySelectedDocumentLine.Id, "", this._document.InternalDocumentId);
                    if (connectedElements > 0)
                    {
                        if (
                            MessageBox.Show(
                            "\n\n"+
                            "いま、あなたが消そうとしている線は、ナレッジマップ上にある \n" +
                            "１つ以上のオブジェクトと関連しています。\n\n" +
                            "この線を消すと、関連するすべてのオブジェクトがナレッジマップ\n"+
                            "からも削除されます。\n\n"+
                            "この動作は取り消せません。それでもこの線を消しますか？" +
                            "\n\n", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                            == MessageBoxResult.No)
                            return;
                    }
                }

                this.DeleteDocumentLineFromDocument(this._currentlySelectedDocumentLine);
                this.AddActionToUndoList(this._currentlySelectedDocumentLine, null, 
                    UndoRedoActions.DeleteDocumentLine, Colors.Black, null);
                this._currentlySelectedDocumentLine = null;
            }
        }
    }
}