using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Models
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int PageOffset { get; set; }
        public int PageCount { get; set; }
        public int TotalCount { get; set; }

        public PaginatedResult()
        {
            Items = new List<T>();
        }
    }
}
