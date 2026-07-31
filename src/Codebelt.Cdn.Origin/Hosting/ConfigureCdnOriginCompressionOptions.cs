using System.IO.Compression;
using Codebelt.Cdn.Origin.Configuration;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace Codebelt.Cdn.Origin.Hosting;

/// <summary>
/// Configures response compression for the Static Content Provider from the bound <see cref="CdnOriginOptions"/>,
/// enabling Brotli and Gzip for compressible content types only.
/// </summary>
/// <seealso cref="IConfigureOptions{TOptions}" />
internal sealed class ConfigureCdnOriginCompressionOptions(IOptions<CdnOriginOptions> options) :
    IConfigureOptions<ResponseCompressionOptions>,
    IConfigureOptions<BrotliCompressionProviderOptions>,
    IConfigureOptions<GzipCompressionProviderOptions>
{
    public void Configure(ResponseCompressionOptions compressionOptions)
    {
        CompressionOptions compression = options.Value.Compression;

        compressionOptions.EnableForHttps = compression.EnableForHttps;
        compressionOptions.Providers.Add<BrotliCompressionProvider>();
        compressionOptions.Providers.Add<GzipCompressionProvider>();
        compressionOptions.MimeTypes =
        [
            .. CdnOriginServiceCollectionExtensions.DefaultCompressibleMimeTypes
                .Concat(compression.AdditionalMimeTypes)
                .Distinct(StringComparer.OrdinalIgnoreCase)
        ];
    }

    public void Configure(BrotliCompressionProviderOptions brotliOptions) => brotliOptions.Level = CompressionLevel.Fastest;

    public void Configure(GzipCompressionProviderOptions gzipOptions) => gzipOptions.Level = CompressionLevel.Fastest;
}
