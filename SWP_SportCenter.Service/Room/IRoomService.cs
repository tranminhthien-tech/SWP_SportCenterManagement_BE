using static SWP_SportCenter.Service.Room.Request;
using static SWP_SportCenter.Service.Room.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Room;

public interface IRoomService
{
    Task<IEnumerable<RoomResponse>> GetAllAsync();
    Task<RoomResponse?> GetByIdAsync(Guid id);
    Task<RoomResponse> CreateAsync(CreateRoomRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateRoomRequest request);
    Task<bool> DeleteAsync(Guid id);
}