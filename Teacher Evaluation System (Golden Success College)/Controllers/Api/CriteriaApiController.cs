using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class CriteriaApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public CriteriaApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/CriteriaApi
        [HttpGet]
        public async Task<IActionResult> GetCriteria()
        {
            var criteria = await _context.Criteria.ToListAsync();

            var data = criteria.Select(c => new
            {
                criteriaId = c.CriteriaId,
                name = c.Name
            });

            return Ok(new
            {
                success = true,
                data = data
            });
        }

        // GET: api/CriteriaApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCriteria(int id)
        {
            var criteria = await _context.Criteria.FindAsync(id);

            if (criteria == null)
                return NotFound(new { success = false, message = "Criteria not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    criteriaId = criteria.CriteriaId,
                    name = criteria.Name
                }
            });
        }

        // POST: api/CriteriaApi
        [HttpPost]
        public async Task<IActionResult> PostCriteria([FromBody] Criteria criteria)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data" });

            // Check for duplicate name
            if (await _context.Criteria.AnyAsync(c => c.Name == criteria.Name))
                return BadRequest(new { success = false, message = "Criteria name already exists" });

            _context.Criteria.Add(criteria);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Criteria created successfully",
                data = new
                {
                    criteriaId = criteria.CriteriaId,
                    name = criteria.Name
                }
            });
        }

        // PUT: api/CriteriaApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCriteria(int id, [FromBody] Criteria criteria)
        {
            if (id != criteria.CriteriaId)
                return BadRequest(new { success = false, message = "Criteria ID mismatch" });

            var existing = await _context.Criteria.FindAsync(id);
            if (existing == null)
                return NotFound(new { success = false, message = "Criteria not found" });

            // Check for duplicate name (excluding current criteria)
            if (await _context.Criteria.AnyAsync(c => c.Name == criteria.Name && c.CriteriaId != id))
                return BadRequest(new { success = false, message = "Criteria name already exists" });

            existing.Name = criteria.Name;

            _context.Entry(existing).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CriteriaExists(id))
                    return NotFound(new { success = false, message = "Criteria not found" });
                else
                    throw;
            }

            return Ok(new
            {
                success = true,
                message = "Criteria updated successfully",
                data = new
                {
                    criteriaId = existing.CriteriaId,
                    name = existing.Name
                }
            });
        }

        // DELETE: api/CriteriaApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCriteria(int id)
        {
            var criteria = await _context.Criteria.FindAsync(id);
            if (criteria == null)
                return NotFound(new { success = false, message = "Criteria not found" });

            try
            {
                _context.Criteria.Remove(criteria);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Criteria deleted successfully"
                });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Cannot delete criteria. It may be in use by other records."
                });
            }
        }

        private bool CriteriaExists(int id)
        {
            return _context.Criteria.Any(e => e.CriteriaId == id);
        }
    }
}