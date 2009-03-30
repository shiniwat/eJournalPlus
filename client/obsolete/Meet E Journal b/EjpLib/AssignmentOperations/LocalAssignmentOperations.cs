using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Net.Mime;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using SiliconStudio.Meet.EjpLib.BaseClasses;

namespace SiliconStudio.Meet.EjpLib.AssignmentOperations
{
	internal enum AssignmentSaveMode
	{
		Save,
		SaveTemp,
		SaveAs,
		Export
	}

    /// <summary>
    /// Static class with helper methods for manipulating
    /// a local Assignment.
    /// </summary>
    internal static class LocalAssignmentFileOperations
    {
        internal static Package targetPackage;
		private static String targetPackagePath;

        //holds a list of Uris to image parts that have not yet been persisted...
        private static List<string> _unsavedImageParts = new List<string>();
        #region Static Methods

		#region Saving

		public static void SaveTemporaryAssignment(BaseClasses.ejpAssignment assignment, string tempFilePath)
		{
			string realSavePath = assignment.FilePackagePath;
			assignment.FilePackagePath = tempFilePath;
			LocalAssignmentFileOperations.SaveAssignment(assignment, AssignmentSaveMode.SaveTemp);
			assignment.FilePackagePath = realSavePath;
		}

		//  --------------------------- SaveAssignment ---------------------------
		/// <summary>
		/// Save (overwrite) E Journal Plus Assignment Package with KM's and Reports.
		/// </summary>
		public static void SaveAssignment(BaseClasses.ejpAssignment assignment, AssignmentSaveMode saveMode)
		{
            try
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Information, 
							"EjpLib - Local Assignment Operations",
							"Saving Assignment "+ assignment.MetaData.Title + 
							"\nMode: " + saveMode.ToString() + 
							"\nPath: " + assignment.FilePackagePath);

                LocalAssignmentFileOperations.targetPackagePath = assignment.FilePackagePath;

                //If we have not yet loaded a package we need to do so before attempting save.
                if (LocalAssignmentFileOperations.targetPackage == null)
                    LocalAssignmentFileOperations.targetPackage = Package.Open(assignment.FilePackagePath, FileMode.OpenOrCreate);

            }
            catch (IOException ioe)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Error, 
					"EjpLib - Local Assignment Operations",
							"Saving Assignment Failed." + 
							"\nTitle: " + assignment.MetaData.Title +
							"\nMode: " + saveMode.ToString() +
							"\nPath: " + assignment.FilePackagePath +
							"\nError: " + ioe.Message);

                throw new ApplicationException("Failed to save the file in the given location.\n" +
                    "Make sure you are not trying to overwrite an assignment\n" +
                    "that is already open in another instance of E Journal Plus.");
            }

			targetPackage.PackageProperties.Created = assignment.MetaData.CreationDate;
			targetPackage.PackageProperties.Modified = assignment.MetaData.LastModifiedDate;
			targetPackage.PackageProperties.Revision = assignment.MetaData.Revision.ToString();
			targetPackage.PackageProperties.Version = assignment.MetaData.Version.ToString();
			targetPackage.PackageProperties.ContentStatus = assignment.MetaData.AssignmentContentType.ToString();
			targetPackage.PackageProperties.Description = "E Journal Plus Assignment";
			targetPackage.PackageProperties.ContentType = "E Journal Plus Assignment";

			LocalAssignmentFileOperations.ClearPackageVolitileParts(targetPackage, saveMode);

			LocalAssignmentFileOperations.AddMetaDataToPackage(targetPackage,
				PackUriHelper.CreatePartUri(new Uri("/meta_data.xml", UriKind.Relative)),
				Enumerations.AssignmentPackagePartRelationship.AssignmentMetaData_v1.ToString(),
				TargetMode.Internal, assignment.MetaData);

			bool addXpsDocuments;
			if (assignment.IsPersisted == true)
				addXpsDocuments = false;
			else
				addXpsDocuments = true;

			int counter = 1;
            
			foreach (ejpStudy studyItem in assignment.Studies)
			{
				PackagePart sp = LocalAssignmentFileOperations.AddStudyMetaDataToPackage(
					targetPackage, Enumerations.AssignmentPackagePartRelationship.StudyMetaData_v1,
					TargetMode.Internal, counter, studyItem.MetaData);
				LocalAssignmentFileOperations.AddStudyToPackage(targetPackage, sp, counter, studyItem, 
					addXpsDocuments, saveMode, assignment.FilePackagePath, true);
				counter++;
			}

			if (saveMode == AssignmentSaveMode.SaveTemp)
			{
				LocalAssignmentFileOperations.targetPackage.Close();
				LocalAssignmentFileOperations.targetPackage = null;
			}
		}//end: SaveAssignment()

		//  --------------------------- SaveAssignmentAs ---------------------------
		/// <summary>
		/// Save an E Journal Plus Assignment Package to a new location with KM's and Reports.
		/// </summary>
        public static void SaveAssignmentAs(BaseClasses.ejpAssignment assignment)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Information,
							"EjpLib - Local Assignment Operations",
							"Saving Assignment " + assignment.MetaData.Title +
							"\nPath: " + assignment.FilePackagePath);

            if (LocalAssignmentFileOperations.targetPackage == null)
                throw new ApplicationException("There is no Package to save...");

            LocalAssignmentFileOperations.targetPackagePath = assignment.FilePackagePath;

            Package newPackage = Package.Open(assignment.FilePackagePath, FileMode.Create);
            newPackage.PackageProperties.Created = assignment.MetaData.CreationDate;
            newPackage.PackageProperties.Modified = assignment.MetaData.LastModifiedDate;
            newPackage.PackageProperties.Revision = assignment.MetaData.Revision.ToString();
            newPackage.PackageProperties.Version = assignment.MetaData.Version.ToString();
            newPackage.PackageProperties.ContentStatus = assignment.MetaData.AssignmentContentType.ToString();
            newPackage.PackageProperties.Description = "E Journal Plus Assignment";
            newPackage.PackageProperties.ContentType = "E Journal Plus Assignment";

            LocalAssignmentFileOperations.AddMetaDataToPackage(newPackage,
                PackUriHelper.CreatePartUri(new Uri("/meta_data.xml", UriKind.Relative)),
                Enumerations.AssignmentPackagePartRelationship.AssignmentMetaData_v1.ToString(),
                TargetMode.Internal, assignment.MetaData);

            LocalAssignmentFileOperations.ClearPackageVolitileParts(newPackage, AssignmentSaveMode.SaveAs);

            bool addXpsDocuments;
            if (assignment.IsPersisted == true)
                addXpsDocuments = false;
            else
                addXpsDocuments = true;

            int counter = 1;
            foreach (ejpStudy studyItem in assignment.Studies)
            {
                PackagePart sp = LocalAssignmentFileOperations.AddStudyMetaDataToPackage(
                    newPackage, Enumerations.AssignmentPackagePartRelationship.StudyMetaData_v1,
                    TargetMode.Internal, counter, studyItem.MetaData);
                LocalAssignmentFileOperations.AddStudyToPackage(newPackage, sp, counter, studyItem,
                    addXpsDocuments, AssignmentSaveMode.SaveAs, assignment.FilePackagePath, true);
                counter++;
            }


            //Release the old assignment to close all filehandles. 080329
            LocalAssignmentFileOperations.ReleaseCurrentWorkingAssignment();
            //Set the current working assignment to be the newly created one. 080329
            LocalAssignmentFileOperations.targetPackage = newPackage;
        }//end: SaveAssignmentAs

		#endregion

		#region Exporting

		//  --------------------------- ExportAssignment ---------------------------
        /// <summary>
        /// Export a E Journal Plus Assignment Package with KM's and Reports.
        /// </summary>
        /// <param name="path">Location where the new Assignment Package is saved.</param>
        /// <param name="XPSDocumentTitle">Title (FileName without extension) of the first XPS file to add to the Assignment Package's First Study.</param>
        public static void ExportAssignment(BaseClasses.ejpAssignment assignment, string path)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Information,
							"EjpLib - Local Assignment Operations",
							"Exporting Assignment " + assignment.MetaData.Title +
							"\nPath: " + assignment.FilePackagePath);

            if (LocalAssignmentFileOperations.targetPackage == null)
                throw new ApplicationException("There is no Package to save...");

            LocalAssignmentFileOperations.targetPackagePath = path;

            Package newPackage = Package.Open(path, FileMode.Create);
            newPackage.PackageProperties.Created = assignment.MetaData.CreationDate;
            newPackage.PackageProperties.Modified = assignment.MetaData.LastModifiedDate;
            newPackage.PackageProperties.Revision = assignment.MetaData.Revision.ToString();
            newPackage.PackageProperties.Version = assignment.MetaData.Version.ToString();
            newPackage.PackageProperties.ContentStatus = assignment.MetaData.AssignmentContentType.ToString();
            newPackage.PackageProperties.Description = "E Journal Plus Assignment";
            newPackage.PackageProperties.ContentType = "E Journal Plus Assignment";

            LocalAssignmentFileOperations.AddMetaDataToPackage(newPackage,
                PackUriHelper.CreatePartUri(new Uri("/meta_data.xml", UriKind.Relative)),
                Enumerations.AssignmentPackagePartRelationship.AssignmentMetaData_v1.ToString(),
                TargetMode.Internal, assignment.MetaData);

            LocalAssignmentFileOperations.ClearPackageVolitileParts(newPackage, AssignmentSaveMode.Export);

            bool addXpsDocuments;
            if (assignment.IsPersisted == true)
                addXpsDocuments = false;
            else
                addXpsDocuments = true;

            int counter = 1;
            foreach (ejpStudy studyItem in assignment.Studies)
            {
                PackagePart sp = LocalAssignmentFileOperations.AddStudyMetaDataToPackage(
                    newPackage, Enumerations.AssignmentPackagePartRelationship.StudyMetaData_v1,
                    TargetMode.Internal, counter, studyItem.MetaData);
                LocalAssignmentFileOperations.AddStudyToPackage(newPackage, sp, counter, studyItem,
                    addXpsDocuments, AssignmentSaveMode.Export, assignment.FilePackagePath, false);
                counter++;
            }
            newPackage.Close();
            LocalAssignmentFileOperations.targetPackagePath = assignment.FilePackagePath;

        }//end: ExportAssignment()

        //  ------------------------- ClearPackageVolitileParts -------------------------
		/// <summary>
		/// Remove all the volitile parts of the package.
		/// </summary>
		private static void ClearPackageVolitileParts(Package targetPackage, AssignmentSaveMode saveMode)
		{
			//Loop through existing study MetaData Parts.
			List<string> reletionshipsToDelete = new List<string>();
			foreach (PackageRelationship studyMetaRel in targetPackage.GetRelationshipsByType(
				Enumerations.AssignmentPackagePartRelationship.StudyMetaData_v1))
			{
				
				PackagePart metaDataPart = targetPackage.GetPart(studyMetaRel.TargetUri);

				//Trace and delete all report parts
				foreach (PackageRelationship reportRel in metaDataPart.GetRelationshipsByType(
						Enumerations.AssignmentPackagePartRelationship.Report_v1))
				{
                    PackagePart tRepoPart = targetPackage.GetPart(reportRel.TargetUri);
					
                    foreach (PackageRelationship commentRel in tRepoPart.GetRelationshipsByType(
                        Enumerations.AssignmentPackagePartRelationship.ReportComments_v1))
                    {
                        targetPackage.DeletePart(commentRel.TargetUri);
                    }

                    targetPackage.DeletePart(reportRel.TargetUri);
				}

                //Trace and delete all report parts 
                foreach (PackageRelationship reportRel in metaDataPart.GetRelationshipsByType(
                        Enumerations.AssignmentPackagePartRelationship.Report_v1))
                {
                    targetPackage.DeletePart(reportRel.TargetUri);
                }

                //Trace and delete all Document Image Borders
                foreach (PackageRelationship dibRel in metaDataPart.GetRelationshipsByType(
                        Enumerations.AssignmentPackagePartRelationship.DocumentImageBorder_v1))
                {
                    targetPackage.DeletePart(dibRel.TargetUri);
                }

                //Trace and delete all Document Lines
                foreach (PackageRelationship dlRel in metaDataPart.GetRelationshipsByType(
                        Enumerations.AssignmentPackagePartRelationship.DocumentLine_v1))
                {
                    targetPackage.DeletePart(dlRel.TargetUri);
                }

				//Trace and delete all KnowledgeMap parts.
				foreach (PackageRelationship kmRel in metaDataPart.GetRelationshipsByType(
						Enumerations.AssignmentPackagePartRelationship.KnowledgeMap_v1))
				{
					PackagePart kmPart = targetPackage.GetPart(kmRel.TargetUri);
					//Trance and delete all shape parts in the current KnowledgeMap.
					foreach (PackageRelationship shapeRel in kmPart.GetRelationshipsByType(Enumerations.AssignmentPackagePartRelationship.Shape_v1))
					{
						PackagePart shapePart = targetPackage.GetPart(shapeRel.TargetUri);
						//Trance and delete all stroke parts in the current Shape.
						foreach (PackageRelationship strokeRel in shapePart.GetRelationshipsByType(Enumerations.AssignmentPackagePartRelationship.Stroke_v1))
							targetPackage.DeletePart(strokeRel.TargetUri);
						targetPackage.DeletePart(shapeRel.TargetUri);
					}

					//Trace and delete all connected stroke parts in the current KnowledgeMap.
					foreach (PackageRelationship connectedStrokeRel in kmPart.GetRelationshipsByType(Enumerations.AssignmentPackagePartRelationship.ConnectedStroke_v1))
					{
						PackagePart connectedStrokePart = targetPackage.GetPart(connectedStrokeRel.TargetUri);
						//Trance and delete all stroke parts in the current Shape.
						foreach (PackageRelationship strokeRel in connectedStrokePart.GetRelationshipsByType(Enumerations.AssignmentPackagePartRelationship.Stroke_v1))
							targetPackage.DeletePart(strokeRel.TargetUri);
						targetPackage.DeletePart(connectedStrokeRel.TargetUri);
					}

					//Trace and delete all image entity parts in the current KnowledgeMap.
					foreach (PackageRelationship imageEntityRel in kmPart.GetRelationshipsByType(Enumerations.AssignmentPackagePartRelationship.KnowledgeMapImageObject_v1))
					{
						targetPackage.DeletePart(imageEntityRel.TargetUri);

                        //Delete unpersisted Images... TODO: Clean up...
                        if (saveMode != AssignmentSaveMode.SaveTemp)
                        {
                            if (LocalAssignmentFileOperations._unsavedImageParts.Contains(imageEntityRel.TargetUri.OriginalString))
                            {
                                int indexToRemove = -1;
                                for (int i = 0; i < LocalAssignmentFileOperations._unsavedImageParts.Count; i++)
                                {
                                    if (LocalAssignmentFileOperations._unsavedImageParts[i].Equals(imageEntityRel.TargetUri.OriginalString))
                                        indexToRemove = i;
                                }
                                if (indexToRemove != -1)
                                    LocalAssignmentFileOperations._unsavedImageParts.RemoveAt(indexToRemove);
                            }
                        }
					}

					//Finally delete the Knowledge Map.
					targetPackage.DeletePart(kmRel.TargetUri);
				}

				//Delete the Study.
				reletionshipsToDelete.Add(studyMetaRel.Id);
				targetPackage.DeletePart(studyMetaRel.TargetUri);
			}

			//Delete all the relationships.
			foreach (string delId in reletionshipsToDelete)
				targetPackage.DeleteRelationship(delId);
		}
        
        //  ---------------------------- ClearUnsavedParts ----------------------------
        /// <summary>
        /// Deletes all the parts that were added to an Assignemnt
        /// but were never actually Saved/Persisted.
        /// </summary>
        public static void ClearUnsavedParts(Package targetPackage)
        {
            if (LocalAssignmentFileOperations._unsavedImageParts.Count == 0)
                return;

            foreach (PackageRelationship studyMetaRel in targetPackage.GetRelationshipsByType(
                Enumerations.AssignmentPackagePartRelationship.StudyMetaData_v1))
            {
                PackagePart metaDataPart = targetPackage.GetPart(studyMetaRel.TargetUri);

                List<PackageRelationship> relsToDelete = new List<PackageRelationship>();
                //Delete all images that were never persisted...
                foreach (PackageRelationship kmRel in metaDataPart.GetRelationshipsByType(
                        Enumerations.AssignmentPackagePartRelationship.KnowledgeMap_v1))
                {
                    PackagePart kmPart = targetPackage.GetPart(kmRel.TargetUri);
                    foreach (PackageRelationship imageEntityRel in kmPart.GetRelationshipsByType(
                        Enumerations.AssignmentPackagePartRelationship.KnowledgeMapImageObject_v1))
                    {
                        foreach (string t_Uri in LocalAssignmentFileOperations._unsavedImageParts)
                        {
                            if (imageEntityRel.TargetUri.OriginalString == t_Uri)
                            {
                                targetPackage.DeletePart(imageEntityRel.TargetUri);
                                relsToDelete.Add(imageEntityRel);
                            }
                        }
                    }
                    foreach (PackageRelationship rel in relsToDelete)
                    {
                        kmPart.DeleteRelationship(rel.Id);
                    }
                }
            }
        }


		//  --------------------------- AddMetaDataToPackage ---------------------------
		/// <summary>
		/// Adds the Meta data part to an Assignment Package
		/// </summary>
		private static void AddMetaDataToPackage(Package targetPackage, Uri uri,
			string relationship, TargetMode relationshipTargetMode, ejpAssignmentMetaData metaData)
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
							"Adding MetaData to Assignment " + metaData.Title);

			PackagePart p = null;

			//If the part already exists:
			if (targetPackage.PartExists(uri))
				p = targetPackage.GetPart(uri);
			else
			{
				// Add the Meta Data part
				p =
						targetPackage.CreatePart(uri,
									   MediaTypeNames.Text.Xml, CompressionOption.Maximum);
			}

			// Serialize a new meta data object into the meta data part.
			using (MemoryStream ms = new MemoryStream())
			{
				XmlSerializer xs = new XmlSerializer(typeof(BaseClasses.ejpAssignmentMetaData));
				xs.Serialize(ms, metaData);
				LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, p.GetStream());
			}// end:using ms

			// Create a Package Relationship
			targetPackage.CreateRelationship(
				p.Uri,
				relationshipTargetMode,
				relationship
				);

		}//end: AddMetaDataToPackage()


		//  --------------------------- AddStudyMetaDataToPackage ---------------------------
		/// <summary>
		/// Adds the Meta data part of a Study to an Assignment Package.
		/// </summary>
		private static PackagePart AddStudyMetaDataToPackage(Package targetPackage, string relationship,
			TargetMode relationshipTargetMode, int NewStudyId, BaseClasses.ejpStudyMetaData metaData)
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Adding Study MetaData to Assignment. Study: " + metaData.Title);

			//Add the Study Meta Data part.
			PackagePart p =
			   targetPackage.CreatePart(PackUriHelper.CreatePartUri(new Uri("S" + NewStudyId.ToString() + "/meta_data.xml", UriKind.Relative)),
				  MediaTypeNames.Text.Xml, CompressionOption.Maximum);


			// Serialize a new meta data object into the meta data part.
			using (MemoryStream ms = new MemoryStream())
			{
				XmlSerializer xs = new XmlSerializer(typeof(BaseClasses.ejpStudyMetaData));
				xs.Serialize(ms, metaData);
				LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, p.GetStream());
			}// end:using ms

			// Create a Relationship for the new Study meta data.
			targetPackage.CreateRelationship(
				p.Uri,
				relationshipTargetMode,
				relationship
				);

			return p;

		}//end: AddStudyMetaDataToPackage()


		//  --------------------------- AddStudyToPackage ---------------------------
		/// <summary>
		/// Add a Study to an Assignment Package. This function will add all the Reports, 
		/// Knowledge Maps and XPS documents off all the Study.
		/// </summary>
		private static void AddStudyToPackage(Package targetPackage, PackagePart parentMetaPart, 
			int studyId, ejpStudy study, bool addXpsDocuments, AssignmentSaveMode saveMode, string packageFilePath,
            bool UpdateXpsReferences)
		{

			SiliconStudio.DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
							"Adding the Study " + study.Title + " to assignment package.");

			int reportCounter = 1;
			foreach (ejpReport reportItem in study.Reports)
			{
				PackagePart parentReportPart = 
                    LocalAssignmentFileOperations.AddReportToPackage(targetPackage, studyId, reportCounter, 
					reportItem, Enumerations.AssignmentPackagePartRelationship.Report_v1, parentMetaPart, 
					TargetMode.Internal);

                LocalAssignmentFileOperations.AddReportCommentsToPackage(targetPackage, reportItem.Comments, 
                    studyId, reportCounter, Enumerations.AssignmentPackagePartRelationship.ReportComments_v1, 
                    parentReportPart, TargetMode.Internal);

				reportCounter++;
			}

			int kmCounter = 1;
			foreach (ejpKnowledgeMap kmItem in study.KnowledgeMaps)
			{
				LocalAssignmentFileOperations.AddKnowledgeMapToPackage(targetPackage, studyId, kmCounter,
					kmItem, Enumerations.AssignmentPackagePartRelationship.KnowledgeMap_v1, parentMetaPart,
					TargetMode.Internal);
				kmCounter++;
			}

            foreach (ejpXpsDocument xpsItem in study.XpsDocuments)
            {
                if (addXpsDocuments)
                {
                    LocalAssignmentFileOperations.AddXPSDocumentToPackage(targetPackage,
                        xpsItem, Enumerations.AssignmentPackagePartRelationship.XPSDocument_v1,
                        TargetMode.Internal, saveMode, packageFilePath, study.MetaData.Id, UpdateXpsReferences);

                    if (saveMode == AssignmentSaveMode.Save)
                        xpsItem.IsExternalToAssignment = false;
                }

                LocalAssignmentFileOperations.AddXPSDocumentLinesToPackage(targetPackage, studyId, xpsItem.InternalDocumentId,
                    xpsItem, Enumerations.AssignmentPackagePartRelationship.DocumentLine_v1,
                    parentMetaPart, TargetMode.Internal);

                LocalAssignmentFileOperations.AddXPSDocumentImageBordersToPackage(targetPackage, studyId, xpsItem.InternalDocumentId,
                    xpsItem, Enumerations.AssignmentPackagePartRelationship.DocumentImageBorder_v1,
                    parentMetaPart, TargetMode.Internal);
            }
        }//end: AddStudyToPackage()

        

        //  ------------------------ AddXPSDocumentImageBordersToPackage ------------------------
        /// <summary>
        /// Adds the documentLines ascociated with an XPS document to an Assignment Package.
        /// </summary>
        private static void AddXPSDocumentImageBordersToPackage(Package targetPackage, int studyId, Guid parentDocId, ejpXpsDocument document,
            string relationship, PackagePart parentMetaPart, TargetMode targetMode)
        {

			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Adding Xps Document Image Borders to Package. " + 
				"\nDocument is: " + document.XpsDocument.CoreDocumentProperties.Title);

            string dlUri = "/XpsDocuments/documentImageBorders-" + parentDocId.ToString() + "/";

            foreach (ejpDocumentImageBorder dib in document.DocumentImageBorders)
            {
                PackagePart p =
                        targetPackage.CreatePart(PackUriHelper.CreatePartUri(
                        new Uri(dlUri + "dib-" + dib.Id.ToString() + ".bin", UriKind.Relative)),
                        MediaTypeNames.Application.Octet, CompressionOption.Maximum);

                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, dib);
                    LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, p.GetStream());
                }// end:using ms

                // Create Package Relationship
                parentMetaPart.CreateRelationship(
                    p.Uri,
                    targetMode,
                    relationship
                    );
            }
        }//end: AddXPSDocumentImageBordersToPackage()

        //  --------------------------- AddXPSDocumentLinesToPackage ---------------------------
		/// <summary>
		/// Adds the documentLines ascociated with an XPS document to an Assignment Package.
		/// </summary>
        private static void AddXPSDocumentLinesToPackage(Package targetPackage, int studyId, Guid parentDocId, ejpXpsDocument document,
            string relationship, PackagePart parentMetaPart, TargetMode targetMode)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Adding Xps Document Lines to Package. " +
				"\nDocument is: " + document.XpsDocument.CoreDocumentProperties.Title);

            string dlUri = "/XpsDocuments/documentLines-" + parentDocId.ToString() + "/";

            foreach (ejpDocumentLine dl in document.DocumentLines)
            {
                PackagePart p =
                        targetPackage.CreatePart(PackUriHelper.CreatePartUri(
                        new Uri(dlUri + "dl-" + dl.Id.ToString() + ".bin", UriKind.Relative)),
                        MediaTypeNames.Application.Octet, CompressionOption.Maximum);

                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, dl);
                    LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, p.GetStream());
                }// end:using ms

                // Create Package Relationship
                parentMetaPart.CreateRelationship(
                    p.Uri,
                    targetMode,
                    relationship
                    );
            }
        }//end: AddXPSDocumentLinesToPackage()


		//  --------------------------- AddReportToPackage ---------------------------
		/// <summary>
		/// Adds a Report to an Assignment Package.
		/// </summary>
		private static PackagePart AddReportToPackage(Package targetPackage, int studyId, int reportId, ejpReport report,
			string relationship, PackagePart parentMetaPart, TargetMode targetMode)
		{

			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Adding Report to Package." +
				"\nReport ID is: " + reportId.ToString());

			PackagePart p =
					targetPackage.CreatePart(PackUriHelper.CreatePartUri(
					new Uri("S" + studyId.ToString() + "/reports/report" + reportId.ToString() + ".xml", UriKind.Relative)),
					MediaTypeNames.Text.Xml, CompressionOption.Maximum);

			using (MemoryStream ms = new MemoryStream())
			{
				XamlWriter.Save(report.Document, ms);
				LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, p.GetStream());
			}// end:using ms

			// Create a Package Relationship
			parentMetaPart.CreateRelationship(
				p.Uri,
				targetMode,
				relationship
				);

            return p;
		}//end: AddReportToPackage()

        private static void AddReportCommentsToPackage(Package targetPackage, List<ejpCAComment> comments, 
            int studyId, int reportId, string relationship, PackagePart parentReportPart, TargetMode targetMode)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Adding Report Comments to Package." +
				"\nReport ID is: " + reportId.ToString());

            PackagePart p =
                    targetPackage.CreatePart(PackUriHelper.CreatePartUri(
                    new Uri("S" + studyId.ToString() + "/reports/report" + reportId.ToString() + "/comments.xml", UriKind.Relative)),
                    MediaTypeNames.Text.Xml, CompressionOption.Maximum);

            // Serialize a new meta data object into the meta data part.
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<BaseClasses.ejpCAComment>));
                xs.Serialize(ms, comments);
                LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, p.GetStream());
            }// end:using ms

            // Create a Package Relationship
            parentReportPart.CreateRelationship(
                p.Uri,
                targetMode,
                relationship
                );

        }
		
		//  --------------------------- AddKnowledgeMapToPackage ---------------------------
		/// <summary>
		/// Adds a Knowledge Map to an Assignment Package.
		/// </summary>
		private static void AddKnowledgeMapToPackage(Package targetPackage, int studyId, 
            int knowledgeMapId, ejpKnowledgeMap knowledgeMap,
			string relationship, PackagePart parentMetaPart, TargetMode targetMode)
		{

			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Adding Knowledge Map to Package." +
				"\nMap ID is: " + knowledgeMapId.ToString());

            string kmUri = "S" + studyId.ToString() + "/knowledgemap" + knowledgeMapId.ToString() + "/";

            //Update Image source uris
            int imageEntCounter = 0;
            foreach (ejpKMImageEntity kmie in knowledgeMap.ImageEntities)
            {
                string kmImageUri = "";
                kmImageUri = kmUri +
                                    "/knowledgemap" + knowledgeMapId.ToString() + "/" +
                                    "image" + imageEntCounter.ToString();
                kmie.ImageSource = kmImageUri;
                imageEntCounter += 1;
            }

			PackagePart p =
					targetPackage.CreatePart(PackUriHelper.CreatePartUri(
					new Uri(kmUri + "knowledgemap" + knowledgeMapId.ToString() + ".bin", UriKind.Relative)),
					MediaTypeNames.Application.Octet, CompressionOption.Maximum);

				using (MemoryStream ms = new MemoryStream())
				{
					BinaryFormatter bf = new BinaryFormatter();
					bf.Serialize(ms, knowledgeMap);
					LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, p.GetStream());
				}// end:using ms

				// Create Package Relationship
				parentMetaPart.CreateRelationship(
					p.Uri,
					targetMode,
					relationship
					);

			//Add all the images from the KM 
			//Added 080613
			foreach (ejpKMImageEntity kmie in knowledgeMap.ImageEntities)
			{
                PackagePart iP =
						targetPackage.CreatePart(PackUriHelper.CreatePartUri(
						new Uri(kmie.ImageSource, UriKind.Relative)),
						MediaTypeNames.Application.Octet, CompressionOption.Maximum);

				LocalAssignmentFileOperations.WriteStreamToPackagePart
					(kmie.ImageStream, iP.GetStream());

				p.CreateRelationship(
					iP.Uri,
					targetMode,
					Enumerations.AssignmentPackagePartRelationship.KnowledgeMapImageObject_v1
				);
                imageEntCounter += 1;
			}


			//Add all the connected Strokes
			int connectedStrokeCounter = 0;
			int strokeCounter = 0;
			foreach (ejpKMConnectedStroke cs in knowledgeMap.ConnectedStrokes)
			{
				//Save the ConnectedStrokeObject
				PackagePart connectedStrokePart = targetPackage.CreatePart(PackUriHelper.CreatePartUri(
					new Uri(kmUri + "ConnectedStroke" + connectedStrokeCounter.ToString(), UriKind.Relative)),
					MediaTypeNames.Application.Octet, CompressionOption.Maximum);

				using (MemoryStream connStrokeMs = new MemoryStream())
				{
					BinaryFormatter bf = new BinaryFormatter();
					bf.Serialize(connStrokeMs, cs);
					LocalAssignmentFileOperations.WriteStreamToPackagePart(connStrokeMs, connectedStrokePart.GetStream());
				}// end:using ms

				p.CreateRelationship(
					connectedStrokePart.Uri,
					TargetMode.Internal,
					EjpLib.Enumerations.AssignmentPackagePartRelationship.ConnectedStroke_v1
					);
				connectedStrokeCounter++;

				//Save the ConnectedStroke Stroke
				PackagePart csP = targetPackage.CreatePart(PackUriHelper.CreatePartUri(
					new Uri(kmUri + "Stroke" + strokeCounter.ToString(), UriKind.Relative)),
					MediaTypeNames.Text.Xml, CompressionOption.Maximum);

				using (MemoryStream ms = new MemoryStream())
				{
					cs.Strokes.Save(ms, false);
					LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, csP.GetStream());
				}// end:using ms

				connectedStrokePart.CreateRelationship(
					csP.Uri,
					TargetMode.Internal,
					EjpLib.Enumerations.AssignmentPackagePartRelationship.Stroke_v1
					);

				strokeCounter++;
			}

			//Add all the Shapes
			int shapeCounter = 0;
			foreach (ejpKMShape shape in knowledgeMap.ShapeEntities)
			{
				//Save the ShapeObject
				PackagePart shapePart = targetPackage.CreatePart(PackUriHelper.CreatePartUri(
					new Uri(kmUri + "Shape" + shapeCounter.ToString(), UriKind.Relative)),
					MediaTypeNames.Application.Octet, CompressionOption.Maximum);

				using (MemoryStream shapeMs = new MemoryStream())
				{
					BinaryFormatter bf = new BinaryFormatter();
					bf.Serialize(shapeMs, shape);
					LocalAssignmentFileOperations.WriteStreamToPackagePart(shapeMs, shapePart.GetStream());
				}// end:using ms

				p.CreateRelationship(
					shapePart.Uri,
					TargetMode.Internal,
					EjpLib.Enumerations.AssignmentPackagePartRelationship.Shape_v1
					);

				shapeCounter++;

				//Save the Shape Stroke
				PackagePart sP = targetPackage.CreatePart(PackUriHelper.CreatePartUri(
					new Uri(kmUri + "Stroke" + strokeCounter.ToString(), UriKind.Relative)),
					MediaTypeNames.Text.Xml, CompressionOption.Maximum);

				using (MemoryStream ms = new MemoryStream())
				{
					shape.Strokes.Save(ms, false);
					LocalAssignmentFileOperations.WriteStreamToPackagePart(ms, sP.GetStream());
				}// end:using ms

				shapePart.CreateRelationship(
					sP.Uri,
					TargetMode.Internal,
					EjpLib.Enumerations.AssignmentPackagePartRelationship.Stroke_v1
					);

				strokeCounter++;
			}
		}//end: AddKnowledgeMapToPackage()


		//  --------------------------- AddXPSDocumentToPackage ---------------------------
		/// <summary>
		/// Adds an Xps Document to an Assignment Package.
		/// </summary>
		internal static void AddXPSDocumentToPackage(Package targetPackage, BaseClasses.ejpXpsDocument xpsDocument,
			string relationship, TargetMode targetMode, AssignmentSaveMode saveMode,
			string packageFilePath, Guid studyMetaDataId, bool UpdateXpsReferences)
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Adding XPS Document to Package." +
				"\nDocument: " + xpsDocument.XpsDocument.CoreDocumentProperties.Title + 
				"\nPackageFilePath: " + packageFilePath +
				"\nXps Document Path: " + xpsDocument.XpsDocumentPath);

            //added double check for duplicate path 080331
            PackagePart xpsDPart = null;
            try
            {
                xpsDPart =
                    targetPackage.CreatePart(PackUriHelper.CreatePartUri(
                    new Uri("/XpsDocuments/" + xpsDocument.InternalDocumentId.ToString() + ".xps", UriKind.Relative)),
                                    "application/vnd.ms-package.xps-fixedpage+xml", CompressionOption.Maximum);
            }
            catch (InvalidOperationException ex)
            {
                //If we failed because a part with this name already exists,
                //change the ID of the document and try again.
                xpsDocument.XpsDocument.CoreDocumentProperties.Identifier = 
                    Helpers.IdManipulation.GetNewGuid().ToString();
                xpsDPart =
                    targetPackage.CreatePart(PackUriHelper.CreatePartUri(
                    new Uri("/XpsDocuments/" + xpsDocument.InternalDocumentId.ToString() + ".xps", UriKind.Relative)),
                                    "application/vnd.ms-package.xps-fixedpage+xml", CompressionOption.Maximum);

				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"EjpLib - Local Assignment Operations",
					"Detected ID Collision. Attempting to change ID." +
					"\nDocument: " + xpsDocument.XpsDocument.CoreDocumentProperties.Title +
					"\nPackageFilePath: " + packageFilePath +
					"\nXps Document Path: " + xpsDocument.XpsDocumentPath + 
					"\nError:"+
					"\n" + ex.Message);

            }

			//// Copy the XPS document into the study
			if (xpsDocument.IsExternalToAssignment && saveMode != AssignmentSaveMode.Export
                && !xpsDocument.XpsDocumentPath.Contains("pack://")) //If the path contains 'pack' it must be internal... 080329
			{
				//We might have to close the Xps Document even if the document is already in a package as otherwise we 
				//might not get all the changes made to the document since it was opened in the application.
				xpsDocument.XpsDocument.Close();


				//this is to add the corretct path-parts in case the path
				//has been malformed. 080513
				bool pathIsMalformed = false; //malformed usually means local file nam only.
				string brokenPath = "";
				string rootedPath = "";
				if (!Path.IsPathRooted(xpsDocument.XpsDocumentPath))
				{
					pathIsMalformed = true;
					brokenPath = xpsDocument.XpsDocumentPath;
					try
					{
						rootedPath = Path.GetFullPath(xpsDocument.XpsDocumentPath);
						xpsDocument.XpsDocumentPath = rootedPath;
					}
					catch (SecurityException sex)
					{
						SiliconStudio.DebugManagers.DebugReporter.Report(
							SiliconStudio.DebugManagers.MessageType.Warning,
							"EjpLib - Local Assignment Operations",
							"Detected Malformed Path. Attempting to fix Path failed due to access restrictions..." +
							"\nOld Path: " + brokenPath + 
							"\nError: " + sex.Message);

						throw new SecurityException("You do not have file persmissions sufficient to fully qualify the path to the " +
							"given XPS Document. Move the file to another location and try opening it again.");
					}
				}

				if (pathIsMalformed)
				{
					SiliconStudio.DebugManagers.DebugReporter.Report(
						SiliconStudio.DebugManagers.MessageType.Warning,
						"EjpLib - Local Assignment Operations",
						"Detected Malformed Path. Path was successfully rooted." +
						"\nOld Path: " + brokenPath +
						"\nNew Path: " + rootedPath);
				}

				using (FileStream fileStream = new FileStream(
					   xpsDocument.XpsDocumentPath, FileMode.Open, FileAccess.Read))
				{
					LocalAssignmentFileOperations.WriteStreamToPackagePart(fileStream, xpsDPart.GetStream());
				}// end:using FileStream
			}
			else
			{

				string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				baseDir += @"\Meet\eJournalPlus";
				string tempXpsPath = baseDir + @"\TemporaryFiles\ejpTempXps" + DateTime.Now.Ticks.ToString() + ".xps";

				Package p = Package.Open(tempXpsPath, FileMode.Create, FileAccess.ReadWrite);
				foreach (PackagePart pp in xpsDocument.XpsPackage.GetParts())
				{
					try
					{
						if (pp.ContentType == "application/vnd.openxmlformats-package.relationships+xml"
							&& pp.Uri.ToString() != "/_rels/.rels")
							continue;

						PackagePart newPart = p.CreatePart(pp.Uri, pp.ContentType, pp.CompressionOption);
						LocalAssignmentFileOperations.WriteStreamToPackagePart(pp.GetStream(), newPart.GetStream());
						foreach (PackageRelationship pr in pp.GetRelationships())
						{
							newPart.CreateRelationship(pr.TargetUri, pr.TargetMode, pr.RelationshipType, pr.Id);
						}
					}
					catch (Exception ex)
					{
						SiliconStudio.DebugManagers.DebugReporter.Report(
							 SiliconStudio.DebugManagers.MessageType.Warning,
							 "EjpLib - Local Assignment Operations",
							 "Unhandled Exception" +
							 "\nError: " + ex.Message);
					}
				}

				p.Close();

				using (FileStream fileStream = new FileStream(
				  tempXpsPath, FileMode.Open, FileAccess.Read))
				{
					LocalAssignmentFileOperations.WriteStreamToPackagePart(fileStream, xpsDPart.GetStream());
				}// end:using FileStream
			}

			PackageRelationship xpsRel = targetPackage.CreateRelationship(
				xpsDPart.Uri,
				targetMode,
				relationship + studyMetaDataId.ToString()
				);

			//If we are savingAs:
			//HERE we need to update the ejpXpsDocument in the inmemory assignment to point to the new Url in the new package.
            if (UpdateXpsReferences == true)
            {
                xpsDocument.UpdatePackageReference(xpsDPart.GetStream(),
                    @"pack://file%3a,," + packageFilePath + xpsRel.TargetUri.OriginalString);
            }

		}//end: AddXPSDocumentToPackage()


		//  --------------------------- ImportImageFileToPackage ---------------------------
		/// <summary>
		/// Adds an Image object to a Knowledge Map in an Assignment Package.
		/// </summary>
		internal static ejpExternalImageEntityWrapper ImportImageFileToPackage(string relationship, string filepath,
			string targetKmPartUri, ejpStudy targetStudy, Guid targetKnowledgeMapId, Guid imageId)
		{
			DebugManagers.DebugReporter.Report(SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Importing Image file to Package." +
				"\nFile Path: " + filepath +
				"\nTarget Study ID: " + targetStudy.MetaData.Id.ToString() +
				"\nTarget KM ID: " + targetKnowledgeMapId.ToString());

			//clean up the uri
			string kmUri = "";
			if (targetKmPartUri.Contains(".bin"))
				kmUri = targetKmPartUri.Substring(0, targetKmPartUri.Length - 4) + "/";
			else
				kmUri = targetKmPartUri + "/";

			PackagePart imagePart =
					targetPackage.CreatePart(PackUriHelper.CreatePartUri(
					new Uri(kmUri + "Image" + imageId.ToString() + Path.GetExtension(filepath), UriKind.Relative)),
					MediaTypeNames.Application.Octet, CompressionOption.Maximum);

			using (FileStream fs = new FileStream(filepath, FileMode.Open))
			{
				LocalAssignmentFileOperations.WriteStreamToPackagePart(fs, imagePart.GetStream());
			}// end:using fs

			PackagePart iParentKMPart = null;
			PackagePart iParentStudyPart = targetPackage.GetPart(targetStudy.PackageRelationshipIDString);
			foreach (PackageRelationship kmRel in iParentStudyPart.GetRelationshipsByType(
				Enumerations.AssignmentPackagePartRelationship.KnowledgeMap_v1))
			{
				Uri PkmUri = PackUriHelper.ResolvePartUri(iParentStudyPart.Uri, kmRel.TargetUri);
				if (PkmUri.ToString() == targetKmPartUri)
				{
					iParentKMPart = targetPackage.GetPart(PkmUri);
					break;
				}
			}

			if (iParentKMPart == null)
				throw new ApplicationException("Could not Build Relationship for Image Object");

			// Create Package Relationship
			PackageRelationship imageRel = iParentKMPart.CreateRelationship(
				imagePart.Uri,
				TargetMode.Internal,
				relationship
				);

            //Fails because the WebRequest class does not support nested Pack Uris...
			//string newObjectPath =
			//	@"pack://file%253a%2C%2C" + LocalAssignmentFileOperations.targetPackagePath.Replace("\\", "%255C") + imageRel.TargetUri.OriginalString.Replace('/', ',');
			//string newObjectPath =
			//	@"pack://file%3a,," + LocalAssignmentFileOperations.targetPackagePath + imageRel.TargetUri.OriginalString;

			string newObjectPath =
				imageRel.TargetUri.OriginalString;

            LocalAssignmentFileOperations._unsavedImageParts.Add(newObjectPath);

			Stream imageStream = imagePart.GetStream();
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.DownloadCompleted += delegate
			{
				bitmapImage.StreamSource.Close();
			};
			bitmapImage.BeginInit();
			bitmapImage.StreamSource = imageStream;
			bitmapImage.EndInit();

			ejpExternalImageEntityWrapper wrapper = 
				new ejpExternalImageEntityWrapper() 
				{ 
					Source = bitmapImage, 
					SourceUri = newObjectPath 
				};

			DebugManagers.DebugReporter.ReportMethodLeave();

			return wrapper;
		}

		/// <summary>
		/// Public Method to add new xpsDocuments to an open Assignment
		/// </summary>
		internal static void ImportXpsDocumentToPackage(BaseClasses.ejpXpsDocument xpsDocument,
			string relationship, Guid studyMetaDataId)
		{

			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Importing XPS Document to Package." +
				"\nDocument: " + xpsDocument.XpsDocument.CoreDocumentProperties.Title +
				"\nXps Document Path: " + xpsDocument.XpsDocumentPath);

			//It would be best if we could check for document complience here.
			//FixedDocumentSequence seq = (FixedDocumentSequence)xpsDocument.XpsDocument.GetFixedDocumentSequence();
			//DocumentReference dref_0 = seq.References[0];

			PackagePart xpsDPart =
				LocalAssignmentFileOperations.targetPackage.CreatePart(PackUriHelper.CreatePartUri
                (new Uri("/XpsDocuments/" + xpsDocument.InternalDocumentId.ToString() + ".xps", UriKind.Relative)),
								"application/vnd.ms-package.xps-fixedpage+xml", CompressionOption.Maximum);
			
			xpsDocument.XpsDocument.Close();
			using (FileStream fileStream = new FileStream(
				   xpsDocument.XpsDocumentPath, FileMode.Open, FileAccess.Read))
			{
				LocalAssignmentFileOperations.WriteStreamToPackagePart(fileStream, xpsDPart.GetStream());
			}// end:using FileStream

			PackageRelationship xpsRel = LocalAssignmentFileOperations.targetPackage.CreateRelationship(
				xpsDPart.Uri,
				TargetMode.Internal,
				relationship + studyMetaDataId.ToString()
				);

			//HERE we need to update the ejpXpsDocument in the inmemory assignment to point to the new Url inside the package.
			xpsDocument.UpdatePackageReference(xpsDPart.GetStream(),
				@"pack://file%3a,," + LocalAssignmentFileOperations.targetPackagePath + xpsRel.TargetUri.OriginalString);

		}//end: ImportXpsDocumentToPackage


        /// <summary>
        /// Public Method to Remove xpsDocuments from an open Assignment
        /// </summary>
        internal static void RemoveXpsDocumentFromPackage(BaseClasses.ejpXpsDocument xpsDocument,
            string relationship, Guid studyMetaDataId)
        {
            //TODO: Implement

        }//end: RemoveXpsDocumentFromPackage


		//  --------------------------- WriteStreamToPackagePart ---------------------------
		/// <summary>
		/// Helper function to write data into Assignment Package parts.
		/// </summary>
		/// <param name="source">Source stream to copy data from.</param>
		/// <param name="target">Target stream to copy data to.</param>
		private static void WriteStreamToPackagePart(Stream source, Stream target)
		{
			const int bufSize = 0x1000;
			byte[] buf = new byte[bufSize];
			source.Seek(0, SeekOrigin.Begin);
			int bytesRead = 0;
			while ((bytesRead = source.Read(buf, 0, bufSize)) > 0)
				target.Write(buf, 0, bytesRead);
		}//end: WriteStreamToPackagePart()

		#endregion

		public static void ReleaseCurrentWorkingAssignment()
		{
			if (targetPackage != null)
			{
				targetPackage.Close();
				targetPackage = null;
			}
		}

		#region Importing

		/// <summary>
		/// Open an Assignment from file.
		/// </summary>
		/// <param name="path">Absolut path to the Assignment.</param>
		/// <returns></returns>
		public static ejpAssignment OpenAssignment(string path)
		{
			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Opening Assignment Package." +
				"\nPath: " + path);

			Package p;
			if (File.Exists(path))
				p = Package.Open(path, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
			else
				throw new FileNotFoundException("File Not Found.");

			ejpAssignment assignment = new ejpAssignment();

			LocalAssignmentFileOperations.ImportMetaDataFromPackage(p, assignment);
			LocalAssignmentFileOperations.ImportStudiesFromPackage(p, assignment, path);

			assignment.FilePackagePath = path;
			targetPackage = p;

            LocalAssignmentFileOperations._unsavedImageParts.Clear();

			return assignment;
		}

        /// <summary>
        /// Imports an Assignment from file.
        /// </summary>
        /// <param name="path">Absolut path to the Assignment.</param>
        /// <returns></returns>
        public static ejpAssignment ImportAssignment(string path, bool setTargetPackage)
        {

			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Importing Assignment Package." +
				"\nPath: " + path +
				"\nSetTargetPackage: " + setTargetPackage.ToString());

            Package p;
            if (File.Exists(path))
                p = Package.Open(path, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
            else
                throw new FileNotFoundException("File Not Found.");

            ejpAssignment assignment = new ejpAssignment();

            LocalAssignmentFileOperations.ImportMetaDataFromPackage(p, assignment);
            LocalAssignmentFileOperations.ImportStudiesFromPackage(p, assignment, path);

            assignment.FilePackagePath = path;

            if (setTargetPackage)
            {
                targetPackage = p;
            }

            return assignment;
        }

		//  --------------------------- ImportMetaDataFromPackage ---------------------------
		/// <summary>
		/// Extract (Deserialize) Assignment Meta Data from an Assignment Package.
		/// </summary>
		/// <param name="sourcePackage"></param>
		/// <returns></returns>
		private static void ImportMetaDataFromPackage(Package sourcePackage, ejpAssignment targetAssignment)
		{

			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
				"Importing Meta Data from Assignment Package." +
				"\nPackage: " + sourcePackage.PackageProperties.Title);

            ejpAssignmentMetaData md = null;
            try
            {
                //Resolve the package relationship to the metadata part.
                foreach (PackageRelationship rel in sourcePackage.GetRelationshipsByType(
                    Enumerations.AssignmentPackagePartRelationship.AssignmentMetaData_v1))
                {
                    //Get the part out of the package.
                    PackagePart metaDataPart = sourcePackage.GetPart(rel.TargetUri);
                    using (Stream dataStream = metaDataPart.GetStream())
                    {
                        //Deserialize the metadata part.
                        XmlSerializer xs = new XmlSerializer(typeof(ejpAssignmentMetaData));
                        md = (ejpAssignmentMetaData)xs.Deserialize(dataStream);
                    }
                }
            }
            catch (Exception ex)
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Error,
					"EjpLib - Local Assignment Operations",
					"Importing Meta Data from Assignment Package Failed." +
					"\nAttempting to add new Meta Data Part." +
					"\nPackage: " + sourcePackage.PackageProperties.Title +
					"\nError: " + ex.Message);

                md = new ejpAssignmentMetaData(Guid.NewGuid(), "Restored", -1, DateTime.Now,
                    DateTime.Now, 1, 1, SiliconStudio.Meet.EjpLib.Enumerations.AssignmentType.WorkingAssignment,
                    false, Guid.Empty);
            }
			if (md == null)
				throw new ApplicationException("Assignment Meta Data not found.");
			else
				targetAssignment.MetaData = md;
		}//end: ImportMetaDataFromPackage()

		//  --------------------------- ImportStudiesFromPackage ---------------------------
		/// <summary>
		/// Import Studies and all their parts from an Assignment Package into a target Assignment.
		/// </summary>
		/// <param name="sourcePackage">The in memory package object containing the Assignment</param>
		/// <param name="targetAssignment">The in memory Assignment to be populated with data</param>
		/// <param name="packageFilePath">The local file path to the physical Assignment on disk</param>
		private static void ImportStudiesFromPackage(Package sourcePackage, ejpAssignment targetAssignment, string packageFilePath)
		{

			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
					"Importing Studies from Assignment Package." +
					"\nPackage: " + sourcePackage.PackageProperties.Title +
					"\nTarget Assignment: " + targetAssignment.MetaData.Title +
					"\nPackage File Path: " + packageFilePath);

			//Resolve the package relationship to the metadata part.
			foreach (PackageRelationship rel in sourcePackage.GetRelationshipsByType(
				Enumerations.AssignmentPackagePartRelationship.StudyMetaData_v1))
			{
				//Get the part out of the package.
				PackagePart metaDataPart = sourcePackage.GetPart(rel.TargetUri);
				using (Stream dataStream = metaDataPart.GetStream())
				{
					//Deserialize the metadata part.
					XmlSerializer xs = new XmlSerializer(typeof(ejpStudyMetaData));
					ejpStudyMetaData md = (ejpStudyMetaData)xs.Deserialize(dataStream);

					//Add the Meta Data object to the Study
					ejpStudy study = new ejpStudy();
					study.MetaData = md;
					study.PackageRelationshipIDString = rel.TargetUri;

					foreach (PackageRelationship xpsRel in sourcePackage.GetRelationshipsByType(
						Enumerations.AssignmentPackagePartRelationship.XPSDocument_v1+md.Id.ToString()))
					{
						Uri xpsUri = PackUriHelper.ResolvePartUri(metaDataPart.Uri, xpsRel.TargetUri);
						PackagePart xpsPart = sourcePackage.GetPart(xpsUri);
						study.LoadXpsDocument(xpsPart.GetStream(FileMode.Open, FileAccess.ReadWrite), @"pack://file%3a,," + packageFilePath + xpsRel.TargetUri.OriginalString, false);
					}

					//Get the Knowledge Maps.
					foreach (PackageRelationship kmRel in metaDataPart.GetRelationshipsByType(
						Enumerations.AssignmentPackagePartRelationship.KnowledgeMap_v1))
					{
						Uri kmUri = PackUriHelper.ResolvePartUri(metaDataPart.Uri, kmRel.TargetUri);
						PackagePart kmPart = sourcePackage.GetPart(kmUri);
						LocalAssignmentFileOperations.ImportKnowledgeMap(study, kmPart, sourcePackage, packageFilePath, kmRel, kmUri);
					}

					//Get the Reports.
					foreach (PackageRelationship reportRel in metaDataPart.GetRelationshipsByType(
						Enumerations.AssignmentPackagePartRelationship.Report_v1))
					{
						Uri reportUri = PackUriHelper.ResolvePartUri(metaDataPart.Uri, reportRel.TargetUri);
						PackagePart reportPart = sourcePackage.GetPart(reportUri);
						FlowDocument d = (FlowDocument)XamlReader.Load(reportPart.GetStream());
                        ejpReport reportObject = new ejpReport(study.MetaData.Id, d);

                        List<ejpCAComment> commentStore = null;

                        //Each Report Only supports 1 comment part at this point, but we might add several
                        //later on...
                        foreach (PackageRelationship commentRel in reportPart.GetRelationshipsByType(
                            Enumerations.AssignmentPackagePartRelationship.ReportComments_v1))
                        {
                            PackagePart commentPart = sourcePackage.GetPart(commentRel.TargetUri);
                            using (Stream commentDataStream = commentPart.GetStream())
                            {
                                //Deserialize the metadata part.
                                XmlSerializer xsC = new XmlSerializer(typeof(List<ejpCAComment>));
                                commentStore = (List<ejpCAComment>)xsC.Deserialize(commentDataStream);
                            }
                        }

                        if (commentStore != null)
                            reportObject.Comments = commentStore;

                        study.ImportReport(reportObject);
					}

                    //Import the Document lines.
                    foreach (PackageRelationship dlRel in metaDataPart.GetRelationshipsByType(
                        Enumerations.AssignmentPackagePartRelationship.DocumentLine_v1))
                    {
                        Uri dlUri = PackUriHelper.ResolvePartUri(metaDataPart.Uri, dlRel.TargetUri);
                        PackagePart dlPart = sourcePackage.GetPart(dlUri);
                        LocalAssignmentFileOperations.ImportDocumentLine(study, dlPart, sourcePackage);
                    }

                    //Last, import the Document Image lines.
                    foreach (PackageRelationship dibRel in metaDataPart.GetRelationshipsByType(
                        Enumerations.AssignmentPackagePartRelationship.DocumentImageBorder_v1))
                    {
                        Uri dibUri = PackUriHelper.ResolvePartUri(metaDataPart.Uri, dibRel.TargetUri);
                        PackagePart dibPart = sourcePackage.GetPart(dibUri);
                        LocalAssignmentFileOperations.ImportDocumentImageBorder(study, dibPart, sourcePackage);
                    }

					//Add the Imported Study to the Target Assignment.
					targetAssignment.Studies.Add(study);
				}
			}

		}//end: ImportStudiesFromPackage()

        private static void ImportDocumentLine(ejpStudy targetStudy, PackagePart dlPart, Package sourcePackage)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
					"Importing Document Line" +
					"\nPackage: " + sourcePackage.PackageProperties.Title +
					"\nTarget Study: " + targetStudy.Title +
					"\nPart Uri: " + dlPart.Uri);

            //De serialize the line
            BinaryFormatter bf = new BinaryFormatter();
            ejpDocumentLine line = (ejpDocumentLine)bf.Deserialize(dlPart.GetStream());

            //Find the XpsDocument that the line belongs to.
			/* If the target study contains only one XPS document is is 
			 * safe to assume that all the lines belong to that document.
			 * This is a safetynet in case for some reason the document Id
			 * has changed and the lines no can no longer find their parent 
			 * document.
			 * 
			 * This is not a safe solution, but we have not yet been able to
			 * reproduce the case when the xps document Id is updated without 
			 * this being reflected in the related document lines...
			 * 
			 * This solution updates all the lines to point to the new doc Id
			 * that the Xps document has been assigned.
			 * 080629
			 */
			bool attemptedIdSynch = false;
			if (targetStudy.XpsDocuments.Count == 1)
			{
				if (line.ParentDocumentId != targetStudy.XpsDocuments[0].InternalDocumentId)
				{
					attemptedIdSynch = true;

					SiliconStudio.DebugManagers.DebugReporter.Report(
						SiliconStudio.DebugManagers.MessageType.Warning,
						"EjpLib - Local Assignment Operations",
							"Importing Document Line" +
							"\nThe line's parent document Id does not match the current document Id of the only Xps Document in the Study." +
							"\nAttempting to re-synch all IDs.");

					//First we need to update the references in all the 
					//text entities on the Kms.
					foreach (ejpKnowledgeMap km in targetStudy.KnowledgeMaps)
					{
						foreach (ejpKMTextEntity te in km.TextEntities)
						{
							if (te.SourceReference.DocumentId == line.ParentDocumentId)
								te.SourceReference.DocumentId = targetStudy.XpsDocuments[0].InternalDocumentId;
						}

						foreach (ejpKMImageEntity ie in km.ImageEntities)
						{
							if (ie.SourceReference.DocumentId == line.ParentDocumentId)
								ie.SourceReference.DocumentId = targetStudy.XpsDocuments[0].InternalDocumentId;
						}
					}

					//Then we update the parent document Id of the line
					line.ParentDocumentId = targetStudy.XpsDocuments[0].InternalDocumentId;

					//Last we add the line to the xps document.
					targetStudy.XpsDocuments[0].DocumentLines.Add(line);
				}
			}
			// This is the normal approach...
			if(attemptedIdSynch == false)
			{
				foreach (ejpXpsDocument document in targetStudy.XpsDocuments)
				{
					//found it
					if (document.InternalDocumentId == line.ParentDocumentId)
						document.DocumentLines.Add(line);
				}
			}
        }

        private static void ImportDocumentImageBorder(ejpStudy targetStudy, PackagePart dibPart, Package sourcePackage)
        {
			SiliconStudio.DebugManagers.DebugReporter.Report(
				SiliconStudio.DebugManagers.MessageType.Information,
				"EjpLib - Local Assignment Operations",
					"Importing Document Image Border" +
					"\nPackage: " + sourcePackage.PackageProperties.Title +
					"\nTarget Study: " + targetStudy.Title +
					"\nPart Uri: " + dibPart.Uri);

            //De serialize the line
            BinaryFormatter bf = new BinaryFormatter();
            ejpDocumentImageBorder border = (ejpDocumentImageBorder)bf.Deserialize(dibPart.GetStream());

            //Find the XpsDocument that the line belongs to.
            foreach (ejpXpsDocument document in targetStudy.XpsDocuments)
            {
                //found it
                if (document.InternalDocumentId == border.ParentDocumentId)
                    document.DocumentImageBorders.Add(border);
            }
        }

		private static void ImportKnowledgeMap(ejpStudy targetStudy, PackagePart kmPart, Package sourcePackage,
			string packagePath, PackageRelationship kmRelationShip, Uri kmUri)
		{
            try
            {
				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - Local Assignment Operations",
					"Importing Knowledge Maps" +
					"\nPackage: " + sourcePackage.PackageProperties.Title +
					"\nTarget Study: " + targetStudy.Title +
					"\nPart Uri: " + kmPart.Uri);

                BinaryFormatter bf = new BinaryFormatter();
                ejpKnowledgeMap map = (ejpKnowledgeMap)bf.Deserialize(kmPart.GetStream());
				map.PackageRelationshipIDString = kmPart.Uri.ToString();

                //Here we need to update the Image source path of all Image entities in the KM to 
                //point to the new location where the file was saved.
                foreach (ejpKMImageEntity kmie in map.ImageEntities)
                {
					//using streams instead of URIs
					foreach (PackageRelationship iRel in kmPart.GetRelationshipsByType(
						Enumerations.AssignmentPackagePartRelationship.KnowledgeMapImageObject_v1))
					{
						Uri iUri = PackUriHelper.ResolvePartUri(kmPart.Uri, iRel.TargetUri);
						if (iUri.ToString().Contains(kmie.ImageSource))
						{
							PackagePart iPart = sourcePackage.GetPart(iUri);
							//kmie.ImageStream = 
							//	iPart.GetStream(FileMode.Open, FileAccess.ReadWrite);

							Stream imageStream = iPart.GetStream();
							BitmapImage bitmapImage = new BitmapImage();
							bitmapImage.DownloadCompleted += delegate
							{
								bitmapImage.StreamSource.Close();
							};
							bitmapImage.BeginInit();
							bitmapImage.StreamSource = imageStream;
							bitmapImage.EndInit();

							kmie.BitmapImageSource = bitmapImage;

						}
					}
                }

                //Extract the Shapes.
                foreach (PackageRelationship shapeRel in kmPart.GetRelationshipsByType(
                            Enumerations.AssignmentPackagePartRelationship.Shape_v1))
                {
                    Uri shapeUri = PackUriHelper.ResolvePartUri(kmPart.Uri, shapeRel.TargetUri);
                    PackagePart shapePart = sourcePackage.GetPart(shapeUri);
                    ejpKMShape kmShape = (ejpKMShape)bf.Deserialize(shapePart.GetStream());

                    //Extract the Shape strokes.
                    foreach (PackageRelationship strokeRel in shapePart.GetRelationshipsByType(
                            Enumerations.AssignmentPackagePartRelationship.Stroke_v1))
                    {
                        Uri strokeUri = PackUriHelper.ResolvePartUri(shapePart.Uri, strokeRel.TargetUri);
                        PackagePart strokePart = sourcePackage.GetPart(strokeUri);
                        kmShape.Strokes = new System.Windows.Ink.StrokeCollection(strokePart.GetStream());
                    }
                    map.ShapeEntities.Add(kmShape);
                }

                //Extract the Connected Strokes.
                foreach (PackageRelationship connectedStrokeRel in kmPart.GetRelationshipsByType(
                            Enumerations.AssignmentPackagePartRelationship.ConnectedStroke_v1))
                {
                    Uri connectedStrokeUri = PackUriHelper.ResolvePartUri(kmPart.Uri, connectedStrokeRel.TargetUri);
                    PackagePart connectedStrokePart = sourcePackage.GetPart(connectedStrokeUri);
                    ejpKMConnectedStroke kmConnectedStroke = (ejpKMConnectedStroke)bf.Deserialize(connectedStrokePart.GetStream());

                    //Extract the Connected Stroke strokes.
                    foreach (PackageRelationship strokeRel in connectedStrokePart.GetRelationshipsByType(
                            Enumerations.AssignmentPackagePartRelationship.Stroke_v1))
                    {
                        Uri strokeUri = PackUriHelper.ResolvePartUri(connectedStrokePart.Uri, strokeRel.TargetUri);
                        PackagePart strokePart = sourcePackage.GetPart(strokeUri);
                        kmConnectedStroke.Strokes = new System.Windows.Ink.StrokeCollection(strokePart.GetStream());
                    }
                    map.ConnectedStrokes.Add(kmConnectedStroke);
                }

                targetStudy.KnowledgeMaps.Add(map);
            }
            catch (Exception ex)
            {

				SiliconStudio.DebugManagers.DebugReporter.Report(
					SiliconStudio.DebugManagers.MessageType.Information,
					"EjpLib - Local Assignment Operations",
					"Importing Knowledge Maps Failed" +
					"\nPackage: " + sourcePackage.PackageProperties.Title +
					"\nTarget Study: " + targetStudy.Title +
					"\nPart Uri: " + kmPart.Uri + 
					"\nError: " + ex.Message);

                throw new Exception("Loading Knowledge Map Failed.\n\nPerhaps this assignment was" +
                    " created with an\nearlier version of EJournal Plus?");

				

            }
		}

		#endregion

		#endregion
	}//end: LocalAssignmentFileOperations
}
