using FlavorNotes.DTO;

namespace FlavorNotes.Services.Interfaces;

public interface ITagService
{
    Task<List<TagDto>> GetAllAsync();
    Task<PagedResponseDto<TagDto>> GetPagedAsync(int page, int pageSize, string? search);
    Task<TagDto?> GetByIdAsync(int id);
    Task<TagDto> CreateAsync(TagDto dto);
    Task DeleteAsync(int id, string userRole);
}

