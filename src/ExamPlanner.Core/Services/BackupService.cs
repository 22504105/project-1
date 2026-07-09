using System.Text.Json;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;

namespace ExamPlanner.Core.Services;

public record BackupSnapshot(
    List<Exam> Exams,
    List<Topic> Topics,
    List<StudySession> Sessions,
    AppSettings Settings);

// Exports all planner data to JSON and restores it, remapping auto-increment
// ids so exam/topic/session relationships stay consistent after import.
public class BackupService
{
    private readonly IPlannerRepository _repo;

    public BackupService(IPlannerRepository repo) => _repo = repo;

    public async Task<string> ExportJsonAsync()
    {
        var exams = await _repo.GetExamsAsync();
        var topics = new List<Topic>();
        foreach (var exam in exams)
            topics.AddRange(await _repo.GetTopicsAsync(exam.Id));
        var sessions = await _repo.GetAllSessionsAsync();
        var settings = await _repo.GetSettingsAsync();

        return JsonSerializer.Serialize(new BackupSnapshot(exams, topics, sessions, settings));
    }

    // Replaces ALL existing data with the snapshot.
    public async Task ImportJsonAsync(string json)
    {
        var snapshot = JsonSerializer.Deserialize<BackupSnapshot>(json)
            ?? throw new ArgumentException("Invalid backup JSON", nameof(json));

        // Clear existing (DeleteExamAsync cascades its topics + sessions).
        foreach (var exam in await _repo.GetExamsAsync())
            await _repo.DeleteExamAsync(exam.Id);

        var examIdMap = new Dictionary<int, int>();
        foreach (var exam in snapshot.Exams)
        {
            var oldId = exam.Id;
            exam.Id = 0;
            examIdMap[oldId] = await _repo.SaveExamAsync(exam);
        }

        var topicIdMap = new Dictionary<int, int>();
        foreach (var topic in snapshot.Topics)
        {
            var oldId = topic.Id;
            topic.Id = 0;
            topic.ExamId = examIdMap[topic.ExamId];
            topicIdMap[oldId] = await _repo.SaveTopicAsync(topic);
        }

        foreach (var session in snapshot.Sessions)
        {
            session.Id = 0;
            session.ExamId = examIdMap[session.ExamId];
            session.TopicId = session.TopicId.HasValue && topicIdMap.TryGetValue(session.TopicId.Value, out var newTopicId)
                ? newTopicId
                : null;
            await _repo.SaveSessionAsync(session);
        }

        await _repo.SaveSettingsAsync(snapshot.Settings);
    }
}
