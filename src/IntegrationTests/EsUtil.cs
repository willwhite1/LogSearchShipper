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

		public static List<Record> GetRecords(string sourceName, string groupId, string fieldName)
		{
			var settings = new ConnectionSettings(new Uri(AppSettings.EsServerUrl));
			var client = new ElasticClient(settings);

			var indexNames = new List<string>();

			var curDay = DateTime.UtcNow.Date - TimeSpan.FromDays(3);
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
							.Fields("@timestamp", fieldName)
							.Size(SegmentSize)
							.From(SegmentSize * segment).
							Query(q =>
							{
								BaseQuery query = null;
								query &= q.Filtered(s => s.Filter(fs => fs.Exists(fieldName)));
								query &= q.Term("source", sourceName);
								query &= q.Term("group_id", groupId);
								return query;
							}
						)
					);

				if (res.ConnectionStatus.Error != null)
					throw new ApplicationException(res.ConnectionStatus.Error.ExceptionMessage);

				foreach (var hit in res.Hits.Hits)
				{
					var fields = ((IEnumerable<KeyValuePair<string, JToken>>)hit.Fields).ToArray();

					var timeArray = (JArray)fields.First(field => field.Key == "@timestamp").Value;
					if (timeArray.Count != 1)
						throw new ApplicationException();
					var timeToken = (JValue)timeArray[0];

					var valueArray = fields.First(field => field.Key == fieldName).Value;
					if (valueArray.Count() != 1)
						throw new ApplicationException();
					var valueToken = (JValue)valueArray[0];

					var record = new Record
					{
						Time = (DateTime)(timeToken.Value),
						Name = fieldName,
						Value = valueToken.Value,
					};
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
