using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Nest;
using Newtonsoft.Json.Linq;

namespace IntegrationTests
{
	static class EsUtil
	{
		private const int SegmentSize = 1024;

		public static List<Record> GetRecords(Dictionary<string, string> args)
		{
			var settings = new ConnectionSettings(new Uri(AppSettings.EsServerUrl));
			var client = new ElasticClient(settings);

			var indexNames = new List<string>();

			var curDay = DateTime.UtcNow.Date - TimeSpan.FromDays(1); // date can change since this test has started
			while (curDay <= DateTime.UtcNow.Date)
			{
				var indexName = "logstash-" + curDay.ToString("yyyy.MM.dd");
				indexNames.Add(indexName);
				curDay = curDay.AddDays(1);
			}

			var records = new List<Record>();

			var curSegment = 0;
			while (true)
			{
				var segment = curSegment;

				var res = client.Search(
					search =>
						search.Indices(indexNames)
							.Size(SegmentSize)
							.From(SegmentSize * segment).
							Query(q =>
							{
								BaseQuery query = null;
								foreach (var arg in args)
								{
									query &= q.Term(arg.Key, arg.Value);
								}
								return query;
							}
						)
					);

				if (res.ConnectionStatus.Error != null)
					throw new ApplicationException(res.ConnectionStatus.Error.ExceptionMessage);

				foreach (var hit in res.Hits.Hits)
				{
					var fields = hit.Source;
					var timeToken = (JValue)fields["@timestamp"];

					var record = new Record
					{
						Time = (DateTime)(timeToken.Value),
					};
					foreach (JProperty pair in fields)
					{
						record.Fields.Add(pair.Name, pair.Value.ToString());
					}

					records.Add(record);
				}

				if (res.Hits.Hits.Count < SegmentSize)
					break;

				curSegment++;
			}

			records.Sort((x, y) => x.Time.CompareTo(y.Time));

			return records;
		}
	}
}
