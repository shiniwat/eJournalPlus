/// -----------------------------------------------------------------
/// ejsSessionToken.cs: data contract of server session token
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Serialization;

namespace EjsWcfService
{
	[DataContract]
	public sealed class ejsSessionToken
	{
		#region Data Members
		[DataMember]
		private DateTime _creationTimeStamp;
		public DateTime CreationTimeStamp
		{
			get { return _creationTimeStamp; }
		}

		[DataMember]
		private Guid _id;
		public Guid Id
		{
			get { return _id; }
		}

		[DataMember]
		private Guid _userId;
		public Guid UserId
		{
			get { return _userId; }
		}

		[DataMember]
		private DateTime _expireTimeStamp;
		public DateTime ExpireTimeStamp
		{
			get { return _expireTimeStamp; }
		}

		[DataMember]
		private Guid _sourceHostId;
		public Guid SourceHostId
		{
			get { return _sourceHostId; }
		}

		[DataMember]
		private bool _isAuthenticated;
		public bool IsAuthenticated
		{
			get { return _isAuthenticated; }
		}

		[DataMember]
		private string _firstName;
		public string FirstName
		{
			get { return _firstName; }
			set { _firstName = value; }
		}

		[DataMember]
		private string _lastName;
		public string LastName
		{
			get { return _lastName; }
			set { _lastName = value; }
		}

		internal string _dataBaseName = "";

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
			DateTime ExpireTimeStamp, bool IsAuthenticated, string firstName, string lastName)
		{
			this._firstName = firstName;
			this._lastName = lastName;
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
					return this._expireTimeStamp.Subtract(this._creationTimeStamp);
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
