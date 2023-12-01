using TAIBackend.DataBase;
using TAIBackend.Entities;
namespace TAIBackend.Filler;

public class UsersAdd
{
    private readonly DataBaseContext _dbContext;

    public UsersAdd(DataBaseContext dbContext)
    {
        _dbContext = dbContext;
    }
   

    public async Task Seed(User user)
    {
        if(await _dbContext.Database.CanConnectAsync())
        {

                    _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            
            
        }
    }
    
}
