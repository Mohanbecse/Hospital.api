using Microsoft.AspNetCore.Mvc;

namespace Hospital.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public class TestController : ControllerBase
    {
        public TestController()
        {
        }

        [HttpGet(Name = "test")]
        public IActionResult Get()
        {
            return Ok("Test OK");
        }
    }
}
