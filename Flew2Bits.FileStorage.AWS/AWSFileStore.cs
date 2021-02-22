using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.StaticFiles;
using OrchardCore.FileStorage;
using OrchardCore.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Flew2Bits.FileStorage.AWS
{
    public class AWSFileStore: IFileStore
    {
        private readonly AWSStorageOptions _options;
        private readonly IClock _clock;
        private readonly AmazonS3Client _s3Client;
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly string _basePrefix = null;

        public AWSFileStore(AWSStorageOptions options, IClock clock, IContentTypeProvider contentTypeProvider)
        {
            _options = options;
            _clock = clock;
            _contentTypeProvider = contentTypeProvider;

            var s3Config = new AmazonS3Config { ServiceURL = options.EndPoint };
            _s3Client = new AmazonS3Client(options.Key, options.Secret, s3Config);

            if (!String.IsNullOrEmpty(_options.BasePath))
            {
                _basePrefix = NormalizePrefix(_options.BasePath);
            }
        }

        public async Task<IFileStoreEntry> GetFileInfoAsync(string path)
        {
            var metadata = await GetObjectMetadataAsync(path);

            if (metadata == null) return null;

            return new AWSFile(path, metadata.ContentLength, metadata.LastModified);
        }

        public async Task<IFileStoreEntry> GetDirectoryInfoAsync(string path)
        {
            if (path == String.Empty)
            {
                return new AWSDirectory(path, _clock.UtcNow);
            }

            var prefix = NormalizePrefix(path);

            var metadata = await GetObjectMetadataAsync(prefix, true);

            if (metadata != null)
            {
                return new AWSDirectory(path, metadata.LastModified);
            }

            return null;
        }

        public IAsyncEnumerable<IFileStoreEntry> GetDirectoryContentAsync(string path = null, bool includeSubDirectories = false)
        {
            if (includeSubDirectories)
            {
                return GetDirectoryContentFlatAsync(path);
            }
            else
            {
                return GetDirectoryContentByHierarchyAsync(path);
            }
        }

        private async IAsyncEnumerable<IFileStoreEntry> GetDirectoryContentByHierarchyAsync(string path = null)
        {
            var results = new List<IFileStoreEntry>();

            var prefix = this.Combine(_basePrefix, path);
            prefix = NormalizePrefix(prefix);

            var request = new ListObjectsV2Request
            {
                BucketName = _options.BucketName,
                Prefix = prefix,
                Delimiter = "/"
            };

            string continuationToken = null;
            do
            {
                request.ContinuationToken = continuationToken;
                var page = await _s3Client.ListObjectsV2Async(request);
                foreach (var commonPrefix in page.CommonPrefixes.Select(p => p[_options.BasePath.Length..].Trim('/')))
                {
                    yield return new AWSDirectory(commonPrefix, _clock.UtcNow);
                }
                foreach (var s3Object in page.S3Objects)
                {

                    var objectKey = s3Object.Key;
                    if (objectKey == prefix) continue;

                    var itemPath = objectKey[_options.BasePath.Length..];
                    yield return new AWSFile(itemPath, s3Object.Size, s3Object.LastModified);
                }

                continuationToken = page.NextContinuationToken;
            }
            while (continuationToken != null);
        }

        private async IAsyncEnumerable<IFileStoreEntry> GetDirectoryContentFlatAsync(string path = null)
        {
            var results = new List<IFileStoreEntry>();

            // Folders are considered case sensitive in blob storage.
            var directories = new HashSet<string>();

            var prefix = this.Combine(_basePrefix, path);
            prefix = NormalizePrefix(prefix);

            var request = new ListObjectsV2Request { BucketName = _options.BucketName, Prefix = prefix };

            string continuationToken = null;
            do
            {
                request.ContinuationToken = continuationToken;
                var page = await _s3Client.ListObjectsV2Async(request);
                foreach (var s3Object in page.S3Objects)
                {
                    var key = s3Object.Key;
                    var objectPath = key[_basePrefix.Length..];
                    if (objectPath.EndsWith("/"))
                    {
                        yield return new AWSDirectory(objectPath.Trim('/'), _clock.UtcNow);
                    }
                    else
                    {
                        yield return new AWSFile(objectPath.Trim('/'), s3Object.Size, s3Object.LastModified);
                    }
                }

                continuationToken = page.NextContinuationToken;
            }
            while (continuationToken != null);
        }


        public async Task<bool> TryCreateDirectoryAsync(string path)
        {
            // Since directories are only created implicitly when creating blobs, we
            // simply pretend like we created the directory, unless there is already
            // a blob with the same path.

            var metadata = await GetObjectMetadataAsync(path);

            if (metadata != null)
            {
                throw new FileStoreException($"Cannot create directory because the path '{path}' already exists and is a file.");
            }

            metadata = await GetObjectMetadataAsync(path, true);

            if (metadata == null)
            {
                await CreateDirectoryAsync(path);
            }

            return true;
        }

        public async Task<bool> TryDeleteFileAsync(string path)
        {
            var fullPath = this.Combine(_options.BasePath, path);
            var request = new DeleteObjectRequest { BucketName = _options.BucketName, Key = fullPath };
            try
            {
                var response = await _s3Client.DeleteObjectAsync(request);
                return true;
            }
            catch (AmazonS3Exception s3ex)
            {
                if (s3ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;
                throw;
            }
        }

        public async Task<bool> TryDeleteDirectoryAsync(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new FileStoreException("Cannot delete the root directory.");
            }

            var objectsWereDeleted = false;
            var prefix = this.Combine(_basePrefix, path);
            prefix = NormalizePrefix(prefix);

            var listRequest = new ListObjectsV2Request { BucketName = _options.BucketName, Prefix = prefix };
            string continuationToken = null;
            do
            {
                listRequest.ContinuationToken = continuationToken;
                var listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                var deleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _options.BucketName,
                    Objects = listResponse.S3Objects.Select(o => new KeyVersion() { Key = o.Key }).ToList()
                };
                try
                {
                    var deleteResponse = await _s3Client.DeleteObjectsAsync(deleteRequest);
                    objectsWereDeleted = true;
                }
                catch { }

                continuationToken = listResponse.NextContinuationToken;
            }
            while (continuationToken != null);

            return objectsWereDeleted;
        }

        public async Task MoveFileAsync(string oldPath, string newPath)
        {
            await CopyFileAsync(oldPath, newPath);
            await TryDeleteFileAsync(oldPath);
        }

        public async Task CopyFileAsync(string srcPath, string dstPath)
        {
            if (srcPath == dstPath)
            {
                throw new ArgumentException($"The values for {nameof(srcPath)} and {nameof(dstPath)} must not be the same.");
            }

            var oldObject = await GetObjectMetadataAsync(srcPath);
            var newObject = await GetObjectMetadataAsync(dstPath);

            if (oldObject == null)
            {
                throw new FileStoreException($"Cannot copy file '{srcPath}' because it does not exist.");
            }

            if (newObject != null)
            {
                throw new FileStoreException($"Cannot copy file '{srcPath}' because a file already exists in the new path '{dstPath}'.");
            }

            var request = new CopyObjectRequest
            {
                SourceBucket = _options.BucketName,
                DestinationBucket = _options.BucketName,
                SourceKey = this.Combine(_basePrefix, srcPath),
                DestinationKey = this.Combine(_basePrefix, dstPath)
            };

            try
            {
                var response = await _s3Client.CopyObjectAsync(request);
            }
            catch
            {
                throw new FileStoreException($"Error while copying file '{srcPath}'.");
            }
        }

        public async Task<Stream> GetFileStreamAsync(string path)
        {
            path = path.Trim('/');
            if (!String.IsNullOrEmpty(_options.BasePath))
            {
                path = this.Combine(_options.BasePath, path);
            }
            var request = new GetObjectRequest { BucketName = _options.BucketName, Key = path };
            try
            {
                var response = await _s3Client.GetObjectAsync(request);

                using var responseStream = response.ResponseStream;
                var memoryStream = new MemoryStream();
                await responseStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0L, SeekOrigin.Begin);

                return memoryStream;
            }
            catch (AmazonS3Exception s3ex)
            {
                if (s3ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    throw new FileStoreException($"Cannot get file stream because the file '{path}' does not exist.");
                throw;
            }
        }

        // Reduces the need to call blob.FetchAttributes, and blob.ExistsAsync,
        // as Azure Storage Library will perform these actions on OpenReadAsync().
        public Task<Stream> GetFileStreamAsync(IFileStoreEntry fileStoreEntry)
        {
            return GetFileStreamAsync(fileStoreEntry.Path);
        }

        public async Task<string> CreateFileFromStreamAsync(string path, Stream inputStream, bool overwrite = false)
        {
            var fullKey = this.Combine(_options.BasePath, path);
            var obj = await GetObjectMetadataAsync(fullKey);

            if (!overwrite && obj != null)
            {
                throw new FileStoreException($"Cannot create file '{path}' because it already exists.");
            }

            _contentTypeProvider.TryGetContentType(path, out var contentType);

            var fileTransferUtility = new TransferUtility(_s3Client);

            var fileTransferRequest = new TransferUtilityUploadRequest
            {
                BucketName = _options.BucketName,
                Key = fullKey,
                InputStream = inputStream
            };
            fileTransferRequest.Metadata.Add("Content-Type", contentType ?? "application/octet-stream");

            await fileTransferUtility.UploadAsync(inputStream, _options.BucketName, fullKey);

            return path;
        }

        private async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(string path, bool isDir = false)
        {
            var blobPath = this.Combine(_options.BasePath, path) + (isDir ? "/" : "");
            var request = new GetObjectMetadataRequest { BucketName = _options.BucketName, Key = blobPath };
            try
            {
                var objectMetadata = await _s3Client.GetObjectMetadataAsync(request);
                return objectMetadata;
            }
            catch (AmazonS3Exception s3Ex)
            {
                if (s3Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw;
            }
        }

        private async Task CreateDirectoryAsync(string path)
        {
            var directory = this.Combine(_basePrefix, path) + "/";
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = directory,
                ContentBody = ""
            };
            await _s3Client.PutObjectAsync(request);
        }

        /// <summary>
        /// Blob prefix requires a trailing slash except when loading the root of the container.
        /// </summary>
        private string NormalizePrefix(string prefix)
        {
            prefix = prefix.Trim('/') + '/';
            if (prefix.Length == 1)
            {
                return String.Empty;
            }
            else
            {
                return prefix;
            }
        }
    }
}
