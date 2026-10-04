namespace SWP_SportCenter.Service.Receptionist;

public interface IReceptionistService
{
    Task<IEnumerable<Response.ReceptionistResponse>> GetAllAsync();
    Task<Response.ReceptionistResponse?> GetByIdAsync(Guid id);
    Task<Response.ReceptionistResponse?> GetByAccountIdAsync(Guid accountId);
    Task<Response.ReceptionistResponse> CreateAsync(
        Request.CreateReceptionistRequest request);
    Task<bool> UpdateAsync(Guid id, Request.UpdateReceptionistRequest request);
    Task<bool> DeleteAsync(Guid id);
}
