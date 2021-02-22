using Fluid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Configuration;
using System;

namespace Flew2Bits.Media.AWS
{
    public class AWSS3StorageOptionsConfiguration: IConfigureOptions<AWSS3StorageOptions>
    {
        private readonly IShellConfiguration _shellConfiguration;
        private readonly ShellSettings _shellSettings;
        private readonly ILogger _logger;

        public AWSS3StorageOptionsConfiguration(
            IShellConfiguration shellConfiguration,
            ShellSettings shellSettings,
            ILogger<AWSS3StorageOptionsConfiguration> logger
            )
        {
            _shellConfiguration = shellConfiguration;
            _shellSettings = shellSettings;
            _logger = logger;
        }
        public void Configure(AWSS3StorageOptions options)
        {
            var section = _shellConfiguration.GetSection("Flew2Bits_Media_AWS");

            options.BucketName = section.GetValue(nameof(options.BucketName), String.Empty);
            options.BasePath = section.GetValue(nameof(options.BasePath), String.Empty);
            options.Secret = section.GetValue(nameof(options.Secret), String.Empty);
            options.Key = section.GetValue(nameof(options.Key), String.Empty);
            options.EndPoint = section.GetValue(nameof(options.EndPoint), String.Empty);

            var templateContext = new TemplateContext();
            templateContext.MemberAccessStrategy.Register<ShellSettings>();
            templateContext.MemberAccessStrategy.Register<AWSS3StorageOptions>();
            templateContext.SetValue("ShellSettings", _shellSettings);

            ParseContainerName(options, templateContext);
            ParseBasePath(options, templateContext);
        }

        private void ParseContainerName(AWSS3StorageOptions options, TemplateContext templateContext)
        {
            // Use Fluid directly as this is transient and cannot invoke _liquidTemplateManager.
            try
            {
                var template = FluidTemplate.Parse(options.BucketName);

                // container name must be lowercase
                options.BucketName = template.Render(templateContext, NullEncoder.Default).ToLower();
                options.BucketName = options.BucketName.Replace("\r", String.Empty).Replace("\n", String.Empty);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to parse AWS Media Storage bucket name.");
                throw;
            }
        }

        private void ParseBasePath(AWSS3StorageOptions options, TemplateContext templateContext)
        {
            try
            {
                var template = FluidTemplate.Parse(options.BasePath);

                options.BasePath = template.Render(templateContext, NullEncoder.Default);
                options.BasePath = options.BasePath.Replace("\r", String.Empty).Replace("\n", String.Empty);
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unable to parse AWS Media Storage base path.");
                throw;
            }
        }
    }
}
