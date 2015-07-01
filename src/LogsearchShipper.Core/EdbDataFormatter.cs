using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net;

namespace LogSearchShipper.Core
{
	static class EdbDataFormatter
	{
		public static void ReportData(IEnumerable<EDBEnvironment> environments)
		{
			var log = LogManager.GetLogger("EdbExpectedEventSourcesLogger");

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
								log.Info(
									new
									{
										environment = envName,
										cluster = cluster,
										host = host,
										service = service.Name,
										event_source = eventSource,
										expected_state = service.State,
										serviceType = service.ServiceType,
										binaryPath = service.BinaryPath,
										systemArea = service.SystemArea,
										tags = service.Tags,
										bundlePath = service.BundlePath,
										website = service.Website,
										applicationUri = service.ApplicationUri,
									});
							}
						}
					}
				}
			}
		}
	}
}
