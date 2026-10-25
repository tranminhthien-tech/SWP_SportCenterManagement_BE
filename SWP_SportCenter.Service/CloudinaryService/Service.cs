using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SWP_SportCenter.Service.MediaService;

namespace SWP_SportCenter.Service.CloudinaryService;

public class CloudinaryService: IService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _cloudinaryOptions = new();

    public CloudinaryService(IConfiguration configuration)
    {
        configuration.GetSection(nameof(CloudinaryOptions)).Bind(_cloudinaryOptions);
        _cloudinary = new Cloudinary(new Account(
            _cloudinaryOptions.CloudName,
            _cloudinaryOptions.ApiKey,
            _cloudinaryOptions.ApiSecret));
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.", nameof(file));

        await using var stream = file.OpenReadStream();

        if (IsImageFile(file))
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream)
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return result.SecureUrl.ToString();
        }

        if (IsVideoFile(file))
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream)
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return result.SecureUrl.ToString();
        }

        throw new ArgumentException("Unsupported file type.");
    }

    public async Task<string> UploadVideoAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.", nameof(file));

        await using var stream = file.OpenReadStream();
        
        if (IsVideoFile(file))
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream)
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return result.SecureUrl.ToString();
        }

        throw new ArgumentException("Unsupported file type.");
    }

    private bool IsImageFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(ext);
    }

    private bool IsVideoFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return allowedExtensions.Contains(ext);
    }  
}