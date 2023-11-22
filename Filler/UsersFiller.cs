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
   

    public async Task Seed()
    {
        if(await _dbContext.Database.CanConnectAsync())
        {

            var user = new User() { FirstName = "Ania", LastName = "Betonowska", NickName = "AniBe" };
                    _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            
            
        }
    }
    
}
