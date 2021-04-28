using System;
using Cuemon;
using Cuemon.Data.Integrity;
using Cuemon.Extensions;
using Cuemon.Extensions.AspNetCore.Http;
using Cuemon.Extensions.IO;
using Cuemon.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Codebelt.Cdn.Origin
{
    public class Startup
    {
        private readonly TimeSpan _maxAge;
        private readonly TimeSpan _sharedMaxAge;
        private readonly string _contentPath;

        public Startup(IConfiguration configuration)
        {
            var maxAge = Convert.ToDouble(configuration["CacheControl:MaxAge:Double"] ?? "12");
            var maxAgeUnit = (configuration["CacheControl:MaxAge:Unit"] ?? "Hours").ToEnum<TimeUnit>();
            var sharedMaxAge = Convert.ToDouble(configuration["CacheControl:SharedMaxAge:Double"] ?? "168");
            var sharedMaxAgeUnit = (configuration["CacheControl:SharedMaxAge:Unit"] ?? "Hours").ToEnum<TimeUnit>();

            _contentPath = configuration["ContentPath"] ?? "/cdnroot";
            _maxAge = maxAge.ToTimeSpan(maxAgeUnit);
            _sharedMaxAge = sharedMaxAge.ToTimeSpan(sharedMaxAgeUnit);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddResponseCaching();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(pb =>
            {
                pb.AllowAnyHeader();
                pb.AllowAnyMethod();
                pb.AllowAnyOrigin();
            });

            app.UseStaticFiles(Patterns.Configure<StaticFileOptions>(o =>
            {
                o.ServeUnknownFileTypes = true;
                o.FileProvider = new PhysicalFileProvider(_contentPath);
                o.OnPrepareResponse = fc =>
                {
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
                        var builder = new ChecksumBuilder(HashFactory.CreateCrc32).CombineWith(fc.File.CreateReadStream().ToByteArray());
                        fc.Context.Response.AddOrUpdateLastModifiedHeader(fc.Context.Request, fc.File.LastModified.UtcDateTime);
                        fc.Context.Response.AddOrUpdateEntityTagHeader(fc.Context.Request, builder);
                    }
                };
            }));
        }
    }
}