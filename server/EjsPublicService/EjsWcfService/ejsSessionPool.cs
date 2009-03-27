/// -----------------------------------------------------------------
/// ejsSessionPool.cs: helper class to manage server sessions.
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EjsWcfService
{
	internal class ejsSessionPool
	{
		/// <summary>
		/// Holds a dictionary of all current sessions.
		/// </summary>
		private static Dictionary<Guid, ejsSessionUserData> _sessionDictionary;

		private object threadLock = new object();

		/// <summary>
		/// Always make sure the session Dictionary exists.
		/// </summary>
		static ejsSessionPool()
		{
			if (ejsSessionPool._sessionDictionary == null)
				ejsSessionPool._sessionDictionary = new Dictionary<Guid, ejsSessionUserData>();
		}

		internal ejsSessionPool() { }

		/// <summary>
		/// Returns an array of all sessionTokens in the pool, without
		/// host Ids.
		/// </summary>
		internal ejsSessionToken[] GetSafePoolCopy()
		{
			try
			{
				List<ejsSessionToken> result = new List<ejsSessionToken>();

				foreach (KeyValuePair<Guid, ejsSessionUserData> record in
					ejsSessionPool._sessionDictionary)
				{
					ejsSessionToken t = new ejsSessionToken(
						Guid.Empty, Guid.Empty, record.Value.SessionToken.UserId,
						record.Value.SessionToken.CreationTimeStamp,
						record.Value.SessionToken.ExpireTimeStamp,
						record.Value.SessionToken.IsAuthenticated,
						record.Value.SessionToken.FirstName,
						record.Value.SessionToken.LastName);

					result.Add(t);
				}

				return result.ToArray();
			}
			catch (Exception)
			{
				throw;
			}
		}

		/// <summary>
		/// Adds an authenticated session with a corresponding session token Id
		/// to the session dictionary.
		/// </summary>
		internal bool AddAuthenticatedSession(ejsSessionToken Token, ejsSessionUserData UserData)
		{
			lock (this.threadLock)
			{
				try
				{
					if (ejsSessionPool._sessionDictionary.Keys.Contains(Token.Id))
					{
						string message = "The given user Token Id already exists in the session pool.\n\n" +
							"Cannot add the same user Token Id twice.";
						ejsLogHelper.LogMessage(message, false);
						throw new ApplicationException(message);
					}
					ejsSessionPool._sessionDictionary.Add(Token.Id, UserData);
					return true;
				}
				catch (Exception)
				{
					throw;
				}
			}
		}

		internal ejsSessionUserData GetUserDataByTokenId(Guid TokenId)
		{
			lock (this.threadLock)
			{
				return ejsSessionPool._sessionDictionary[TokenId];
			}
		}

		internal int GetCount()
		{
			int count = 0;
			lock (this.threadLock)
			{
				count = ejsSessionPool._sessionDictionary.Count;
			}
			return count;
		}

		internal void ResetPool()
		{
			lock (this.threadLock)
			{
				ejsSessionPool._sessionDictionary.Clear();
			}
		}

		internal void InvalidateSession(Guid TokenId)
		{
			lock (this.threadLock)
			{
				ejsSessionPool._sessionDictionary.Remove(TokenId);
			}
		}

		internal bool ValidateSessionByToken(ejsSessionToken claimsToken)
		{

			bool validationResult = false;

			if (ejsSessionPool._sessionDictionary.ContainsKey(claimsToken.Id))
			{
				if (claimsToken.SourceHostId ==
					ejsSessionPool._sessionDictionary[claimsToken.Id].SessionToken.SourceHostId)
				{
					if (ejsSessionPool._sessionDictionary[claimsToken.Id].SessionToken.IsAuthenticated)
					{
						if (ejsSessionPool._sessionDictionary[claimsToken.Id].
							SessionToken.GetRemainingLifeTime().TotalMilliseconds > 0.0000)
						{
							validationResult = true;
						}
					}
				}
			}

			//Provide some simple logging on failure.
			if (validationResult == false)
			{
				ejsLogHelper.LogMessage("Token validation failed for Token: " +
					claimsToken.Id.ToString(), false);
			}

			return validationResult;
		}

		internal void CyclePool()
		{
			lock (this.threadLock)
			{

				List<Guid>
					tokensToRemove = new List<Guid>();

				foreach (KeyValuePair<Guid, ejsSessionUserData> record in
					ejsSessionPool._sessionDictionary)
				{
					if (record.Value.SessionToken.GetRemainingLifeTime().TotalMilliseconds < 0.0000)
					{
						tokensToRemove.Add(record.Key);
					}
				}

				foreach (Guid t in tokensToRemove)
					ejsSessionPool._sessionDictionary.Remove(t);
			}
		}
	}
}
