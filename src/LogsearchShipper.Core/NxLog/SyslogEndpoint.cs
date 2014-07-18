namespace LogSearchShipper.Core.NxLog
{
	public class SyslogEndpoint
	{
		private readonly string _host;
		private readonly int _port;

		public SyslogEndpoint(string host, int port)
		{
			_host = host;
			_port = port;
		}

		public string Host
		{
			get { return _host; }
		}

		public int Port
		{
			get { return _port; }
		}
	}
}