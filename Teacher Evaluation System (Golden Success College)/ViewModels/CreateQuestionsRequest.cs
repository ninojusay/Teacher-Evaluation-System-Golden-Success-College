namespace Teacher_Evaluation_System__Golden_Success_College_.ViewModels
{
    public class CreateQuestionsRequest
    {
        public int CriteriaId { get; set; }
        public List<string> Descriptions { get; set; } = new();
    }

    public class UpdateQuestionsRequest
    {
        public int CriteriaId { get; set; }
        public List<string> Descriptions { get; set; } = new();
    }
}