using OrchardCore.ResourceManagement;

namespace OrchardCore.Themes.Flew2BitsTheBootstrapTheme
{
    public class ResourceManifest : IResourceManifestProvider
    {
        public void BuildManifests(IResourceManifestBuilder builder)
        {
            var manifest = builder.Add();

            manifest
                .DefineStyle("TheBootstrapTheme-bootstrap-oc")
                .SetUrl("~/Flew2BitsTheBootstrapTheme/css/bootstrap-oc.min.css", "~/Flew2BitsTheBootstrapTheme/css/bootstrap-oc.css")
                .SetVersion("1.0.0");

            manifest
                .DefineStyle("navbar-top-fixed")
                .SetUrl("~/Flew2BitsTheBootstrapTheme/css/navbar-top-fixed.css")
                .SetVersion("1.0.0");

            manifest
                .DefineStyle("sticky-footer")
                .SetUrl("~/Flew2BitsTheBootstrapTheme/css/sticky-footer.css")
                .SetVersion("1.0.0");
        }
    }
}
