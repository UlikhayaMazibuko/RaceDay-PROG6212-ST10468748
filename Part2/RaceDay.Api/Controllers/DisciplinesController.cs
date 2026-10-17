using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers
{

    [ApiController]
    [Route("api/disciplines")]
    [Produces("application/json")]
    public class DisciplinesController : ControllerBase
    {
        private readonly RaceDayDbContext _db;

        public DisciplinesController(RaceDayDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<DisciplineResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDisciplines()
        {
            var disciplines = await _db.Disciplines
                .OrderBy(d => d.DisciplineId)
                .Select(d => new DisciplineResponse
                {
                    DisciplineId = d.DisciplineId,
                    DisciplineName = d.DisciplineName
                })
                .ToListAsync();

            return Ok(disciplines);
        }
    }
}