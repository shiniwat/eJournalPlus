//#define PUBLIC_BUILD
#define INTERNAL_BUILD

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Xps;
using System.Windows.Shapes;
using System.Windows.Threading;
using SiliconStudio.Meet.EjpControls.Enumerations;

namespace SiliconStudio.Meet.EjpControls
{
	public class MyRtb : RichTextBox
	{
		public FlowDocument DocumentAttached
		{
			get;
			set;
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			System.Diagnostics.Debug.WriteLine(string.Format("MyRtb.ArrangeOverride: size={0:F0},{1:F0}", arrangeBounds.Width, arrangeBounds.Height));
			if (IsVisible == true && DocumentAttached != null && Document.Blocks.Count <= 1)
			{
				this.Document = DocumentAttached;
			}
			return base.ArrangeOverride(arrangeBounds);
		}

		protected override Size MeasureOverride(Size constraint)
		{
			//System.Diagnostics.Debug.WriteLine("MyRtb:MeasureOverride");
			return base.MeasureOverride(constraint);
		}

		protected override void OnChildDesiredSizeChanged(UIElement child)
		{
			//System.Diagnostics.Debug.WriteLine("MyRtb:OnChildDesiredSizeChanged");
			base.OnChildDesiredSizeChanged(child);
		}
	}
	/// <summary>
	/// Interaction logic for ReportEditor.xaml
	/// </summary>
	public partial class ReportEditor : System.Windows.Controls.UserControl
	{

		//This is used to tell the wehter the control has been
		//rendered and the comments have been added.
		private bool _isCommentLoadingComplete;

        private DispatcherTimer _delayTimer;
        private int _delayLoadingTrials;

		private EjpLib.BaseClasses.ejpReport _reportObject;

		private Guid _parentStudyId;
		public Guid ParentStudyId
		{
			get { return _parentStudyId; }
			set { _parentStudyId = value; }
		}

		private bool _commentToolEnabled;
		public bool CommentToolEnabled
		{
			get { return _commentToolEnabled; }
			set { _commentToolEnabled = value; }
		}

		public string CurrentOwnerName { get; set; }
		public Guid CurrentOwnerId { get; set; }

		private List<KnowledgeMapComment> _comments;

		public List<KnowledgeMapComment> Comments
		{
			get
			{
				return this._comments;

				//List<KnowledgeMapComment> res = new List<KnowledgeMapComment>();
				//foreach (ReportCommentAdorner a in this._commentAdorners)
				//    res.Add(a.Comment);

				//return res;
			}
		}

		private bool _isEditingLocked;
		public bool IsEditingLocked
		{
			get { return _isEditingLocked; }
			set
			{
				if (value == true)
				{
					this._textArea.IsReadOnly = true;
					this._tb_ToolBox.IsEnabled = false;
					this._tb_ToolBox.Visibility = Visibility.Collapsed;
					this._mi_TextAreaContextMenu_FontListHeader.IsEnabled = false;
					this._mi_TextAreaContextMenu_FontSizeHeader.IsEnabled = false;
#if INTERNAL_BUILD
					//this._mi_TextAreaContextMenu_AddCommentHeader.IsEnabled = true;
#endif
#if PUBLIC_BUILD
                    //this._mi_TextAreaContextMenu_AddCommentHeader.IsEnabled = false;
#endif
				}
				else
				{
					this._tb_ToolBox.IsEnabled = true;
					this._textArea.IsReadOnly = false;
					this._tb_ToolBox.Visibility = Visibility.Visible;
					this._textArea.ContextMenu.IsEnabled = true;
					this._mi_TextAreaContextMenu_FontListHeader.IsEnabled = true;
					this._mi_TextAreaContextMenu_FontSizeHeader.IsEnabled = true;
					//this._mi_TextAreaContextMenu_AddCommentHeader.IsEnabled = false;

					//Remove all the comments 080329
					//this.RemoveAllComments();
				}
				_isEditingLocked = value;
			}
		}

		public ReportEditor()
		{
			InitializeComponent();
			this._comments = new List<KnowledgeMapComment>();
			this._textArea.PreviewDrop += new DragEventHandler(_textArea_PreviewDrop);
			this._textArea.PreviewDragOver += new DragEventHandler(_textArea_PreviewDragOver);
			this._textArea.TextChanged += new TextChangedEventHandler(_textArea_TextChanged);
			this._textArea.PreviewMouseRightButtonUp += new MouseButtonEventHandler(_textArea_PreviewMouseRightButtonUp);
			this._textArea.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(_textArea_SelectionChanged);
			this.CurrentOwnerName = "";
			this.CurrentOwnerId = Guid.Empty;

			foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
			{
				MenuItem fMi = new MenuItem();
				fMi.Header = fontFamily.ToString();
				fMi.Click += new RoutedEventHandler(OnSetFontFamilyForSelection);
				this._mi_TextAreaContextMenu_FontListHeader.Items.Add(fMi);
			}

			for (int i = 6; i < 50; i += 2)
			{
				MenuItem sMi = new MenuItem();
				sMi.Header = i.ToString();
				sMi.Click += new RoutedEventHandler(OnSetFontSizeForSelection);
				this._mi_TextAreaContextMenu_FontSizeHeader.Items.Add(sMi);
			}

			CommandBinding PasteBinding = new CommandBinding(ApplicationCommands.Paste, PasteExecuted, PasteCanExecute);
			this._textArea.CommandBindings.Add(PasteBinding);

			this.GotFocus += new RoutedEventHandler(ReportEditor_GotFocus);
		}

		//Deferr loading the comments until the rest of the control
		//is ready. Otherwise adorner operations will fail since the 
		//control has not yet been rendered.
		void ReportEditor_GotFocus(object sender, RoutedEventArgs e)
		{
            EnsureImportComment();
		}

        private void EnsureImportComment()
        {
            if (this._isCommentLoadingComplete == false)
            {
                if (this._reportObject.Comments != null)
                {
                    foreach (EjpLib.BaseClasses.ejpCAComment comment in this._reportObject.Comments)
                    {
                        if (!this.ImportComment(comment))
                        {
                            // don't mark "isImportComplete" in this case.
                            return;
                        }
                    }
                }
                this._isCommentLoadingComplete = true;
            }
        }

        public void DelayImportComment()
        {
            _delayTimer = new DispatcherTimer();
            _delayTimer.Interval = TimeSpan.FromMilliseconds(500);
            _delayTimer.Tick += new EventHandler(DelayImportComment_Tick);
            _delayLoadingTrials = 0;
            _delayTimer.Start();
        }

        private void DelayImportComment_Tick(object sender, EventArgs e)
        {
            EnsureImportComment();
            const int _maxTrials = 4;
            if (_isCommentLoadingComplete || ++_delayLoadingTrials > _maxTrials)
            {
                _delayTimer.Stop();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("comment were not loaded");
            }
        }

		private void PasteExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			this._textArea.Paste();
		}

		private void PasteCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			if (Clipboard.ContainsImage())
			{
				e.CanExecute = false;
				e.Handled = true;
			}
		}

		private void _textArea_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			//Do not show the context menu for textBlocks.
			if (e.OriginalSource is TextBlock)
				e.Handled = true;
			else
			{
				//also, no menu when in commenting mode and no selection is made...
				if (this.IsEditingLocked && this._textArea.Selection.Text == "")
					e.Handled = true;
			}
		}

		private void OnSetFontFamilyForSelection(object sender, RoutedEventArgs e)
		{
			this.SetFontFamilyForSelection(new FontFamily(((MenuItem)sender).Header as string));
		}

		private void OnSetFontSizeForSelection(object sender, RoutedEventArgs e)
		{
			this.SetFontSizeForSelection(Int32.Parse(((MenuItem)sender).Header as string));
		}

		private void _textArea_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.UpdateLetterCount();
		}

		private void SetFontSizeForSelection(double size)
		{
			try
			{
				if (!this._textArea.Selection.IsEmpty)
				{
					TextRange tr = new TextRange(
						this._textArea.Selection.Start,
						this._textArea.Selection.End);

					tr.ApplyPropertyValue(TextElement.FontSizeProperty, size);
				}
				else
				{
					Run r = new Run(" ", this._textArea.CaretPosition);
					r.FontSize = size;

					this._textArea.CaretPosition = r.ContentStart;
				}

				this._textArea.Focus();
			}
			catch (Exception)
			{
			}
		}

		private void SetFontFamilyForSelection(FontFamily font)
		{
			try
			{
				if (!this._textArea.Selection.IsEmpty)
				{
					TextRange tr = new TextRange(
						this._textArea.Selection.Start,
						this._textArea.Selection.End);

					tr.ApplyPropertyValue(TextElement.FontFamilyProperty, font);
				}
				else
				{
					Run r = new Run(" ", this._textArea.CaretPosition);
					r.FontFamily = font;

					this._textArea.CaretPosition = r.ContentStart;
				}

				this._textArea.Focus();
			}
			catch (Exception)
			{
			}
		}

		private void UpdateLetterCount()
		{
			int total = 0;
			int copied = 0;
			foreach (Block bl in this._textArea.Document.Blocks)
			{
				Paragraph p = bl as Paragraph;
				if (p != null)
				{
					foreach (Inline inL in p.Inlines)
					{
						if (inL is Run)
							total += ((Run)inL).Text.Length;
						else if (inL is InlineUIContainer)
						{
							ReportQuoteBoxEX rqb = ((InlineUIContainer)inL).Child as ReportQuoteBoxEX;
							if (rqb != null)
							{
								total += (rqb.QuoteContent.Length + rqb.QuoteTitle.Length);
								copied += rqb.QuoteContent.Length;
							}
						}
					}
				}
			}

			if (total == 0)
				this._l_LinkPercentage.Content = "0%";
			else
			{
				double p = (double)copied / (double)total;
				int p2 = (int)(p * 100);
				if (p2 == 0)
					this._l_LinkPercentage.Content = ">1%";
				else
					this._l_LinkPercentage.Content = p2.ToString() + "%";
			}

			this._l_MojiCount.Content = total.ToString() + Application.Current.Resources["Str_UiLbl_CharacterLiteral"] as string;

		}

		private void _textArea_PreviewDragOver(object sender, DragEventArgs e)
		{
			foreach (string format in e.Data.GetFormats())
			{
				if (format.Contains("KnowledgeMapImageEntity"))
				{
					e.Effects = DragDropEffects.None;
					e.Handled = true;
					return;
				}
			}

			this._textArea.Focus();
			Point point = e.GetPosition(this._textArea);
			TextPointer position = this._textArea.GetPositionFromPoint(point, true);
			this._textArea.CaretPosition = position;
			e.Handled = true;
		}

		public void SetActiveTextColor(SolidColorBrush newColor)
		{
			if (!this._textArea.Selection.IsEmpty)
			{
				TextRange tr = new TextRange(
					this._textArea.Selection.Start,
					this._textArea.Selection.End);

				tr.ApplyPropertyValue(TextElement.ForegroundProperty, newColor);
			}
			else
			{
				Run r = new Run("", this._textArea.CaretPosition);
				r.Foreground = newColor;

				this._textArea.CaretPosition = r.ContentStart;
				this._textArea.Focus();
			}

			this._textArea.Focus();
		}

		public void ImportReport(EjpLib.BaseClasses.ejpReport report, Guid parentStudyId)
		{
			try
			{
				this._reportObject = report;
				this._parentStudyId = parentStudyId;
				//  [shiniwa] uncomment out here to get repro...
				//this._textArea.Document = report.Document;
				this._textArea.Measure(Size.Empty);
				this._textArea.UpdateLayout();
				this.UpdateLetterCount();
				//  Let's buffer the FlowDocument in RTB... it'll be automatically attached when it is absolutely needed.
				this._textArea.DocumentAttached = report.Document;
			}
			catch (Exception)
			{
				throw new ApplicationException(Application.Current.Resources["EX_ReportLoadFailed_Version"] as string);
			}
		}

		/// <summary>
		/// Update the list of Comments in the locally held reportObject
		/// </summary>
		public void ExportReportComments()
		{
			if (this._isCommentLoadingComplete == false)
				return;

			this._reportObject.Comments =
				new List<SiliconStudio.Meet.EjpLib.BaseClasses.ejpCAComment>();

			foreach (KnowledgeMapComment comment in this._comments)
			{
				EjpLib.BaseClasses.ejpCAComment com =
					new EjpLib.BaseClasses.ejpCAComment()
					{
						AuthorId = comment.OriginalAuthorId,
						AuthorName = comment.OriginalAuthorName,
						CommentId = comment.CommentId,
						OriginalPositionX = comment.OriginalCoordinates.X,
						OriginalPositionY = comment.OriginalCoordinates.Y,
						PositionX = 0,
						PositionY = 0,
						CommentedTextInDocument = comment.CommentTextInDocument
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

				this._reportObject.Comments.Add(com);
			}
		}

		public void LoadTextColors(List<SolidColorBrush> colors)
		{
			//this._csb_ColorSwatchButton.Items = colors;
		}

		/// <summary>
		/// Handles the actual drop.
		/// </summary>
		private void _textArea_PreviewDrop(object sender, DragEventArgs e)
		{
			try
			{
				if (this.IsEditingLocked)
				{
					MessageBox.Show(Application.Current.Resources["ERR_ReportEditFailed_Locked"] as string,
						Application.Current.Resources["Str_MsgTitle_ReportLocked"] as string, MessageBoxButton.OK, MessageBoxImage.Stop);
					return;
				}

				bool isKMEntity = false;
				foreach (string format in e.Data.GetFormats())
				{
					if (format.Contains("KnowledgeMapTextEntity"))
						isKMEntity = true;
				}

				if (isKMEntity)
				{
					KnowledgeMapTextEntity t = (KnowledgeMapTextEntity)e.Data.GetData(typeof(KnowledgeMapTextEntity));
					string id = t.Id.ToString();
					string body = t.Body;
					string title = "";
					SolidColorBrush titleFillBrush = Brushes.White;

					//090220
					if (t.EntityType == KnowledgeMapEntityType.ConnectedToDocument)
					{
						title = t.SourceReference.DocumentTitle + " (p." + t.SourceReference.PageNumber.ToString() + ")";
						titleFillBrush = new SolidColorBrush(
							Color.FromArgb(255, t.Color.Color.R, t.Color.Color.G, t.Color.Color.B));
					}
					else
					{
						title = Application.Current.Resources["Str_UiLbl_ReportReferencedSelfNodeTitle"] as string + " " + t.Title;
						titleFillBrush = Brushes.DarkGray;
					}

					using (this._textArea.DeclareChangeBlock())
					{
						Point point = e.GetPosition(this._textArea);
						TextPointer position = this._textArea.GetPositionFromPoint(point, true);
						//080731
						TextPointer insP = position.InsertParagraphBreak();

						//080805
						ReportQuoteBoxEX rqb = new ReportQuoteBoxEX();
						rqb.Fill = titleFillBrush;
						rqb.QuoteContent = body;
						rqb.QuoteTitle = title;
						rqb.PreviewMouseDown += new MouseButtonEventHandler(tb_PreviewMouseDown);
						rqb.Margin = new Thickness(25, 16, 25, 16);
						InlineUIContainer container = new InlineUIContainer(rqb, position);

						//080731
						insP.InsertParagraphBreak();
					}
					e.Handled = true;
				}
			}
			catch (Exception)
			{
				//TODO: Fix message...
				MessageBox.Show(Application.Current.Resources["EX_ReportReferenceAddFailed"] as string, Application.Current.Resources["Str_ErrorDlgTitle"] as string, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		void tb_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
		}

		#region Hyperlinks

		private void onFormat_Hyperlink(object sender, RoutedEventArgs e)
		{
			TextPointer tp1 = this._textArea.Selection.Start;
			TextPointer tp2 = this._textArea.Selection.End;
			TextRange tr = new TextRange(tp1, tp2);

			//Run r = tr.Start.Parent as Run;
			//if (r.NextInline is Hyperlink)
			//{
			//    //MessageBox.Show("This is a link");
			//    TextPointer hrefStart = r.NextInline.ContentStart;
			//    TextPointer hrefEnd = r.NextInline.ContentEnd;
			//    TextRange hrefTr = new TextRange(hrefStart, hrefEnd);
			//    FontFamily ffamily = (FontFamily)hrefTr.GetPropertyValue(TextElement.FontFamilyProperty);
			//    double fsize = (double)hrefTr.GetPropertyValue(TextElement.FontSizeProperty);
			//    Run deFormated = new Run(hrefTr.Text, hrefTr.Start);

			//    return;
			//}

			//First we need to check if the user has selected a previously added Hyperlink.


			Windows.ReportUrlInputWindow uriIw = new Windows.ReportUrlInputWindow(tr.Text, "");
			uriIw.Closing += delegate(object s, System.ComponentModel.CancelEventArgs e2)
				{
					if (uriIw.Cancelled == false)
					{
						if (tr.Text.Length != 0)
						{
							try
							{
								tr.Text = uriIw.Explanation;
								Hyperlink h = new Hyperlink(tp1, tp2);
								h.ToolTip = new Label() { Content = Application.Current.Resources["Str_ToolTip_ReportUrl"] as string };
								h.NavigateUri = new Uri(uriIw.Url);
								h.Foreground = Brushes.Blue;
								h.RequestNavigate += new RequestNavigateEventHandler(h_Click);

							}
							catch (Exception)
							{
								MessageBox.Show(Application.Current.Resources["EX_ReportAddUrlFailed"] as string,
									Application.Current.Resources["Str_ErrorDlgTitle"] as string, MessageBoxButton.OK, MessageBoxImage.Error);
							}
						}
						else
						{
							try
							{
								Hyperlink h = new Hyperlink(new Run(uriIw.Explanation), this._textArea.CaretPosition);
								h.NavigateUri = new Uri(uriIw.Url);
								h.Foreground = Brushes.Blue;
								h.RequestNavigate += new RequestNavigateEventHandler(h_Click);
							}
							catch (Exception)
							{
								MessageBox.Show(Application.Current.Resources["EX_ReportAddUrlFailed"] as string, Application.Current.Resources["Str_ErrorDlgTitle"] as string,
									MessageBoxButton.OK, MessageBoxImage.Error);
							}
						}
						tr.Select(tp1, tp2);
					}
				};

			uriIw.ShowDialog();
		}

		private void h_Click(object sender, RoutedEventArgs e)
		{
			Hyperlink h = sender as Hyperlink;
			string navigateUri = h.NavigateUri.ToString();
			Process.Start(new ProcessStartInfo(navigateUri));
			e.Handled = true;
		}

		#endregion

		#region Formatting

		private void OnSetBold(object sender, RoutedEventArgs e)
		{
			//this means that someone is trying to update
			//the ui without firing the asc. event.
			if (sender == null && e == null)
				return;

			EditingCommands.ToggleBold.Execute(null, this._textArea);
			this._b_Edit_ResetFocus();
		}

		private void OnSetItalic(object sender, RoutedEventArgs e)
		{
			//this means that someone is trying to update
			//the ui without firing the asc. event.
			if (sender == null && e == null)
				return;

			EditingCommands.ToggleItalic.Execute(null, this._textArea);
			this._b_Edit_ResetFocus();
		}

		private void OnSetUnderline(object sender, RoutedEventArgs e)
		{
			//this means that someone is trying to update
			//the ui without firing the asc. event.
			if (sender == null && e == null)
				return;

			EditingCommands.ToggleUnderline.Execute(null, this._textArea);
			this._b_Edit_ResetFocus();
		}

		private void OnAlignLeft(object sender, RoutedEventArgs e)
		{
			//this means that someone is trying to update
			//the ui without firing the asc. event.
			if (sender == null && e == null)
				return;

			EditingCommands.AlignLeft.Execute(null, this._textArea);
			this._b_Edit_ResetFocus();
		}

		private void OnAlignCenter(object sender, RoutedEventArgs e)
		{
			//this means that someone is trying to update
			//the ui without firing the asc. event.
			if (sender == null && e == null)
				return;

			EditingCommands.AlignCenter.Execute(null, this._textArea);
			this._b_Edit_ResetFocus();
		}

		#endregion

		private void OnRunBulletList(object sender, RoutedEventArgs e)
		{
			//this means that someone is trying to update
			//the ui without firing the asc. event.
			if (sender == null && e == null)
				return;

			EditingCommands.ToggleBullets.Execute(null, this._textArea);
			this._b_Edit_ResetFocus();
		}

		private void _b_Edit_ResetFocus()
		{
			this._textArea.Focus();
		}

		#region Exporting and Printing

		public void ExportReportToRtf(string path)
		{
			try
			{
				FlowDocument new_fd = this.CloneCurrentReportDocument(true);

				TextRange range = new TextRange(new_fd.ContentStart, new_fd.ContentEnd);
				Stream rtfStream = new FileStream(path, FileMode.Create);
				range.Save(rtfStream, DataFormats.Rtf);
				rtfStream.Close();
			}
			catch (Exception)
			{
				MessageBox.Show(Application.Current.Resources["EX_ReportExportFailed"] as string, Application.Current.Resources["Str_ErrorDlgTitle"] as string,
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public string ExportReportToHTMLString()
		{
			try
			{
				FlowDocument new_fd = this.CloneCurrentReportDocument(true);
				string htmlRep = Helpers.FlowDocToHTMLConverter.ConvertXamlToHtml(XamlWriter.Save(new_fd));

				return htmlRep;
			}
			catch (Exception)
			{
				MessageBox.Show(Application.Current.Resources["EX_ReportExportFailed"] as string, Application.Current.Resources["Str_ErrorDlgTitle"] as string,
					MessageBoxButton.OK, MessageBoxImage.Error);
				return "";
			}
		}

		public void PrintReport()
		{
			try
			{
				FlowDocument fd = this.CloneCurrentReportDocument(false);


				PrintDocumentImageableArea pd_ia = null;
				XpsDocumentWriter docWriter = PrintQueue.CreateXpsDocumentWriter(ref pd_ia);
				if (docWriter != null && pd_ia != null)
				{
					DocumentPaginator d_paginator = ((IDocumentPaginatorSource)fd).DocumentPaginator;
					d_paginator.PageSize = new Size(pd_ia.MediaSizeWidth, pd_ia.MediaSizeHeight);
					Thickness pagePadding = fd.PagePadding;
					fd.PagePadding = new Thickness(
							Math.Max(pd_ia.OriginWidth, (pagePadding.Left + 29)),
							Math.Max(pd_ia.OriginHeight, (pagePadding.Top + 29)),
							Math.Max(pd_ia.MediaSizeWidth - (pd_ia.OriginWidth + pd_ia.ExtentWidth), (pagePadding.Right + 29)),
							Math.Max(pd_ia.MediaSizeHeight - (pd_ia.OriginHeight + pd_ia.ExtentHeight), (pagePadding.Bottom + 29)));
					fd.ColumnWidth = double.PositiveInfinity;
					docWriter.Write(d_paginator);
				}
			}
			catch (Exception)
			{
				MessageBox.Show(Application.Current.Resources["EX_ReportPrintFailed"] as string, Application.Current.Resources["Str_ErrorDlgTitle"] as string,
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private class InsertBlock
		{
			public Block insertPoint;
			public Block blockToInsert;
		}

		private FlowDocument CloneCurrentReportDocument(bool convertInlineUiElements)
		{
			MemoryStream ms = new MemoryStream();
			XamlWriter.Save(this._textArea.DocumentAttached, ms);
			ms.Position = 0;
			FlowDocument new_fd = (FlowDocument)XamlReader.Load(ms);
			ms.Close();
			ms.Dispose();

			List<InsertBlock> blocksToInsert = new List<InsertBlock>();
			Block prev_Block = new_fd.Blocks.FirstBlock;

			foreach (Block oriBlock in new_fd.Blocks)
			{
				Paragraph p = oriBlock as Paragraph;
				if (p != null)
				{
					Paragraph newParagraph = new Paragraph();
					bool AddP = false;

					foreach (Inline inL in p.Inlines)
					{
						if (inL is InlineUIContainer)
						{
							ReportQuoteBoxEX tb = ((InlineUIContainer)inL).Child as ReportQuoteBoxEX;
							if (tb != null)
							{
								Run r = new Run(tb.QuoteTitle + "\n" + tb.QuoteContent);
								r.Foreground = tb.Fill;
								newParagraph.Inlines.Add(r);
								AddP = true;
							}
						}
					}

					if (AddP)
						blocksToInsert.Add(new InsertBlock() { blockToInsert = newParagraph, insertPoint = prev_Block });
				}
				prev_Block = oriBlock;
			}

			if (convertInlineUiElements)
			{
				foreach (InsertBlock bI in blocksToInsert)
					new_fd.Blocks.InsertAfter(bI.insertPoint, bI.blockToInsert);
			}

			return new_fd;
		}

		#endregion

		#region CA Comments

		/// <summary>
		/// This is used only in CA mode at the moment. 
		/// Whenever the user selects a range of text in the Text Area,
		/// we try to add a comment to that selection.
		/// </summary>
		private void _textArea_SelectionChanged(object sender, RoutedEventArgs e)
		{
			if (this._commentToolEnabled)
			{
				if (e.OriginalSource is RichTextBox)
				{
					if (this.IsEditingLocked && this._textArea.Selection.IsEmpty == false)
					{
						this.AddCommentToSelection();
					}
				}
			}
		}

		public void SetFocusToInputArea()
		{
			this._textArea.Focus();
		}

		private bool ImportComment(EjpLib.BaseClasses.ejpCAComment comment)
		{
			TextPointer commentStart = this._textArea.Document.ContentStart.GetPositionAtOffset(Math.Abs((int)comment.OriginalPositionX));
            TextPointer commentEnd = commentStart.GetPositionAtOffset(Math.Abs((int)comment.OriginalPositionY));
            if (commentStart == null || commentEnd == null)
            {
                return false;
            }
            Point commmentInsertPoint = commentStart.GetCharacterRect(LogicalDirection.Forward).TopLeft;

			KnowledgeMapComment commentToAdd = new KnowledgeMapComment()
			{
				OriginalAuthorId = comment.AuthorId,
				OriginalAuthorName = comment.AuthorName,
				CurrentAuthorId = this.CurrentOwnerId,
				CurrentAuthorName = this.CurrentOwnerName,
				CommentId = comment.CommentId,
				Width = 200,
				Height = 300,
				CommentTextInDocument = comment.CommentedTextInDocument,
				OriginalCoordinates = new Point(comment.OriginalPositionX, comment.OriginalPositionY),
				CommentedTextStart = commentStart
			};

			commentToAdd.DisableReturnToOriginalPosition();

			foreach (EjpLib.BaseClasses.ejpCACommentMessage message in comment.Messages)
			{
				commentToAdd.Messages.Add(
					new CommentMessage()
					{
						Author = message.Author,
						AuthorId = message.AuthorId,
						Date = message.Date,
						Message = message.Message
					}
					);
			}

			this._c_FakeAdornerLayer.Children.Add(commentToAdd);

			Point p =
					commentToAdd.CommentedTextStart.GetCharacterRect(
					LogicalDirection.Forward).TopLeft;

			if (((p.Y - 10) + 300) < this._c_FakeAdornerLayer.ActualHeight)
				Canvas.SetTop(commentToAdd, p.Y - 10);
			else
				Canvas.SetTop(commentToAdd, (p.Y - 10) - 300);

			if ((p.X + 200) < this._c_FakeAdornerLayer.ActualWidth)
				Canvas.SetLeft(commentToAdd, p.X);
			else
				Canvas.SetLeft(commentToAdd, p.X - 178);

			commentToAdd.OnDeleteComment += new DeleteKnowledgeMapComment(comment_OnDeleteComment);
			commentToAdd.OnMinimizeComment += new KnowledgeMapCommentViewStateChanged(comment_OnMinimizeComment);
			commentToAdd.OnMaximizeComment += new KnowledgeMapCommentViewStateChanged(comment_OnMaximizeComment);

			//this._comments.Add(commentToAdd);


			//090126
			Rect tStartRect = commentStart.GetCharacterRect(LogicalDirection.Forward);
			Rect tEndRect = commentEnd.GetCharacterRect(LogicalDirection.Forward);

			DrawingBrush lineHandleSBrush = this.Resources["commentLineHandleS_default"] as DrawingBrush;
			DrawingBrush lineHandleEBrush = this.Resources["commentLineHandleE_default"] as DrawingBrush;
			commentToAdd.StartHandleRectangle = new Rectangle()
			{
				Fill = lineHandleSBrush
			};
			commentToAdd.StartHandleRectangle.Width = 9;
			commentToAdd.StartHandleRectangle.Height = (tStartRect.Height < 5) ? 10 : tStartRect.Height; ;
			commentToAdd.EndHandleRectangle = new Rectangle()
			{
				Fill = lineHandleEBrush
			};
			commentToAdd.EndHandleRectangle.Width = 9;
			commentToAdd.EndHandleRectangle.Height = (tEndRect.Height < 5) ? 10 : tEndRect.Height;
			commentToAdd.StartHandleRectangle.SetValue(Canvas.TopProperty, tStartRect.TopLeft.Y);
			commentToAdd.StartHandleRectangle.SetValue(Canvas.LeftProperty, tStartRect.TopLeft.X);
			commentToAdd.EndHandleRectangle.SetValue(Canvas.TopProperty, tEndRect.TopLeft.Y);
			commentToAdd.EndHandleRectangle.SetValue(Canvas.LeftProperty, tEndRect.TopLeft.X);

			this._c_FakeAdornerLayer.Children.Insert(0, commentToAdd.StartHandleRectangle);
			this._c_FakeAdornerLayer.Children.Insert(0, commentToAdd.EndHandleRectangle);

			this._comments.Add(commentToAdd);
            return true;
		}



		/// <summary>
		/// Insert a Comment (Commented Assignment Only) for the 
		/// current selection in the TextArea.
		/// </summary>
		private void OnAddCommentToSelection(object sender, RoutedEventArgs e)
		{
			this.AddCommentToSelection();
		}

		private void AddCommentToSelection()
		{
			try
			{
				if (this._textArea.Selection.IsEmpty)
					return;

				//New comment system 081119
				Rect tStartRect =
						this._textArea.Selection.Start.GetCharacterRect(LogicalDirection.Forward);
				Rect tEndRect =
						this._textArea.Selection.End.GetCharacterRect(LogicalDirection.Forward);

				TextPointer tp1 = this._textArea.Selection.Start;
				TextPointer tp2 = this._textArea.Selection.End;
				TextRange range = new TextRange(tp1, tp2);

                int cStartOffset = tp1.GetOffsetToPosition(this._textArea.Document.ContentStart);
                int cEndOffset = tp2.GetOffsetToPosition(tp1);

				KnowledgeMapComment comment = new KnowledgeMapComment()
				{
					OriginalAuthorId = this.CurrentOwnerId,
					OriginalAuthorName = this.CurrentOwnerName,
					CurrentAuthorId = this.CurrentOwnerId,
					CurrentAuthorName = this.CurrentOwnerName,
					CommentId = Guid.NewGuid(),
					Width = 200,
					Height = 300,
					CommentTextInDocument = range.Text,
					OriginalCoordinates = new Point(cStartOffset, cEndOffset),
					CommentedTextStart = tp1
				};

				comment.DisableReturnToOriginalPosition();

				this._c_FakeAdornerLayer.Children.Add(comment);

				if (((tStartRect.TopLeft.Y - 10) + 300) < this._c_FakeAdornerLayer.ActualHeight)
					Canvas.SetTop(comment, tStartRect.TopLeft.Y - 10);
				else
					Canvas.SetTop(comment, (tStartRect.TopLeft.Y - 10) - 300);

				if ((tStartRect.TopLeft.X + 200) < this._c_FakeAdornerLayer.ActualWidth)
					Canvas.SetLeft(comment, tStartRect.TopLeft.X);
				else
					Canvas.SetLeft(comment, tStartRect.TopLeft.X - 178);

				comment.OnDeleteComment += new
					DeleteKnowledgeMapComment(comment_OnDeleteComment);
				comment.OnMinimizeComment += new
					KnowledgeMapCommentViewStateChanged(comment_OnMinimizeComment);
				comment.OnMaximizeComment += new
					KnowledgeMapCommentViewStateChanged(comment_OnMaximizeComment);
				this._comments.Add(comment);

				//090126
				DrawingBrush lineHandleSBrush =
					this.Resources["commentLineHandleS_default"] as DrawingBrush;
				DrawingBrush lineHandleEBrush =
					this.Resources["commentLineHandleE_default"] as DrawingBrush;
				comment.StartHandleRectangle = new Rectangle()
				{
					Fill
						= lineHandleSBrush
				};
				comment.StartHandleRectangle.Width = 9;
				comment.StartHandleRectangle.Height = (tStartRect.Height < 5) ? 10 : tStartRect.Height; ;
				comment.EndHandleRectangle = new Rectangle()
				{
					Fill =
						lineHandleEBrush
				};
				comment.EndHandleRectangle.Width = 9;
				comment.EndHandleRectangle.Height = (tEndRect.Height < 5) ? 10 : tEndRect.Height;

				comment.StartHandleRectangle.SetValue(Canvas.TopProperty,
				tStartRect.TopLeft.Y);

				comment.StartHandleRectangle.SetValue(Canvas.LeftProperty,
				tStartRect.TopLeft.X);

				comment.EndHandleRectangle.SetValue(Canvas.TopProperty,
				tEndRect.TopLeft.Y);

				comment.EndHandleRectangle.SetValue(Canvas.LeftProperty,
				tEndRect.TopLeft.X);


				this._c_FakeAdornerLayer.Children.Insert(0, comment.StartHandleRectangle);
				this._c_FakeAdornerLayer.Children.Insert(0, comment.EndHandleRectangle);

				return;
			}
			catch (Exception)
			{
				throw;
			}
		}

		private void comment_OnMaximizeComment(KnowledgeMapComment comment)
		{
			double newLeft = Canvas.GetLeft(comment);
			if ((newLeft + 178 + 200) < this._c_FakeAdornerLayer.ActualWidth)
				Canvas.SetLeft(comment, newLeft + 178);

			double newTop = Canvas.GetTop(comment);
			if ((newTop + 300) > this._c_FakeAdornerLayer.ActualHeight)
				Canvas.SetTop(comment, newTop - 300);

			this._c_FakeAdornerLayer.InvalidateVisual();
		}

		private void comment_OnMinimizeComment(KnowledgeMapComment comment)
		{
			Canvas.SetLeft(comment, comment.CommentedTextStart.GetCharacterRect(LogicalDirection.Forward).TopLeft.X);
			Canvas.SetTop(comment, comment.CommentedTextStart.GetCharacterRect(LogicalDirection.Forward).TopLeft.Y - 10);
			this._c_FakeAdornerLayer.InvalidateVisual();
		}

		private void comment_OnDeleteComment(KnowledgeMapComment comment)
		{
			this._c_FakeAdornerLayer.Children.Remove(comment.StartHandleRectangle);
			this._c_FakeAdornerLayer.Children.Remove(comment.EndHandleRectangle);
			this._c_FakeAdornerLayer.Children.Remove(comment);
			this._comments.Remove(comment);
		}

		private void RemoveAllComments()
		{
			this._c_FakeAdornerLayer.Children.Clear();
			this._comments.Clear();
		}

		#endregion

		private void _textArea_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			foreach (KnowledgeMapComment comment in this._comments)
			{
				Canvas.SetTop(comment, Canvas.GetTop(comment) - e.VerticalChange);
				Canvas.SetTop(comment.StartHandleRectangle, Canvas.GetTop(comment.StartHandleRectangle) - e.VerticalChange);
				Canvas.SetTop(comment.EndHandleRectangle, Canvas.GetTop(comment.EndHandleRectangle) - e.VerticalChange);
			}
		}

		/// <summary>
		/// FIX ME...
		/// </summary>
		private void _textArea_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			foreach (KnowledgeMapComment comment in this._comments)
			{
				double newY = comment.CommentedTextStart.GetCharacterRect(LogicalDirection.Forward).TopLeft.Y;
				double newX = comment.CommentedTextStart.GetCharacterRect(LogicalDirection.Forward).TopLeft.X;

				if (comment.CurrentViewState == KnowledgeMapCommentViewState.Maximized)
				{
					if (newY < this._c_FakeAdornerLayer.ActualHeight && newY > 0) // inview
					{
						if ((newY + 300) > this._c_FakeAdornerLayer.ActualHeight)
							Canvas.SetTop(comment, newY - 300);
						else
							Canvas.SetTop(comment, newY - 10);
					}
					else
						Canvas.SetTop(comment, newY - 10);

					if ((newX + 200) > this._c_FakeAdornerLayer.ActualWidth)
						Canvas.SetLeft(comment, newX - 200);
					else
						Canvas.SetLeft(comment, newX);
				}
				else
				{
					Canvas.SetTop(comment,
						comment.CommentedTextStart.GetCharacterRect(LogicalDirection.Forward).TopLeft.Y - 10);
					Canvas.SetLeft(comment,
						comment.CommentedTextStart.GetCharacterRect(LogicalDirection.Forward).TopLeft.X);
				}

				Canvas.SetTop(comment.StartHandleRectangle,
						comment.CommentedTextStart.GetCharacterRect(LogicalDirection.Forward).TopLeft.Y);
				Canvas.SetLeft(comment.StartHandleRectangle,
					comment.CommentedTextStart.GetCharacterRect(LogicalDirection.Forward).TopLeft.X);

				double newEX = comment.CommentedTextStart.GetPositionAtOffset(comment.CommentTextInDocument.Length).GetCharacterRect(LogicalDirection.Forward).TopLeft.X;
				double newEY = comment.CommentedTextStart.GetPositionAtOffset(comment.CommentTextInDocument.Length).GetCharacterRect(LogicalDirection.Forward).TopLeft.Y;

				Canvas.SetTop(comment.EndHandleRectangle, newEY);
				Canvas.SetLeft(comment.EndHandleRectangle, newEX);
			}
		}
	}
}