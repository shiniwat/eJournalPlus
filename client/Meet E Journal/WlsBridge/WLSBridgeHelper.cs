using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Xml.XPath;
using Microsoft.WindowsLive.Id.Client;

namespace WlsBridge
{
	public enum WlsBridgeStatusCode
	{
		Idle,
		AuthenticationError,
		ConnectivityError,
		DataFormatError,
		UserAuthenticated,
		LogOutError,
		XmlRpcError
	}

	public class WLSBridgeHelper
	{
		private Identity _userId;
		private IdentityManager _manager;
		private string _userAgent = "WindowsLiveSpaces CustomHelper";
		private string _albumName = "Blog Images";	//	this name is used by WindowsLiveWriter.

		public WlsBridgeStatusCode LastStatusCode { get; private set; }
		public string LastStatusMessage { get; private set; }

		private const string _localAppIDString =
			"EJournalPlus";

		private string _userSpaceUri = string.Empty;

		/// <summary>
		/// Constructor, parameterless
		/// </summary>
		public WLSBridgeHelper(string userSpaceUri)
		{
			this._userSpaceUri = userSpaceUri;
			_manager = IdentityManager.CreateInstance(_localAppIDString, "eJournalPlus");
		}

		/// <summary>
		/// Clear authenticated user, forces login at next connection attempt.
		/// </summary>
		public void ResetAuthenticate()
		{
			if (_userId != null)
				_userId = null;
		}

		/// <summary>
		/// Authenticate the user using WindowsLive.Id.Client.dll
		/// </summary>
		private void Authenticate()
		{
			if (_userId == null)
			{
				_userId = _manager.CreateIdentity();
				if (_userId.IsAuthenticated)
					Debug.WriteLine("userID = " + _userId.UserName);
				else
				{
					bool authenticated = _userId.Authenticate();
					if (authenticated)
						this.LastStatusCode = WlsBridgeStatusCode.UserAuthenticated;
					else
						this.LastStatusCode = WlsBridgeStatusCode.AuthenticationError;
				}
			}
		}

		/// <summary>
		/// Post a blog entry to a live space.
		/// </summary>
		/// <param name="subject">Title of the post</param>
		/// <param name="body">Content of the post</param>
		public void PostBlog(string subject, string body)
		{
			this.Authenticate();
			if (this.LastStatusCode == WlsBridgeStatusCode.AuthenticationError)
				return;

			string spacePrefix = GetSpaceNamePrefix();
			const string SPACES_API_URL = "https://storage.msn.com/storageservice/MetaWeblog.rpc";
			const string requestXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><methodCall><methodName>metaWeblog.newPost</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param><param><value><string>{2}</string></value></param><param><value><struct><member><name>title</name><value><string>{3}</string></value></member><member><name>link</name><value><string /></value></member><member><name>description</name><value><string>{4}</string></value></member><member><name>categories</name><value><array><data /></array></value></member></struct></value></param><param><value><boolean>1</boolean></value></param></params></methodCall>";

			subject = ReplaceInvalidXMLChars(subject);
			body = ReplaceInvalidXMLChars(body);
			byte[] postData = new UTF8Encoding(false).GetBytes(String.Format(requestXml, "MyBlog", spacePrefix, "", subject, body));
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(SPACES_API_URL);
			string ticket = "";
			//Get ticket for storage service.
			try
			{
				ticket = _userId.GetTicket("storage.msn.com", "MBI", true);
			}
			catch (WLLogOnException wlsEx)
			{
				if (wlsEx.FlowUrl != null)
					this.LastStatusMessage = wlsEx.ErrorString + "Please go to " +
						wlsEx.FlowUrl.AbsoluteUri + "to correct the condition that caused the error";
				else
					this.LastStatusMessage = wlsEx.ErrorString;

				this.LastStatusCode = WlsBridgeStatusCode.AuthenticationError;

				return;	//	cannot continue
			}

			request.Headers.Add("Authorization", "WLID1.0 " + ticket);
			request.AllowAutoRedirect = false;
			request.UserAgent = _userAgent;
			request.ContentType = "text/xml";
			request.Pipelined = false;
			request.ProtocolVersion = HttpVersion.Version10;
			request.Method = "POST";
			using (Stream requestStream = request.GetRequestStream())
			{
				requestStream.Write(postData, 0, postData.Length);
			}
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			string rcode = response.StatusCode.ToString();
			System.Diagnostics.Debug.WriteLine("responseCode = " + rcode);

			this.OpenSpaceInBrowser();

			this.LastStatusCode = WlsBridgeStatusCode.Idle;

		}

		private void OpenSpaceInBrowser()
		{
			//Try opening a pre-authenticated browser window to view the user's blog.
			try
			{
				string spacePrefix = GetSpaceNamePrefix();
				this._userId.OpenAuthenticatedBrowser("http://" + spacePrefix + ".spaces.live.com/blog/", "lbi");
			}
			catch (WLLogOnException)
			{
				//Fail Silently.
			}
		}

		/// <summary>
		/// Properly format a string to escape all invalid XML chars...
		/// </summary>
		/// <param name="content">String to clean</param>
		/// <returns>Valid XML string</returns>
		public static string ReplaceInvalidXMLChars(string content)
		{
			//inString = inString.Replace("&", "&amp;");
			content = content.Replace("<", "&lt;");
			content = content.Replace(">", "&gt;");
			content = content.Replace("\"", "&quot;");
			content = content.Replace("'", "&apos;");

			return content;
		}

		/// <summary>
		/// Get the space name prefix.
		/// e.g. if the target space is ejp.spaces.live.com, the prefix would be "ejp"
		/// </summary>
		/// <returns>The prefix part of the spaces url</returns>
		internal string GetSpaceNamePrefix()
		{
			Authenticate();
			if (this.LastStatusCode == WlsBridgeStatusCode.AuthenticationError)
				return "";

			const string SPACES_ROOT = "http://cid-{0}.spaces.live.com/";

			string targetUrl = string.Format(SPACES_ROOT, _userId.cId);
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUrl);
			string ticket = "";
			//Get ticket for storage service.
			try
			{
				ticket = _userId.GetTicket("spaces.live.com", "MBI", true);
			}
			catch (WLLogOnException wlsEx)
			{
				if (wlsEx.FlowUrl != null)
					this.LastStatusMessage = wlsEx.ErrorString + "Please go to " +
						wlsEx.FlowUrl.AbsoluteUri + "to correct the condition that caused the error";
				else
					this.LastStatusMessage = wlsEx.ErrorString;

				this.LastStatusCode = WlsBridgeStatusCode.AuthenticationError;
				return null;	//	cannot continue
			}
			request.Headers.Add("Authorization", "WLID1.0 " + ticket);
			request.AllowAutoRedirect = false;
			request.UserAgent = _userAgent;
			request.Pipelined = false;
			request.ProtocolVersion = HttpVersion.Version10;
			request.Method = "GET";
			string spacePrefix = "";
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				string rcode = response.StatusCode.ToString();
				// rcode should be 302 (Found).
				Debug.WriteLine("responseCode = " + rcode);
				spacePrefix = response.GetResponseHeader("Location");
				//	intersted in the very 1st token only.
				string delimiter = ".";
				string[] tokens = spacePrefix.Split(delimiter.ToCharArray());
				if (tokens.Length > 0)
				{
					spacePrefix = tokens[0];
				}
				int start = spacePrefix.IndexOf("//");
				if (start > 0)
				{
					spacePrefix = spacePrefix.Substring(start + 2);
				}
			}
			catch (WebException)
			{
				this.LastStatusCode = WlsBridgeStatusCode.ConnectivityError;
			}

			this.LastStatusCode = WlsBridgeStatusCode.Idle;
			return spacePrefix;
		}

		/// <summary>
		/// We need at least one photo album to populate arbitrary blog images...
		/// The album name is predefined as "Blog Images", which is the same name as LiveWriter uses.
		/// This method ensures the photo album exists already.
		/// </summary>
		public void EnsureAlbumExist()
		{
			Authenticate();
			if (this.LastStatusCode == WlsBridgeStatusCode.AuthenticationError)
				return;

			const string PHOTO_WEBDAV_ROOT = "https://storage.msn.com/mydata/myspace/spacefolder/photoalbums/";

			string targetUrl = PHOTO_WEBDAV_ROOT;
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUrl);
			string ticket = "";
			//Get ticket for storage service.
			try
			{
				ticket = _userId.GetTicket("storage.msn.com", "MBI", true);
			}
			catch (WLLogOnException wlsEx)
			{
				if (wlsEx.FlowUrl != null)
					this.LastStatusMessage = wlsEx.ErrorString + "Please go to " +
						wlsEx.FlowUrl.AbsoluteUri + "to correct the condition that caused the error";
				else
					this.LastStatusMessage = wlsEx.ErrorString;

				this.LastStatusCode = WlsBridgeStatusCode.AuthenticationError;
				return;	//	cannot continue
			}
			request.Headers.Add("Authorization", "WLID1.0 " + ticket);
			request.AllowAutoRedirect = false;
			request.UserAgent = _userAgent;
			request.Pipelined = false;
			request.ProtocolVersion = HttpVersion.Version11;
			request.Method = "GET";
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				string rcode = response.StatusCode.ToString();
				System.Diagnostics.Debug.WriteLine("responseCode = " + rcode);
				using (Stream stream = response.GetResponseStream())
				{
					bool folderExists = false;
					XPathDocument document = new XPathDocument(stream);
					XPathNavigator navigator = document.CreateNavigator();
					XPathNodeIterator iterator = navigator.Select("/Folder/Items/Folder/RelationshipName");
					while (iterator.MoveNext())
					{
						string relationshipName = iterator.Current.Value;
						if (string.Compare(relationshipName, _albumName, true) == 0)
						{
							folderExists = true;
							break;
						}
					}

					if (!folderExists)
					{
						MakeAlbum();
						if (this.LastStatusCode == WlsBridgeStatusCode.AuthenticationError)
							return;
					}
				}
			}
			catch (WebException)
			{
				this.LastStatusCode = WlsBridgeStatusCode.ConnectivityError;
			}

			this.LastStatusCode = WlsBridgeStatusCode.Idle;
		}

		/// <summary>
		/// Create "Blog Images" photo album (only if it doesn't exist already).
		/// </summary>
		private void MakeAlbum()
		{
			Authenticate();
			if (this.LastStatusCode == WlsBridgeStatusCode.AuthenticationError)
				return;

			const string PHOTO_API_URL = "https://storage.msn.com/mydata/myspace/spacefolder/photoalbums/{0}";

			string targetUrl = string.Format(PHOTO_API_URL, _albumName);
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUrl);
			string ticket = "";
			//Get ticket for storage service.
			try
			{
				ticket = _userId.GetTicket("storage.msn.com", "MBI", true);
			}
			catch (WLLogOnException wlsEx)
			{
				if (wlsEx.FlowUrl != null)
					this.LastStatusMessage = wlsEx.ErrorString + "Please go to " +
						wlsEx.FlowUrl.AbsoluteUri + "to correct the condition that caused the error";
				else
					this.LastStatusMessage = wlsEx.ErrorString;

				this.LastStatusCode = WlsBridgeStatusCode.AuthenticationError;
				return;	//	cannot continue
			}
			request.Headers.Add("Authorization", "WLID1.0 " + ticket);
			request.AllowAutoRedirect = false;
			request.UserAgent = _userAgent;
			request.Pipelined = false;
			request.ProtocolVersion = HttpVersion.Version11;
			request.Method = "MKCOL";
			try
			{
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				string rcode = response.StatusCode.ToString();
				System.Diagnostics.Debug.WriteLine("responseCode = " + rcode);
			}
			catch (WebException)
			{
				this.LastStatusCode = WlsBridgeStatusCode.ConnectivityError;
			}

			this.LastStatusCode = WlsBridgeStatusCode.Idle;
		}

		/// <summary>
		/// Put a photo into "Blog Images" album.
		/// </summary>
		/// <param name="photoName">The name of photo (display name in Photo Album)</param>
		/// <param name="dataStream">MemoryStream, which represents actual picture (JPEG, PNG, or whatever)</param>
		/// <param name="contentType">image/png, image/jpeg, etc</param>
		/// <returns></returns>
		public string PutPhoto(string photoName, byte[] data, string contentType)
		{
			Authenticate();
			if (this.LastStatusCode == WlsBridgeStatusCode.AuthenticationError)
				return "";

			const string PHOTO_API_URL = "https://storage.msn.com/mydata/myspace/spacefolder/photoalbums/{0}/{1}";

			string targetUrl = string.Format(PHOTO_API_URL, _albumName, photoName);
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUrl);
			string ticket = "";
			//Get ticket for storage service.
			try
			{
				ticket = _userId.GetTicket("storage.msn.com", "MBI", true);
			}
			catch (WLLogOnException wlsEx)
			{
				if (wlsEx.FlowUrl != null)
					this.LastStatusMessage = wlsEx.ErrorString + "Please go to " +
						wlsEx.FlowUrl.AbsoluteUri + "to correct the condition that caused the error";
				else
					this.LastStatusMessage = wlsEx.ErrorString;

				this.LastStatusCode = WlsBridgeStatusCode.AuthenticationError;
				return null;	//	cannot continue
			}
			request.Headers.Add("Authorization", "WLID1.0 " + ticket);
			request.AllowAutoRedirect = false;
			request.UserAgent = _userAgent;
			request.Pipelined = false;
			request.ProtocolVersion = HttpVersion.Version11;
			request.Method = "PUT";
			request.ContentType = contentType; // e.g. "image/png"
			string location = "";
			try
			{
				using (Stream requestStream = request.GetRequestStream())
				{
					requestStream.Write(data, 0, data.Length);
				}

				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				string rcode = response.StatusCode.ToString();
				System.Diagnostics.Debug.WriteLine("responseCode = " + rcode);
				location = response.GetResponseHeader("Location");
			}
			catch (WebException)
			{
				this.LastStatusCode = WlsBridgeStatusCode.ConnectivityError;
			}
			catch (NullReferenceException)
			{
				this.LastStatusCode = WlsBridgeStatusCode.ConnectivityError;
			}

			this.LastStatusCode = WlsBridgeStatusCode.Idle;
			return location;
		}
	}
}
