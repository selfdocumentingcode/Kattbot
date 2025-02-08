using System.Collections.Generic;

namespace Kattbot.Common.Models;

public class PaginatedResult<T>
{
    public PaginatedResult()
    {
        Items = new List<T>();
    }

    public List<T> Items { get; set; }

    public int PageOffset { get; set; }

    public int PageCount { get; set; }

    public int TotalCount { get; set; }
}
