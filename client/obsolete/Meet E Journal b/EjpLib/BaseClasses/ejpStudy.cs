using System;
using System.Collections.ObjectModel;
using System.IO;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
	/// <summary>
	/// Represents a Study inside an Assignment.
	/// </summary>
	public sealed class ejpStudy : IDisposable
	{
		#region Properties with Accessors

        public int LastDispplayedKMIndex = 0;
        public int LastDispplayedReportXPSIndex = 0;

		private ejpStudyMetaData _metaData;
		public ejpStudyMetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}

        //For databinding
        public string Title
        {
            get { return this._metaData.Title; }
            set { this._metaData.Title = value; }
        }

        //For databinding
        public string IdString
        {
            get { return this._metaData.Id.ToString(); }
        }

		private ObservableCollection<ejpXpsDocument> _xpsDocuments;
		public ObservableCollection<ejpXpsDocument> XpsDocuments
		{
			get { return _xpsDocuments; }
			set { _xpsDocuments = value; }
		}

		private ObservableCollection<ejpReport> _reports;
		public ObservableCollection<ejpReport> Reports
		{
			get { return _reports; }
			set { _reports = value; }
		}

		private ObservableCollection<ejpKnowledgeMap> _knowledgeMaps;
		public ObservableCollection<ejpKnowledgeMap> KnowledgeMaps
		{
			get { return _knowledgeMaps; }
			set { _knowledgeMaps = value; }
		}

        public Guid LastEditedReport { get; set; }
        public Guid LastEditedKnowledgeMap { get; set; }
        public Guid LastEditedXpsDocument { get; set; }

		/// <summary>
		/// The string that identifies this studyPart within its parent Assignment Package.
		/// This is a package Id and not the ID of the study itself.
		/// </summary>
		public Uri PackageRelationshipIDString { get; set; }

		#endregion

		#region Private Properties

		#endregion

		#region Constructors
		/// <summary>
		/// Default constructor for New Studies.
		/// </summary>
		internal ejpStudy(Guid parentAssignmentId, Guid id, string title, 
            int EJSDatabaseId, string firstXpsDocumentPath, bool XpsExternalToAssignment,
            Guid xpsDocumentId, string xpsDocumentTitle)
		{

			this._metaData = new ejpStudyMetaData(parentAssignmentId, id, title, EJSDatabaseId);
			
			this._xpsDocuments = new ObservableCollection<ejpXpsDocument>();
			this._knowledgeMaps = new ObservableCollection<ejpKnowledgeMap>();
			this._reports = new ObservableCollection<ejpReport>();

			this.LoadXpsDocument(firstXpsDocumentPath, xpsDocumentTitle, XpsExternalToAssignment, xpsDocumentId);
			//this.AddKnowledgeMap();
			//this.AddReport();

		}//end: Constructor()

        /// <summary>
        /// Default constructor for New Empty Studies.
        /// </summary>
        public ejpStudy(Guid parentAssignmentId, Guid id, string title, int EJSDatabaseId)
        {

            this._metaData = new ejpStudyMetaData(parentAssignmentId, id, title, EJSDatabaseId);

            this._xpsDocuments = new ObservableCollection<ejpXpsDocument>();
            this._knowledgeMaps = new ObservableCollection<ejpKnowledgeMap>();
            this._reports = new ObservableCollection<ejpReport>();

            //this.AddKnowledgeMap();
            //this.AddReport();

        }//end: Constructor()

		/// <summary>
		/// Internal empty constructor.
		/// </summary>
		internal ejpStudy()
		{
			this._xpsDocuments = new ObservableCollection<ejpXpsDocument>();
			this._knowledgeMaps = new ObservableCollection<ejpKnowledgeMap>();
			this._reports = new ObservableCollection<ejpReport>();
		}//end: Constructor()

		#endregion

		#region Internal Methods

		/// <summary>
		/// Import a Knowledge Map to the Study.
		/// </summary>
		internal void ImportKnowledgeMap(ejpKnowledgeMap map)
		{
			if (this._knowledgeMaps.Count == 2)
				throw new Exception("No more than 2 knowledgemaps are allowed.");

			this._knowledgeMaps.Add(map);
		}

		/// <summary>
		/// Import a Report to the Study.
		/// </summary>
		internal void ImportReport(ejpReport report)
		{
			if (this._reports.Count == 2)
				throw new Exception("No more than 2 Reports are allowed.");

			this._reports.Add(report);
		}

		#endregion

		#region Public Methods

        /// <summary>
        /// Used to add new XPS document to an already existing Study
        /// </summary>
		public ejpXpsDocument ImportXpsDocument(string path, string title, bool isExternalToAssignment, Guid xpsDocumentId)
		{
            ejpXpsDocument d = new ejpXpsDocument(path, title, isExternalToAssignment, xpsDocumentId, this._metaData.Id);
			EjpLib.AssignmentOperations.LocalAssignmentFileOperations.ImportXpsDocumentToPackage(
                d, Enumerations.AssignmentPackagePartRelationship.XPSDocument_v1, this.MetaData.Id);
			this._xpsDocuments.Add(d);
			return d;
		}

		/// <summary>
		/// Import a local image file into the current study
		/// </summary>
		public ejpExternalImageEntityWrapper ImportImageFileToStudy(string filePath, ejpKnowledgeMap targetKM)
		{
			DebugManagers.DebugReporter.ReportMethodEnter();

			ejpExternalImageEntityWrapper wrapper =
				EjpLib.AssignmentOperations.LocalAssignmentFileOperations.ImportImageFileToPackage(
				Enumerations.AssignmentPackagePartRelationship.KnowledgeMapImageObject_v1,
				filePath,
				targetKM.PackageRelationshipIDString,
				this,
				targetKM.Id,
				Helpers.IdManipulation.GetNewGuid());

			DebugManagers.DebugReporter.ReportMethodLeave();

			return wrapper;
		}

        /// <summary>
        /// Import a local image file into the current study and set it as Guide for the
        /// given KnowledgeMap
        /// </summary>
        public ejpExternalImageEntityWrapper ImportKnowledgeMapGuideToStudy(string filePath, ejpKnowledgeMap targetKM)
        {
            DebugManagers.DebugReporter.ReportMethodEnter();

            ejpExternalImageEntityWrapper wrapper =
                EjpLib.AssignmentOperations.LocalAssignmentFileOperations.ImportImageFileToPackage(
                Enumerations.AssignmentPackagePartRelationship.KnowledgeMapImageObject_v1,
                filePath,
                targetKM.PackageRelationshipIDString,
                this,
                targetKM.Id,
                Helpers.IdManipulation.GetNewGuid());

            //Let the KM now the source Uri of the image
            //designated as Guide. Until the map is saved,
            //the only data saved here is the source Uri and
            //and empty Id. The rest of the fields get set
            //when the KM is saved...
            targetKM.Guide = new ejpKMGuide()
            {
                Id = Guid.Empty,
                SourceUri = wrapper.SourceUri
            };

            DebugManagers.DebugReporter.ReportMethodLeave();

            return wrapper;
        }

        /// <summary>
        /// Only used when creating new Assignments with a first XPS document
        /// </summary>
        internal void LoadXpsDocument(string path, string title, bool isExternalToAssignment, Guid xpsDocumentId)
        {
            //TODO: check that the XPS document does not already exist in the study.
            this._xpsDocuments.Add(new ejpXpsDocument(path, title, isExternalToAssignment, xpsDocumentId, this._metaData.Id));
        }

        /// <summary>
        /// Used to reload XPS Documents that are already part of an Assignment
        /// </summary>
		internal void LoadXpsDocument(Stream data, string path, bool isExternalToAssignment)
		{
			//TODO: check that the XPS document does not already exist in the study.
            this._xpsDocuments.Add(new ejpXpsDocument(data, path, isExternalToAssignment, this._metaData.Id));
		}

        /// <summary>
        /// Adds a new Knowledge Map to the study.
        /// </summary>
        /// <returns>The new Knowledge Map</returns>
		public ejpKnowledgeMap AddKnowledgeMap()
		{
			if (this._knowledgeMaps.Count == 2)
				throw new Exception("No more than 2 knowledgemaps are allowed.");

            ejpKnowledgeMap m = new ejpKnowledgeMap(this.MetaData.Id);
			this._knowledgeMaps.Add(m);
            return m;
		}

        /// <summary>
        /// Adds a new Report to the study.
        /// </summary>
        /// <returns>The new Report</returns>
		public ejpReport AddReport()
		{
			if (this._reports.Count == 2)
				throw new Exception("No more than 2 Reports are allowed.");

            ejpReport r = new ejpReport(this._metaData.Id);
			this._reports.Add(r);
            return r;
		}

        public void RemoveReport() { /*throw new NotImplementedException();*/ }

        public void RemoveKnowledgeMap() { /*throw new NotImplementedException();*/ }

		public override string ToString()
		{
			return this._metaData.Title;
		}

		#region IDisposable Members
		public void Dispose()
		{
			foreach (ejpXpsDocument d in this._xpsDocuments)
			{
				d.Dispose();
			}
		}
		#endregion
		
		#endregion

		#region Private Methods
		
		#endregion

		#region Static Methods
		
		#endregion

	}//end: ejpStudy
}
