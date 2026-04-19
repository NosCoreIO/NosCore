//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace NosCore.Core.Observability
{
    public static class NosCoreTelemetry
    {
        public const string ActivitySourceName = "NosCore";
        public const string MeterName = "NosCore";

        public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
        public static readonly Meter Meter = new(MeterName);

        // Configured via standard OTEL env vars: OTEL_SERVICE_NAME,
        // OTEL_RESOURCE_ATTRIBUTES, OTEL_EXPORTER_OTLP_ENDPOINT,
        // OTEL_EXPORTER_OTLP_PROTOCOL.
        public static IServiceCollection AddNosCoreTelemetry(this IServiceCollection services, string defaultServiceName, bool includeAspNetCore = false)
        {
            services.AddOpenTelemetry()
                .ConfigureResource(rb => rb.AddService(defaultServiceName))
                .WithTracing(t =>
                {
                    t.AddSource(ActivitySourceName);
                    t.AddHttpClientInstrumentation();
                    t.AddEntityFrameworkCoreInstrumentation();
                    if (includeAspNetCore)
                    {
                        t.AddAspNetCoreInstrumentation();
                    }
                    t.AddOtlpExporter();
                })
                .WithMetrics(m =>
                {
                    m.AddMeter(MeterName);
                    m.AddRuntimeInstrumentation();
                    m.AddHttpClientInstrumentation();
                    if (includeAspNetCore)
                    {
                        m.AddAspNetCoreInstrumentation();
                    }
                    m.AddOtlpExporter();
                });

            return services;
        }
    }
}
