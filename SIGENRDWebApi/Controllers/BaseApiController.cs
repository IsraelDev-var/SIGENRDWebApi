using Microsoft.AspNetCore.Mvc;

namespace SIGENRDWebApi.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")] // Nota: apiVersion suele ser camelCase
    [ApiController]
    public abstract class BaseApiController : ControllerBase // Corregido: BaseApiController
    {
    }
}
