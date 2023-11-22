using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TAIBackend.Entities;
using TAIBackend.Filler;

namespace TAIBackend.routes.db_add
{
    public class DatabaseController : Controller
    {
        private readonly UsersAdd _usersAdd;
        public DatabaseController(UsersAdd usersAdd)
        {
            _usersAdd = usersAdd;
        }

        [HttpGet("push")]
        public async Task<IActionResult> AddUser()
        {
            var user = new User() { FirstName = "Paweł", LastName = "Tomczyk", NickName = "As" };

            await _usersAdd.Seed(user);

            return Ok(user);


        }

        
    }
}
