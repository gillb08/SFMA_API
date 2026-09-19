using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SFMA_API.Data.Extensions;
using SFMA_API.Data.Interfaces;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SFMA_API.Data.Implementation
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly DbContext _dbContext;
        private readonly DbSet<T> _dbSet;

        public Repository(DbContext context)
        {
            _dbContext = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _dbContext.Set<T>();
        }

        public virtual T Add(T obj)
        {
            _dbSet.Add(obj);
            return obj;
        }

        public virtual async Task<T> AddAsync(T obj, bool tracking = false)
        {
            await _dbSet.AddAsync(obj);
            await SaveAsync();
            if (!tracking)
                _dbContext.Entry(obj).State = EntityState.Detached;
            return obj;
        }

        public virtual void AddRange(IEnumerable<T> records)
        {
            _dbSet.AddRange(records);
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> records)
        {
            await _dbSet.AddRangeAsync(records);
            await SaveAsync();
        }

        public virtual long Count(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null ? _dbSet.LongCount() : _dbSet.LongCount(predicate);
        }

        public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null ? await _dbSet.LongCountAsync() : await _dbSet.LongCountAsync(predicate);
        }

        public virtual async Task<decimal> SumAsync(Expression<Func<T, decimal>> predicate)
        {
            return await _dbSet.SumAsync(predicate);
        }

        public virtual async Task<int> SumAsync(Expression<Func<T, int>> predicate)
        {
            return await _dbSet.SumAsync(predicate);
        }

        public virtual async Task<long> SumAsync(Expression<Func<T, long>> predicate)
        {
            return await _dbSet.SumAsync(predicate);
        }

        public virtual bool Delete(Expression<Func<T, bool>> predicate)
        {
            var records = _dbSet.Where(predicate);
            _dbSet.RemoveRange(records);
            return true;
        }

        public virtual bool Delete(T obj)
        {
            _dbSet.Remove(obj);
            return true;
        }

        public virtual async Task DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            Delete(predicate);
            await SaveAsync();
        }

        public virtual async Task DeleteAsync(T obj, bool tracking = false)
        {
            Delete(obj);
            await SaveAsync();
        }

        public virtual bool DeleteById(object id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
            {
                Delete(entity);
                return true;
            }
            return false;
        }

        public virtual async Task DeleteByIdAsync(object id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                Delete(entity);
                await SaveAsync();
            }
        }

        public virtual bool DeleteRange(Expression<Func<T, bool>> predicate)
        {
            return Delete(predicate);
        }

        public virtual bool DeleteRange(IEnumerable<T> records)
        {
            _dbSet.RemoveRange(records);
            return true;
        }

        public virtual async Task DeleteRangeAsync(Expression<Func<T, bool>> predicate)
        {
            DeleteRange(predicate);
            await SaveAsync();
        }

        public virtual async Task DeleteRangeAsync(IEnumerable<T> records)
        {
            DeleteRange(records);
            await SaveAsync();
        }

        public virtual void Dispose()
        {
            _dbContext.Dispose();
        }

        public virtual IEnumerable<T> GetAll(Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, params string[] includeProperties)
        {
            IQueryable<T> query = _dbSet;
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
            return orderBy != null ? orderBy(query).ToList() : query.ToList();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
        {
            IQueryable<T> query = _dbSet;
            if (include != null)
            {
                query = include(query);
            }
            return orderBy != null ? await orderBy(query).ToListAsync() : await query.ToListAsync();
        }

        public virtual IEnumerable<T> GetBy(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int? skip = null, int? take = null, params string[] includeProperties)
        {
            IQueryable<T> query = _dbSet;
            if (predicate != null)
            {
                query = query.Where(predicate);
            }
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);
            return orderBy != null ? orderBy(query).ToList() : query.ToList();
        }

        public virtual async Task<PagedList<T>> GetPagedItems(RequestParameters parameters, Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
        {
            IQueryable<T> query = _dbSet;
            if (include != null) query = include(query);
            return await query.GetPagedItems(parameters, predicate);
        }

        public virtual async Task<IEnumerable<T>> GetByAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int? skip = null, int? take = null, params string[] includeProperties)
        {
            IQueryable<T> query = _dbSet;
            if (predicate != null) query = query.Where(predicate);
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }
            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);
            return orderBy != null ? await orderBy(query).ToListAsync() : await query.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetByAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int? skip = null, int? take = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool tracking = false)
        {
            IQueryable<T> query = tracking ? _dbSet : _dbSet.AsNoTracking();
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);
            return orderBy != null ? await orderBy(query).ToListAsync() : await query.ToListAsync();
        }

        public virtual async Task<T?> GetSingleByAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int? skip = null, int? take = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool tracking = false)
        {
            IQueryable<T> query = tracking ? _dbSet : _dbSet.AsNoTracking();
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);
            return orderBy != null ? await orderBy(query).FirstOrDefaultAsync() : await query.FirstOrDefaultAsync();
        }

        public virtual async Task<T?> LastAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool disableTracking = true)
        {
            IQueryable<T> query = disableTracking ? _dbSet.AsNoTracking() : _dbSet;
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            return orderBy != null ? await orderBy(query).LastOrDefaultAsync() : await query.LastOrDefaultAsync();
        }

        public virtual T? GetById(object id)
        {
            return _dbSet.Find(id);
        }

        public virtual async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null ? await _dbSet.AnyAsync() : await _dbSet.AnyAsync(predicate);
        }

        public virtual bool Any(Expression<Func<T, bool>>? predicate = null)
        {
            return predicate == null ? _dbSet.Any() : _dbSet.Any(predicate);
        }

        public virtual IQueryable<T> GetQueryable(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, int? skip = null, int? take = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
        {
            IQueryable<T> query = _dbSet;
            if (include != null) query = include(query);
            if (predicate != null) query = query.Where(predicate);
            if (skip.HasValue) query = query.Skip(skip.Value);
            if (take.HasValue) query = query.Take(take.Value);
            return orderBy != null ? orderBy(query) : query;
        }

        public virtual T? GetSingleBy(Expression<Func<T, bool>> predicate)
        {
            return _dbSet.FirstOrDefault(predicate);
        }

        public virtual async Task<T?> GetSingleByAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual int Save()
        {
            return _dbContext.SaveChanges();
        }

        public virtual async Task<int> SaveAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public virtual T Update(T obj)
        {
            _dbSet.Update(obj);
            return obj;
        }

        public virtual async Task<T> UpdateAsync(T obj, bool tracking = false)
        {
            Update(obj);
            await SaveAsync();
            if (!tracking)
                _dbContext.Entry(obj).State = EntityState.Detached;
            return obj;
        }

        public virtual void UpdateRange(IEnumerable<T> records)
        {
            _dbSet.UpdateRange(records);
        }

        public virtual async Task UpdateRangeAsync(IEnumerable<T> records)
        {
            UpdateRange(records);
            await SaveAsync();
        }
    }
}
