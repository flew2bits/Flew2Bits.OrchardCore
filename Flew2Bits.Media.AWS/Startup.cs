using System;
using System.IO;
using Flew2Bits.Media.AWS.Controllers;
using Flew2Bits.FileStorage.AWS;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.FileStorage;
using OrchardCore.Media;
using OrchardCore.Media.Core;
using OrchardCore.Media.Core.Events;
using OrchardCore.Media.Events;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Security.Permissions;

namespace Flew2Bits.Media.AWS
{
    [Feature("Flew2Bits.Media.AWS.Storage")]
    public class Startup : OrchardCore.Modules.StartupBase
    {
        private readonly AdminOptions _adminOptions;
        private readonly ILogger _logger;
        private readonly IShellConfiguration _configuration;

        public Startup(IOptions<AdminOptions> adminOptions, ILogger<Startup> logger, IShellConfiguration configuration)
        {
            _adminOptions = adminOptions.Value;
            _logger = logger;
            _configuration = configuration;
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "AWSS3.Options",
                areaName: "Flew2Bits.Media.AWS",
                pattern: _adminOptions.AdminUrlPrefix + "/AWSS3/Options",
                defaults: new { controller = typeof(AdminController).ControllerName(), action = nameof(AdminController.Options) }
            );
        }

        public override int Order => 10;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<INavigationProvider, AdminMenu>();
            services.AddTransient<IConfigureOptions<AWSS3StorageOptions>, AWSS3StorageOptionsConfiguration>();

            // Only replace default implementation if options are valid.
            var bucketName = _configuration[$"Flew2Bits_Media_AWS:{nameof(AWSS3StorageOptions.BucketName)}"];
            var secret = _configuration[$"Flew2Bits_Media_AWS:{nameof(AWSS3StorageOptions.Secret)}"];
            var key = _configuration[$"Flew2Bits_Media_AWS:{nameof(AWSS3StorageOptions.Key)}"];
            var endPoint = _configuration[$"Flew2Bits_Media_AWS:{nameof(AWSS3StorageOptions.EndPoint)}"];

            if (CheckOptions(bucketName, key, secret, endPoint, _logger))
            {
                // Register a media cache file provider.
                services.AddSingleton<IMediaFileStoreCacheFileProvider>(serviceProvider =>
                {
                    var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

                    if (String.IsNullOrWhiteSpace(hostingEnvironment.WebRootPath))
                    {
                        throw new Exception("The wwwroot folder for serving cache media files is missing.");
                    }

                    var mediaOptions = serviceProvider.GetRequiredService<IOptions<MediaOptions>>().Value;
                    var shellOptions = serviceProvider.GetRequiredService<IOptions<ShellOptions>>();
                    var shellSettings = serviceProvider.GetRequiredService<ShellSettings>();
                    var logger = serviceProvider.GetRequiredService<ILogger<DefaultMediaFileStoreCacheFileProvider>>();

                    var mediaCachePath = GetMediaCachePath(hostingEnvironment, DefaultMediaFileStoreCacheFileProvider.AssetsCachePath, shellSettings);

                    if (!Directory.Exists(mediaCachePath))
                    {
                        Directory.CreateDirectory(mediaCachePath);
                    }

                    return new DefaultMediaFileStoreCacheFileProvider(logger, mediaOptions.AssetsRequestPath, mediaCachePath);
                });

                // Replace the default media file provider with the media cache file provider.
                services.Replace(ServiceDescriptor.Singleton<IMediaFileProvider>(serviceProvider =>
                    serviceProvider.GetRequiredService<IMediaFileStoreCacheFileProvider>()));

                // Register the media cache file provider as a file store cache provider.
                services.AddSingleton<IMediaFileStoreCache>(serviceProvider =>
                    serviceProvider.GetRequiredService<IMediaFileStoreCacheFileProvider>());

                // Replace the default media file store with a blob file store.
                services.Replace(ServiceDescriptor.Singleton<IMediaFileStore>(serviceProvider =>
                {
                    var blobStorageOptions = serviceProvider.GetRequiredService<IOptions<AWSS3StorageOptions>>().Value;
                    var shellOptions = serviceProvider.GetRequiredService<IOptions<ShellOptions>>();
                    var shellSettings = serviceProvider.GetRequiredService<ShellSettings>();
                    var mediaOptions = serviceProvider.GetRequiredService<IOptions<MediaOptions>>().Value;
                    var clock = serviceProvider.GetRequiredService<IClock>();
                    var contentTypeProvider = serviceProvider.GetRequiredService<IContentTypeProvider>();
                    var mediaEventHandlers = serviceProvider.GetServices<IMediaEventHandler>();
                    var mediaCreatingEventHandlers = serviceProvider.GetServices<IMediaCreatingEventHandler>();
                    var logger = serviceProvider.GetRequiredService<ILogger<DefaultMediaFileStore>>();

                    var fileStore = new AWSFileStore(blobStorageOptions, clock, contentTypeProvider);

                    var mediaPath = GetMediaPath(shellOptions.Value, shellSettings, mediaOptions.AssetsPath);

                    var mediaUrlBase = "/" + fileStore.Combine(shellSettings.RequestUrlPrefix, mediaOptions.AssetsRequestPath);

                    var originalPathBase = serviceProvider.GetRequiredService<IHttpContextAccessor>()
                        .HttpContext?.Features.Get<ShellContextFeature>()?.OriginalPathBase ?? null;

                    if (originalPathBase.HasValue)
                    {
                        mediaUrlBase = fileStore.Combine(originalPathBase.Value, mediaUrlBase);
                    }

                    return new DefaultMediaFileStore(fileStore, mediaUrlBase, mediaOptions.CdnBaseUrl, mediaEventHandlers, mediaCreatingEventHandlers, logger);
                }));

                services.AddSingleton<IMediaEventHandler, DefaultMediaFileStoreCacheEventHandler>();
            }
        }

        private string GetMediaPath(ShellOptions shellOptions, ShellSettings shellSettings, string assetsPath)
        {
            return PathExtensions.Combine(shellOptions.ShellsApplicationDataPath, shellOptions.ShellsContainerName, shellSettings.Name, assetsPath);
        }

        private string GetMediaCachePath(IWebHostEnvironment hostingEnvironment, string assetsPath, ShellSettings shellSettings)
        {
            return PathExtensions.Combine(hostingEnvironment.WebRootPath, assetsPath, shellSettings.Name);
        }

        private static bool CheckOptions(string bucketName, string key, string secret, string endPoint, ILogger logger)
        {
            var optionsAreValid = true;

            if (String.IsNullOrWhiteSpace(key))
            {
                logger.LogError("AWS Media Storage is enabled but not active because the 'Key' is missing or empty in application configuration.");
                optionsAreValid = false;
            }

            if (String.IsNullOrWhiteSpace(secret))
            {
                logger.LogError("AWS Media Storage is enabled but not active because the 'Secret' is missing or empty in application configuration.");
                optionsAreValid = false;
            }

            if (String.IsNullOrWhiteSpace(bucketName))
            {
                logger.LogError("AWS Media Storage is enabled but not active because the 'BucketName' is missing or empty in application configuration.");
                optionsAreValid = false;
            }

            if (String.IsNullOrWhiteSpace(endPoint))
            {
                logger.LogError("AWS Media Storage is enabled but not active because the 'EndPoint' is missing or empty in application configuration.");
                optionsAreValid = false;
            }

            return optionsAreValid;
        }
    }
}
