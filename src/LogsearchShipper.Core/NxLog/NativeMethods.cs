using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LogSearchShipper.Core.NxLog
{
	static class NativeMethods
	{
		[DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseServiceHandle(IntPtr hSCObject);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceAccessRights dwDesiredAccess);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName,
			ServiceAccessRights dwDesiredAccess, int dwServiceType, ServiceBootFlag dwStartType, ServiceError dwErrorControl,
			string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies,
			string lpServiceStartName, string lpPassword);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteService(IntPtr hService);

		public const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;

		[Flags]
		public enum ScmAccessRights
		{
			Connect = 0x0001,
			CreateService = 0x0002,
			EnumerateService = 0x0004,
			Lock = 0x0008,
			QueryLockStatus = 0x0010,
			ModifyBootConfig = 0x0020,
			StandardRightsRequired = 0xF0000,
			AllAccess = (StandardRightsRequired | Connect | CreateService |
				EnumerateService | Lock | QueryLockStatus | ModifyBootConfig)
		}

		[Flags]
		public enum ServiceAccessRights
		{
			QueryConfig = 0x1,
			ChangeConfig = 0x2,
			QueryStatus = 0x4,
			EnumerateDependants = 0x8,
			Start = 0x10,
			Stop = 0x20,
			PauseContinue = 0x40,
			Interrogate = 0x80,
			UserDefinedControl = 0x100,
			Delete = 0x00010000,
			StandardRightsRequired = 0xF0000,
			AllAccess = (StandardRightsRequired | QueryConfig | ChangeConfig |
				QueryStatus | EnumerateDependants | Start | Stop | PauseContinue |
				Interrogate | UserDefinedControl)
		}

		public enum ServiceBootFlag
		{
			Start = 0x00000000,
			SystemStart = 0x00000001,
			AutoStart = 0x00000002,
			DemandStart = 0x00000003,
			Disabled = 0x00000004
		}

		public enum ServiceError
		{
			Ignore = 0x00000000,
			Normal = 0x00000001,
			Severe = 0x00000002,
			Critical = 0x00000003
		}
	}
}
