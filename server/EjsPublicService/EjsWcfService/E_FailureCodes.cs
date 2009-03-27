/// -----------------------------------------------------------------
/// E_FailureCodes.cs: enumeration of error codes.
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------

using System;
using System.ComponentModel;

namespace EjsWcfService
{
	public enum FAILURE_CODES
	{
		[Description("The Type of Failure is Unknown.")]
		Unknown = 0,

		[Description("Authenticating the specified credentials failed.")]
		AuthenticationFailed = 4,

		[Description("Adding a new authenticated session to the sessoin pool failed.")]
		AddAuthenticatedSessionToPoolFailed = 5,

		[Description("Removing authenticated token failed.")]
		SessionInvalidationFailed = 6,

		[Description("Validating a Session by a received Token failed.")]
		SessionValidationFailed = 7,

		[Description("Failed to get a list of available Assignments from the E Journal Server.")]
		GetAvailableAssignmentsFailed = 8,

		[Description("Failed to open the connection to the E Journal Server Database.")]
		OpenDatabaseConnectionFailed = 9,

		[Description("Failed to close the connection to the E Journal Server Database.")]
		CloseDatabaseConnectionFailed = 10,

		[Description("Failed to register a new user in the E Journal Server Database.")]
		RegisterNewUserFailed = 11,

		[Description("Failed to register a user to a course the E Journal Server Database.")]
		RegisterUserToCourseFailed = 12,

		[Description("Failed to get a list of all the users in the E Journal Server Database.")]
		GetAllRegisteredUsersFailed = 13,

		[Description("Failed to get a list of all the courses in the E Journal Server Database.")]
		GetAllRegisteredCoursesFailed = 14,

		[Description("Failed to save the Assignment data to the E Journal Server Database.")]
		SaveAssignmentDataFailed = 15,

		[Description("Failed to save the Study data to the E Journal Server Database.")]
		SaveStudyDataFailed = 15,

		[Description("Failed to all Course Documents from the E Journal Server Database.")]
		GetAllCourseDocumentsFailed = 16,

		[Description("Failed to Delete an Assignment from the E Journal Server Database.")]
		DeleteAssignmentFailed = 17,

		[Description("Failed to Delete a Course Document from the E Journal Server Database.")]
		DeleteCourseDocumentFailed = 18,

		[Description("Failed to Restore an Assignment in the E Journal Server Database.")]
		RestoreAssignmentFailed = 19,

		[Description("Failed to update a course document record in the E Journal Server Database.")]
		UpdateCourseDocumentFailed = 20,

		[Description("Failed to update a user record in the E Journal Server Database.")]
		UpdateUserFailed = 21,

		[Description("Failed to update a course record in the E Journal Server Database.")]
		UpdateCourseFailed = 22,

		[Description("Failed to update a Users's Password in the E Journal Server Database.")]
		UpdateUserPasswordFailed = 23,

		[Description("Failed to get a course document from the E Journal Server Database.")]
		GetCourseDocumentFailed = 24,

		[Description("Failed to get an assignment from the E Journal Server Database.")]
		GetAssignmentFailed = 25,

		[Description("Failed to get courses registered for the user from the E Journal Server Database.")]
		GetRegisteredCoursesForUserFailed = 26,

		[Description("Failed to add a course document to the E Journal Server Database.")]
		AddCourseDocumentFailed = 27,

		[Description("Failed to update a course in the E Journal Server Database.")]
		UpdateCourseRecordFailed = 28,

		[Description("Failed to delete a course from the E Journal Server Database.")]
		DeleteCourseRecordFailed = 29,

		[Description("Failed to remove a Course Registration Record from the E Journal Server Database.")]
		DeleteCourseRegistrationRecordFailed = 30,

		[Description("Failed to hide an Assignment Record from the E Journal Server Database.")]
		HideAssignmentFailed = 31,
	}
}

