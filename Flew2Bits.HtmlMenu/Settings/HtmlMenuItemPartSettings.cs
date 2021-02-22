using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Flew2Bits.HtmlMenu.Settings
{
    public class HtmlMenuItemPartSettings
    {
        [DefaultValue(true)]
        public bool SanitizeHtml { get; set; } = true;
    }
}
