/// -----------------------------------------------------------------
/// RemoteUserOperations.cs: User Operations stuff
/// License: see http://ejournalplus.codeplex.com/license; All Rights follows the MS-PL
/// Current owner: shiniwa
/// The project decription: please refer to http://codeplex.com/ejournalplus/
/// -----------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace EjsWcfService.UserOp
{
	internal static class RemoteUserOperations
	{
		internal static void GetRegisteredUserList(SqlConnection dBconnection,
			int UserGroupId, out List<ejsUserInfo> result)
		{
			result = new List<ejsUserInfo>();

			SqlCommand command = new SqlCommand();
			SqlDataReader reader = null;
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "GetAllRegisteredUsers";

				command.Parameters.Add("UserGroupId", System.Data.SqlDbType.Int).Value = UserGroupId;

				reader = command.ExecuteReader();

				if (reader.HasRows)
				{
					while (reader.Read())
					{
						result.Add(new ejsUserInfo
						{
							UserName = reader.GetString(0),
							FirstName = reader.GetString(1),
							LastName = reader.GetString(2),
							Email = reader.GetString(3),
							DatabaseName = reader.GetString(4),
							IsAccountActive = reader.GetBoolean(5),
							Id = reader.GetGuid(6).ToString(),
							UserGroupId = reader.GetInt32(7)
						});
					}
				}

			}
			finally
			{
				command.Dispose();
				if (reader != null)
				{
					reader.Close();
					reader.Dispose();
				}
			}
		}

		internal static int RegisterNewUser(SqlConnection dBconnection,
			ejsUserInfo newUser, string userName, string password,
			bool isAccountActive, int userGroupId)
		{
			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "RegisterNewUser";

				command.Parameters.Add("UserName", SqlDbType.VarChar, 50);
				command.Parameters.Add("Password", SqlDbType.VarChar, 512);
				command.Parameters.Add("FirstName", SqlDbType.NVarChar, 100);
				command.Parameters.Add("LastName", SqlDbType.NVarChar, 100);
				command.Parameters.Add("Email", SqlDbType.VarChar, 128);
				command.Parameters.Add("DBName", SqlDbType.VarChar, 128);
				command.Parameters.Add("IsAccountActive", SqlDbType.Bit);
				command.Parameters.Add("UserGroupId", SqlDbType.Int);
				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier);

				command.Parameters[0].Value = userName;
				command.Parameters[1].Value = password;
				command.Parameters[2].Value = newUser.FirstName;
				command.Parameters[3].Value = newUser.LastName;
				command.Parameters[4].Value = newUser.Email;
				command.Parameters[5].Value = "meetF_" + newUser.Id.ToString();
				command.Parameters[6].Value = true;
				command.Parameters[7].Value = userGroupId;
				command.Parameters[8].Value = new Guid(newUser.Id);

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;
				return resultCode;

			}
			finally
			{
				command.Dispose();
			}
		}


		/// <summary>
		/// Updates the record for a user in the EJS
		/// </summary>
		/// <param name="operatorId">ID of the person performing the update</param>
		/// <param name="password">Send 'NoChange' for no update.</param>
		internal static int UpdateUserData(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsUserInfo userInfo, string password)
		{
			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "UpdateUser";

				command.Parameters.Add("OperatorId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("UserName", SqlDbType.VarChar, 50).Value = userInfo.UserName;
				command.Parameters.Add("FirstName", SqlDbType.NVarChar, 100).Value = userInfo.FirstName;
				command.Parameters.Add("LastName", SqlDbType.NVarChar, 100).Value = userInfo.LastName;
				command.Parameters.Add("IsAccountActive", SqlDbType.Bit).Value = userInfo.IsAccountActive;
				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = new Guid(userInfo.Id);
				command.Parameters.Add("Email", SqlDbType.VarChar, 128).Value = userInfo.Email;
				command.Parameters.Add("NewUserGroupId", SqlDbType.Int).Value = userInfo.UserGroupId;

				if (password != "")
					command.Parameters.Add("Password", SqlDbType.VarChar, 512).Value = password;
				else
					command.Parameters.Add("Password", SqlDbType.VarChar, 512).Value = "NoChange";

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;
				return resultCode;

			}
			finally
			{
				command.Dispose();
			}
		}

		/// <summary>
		/// Deletes the record of a user in the EJS
		/// </summary>
		internal static int DeleteUserData(SqlConnection dBconnection,
			ejsSessionToken sessionToken, ejsUserInfo userInfo)
		{
			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "DeleteUser";

				command.Parameters.Add("OperatorId", SqlDbType.UniqueIdentifier).Value = sessionToken.UserId;
				command.Parameters.Add("UserId", SqlDbType.UniqueIdentifier).Value = new Guid(userInfo.Id);

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;
				return resultCode;

			}
			finally
			{
				command.Dispose();
			}
		}

		/// <summary>
		/// Validates the provided credentials against the Ejs Database.
		/// </summary>
		/// <param name="dBconnection">Connection to use when communicating with the EJS Database.</param>
		/// <param name="userName">User Name part of the credentials.</param>
		/// <param name="password">Password part of the credentials.</param>
		/// <param name="token">A Token that will be updated with the user Id (Guid) from the database.</param>
		/// <returns>0 on success, 1 on failure to authenticate</returns>
		internal static int AuthenticateUserCredentials(SqlConnection dBconnection,
			string userName, string password, ref ejsSessionToken token)
		{
			/*TODO: Maybe rename this to LogIn or something similar...*/

			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "ValidateUserNamePassword";

				command.Parameters.Add("UserName", SqlDbType.VarChar, 50);
				command.Parameters.Add("Password", SqlDbType.VarChar, 512);
				command.Parameters[0].Value = userName;
				command.Parameters[1].Value = password;

				SqlParameter pmId = new SqlParameter("Id", SqlDbType.VarChar, 38);
				pmId.Direction = ParameterDirection.Output;
				command.Parameters.Insert(2, pmId);

				SqlParameter pmFirstName = new SqlParameter("FirstName", SqlDbType.VarChar, 100);
				pmFirstName.Direction = ParameterDirection.Output;
				command.Parameters.Insert(3, pmFirstName);

				SqlParameter pmLastName = new SqlParameter("LastName", SqlDbType.VarChar, 100);
				pmLastName.Direction = ParameterDirection.Output;
				command.Parameters.Insert(4, pmLastName);

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;

				if (resultCode == 0)
				{
					token = new ejsSessionToken(
									token.Id, token.SourceHostId, new Guid((string)pmId.Value), DateTime.Now,
									DateTime.Now.AddHours(12), true, (string)pmFirstName.Value, (string)pmLastName.Value);
					return 0;
				}
				else
					return 1;
			}
			finally
			{
				command.Dispose();
			}
		}

		/// <summary>
		/// Validates the provided credentials against the Ejs Database.
		/// </summary>
		/// <param name="dBconnection">Connection to use when communicating with the EJS Database.</param>
		/// <param name="userName">User Name part of the credentials.</param>
		/// <param name="oldPassword">Old Password part of the credentials.</param>
		/// <param name="newPassword">New Password part of the credentials.</param>
		/// <returns>0 on success, 1 on failure to authenticate</returns>
		internal static int UpdateUserPassword(SqlConnection dBconnection,
			string userName, string oldPassword, string newPassword)
		{
			/*TODO: Maybe rename this to LogIn or something similar...*/

			SqlCommand command = new SqlCommand();
			command.CommandTimeout = 60;

			try
			{
				command.Connection = dBconnection;
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.CommandText = "UpdateUserPassword";

				command.Parameters.Add("UserName", SqlDbType.VarChar, 50).Value = userName;
				command.Parameters.Add("OldPassword", SqlDbType.VarChar, 512).Value = oldPassword;
				command.Parameters.Add("NewPassword", SqlDbType.VarChar, 512).Value = newPassword;

				SqlParameter returnValue = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
				returnValue.Direction = ParameterDirection.ReturnValue;
				command.Parameters.Add(returnValue);

				command.ExecuteNonQuery();
				int resultCode = (int)returnValue.Value;
				return resultCode;
			}
			finally
			{
				command.Dispose();
			}
		}
	}
}
