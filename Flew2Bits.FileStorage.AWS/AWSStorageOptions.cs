using System;
using System.Collections.Generic;
using System.Text;

namespace Flew2Bits.FileStorage.AWS
{
    public abstract class AWSStorageOptions
    {
        /// <summary>
        /// The AWS endpoint URL.
        /// </summary>
        public string EndPoint { get; set; }

        /// <summary>
        /// The AWS Blob Key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The AWS Blob Secret
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        /// The AWS Blob bucket name.
        /// </summary>
        public string BucketName { get; set; }

        /// <summary>
        /// The base directory path to use inside the container for this stores contents.
        /// </summary>
        public string BasePath { get; set; }
    }
}
