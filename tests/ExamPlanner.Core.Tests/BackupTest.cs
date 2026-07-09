using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;
using SQLite;

namespace ExamPlanner.Core.Tests;

[Collection("Sqlite")]
public class BackupTest : IDisposable
{
    private readonly List<string> _paths = new();

    private SqlitePlannerRepository NewRepo()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ep_{Guid.NewGuid():N}.db3");
        _paths.Add(path);
        return new SqlitePlannerRepository(path);
    }

    public void Dispose()
    {
        SQLiteAsyncConnection.ResetPool();
        foreach (var p in _paths)
            if (File.Exists(p)) File.Delete(p);
    }

    [Fact]
    public async Task Export_Then_Import_Preserves_Data_And_Relationships()
    {
        var src = NewRepo();
        var examId = await src.SaveExamAsync(new Exam { Name = "Matan", MinutesPerTopic = 40 });
        var topicId = await src.SaveTopicAsync(new Topic { ExamId = examId, Title = "Limits", Difficulty = TopicDifficulty.Hard });
        await src.SaveSessionAsync(new StudySession { ExamId = examId, TopicId = topicId, Minutes = 55, StartedAt = new DateTime(2026, 6, 1) });
        await src.SaveSettingsAsync(new AppSettings { AvailableHoursPerDay = 3, SatHours = 5 });

        var json = await new BackupService(src).ExportJsonAsync();

        var dst = NewRepo();
        await new BackupService(dst).ImportJsonAsync(json);

        var exams = await dst.GetExamsAsync();
        Assert.Single(exams);
        Assert.Equal("Matan", exams[0].Name);

        var topics = await dst.GetTopicsAsync(exams[0].Id);
        Assert.Single(topics);
        Assert.Equal("Limits", topics[0].Title);
        Assert.Equal(TopicDifficulty.Hard, topics[0].Difficulty);
        Assert.Equal(exams[0].Id, topics[0].ExamId);          // exam relationship remapped

        var sessions = await dst.GetAllSessionsAsync();
        Assert.Single(sessions);
        Assert.Equal(55, sessions[0].Minutes);
        Assert.Equal(exams[0].Id, sessions[0].ExamId);
        Assert.Equal(topics[0].Id, sessions[0].TopicId);      // topic relationship remapped

        var settings = await dst.GetSettingsAsync();
        Assert.Equal(3, settings.AvailableHoursPerDay);
        Assert.Equal(5, settings.SatHours);
    }

    [Fact]
    public async Task Import_Replaces_Existing_Data()
    {
        var src = NewRepo();
        await src.SaveExamAsync(new Exam { Name = "Kept" });
        var json = await new BackupService(src).ExportJsonAsync();

        var dst = NewRepo();
        await dst.SaveExamAsync(new Exam { Name = "Old1" });
        await dst.SaveExamAsync(new Exam { Name = "Old2" });

        await new BackupService(dst).ImportJsonAsync(json);

        var exams = await dst.GetExamsAsync();
        Assert.Single(exams);
        Assert.Equal("Kept", exams[0].Name);
    }

    [Theory]
    [InlineData("null")]     // deserializes to null snapshot
    [InlineData("{}")]       // deserializes to a snapshot with null lists
    public async Task Import_Throws_On_Malformed_Json(string json)
    {
        var repo = NewRepo();
        await Assert.ThrowsAsync<ArgumentException>(() => new BackupService(repo).ImportJsonAsync(json));
    }
}
