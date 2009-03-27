/// -----------------------------------------------------------------
/// LocalAssignmentOperations.cs: Assignment operation stuff
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.IO.IsolatedStorage;
using SiliconStudio.Meet.EjpLib;
using SiliconStudio.Meet.EjpLib.AssignmentOperations;

namespace EjsWcfService.AssignmentOp
{
	internal static class LocalAssignmentOperations
	{
		/// <summary>
		/// Merges the comments of a set of assignments.
		/// Note that the list of assignments to merge also
		/// must contain the parent assignment to which all
		/// other assignments will be added.
		/// </summary>
		internal static byte[] MergeCommentsInAssignments(
			ejsSessionToken Token,
			List<string> pathsToAssignmentsToMerge,
			ejsAssignment parentAssignment)
		{

			ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': MCA (Merge).", true);

			//	instantiate IS helper stuff,
			AssignmentOperationWithStorage store = new AssignmentOperationWithStorage(sessionManager._isoStore);

			byte[] result = null;

			string pathToParentAssignment = "";

			List<SiliconStudio.Meet.EjpLib.BaseClasses.ejpAssignment> assignments =
				new List<SiliconStudio.Meet.EjpLib.BaseClasses.ejpAssignment>();

			List<SiliconStudio.Meet.EjpLib.BaseClasses.ejpStudy> studies =
				new List<SiliconStudio.Meet.EjpLib.BaseClasses.ejpStudy>();

			foreach (string path in pathsToAssignmentsToMerge)
			{
				try
				{
					ejsLogHelper.LogMessage("User '" +
						sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
						"': MCA (Merge).\n" +
						"Mergeing " + path + ".\n", false);

					SiliconStudio.Meet.EjpLib.BaseClasses.ejpAssignment a =
						//SiliconStudio.Meet.EjpLib.AssignmentOperations.LocalAssignmentFileOperations.OpenAssignment(
						//path);
						store.OpenAssignment(path);

					ejsLogHelper.LogMessage("User '" +
						sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
						"': MCA (Merge).\n" +
						"Assignment " + a.MetaData.Id + " was recreated.\n", false);

					if (a.MetaData.Id == parentAssignment.ExternalAssignmentId)
					{
						pathToParentAssignment = path;
						a.Close(false);
					}
					else
					{
						studies.AddRange(a.Studies);
					}
					a.Close(false);
				}
				catch (Exception ex)
				{
					ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': MCA (Merge).\n" +
					"Failed to Merge " + path + ".\n" +
					ex.Message, false);
				}
			}

			if (pathToParentAssignment == "")
			{
				throw new ApplicationException("Could not find Parent Assignment in the list of Assignments to Merge");
			}

			SiliconStudio.Meet.EjpLib.BaseClasses.ejpAssignment parent =
				//SiliconStudio.Meet.EjpLib.AssignmentOperations.LocalAssignmentFileOperations.OpenAssignment(
				//pathToParentAssignment);
				store.OpenAssignment(pathToParentAssignment);

			foreach (
				SiliconStudio.Meet.EjpLib.BaseClasses.ejpStudy childStudy
				in studies
				)
			{
				try
				{
					foreach (
						SiliconStudio.Meet.EjpLib.BaseClasses.ejpStudy parentStudy
						in parent.Studies
						)
					{
						if (childStudy.MetaData.Id == parentStudy.MetaData.Id)
						{
							for (int i = 0; i < childStudy.KnowledgeMaps.Count; i++)
							{
								if (childStudy.KnowledgeMaps[i].Comments != null &&
									parentStudy.KnowledgeMaps[i].Comments != null)
								{
									//Added 0901123
									bool belongsToParentCommentBox = false;
									foreach (var Ccomment in childStudy.KnowledgeMaps[i].Comments)
									{
										//Console.WriteLine(Ccomment.AuthorName);
										foreach (var Pcomment in parentStudy.KnowledgeMaps[i].Comments)
										{
											Ccomment.AuthorId = Token.UserId;
											Ccomment.AuthorName = Token.LastName + " " + Token.FirstName;
											Pcomment.AuthorId = Token.UserId;
											Pcomment.AuthorName = Token.LastName + " " + Token.FirstName;

											if (Ccomment.CommentId == Pcomment.CommentId)
											{
												foreach (var Cmess in Ccomment.Messages)
												{
													bool add = true;
													foreach (var Pmess in Pcomment.Messages)
													{
														if (Pmess.Message == Cmess.Message &&
															Pmess.AuthorId == Cmess.AuthorId)
															add = false;
													}
													if (add)
														Pcomment.Messages.Add(Cmess);

												}
												//Pcomment.Messages.AddRange(Ccomment.Messages.ToArray());
												belongsToParentCommentBox = true;
											}
										}
									}
									if (!belongsToParentCommentBox)
										parentStudy.KnowledgeMaps[i].Comments.AddRange(childStudy.KnowledgeMaps[i].Comments);
								}
							}

							for (int j = 0; j < childStudy.Reports.Count; j++)
							{
								if (childStudy.Reports[j].Comments != null &&
									parentStudy.Reports[j].Comments != null)
								{
									//Added 0901123
									bool belongsToParentCommentBox = false;
									foreach (var Ccomment in childStudy.Reports[j].Comments)
									{
										foreach (var Pcomment in parentStudy.Reports[j].Comments)
										{
											Ccomment.AuthorId = Token.UserId;
											Ccomment.AuthorName = Token.LastName + " " + Token.FirstName;
											Pcomment.AuthorId = Token.UserId;
											Pcomment.AuthorName = Token.LastName + " " + Token.FirstName;

											if (Ccomment.CommentId == Pcomment.CommentId)
											{
												foreach (var Cmess in Ccomment.Messages)
												{
													bool add = true;
													foreach (var Pmess in Pcomment.Messages)
													{
														if (Pmess.Message == Cmess.Message &&
															Pmess.AuthorId == Cmess.AuthorId)
															add = false;
													}
													if (add)
														Pcomment.Messages.Add(Cmess);

												}
												//Pcomment.Messages.AddRange(Ccomment.Messages.ToArray());
												belongsToParentCommentBox = true;
											}
										}
									}
									if (!belongsToParentCommentBox)
										parentStudy.Reports[j].Comments.AddRange(childStudy.Reports[j].Comments);
								}
							}

							for (int k = 0; k < childStudy.ReportsNV.Count; k++)
							{
								if (childStudy.ReportsNV[k].Comments != null &&
									parentStudy.ReportsNV[k].Comments != null)
								{
									//Added 0901123
									bool belongsToParentCommentBox = false;
									foreach (var Ccomment in childStudy.ReportsNV[k].Comments)
									{
										foreach (var Pcomment in parentStudy.ReportsNV[k].Comments)
										{
											Ccomment.AuthorId = Token.UserId;
											Ccomment.AuthorName = Token.LastName + " " + Token.FirstName;
											Pcomment.AuthorId = Token.UserId;
											Pcomment.AuthorName = Token.LastName + " " + Token.FirstName;

											if (Ccomment.CommentId == Pcomment.CommentId)
											{
												foreach (var Cmess in Ccomment.Messages)
												{
													bool add = true;
													foreach (var Pmess in Pcomment.Messages)
													{
														if (Pmess.Message == Cmess.Message &&
															Pmess.AuthorId == Cmess.AuthorId)
															add = false;
													}
													if (add)
														Pcomment.Messages.Add(Cmess);

												}
												//Pcomment.Messages.AddRange(Ccomment.Messages.ToArray());
												belongsToParentCommentBox = true;
											}
										}
									}

									if (!belongsToParentCommentBox)
										parentStudy.ReportsNV[k].Comments.AddRange(childStudy.ReportsNV[k].Comments);
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					ejsLogHelper.LogMessage("User '" +
						sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
						"': MCA (Merge).\n" +
						"Failed to Copy comments in " + childStudy.MetaData.Title + ".\n" +
						ex.Message, false);
				}
			}

			//090109
			try
			{
				parent.Close(true);
			}
			catch (Exception ex)
			{
				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': MCA (Merge).\n" +
					"Failed to close the Parent Assignment. Path was: " + pathToParentAssignment + ". " +
					ex.Message, false);
			}

			try
			{
				//read the bytes of the new file and return them to the caller.
				//using (FileStream fs = new FileStream(pathToParentAssignment, FileMode.Open))
				using (IsolatedStorageFileStream fs = new IsolatedStorageFileStream(pathToParentAssignment, FileMode.Open, FileAccess.Read, sessionManager._isoStore))
				{
					using (BinaryReader br = new BinaryReader(fs))
					{
						long fileSize = fs.Length;
						result = br.ReadBytes((int)fileSize);
					}
				}
			}
			catch (Exception ex)
			{
				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': MCA (Merge).\n" +
					"Failed to read the bytes of the newly created Merged Assignment. Path was: " + pathToParentAssignment + ". " +
					ex.Message, false);
			}
			if (result != null)
			{
				return result;
			}
			else
			{
				throw new ApplicationException("Failed to merge the provided Assignmets.");
			}
		}
	}
}
