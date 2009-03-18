using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Ink;
using System.Windows.Media;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
	/// <summary>
	/// Represents a Knowledge Map inside a Study.
	/// </summary>
	[Serializable]
	public sealed class ejpKnowledgeMap : ejpStudyPart, IDeserializationCallback
	{
		#region Properties with Accessors

        ejpKMGuide _guide;
        public ejpKMGuide Guide
        {
            get { return _guide; }
            set { _guide = value; }
        }

		List<ejpKMTextEntity> _textEntities;
		public List<ejpKMTextEntity> TextEntities
		{
			get { return _textEntities; }
			set { _textEntities = value; }
		}

        List<ejpKMLabel> _labels;
        public List<ejpKMLabel> Labels
        {
            get { return _labels; }
            set { _labels = value; }
        }

        List<ejpKMImageEntity> _imageEntities;
        public List<ejpKMImageEntity> ImageEntities
        {
            get { return _imageEntities; }
            set { _imageEntities = value; }
        }

        List<ejpCAComment> _comments;
        public List<ejpCAComment> Comments
        {
            get { return _comments; }
            set { _comments = value; }
        }

		[NonSerialized]
		List<ejpKMShape> _shapeEntities;
		public List<ejpKMShape> ShapeEntities
		{
			get { return _shapeEntities; }
			set { _shapeEntities = value; }
		}

		[NonSerialized]
		List<ejpKMConnectedStroke> _connectedStrokes;
		public List<ejpKMConnectedStroke> ConnectedStrokes
		{
			get { return _connectedStrokes; }
			set { _connectedStrokes = value; }
		}

		#endregion

		#region Private Properties

		#endregion

		#region Constructors
		public ejpKnowledgeMap(Guid parentStudyId)
			: base(parentStudyId)
		{
			this._connectedStrokes = new List<ejpKMConnectedStroke>();
			this._shapeEntities = new List<ejpKMShape>();
			this._textEntities = new List<ejpKMTextEntity>();
            this._imageEntities = new List<ejpKMImageEntity>();
            this._comments = new List<ejpCAComment>();
            this._labels = new List<ejpKMLabel>();

		}//end: Constructor
		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		#endregion

		#region Static Methods

		#endregion

		#region IDeserializationCallback Members

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			this._connectedStrokes = new List<ejpKMConnectedStroke>();
			this._shapeEntities = new List<ejpKMShape>();
		}

		#endregion
	}//end: ejpKnowledgeMap

	[Serializable]
	public class ejpKMTextEntity
	{
		private Guid _id;
		public Guid Id
		{
			get { return _id; }
			set { _id = value; }
		}
		
		private string _title;
		public string Title
		{
			get { return _title; }
			set { _title = value; }
		}

		private string _body;
		public string Body
		{
			get { return _body; }
			set { _body = value; }
		}

		private double _x;
		public double X
		{
			get { return _x; }
			set { _x = value; }
		}

		private double _y;
		public double Y
		{
			get { return _y; }
			set { _y = value; }
		}

		private double _width;
		public double Width
		{
			get { return _width; }
			set { _width = value; }
		}

		private double _height;
		public double Height
		{
			get { return _height; }
			set { _height = value; }
		}

		private int _entityType;
		public int EntityType
		{
			get { return _entityType; }
			set { _entityType = value; }
		}

        private ejpSolidColor _color;
        public ejpSolidColor Color
        {
            get { return _color; }
            set { _color = value; }
        }

        private double _fontSize;
        public double FontSize
        {
            get { return _fontSize; }
            set { _fontSize = value; }
        }

        private ejpDocumentReference _sourceReference;
        public ejpDocumentReference SourceReference
        {
            get { return _sourceReference; }
            set { _sourceReference = value; }
        }

        string _commentText;
        public string CommentText
        {
            get { return _commentText; }
            set { _commentText = value; }
        }

		public ejpKMTextEntity(string body, string title, int entityType, 
            Guid id, double x, double y, double width, double height, double fontSize, 
            ejpDocumentReference sourceReference, ejpSolidColor color, string commentText)
		{
			this._body = body;
			this._title = title;
			this._id = id;
			this._x = x;
			this._y = y;
			this._width = width;
			this._height = height;
			this._entityType = entityType;
            this._color = color;
            this._sourceReference = sourceReference;
            this._fontSize = fontSize;
            this._commentText = commentText;
		}
	}

    [Serializable]
    public class ejpKMLabel
    {
        public Guid Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public ejpSolidColor Color{ get; set; }
        public string Contents { get; set; }
        public double FontSize { get; set; }

        public ejpKMLabel()
        {

        }

    }

    [Serializable]
    public class ejpKMImageEntity
    {
        /// <summary>
        /// Used only here in the Server mangled lib
        /// </summary>
        public Uri ImageBytesUri { get; set; }

        public byte[] imageBytesAsFoundInAss { get; set; }

        private Guid _id;
        public Guid Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        private double _x;
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        private double _y;
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        private double _width;
        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        private double _height;
        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }

        private int _entityType;
        public int EntityType
        {
            get { return _entityType; }
            set { _entityType = value; }
        }

        private ejpSolidColor _color;
        public ejpSolidColor Color
        {
            get { return _color; }
            set { _color = value; }
        }

        private ejpDocumentReference _sourceReference;
        public ejpDocumentReference SourceReference
        {
            get { return _sourceReference; }
            set { _sourceReference = value; }
        }

		/// <summary>
		/// within the package!
		/// </summary>
        private string _imageSource;
        public string ImageSource
        {
            get { return _imageSource; }
            set { _imageSource = value; }
        }

		[NonSerialized]
		private Stream _imageStream;
		/// <summary>
		/// This field is used to pass the image data
		/// as a stream back from the Ejp Client during
		/// save operations.
		/// </summary>
		public Stream ImageStream
		{
			get { return _imageStream; }
			set { _imageStream = value; }
		}

        private string _targetPathData;
        public string TargetPathData
        {
            get { return _targetPathData; }
            set { _targetPathData = value; }
        }

        string _commentText;
        public string CommentText
        {
            get { return _commentText; }
            set { _commentText = value; }
        }

		/// <summary>
		/// 080616
		/// </summary>
		[NonSerialized]
		ImageSource _bitmapImageSource;
		public ImageSource BitmapImageSource
		{
			get { return _bitmapImageSource; }
			set { _bitmapImageSource = value; }
		}

        public ejpKMImageEntity(string title, int entityType,
            Guid id, double x, double y, double width, double height,
            ejpDocumentReference sourceReference, ejpSolidColor color, 
            string imageSource, string targetPathData, string commentText)
        {
            this._title = title;
            this._id = id;
            this._x = x;
            this._y = y;
            this._width = width;
            this._height = height;
            this._entityType = entityType;
            this._color = color;
            this._sourceReference = sourceReference;
            this._imageSource = imageSource;
            this._targetPathData = targetPathData;
            this._commentText = commentText;
        }

		//Added support for live stream to image source.
		//080613
		public ejpKMImageEntity(string title, int entityType,
			Guid id, double x, double y, double width, double height,
			ejpDocumentReference sourceReference, ejpSolidColor color,
			string imageSource, string targetPathData, string commentText,
			Stream imageSourceStream)
		{
			this._imageStream = imageSourceStream;
			this._imageStream.Seek(0, SeekOrigin.Begin);

			this._title = title;
			this._id = id;
			this._x = x;
			this._y = y;
			this._width = width;
			this._height = height;
			this._entityType = entityType;
			this._color = color;
			this._sourceReference = sourceReference;
			this._imageSource = imageSource;
			this._targetPathData = targetPathData;
			this._commentText = commentText;
		}
    }

    [Serializable]
    public class ejpKMGuide
    {
        public string SourceUri { get; set; }
        public Guid Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }

    }

	/// <summary>
	/// Simple wrapper to circumvent the access restrictions
	/// on reading data from an already open package.
	/// </summary>
	public struct ejpExternalImageEntityWrapper
	{
		public ImageSource Source { get; set; }
		public string SourceUri { get; set; }
	}

    [Serializable]
    public class ejpCAComment
    {
        public string AuthorName { get; set; }
        public string Comment { get; set; }
        public string CommentedTextInDocument { get; set; }
        public string Date { get; set; }
        public Guid AuthorId { get; set; }
        public Guid CommentId { get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double OriginalPositionX { get; set; }
        public double OriginalPositionY { get; set; }

        public List<ejpCACommentMessage> Messages { get; set; }

        public ejpCAComment() {}
    }

    [Serializable]
    public class ejpCACommentMessage
    {
        public DateTime Date { get; set; }
        public String Message { get; set; }
        public String Author { get; set; }
        public Guid AuthorId { get; set; }
    }

    [Serializable]
    public class ejpSolidColor
    {
        public byte _a { get; set; }
        public byte _r { get; set; }
        public byte _g { get; set; }
        public byte _b { get; set; }

        public ejpSolidColor() { }
    }

	[Serializable]
	public class ejpKMConnectedStroke : IDeserializationCallback 
	{
		private Guid _sourceEntityId;
		public Guid SourceEntityId
		{
			get { return _sourceEntityId; }
			set { _sourceEntityId = value; }
		}

		private Guid _targetEntityId;
		public Guid TargetEntityId
		{
			get { return _targetEntityId; }
			set { _targetEntityId = value; }
		}

		private int _arrowMode;
		public int ArrowMode
		{
			get { return _arrowMode; }
			set { _arrowMode = value; }
		}

		[NonSerialized]
		private StrokeCollection _strokes;
		public StrokeCollection Strokes
		{
			get { return _strokes; }
			set { _strokes = value; }
		}

		public ejpKMConnectedStroke(Guid sourceEntityID, Guid targetEntityId, Stroke stroke, int arrowMode)
		{
			this._sourceEntityId = sourceEntityID;
			this._targetEntityId = targetEntityId;
			this.Strokes = new StrokeCollection();
			this.Strokes.Add(stroke);
			this._arrowMode = arrowMode;
		}

		#region IDeserializationCallback Members

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			this._strokes = new StrokeCollection();
		}

		#endregion
	}

	[Serializable]
	public class ejpKMShape : IDeserializationCallback
	{
		[NonSerialized]
		private StrokeCollection _strokes;
		public StrokeCollection Strokes
		{
			get { return _strokes; }
			set { _strokes = value; }
		}

		public ejpKMShape(Stroke stroke)
		{
			this._strokes = new StrokeCollection();
			this._strokes.Add(stroke);
		}

		#region IDeserializationCallback Members

		void IDeserializationCallback.OnDeserialization(object sender)
		{
			this._strokes = new StrokeCollection();
		}

		#endregion
	}
}
