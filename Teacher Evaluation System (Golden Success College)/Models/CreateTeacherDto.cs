using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.Models
{
    public class CreateTeacherDto
    {
        [FromForm]
        [Required]
        public string FullName { get; set; }

        [FromForm]
        [Required]
        public string Department { get; set; }

        [FromForm]
        [Required]
        public int LevelId { get; set; }

        [FromForm]
        public bool IsActive { get; set; }

        [FromForm]
        public IFormFile? PictureFile { get; set; }
    }
}
