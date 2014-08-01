﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;

namespace LogSearchShipper.Core.NxLog
{
	static class ServiceControllerEx
	{
		public static void CreateService(string name, string filePath)
		{
			DeleteService(name);

			var scmHandle = IntPtr.Zero;
			var serviceHandle = IntPtr.Zero;

			try
			{
				scmHandle = NativeMethods.OpenSCManager(null, null, (int)NativeMethods.ScmAccessRights.AllAccess);

				serviceHandle = NativeMethods.CreateService(scmHandle, name, name, NativeMethods.ServiceAccessRights.AllAccess,
					NativeMethods.SERVICE_WIN32_OWN_PROCESS, NativeMethods.ServiceBootFlag.AutoStart, NativeMethods.ServiceError.Normal,
					filePath, null, IntPtr.Zero, null, null, null);

				if (serviceHandle == IntPtr.Zero)
					throw new ApplicationException("Failed to install service.");
			}
			finally
			{
				if (serviceHandle != IntPtr.Zero)
					NativeMethods.CloseServiceHandle(serviceHandle);

				if (scmHandle != IntPtr.Zero)
					NativeMethods.CloseServiceHandle(scmHandle);
			}
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
					NativeMethods.DeleteService(serviceHandle);
					NativeMethods.CloseServiceHandle(serviceHandle);
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
			service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
		}

		public static void StopService(string name)
		{
			try
			{
				var service = new ServiceController(name);
				service.Stop();
				service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
			}
			catch (InvalidOperationException)
			{
				// service doesn't exist
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
