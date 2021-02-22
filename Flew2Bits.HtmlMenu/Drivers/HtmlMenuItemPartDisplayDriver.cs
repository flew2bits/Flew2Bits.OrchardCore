﻿using Flew2Bits.HtmlMenu.Models;
using Flew2Bits.HtmlMenu.Settings;
using Flew2Bits.HtmlMenu.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Infrastructure.Html;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Flew2Bits.HtmlMenu.Drivers
{
    public class HtmlMenuItemPartDisplayDriver : ContentPartDisplayDriver<HtmlMenuItemPart>
    {
        private readonly IHtmlSanitizerService _htmlSanitizerService;

        public HtmlMenuItemPartDisplayDriver(IHtmlSanitizerService htmlSanitizerService)
        {
            _htmlSanitizerService = htmlSanitizerService;
        }

        public override IDisplayResult Display(HtmlMenuItemPart part, BuildPartDisplayContext context)
        {
            var settings = context.TypePartDefinition.GetSettings<HtmlMenuItemPartSettings>();

            if (settings.SanitizeHtml)
            {
                part.Html = _htmlSanitizerService.Sanitize(part.Html);
            }

            return Combine(
                Dynamic("HtmlMenuItemPart_Admin", shape =>
                {
                    shape.MenuItemPart = part;
                })
                .Location("Admin", "Content:10"),
                Dynamic("HtmlMenuItemPart_Thumbnail", shape =>
                {
                    shape.MenuItemPart = part;
                })
                .Location("Thumbnail", "Content:10")
            );
        }

        public override IDisplayResult Edit(HtmlMenuItemPart part)
        {
            return Initialize<HtmlMenuItemPartEditViewModel>("HtmlMenuItemPart_Edit", model =>
            {
                model.Name = part.ContentItem.DisplayText;
                model.Url = part.Url;
                model.Html = part.Html;
                model.MenuItemPart = part;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(HtmlMenuItemPart part, IUpdateModel updater, UpdatePartEditorContext context)
        {
            var settings = context.TypePartDefinition.GetSettings<HtmlMenuItemPartSettings>();
            var model = new HtmlMenuItemPartEditViewModel();
            if (await updater.TryUpdateModelAsync(model, Prefix))
            {
                part.ContentItem.DisplayText = model.Name;
                part.Html = settings.SanitizeHtml ? _htmlSanitizerService.Sanitize(model.Html) : model.Html;
                part.Url = model.Url;
            }

            return Edit(part, context);
        }
    }
}
