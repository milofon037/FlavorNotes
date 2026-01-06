using Microsoft.EntityFrameworkCore;
using FlavorNotes.Data;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories.Interfaces;

namespace FlavorNotes.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;

    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Tag>> GetAllAsync()
    {
        return await _context.Tags.ToListAsync();
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        return await _context.Tags.FindAsync(id);
    }

    public async Task<Tag> CreateAsync(Tag tag)
    {
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task DeleteAsync(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag != null)
        {
            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Tags.AnyAsync(t => t.TagId == id);
    }
}

