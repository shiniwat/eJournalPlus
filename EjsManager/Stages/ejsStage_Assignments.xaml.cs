using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using SiliconStudio.Meet.EjsManager.ServiceOperations;
using SiliconStudio.Meet.EjsManager.ejsServiceReference;

namespace SiliconStudio.Meet.EjsManager
{
	/// <summary>
	/// Interaction logic for ejsStage_Assignments.xaml
	/// </summary>
	public partial class ejsStage_Assignments : ejsManagerStage
	{
		public ejsStage_Assignments()
		{
			InitializeComponent();
        }

        #region Prepare Stage

        public override void PrepareStage()
		{
            lock (this.threadLock)
            {
                if (this._isStageBusy)
                    return;

                if (this.IsStageReady == true)
                    return;

                this._isStageBusy = true;

                BackgroundWorker bgw = new BackgroundWorker();
                bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PrepareOperationCompleted);
                bgw.WorkerSupportsCancellation = true;
                bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
                {
                    try
                    {
                        e.Result = ejsBridgeManager.GetAllPublishedAssignments(
                            this.CurrentUserToken, true);
                    }
                    catch (Exception ex)
                    {
						MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Getting All Assignments on eJournalServer...");
            }
		}

		private void PrepareOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled == false
				&& e.Error == null)
			{
				ejsAssignment[] assignments =
					e.Result as ejsAssignment[];
				if (assignments != null)
				{
					ObservableAssignmentList l =
						App.Current.Resources["CompleteAssignmentsList"] as ObservableAssignmentList;

					l.Clear();

					for (int i = 0; i < assignments.Length; i++)
						l.Add(assignments[i]);

					this.OrganizeAssignments(l);
				}
			}

			this.IsStageReady = true;
            this._isStageBusy = false;

			this.RaiseAsyncOperationCompletedEvent();

        }

        #endregion

        #region Update Data

        private void OnUpdateList(object sender, RoutedEventArgs e)
		{
			this.UpdateData();
        }

        private void UpdateData()
        {
            this.IsStageReady = false;
            this._tv_Assignments.Items.Clear();
            ObservableAssignmentList l =
                        App.Current.Resources["CompleteAssignmentsList"] as ObservableAssignmentList;
            l.Clear();
            this.PrepareStage();
        }

        #endregion

        #region Delete / Hide Item

		private void OnDeleteCurrentItem(object sender, RoutedEventArgs e)
		{
			if (this._tv_Assignments.SelectedItem == null)
				return;

            if (this._tv_Assignments.SelectedItem is CourseTreeViewItem)
                return;

			if (this.GetDeleteConfirmation() == true)
			{
				AssignmentTreeViewItem assignment =
						 this._tv_Assignments.SelectedItem as AssignmentTreeViewItem;

				this.DeleteAssignment(assignment.Assignment);
			}

		}

		private void DeleteAssignment(ejsAssignment assignment)
		{
			lock (this.threadLock)
			{
				if (this._isStageBusy)
					return;

				this._isStageBusy = true;

				BackgroundWorker bgw = new BackgroundWorker();
				bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(UpdateItemOperationCompleted);
				bgw.WorkerSupportsCancellation = true;
				bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
				{
					try
					{
						ejsBridgeManager.DeleteAssignment(this.CurrentUserToken, assignment);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						e.Cancel = true;
					}
				};

				bgw.RunWorkerAsync();

				this.RaiseAsyncOperationStartedEvent("Deleting Assignment on eJournalServer...");
			}
		}

        private void OnHideCurrentItem(object sender, RoutedEventArgs e)
		{
			if (this._tv_Assignments.SelectedItem == null)
				return;

            if (this._tv_Assignments.SelectedItem is CourseTreeViewItem)
                return;

            AssignmentTreeViewItem assignment =
                     this._tv_Assignments.SelectedItem as AssignmentTreeViewItem;

            this.HideAssignment(assignment.Assignment);
            
		}

        private void HideAssignment(ejsAssignment assignment)
        {
            lock (this.threadLock)
            {
                if (this._isStageBusy)
                    return;

                this._isStageBusy = true;

                BackgroundWorker bgw = new BackgroundWorker();
                bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(UpdateItemOperationCompleted);
                bgw.WorkerSupportsCancellation = true;
                bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
                {
                    try
                    {
                        ejsBridgeManager.HideAssignment(this.CurrentUserToken, assignment);
                    }
                    catch (Exception ex)
                    {
						MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Deleting Assignment on eJournalServer...");
            }
        }

        #endregion

        #region Helpers and Shared Methods

        private void UpdateItemOperationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.IsStageReady = true;
            this._isStageBusy = false;
            this.RaiseAsyncOperationCompletedEvent();
            this.UpdateData();
        }

        private void OrganizeAssignments(ObservableAssignmentList assignments)
		{

			this._tv_Assignments.Items.Clear();

			ObservableCourseList clist =
				App.Current.Resources["CompleteCoursesList"] as ObservableCourseList;

			foreach (ejsCourse course in clist)
			{
				CourseTreeViewItem c = new CourseTreeViewItem(course);
				this._tv_Assignments.Items.Add(c);
			}

			List<AssignmentTreeViewItem> topAssignments =
				new List<AssignmentTreeViewItem>();

			foreach (ejsAssignment assignment in assignments)
			{
				//1 = Commented Assignment
				if (assignment.AssignmentContentType == 1)
					continue; //We only add the 'real' assignments first

				AssignmentTreeViewItem t = new AssignmentTreeViewItem(assignment);
                if (assignment.IsAvailable)
                {
                    t.DefaultImage = new BitmapImage(new Uri("pack://application:,,,/Stages/imgData/aTvS.png"));
                    t.SelectedImage = new BitmapImage(new Uri("pack://application:,,,/Stages/imgData/aTvD.png"));
                }
                else
                {
                    t.DefaultImage = new BitmapImage(new Uri("pack://application:,,,/Stages/imgData/aNA.png"));
                    t.SelectedImage = new BitmapImage(new Uri("pack://application:,,,/Stages/imgData/aNA.png"));
                }

				foreach (CourseTreeViewItem cItem in this._tv_Assignments.Items)
				{
					if (cItem.Course._id == assignment.CourseId)
						cItem.Items.Add(t);
				}

				topAssignments.Add(t);
			}

			//Second run to add all the children
			foreach (ejsAssignment assignment in assignments)
			{
				//0 = Normal Assignment
				if (assignment.AssignmentContentType == 0)
					continue; //We're only adding the Commented Assignments

				if (assignment.CourseId == -1) //-1 = commented assignments do not belong to courses 
				{
					foreach (AssignmentTreeViewItem ParentAssignment in topAssignments)
						this.BuildAssignmentTree(ParentAssignment, assignment, ParentAssignment);
				}
			}

			foreach (AssignmentTreeViewItem ParentAssignment in topAssignments)
			{
				ParentAssignment.TextDetails.Text +=
					" Comments: " + ParentAssignment.Assignment.CommentCount.ToString();
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
                if (child.IsAvailable)
                {
                    cat.DefaultImage = new BitmapImage(new Uri("pack://application:,,,/Stages/imgData/caTvS.png"));
                    cat.SelectedImage = new BitmapImage(new Uri("pack://application:,,,/Stages/imgData/caTvD.png"));
                }
                else
                {
                    cat.DefaultImage = new BitmapImage(new Uri("pack://application:,,,/Stages/imgData/aNA.png"));
                    cat.SelectedImage = new BitmapImage(new Uri("pack://application:,,,/Stages/imgData/aNA.png"));
                }
				root.Items.Add(cat);
				//Add up the total number of comments in this branch..
				branchRoot.Assignment.CommentCount += child.CommentCount;
			}
			else
			{
				foreach (AssignmentTreeViewItem childItem in root.Items)
					this.BuildAssignmentTree(childItem, child, branchRoot);
			}
        }

        #endregion

        #region Restore Item

        private void OnRestoreCurrentItem(object sender, RoutedEventArgs e)
        {
            if (this._tv_Assignments.SelectedItem == null)
                return;

            if (this._tv_Assignments.SelectedItem is
                CourseTreeViewItem)
                return;

            AssignmentTreeViewItem assignment =
                     this._tv_Assignments.SelectedItem as AssignmentTreeViewItem;

            this.RestoreAssignment(assignment.Assignment);

        }

        private void RestoreAssignment(ejsAssignment assignment)
        {
            lock (this.threadLock)
            {
                if (this._isStageBusy)
                    return;

                this._isStageBusy = true;

                BackgroundWorker bgw = new BackgroundWorker();
                bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(UpdateItemOperationCompleted);
                bgw.WorkerSupportsCancellation = true;
                bgw.DoWork += delegate(object sender, DoWorkEventArgs e)
                {
                    try
                    {
                        ejsBridgeManager.RestoreAssignment(this.CurrentUserToken, assignment);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                };

                bgw.RunWorkerAsync();

                this.RaiseAsyncOperationStartedEvent("Restoring Assignment on eJournalServer...");
            }
        }

        #endregion

    }


	internal class CourseTreeViewItem : TreeViewItem
	{
		protected TextBlock _textTitle;
		private TextBlock _textDetails;

		public TextBlock TextDetails
		{
			get { return _textDetails; }
			set { _textDetails = value; }
		}

		protected ejsCourse _course;

		public string Text
		{
			get { return this.GetItemText(); }
		}

		public ejsCourse Course
		{
			get { return _course; }
		}

		public CourseTreeViewItem(ejsCourse course)
		{
			//Give som space...
			this.Margin = new Thickness(0, 2, 0, 2);

			//Set the assignment
			this._course = course;

			//Build the visual
			StackPanel panel = new StackPanel();
			panel.Orientation = Orientation.Horizontal;
			this.Header = panel;

			this._textTitle = new TextBlock();
			this._textTitle.FontWeight = FontWeights.Bold;
			this._textTitle.VerticalAlignment = VerticalAlignment.Center;
			this._textTitle.Margin = new Thickness(0, 0, 6, 0);
			panel.Children.Add(this._textTitle);

			this._textDetails = new TextBlock();
			this._textDetails.VerticalAlignment = VerticalAlignment.Center;
			panel.Children.Add(this._textDetails);

			//Set the text of the visual
			this._textTitle.Text = this._course._name;
			this._textDetails.Text = " (" + this._course._description + ")";
		}

		public override string ToString()
		{
			if (this._course != null)
				return this.GetItemText();
			else
				return "No Course Set";
		}

		protected virtual string GetItemText()
		{
			return this._textTitle.Text + this._textDetails.Text;
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
