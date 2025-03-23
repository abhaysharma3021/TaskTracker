using System.Linq.Expressions;
using TaskTracker.Shared;

namespace TaskTracker.Domain.Interfaces;

public interface ITaskRepository
{
    Task<Domain.Entities.Task?> GetByIdAsync(int id);
    Task<IEnumerable<Entities.Task>> GetAllAsync();
    Task<int> AddAsync(Entities.Task task);
    Task<Result> UpdateAsync(Entities.Task task);
    Task<Result> DeleteAsync(int id);

    Task<IEnumerable<Entities.Task>> FindAsync(Expression<Func<Entities.Task, bool>> predicate);
    Task<Entities.Task?> FindSingleAsync(Expression<Func<Entities.Task, bool>> predicate);
}
