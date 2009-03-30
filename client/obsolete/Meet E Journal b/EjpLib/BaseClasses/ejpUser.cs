using System;

namespace SiliconStudio.Meet.EjpLib.BaseClasses
{
	/// <summary>
	/// Abstract base class for different types of
	/// users of the application.
	/// </summary>
	public abstract class ejpUser
	{
		#region Properties with Accessors
		
		public Guid Id
		{
			get { return this._sessionToken._userId; }
			set { this._sessionToken._userId = value; }
		}

        private EjsBridge.ejsService.ejsSessionToken _sessionToken;
        public EjsBridge.ejsService.ejsSessionToken SessionToken
        {
            get { return _sessionToken; }
            set { _sessionToken = value; }
        }

        public string FirstName
        {
            get { return this._sessionToken._firstName; }
            set { this._sessionToken._firstName = value; }
        }

        public string LastName
        {
            get { return this._sessionToken._lastName; }
            set { this._sessionToken._lastName = value; }
        }

        private string _emailAddress;
        public string EmailAddress
        {
            get { return _emailAddress; }
            set { _emailAddress = value; }
        }

        #endregion

		#region Private Properties

		#endregion

		#region Constructors
		/// <summary>
		/// Private default constructor
		/// </summary>
		private ejpUser()
		{
			throw new InvalidOperationException("Never instantiate this class directly!");
		}//end: Constructor

		/// <summary>
		/// Protected base constructor.
		/// </summary>
		/// <param name="Id">Guid of the user.</param>
		protected ejpUser(EjsBridge.ejsService.ejsSessionToken ejsUserToken)
		{
            this._sessionToken = ejsUserToken;
		}//end: Constructor
		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		#endregion

		#region Static Methods

		#endregion
	
	}//end: ejpUser
}
