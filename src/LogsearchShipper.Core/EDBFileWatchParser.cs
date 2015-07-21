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
			ServiceName = source.Element("ServiceName").Value;
			State = source.Element("State").Value;

			for (int i = 0; ; i++)
			{
				var sourceName = "LogPath" + (i == 0 ? "" : i.ToString());
				var sourceElem = source.Element(sourceName);
				if (sourceElem == null)
					break;

				EventSources.Add(sourceElem.Value);
			}

			var xsiNamespace = source.Document.Root.GetNamespaceOfPrefix("xsi");
			ServiceType = source.Attributes(xsiNamespace + "type").First().Value;

			BinaryPath = source.Element("BinaryPath").Value;
			SystemArea = source.Element("SystemArea").Value;

			Tags = TryGetField(source, "Tags");
			BundlePath = TryGetField(source, "BundlePath");
			Website = TryGetField(source, "Website");
			ApplicationUri = TryGetField(source, "ApplicationUri");

			LogPath = source.Element("LogPath").Value;
			LogPathType = source.Element("LogPathType").Value;

			LogPath1 = source.Element("LogPath1").Value;
			LogPath1Type = source.Element("LogPath1Type").Value;

			LogPath2 = source.Element("LogPath2").Value;
			LogPath2Type = source.Element("LogPath2Type").Value;
		}

		private string TryGetField(XElement serviceNode, string elementName)
		{
			var element = serviceNode.Element(elementName);
			return element == null ? null : element.Value;
		}

		public string Name;
		public string ServiceName;
		public string State;
		public List<string> EventSources = new List<string>();

		public string ServiceType;
		public string BinaryPath;
		public string SystemArea;

		public string Tags;
		public string BundlePath;
		public string Website;
		public string ApplicationUri;

		public string LogPath;
		public string LogPathType;

		public string LogPath1;
		public string LogPath1Type;

		public string LogPath2;
		public string LogPath2Type;
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
		public static bool RegExMatches(this string str, string regEx)
		{
			if (string.IsNullOrEmpty(regEx))
				return false;
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
			var environmentDataXml = LoadEDBXml();

			var serversFiltered = environmentDataXml.Descendants("Servers").Descendants("Server").Where(
				server =>
				{
					var serverName = server.Element("Name").Value;
					var networkArea = server.Element("NetworkArea").Value;

					return serverName.RegExMatches(_environmentWatchElement.ServerNames) &&
						!serverName.RegExMatches(_environmentWatchElement.ServerNamesNotMatch) &&
						networkArea.RegExMatches(_environmentWatchElement.NetworkAreas) &&
						!networkArea.RegExMatches(_environmentWatchElement.NetworkAreasNotMatch);
				}).ToArray();

			var watches = new List<FileWatchElement>();
			foreach (var serverNode in serversFiltered)
			{
				var serverName = serverNode.Element("Name").Value;
				var serverNetworkArea = serverNode.Element("NetworkArea").Value;

				foreach (var serviceNode in serverNode.Descendants("Services").Descendants("Entity"))
				{
					var serviceName = serviceNode.Element("Name").Value;

					if (!serviceName.RegExMatches(_environmentWatchElement.ServiceNames) ||
							serviceName.RegExMatches(_environmentWatchElement.ServiceNamesNotMatch))
						continue;

					var fields = new FieldCollection
					{
						new FieldElement { Key = "host", Value = serverName },
						new FieldElement { Key = "service", Value = serviceName },
					};

					foreach (FieldElement field in _environmentWatchElement.Fields)
					{
						fields.Add(field);
					}

					var serviceLogFile = (string)serviceNode.Elements("LogPath").FirstOrDefault();
					var serviceLogType = (string)serviceNode.Elements("LogPathType").FirstOrDefault();
					var serviceLogFile1 = (string)serviceNode.Elements("LogPath1").FirstOrDefault();
					var serviceLogType1 = (string)serviceNode.Elements("LogPath1Type").FirstOrDefault();
					var serviceLogFile2 = (string)serviceNode.Elements("LogPath2").FirstOrDefault();
					var serviceLogType2 = (string)serviceNode.Elements("LogPath2Type").FirstOrDefault();

					AddFileWatchElementForLogFile(serviceLogFile, serviceLogType, watches, fields, serverNetworkArea, serverName,
						serviceName);
					AddFileWatchElementForLogFile(serviceLogFile1, serviceLogType1, watches, fields, serverNetworkArea, serverName,
						serviceName);
					AddFileWatchElementForLogFile(serviceLogFile2, serviceLogType2, watches, fields, serverNetworkArea, serverName,
						serviceName);
				}
			}

			return watches;
		}

		private void AddFileWatchElementForLogFile(string logFile, string logType, ICollection<FileWatchElement> watches,
			FieldCollection fields, string serverNetworkArea, string serverName, string serviceName)
		{
			//Don't ship logs without a type or with an empty type
			if (!string.IsNullOrEmpty(logType) && !string.IsNullOrWhiteSpace(logType))
			{
				var newWatch = new FileWatchElement
				{
					Files = logFile,
					Type = logType,
					Fields = fields
				};

				var overrideConfig = FindOverride(serviceName);
				if (overrideConfig != null)
				{
					newWatch.CloseWhenIdle = overrideConfig.CloseWhenIdle;
					if (overrideConfig.CustomNxlogConfig != null)
					{
						newWatch.CustomNxlogConfig = new CustomNxlogConfig
							{
								Value = overrideConfig.CustomNxlogConfig.Value
							};
					}
					newWatch.SourceTailer = overrideConfig.SourceTailer;
					newWatch.MultilineRule = overrideConfig.MultilineRule;

					foreach (FieldElement overrideField in overrideConfig.Fields)
					{
						newWatch.Fields.Remove(overrideField.Key);
						newWatch.Fields.Add(new FieldElement { Key = overrideField.Key, Value = overrideField.Value });
					}
				}

				watches.Add(newWatch);

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
			var path = _environmentWatchElement.DataFile.Replace('\\', Path.DirectorySeparatorChar);
			//Use StreamReader to autodetect file encoding - http://stackoverflow.com/a/4569093/13238
			using (var sr = new StreamReader(path, true))
			{
				environmentDataXml = XDocument.Load(sr);
			}
			return environmentDataXml;
		}

		public IEnumerable<EDBEnvironment> GenerateLogsearchEnvironmentDiagram()
		{
			var environmentDataXml = LoadEDBXml();

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
								Description = (string)server.Elements("Description").FirstOrDefault(),
								Tags = (string)server.Elements("Tags").FirstOrDefault(),
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

		OverrideConfig FindOverride(string serviceName)
		{
			var overrides = _environmentWatchElement.OverrideConfigs;

			foreach (var overrideConfig in overrides)
			{
				var regex = new Regex(overrideConfig.ForServiceNames);
				if (regex.Match(serviceName).Success)
					return overrideConfig;
			}

			return null;
		}
	}
}