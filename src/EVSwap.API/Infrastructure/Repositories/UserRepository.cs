using EVSwap.API.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> FindByEmailAsync(string email)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> FindByUsernameAsync(string username)
        => await _dbSet.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        => await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync();
}
