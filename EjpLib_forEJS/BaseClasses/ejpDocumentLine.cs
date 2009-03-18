using System;
using System.Collections.Generic;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
    [Serializable]
    public class ejpDocumentLine
    {
        private ejpDocumentLinePart _start;
        public ejpDocumentLinePart Start
        {
            get { return _start; }
            set
            {
                _start = value;
            }
        }

        private ejpDocumentLinePart _end;
        public ejpDocumentLinePart End
        {
            get { return _end; }
            set
            {
                _end = value;
            }
        }

        private List<ejpDocumentLinePart> _lineParts = new List<ejpDocumentLinePart>();
        public List<ejpDocumentLinePart> LineParts
        {
            get { return _lineParts; }
            set { this._lineParts = value; }
        }

        private ejpDocumentLineComment _lineComment;
        public ejpDocumentLineComment LineComment
        {
            get { return _lineComment; }
            set { _lineComment = value; }
        }

        public bool HasComment { get; set; }
        public int LineType { get; set; }
        public Guid ParentDocumentId { get; set; }
        public Guid ParentStudyId { get; set; }
        public int StartPageNumber { get; set; }
        private ejpSolidColor _color;
        public ejpSolidColor Color
        {
            get { return this._color; }
            set { this._color = value; }
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

        public ejpDocumentLine()
        {

        }
    }

    [Serializable]
    public class ejpDocumentLineComment
    {
        private string _content;
        public string Content
        {
            get
            {
                if (this._content != null)
                    return _content;
                else
                    return "No Comment yet.";
            }
            set { _content = value; }
        }

        public string DateAdded { get; set; }
        public string Author { get; set; }
        public Guid AuthorId { get; set; }
        public Guid ParentDocumentLineId { get; set; }
        public Guid ParentDocumentId { get; set; }
        public Guid ParentStudyId { get; set; }

        public ejpDocumentLineComment()
        {

        }
    }

    [Serializable]
    public class ejpDocumentLinePart
    {
        public int PageNumber { get; set; }

        public double Width { get; set; }
        public double BoxHeight { get; set; } 
        public double BoxTop { get; set; }
        public double BoxLeft { get; set; }
        public double LineLeft { get; set; }
        public double LineTop { get; set; }

        public Guid ParentPenLineId { get; set; }
        public int Position { get; set; }
        public double OffsetStart { get; set; }
        public string ParentGlyphName { get; set; }

        public ejpDocumentLinePart()
        {

        }
    }
}
