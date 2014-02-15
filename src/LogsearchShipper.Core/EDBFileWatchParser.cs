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

	public class EDBFileWatchParser
	{
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(EDBFileWatchParser));

		EnvironmentWatchElement _environmentWatchElement;

		public EDBFileWatchParser (EnvironmentWatchElement environmentWatchElement)
		{
			_environmentWatchElement = environmentWatchElement;
		}

		public IEnumerable<FileWatchElement> ToFileWatchCollection ()
		{
			var watches = new List<FileWatchElement> ();
			//TODO
//			watches.Add (new FileWatchElement { Files = @"\\PKH-PPE-APP10\logs\Apps\PriceHistoryService\log.log", Type = "log4net", Fields = _environmentWatchElement.Fields });
//			watches.Add (new FileWatchElement { Files = @"c:\foo\bar2.log", Type = "log4net", Fields = _environmentWatchElement.Fields });
			return watches;
		}

		public IEnumerable<EDBEnvironment> GenerateLogsearchEnvironmentDiagram ()
		{
            XDocument environmentDataXml;
            //Use StreamReader to autodetect file encoding - http://stackoverflow.com/a/4569093/13238
            using (StreamReader sr = new StreamReader(_environmentWatchElement.DataFile.Replace('\\', Path.DirectorySeparatorChar), true))
            {
                environmentDataXml = XDocument.Load(sr);
            }

			/* NB Note how we force LINQ evaluation for each query by calling ToArray().  
			 * Without this data seems to get duplicated.
			 */

			var environments = (from server in environmentDataXml.Descendants ("Servers").Descendants("Server")
								select new EDBEnvironment {
									Name = server.Element("Environment").Value
								}
			).Distinct (new EDBEnvironmentComparer()).ToArray ();

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

			var environmentHierarchy = from environment in environments
			    select new EDBEnvironment
				{
					Name = environment.Name,
					ServerGroups = from serverGroup in networkAreas
						select new
						{
							serverGroup.Name,
							Servers = from server in servers
						              where server.Environment == environment.Name
					                  && server.NetworkArea == serverGroup.Name
					                  select server
						}
				};

			return environmentHierarchy.ToArray ();
		}
	}
}


