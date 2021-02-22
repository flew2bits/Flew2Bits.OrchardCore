using OrchardCore.ContentManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace Flew2Bits.HtmlMenu.Models
{
    public class HtmlMenuItemPart : ContentPart
    {
        public string Url { get; set; }
        public string Html { get; set; }
    }
}
