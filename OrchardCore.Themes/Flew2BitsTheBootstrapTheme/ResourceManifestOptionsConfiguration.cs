using Microsoft.Extensions.Options;
using OrchardCore.ResourceManagement;

namespace OrchardCore.Themes.Flew2BitsTheBootstrapTheme
{
    public class ResourceManifestOptionsConfiguration : IConfigureOptions<ResourceManagementOptions>
    {
        private static ResourceManifest _manifest;

        static ResourceManifestOptionsConfiguration() {
            _manifest = new ResourceManifest();


        }

        public void BuildManifests()
        {
            _manifest
                .DefineStyle("TheBootstrapTheme-bootstrap-oc")
                .SetUrl("~/Flew2BitsTheBootstrapTheme/css/bootstrap-oc.min.css", "~/Flew2BitsTheBootstrapTheme/css/bootstrap-oc.css")
                .SetVersion("1.0.0");

            _manifest
                .DefineStyle("navbar-top-fixed")
                .SetUrl("~/Flew2BitsTheBootstrapTheme/css/navbar-top-fixed.css")
                .SetVersion("1.0.0");

            _manifest
                .DefineStyle("sticky-footer")
                .SetUrl("~/Flew2BitsTheBootstrapTheme/css/sticky-footer.css")
                .SetVersion("1.0.0");
        }

        public void Configure(ResourceManagementOptions options)
        {
            options.ResourceManifests.Add(_manifest);
        }
    }
}
