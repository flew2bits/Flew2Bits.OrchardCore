This is an OrchardCore CMS module that provides a Media File store through AWS S3 (or compatible) storage. 

In your appsettings.json file, include the following configuration:

```
{
  "OrchardCore": {
    "Flew2Bits_Media_AWS": {
      // Set to the AWS EndPoint/Server URL (including protocol)
      "EndPoint": "", 
      // Set to the AWS Bucket Name
      "BucketName": "someBucketName",
      // Optionally, set to a path to store media in a subdirectory inside your container.
      "BasePath": "some/base/path",
      // Set the AWS S3 Key
      "Key": "myKey",
      // Set the AWS S3 Secret
      "Secret": "mySecret"
    }
  }
}
```

Both the `BucketName` and `BasePath` are liquid enabled to allow for setting up content based on ShellSettings. See the [documentation](https://docs.orchardcore.net/en/dev/docs/reference/modules/Media.Azure/#templating-configuration) for Media.Azure module for details.
