using FlavorNotes.Models.Entities;

namespace FlavorNotes.Repositories.Interfaces;

public interface IUnitRepository
{
    Task<List<Unit>> GetAllAsync();
    Task<Unit?> GetByIdAsync(int id);
    Task<Unit> CreateAsync(Unit unit);
    Task<bool> ExistsAsync(int id);
}


