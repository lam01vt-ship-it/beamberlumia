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

    /// <summary>
    /// Sorts products by the public sort key. Supported keys: "popular", "latest",
    /// "price_asc", "price_desc". Anything else (including null) falls back to the
    /// default manual order (SortOrder).
    /// </summary>
    public static IOrderedQueryable<Product> ApplySort(this IQueryable<Product> query, string? sort) =>
        (sort?.Trim().ToLowerInvariant()) switch
        {
            "popular" => query.OrderByDescending(p => p.IsFeatured).ThenBy(p => p.SortOrder),
            "latest" => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.SortOrder),
            "price_asc" => query.OrderBy(p => p.Price).ThenBy(p => p.SortOrder),
            "price_desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.SortOrder),
            _ => query.OrderBy(p => p.SortOrder),
        };
}
