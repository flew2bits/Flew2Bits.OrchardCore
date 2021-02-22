using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "Amazon Web Services S3 Media",
    Author = "Titus Anderson",
    Version = "0.0.1"
)]

[assembly: Feature(
    Id = "Flew2Bits.Media.AWS.Storage",
    Name = "AWS Media Storage",
    Description = "Enables support for storing media files in AWS S3 Storage.",
    Dependencies = new[]
    {
        "OrchardCore.Media.Cache"
    },
    Category = "Hosting"
)]
