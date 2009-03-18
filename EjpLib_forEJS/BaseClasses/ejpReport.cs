using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
	/// <summary>
	/// Represents a Report inside a Study.
	/// </summary>
	public sealed class ejpReport : ejpStudyPart
	{
		#region Properties with Accessors

		private FlowDocument _document;
		public FlowDocument Document
		{
			get { return _document; }
			set { _document = value; }
		}

        private List<ejpCAComment> _comments;
        public List<ejpCAComment> Comments
        {
            get { return _comments; }
            set { _comments = value; }
        }
        

		#endregion

		#region Private Properties

		#endregion

		#region Constructors
		public ejpReport(Guid parentStudyId)
			: base(parentStudyId)
		{
            this._comments = new List<ejpCAComment>();
			this._document = new FlowDocument();
		}//end: Constructor

		public ejpReport(Guid parentStudyId, FlowDocument document)
			: base(parentStudyId)
		{
			this._document = document;
		}//end: Constructor
		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		#endregion

		#region Static Methods

		#endregion
	
	}//end: ejpReport

    /// <summary>
    /// Represents a Report inside a Study. This is a nonVisual, it has no
    /// concept of FlowDocument, instead it stores its FD as a string.
    /// </summary>
    public sealed class ejpReportNV : ejpStudyPart
    {
        #region Properties with Accessors

        private string _document;
        public string Document
        {
            get { return _document; }
            set { _document = value; }
        }

        private List<ejpCAComment> _comments;
        public List<ejpCAComment> Comments
        {
            get { return _comments; }
            set { _comments = value; }
        }


        #endregion

        #region Private Properties

        #endregion

        #region Constructors
        public ejpReportNV(Guid parentStudyId)
            : base(parentStudyId)
        {
            this._comments = new List<ejpCAComment>();
            this._document = "";
        }//end: Constructor

        public ejpReportNV(Guid parentStudyId, string document)
            : base(parentStudyId)
        {
            this._document = document;
            this._comments = new List<ejpCAComment>();
        }//end: Constructor
        #endregion

        #region Public Methods

        #endregion

        #region Private Methods

        #endregion

        #region Static Methods

        #endregion

    }//end: ejpReport

}


