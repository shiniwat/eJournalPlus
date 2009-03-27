/// -----------------------------------------------------------------
/// RemoteAssignmentOperations.cs: Assignment Operation stuff
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.IO;
using System.IO.IsolatedStorage;

namespace EjsWcfService.AssignmentOp
{
	internal static class RemoteAssignmentOperations
	{
		/// <summary>
		/// Get a list containing all the assignments visible to the user.
		/// </summary>
		internal static int GetAssignmentListForUser(SqlConnection dBconnection)
		{
			return 0;
		}

		/// <summary>
		/// Gets a single assignment from the database.
		/// </summary>
		internal static byte[] GetAssignment(SqlConnection dBconnection,
			ejsAssignment assignment, ejsSessionToken Token, int dataBaseId)
		{

			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				byte[] result = new byte[assignment.DataSize];

				int bufferSize = (int)assignment.DataSize;
				byte[] outbyte = new byte[bufferSize];
				MemoryStream ms = new MemoryStream(bufferSize);
				BinaryWriter bw = new BinaryWriter(ms);
				long startIndex = 0;
				long retval;

				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetAssignment";

				command.Parameters.Add("UserId", System.Data.SqlDbType.UniqueIdentifier).Value = Token.UserId;
				command.Parameters.Add("AssignmentOwnerId", System.Data.SqlDbType.Int).Value = assignment.OriginalOwnerDbId;
				command.Parameters.Add("AssignmentId", System.Data.SqlDbType.Int).Value = dataBaseId;

				reader = command.ExecuteReader(CommandBehavior.SequentialAccess);

				while (reader.Read())
				{
					retval = reader.GetBytes(0, startIndex, outbyte, 0, bufferSize);

					while (retval == bufferSize)
					{
						bw.Write(outbyte);
						bw.Flush();

						startIndex += bufferSize;
						retval = reader.GetBytes(0, startIndex, outbyte, 0, bufferSize);
					}

					bw.Write(outbyte, 0, (int)retval);
					bw.Flush();
					bw.Close();

					ms.Close();
					ms.Dispose();
				}

				result = ms.GetBuffer();
				return result;
			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}

		/// <summary>
		/// Gets the root assignment (of a CA tree) with the comments of all
		/// the child CAs merged into one file.
		/// This is currently a costly operation due to the fact that the 
		/// database was not designed to accomodate this kind of operation.
		/// </summary>
		internal static byte[] GetMergedCommentedAssignment(SqlConnection dBconnection,
			ejsAssignment assignment, ejsSessionToken Token, int dataBaseId, ejsAssignment[] childrenToBeMerged)
		{

			//Add the parentAssignment to the list of assignments
			//to download. Will seperate it out later.
			List<ejsAssignment> assignmentsToDownload =
				new List<ejsAssignment>(childrenToBeMerged);
			assignmentsToDownload.Add(assignment);

			string UserName =
				sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName;

			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			List<string> assignmentsLocalPaths = new List<string>();

			foreach (ejsAssignment child in assignmentsToDownload)
			{
				try
				{
					ejsLogHelper.LogMessage("User '" +
						UserName +
						"': MCA (DL): " + child.Title + ".", false);

					byte[] assignmentBytes = new byte[child.DataSize];

					int bufferSize = (int)child.DataSize;
					byte[] outbyte = new byte[bufferSize];
					MemoryStream ms = new MemoryStream(bufferSize);
					BinaryWriter bw = new BinaryWriter(ms);
					long startIndex = 0;
					long retval;

					command.Parameters.Clear();

					command.Connection = dBconnection;
					command.CommandType = System.Data.CommandType.StoredProcedure;
					command.CommandText = "GetAssignment";

					command.Parameters.Add("UserId", System.Data.SqlDbType.UniqueIdentifier).Value = Token.UserId;
					command.Parameters.Add("AssignmentOwnerId", System.Data.SqlDbType.Int).Value = child.OriginalOwnerDbId;
					command.Parameters.Add("AssignmentId", System.Data.SqlDbType.Int).Value = child.EJSDatabaseId;

					reader = command.ExecuteReader(CommandBehavior.SequentialAccess);

					while (reader.Read())
					{
						retval = reader.GetBytes(0, startIndex, outbyte, 0, bufferSize);

						while (retval == bufferSize)
						{
							bw.Write(outbyte);
							bw.Flush();

							startIndex += bufferSize;
							retval = reader.GetBytes(0, startIndex, outbyte, 0, bufferSize);
						}

						bw.Write(outbyte, 0, (int)retval);
						bw.Flush();
						bw.Close();
					}

					assignmentBytes = ms.GetBuffer();

					ms.Close();
					ms.Dispose();

					command.Dispose();
					if (reader != null)
					{
						reader.Close();
						reader.Dispose();
					}

					//Write the downloaded assignment to a temporary local copy
					//string path = Program.TemporaryStorageLocation + Guid.NewGuid() + ".tejp";
					//using (FileStream fs = new FileStream(
					//	path,
					//	FileMode.Create,
					//	FileAccess.Write))
					string newName = Guid.NewGuid() + ".tejp";
					if (sessionManager._isoStore == null)
					{
						sessionManager._isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.Domain | IsolatedStorageScope.Assembly | IsolatedStorageScope.User, null, null);
					}
					using (IsolatedStorageFileStream fs = new IsolatedStorageFileStream(newName, FileMode.Create, FileAccess.ReadWrite, sessionManager._isoStore))
					{
						using (BinaryWriter br = new BinaryWriter(fs))
						{
							br.Write(assignmentBytes);
							br.Flush();
							br.Close();
						}
						fs.Close();
					}
					assignmentsLocalPaths.Add(newName);//path);
				}
				catch(ArgumentException ae)
				{
					System.Diagnostics.Debug.WriteLine(ae.Message);
				}
				catch(IsolatedStorageException ex)
				{
					System.Diagnostics.Debug.WriteLine(ex.Message);
				}
				finally
				{
					command.Dispose();
					if (reader != null)
					{
						reader.Close();
						reader.Dispose();
					}
				}
			}

			byte[] result = LocalAssignmentOperations.MergeCommentsInAssignments(
				Token, assignmentsLocalPaths, assignment);

			sessionManager.ClearTempFiles();

			return result;


		}

		/// <summary>
		/// Pushes the given study into the EJS database.
		/// </summary>
		internal static void SaveStudyMetaData(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsStudyMetaData study, int parentAssignmentId)
		{

			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "UploadAndSaveStudyMetaData";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("Title", SqlDbType.NVarChar, 50).Value = study.Title;
				command.Parameters.Add("Description", System.Data.SqlDbType.NVarChar, 500).Value = study.Description;
				command.Parameters.Add("ParentAssignmentId", SqlDbType.Int).Value = parentAssignmentId;
				command.Parameters.Add("CreationDate", System.Data.SqlDbType.DateTime).Value = study.CreationDate;
				command.Parameters.Add("LastModifiedDate", System.Data.SqlDbType.DateTime).Value = study.LastModifiedDate;
				command.Parameters.Add("IsAvailable", System.Data.SqlDbType.Bit).Value = 1;
				command.Parameters.Add("CommentCount", SqlDbType.Int).Value = study.CommentCount;

				command.ExecuteNonQuery();
			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}

		/// <summary>
		/// Pushes the given assignment into the EJS database.
		/// </summary>
		internal static int SaveAndUploadAssignment(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsAssignment assignment, byte[] Data)
		{

			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "UploadAndSaveAssignment";

				if (assignment.Title.Length > 49)
					assignment.Title = assignment.Title.Substring(0, 49);
				if (assignment.Title == "")
					assignment.Title = "No Title";
				if (assignment.Description == "")
					assignment.Description = "No Description";


				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("AssignmentTitle", SqlDbType.NVarChar, 50).Value = assignment.Title;
				command.Parameters.Add("Description", System.Data.SqlDbType.NVarChar, 500).Value = assignment.Description;
				command.Parameters.Add("StudyCount", SqlDbType.Int).Value = assignment.StudyCount;
				command.Parameters.Add("Status", SqlDbType.Int).Value = assignment.AssignmentContentType;
				command.Parameters.Add("CreationDate", System.Data.SqlDbType.DateTime).Value = assignment.CreationDate;
				command.Parameters.Add("LastModifiedDate", System.Data.SqlDbType.DateTime).Value = assignment.LastModifiedDate;
				command.Parameters.Add("Version", SqlDbType.Int).Value = assignment.Version;
				command.Parameters.Add("IsAvailable", System.Data.SqlDbType.Bit).Value = 1;
				command.Parameters.Add("DataSize", System.Data.SqlDbType.BigInt).Value = Data.Length;
				command.Parameters.Add("Data", System.Data.SqlDbType.VarBinary, (int)Data.Length).Value = Data;
				command.Parameters.Add("CommentCount", SqlDbType.Int).Value = assignment.CommentCount;
				command.Parameters.Add("CourseId", SqlDbType.Int).Value = assignment.CourseId;
				command.Parameters.Add("ExternalAssignmentId", SqlDbType.UniqueIdentifier).Value = assignment.ExternalAssignmentId;
				command.Parameters.Add("ParentAssignmentId", SqlDbType.UniqueIdentifier).Value = assignment.ParentAssignmentId;

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;
				return resultCode;

			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}

		/// <summary>
		/// Get a list of all the users assignments.
		/// </summary>
		internal static void GetAllAssignments(SqlConnection dBconnection, bool IncludeNotAvailable,
			ejsSessionToken sessionToken, ref List<ejsAssignment> result)
		{
			result.Clear();
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetAllAssignmentsForUser";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("IncludeNotAvailable", SqlDbType.Bit).Value = IncludeNotAvailable;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						ejsAssignment assign = new ejsAssignment();
						assign.EJSDatabaseId = reader.GetInt32(0);
						assign.Title = reader.GetString(1);
						assign.Description = reader.GetString(2);
						assign.StudyCount = reader.GetInt32(3);
						assign.IsAvailable = reader.GetBoolean(9); //Added 080829
						//assign.OwnerUserId = sessionToken.UserId; //Removed 080318, replaced with the next line.
						assign.OwnerUserId = new Guid(reader.GetGuid(14).ToString());
						assign.OriginalOwnerDbId = reader.GetInt32(4);
						assign.AssignmentContentType = reader.GetInt32(5);
						assign.CreationDate = reader.GetDateTime(6);
						assign.LastModifiedDate = reader.GetDateTime(7);
						assign.Version = reader.GetInt32(8);
						assign.DataSize = reader.GetInt64(10);
						assign.CommentCount = reader.GetInt32(11);
						assign.IsManagedByEJournalServer = true;
						assign.OwnerName = reader.GetString(13);
						assign.CourseId = reader.GetInt32(12);
						assign.ExternalAssignmentId = new Guid(reader.GetGuid(15).ToString()); //Added 080318
						assign.ParentAssignmentId = new Guid(reader.GetGuid(16).ToString()); //Added 080318
						result.Add(assign);
					}
				}
			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}

		/// <summary>
		/// Gets all the studies for the given user.
		/// </summary>
		/// <param name="dBconnection"></param>
		/// <param name="IncludeNotAvailable"></param>
		/// <param name="sessionToken"></param>
		/// <param name="result"></param>
		internal static void GetAllStudies(SqlConnection dBconnection, bool IncludeNotAvailable,
			ejsSessionToken sessionToken, ref List<ejsStudyMetaData> result)
		{
			result.Clear();
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetAllStudiesForUser";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("IncludeNotAvailable", SqlDbType.Bit).Value = IncludeNotAvailable;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						ejsStudyMetaData study = new ejsStudyMetaData();
						study.Title = reader.GetString(1);
						study.Description = reader.GetString(2);
						study.ParentAssignmentId = reader.GetInt32(4);
						study.CreationDate = reader.GetDateTime(5);
						study.LastModifiedDate = reader.GetDateTime(6);
						study.IsAvailable = reader.GetBoolean(7);
						study.CommentCount = reader.GetInt32(8);
						result.Add(study);
					}
				}
			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}

		/// <summary>
		/// Gets all the studies asc. with the given assignment.
		/// </summary>
		internal static void GetStudiesForAssignment(SqlConnection dBconnection, ejsSessionToken sessionToken,
			ejsAssignment assignment, ref List<ejsStudyMetaData> result)
		{
			result.Clear();
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetStudiesForAssignment";

				command.Parameters.Add("UserId", SqlDbType.Int).Value = assignment.OriginalOwnerDbId;
				command.Parameters.Add("AssignmentId", SqlDbType.Int).Value = assignment.EJSDatabaseId;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						ejsStudyMetaData study = new ejsStudyMetaData();
						study.Title = reader.GetString(1);
						study.Description = reader.GetString(2);
						study.ParentAssignmentId = reader.GetInt32(4);
						study.CreationDate = reader.GetDateTime(5);
						study.LastModifiedDate = reader.GetDateTime(6);
						study.IsAvailable = reader.GetBoolean(7);
						study.CommentCount = reader.GetInt32(8);
						result.Add(study);
					}
				}
			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}

		/// <summary>
		/// Deletes the given assignment from the EJS database.
		/// </summary>
		internal static int DeleteAssignment(SqlConnection dBconnection,
			ejsSessionToken Token, ejsAssignment Assignment)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "DeleteAssignment";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = Token.UserId;
				command.Parameters.Add("AssignmentOwnerId", SqlDbType.Int).Value = Assignment.OriginalOwnerDbId;
				command.Parameters.Add("AssignmentId", SqlDbType.Int).Value = Assignment.EJSDatabaseId;

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;
				return resultCode;
			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}

		/// <summary>
		/// Hides the given assignment on the EJS database.
		/// The data is never removed but only marked as Not Available.
		/// </summary>
		internal static int HideAssignment(SqlConnection dBconnection,
			ejsSessionToken Token, ejsAssignment Assignment)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "HideAssignment";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = Token.UserId;
				command.Parameters.Add("AssignmentOwnerId", SqlDbType.Int).Value = Assignment.OriginalOwnerDbId;
				command.Parameters.Add("AssignmentId", SqlDbType.Int).Value = Assignment.EJSDatabaseId;

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;
				return resultCode;
			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}


		/// <summary>
		/// Restores the given assignment in the EJS database.
		/// </summary>
		internal static int RestoreAssignment(SqlConnection dBconnection,
			ejsSessionToken Token, ejsAssignment Assignment)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "RestoreAssignment";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = Token.UserId;
				command.Parameters.Add("AssignmentOwnerId", SqlDbType.Int).Value = Assignment.OriginalOwnerDbId;
				command.Parameters.Add("AssignmentId", SqlDbType.Int).Value = Assignment.EJSDatabaseId;

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;
				return resultCode;
			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}
	}
}
