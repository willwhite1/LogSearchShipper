using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;

using log4net;

namespace LogSearchShipper.Core.NxLog
{
	static class ServiceControllerEx
	{
		public static void CreateService(string name, string filePath, string userName, string password)
		{
			var scmHandle = IntPtr.Zero;
			var serviceHandle = IntPtr.Zero;

			try
			{
				scmHandle = NativeMethods.OpenSCManager(null, null, (int)NativeMethods.ScmAccessRights.AllAccess);

				if (scmHandle == IntPtr.Zero)
					throw new ApplicationException("Failed to open service manager", new Win32Exception());

				// NOTE when user name is empty, LocalService account is used by default
				if (userName == "")
					userName = null;
				if (password == "")
					password = null;

				serviceHandle = NativeMethods.CreateService(scmHandle, name, name, NativeMethods.ServiceAccessRights.AllAccess,
					NativeMethods.SERVICE_WIN32_OWN_PROCESS, NativeMethods.ServiceBootFlag.DemandStart, NativeMethods.ServiceError.Normal,
					filePath, null, IntPtr.Zero, null, userName, password);

				if (serviceHandle == IntPtr.Zero)
				{
					var message = string.Format("Failed to install service {0} : {1}", name, filePath);
					throw new ApplicationException(message, new Win32Exception());
				}
			}
			finally
			{
				if (serviceHandle != IntPtr.Zero)
					NativeMethods.CloseServiceHandle(serviceHandle);

				if (scmHandle != IntPtr.Zero)
					NativeMethods.CloseServiceHandle(scmHandle);
			}

			var args = string.Format(" failure \"{0}\" reset= 3600 actions= restart/60/restart/60/restart/60", name);
			ProcessUtils.Execute("sc", args);
		}

		public static void DeleteService(string name)
		{
			var scmHandle = IntPtr.Zero;
			var serviceHandle = IntPtr.Zero;

			try
			{
				scmHandle = NativeMethods.OpenSCManager(null, null, (int)NativeMethods.ScmAccessRights.AllAccess);

				serviceHandle = NativeMethods.OpenService(scmHandle, name, NativeMethods.ServiceAccessRights.AllAccess);
				if (serviceHandle != IntPtr.Zero)
				{
					if (!NativeMethods.DeleteService(serviceHandle))
					{
						var message = string.Format("Failed to delete service {0}", name);
						throw new ApplicationException(message, new Win32Exception());
					}
				}
			}
			finally
			{
				if (serviceHandle != IntPtr.Zero)
					NativeMethods.CloseServiceHandle(serviceHandle);

				if (scmHandle != IntPtr.Zero)
					NativeMethods.CloseServiceHandle(scmHandle);
			}
		}

		public static void StartService(string name)
		{
			var service = new ServiceController(name);
			service.Start();
			service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(1));
		}

		public static void StopService(string name)
		{
			var timeout = TimeSpan.FromMinutes(2);
			var processId = GetProcessId(name);
			var service = new ServiceController(name);
			var startTime = DateTime.UtcNow;

			try
			{
				service.Stop();
				return;
			}
			catch (InvalidOperationException exc)
			{
				var win32Exc = exc.InnerException as Win32Exception;
				if (win32Exc == null)
					throw;

				var errCode = win32Exc.NativeErrorCode;
				if (errCode == ERROR_SERVICE_DOES_NOT_EXIST || errCode == ERROR_SERVICE_NOT_ACTIVE)
					return;
				if (errCode != ERROR_SERVICE_REQUEST_TIMEOUT)
					throw;
			}

			Process process;
			try
			{
				process = Process.GetProcessById(processId);
			}
			catch (ArgumentException)
			{
				// there is no process with such Id
				return;
			}

			var timeoutLeft = timeout - (DateTime.UtcNow - startTime);
			if (!process.WaitForExit((int)timeoutLeft.TotalMilliseconds))
				Log.Warn(string.Format("The service {0} stop attempt has timed out", name));
		}

		private const int ERROR_SERVICE_REQUEST_TIMEOUT = 1053;
		private const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;
		private const int ERROR_SERVICE_NOT_ACTIVE = 1062;

        public static int GetProcessId(string serviceName)
        {
            var results = Process.GetProcessesByName("nxlog");
            if (results.Length == 0)
                return 0;

            if (results.Length != 1)
                throw new ApplicationException();

            return results[0].Id;

        }


		private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceControllerEx));
	}
}
