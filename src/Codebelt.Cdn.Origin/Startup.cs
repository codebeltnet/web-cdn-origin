using System;
using System.IO;
using Cuemon;
using Cuemon.Data.Integrity;
using Cuemon.Extensions;
using Cuemon.Extensions.AspNetCore.Http;
using Cuemon.Extensions.Collections.Generic;
using Cuemon.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Codebelt.Cdn.Origin
{
    public class Startup
    {
        private readonly TimeSpan _maxAge;
        private readonly TimeSpan _sharedMaxAge;
        private readonly string _contentPath;
        private readonly string[] _defaultFiles;
        private readonly int _bytesToReadForEntityTagHeader;

        public Startup(IConfiguration configuration)
        {
            var maxAge = Convert.ToDouble(configuration["CACHECONTROL_MAXAGE"] ?? "12");
            var maxAgeTimeUnit = (configuration["CACHECONTROL_MAXAGE_TIMEUNIT"] ?? "Hours").ToEnum<TimeUnit>();
            var sharedMaxAge = Convert.ToDouble(configuration["CACHECONTROL_SHAREDMAXAGE"] ?? "168");
            var sharedMaxAgeTimeUnit = (configuration["CACHECONTROL_SHAREDMAXAGE_TIMEUNIT"] ?? "Hours").ToEnum<TimeUnit>();

            _bytesToReadForEntityTagHeader = Convert.ToInt32(configuration["ETAG_BYTESTOREAD"] ?? $"{int.MaxValue}");
            _contentPath = configuration["CDNROOT"] ?? "/cdnroot";
            _defaultFiles = (configuration["CDNROOT_DEFAULTFILES"] ?? "default.htm;default.html;index.htm;index.html").Split(';');
            _maxAge = maxAge.ToTimeSpan(maxAgeTimeUnit);
            _sharedMaxAge = sharedMaxAge.ToTimeSpan(sharedMaxAgeTimeUnit);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCaching();
            services.AddResponseCompression();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseDefaultFiles(Patterns.Configure<DefaultFilesOptions>(o =>
            {
                o.DefaultFileNames.Clear();
                o.DefaultFileNames.AddRange(_defaultFiles);
                o.FileProvider = new CaseInsensitivePhysicalFileProvider(_contentPath);
            }));

            app.UseResponseCompression();

            app.UseStaticFiles(Patterns.Configure<StaticFileOptions>(o =>
            {
                o.ServeUnknownFileTypes = true;
                o.FileProvider = new CaseInsensitivePhysicalFileProvider(_contentPath);
                o.OnPrepareResponse = fc =>
                {
                    fc.Context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                    fc.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MustRevalidate = true,
                        NoTransform = true,
                        MaxAge = _maxAge,
                        SharedMaxAge = _sharedMaxAge
                    };
                    fc.Context.Response.Headers[HeaderNames.Expires] = DateTime.UtcNow.Add(_maxAge).ToString("R");
                    if (!fc.File.IsDirectory && fc.File.Exists)
                    {
                        var builder = new ChecksumBuilder(() => UnkeyedHashFactory.CreateCryptoMd5()).CombineWith(fc.File.CreateReadStream().ToByteArray(_bytesToReadForEntityTagHeader));
                        fc.Context.Response.AddOrUpdateLastModifiedHeader(fc.Context.Request, fc.File.LastModified.UtcDateTime);
                        fc.Context.Response.AddOrUpdateEntityTagHeader(fc.Context.Request, builder, fc.File.Length > _bytesToReadForEntityTagHeader);
                        if (fc.Context.Response.StatusCode == StatusCodes.Status304NotModified)
                        {
                            fc.Context.Response.Body = new MemoryStream();
                            fc.Context.Response.ContentLength = 0;
                        }
                    }
                };
            }));
        }
    }
}