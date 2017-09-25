﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System;
using System.IO;

namespace Webpack.AspNetCore.Internal
{
    internal class WebpackContext
    {
        public WebpackOptions Options { get; set; }
        public IFileProvider AssetFileProvider { get; private set; }
        public PathString ManifestPathBase { get; private set; }
        public string ManifestFileName { get; private set; }
        public string DevServerHost { get; private set; }
        public Uri DevServerManifestUri { get; private set; }

        public IFileInfo GetManifestFileInfo()
        {
            return AssetFileProvider.GetFileInfo(ManifestFileName);
        }

        public WebpackContext(IOptions<WebpackOptions> options, IHostingEnvironment env)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (env == null)
            {
                throw new ArgumentNullException(nameof(env));
            }

            Options = options.Value;

            var manifestPath = Options.ManifestPath.Value;
            var manifestFileName = manifestPath.Substring(manifestPath.LastIndexOf('/') + 1);
            var manifestRootRelativePath = manifestPath
                .Substring(0, manifestPath.LastIndexOf('/'))
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);
            var manifestRootPath = Path.Combine(env.WebRootPath, manifestRootRelativePath);

            AssetFileProvider = new PhysicalFileProvider(manifestRootPath);
            ManifestFileName = manifestFileName;
            ManifestPathBase = getManifestPathBase();

            if (Options.UseDevServer)
            {
                DevServerHost = getDevServerHost();
                DevServerManifestUri = getDevServerManifestUri();
            }

            string getDevServerHost() => $"{Options.DevServerHost}:{Options.DevServerPort}";

            Uri getDevServerManifestUri()
            {
                var uriBuilder = new UriBuilder
                {
                    Scheme = Options.DevServerScheme,
                    Host = Options.DevServerHost,
                    Port = Options.DevServerPort,
                    Path = Options.DevServerPublicPath.Add(Options.ManifestPath)
                };

                return uriBuilder.Uri;
            }

            PathString getManifestPathBase()
            {
                var contentRootDirUrl = new Uri(env.ContentRootPath + Path.DirectorySeparatorChar);
                var manifestRootPathUrl = new Uri(manifestRootPath);
                var manifestPathBaseUrl = contentRootDirUrl.MakeRelativeUri(manifestRootPathUrl).ToString();

                return new PathString('/' + manifestPathBaseUrl);
            }
        }
    }
}