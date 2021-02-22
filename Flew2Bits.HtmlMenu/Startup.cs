using Flew2Bits.HtmlMenu.Drivers;
using Flew2Bits.HtmlMenu.Models;
using Flew2Bits.HtmlMenu.Settings;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Data.Migration;
using OrchardCore.Modules;

namespace Flew2Bits.HtmlMenu
{
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDataMigration, Migrations>();

            services.AddContentPart<HtmlMenuItemPart>()
                .UseDisplayDriver<HtmlMenuItemPartDisplayDriver>();
            services.AddScoped<IContentTypePartDefinitionDisplayDriver, HtmlMenuItemPartSettingsDisplayDriver>();
        }
    }
}
