using System.Collections.Generic;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class TeacherStudentStatusViewModel
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public bool HasEvaluated { get; set; }
        public int? EvaluationId { get; set; }
        public bool IsAnonymous { get; set; }
    }

    public class TeacherDetailsViewModel
    {
        public int TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherPicturePath { get; set; }
        public List<TeacherStudentStatusViewModel> StudentStatuses { get; set; } = new();
    }
}
