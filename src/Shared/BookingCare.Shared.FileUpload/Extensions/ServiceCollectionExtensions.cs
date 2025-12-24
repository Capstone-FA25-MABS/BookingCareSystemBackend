using Amazon.CloudFront;
using Amazon.S3;
using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BookingCare.Shared.FileUpload.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add S3 File Upload services to the service collection
    /// </summary>
    public static IServiceCollection AddS3FileUpload(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure S3 settings
        services.Configure<S3Configuration>(configuration.GetSection(S3Configuration.SectionName));
        services.Configure<CloudFrontConfiguration>(configuration.GetSection(CloudFrontConfiguration.SectionName));

        // Validate configuration
        services.AddSingleton<IValidateOptions<S3Configuration>, S3ConfigurationValidator>();

        // Add AWS services
        AddAwsServices(services);

        return services;
    }

    /// <summary>
    /// Add S3 File Upload services with custom configuration
    /// </summary>
    public static IServiceCollection AddS3FileUpload(this IServiceCollection services,
        Action<S3Configuration> configureS3,
        Action<CloudFrontConfiguration>? configureCloudFront = null)
    {
        services.Configure(configureS3);

        if (configureCloudFront != null)
        {
            services.Configure(configureCloudFront);
        }

        // Validate configuration
        services.AddSingleton<IValidateOptions<S3Configuration>, S3ConfigurationValidator>();

        // Add AWS services
        AddAwsServices(services);

        return services;
    }

    /// <summary>
    /// Add AWS services (S3 and CloudFront) to the service collection
    /// </summary>
    private static void AddAwsServices(IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(provider =>
        {
            var s3Config = provider.GetRequiredService<IOptions<S3Configuration>>().Value;

            var awsConfig = new Amazon.S3.AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3Config.Region),
                Timeout = TimeSpan.FromSeconds(s3Config.AwsSdkTimeoutSeconds),
                ReadWriteTimeout = TimeSpan.FromSeconds(s3Config.AwsSdkReadWriteTimeoutSeconds),
                RetryMode = Amazon.Runtime.RequestRetryMode.Standard,
                MaxErrorRetry = s3Config.AwsSdkMaxErrorRetry
            };

            return new AmazonS3Client(s3Config.AccessKey, s3Config.SecretKey, awsConfig);
        });

        services.AddSingleton<IAmazonCloudFront>(provider =>
        {
            var s3Config = provider.GetRequiredService<IOptions<S3Configuration>>().Value;
            var cloudFrontConfig = provider.GetRequiredService<IOptions<CloudFrontConfiguration>>().Value;

            if (string.IsNullOrEmpty(cloudFrontConfig.DistributionId))
            {
                return null!; // CloudFront is optional
            }

            var awsConfig = new AmazonCloudFrontConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3Config.Region)
            };

            return new AmazonCloudFrontClient(s3Config.AccessKey, s3Config.SecretKey, awsConfig);
        });

        // Add file upload service
        services.AddScoped<IFileUploadService, S3FileUploadService>();

        // Add file upload orchestrator
        services.AddScoped<FileUploadOrchestrator>();
    }
}

public class S3ConfigurationValidator : IValidateOptions<S3Configuration>
{
    public ValidateOptionsResult Validate(string? name, S3Configuration options)
    {
        var failures = new List<string>();

        if (string.IsNullOrEmpty(options.AccessKey))
        {
            failures.Add("AWS Access Key is required");
        }

        if (string.IsNullOrEmpty(options.SecretKey))
        {
            failures.Add("AWS Secret Key is required");
        }

        if (string.IsNullOrEmpty(options.BucketName))
        {
            failures.Add("S3 Bucket Name is required");
        }

        if (string.IsNullOrEmpty(options.Region))
        {
            failures.Add("AWS Region is required");
        }

        if (options.MaxFileSizeBytes <= 0)
        {
            failures.Add("Max file size must be greater than 0");
        }

        if (options.AllowedFileExtensions == null || options.AllowedFileExtensions.Length == 0)
        {
            failures.Add("At least one allowed file extension must be specified");
        }

        if (options.UploadTimeoutSeconds <= 0)
        {
            failures.Add("Upload timeout must be greater than 0");
        }

        if (options.UploadReadWriteTimeoutSeconds <= 0)
        {
            failures.Add("Upload read/write timeout must be greater than 0");
        }

        if (options.AwsSdkTimeoutSeconds <= 0)
        {
            failures.Add("AWS SDK timeout must be greater than 0");
        }

        if (options.AwsSdkReadWriteTimeoutSeconds <= 0)
        {
            failures.Add("AWS SDK read/write timeout must be greater than 0");
        }

        if (options.AwsSdkMaxErrorRetry < 0)
        {
            failures.Add("AWS SDK max error retry cannot be negative");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}