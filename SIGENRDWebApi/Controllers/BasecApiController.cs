using Microsoft.AspNetCore.Mvc;

namespace SIGENRDWebApi.Controllers
{
    [Route("api/v{version:apiversion}/[controller]")]
    [ApiController]
    public abstract class BasecApiController : ControllerBase
    {
        
    }
}
