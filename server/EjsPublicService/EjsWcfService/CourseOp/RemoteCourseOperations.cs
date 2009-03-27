/// -----------------------------------------------------------------
/// RemoteCourseOperations.cs: Course operation class
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
using EjsWcfService.UserOp;

namespace EjsWcfService.CourseOp
{
	internal static class RemoteCourseOperations
	{
		/// <summary>
		/// Returns all the courses that a particular user has registered to.
		/// </summary>
		internal static void GetRegisteredCoursesForUser(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ref List<ejsCourse> courseList)
		{
			courseList.Clear();
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetRegisteredCoursesForUser";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier);
				command.Parameters[0].Value = sessionToken.UserId;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						ejsCourse course = new ejsCourse();
						course.Id = reader.GetInt32(0);
						course.Name = reader.GetString(1);
						course.Description = reader.GetString(2);
						course.Owner = reader.GetString(3);
						course.CreationDate = reader.GetDateTime(4);
						courseList.Add(course);
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
		/// Adds a record to the Course Registrations database in the E Journal Server.
		/// </summary>
		internal static int RegisterUserToCourse(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsCourse Course)
		{
			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "RegisterUserToCourse";

				command.Parameters.Add("CourseId", System.Data.SqlDbType.Int).Value = Course.Id;
				command.Parameters.Add("UserId", System.Data.SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("RegistrationDate", System.Data.SqlDbType.DateTime).Value = DateTime.Now;

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
			}
		}

		internal static int RemoveUserFromCourse(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsUserInfo User, ejsCourse Course)
		{
			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "RemoveUserFromCourse";

				command.Parameters.Add("CourseId", System.Data.SqlDbType.Int).Value = Course.Id;
				command.Parameters.Add("UserId", System.Data.SqlDbType.UniqueIdentifier).Value = new Guid(User.Id);

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
			}
		}

		internal static int RegisterUserToCourse_adm(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsUserInfo User, ejsCourse Course)
		{
			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "RegisterUserToCourse";

				command.Parameters.Add("CourseId", System.Data.SqlDbType.Int).Value = Course.Id;
				command.Parameters.Add("UserId", System.Data.SqlDbType.UniqueIdentifier).Value = new Guid(User.Id);
				command.Parameters.Add("RegistrationDate", System.Data.SqlDbType.DateTime).Value = DateTime.Now;

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
			}
		}

		internal static int UpdateCourseRecord(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsCourse course)
		{
			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "UpdateCourseRecord";

				command.Parameters.Add("UserId", System.Data.SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("CourseId", System.Data.SqlDbType.Int).Value = course.Id;
				command.Parameters.Add("Name", System.Data.SqlDbType.NVarChar, 150).Value = course.Name;
				command.Parameters.Add("Description", System.Data.SqlDbType.NVarChar, 500).Value = course.Description;
				command.Parameters.Add("Owner", System.Data.SqlDbType.NVarChar, 200).Value = course.Owner;
				command.Parameters.Add("IsActive", System.Data.SqlDbType.Bit).Value = course.IsActive;


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
			}
		}

		internal static int DeleteCourseRecord(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsCourse course)
		{
			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "DeleteCourse";

				command.Parameters.Add("UserId", System.Data.SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("CourseId", System.Data.SqlDbType.Int).Value = course.Id;

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
			}
		}

		internal static void GetAllCourseDocuments(SqlConnection dBconnection,
			ejsSessionToken sessionToken, bool includeNotAvailable, ref List<ejsCourseDocument> result)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetAllCourseDocuments";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("IncludeNotAvailable", SqlDbType.Bit).Value = includeNotAvailable;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						ejsCourseDocument doc = new ejsCourseDocument();
						doc.Id = reader.GetInt32(0);
						doc.Name = reader.GetString(1);
						doc.Description = reader.GetString(2);
						doc.DocumentId = reader.GetGuid(3);
						doc.CreationDate = reader.GetDateTime(4);
						doc.IsAvailable = reader.GetBoolean(5);
						doc.CourseId = reader.GetInt32(6);
						result.Add(doc);
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
		/// Pulls all the CourseRegistrations records out of the EJS
		/// </summary>
		internal static void GetAllCourseRegistrations(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ref List<ejsCourseRegistration> result)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetAllCourseRegistrations";

				//command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						ejsCourseRegistration reg = new ejsCourseRegistration();
						reg.CourseId = reader.GetInt32(0);
						reg.UserId = reader.GetGuid(1).ToString();
						reg.RegisterDate = reader.GetDateTime(2);
						result.Add(reg);
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

		internal static void GetDocumentsForUserRegisteredCourses(
			SqlConnection dBconnection, ejsSessionToken sessionToken,
			ref List<ejsCourse> courses)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			//Clear all the course document lists.
			foreach (ejsCourse courseToClear in courses)
			{
				if (courseToClear.Documents == null)
					courseToClear.Documents = new List<ejsCourseDocument>();
				else
					courseToClear.Documents.Clear();
			}

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetDocumentsFromUserRegisteredCourses";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier);
				command.Parameters[0].Value = sessionToken.UserId;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					List<ejsCourseDocument> documents = new List<ejsCourseDocument>();
					while (reader.Read())
					{
						int courseId = reader.GetInt32(5);

						foreach (ejsCourse parentCourse in courses)
						{
							if (parentCourse.Id == courseId)
							{
								parentCourse.Documents.Add(new ejsCourseDocument
								{
									Id = reader.GetInt32(0),
									Name = reader.GetString(1),
									Description = reader.GetString(2),
									DocumentId = reader.GetGuid(3),
									CreationDate = reader.GetDateTime(4),
									ByteSize = reader.GetInt64(6)
								});
							}
						}
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
		/// Download a copy of a document stored in a course in the database.
		/// </summary>
		internal static byte[] GetCourseDocument(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsCourseDocument document)
		{

			SqlCommand command = null;
			SqlDataReader reader = null;

			try
			{
				byte[] result = new byte[document.ByteSize];

				int bufferSize = (int)document.ByteSize;
				byte[] outbyte = new byte[bufferSize];
				MemoryStream ms = new MemoryStream(bufferSize);
				BinaryWriter bw = new BinaryWriter(ms);
				long startIndex = 0;
				long retval;

				command = new SqlCommand("SELECT Data FROM CourseDocuments WHERE DocumentId = @Id", dBconnection);
				command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = document.DocumentId;
				command.CommandTimeout = 60;

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
		/// Deletes a Course Document from the EJS
		/// </summary>
		internal static int DeleteCourseDocument(SqlConnection dBconnection,
			ejsSessionToken Token, ejsCourseDocument document)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "DeleteCourseDocument";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = Token.UserId;
				command.Parameters.Add("DocumentId", SqlDbType.UniqueIdentifier).Value = document.DocumentId;

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
		/// Updates a Course Document record in the EJS
		/// </summary>
		internal static void UpdateCourseDocument(SqlConnection dBconnection,
			ejsSessionToken Token, ejsCourseDocument document)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "UpdateCourseDocument";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = Token.UserId;
				command.Parameters.Add("Title", System.Data.SqlDbType.NVarChar, 150).Value = document.Name;
				command.Parameters.Add("Description", System.Data.SqlDbType.NVarChar, 500).Value = document.Description;
				command.Parameters.Add("DocumentId", System.Data.SqlDbType.UniqueIdentifier).Value = document.DocumentId;
				command.Parameters.Add("IsAvailable", System.Data.SqlDbType.Bit).Value = document.IsAvailable;
				command.Parameters.Add("CourseId", SqlDbType.Int).Value = document.CourseId;

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
		/// Upload a document to a course in the E J S.
		/// </summary>
		internal static void RegisterDocumentToCourse
			(SqlConnection dBconnection, ejsSessionToken sessionToken,
			ejsCourseDocument document, int courseId, byte[] documentData)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "AddDocumentToCourse";

				command.Parameters.Add("Title", System.Data.SqlDbType.NVarChar, 150).Value = document.Name;
				command.Parameters.Add("Description", System.Data.SqlDbType.NVarChar, 500).Value = document.Description;
				command.Parameters.Add("DocumentId", System.Data.SqlDbType.UniqueIdentifier).Value = document.DocumentId;
				command.Parameters.Add("CreationDate", System.Data.SqlDbType.DateTime).Value = document.CreationDate;
				command.Parameters.Add("IsAvailable", System.Data.SqlDbType.Bit).Value = document.IsAvailable;
				command.Parameters.Add("CourseId", SqlDbType.Int).Value = courseId;
				command.Parameters.Add("Data", System.Data.SqlDbType.VarBinary, (int)documentData.Length).Value = documentData;
				command.Parameters.Add("DataSize", System.Data.SqlDbType.BigInt).Value = documentData.Length;

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
		/// Returns a list of ALL the courses currentlty registered in the server.
		/// </summary>
		internal static void GetAllRegisteredCourses(SqlConnection dBconnection,
			ejsSessionToken sessionToken, bool includeDisabledCourses, ref List<ejsCourse> result)
		{
			result.Clear();
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetAllRegisteredCourses";

				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("IncludeNotAvailable", SqlDbType.Bit).Value = 1;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						ejsCourse course = new ejsCourse();
						course.Id = reader.GetInt32(0);
						course.Name = reader.GetString(1);
						course.Description = reader.GetString(2);
						course.Owner = reader.GetString(3);
						course.CreationDate = reader.GetDateTime(4);
						course.IsActive = reader.GetBoolean(5);
						result.Add(course);
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
		/// Register the given ejsCourse object as a new course in the 
		/// E Journal Server Courses database.
		/// </summary>
		internal static int RegisterNewCourse(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsCourse newCourse)
		{
			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "RegisterNewCourse";

				command.Parameters.Add("Name", System.Data.SqlDbType.NVarChar, 150).Value = newCourse.Name;
				command.Parameters.Add("Description", System.Data.SqlDbType.NVarChar, 500).Value = newCourse.Description;
				command.Parameters.Add("Owner", System.Data.SqlDbType.NVarChar, 200).Value = newCourse.Owner;
				command.Parameters.Add("CreationDate", System.Data.SqlDbType.DateTime).Value = newCourse.CreationDate;
				command.Parameters.Add("IsActive", System.Data.SqlDbType.Bit).Value = newCourse.IsActive;

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
