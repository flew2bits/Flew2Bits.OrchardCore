using System;
using System.Collections.Generic;
using System.Text;

namespace Flew2Bits.Media.AWS.ViewModels
{
    public class OptionsViewModel
    {
        public string Key { get; set; }
        public string Secret { get; set; }
        public string BucketName { get; set; }
        public string EndPoint { get; set; }
        public string BasePath { get; set; }
    }
}
