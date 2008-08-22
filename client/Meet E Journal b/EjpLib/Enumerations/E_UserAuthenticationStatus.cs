
namespace SiliconStudio.Meet.EjpLib.Enumerations
{
	/// <summary>
	/// Describes the various possible statuses of the users
	/// session in regards to authentication with the E Journal Server.
	/// </summary>
	public enum UserAuthenticationStatus
	{
		/// <summary>
		/// The user has not yet been authorized with the
		/// E Journal Server. In this case the application
		/// has not yet asked for user credentials.
		/// </summary>
		NeverAuthenticated = 0,

		/// <summary>
		/// The user was authenticated with the E Journal Server
		/// within the Time Out span. There is no need to
		/// authenticate the user again. The application has
		/// </summary>
		HasAuthenticatedSession = 1,

		/// <summary>
		/// The user has provided valid user credentials and
		/// was previously authenticated with the E Journal Server.
		/// However, the authenticated session has timed out and
		/// the application should re-authenticate the user
		/// before attempting any operations that require
		/// communication with the E Journal Server.
		/// </summary>
		AuthenticatedSessionHasTimedOut = 2

	}//end: UserAuthenticationStatus
}
