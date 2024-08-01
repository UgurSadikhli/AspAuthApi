using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Task.Services
{
   public class BlobService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Blob storage connection string is not configured.");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public BlobContainerClient GetBlobContainerClient(string containerName)
    {
        return _blobServiceClient.GetBlobContainerClient(containerName);
    }
}

}
