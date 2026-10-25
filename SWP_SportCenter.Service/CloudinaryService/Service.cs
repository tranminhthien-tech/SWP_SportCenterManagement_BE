using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SWP_SportCenter.Service.MediaService;

namespace SWP_SportCenter.Service.CloudinaryService;

public class CloudinaryService: IService
{
    // CODE CŨ: private readonly Cloudinary _cloudinary;
    // FIX B22: tạo client khi thực sự upload, không làm hỏng form không đổi ảnh.
    private readonly Lazy<Cloudinary> _cloudinary;
    private readonly CloudinaryOptions _cloudinaryOptions = new();

    public CloudinaryService(IConfiguration configuration)
    {
        configuration.GetSection(nameof(CloudinaryOptions)).Bind(_cloudinaryOptions);
        /* CODE CŨ: _cloudinary = new Cloudinary(new Account(
            _cloudinaryOptions.CloudName, _cloudinaryOptions.ApiKey, _cloudinaryOptions.ApiSecret)); */
        _cloudinary = new Lazy<Cloudinary>(() => new Cloudinary(new Account(
            _cloudinaryOptions.CloudName, _cloudinaryOptions.ApiKey, _cloudinaryOptions.ApiSecret))); // FIX B22
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        // FIX B22: endpoint ảnh không nhận video; giới hạn và xác thực chữ ký trước gọi cloud.
        await ImageValidation.ValidateAsync(file);
        if (string.IsNullOrWhiteSpace(_cloudinaryOptions.CloudName) || string.IsNullOrWhiteSpace(_cloudinaryOptions.ApiKey) ||
            string.IsNullOrWhiteSpace(_cloudinaryOptions.ApiSecret))
            throw new HttpRequestException("Dịch vụ ảnh chưa được cấu hình trên server.");
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.", nameof(file));

        await using var stream = file.OpenReadStream();

        if (IsImageFile(file))
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, stream)
            };

            // CODE CŨ: _cloudinary.UploadAsync(uploadParams)
            var result = await _cloudinary.Value.UploadAsync(uploadParams); // FIX B22

            // CODE CŨ: throw new Exception(result.Error.Message);
            // FIX B22: không đưa thông tin kỹ thuật/credential từ provider ra client.
            if (result.Error != null || result.SecureUrl == null)
                throw new HttpRequestException("Không thể tải ảnh lên dịch vụ lưu trữ.");

            return result.SecureUrl.ToString();
        }

        if (IsVideoFile(file))
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream)
            };

            // CODE CŨ: _cloudinary.UploadAsync(uploadParams)
            var result = await _cloudinary.Value.UploadAsync(uploadParams); // FIX B22

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

            // CODE CŨ: _cloudinary.UploadAsync(uploadParams)
            var result = await _cloudinary.Value.UploadAsync(uploadParams); // FIX B22

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
