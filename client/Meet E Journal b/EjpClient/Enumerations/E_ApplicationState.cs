
namespace ejpClient.Enumerations
{
    public enum ApplicationState
    {
        /// <summary>
        /// No assignment is loaded.
        /// </summary>
        Cold,

        /// <summary>
        /// Assignment data has been loaded into the
        /// application.
        /// </summary>
        AssignmentLoaded,

        /// <summary>
        /// An assignment belonging to a user
        /// other than the currently active user
        /// has been loaded into the application.
        /// </summary>
        ComAssignmentLoaded


    }
}
