using FlavorNotes.Data;
using FlavorNotes.Models.Entities;
using FlavorNotes.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlavorNotes.Tests;

public class UserRepositoryTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddUserAndReturnIt()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        var result = await repo.CreateAsync(user);

        Assert.NotNull(result);
        Assert.True(result.UserId > 0);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenExists()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        var user = new User
        {
            Username = "findme",
            Email = "findme@example.com",
            PasswordHash = "hash",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        
        await repo.CreateAsync(user);
        var result = await repo.GetByIdAsync(user.UserId);

        Assert.NotNull(result);
        Assert.Equal("findme", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        
        var result = await repo.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_ShouldReturnUser_WhenExists()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        var user = new User
        {
            Username = "unique",
            Email = "unique@example.com",
            PasswordHash = "hash",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        
        await repo.CreateAsync(user);
        var result = await repo.GetByUsernameAsync("unique");

        Assert.NotNull(result);
        Assert.Equal("unique", result.Username);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenExists()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        var user = new User
        {
            Username = "user123",
            Email = "unique@test.com",
            PasswordHash = "hash",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        
        await repo.CreateAsync(user);
        var result = await repo.GetByEmailAsync("unique@test.com");

        Assert.NotNull(result);
        Assert.Equal("unique@test.com", result.Email);
    }

    [Fact]
    public async Task UsernameExistsAsync_ShouldReturnTrue_WhenExists()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        var user = new User
        {
            Username = "exists",
            Email = "exists@example.com",
            PasswordHash = "hash",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        
        await repo.CreateAsync(user);
        var exists = await repo.UsernameExistsAsync("exists");

        Assert.True(exists);
    }

    [Fact]
    public async Task UsernameExistsAsync_ShouldReturnFalse_WhenNotExists()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        
        var exists = await repo.UsernameExistsAsync("notexists");

        Assert.False(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnTrue_WhenExists()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        var user = new User
        {
            Username = "emailtest",
            Email = "emailexists@example.com",
            PasswordHash = "hash",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        
        await repo.CreateAsync(user);
        var exists = await repo.EmailExistsAsync("emailexists@example.com");

        Assert.True(exists);
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnFalse_WhenNotExists()
    {
        var context = GetDbContext();
        var repo = new UserRepository(context);
        
        var exists = await repo.EmailExistsAsync("notexists@example.com");

        Assert.False(exists);
    }
}

public class CategoryRepositoryTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddCategoryAndReturnIt()
    {
        var context = GetDbContext();
        var repo = new CategoryRepository(context);
        var category = new Category { Name = "Desserts" };

        var result = await repo.CreateAsync(category);

        Assert.NotNull(result);
        Assert.True(result.CategoryId > 0);
        Assert.Equal("Desserts", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenExists()
    {
        var context = GetDbContext();
        var repo = new CategoryRepository(context);
        var category = new Category { Name = "Breakfast" };
        
        await repo.CreateAsync(category);
        var result = await repo.GetByIdAsync(category.CategoryId);

        Assert.NotNull(result);
        Assert.Equal("Breakfast", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        var context = GetDbContext();
        var repo = new CategoryRepository(context);
        
        var result = await repo.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        var context = GetDbContext();
        var repo = new CategoryRepository(context);
        
        await repo.CreateAsync(new Category { Name = "Lunch" });
        await repo.CreateAsync(new Category { Name = "Dinner" });
        
        var result = await repo.GetAllAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory()
    {
        var context = GetDbContext();
        var repo = new CategoryRepository(context);
        var category = new Category { Name = "Original" };
        
        await repo.CreateAsync(category);
        category.Name = "Updated";
        await repo.UpdateAsync(category);

        var result = await repo.GetByIdAsync(category.CategoryId);
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCategory()
    {
        var context = GetDbContext();
        var repo = new CategoryRepository(context);
        var category = new Category { Name = "ToDelete" };
        
        await repo.CreateAsync(category);
        await repo.DeleteAsync(category.CategoryId);

        var result = await repo.GetByIdAsync(category.CategoryId);
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
    {
        var context = GetDbContext();
        var repo = new CategoryRepository(context);
        var category = new Category { Name = "Exists" };
        
        await repo.CreateAsync(category);
        var exists = await repo.ExistsAsync(category.CategoryId);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNotExists()
    {
        var context = GetDbContext();
        var repo = new CategoryRepository(context);
        
        var exists = await repo.ExistsAsync(999);

        Assert.False(exists);
    }
}