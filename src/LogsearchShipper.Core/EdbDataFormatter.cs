using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LogSearchShipper.Core
{
	static class EdbDataFormatter
	{
		public static void ReportData(IEnumerable<EDBEnvironment> environments)
		{
			foreach (var environment in environments)
			{
				var envName = environment.Name;

				foreach (var serverGroup in environment.ServerGroups)
				{
					foreach (var server in serverGroup.Servers)
					{
						var cluster = server.NetworkArea;
						var host = server.Name;

						foreach (var serviceData in server.Services)
						{
							var service = new EdbService(serviceData);

							foreach (var eventSource in service.EventSources)
							{
								
							}
						}
					}
				}
			}
		}
	}
}
