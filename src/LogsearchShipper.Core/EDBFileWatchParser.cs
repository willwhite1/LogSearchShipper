using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using log4net;
using LogSearchShipper.Core.ConfigurationSections;

namespace LogSearchShipper.Core
{
	public class EDBEnvironment
	{
		public string Name { get; set; }
		public List<EDBServerGroup> ServerGroups { get; set; }
	}

	public class EDBServerGroup
	{
		public string Name;
		public List<EdbServer> Servers;
	}

	public class EdbServer
	{
		public string Name;
		public string Description;
		public string Tags;
		public string Domain;
		public string Environment;
		public string NetworkArea;
		public List<XElement> Services;
	}

	public class EdbService
	{
		public EdbService(XElement source)
		{
			Name = source.Element("Name").Value;

			for (int i = 0;; i++)
			{
				var sourceName = "LogPath" + (i == 0 ? "" : i.ToString());
				var sourceElem = source.Element(sourceName);
				if (sourceElem == null)
					break;

				EventSources.Add(sourceElem.Value);
			}
		}

		public string Name;
		public List<string> EventSources = new List<string>();
	}

	public class EDBEnvironmentComparer : IEqualityComparer<EDBEnvironment>
	{
		public bool Equals(EDBEnvironment e1, EDBEnvironment e2)
		{
			return e1.Name.ToUpper() == e2.Name.ToUpper();
		}

		public int GetHashCode(EDBEnvironment obj)
		{
			return obj.Name.GetHashCode();
		}
	}

	public static class SearchExtensionMethods
	{
		public static bool RegExContains(this string str, string regEx)
		{
			return Regex.Match(str, regEx, RegexOptions.IgnoreCase).Success;
		}
	}

	public class EDBFileWatchParser
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(EDBFileWatchParser));

		private readonly EnvironmentWatchElement _environmentWatchElement;

		public EDBFileWatchParser(EnvironmentWatchElement environmentWatchElement)
		{
			_environmentWatchElement = environmentWatchElement;
		}

		public List<FileWatchElement> ToFileWatchCollection()
		{
			XDocument environmentDataXml = LoadEDBXml();

			var servers = from server in environmentDataXml.Descendants("Servers").Descendants("Server")
				where server.Element("Name").Value.RegExContains(_environmentWatchElement.ServerNames)
				      && server.Element("NetworkArea").Value.RegExContains(_environmentWatchElement.NetworkAreas)
				select new
				{
					Name = server.Element("Name").Value,
					NetworkArea = server.Element("NetworkArea").Value,
					Services = from service in server.Descendants("Services").Descendants("Entity")
						where service.Element("Name").Value.RegExContains(_environmentWatchElement.ServiceNames)
						select new
						{
							Name = service.Element("Name").Value,
							LogFile = (string) service.Elements("LogPath").FirstOrDefault(),
							LogType = (string) service.Elements("LogPathType").FirstOrDefault(),
							LogFile1 = (string) service.Elements("LogPath1").FirstOrDefault(),
							LogType1 = (string) service.Elements("LogPath1Type").FirstOrDefault(),
							LogFile2 = (string) service.Elements("LogPath2").FirstOrDefault(),
							LogType2 = (string) service.Elements("LogPath2Type").FirstOrDefault()
						}
				};

			var watches = new List<FileWatchElement>();
			foreach (var server in servers)
			{
				foreach (var service in server.Services)
				{
					var fields = new FieldCollection();
					fields.Add(new FieldElement {Key = "host", Value = server.Name});
					fields.Add(new FieldElement {Key = "service", Value = service.Name});
					foreach (FieldElement field in _environmentWatchElement.Fields)
					{
						fields.Add(field);
					}

					AddFileWatchElementForLogFile(service.LogFile, service.LogType, watches, fields, server.NetworkArea, server.Name,
						service.Name);
					AddFileWatchElementForLogFile(service.LogFile1, service.LogType1, watches, fields, server.NetworkArea, server.Name,
						service.Name);
					AddFileWatchElementForLogFile(service.LogFile2, service.LogType2, watches, fields, server.NetworkArea, server.Name,
						service.Name);
				}
			}
			return watches;
		}

		private void AddFileWatchElementForLogFile(string logFile, string logType,
			ICollection<FileWatchElement> watches, FieldCollection fields,
			string serverNetworkArea, string serverName, string serviceName)
		{
			if (!string.IsNullOrEmpty(logType) && !string.IsNullOrWhiteSpace(logType))
				//Don't ship logs without a type or with an empty type
			{
				watches.Add(new FileWatchElement
				{
					Files = logFile,
					Type = logType,
					Fields = fields
				});
				_log.DebugFormat(
					"Added file watch from EDB: {0} ({1}) => Matched NetworkArea:{2} ~= {3}, ServerName:{4} ~= {5}, ServiceName:{6} ~= {7}",
					logFile, logType,
					serverNetworkArea, _environmentWatchElement.NetworkAreas,
					serverName, _environmentWatchElement.ServerNames,
					serviceName, _environmentWatchElement.ServiceNames);
			}
			else
			{
				if (!string.IsNullOrEmpty(logFile))
				{
					_log.DebugFormat(
						"Ignored file watch from EDB because it has an empty type: {0} ({1})",
						logFile, logType);
				}
			}
		}

		private XDocument LoadEDBXml()
		{
			XDocument environmentDataXml;
			//Use StreamReader to autodetect file encoding - http://stackoverflow.com/a/4569093/13238
			using (var sr = new StreamReader(_environmentWatchElement.DataFile.Replace('\\', Path.DirectorySeparatorChar), true))
			{
				environmentDataXml = XDocument.Load(sr);
			}
			return environmentDataXml;
		}

		public IEnumerable<EDBEnvironment> GenerateLogsearchEnvironmentDiagram()
		{
			XDocument environmentDataXml = LoadEDBXml();

			/* NB Note how we force LINQ evaluation for each query by calling ToArray().  
			 * Without this data seems to get duplicated.
			 */

			var networkAreas = (from server in environmentDataXml.Descendants("Servers").Descendants("Server")
				select new
				{
					Name = server.Element("NetworkArea").Value
				}
				).Distinct().ToArray();

			var servers = (from server in environmentDataXml.Descendants("Servers").Descendants("Server")
				select new EdbServer
				{
					Name = server.Element("Name").Value,
					Description = (string) server.Elements("Description").FirstOrDefault(),
					Tags = (string) server.Elements("Tags").FirstOrDefault(),
					Domain = server.Element("Domain").Value,
					Environment = server.Element("Environment").Value,
					NetworkArea = server.Element("NetworkArea").Value,
					Services = (from service in server.Descendants("Services").Descendants("Entity")
								select service).ToList()
				}).Distinct().ToArray();

			var environmentHierarchy = new List<EDBEnvironment>
			{
				new EDBEnvironment
				{
					Name = environmentDataXml.Element("Environment").Element("Name").Value,
					ServerGroups = (from serverGroup in networkAreas
						select new EDBServerGroup
						{
							Name = serverGroup.Name,
							Servers = (from server in servers
								where server.NetworkArea == serverGroup.Name
								select server).ToList(),
						}).ToList()
				}
			};

			return environmentHierarchy.ToArray();
		}
	}
}