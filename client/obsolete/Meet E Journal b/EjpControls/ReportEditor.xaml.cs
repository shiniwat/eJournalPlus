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

namespace SiliconStudio.Meet.EjpControls
{
	/// <summary>
	/// Interaction logic for ReportEditor.xaml
	/// </summary>
	public partial class ReportEditor : System.Windows.Controls.UserControl
	{

        //This is used to tell the wehter the control has been
        //rendered and the comments have been added.
        private bool _isCommentLoadingComplete;

        private EjpLib.BaseClasses.ejpReport _reportObject;

        private Guid _parentStudyId;
        public Guid ParentStudyId
        {
            get { return _parentStudyId; }
            set { _parentStudyId = value; }
        }

        public string CurrentOwnerName { get; set; }
        public Guid CurrentOwnerId { get; set; }

        private List<ReportCommentAdorner> _commentAdorners;

        public List<KnowledgeMapComment> Comments
        {
            get
            {
                List<KnowledgeMapComment> res = new List<KnowledgeMapComment>();
                foreach (ReportCommentAdorner a in this._commentAdorners)
                    res.Add(a.Comment);
                
                return res;
            }
        }

		private bool _isEditingLocked;
		public bool IsEditingLocked
		{
			get { return _isEditingLocked; }
			set {
                if (value == true)
                {
                    this._textArea.IsReadOnly = true;
                    this._tb_ToolBox.IsEnabled = false;
                    this._tb_ToolBox.Visibility = Visibility.Collapsed;
                    this._mi_TextAreaContextMenu_FontListHeader.IsEnabled = false;
                    this._mi_TextAreaContextMenu_FontSizeHeader.IsEnabled = false;
#if INTERNAL_BUILD
                    this._mi_TextAreaContextMenu_AddCommentHeader.IsEnabled = true;
#endif
#if PUBLIC_BUILD
                    this._mi_TextAreaContextMenu_AddCommentHeader.IsEnabled = false;
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
                    this._mi_TextAreaContextMenu_AddCommentHeader.IsEnabled = false;
                    
                    //Remove all the comments 080329
                    this.RemoveAllComments();
                }
                _isEditingLocked = value; 
            }
		}

		public ReportEditor()
		{
			InitializeComponent();
            this._commentAdorners = new List<ReportCommentAdorner>();
			this._textArea.PreviewDrop += new DragEventHandler(_textArea_PreviewDrop);
            this._textArea.PreviewDragOver += new DragEventHandler(_textArea_PreviewDragOver);
            this._textArea.TextChanged += new TextChangedEventHandler(_textArea_TextChanged);
            this._textArea.PreviewMouseRightButtonUp += new MouseButtonEventHandler(_textArea_PreviewMouseRightButtonUp);
            this.CurrentOwnerName = "";
            this.CurrentOwnerId = Guid.Empty;

            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies) 
            {
                MenuItem fMi = new MenuItem();
                fMi.Header = fontFamily.ToString();
                fMi.Click += new RoutedEventHandler(OnSetFontFamilyForSelection);
                this._mi_TextAreaContextMenu_FontListHeader.Items.Add(fMi);
            }

            for (int i = 6; i < 50; i+=2)
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
            if (this._isCommentLoadingComplete == false)
            {
                this._commentAdorners = new List<ReportCommentAdorner>();
                if (this._reportObject.Comments != null)
                {
                    foreach (EjpLib.BaseClasses.ejpCAComment comment in this._reportObject.Comments)
                    {
                        this.ImportComment(comment);
                    }
                }
                this._isCommentLoadingComplete = true;
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
                if (this.IsEditingLocked && this._textArea.Selection.Text =="")
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
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						   SiliconStudio.DebugManagers.MessageType.Error,
						   "EjpControls - Report Editor",
						   "Failed to set Font Size for selection" +
						   "\nParent Study ID: " + this.ParentStudyId.ToString() +
						   "\nReport ID: " + this._reportObject.Id.ToString() +
						   "\nError: " + ex.Message);
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
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						   SiliconStudio.DebugManagers.MessageType.Error,
						   "EjpControls - Report Editor",
						   "Failed to set Font for selection" +
						   "\nParent Study ID: " + this.ParentStudyId.ToString() +
						   "\nReport ID: " + this._reportObject.Id.ToString() +
						   "\nError: " + ex.Message);
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
                            total += rqb.QuoteContent.Length;
                            copied += rqb.QuoteContent.Length;
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
                if(p2 == 0)
                    this._l_LinkPercentage.Content = ">1%";
                else
                    this._l_LinkPercentage.Content = p2.ToString() +"%";
            }

            this._l_MojiCount.Content = total.ToString() + "文字";
            
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
                this._textArea.Document = report.Document;
                this._parentStudyId = parentStudyId;
                this._textArea.Measure(Size.Empty);
                this._textArea.UpdateLayout();

                //this._commentAdorners = new List<ReportCommentAdorner>();
                //if (report.Comments != null)
                //{
                //    foreach (EjpLib.BaseClasses.ejpCAComment comment in report.Comments)
                //    {
                //        this.ImportComment(comment);
                //    }
                //}

                this.UpdateLetterCount();
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
						   SiliconStudio.DebugManagers.MessageType.Error,
						   "EjpControls - Report Editor",
						   "Failed to Import Report" +
						   "\nReport ID: " + this._reportObject.Id.ToString() +
						   "\nError: " + ex.Message);

                throw new ApplicationException(
                    "Failed to load a Report in the current Assignment.\n" +
                    "Perhaps the Assignment was created with an" +
                    "earlier version of eJournalPlus?");
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

            foreach (ReportCommentAdorner a in this._commentAdorners)
            {

                EjpLib.BaseClasses.ejpCAComment com =
                    new EjpLib.BaseClasses.ejpCAComment()
                    {
                        AuthorId = a.Comment.OriginalAuthorId,
                        AuthorName = a.Comment.OriginalAuthorName,
                        CommentId = a.Comment.CommentId,
                        OriginalPositionX = a.Comment.OriginalCoordinates.X,
                        OriginalPositionY = a.Comment.OriginalCoordinates.Y,
                        PositionX = 0,
                        PositionY = 0,
                        CommentedTextInDocument = a.Comment.CommentTextInDocument
                    };

                if (com.Messages == null)
                    com.Messages = new List<SiliconStudio.Meet.EjpLib.BaseClasses.ejpCACommentMessage>();

                foreach (CommentMessage item in a.Comment.Messages)
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
                    MessageBox.Show("このリポートは、現在編集出来ません",
                        "レポートがロックされている。", MessageBoxButton.OK, MessageBoxImage.Stop);
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

                    using (this._textArea.DeclareChangeBlock())
                    {
                        Point point = e.GetPosition(this._textArea);
                        TextPointer position = this._textArea.GetPositionFromPoint(point, true);
                        //080731
                        TextPointer insP = position.InsertParagraphBreak();

                        //080805
                        ReportQuoteBoxEX rqb = new ReportQuoteBoxEX();
                        rqb.Fill = new SolidColorBrush(
                            Color.FromArgb(255, t.Color.Color.R, t.Color.Color.G, t.Color.Color.B));
                        rqb.QuoteContent = body;
                        rqb.PreviewMouseDown += new MouseButtonEventHandler(tb_PreviewMouseDown);
                        rqb.Margin = new Thickness(25, 16, 25, 16);
                        InlineUIContainer container = new InlineUIContainer(rqb, position);

                        //080731
                        insP.InsertParagraphBreak();
                    }
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                //TODO: Fix message...
                MessageBox.Show("この位置に引用出来ませんでした。もう一度他の位置で試して見て下さい。\n\n"
                    + "（別の引用の上には駄目ですよ）", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
		}

        void tb_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        void newRun_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Up &&
                e.Key != Key.Down &&
                e.Key != Key.Left &&
                e.Key != Key.Right)
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
                                h.ToolTip = new Label() { Content = "コントロールボタン＋クリックでリンク先を表示します" };
                                h.NavigateUri = new Uri(uriIw.Url);
                                h.Foreground = Brushes.Blue;
                                h.RequestNavigate += new RequestNavigateEventHandler(h_Click);
								
                            }
                            catch (Exception ex)
                            {
								SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - Report Editor",
								   "Failed to insert Hyperlink" +
								   "\nReport ID: " + this._reportObject.Id.ToString() +
								   "\nError: " + ex.Message);

                                MessageBox.Show("リンクを作成する際に失敗しました。\nUrlを正しく入力したかどうかを確かめた上もう一度試してみてください。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            catch (Exception ex)
                            {
								SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - Report Editor",
								   "Failed to insert Hyperlink" +
								   "\nReport ID: " + this._reportObject.Id.ToString() +
								   "\nError: " + ex.Message);

                                MessageBox.Show("リンクを作成する際に失敗しました。\nUrlを正しく入力したかどうかを確かめた上もう一度試してみてください。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - Report Editor",
								   "Failed to export Report to RTF" +
								   "\nPath: " + path +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nReport ID: " + this._reportObject.Id.ToString() +
								   "\nError: " + ex.Message);

                MessageBox.Show("レポートを書き出すする際に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - Report Editor",
								   "Failed to export Report to HTML" +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nReport ID: " + this._reportObject.Id.ToString() +
								   "\nError: " + ex.Message);

                MessageBox.Show("レポートをHTML化した際に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            Math.Max(pd_ia.OriginWidth, pagePadding.Left),
                            Math.Max(pd_ia.OriginHeight, pagePadding.Top),
                            Math.Max(pd_ia.MediaSizeWidth - (pd_ia.OriginWidth + pd_ia.ExtentWidth), pagePadding.Right),
                            Math.Max(pd_ia.MediaSizeHeight - (pd_ia.OriginHeight + pd_ia.ExtentHeight), pagePadding.Bottom));
                    fd.ColumnWidth = double.PositiveInfinity;
                    docWriter.Write(d_paginator);
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - Report Editor",
								   "Failed to print Report" +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nReport ID: " + this._reportObject.Id.ToString() +
								   "\nError: " + ex.Message);

                MessageBox.Show("印刷する際に失敗しました。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CloneCurrentReportDocument(bool convertInlineUiElements)
        {
            MemoryStream ms = new MemoryStream();
            XamlWriter.Save(this._textArea.Document, ms);
            ms.Position = 0;
            FlowDocument new_fd = (FlowDocument)XamlReader.Load(ms);
            ms.Close();
            ms.Dispose();

            Dictionary<Block, Block> blocksToInsert = new Dictionary<Block, Block>();
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
                                Run r = new Run(tb.QuoteContent);
                                r.Foreground = tb.Fill;
                                newParagraph.Inlines.Add(r);
                                AddP = true;
                            }
                        }
                    }

                    if (AddP)
                        blocksToInsert.Add(prev_Block, newParagraph);
                }
                prev_Block = oriBlock;
            }

            if (convertInlineUiElements)
            {
                foreach (Block bI in blocksToInsert.Keys)
                    new_fd.Blocks.InsertAfter(bI, blocksToInsert[bI]);
            }

            return new_fd;
        }

        #endregion

        #region CA Comments

        public void SetFocusToInputArea()
        {
            this._textArea.Focus();
        }

        private void ImportComment(EjpLib.BaseClasses.ejpCAComment comment)
        {
            TextPointer commentStart = this._textArea.GetPositionFromPoint(
                new Point(comment.OriginalPositionX, comment.OriginalPositionY), true);

            TextPointer commentEnd = commentStart.GetPositionAtOffset(comment.CommentedTextInDocument.Length);

            TextRange range = new TextRange(commentStart, commentEnd);
            range.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Color.FromArgb(80, 0, 255, 0)));

            ReportCommentAdorner comAd =
                new ReportCommentAdorner(
                    this._textArea, commentStart, commentEnd, comment.AuthorName, comment.AuthorId, comment.Messages,
                    comment.CommentId);

            AdornerLayer commentAdornerLayer = AdornerLayer.GetAdornerLayer(this._textArea);
            comAd.OnCommentAdornerDeleted += new CommentAdornerDeleted(OnCommentAdornerDeleted);
            commentAdornerLayer.Add(comAd);
            this._commentAdorners.Add(comAd);
        }

        /// <summary>
        /// Insert a Comment (Commented Assignment Only) for the 
        /// current selection in the TextArea.
        /// </summary>
        private void OnAddCommentToSelection(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this._textArea.Selection.IsEmpty)
                    return;

                TextPointer tp1 = this._textArea.Selection.Start;
                TextPointer tp2 = this._textArea.Selection.End;
                TextRange range = new TextRange(tp1, tp2);
                range.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Color.FromArgb(80,0,255,0)));

                ReportCommentAdorner comAd = 
                    new ReportCommentAdorner(
                        this._textArea, tp1, tp2, this.CurrentOwnerName, this.CurrentOwnerId, new List<EjpLib.BaseClasses.ejpCACommentMessage>(),
                        EjpLib.Helpers.IdManipulation.GetNewGuid());

                AdornerLayer commentAdornerLayer = AdornerLayer.GetAdornerLayer(this._textArea);
                comAd.OnCommentAdornerDeleted += new CommentAdornerDeleted(OnCommentAdornerDeleted);
                commentAdornerLayer.Add(comAd);
                this._commentAdorners.Add(comAd);
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
								   SiliconStudio.DebugManagers.MessageType.Error,
								   "EjpControls - Report Editor",
								   "Failed to Add Comment to Selection" +
								   "\nParent Study ID: " + this.ParentStudyId.ToString() +
								   "\nReport ID: " + this._reportObject.Id.ToString() +
								   "\nError: " + ex.Message);
                throw;
            }
        }

        private void RemoveAllComments()
        {
            AdornerLayer commentAdornerLayer = AdornerLayer.GetAdornerLayer(this._textArea);

            foreach (ReportCommentAdorner commentAdorner in this._commentAdorners)
            {
                commentAdornerLayer.Remove(commentAdorner);
                TextRange range = new TextRange(commentAdorner.CommentedSelectionStart, commentAdorner.CommentedSelectionEnd);
                range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.White);
            }

            this._commentAdorners.Clear();
        }

        private void OnCommentAdornerDeleted(ReportCommentAdorner commentAdorner)
        {
            AdornerLayer commentAdornerLayer = AdornerLayer.GetAdornerLayer(this._textArea);
            commentAdornerLayer.Remove(commentAdorner);
            TextRange range = new TextRange(commentAdorner.CommentedSelectionStart, commentAdorner.CommentedSelectionEnd);
            range.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.White);
            this._commentAdorners.Remove(commentAdorner);
        }

        #endregion
    }

    internal delegate void CommentAdornerDeleted(ReportCommentAdorner commentAdorner);

    internal class ReportCommentAdorner : Adorner
    {
        internal event CommentAdornerDeleted OnCommentAdornerDeleted;

        //Holds the visual elements of the adorner.
        private VisualCollection _visualChildren;

        protected override int VisualChildrenCount { get { return _visualChildren.Count; } }
        protected override Visual GetVisualChild(int index) { return _visualChildren[index]; }

        //private Thumb _resizeGrip;
        private bool _isDragging;
        private Point _currentDragPosition;
        private Point _currentDragOffset;
        private bool _renderInDefaultPosition;
        private Point _customPosition;
        private string _commentedTextInDocument;


        private KnowledgeMapComment _comment;
        public KnowledgeMapComment Comment
        {
            get { return _comment; }
            set { _comment = value; }
        }

        private TextPointer _commentedSelectionStart;
        public TextPointer CommentedSelectionStart
        {
            get { return _commentedSelectionStart; }
            set { _commentedSelectionStart = value; }
        }
        
        private TextPointer _commentedSelectionEnd;
        public TextPointer CommentedSelectionEnd
        {
            get { return _commentedSelectionEnd; }
            set { _commentedSelectionEnd = value; }
        }
        private UIElement _adornedElement;

        public ReportCommentAdorner(UIElement adornedElement, TextPointer selectionStart, TextPointer selectionEnd,
            string authorName, Guid authorId, List<EjpLib.BaseClasses.ejpCACommentMessage> messages, Guid commentId)
            : base(adornedElement)
        {
            this._adornedElement = adornedElement;
            this._commentedSelectionStart = selectionStart;
            this._commentedSelectionEnd = selectionEnd;
            this._visualChildren = new VisualCollection(this);
            this._renderInDefaultPosition = true;
            this.BuildComment(authorName, authorId, messages, commentId);
            
            this._comment.OriginalCoordinates = 
                this._commentedSelectionStart.GetCharacterRect(LogicalDirection.Forward).TopLeft;

            //this._resizeGrip.DragDelta += 
            //    new DragDeltaEventHandler(this.HandleResize);
        }

        private void BuildComment(string authorName, Guid authorId, List<EjpLib.BaseClasses.ejpCACommentMessage> messages, Guid commentId)
        {
            TextRange range = new TextRange(this._commentedSelectionStart, this._commentedSelectionEnd);

            this._comment = new KnowledgeMapComment()
            {
                OriginalAuthorId = authorId,
                OriginalAuthorName = authorName,
                CurrentAuthorId = authorId,
                CurrentAuthorName = authorName,
                CommentId = Guid.NewGuid(),
                Width = 200,
                Height = 300,
                CommentTextInDocument = range.Text
            };

            if (messages != null) //== empty comment box..
            {
                foreach (EjpLib.BaseClasses.ejpCACommentMessage message in messages)
                {
                    this._comment.Messages.Add(
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

            this._comment.OnDeleteComment += new DeleteKnowledgeMapComment(_comment_OnDeleteComment);
            //this._comment.OnMaximizeComment += new KnowledgeMapCommentViewStateChanged(_comment_OnMaximizeComment);
            //this._comment.OnMinimizeComment += new KnowledgeMapCommentViewStateChanged(_comment_OnMinimizeComment);
            //this._comment._r_PushPin.MouseLeftButtonDown += new MouseButtonEventHandler(CommentBgMouseDown);
            //this._comment._r_PushPin.MouseLeftButtonUp += new MouseButtonEventHandler(CommentBgMouseUp);
            //this._comment._r_PushPin.StylusDown += new StylusDownEventHandler(CommentBgStylusDown);
            //this._comment._r_PushPin.StylusUp += new StylusEventHandler(CommentStylusUp);
            //Enable to drag comments...
            //this._comment._r_PushPin.MouseMove += new MouseEventHandler(CommentMouseMove);
            //this._comment._r_PushPin.StylusMove += new StylusEventHandler(CommentStylusMove);
            this._visualChildren.Add(this._comment);

            //build the resize grip
            //if (this._resizeGrip != null) 
               // return;

            //this._resizeGrip = new Thumb();
            //this._comment.BorderThickness = new Thickness(0);
            //this._resizeGrip.Height = this._resizeGrip.Width = 17;
            //this._resizeGrip.Opacity = 0.4;
            //this._resizeGrip.Background = this._comment.Resources["ResizeHandleBrush"] as DrawingBrush;
            //this._visualChildren.Add(this._resizeGrip);
        }

        #region dragging
        private void CommentStylusUp(object sender, StylusEventArgs e)
        {
            this.InputUp();
        }

        private void CommentStylusMove(object sender, StylusEventArgs e)
        {
            this.InputMove(e.GetPosition(this._adornedElement));
        }

        private void CommentMouseMove(object sender, MouseEventArgs e)
        {
            this.InputMove(e.GetPosition(this._adornedElement));
        }

        private void CommentBgStylusDown(object sender, StylusDownEventArgs e)
        {
            this.InputDown(e.GetPosition(this._adornedElement), e.GetPosition(this._comment));
        }

        private void CommentBgMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.InputDown(e.GetPosition(this._adornedElement), e.GetPosition(this._comment));
        }

        private void CommentBgMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.InputUp();
        }

        private void InputMove(Point position)
        {
            if (this._isDragging)
            {
                this.UpdateCurrentDragPosition(position);
                this.InvalidateVisual();
            }
        }

        private void InputUp()
        {
            if (this._isDragging)
            {
                this._isDragging = false;
                this._comment.IsDragging = false;
            }
        }

        private void InputDown(Point adornerRelativePosition, Point commentRelativePosition)
        {
            if(this._isDragging == false)
                this.InitDragOperation(adornerRelativePosition, commentRelativePosition);
        }

        private void InitDragOperation(Point point, Point offset)
        {
            this._comment.IsDragging = true;
            this._currentDragOffset = offset;
            this._currentDragPosition = point;
            this._isDragging = true;
        }

        private void UpdateCurrentDragPosition(Point point)
        {
            this._currentDragPosition = point;
            this._renderInDefaultPosition = false;
            this.Measure(new Size());
        }
        #endregion

        #region events

        private void _comment_OnMinimizeComment(KnowledgeMapComment comment)
        {
            //this._resizeGrip.Visibility = Visibility.Collapsed;
        }

        private void _comment_OnMaximizeComment(KnowledgeMapComment comment)
        {
            //this._resizeGrip.Visibility = Visibility.Visible;
        }

        private void _comment_OnDeleteComment(KnowledgeMapComment comment)
        {
            this.PropagateCommentDeleted();  
        }

        private void PropagateCommentDeleted()
        {
            if (this.OnCommentAdornerDeleted != null)
                this.OnCommentAdornerDeleted(this);
        }

        #endregion

        //protected override void OnRender(DrawingContext drawingContext)
        //{
        //    //Rect adornedElementRect = new Rect(this.AdornedElement.DesiredSize);
        //    //SolidColorBrush renderBrush = new SolidColorBrush(Colors.Green);
        //    //Pen renderPen = new Pen(new SolidColorBrush(Colors.Navy), 1.5);

        //    //TextRange range = new TextRange(this._commentedSelectionStart, this._commentedSelectionEnd);
        //    //Rect commentStartRect = this._commentedSelectionStart.GetCharacterRect(LogicalDirection.Forward);
            
        //    //Point a1 = this._commentedSelectionStart.GetCharacterRect(LogicalDirection.Forward).TopLeft;
        //    //Point a2 = this._commentedSelectionEnd.GetCharacterRect(LogicalDirection.Backward).BottomRight;
        //    //Rect commentBounds = new Rect(a1, a2);
        //    //Point commentCenter = new Point(commentBounds.Left + (commentBounds.Width * 0.5), commentBounds.Top + (commentBounds.Height * 0.5)); 

        //    //Point screenPoint = this._comment.PointToScreen(this._comment._tb_Message.GetRectFromCharacterIndex(0).TopLeft);
        //    //Point parentPoint = this._adornedElement.PointFromScreen(screenPoint);
        //    //parentPoint.Offset(this._comment.ActualWidth * 0.5, this._comment.ActualHeight * 0.5);
        //    //drawingContext.DrawLine(renderPen, commentCenter, parentPoint);
        //}

        // Arrange the Adorner.
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this._comment.Height.Equals(Double.NaN))
                this._comment.Height = 300;
            if (this._comment.Width.Equals(Double.NaN))
                this._comment.Width = 200;

           
            double posX, posY;

            if (this._isDragging)
            {
                posX = this._currentDragPosition.X - this._currentDragOffset.X;
                posY = this._currentDragPosition.Y - this._currentDragOffset.Y;
                this._customPosition.X = posX;
                this._customPosition.Y = posY;
            }
            else if (_renderInDefaultPosition)
            {
                TextRange range = new TextRange(this._commentedSelectionStart, this._commentedSelectionEnd);
                Rect commentStartRect = this._commentedSelectionStart.GetCharacterRect(LogicalDirection.Forward);
                Rect commentEndRect = this._commentedSelectionEnd.GetCharacterRect(LogicalDirection.Forward);

                Rect compositeRect = new Rect(commentStartRect.TopLeft, commentEndRect.BottomRight);

                posX = (compositeRect.Left + compositeRect.Width * 0.5) - 100;
                posX = (posX > 0) ? posX : 10;

                posY = commentStartRect.Y - 100;
                posY = (posY > 0) ? posY : 10;

                posX = commentStartRect.Left + 3;
                posY = commentStartRect.Top -20;
            }
            else
            {
                posX = this._customPosition.X;
                posY = this._customPosition.Y;
            }

            this._comment.Arrange(
                new Rect(posX, posY,
                    this._comment.Width, this._comment.Height));

            //this._resizeGrip.Arrange(new Rect(
            //    posX + (this._comment.Width - 25),
            //    posY + (this._comment.Height - 25),
            //    25, 25));

            // Return the final size.
            return finalSize;
        }

        // Handler for resizing from the bottom-right.
        private void HandleResize(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;
            this._comment.Width = Math.Max(this._comment.Width + args.HorizontalChange, hitThumb.DesiredSize.Width);
            this._comment.Height = Math.Max(args.VerticalChange + this._comment.Height, hitThumb.DesiredSize.Height);
            this.UpdateLayout();
        }


    }
}