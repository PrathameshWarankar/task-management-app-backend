using Microsoft.AspNetCore.Mvc;
using task_management_app_backend.Models;
using task_management_app_backend.Services;

namespace task_management_app_backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Home : ControllerBase
    {
        private readonly DBServices _dBServices;

        public Home(DBServices dBServices)
        {
            _dBServices = dBServices;
        }

        [HttpPost]
        [Route("CreateAccount")]
        public async Task<IActionResult> CreateAccount([FromBody] User user)
        {
            var status = await _dBServices.CreateUser(user);

            if (status.IsError == false)
                return StatusCode(201, new { message = "Account created successfully" });
            else
                return StatusCode(Convert.ToInt32(status.ErrorCode), new { message = status.ErrorMessage});
        }
    }
}
