using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SiliconStudio.Meet.EjpControls.Enumerations;

namespace SiliconStudio.Meet.EjpControls
{
	/// <summary>
	/// Interaction logic for KnowledgeMapImageEntity.xaml
	/// </summary>
	public partial class KnowledgeMapImageEntity : KnowledgeMapEntityBase
	{
		public event ReferenceNavigateRequest OnReferenceNavigateRequest;

		private SolidColorBrush _color;
		public SolidColorBrush Color
		{
			set { this.SetEntityColor(value); }
			get { return this._color; }
		}

		public string Title
		{
			set { this._TB_TitleArea.Text = value; }
			get { return this._TB_TitleArea.Text; }
		}

		public string CommentText
		{
			get { return this._commentText; }
			set { this._commentText = value; }
		}

		public string ImageSourceUri { get; set; }
		private MemoryStream _imageMemoryStream = null;
		public Stream ImageStream
		{
			get
			{
				if (this._imageMemoryStream == null)
				{
					BitmapImage source = this._I_ImageArea.Source as BitmapImage;

					if (source != null)
					{
						source.StreamSource.Seek(0, SeekOrigin.Begin);
						byte[] iCopy = new byte[(int)source.StreamSource.Length];
						source.StreamSource.Read(iCopy, 0, (int)source.StreamSource.Length);
						this._imageMemoryStream = new MemoryStream(iCopy);
					}
					else
					{
						BitmapFrame bmpsource = this._I_ImageArea.Source as BitmapFrame;
						if (bmpsource != null)
						{
							BmpBitmapEncoder pngEnc = new BmpBitmapEncoder();
							this._imageMemoryStream = new MemoryStream();
							pngEnc.Frames.Add(bmpsource);
							pngEnc.Save(this._imageMemoryStream);
							this._imageMemoryStream.Flush();
						}
						else
							throw new InvalidCastException(Application.Current.Resources["EX_LoadKMIMGEntityFailed"] as string);//Properties.Resources.EX_LoadKMIMGEntityFailed);
					}
				}

				this._imageMemoryStream.Seek(0, SeekOrigin.Begin);
				return this._imageMemoryStream;
			}
		}

		public KnowledgeMapEntityCommentNote CommentNote
		{
			get
			{
				if (this._commentNote == null)
				{
					this._commentNote = new KnowledgeMapEntityCommentNote();
					this._commentNote.Visibility = Visibility.Collapsed;
					this._commentNote.CommentText = this._commentText;
					this._commentNote.ParentEntity = this;
				}
				return this._commentNote;
			}
			set { this._commentNote = value; }
		}

		private string _targetPathData;
		public string TargetPathData
		{
			get { return _targetPathData; }
			set { _targetPathData = value; }
		}

		public KnowledgeMapImageEntity(Guid id)
		{
			InitializeComponent();
			this._id = id;
			this._expanded = true;
			this._currentlyDisplayedAnchorPoints = new List<UIElement>();
			this._connectedLines = new List<KnowledgeMapConnectedLine>();
			this.SizeChanged += new SizeChangedEventHandler(KnowledgeMapImageEntity_SizeChanged);
			//this._commentNote = new KnowledgeMapEntityCommentNote();
			//this._commentNote.CommentText = this._commentText;
			//this._commentNote.ParentEntity = this;
		}

		public KnowledgeMapImageEntity()
		{
			this._commentNote = new KnowledgeMapEntityCommentNote();
			this._commentNote.CommentText = this._commentText;
			this._commentNote.ParentEntity = this;
		}

		void KnowledgeMapImageEntity_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			try
			{
				this.UpdateAllConnectedLines();
				this.DrawCommentConnectorLine();
			}
			catch (Exception)
			{
				//Fail silently
			}
		}

		public override void SetEntityColor(SolidColorBrush newColor)
		{
			//this._r_BodyRectangle.Fill = newColor;
			this._r_TitleRectangle.Fill = newColor;
			this._color = newColor;
		}

		public override Color GetEntityColor()
		{
			return this._color.Color;
		}

		public override void DisplayAnchorPoint(Rect location)
		{
			if (this._currentlyDisplayedAnchorPoints.Count > 0)
				return;

			Rectangle r = new Rectangle();
			r.Fill = this.Resources["AnchorPointBrush"] as DrawingBrush;
			r.Margin = new Thickness(location.Left, location.Top, 0, 0);
			r.Width = location.Width;
			r.Height = location.Height;
			//r.Opacity = 0.3f;
			r.SetValue(Grid.RowSpanProperty, 4);
			r.HorizontalAlignment = HorizontalAlignment.Left;
			r.VerticalAlignment = VerticalAlignment.Top;
			this.RootGrid.Children.Add(r);
			this._currentlyDisplayedAnchorPoints.Add(r);
		}

		protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
		{
			base.OnMouseDoubleClick(e);
			if (this.EntityType == KnowledgeMapEntityType.ConnectedToDocument)
			{
				if (this.OnReferenceNavigateRequest != null)
				{
					if (this._sourceReference != null)
						this.OnReferenceNavigateRequest(this._sourceReference);
				}
			}
		}

		public override void ShowCommentNote(object sender, RoutedEventArgs e)
		{
			InkCanvas parentCanvas = this.Parent as InkCanvas;

			if (this._commentNote == null)
			{
				this._commentNote = new KnowledgeMapEntityCommentNote();
				this._commentNote.CommentText = this._commentText;
				this._commentNote.ParentEntity = this;

			}	//TODO: do we really need to set this?
			else if (this._commentNote.ParentEntity == null)
			{
				this._commentNote.ParentEntity = this;
			}
			if (!parentCanvas.Children.Contains(this._commentNote))
			{
				parentCanvas.Children.Add(this._commentNote);
			}

			if (parentCanvas.Children.Contains(this._commentNote) == false)
				parentCanvas.Children.Add(this._commentNote);

			double x = (double)this.GetValue(InkCanvas.LeftProperty);
			double y = (double)this.GetValue(InkCanvas.TopProperty);

			x = x + this.Width + 5;
			y = y - (this._commentNote.Height * 0.5);

			if (x >= (parentCanvas.Width - (this._commentNote.Width + 5)))
				x = (x - (this.Width + 10)) - this._commentNote.Width;
			if (x <= 0)
				x = 5;
			if (y <= 0)
				y = 5;

			this._commentNote.SetValue(InkCanvas.LeftProperty, x);
			this._commentNote.SetValue(InkCanvas.TopProperty, y);

			this._commentNote.Visibility = Visibility.Visible;

			this.DrawCommentConnectorLine();
		}

		public override void HideAnchorPoints()
		{
			foreach (UIElement e in this._currentlyDisplayedAnchorPoints)
			{
				this.RootGrid.Children.Remove(e);
			}
			this._currentlyDisplayedAnchorPoints.Clear();
		}

		private void OnNavigateReference(object sender, RoutedEventArgs e)
		{
			if (this._entityType == KnowledgeMapEntityType.ConnectedToDocument)
			{
				if (this.OnReferenceNavigateRequest != null)
				{
					if (this._sourceReference != null)
						this.OnReferenceNavigateRequest(this._sourceReference);
				}
			}
		}

		private void OnToggleExpanded(object sender, RoutedEventArgs e)
		{
			if (this._expanded)
			{
				this._previousHeight = this.Height;
				this.Height = 45;
				this._expanded = false;
				this._E_ExpanderBar.VerticalAlignment = VerticalAlignment.Top;
			}
			else
			{
				this.Height = this._previousHeight;
				this._expanded = true;
				this._E_ExpanderBar.VerticalAlignment = VerticalAlignment.Bottom;
			}
		}

		/// <summary>
		/// Release all Resources used by this entity, in
		/// particular the stream that holds the Image loaded into
		/// the Image area.
		/// </summary>
		public override void Release()
		{
			lock (this._locker)
			{
				try
				{

					if (this._imageMemoryStream != null)
						this._imageMemoryStream.Dispose();

				}
				catch (ObjectDisposedException)
				{
				}
				catch (InvalidOperationException)
				{
				}
				catch (Exception)
				{
				}
			}
		}

		public override void DrawCommentConnectorLine()
		{
			InkCanvas parentCanvas = this.Parent as InkCanvas;

			if (this._commentNote == null)
				return;
			else if (this._commentNote.Visibility == Visibility.Collapsed)
			{
				if (parentCanvas.Strokes.Contains(this.CommentConnectorStroke))
					parentCanvas.Strokes.Remove(this.CommentConnectorStroke);

				parentCanvas.Select((IEnumerable<UIElement>)null);
				parentCanvas.Children.Remove(this._commentNote);

				return;
			}

			if (this.CommentConnectorStroke != null)
			{
				if (parentCanvas.Strokes.Contains(this.CommentConnectorStroke))
					parentCanvas.Strokes.Remove(this.CommentConnectorStroke);
			}

			if (this.CommentNote.Visibility == Visibility.Visible)
			{
				StylusPointCollection spCollection = new StylusPointCollection();

				//Start center-below entity
				spCollection.Add(new StylusPoint(
					this.GetBounds(0).Left + (this.ActualWidth * 0.5),
					this.GetBounds(2).Bottom + 2));

				//Go to 10p below start
				spCollection.Add(new StylusPoint(
					this.GetBounds(0).Left + (this.ActualWidth * 0.5),
					this.GetBounds(2).Bottom + 10));

				//Go left/right to the center of dist. between comment and entity
				if ((double)this.CommentNote.GetValue(InkCanvas.LeftProperty) > spCollection[0].X)
				{
					double xDiff = ((double)this.CommentNote.GetValue(InkCanvas.LeftProperty) - this.GetBounds(0).Right) * 0.5;
					spCollection.Add(new StylusPoint(
					this.GetBounds(0).Right + xDiff,
					this.GetBounds(2).Bottom + 10));
				}
				else
				{
					double xDiff = (this.GetBounds(0).Left -
						((double)this.CommentNote.GetValue(InkCanvas.LeftProperty) + this.CommentNote.ActualWidth)) * 0.5;
					spCollection.Add(new StylusPoint(
					this.GetBounds(0).Left - xDiff,
					this.GetBounds(2).Bottom + 10));
				}

				//Go up/down to the center of diff between comment and entity
				spCollection.Add(new StylusPoint(
					spCollection[2].X,
					(double)this.CommentNote.GetValue(InkCanvas.TopProperty) - 10));

				//Go to the top of the comment note
				spCollection.Add(new StylusPoint(
					(double)this.CommentNote.GetValue(InkCanvas.LeftProperty) + (this.CommentNote.ActualWidth * 0.5),
					(double)this.CommentNote.GetValue(InkCanvas.TopProperty) - 10));

				//End center-on top of comment.
				spCollection.Add(new StylusPoint(
					(double)this.CommentNote.GetValue(InkCanvas.LeftProperty) + (this.CommentNote.ActualWidth * 0.5),
					(double)this.CommentNote.GetValue(InkCanvas.TopProperty) - 2));

				this.CommentConnectorStroke = new Stroke(spCollection);
				parentCanvas.Strokes.Add(this.CommentConnectorStroke);
			}
		}
	}
}
