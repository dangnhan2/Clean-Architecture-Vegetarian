using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vegetarian.Application.Repositories;
using Vegetarian.Domain.Models;
using Vegetarian.Infrastructure.Data;

namespace Vegetarian.Infrastructure.Repositories
{
    public class VoucherRepo : GenericRepo<Voucher>, IVoucherRepo
    {
        public VoucherRepo(VegetarianDbContext context) : base(context) { }
    }
}
