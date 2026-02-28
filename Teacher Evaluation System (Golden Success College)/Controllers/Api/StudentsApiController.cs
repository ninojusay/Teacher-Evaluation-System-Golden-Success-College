using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.Helper;
using Teacher_Evaluation_System__Golden_Success_College_.Services;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsApiController : ControllerBase
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly EmailService _emailService;

        public StudentsApiController(
            Teacher_Evaluation_System__Golden_Success_College_Context context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: api/StudentsApi
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetStudents()
        {
            try
            {
                var students = await _context.Student
                    .Include(s => s.Level)
                    .Include(s => s.Role)
                    .Include(s => s.Section)
                    .ToListAsync();

                var data = students.Select(s => new
                {
                    studentId = s.StudentId,
                    fullName = s.FullName,
                    email = s.Email,
                    emailConfirmed = s.EmailConfirmed,
                    isTemporaryPassword = s.IsTemporaryPassword,
                    isActive = s.IsActive,
                    levelId = s.LevelId,
                    levelName = s.Level?.LevelName,
                    sectionId = s.SectionId,
                    sectionName = s.Section?.SectionName,
                    collegeYearLevel = s.CollegeYearLevel,
                    roleId = s.RoleId,
                    roleName = s.Role?.Name
                });

                return Ok(new ApiResponse<IEnumerable<object>>
                {
                    Success = true,
                    Message = "Students loaded successfully",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<IEnumerable<object>>
                {
                    Success = false,
                    Message = $"Error loading students: {ex.Message}",
                    Data = null
                });
            }
        }

        // GET: api/StudentsApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> GetStudent(int id)
        {
            try
            {
                var student = await _context.Student
                    .Include(s => s.Level)
                    .Include(s => s.Role)
                    .Include(s => s.Section)
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Student not found",
                        Data = null
                    });
                }

                var data = new
                {
                    studentId = student.StudentId,
                    fullName = student.FullName,
                    email = student.Email,
                    emailConfirmed = student.EmailConfirmed,
                    isTemporaryPassword = student.IsTemporaryPassword,
                    isActive = student.IsActive,
                    levelId = student.LevelId,
                    levelName = student.Level?.LevelName,
                    sectionId = student.SectionId,
                    sectionName = student.Section?.SectionName,
                    collegeYearLevel = student.CollegeYearLevel,
                    roleId = student.RoleId,
                    roleName = student.Role?.Name
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Student loaded successfully",
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error loading student: {ex.Message}",
                    Data = null
                });
            }
        }

        // POST: api/StudentsApi
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> PostStudent([FromBody] StudentDto studentDto)
        {
            if (string.IsNullOrWhiteSpace(studentDto.FullName) ||
                string.IsNullOrWhiteSpace(studentDto.Email) ||
                studentDto.LevelId <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Missing required fields.",
                    Data = null
                });
            }

            if (await _context.Student.AnyAsync(s => s.Email.ToLower() == studentDto.Email.ToLower()))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Email already exists.",
                    Data = null
                });
            }

            try
            {
                string temporaryPassword = PasswordHelper.GenerateRandomPassword(10);
                string confirmationToken = Guid.NewGuid().ToString();

                var student = new Student
                {
                    FullName = studentDto.FullName,
                    Email = studentDto.Email,
                    Password = PasswordHelper.HashPassword(temporaryPassword),
                    LevelId = studentDto.LevelId,
                    SectionId = studentDto.SectionId,
                    CollegeYearLevel = studentDto.CollegeYearLevel ?? 0,
                    RoleId = 1,
                    EmailConfirmed = false,
                    EmailConfirmationToken = confirmationToken,
                    TokenExpirationDate = DateTime.UtcNow.AddHours(24),
                    IsTemporaryPassword = true,
                    IsActive = true
                };

                var level = await _context.Level.FindAsync(student.LevelId);
                if (level != null && level.LevelName.ToLower().Contains("college"))
                {
                    student.CollegeYearLevel = (student.CollegeYearLevel >= 1 && student.CollegeYearLevel <= 4)
                        ? student.CollegeYearLevel : 1;
                }
                else
                {
                    student.CollegeYearLevel = 0;
                }

                _context.Student.Add(student);
                await _context.SaveChangesAsync();

                // ── AUTO-ENROLL ──────────────────────────────────────────────
                // After student is saved, find all subjects that match
                // their Level & Section and enroll them automatically
                int enrolledCount = await AutoEnrollStudentAsync(student);
                // ────────────────────────────────────────────────────────────

                var request = HttpContext.Request;
                string activationLink = $"{request.Scheme}://{request.Host.ToUriComponent()}/Auth/ConfirmEmail?token={confirmationToken}&email={student.Email}";
                string emailBody = GetActivationEmailBody(student.FullName, temporaryPassword, activationLink);
                string responseMessage;

                try
                {
                    await _emailService.SendEmailAsync(
                        student.Email,
                        "Golden Success College: Account Activation & Temporary Password",
                        emailBody
                    );
                    responseMessage = $"Student created successfully. Activation email sent to {student.Email}. {enrolledCount} subject(s) auto-enrolled.";
                }
                catch (Exception emailEx)
                {
                    responseMessage = $"Student created successfully, but email notification failed. Temporary password: {temporaryPassword}. {enrolledCount} subject(s) auto-enrolled.";
                }

                await _context.Entry(student).Reference(s => s.Level).LoadAsync();
                await _context.Entry(student).Reference(s => s.Section).LoadAsync();
                await _context.Entry(student).Reference(s => s.Role).LoadAsync();

                var data = new
                {
                    studentId = student.StudentId,
                    fullName = student.FullName,
                    email = student.Email,
                    emailConfirmed = student.EmailConfirmed,
                    isTemporaryPassword = student.IsTemporaryPassword,
                    isActive = student.IsActive,
                    levelId = student.LevelId,
                    levelName = student.Level?.LevelName,
                    sectionId = student.SectionId,
                    sectionName = student.Section?.SectionName,
                    collegeYearLevel = student.CollegeYearLevel,
                    roleId = student.RoleId,
                    roleName = student.Role?.Name
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = responseMessage,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error creating student: {ex.Message}",
                    Data = null
                });
            }
        }

        // PUT: api/StudentsApi/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> PutStudent(int id, [FromBody] StudentDto studentDto)
        {
            if (id != studentDto.StudentId)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Student ID mismatch.",
                    Data = null
                });
            }

            if (string.IsNullOrWhiteSpace(studentDto.FullName) || string.IsNullOrWhiteSpace(studentDto.Email))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Missing required fields (Name/Email).",
                    Data = null
                });
            }

            var existingStudent = await _context.Student.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == id);
            if (existingStudent == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Student not found.",
                    Data = null
                });
            }

            if (await _context.Student.AnyAsync(s => s.Email.ToLower() == studentDto.Email.ToLower() && s.StudentId != id))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Email already exists.",
                    Data = null
                });
            }

            try
            {
                var student = new Student
                {
                    StudentId = studentDto.StudentId,
                    FullName = studentDto.FullName,
                    Email = studentDto.Email,
                    LevelId = studentDto.LevelId,
                    SectionId = studentDto.SectionId,
                    CollegeYearLevel = studentDto.CollegeYearLevel ?? 0,
                    RoleId = 1,
                    EmailConfirmed = existingStudent.EmailConfirmed,
                    EmailConfirmationToken = existingStudent.EmailConfirmationToken,
                    TokenExpirationDate = existingStudent.TokenExpirationDate,
                    IsTemporaryPassword = existingStudent.IsTemporaryPassword,
                    IsActive = existingStudent.IsActive
                };

                var level = await _context.Level.FindAsync(student.LevelId);
                if (level != null && level.LevelName.ToLower().Contains("college"))
                {
                    student.CollegeYearLevel = (student.CollegeYearLevel >= 1 && student.CollegeYearLevel <= 4)
                        ? student.CollegeYearLevel : 1;
                }
                else
                {
                    student.CollegeYearLevel = 0;
                }

                if (!string.IsNullOrWhiteSpace(studentDto.Password))
                {
                    student.Password = PasswordHelper.HashPassword(studentDto.Password);
                    student.IsTemporaryPassword = false;
                }
                else
                {
                    student.Password = existingStudent.Password;
                }

                _context.Entry(student).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // ── AUTO-ENROLL ──────────────────────────────────────────────
                // Enroll into any new subjects if Level/Section changed
                int enrolledCount = await AutoEnrollStudentAsync(student);
                // ────────────────────────────────────────────────────────────

                await _context.Entry(student).Reference(s => s.Level).LoadAsync();
                await _context.Entry(student).Reference(s => s.Section).LoadAsync();
                await _context.Entry(student).Reference(s => s.Role).LoadAsync();

                var data = new
                {
                    studentId = student.StudentId,
                    fullName = student.FullName,
                    email = student.Email,
                    emailConfirmed = student.EmailConfirmed,
                    isTemporaryPassword = student.IsTemporaryPassword,
                    isActive = student.IsActive,
                    levelId = student.LevelId,
                    levelName = student.Level?.LevelName,
                    sectionId = student.SectionId,
                    sectionName = student.Section?.SectionName,
                    collegeYearLevel = student.CollegeYearLevel,
                    roleId = student.RoleId,
                    roleName = student.Role?.Name
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Student updated successfully. {(enrolledCount > 0 ? $"{enrolledCount} new subject(s) auto-enrolled." : "")}",
                    Data = data
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Student not found",
                        Data = null
                    });
                }
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error updating student: {ex.Message}",
                    Data = null
                });
            }
        }

        // DELETE: api/StudentsApi/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Student.FindAsync(id);

                if (student == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Student not found",
                        Data = null
                    });
                }

                _context.Student.Remove(student);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = $"Student '{student.FullName}' deleted successfully",
                    Data = "Deleted"
                });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Cannot delete student. This student may have related records (enrollments, evaluations). Please remove related records first.",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Error deleting student: {ex.Message}",
                    Data = null
                });
            }
        }

        // POST: api/StudentsApi/ToggleActive/5
        [HttpPost("ToggleActive/{id}")]
        public async Task<ActionResult> ToggleActiveStudent(int id)
        {
            var student = await _context.Student.FindAsync(id);
            if (student == null)
                return NotFound(new { success = false, message = "Student not found" });

            student.IsActive = !student.IsActive;
            _context.Student.Update(student);
            await _context.SaveChangesAsync();

            string action = student.IsActive ? "reactivated" : "deactivated";
            return Ok(new { success = true, message = $"Student {action} successfully", isActive = student.IsActive });
        }

        // ── AUTO-ENROLL HELPER ─────────────────────────────────────────────────
        // Finds all subjects that match the student's Level & Section
        // and automatically enrolls them. Skips already enrolled subjects.
        // Returns the number of new enrollments created.
        private async Task<int> AutoEnrollStudentAsync(Student student)
        {
            if (!student.SectionId.HasValue || student.LevelId <= 0)
                return 0;

            // Get all subjects matching student's Level + Section with a teacher assigned
            var subjects = await _context.Subject
                .Where(s => s.LevelId == student.LevelId
                         && s.SectionId == student.SectionId
                         && s.TeacherId != null
                         && s.TeacherId > 0)
                .ToListAsync();

            if (!subjects.Any())
                return 0;

            // Get already enrolled subject IDs to avoid duplicates
            var alreadyEnrolledSubjectIds = await _context.Enrollment
                .Where(e => e.StudentId == student.StudentId)
                .Select(e => e.SubjectId)
                .ToListAsync();

            int count = 0;
            foreach (var subject in subjects)
            {
                if (!alreadyEnrolledSubjectIds.Contains(subject.SubjectId))
                {
                    _context.Enrollment.Add(new Enrollment
                    {
                        StudentId = student.StudentId,
                        SubjectId = subject.SubjectId,
                        TeacherId = subject.TeacherId
                    });
                    count++;
                }
            }

            if (count > 0)
                await _context.SaveChangesAsync();

            return count;
        }
        // ──────────────────────────────────────────────────────────────────────

        private bool StudentExists(int id)
        {
            return _context.Student.Any(e => e.StudentId == id);
        }

        private string GetActivationEmailBody(string fullName, string temporaryPassword, string activationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Account Activation</title>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .container {{ width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 0 15px rgba(0,0,0,0.05); }}
        .header {{ background-color: #007bff; color: #ffffff; padding: 30px 20px; border-top-left-radius: 8px; border-top-right-radius: 8px; text-align: center; }}
        .content {{ padding: 30px; line-height: 1.6; color: #343a40; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #6c757d; border-top: 1px solid #dee2e6; }}
        .btn {{ display: inline-block; padding: 12px 25px; margin: 25px 0; font-size: 16px; color: #ffffff; background-color: #007bff; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .temp-password {{ display: block; background-color: #e9ecef; padding: 15px; border-radius: 4px; font-size: 18px; font-weight: bold; text-align: center; margin: 20px 0; color: #495057; }}
    </style>
</head>
<body>
    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color: #f8f9fa; padding: 30px 0;'>
        <tr>
            <td align='center'>
                <table class='container' cellpadding='0' cellspacing='0' border='0'>
                    <tr><td class='header'><h2>Golden Success College</h2><p>Student Account Activation</p></td></tr>
                    <tr><td class='content'>
                        <p>Dear <strong>{fullName}</strong>,</p>
                        <p>Welcome to Golden Success College! Your student account has been successfully created.</p>
                        <p>Your temporary password is:</p>
                        <div class='temp-password'>{temporaryPassword}</div>
                        <p style='text-align: center;'><a href='{activationLink}' class='btn'>Activate Account</a></p>
                        <p style='color: #dc3545;'><strong>IMPORTANT:</strong> You must change this password upon your first login.</p>
                    </td></tr>
                    <tr><td class='footer'><p>&copy; {DateTime.Now.Year} Golden Success College. All rights reserved.</p></td></tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}