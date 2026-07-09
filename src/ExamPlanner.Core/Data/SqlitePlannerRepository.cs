using ExamPlanner.Core.Models;
using SQLite;

namespace ExamPlanner.Core.Data;

public class SqlitePlannerRepository : IPlannerRepository
{
    private readonly SQLiteAsyncConnection _db;
    private bool _initialized;

    static SqlitePlannerRepository() => SQLitePCL.Batteries_V2.Init();

    public SqlitePlannerRepository(string dbPath)
    {
        _db = new SQLiteAsyncConnection(dbPath);
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        await _db.CreateTableAsync<Exam>();
        await _db.CreateTableAsync<Topic>();
        await _db.CreateTableAsync<AppSettings>();
        await _db.CreateTableAsync<StudySession>();
        _initialized = true;
    }

    public async Task<List<Exam>> GetExamsAsync()
    {
        await InitializeAsync();
        return await _db.Table<Exam>().OrderBy(e => e.Date).ToListAsync();
    }

    public async Task<Exam?> GetExamAsync(int id)
    {
        await InitializeAsync();
        return await _db.FindAsync<Exam>(id);
    }

    public async Task<int> SaveExamAsync(Exam exam)
    {
        await InitializeAsync();
        if (exam.Id != 0)
            await _db.UpdateAsync(exam);
        else
        {
            exam.CreatedAt = DateTime.UtcNow;
            await _db.InsertAsync(exam);
        }
        return exam.Id;
    }

    public async Task DeleteExamAsync(int id)
    {
        await InitializeAsync();
        await _db.ExecuteAsync("DELETE FROM Topic WHERE ExamId = ?", id);
        await _db.ExecuteAsync("DELETE FROM StudySession WHERE ExamId = ?", id);
        await _db.DeleteAsync<Exam>(id);
    }

    public async Task<List<Topic>> GetTopicsAsync(int examId)
    {
        await InitializeAsync();
        return await _db.Table<Topic>()
            .Where(t => t.ExamId == examId)
            .OrderBy(t => t.Position)
            .ToListAsync();
    }

    public async Task<int> SaveTopicAsync(Topic topic)
    {
        await InitializeAsync();
        if (topic.Id != 0)
            await _db.UpdateAsync(topic);
        else
            await _db.InsertAsync(topic);
        return topic.Id;
    }

    public async Task DeleteTopicAsync(int id)
    {
        await InitializeAsync();
        await _db.DeleteAsync<Topic>(id);
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        await InitializeAsync();
        var settings = await _db.FindAsync<AppSettings>(1);
        if (settings == null)
        {
            settings = new AppSettings { Id = 1, AvailableHoursPerDay = 2 };
            await _db.InsertAsync(settings);
        }
        return settings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await InitializeAsync();
        settings.Id = 1;
        await _db.InsertOrReplaceAsync(settings);
    }

    public async Task<int> SaveSessionAsync(StudySession session)
    {
        await InitializeAsync();
        await _db.InsertAsync(session);
        return session.Id;
    }

    public async Task<List<StudySession>> GetSessionsAsync(int examId)
    {
        await InitializeAsync();
        return await _db.Table<StudySession>()
            .Where(s => s.ExamId == examId)
            .OrderBy(s => s.StartedAt)
            .ToListAsync();
    }

    public async Task<List<StudySession>> GetAllSessionsAsync()
    {
        await InitializeAsync();
        return await _db.Table<StudySession>()
            .OrderBy(s => s.StartedAt)
            .ToListAsync();
    }
}
