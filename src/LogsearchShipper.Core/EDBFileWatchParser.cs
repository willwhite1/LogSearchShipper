using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Web;
using LogsearchShipper.Core.ConfigurationSections;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace LogsearchShipper.Core
{
    public class EDBEnvironment
    {
		public string Name { get; set; }
		public object ServerGroups { get; set; }
    }

    public class EDBEnvironmentComparer: IEqualityComparer<EDBEnvironment>
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
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(EDBFileWatchParser));

		EnvironmentWatchElement _environmentWatchElement;

		public EDBFileWatchParser (EnvironmentWatchElement environmentWatchElement)
		{
			_environmentWatchElement = environmentWatchElement;
		}

		public List<FileWatchElement> ToFileWatchCollection ()
		{
			var environmentDataXml = LoadEDBXml ();

			var servers = from server in environmentDataXml.Descendants ("Servers").Descendants ("Server")
			              where server.Element ("Name").Value.RegExContains(_environmentWatchElement.ServerNames)
			              && server.Element ("NetworkArea").Value.RegExContains(_environmentWatchElement.NetworkAreas)
			              select new {
								Name = server.Element ("Name").Value,
								NetworkArea = server.Element ("NetworkArea").Value,
								Services = 	from service in server.Descendants ("Services").Descendants ("Entity")
					                        where service.Element ("Name").Value.RegExContains(_environmentWatchElement.ServiceNames)
				                            select new {
												Name = service.Element ("Name").Value,
												LogFile = (string)service.Elements("LogPath").FirstOrDefault(),
												LogType = (string)service.Elements("LogPathType").FirstOrDefault(),
                                                LogFile1 = (string)service.Elements("LogPath1").FirstOrDefault(),
                                                LogType1 = (string)service.Elements("LogPathType1").FirstOrDefault(),
                                                LogFile2 = (string)service.Elements("LogPath2").FirstOrDefault(),
                                                LogType2 = (string)service.Elements("LogPathType2").FirstOrDefault()
											}
			};

			var watches = new List<FileWatchElement> ();
			foreach (var server in servers) {
				foreach (var service in server.Services) {
					var fields = new FieldCollection ();
                    fields.Add(new FieldElement { Key = "@shipper.host", Value = Environment.MachineName });
					fields.Add (new FieldElement{ Key = "@source.service", Value = service.Name });
                    fields.Add(new FieldElement { Key = "@source.host", Value = server.Name });
					foreach (FieldElement field in _environmentWatchElement.Fields) {
						fields.Add (field);
					}

                    AddFileWatchElementForLogFile(service.LogFile, service.LogType, watches, fields, server.NetworkArea, server.Name, service.Name); 
                    AddFileWatchElementForLogFile(service.LogFile1, service.LogType1, watches, fields, server.NetworkArea, server.Name, service.Name); 
                    AddFileWatchElementForLogFile(service.LogFile2, service.LogType2, watches, fields, server.NetworkArea, server.Name, service.Name); 
				}
			}
			return watches;
		}

	    private void AddFileWatchElementForLogFile(string logFile, string logType, 
            ICollection<FileWatchElement> watches, FieldCollection fields, 
            string serverNetworkArea, string serverName, string serviceName)
	    {
	        if (!string.IsNullOrEmpty(logType) && !string.IsNullOrWhiteSpace(logType)) //Don't ship logs without a type or with an empty type
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

	    private XDocument LoadEDBXml ()
		{
			XDocument environmentDataXml;
			//Use StreamReader to autodetect file encoding - http://stackoverflow.com/a/4569093/13238
			using (StreamReader sr = new StreamReader (_environmentWatchElement.DataFile.Replace ('\\', Path.DirectorySeparatorChar), true)) {
				environmentDataXml = XDocument.Load (sr);
			}
			return environmentDataXml;
		}

		public IEnumerable<EDBEnvironment> GenerateLogsearchEnvironmentDiagram ()
		{
            var environmentDataXml = LoadEDBXml ();

			/* NB Note how we force LINQ evaluation for each query by calling ToArray().  
			 * Without this data seems to get duplicated.
			 */

			var networkAreas = (from server in environmentDataXml.Descendants ("Servers").Descendants("Server")
			                    select new {
									Name = server.Element("NetworkArea").Value
								}
			).Distinct ().ToArray ();


			var servers = (from server in environmentDataXml.Descendants ("Servers").Descendants ("Server")
			               select new {
								Name = server.Element ("Name").Value,
								Description = (string)server.Elements("Description").FirstOrDefault(),
								Domain = server.Element ("Domain").Value,
								Environment = server.Element ("Environment").Value,
								NetworkArea = server.Element ("NetworkArea").Value,
								Services = from service in server.Descendants("Services").Descendants("Entity")   
											select service
				}).Distinct ().ToArray ();

		    var environmentHierarchy = new List<EDBEnvironment>
		    {
		        new EDBEnvironment
		        {
		            Name = environmentDataXml.Element("Environment").Element("Name").Value,
		            ServerGroups = from serverGroup in networkAreas
		                select new
		                {
		                    serverGroup.Name,
		                    Servers = from server in servers
		                        where server.NetworkArea == serverGroup.Name
		                        select server
		                }
		        }
		    };

			return environmentHierarchy.ToArray ();
		}
	}
}


