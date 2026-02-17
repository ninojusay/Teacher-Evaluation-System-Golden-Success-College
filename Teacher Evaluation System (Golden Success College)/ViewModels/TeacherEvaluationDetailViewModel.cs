using System;
using System.Collections.Generic;

namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class TeacherEvaluationDetailViewModel
    {
        public int EvaluationId { get; set; }
        public string? SubjectName { get; set; }
        public string? StudentName { get; set; }
        public bool IsAnonymous { get; set; }
        public DateTime DateEvaluated { get; set; }
        public double AverageScore { get; set; }
        public string? Comments { get; set; }
        public List<QuestionResultViewModel> Questions { get; set; } = new List<QuestionResultViewModel>();
    }
}
