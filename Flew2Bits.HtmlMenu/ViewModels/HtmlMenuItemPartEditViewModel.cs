using Flew2Bits.HtmlMenu.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Flew2Bits.HtmlMenu.ViewModels
{
    public class HtmlMenuItemPartEditViewModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Html { get; set; }

        [BindNever]
        public HtmlMenuItemPart MenuItemPart { get; set; }
    }
}
