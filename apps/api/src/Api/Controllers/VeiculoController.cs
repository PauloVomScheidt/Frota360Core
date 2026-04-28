using Frota360.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Frota360.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VeiculoController(IVeiculoService service) : ControllerBase
    {
        /// <summary>Retorna todos os veículos da frota.</summary>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var veiculos = await service.GetAllAsync();
            return Ok(veiculos);
        }
    }
}
