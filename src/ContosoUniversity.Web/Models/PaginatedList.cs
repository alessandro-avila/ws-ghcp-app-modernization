using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Web.Models;

/// <summary>
/// Async paginated list helper ported from src/ContosoUniversity/PaginatedList.cs
/// (legacy MVC 5 sync version).
///
/// rw-004 (F-001): converted <c>Create</c> to async <c>CreateAsync</c> using
/// <see cref="EntityFrameworkQueryableExtensions.CountAsync{T}(IQueryable{T}, System.Threading.CancellationToken)"/>
/// and <see cref="EntityFrameworkQueryableExtensions.ToListAsync{T}(IQueryable{T}, System.Threading.CancellationToken)"/>
/// so the StudentsController index path stays fully async end-to-end.
///
/// First reusable rewrite helper - subsequent CRUD controllers (Courses,
/// Instructors) consume this via the same sort/filter/paging template.
/// </summary>
public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);

        AddRange(items);
    }

    public bool HasPreviousPage => PageIndex > 1;

    public bool HasNextPage => PageIndex < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}
