using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByUsernameAsync(string username);
    Task<IEnumerable<string>> GetUserRolesAsync(int userId);
}
