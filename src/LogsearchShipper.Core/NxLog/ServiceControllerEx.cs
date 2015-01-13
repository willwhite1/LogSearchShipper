using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;

namespace LogSearchShipper.Core.NxLog
{
	static class ServiceControllerEx
	{
		public static void CreateService(string name, string filePath, string userName, string password)
		{
			DeleteService(name);

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
					NativeMethods.SERVICE_WIN32_OWN_PROCESS, NativeMethods.ServiceBootFlag.AutoStart, NativeMethods.ServiceError.Normal,
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
			StopService(name);

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
			try
			{
				var service = new ServiceController(name);
				service.Stop();
				service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(2));
			}
			catch (InvalidOperationException)
			{
				// the service doesn't exist
			}
		}

		public static int GetProcessId(string serviceName)
		{
			var searcher = new ManagementObjectSearcher(string.Format("SELECT ProcessId FROM Win32_Service WHERE Name='{0}'", serviceName));
			var moc = searcher.Get();
			foreach (var cur in moc)
			{
				var mo = (ManagementObject)cur;
				return Convert.ToInt32(mo["ProcessId"]);
			}

			throw new ApplicationException();
		}
	}
}
