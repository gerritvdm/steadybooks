using Microsoft.EntityFrameworkCore;
using SteadyBooks.Data;

namespace SteadyBooks.Services;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _context;
    private readonly IResiliencePipelineService _resiliencePipelineService;
    private readonly ILogger<Repository<T>> _logger;
    private readonly DbSet<T> _dbSet;

    public Repository(
        ApplicationDbContext context,
        IResiliencePipelineService resiliencePipelineService,
        ILogger<Repository<T>> logger)
    {
        _context = context;
        _resiliencePipelineService = resiliencePipelineService;
        _logger = logger;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetDatabasePipeline<T?>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            _logger.LogDebug("Getting entity of type {EntityType} with id {Id}", typeof(T).Name, id);
            return await _dbSet.FindAsync(new object[] { id }, ct);
        }, cancellationToken);
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetDatabasePipeline<IEnumerable<T>>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            _logger.LogDebug("Getting all entities of type {EntityType}", typeof(T).Name);
            return await _dbSet.ToListAsync(ct);
        }, cancellationToken);
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetDatabasePipeline<T>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            _logger.LogDebug("Adding entity of type {EntityType}", typeof(T).Name);
            var entry = await _dbSet.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            return entry.Entity;
        }, cancellationToken);
    }

    public async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetDatabasePipeline<T>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            _logger.LogDebug("Updating entity of type {EntityType}", typeof(T).Name);
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(ct);
            return entity;
        }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetDatabasePipeline<bool>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            _logger.LogDebug("Deleting entity of type {EntityType} with id {Id}", typeof(T).Name, id);
            var entity = await _dbSet.FindAsync(new object[] { id }, ct);
            if (entity == null)
            {
                return false;
            }

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pipeline = _resiliencePipelineService.GetDatabasePipeline<int>();
        
        return await pipeline.ExecuteAsync(async ct =>
        {
            _logger.LogDebug("Saving changes to database");
            return await _context.SaveChangesAsync(ct);
        }, cancellationToken);
    }
}
