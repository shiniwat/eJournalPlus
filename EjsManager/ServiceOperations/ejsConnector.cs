using System;
using System.Collections.Generic;
using System.Text;
using SiliconStudio.Meet.EjsManager.ejsServiceReference;
using System.ServiceModel;

namespace SiliconStudio.Meet.EjsManager.ServiceOperations
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

		#region User Op

		/// <summary>
		/// Authenticate a user with the EJS.
		/// If authentication is successful a new Session Token
		/// is returned.
		/// </summary>
		/// <param name="userName">User Name to validate.</param>
		/// <param name="password">Password to validate.</param>
		/// <param name="sourceHostId">A uniqe string that identifies this client's host. This string is per session.</param>
		/// <returns>A Token that contains the negotiated login data.</returns>
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
				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				throw new ApplicationException("EJSと接続する際に失敗しました。");
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
			ejsSessionToken tokenToInvalidate)
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
				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				//throw new ApplicationException("EJSと接続する際に失敗しました。");
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
				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

        /// <summary>
        /// Deletes the record of single User in the eJournalServer
        /// </summary>
        public static void DeleteUser(ejsSessionToken sessionToken, ejsUserInfo userToDelete)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                _client.DeleteUserRecord(sessionToken, userToDelete);
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                sessionToken._isAuthenticated = false;
                throw new ApplicationException("EJSと接続する際に失敗しました。");
            }
            finally
            {
                if (_client != null)
                    _client.Close();
            }
        }

        /// <summary>
        /// Registers a new User in the eJournalServer
        /// </summary>
        public static void AddNewUser(ejsSessionToken sessionToken, ejsUserInfo newUser,
            int userGroup, string password)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                _client.RegisterNewUser(sessionToken, newUser, newUser.UserName, password, userGroup, newUser.IsAccountActive);
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                sessionToken._isAuthenticated = false;
                throw new ApplicationException("EJSと接続する際に失敗しました。");
            }
            finally
            {
                if (_client != null)
                    _client.Close();
            }
        }

        /// <summary>
        /// Updates the record of single User in the eJournalServer
        /// </summary>
        public static void UpdateUser(ejsSessionToken sessionToken,
            ejsUserInfo userToUpdate, string newPassword)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                _client.UpdateUserRecord(sessionToken, userToUpdate, newPassword);
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                sessionToken._isAuthenticated = false;
                throw new ApplicationException("EJSと接続する際に失敗しました。");
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
			ejsSessionToken sessionToken, bool includeDocuments)
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

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Gets all the user records from the eJournalServer
		/// </summary>
		public static ejsUserInfo[] GetAllUserRecords(
			ejsSessionToken sessionToken)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);

				//3 == regular users
				//-1 == all users
				ejsUserInfo[] results = _client.GetAllRegisteredUsers(sessionToken, -1);

				return results;
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		#endregion

		#region Course Op

		/// <summary>
		/// Adds a course to the EJS
		/// </summary>
		public static void AddNewCourse(ejsSessionToken sessionToken,
			ejsCourse course)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.RegisterNewCourse(sessionToken, course);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
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
			ejsSessionToken sessionToken, bool includeDisabledCourses)
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

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

        /// <summary>
        /// Adds a course document to a course in the EJS
        /// </summary>
        public static void AddDocumentToCourse(ejsSessionToken sessionToken,
            ejsCourseDocument document, int courseId, byte[] data)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                _client.AddDocumentToCourse(sessionToken, document, courseId, data);
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                sessionToken._isAuthenticated = false;
                throw new ApplicationException("EJSと接続する際に失敗しました。");
            }
            finally
            {
                if (_client != null)
                    _client.Close();
            }
        }

        /// <summary>
        /// Returns all the courses documents registered in EJS.
        /// </summary>
        public static ejsCourseDocument[] GetAllCourseDocuments(
            ejsSessionToken sessionToken, bool includeDisabledDocuments)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                ejsCourseDocument[] results = _client.GetAllCourseDocuments(sessionToken, includeDisabledDocuments);
                return results;
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                throw new ApplicationException("EJSと接続する際に失敗しました。");
            }
            finally
            {
                if (_client != null)
                    _client.Close();
            }
        }

        /// <summary>
        /// Deletes a single course document from the eJournalServer
        /// </summary>
        public static void DeleteCourseDocument(ejsSessionToken sessionToken,
            ejsCourseDocument documentToDelete)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                _client.DeleteCourseDocument(sessionToken, documentToDelete);
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                sessionToken._isAuthenticated = false;
                throw new ApplicationException("EJSと接続する際に失敗しました。");
            }
            finally
            {
                if (_client != null)
                    _client.Close();
            }
        }

        /// <summary>
        /// Updates the record of single course document in the eJournalServer
        /// </summary>
        public static void UpdateCourseDocument(ejsSessionToken sessionToken,
            ejsCourseDocument documentToUpdate)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                _client.UpdateCourseDocument(sessionToken, documentToUpdate);
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                sessionToken._isAuthenticated = false;
                throw new ApplicationException("EJSと接続する際に失敗しました。");
            }
            finally
            {
                if (_client != null)
                    _client.Close();
            }
        }

		/// <summary>
		/// Updates the record of a single course in the eJournalServer
		/// </summary>
		public static void UpdateCourse(ejsSessionToken sessionToken,
			ejsCourse courseToUpdate)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.UpdateCourseRecord(sessionToken, courseToUpdate);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Deletes the record of a single course in the eJournalServer
		/// </summary>
		public static void DeleteCourse(ejsSessionToken sessionToken,
			ejsCourse courseToDelete)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.DeleteCourseRecord(sessionToken, courseToDelete);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

        /// <summary>
        /// Returns all the course registrations in the EJS.
        /// </summary>
        public static ejsCourseRegistration[] GetAllRegisteredCourseRegistrations(
            ejsSessionToken sessionToken)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                ejsCourseRegistration[] results = 
                    _client.GetAllRegisteredCourseRegistrations(sessionToken);
                return results;
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                throw new ApplicationException("EJSと接続する際に失敗しました。");
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
		public static void RegisterUserToCourse_adm(
			ejsSessionToken sessionToken, ejsUserInfo userInfo, ejsCourse course)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.RegisterUserToCourse_adm(sessionToken, userInfo, course);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Removes a user from a course in the EJS.
		/// </summary>
		public static void RemoveUserFromCourse(
			ejsSessionToken sessionToken, ejsUserInfo userInfo, ejsCourse course)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
				_client.RemoveUserFromCourse(sessionToken, userInfo, course);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		#endregion

		#region Assignment Op

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

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Deletes a single Assignment from the eJournalServer
		/// </summary>
		public static void DeleteAssignment(ejsSessionToken sessionToken, 
			ejsAssignment assignmentToDelete)
		{
			EjsPublicServiceClient _client = null;
			try
			{
				_client = new EjsPublicServiceClient();
				_client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);

				_client.DeleteAssignment(sessionToken, assignmentToDelete);
			}
			catch (FaultException<ejsFailureReport> ex)
			{
				if (ex.Detail._failureCode == 7)
					sessionToken._isAuthenticated = false;

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

		/// <summary>
		/// Hides a single Assignment on the eJournalServer
		/// </summary>
		public static void HideAssignment(ejsSessionToken sessionToken,
			ejsAssignment assignmentToHide)
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

				throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
			}
			catch (Exception)
			{
				sessionToken._isAuthenticated = false;
				throw new ApplicationException("EJSと接続する際に失敗しました。");
			}
			finally
			{
				if (_client != null)
					_client.Close();
			}
		}

        /// <summary>
        /// Restores a single Assignment in the eJournalServer
        /// </summary>
        public static void RestoreAssignment(ejsSessionToken sessionToken, ejsAssignment assignmentToRestore)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);
                _client.RestoreAssignment(sessionToken, assignmentToRestore);
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                sessionToken._isAuthenticated = false;
                throw new ApplicationException("EJSと接続する際に失敗しました。");
            }
            finally
            {
                if (_client != null)
                    _client.Close();
            }
		}

		#endregion

		#region Server Op

        /// <summary>
        /// Gets all the current stats from the designated server.
        /// </summary>
        public static ejsServerStats GetServerStats(
            ejsSessionToken sessionToken)
        {
            EjsPublicServiceClient _client = null;
            try
            {
                _client = new EjsPublicServiceClient();
                _client.Endpoint.Address = new EndpointAddress(ejsBridgeManager.EjsAddress);

                ejsServerStats result = _client.GetCurrentServerStats(sessionToken);

                return result;
            }
            catch (FaultException<ejsFailureReport> ex)
            {
                if (ex.Detail._failureCode == 7)
                    sessionToken._isAuthenticated = false;

                throw new ApplicationException(ex.Detail._header + "\n" + ex.Detail._originalException.Message);
            }
            catch (Exception)
            {
                sessionToken._isAuthenticated = false;
                throw new ApplicationException("EJSと接続する際に失敗しました。");
            }
            finally
            {
                if (_client != null)
                    _client.Close();
            }
		}

		#endregion
	}
}
