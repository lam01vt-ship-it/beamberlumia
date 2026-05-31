using Microsoft.EntityFrameworkCore;
using Tosix.Api.Entities;

namespace Tosix.Api.Data;

public static class ProductQueries
{
    public static IQueryable<Product> ApplySearch(this IQueryable<Product> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = $"%{search.Trim()}%";
        return query.Where(p =>
            EF.Functions.ILike(p.Name, term) || EF.Functions.ILike(p.Code, term));
    }
}
