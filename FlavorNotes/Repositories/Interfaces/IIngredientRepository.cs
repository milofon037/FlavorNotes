using FlavorNotes.Models.Entities;

namespace FlavorNotes.Repositories.Interfaces;

public interface IIngredientRepository
{
    Task<List<Ingredient>> GetAllAsync();
    Task<Ingredient?> GetByIdAsync(int id);
    Task<Ingredient> CreateAsync(Ingredient ingredient);
    Task<bool> ExistsAsync(int id);
}

