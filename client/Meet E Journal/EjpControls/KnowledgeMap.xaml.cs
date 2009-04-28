using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using SiliconStudio.Meet.EjpControls.Enumerations;
using System.Text;

namespace SiliconStudio.Meet.EjpControls
{
    /// <summary>
    /// Interaction logic for KnowledgeMap.xaml
    /// </summary>
    [Serializable]
    public enum KnowledgeMapInputState
    {
        None,
        Select,
        Freehand,
        Gesture,
        Label,
        EraseByPoint,
        EraseByStroke,
        Line,
        Square,
        Circle,
        SingleArrow,
        DoubleArrow,
        Note,
        FreehandSquare,
        FreehandCircle,
        PushPin
    }

    [Serializable]
    public enum StrokeArrowMode
    {
        None,
        Single,
        Double
    }

    public delegate void EntityRequestedReferenceNavigate(XpsDocumentReference reference);
    public delegate void LinkedEntityColorChanged(Guid linkTargetId, string TargetPathData, SolidColorBrush newColor);
    public delegate void MapLockStatusChanged(bool IsMaplockOn);
    public delegate void KnowledgeMapRequestToolUpdate(KnowledgeMap sender);

    [Serializable]
    public partial class KnowledgeMap : System.Windows.Controls.UserControl
    {

        public event EntityRequestedReferenceNavigate OnEntityRequestedReferenceNavigate;
        public event LinkedEntityColorChanged OnLinkedEntityColorChanged;
        public event MapLockStatusChanged OnMapLockStatusChanged;
        public event KnowledgeMapRequestToolUpdate OnKnowledgeMapRequestToolUpdate;

        public Guid CurrentOwnerId { get; set; }
        public string CurrentOwnerName { get; set; }

        private Guid _parentStudyId;
        public Guid ParentStudyId
        {
            get { return _parentStudyId; }
            set { _parentStudyId = value; }
        }

        public bool HasGuide
        {
            get
            {
                if (this._knowledgeMapGuideControl != null)
                    return true;
                else
                    return false;
            }
        }

        public EjpLib.BaseClasses.ejpKnowledgeMap LocalMapObject
        {
            get
            {
                return this._localMapObject;
            }
        }

        private bool _hasInputFocus = false;
        public bool HasInputFocus
        {
            get { return _hasInputFocus; }
            set { _hasInputFocus = value; }
        }

        private bool _isEditingLocked;
        public bool IsEditingLocked
        {
            get { return _isEditingLocked; }
            set
            {
                if (value == true)
                {
                    this.SetInputState(KnowledgeMapInputState.None);
                }
                else
                    this._IC_MapCanvas.AllowDrop = true;

                _isEditingLocked = value;
            }
        }

        //List of all the elements in the current map that are not shapes
        private List<UIElement> _nonShapeEntities;

        //Global list of all the comments currently added to this map.
        private List<KnowledgeMapComment> _commentsList;

        //Used to detirmine drop location.
        private Point _currentMousePoint;

        private DrawingAttributes _currentDrawingAttributes;
        public DrawingAttributes CurrentDrawingAttributes
        {
            get { return _currentDrawingAttributes; }
            set { _currentDrawingAttributes = value; }
        }

        private StrokeArrowMode _strokeArrowMode;
        public StrokeArrowMode StrokeArrowMode
        {
            get { return _strokeArrowMode; }
            set { _strokeArrowMode = value; }
        }

        private bool _entityConnectionEnabled;
        public bool EntityConnectionEnabled
        {
            get { return _entityConnectionEnabled; }
            set { _entityConnectionEnabled = value; }
        }

        private KnowledgeMapInputState _inputState;
        public KnowledgeMapInputState InputState
        {
            get { return _inputState; }
            set
            {
                _inputState = value;
                this.SetInputState(value);
            }
        }

        private SolidColorBrush _currentBrush;
        public SolidColorBrush CurrentBrush
        {
            get { return _currentBrush; }
            set { _currentBrush = value; }
        }

        private bool _isScaleLockActive;
        public bool IsScaleLockActive
        {
            get { return _isScaleLockActive; }
            set { _isScaleLockActive = value; }
        }

        public int CommentCount
        {
            get { return this._commentsList.Count; }
        }

        private bool _isDrawingSelectionLoop;
        private Stroke _selectionLoopStroke = null;

        private EjpLib.BaseClasses.ejpKnowledgeMap _localMapObject;

        //Used in all scenarios where we need to track the movment of the mouse.
        private Point _mouseStartPosition;

        private double _currentZoomValue;


        //KM Guide fields.
        private Image _knowledgeMapGuideControl;
        private Stream _knowledgeMapGuideDataStream;

        //Undo/Redo
        private Stack<UndoRedoAction> UndoStack = new Stack<UndoRedoAction>(25);
        private Stack<UndoRedoAction> RedoStack = new Stack<UndoRedoAction>(25);

        private UIElement _currentlyHoveringUiElement = null;

        public KnowledgeMap()
        {
            InitializeComponent();
            this._nonShapeEntities = new List<UIElement>();
            this._commentsList = new List<KnowledgeMapComment>();
            this.SetInputState(KnowledgeMapInputState.None);
            this._strokeArrowMode = StrokeArrowMode.None;
            this._IC_MapCanvas.StrokeCollected += new InkCanvasStrokeCollectedEventHandler(_IC_MapCanvas_StrokeCollected);
            this._IC_MapCanvas.SelectionMoved += new EventHandler(_IC_MapCanvas_SelectionMoved);
            this._currentDrawingAttributes = this._IC_MapCanvas.DefaultDrawingAttributes.Clone();
            this._currentBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            this._mouseStartPosition = new Point(-1, -1);
            this._currentZoomValue = 100;
            this._IC_MapCanvas.SelectionMoving += new InkCanvasSelectionEditingEventHandler(_IC_MapCanvas_SelectionMoving);
            this._IC_MapCanvas.SelectionChanging += new InkCanvasSelectionChangingEventHandler(_IC_MapCanvas_SelectionChanging);

            this._IC_MapCanvas.StrokeErasing += new InkCanvasStrokeErasingEventHandler(_IC_MapCanvas_StrokeErasing);
        }

        #region Map Canvas Specific callbacks

        private void _IC_MapCanvas_StrokeErasing(object sender, InkCanvasStrokeErasingEventArgs e)
        {
            foreach (UIElement uie in this._nonShapeEntities)
            {
                if (uie is KnowledgeMapEntityBase)
                {
                    if (((KnowledgeMapEntityBase)uie).CommentConnectorStroke == e.Stroke)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            this.UndoStack.Push(
                new UndoRedoAction
                {
                    InkStroke = e.Stroke,
                    Action = UndoRedoActions.DeleteStroke
                }
                );

            this.RedoStack.Clear();
            //this.SetInputState(KnowledgeMapInputState.Select);
        }

        /// <summary>
        /// Always scroll the KM to show the object being dragged.
        /// </summary>
        private void _IC_MapCanvas_SelectionMoving(object sender, InkCanvasSelectionEditingEventArgs e)
        {
            this._IC_MapCanvas.BringIntoView(e.NewRectangle);
            Debug.Print("KM: Selection Moved...");
        }

        private void _IC_MapCanvas_SelectionChanging(object sender, InkCanvasSelectionChangingEventArgs e)
        {
            Debug.Print("KM: Selection Changed...");
            //we could stop it here.... maybe...

            StrokeCollection sCol = new StrokeCollection();

            //TODO: Improve (re-construct)
            foreach (Stroke icStroke in e.GetSelectedStrokes())
            {
                bool skip = false;
                foreach (UIElement uie in this._nonShapeEntities)
                {
                    if (uie is KnowledgeMapEntityBase)
                    {
                        if (icStroke ==
                            ((KnowledgeMapEntityBase)uie).CommentConnectorStroke)
                        {
                            skip = true;
                        }
                    }
                }
                if (!skip)
                    sCol.Add(icStroke);
            }

            e.SetSelectedStrokes(sCol);

            if (this._IC_MapCanvas.GetSelectedStrokes().Count != 0
                   || this._IC_MapCanvas.GetSelectedElements().Count != 0)
            {

            }

            if (this._IC_MapCanvas.Children.Count != 0)
            {
                AdornerLayer al = AdornerLayer.GetAdornerLayer(this._IC_MapCanvas.Children[0]);
                if (al != null)
                    al.Visibility = Visibility.Visible;
            }
        }

        //handle custom entity copying and pasting
        private void HandleEntityCopy(object sender, DataObjectCopyingEventArgs e)
        {
            if (e.IsDragDrop == true)
                return;
        }

        private void _IC_MapCanvas_SelectionMoved(object sender, EventArgs e)
        {
            foreach (UIElement u in this._IC_MapCanvas.GetSelectedElements())
            {
                double newLeft = (double)u.GetValue(InkCanvas.LeftProperty);
                double newTop = (double)u.GetValue(InkCanvas.TopProperty);

                if (newLeft < 0)
                    InkCanvas.SetLeft(u, 5);
                else if ((newLeft + 15) > this._IC_MapCanvas.ActualWidth)
                {
                    if (u is KnowledgeMapEntityBase)
                        InkCanvas.SetLeft(u, this._IC_MapCanvas.ActualWidth - ((KnowledgeMapEntityBase)u).ActualWidth + 5);
                    else if (u is TextBox)
                        InkCanvas.SetLeft(u, this._IC_MapCanvas.ActualWidth - ((TextBox)u).ActualWidth + 5);
                }

                if (newTop < 0)
                    InkCanvas.SetTop(u, 5);
                else if ((newTop + 15) > this._IC_MapCanvas.ActualHeight)
                {
                    if (u is KnowledgeMapEntityBase)
                        InkCanvas.SetTop(u, this._IC_MapCanvas.ActualHeight - ((KnowledgeMapEntityBase)u).ActualHeight + 5);
                    else if (u is TextBox)
                        InkCanvas.SetTop(u, this._IC_MapCanvas.ActualHeight - ((TextBox)u).ActualHeight + 5);
                }

                KnowledgeMapEntityBase k = u as KnowledgeMapEntityBase;
                if (k != null)
                {
                    k.UpdateAllConnectedLines();
                    k.DrawCommentConnectorLine();
                }

                KnowledgeMapEntityCommentNote cn = u as KnowledgeMapEntityCommentNote;
                if (cn != null)
                {
                    cn.ParentEntity.DrawCommentConnectorLine();
                }
            }
        }

        private void _IC_MapCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            switch (this._inputState)
            {
                case KnowledgeMapInputState.Freehand:
                    this.UndoStack.Push(
                       new UndoRedoAction
                       {
                           InkStroke = e.Stroke,
                           Action = UndoRedoActions.AddStroke
                       }
                       );

                    this.RedoStack.Clear();
                    break;
                case KnowledgeMapInputState.Gesture:
                    break;
                case KnowledgeMapInputState.Line:
                    this._IC_MapCanvas.Strokes.Remove(e.Stroke);
                    Stroke line = InkTransformerHelper.StrokeToLine(e.Stroke);
                    line.DrawingAttributes = this._currentDrawingAttributes.Clone();
                    line.DrawingAttributes.FitToCurve = false;
                    line.DrawingAttributes.Color = this._currentBrush.Color;
                    if (this._strokeArrowMode == StrokeArrowMode.Single)
                    {
                        line = InkTransformerHelper.LineToSingleArrow(line);
                    }
                    else if (this._strokeArrowMode == StrokeArrowMode.Double)
                    {
                        line = InkTransformerHelper.LineToDoubleArrow(line);
                    }
                    this.ConnectStrokeToEntities(line);
                    line.DrawingAttributes.Width = 2.0;
                    line.DrawingAttributes.Height = 2.0;
                    line.DrawingAttributes.IgnorePressure = true;
                    this._IC_MapCanvas.Strokes.Add(line);

                    this.UndoStack.Push(
                       new UndoRedoAction
                       {
                           InkStroke = line,
                           Action = UndoRedoActions.AddStroke
                       }
                       );

                    this.RedoStack.Clear();

                    break;

                case KnowledgeMapInputState.SingleArrow:
                    this._IC_MapCanvas.Strokes.Remove(e.Stroke);
                    Stroke sAline = InkTransformerHelper.StrokeToLine(e.Stroke);
                    sAline.DrawingAttributes = this._currentDrawingAttributes.Clone();
                    sAline.DrawingAttributes.FitToCurve = false;
                    sAline.DrawingAttributes.Color = this._currentBrush.Color;
                    sAline = InkTransformerHelper.LineToSingleArrow(sAline);
                    this.ConnectStrokeToEntities(sAline);
                    sAline.DrawingAttributes.Width = 2.0;
                    sAline.DrawingAttributes.Height = 2.0;
                    sAline.DrawingAttributes.IgnorePressure = true;
                    this._IC_MapCanvas.Strokes.Add(sAline);

                    this.UndoStack.Push(
                       new UndoRedoAction
                       {
                           InkStroke = sAline,
                           Action = UndoRedoActions.AddStroke
                       }
                       );

                    this.RedoStack.Clear();

                    break;

                case KnowledgeMapInputState.DoubleArrow:
                    this._IC_MapCanvas.Strokes.Remove(e.Stroke);
                    Stroke dAline = InkTransformerHelper.StrokeToLine(e.Stroke);
                    dAline.DrawingAttributes = this._currentDrawingAttributes.Clone();
                    dAline.DrawingAttributes.FitToCurve = false;
                    dAline.DrawingAttributes.Color = this._currentBrush.Color;
                    dAline = InkTransformerHelper.LineToDoubleArrow(dAline);
                    this.ConnectStrokeToEntities(dAline);
                    dAline.DrawingAttributes.Width = 2.0;
                    dAline.DrawingAttributes.Height = 2.0;
                    dAline.DrawingAttributes.IgnorePressure = true;
                    this._IC_MapCanvas.Strokes.Add(dAline);

                    this.UndoStack.Push(
                       new UndoRedoAction
                       {
                           InkStroke = dAline,
                           Action = UndoRedoActions.AddStroke
                       }
                       );

                    this.RedoStack.Clear();

                    break;

                case KnowledgeMapInputState.FreehandSquare:
                    this._IC_MapCanvas.Strokes.Remove(e.Stroke);
                    Stroke square = InkTransformerHelper.StrokeToSquare(e.Stroke);
                    square.DrawingAttributes = this._currentDrawingAttributes.Clone();
                    square.DrawingAttributes.Color = this._currentBrush.Color;
                    this._IC_MapCanvas.Strokes.Add(square);

                    this.UndoStack.Push(
                       new UndoRedoAction
                       {
                           InkStroke = square,
                           Action = UndoRedoActions.AddStroke
                       }
                       );

                    this.RedoStack.Clear();

                    break;
                case KnowledgeMapInputState.FreehandCircle:
                    this._IC_MapCanvas.Strokes.Remove(e.Stroke);
                    Stroke circle = InkTransformerHelper.StrokeToCircle(e.Stroke);
                    circle.DrawingAttributes = this._currentDrawingAttributes.Clone();
                    circle.DrawingAttributes.Color = this._currentBrush.Color;
                    this._IC_MapCanvas.Strokes.Add(circle);

                    this.UndoStack.Push(
                       new UndoRedoAction
                       {
                           InkStroke = circle,
                           Action = UndoRedoActions.AddStroke
                       }
                       );

                    this.RedoStack.Clear();

                    break;
                case KnowledgeMapInputState.Note:
                    break;
                case KnowledgeMapInputState.Label:
                    break;
                default:
                    break;
            }

        }

        #endregion

        #region Stylus Events
        private void OnStylusOutOfRange(object sender, StylusEventArgs e)
        {
            foreach (UIElement kmeBase in this._nonShapeEntities)
            {
                if (kmeBase is KnowledgeMapEntityBase)
                {
                    ((KnowledgeMapEntityBase)(kmeBase)).HideAnchorPoints();
                }
            }

            if (this._inputState == KnowledgeMapInputState.Select)
            {
                if (this._isDrawingSelectionLoop)
                {
                    if (this._selectionLoopStroke != null)
                    {
                        this._IC_MapCanvas.Strokes.Remove(this._selectionLoopStroke);
                        this._selectionLoopStroke = null;
                    }
                }
                this._isDrawingSelectionLoop = false;
            }
        }

        private void OnStylusUp(object sender, StylusEventArgs e)
        {
            if (this._inputState == KnowledgeMapInputState.Select)
            {
                if (this._isDrawingSelectionLoop)
                {
                    if (this._selectionLoopStroke != null)
                    {
                        this._IC_MapCanvas.Strokes.Remove(this._selectionLoopStroke);
                        this._selectionLoopStroke = null;
                    }
                }
                this._isDrawingSelectionLoop = false;
            }
        }

        private void OnStylusDown(object sender, StylusEventArgs e)
        {
            if (this.HasInputFocus == false)
            {
                e.Handled = true;
                return;
            }

            switch (this._inputState)
            {
                case KnowledgeMapInputState.Select:
                    UIElement result = null;
                    result = this.HittestEntities(e.GetPosition(this._IC_MapCanvas), 0);
                    if (result != null)
                    {
                        this._IC_MapCanvas.Select(new List<UIElement> { result });
                    }
                    else
                    {
                        foreach (Stroke icStroke in this._IC_MapCanvas.Strokes)
                        {
                            if (icStroke.GetGeometry().GetWidenedPathGeometry(
                                new Pen(Brushes.White, 3)).StrokeContains(
                                new Pen(Brushes.White, 3), e.GetPosition(
                                this._IC_MapCanvas)))
                            {
                                bool skip = false;
                                //TODO: Improve (re-construct)
                                foreach (UIElement uie in this._nonShapeEntities)
                                {
                                    if (uie is KnowledgeMapEntityBase)
                                    {
                                        if (icStroke ==
                                            ((KnowledgeMapEntityBase)uie).CommentConnectorStroke)
                                            skip = true;
                                    }
                                }
                                if (!skip)
                                    this._IC_MapCanvas.Select(new StrokeCollection { icStroke });
                                break;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Preview Mouse Events

        private void OnMapCanvasPreviewMouseDown(object sender, MouseEventArgs e)
        {
            if (this.HasInputFocus == false)
            {
                e.Handled = true;
                return;
            }

            this._mouseStartPosition = e.GetPosition(this._IC_MapCanvas);
            StylusPointCollection stp = new StylusPointCollection();
            Stroke s;
            switch (this._inputState)
            {
                case KnowledgeMapInputState.None:
                    if (this._isEditingLocked)
                    {
                        Point p = e.GetPosition(this._IC_MapCanvas);
                        UIElement result = this.HittestEntities(p, 2);
                        if (result != null)
                        {
                            KnowledgeMapEntityBase entity = result as KnowledgeMapEntityBase;
                            if (entity != null)
                            {
                                this.InitiateEntityDragAndDrop(entity);
                            }
                        }
                    }
                    break;
                case KnowledgeMapInputState.Gesture:
                    break;
                case KnowledgeMapInputState.Note:
                    s = InkTransformerHelper.PositionsToSquare(
                        this._mouseStartPosition, e.GetPosition(this._IC_MapCanvas));
                    s.DrawingAttributes = this.Resources["VirtualSelectionZoneBrush"] as DrawingAttributes;
                    this._IC_MapCanvas.Strokes.Add(s);
                    break;
                case KnowledgeMapInputState.Select:
                    stp.Add(new StylusPoint(this._mouseStartPosition.X, this._mouseStartPosition.Y));
                    stp.Add(new StylusPoint(this._mouseStartPosition.X + 2, this._mouseStartPosition.Y + 2));
                    this._selectionLoopStroke = new Stroke(stp);
                    this._selectionLoopStroke.DrawingAttributes = this.Resources["VirtualSelectionZoneBrush"] as DrawingAttributes;
                    this._IC_MapCanvas.Strokes.Add(this._selectionLoopStroke);
                    this._isDrawingSelectionLoop = true;
                    break;
                case KnowledgeMapInputState.Square:
                    s = InkTransformerHelper.PositionsToSquare(
                        this._mouseStartPosition, e.GetPosition(this._IC_MapCanvas));
                    s.DrawingAttributes = this._currentDrawingAttributes.Clone();
                    s.DrawingAttributes.Color = this._currentBrush.Color;
                    this._IC_MapCanvas.Strokes.Add(s);
                    break;
                case KnowledgeMapInputState.Circle:
                    s = InkTransformerHelper.PositionsToCircle(
                        this._mouseStartPosition, e.GetPosition(this._IC_MapCanvas));
                    s.DrawingAttributes = this._currentDrawingAttributes.Clone();
                    s.DrawingAttributes.Color = this._currentBrush.Color;
                    this._IC_MapCanvas.Strokes.Add(s);
                    break;
                default:
                    break;
            }
        }

        private void OnMapCanvasPreviewMouseUp(object sender, MouseEventArgs e)
        {
            //enable to prevent FocusSwitch
            if (this.HasInputFocus == false)
            {
                // e.Handled = true;
                return;
            }

            switch (this._inputState)
            {
                case KnowledgeMapInputState.Select:
                    if (this._IC_MapCanvas.Strokes.Count > 0)
                    {
                        if (this._selectionLoopStroke != null)
                        {
                            this._IC_MapCanvas.Strokes.Remove(this._selectionLoopStroke);
                            this._selectionLoopStroke = null;
                        }
                    }
                    this._isDrawingSelectionLoop = false;
                    break;
                case KnowledgeMapInputState.Gesture:
                    break;
                case KnowledgeMapInputState.Note:

                    this._IC_MapCanvas.Strokes.RemoveAt(
                        this._IC_MapCanvas.Strokes.Count - 1);

                    Point p = e.GetPosition(this._IC_MapCanvas);

                    double nLeft = (p.X < this._mouseStartPosition.X) ? p.X : this._mouseStartPosition.X;
                    double nTop = (p.Y < this._mouseStartPosition.Y) ? p.Y : this._mouseStartPosition.Y;

                    Rect newEntityRect = new Rect(this._mouseStartPosition, e.GetPosition(this._IC_MapCanvas));

                    if (newEntityRect.Width < 150)
                        newEntityRect.Width = 150;
                    if (newEntityRect.Height < 150)
                        newEntityRect.Height = 150;

                    this.AddTextEntity(Application.Current.Resources["Str_NewKMSelfNodeTitle"] as string, "", new XpsDocumentReference(), "",
                        KnowledgeMapEntityType.OriginalToMap,
                        Brushes.Black, nLeft, nTop,
                        newEntityRect.Width, newEntityRect.Height);

                    //Since the Note state manipulates the visual children 
                    //of the canvas we have to override the default behaviour
                    //of setting input mode to Select.
                    this.SetInputState(KnowledgeMapInputState.Select);
                    this._mouseStartPosition = new Point(-1, -1);
                    break;

                case KnowledgeMapInputState.Label:
                    Point newp = e.GetPosition(this._IC_MapCanvas);
                    if (this.HittestEntities(newp, 3) != null)
                        break;

                    TextBox tb = new TextBox();
                    tb.SetValue(InkCanvas.LeftProperty, newp.X);
                    tb.SetValue(InkCanvas.TopProperty, newp.Y);
                    tb.FontSize = 12;
                    tb.Foreground = this._currentBrush;
                    tb.Background = Brushes.Transparent;
                    tb.AcceptsReturn = true;
                    tb.Text = Application.Current.Resources["Str_NewKMLabelDefaultContent"] as string;
                    tb.BorderThickness = new Thickness(2);
                    this._nonShapeEntities.Add(tb);
                    this._IC_MapCanvas.Children.Add(tb);

                    tb.SelectAll();
                    tb.Focus();

                    this.UndoStack.Push(new UndoRedoAction
                            {
                                Action = UndoRedoActions.AddTextLabel,
                                TextLabel = tb
                            });

                    this.RedoStack.Clear();

                    //Added 080724
                    this.SetInputState(KnowledgeMapInputState.Select);

                    break;

                case KnowledgeMapInputState.EraseByStroke:

                    UIElement entityToDelete = HittestEntities(e.GetPosition((UIElement)sender), 0);
                    if (entityToDelete != null)
                    {
                        if (entityToDelete is KnowledgeMapEntityBase)
                        {
                            this.RemoveKnowledgeMapEntity((KnowledgeMapEntityBase)entityToDelete);

                            this.UndoStack.Push(new UndoRedoAction
                                {
                                    Action = UndoRedoActions.DeleteEntity,
                                    Entity = (KnowledgeMapEntityBase)entityToDelete
                                });
                        }
                        else if (entityToDelete is TextBox)
                        {
                            this.RemoveTextLabel(entityToDelete as TextBox);
                            this.UndoStack.Push(new UndoRedoAction
                            {
                                Action = UndoRedoActions.DeleteTextLabel,
                                TextLabel = (TextBox)entityToDelete
                            });
                        }

                        this.RedoStack.Clear();
                    }
                    //Turn of the Eraser after each use.
                    this.SetInputState(KnowledgeMapInputState.Select);
                    break;

                case KnowledgeMapInputState.Circle:
                    this.UndoStack.Push(new UndoRedoAction
                    {
                        Action = UndoRedoActions.AddStroke,
                        InkStroke = this._IC_MapCanvas.Strokes[this._IC_MapCanvas.Strokes.Count - 1]
                    });
                    this.RedoStack.Clear();
                    break;

                case KnowledgeMapInputState.Square:
                    this.UndoStack.Push(new UndoRedoAction
                    {
                        Action = UndoRedoActions.AddStroke,
                        InkStroke = this._IC_MapCanvas.Strokes[this._IC_MapCanvas.Strokes.Count - 1]
                    });
                    this.RedoStack.Clear();
                    break;

                default:
                    break;
            }
        }

        #endregion

        #region MapZoom

        private bool t_IsColdZoomUpdate;

        private void OnSetScaleLock(object sender, RoutedEventArgs e)
        {
            this._isScaleLockActive = true;
            this.UpdateScale();
        }

        private void OnReleaseScaleLock(object sender, RoutedEventArgs e)
        {
            this._isScaleLockActive = false;
        }

        public void UpdateScale()
        {
            this.t_IsColdZoomUpdate = true;

            if (this._isScaleLockActive)
            {
                double xfactor = (this._SC_Scroller.ActualHeight - 18) / this._IC_MapCanvas.Height;

                this._IC_MapCanvas.LayoutTransform =
                                    new ScaleTransform(xfactor, xfactor,
                                    this._IC_MapCanvas.ActualWidth * 0.5, this._IC_MapCanvas.ActualHeight * 0.5);

                this._cv_CommentSurface.LayoutTransform =
                                    new ScaleTransform(xfactor, xfactor,
                                    this._cv_CommentSurface.ActualWidth * 0.5, this._cv_CommentSurface.ActualHeight * 0.5);

                this._currentZoomValue = xfactor * 100;
                this._l_CurrentZoomValue.Content = ((int)this._currentZoomValue).ToString() + "%";
                if (this._currentZoomValue < 200)
                    this._b_IncreaseZoom.IsEnabled = true;
                if (this._currentZoomValue < 25)
                    this._b_DecreaseZoom.IsEnabled = false;
            }

            this._sl_ZoomSlider.Value = this._currentZoomValue;

            this.t_IsColdZoomUpdate = false;
        }

        private void OnKnowledgeMapZoomChanged(object sender, RoutedEventArgs e)
        {
            this._l_CurrentZoomValue.Content = this._sl_ZoomSlider.Value.ToString() + "%";

            if (!t_IsColdZoomUpdate)
            {
                if (this._isScaleLockActive)
                    this._tb_ScaleLock.IsChecked = false;

                this._currentZoomValue = this._sl_ZoomSlider.Value;

                ScaleTransform newTrans = new ScaleTransform(this._currentZoomValue * 0.01, this._currentZoomValue * 0.01,
                        this._IC_MapCanvas.ActualWidth * 0.5, this._IC_MapCanvas.ActualHeight * 0.5);

                ScaleTransform newTransCMS = new ScaleTransform(this._currentZoomValue * 0.01, this._currentZoomValue * 0.01,
                        this._cv_CommentSurface.ActualWidth * 0.5, this._cv_CommentSurface.ActualHeight * 0.5);

                this._IC_MapCanvas.LayoutTransform = newTrans;
                this._cv_CommentSurface.LayoutTransform = newTransCMS;
            }

        }

        private void OnIncreaseZoomKnowledgemap(object sender, RoutedEventArgs e)
        {
            if (this._isScaleLockActive)
            {
                this._tb_ScaleLock.IsChecked = false;
            }

            if (this._currentZoomValue < 200)
            {
                this._b_DecreaseZoom.IsEnabled = true;
                this._currentZoomValue += 25;

                double res = (double)this._currentZoomValue % 25.0;
                if (res != 0)
                {
                    if (res <= 12.5)
                        this._currentZoomValue -= res;
                    else
                        this._currentZoomValue += res;
                }

                this._currentZoomValue = (this._currentZoomValue > 200) ? 200 : (int)this._currentZoomValue;

                this._l_CurrentZoomValue.Content = this._currentZoomValue.ToString() + "%";

                if (this._currentZoomValue >= 200)
                    this._b_IncreaseZoom.IsEnabled = false;

                ScaleTransform newTrans = new ScaleTransform(this._currentZoomValue * 0.01, this._currentZoomValue * 0.01,
                    this._IC_MapCanvas.ActualWidth * 0.5, this._IC_MapCanvas.ActualHeight * 0.5);

                this._IC_MapCanvas.LayoutTransform = newTrans;
                this._cv_CommentSurface.LayoutTransform = newTrans;

            }
        }

        private void OnDecreaseZoomKnowledgemap(object sender, RoutedEventArgs e)
        {
            if (this._isScaleLockActive)
            {
                this._tb_ScaleLock.IsChecked = false;
            }

            if (this._currentZoomValue > 25)
            {
                this._b_IncreaseZoom.IsEnabled = true;
                this._currentZoomValue -= 25;

                double res = (double)this._currentZoomValue % 25.0;
                if (res != 0)
                {
                    if (res <= 12.5)
                        this._currentZoomValue -= res;
                    else
                        this._currentZoomValue += res;
                }

                this._currentZoomValue = (this._currentZoomValue < 25) ? 200 : (int)this._currentZoomValue;
                this._l_CurrentZoomValue.Content = this._currentZoomValue.ToString() + "%";

                if (this._currentZoomValue <= 25)
                    this._b_DecreaseZoom.IsEnabled = false;

                ScaleTransform newTrans = new ScaleTransform(this._currentZoomValue * 0.01, this._currentZoomValue * 0.01,
                    this._IC_MapCanvas.ActualWidth * 0.5, this._IC_MapCanvas.ActualHeight * 0.5);

                this._IC_MapCanvas.LayoutTransform = newTrans;
                this._cv_CommentSurface.LayoutTransform = newTrans;
            }
        }

        #endregion

        private void OnSetMapLock(object sender, RoutedEventArgs e)
        {
            this._isEditingLocked = true;
            this.SetInputState(KnowledgeMapInputState.None);
            if (this.OnMapLockStatusChanged != null)
                this.OnMapLockStatusChanged(true);
        }

        private void OnReleaseMapLock(object sender, RoutedEventArgs e)
        {
            this._isEditingLocked = false;
            if (this.OnMapLockStatusChanged != null)
                this.OnMapLockStatusChanged(false);
        }

        private void OnMapCanvasDragOver(object sender, DragEventArgs e)
        {
            this._currentMousePoint = e.GetPosition(this._IC_MapCanvas);
        }

        /// <summary>
        /// Continuos hittesting of the inkcanvas.
        /// </summary>
        private void OnHittestMapCanvas(object sender, MouseEventArgs e)
        {
            if (this.HasInputFocus == false)
            {
                //e.Handled = true;
                return;
            }

            //DropShadow effect on mouse over 080417
            UIElement u3 = this.HittestEntities(e.GetPosition(this._IC_MapCanvas), 1);
            if (u3 != null)
            {
                if (u3 != this._currentlyHoveringUiElement)
                {
                    if (this._currentlyHoveringUiElement != null)
                        this._currentlyHoveringUiElement.Effect = null;
                    u3.Effect = new DropShadowEffect() { ShadowDepth = 10, Color = Colors.Black, BlurRadius = 7 };
                    this._currentlyHoveringUiElement = u3;
                }
            }
            else
            {
                if (this._currentlyHoveringUiElement != null)
                {
                    this._currentlyHoveringUiElement.Effect = null;
                    this._currentlyHoveringUiElement = null;
                }
            }


            Point p = e.GetPosition((UIElement)sender);
            switch (this._inputState)
            {
                case KnowledgeMapInputState.EraseByStroke: //080329
                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        UIElement u = this.HittestEntities(e.GetPosition(this._IC_MapCanvas), 1);
                        if (u != null)
                        {
                            if (u is KnowledgeMapEntityBase)
                            {
                                this.RemoveKnowledgeMapEntity((KnowledgeMapEntityBase)u);

                                this.UndoStack.Push(new UndoRedoAction
                                {
                                    Action = UndoRedoActions.DeleteEntity,
                                    Entity = (KnowledgeMapEntityBase)u
                                });
                            }
                            else if (u is TextBox)
                            {
                                this.RemoveTextLabel(u as TextBox);
                                this.UndoStack.Push(new UndoRedoAction
                                {
                                    Action = UndoRedoActions.DeleteTextLabel,
                                    TextLabel = (TextBox)u
                                });
                            }

                            this.RedoStack.Clear();
                        }
                    }
                    break;
                case KnowledgeMapInputState.Select:

                    //If the user is moving an entity we do not want to
                    //shift the focus to a new entity. OR if the user is
                    //doing a lasso selection we want to draw a line back to 
                    //the original selection point to show the region being selected.
                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        if (this._IC_MapCanvas.GetSelectedElements().Count == 0 &&
                            this._IC_MapCanvas.GetSelectedStrokes().Count == 0)
                        {
                            if (this._selectionLoopStroke != null)
                            {
                                this._IC_MapCanvas.Strokes.Remove(this._selectionLoopStroke);
                                this._selectionLoopStroke = null;
                            }

                            StylusPointCollection stpc = new StylusPointCollection();
                            stpc.Add(new StylusPoint(this._mouseStartPosition.X, this._mouseStartPosition.Y));
                            stpc.Add(new StylusPoint(e.GetPosition(this._IC_MapCanvas).X, e.GetPosition(this._IC_MapCanvas).Y));
                            this._selectionLoopStroke = new Stroke(stpc);
                            this._selectionLoopStroke.DrawingAttributes = this.Resources["VirtualSelectionZoneBrush"] as DrawingAttributes;
                            this._IC_MapCanvas.Strokes.Add(this._selectionLoopStroke);
                            this._isDrawingSelectionLoop = true;
                        }
                        else
                        {
                            this._IC_MapCanvas.BringIntoView(new Rect(e.GetPosition(this._IC_MapCanvas), e.GetPosition(this._IC_MapCanvas)));
                        }

                        return;
                    }
                    break;

                case KnowledgeMapInputState.Note:

                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        if (this._mouseStartPosition.X == -1 && this._mouseStartPosition.Y == -1)
                            return;

                        Stroke notePrev =
                               InkTransformerHelper.PositionsToSquare(
                               this._mouseStartPosition,
                               e.GetPosition(this._IC_MapCanvas));

                        notePrev.DrawingAttributes = this.Resources["VirtualSelectionZoneBrush"] as DrawingAttributes;
                        this._IC_MapCanvas.Strokes.RemoveAt(
                            this._IC_MapCanvas.Strokes.Count - 1);
                        this._IC_MapCanvas.Strokes.Add(notePrev);
                    }
                    break;
                case KnowledgeMapInputState.Square:

                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        Stroke prevSquare =
                            InkTransformerHelper.PositionsToSquare(
                            this._mouseStartPosition,
                            new Point(e.GetPosition(this._IC_MapCanvas).X, e.GetPosition(this._IC_MapCanvas).Y));
                        this._IC_MapCanvas.Strokes.RemoveAt(
                            this._IC_MapCanvas.Strokes.Count - 1);
                        prevSquare.DrawingAttributes = this._currentDrawingAttributes.Clone();
                        prevSquare.DrawingAttributes.FitToCurve = false;
                        prevSquare.DrawingAttributes.Color = this._currentBrush.Color;
                        this._IC_MapCanvas.Strokes.Add(prevSquare);
                    }
                    break;

                case KnowledgeMapInputState.Circle:

                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        Stroke prevCircle =
                            InkTransformerHelper.PositionsToCircle(
                            this._mouseStartPosition,
                            e.GetPosition(this._IC_MapCanvas));
                        this._IC_MapCanvas.Strokes.RemoveAt(
                            this._IC_MapCanvas.Strokes.Count - 1);
                        prevCircle.DrawingAttributes = this._currentDrawingAttributes.Clone();
                        prevCircle.DrawingAttributes.Color = this._currentBrush.Color;
                        this._IC_MapCanvas.Strokes.Add(prevCircle);
                    }
                    break;

                default:
                    break;
            }

            if (this._entityConnectionEnabled)
            {
                UIElement u = this.HittestEntities(p, 20);
                if (u != null)
                    this.DisplayEntityAnchorPoint(Mouse.GetPosition(u), u);
            }
        }

        /// <summary>
        /// Place the new stroke so that it is aligned with an anchor point on 
        /// an entity, if it is withing the bounds of an anchor point.
        /// Also, adds the new connected line to the entities collection of
        /// connected lines.
        /// </summary>
        private void ConnectStrokeToEntities(Stroke originalStroke)
        {
            KnowledgeMapEntityBase kStart = null;
            KnowledgeMapConnectedLine startLineEnd = null;
            Point startPoint = new Point(originalStroke.StylusPoints[0].X, originalStroke.StylusPoints[0].Y);
            Point screenPoint = this._IC_MapCanvas.PointToScreen(startPoint);
            UIElement startElement = this.HittestEntities(startPoint, 30);
            if (startElement != null)
            {
                Point elementPoint = startElement.PointFromScreen(screenPoint);
                Point startAnchorPoint = this.GetEntityAnchorPoint(elementPoint, startElement);
                if (startAnchorPoint.X != -10000)
                {
                    originalStroke.StylusPoints[0] = new StylusPoint(startAnchorPoint.X, startAnchorPoint.Y);
                    kStart = startElement as KnowledgeMapEntityBase;
                    startLineEnd = new KnowledgeMapConnectedLine(originalStroke, this._strokeArrowMode, kStart, null);
                }
            }

            if (startLineEnd == null)
                return;

            KnowledgeMapConnectedLine endLineEnd = null;
            UIElement endElement = this.HittestEntities(Mouse.GetPosition(this._IC_MapCanvas), 30);
            if (endElement != null)
            {
                Point endAnchorPoint = this.GetEntityAnchorPoint(Mouse.GetPosition(endElement), endElement);
                if (endAnchorPoint.X != -10000)
                {
                    originalStroke.StylusPoints[originalStroke.StylusPoints.Count - 1] = new StylusPoint(endAnchorPoint.X, endAnchorPoint.Y);
                    KnowledgeMapEntityBase kEnd = endElement as KnowledgeMapEntityBase;
                    startLineEnd.TargetEntity = kEnd;
                    endLineEnd = new KnowledgeMapConnectedLine(originalStroke, this._strokeArrowMode, startLineEnd.SourceEntity, kEnd);
                    kEnd.AddConnectedStrokes(endLineEnd);
                }
            }

            if (kStart != null)
                kStart.AddConnectedStrokes(startLineEnd);
        }

        /// <summary>
        /// Locate an Anchorpoint within an entity and display its visual.
        /// </summary>
        private void DisplayEntityAnchorPoint(Point mousePosition, UIElement entityToTest)
        {
            KnowledgeMapEntityBase k = entityToTest as KnowledgeMapEntityBase;
            if (k != null)
            {
                foreach (Rect r in k.GetConnectableRegions(30))
                {
                    if (r.Contains(mousePosition))
                    {
                        k.DisplayAnchorPoint(r);
                        break;
                    }
                    else
                        k.HideAnchorPoints();
                }
            }
        }

        /// <summary>
        /// Returns a Point representing the coordinate withing the inkccanvas where
        /// an entity anchor point exists.
        /// </summary>
        private Point GetEntityAnchorPoint(Point mousePosition, UIElement entityToTest)
        {
            KnowledgeMapEntityBase k = entityToTest as KnowledgeMapEntityBase;
            if (k != null)
            {
                foreach (Rect r in k.GetConnectableRegions(30))
                {
                    if (r.Contains(mousePosition))
                    {
                        Point connectionPoint = new Point();
                        connectionPoint.X = r.Left + r.Width * 0.5;
                        connectionPoint.Y = r.Top + r.Height * 0.5;
                        Point screenPoint = entityToTest.PointToScreen(connectionPoint);
                        connectionPoint = this._IC_MapCanvas.PointFromScreen(screenPoint);
                        return connectionPoint;
                    }
                    else
                        k.HideAnchorPoints();
                }
            }
            return new Point(-10000, Double.NaN);
        }

        /// <summary>
        /// Search entities in the inkcanvas and select if found.
        /// </summary>
        private void FindSelectOnMouseOver(Point p)
        {
            List<UIElement> uiel = new List<UIElement>();

            UIElement u = this.HittestEntities(p, 10);

            if (u != null)
            {
                uiel.Add(u);
                this.SelectEnteties(uiel);
            }
        }

        /// <summary>
        /// Return the first found entity in the inkcanvas that
        /// responds to hittesting.
        /// </summary>
        private UIElement HittestEntities(Point p, int searchMargin)
        {
            UIElement result = null;
            for (int i = this._nonShapeEntities.Count - 1; i > -1; i--)
            {
                TextBox tb = this._nonShapeEntities[i] as TextBox;
                if (tb != null)
                {
                    double tbLeft = (double)tb.GetValue(InkCanvas.LeftProperty);
                    double tbTop = (double)tb.GetValue(InkCanvas.TopProperty);
                    Rect hittestRect = new Rect(
                        new Point(tbLeft - searchMargin, tbTop - searchMargin),
                        new Size(tb.RenderSize.Width + (searchMargin * 2),
                            tb.RenderSize.Height + (searchMargin * 2)));

                    if (hittestRect.Contains(p))
                    {
                        result = tb;
                        //break;
                    }
                }

                KnowledgeMapEntityBase k = this._nonShapeEntities[i] as KnowledgeMapEntityBase;
                if (k != null)
                {
                    if (k.GetBounds(searchMargin).Contains(p))
                    {
                        result = this._nonShapeEntities[i];
                        break;
                    }
                }
            }
            //IF we still have no match we resort to testing 
            //the child collection of the inkcanvas.
            if (result == null)
            {
                foreach (UIElement uie in this._IC_MapCanvas.Children)
                {
                    if (this._knowledgeMapGuideControl != null)
                    {
                        //The Guide responds to none of our fancy effects :)
                        if (uie == this._knowledgeMapGuideControl)
                            continue;
                    }
                    try
                    {
                        Rect region = new Rect(
                            new Point(
                                (double)uie.GetValue(InkCanvas.LeftProperty),
                                (double)uie.GetValue(InkCanvas.TopProperty)
                                ),
                                uie.RenderSize);
                        if (region.Contains(p))
                        {
                            result = uie;
                            //break;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return result;
        }

        private void SelectEnteties(List<UIElement> enteties)
        {
            this._IC_MapCanvas.Select(enteties);
        }

        private void SelectShapes(List<Stroke> strokes)
        {
            this._IC_MapCanvas.Select(new StrokeCollection(strokes));
        }

        public void DeleteSelection()
        {
            try
            {
                bool clearRedo = false;
                int elementCount = this._IC_MapCanvas.GetSelectedElements().Count;
                for (int i = 0; i < elementCount; i++)
                {
                    clearRedo = true;
                    if (this._IC_MapCanvas.GetSelectedElements()[0] is KnowledgeMapEntityBase)
                    {
                        this.UndoStack.Push(new UndoRedoAction
                        {
                            Action = UndoRedoActions.DeleteEntity,
                            Entity = (KnowledgeMapEntityBase)this._IC_MapCanvas.GetSelectedElements()[0]
                        });

                        this.RemoveKnowledgeMapEntity((KnowledgeMapEntityBase)
                                        this._IC_MapCanvas.GetSelectedElements()[0]);
                    }

                    else if (this._IC_MapCanvas.GetSelectedElements()[0] is TextBox)
                    {
                        this.UndoStack.Push(new UndoRedoAction
                        {
                            Action = UndoRedoActions.DeleteTextLabel,
                            TextLabel = (TextBox)this._IC_MapCanvas.GetSelectedElements()[0]
                        });

                        this.RemoveTextLabel((TextBox)
                            this._IC_MapCanvas.GetSelectedElements()[0]);
                    }
                }

                int strokeCount = this._IC_MapCanvas.GetSelectedStrokes().Count;
                for (int j = 0; j < strokeCount; j++)
                {
                    clearRedo = true;
                    this.UndoStack.Push(new UndoRedoAction
                    {
                        Action = UndoRedoActions.DeleteStroke,
                        InkStroke = this._IC_MapCanvas.GetSelectedStrokes()[0]
                    });

                    this.RemoveInkStroke(this._IC_MapCanvas.GetSelectedStrokes()[0]);
                }

                if (clearRedo)
                {
                    //Turn of the Eraser after each use.
                    this.SetInputState(KnowledgeMapInputState.Select);
                    this.RedoStack.Clear();
                }
            }
            catch (Exception)
            {
            }
        }

        private void FireRequestToolUpdate()
        {
            if (this.OnKnowledgeMapRequestToolUpdate != null)
                this.OnKnowledgeMapRequestToolUpdate(this);
        }

        private void SetInputState(KnowledgeMapInputState state)
        {
            this._entityConnectionEnabled = false;
            this._inputState = state;
            switch (state)
            {
                case KnowledgeMapInputState.Select:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.Select;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.Freehand:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.Gesture:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.GestureOnly;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.EraseByPoint:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.EraseByStroke:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.Line:
                    this._entityConnectionEnabled = true;
                    this._strokeArrowMode = StrokeArrowMode.None;
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.Circle:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.None;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.FreehandCircle:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.Square:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.None;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.FreehandSquare:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.None:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.None;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.SingleArrow:
                    this._B_ToggleMapLock.IsChecked = false;
                    this._entityConnectionEnabled = true;
                    this._strokeArrowMode = StrokeArrowMode.Single;
                    //this._inputState = KnowledgeMapInputState.Line;
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.DoubleArrow:
                    this._entityConnectionEnabled = true;
                    this._strokeArrowMode = StrokeArrowMode.Double;
                    //this._inputState = KnowledgeMapInputState.Line;
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.Note:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.None;
                    this._IC_MapCanvas.Cursor = Cursors.Cross;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                case KnowledgeMapInputState.Label:
                    this._IC_MapCanvas.EditingMode = InkCanvasEditingMode.None;
                    this._IC_MapCanvas.Cursor = Cursors.Cross;
                    this._B_ToggleMapLock.IsChecked = false;
                    this.FireRequestToolUpdate();
                    break;
                default:
                    break;
            }

        }

        private void InitiateEntityDragAndDrop(object draggedObject)
        {
            if (draggedObject.GetType() == typeof(KnowledgeMapTextEntity))
            {
                KnowledgeMapTextEntity te = draggedObject as KnowledgeMapTextEntity;
                KnowledgeMapTextEntity newTe =
                    new KnowledgeMapTextEntity(
                        EjpLib.Helpers.IdManipulation.GetNewGuid(),
                        te.EntityType);

                newTe.DragSourceId = te.Id;
                newTe.Title = te.Title;
                newTe.Body = te.Body;
                newTe.Height = te.Height;
                newTe.Width = te.Width;
                newTe.SetEntityColor(te.Color);
                newTe.SourceReference = te.SourceReference;
                if (newTe.CommentNote != null)
                    newTe.CommentNote.Visibility = Visibility.Collapsed;

                DragDrop.DoDragDrop(this, newTe, DragDropEffects.All);
            }
            else if (draggedObject.GetType() == typeof(KnowledgeMapImageEntity))
            {
                KnowledgeMapImageEntity ie = draggedObject as KnowledgeMapImageEntity;
                KnowledgeMapImageEntity newIe =
                    new KnowledgeMapImageEntity(
                        EjpLib.Helpers.IdManipulation.GetNewGuid());

                newIe._I_ImageArea.Source = ie._I_ImageArea.Source;
                newIe.EntityType = ie.EntityType;
                newIe.DragSourceId = ie.Id;
                newIe.Title = ie.Title;
                newIe.Height = ie.Height;
                newIe.Width = ie.Width;
                newIe.SetEntityColor(ie.Color);
                newIe.SourceReference = ie.SourceReference;
                newIe.TargetPathData = ie.TargetPathData;
                newIe.CommentNote.Visibility = Visibility.Collapsed;

                DragDrop.DoDragDrop(this, newIe, DragDropEffects.All);
            }
        }

        private void OnMapCanvasDrop(object sender, DragEventArgs e)
        {

            bool isReferencedText = false;
            bool isKMEntity = false;
            bool isReferencedImage = false;
            foreach (string format in e.Data.GetFormats())
            {
                if (format.Contains("DragDropQuote"))
                    isReferencedText = true;
                if (format.Contains("KnowledgeMap"))
                    isKMEntity = true;
                if (format.Contains("DragDropImage"))
                    isReferencedImage = true;
            }

            if (isReferencedImage)
            {
                if (this.IsEditingLocked)
                {
                    MessageBox.Show(Application.Current.Resources["ERR_AddKMEntityFailed_Locked"] as string,
                        Application.Current.Resources["Str_MsgTitle_KMLocked"] as string, MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                //TODO: Adjust so that the image is set to the right size and note the note it self.
                //at this point the height and width of the image is used to set the size of the note
                //causing the image to be shrunk....
                Helpers.DragDropImage dropImage =
                    (Helpers.DragDropImage)e.Data.GetData(typeof(Helpers.DragDropImage));

                Color newColor = Color.FromArgb(255, dropImage.Color.R, dropImage.Color.G, dropImage.Color.B);

                this.AddImageEntity(Application.Current.Resources["Str_NewKMIMGEntityTitle"] as string, dropImage.IBrush.ImageSource, new SolidColorBrush(newColor),
                    KnowledgeMapEntityType.ConnectedToDocument, dropImage.TargetPathData, "",
                    this._currentMousePoint.X,
                    this._currentMousePoint.Y,
                    200, 150,
                    dropImage.Reference);
            }

            else if (isReferencedText)
            {
                if (this.IsEditingLocked)
                {
                    MessageBox.Show(Application.Current.Resources["ERR_AddKMEntityFailed_Locked"] as string,
                        Application.Current.Resources["Str_MsgTitle_KMLocked"] as string, MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                Helpers.DragDropQuote q = (Helpers.DragDropQuote)e.Data.GetData(typeof(Helpers.DragDropQuote));
                Color newColor = Color.FromArgb(255, q.Color.R, q.Color.G, q.Color.B);
                this.AddTextEntity(Application.Current.Resources["Str_NewKMTextEntityTitle"] as string, q.UnicodeString, q.Reference, q.CommentString,
                    KnowledgeMapEntityType.ConnectedToDocument, new SolidColorBrush(newColor),
                    this._currentMousePoint.X, this._currentMousePoint.Y, 200, 250);
            }
            else if (isKMEntity)
            {
                KnowledgeMapEntityBase t = null;
                t = (KnowledgeMapTextEntity)e.Data.GetData(typeof(KnowledgeMapTextEntity));
                if (t == null)
                    t = (KnowledgeMapImageEntity)e.Data.GetData(typeof(KnowledgeMapImageEntity));

                //To make sure we do not add the same object twice...
                foreach (UIElement uie in this._nonShapeEntities)
                {
                    if (uie is KnowledgeMapEntityBase)
                    {
                        if (((KnowledgeMapEntityBase)uie).Id == t.DragSourceId)
                            return;
                    }
                }

                this._B_ToggleMapLock.IsChecked = false;

                this.AddDraggedEntity(
                    t,
                    this._currentMousePoint.X - (t.ActualWidth * 0.5),
                    this._currentMousePoint.Y - (t.ActualHeight * 0.5));
            }

            this._hasInputFocus = true;
            this.Focus();
        }

        private double AdjustXPositionToFitMap(double positionX, double width)
        {
            if (positionX < 0)
                return 5;
            else if (positionX > (this._IC_MapCanvas.ActualWidth - 10))
                return (this._IC_MapCanvas.ActualWidth - (width + 5));
            else
                return positionX;
        }

        private double AdjustYPositionToFitMap(double positionY, double height)
        {
            if (positionY < 0)
                return 5;
            else if (positionY > (this._IC_MapCanvas.ActualHeight - 10))
                return (this._IC_MapCanvas.ActualHeight - (height + 5));
            else
                return positionY;
        }

        #region Adding Entities

        public void AddImageEntity(string title, ImageSource source, SolidColorBrush brush,
            Enumerations.KnowledgeMapEntityType entityType, string targetPathData, string comment,
            double positionX, double positionY, double width, double height, XpsDocumentReference reference)
        {
            KnowledgeMapImageEntity kmi =
                new KnowledgeMapImageEntity(EjpLib.Helpers.IdManipulation.GetNewGuid());
            kmi._I_ImageArea.Source = source;
            kmi.Title = title;
            kmi.SetEntityColor(brush);

            positionX = this.AdjustXPositionToFitMap(positionX, width);
            positionY = this.AdjustYPositionToFitMap(positionY, 25);

            kmi.SetValue(InkCanvas.LeftProperty, positionX);
            kmi.SetValue(InkCanvas.TopProperty, positionY);

            kmi.Height = height;
            kmi.Width = width;
            kmi.TargetPathData = targetPathData;
            kmi.SourceReference = reference;
            kmi.EntityType = entityType;
            kmi.OnReferenceNavigateRequest += new ReferenceNavigateRequest(k_OnReferenceNavitageRequest);
            this._nonShapeEntities.Add(kmi);
            this._IC_MapCanvas.Children.Add(kmi);

            kmi.CommentNote = new KnowledgeMapEntityCommentNote { Visibility = Visibility.Collapsed };
            kmi.CommentNote._tb_NoteArea.Text = comment;
            kmi.CommentNote.ParentEntity = kmi;
            this._IC_MapCanvas.Children.Add(kmi.CommentNote);

            this.UndoStack.Push(new UndoRedoAction
            {
                Action = UndoRedoActions.AddEntity,
                Entity = kmi
            });

        }

        public void AddTextEntity(string title, string body, XpsDocumentReference reference, string comment,
            KnowledgeMapEntityType type, SolidColorBrush brush,
            double positionX, double positionY, double width, double height)
        {
            KnowledgeMapTextEntity k = new KnowledgeMapTextEntity(EjpLib.Helpers.IdManipulation.GetNewGuid(), type);
            k.Title = title;
            k.Body = body;
            k.CommentText = comment;
            k.SetEntityColor(brush);

            positionX = this.AdjustXPositionToFitMap(positionX, width);
            positionY = this.AdjustYPositionToFitMap(positionY, 25);

            k.SetValue(InkCanvas.LeftProperty, positionX);
            k.SetValue(InkCanvas.TopProperty, positionY);
            k.Height = Double.NaN;
            k.Width = width;
            k.SourceReference = reference;
            k.OnReferenceNavigateRequest += new ReferenceNavigateRequest(k_OnReferenceNavitageRequest);

            k.CommentNote = new KnowledgeMapEntityCommentNote { Visibility = Visibility.Collapsed };
            k.CommentNote._tb_NoteArea.Text = comment;
            k.CommentNote.ParentEntity = k;
            this._IC_MapCanvas.Children.Add(k.CommentNote);

            this._nonShapeEntities.Add(k);
            this._IC_MapCanvas.Children.Add(k);

            //this.SetInputState(KnowledgeMapInputState.Select);
            //List<UIElement> uiel = new List<UIElement>();
            //uiel.Add(k);
            //this.SelectEnteties(uiel);

            this.UndoStack.Push(new UndoRedoAction
            {
                Action = UndoRedoActions.AddEntity,
                Entity = k
            });

        }

        private void AddDraggedEntity(KnowledgeMapEntityBase entity,
            double positionX, double positionY)
        {
            positionX = this.AdjustXPositionToFitMap(positionX, entity.ActualWidth);
            positionY = this.AdjustYPositionToFitMap(positionY, 25);

            if (entity is KnowledgeMapImageEntity)
            {
                KnowledgeMapImageEntity ie = entity as KnowledgeMapImageEntity;
                ie.SetValue(InkCanvas.LeftProperty, positionX);
                ie.SetValue(InkCanvas.TopProperty, positionY);
                ie.OnReferenceNavigateRequest += new ReferenceNavigateRequest(k_OnReferenceNavitageRequest);
                this._nonShapeEntities.Add(ie);
                this._IC_MapCanvas.Children.Add(ie);
            }
            else if (entity is KnowledgeMapTextEntity)
            {
                KnowledgeMapTextEntity te = entity as KnowledgeMapTextEntity;
                te.SetValue(InkCanvas.LeftProperty, positionX);
                te.SetValue(InkCanvas.TopProperty, positionY);
                te.OnReferenceNavigateRequest += new ReferenceNavigateRequest(k_OnReferenceNavitageRequest);
                this._nonShapeEntities.Add(te);
                this._IC_MapCanvas.Children.Add(te);
            }

            this.UndoStack.Push(new UndoRedoAction
            {
                Action = UndoRedoActions.AddEntity,
                Entity = entity
            });

            //this.SetInputState(KnowledgeMapInputState.Select);
        }

        public void SetKnowledgeMapGuide(string name, ImageSource source,
            double width, double height, double x, double y)
        {
            //return;
            /* Todo:
             * Decide whether the guide should be placed in the center of the KM
             * or in the Top-Left corner. Once this has been decided, calculate the
             * size of the image control to fit the chosen option.
             */
            try
            {

                this._knowledgeMapGuideControl = new Image();

                this._knowledgeMapGuideControl.Source = source;

                if (x < 1)
                    this._knowledgeMapGuideControl.SetValue(InkCanvas.LeftProperty, 1.0);
                else
                    this._knowledgeMapGuideControl.SetValue(InkCanvas.LeftProperty, x);

                if (y < 1)
                    this._knowledgeMapGuideControl.SetValue(InkCanvas.TopProperty, 1.0);
                else
                    this._knowledgeMapGuideControl.SetValue(InkCanvas.TopProperty, y);

                if (width > 0)
                    this._knowledgeMapGuideControl.Width = width;

                if (height > 0)
                    this._knowledgeMapGuideControl.Height = height;

                this._IC_MapCanvas.Children.Insert(0, this._knowledgeMapGuideControl);

            }
            catch (Exception)
            {
                this._knowledgeMapGuideControl = null;

                throw new ApplicationException(Application.Current.Resources["EX_SetGuideFailed"] as string);
            }
        }

        public void ClearKnowledgeMapGuide()
        {
            try
            {
                if (this._knowledgeMapGuideControl != null)
                    this._IC_MapCanvas.Children.Remove(this._knowledgeMapGuideControl);
                this._knowledgeMapGuideControl = null;
                if (this._knowledgeMapGuideDataStream != null)
                    this._knowledgeMapGuideDataStream.Close();
            }
            catch (Exception)
            {
                throw new ApplicationException(Application.Current.Resources["EX_DelGuideFailed"] as string);
            }
        }

        private Stream GetGuideImageStream()
        {
            if (this._knowledgeMapGuideDataStream == null ||
                this._knowledgeMapGuideDataStream.CanRead == false)
            {
                BitmapImage source = this._knowledgeMapGuideControl.Source as BitmapImage;

                if (source != null)
                {
                    source.StreamSource.Seek(0, SeekOrigin.Begin);
                    byte[] iCopy = new byte[(int)source.StreamSource.Length];
                    source.StreamSource.Read(iCopy, 0, (int)source.StreamSource.Length);
                    this._knowledgeMapGuideDataStream = new MemoryStream(iCopy);
                }
                else
                {
                    BitmapFrame bmpsource = this._knowledgeMapGuideControl.Source as BitmapFrame;
                    if (bmpsource != null)
                    {
                        BmpBitmapEncoder bmpEnc = new BmpBitmapEncoder();
                        this._knowledgeMapGuideDataStream = new MemoryStream();
                        bmpEnc.Frames.Add(bmpsource);
                        bmpEnc.Save(this._knowledgeMapGuideDataStream);
                        this._knowledgeMapGuideDataStream.Flush();
                    }
                    else
                        throw new InvalidCastException(Application.Current.Resources["EX_LoadGuideFailed"] as string);
                }
            }

            this._knowledgeMapGuideDataStream.Seek(0, SeekOrigin.Begin);
            return this._knowledgeMapGuideDataStream;
        }

        #endregion

        #region Propage Events and Notifications

        private void k_OnReferenceNavitageRequest(XpsDocumentReference reference)
        {
            if (this.OnEntityRequestedReferenceNavigate != null)
                this.OnEntityRequestedReferenceNavigate(reference);
        }

        public void PropagateIncreaseFontSize()
        {
            foreach (UIElement uie in this._IC_MapCanvas.GetSelectedElements())
            {
                if (uie is KnowledgeMapEntityBase)
                {
                    if (uie is KnowledgeMapTextEntity)
                    {
                        ((KnowledgeMapTextEntity)uie).IncreaseFontSizeBy(4);
                    }
                }
                else if (uie is TextBox)
                {
                    TextBox tb = uie as TextBox;
                    if (tb.FontSize < 48)
                        tb.FontSize += 4;
                }
            }
        }

        public void PropagateDecreaseFontSize()
        {
            foreach (UIElement uie in this._IC_MapCanvas.GetSelectedElements())
            {
                if (uie is KnowledgeMapEntityBase)
                {
                    if (uie is KnowledgeMapTextEntity)
                    {
                        ((KnowledgeMapTextEntity)uie).DecreaseFontSizeBy(4);
                    }
                }
                else if (uie is TextBox)
                {
                    TextBox tb = uie as TextBox;
                    if (tb.FontSize > 8)
                        tb.FontSize -= 4;
                }
            }
        }

        public void PropagateColorSelectionEvent(SolidColorBrush brush, bool updateSelection)
        {
            this._currentBrush = brush;
            this._currentDrawingAttributes = this._currentDrawingAttributes.Clone();
            this._currentDrawingAttributes.Color = brush.Color;
            this._IC_MapCanvas.DefaultDrawingAttributes = this._currentDrawingAttributes;

            if (!updateSelection)
                return;

            foreach (UIElement uie in this._IC_MapCanvas.GetSelectedElements())
            {

                if (brush.Color != Colors.Black)
                {
                    if (uie is KnowledgeMapEntityBase)
                    {
                        if (uie is KnowledgeMapTextEntity)
                        {
                            if (this.OnLinkedEntityColorChanged != null)
                                this.OnLinkedEntityColorChanged(((KnowledgeMapTextEntity)uie).SourceReference.TargetLineId, "", brush);

                            if (((KnowledgeMapTextEntity)uie).EntityType == KnowledgeMapEntityType.OriginalToMap)
                                return;
                        }

                        this.UndoStack.Push(
                            new UndoRedoAction
                            {
                                Action = UndoRedoActions.SetEntityColor,
                                Entity = uie as KnowledgeMapEntityBase,
                                Color = ((KnowledgeMapEntityBase)uie).GetEntityColor()
                            });

                        this.RedoStack.Clear();

                        ((KnowledgeMapEntityBase)uie).SetEntityColor(brush);

                        if (uie is KnowledgeMapImageEntity)
                        {
                            if (this.OnLinkedEntityColorChanged != null)
                                this.OnLinkedEntityColorChanged(Guid.Empty, ((KnowledgeMapImageEntity)uie).TargetPathData, brush);
                        }
                    }
                }

                if (uie is TextBox)
                {
                    this.UndoStack.Push(
                            new UndoRedoAction
                            {
                                Action = UndoRedoActions.SetTextLabelColor,
                                TextLabel = uie as TextBox,
                                Color = (((TextBox)uie).Foreground as SolidColorBrush).Color
                            });

                    this.RedoStack.Clear();

                    ((TextBox)uie).Foreground = brush;
                }
            }

            foreach (Stroke s in this._IC_MapCanvas.GetSelectedStrokes())
            {
                DrawingAttributes d = s.DrawingAttributes.Clone();

                this.UndoStack.Push(
                            new UndoRedoAction
                            {
                                Action = UndoRedoActions.SetStrokeColor,
                                InkStroke = s,
                                Color = d.Color
                            });

                this.RedoStack.Clear();

                d.Color = brush.Color;
                s.DrawingAttributes = d;
            }
        }

        public void PropagateDocumentLineDeleted(Guid lineId)
        {
            List<UIElement> elementsToRemove = new List<UIElement>();
            foreach (UIElement uie in this._IC_MapCanvas.Children)
            {
                if (uie is KnowledgeMapEntityBase)
                {

                    if (((KnowledgeMapEntityBase)uie).EntityType == KnowledgeMapEntityType.OriginalToMap)
                        continue;
                    else
                    {
                        if (((KnowledgeMapEntityBase)uie).SourceReference.TargetLineId == lineId)
                        {
                            this._nonShapeEntities.Remove(uie);
                            elementsToRemove.Add(uie);
                        }
                    }
                }
            }

            foreach (UIElement elementToRemove in elementsToRemove)
            {
                if (elementToRemove is KnowledgeMapTextEntity)
                {
                    KnowledgeMapTextEntity textEntity = elementToRemove as KnowledgeMapTextEntity;
                    if (this._IC_MapCanvas.Children.Contains(textEntity.CommentNote))
                        this._IC_MapCanvas.Children.Remove(textEntity.CommentNote);
                    if (this._IC_MapCanvas.Strokes.Contains(textEntity.CommentConnectorStroke))
                        this._IC_MapCanvas.Strokes.Remove(textEntity.CommentConnectorStroke);
                }
                else if (elementToRemove is KnowledgeMapImageEntity)
                {
                    KnowledgeMapImageEntity imageEntity = elementToRemove as KnowledgeMapImageEntity;
                    if (this._IC_MapCanvas.Children.Contains(imageEntity.CommentNote))
                        this._IC_MapCanvas.Children.Remove(imageEntity.CommentNote);
                    if (this._IC_MapCanvas.Strokes.Contains(imageEntity.CommentConnectorStroke))
                        this._IC_MapCanvas.Strokes.Remove(imageEntity.CommentConnectorStroke);
                }

                if (this._IC_MapCanvas.Children.Contains(elementToRemove))
                    this._IC_MapCanvas.Children.Remove(elementToRemove);
            }
        }

        public void PropagateDocumentLineContentsChanged(Helpers.DragDropQuote data)
        {
            foreach (UIElement uie in this._IC_MapCanvas.Children)
            {
                if (uie is KnowledgeMapEntityBase)
                {
                    if (((KnowledgeMapEntityBase)uie).EntityType == KnowledgeMapEntityType.OriginalToMap)
                        continue;
                    else
                    {
                        if (((KnowledgeMapEntityBase)uie).SourceReference.TargetLineId == data.Reference.TargetLineId)
                        {
                            if (uie is KnowledgeMapTextEntity)
                            {
                                KnowledgeMapTextEntity kmT = uie as KnowledgeMapTextEntity;
                                kmT.Body = data.UnicodeString;
                                Color newColor = Color.FromArgb(255, data.Color.R, data.Color.G, data.Color.B);
                                kmT.SetEntityColor(new SolidColorBrush(data.Color));
                            }
                            else if (uie is KnowledgeMapImageEntity)
                            {
                                KnowledgeMapImageEntity kmI = uie as KnowledgeMapImageEntity;
                                kmI.SetEntityColor(new SolidColorBrush(data.Color));
                            }
                        }
                    }
                }
            }
        }

        public void PropagateImageLineColorChanged(Color newColor, string targetPathData)
        {
            newColor = Color.FromArgb(255, newColor.R, newColor.G, newColor.B);
            foreach (UIElement uie in this._IC_MapCanvas.Children)
            {
                if (uie is KnowledgeMapEntityBase)
                {
                    if (((KnowledgeMapEntityBase)uie).EntityType == KnowledgeMapEntityType.OriginalToMap)
                        continue;
                    else
                    {
                        if (uie is KnowledgeMapImageEntity)
                        {
                            KnowledgeMapImageEntity kmI = uie as KnowledgeMapImageEntity;
                            if (kmI.TargetPathData == targetPathData)
                            {
                                kmI.SetEntityColor(new SolidColorBrush(newColor));
                            }
                        }
                    }
                }
            }
        }

        public void PropagateImageLineDeleted(string targetPathData, Guid lineParentDocumentId)
        {
            List<UIElement> elementsToRemove = new List<UIElement>();

            foreach (UIElement uie in this._IC_MapCanvas.Children)
            {
                if (uie is KnowledgeMapEntityBase)
                {
                    if (((KnowledgeMapEntityBase)uie).EntityType == KnowledgeMapEntityType.OriginalToMap)
                        continue;
                    else
                    {
                        if (uie is KnowledgeMapImageEntity)
                        {
                            KnowledgeMapImageEntity kmI = uie as KnowledgeMapImageEntity;
                            if (kmI.TargetPathData == targetPathData
                                && kmI.SourceReference.DocumentId == lineParentDocumentId)
                            {
                                this._nonShapeEntities.Remove(uie);
                                elementsToRemove.Add(uie);
                            }
                        }
                    }
                }
            }

            foreach (UIElement elementToRemove in elementsToRemove)
            {
                if (this._IC_MapCanvas.Children.Contains(elementToRemove))
                    this._IC_MapCanvas.Children.Remove(elementToRemove);
            }
        }

        #endregion

        public List<KnowledgeMapEntityBase> FindConnectedEntities(Guid parentId, string parentPathData, Guid documentId)
        {
            List<KnowledgeMapEntityBase> foundElements = new List<KnowledgeMapEntityBase>();

            foreach (UIElement uie in this._IC_MapCanvas.Children)
            {
                if (uie is KnowledgeMapEntityBase)
                {
                    if (uie is KnowledgeMapTextEntity)
                    {
                        if (((KnowledgeMapTextEntity)uie).SourceReference.TargetLineId == parentId
                            && ((KnowledgeMapTextEntity)uie).SourceReference.DocumentId == documentId)
                            foundElements.Add(uie as KnowledgeMapEntityBase);
                    }
                    else if (uie is KnowledgeMapImageEntity)
                    {
                        if (((KnowledgeMapImageEntity)uie).SourceReference.ParentPathData == parentPathData
                            && ((KnowledgeMapImageEntity)uie).SourceReference.DocumentId == documentId)
                            foundElements.Add(uie as KnowledgeMapEntityBase);
                    }
                }
            }

            return foundElements;
        }

        #region Invoke Copy & Paste

        /// <summary>
        /// Invoke to manually place a copy of the currently selected entity (if any)
        /// onto the Clipboard.
        /// </summary>
        /// <returns>bool if there where object copied to the clipboard.</returns>
        public bool InvokeCopyCommand()
        {
            if (this._IC_MapCanvas.GetSelectedElements().Count == 0)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Invoke to manually plaste a copy of the current entity (if any)
        /// in the Clipboard onto the KM.
        /// </summary>
        /// <returns>bool if there where objects copied to the Km.</returns>
        public bool InvokePasteCommand()
        {
            //KnowledgeMapTextEntity t = (KnowledgeMapTextEntity)Clipboard.GetData("KnowledgeMapTextEntity");
            //this._IC_MapCanvas.Children.Add(t);
            return true;
        }

        #endregion

        #region Importing Map Object

        public void ImportMapObject(EjpLib.BaseClasses.ejpKnowledgeMap map)
        {
            this._localMapObject = map;
            this._parentStudyId = map.Id;

            //Import all the text entities.
            foreach (EjpLib.BaseClasses.ejpKMTextEntity kmte in map.TextEntities)
            {
                KnowledgeMapEntityType enType = KnowledgeMapEntityType.OriginalToMap;
                try
                {
                    enType = (KnowledgeMapEntityType)Enum.Parse(typeof(KnowledgeMapEntityType), kmte.EntityType.ToString());
                }
                catch (System.ArgumentException)
                {
                    enType = KnowledgeMapEntityType.OriginalToMap;
                }

                KnowledgeMapTextEntity k = new KnowledgeMapTextEntity(kmte.Id, enType);
                k.OnReferenceNavigateRequest += new ReferenceNavigateRequest(this.k_OnReferenceNavitageRequest);

                k.CommentText = kmte.CommentText;

                //081210
                //Always set the alpha channel of the entity color to 255.
                //This is to update old assignments that use a prev. Coloring scheme.
                kmte.Color._a = 255;

                k._TB_TextArea.Text = kmte.Body;
                k._TB_TitleArea.Text = kmte.Title;
                k._TB_TextArea.FontSize = (kmte.FontSize > 0) ? kmte.FontSize : 10;
                k.SetValue(InkCanvas.LeftProperty, kmte.X);
                k.SetValue(InkCanvas.TopProperty, kmte.Y);
                k.SetEntityColor(
                    new SolidColorBrush(
                        new Color
                        {
                            A = kmte.Color._a,
                            R = kmte.Color._r,
                            G = kmte.Color._g,
                            B = kmte.Color._b
                        }
                        ));

                k.SourceReference =
                    new XpsDocumentReference
                    {
                        TargetLineId = kmte.SourceReference.TargetLineId,
                        AnchorX = kmte.SourceReference.AnchorX,
                        AnchorY = kmte.SourceReference.AnchorY,
                        Content = kmte.SourceReference.Content,
                        DocumentId = kmte.SourceReference.DocumentId,
                        DocumentParentStudyId = kmte.SourceReference.DocumentParentStudyId,
                        PageNumber = kmte.SourceReference.PageNumber,
                        DocumentTitle = kmte.SourceReference.DocumentTitle
                    };

                if (enType == KnowledgeMapEntityType.OriginalToMap)
                    k.SetEntityColor(Brushes.Black);

                k.Width = (kmte.Width > 0.0) ? kmte.Width : 150;
                k.Height = (kmte.Height > 0.0) ? kmte.Height : Double.NaN;
                this._IC_MapCanvas.Children.Add(k);
                this._nonShapeEntities.Add(k);
            }

            foreach (EjpLib.BaseClasses.ejpKMImageEntity kmi in map.ImageEntities)
            {
                if (map.Guide != null) //basically a version check...
                {
                    if (kmi.Id == map.Guide.Id)
                    {
                        this.SetKnowledgeMapGuide("", kmi.BitmapImageSource, map.Guide.Width,
                            map.Guide.Height, map.Guide.X, map.Guide.Y);
                        continue;
                    }
                }


                KnowledgeMapEntityType enType = KnowledgeMapEntityType.OriginalToMap;
                try
                {
                    enType = (KnowledgeMapEntityType)Enum.Parse(typeof(KnowledgeMapEntityType), kmi.EntityType.ToString());
                }
                catch (System.ArgumentException)
                {
                    enType = KnowledgeMapEntityType.OriginalToMap;
                }

                KnowledgeMapImageEntity ie = new KnowledgeMapImageEntity(kmi.Id);
                ie.EntityType = enType;
                ie.OnReferenceNavigateRequest += new ReferenceNavigateRequest(this.k_OnReferenceNavitageRequest);

                ie.CommentText = kmi.CommentText;

                ie._TB_TitleArea.Text = kmi.Title;
                ie.SetValue(InkCanvas.LeftProperty, kmi.X);
                ie.SetValue(InkCanvas.TopProperty, kmi.Y);
                ie.SetEntityColor(
                    new SolidColorBrush(
                        new Color
                        {
                            A = kmi.Color._a,
                            R = kmi.Color._r,
                            G = kmi.Color._g,
                            B = kmi.Color._b
                        }
                        ));

                ie.SourceReference =
                    new XpsDocumentReference
                    {
                        TargetLineId = kmi.SourceReference.TargetLineId,
                        AnchorX = kmi.SourceReference.AnchorX,
                        AnchorY = kmi.SourceReference.AnchorY,
                        Content = kmi.SourceReference.Content,
                        DocumentId = kmi.SourceReference.DocumentId,
                        DocumentParentStudyId = kmi.SourceReference.DocumentParentStudyId,
                        PageNumber = kmi.SourceReference.PageNumber
                    };

                if (enType == KnowledgeMapEntityType.OriginalToMap)
                    ie.SetEntityColor(Brushes.Black);

                ie.Width = kmi.Width;
                ie.Height = kmi.Height;
                ie.TargetPathData = kmi.TargetPathData;
                ie.SourceReference.ParentPathData = kmi.TargetPathData;

                //080613
                //ie.ImageStream = kmi.ImageStream;

                //080616
                ie._I_ImageArea.Source = kmi.BitmapImageSource;

                this._IC_MapCanvas.Children.Add(ie);
                this._nonShapeEntities.Add(ie);
            }

            //Import all the shapes.
            foreach (EjpLib.BaseClasses.ejpKMShape kmshape in map.ShapeEntities)
            {
                foreach (Stroke s in kmshape.Strokes)
                    this._IC_MapCanvas.Strokes.Add(s);
            }

            //Import all the Connected Lines.
            foreach (EjpLib.BaseClasses.ejpKMConnectedStroke kmcs in map.ConnectedStrokes)
            {
                KnowledgeMapEntityBase sourceEntity = null;
                KnowledgeMapEntityBase targetEntity = null;
                foreach (UIElement uie in this._IC_MapCanvas.Children)
                {
                    KnowledgeMapEntityBase kmbe = uie as KnowledgeMapEntityBase;
                    if (kmbe != null)
                    {
                        if (kmbe.Id == kmcs.SourceEntityId)
                            sourceEntity = kmbe;
                        else if (kmbe.Id == kmcs.TargetEntityId)
                            targetEntity = kmbe;
                    }
                }

                StrokeArrowMode arrowMode = StrokeArrowMode.None;
                try
                {
                    arrowMode = (StrokeArrowMode)Enum.Parse(typeof(StrokeArrowMode), kmcs.ArrowMode.ToString());
                }
                catch (System.ArgumentException)
                {
                    arrowMode = StrokeArrowMode.None;
                }

                if (sourceEntity != null)
                {
                    KnowledgeMapConnectedLine cl = new KnowledgeMapConnectedLine(kmcs.Strokes[0], arrowMode, sourceEntity, targetEntity);
                    sourceEntity.AddConnectedStrokes(cl);
                    if (targetEntity != null)
                        targetEntity.AddConnectedStrokes(cl);

                    this._IC_MapCanvas.Strokes.Add(cl.Stroke);
                }

            }

            //import all the labels
            foreach (EjpLib.BaseClasses.ejpKMLabel label in map.Labels)
            {
                TextBox tb = new TextBox();
                tb.SetValue(InkCanvas.LeftProperty, label.X);
                tb.SetValue(InkCanvas.TopProperty, label.Y);
                tb.FontSize = label.FontSize;
                tb.Foreground = new SolidColorBrush(
                    Color.FromArgb(label.Color._a, label.Color._r, label.Color._g, label.Color._b));
                tb.Background = Brushes.Transparent;
                tb.AcceptsReturn = true;
                tb.Text = label.Contents;
                tb.BorderThickness = new Thickness(0);
                this._nonShapeEntities.Add(tb);
                this._IC_MapCanvas.Children.Add(tb);
            }

            //import all the comments
            if (map.Comments == null) //this is basically a version check
            {
                map.Comments = new List<SiliconStudio.Meet.EjpLib.BaseClasses.ejpCAComment>();
            }

            foreach (EjpLib.BaseClasses.ejpCAComment comment in map.Comments)
            {
                KnowledgeMapComment newComment = new KnowledgeMapComment()
                {
                    CurrentAuthorId = this.CurrentOwnerId,
                    CurrentAuthorName = this.CurrentOwnerName,
                    OriginalAuthorName = comment.AuthorName,
                    OriginalAuthorId = comment.AuthorId,
                    CommentId = comment.CommentId,
                    Width = 200,
                    Height = 300,
                    OriginalCoordinates = new Point(comment.OriginalPositionX, comment.OriginalPositionY),
                    PushPinCoordinates = new Point((comment.PositionX + 200) - 25, comment.PositionY)
                };

                if (comment.Messages != null) //== empty comment box..
                {
                    foreach (EjpLib.BaseClasses.ejpCACommentMessage message in comment.Messages)
                    {
                        newComment.Messages.Add(
                            new CommentMessage()
                            {
                                Date = message.Date,
                                AuthorId = message.AuthorId,
                                Author = message.Author,
                                Message = message.Message
                            }
                            );
                    }
                }

                Canvas.SetLeft(newComment, comment.PositionX);
                Canvas.SetTop(newComment, comment.PositionY);
                this._commentsList.Add(newComment);
                this._cv_CommentSurface.Children.Add(newComment);
                newComment.OnDeleteComment += new DeleteKnowledgeMapComment(OnCommentDeleted);
                newComment.OnMaximizeComment += new KnowledgeMapCommentViewStateChanged(comment_OnMaximizeComment);
                newComment.OnMinimizeComment += new KnowledgeMapCommentViewStateChanged(comment_OnMinimizeComment);
            }
        }

        #endregion

        #region Exporting Map and Contents

        private Int32Rect GetVisualsOnCanvasBoundingBox()
        {
            double top = double.MaxValue;
            double left = double.MaxValue;
            double bottom = double.MinValue;
            double right = double.MinValue;

            foreach (UIElement uie in this._IC_MapCanvas.Children)
            {
                if (uie is Control)
                {
                    Control c = uie as Control;

                    double cLeft = (double)c.GetValue(InkCanvas.LeftProperty);
                    double cTop = (double)c.GetValue(InkCanvas.TopProperty);
                    double cBottom = cTop + c.ActualHeight;
                    double cRight = cLeft + c.ActualWidth;

                    if (cLeft < left) left = cLeft;
                    if (cTop < top) top = cTop;
                    if (cBottom > bottom) bottom = cBottom;
                    if (cRight > right) right = cRight;
                }
            }

            if (this._knowledgeMapGuideControl != null)
            {
                double cLeft = (double)this._knowledgeMapGuideControl.GetValue(InkCanvas.LeftProperty);
                double cTop = (double)this._knowledgeMapGuideControl.GetValue(InkCanvas.TopProperty);
                double cBottom = cTop + this._knowledgeMapGuideControl.ActualHeight;
                double cRight = cLeft + this._knowledgeMapGuideControl.ActualWidth;

                if (cLeft < left) left = cLeft;
                if (cTop < top) top = cTop;
                if (cBottom > bottom) bottom = cBottom;
                if (cRight > right) right = cRight;
            }

            foreach (Stroke s in this._IC_MapCanvas.Strokes)
            {
                if (s.GetBounds().Left < left) left = s.GetBounds().Left;
                if (s.GetBounds().Top < top) top = s.GetBounds().Top;
                if (s.GetBounds().Bottom > bottom) bottom = s.GetBounds().Bottom;
                if (s.GetBounds().Right > right) right = s.GetBounds().Right;
            }

            Int32Rect result = new Int32Rect(
                (int)Math.Floor(left),
                (int)Math.Floor(top),
                (int)Math.Ceiling(right - left),
                (int)Math.Ceiling(bottom - top));

            result.X = (result.X > 0) ? result.X : 0;
            result.Y = (result.Y > 0) ? result.Y : 0;
            result.Width =
                (result.Width < this._IC_MapCanvas.ActualWidth) ?
                result.Width : (int)Math.Ceiling(this._IC_MapCanvas.ActualWidth - result.X);
            result.Height =
                (result.Height < this._IC_MapCanvas.ActualHeight) ?
                result.Height : (int)Math.Ceiling(this._IC_MapCanvas.ActualHeight - result.Y);

            return result;

        }

        public void ExportMapToImage(string path)
        {
            try
            {
                int Height = (int)Math.Ceiling(this._IC_MapCanvas.ActualHeight);
                int Width = (int)Math.Ceiling(this._IC_MapCanvas.ActualWidth);

                if (this._IC_MapCanvas.GetSelectionBounds() != Rect.Empty)
                {

                    Rect dSelRect = this._IC_MapCanvas.GetSelectionBounds();
                    Int32Rect selectRect = new Int32Rect((int)dSelRect.X + (int)this._IC_MapCanvas.Margin.Left, (int)dSelRect.Y + (int)this._IC_MapCanvas.Margin.Top, (int)dSelRect.Width + 2, (int)dSelRect.Height + 2);

                    ReadOnlyCollection<UIElement> selUIE = this._IC_MapCanvas.GetSelectedElements();
                    StrokeCollection selStroke = this._IC_MapCanvas.GetSelectedStrokes();

                    this._IC_MapCanvas.Select(null, null);
                    this._IC_MapCanvas.UpdateLayout();

                    RenderTargetBitmap outImage = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);

                    //Try to reverse any scaling applied to the MapCanvas
                    if (this._IC_MapCanvas.LayoutTransform is ScaleTransform)
                    {
                        ScaleTransform st = this._IC_MapCanvas.LayoutTransform as ScaleTransform;
                        DrawingVisual zeroScaleDv = new DrawingVisual();
                        using (DrawingContext dc = zeroScaleDv.RenderOpen())
                        {
                            VisualBrush vb = new VisualBrush(this._IC_MapCanvas);
                            dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(Width, Height)));
                        }
                        outImage.Render(zeroScaleDv);
                    }
                    else
                        outImage.Render(this._IC_MapCanvas);

                    CroppedBitmap cpb = new CroppedBitmap(outImage, selectRect);

                    BitmapEncoder encoder = null;
                    if (path.EndsWith(".png"))
                        encoder = new PngBitmapEncoder();
                    else if (path.EndsWith(".jpg"))
                        encoder = new JpegBitmapEncoder();
                    else if (path.EndsWith(".bmp"))
                        encoder = new BmpBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(cpb));
                    using (Stream stm = File.Create(path))
                    {
                        encoder.Save(stm);
                    }
                    this._IC_MapCanvas.Select(selStroke, selUIE);

                }
                else
                {

                    RenderTargetBitmap outPutImage = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);

                    //Try to reverse any scaling applied to the MapCanvas
                    if (this._IC_MapCanvas.LayoutTransform is ScaleTransform)
                    {
                        ScaleTransform st = this._IC_MapCanvas.LayoutTransform as ScaleTransform;
                        DrawingVisual zeroScaleDv = new DrawingVisual();
                        using (DrawingContext dc = zeroScaleDv.RenderOpen())
                        {
                            VisualBrush vb = new VisualBrush(this._IC_MapCanvas);
                            dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(Width, Height)));
                        }
                        outPutImage.Render(zeroScaleDv);
                    }
                    else
                        outPutImage.Render(this._IC_MapCanvas);

                    BitmapEncoder encoder = null;
                    if (path.EndsWith(".png"))
                        encoder = new PngBitmapEncoder();
                    else if (path.EndsWith(".jpg"))
                        encoder = new JpegBitmapEncoder();
                    else if (path.EndsWith(".bmp"))
                        encoder = new BmpBitmapEncoder();

                    encoder.Frames.Add(BitmapFrame.Create(outPutImage));
                    using (Stream stm = File.Create(path))
                    {
                        encoder.Save(stm);
                    }

                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Transforms the current KM into a Base64 encoded string.
        /// </summary>
        public string ExportMapToBase64String()
        {
            try
            {
                int Height = (int)Math.Ceiling(this._IC_MapCanvas.ActualHeight);
                int Width = (int)Math.Ceiling(this._IC_MapCanvas.ActualWidth);

                RenderTargetBitmap outPutImage = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);

                //Try to reverse any scaling applied to the MapCanvas
                if (this._IC_MapCanvas.LayoutTransform is ScaleTransform)
                {
                    ScaleTransform st = this._IC_MapCanvas.LayoutTransform as ScaleTransform;
                    DrawingVisual zeroScaleDv = new DrawingVisual();
                    using (DrawingContext dc = zeroScaleDv.RenderOpen())
                    {
                        VisualBrush vb = new VisualBrush(this._IC_MapCanvas);
                        dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(Width, Height)));
                    }
                    outPutImage.Render(zeroScaleDv);
                }
                else
                    outPutImage.Render(this._IC_MapCanvas);

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(outPutImage));
                string base64String = "";
                using (MemoryStream stm = new MemoryStream())
                {
                    encoder.Save(stm);
                    byte[] array = stm.ToArray();
                    base64String = Convert.ToBase64String(array);
                }

                return base64String;
            }
            catch (Exception)
            {
                return "Failed";
            }
        }

        /// <summary>
        /// Transforms the current KM into a byte array.
        /// </summary>
        public byte[] ExportMapToByteArray()
        {
            try
            {

                int Height = (int)Math.Ceiling(this._IC_MapCanvas.ActualHeight);
                int Width = (int)Math.Ceiling(this._IC_MapCanvas.ActualWidth);

                RenderTargetBitmap outImage = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);

                //Try to reverse any scaling applied to the MapCanvas
                if (this._IC_MapCanvas.LayoutTransform is ScaleTransform)
                {
                    ScaleTransform st = this._IC_MapCanvas.LayoutTransform as ScaleTransform;
                    DrawingVisual zeroScaleDv = new DrawingVisual();
                    using (DrawingContext dc = zeroScaleDv.RenderOpen())
                    {
                        VisualBrush vb = new VisualBrush(this._IC_MapCanvas);
                        dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(Width, Height)));
                    }
                    outImage.Render(zeroScaleDv);
                }
                else
                    outImage.Render(this._IC_MapCanvas);

                Int32Rect vizRect = this.GetVisualsOnCanvasBoundingBox();

                CroppedBitmap cpb = new CroppedBitmap(outImage, vizRect);

                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(cpb));
                byte[] result;
                using (MemoryStream stm = new MemoryStream())
                {
                    encoder.Save(stm);
                    result = stm.ToArray();
                }

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void ExportMapObject()
        {
            this._localMapObject.TextEntities.Clear();
            this._localMapObject.ImageEntities.Clear();
            this._localMapObject.Labels.Clear();

            foreach (UIElement entity in this._IC_MapCanvas.Children)
            {
                KnowledgeMapTextEntity kmt = entity as KnowledgeMapTextEntity;
                if (kmt != null)
                {
                    if (kmt.CommentNote != null)
                        kmt.CommentText = kmt.CommentNote.CommentText;

                    this._localMapObject.TextEntities.Add(
                        new EjpLib.BaseClasses.ejpKMTextEntity(
                        kmt.Body, kmt.Title, (int)kmt.EntityType, kmt.Id,
                        kmt.GetBounds(0).Left,
                        kmt.GetBounds(0).Top,
                        kmt.GetBounds(0).Width,
                        kmt.GetBounds(0).Height,
                        kmt.FontSize,
                        new EjpLib.BaseClasses.ejpDocumentReference
                        {
                            TargetLineId = kmt.SourceReference.TargetLineId,
                            AnchorX = kmt.SourceReference.AnchorX,
                            AnchorY = kmt.SourceReference.AnchorY,
                            Content = kmt.SourceReference.Content,
                            DocumentId = kmt.SourceReference.DocumentId,
                            DocumentTitle = kmt.SourceReference.DocumentTitle,
                            DocumentParentStudyId = kmt.SourceReference.DocumentParentStudyId,
                            PageNumber = kmt.SourceReference.PageNumber
                        },
                        new EjpLib.BaseClasses.ejpSolidColor
                        {
                            _a = kmt.Color.Color.A,
                            _r = kmt.Color.Color.R,
                            _g = kmt.Color.Color.G,
                            _b = kmt.Color.Color.B
                        },
                        kmt.CommentText
                        ));
                }

                KnowledgeMapImageEntity kmi = entity as KnowledgeMapImageEntity;
                if (kmi != null)
                {
                    if (kmi.CommentNote != null)
                        kmi.CommentText = kmi.CommentNote.CommentText;

                    this._localMapObject.ImageEntities.Add(
                        new EjpLib.BaseClasses.ejpKMImageEntity(
                            kmi.Title, (int)kmi.EntityType, kmi.Id,
                            kmi.GetBounds(0).Left,
                        kmi.GetBounds(0).Top,
                        kmi.GetBounds(0).Width,
                        kmi.GetBounds(0).Height,
                        new EjpLib.BaseClasses.ejpDocumentReference
                        {
                            TargetLineId = kmi.SourceReference.TargetLineId,
                            AnchorX = kmi.SourceReference.AnchorX,
                            AnchorY = kmi.SourceReference.AnchorY,
                            Content = kmi.SourceReference.Content,
                            DocumentId = kmi.SourceReference.DocumentId,
                            DocumentParentStudyId = kmi.SourceReference.DocumentParentStudyId,
                            PageNumber = kmi.SourceReference.PageNumber
                        },
                        new EjpLib.BaseClasses.ejpSolidColor
                        {
                            _a = kmi.Color.Color.A,
                            _r = kmi.Color.Color.R,
                            _g = kmi.Color.Color.G,
                            _b = kmi.Color.Color.B
                        },
                        "", kmi.TargetPathData, kmi.CommentText,
                        kmi.ImageStream
                        ));
                }

                TextBox tb = entity as TextBox;
                if (tb != null)
                {
                    this._localMapObject.Labels.Add(
                        new EjpLib.BaseClasses.ejpKMLabel
                        {
                            Id = EjpLib.Helpers.IdManipulation.GetNewGuid(),
                            Contents = tb.Text,
                            FontSize = tb.FontSize,
                            X = (double)tb.GetValue(InkCanvas.LeftProperty),
                            Y = (double)tb.GetValue(InkCanvas.TopProperty),
                            Color = new SiliconStudio.Meet.EjpLib.BaseClasses.ejpSolidColor
                            {
                                _a = ((SolidColorBrush)tb.Foreground).Color.A,
                                _b = ((SolidColorBrush)tb.Foreground).Color.B,
                                _g = ((SolidColorBrush)tb.Foreground).Color.G,
                                _r = ((SolidColorBrush)tb.Foreground).Color.R
                            }
                        });
                }
            }

            /* Guide Image Object, disguised as a regular image object!
             * Beware of the Ninja code that stabs you in the back!
             */
            if (this._knowledgeMapGuideControl != null)
            {
                this._localMapObject.Guide.Height = this._knowledgeMapGuideControl.ActualHeight;
                this._localMapObject.Guide.Width = this._knowledgeMapGuideControl.ActualWidth;
                this._localMapObject.Guide.X = (double)this._knowledgeMapGuideControl.GetValue(InkCanvas.LeftProperty);
                this._localMapObject.Guide.Y = (double)this._knowledgeMapGuideControl.GetValue(InkCanvas.TopProperty);

                this._localMapObject.Guide.Id = EjpLib.Helpers.IdManipulation.GetNewGuid();

                this._localMapObject.ImageEntities.Add(
                           new EjpLib.BaseClasses.ejpKMImageEntity(
                               "", (int)KnowledgeMapEntityType.OriginalToMap,
                               this._localMapObject.Guide.Id,
                               this._localMapObject.Guide.X,
                               this._localMapObject.Guide.Y,
                               this._localMapObject.Guide.Width,
                               this._localMapObject.Guide.Height,
                           new EjpLib.BaseClasses.ejpDocumentReference(),
                           new EjpLib.BaseClasses.ejpSolidColor(),
                           "", "", "",
                           this.GetGuideImageStream()
                           ));
            }
            /*end guide code*/

            this._localMapObject.ConnectedStrokes.Clear();
            List<Stroke> connectedStrokes = new List<Stroke>();			//not to be included when exporting the regular strokes collection.
            foreach (UIElement entity in this._IC_MapCanvas.Children)
            {
                KnowledgeMapTextEntity kmt = entity as KnowledgeMapTextEntity;
                if (kmt != null)
                {
                    foreach (KnowledgeMapConnectedLine cl in kmt.ConnectedLines)
                    {
                        //Never export duplicate connected strokes,
                        //instead, when importing them add them from the source to
                        //the target entity.
                        if (connectedStrokes.Contains(cl.Stroke))
                            continue;

                        //Some connected strokes still live inside the entities but not
                        //on the canvas. This is to make Undo/Redo easier. However,
                        //if the connected stroke is no longer live on the canvas, it
                        //should not be saved.
                        if (this._IC_MapCanvas.Strokes.Contains(cl.Stroke) == false)
                            continue;

                        Guid tempGuid = Guid.Empty;
                        if (cl.TargetEntity != null)
                            tempGuid = cl.TargetEntity.Id;

                        this._localMapObject.ConnectedStrokes.Add(
                            new SiliconStudio.Meet.EjpLib.BaseClasses.ejpKMConnectedStroke(
                            cl.SourceEntity.Id, tempGuid, cl.Stroke, (int)cl.ArrowMode
                            ));

                        connectedStrokes.Add(cl.Stroke);
                    }
                }
            }

            this._localMapObject.ShapeEntities.Clear();
            foreach (Stroke s in this._IC_MapCanvas.Strokes)
            {
                bool isCommentConnector = false;
                foreach (UIElement uie in this._nonShapeEntities)
                {
                    if (uie is KnowledgeMapEntityBase)
                    {
                        if (((KnowledgeMapEntityBase)uie).CommentConnectorStroke == s)
                        {
                            isCommentConnector = true;
                            break;
                        }
                    }
                }

                if (connectedStrokes.Contains(s) || isCommentConnector)
                    continue;

                else
                {
                    EjpLib.BaseClasses.ejpKMShape shape = new SiliconStudio.Meet.EjpLib.BaseClasses.ejpKMShape(s);
                    this._localMapObject.ShapeEntities.Add(shape);
                }
            }

            //this is basically a version check. (if value is null, this file was created before EJP supported CA) 
            if (this._localMapObject.Comments != null)
            {
                List<KnowledgeMapComment> commentsToMax = new List<KnowledgeMapComment>();

                this._localMapObject.Comments.Clear();
                foreach (KnowledgeMapComment comment in this._commentsList)
                {

                    if (comment.CurrentViewState == KnowledgeMapCommentViewState.Maximized)
                    {
                        comment.CurrentViewState = KnowledgeMapCommentViewState.Minimized;
                        commentsToMax.Add(comment);
                    }

                    Point commentLocation = new Point()
                    {
                        X = Canvas.GetLeft(comment) - 175,
                        Y = Canvas.GetTop(comment)
                    };

                    //if (comment.CurrentViewState == KnowledgeMapCommentViewState.Minimized)
                    //{
                    //    commentLocation.X -= 175;
                    //}


                    EjpLib.BaseClasses.ejpCAComment com =
                        new SiliconStudio.Meet.EjpLib.BaseClasses.ejpCAComment()
                    {
                        AuthorId = comment.OriginalAuthorId,
                        AuthorName = comment.OriginalAuthorName,
                        CommentId = comment.CommentId,
                        PositionX = commentLocation.X,
                        PositionY = commentLocation.Y,
                        OriginalPositionX = comment.OriginalCoordinates.X,
                        OriginalPositionY = comment.OriginalCoordinates.Y
                    };

                    if (com.Messages == null)
                        com.Messages = new List<SiliconStudio.Meet.EjpLib.BaseClasses.ejpCACommentMessage>();

                    foreach (CommentMessage item in comment.Messages)
                    {
                        com.Messages.Add(
                            new SiliconStudio.Meet.EjpLib.BaseClasses.ejpCACommentMessage()
                            {
                                Author = item.Author,
                                AuthorId = item.AuthorId,
                                Message = item.Message,
                                Date = item.Date
                            }
                            );
                    }

                    this._localMapObject.Comments.Add(com);
                }

                foreach (KnowledgeMapComment comMax in commentsToMax)
                {
                    comMax.CurrentViewState = KnowledgeMapCommentViewState.Maximized;
                }
            }

        }

        #endregion

        public void PrintKnowledgeMap()
        {
            int Height = (int)this._IC_MapCanvas.ActualHeight;
            int Width = (int)this._IC_MapCanvas.ActualWidth;

            RenderTargetBitmap outPutImage = new RenderTargetBitmap(Width, Height, 96, 96, PixelFormats.Pbgra32);
            outPutImage.Render(this._IC_MapCanvas);

            PrintDocumentImageableArea pd_ia = null;
            XpsDocumentWriter docWriter = PrintQueue.CreateXpsDocumentWriter(ref pd_ia);
            if (docWriter != null && pd_ia != null)
            {
                docWriter.Write(this._IC_MapCanvas);
            }

        }

        #region Undo Redo

        public void PropagateUndo()
        {
            try
            {
                UndoRedoAction ura = this.UndoStack.Pop();

                switch (ura.Action)
                {
                    case UndoRedoActions.DeleteEntity:
                        this.ReAddKnowledgeMapEntity(ura.Entity);
                        break;
                    case UndoRedoActions.AddEntity:
                        this.RemoveKnowledgeMapEntity(ura.Entity);
                        break;
                    case UndoRedoActions.DeleteStroke:
                        this.ReAddInkStroke(ura.InkStroke);
                        break;
                    case UndoRedoActions.AddStroke:
                        this.RemoveInkStroke(ura.InkStroke);
                        break;
                    case UndoRedoActions.AddTextLabel:
                        this.RemoveTextLabel(ura.TextLabel);
                        break;
                    case UndoRedoActions.DeleteTextLabel:
                        this.ReAddTextLabel(ura.TextLabel);
                        break;
                    case UndoRedoActions.SetTextLabelColor:
                        Color t_Color = (((TextBox)ura.TextLabel).Foreground as SolidColorBrush).Color;
                        this.SetTextLabelColor(ura.TextLabel, ura.Color);
                        ura.Color = t_Color;
                        break;
                    case UndoRedoActions.SetEntityColor:
                        Color r_Color = ura.Entity.GetEntityColor();
                        this.SetEntityColor(ura.Entity, ura.Color);
                        ura.Color = r_Color;
                        break;
                    case UndoRedoActions.SetStrokeColor:
                        Color s_Color = ura.InkStroke.DrawingAttributes.Color;
                        this.SetStrokeColor(ura.InkStroke, ura.Color);
                        ura.Color = s_Color;
                        break;
                    default:
                        break;
                }

                this.RedoStack.Push(ura);
            }
            catch (Exception)
            {
            }
        }

        public void PropagateRedo()
        {
            try
            {
                UndoRedoAction ura = this.RedoStack.Pop();

                switch (ura.Action)
                {
                    case UndoRedoActions.DeleteEntity:
                        this.RemoveKnowledgeMapEntity(ura.Entity);
                        break;
                    case UndoRedoActions.AddEntity:
                        this.ReAddKnowledgeMapEntity(ura.Entity);
                        break;
                    case UndoRedoActions.DeleteStroke:
                        this.RemoveInkStroke(ura.InkStroke);
                        break;
                    case UndoRedoActions.AddStroke:
                        this.ReAddInkStroke(ura.InkStroke);
                        break;
                    case UndoRedoActions.AddTextLabel:
                        this.ReAddTextLabel(ura.TextLabel);
                        break;
                    case UndoRedoActions.DeleteTextLabel:
                        this.RemoveTextLabel(ura.TextLabel);
                        break;
                    case UndoRedoActions.SetTextLabelColor:
                        Color t_Color = (((TextBox)ura.TextLabel).Foreground as SolidColorBrush).Color;
                        this.SetTextLabelColor(ura.TextLabel, ura.Color);
                        ura.Color = t_Color;
                        break;
                    case UndoRedoActions.SetEntityColor:
                        Color r_Color = ura.Entity.GetEntityColor();
                        this.SetEntityColor(ura.Entity, ura.Color);
                        ura.Color = r_Color;
                        break;
                    case UndoRedoActions.SetStrokeColor:
                        Color s_Color = ura.InkStroke.DrawingAttributes.Color;
                        this.SetStrokeColor(ura.InkStroke, ura.Color);
                        ura.Color = s_Color;
                        break;
                    default:
                        break;
                }

                this.UndoStack.Push(ura);
            }
            catch (Exception)
            {
            }
        }

        private void ReAddTextLabel(TextBox textLabel)
        {
            this._IC_MapCanvas.Children.Add(textLabel);
        }

        private void RemoveTextLabel(TextBox textLabel)
        {
            if (this._IC_MapCanvas.Children.Contains(textLabel))
                this._IC_MapCanvas.Children.Remove(textLabel);
        }

        private void SetTextLabelColor(TextBox textLabel, Color color)
        {
            textLabel.Foreground = new SolidColorBrush(color);
        }

        private void SetStrokeColor(Stroke inkStroke, Color color)
        {
            DrawingAttributes da = inkStroke.DrawingAttributes.Clone();
            da.Color = color;
            inkStroke.DrawingAttributes = da;
        }

        private void SetEntityColor(KnowledgeMapEntityBase entity, Color color)
        {
            entity.SetEntityColor(new SolidColorBrush(color));

            if (entity is KnowledgeMapImageEntity)
            {
                if (this.OnLinkedEntityColorChanged != null)
                    this.OnLinkedEntityColorChanged(Guid.Empty, ((KnowledgeMapImageEntity)entity).TargetPathData, new SolidColorBrush(color));
            }
            else if (entity is KnowledgeMapTextEntity)
            {
                if (this.OnLinkedEntityColorChanged != null)
                    this.OnLinkedEntityColorChanged(((KnowledgeMapTextEntity)entity).SourceReference.TargetLineId, "", new SolidColorBrush(color));
            }
        }

        private void ReAddInkStroke(Stroke inkStroke)
        {
            this._IC_MapCanvas.Strokes.Add(inkStroke);
        }

        private void RemoveInkStroke(Stroke inkStroke)
        {
            this._IC_MapCanvas.Strokes.Remove(inkStroke);
        }

        private void ReAddKnowledgeMapEntity(KnowledgeMapEntityBase entity)
        {
            KnowledgeMapTextEntity te = entity as KnowledgeMapTextEntity;
            if (te != null)
            {
                if (te.CommentNote != null)
                    this._IC_MapCanvas.Children.Add(te.CommentNote);

                this._IC_MapCanvas.Children.Add(te);
                this._nonShapeEntities.Add(te);
                te.DrawCommentConnectorLine();
                return;
            }

            KnowledgeMapImageEntity ie = entity as KnowledgeMapImageEntity;
            if (te != null)
            {
                this._IC_MapCanvas.Children.Add(ie.CommentNote);
                this._IC_MapCanvas.Children.Add(ie);
                ie.DrawCommentConnectorLine();
                this._nonShapeEntities.Add(ie);
                return;
            }
        }

        private void RemoveKnowledgeMapEntity(KnowledgeMapEntityBase entity)
        {
            if (this._IC_MapCanvas.Children.Contains(entity))
            {
                if (entity is KnowledgeMapTextEntity)
                {
                    KnowledgeMapTextEntity textEntity = entity as KnowledgeMapTextEntity;
                    if (this._IC_MapCanvas.Children.Contains(textEntity.CommentNote))
                        this._IC_MapCanvas.Children.Remove(textEntity.CommentNote);

                }
                else if (entity is KnowledgeMapImageEntity)
                {
                    KnowledgeMapImageEntity imageEntity = entity as KnowledgeMapImageEntity;
                    if (this._IC_MapCanvas.Children.Contains(imageEntity.CommentNote))
                        this._IC_MapCanvas.Children.Remove(imageEntity.CommentNote);
                }

                if (this._IC_MapCanvas.Strokes.Contains(entity.CommentConnectorStroke))
                    this._IC_MapCanvas.Strokes.Remove(entity.CommentConnectorStroke);

                this._IC_MapCanvas.Children.Remove(entity);
            }

            if (this._nonShapeEntities.Contains(entity))
                this._nonShapeEntities.Remove(entity);
        }

        private enum UndoRedoActions
        {
            DeleteEntity,
            AddEntity,
            DeleteStroke,
            AddStroke,
            AddTextLabel,
            DeleteTextLabel,
            SetTextLabelColor,
            SetEntityColor,
            SetStrokeColor
        }

        private class UndoRedoAction
        {
            public TextBox TextLabel { get; set; }
            public KnowledgeMapEntityBase Entity { get; set; }
            public Stroke InkStroke { get; set; }
            public UndoRedoActions Action { get; set; }
            public Color Color { get; set; }

            public UndoRedoAction()
            {
            }
        }

        #endregion

        #region CommentMode Related Methods

        public void SetCommentMode()
        {
            this._cv_CommentSurface.Visibility = Visibility.Visible;
            this.IsEditingLocked = true;
        }

        public void SetNormalMode()
        {
            this._cv_CommentSurface.Visibility = Visibility.Collapsed;

            foreach (KnowledgeMapComment kmC in this._commentsList)
            {
                StringBuilder sb = new StringBuilder();
                foreach (CommentMessage cm in kmC.Messages)
                {
                    sb.AppendLine(cm.Message);
                    sb.AppendLine(cm.Date.ToString());
                    if (cm.Author.Length != 0)
                        sb.AppendLine(cm.Author);
                    sb.AppendLine();
                }

                this.AddTextEntity(Application.Current.Resources["Str_ConvertedCommentNodeTitle"] as string, sb.ToString(),
                    new XpsDocumentReference(), "",
                        KnowledgeMapEntityType.OriginalToMap,
                        Brushes.Black,
                        kmC.OriginalCoordinates.X,
                        kmC.OriginalCoordinates.Y,
                        200, 300);
            }

            this._commentsList.Clear();
            this.IsEditingLocked = false;
        }

        #region On CMS Canvas Input Down

        private bool _isDraggingComment = false;
        private Point _commentDragPositionOffset = new Point(0, 0);
        private KnowledgeMapComment _currentlyDraggedComment = null;
        public bool IsMapCommentLayerUserLocked { get; set; }

        private void _CMS_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            KnowledgeMapComment comment = this.HittestComments(e.GetPosition(this._cv_CommentSurface));
            if (comment != null)
            {
                this.InitDragComment(e.GetPosition(comment), comment);
            }
        }

        private void _CMS_StylusDown(object sender, StylusDownEventArgs e)
        {
            KnowledgeMapComment comment = this.HittestComments(e.GetPosition(this._cv_CommentSurface));
            if (comment != null)
            {
                this.InitDragComment(e.GetPosition(comment), comment);
            }
        }

        private void _CMS_MouseMove(object sender, MouseEventArgs e)
        {
            this._CMS_InputMove(e.GetPosition(this._cv_CommentSurface));
        }

        private void _CMS_StylusMove(object sender, StylusEventArgs e)
        {
            this._CMS_InputMove(e.GetPosition(this._cv_CommentSurface));
        }

        private void _CMS_InputMove(Point position)
        {
            if (this._isDraggingComment && this._currentlyDraggedComment != null)
            {
                if (Mouse.LeftButton == MouseButtonState.Released)
                {
                    this._isDraggingComment = false;
                    this._commentDragPositionOffset = new Point(0, 0);
                    this._currentlyDraggedComment.IsDragging = false;
                    return;
                }

                this._currentlyDraggedComment.IsDragging = true;
                double newX = position.X - this._commentDragPositionOffset.X;
                double newY = position.Y - this._commentDragPositionOffset.Y;
                if (newX > 0 && newX < (this._cv_CommentSurface.Width - 26))
                {
                    Canvas.SetLeft(
                        this._currentlyDraggedComment, newX);
                }
                if (newY > 0 && newY < (this._cv_CommentSurface.Height - 26))
                {
                    Canvas.SetTop(
                        this._currentlyDraggedComment, newY);
                }

                Point newPushP = new Point();
                if (this._currentlyDraggedComment.CurrentViewState
                    == KnowledgeMapCommentViewState.Maximized)
                    newPushP = new Point(newX + 175, newY);
                else
                    newPushP = new Point(newX, newY);

                this._currentlyDraggedComment.PushPinCoordinates = newPushP;
            }
        }

        private void InitDragComment(Point offset, KnowledgeMapComment comment)
        {
            if (this.IsMapCommentLayerUserLocked)
            {
                if (this.CurrentOwnerId == comment.OriginalAuthorId)
                {
                    this._isDraggingComment = true;
                    this._commentDragPositionOffset = offset;
                    this._currentlyDraggedComment = comment;
                }
            }
            else
            {
                this._isDraggingComment = true;
                this._commentDragPositionOffset = offset;
                this._currentlyDraggedComment = comment;
            }
        }

        #endregion

        #region On CMS Canvas Input Up

        private void _CMS_StylusOutOfRange(object sender, StylusEventArgs e)
        {
            if (this._isDraggingComment)
            {
                this._isDraggingComment = false;
                this._commentDragPositionOffset = new Point(0, 0);
                this._currentlyDraggedComment.IsDragging = false;
            }
        }

        private void _CMS_StylusUp(object sender, StylusEventArgs e)
        {
            this.OnCMSCanvasInputUp(e.GetPosition(this._cv_CommentSurface));
        }

        private void _CMS_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.OnCMSCanvasInputUp(e.GetPosition(this._cv_CommentSurface));
        }

        private void OnCMSCanvasInputUp(Point position)
        {
            if (this._isDraggingComment)
            {
                this._isDraggingComment = false;
                this._commentDragPositionOffset = new Point(0, 0);
                this._currentlyDraggedComment.IsDragging = false;
            }
            else
            {
                if (this._inputState == KnowledgeMapInputState.PushPin)
                {
                    KnowledgeMapComment comment = this.HittestComments(position);
                    if (comment == null)
                    {
                        this.InsertNewComment(
                          position);
                    }
                }
            }
        }

        #endregion

        private KnowledgeMapComment HittestComments(Point positionToTest)
        {
            KnowledgeMapComment result = null;
            foreach (KnowledgeMapComment comment in this._commentsList)
            {
                Rect clientRect = new Rect(
                    Canvas.GetLeft(comment),
                    Canvas.GetTop(comment),
                    comment.Width,
                    comment.Height);
                if (clientRect.Contains(positionToTest))
                    result = comment;
            }

            return result;
        }

        private void InsertNewComment(Point origin)
        {
            try
            {
                Point originalPosition = origin;

                KnowledgeMapComment comment = new KnowledgeMapComment()
                    {
                        OriginalAuthorId = this.CurrentOwnerId,
                        OriginalAuthorName = this.CurrentOwnerName,
                        CurrentAuthorId = this.CurrentOwnerId,
                        CurrentAuthorName = this.CurrentOwnerName,
                        CommentId = Guid.NewGuid(),
                        OriginalCoordinates = origin,
                        Width = 200,
                        Height = 300,
                    };

                this._cv_CommentSurface.Children.Add(comment);

                if ((origin.X - comment.Width) < 0)
                    origin.X = comment.Width + 5;
                if ((origin.Y + comment.Height) > this._IC_MapCanvas.ActualHeight)
                    origin.Y = (this._IC_MapCanvas.ActualHeight - comment.Height) - 5;

                Canvas.SetLeft(comment, origin.X - 188);
                Canvas.SetTop(comment, origin.Y - 12);

                comment.PushPinCoordinates = new Point(originalPosition.X - 12, originalPosition.Y - 12);
                this._commentsList.Add(comment);

                comment.OnDeleteComment +=
                    new DeleteKnowledgeMapComment(OnCommentDeleted);
                comment.OnMaximizeComment +=
                    new KnowledgeMapCommentViewStateChanged(comment_OnMaximizeComment);
                comment.OnMinimizeComment +=
                    new KnowledgeMapCommentViewStateChanged(comment_OnMinimizeComment);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void comment_OnMinimizeComment(KnowledgeMapComment comment)
        {
            Debug.Print("In KM");
            Canvas.SetLeft(comment, comment.PushPinCoordinates.X);
            Canvas.SetTop(comment, comment.PushPinCoordinates.Y);
        }

        private void comment_OnMaximizeComment(KnowledgeMapComment comment)
        {
            double newLeft = Canvas.GetLeft(comment);

            if (newLeft < 0)
                Canvas.SetLeft(comment, 10);

            double newTop = Canvas.GetTop(comment);
            // [shiniwa] Need to check if the size of _IC_MapCanvas is 0 here.. Or, the comment will unexpectedly go away.
            if ((newTop + 300) > this._IC_MapCanvas.ActualHeight && this._IC_MapCanvas.ActualHeight > 1.0d)
                Canvas.SetTop(comment, this._IC_MapCanvas.ActualHeight - 310);
        }

        private void OnCommentDeleted(KnowledgeMapComment comment)
        {
            this._cv_CommentSurface.Children.Remove(comment);
            this._commentsList.Remove(comment);
        }

        #endregion

        #region Finalizing

        /// <summary>
        /// Used only to lock the current map for
        /// thread safety.
        /// </summary>
        private object _locker = new object();

        /// <summary>
        /// Release all Resources used by this Map.
        /// </summary>
        public void Release()
        {
            lock (this._locker)
            {
                foreach (UIElement entity in this._nonShapeEntities)
                {
                    KnowledgeMapEntityBase baseE = entity as KnowledgeMapEntityBase;
                    if (baseE != null)
                    {
                        baseE.Release();
                    }
                }
            }
        }

        #endregion


        public void HideMapLock()
        {
            this._B_ToggleMapLock.Visibility = Visibility.Collapsed;
            this._B_ToggleMapLock.IsEnabled = false;
        }

        public void ShowMapLock()
        {
            this._B_ToggleMapLock.Visibility = Visibility.Visible;
            this._B_ToggleMapLock.IsEnabled = true;
        }
    }

}