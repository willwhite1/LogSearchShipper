using System;
using System.IO;
using log4net.Layout.Pattern;
using System.Text;
using System.Reflection;

namespace LogstashForwarder.Core
{
    public class ISO8601DatePatternConverter : PatternLayoutConverter
    {
        public const string ISO8601 = "yyyy-MM-ddTHH:mm:ss.fffZ";

        protected override void Convert(TextWriter writer, log4net.Core.LoggingEvent loggingEvent)
        {
            writer.Write(loggingEvent.TimeStamp.ToUniversalTime().ToString(ISO8601));
        }
    }

    public class JSONFragmentPatternConverter : PatternLayoutConverter
    {

        protected override void Convert(TextWriter writer, log4net.Core.LoggingEvent loggingEvent)
        {
            writer.Write(loggingEvent.MessageObject.ToJSONFragment().TrimStart(new[] { ',' }));
        }
    }

    public static class MyExtensions
    {
        public static string ToJSONFragment(this object element)
        {
            var sb = new StringBuilder();
            if (element == null || element is ValueType || element is string)
            {
                sb.AppendFormat(",\"message\":{0}", FormatValue(element));
            }
            else
            {
                MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                foreach (var memberInfo in members)
                {
                    var fieldInfo = memberInfo as FieldInfo;
                    var propertyInfo = memberInfo as PropertyInfo;

                    if (fieldInfo == null && propertyInfo == null)
                        continue;

                    var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                    object value = fieldInfo != null
                                   ? fieldInfo.GetValue(element)
                                   : propertyInfo.GetValue(element, null);

                    if (type.IsValueType || type == typeof(string))
                    {
                        sb.AppendFormat(",\"{0}\":{1}", memberInfo.Name, FormatValue(value));
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //                                                var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                        //                                                Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                    }
                }
            }
            return sb.ToString();
        }

        private static string FormatValue(object o)
        {
            if (o == null)
                return ("null");

            if (o is DateTime)
                return (((DateTime)o).ToString(ISO8601DatePatternConverter.ISO8601));

            if (o is string)
                return string.Format("\"{0}\"", o);

            if (o is ValueType)
                return (o.ToString());

            //                        if (o is IEnumerable)
            //                                return ("...");

            throw new ArgumentException("Don't know how to Format object of type " + o.GetType());
        }
    }
}