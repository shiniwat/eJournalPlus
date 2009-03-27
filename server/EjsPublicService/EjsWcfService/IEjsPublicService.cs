/// -----------------------------------------------------------------
/// IEjsPublicService.cs: this is the service contract compatible with original WCF exe.
/// connections to the Database.
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
using EjsWcfService.UserOp;
using EjsWcfService.AssignmentOp;
using EjsWcfService.CourseOp;
using SiliconStudio.Meet.EjpLib.BaseClasses;

namespace EjsWcfService
{
	/// <summary>
	/// iEjsPublicService is basically ported from EjsPublicService.exe.
	/// </summary>
	[ServiceContract (Name = "EjsPublicService", Namespace = "http://meet.ejs/services")]
	public interface IEjsPublicService
	{
		/* Methods
		 * 
		 * Let the service host keep a list of current sessions
		 * that holds a session ID, user ID and other data. Let
		 * the service match the rec. session ID to this list 
		 * and take action from there on. This way we do not need
		 * to pass the user ID to the service on each call, 
		 * limiting the attack surface slightly. Also, if there is 
		 * a session Id coming in that is not currently active, whose
		 * authentication has timed out or with a source that differs
		 * from the source registered with the service's version, we 
		 * can signal an authentication failure.
		 */

		#region User Op

		/// <summary>
		/// Authenticates a set of user credentials against the
		/// user database on the E Journal Server.
		/// </summary>
		/// <param name="UserName">A User Name registered with the E Journal Server.</param>
		/// <param name="Password">The Password ascociated with the given User Name.</param>
		/// <param name="SourceHostId">The Id of the Host that requested the Token. 
		///                            This Id gets embedded in the returned ejsSessionToken.</param>
		/// <returns>A new ejsSessionToken for the authenticated session.</returns>
		[OperationContract
			(
			AsyncPattern = false
			)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		ejsSessionToken Authenticate(string UserName, string Password, Guid SourceId);


		/// <summary>
		/// Updates a user record.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="userInfo">The userInfo object to update</param>
		/// <param name="password">The new password. Send 'NoChange' for no update.</param>
		[OperationContract
			(
			AsyncPattern = false
			)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void UpdateUserRecord(ejsSessionToken Token, ejsUserInfo userInfo, string password);


		/// <summary>
		/// Deletes a user record.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="userInfo">The userInfo object to delete</param>
		[OperationContract
			(
			AsyncPattern = false
			)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void DeleteUserRecord(ejsSessionToken Token, ejsUserInfo userInfo);

		/// <summary>
		/// Updates the password for a user registered in the E Journal Server
		/// </summary>
		/// <param name="UserName">User Name part of the credentials.</param>
		/// <param name="OldPassword">Old Password part of the credentials.</param>
		/// <param name="NewPassword">New Password part of the credentials.</param>
		[OperationContract
			(
			AsyncPattern = false
			)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void UpdateUserPassword(string UserName, string OldPassword, string NewPassword);

		/// <summary>
		/// Logs out a user from the E Journal Server. This process involves removing the 
		/// servers copy of the Session Token used by this user for all transaction.
		/// </summary>
		/// <param name="Token">The token that is to be invalidated.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void InvalidateToken(ejsSessionToken Token);

		/// <summary>
		/// Registers a new user to the EJS User database.
		/// </summary>
		/// <param name="userName">A Uniqe username.</param>
		/// <param name="password">A Password no longer than 512 chars.</param>
		/// <param name="userGroupId">The user group to which this user belongs.</param>
		/// <param name="isAccountActive">True if the user is active.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void RegisterNewUser(ejsSessionToken Token, ejsUserInfo newUser, string userName, string password, int userGroupId, bool isAccountActive);

		/// <summary>
		/// Get a list of all the users registered in the database.
		/// </summary>
		/// <param name="Token">The session token to use for this transaction.</param>
		/// <param name="UserGroupId">The group id for the users to select. Pass -1 for all groups.</param>
		/// <returns>An array of ejsUserInfo objects.</returns>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		ejsUserInfo[] GetAllRegisteredUsers(ejsSessionToken Token, int UserGroupId);

		#endregion

		#region Assignment Op

		/// <summary>
		/// Deletes an assignment from the EJS Database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="Assignment">The assignment to delete.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void DeleteAssignment(ejsSessionToken Token, ejsAssignment Assignment);

		/// <summary>
		/// Deletes an assignment from the EJS Database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="Assignment">The assignment to delete.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void HideAssignment(ejsSessionToken Token, ejsAssignment Assignment);

		/// <summary>
		/// Restores an assignment in the EJS Database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="Assignment">The assignment to restore.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void RestoreAssignment(ejsSessionToken Token, ejsAssignment Assignment);

		/// <summary>
		/// Returns a list of all the available Assignments for the selected User Id.
		/// The returned data contains only the meta data for the Assignments.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <returns>A List of Assignment Meta Data objects.</returns>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		ejsAssignment[] GetAllAssignments(ejsSessionToken Token, bool IncludeNotAvailable);


		/// <summary>
		/// Saves and uploads an assignent file to the EJS.
		/// </summary>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		int SaveAndUploadAssignment(ejsSessionToken Token, ejsAssignment assignment, byte[] data);

		/// <summary>
		/// uploads the meta data for one study to the ejs
		/// </summary>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void SaveStudyMetaData(ejsSessionToken Token, ejsStudyMetaData study, int parentAssignmentId);

		/// <summary>
		/// Gets a single assignment from the EJS Database
		/// </summary>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		byte[] GetAssignment(ejsSessionToken Token, ejsAssignment assignment);

		/// <summary>
		/// Gets an Assnignment with all available Comments Merged
		/// </summary>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		byte[] GetCommentsMergedAssignment(ejsSessionToken Token, ejsAssignment assignment, ejsAssignment[] assignmentsToMerge);


		#endregion

		#region Course Op

		/// <summary>
		/// Gets all course documents
		/// </summary>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		ejsCourseDocument[] GetAllCourseDocuments(ejsSessionToken Token, bool IncludeNotAvailable);

		/// <summary>
		/// Retiurns a list of all the courses that the given user is registered to.
		/// Optionally also populates the documents list with all the documents available
		/// to each course.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="includeCourseDocuments">Include all the documents registered to each course.</param>
		/// <returns>An Array of Course objects.</returns>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		CourseOp.ejsCourse[] GetRegisteredCoursesForUser(ejsSessionToken Token, bool includeCourseDocuments);

		/// <summary>
		/// Adds a document to a course.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="Document">The document meta data to upload.</param>
		/// <param name="CourseId">The course Id that the document should be added to.</param>
		/// <param name="DocumentData">The byte data for the document.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void AddDocumentToCourse(ejsSessionToken Token, ejsCourseDocument Document, int CourseId, byte[] DocumentData);

		/// <summary>
		/// Updates a course document record.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="Document">The document meta data to upload.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void UpdateCourseDocument(ejsSessionToken Token, ejsCourseDocument Document);

		/// <summary>
		/// Deletes the given course document from the EJS database.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="documentToDelete">The document to delete.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void DeleteCourseDocument(ejsSessionToken Token, CourseOp.ejsCourseDocument documentToDelete);

		/// <summary>
		/// Get a document (registered to a course) from the Ejs.
		/// </summary>
		/// <param name="Token">The Session Token for the user's current 
		///                     authenticated Session.</param>
		/// <param name="Document">The document meta data for the 
		///                        document to download.</param>
		/// <returns></returns>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		byte[] GetCourseDocument(ejsSessionToken Token, ejsCourseDocument Document);

		/// <summary>
		/// Gets all the courses that are currently registered in the 
		/// in the Server.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="includeDisabledCourses">Determines whether courses
		///                                      not available for registration
		///                                      should be included in the result.</param>
		/// <returns>An array of ejsCourse objects.</returns>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		CourseOp.ejsCourse[] GetAllRegisteredCourses(ejsSessionToken Token, bool includeDisabledCourses);

		/// <summary>
		/// Gets all the Courses Registrations from the Server.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <returns>An array of ejsCourseRegistration objects.</returns>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		CourseOp.ejsCourseRegistration[] GetAllRegisteredCourseRegistrations(ejsSessionToken Token);

		/// <summary>
		/// Registers a new Course to the EJS User database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="NewCourse">The new course to add to the server.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void RegisterNewCourse(ejsSessionToken Token, CourseOp.ejsCourse NewCourse);

		/// <summary>
		/// Adds a new record to the EJS Course Registrations database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="Course">The course the user should be registered to.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void RegisterUserToCourse(ejsSessionToken Token, ejsCourse Course);

		/// <summary>
		/// Adds a new record to the EJS Course Registrations database.
		/// This method is called by Teachers / Administrators to add users
		/// other then themselves to courses.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="User">User info for the user to register.</param>
		/// <param name="Course">The course the user should be registered to.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void RegisterUserToCourse_adm(ejsSessionToken Token, ejsUserInfo User, ejsCourse Course);


		/// <summary>
		/// Removes a record from the EJS Course Registrations database.
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		/// <param name="User">User info for the user to register.</param>
		/// <param name="Course">The course the user should be registered to.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void RemoveUserFromCourse(ejsSessionToken Token, ejsUserInfo User, ejsCourse Course);

		/// <summary>
		/// Deletes a course from the EJS Courses database.
		/// </summary>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void DeleteCourseRecord(ejsSessionToken Token, ejsCourse Course);

		/// <summary>
		/// Updates a course in the EJS Courses database.
		/// </summary>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		void UpdateCourseRecord(ejsSessionToken Token, ejsCourse Course);

		#endregion

		#region Server Stats Op

		/// <summary>
		/// Gets a snapshot of the current status of the Server
		/// </summary>
		/// <param name="Token">Session Token to use for the operations.</param>
		[OperationContract
					(
					AsyncPattern = false
					)]
		[FaultContract
			(
			typeof(ejsFailureReport)
			)]
		ServerOp.ejsServerStats GetCurrentServerStats(ejsSessionToken Token);

		#endregion

	}
}
