using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Packaging;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{

	/// <summary>
	/// Container class for the physical File Package + Server managed meta data fields.
	/// </summary>
	public sealed class ejpAssignment : IDisposable
	{
		#region Properties With Accessors
		private string _filePackagePath = "";
		public string FilePackagePath
		{
			get { return _filePackagePath; }
			set { _filePackagePath = value; }
		}

		private int _ejsDbId;
		public int EjsDatabaseId
		{
			get { return _ejsDbId; }
			set { _ejsDbId = value; }
		}

		//Determines wheter the assignment 
		//has been saved once or not.
		private bool _isPersisted;
		public bool IsPersisted
		{
			get { return _isPersisted; }
			set { _isPersisted = value; }
		}

		private ObservableCollection<ejpStudy> _studies;
		public ObservableCollection<ejpStudy> Studies
		{
			get { return _studies; }
			set { _studies = value; }
		}

		private ejpAssignmentMetaData _metaData;
		public ejpAssignmentMetaData MetaData
		{
			get { return _metaData; }
			set { _metaData = value; }
		}

        private string tempFilePath;
        public string TempFilePath
        {
            get { return tempFilePath; }
            set { tempFilePath = value; }
        }
		private bool _isDisposed;

		#endregion

        /// <summary>
        /// Holds a list of all the assignment objects created during 
        /// Import operations.
        /// </summary>
        private static List<ejpAssignment> _importedAssignments = new List<ejpAssignment>();

		#region Constructors

		internal ejpAssignment() 
		{
			this._studies = new ObservableCollection<ejpStudy>();
		}

		private ejpAssignment(Guid id, string title, bool isManagedByEJournalServer, Guid ownerUserId)
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment Constructor",
					"Creating new Assignment Instance" +
					"\nTitle: " + title +
					"\nIs Managed by EJS: " + isManagedByEJournalServer.ToString());

			this._metaData = new ejpAssignmentMetaData(id, title, -1, DateTime.Now, 
                DateTime.Now, 1, 0, SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.WorkingAssignment, 
                isManagedByEJournalServer, ownerUserId);
			this._studies = new ObservableCollection<ejpStudy>();
		}

		#endregion

		#region Public Methods
		public void SaveTemporaryCopy(string path)
		{
			throw new NotImplementedException();
		}

		public void Save()
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Saving Assignment" +
					"\nTitle: " + this.MetaData.Title +
					"\nPath: " + this._filePackagePath);


            EjpLib.AssignmentOperations.LocalAssignmentFileOperations.SaveAssignment(this, AssignmentOperations.AssignmentSaveMode.Save);
			this.IsPersisted = true;
		}

		public void SaveAs()
		{

			SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Saving Assignment As" +
					"\nTitle: " + this.MetaData.Title +
					"\nPath: " + this._filePackagePath);

            //holds the uris to the Xps packages before they are updated in the
            //save as operation. 080405
            //here we also need to update the km entities parent doc ID link.
            List<Uri> oldPackageStoreUris = new List<Uri>();
            foreach (ejpStudy study in this._studies)
            {
                foreach (ejpXpsDocument xpsD in study.XpsDocuments)
                {
                    oldPackageStoreUris.Add(xpsD.PackageUri);
                    xpsD.XpsDocument.CoreDocumentProperties.Identifier =
                        Helpers.IdManipulation.GetNewGuid().ToString();
                }
            }

            EjpLib.AssignmentOperations.LocalAssignmentFileOperations.SaveAssignmentAs(this);

            //remove all the old PackageUris from the Store. 080405
			foreach (Uri puri in oldPackageStoreUris)
			{
				PackageStore.RemovePackage(puri);
			}

			this.IsPersisted = true;
		}

		public void Export(string targetPath)
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Exporting Assignment" +
					"\nTitle: " + this.MetaData.Title +
					"\nPath: " + this._filePackagePath +
					"\nNew Path: " + targetPath);

			EjpLib.AssignmentOperations.LocalAssignmentFileOperations.ExportAssignment(this, targetPath);
		}

        public ejpStudy CreateNewStudy()
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Information,
						 "EjpLib - ejpAssignment",
						 "Adding new (Empty) Study");

			//Add a number to the new study title if there are already 
			//unnamed studies in the assignment.
			int nameCounter = 0;
			foreach (ejpStudy existingStudy in this._studies)
			{
                if (existingStudy.Title.Contains("ñºèÃñ¢ê›íË (")
                    || existingStudy.Title == "ñºèÃñ¢ê›íË")
					nameCounter += 1;
			}
            string newStudyTitle = "ñºèÃñ¢ê›íË";
			if (nameCounter != 0)
				newStudyTitle += " (" + nameCounter.ToString() + ")";

            ejpStudy s = new ejpStudy(this._metaData.Id, Helpers.IdManipulation.GetNewGuid(), newStudyTitle, -1);
            s.AddKnowledgeMap();
            s.AddReport();
            this._studies.Add(s);
            return s;
        }

        public void RemoveStudy(Guid idOfStudyToRemove)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Information,
						 "EjpLib - ejpAssignment",
						 "Removing Study" +
						 "\nStudy ID: " + idOfStudyToRemove.ToString());

            ejpStudy studyToRemove = new ejpStudy();
            foreach (ejpStudy study in this._studies)
            {
                if (study.MetaData.Id == idOfStudyToRemove)
                    studyToRemove = study;
            }

            foreach (ejpXpsDocument xpsD in studyToRemove.XpsDocuments)
                EjpLib.AssignmentOperations.LocalAssignmentFileOperations.RemoveXpsDocumentFromPackage(
                    xpsD, 
                    EjpLib.Enumerations.AssignmentPackagePartRelationship.XPSDocument_v1, 
                    studyToRemove.MetaData.Id);

            this._studies.Remove(studyToRemove);
        }

		internal ejpStudy CreateNewStudy(string firstXpsDocumentPath, string xpsDocumentTitle, Guid xpsDocumentId)
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(
						 SiliconStudio.DebugManagers.MessageType.Information,
						 "EjpLib - ejpAssignment",
						 "Adding new Study" +
						 "\nFirst XPS Document Path :" + firstXpsDocumentPath +
						 "\nFirst XPS Document Title :" + xpsDocumentTitle);

            ejpStudy s = new ejpStudy(this._metaData.Id, Helpers.IdManipulation.GetNewGuid(),
                "ñºèÃñ¢ê›íË", -1, firstXpsDocumentPath, true, xpsDocumentId, xpsDocumentTitle);
            s.AddReport();
            s.AddKnowledgeMap();
			this._studies.Add(s);
            return s;
		}

        public void Close(bool SaveOnClose)
        {
            try
            {
                if (SaveOnClose)
                    this.Save();
                else //080728
                {
                    if (EjpLib.AssignmentOperations.LocalAssignmentFileOperations.targetPackage != null)
                    {
                        EjpLib.AssignmentOperations.LocalAssignmentFileOperations.ClearUnsavedParts(
                            EjpLib.AssignmentOperations.LocalAssignmentFileOperations.targetPackage);
                    }
                }
            }
            catch (Exception ex) { /*ignore*/ }

            try
            {
                this.Dispose();
                this._isDisposed = true;
            }
            catch (Exception ex) { /*ignore*/ }
        }

		public void Dispose()
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Disposing Assignment" +
					"\nTitle: " + this.MetaData.Title +
					"\nPath: " + this._filePackagePath);

			if (this._isDisposed == false)
			{
				if (this._studies != null)
				{
					foreach (ejpStudy s in this._studies)
						s.Dispose();
				}
				AssignmentOperations.LocalAssignmentFileOperations.ReleaseCurrentWorkingAssignment();
                this._isDisposed = true;
			}
		}
		#endregion 

		#region Private methods
		#endregion

		#region Static methods

		#region Create new Assignment

		/// <summary>
		/// Create a new Assignment with a study based on an XPS document.
		/// </summary>
		/// <remarks>UC-36</remarks>
		/// <param name="title">Title of the Assignment.</param>
		/// <param name="firstXPSDocumentPath">Path to the first XPS document to add to the Assignment.</param>
		/// <param name="owner">The owner of this Assignment.</param>
		/// <returns>A new Assignment Object.</returns>
		public static ejpAssignment Create(
            string title, string firstXPSDocumentPath, string firstXpsDocumentTitle, 
            ejpUser owner, bool isManagedByEJournalServer,
            Guid xpsDocumentId, string tempFilePath)
		{
			try
			{

				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Creating Assignment" +
					"\nTitle: " + title +
					"\nFirst XPS Document Path: " + firstXPSDocumentPath +
					"\nTemp File Path: " + tempFilePath);

				//TODO: Improve.
				ejpAssignment a = new ejpAssignment(Helpers.IdManipulation.GetNewGuid(), title, 
                    isManagedByEJournalServer, owner.Id);
				a.CreateNewStudy(firstXPSDocumentPath, firstXpsDocumentTitle, xpsDocumentId);

				//string tempFilePath = @"c:\Windows\Temp\TempEjpPackage"+DateTime.Now.Ticks.ToString()+".ejp";
				AssignmentOperations.LocalAssignmentFileOperations.SaveTemporaryAssignment(a, tempFilePath);
				a.Dispose();

				ejpAssignment b = AssignmentOperations.LocalAssignmentFileOperations.OpenAssignment(tempFilePath);
				b.tempFilePath = tempFilePath;
				b.FilePackagePath = "";
				
				return b;
			}
			catch (Exception ex)
			{
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"EjpLib - ejpAssignment",
					"Creating Assignment Failed" +
					"\nTitle: " + title +
					"\nFirst XPS Document Path: " + firstXPSDocumentPath +
					"\nTemp File Path: " + tempFilePath +
					"\nError :" + ex.Message);

				return null;
			}
		}

        public static ejpAssignment CreateEmpty(string title, ejpStudent ownerUser, bool isManagedByEJournalServer,
            string tempFilePath)
        {
            try
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Creating Empty Assignment" +
					"\nTitle: " + title +
					"\nTemp File Path: " + tempFilePath);

                //TODO: Improve.
                ejpAssignment a = new ejpAssignment(Helpers.IdManipulation.GetNewGuid(), title, 
                    isManagedByEJournalServer, ownerUser.Id);

                AssignmentOperations.LocalAssignmentFileOperations.SaveTemporaryAssignment(a, tempFilePath);
                a.Dispose();

                ejpAssignment b = AssignmentOperations.LocalAssignmentFileOperations.ImportAssignment(tempFilePath, true);
                b.tempFilePath = tempFilePath;
                b.FilePackagePath = "";

                return b;
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"EjpLib - ejpAssignment",
					"Creating Empty Assignment Failed" +
					"\nTitle: " + title +
					"\nTemp File Path: " + tempFilePath +
					"\nError :" + ex.Message);

                return null;
            }
        }

        public static void ReleaseAllImportedAssignments()
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Releasing all touched XPS Document by emptying the application Package Store");

            try
            {
                foreach (ejpAssignment ass in ejpAssignment._importedAssignments)
                    ass.Close(false);
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"EjpLib - ejpAssignment",
					"Failed to Releas all touched XPS Document by emptying the application Package Store" +
					"\nError :" + ex.Message);
            }
        }

        public List<ejpStudy> Import(string path)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Importing an Assignment Package" +
					"\nPath: " + path);

            ejpAssignment a = AssignmentOperations.LocalAssignmentFileOperations.ImportAssignment(path, false);

            List<ejpStudy> importedStudies = new List<ejpStudy>();

            Dictionary<Guid, Guid> oldEntityIdToNewIdMapping = new Dictionary<Guid, Guid>();
            Dictionary<Guid, Guid> oldStudyIdToNewIdMapping = new Dictionary<Guid, Guid>();
            List<ejpKMTextEntity> allKMTextEntities = new List<ejpKMTextEntity>();
            List<ejpKMImageEntity> allKMImageEntities = new List<ejpKMImageEntity>();

            foreach (ejpStudy study in a.Studies)
            {
                //Update the Ids of all studies that are merged
                //more than once. Also, set all the parentId properties of all the objects
                //in the study.
                int mergedNumToTitle = 1;
                foreach (ejpStudy old_study in this._studies)
                {
                    if (old_study.IdString == study.IdString)
                        study.MetaData.Id = Helpers.IdManipulation.GetNewGuid();

                    if (study.Title == old_study.Title)
                        mergedNumToTitle += 1;
                }

                if (mergedNumToTitle != 0)
                {
                   // study.Title = study.Title + "(" + mergedNumToTitle.ToString() + ")";
                }

                //This will be the new study added to the current assignment
                ejpStudy newStudy = new ejpStudy(this._metaData.Id, study.MetaData.Id, study.Title, study.MetaData.EJSDatabaseId);

                //We also need to update all the parentObject references.
                //Dictionary<Guid, Guid> oldIdToNewIdMapping = new Dictionary<Guid, Guid>();

                //Add all the XpsDocuments
                foreach (ejpXpsDocument xpsD in study.XpsDocuments)
                {
                    Guid oldId = xpsD.InternalDocumentId;
                    Guid newId = Helpers.IdManipulation.GetNewGuid();
                    
                    xpsD.XpsDocument.CoreDocumentProperties.Identifier = newId.ToString();
                    EjpLib.AssignmentOperations.LocalAssignmentFileOperations.AddXPSDocumentToPackage(
                        SiliconStudio.Meet.EjpLib.AssignmentOperations.LocalAssignmentFileOperations.targetPackage,
                        xpsD, Enumerations.AssignmentPackagePartRelationship.XPSDocument_v1, TargetMode.Internal,
                        SiliconStudio.Meet.EjpLib.AssignmentOperations.AssignmentSaveMode.Save,
                        this._filePackagePath, newStudy.MetaData.Id, true);

                    //we have to do this twice. Once to get the package Uri right, and a second
                    //time here since the package returned by LocalAssignmentOps is a copy of the
                    //old one which still has the old Id.
                    xpsD.XpsDocument.CoreDocumentProperties.Identifier = newId.ToString();
                    newStudy.XpsDocuments.Add(xpsD);

                    //Update the references for all the lines in the document.
                    foreach (ejpDocumentLine docLine in xpsD.DocumentLines)
                    {
                        docLine.ParentDocumentId = xpsD.InternalDocumentId;
                        docLine.ParentStudyId = newStudy.MetaData.Id;
                    }

                    foreach (ejpDocumentImageBorder docImB in xpsD.DocumentImageBorders)
                    {
                        docImB.ParentDocumentId = xpsD.InternalDocumentId;
                        docImB.ParentStudyId = newStudy.MetaData.Id;
                    }

                    oldEntityIdToNewIdMapping.Add(oldId, xpsD.InternalDocumentId);
                    oldStudyIdToNewIdMapping.Add(oldId, newStudy.MetaData.Id);
                }

                //Fill the new Study with the data from the old imported one.
                foreach (ejpKnowledgeMap km in study.KnowledgeMaps)
                {
                    ejpKnowledgeMap nKm = new ejpKnowledgeMap(newStudy.MetaData.Id);
                    nKm.Id = km.Id;

                    foreach (ejpKMLabel label in km.Labels)
                        nKm.Labels.Add(label);

                    foreach (ejpKMConnectedStroke connS in km.ConnectedStrokes)
                        nKm.ConnectedStrokes.Add(connS);

                    foreach (ejpKMShape shape in km.ShapeEntities)
                        nKm.ShapeEntities.Add(shape);

                    foreach (ejpKMTextEntity textE in km.TextEntities)
                    {
                        if (textE.EntityType != 1) //1 == original to map = no source reference!
                        {
                            allKMTextEntities.Add(textE);
                            //textE.SourceReference.DocumentId = oldIdToNewIdMapping[textE.SourceReference.DocumentId];
                            //textE.SourceReference.DocumentParentStudyId = newStudy.MetaData.Id;
                        }
                        nKm.TextEntities.Add(textE);
                    }

                    foreach (ejpKMImageEntity kmi in km.ImageEntities)
                    {
                        if (kmi.EntityType != 1)
                        {
                            allKMImageEntities.Add(kmi);
                            //kmi.SourceReference.DocumentId = oldIdToNewIdMapping[kmi.SourceReference.DocumentId];
                            //kmi.SourceReference.DocumentParentStudyId = newStudy.MetaData.Id;
                        }
                        nKm.ImageEntities.Add(kmi);
                    }

                    newStudy.ImportKnowledgeMap(nKm);
                }

                foreach (ejpReport report in study.Reports)
                {
                    report.ParentStudyId = newStudy.MetaData.Id;
                    newStudy.Reports.Add(report);
                }
                
                importedStudies.Add(newStudy);
                this._studies.Add(newStudy);
            }

            //Update the sourceID references of all the KM entities.
            //We do it here because there might be references that point
            //to other-study documents, so we first have to iterate over
            //all the available xps document above to get all the IDs...
            foreach (ejpKMTextEntity textE in allKMTextEntities)
            {
                Guid key = textE.SourceReference.DocumentId;
                textE.SourceReference.DocumentId = oldEntityIdToNewIdMapping[key];
                textE.SourceReference.DocumentParentStudyId = oldStudyIdToNewIdMapping[key];
            }

            foreach (ejpKMImageEntity kmi in allKMImageEntities)
            {
                Guid key = kmi.SourceReference.DocumentId;
                kmi.SourceReference.DocumentId = oldEntityIdToNewIdMapping[key];
                kmi.SourceReference.DocumentParentStudyId = oldStudyIdToNewIdMapping[key];
            }



            ejpAssignment._importedAssignments.Add(a);
            return importedStudies;

        }//end: Import()

        private void UpdateStudyItemsParentIdReference(ejpStudy study, Guid parentStudyId)
        {

        }

		#endregion

		#region Open Assignment

		public static ejpAssignment Open(string path)
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - ejpAssignment",
					"Opening an Assignment" +
					"\nPath: " + path);

			ejpAssignment a = AssignmentOperations.LocalAssignmentFileOperations.OpenAssignment(path);
			a.IsPersisted = true;
			return a;
		}//end: Open()

        

		#endregion
		
		#endregion


        
    }//end: ejpAssignment
}
