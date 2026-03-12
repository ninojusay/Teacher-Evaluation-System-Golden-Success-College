using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Teacher_Evaluation_System__Golden_Success_College_.Models;

namespace Teacher_Evaluation_System__Golden_Success_College_.Services
{
    public interface ITeacherAiSummaryService
    {
        string GenerateSummary(string teacherName, List<Evaluation> evaluations);
        string GenerateAdvice(List<Evaluation> evaluations);
    }

    public class TeacherAiSummaryService : ITeacherAiSummaryService
    {
        public string GenerateSummary(string teacherName, List<Evaluation> evaluations)
        {
            if (evaluations == null || !evaluations.Any())
                return $"{teacherName} has no evaluations yet for this period.";

            int totalEvaluations = evaluations.Count;
            double avgScore = evaluations.Average(e => e.AverageScore);
            
            var criteriaScores = evaluations
                .SelectMany(e => e.Scores)
                .GroupBy(s => s.Question?.Criteria?.Name ?? "General")
                .Select(g => new { Name = g.Key, Avg = g.Average(s => s.ScoreValue) })
                .OrderByDescending(c => c.Avg)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"📊 **Teacher Performance Profile: {teacherName}**");
            sb.AppendLine($"Overall Score: **{avgScore:F2} / 4.00** (Based on {totalEvaluations} evaluations)");
            sb.AppendLine($"Archetype: **{GetTeacherArchetype(avgScore, criteriaScores)}**");
            sb.AppendLine();
            
            sb.AppendLine("🏆 **Top Performing Areas:**");
            foreach (var c in criteriaScores.Take(2))
            {
                sb.AppendLine($"- **{c.Name}** ({c.Avg:F2}): {GetAssessment(c.Avg)}");
            }
            sb.AppendLine();

            sb.AppendLine("🛠️ **Critical Growth Areas:**");
            var needsWork = criteriaScores.Where(c => c.Avg < 3.0).OrderBy(c => c.Avg).ToList();
            if (needsWork.Any())
            {
                foreach (var c in needsWork.Take(2))
                {
                    sb.AppendLine($"- **{c.Name}** ({c.Avg:F2}): Significant room for technical improvement.");
                }
            }
            else
            {
                sb.AppendLine("- No major weaknesses detected. Performance is consistent across all metrics.");
            }
            
            sb.AppendLine();
            sb.AppendLine("📅 **Performance Trends:**");
            sb.AppendLine($"- Engagement Level: **{GetEngagementLevel(totalEvaluations)}**");
            sb.AppendLine($"- Consistency Score: **{GetConsistencyRating(evaluations)}**");

            return sb.ToString();
        }

        public string GenerateAdvice(List<Evaluation> evaluations)
        {
            if (evaluations == null || !evaluations.Any())
                return "No data available to provide advice.";

            double avgScore = evaluations.Average(e => e.AverageScore);
            var comments = evaluations.Where(e => !string.IsNullOrEmpty(e.Comments)).Select(e => e.Comments).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("🚀 **AI Professional Growth Roadmap:**");
            
            // Month 1
            sb.AppendLine("**Month 1: Foundation & Feedback**");
            sb.AppendLine("- Conduct a 'One-Minute Paper' at the end of each class to gauge immediate student understanding.");
            sb.AppendLine("- Review the specific criteria for 'Communication' and simplify lecture notes.");
            sb.AppendLine();

            // Month 2
            sb.AppendLine("**Month 2: Active Learning Implementation**");
            sb.AppendLine("- Increase interactive polling or group discussions by 20% to boost classroom engagement.");
            sb.AppendLine("- Implement peer-assessment tasks to diversify the learning experience.");
            sb.AppendLine();

            // Month 3
            sb.AppendLine("**Month 3: Advanced Pedagogy**");
            sb.AppendLine("- Integrate multimedia or case-study methods into 50% of your teaching modules.");
            sb.AppendLine("- Seek a peer-observation session with a top-performing colleague.");
            
            if (comments.Any())
            {
                sb.AppendLine();
                sb.AppendLine("⭐ **Student Voice Analysis:**");
                var themes = ExtractThemes(comments);
                foreach(var theme in themes)
                {
                    sb.AppendLine($"- {theme}");
                }
            }

            return sb.ToString();
        }

        private string GetTeacherArchetype(double avg, dynamic criteria)
        {
            if (avg >= 3.6) return "The Academic Mastermind";
            
            var highest = criteria[0].Name;
            if (highest.Contains("Management", StringComparison.OrdinalIgnoreCase)) return "The Disciplined Mentor";
            if (highest.Contains("Interaction", StringComparison.OrdinalIgnoreCase) || highest.Contains("Communication", StringComparison.OrdinalIgnoreCase)) return "The Engaging Communicator";
            if (highest.Contains("Skills", StringComparison.OrdinalIgnoreCase)) return "The Subject Matter Expert";
            
            return "The Consistent Educator";
        }

        private string GetEngagementLevel(int count)
        {
            if (count > 50) return "High (Strong Student Connection)";
            if (count > 20) return "Moderate (Active Classroom)";
            return "Emerging (Low Feedback Volume)";
        }

        private string GetConsistencyRating(List<Evaluation> evaluations)
        {
            var scores = evaluations.Select(e => e.AverageScore).ToList();
            double standardDeviation = Math.Sqrt(scores.Average(v => Math.Pow(v - scores.Average(), 2)));
            
            if (standardDeviation < 0.3) return "High (Reliable Performance)";
            if (standardDeviation < 0.6) return "Stable (Predictable Outcome)";
            return "Variable (Fluctuating Performance)";
        }

        private List<string> ExtractThemes(List<string> comments)
        {
            var themes = new List<string>();
            string allText = string.Join(" ", comments).ToLower();

            if (allText.Contains("clear") || allText.Contains("explain")) themes.Add("Appreciated for **Clarity of Explanation**.");
            if (allText.Contains("kind") || allText.Contains("humble") || allText.Contains("nice")) themes.Add("Strongly valued for **Positive Personality**.");
            if (allText.Contains("fast") || allText.Contains("pace")) themes.Add("Frequent mentions of **Instructional Pacing** (needs adjustment).");
            if (allText.Contains("example") || allText.Contains("demo")) themes.Add("Excellent use of **Practical Examples**.");
            
            if (!themes.Any()) themes.Add("Students generally focus on overall performance rather than specific traits.");
            
            return themes.Take(3).ToList();
        }

        private string GetAssessment(double score)
        {
            if (score >= 3.5) return "Distinguished";
            if (score >= 3.0) return "Proficient";
            if (score >= 2.5) return "Satisfactory";
            return "Requires Improvement";
        }
    }
}
