using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        public string? FullName { get; set; }
        public string? Email { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        // --- NEW SECURITY FIELDS ---
        public bool EmailConfirmed { get; set; } = false; // Is the email verified?
        public string? EmailConfirmationToken { get; set; } // Token for activation link
        public DateTime? TokenExpirationDate { get; set; } // Token expiry time
        public bool IsTemporaryPassword { get; set; } = true; // Force password change on login

        // --- END NEW SECURITY FIELDS ---

        public int LevelId { get; set; }
        [ForeignKey(nameof(LevelId))]
        public Level? Level { get; set; }

        public int? SectionId { get; set; }
        [ForeignKey(nameof(SectionId))]
        public Section? Section { get; set; }

        public int? CollegeYearLevel { get; set; }
        public int RoleId { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }


        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
    }
}