using Flew2Bits.HtmlMenu.Models;
using Flew2Bits.HtmlMenu.ViewModels;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using System;
using System.Threading.Tasks;

namespace Flew2Bits.HtmlMenu.Settings
{
    public class HtmlMenuItemPartSettingsDisplayDriver: ContentTypePartDefinitionDisplayDriver
    {
        public override IDisplayResult Edit(ContentTypePartDefinition contentTypePartDefinition, IUpdateModel updater)
        {
            if (!String.Equals(nameof(HtmlMenuItemPart), contentTypePartDefinition.PartDefinition.Name))
            {
                return null;
            }

            return Initialize<HtmlMenuItemPartSettingsViewModel>("HtmlMenuItemPartSettings_Edit", model =>
            {
                var settings = contentTypePartDefinition.GetSettings<HtmlMenuItemPartSettings>();

                model.SanitizeHtml = settings.SanitizeHtml;
            })
            .Location("Content:20");
        }

        private object Initialize<T>(string v, Action<object> p)
        {
            throw new NotImplementedException();
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentTypePartDefinition contentTypePartDefinition, UpdateTypePartEditorContext context)
        {
            if (!String.Equals(nameof(HtmlMenuItemPart), contentTypePartDefinition.PartDefinition.Name))
            {
                return null;
            }

            var model = new HtmlMenuItemPartSettingsViewModel();
            var settings = new HtmlMenuItemPartSettings();

            if (await context.Updater.TryUpdateModelAsync(model, Prefix))
            {
                settings.SanitizeHtml = model.SanitizeHtml;

                context.Builder.WithSettings(settings);
            }

            return Edit(contentTypePartDefinition, context.Updater);
        }
    }
}
