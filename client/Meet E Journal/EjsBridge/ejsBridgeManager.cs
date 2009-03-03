using System;
using System.IO;
using System.ServiceModel;
using EjsBridge.ejsService;

namespace EjsBridge
{
	public static class ejsBridgeManager
	{
		/// <summary>
		/// Sets the address to the E Journal Server.
		/// The address must be a complete url including port number
		/// and endpoint name
		/// <example>http://url.com:Port/EJS/PublicServiceName</example>
		/// </summary>
		public static string EjsAddress = "";

		/// <summary>
		/// Authenticate a user with the EJS.
		/// If authentication is successful a new Session Token
		/// is returned.
		/// </summary>
		/// <param name="userName">User Name to validate.</param>
		/// <param name="password">Password to validate.</param>
		/// <param name="sourceHostId">A uniqe string that identifies this client's host. This string is per session.</param>
		/// <returns></returns>
		public static ejsSessionToken AuthenticateUser(
			string userName, string password, Guid sourceHostId)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				ejsSessionToken newToken = _client.Authenticate(userName, password, sourceHostId);
				return newToken;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Invalidate a user Token in the service token pool.
		/// </summary>
		/// <param name="tokenToInvalidate">The token to invalidate.</param>
		public static void LogOutUser(
			EjsBridge.ejsService.ejsSessionToken tokenToInvalidate)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.InvalidateToken(tokenToInvalidate);
				tokenToInvalidate._isAuthenticated = false;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				//throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Update a user password in the EJS.
		/// </summary>
		public static void UpdateUserPassword(
			string UserName, string OldPassword, string NewPassword)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.UpdateUserPassword(UserName, OldPassword, NewPassword);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Returns all the courses that a particular user has registered to.
		/// </summary>
		public static ejsCourse[] GetRegisteredCoursesForUser(
			ejsService.ejsSessionToken sessionToken, bool includeDocuments)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				ejsCourse[] results = _client.GetRegisteredCoursesForUser(sessionToken, includeDocuments);
				return results;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Returns all the courses registered in EJS.
		/// </summary>
		public static ejsCourse[] GetAllRegisteredCourses(
			ejsService.ejsSessionToken sessionToken, bool includeDisabledCourses)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				ejsCourse[] results = _client.GetAllRegisteredCourses(sessionToken, includeDisabledCourses);
				return results;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Registers a user to a course in the EJS.
		/// </summary>
		public static void RegisterUserToCourse(
			ejsService.ejsSessionToken sessionToken, ejsCourse course)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.RegisterUserToCourse(sessionToken, course);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Download the specified Course Document to the given path if possible. If not, 
		/// choose a new location and return it to the caller.
		/// </summary>
		public static string DownloadCourseDocument(
			ejsSessionToken sessionToken, string path, ejsService.ejsCourseDocument document)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);

				byte[] data = _client.GetCourseDocument(sessionToken, document);
				FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
				BinaryWriter br = new BinaryWriter(fs);
				br.Write(data);
				br.Flush();
				br.Close();
				fs.Close();

				return path;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Download the specified Course Document to the given path if possible. If not, 
		/// choose a new location and return it to the caller.
		/// </summary>
		public static int SaveAndUploadNewAssignment(
			ejsSessionToken sessionToken, string path, ejsService.ejsAssignment assignment)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);

				FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
				BinaryReader br = new BinaryReader(fs);
				long fileSize = fs.Length;
				byte[] data = br.ReadBytes((int)fs.Length);
				br.Close();
				fs.Close();
				fs.Dispose();

				assignment.DataSize = data.Length;

				int NewID = _client.SaveAndUploadAssignment(sessionToken, assignment, data);

				if (NewID == -1)
					throw new ApplicationException(Properties.Resources.EX_AsgUploadFailed);

				return NewID;

			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		public static void SaveAndUploadStudyMetaData(
			ejsSessionToken sessionToken, ejsStudyMetaData study, int parentAssignmentId)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.SaveStudyMetaData(sessionToken, study, parentAssignmentId);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		public static ejsAssignment[] GetAllPublishedAssignments(
			ejsSessionToken sessionToken, bool IncludeNotAvailable)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				ejsAssignment[] results = _client.GetAllAssignments(sessionToken, IncludeNotAvailable);

				return results;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Download a single assignment from the eJournalServer.
		/// </summary>
		public static string DownloadAssignment(
			ejsSessionToken sessionToken, string path, ejsService.ejsAssignment assignment)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);

				byte[] data = _client.GetAssignment(sessionToken, assignment);
				FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
				BinaryWriter br = new BinaryWriter(fs);
				br.Write(data);
				br.Flush();
				br.Close();
				fs.Close();

				return path;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Download a single assignment with all connected assignment comments
		/// from the eJournalServer.
		/// </summary>
		public static string DownloadCommentsMergedAssignment(
			ejsSessionToken sessionToken, string path,
			ejsService.ejsAssignment assignment, ejsService.ejsAssignment[] assignmentsToMerge)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);

				byte[] data = _client.GetCommentsMergedAssignment(sessionToken, assignment, assignmentsToMerge);
				FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
				BinaryWriter br = new BinaryWriter(fs);
				br.Write(data);
				br.Flush();
				br.Close();
				fs.Close();

				return path;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		public static void HideAssignment(ejsSessionToken sessionToken, ejsAssignment assignmentToHide)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.HideAssignment(sessionToken, assignmentToHide);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException(Properties.Resources.EX_EjsConnectionFailed);
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}
	}
}