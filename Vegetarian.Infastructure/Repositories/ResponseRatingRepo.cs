using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Abstractions.Persistence;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.Infrastructure.Repositories
{
    public class ResponseRatingRepo : GenericRepo<ResponseRating>, IResponseRatingRepo
    {
        public ResponseRatingRepo(VegetarianDbContext context) : base(context) { }
      
    }
}
