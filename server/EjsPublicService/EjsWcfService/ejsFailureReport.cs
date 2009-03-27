/// -----------------------------------------------------------------
/// ejsFailureReport.cs: WCF data contracts about failure details.
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
	public class ejsFailureReport
	{
		[DataMember]
		private bool _isHandled;
		public bool IsHandled
		{
			get { return _isHandled; }
			set { _isHandled = value; }
		}

		[DataMember]
		private string _header;
		public string Header
		{
			get { return _header; }
		}

		[DataMember]
		private int _failureCode;
		public int FailureCode
		{
			get { return _failureCode; }
		}

		[DataMember]
		private string _message;
		public string Message
		{
			get { return _message; }
		}

		[DataMember]
		private DateTime _timeStamp;
		public DateTime TimeStamp
		{
			get { return _timeStamp; }
		}

		[DataMember]
		private Exception _originalException;
		public Exception OriginalException
		{
			get { return _originalException; }
		}

		public ejsFailureReport(int FailureCode, string Header,
			string Message, Exception OriginalException, bool IsHandled)
		{
			this._timeStamp = DateTime.Now;
			this._message = Message;
			this._header = Header;
			this._failureCode = FailureCode;
			this._isHandled = IsHandled;
			this._originalException = OriginalException;

			ejsLogHelper.LogMessage("Fault: " + Header, false);
			if (OriginalException != null)
				ejsLogHelper.LogMessage(OriginalException.Message, false);
			ejsLogHelper.LogMessage(Message, true);

		}
	}
}
