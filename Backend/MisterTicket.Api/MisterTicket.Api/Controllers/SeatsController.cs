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
    public class SeatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SeatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Seats
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SeatDto>>> GetSeats()
        {
            var seats = await _context.Seats
                .Include(s => s.Court)
                .Select(s => new SeatDto
                {
                    Id = s.Id,
                    SeatNumber = s.SeatNumber,
                    Row = s.Row,
                    Zone = s.Zone,
                    Price = s.Price,
                    Status = s.Status,
                    CourtId = s.CourtId,
                    CourtName = s.Court.Name
                })
                .ToListAsync();

            return Ok(seats);
        }

        // GET: api/Seats/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SeatDto>> GetSeat(int id)
        {
            var seat = await _context.Seats
                .Include(s => s.Court)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seat == null)
            {
                return NotFound();
            }

            var seatDto = new SeatDto
            {
                Id = seat.Id,
                SeatNumber = seat.SeatNumber,
                Row = seat.Row,
                Zone = seat.Zone,
                Price = seat.Price,
                Status = seat.Status,
                CourtId = seat.CourtId,
                CourtName = seat.Court.Name
            };

            return Ok(seatDto);
        }

        // GET: api/Seats/court/1
        [HttpGet("court/{courtId}")]
        public async Task<ActionResult<IEnumerable<SeatDto>>> GetSeatsByCourt(int courtId)
        {
            var seats = await _context.Seats
                .Include(s => s.Court)
                .Where(s => s.CourtId == courtId)
                .Select(s => new SeatDto
                {
                    Id = s.Id,
                    SeatNumber = s.SeatNumber,
                    Row = s.Row,
                    Zone = s.Zone,
                    Price = s.Price,
                    Status = s.Status,
                    CourtId = s.CourtId,
                    CourtName = s.Court.Name
                })
                .ToListAsync();

            return Ok(seats);
        }

        // POST: api/Seats
        [HttpPost]
        // [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<SeatDto>> CreateSeat(CreateSeatDto createSeatDto)
        {
            var court = await _context.Courts.FindAsync(createSeatDto.CourtId);
            if (court == null)
            {
                return BadRequest(new { message = "Court not found" });
            }

            var seat = new Seat
            {
                SeatNumber = createSeatDto.SeatNumber,
                Row = createSeatDto.Row,
                Zone = createSeatDto.Zone,
                Price = createSeatDto.Price,
                CourtId = createSeatDto.CourtId,
                Status = "Available"
            };

            _context.Seats.Add(seat);
            await _context.SaveChangesAsync();

            var seatDto = new SeatDto
            {
                Id = seat.Id,
                SeatNumber = seat.SeatNumber,
                Row = seat.Row,
                Zone = seat.Zone,
                Price = seat.Price,
                Status = seat.Status,
                CourtId = seat.CourtId,
                CourtName = court.Name
            };

            return CreatedAtAction(nameof(GetSeat), new { id = seat.Id }, seatDto);
        }

        // PUT: api/Seats/5
        [HttpPut("{id}")]
        // [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> UpdateSeat(int id, CreateSeatDto updateSeatDto)
        {
            var seat = await _context.Seats.FindAsync(id);

            if (seat == null)
            {
                return NotFound();
            }

            var court = await _context.Courts.FindAsync(updateSeatDto.CourtId);
            if (court == null)
            {
                return BadRequest(new { message = "Court not found" });
            }

            seat.SeatNumber = updateSeatDto.SeatNumber;
            seat.Row = updateSeatDto.Row;
            seat.Zone = updateSeatDto.Zone;
            seat.Price = updateSeatDto.Price;
            seat.CourtId = updateSeatDto.CourtId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Seats/5
        [HttpDelete("{id}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSeat(int id)
        {
            var seat = await _context.Seats.FindAsync(id);

            if (seat == null)
            {
                return NotFound();
            }

            _context.Seats.Remove(seat);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // POST: api/Seats/generate/{courtId}
        [HttpPost("generate/{courtId}")]
        // [Authorize(Roles = "Admin")]
        public async Task<ActionResult> GenerateSeats(int courtId, [FromBody] GenerateSeatsDto dto)
        {
            var court = await _context.Courts.FindAsync(courtId);
            if (court == null)
            {
                return NotFound(new { message = "Court not found" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Supprimer les anciennes données
                var oldSeatIds = await _context.Seats
                    .Where(s => s.CourtId == courtId)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (oldSeatIds.Any())
                {
                    var reservationSeatsToRemove = await _context.ReservationSeats
                        .Where(rs => oldSeatIds.Contains(rs.SeatId))
                        .ToListAsync();

                    if (reservationSeatsToRemove.Any())
                    {
                        _context.ReservationSeats.RemoveRange(reservationSeatsToRemove);
                        await _context.SaveChangesAsync();
                    }

                    var oldSeats = await _context.Seats
                        .Where(s => s.CourtId == courtId)
                        .ToListAsync();

                    _context.Seats.RemoveRange(oldSeats);
                    await _context.SaveChangesAsync();
                }

                // 2. Générer selon template
                var seats = new List<Seat>();

                switch (dto.Template)
                {
                    case "small": // EXACTEMENT 40 sièges
                                  // VIP : 0 sièges (pas de VIP en small)
                                  // Tribune : 50% = 20 sièges
                        seats.AddRange(GenerateTribuneSeats(courtId, dto.TribunePrice, 2, 5, "Tribune Gauche"));  // 10 sièges
                        seats.AddRange(GenerateTribuneSeats(courtId, dto.TribunePrice, 2, 5, "Tribune Droite"));  // 10 sièges
                                                                                                                  // Gradin : 50% = 20 sièges
                        seats.AddRange(GenerateGradinSeats(courtId, dto.GradinPrice, 2, 10));  // 20 sièges
                                                                                               // TOTAL = 0 + 10 + 10 + 20 = 40 ✓
                        break;

                    case "medium": // EXACTEMENT 70 sièges
                                   // VIP : 10% ≈ 7 sièges (on fait 6 pour être rond)
                        seats.AddRange(GenerateVipSeats(courtId, dto.VipPrice, 1, 6));  // 6 sièges

                        // Tribune : 40% = 28 sièges (14 gauche, 14 droite)
                        seats.AddRange(GenerateTribuneSeats(courtId, dto.TribunePrice, 2, 7, "Tribune Gauche"));  // 14 sièges
                        seats.AddRange(GenerateTribuneSeats(courtId, dto.TribunePrice, 2, 7, "Tribune Droite"));  // 14 sièges

                        // Gradin : 50% = 36 sièges
                        seats.AddRange(GenerateGradinSeats(courtId, dto.GradinPrice, 3, 12));  // 36 sièges

                        // TOTAL = 6 + 14 + 14 + 36 = 70 ✓
                        break;

                    case "large": // EXACTEMENT 120 sièges
                                  // VIP : 15% = 18 sièges
                        seats.AddRange(GenerateVipSeats(courtId, dto.VipPrice, 2, 9));  // 18 sièges

                        // Tribune : 35% = 42 sièges (21 gauche, 21 droite)
                        seats.AddRange(GenerateTribuneSeats(courtId, dto.TribunePrice, 3, 7, "Tribune Gauche"));  // 21 sièges
                        seats.AddRange(GenerateTribuneSeats(courtId, dto.TribunePrice, 3, 7, "Tribune Droite"));  // 21 sièges

                        // Gradin : 50% = 60 sièges
                        seats.AddRange(GenerateGradinSeats(courtId, dto.GradinPrice, 4, 15));  // 60 sièges

                        // TOTAL = 18 + 21 + 21 + 60 = 120 ✓
                        break;
                    default:
                        await transaction.RollbackAsync();
                        return BadRequest(new { message = "Invalid template" });
                }

                // 3. VALIDATION : vérifier que capacité suffit
                if (seats.Count > court.Capacity)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        message = $"Template génère {seats.Count} sièges mais capacité du court est seulement {court.Capacity}. Choisissez un template plus petit ou augmentez la capacité."
                    });
                }

                _context.Seats.AddRange(seats);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    message = $"{seats.Count} seats generated successfully",
                    count = seats.Count,
                    capacity = court.Capacity,
                    remaining = court.Capacity - seats.Count
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error generating seats", error = ex.Message });
            }
        }

        // Méthodes helpers pour générer les sièges
        private List<Seat> GenerateVipSeats(int courtId, decimal price, int rows, int seatsPerRow)
        {
            var seats = new List<Seat>();
            var rowLetters = new[] { "A", "B", "C", "D" };

            for (int r = 0; r < rows; r++)
            {
                for (int s = 1; s <= seatsPerRow; s++)
                {
                    seats.Add(new Seat
                    {
                        CourtId = courtId,
                        SeatNumber = $"VIP-{rowLetters[r]}{s}",
                        Row = rowLetters[r],
                        Zone = "VIP",
                        Price = price,
                        Status = "Available"
                    });
                }
            }
            return seats;
        }

        private List<Seat> GenerateTribuneSeats(int courtId, decimal price, int rows, int seatsPerRow, string zone = "Tribune")
        {
            var seats = new List<Seat>();
            var rowLetters = new[] { "B", "C", "D", "E" };

            for (int r = 0; r < rows; r++)
            {
                for (int s = 1; s <= seatsPerRow; s++)
                {
                    var prefix = zone == "Tribune Gauche" ? "TG" : zone == "Tribune Droite" ? "TD" : "T";
                    seats.Add(new Seat
                    {
                        CourtId = courtId,
                        SeatNumber = $"{prefix}-{rowLetters[r]}{s}",
                        Row = rowLetters[r],
                        Zone = zone,
                        Price = price,
                        Status = "Available"
                    });
                }
            }
            return seats;
        }

        private List<Seat> GenerateGradinSeats(int courtId, decimal price, int rows, int seatsPerRow, int rowOffset = 0)
        {
            var seats = new List<Seat>();
            var rowLetters = new[] { "D", "E", "F", "G", "H", "I" }; // Plus de lettres

            for (int r = 0; r < rows; r++)
            {
                for (int s = 1; s <= seatsPerRow; s++)
                {
                    seats.Add(new Seat
                    {
                        CourtId = courtId,
                        SeatNumber = $"GH-{rowLetters[r + rowOffset]}{s}",
                        Row = rowLetters[r + rowOffset],
                        Zone = "Gradin Haut",
                        Price = price,
                        Status = "Available"
                    });
                }
            }
            return seats;
        }
    }
}