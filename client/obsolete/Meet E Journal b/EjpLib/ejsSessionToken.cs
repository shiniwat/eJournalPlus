using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiliconStudio.Meet.EjpLib.EJS
{
    public class ejsSessionToken
    {
        #region Members
        /// <summary>
        /// Creation time of this token.
        /// </summary>
        private DateTime _creationTimeStamp;
        public DateTime CreationTimeStamp
        {
            get { return _creationTimeStamp; }
            set { this._creationTimeStamp = value; }
        }

        /// <summary>
        /// Token Id, assigned bu EJS.
        /// </summary>
        private Guid _id;
        public Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Id of user for which this token was issued.
        /// </summary>
        private Guid _userId;
        public Guid UserId
        {
            get { return _userId; }
            set { this._userId = value; }
        }

        /// <summary>
        /// Declares when this token expires.
        /// </summary>
        private DateTime _expireTimeStamp;
        public DateTime ExpireTimeStamp
        {
            get { return _expireTimeStamp; }
            set { this._expireTimeStamp = value; }
        }

        /// <summary>
        /// Not the actual machine Id, but rather
        /// an Id created for each token and shared
        /// between the Ejs and the client.
        /// </summary>
        private Guid _sourceHostId;
        public Guid SourceHostId
        {
            get { return _sourceHostId; }
            set { this._sourceHostId = value; }
        }

        /// <summary>
        /// Used internally to tell whether this
        /// token is currently authenticated.
        /// </summary>
        private bool _isAuthenticated;
        public bool IsAuthenticated
        {
            get { return _isAuthenticated; }
        }
        #endregion

        #region Constructors

        public ejsSessionToken()
        {
            this._creationTimeStamp = DateTime.Now;
            this._expireTimeStamp = DateTime.Now.AddMinutes(30);
            this._id = Guid.NewGuid();
            this._isAuthenticated = false;
            this._sourceHostId = Guid.NewGuid();
            this._userId = Guid.NewGuid();
        }

        public ejsSessionToken(Guid Id, Guid SourceId, Guid UserId, DateTime CreationTimeStamp,
            DateTime ExpireTimeStamp, bool IsAuthenticated)
        {
            this._creationTimeStamp = CreationTimeStamp;
            this._expireTimeStamp = ExpireTimeStamp;
            this._id = Id;
            this._isAuthenticated = IsAuthenticated;
            this._sourceHostId = SourceId;
            this._userId = UserId;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns a TimeSpan representing the remaining time until this token expires.
        /// </summary>
        /// <returns>A TimeSpan representing the time remaining until this token expires.
        ///          If the Token has not been authenticated, the return value is TimeSpan.Zero.</returns>
        public TimeSpan GetRemainingLifeTime()
        {
            try
            {
                if (this._isAuthenticated)
                    return this._creationTimeStamp.Subtract(this._expireTimeStamp);
                else
                    return TimeSpan.Zero;
            }
            catch (Exception)
            {
                //TODO: Add Logging code.
                this._isAuthenticated = false;
                throw new ApplicationException("Unable to calculate the remaining life time of the Session Id." +
                    "Since this is a potential security problem, this Session Id as been annulated.");
            }
        }

        /// <summary>
        /// Sets the IsAuthenticated property to False.
        /// </summary>
        public void Invalidate()
        {
            this._isAuthenticated = false;
        }

        #endregion
    }
}
