using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Xps.Packaging;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
	public class ejpXpsDocument : IDisposable
	{
		#region Properties with Accessors

		private List<ejpDocumentLine> _documentLines =
			new List<ejpDocumentLine>();
		public List<ejpDocumentLine> DocumentLines
		{
			get { return _documentLines; }
			set { _documentLines = value; }
		}

		private List<ejpDocumentImageBorder> _documentImageBorders =
					new List<ejpDocumentImageBorder>();
		public List<ejpDocumentImageBorder> DocumentImageBorders
		{
			get { return _documentImageBorders; }
			set { _documentImageBorders = value; }
		}

		private XpsDocument _xpsDocument;
		public XpsDocument XpsDocument
		{
			get { return _xpsDocument; }
			set { _xpsDocument = value; }
		}

		private string _xpsDocumentPath;
		public string XpsDocumentPath
		{
			get { return _xpsDocumentPath; }
			set { _xpsDocumentPath = value; }
		}

		private Uri _packageUri;
		public Uri PackageUri
		{
			get { return _packageUri; }
			set { _packageUri = value; }
		}

		private Package _xpsPackage = null; // XPS document package.
		public Package XpsPackage
		{
			get { return _xpsPackage; }
			set { _xpsPackage = value; }
		}

		private Uri _fixedDocSeqUri;
		public Uri FixedDocSeqUri
		{
			get { return _fixedDocSeqUri; }
			set { _fixedDocSeqUri = value; }
		}

		private bool _isExternalToAssignment;
		public bool IsExternalToAssignment
		{
			get { return _isExternalToAssignment; }
			set { _isExternalToAssignment = value; }
		}

		private Guid _parentStudyId;
		public Guid ParentStudyId
		{
			get { return _parentStudyId; }
			set { _parentStudyId = value; }
		}

		public Guid InternalDocumentId
		{
			get
			{
				Guid g;
				try
				{
					g = new Guid(this._xpsDocument.CoreDocumentProperties.Identifier);
					return g;
				}
				catch (ArgumentNullException)
				{
					g = Helpers.IdManipulation.GetNewGuid();
					this._xpsDocument.CoreDocumentProperties.Identifier = g.ToString();
					return g;
				}
				catch (FormatException)
				{
					return Guid.Empty;
				}
			}
		}

		#endregion

		#region Private Properties
		private readonly string _fixedDocumentSequenceContentType =
			"application/vnd.ms-package.xps-fixeddocumentsequence+xml";
		#endregion

		#region Constructors

		public ejpXpsDocument(string path, string title, bool isExternalToAssignment,
			Guid xpsDocumentId, Guid parentStudyId)
		{
			this._isExternalToAssignment = isExternalToAssignment;

			if (!File.Exists(path))
				throw new IOException(Application.Current.Resources["EX_Fnf_XPSNotFound"] as string);//Properties.Resources.EX_Fnf_XPSNotFound);

			this._xpsDocumentPath = path;
			this._packageUri = new Uri(path, UriKind.Absolute);

			try
			{
				this._xpsDocument = new XpsDocument(path, FileAccess.ReadWrite);

				FileInfo fi = new FileInfo(path);
				this._xpsDocument.CoreDocumentProperties.Title = title;
				this._xpsDocument.CoreDocumentProperties.Identifier = xpsDocumentId.ToString();

				this._xpsPackage = PackageStore.GetPackage(this._packageUri);
				if ((this._xpsPackage == null) || (this._xpsDocument == null))
					throw new Exception(Application.Current.Resources["EX_Fnf_XPSNotFound"] as string);//Properties.Resources.EX_Fnf_XPSNotFound);

				this.GetFixedDocumentSequenceUri();
			}
			catch (Exception)
			{
			}

		}// end:Constructor()

		public ejpXpsDocument(Stream data, string path,
			bool isExternalToAssignment, Guid parentStudyId)
		{
			this._isExternalToAssignment = isExternalToAssignment;
			this._xpsDocumentPath = path;
			this._packageUri = new Uri(path, UriKind.Absolute);

			data.Seek(0, SeekOrigin.Begin);
			this._xpsPackage = Package.Open(data, FileMode.Open, FileAccess.ReadWrite);

			try
			{
				PackageStore.RemovePackage(this._packageUri);
				PackageStore.AddPackage(this._packageUri, this._xpsPackage);
				this._xpsDocument = new XpsDocument(this._xpsPackage, CompressionOption.Maximum, path);
				this.GetFixedDocumentSequenceUri();
			}
			catch (Exception)
			{
			}

		}// end:Constructor()

		#endregion

		#region Public Methods

		/// <summary>
		/// Update the file package reference that this XpsDocument
		/// is loaded from.
		/// </summary>
		public void UpdatePackageReference(Stream data, string path)
		{
			this._xpsDocumentPath = path;
			this._packageUri = new Uri(path, UriKind.Absolute);

			data.Seek(0, SeekOrigin.Begin);
			this._xpsPackage = Package.Open(data, FileMode.Open, FileAccess.ReadWrite);

			try
			{
				PackageStore.RemovePackage(this._packageUri);
				PackageStore.AddPackage(this._packageUri, this._xpsPackage);
				this._xpsDocument = new XpsDocument(this._xpsPackage, CompressionOption.Maximum, path);
				this.GetFixedDocumentSequenceUri();
			}
			catch (Exception)
			{
				throw;
			}
		}

		#region IDisposable Members
		public void Dispose()
		{
			PackageStore.RemovePackage(this._packageUri);
			this._xpsDocument.Close();
		}
		#endregion
		#endregion

		#region Private Methods
		private void GetFixedDocumentSequenceUri()
		{
			// Get the Uri to the FixedDocumentSequenceUri that is needed to save annotations
			// into the document.
			foreach (PackagePart part in this._xpsPackage.GetParts())
			{
				if (part.ContentType == this._fixedDocumentSequenceContentType)
					this._fixedDocSeqUri = part.Uri;
			}
		}
		#endregion

	}//end: ejpXpsDocument
}