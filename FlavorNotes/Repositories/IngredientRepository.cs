using Microsoft.EntityFrameworkCore;
using FlavorNotes.Data;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;
using FlavorNotes.DTO;

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

    public async Task<PagedResponseDto<IngredientDto>> GetPagedAsync(int page, int pageSize, string? search)
    {
        var query = _context.Ingredients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.Contains(search));
        }

        var total = await query.CountAsync();

        var ingredients = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = ingredients.Select(i => new IngredientDto
        {
            IngredientId = i.IngredientId,
            Name = i.Name
        }).ToList();

        return new PagedResponseDto<IngredientDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
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


