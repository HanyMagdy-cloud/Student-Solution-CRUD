using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApi.Data;
using StudentApi.Models;

namespace StudentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly StudentDbContext _db;

        public StudentsController(StudentDbContext db)
        {
            _db = db;
        }

        // GET: /api/students
        // Optional search: /api/students?searchString=alice  (matches Name contains)
        [HttpGet]
        public async Task<IActionResult> GetStudents([FromQuery] string? searchString)
        {
            // If no search term -> return all students
            if (string.IsNullOrWhiteSpace(searchString))
            {
                var allStudents = await _db.Students
                    .OrderBy(s => s.Id)
                    .ToListAsync();

                return Ok(allStudents);
            }

            // If there IS a search term -> filter by Name
            var filtered = await _db.Students
                .Where(s => s.Name != null && s.Name.Contains(searchString))
                .OrderBy(s => s.Id)
                .ToListAsync();

            return Ok(filtered);
        }

        // GET: /api/students/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _db.Students.FindAsync(id);
            return student is null ? NotFound() : Ok(student);
        }

        // POST: /api/students
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] Student student)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _db.Students.Add(student);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
        }

        // PUT: /api/students/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] Student updated)
        {
            if (id != updated.Id) return BadRequest("Route id and body id must match.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _db.Students.FindAsync(id);
            if (existing is null) return NotFound();

            existing.Name = updated.Name;
            existing.Email = updated.Email;
            existing.Phone = updated.Phone;
            existing.DateOfBearth = updated.DateOfBearth;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/students/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var existing = await _db.Students.FindAsync(id);
            if (existing is null) return NotFound();

            _db.Students.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
