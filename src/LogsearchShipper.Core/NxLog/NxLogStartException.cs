using System;

namespace LogSearchShipper.Core.NxLog
{
	public class NxLogStartException : Exception
	{
		public NxLogStartException(string message) : base(message)
		{
		}
	}
}