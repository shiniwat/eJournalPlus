using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EjsBridge.ejsService;

namespace ejpClient.ejpWindows
{
	/// <summary>
	/// Interaction logic for EjsOpenAssignmentWindowEX.xaml
	/// </summary>
	public partial class EjsOpenAssignmentWindowEX : Window
	{
		/// <summary>
        /// This property can be examined by instantiators
        /// when the window is closed, to see if the operation
        /// was cancelled.
        /// </summary>
        private bool _cancelled = true;
        public bool Cancelled
        {
            get { return _cancelled; }
            set { _cancelled = value; }
        }

        /// <summary>
        /// The Online assignment to open.
        /// </summary>
        private ejsAssignment _assignmentToOpen;
        public ejsAssignment AssignmentToOpen
        {
            get { return _assignmentToOpen; }
            set { _assignmentToOpen = value; }
        }

        /// <summary>
        /// Signifies wether the selected assignement
        /// should be opened as a new Commented Assignment.
        /// If this is set to True, the user has chosen
        /// to open a normal Assignment as a Commented Assignment.
        /// This will effectively create a new Commented Assignemnt.
        /// </summary>
        private bool _openSelectedAssignmentAsCommented;
        public bool OpenSelectedAssignmentAsCommented
        {
            get { return _openSelectedAssignmentAsCommented; }
        }

        /// <summary>
        /// If this is set to true, the chosen Assignment is to be included in
        /// an already open Assignment. That is, the chosen Assignment is to be
        /// merged with the currently open Assignment.
        /// </summary>
        public bool OpenForMerge { get; set; }

        /// <summary>
        /// List of all Meta data of all Assignments available to this
        /// session on eJournal Server.
        /// </summary>
        private EjsBridge.ejsService.ejsAssignment[] _assignments;

		public EjsOpenAssignmentWindowEX(bool openForMerge)
        {
            InitializeComponent();
            if (openForMerge)
            {
                this._b_DeleteAssignment.IsEnabled = false;
                this._b_MergeAndOpen.IsEnabled = false;
                this._b_OpenCommentedAssignment.IsEnabled = false;
                this._b_OpenAssignment.Content = "読み込む";
                this.OpenForMerge = true;
            }
            else
            {
                this._b_DeleteAssignment.IsEnabled = true;
                this._b_MergeAndOpen.IsEnabled = true;
                this._b_OpenCommentedAssignment.IsEnabled = true;
                this._b_OpenAssignment.Content = "開く";
                this.OpenForMerge = false;
            }

        }

        protected override void OnContentRendered(EventArgs e)
        {
            if (App.IsCurrentUserEJSAuthenticated() == false)
            {
                ejpWindows.EjsLoginWindow loginWindow = new EjsLoginWindow();
                loginWindow.ShowDialog();
            }
            if (App.IsCurrentUserEJSAuthenticated() == false)
            {
                this._cancelled = true;
                this.Close();
            }
            else
            {
                this._tb_LoginName.Text =
                    App._currentEjpStudent.LastName + " " + App._currentEjpStudent.FirstName;

                this.LoadDataFromEjs();
            }

            //Set the combobox to display the documents
            //of the first course in the list, if there are
            //any courses in the list.
            if (this._cb_Courses.Items.Count > 0)
                this._cb_Courses.SelectedIndex = 0;
        }

        /// <summary>
        /// Connect to the EJS and get data on all courses and 
        /// published assignments.
        /// </summary>
        private void LoadDataFromEjs()
        {
            try
            {
                ejpWindows.LoadingMessageWindow lmw =
                        new ejpClient.ejpWindows.LoadingMessageWindow();

                EjsBridge.ejsService.ejsCourse[] courses = null;

                BackgroundWorker bgw = new BackgroundWorker();
                bgw.DoWork += delegate(object s3, DoWorkEventArgs doWorkArgs)
                {
                    courses =
                    EjsBridge.ejsBridgeManager.GetRegisteredCoursesForUser(
                    App._currentEjpStudent.SessionToken, true);

                    this._assignments = EjsBridge.ejsBridgeManager.GetAllPublishedAssignments(
                        App._currentEjpStudent.SessionToken, false);
                };

                bgw.RunWorkerCompleted += delegate(object s4, RunWorkerCompletedEventArgs workerCompletedArgs)
                {
                    lmw.Close();
                    bgw.Dispose();
                };

                bgw.RunWorkerAsync();
                lmw.ShowDialog();

                ObservableCourseList cList = this.Resources["CourseList"] as ObservableCourseList;
                cList.Clear();

                foreach (ejsCourse course in courses)
                {
                    cList.Add(course);
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					  SiliconStudio.DebugManagers.MessageType.Error,
					  "eJournalPlus Client - EJS Open Assignment Window",
					  "Loading data from EJS Failed" +
					  "\nError: " + ex.Message);
            }
        }

        private void OnCourseListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._cb_Courses.SelectedValue != null)
            {
                ejsCourse course = this._cb_Courses.SelectedValue as ejsCourse;

				this._tv_Assignments.Items.Clear();
				//First add all the original assignments
				AssignmentTreeViewItem tParent = null;
				foreach (ejsAssignment ass in this._assignments)
				{
					//1 = Commented Assignment
					if (ass.AssignmentContentType == 1)
						continue; //We only add the 'real' assignments first

                    if (ass.CourseId == course._id) //does this assignment belong to the current course?
					{
						AssignmentTreeViewItem t = new AssignmentTreeViewItem(ass);
						t.DefaultImage = new BitmapImage(new Uri("pack://application:,,,/imgData/aTvS.png"));
						t.SelectedImage = new BitmapImage(new Uri("pack://application:,,,/imgData/aTvD.png"));
						this._tv_Assignments.Items.Add(t);
						tParent = t;
					}
				}

                foreach (ejsAssignment ass in this._assignments)
                {
                    //0 = Normal Assignment
                    if (ass.AssignmentContentType == 0)
                        continue; //We're only adding the Commented Assignments

                    if (ass.CourseId == -1) //-1 = commented assignments do not belong to courses 
                    {
                        foreach (AssignmentTreeViewItem ParentAssignment in this._tv_Assignments.Items)
                        {
                            this.BuildAssignmentTree(ParentAssignment, ass, ParentAssignment);
                        }
                    }
                }

                foreach (AssignmentTreeViewItem ParentAssignment in this._tv_Assignments.Items)
                {
                    ParentAssignment.TextDetails.Text +=
                        " Comments: " + ParentAssignment.Assignment.CommentCount.ToString();
                }
            }
        }

        /// <summary>
        /// Called to update the branches of the tree once the root nodes 
        /// have been added. This method is meant to be called recursively.
        /// </summary>
        /// <param name="root">Root node</param>
        /// <param name="child">Current Child</param>
        /// <param name="branchRoot">Original root of this branch</param>
        private void BuildAssignmentTree(AssignmentTreeViewItem root, 
            ejsAssignment child, AssignmentTreeViewItem branchRoot)
        {
            if (root.Assignment.ExternalAssignmentId == child.ParentAssignmentId)
            {
                CommentedAssignmentTreeViewItem cat = new CommentedAssignmentTreeViewItem(child);
                cat.BranchRoot = branchRoot;
                cat.DefaultImage = new BitmapImage(new Uri("pack://application:,,,/imgData/caTvS.png"));
                cat.SelectedImage = new BitmapImage(new Uri("pack://application:,,,/imgData/caTvD.png"));
                root.Items.Add(cat);
                //Add up the total number of comments in this branch..
                branchRoot.Assignment.CommentCount += child.CommentCount;
            }
            else
            {
                foreach (AssignmentTreeViewItem childItem in root.Items)
                {
                    this.BuildAssignmentTree(childItem, child, branchRoot);
                }
            }
        }

        private void OnDeleteAssignment(object sender, RoutedEventArgs e)
        {
            if (this._tv_Assignments.SelectedItem != null)
            {
                AssignmentTreeViewItem selectedAss = this._tv_Assignments.SelectedItem as AssignmentTreeViewItem;
                if (selectedAss == null)
                    return;

                ejsAssignment assToDelete = selectedAss.Assignment;

                if (assToDelete.OwnerUserId != App._currentEjpStudent.Id)
                    MessageBox.Show("他人のアサインメントは削除出来ません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Stop);
                else
                {
                    try
                    {
                        int selectedCoursItemId = this._cb_Courses.SelectedIndex;
                        if (MessageBox.Show("選択されたアサインメントを削除しますか？", "削除",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            EjsBridge.ejsBridgeManager.DeleteAssignment(
                            App._currentEjpStudent.SessionToken, assToDelete);
                            this.LoadDataFromEjs();

                            //Set the combobox to display the documents
                            //of the prev selected course in the list, if there are
                            //any courses in the list.
                            if(this._cb_Courses.Items.Count != 0)
                                this._cb_Courses.SelectedIndex = selectedCoursItemId;
                        }
                    }
                    catch (ApplicationException ex)
                    {
						SiliconStudio.DebugManagers.DebugReporter.Report(
							 SiliconStudio.DebugManagers.MessageType.Error,
							 "eJournalPlus Client - EJS Open Assignment Window",
							 "Deleting Assignment from EJS Failed" +
							 "\nError: " + ex.Message);
					
                        MessageBox.Show("選択されたアサインメントは削除できません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void OnOpenAssignment(object sender, RoutedEventArgs e)
        {
            if (this._tv_Assignments.SelectedItem != null)
            {
                AssignmentTreeViewItem selectedAss = this._tv_Assignments.SelectedItem as AssignmentTreeViewItem;
                if (selectedAss == null)
                    return;

                if (selectedAss is CommentedAssignmentTreeViewItem)
                {
                    if (
                    MessageBox.Show("選択したファイルにはコメントがあるため" +
                        "\nコメントモードに開きます。\n\n" +
                        "宜しいですか？", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question)
                        == MessageBoxResult.No)
                        return;
                }

                this._assignmentToOpen = selectedAss.Assignment;

                if (this._assignmentToOpen != null)
                {
                    this._cancelled = false;
                    this._openSelectedAssignmentAsCommented = false;
                    this.Close();
                }
                else
                    MessageBox.Show("選択されたアサインメントが開けません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                this._cancelled = true;
                this.Close();
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this._cancelled = true;
            this.Close();
        }

        private void OnOpenAssignmentAsCA(object sender, RoutedEventArgs e)
        {
            if (this._tv_Assignments.SelectedItem != null)
            {
                AssignmentTreeViewItem selectedAss = this._tv_Assignments.SelectedItem as AssignmentTreeViewItem;
                if (selectedAss == null)
                    return;

                //If this value is an empty GUID that means the assignment
                //was created with version 1 of EJP+EJP. Thus, it cannot
                //be opened as a Commented Assignment with this version of EJP.
                if (selectedAss.Assignment.ExternalAssignmentId == Guid.Empty)
                {
                    MessageBox.Show("\n\nThis assignment was created with an earlier version\n" +
                                    "of EJournal Plus, therefor it cannot be opened in \n" +
                                    "Comment Mode.\n\n" +
                                    "If you wish do upgrade this Assignment, first open it\n" +
                                    "in Normal Mode, then re-publish it to E Journal Server.\n" +
                                    "After this you can open the new Published version in \n" +
                                    "Comment Mode.", "Version Error", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                this._assignmentToOpen = selectedAss.Assignment;
                if (this._assignmentToOpen != null)
                {
                    this._cancelled = false;
                    this._openSelectedAssignmentAsCommented = true;
                    this.Close();
                }
                else
                    MessageBox.Show("選択されたアサインメントが開けません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                this._cancelled = true;
                this.Close();
            }
        }

        /// <summary>
        /// Tell EJS to merge all the comments and open the Assignment
        /// as a Commented Assignment.
        /// </summary>
        private void OnMergeCommentsAndOpen(object sender, RoutedEventArgs e)
        {
            //TODO: Implement
        }

        private void _tv_Assignments_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is AssignmentTreeViewItem)
            {
                AssignmentTreeViewItem selAss = e.NewValue as AssignmentTreeViewItem;
                this._b_OpenAssignment.IsEnabled = true;
                
                //Do not enable anything else...
                if (this.OpenForMerge)
                    return;
                    
                this._b_OpenCommentedAssignment.IsEnabled = true;

                if (App._currentEjpStudent.Id == selAss.Assignment.OwnerUserId)
                {
                    this._b_DeleteAssignment.IsEnabled = true;
                    if (selAss.Items.Count != 0)
                        this._b_MergeAndOpen.IsEnabled = true;
                    else
                        this._b_MergeAndOpen.IsEnabled = false;
                }
                else
                {
                    this._b_DeleteAssignment.IsEnabled = false;
                    this._b_MergeAndOpen.IsEnabled = false;
                }

                //Version Check
                if(selAss.Assignment.ExternalAssignmentId == 
                    Guid.Empty)
                    this._b_OpenCommentedAssignment.IsEnabled = false;
            }
            if (e.NewValue is CommentedAssignmentTreeViewItem)
            {
                //Do not enable anything...
                if (this.OpenForMerge)
                    return;

                CommentedAssignmentTreeViewItem selAss = e.NewValue as CommentedAssignmentTreeViewItem;
                this._b_OpenAssignment.IsEnabled = false;
                this._b_OpenCommentedAssignment.IsEnabled = true;
                this._b_MergeAndOpen.IsEnabled = false;

                if (selAss.BranchRoot.Assignment.OwnerUserId == App._currentEjpStudent.Id)
                {
                    this._b_DeleteAssignment.IsEnabled = true;
                }
                else
                    this._b_DeleteAssignment.IsEnabled = false;

                //Version Check
                if (selAss.Assignment.ExternalAssignmentId ==
                    Guid.Empty)
                    this._b_OpenCommentedAssignment.IsEnabled = false;
            }
        }
                       
    }

	internal class AssignmentTreeViewItem : TreeViewItem
	{
		protected TextBlock _textTitle;
        private TextBlock _textDetails;
        
        public TextBlock TextDetails
        {
            get { return _textDetails; }
            set { _textDetails = value; }
        }

		protected Image _image;
		protected ImageSource _imgSelected, _imgDefault;
		protected ejsAssignment _assignment;

		public string Text
		{
			get { return this.GetItemText(); }
		}

		public ImageSource SelectedImage
		{
			get { return this._imgSelected; }

			set
			{
				this._imgSelected = value;
				if (this.IsSelected)
					this._image.Source = this._imgSelected;
			}
		}

		public ImageSource DefaultImage
		{
			get { return this._imgDefault; }

			set
			{
				this._imgDefault = value;
				if (!this.IsSelected)
					this._image.Source = this._imgDefault;
			}
		}

		public ejsAssignment Assignment
		{
			get { return _assignment; }
		}
	
		public AssignmentTreeViewItem(ejsAssignment assignment)
		{
			//Give som space...
			this.Margin = new Thickness(0, 2, 0, 2);

			//Set the assignment
			this._assignment = assignment;

			//Build the visual
			StackPanel panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			this.Header = panel;

			this._image = new Image();
			this._image.VerticalAlignment = VerticalAlignment.Center;
			this._image.Margin = new Thickness(0, 0, 2, 0);
			panel.Children.Add(this._image);

			this._textTitle = new TextBlock();
			this._textTitle.FontWeight = FontWeights.Bold;
			this._textTitle.VerticalAlignment = VerticalAlignment.Center;
			this._textTitle.Margin = new Thickness(0, 0, 6, 0);
			panel.Children.Add(this._textTitle);

			this._textDetails = new TextBlock();
			this._textDetails.VerticalAlignment = VerticalAlignment.Center;
			panel.Children.Add(this._textDetails);

			//Set the text of the visual
			this._textTitle.Text = this._assignment.Title;
			this._textDetails.Text =
					" (" +	
					this._assignment.OwnerName +
					" / " + this._assignment.CreationDate.ToShortDateString() +
					" " + this._assignment.CreationDate.ToLongTimeString() +
					")"; 
		}

		public override string ToString()
		{
			if (this._assignment != null)
				return this.GetItemText();
			else
				return "No Assignment Set";
		}

		protected virtual string GetItemText()
		{
			return this._textTitle.Text + this._textDetails.Text;
		}

		protected override void OnSelected(RoutedEventArgs e)
		{
			base.OnSelected(e);
			this._image.Source = this._imgSelected;
		}

		protected override void OnUnselected(RoutedEventArgs e)
		{
			base.OnUnselected(e);
			this._image.Source = this._imgDefault;
		}
	}

	internal class CommentedAssignmentTreeViewItem : AssignmentTreeViewItem
	{
        public AssignmentTreeViewItem BranchRoot { get; set; }

		public CommentedAssignmentTreeViewItem(ejsAssignment assignment)
			: base(assignment)
		{
			this._textTitle.Text = assignment.OwnerName;
			this.TextDetails.Text =
				this._assignment.CreationDate.ToShortDateString() +
					" " + this._assignment.CreationDate.ToLongTimeString() +
					" Comments: " + this._assignment.CommentCount.ToString();
		}
	}
}
