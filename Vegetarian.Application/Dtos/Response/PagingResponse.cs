using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.Response
{
    public class PagingResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public IEnumerable<T> Data { get; set; }

        public PagingResponse(int page, int pageSize, int total, IEnumerable<T> data)
        {
            Page = page;
            PageSize = pageSize;
            Total = total;
            Data = data;
        }
    }
}
