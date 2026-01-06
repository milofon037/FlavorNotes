using Microsoft.EntityFrameworkCore;
using FlavorNotes.Data;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Repositories;

public class UnitRepository : IUnitRepository
{
    private readonly ApplicationDbContext _context;

    public UnitRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Unit>> GetAllAsync()
    {
        return await _context.Units.ToListAsync();
    }

    public async Task<Unit?> GetByIdAsync(int id)
    {
        return await _context.Units.FindAsync(id);
    }

    public async Task<Unit> CreateAsync(Unit unit)
    {
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return unit;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Units.AnyAsync(u => u.UnitId == id);
    }
}

