using Project.Domain.Entities;

namespace Project.DAL.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
