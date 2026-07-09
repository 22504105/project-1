using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using SQLite;

namespace ExamPlanner.Core.Tests;

public class RepositoryTest : IDisposable
{
    private readonly string _dbPath;
    private readonly SqlitePlannerRepository _repo;

    public RepositoryTest()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"ep_{Guid.NewGuid():N}.db3");
        _repo = new SqlitePlannerRepository(_dbPath);
    }

    public void Dispose()
    {
        // sqlite-net-pcl pools connections by path; on Windows the OS file lock
        // persists until the pool is reset, so the temp file can't be deleted otherwise.
        SQLiteAsyncConnection.ResetPool();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task Save_Then_Get_Exam_RoundTrips()
    {
        var id = await _repo.SaveExamAsync(new Exam { Name = "Matan", Date = new DateTime(2026, 6, 12), MinutesPerTopic = 40 });
        var loaded = await _repo.GetExamAsync(id);
        Assert.NotNull(loaded);
        Assert.Equal("Matan", loaded!.Name);
        Assert.Equal(40, loaded.MinutesPerTopic);
    }

    [Fact]
    public async Task DeleteExam_Also_Deletes_Its_Topics()
    {
        var examId = await _repo.SaveExamAsync(new Exam { Name = "Physics" });
        await _repo.SaveTopicAsync(new Topic { ExamId = examId, Title = "Optics" });
        await _repo.DeleteExamAsync(examId);
        var topics = await _repo.GetTopicsAsync(examId);
        Assert.Empty(topics);
        Assert.Null(await _repo.GetExamAsync(examId));
    }

    [Fact]
    public async Task GetSettings_Creates_Default_When_Missing()
    {
        var settings = await _repo.GetSettingsAsync();
        Assert.Equal(1, settings.Id);
        Assert.Equal(2, settings.AvailableHoursPerDay);
    }

    [Fact]
    public async Task SaveTopic_RoundTrips_Difficulty()
    {
        var examId = await _repo.SaveExamAsync(new Exam { Name = "Matan" });
        await _repo.SaveTopicAsync(new Topic { ExamId = examId, Title = "Series", Difficulty = TopicDifficulty.Hard });

        var topics = await _repo.GetTopicsAsync(examId);
        Assert.Single(topics);
        Assert.Equal(TopicDifficulty.Hard, topics[0].Difficulty);
    }
}
