using Microsoft.AspNetCore.Http;

namespace SWP_SportCenter.Service.MediaService;

public interface IService
{
    public Task<string> UploadAsync(IFormFile file);
    public Task<string> UploadVideoAsync(IFormFile file);
}