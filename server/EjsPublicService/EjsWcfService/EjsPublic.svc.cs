/// -----------------------------------------------------------------
/// EjsPublic.svc.cs: Service entry point
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Diagnostics;
using EjsWcfService.UserOp;
using EjsWcfService.AssignmentOp;
using EjsWcfService.CourseOp;

namespace EjsWcfService
{
	// NOTE: If you change the class name "Service1" here, you must also update the reference to "Service1" in Web.config and in the associated .svc file.
	public class ejsPublicService : IEjsPublicService
	{
		#region ctor
		public ejsPublicService()
		{
		}
		#endregion
		
		#region IEjsPublicService Members

		#region User Operations

		/// <summary>
		/// Authenticates a set of user credentials against the
		/// user database on the E Journal Server.
		/// </summary>
		/// <param name="UserName">A User Name registered with the E Journal Server.</param>
		/// <param name="Password">The Password ascociated with the given User Name.</param>
		/// <param name="SourceHostId">The Id of the Host that requested the Token. 
		///                            This Id gets embedded in the returned ejsSessionToken.</param>
		/// <returns>A new ejsSessionToken for the authenticated session.</returns>
		public ejsSessionToken Authenticate(string UserName, string Password, Guid SourceId)
		{
			SqlConnection connection = null;

			try
			{
				/* Authenticate the credentials against the local database, 
				 * if this succeeds, generate a new ejsSessionToken and add
				 * this token to the local TokenPool. Last, return this new
				 * Token to the caller. If authentication fails, return a 
				 * ejsFailure report. */

				lock (this)
				{
					sessionManager.connectionCount += 1;

					ejsSessionToken newToken = new ejsSessionToken(
							Guid.NewGuid(), SourceId, Guid.Empty, DateTime.Now,
							DateTime.Now.AddHours(12), false, "", "");

					connection = EjsConnectionHandler.OpenDBConnection(
						ConfigurationManager.AppSettings["connectionString"]);

					int exitCode = -1;
					if (connection != null)
					{
						exitCode = UserOp.RemoteUserOperations.AuthenticateUserCredentials(
							connection,
							UserName, Password, ref newToken);
					}

					if (exitCode == 0)
					{
						ejsSessionUserData u = new ejsSessionUserData(newToken.LastName + ", " + newToken.FirstName, newToken);
						sessionManager.TokenPool.AddAuthenticatedSession(newToken, u);

						ejsLogHelper.LogMessage("User '" + u.UserName + "' Retreived a Session Token. " +
													"Token Expire Date: " + newToken.ExpireTimeStamp.ToLongDateString(), false);

						ejsLogHelper.LogMessage("Session Pool now contains "
							+ sessionManager.TokenPool.GetCount().ToString() +
							" authenticated sessions.", true);
					}
					else if (exitCode == -1)
					{
						sessionManager.connectionCount -= 1;
						throw new Exception("Server is busy. Please try again in a few seconds.");
					}
					else
					{
						sessionManager.connectionCount -= 1;
						throw new Exception("Authentication Failed.");
					}

					sessionManager.connectionCount -= 1;
					return newToken;
				}
			}
			catch (Exception ex)
			{
				//TODO: Add Logging code to event log
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.AuthenticationFailed,
					"Authentication Failed (User Name: " + UserName + ")",
					"Your credentials could not be authenticated.\nMake sure you typed them correctly.",
					ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Update the password for a user registered in the E Journal Server.
		/// </summary>
		/// <param name="UserName">User Name part of the credentials.</param>
		/// <param name="OldPassword">Old Password part of the credentials.</param>
		/// <param name="NewPassword">New Password part of the credentials.</param>
		public void UpdateUserPassword(string UserName, string OldPassword, string NewPassword)
		{
			SqlConnection connection = null;

			try
			{
				ejsLogHelper.LogMessage("UserName '" +
					UserName +
					"': UpdateUserPassword.", true);

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -1;
				if (connection != null)
				{
					exitCode = UserOp.RemoteUserOperations.UpdateUserPassword(
						connection, UserName, OldPassword, NewPassword);
				}

				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Update Password: User Name and Old Password do not match.");
				else if (exitCode == -1)
					throw new Exception("Server is busy. Please try again in a few seconds.");
				else
					throw new Exception("Failed to Update Password: Uknown Problem.");
			}
			catch (Exception ex)
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.UpdateUserPasswordFailed,
				   "Update User Password Failed",
				   "Failed to Update the Password for User '" + UserName + "'\n",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Logs out a user from the E Journal Server. This process involves removing the 
		/// servers copy of the Session Token used by this user for all transaction.
		/// </summary>
		/// <param name="Token">The token that is to be invalidated.</param>
		public void InvalidateToken(ejsSessionToken Token)
		{
			try
			{
				/* Invalidate the given token making all
				 * further connections impossible. */

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"' was removed from the Session Pool", false);

				sessionManager.TokenPool.InvalidateSession(Token.Id);
				ejsLogHelper.LogMessage("Session Pool now contains "
					+ sessionManager.TokenPool.GetCount().ToString() +
					" authenticated sessions.", true);
			}
			catch (Exception ex)
			{
				//TODO: Add Logging code to event log
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionInvalidationFailed,
					"Invalidating Token Failed",
					"Your Session Token could note be invalidated correctly.",
					ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
		}

		/// <summary>
		/// Adds a new user to the EJS User database.
		/// </summary>
		/// <param name="newUser">General information on the new user.</param>
		/// <param name="UserName">User Name for the new user.</param>
		/// <param name="Password">Password for the new user.</param>
		/// <param name="UserGroupId">User Group (Teachers, students ...) of the new User.</param>
		/// <param name="IsAccountActive">Tells whether the new account is active and can log in or not.</param>
		public void RegisterNewUser(ejsSessionToken Token, ejsUserInfo newUser, string UserName, string Password,
			int UserGroupId, bool IsAccountActive)
		{
			SqlConnection connection = null;
			try
			{
				/* TODO:
				 * We need to work out a  policy for this so that only 
				 * authenticated teachers can add new users.*/

				if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
				{
					ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
						"Validating Session Failed",
						"Your Session could not be validated using the Token provided.",
						null, false);
					throw new FaultException<ejsFailureReport>(r, r.Header);
				}

				ejsLogHelper.LogMessage("User '" +
									sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
									"': RegisterNewUser ('" + UserName + "').", true);

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -2;
				if (connection != null)
				{
					exitCode = UserOp.RemoteUserOperations.RegisterNewUser(
						connection,
						newUser, UserName, Password, IsAccountActive, UserGroupId);
				}

				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Register new user: User Name already exists.");
				else if (exitCode == 2)
					throw new Exception("Server is busy. Please try again in a few seconds.");
				else
					throw new Exception("Failed to Register new user: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.RegisterNewUserFailed,
								   "Register new User Failed",
								   ex.Message + "\n" +
								   "Please try again using a different User Name",
								   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Get a list of all the users registered in the database.
		/// </summary>
		/// <param name="Token">The session token to use for this transaction.</param>
		/// <param name="UserGroupId">The group id for the users to select. Pass -1 for all groups.</param>
		/// <returns>An array of ejsUserInfo objects.</returns>
		public ejsUserInfo[] GetAllRegisteredUsers(ejsSessionToken Token, int UserGroupId)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, get the course meta data 
				 *for all the Users in the database.
				 *The return type is a simple array.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetAllRegisteredUsers.", true);

				List<UserOp.ejsUserInfo> result = new List<UserOp.ejsUserInfo>();

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				if (connection != null)
				{
					UserOp.RemoteUserOperations.GetRegisteredUserList(
						connection, UserGroupId, out result);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

				return result.ToArray();
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetAllRegisteredUsersFailed,
				   "Get All Registered Users Failed",
				   "Failed to get list of all registered users in the database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Updates a user record.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="userInfo">The userInfo object to update</param>
		/// <param name="password">The new password. Send 'NoChange' for no update.</param>
		public void UpdateUserRecord(
			ejsSessionToken Token, ejsUserInfo userInfo, string password)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, update the user record */

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': UpdateUserRecord.", true);

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -2;
				if (connection != null)
				{
					exitCode = UserOp.RemoteUserOperations.UpdateUserData(connection,
						Token, userInfo, password);
				}
				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Update User: User Name already exists.");
				else if (exitCode == 2)
					throw new Exception("Failed to Update User: Insufficient operator level.");
				else if (exitCode == 3)
					throw new Exception("Failed to Update User: Operator Id does not exist.");
				else if (exitCode == -2)
					throw new Exception("Server is busy. Please try again in a few seconds.");
				else
					throw new Exception("Failed to Update User: Uknown Problem.");
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.UpdateUserFailed,
				   "Update User Record failed",
				   "Failed to update the specified record in the database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Deletes a user record.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="userInfo">The userInfo object to delete</param>
		public void DeleteUserRecord(ejsSessionToken Token, ejsUserInfo userInfo)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, delete the user record */

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': DeleteUserRecord.", true);

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -2;
				if (connection != null)
				{
					exitCode = UserOp.RemoteUserOperations.DeleteUserData(connection,
						Token, userInfo);
				}
				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Delete User: Insufficient operator level.");
				else if (exitCode == 2)
					throw new Exception("Failed to Delete User: Operator Id does not exist.");
				else if (exitCode == -2)
					throw new Exception("Server is busy. Please try again in a few seconds.");
				else
					throw new Exception("Failed to Delete User: Uknown Problem.");
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.UpdateUserFailed,
				   "Delete User Record failed",
				   "Failed to delete the specified record from the database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		#endregion

		#region Assignment Operations

		/// <summary>
		/// Deletes the given assignment from the EJS database.
		/// </summary>
		public void DeleteAssignment(ejsSessionToken Token,
			ejsAssignment Assignment)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/* If the user was authenticated successfully, update the
				 * record for the given assignment and mark it as not available
				 * in the EJS Database.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': DeleteAssignment.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -2;
				if (connection != null)
				{
					exitCode = AssignmentOp.RemoteAssignmentOperations.DeleteAssignment(
					   connection, Token, Assignment);
				}
				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Delete Assignment: Assignment Belongs to another user.");
				else if (exitCode == -2)
					throw new Exception("Server is busy. Please try again in a few seconds.");
				else
					throw new Exception("Failed to Delete Assignment: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.DeleteAssignmentFailed,
				   "Delete Assignment Failed",
				   "Failed to Delete the assignment : " + Assignment.Title + ".\n",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}


		/// <summary>
		/// Hides the given assignment from the EJS database.
		/// </summary>
		public void HideAssignment(ejsSessionToken Token, AssignmentOp.ejsAssignment Assignment)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/* If the user was authenticated successfully, update the
				 * record for the given assignment and mark it as not available
				 * in the EJS Database.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': HideAssignment.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -2;
				if (connection != null)
				{
					exitCode = AssignmentOp.RemoteAssignmentOperations.HideAssignment(
					   connection, Token, Assignment);
				}
				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Hide Assignment: Assignment Belongs to another user.");
				else if (exitCode == -2)
					throw new Exception("Server is busy. Please try again in a few seconds.");
				else
					throw new Exception("Failed to Hide Assignment: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.HideAssignmentFailed,
				   "Hide Assignment Failed",
				   "Failed to Hide the assignment : " + Assignment.Title + ".\n",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}


		/// <summary>
		/// Restores the given assignment in the EJS database.
		/// </summary>
		public void RestoreAssignment(ejsSessionToken Token, ejsAssignment Assignment)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/* If the user was authenticated successfully, update the
				 * record for the given assignment and mark it as available
				 * in the EJS Database.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': RestoreAssignment.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -2;
				if (connection != null)
				{
					exitCode = AssignmentOp.RemoteAssignmentOperations.RestoreAssignment(
					   connection, Token, Assignment);
				}
				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Restore Assignment: Assignment Belongs to another user.");
				else if (exitCode == -2)
					throw new Exception("Server is busy. Please try again in a few seconds.");
				else
					throw new Exception("Failed to Restore Assignment: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.RestoreAssignmentFailed,
				   "Restore Assignment Failed",
				   "Failed to Restore the assignment : " + Assignment.Title + ".\n",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}


		/// <summary>
		/// Returns a list of all the available Assignments for the selected User Id.
		/// The returned data contains only the meta data for the Assignments.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <returns>A List of Assignment Meta Data objects.</returns>
		public ejsAssignment[] GetAllAssignments(ejsSessionToken Token, bool IncludeNotAvailable)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, get the meta data 
				 *for all the assignments available in the database that belongs to this
				 *user. The return type is a simple array.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetAllAssignments.", true);

				List<ejsAssignment> result = new List<ejsAssignment>();

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				if (connection != null)
				{
					AssignmentOp.RemoteAssignmentOperations.GetAllAssignments(
					   connection, IncludeNotAvailable, Token, ref result);

					foreach (ejsAssignment assignment in result)
					{
						//List<ejsStudyMetaData> resultStudies = new List<ejsStudyMetaData>();
						//AssignmentOp.RemoteAssignmentOperations.GetStudiesForAssignment(
						//    connection, Token, assignment, ref resultStudies);

						//assignment.studies = resultStudies.ToArray();

						List<ejsStudyMetaData> resultStudies = new List<ejsStudyMetaData>();
						ejsStudyMetaData metaDataDummy = new ejsStudyMetaData();
						metaDataDummy.Title = "現在スタディリストは表示しない。";
						metaDataDummy.Description = "";
						metaDataDummy.ParentAssignmentId = assignment.EJSDatabaseId;
						metaDataDummy.CreationDate = DateTime.Now;
						metaDataDummy.LastModifiedDate = DateTime.Now;
						metaDataDummy.IsAvailable = true;
						metaDataDummy.CommentCount = 0;

						resultStudies.Add(metaDataDummy);
						assignment.studies = resultStudies.ToArray();

					}
				}
				else
				{
					throw new Exception("Server is busy. Please try again in a few seconds.");
				}

				return result.ToArray();
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetAvailableAssignmentsFailed,
				   "Get Available Assignments Failed",
				   "Failed to get list of Available Assignments. The server might be busy at the moment, please try again in a minute.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Saves and uploads an assignent file to the EJS.
		/// </summary>
		public int SaveAndUploadAssignment(ejsSessionToken Token, ejsAssignment assignment, byte[] data)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': SaveAndUploadAssignment.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				int NewID = -1;
				if (connection != null)
				{
					NewID = AssignmentOp.RemoteAssignmentOperations.SaveAndUploadAssignment(
					   connection, Token, assignment, data);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

				return NewID;

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SaveAssignmentDataFailed,
				   "Save Assignment Data Failed",
				   "Failed to Save the assignment data to the EJS. Title: " + assignment.Title,
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Saves and uploads the meta data for one Study to EJS
		/// </summary>
		public void SaveStudyMetaData(ejsSessionToken Token, ejsStudyMetaData study, int parentAssignmentId)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': SaveStudyMetaData.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				if (connection != null)
				{
					AssignmentOp.RemoteAssignmentOperations.SaveStudyMetaData(
					   connection, Token, study, parentAssignmentId);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SaveStudyDataFailed,
				   "Save Study Data Failed",
				   "Failed to Save the Study data to the EJS.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Get a single Assignment from the EJS Database.
		/// </summary>
		public byte[] GetAssignment(ejsSessionToken Token, ejsAssignment assignment)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/* If the user was authenticated successfully, read the data for a
				 * document in the database into a byte array and return it to the
				 * caller.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetAssignment.", true);

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				byte[] result = null;
				if (connection != null)
				{
					result = AssignmentOp.RemoteAssignmentOperations.GetAssignment(
						connection, assignment, Token, assignment.EJSDatabaseId);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

				return result;
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetAssignmentFailed,
				   "Get document failed",
				   "Failed to get the specified document data from the document database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Get a single Assignment from the EJS Database.
		/// </summary>
		public byte[] GetCommentsMergedAssignment(ejsSessionToken Token,
			ejsAssignment assignment, ejsAssignment[] assignmentsToMerge)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/* If the user was authenticated successfully, merge the comments in all the specified
				 * documents into the parent Afor all
				 * the document in the database into a byte array and return it to the
				 * caller.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetCommentsMergedAssignment.", false);

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				byte[] result = null;
				if (connection != null)
				{
					result = AssignmentOp.RemoteAssignmentOperations.GetMergedCommentedAssignment(
						connection, assignment, Token, assignment.EJSDatabaseId, assignmentsToMerge);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

				return result;
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.Unknown,
				   "Get Comments Merged Assignment failed",
				   "Failed to get the specified document data from the document database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		#endregion

		#region Course Operations

		/// <summary>
		/// Gets all the courses that the given user is registered to.
		/// Depending on the arguments, each course is also populated
		/// with all the documents registered to that couse.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="includeCourseDocuments">Determines whether each couse should
		///                                      also contain all the documents registered 
		///                                      to that course.</param>
		/// <returns>An array of ejsCourse objects.</returns>
		public CourseOp.ejsCourse[] GetRegisteredCoursesForUser(
			ejsSessionToken Token, bool includeCourseDocuments)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, get the course meta data 
				 *for all the courses in the database that this user is registered to.
				 *Based on preference, include all the Documents registerd to the course.
				 *These documents are not assignments but the documents that can be used to build studies.
				 *The return type is a simple array.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetRegisteredCoursesForUser.", true);

				List<CourseOp.ejsCourse> result = new List<CourseOp.ejsCourse>();

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				if (connection != null)
				{
					CourseOp.RemoteCourseOperations.GetRegisteredCoursesForUser(
						connection, Token, ref result);

					if (includeCourseDocuments == true)
					{
						CourseOp.RemoteCourseOperations.GetDocumentsForUserRegisteredCourses(
							connection, Token, ref result);
					}

					return result.ToArray();
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetRegisteredCoursesForUserFailed,
				   "Get Registered Courses Failed",
				   "Failed to get list of registered courses for user.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		public ejsCourseDocument[] GetAllCourseDocuments(
			ejsSessionToken Token, bool IncludeNotAvailable)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, get the course meta data 
				 *for all the courses in the database.
				 *The return type is a simple array.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetAllCourseDocuments.", true);

				List<CourseOp.ejsCourseDocument> result = new List<CourseOp.ejsCourseDocument>();

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				if (connection != null)
				{
					CourseOp.RemoteCourseOperations.GetAllCourseDocuments(
						connection, Token, IncludeNotAvailable, ref result);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

				return result.ToArray();
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetAllCourseDocumentsFailed,
				   "Get All Course Document Failed",
				   "Failed to get list of all registered courses documents in the database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Adds a document to a course.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="Document">The document meta data to upload.</param>
		/// <param name="CourseId">The course Id that the document should be added to.</param>
		/// <param name="DocumentData">The byte data for the document.</param>
		public void AddDocumentToCourse(
			ejsSessionToken Token, ejsCourseDocument Document, int CourseId, byte[] DocumentData)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, push the data in the
				 * byte array into the database and add all the meta data supplied 
				 * in the document and courseId parameters.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': AddDocumentToCourse.", true);

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				CourseOp.RemoteCourseOperations.RegisterDocumentToCourse(
					connection, Token, Document, CourseId, DocumentData);

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.AddCourseDocumentFailed,
				   "Add document failed",
				   "Failed to add the specified file data to the course document database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Updates a course document record.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="Document">The document meta data to upload.</param>
		public void UpdateCourseDocument(
			ejsSessionToken Token, ejsCourseDocument Document)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, add all the meta data supplied 
				 * in the document parameters.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': UpdateCourseDocument.", true);

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				if (connection != null)
				{
					CourseOp.RemoteCourseOperations.UpdateCourseDocument(
						connection, Token, Document);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.UpdateCourseDocumentFailed,
				   "Add document failed",
				   "Failed to update the specified record in the course document database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Deletes the given course document from the EJS database.
		/// </summary>
		public void DeleteCourseDocument(ejsSessionToken Token, CourseOp.ejsCourseDocument documentToDelete)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/* If the user was authenticated successfully, delete
				 * the given course document from the EJS Database.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': DeleteCourseDocument.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -2;
				if (connection != null)
				{
					exitCode = CourseOp.RemoteCourseOperations.DeleteCourseDocument(
					   connection, Token, documentToDelete);
				}
				if (exitCode == 0)
					return;
				else if (exitCode == -2)
					throw new Exception("Failed to Delete Course Document: User does not have sufficient rights.");
				else
					throw new Exception("Failed to Delete Course Document: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.DeleteCourseDocumentFailed,
				   "Delete Course Document Failed",
				   "Failed to Delete the Course Document : " + documentToDelete.Name + ".\n",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Gets the byte representation of a document in a course.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="Document">The document meta data to upload.</param>
		public byte[] GetCourseDocument(
			ejsSessionToken Token, ejsCourseDocument Document)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/* If the user was authenticated successfully, read the data for a
				 * document in the database into a byte array and return it to the
				 * caller.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetCourseDocument.", true);

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				byte[] result = null;
				if (connection != null)
				{
					result = CourseOp.RemoteCourseOperations.GetCourseDocument(
						connection, Token, Document);
					return result;
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetCourseDocumentFailed,
				   "Get document failed",
				   "Failed to get the specified document data from the course document database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Gets all the courses that are currently registered in the 
		/// in the Server.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="includeDisabledCourses">Determines whether courses
		///                                      not available for registration
		///                                      should be included in the result.</param>
		/// <returns>An array of ejsCourse objects.</returns>
		public CourseOp.ejsCourse[] GetAllRegisteredCourses(
			ejsSessionToken Token, bool includeDisabledCourses)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, get the course meta data 
				 *for all the courses in the database.
				 *The return type is a simple array.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetAllRegisteredCourses.", true);

				List<CourseOp.ejsCourse> result = new List<CourseOp.ejsCourse>();

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				if (connection != null)
				{
					CourseOp.RemoteCourseOperations.GetAllRegisteredCourses(
						connection, Token, includeDisabledCourses, ref result);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

				return result.ToArray();
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetAllRegisteredCoursesFailed,
				   "Get All Registered Courses Failed",
				   "Failed to get list of all registered courses in the database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Gets all the Courses Registrations from the Server.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <returns>An array of ejsCourseRegistration objects.</returns>
		public CourseOp.ejsCourseRegistration[] GetAllRegisteredCourseRegistrations(
			ejsSessionToken Token)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				/*If the user was authenticated successfully, get the Course 
				 * Registrations in the database.
				 *The return type is a simple array.*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetAllRegisteredCourseRegistrations.", true);

				List<CourseOp.ejsCourseRegistration> result =
					new List<CourseOp.ejsCourseRegistration>();

				//Open a connection
				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				if (connection != null)
				{
					CourseOp.RemoteCourseOperations.GetAllCourseRegistrations(
						connection, Token, ref result);
				}
				else
					throw new Exception("Server is busy. Please try again in a few seconds.");

				return result.ToArray();
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetAllRegisteredCoursesFailed,
				   "Get All Registered CoursesRegistrations Failed",
				   "Failed to get list of all Course Registration Records in the database.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Adds a new course to the EJS Courses database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="NewCourse">The new course to add to EJS.</param>
		public void RegisterNewCourse(ejsSessionToken Token, ejsCourse NewCourse)
		{
			SqlConnection connection = null;
			try
			{
				if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
				{
					ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
						"Validating Session Failed",
						"Your Session could not be validated using the Token provided.",
						null, false);
					throw new FaultException<ejsFailureReport>(r, r.Header);
				}

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': RegisterNewCourse.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = CourseOp.RemoteCourseOperations.RegisterNewCourse(
					connection,
					Token, NewCourse);

				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Register new course: Course Name already exists.");
				else
					throw new Exception("Failed to Register new course: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.RegisterNewUserFailed,
								   "Register new Course",
								   ex.Message + "\n" +
								   "Please try again using a different Course Name",
								   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Updates a course in the EJS Courses database.
		/// </summary>
		public void UpdateCourseRecord(ejsSessionToken Token, ejsCourse Course)
		{
			SqlConnection connection = null;
			try
			{
				if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
				{
					ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
						"Validating Session Failed",
						"Your Session could not be validated using the Token provided.",
						null, false);
					throw new FaultException<ejsFailureReport>(r, r.Header);
				}

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': UpdateCourseRecord.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = CourseOp.RemoteCourseOperations.UpdateCourseRecord(
					connection, Token, Course);

				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to update course: Insufficient User Level.");
				else if (exitCode == 2)
					throw new Exception("Failed to update course: Course does not exist.");
				else
					throw new Exception("Failed to update course: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.UpdateCourseRecordFailed,
								   "Update Course Record",
								   ex.Message,
								   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Deletes a course from the EJS Courses database.
		/// </summary>
		public void DeleteCourseRecord(ejsSessionToken Token, ejsCourse Course)
		{
			SqlConnection connection = null;
			try
			{
				if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
				{
					ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
						"Validating Session Failed",
						"Your Session could not be validated using the Token provided.",
						null, false);
					throw new FaultException<ejsFailureReport>(r, r.Header);
				}

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': DeleteCourseRecord.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
					ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = CourseOp.RemoteCourseOperations.DeleteCourseRecord(
					connection, Token, Course);

				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to delete course: Insufficient User Level.");
				else if (exitCode == 2)
					throw new Exception("Failed to delete course: User (Operator) does not exist.");
				else
					throw new Exception("Failed to delete course: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.DeleteCourseRecordFailed,
								   "Delete Course Record",
								   ex.Message,
								   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		/// <summary>
		/// Adds a new record to the EJS Course Registrations database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="Course">The course the user should be registered to.</param>
		public void RegisterUserToCourse(ejsSessionToken Token, ejsCourse Course)
		{
			try
			{
				if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
				{
					ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
						"Validating Session Failed",
						"Your Session could not be validated using the Token provided.",
						null, false);
					throw new FaultException<ejsFailureReport>(r, r.Header);
				}

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': RegisterUserToCourse.", true);

				int exitCode = CourseOp.RemoteCourseOperations.RegisterUserToCourse(
					EjsConnectionHandler.OpenDBConnection(ConfigurationManager.AppSettings["connectionString"]),
					Token, Course);

				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Register User to Course: Already Registered.");
				else
					throw new Exception("Failed to Register User to Course: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.RegisterUserToCourseFailed,
								   "Register User to Course",
								   ex.Message + "\n" +
								   "Please try again later.",
								   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
		}

		/// <summary>
		/// Adds a new record to the EJS Course Registrations database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="User">User info for the user to register.</param>
		/// <param name="Course">The course the user should be registered to.</param>
		public void RegisterUserToCourse_adm(ejsSessionToken Token, ejsUserInfo User, ejsCourse Course)
		{
			try
			{
				if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
				{
					ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
						"Validating Session Failed",
						"Your Session could not be validated using the Token provided.",
						null, false);
					throw new FaultException<ejsFailureReport>(r, r.Header);
				}

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': RegisterUserToCourse.", true);

				int exitCode = CourseOp.RemoteCourseOperations.RegisterUserToCourse_adm(
					EjsConnectionHandler.OpenDBConnection(ConfigurationManager.AppSettings["connectionString"]),
					Token, User, Course);

				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Register User to Course: Already Registered.");
				else
					throw new Exception("Failed to Register User to Course: Uknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.RegisterUserToCourseFailed,
								   "Register User to Course",
								   ex.Message + "\n" +
								   "Please try again later.",
								   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
		}

		/// <summary>
		/// Removes a record from the EJS Course Registrations database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="User">User info for the user to register.</param>
		/// <param name="Course">The course the user should be registered to.</param>
		public void RemoveUserFromCourse(ejsSessionToken Token, ejsUserInfo User, ejsCourse Course)
		{

			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			SqlConnection connection = null;

			try
			{
				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': RemoveUserFromCourse.", true);

				connection = EjsConnectionHandler.OpenDBConnection(
				   ConfigurationManager.AppSettings["connectionString"]);

				int exitCode = -2;
				if (connection != null)
				{
					exitCode = CourseOp.RemoteCourseOperations.RemoveUserFromCourse(
						connection, Token, User, Course);
				}

				if (exitCode == 0)
					return;
				else if (exitCode == 1)
					throw new Exception("Failed to Remove User from Course: Registration Does Not Exist.");
				else if (exitCode == -2)
					throw new Exception("Failed to Remove User from Course: Server Busy.");
				else if (exitCode == 2)
					throw new Exception("Failed to Remove User from Course: User Does Not Exist.");
				else
					throw new Exception("Failed to Remove User from Course: Unknown Problem.");

			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.DeleteCourseRegistrationRecordFailed,
								   "Remove User from Course Failed",
								   ex.Message + "\n" +
								   "Please try again later.",
								   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
			finally
			{
				if (connection != null)
					EjsConnectionHandler.CloseDBConnection(connection);
			}
		}

		#endregion

		#region Server Operations

		/// <summary>
		/// Gets a snapshot of the current status of the Server.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <returns>An ejsServerStats object.</returns>
		public ServerOp.ejsServerStats GetCurrentServerStats(ejsSessionToken Token)
		{
			if (!sessionManager.TokenPool.ValidateSessionByToken(Token))
			{
				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.SessionValidationFailed,
					"Validating Session Failed",
					"Your Session could not be validated using the Token provided.",
					null, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}

			try
			{
				/*If the user was authenticated successfully, get
				 * a snapshot of the current status of the Server*/

				ejsLogHelper.LogMessage("User '" +
					sessionManager.TokenPool.GetUserDataByTokenId(Token.Id).UserName +
					"': GetCurrentServerStats.", true);

				ServerOp.ejsServerStats result = null;

				ServerOp.RemoteServerStatsOperations.GetServerStats(ref result);

				return result;
			}
			catch (Exception ex)
			{
				if (ex is FaultException)
					throw ex;

				ejsFailureReport r = new ejsFailureReport((int)FAILURE_CODES.GetAllRegisteredCoursesFailed,
				   "Get Server Stats Failed",
				   "Failed to get snapshot of the current status of the server.",
				   ex, false);
				throw new FaultException<ejsFailureReport>(r, r.Header);
			}
		}

		#endregion

		#endregion
	}
}
