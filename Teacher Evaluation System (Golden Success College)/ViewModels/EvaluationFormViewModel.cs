using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{

    public class EnrollmentCreateViewModel
    {
        public int StudentId { get; set; }
        public List<int> SubjectIds { get; set; } = new List<int>();
    }
}