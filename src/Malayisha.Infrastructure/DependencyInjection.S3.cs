using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Malayisha.Application.Abstractions.Storage;
using Malayisha.Infrastructure.Options;
using Malayisha.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Malayisha.Infrastructure;

public static partial class DependencyInjection
{
    private static void AddS3(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<S3Options>(configuration.GetSection(S3Options.SectionName));

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var s3Options = serviceProvider.GetRequiredService<IOptions<S3Options>>().Value;

            if (string.IsNullOrWhiteSpace(s3Options.Region))
            {
                throw new InvalidOperationException(
                    "S3 region was not configured. Set S3:Region in application configuration.");
            }

            var region = RegionEndpoint.GetBySystemName(s3Options.Region);
            AWSCredentials? credentials = null;

            if (!string.IsNullOrWhiteSpace(s3Options.AccessKeyId) &&
                !string.IsNullOrWhiteSpace(s3Options.SecretAccessKey))
            {
                credentials = new BasicAWSCredentials(s3Options.AccessKeyId, s3Options.SecretAccessKey);
            }

            return credentials is null
                ? new AmazonS3Client(region)
                : new AmazonS3Client(credentials, region);
        });

        services.AddSingleton<IFileStorageService, S3FileStorageService>();
    }
}
