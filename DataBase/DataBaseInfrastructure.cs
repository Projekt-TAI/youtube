using Microsoft.EntityFrameworkCore;
using TAIBackend.Filler;

namespace TAIBackend.DataBase
{
    public static class DataBaseInfrastructure
    {

        public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddDbContext<DataBaseContext>(options => options.UseSqlServer(
                configuration.GetConnectionString("YouTube")));


            services.AddScoped<UsersAdd>();
        }


    }
}
