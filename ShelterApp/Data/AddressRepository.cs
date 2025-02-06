using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using ShelterApp;

namespace ShelterApp.Data
{
    public class AddressRepository : GenericRepository<Address>
    {
        public AddressRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<int> RegionCount()
        {
            return await _dbSet.Select(a => a.Region).Distinct().CountAsync();
        }
    }
}
