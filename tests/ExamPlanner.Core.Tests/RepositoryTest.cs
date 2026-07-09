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

    [Fact]
    public async Task SaveSettings_RoundTrips_WeekdayHours()
    {
        await _repo.SaveSettingsAsync(new AppSettings { AvailableHoursPerDay = 2, SatHours = 5, SunHours = 6 });

        var loaded = await _repo.GetSettingsAsync();
        Assert.Equal(5, loaded.SatHours);
        Assert.Equal(6, loaded.SunHours);
        Assert.Null(loaded.MonHours);
    }

    [Fact]
    public async Task SaveSession_RoundTrips_And_GetByExam()
    {
        var examId = await _repo.SaveExamAsync(new Exam { Name = "Matan" });
        await _repo.SaveSessionAsync(new StudySession
        {
            ExamId = examId, TopicId = 7, Minutes = 45, StartedAt = new DateTime(2026, 6, 1, 10, 0, 0)
        });

        var sessions = await _repo.GetSessionsAsync(examId);
        Assert.Single(sessions);
        Assert.Equal(45, sessions[0].Minutes);
        Assert.Equal(7, sessions[0].TopicId);
        Assert.Single(await _repo.GetAllSessionsAsync());
    }

    [Fact]
    public async Task DeleteExam_Also_Deletes_Its_Sessions()
    {
        var examId = await _repo.SaveExamAsync(new Exam { Name = "Physics" });
        await _repo.SaveSessionAsync(new StudySession { ExamId = examId, Minutes = 30, StartedAt = new DateTime(2026, 6, 2) });

        await _repo.DeleteExamAsync(examId);

        Assert.Empty(await _repo.GetSessionsAsync(examId));
        Assert.Empty(await _repo.GetAllSessionsAsync());
    }
}
