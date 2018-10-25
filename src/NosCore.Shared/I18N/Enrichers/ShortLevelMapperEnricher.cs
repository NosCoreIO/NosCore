using Serilog.Core;
using Serilog.Events;

namespace NosCore.Shared.I18N.Enrichers
{
    public class ShortLevelMapperEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var logLevel = string.Empty;

            switch (logEvent.Level)
            {
                case LogEventLevel.Debug:
                    logLevel = "[DEBUG]";
                    break;

                case LogEventLevel.Error:
                    logLevel = "[ERROR]";
                    break;

                case LogEventLevel.Fatal:
                    logLevel = "[FATAL]";
                    break;

                case LogEventLevel.Information:
                    logLevel = "[INFO] ";
                    break;

                case LogEventLevel.Verbose:
                    logLevel = "[ALL]  ";
                    break;

                case LogEventLevel.Warning:
                    logLevel = "[WARN] ";
                    break;
            }

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ShortLevel", logLevel));
        }
    }
}