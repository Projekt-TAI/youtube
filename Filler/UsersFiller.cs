using Microsoft.AspNetCore.Mvc;
using TAIBackend.DataBase;
using TAIBackend.Entities;
namespace TAIBackend.Filler;

public class UsersFiller
{
    private readonly DataBaseContext _dbContext;

    public UsersFiller(DataBaseContext dbContext)
    {
        _dbContext = dbContext;
    }
   

    public async Task Seed(List<User> test)
    {
        if(await _dbContext.Database.CanConnectAsync())
        {
                       
              foreach(var user in test)
                    _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            
            
        }
    }
    
}
