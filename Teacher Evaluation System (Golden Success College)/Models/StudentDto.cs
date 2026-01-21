using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class StudentDto
    {
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? Email { get; set; }

        public string? Password { get; set; }

        [Required(ErrorMessage = "Level is required")]
        public int LevelId { get; set; }

        public int? SectionId { get; set; }

        public int? CollegeYearLevel { get; set; }
    }
}
