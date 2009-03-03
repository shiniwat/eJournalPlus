using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SiliconStudio.Meet.EjpControls.Helpers
{
	public class DocumentLine
	{
		private Color _originalColor;

		private PenLinePart _start;
		public PenLinePart Start
		{
			get { return _start; }
			set
			{
				_start = value;
			}
		}

		private PenLinePart _end;
		public PenLinePart End
		{
			get { return _end; }
			set
			{
				_end = value;
			}
		}

		private List<PenLinePart> _lineParts = new List<PenLinePart>();
		public List<PenLinePart> LineParts
		{
			get { return _lineParts; }
		}

		public Enumerations.E_DocumentEntityAdornerType LineType { get; set; }
		public Guid ParentDocumentId { get; set; }
		public Guid ParentStudyId { get; set; }

		internal int StartPageNumber { get; set; }

		private bool _hasComment;
		public bool HasComment
		{
			get { return _hasComment; }
			set { _hasComment = value; }
		}

		private DocumentLineComment _comment;
		public DocumentLineComment Comment
		{
			get { return _comment; }
			set { _comment = value; }
		}

		private bool _isCommentVisualOpen;
		public bool IsCommentVisualOpen
		{
			get { return _isCommentVisualOpen; }
			set { _isCommentVisualOpen = value; }
		}

		private Rectangle _commentIcon;
		public Rectangle CommentIcon
		{
			get { return _commentIcon; }
			set { _commentIcon = value; }
		}

		public string Contents
		{
			get
			{
				//Start by locating the Start Char index in the Start string
				double startCharOffset = 0.0d;
				int startCharIndex = 0;

				GlyphRun sGRun = this.Start.Glyphs.ToGlyphRun();

				for (int i = 0; i < (sGRun.AdvanceWidths.Count); i++)
				{
					startCharOffset += sGRun.AdvanceWidths[i];
					if (startCharOffset >= (this.Start.OffsetStart))
					{
						startCharIndex = i;
						break;
					}
				}

				//Then find the end char index in the End string
				int endCharIndex = this.End.Glyphs.UnicodeString.Length - 1;
				double endCharOffset = this.End.Glyphs.ToGlyphRun().ComputeAlignmentBox().Width;
				double z = this.End.OffsetStart + this.End.Width;

				GlyphRun eGRun = this.End.Glyphs.ToGlyphRun();

				for (int j = this.End.Glyphs.UnicodeString.Length - 1; j >= 0; j--)
				{
					endCharOffset -= eGRun.AdvanceWidths[j];
					if (endCharOffset <= (z))
					{
						endCharIndex = j;
						break;
					}

				}

				//If start and end are the same, 
				//Substring only the Start string
				if (this.Start == this.End)
				{
					string st = this.Start.Glyphs.UnicodeString.Substring(startCharIndex, (endCharIndex - startCharIndex));
					return st;
				}
				else //Otherwise puzzle it together from the pieces
				{
					string st = this.Start.Glyphs.UnicodeString.Substring(startCharIndex);
					foreach (PenLinePart plp in this.LineParts)
					{
						//Need to find a way to preserve line breaks.
						if (plp.Glyphs.UnicodeString == " ")
						{
							Debug.Print(plp.Glyphs.UnicodeString);
							st += "\n";
						}


						if (plp == this.Start
							|| plp == this.End)
							continue;

						st += plp.Glyphs.UnicodeString;
					}
					st += this.End.Glyphs.UnicodeString.Substring(0, endCharIndex + 1);
					return st;
				}
			}
			set
			{
				this.Contents = value;
			}
		}

		public Point Anchor
		{
			get
			{
				return new Point(
					this.Start.BoxLeft, this.Start.BoxTop);
			}
		}

		internal Rect BoundingBox
		{
			get
			{
				return this.GetBoundingBox();
			}
		}

		private Guid _id;
		public Guid Id
		{
			get
			{
				//if the Id has not been set, set it now.
				if (this._id == null)
					this._id = EjpLib.Helpers.IdManipulation.GetNewGuid();
				return _id;
			}
			set { this._id = value; }
		}

		private ColorAnimation _selectedAnim;

		/// <summary>
		/// Default Constructor.
		/// </summary>
		public DocumentLine()
		{
			this.BuildAnim();
		}

		private void BuildAnim()
		{
			this._selectedAnim = new ColorAnimation(
				Color.FromArgb(128, 200, 200, 200), Color.FromArgb(255, 25, 25, 25),
				new Duration(new TimeSpan(0, 0, 0, 0, 500)), FillBehavior.Stop);
			this._selectedAnim.RepeatBehavior = RepeatBehavior.Forever;
			this._selectedAnim.AutoReverse = true;
		}

		public bool ContainsGlyphs(Glyphs glyphs)
		{
			foreach (PenLinePart plp in this.LineParts)
			{
				if (plp.Glyphs == glyphs)
					return true;
			}
			return false;
		}

		//Make sure the handles stay aligned to the
		//borders of the glyphs it contains.
		public void AlignHandlesToGlyphs(double previousLineOffsetLimit, double nextLineOffsetLimit,
			bool adjustStart, bool adjustEnd)
		{
			if (adjustStart)
			{
				//first do the start:
				//Start by locating the Start Char offset in the Start string
				double startCharOffset = 0.0d;
				GlyphRun sGRun = this.Start.Glyphs.ToGlyphRun();
				for (int i = 0; i < (sGRun.AdvanceWidths.Count); i++)
				{
					startCharOffset += sGRun.AdvanceWidths[i];
					if (startCharOffset >= (this.Start.OffsetStart))
					{
						startCharOffset -= this.Start.StartHandle.Width;
						break;
					}
				}

				//Update the position of the start handle and the length of the line
				Canvas.SetLeft(this.Start.StartHandle, (this.Start.Glyphs.OriginX + startCharOffset));
				double lpLeft = Canvas.GetLeft(this.Start.Line);
				Canvas.SetLeft(this.Start.Line, (this.Start.Glyphs.OriginX + startCharOffset));
				double lnLeft = Canvas.GetLeft(this.Start.Line);
				this.Start.Line.Width += (lpLeft - lnLeft);
			}

			if (adjustEnd)
			{
				//then do the end :)
				double endCharOffset = this.End.Glyphs.ToGlyphRun().ComputeAlignmentBox().Width;
				double z = this.End.OffsetStart + this.End.Width;
				GlyphRun eGRun = this.End.Glyphs.ToGlyphRun();

				for (int j = this.End.Glyphs.UnicodeString.Length - 1; j >= 0; j--)
				{
					endCharOffset -= eGRun.AdvanceWidths[j];
					if (endCharOffset <= (z))
					{
						endCharOffset += eGRun.AdvanceWidths[j];
						endCharOffset -= this.End.EndHandle.Width;
						break;
					}
				}

				Point p = new Point(this.End.Glyphs.OriginX + endCharOffset, this._end.Glyphs.OriginY);
				double lpRight = Canvas.GetLeft(this.End.EndHandle);
				Canvas.SetLeft(this.End.EndHandle, this.End.Glyphs.OriginX + endCharOffset);
				double npRight = Canvas.GetLeft(this.End.EndHandle);
				this.End.Line.Width += (npRight - lpRight);
			}

			//TODO: Since the length is updated in the lines, we alse need to update their offsets and
			//		other variables...
			//Debug.Print("Handles where aligned (Start: {0})", startCharOffset);
		}


		public void AlignCommentVisual()
		{
			this._commentIcon.Width = 12;
			this._commentIcon.Height = 4;
			Canvas.SetLeft(this._commentIcon, this._start.BoundingBoxWithHandles.Left + 4);
			Canvas.SetTop(this._commentIcon, this._start.BoundingBoxWithHandles.Bottom + 3);
			this._commentIcon.MouseEnter += new MouseEventHandler(_commentVisual_MouseEnter);
			this._commentIcon.MouseLeave += new MouseEventHandler(_commentVisual_MouseLeave);
		}

		private void _commentVisual_MouseEnter(object sender, MouseEventArgs e)
		{
			int cCount = ((Canvas)this._commentIcon.Parent).Children.Count;
			Canvas.SetTop(this._commentIcon, this._start.BoundingBoxWithHandles.Bottom - 3);
			Canvas.SetZIndex(this._commentIcon, cCount - 1);
			this._commentIcon.Width = 24;
			this._commentIcon.Height = 24;
		}

		private void _commentVisual_MouseLeave(object sender, MouseEventArgs e)
		{
			Canvas.SetTop(this._commentIcon, this._start.BoundingBoxWithHandles.Bottom + 3);
			this._commentIcon.Width = 12;
			this._commentIcon.Height = 4;
		}

		public void DeleteComment()
		{
			this.CommentIcon = null;
			this.Comment = null;
			this.HasComment = false;
		}

		public void SetColor(SolidColorBrush colorBrush)
		{
			foreach (PenLinePart plp in this.LineParts)
			{
				plp.LineBrush = colorBrush;
				plp.Line.Fill = colorBrush;
			}
			this._originalColor = colorBrush.Color;
		}

		public Color GetColor()
		{
			if (this._originalColor.Equals(Color.FromArgb(0, 0, 0, 0)))
				this._originalColor = this.Start.LineBrush.Color;

			return this._originalColor;
			//return this.Start.LineBrush.Color;
		}

		public void AddPart(PenLinePart part)
		{
			if (this._lineParts == null)
				this._lineParts = new List<PenLinePart>();

			this._lineParts.Add(part);
		}

		private Rect GetBoundingBox()
		{
			double leftMin = double.MaxValue;
			double rightMax = double.MinValue;
			double top = double.MinValue;
			double bottom = double.MaxValue;

			foreach (PenLinePart plPart in this.LineParts)
			{
				leftMin = (plPart.BoundingBoxWithHandles.Left < leftMin) ? plPart.BoundingBoxWithHandles.Left : leftMin;
				rightMax = (plPart.BoundingBoxWithHandles.Right > rightMax) ? plPart.BoundingBoxWithHandles.Right : rightMax;
				top = (plPart.BoundingBoxWithHandles.Top > top) ? plPart.BoundingBoxWithHandles.Top : top;
				bottom = (plPart.BoundingBoxWithHandles.Bottom < bottom) ? plPart.BoundingBoxWithHandles.Bottom : bottom;
			}
			return new Rect(new Point(leftMin, top), new Point(rightMax, bottom));
		}

		public void MarkAsSelected(bool IsSelected, Brush startBrush, Brush endBrush)
		{
			this.Start.StartHandle.Fill = startBrush;
			this.End.EndHandle.Fill = endBrush;
		}



		/// <summary>
		/// Update the lines visual to show that it
		/// is being 'touched'. This could signify a mouse over event.
		/// </summary>
		/// <param name="ShouldGiveVisualFeedback">Tells wether to start or stop the feedback.</param>
		public void RunTuchedFeedback(bool ShouldGiveVisualFeedback)
		{
			if (ShouldGiveVisualFeedback)
			{
				this._originalColor = this.Start.LineBrush.Color;

				foreach (PenLinePart plp in this.LineParts)
				{
					if (this.LineType ==
							SiliconStudio.Meet.EjpControls.Enumerations.E_DocumentEntityAdornerType.PenLine)
					{
						plp.LineBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
						plp.Line.Fill = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
					}
					else if (this.LineType ==
							SiliconStudio.Meet.EjpControls.Enumerations.E_DocumentEntityAdornerType.MarkerLine)
					{
						plp.LineBrush = new SolidColorBrush(Color.FromArgb(75, 0, 0, 0));
						plp.Line.Fill = new SolidColorBrush(Color.FromArgb(75, 0, 0, 0));
					}
				}

			}
			else
			{
				//if (this.LineType ==
				//    SiliconStudio.Meet.EjpControls.Enumerations.E_DocumentEntityAdornerType.PenLine)
				this.SetColor(new SolidColorBrush(this._originalColor));
			}
		}

		internal bool HitTest(Point pointToTest, int cPageNumber)
		{
			foreach (PenLinePart lPart in this._lineParts)
			{
				if (lPart.BoundingBox.Contains(pointToTest)
					&& lPart.PageNumber == cPageNumber)
					return true;
			}
			return false;
		}

		internal PenLinePart GetMouseOverLinePart(Point pointToTest, int cPageNumber)
		{
			foreach (PenLinePart lPart in this._lineParts)
			{
				if (lPart.BoundingBoxWithHandles.Contains(pointToTest)
					&& lPart.PageNumber == cPageNumber)
					return lPart;
			}
			return null;
		}
	}

	/// <summary>
	/// Part of a DocumentLIne
	/// </summary>
	public class PenLinePart
	{
		public int PageNumber { get; set; }
		public Glyphs Glyphs { get; set; }

		public double BoxHeight { get; set; } //Height of the Box that encapsulates the line on with this Pen Line is drawn
		public double BoxTop { get; set; }
		public double BoxLeft
		{
			get
			{
				return (double)this.Line.GetValue(Canvas.LeftProperty);
			}
			set { }
		}
		public double LineLeft { get; set; }
		public double LineTop { get; set; }

		public Guid ParentPenLineId { get; set; }
		public Enumerations.PenLineGlyphsPosition Position { get; set; }
		public Rectangle Line { get; set; }

		public Rectangle StartHandle { get; set; }
		public Rectangle EndHandle { get; set; }

		public SolidColorBrush LineBrush { get; set; }
		public double OffsetStart { get; set; }
		public string ParentGlyphName
		{
			get
			{
				if (this.Glyphs != null)
					return this.Glyphs.Name;
				else
					return "";
			}
		}

		public double Width
		{
			get { return this.Line.Width; }
		}

		public double LineHeight
		{
			get { return this.Line.Height; }
		}

		public Rect BoundingBox
		{
			get
			{
				//see if we can solve this using less manual data... (originX + widht = right corner and so on...)
				double width = ((this.Width - this.StartHandle.Width) - this.EndHandle.Width);
				Rect res = new Rect(
				this.Glyphs.OriginX + this.OffsetStart + this.StartHandle.Width,
				this.BoxTop - 3,
				(width > 0) ? width : 0,
				this.BoxHeight + 6);

				//Original
				//double width = ((this.Width - this.StartHandle.Width) - this.EndHandle.Width);
				//Rect res = new Rect(
				//this.BoxLeft + this.StartHandle.Width,
				//this.BoxTop - 3,
				//(width > 0) ? width : 5,
				//this.BoxHeight + 6);

				return res;
			}
		}

		internal Rect BoundingBoxWithHandles
		{
			get
			{
				double width = this.Width;
				Rect res = new Rect(
				(this.Glyphs.OriginX + this.OffsetStart),
				this.BoxTop,
				(width > 0) ? width : 0,
				this.BoxHeight);

				//Original
				//double width = this.Width;
				//Rect res = new Rect(
				//this.BoxLeft,
				//this.BoxTop,
				//(width > 0) ? width : 5,
				//this.BoxHeight);

				return res;
			}
		}

		public PenLinePart()
		{

		}
	}

	public class DocumentLineComment
	{
		private string _content;
		public string Content
		{
			get
			{
				if (this._content != null)
					return _content;
				else
					return "";
			}
			set { _content = value; }
		}

		public string DateAdded { get; set; }
		public string Author { get; set; }
		public Guid AuthorId { get; set; }
		public Guid ParentDocumentLineId { get; set; }
		public Guid ParentDocumentId { get; set; }
		public Guid ParentStudyId { get; set; }

		public DocumentLineComment()
		{

		}
	}
}
