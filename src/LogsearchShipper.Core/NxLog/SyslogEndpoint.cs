using System;

namespace LogSearchShipper.Core.NxLog
{
	public class SyslogEndpoint
	{
		private readonly string _host;
		private readonly int _port;
	    private readonly string _token;

	    public SyslogEndpoint(string host, int port, string token)
		{
			_host = host;
			_port = port;
	        _token = token;
		}

        public SyslogEndpoint(string host, int port) : this(host, port, string.Empty)
        {            
        }

		public string Host
		{
			get { return _host; }
		}

		public int Port
		{
			get { return _port; }
		}

	    public string Token
	    {
	        get { return _token; }
	    }
	}
}