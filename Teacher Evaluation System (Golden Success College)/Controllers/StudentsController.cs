using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Helper;
using Teacher_Evaluation_System__Golden_Success_College_.Models;
using Teacher_Evaluation_System__Golden_Success_College_.Services;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    [Authorize(Roles = "Admin,Super Admin")]
    public class StudentsController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly EmailService _emailService;

        public StudentsController(
            Teacher_Evaluation_System__Golden_Success_College_Context context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            try
            {
                var students = await _context.Student
                    .Include(s => s.Level)
                    .Include(s => s.Role)
                    .Include(s => s.Section)
                    .OrderBy(s => s.FullName)
                    .ToListAsync();

                // Load levels for dropdown
                ViewData["LevelId"] = new SelectList(
                    await _context.Level.OrderBy(l => l.LevelName).ToListAsync(),
                    "LevelId",
                    "LevelName"
                );

                // Load sections with their Level for grouping
                var sections = await _context.Section
                    .Include(s => s.Level)
                    .OrderBy(s => s.Level.LevelName)
                    .ThenBy(s => s.SectionName)
                    .ToListAsync();

                // Convert to SelectListItem with Group (optgroup)
                var sectionSelectList = sections.Select(s => new SelectListItem
                {
                    Value = s.SectionId.ToString(),
                    Text = s.SectionName,
                    Group = new SelectListGroup { Name = s.Level?.LevelName ?? "Unknown" }
                }).ToList();

                ViewData["SectionId"] = sectionSelectList;

                return View(students);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading students: {ex.Message}";
                return View(new List<Student>());
            }
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Student ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var student = await _context.Student
                    .Include(s => s.Level)
                    .Include(s => s.Role)
                    .Include(s => s.Section)
                    .FirstOrDefaultAsync(m => m.StudentId == id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found";
                    return RedirectToAction(nameof(Index));
                }

                return View(student);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading student details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Students/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                await LoadDropdownsAsync();
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading create form: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,LevelId,SectionId,CollegeYearLevel")] Student student)
        {
            // Remove navigation properties from ModelState validation
            ModelState.Remove("Level");
            ModelState.Remove("Role");
            ModelState.Remove("Section");
            ModelState.Remove("Password");
            ModelState.Remove("Enrollments");

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(student.FullName))
                    {
                        ModelState.AddModelError("FullName", "Full Name is required");
                        await LoadDropdownsAsync(student);
                        return View(student);
                    }

                    if (string.IsNullOrWhiteSpace(student.Email))
                    {
                        ModelState.AddModelError("Email", "Email is required");
                        await LoadDropdownsAsync(student);
                        return View(student);
                    }

                    // Check for duplicate email
                    var existingStudent = await _context.Student
                        .FirstOrDefaultAsync(s => s.Email.ToLower() == student.Email.ToLower());

                    if (existingStudent != null)
                    {
                        ModelState.AddModelError("Email", "This email is already registered");
                        await LoadDropdownsAsync(student);
                        return View(student);
                    }

                    // Validate Level
                    var level = await _context.Level.FindAsync(student.LevelId);
                    if (level == null)
                    {
                        ModelState.AddModelError("LevelId", "Invalid level selected");
                        await LoadDropdownsAsync(student);
                        return View(student);
                    }

                    // Validate Section if provided
                    if (student.SectionId.HasValue)
                    {
                        var section = await _context.Section.FindAsync(student.SectionId.Value);
                        if (section == null)
                        {
                            ModelState.AddModelError("SectionId", "Invalid section selected");
                            await LoadDropdownsAsync(student);
                            return View(student);
                        }
                    }

                    // 1. Generate Temporary Password and Token
                    string temporaryPassword = PasswordHelper.GenerateRandomPassword(10);
                    string confirmationToken = Guid.NewGuid().ToString();
                    DateTime tokenExpiration = DateTime.UtcNow.AddHours(24);

                    // 2. Set default role and hash password
                    student.RoleId = 1; // Student role
                    student.Password = PasswordHelper.HashPassword(temporaryPassword);

                    // 3. Set security/onboarding flags
                    student.EmailConfirmed = false;
                    student.EmailConfirmationToken = confirmationToken;
                    student.TokenExpirationDate = tokenExpiration;
                    student.IsTemporaryPassword = true;

                    // 4. Auto-set CollegeYearLevel based on Level
                    if (level.LevelName.ToLower().Contains("college"))
                    {
                        if (!student.CollegeYearLevel.HasValue ||
                            student.CollegeYearLevel < 1 ||
                            student.CollegeYearLevel > 4)
                        {
                            student.CollegeYearLevel = 1;
                        }
                    }
                    else
                    {
                        student.CollegeYearLevel = 0;
                    }

                    // 5. Add student to database
                    _context.Add(student);
                    await _context.SaveChangesAsync();

                    // 6. AUTO-ENROLL: Enroll student into all subjects matching their Level & Section
                    int enrolledCount = await AutoEnrollStudentAsync(student);

                    // 7. Prepare and send activation email
                    try
                    {
                        string subject = "Account Activation and Temporary Password";
                        string activationLink = Url.Action(
                            "ConfirmEmail",
                            "Auth",
                            new { token = confirmationToken, email = student.Email },
                            Request.Scheme
                        );

                        string emailBody = GetActivationEmailBody(
                            student.FullName,
                            temporaryPassword,
                            activationLink
                        );

                        // Uncomment when email service is ready
                        //await _emailService.SendEmailAsync(student.Email, subject, emailBody);

                        TempData["SuccessMessage"] = $"Student '{student.FullName}' created successfully! " +
                            $"Temporary password: {temporaryPassword}. " +
                            $"{enrolledCount} subject(s) auto-enrolled based on Level & Section.";
                    }
                    catch (Exception emailEx)
                    {
                        TempData["WarningMessage"] = $"Student created but email notification failed: {emailEx.Message}. " +
                            $"Temporary password: {temporaryPassword}. " +
                            $"{enrolledCount} subject(s) auto-enrolled.";
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException dbEx)
                {
                    ModelState.AddModelError("", $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    await LoadDropdownsAsync(student);
                    return View(student);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating student: {ex.Message}");
                    await LoadDropdownsAsync(student);
                    return View(student);
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", errors);
            await LoadDropdownsAsync(student);
            return View(student);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Student ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var student = await _context.Student.FindAsync(id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found";
                    return RedirectToAction(nameof(Index));
                }

                await LoadDropdownsAsync(student);
                return View(student);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading student: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("StudentId,FullName,Email,Password,LevelId,SectionId,CollegeYearLevel")] Student student)
        {
            if (id != student.StudentId)
            {
                TempData["ErrorMessage"] = "Student ID mismatch";
                return RedirectToAction(nameof(Index));
            }

            // Remove navigation properties from ModelState validation
            ModelState.Remove("Level");
            ModelState.Remove("Role");
            ModelState.Remove("Section");
            ModelState.Remove("Enrollments");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingStudent = await _context.Student
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.StudentId == id);

                    if (existingStudent == null)
                    {
                        TempData["ErrorMessage"] = "Student not found";
                        return RedirectToAction(nameof(Index));
                    }

                    if (string.IsNullOrWhiteSpace(student.FullName))
                    {
                        ModelState.AddModelError("FullName", "Full Name is required");
                        await LoadDropdownsAsync(student);
                        return View(student);
                    }

                    if (string.IsNullOrWhiteSpace(student.Email))
                    {
                        ModelState.AddModelError("Email", "Email is required");
                        await LoadDropdownsAsync(student);
                        return View(student);
                    }

                    var duplicateEmail = await _context.Student
                        .AnyAsync(s => s.Email.ToLower() == student.Email.ToLower() && s.StudentId != id);

                    if (duplicateEmail)
                    {
                        ModelState.AddModelError("Email", "This email is already registered");
                        await LoadDropdownsAsync(student);
                        return View(student);
                    }

                    var level = await _context.Level.FindAsync(student.LevelId);
                    if (level == null)
                    {
                        ModelState.AddModelError("LevelId", "Invalid level selected");
                        await LoadDropdownsAsync(student);
                        return View(student);
                    }

                    if (student.SectionId.HasValue)
                    {
                        var section = await _context.Section.FindAsync(student.SectionId.Value);
                        if (section == null)
                        {
                            ModelState.AddModelError("SectionId", "Invalid section selected");
                            await LoadDropdownsAsync(student);
                            return View(student);
                        }
                    }

                    // Preserve existing security fields
                    student.EmailConfirmed = existingStudent.EmailConfirmed;
                    student.EmailConfirmationToken = existingStudent.EmailConfirmationToken;
                    student.TokenExpirationDate = existingStudent.TokenExpirationDate;
                    student.IsTemporaryPassword = existingStudent.IsTemporaryPassword;
                    student.RoleId = 1;

                    if (level.LevelName.ToLower().Contains("college"))
                    {
                        if (!student.CollegeYearLevel.HasValue ||
                            student.CollegeYearLevel < 1 ||
                            student.CollegeYearLevel > 4)
                        {
                            student.CollegeYearLevel = 1;
                        }
                    }
                    else
                    {
                        student.CollegeYearLevel = 0;
                    }

                    if (!string.IsNullOrWhiteSpace(student.Password))
                    {
                        student.Password = PasswordHelper.HashPassword(student.Password);
                        student.IsTemporaryPassword = false;
                    }
                    else
                    {
                        student.Password = existingStudent.Password;
                    }

                    // Update student
                    _context.Update(student);
                    await _context.SaveChangesAsync();

                    // AUTO-ENROLL: Enroll into any new subjects if Level/Section changed
                    int enrolledCount = await AutoEnrollStudentAsync(student);

                    TempData["SuccessMessage"] = $"Student '{student.FullName}' updated successfully. " +
                        (enrolledCount > 0 ? $"{enrolledCount} new subject(s) auto-enrolled." : "No new subjects to enroll.");

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId))
                    {
                        TempData["ErrorMessage"] = "Student not found";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    ModelState.AddModelError("", $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    await LoadDropdownsAsync(student);
                    return View(student);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating student: {ex.Message}");
                    await LoadDropdownsAsync(student);
                    return View(student);
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", errors);
            await LoadDropdownsAsync(student);
            return View(student);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Student ID is required";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var student = await _context.Student
                    .Include(s => s.Level)
                    .Include(s => s.Role)
                    .Include(s => s.Section)
                    .FirstOrDefaultAsync(m => m.StudentId == id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found";
                    return RedirectToAction(nameof(Index));
                }

                return View(student);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading student: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var student = await _context.Student.FindAsync(id);

                if (student == null)
                {
                    TempData["ErrorMessage"] = "Student not found";
                    return RedirectToAction(nameof(Index));
                }

                string studentName = student.FullName;

                _context.Student.Remove(student);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Student '{studentName}' deleted successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                TempData["ErrorMessage"] = "Cannot delete student. This student may have enrollments or evaluations. " +
                    "Please remove related records first or contact system administrator.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting student: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ── AUTO-ENROLL HELPER ─────────────────────────────────────────────────
        // Automatically enrolls a student into all subjects that match
        // their Level and Section. Returns number of new enrollments created.
        private async Task<int> AutoEnrollStudentAsync(Student student)
        {
            if (!student.SectionId.HasValue || student.LevelId <= 0)
                return 0;

            // Get all subjects matching student's Level and Section with a teacher assigned
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
            {
                await _context.SaveChangesAsync();
            }

            return count;
        }

        // ── HELPERS ────────────────────────────────────────────────────────────

        private bool StudentExists(int id)
        {
            return _context.Student.Any(e => e.StudentId == id);
        }

        private async Task LoadDropdownsAsync(Student student = null)
        {
            try
            {
                var levels = await _context.Level
                    .OrderBy(l => l.LevelName)
                    .ToListAsync();

                ViewData["LevelId"] = new SelectList(
                    levels,
                    "LevelId",
                    "LevelName",
                    student?.LevelId
                );

                var sections = await _context.Section
                    .Include(s => s.Level)
                    .OrderBy(s => s.Level.LevelName)
                    .ThenBy(s => s.SectionName)
                    .ToListAsync();

                var sectionSelectList = sections.Select(s => new SelectListItem
                {
                    Value = s.SectionId.ToString(),
                    Text = s.SectionName,
                    Group = new SelectListGroup { Name = s.Level?.LevelName ?? "Unknown" },
                    Selected = student != null && s.SectionId == student.SectionId
                }).ToList();

                ViewData["SectionId"] = sectionSelectList;

                var roles = await _context.Role
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                ViewData["RoleId"] = new SelectList(
                    roles,
                    "RoleId",
                    "Name",
                    student?.RoleId ?? 1
                );
            }
            catch (Exception ex)
            {
                ViewData["LevelId"] = new SelectList(new List<object>(), "LevelId", "LevelName");
                ViewData["SectionId"] = new List<SelectListItem>();
                ViewData["RoleId"] = new SelectList(new List<object>(), "RoleId", "Name");
                throw new Exception($"Error loading dropdown data: {ex.Message}", ex);
            }
        }

        private string GetActivationEmailBody(string fullName, string temporaryPassword, string activationLink)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'><title>Account Activation</title><style>
    body {{ font-family: Arial, sans-serif; background-color: #f8f9fa; margin: 0; padding: 0; }}
    .container {{ width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; box-shadow: 0 0 15px rgba(0,0,0,0.05); }}
    .header {{ background-color: #007bff; color: #ffffff; padding: 30px 20px; border-top-left-radius: 8px; border-top-right-radius: 8px; text-align: center; }}
    .content {{ padding: 30px; line-height: 1.6; color: #343a40; }}
    .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #6c757d; border-top: 1px solid #dee2e6; }}
    .btn {{ display: inline-block; padding: 12px 25px; margin: 25px 0; font-size: 16px; color: #ffffff; background-color: #007bff; text-decoration: none; border-radius: 5px; font-weight: bold; border: 1px solid #007bff; }}
    .temp-password {{ display: block; background-color: #e9ecef; padding: 15px; border-radius: 4px; font-size: 18px; font-weight: bold; text-align: center; margin: 20px 0; color: #495057; border: 1px solid #ced4da; }}
    .warning {{ font-size: 14px; color: #dc3545; margin-top: 25px; border-left: 4px solid #dc3545; padding-left: 10px; background-color: #fff3f3; padding: 10px; }}
    .info-box {{ background-color: #f8f9fa; border: 1px solid #dee2e6; padding: 15px; border-radius: 4px; margin: 15px 0; }}
</style></head><body>
    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color: #f8f9fa; padding: 30px 0;'>
        <tr>
            <td align='center'>
                <table class='container' cellpadding='0' cellspacing='0' border='0'>
                    <tr><td class='header'><h2>Golden Success College</h2><p>Student Account Activation</p></td></tr>
                    <tr><td class='content'>
                        <p style='color: #343a40;'>Dear <strong>{fullName}</strong>,</p>
                        <p>Welcome to Golden Success College! Your student account has been successfully created.</p>
                        <p>To get started, please use the following temporary password to log in:</p>
                        <table width='100%' cellpadding='0' cellspacing='0' border='0'><tr><td style='text-align: center;'>
                            <div class='temp-password'>{temporaryPassword}</div>
                        </td></tr></table>
                        <div class='info-box'>
                            <strong>Next Steps:</strong>
                            <ol style='margin: 10px 0; padding-left: 20px;'>
                                <li>Click the activation button below to confirm your email address</li>
                                <li>Log in using your email and the temporary password above</li>
                                <li>You will be prompted to change your password on first login</li>
                            </ol>
                        </div>
                        <p style='text-align: center;'><a href='{activationLink}' class='btn'>Activate Account</a></p>
                        <div class='warning'>
                            <strong>⚠️ IMPORTANT SECURITY NOTE:</strong><br/>
                            This is a temporary password. You <strong>must</strong> change it upon your first login for security purposes.
                            This activation link will expire in 24 hours.
                        </div>
                        <p style='margin-top: 20px; font-size: 14px; color: #6c757d;'>If you did not request this account, please ignore this email.</p>
                    </td></tr>
                    <tr><td class='footer'><p>&copy; {DateTime.Now.Year} Golden Success College. All rights reserved.</p></td></tr>
                </table>
            </td>
        </tr>
    </table>
</body></html>";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            try
            {
                var student = await _context.Student.FindAsync(id);

                if (student == null)
                {
                    return Json(new { success = false, message = "Student not found" });
                }

                string newPassword = PasswordHelper.GenerateRandomPassword(10);
                string confirmationToken = Guid.NewGuid().ToString();

                student.Password = PasswordHelper.HashPassword(newPassword);
                student.IsTemporaryPassword = true;
                student.EmailConfirmationToken = confirmationToken;
                student.TokenExpirationDate = DateTime.UtcNow.AddHours(24);

                _context.Update(student);
                await _context.SaveChangesAsync();

                try
                {
                    string subject = "Password Reset - Golden Success College";
                    string activationLink = Url.Action(
                        "ConfirmEmail",
                        "Auth",
                        new { token = confirmationToken, email = student.Email },
                        Request.Scheme
                    );

                    string emailBody = GetPasswordResetEmailBody(
                        student.FullName,
                        newPassword,
                        activationLink
                    );

                    await _emailService.SendEmailAsync(student.Email, subject, emailBody);
                }
                catch (Exception emailEx)
                {
                    return Json(new
                    {
                        success = true,
                        message = $"Password reset but email notification failed. New password: {newPassword}",
                        password = newPassword
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Password reset successfully",
                    password = newPassword
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error resetting password: {ex.Message}" });
            }
        }

        private string GetPasswordResetEmailBody(string fullName, string newPassword, string activationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Password Reset</title>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f8f9fa; margin: 0; padding: 0; }}
        .container {{ width: 100%; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; }}
        .header {{ background-color: #dc3545; color: #ffffff; padding: 30px 20px; border-top-left-radius: 8px; border-top-right-radius: 8px; text-align: center; }}
        .content {{ padding: 30px; line-height: 1.6; color: #343a40; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #6c757d; border-top: 1px solid #dee2e6; }}
        .btn {{ display: inline-block; padding: 12px 25px; margin: 25px 0; font-size: 16px; color: #ffffff; background-color: #dc3545; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .temp-password {{ display: block; background-color: #fff3cd; padding: 15px; border-radius: 4px; font-size: 18px; font-weight: bold; text-align: center; margin: 20px 0; color: #856404; border: 1px solid #ffc107; }}
        .warning {{ background-color: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 15px; border-radius: 4px; margin-top: 20px; }}
    </style>
</head>
<body>
    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background-color: #f8f9fa; padding: 30px 0;'>
        <tr><td align='center'>
            <table class='container' cellpadding='0' cellspacing='0' border='0'>
                <tr><td class='header'><h2>Password Reset</h2><p>Golden Success College</p></td></tr>
                <tr><td class='content'>
                    <p>Dear <strong>{fullName}</strong>,</p>
                    <p>Your password has been reset by an administrator.</p>
                    <div class='temp-password'>{newPassword}</div>
                    <p style='text-align:center;'><a href='{activationLink}' class='btn'>Log In Now</a></p>
                    <div class='warning'>
                        <strong>⚠️ IMPORTANT:</strong> Change this temporary password immediately upon your next login.
                        This link will expire in 24 hours.
                    </div>
                </td></tr>
                <tr><td class='footer'><p>&copy; {DateTime.Now.Year} Golden Success College. All rights reserved.</p></td></tr>
            </table>
        </td></tr>
    </table>
</body>
</html>";
        }
    }
}