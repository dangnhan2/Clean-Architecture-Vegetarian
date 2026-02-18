using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vegetarian.Application.Dtos.QueryParams
{
    public class RatingParams : BaseQueryParams
    {
        public int? Stars { get; set; }
    }
}
