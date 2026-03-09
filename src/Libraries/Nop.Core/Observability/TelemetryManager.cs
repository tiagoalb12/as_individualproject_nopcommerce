using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

namespace Nop.Core.Telemetry
{
    public sealed class TelemetryManager : IDisposable
    {
        private static readonly Lazy<TelemetryManager> instance =
            new Lazy<TelemetryManager>(() => new TelemetryManager());

        private readonly TracerProvider tracerProvider;
        private bool disposed = false;

        public static TelemetryManager Instance => instance.Value;

        private TelemetryManager()
        {
            tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(
                        serviceName: "nopcommerce-service",
                        serviceVersion: "5.0.0",
                        serviceInstanceId: Environment.MachineName))
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddSqlClientInstrumentation(options =>
                {
                    options.SetDbStatementForText = false;
                    options.RecordException = true;
                    options.EnableConnectionLevelAttributes = true;
                })
                .AddSource("NopCommerce.Custom")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://telemetry_service:4317");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })
                .Build();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                tracerProvider?.Dispose();
                disposed = true;
            }
        }
    }
}