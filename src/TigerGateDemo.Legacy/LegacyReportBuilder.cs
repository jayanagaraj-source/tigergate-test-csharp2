using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using log4net;
using Newtonsoft.Json;

namespace TigerGateDemo.Legacy
{
    /// <summary>
    /// Kept alive for the on-premise reporting host, which is still on .NET Framework 4.7.2.
    /// </summary>
    public class LegacyReportBuilder
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LegacyReportBuilder));

        private readonly string _title;
        private readonly List<KeyValuePair<string, decimal>> _rows = new List<KeyValuePair<string, decimal>>();

        public LegacyReportBuilder(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException("title");
            }

            _title = title;
        }

        public LegacyReportBuilder AddRow(string label, decimal amount)
        {
            _rows.Add(new KeyValuePair<string, decimal>(label, amount));
            return this;
        }

        public decimal Total
        {
            get
            {
                decimal total = 0m;
                foreach (var row in _rows)
                {
                    total += row.Value;
                }

                return total;
            }
        }

        public string ToCsv()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Label,Amount");

            foreach (var row in _rows)
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}\",{1}",
                    row.Key.Replace("\"", "\"\""),
                    row.Value));
            }

            builder.AppendLine(string.Format(CultureInfo.InvariantCulture, "\"Total\",{0}", Total));
            return builder.ToString();
        }

        public string ToJson()
        {
            Log.InfoFormat("Serialising legacy report '{0}' with {1} rows", _title, _rows.Count);

            return JsonConvert.SerializeObject(new
            {
                title = _title,
                generatedAtUtc = DateTime.UtcNow,
                rows = _rows,
                total = Total
            }, Formatting.Indented);
        }
    }
}
