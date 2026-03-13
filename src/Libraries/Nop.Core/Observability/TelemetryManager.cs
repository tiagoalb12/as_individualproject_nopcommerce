using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System;

namespace Nop.Core.Telemetry
{
    public sealed class TelemetryManager : IDisposable
    {
        private static readonly Lazy<TelemetryManager> instance =
            new Lazy<TelemetryManager>(() => new TelemetryManager());

        private readonly TracerProvider tracerProvider;
        private readonly MeterProvider meterProvider;
        private bool disposed = false;

        public static TelemetryManager Instance => instance.Value;

        private TelemetryManager()
        {
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(
                    serviceName: "nopcommerce-service",
                    serviceVersion: "5.0.0",
                    serviceInstanceId: Environment.MachineName);

            tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
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
                                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = (httpContext) =>
                    {
                        return !httpContext.Request.Path.StartsWithSegments("/health");
                    };
                })
                .AddSource("Nop.Web.CatalogController")
                .AddSource("Nop.Services.Catalog.ProductService")
                .AddSource("Nop.Services.Catalog.PriceCalculation")
                .AddSource("Nop.Web.ProductModelFactory")
                .AddSource("NopCommerce.Custom")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://telemetry_service:4317");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })
                .Build();

            meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddHttpClientInstrumentation()
                .AddMeter("NopCommerce.Custom")
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
                meterProvider?.Dispose();
                disposed = true;
            }
        }
    }
}