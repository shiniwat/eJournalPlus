using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsLive.Id.Client;
using System.Collections;
using System.Net;
using System.IO;

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

    public static class WindowsLiveBridge
    {
        public static bool IsUserAthenticated { get; private set; }
        public static WlsBridgeStatusCode LastStatusCode { get; private set; }
        public static string LastStatusMessage { get; private set; }
        public static string CurrentUserName { get; private set; }

        private static string _localAppIDString =
            "MEET;henric@siliconstudio.co.jp;EJournalPlus";

        private const string SPACES_API_URL = "https://storage.msn.com/storageservice/MetaWeblog.rpc";
        private const string requestXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><methodCall><methodName>metaWeblog.newPost</methodName><params><param><value><string>{0}</string></value></param><param><value><string>{1}</string></value></param><param><value><string>{2}</string></value></param><param><value><struct><member><name>title</name><value><string>{3}</string></value></member><member><name>link</name><value><string /></value></member><member><name>description</name><value><string>{4}</string></value></member><member><name>categories</name><value><array><data /></array></value></member></struct></value></param><param><value><boolean>1</boolean></value></param></params></methodCall>";

        private static IdentityManager _oIDmgr = null;
        private static Identity _oID;

        /// <summary>
        /// Opens an authenticated browser.
        /// </summary>
        private static WlsBridgeStatusCode OpenSpaceInBrowser(string spaceName)
        {
            try
            {
                WindowsLiveBridge._oID.OpenAuthenticatedBrowser("http://" + spaceName + ".spaces.live.com/blog/", "mbi");
                WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.Idle;
                return WlsBridgeStatusCode.Idle;
            }
            catch (WLLogOnException wlsEx)
            {
                if (wlsEx.FlowUrl != null)
                    WindowsLiveBridge.LastStatusMessage = wlsEx.ErrorString +
                        "More info at: " + wlsEx.FlowUrl.AbsoluteUri;
                else
                    WindowsLiveBridge.LastStatusMessage = wlsEx.ErrorString;

                WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.AuthenticationError;
                return WlsBridgeStatusCode.AuthenticationError;
            }
        }

        /// <summary>
        /// Publish a Report to a Windows Live Space.
        /// A 'Report' in this context is a complete post including
        /// a screenshot of a KnowledgeMap.
        /// </summary>
        /// <param name="reportBody">Textual body of the Report.</param>
        /// <param name="knowledgeMap">Byte array rep. of a KnowledgeMap screenshot.</param>
        /// <returns></returns>
        public static WlsBridgeStatusCode PublishReport(string reportBody, string postTitle, string knowledgeMap, byte[] kmByteArray, string spaceName)
        {
            //If user is not authenticated, run the authentication proc.
            if (!WindowsLiveBridge.IsUserAthenticated)
            {
                //Authenticate User
                WindowsLiveBridge.AuthenticateUser();
                if (WindowsLiveBridge.LastStatusCode != WlsBridgeStatusCode.UserAuthenticated)
                    return WindowsLiveBridge.LastStatusCode;
            }

            //We made it through the login, now lets try pushing som data to the Space.
            if (WindowsLiveBridge.PublishBlogPostData_RPCXML(reportBody, postTitle, knowledgeMap, kmByteArray, spaceName) != WlsBridgeStatusCode.Idle)
                return WindowsLiveBridge.LastStatusCode;

            //Last, lets open a browser and show the result:
            if (WindowsLiveBridge.OpenSpaceInBrowser(spaceName) != WlsBridgeStatusCode.Idle)
                return WindowsLiveBridge.LastStatusCode;

            return WlsBridgeStatusCode.Idle;
        }

        /// <summary>
        /// Authenticates a User by displaying the Log In dialog.
        /// </summary>
        private static void AuthenticateUser()
        {
            //Instantiate the IdentityManager and Register an Application ID
            if (WindowsLiveBridge._oIDmgr == null)
            {
                try
                {
                    WindowsLiveBridge._oIDmgr =
                        IdentityManager.CreateInstance(
                        WindowsLiveBridge._localAppIDString, "Windows Live ID Client");
                }
                catch (WLLogOnException wlsEx)
                {
                    WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.ConnectivityError;

                    if (wlsEx.FlowUrl != null)
                        WindowsLiveBridge.LastStatusMessage = wlsEx.ErrorString +
                            "More info at: " + wlsEx.FlowUrl.AbsoluteUri;
                    else
                        WindowsLiveBridge.LastStatusMessage = wlsEx.ErrorString;
                    return;
                }
            }

            //Instantiate the Identity Object and display the login screen
            try
            {
                WindowsLiveBridge._oID = WindowsLiveBridge._oIDmgr.CreateIdentity();
                //If authentication completed,
                if (WindowsLiveBridge._oID.Authenticate())
                {
                    WindowsLiveBridge.CurrentUserName = WindowsLiveBridge._oID.UserName;
                    WindowsLiveBridge.IsUserAthenticated = true;
                    WindowsLiveBridge.LastStatusMessage = "User Athenticated";
                    WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.UserAuthenticated;
                    return;
                }
            }
            catch (WLLogOnException wlsEx)
            {
                WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.AuthenticationError;

                if (wlsEx.FlowUrl != null)
                    WindowsLiveBridge.LastStatusMessage = wlsEx.ErrorString +
                        "More info at: " + wlsEx.FlowUrl.AbsoluteUri;
                else
                    WindowsLiveBridge.LastStatusMessage = wlsEx.ErrorString;

                return;
            }
        }


        /// <summary>
        /// Pushes a blog post into a spaces blog.
        /// </summary>
        /// <param name="postBody">Body of the Post</param>
        /// <param name="subject">Subject (Title) of the Post</param>
        /// <param name="knowledgeMapBits"></param>
        /// <param name="kmByteArray"></param>
        /// <param name="spaceUrl">Url to the space that this post will be published to</param>
        private static WlsBridgeStatusCode PublishBlogPostData(string postBody, string subject,
            string knowledgeMapBits, byte[] kmByteArray, string spaceUrl)
        {
            spaceUrl = WindowsLiveBridge.ReplaceInvalidXMLChars(spaceUrl);
            subject = WindowsLiveBridge.ReplaceInvalidXMLChars(subject);
            postBody = WindowsLiveBridge.ReplaceInvalidXMLChars(postBody);
            byte[] postData =
                new UTF8Encoding(false).GetBytes(String.Format(requestXml, "MyBlog", spaceUrl, "", subject, postBody));
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(WindowsLiveBridge.SPACES_API_URL);
            string ticket = "";

            try
            {
                ticket = WindowsLiveBridge._oID.GetTicket("storage.msn.com", "MBI", true);
            }
            catch (XmlRpcFaultException xrfe)
            {
                WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.XmlRpcError;

                if (xrfe.HelpLink != null)
                    WindowsLiveBridge.LastStatusMessage = xrfe.FaultString +
                        "More info at: " + xrfe.HelpLink;
                else
                    WindowsLiveBridge.LastStatusMessage = xrfe.FaultString;

                return WlsBridgeStatusCode.AuthenticationError;
            }

            request.Headers.Add("Authorization", "WLID1.0 " + ticket);
            request.AllowAutoRedirect = false;
            request.UserAgent = "EJournal Plus Client v1";
            request.ContentType = "text/xml";
            request.Pipelined = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Method = "POST";
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(postData, 0, postData.Length);
            }
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if(response.StatusCode == HttpStatusCode.OK)
                return WlsBridgeStatusCode.Idle;
            else
                return WlsBridgeStatusCode.ConnectivityError;
        }



        /// <summary>
        /// Pushes a blog post into a spaces blog.
        /// </summary>
        /// <param name="postBody">Body of the Post</param>
        /// <param name="subject">Subject (Title) of the Post</param>
        /// <param name="spaceUrl">Url to the space that this post will be published to</param>
        private static WlsBridgeStatusCode PublishBlogPostData_RPCXML(string postBody, string subject, 
            string knowledgeMapBits, byte[] kmByteArray, string spaceUrl)
        {
            if (!WindowsLiveBridge.IsUserAthenticated)
                throw new ApplicationException("Must log in user before trying to publish.");

            MsnSpacesMetaWeblog mw = new MsnSpacesMetaWeblog();
            string username = "ejpDev";
            string password = "ejpTest";
            mw.Credentials = new NetworkCredential(username, password);

            try
            {
                //Since Spaces does not support the newMediaObject we cannot use this approach...
                ////First post the image
                //MediaObject mo = new MediaObject()
                //{
                //    name = "",
                //    type = "image/png",
                //    url = "",
                //    bits = knowledgeMapBits
                //};
                //mo = mw.newMediaObject("MyBlog", username, password, mo);
                
                ////First post the image
                //WindowsLiveBridge.PublishKnowledgeMap(subject + "_KnowledgeMap.png", kmByteArray);

                //Then post the text
                Post post = new Post()
                {
                    categories = new string[] { "EJournal Plus Reports" },
                    title = subject,
                    description = postBody,
                    dateCreated = DateTime.Now
                };
                //First param must be the keyword 'MyBlog', that signifies that the post is to be
                //added to the users blog.
                string postID = mw.newPost("MyBlog", username, password, post, true);

                WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.Idle;
                WindowsLiveBridge.LastStatusMessage = "Post Published (ID: " + postID + ")";
                return WlsBridgeStatusCode.Idle;
            }
            catch (XmlRpcFaultException xrfe)
            {
                WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.XmlRpcError;

                if (xrfe.HelpLink != null)
                    WindowsLiveBridge.LastStatusMessage = xrfe.FaultString +
                        "More info at: " + xrfe.HelpLink;
                else
                    WindowsLiveBridge.LastStatusMessage = xrfe.FaultString;

                return WlsBridgeStatusCode.XmlRpcError;
            }
        }


        private static bool PublishKnowledgeMap(string kmTitle, byte[] kmByteArray)
        {
            if (!WindowsLiveBridge.IsUserAthenticated)
                throw new ApplicationException("Must log in user before trying to publish.");

            String strURL = "https://cumulus.services.live.com/" + WindowsLiveBridge.CurrentUserName + "@live.com/SpacesPhotos/EJournalKMs/" + kmTitle;
            HttpWebRequest serviceRequest = (HttpWebRequest)WebRequest.Create(strURL);
            serviceRequest.UserAgent = "E Journal Plus Client"; 
            serviceRequest.Method = "PUT";
            serviceRequest.ContentType = "image/jpeg";
            //serviceRequest.Headers.Add("Authorization", "DomainAuthToken at=\"" + WindowsLiveBridge._oID.GetTicket("http://live.com", "MBI", true) + "\"");
            //serviceRequest.Headers.Add("Authorization", "WLID1.0 t=\"" + _oID.ExportAuthString() + "\"");
            serviceRequest.Headers.Add("Authorization", "DelegationToken dt=\"" + _oID.GetTicket("http://live.com", "MBI", true) + "\"");
            using (MemoryStream kmMS = new MemoryStream(kmByteArray))
            {
                using (Stream newStream = serviceRequest.GetRequestStream())
                {
                    CopyStream(kmMS, newStream);
                }
            }

            HttpWebResponse response = (HttpWebResponse)serviceRequest.GetResponse();
            String strResponse = response.StatusCode.ToString();

            return true;
        }

        private static void CopyStream(Stream istream, Stream ostream)
        {
            byte[] buffer = new byte[2048];
            int bytes;
            while (0 < (bytes = istream.Read(buffer, 0, buffer.Length)))
                ostream.Write(buffer, 0, bytes);
        }

        /// <summary>
        /// Replaces chars that are invalid in a well formatted xml string.
        /// </summary>
        public static string ReplaceInvalidXMLChars(string inString)
        {
            inString = inString.Replace("<", "\\<");
            inString = inString.Replace(">", "\\>");
            inString = inString.Replace("\"", "&quot;");
            inString = inString.Replace("'", "&apos;");
            inString = inString.Replace("&", "&amp;");

            return inString;
        }

        /// <summary>
        /// Loggs out the current user and cleans up resources.
        /// </summary>
        public static WlsBridgeStatusCode Close()
        {
            //If the user is logged in, log out and clean up.
            if (WindowsLiveBridge._oID != null)
            {
                try
                {
                    WindowsLiveBridge._oID.CloseIdentityHandle();
                    WindowsLiveBridge.CurrentUserName = "";
                    WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.Idle;
                    WindowsLiveBridge.LastStatusMessage = "User Logged out.";
                    WindowsLiveBridge.IsUserAthenticated = false;

                    return WlsBridgeStatusCode.Idle;
                }
                catch (WLLogOnException wlsEx)
                {
                    WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.LogOutError;

                    if (wlsEx.FlowUrl != null)
                        WindowsLiveBridge.LastStatusMessage = wlsEx.ErrorString +
                            "More info at: " + wlsEx.FlowUrl.AbsoluteUri;
                    else
                        WindowsLiveBridge.LastStatusMessage = wlsEx.ErrorString;

                    return WlsBridgeStatusCode.LogOutError;
                }
                catch (Exception)
                {
                    WindowsLiveBridge.LastStatusMessage = "Unknown Error";
                    return WlsBridgeStatusCode.ConnectivityError;
                }
            }
            else
            {
                WindowsLiveBridge.LastStatusCode = WlsBridgeStatusCode.Idle;
                return WlsBridgeStatusCode.Idle;
            }
        }

    }
}