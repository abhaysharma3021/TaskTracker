using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;
using TaskTracker.Shared;

namespace TaskTracker.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<Domain.Entities.Task> _dbSet;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<Domain.Entities.Task>();
    }

    public async Task<int> AddAsync(Domain.Entities.Task task)
    {
        await _dbSet.AddAsync(task);
        await _context.SaveChangesAsync();
        return task.Id;
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return Result.Success();
        }
        return Result.Failure($"Invalid Entity: {nameof(entity)}");
    }

    public async Task<IEnumerable<Domain.Entities.Task>> FindAsync(Expression<Func<Domain.Entities.Task, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task<Domain.Entities.Task?> FindSingleAsync(Expression<Func<Domain.Entities.Task, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<Domain.Entities.Task>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<Domain.Entities.Task?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<Result> UpdateAsync(Domain.Entities.Task task)
    {
        _dbSet.Update(task);
        await _context.SaveChangesAsync();

        return Result.Success();
    }
}
