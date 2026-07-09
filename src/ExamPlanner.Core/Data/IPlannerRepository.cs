using ExamPlanner.Core.Models;

namespace ExamPlanner.Core.Data;

public interface IPlannerRepository
{
    Task InitializeAsync();
    Task<List<Exam>> GetExamsAsync();
    Task<Exam?> GetExamAsync(int id);
    Task<int> SaveExamAsync(Exam exam);
    Task DeleteExamAsync(int id);
    Task<List<Topic>> GetTopicsAsync(int examId);
    Task<int> SaveTopicAsync(Topic topic);
    Task DeleteTopicAsync(int id);
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
}
