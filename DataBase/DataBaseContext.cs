using Microsoft.EntityFrameworkCore;
using TAIBackend.Entities;

namespace TAIBackend.DataBase
{
    public class DataBaseContext : DbContext
    {
        public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }

    }

    
}
