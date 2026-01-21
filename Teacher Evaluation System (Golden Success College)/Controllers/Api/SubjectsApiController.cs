using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectsApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;

        public SubjectsApiController(Teacher_Evaluation_System__Golden_Success_College_Context context)
        {
            _context = context;
        }

        // GET: api/SubjectsApi
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetSubjects()
        {
            var subjects = await _context.Subject
                .Include(s => s.Level)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Level)
                .Include(s => s.Teacher)
                .ToListAsync();

            var data = subjects.Select(s => new
            {
                subjectId = s.SubjectId,
                subjectName = s.SubjectName,
                subjectCode = s.SubjectCode,
                levelId = s.LevelId,
                levelName = s.Level?.LevelName,
                sectionId = s.SectionId,
                sectionName = s.Section?.SectionName,
                sectionLevelName = s.Section?.Level?.LevelName,
                teacherId = s.TeacherId,
                teacherName = s.Teacher?.FullName,
                schedule = s.Schedule
            });

            return new ApiResponse<IEnumerable<object>>
            {
                Success = true,
                Message = "Subjects loaded successfully",
                Data = data
            };
        }

        // GET: api/SubjectsApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetSubject(int id)
        {
            var subject = await _context.Subject
                .Include(s => s.Level)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Level)
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.SubjectId == id);

            if (subject == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Subject not found",
                    Data = null
                });
            }

            var data = new
            {
                subjectId = subject.SubjectId,
                subjectName = subject.SubjectName,
                subjectCode = subject.SubjectCode,
                levelId = subject.LevelId,
                levelName = subject.Level?.LevelName,
                sectionId = subject.SectionId,
                sectionName = subject.Section?.SectionName,
                sectionLevelName = subject.Section?.Level?.LevelName,
                teacherId = subject.TeacherId,
                teacherName = subject.Teacher?.FullName,
                schedule = subject.Schedule
            };

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Subject loaded successfully",
                Data = data
            };
        }

        // POST: api/SubjectsApi
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostSubject([FromBody] SubjectDto subjectDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid data",
                    Data = null
                });
            }

            // Check for duplicate subject code
            if (await _context.Subject.AnyAsync(s => s.SubjectCode == subjectDto.SubjectCode))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Subject code already exists",
                    Data = null
                });
            }

            var subject = new Subject
            {
                SubjectName = subjectDto.SubjectName,
                SubjectCode = subjectDto.SubjectCode,
                LevelId = subjectDto.LevelId,
                SectionId = subjectDto.SectionId,
                TeacherId = subjectDto.TeacherId,
                Schedule = subjectDto.Schedule
            };

            _context.Subject.Add(subject);
            await _context.SaveChangesAsync();

            // Reload to get navigation properties
            await _context.Entry(subject).Reference(s => s.Level).LoadAsync();
            await _context.Entry(subject).Reference(s => s.Section).LoadAsync();
            await _context.Entry(subject).Reference(s => s.Teacher).LoadAsync();
            if (subject.Section != null)
            {
                await _context.Entry(subject.Section).Reference(sec => sec.Level).LoadAsync();
            }

            var data = new
            {
                subjectId = subject.SubjectId,
                subjectName = subject.SubjectName,
                subjectCode = subject.SubjectCode,
                levelId = subject.LevelId,
                levelName = subject.Level?.LevelName,
                sectionId = subject.SectionId,
                sectionName = subject.Section?.SectionName,
                sectionLevelName = subject.Section?.Level?.LevelName,
                teacherId = subject.TeacherId,
                teacherName = subject.Teacher?.FullName,
                schedule = subject.Schedule
            };

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Subject created successfully",
                Data = data
            };
        }

        // PUT: api/SubjectsApi/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> PutSubject(int id, [FromBody] SubjectDto subjectDto)
        {
            if (id != subjectDto.SubjectId)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Subject ID mismatch",
                    Data = null
                });
            }

            var existingSubject = await _context.Subject.AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubjectId == id);

            if (existingSubject == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Subject not found",
                    Data = null
                });
            }

            // Check for duplicate subject code (excluding current subject)
            if (await _context.Subject.AnyAsync(s => s.SubjectCode == subjectDto.SubjectCode && s.SubjectId != id))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Subject code already exists",
                    Data = null
                });
            }

            var subject = new Subject
            {
                SubjectId = subjectDto.SubjectId,
                SubjectName = subjectDto.SubjectName,
                SubjectCode = subjectDto.SubjectCode,
                LevelId = subjectDto.LevelId,
                SectionId = subjectDto.SectionId,
                TeacherId = subjectDto.TeacherId,
                Schedule = subjectDto.Schedule
            };

            _context.Entry(subject).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SubjectExists(id))
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Subject not found",
                        Data = null
                    });
                }
                else
                {
                    throw;
                }
            }

            // Reload to get navigation properties
            await _context.Entry(subject).Reference(s => s.Level).LoadAsync();
            await _context.Entry(subject).Reference(s => s.Section).LoadAsync();
            await _context.Entry(subject).Reference(s => s.Teacher).LoadAsync();
            if (subject.Section != null)
            {
                await _context.Entry(subject.Section).Reference(sec => sec.Level).LoadAsync();
            }

            var data = new
            {
                subjectId = subject.SubjectId,
                subjectName = subject.SubjectName,
                subjectCode = subject.SubjectCode,
                levelId = subject.LevelId,
                levelName = subject.Level?.LevelName,
                sectionId = subject.SectionId,
                sectionName = subject.Section?.SectionName,
                sectionLevelName = subject.Section?.Level?.LevelName,
                teacherId = subject.TeacherId,
                teacherName = subject.Teacher?.FullName,
                schedule = subject.Schedule
            };

            return new ApiResponse<object>
            {
                Success = true,
                Message = "Subject updated successfully",
                Data = data
            };
        }

        // DELETE: api/SubjectsApi/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteSubject(int id)
        {
            var subject = await _context.Subject.FindAsync(id);
            if (subject == null)
            {
                return NotFound(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Subject not found",
                    Data = null
                });
            }

            try
            {
                _context.Subject.Remove(subject);
                await _context.SaveChangesAsync();

                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Subject deleted successfully",
                    Data = "Deleted"
                };
            }
            catch (DbUpdateException)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Cannot delete subject. Subject may have enrollments or evaluations.",
                    Data = null
                });
            }
        }

        private bool SubjectExists(int id)
        {
            return _context.Subject.Any(e => e.SubjectId == id);
        }
    }

    // DTO for Subject
    public class SubjectDto
    {
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public string? SubjectCode { get; set; }
        public int LevelId { get; set; }
        public int SectionId { get; set; }
        public int TeacherId { get; set; }
        public string? Schedule { get; set; }
    }
}