using Microsoft.EntityFrameworkCore;
using SFMA_API.Data.Extensions.Utilities;
using SFMA_API.Models.Dtos.Request;
using SFMA_API.Models.Dtos.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SFMA_API.Data.Extensions
{
    public static class RepositoryExtensions
    {
        public static IQueryable<T> Sort<T>(this IQueryable<T> query, string orderByQueryString)
        {
            if (string.IsNullOrWhiteSpace(orderByQueryString))
                return query;

            var orderQuery = OrderQueryBuilder.CreateOrderQuery<T>(orderByQueryString);

            if (string.IsNullOrWhiteSpace(orderQuery))
                return query;

            return query.OrderBy(orderQuery);
        }

        public static async Task<PagedList<T>> GetPagedItems<T>(this IQueryable<T> query, RequestParameters parameters, Expression<Func<T, bool>>? searchExpression = null)
        {
            var skip = (parameters.PageNumber - 1) * parameters.PageSize;
            if (searchExpression != null)
                query = query.Where(searchExpression);

            if (!string.IsNullOrWhiteSpace(parameters.OrderBy))
                query = query.Sort(parameters.OrderBy);

            var count = await query.CountAsync();
            var items = await query.Skip(skip).Take(parameters.PageSize).ToListAsync();
            return new PagedList<T>(items, count, parameters.PageNumber, parameters.PageSize);
        }

        public static PagedList<T> GetPagedItems<T>(this IEnumerable<T> query, RequestParameters parameters, Func<T, bool>? searchExpression = null)
        {
            var skip = (parameters.PageNumber - 1) * parameters.PageSize;
            if (searchExpression != null)
                query = query.Where(searchExpression);

            var list = query.ToList();
            var items = list.Skip(skip).Take(parameters.PageSize).ToList();
            return new PagedList<T>(items, list.Count, parameters.PageNumber, parameters.PageSize);
        }
    }
}
