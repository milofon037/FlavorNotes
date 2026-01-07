using FlavorNotes.DTO;

namespace FlavorNotes.Services.Interfaces;

public interface IUserService
{
    Task<PagedResponseDto<UserDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<UserDto?> GetByIdAsync(int id);
    Task<UserDto> UpdateRoleAsync(int id, UpdateRoleDto dto, string adminRole);
}

