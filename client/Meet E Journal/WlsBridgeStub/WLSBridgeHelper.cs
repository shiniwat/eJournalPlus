using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WlsBridgeStub
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
        public WlsBridgeStatusCode LastStatusCode { get; private set; }
        public WLSBridgeHelper(string uriString)
        {
        }


        /// <summary>
        /// Clear authenticated user, forces login at next connection attempt.
        /// </summary>
        public void ResetAuthenticate()
        {
        }

        public void PostBlog(string subject, string body)
        {
            // do nothing.
        }

        public string PutPhoto(string photoName, byte[] data, string contentType)
        {
            return string.Empty;
        }

        public void EnsureAlbumExist()
        {
        }
    }
}
