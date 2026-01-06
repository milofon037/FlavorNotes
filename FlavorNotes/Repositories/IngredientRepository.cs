using Microsoft.EntityFrameworkCore;
using FlavorNotes.Data;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Repositories;

public class IngredientRepository : IIngredientRepository
{
    private readonly ApplicationDbContext _context;

    public IngredientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Ingredient>> GetAllAsync()
    {
        return await _context.Ingredients.ToListAsync();
    }

    public async Task<Ingredient?> GetByIdAsync(int id)
    {
        return await _context.Ingredients.FindAsync(id);
    }

    public async Task<Ingredient> CreateAsync(Ingredient ingredient)
    {
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();
        return ingredient;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Ingredients.AnyAsync(i => i.IngredientId == id);
    }
}

