using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Teacher_Evaluation_System__Golden_Success_College_.Data;
using Teacher_Evaluation_System__Golden_Success_College_.Helper;
using Teacher_Evaluation_System__Golden_Success_College_.Services;
using Teacher_Evaluation_System__Golden_Success_College_.ViewModels;

namespace Teacher_Evaluation_System__Golden_Success_College_.Controllers
{
    public class AuthController : Controller
    {
        private readonly Teacher_Evaluation_System__Golden_Success_College_Context _context;
        private readonly EmailService _emailService;
        private readonly IActivityLogService _activityLogService;

        public AuthController(
            Teacher_Evaluation_System__Golden_Success_College_Context context,
            EmailService emailService,
            IActivityLogService activityLogService)
        {
            _context = context;
            _emailService = emailService;
            _activityLogService = activityLogService;
        }

        // GET: Login Page
        public IActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Email and Password are required.";
                return View();
            }

            // Try User Login
            var user = await _context.User
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user != null && !string.IsNullOrEmpty(user.Password) && PasswordHelper.VerifyPassword(password, user.Password))
            {
                // FIX: Check if user account is active before signing in
                if (!user.IsActive)
                {
                    TempData["ErrorMessage"] = "This account has been deactivated. Please contact an administrator.";
                    return View();
                }

                // Sign in user first
                await SignInUser(user.UserId, user.FullName!, user.Role!.Name);

                // Log the login activity AFTER sign in (so User claims are available)
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                // Create a ClaimsPrincipal with the user info for logging
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName ?? "Unknown"),
                    new Claim(ClaimTypes.Role, user.Role?.Name ?? "Unknown")
                };
                var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

                await _activityLogService.LogLoginAsync(userPrincipal, ipAddress, userAgent);

                TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";
                return RedirectToAction("Index", "Dashboard");
            }

            // Try Student Login
            var student = await _context.Student
                .Include(s => s.Role)
                .FirstOrDefaultAsync(s => s.Email == email);

            if (student != null && PasswordHelper.VerifyPassword(password, student.Password))
            {
                // Sign in student first
                await SignInUser(student.StudentId, student.FullName!, student.Role!.Name);

                // Log the login activity AFTER sign in
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                // Create a ClaimsPrincipal with the student info for logging
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, student.StudentId.ToString()),
                    new Claim(ClaimTypes.Name, student.FullName ?? "Unknown"),
                    new Claim(ClaimTypes.Role, student.Role?.Name ?? "Student")
                };
                var studentPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

                await _activityLogService.LogLoginAsync(studentPrincipal, ipAddress, userAgent);

                TempData["SuccessMessage"] = $"Welcome back, {student.FullName}!";
                return RedirectToAction("Index", "Dashboard");
            }

            // Failed Login
            TempData["ErrorMessage"] = "Invalid Email or Password.";
            return View();
        }

        // Sign In Method (CREATES COOKIE)
        private async Task SignInUser(int id, string fullName, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Role, role),
                new Claim("RoleName", role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Create Cookie
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Optional: Session
            HttpContext.Session.SetInt32("UserId", id);
            HttpContext.Session.SetString("FullName", fullName);
            HttpContext.Session.SetString("Role", role);
        }

        // Logout
        public async Task<IActionResult> Logout()
        {
            // Log the logout activity BEFORE signing out (while User claims are still available)
            var ipAddress = GetClientIpAddress();
            await _activityLogService.LogLogoutAsync(User, ipAddress);

            await HttpContext.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // Access Denied Page
        public IActionResult AccessDenied()
        {
            return View();
        }


        // GET: /Auth/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.User.FirstOrDefaultAsync(u => u.Email == model.Email);
            var student = await _context.Student.FirstOrDefaultAsync(s => s.Email == model.Email);

            if (user == null && student == null)
            {
                ModelState.AddModelError("", "Email not found in our system.");
                return View(model);
            }

            // Generate a secure reset token
            var token = Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.Now.AddHours(1); // Token valid for 1 hour

            // IMPORTANT: Save token to database
            if (user != null)
            {
                user.ResetToken = token;
                user.ResetTokenExpiry = tokenExpiry;
                _context.Update(user);
            }

            if (student != null)
            {
                student.ResetToken = token;
                student.ResetTokenExpiry = tokenExpiry;
                _context.Update(student);
            }

            await _context.SaveChangesAsync();

            // Build reset link
            var resetLink = Url.Action("ResetPassword", "Auth",
                new { token = token, email = model.Email },
                Request.Scheme);

            string subject = "Password Reset Request";
            string body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Password Reset</title>
        </head>
        <body style='font-family: Arial, sans-serif; background-color: #f8f9fa; margin: 0; padding: 0;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; padding: 30px 0;'>
                <tr>
                    <td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                            <tr>
                                <td style='padding: 30px; text-align: center;'>
                                    <h2 style='color: #343a40;'>Reset Your Password</h2>
                                    <p style='color: #6c757d; font-size: 16px; line-height: 1.5;'>
                                        You requested a password reset. Click the button below to set a new password:
                                    </p>
                                    <a href='{resetLink}' 
                                       style='display: inline-block; padding: 12px 25px; margin: 20px 0; font-size: 16px; color: #ffffff; background-color: #007bff; text-decoration: none; border-radius: 5px;'>
                                       Reset Password
                                    </a>
                                    <p style='color: #6c757d; font-size: 14px;'>
                                        <strong>This link will expire in 1 hour.</strong>
                                    </p>
                                    <p style='color: #6c757d; font-size: 14px;'>
                                        If you did not request this, please ignore this email and your password will remain unchanged.
                                    </p>
                                    <hr style='border: none; border-top: 1px solid #dee2e6; margin: 20px 0;'/>
                                    <p style='color: #adb5bd; font-size: 12px;'>
                                        &copy; {DateTime.Now.Year} Golden Success College. All rights reserved.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, subject, body);
                TempData["SuccessMessage"] = "Password reset link has been sent to your email. Please check your inbox.";
            }
            catch (Exception ex)
            {
                // Log error if you have logging configured
                TempData["ErrorMessage"] = "Failed to send reset email. Please try again later.";
                return View(model);
            }

            return RedirectToAction(nameof(ForgotPassword));
        }

        // GET: /Auth/ResetPassword?token=xyz&email=abc
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction(nameof(Login));
            }

            // VERIFY TOKEN EXISTS AND IS VALID
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Email == email && u.ResetToken == token);

            var student = await _context.Student
                .FirstOrDefaultAsync(s => s.Email == email && s.ResetToken == token);

            if (user == null && student == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired reset link.";
                return RedirectToAction(nameof(Login));
            }

            // CHECK IF TOKEN HAS EXPIRED
            var tokenExpiry = user?.ResetTokenExpiry ?? student?.ResetTokenExpiry;
            if (tokenExpiry == null || tokenExpiry < DateTime.Now)
            {
                TempData["ErrorMessage"] = "This reset link has expired. Please request a new one.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ForgotPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        // POST: /Auth/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // VERIFY TOKEN AGAIN
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token);

            var student = await _context.Student
                .FirstOrDefaultAsync(s => s.Email == model.Email && s.ResetToken == model.Token);

            if (user == null && student == null)
            {
                ModelState.AddModelError("", "Invalid or expired reset token.");
                return View(model);
            }

            // CHECK EXPIRY AGAIN
            var tokenExpiry = user?.ResetTokenExpiry ?? student?.ResetTokenExpiry;
            if (tokenExpiry == null || tokenExpiry < DateTime.Now)
            {
                ModelState.AddModelError("", "This reset link has expired. Please request a new one.");
                return View(model);
            }

            // Update password and CLEAR THE TOKEN
            if (user != null)
            {
                user.Password = PasswordHelper.HashPassword(model.NewPassword);
                user.ResetToken = null; // Clear token so it can't be reused
                user.ResetTokenExpiry = null;
                _context.Update(user);
            }
            else if (student != null)
            {
                student.Password = PasswordHelper.HashPassword(model.NewPassword);
                student.ResetToken = null; // Clear token so it can't be reused
                student.ResetTokenExpiry = null;
                _context.Update(student);
            }

            await _context.SaveChangesAsync();

            // Send success email
            string subject = "Password Successfully Changed";
            string body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <title>Password Changed</title>
        </head>
        <body style='font-family: Arial, sans-serif; background-color: #f8f9fa; margin: 0; padding: 0;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f8f9fa; padding: 30px 0;'>
                <tr>
                    <td align='center'>
                        <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                            <tr>
                                <td style='padding: 30px; text-align: center;'>
                                    <h2 style='color: #28a745;'>Password Successfully Changed</h2>
                                    <p style='color: #6c757d; font-size: 16px; line-height: 1.5;'>
                                        Your password has been successfully updated.
                                    </p>
                                    <p style='color: #6c757d; font-size: 16px; line-height: 1.5;'>
                                        You can now login with your new password.
                                    </p>
                                    <p style='color: #dc3545; font-size: 14px; line-height: 1.5;'>
                                        <strong>If you did not perform this action, please contact support immediately.</strong>
                                    </p>
                                    <hr style='border: none; border-top: 1px solid #dee2e6; margin: 20px 0;'/>
                                    <p style='color: #adb5bd; font-size: 12px;'>
                                        &copy; {DateTime.Now.Year} Golden Success College. All rights reserved.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>";

            var recipientEmail = user?.Email ?? student?.Email;
            if (!string.IsNullOrEmpty(recipientEmail))
            {
                await _emailService.SendEmailAsync(recipientEmail, subject, body);
            }

            TempData["SuccessMessage"] = "Password successfully updated! You can now login with your new password.";
            return RedirectToAction("Login", "Auth");
        }



        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Check for forwarded IP (when behind proxy/load balancer)
            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }

            return ipAddress ?? "Unknown";
        }

        // NOTE: Your primary login actions (Login/Logout/AccessDenied) would be here too.

        // GET: /Auth/ConfirmEmail?token=...&email=...
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid activation link or missing parameters.";
                return RedirectToAction("Login");
            }

            var student = await _context.Student.FirstOrDefaultAsync(s => s.Email == email);

            if (student == null)
            {
                TempData["ErrorMessage"] = "Account not found.";
                return RedirectToAction("Login");
            }

            // 1. Check if already confirmed (Optional, good practice)
            if (student.EmailConfirmed)
            {
                TempData["WarningMessage"] = "Your email is already confirmed. Please log in.";
                return RedirectToAction("Login");
            }

            // 2. Check the token validity
            if (student.EmailConfirmationToken != token)
            {
                TempData["ErrorMessage"] = "Invalid confirmation token.";
                return RedirectToAction("Login");
            }

            // 3. Check for expiration
            if (student.TokenExpirationDate.HasValue && student.TokenExpirationDate.Value < DateTime.UtcNow)
            {
                // Note: In a production app, you would offer to resend the email here.
                TempData["ErrorMessage"] = "The activation link has expired. Please contact administration or use the 'Forgot Password' feature to get a new link.";
                return RedirectToAction("Login");
            }

            // 4. Confirmation Successful: Update the database
            student.EmailConfirmed = true;
            student.EmailConfirmationToken = null; // Clear the token for security

            // NOTE: IsTemporaryPassword remains TRUE here, which will force a password change upon first successful login.

            _context.Update(student);
            await _context.SaveChangesAsync();

            // 5. Redirect to Login with success message
            TempData["SuccessMessage"] = "Email confirmed successfully! Please log in using the temporary password provided in your email. You will be prompted to set a permanent password.";

            // Redirect to the Login action
            return RedirectToAction("Login");
        }
    }
}