using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Api.Data;
using MisterTicket.Api.DTOs;
using MisterTicket.Api.Models;

namespace MisterTicket.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CourtsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CourtsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Courts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourtDto>>> GetCourts()
        {
            var courts = await _context.Courts
                .Select(c => new CourtDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Capacity = c.Capacity,
                    Description = c.Description
                })
                .ToListAsync();

            return Ok(courts);
        }

        // GET: api/Courts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CourtDto>> GetCourt(int id)
        {
            var court = await _context.Courts.FindAsync(id);

            if (court == null)
            {
                return NotFound();
            }

            var courtDto = new CourtDto
            {
                Id = court.Id,
                Name = court.Name,
                Capacity = court.Capacity,
                Description = court.Description
            };

            return Ok(courtDto);
        }

        // POST: api/Courts
        [HttpPost]
        [Authorize(Roles = "Admin")] // SEULEMENT ADMIN
        public async Task<ActionResult<CourtDto>> CreateCourt(CreateCourtDto createCourtDto)
        {
            var court = new Court
            {
                Name = createCourtDto.Name,
                Capacity = createCourtDto.Capacity,
                Description = createCourtDto.Description
            };

            _context.Courts.Add(court);
            await _context.SaveChangesAsync();

            var courtDto = new CourtDto
            {
                Id = court.Id,
                Name = court.Name,
                Capacity = court.Capacity,
                Description = court.Description
            };

            return CreatedAtAction(nameof(GetCourt), new { id = court.Id }, courtDto);
        }

        // PUT: api/Courts/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // SEULEMENT ADMIN
        public async Task<IActionResult> UpdateCourt(int id, CreateCourtDto updateCourtDto)
        {
            var court = await _context.Courts.FindAsync(id);

            if (court == null)
            {
                return NotFound();
            }

            court.Name = updateCourtDto.Name;
            court.Capacity = updateCourtDto.Capacity;
            court.Description = updateCourtDto.Description;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Courts/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // SEULEMENT ADMIN
        public async Task<IActionResult> DeleteCourt(int id)
        {
            var court = await _context.Courts.FindAsync(id);

            if (court == null)
            {
                return NotFound();
            }

            _context.Courts.Remove(court);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}