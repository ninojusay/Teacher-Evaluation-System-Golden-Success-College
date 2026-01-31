using Microsoft.AspNetCore.Mvc;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestApiController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Hello from API");
        }
    }
}