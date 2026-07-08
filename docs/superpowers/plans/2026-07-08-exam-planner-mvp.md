# ExamPlanner MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Собрать локальное Android-приложение (.NET MAUI) для студентов: экзамены с датами, темы с прогрессом (3 статуса), детерминированное планирование времени (рекомендация/день + дневной план).

**Architecture:** Чистая логика (модели, репозиторий SQLite, расчётный `PlannerService`) вынесена в библиотеку `ExamPlanner.Core` (`net8.0`), которая тестируется юнит-тестами без MAUI. MAUI-приложение `ExamPlanner` (`net8.0-android`) даёт UI по MVVM (CommunityToolkit.Mvvm) поверх Core.

**Tech Stack:** .NET 8 · .NET MAUI (Android) · MVVM (CommunityToolkit.Mvvm) · SQLite (sqlite-net-pcl) · xUnit.

## Global Constraints

- Целевой SDK: .NET 8 (установлен 8.0.422). Вызов `dotnet` из Bash — по полному пути `"C:\Program Files\dotnet\dotnet.exe"` (в PATH его нет).
- Домашний путь содержит кириллицу (`C:\Users\Евгений`) — при чтении stdin/файлов в PowerShell использовать UTF-8 (см. `CLAUDE.md`).
- Solution в формате `.slnx` (`project-1.slnx`). SDK 8 не умеет `dotnet sln` для `.slnx` — проекты добавляются **ручным редактированием** `project-1.slnx` (XML).
- MVP: только Android, один пользователь, локально, без сети/ИИ/уведомлений.
- Язык кода — английский (идентификаторы), строки UI — русские.
- Все расчёты детерминированы и покрыты юнит-тестами (`PlannerService`).
- Коммиты частые, идентичность: `22504105 <universgroup13@gmail.com>`.
- Ветка работы: `feature/exam-planner-mvp`.

---

### Task 0: Solution scaffolding — Core + Tests projects

Создаёт библиотеку ядра и тест-проект (оба `net8.0`, без MAUI), подключает их к solution. Даёт зелёный `dotnet test` с одним тривиальным тестом — фундамент для TDD ядра.

**Files:**
- Create: `src/ExamPlanner.Core/ExamPlanner.Core.csproj`
- Create: `tests/ExamPlanner.Core.Tests/ExamPlanner.Core.Tests.csproj`
- Create: `tests/ExamPlanner.Core.Tests/SmokeTest.cs`
- Modify: `project-1.slnx` (добавить два проекта)

**Interfaces:**
- Produces: проекты `ExamPlanner.Core` и `ExamPlanner.Core.Tests`; тест-проект ссылается на Core.

- [ ] **Step 1: Create Core class library**

Run:
```bash
DOTNET="/c/Program Files/dotnet/dotnet.exe"
"$DOTNET" new classlib -f net8.0 -n ExamPlanner.Core -o src/ExamPlanner.Core
rm -f src/ExamPlanner.Core/Class1.cs
```

- [ ] **Step 2: Add SQLite packages to Core**

Run:
```bash
"$DOTNET" add src/ExamPlanner.Core package sqlite-net-pcl --version 1.9.172
"$DOTNET" add src/ExamPlanner.Core package SQLitePCLRaw.bundle_green --version 2.1.10
```

- [ ] **Step 3: Create xUnit test project**

Run:
```bash
"$DOTNET" new xunit -f net8.0 -n ExamPlanner.Core.Tests -o tests/ExamPlanner.Core.Tests
rm -f tests/ExamPlanner.Core.Tests/UnitTest1.cs
"$DOTNET" add tests/ExamPlanner.Core.Tests reference src/ExamPlanner.Core
```

- [ ] **Step 4: Add smoke test**

Create `tests/ExamPlanner.Core.Tests/SmokeTest.cs`:
```csharp
namespace ExamPlanner.Core.Tests;

public class SmokeTest
{
    [Fact]
    public void Solution_Builds_And_Tests_Run()
    {
        Assert.True(true);
    }
}
```

- [ ] **Step 5: Register both projects in the .slnx**

Modify `project-1.slnx` — add the two projects (keep the existing `src/App`):
```xml
<Solution>
  <Folder Name="/src/">
    <Project Path="src/App/App.csproj" />
    <Project Path="src/ExamPlanner.Core/ExamPlanner.Core.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/ExamPlanner.Core.Tests/ExamPlanner.Core.Tests.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 6: Run the test to verify green**

Run:
```bash
"$DOTNET" test tests/ExamPlanner.Core.Tests
```
Expected: `Passed! - Failed: 0, Passed: 1`.

- [ ] **Step 7: Commit**

```bash
git add src/ExamPlanner.Core tests/ExamPlanner.Core.Tests project-1.slnx
git commit -m "chore: scaffold ExamPlanner.Core and test project"
```

---

### Task 1: Domain models

Модели данных и enum статуса — фундамент для репозитория и расчётов.

**Files:**
- Create: `src/ExamPlanner.Core/Models/TopicStatus.cs`
- Create: `src/ExamPlanner.Core/Models/Exam.cs`
- Create: `src/ExamPlanner.Core/Models/Topic.cs`
- Create: `src/ExamPlanner.Core/Models/AppSettings.cs`
- Test: `tests/ExamPlanner.Core.Tests/ModelsTest.cs`

**Interfaces:**
- Produces:
  - `enum TopicStatus { NotStarted = 0, InProgress = 1, Done = 2 }`
  - `class Exam { int Id; string Name; DateTime Date; int MinutesPerTopic; DateTime CreatedAt; }`
  - `class Topic { int Id; int ExamId; string Title; TopicStatus Status; int Position; }`
  - `class AppSettings { int Id; double AvailableHoursPerDay; }`

- [ ] **Step 1: Write the failing test**

Create `tests/ExamPlanner.Core.Tests/ModelsTest.cs`:
```csharp
using ExamPlanner.Core.Models;

namespace ExamPlanner.Core.Tests;

public class ModelsTest
{
    [Fact]
    public void TopicStatus_Has_Three_Ordered_States()
    {
        Assert.Equal(0, (int)TopicStatus.NotStarted);
        Assert.Equal(1, (int)TopicStatus.InProgress);
        Assert.Equal(2, (int)TopicStatus.Done);
    }

    [Fact]
    public void Topic_Defaults_To_NotStarted()
    {
        var topic = new Topic { Title = "Limits" };
        Assert.Equal(TopicStatus.NotStarted, topic.Status);
        Assert.Equal("Limits", topic.Title);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests`
Expected: FAIL — `TopicStatus`/`Topic` не найдены.

- [ ] **Step 3: Create the models**

Create `src/ExamPlanner.Core/Models/TopicStatus.cs`:
```csharp
namespace ExamPlanner.Core.Models;

public enum TopicStatus
{
    NotStarted = 0,
    InProgress = 1,
    Done = 2
}
```

Create `src/ExamPlanner.Core/Models/Exam.cs`:
```csharp
using SQLite;

namespace ExamPlanner.Core.Models;

public class Exam
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int MinutesPerTopic { get; set; } = 30;
    public DateTime CreatedAt { get; set; }
}
```

Create `src/ExamPlanner.Core/Models/Topic.cs`:
```csharp
using SQLite;

namespace ExamPlanner.Core.Models;

public class Topic
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public TopicStatus Status { get; set; } = TopicStatus.NotStarted;
    public int Position { get; set; }
}
```

Create `src/ExamPlanner.Core/Models/AppSettings.cs`:
```csharp
using SQLite;

namespace ExamPlanner.Core.Models;

public class AppSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;
    public double AvailableHoursPerDay { get; set; } = 2;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner.Core/Models tests/ExamPlanner.Core.Tests/ModelsTest.cs
git commit -m "feat(core): add domain models (Exam, Topic, AppSettings, TopicStatus)"
```

---

### Task 2: SQLite repository

Асинхронный репозиторий поверх sqlite-net с самоинициализацией таблиц. Тестируется на временном файле БД.

**Files:**
- Create: `src/ExamPlanner.Core/Data/IPlannerRepository.cs`
- Create: `src/ExamPlanner.Core/Data/SqlitePlannerRepository.cs`
- Test: `tests/ExamPlanner.Core.Tests/RepositoryTest.cs`

**Interfaces:**
- Consumes: models from Task 1.
- Produces `IPlannerRepository`:
  - `Task<List<Exam>> GetExamsAsync()`
  - `Task<Exam?> GetExamAsync(int id)`
  - `Task<int> SaveExamAsync(Exam exam)`
  - `Task DeleteExamAsync(int id)`
  - `Task<List<Topic>> GetTopicsAsync(int examId)`
  - `Task<int> SaveTopicAsync(Topic topic)`
  - `Task DeleteTopicAsync(int id)`
  - `Task<AppSettings> GetSettingsAsync()`
  - `Task SaveSettingsAsync(AppSettings settings)`
  - `Task InitializeAsync()`
- Produces `SqlitePlannerRepository(string dbPath) : IPlannerRepository`.

- [ ] **Step 1: Write the failing test**

Create `tests/ExamPlanner.Core.Tests/RepositoryTest.cs`:
```csharp
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;

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
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests`
Expected: FAIL — `SqlitePlannerRepository` не найден.

- [ ] **Step 3: Create the repository interface**

Create `src/ExamPlanner.Core/Data/IPlannerRepository.cs`:
```csharp
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
```

- [ ] **Step 4: Create the repository implementation**

Create `src/ExamPlanner.Core/Data/SqlitePlannerRepository.cs`:
```csharp
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
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests`
Expected: PASS (all repository tests green).

- [ ] **Step 6: Commit**

```bash
git add src/ExamPlanner.Core/Data tests/ExamPlanner.Core.Tests/RepositoryTest.cs
git commit -m "feat(core): add SQLite planner repository"
```

---

### Task 3: PlannerService — per-exam pace

Расчёт темпа по одному экзамену (остаток, дни, минут/день, срочность, дневная квота тем). Ядро планирования; чистые функции.

**Files:**
- Create: `src/ExamPlanner.Core/Services/PlannerModels.cs`
- Create: `src/ExamPlanner.Core/Services/PlannerService.cs`
- Test: `tests/ExamPlanner.Core.Tests/PlannerPaceTest.cs`

**Interfaces:**
- Consumes: `Exam`, `Topic`, `TopicStatus` from Task 1.
- Produces:
  - `record ExamPace(int ExamId, string Name, DateTime Date, int TotalTopics, int RemainingTopics, int DaysLeft, double NeedMinutesPerDay, int TodayQuota, bool IsUrgent)`
  - `static class/instance PlannerService` with:
    - `int DaysLeft(DateTime examDate, DateTime today)`
    - `int TodayQuota(int remaining, int daysLeft)`
    - `ExamPace BuildPace(Exam exam, IReadOnlyList<Topic> topics, DateTime today)`

- [ ] **Step 1: Write the failing test**

Create `tests/ExamPlanner.Core.Tests/PlannerPaceTest.cs`:
```csharp
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class PlannerPaceTest
{
    private static readonly DateTime Today = new(2026, 6, 1);
    private readonly PlannerService _svc = new();

    private static List<Topic> Topics(int total, int done)
    {
        var list = new List<Topic>();
        for (int i = 0; i < total; i++)
            list.Add(new Topic { Id = i + 1, Title = $"T{i}", Position = i, Status = i < done ? TopicStatus.Done : TopicStatus.NotStarted });
        return list;
    }

    [Fact]
    public void BuildPace_Computes_Need_And_Quota()
    {
        var exam = new Exam { Id = 7, Name = "Matan", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 };
        var pace = _svc.BuildPace(exam, Topics(total: 10, done: 0), Today);

        Assert.Equal(5, pace.DaysLeft);          // 6 Jun - 1 Jun
        Assert.Equal(10, pace.RemainingTopics);
        Assert.Equal(60, pace.NeedMinutesPerDay); // 10 * 30 / 5
        Assert.Equal(2, pace.TodayQuota);         // ceil(10 / 5)
        Assert.False(pace.IsUrgent);
    }

    [Fact]
    public void BuildPace_PastDate_ClampsDaysLeft_And_MarksUrgent()
    {
        var exam = new Exam { Id = 1, Name = "Late", Date = new DateTime(2026, 5, 30), MinutesPerTopic = 20 };
        var pace = _svc.BuildPace(exam, Topics(total: 3, done: 0), Today);

        Assert.Equal(1, pace.DaysLeft);   // clamped
        Assert.True(pace.IsUrgent);
        Assert.Equal(3, pace.TodayQuota); // ceil(3 / 1)
    }

    [Fact]
    public void BuildPace_AllDone_HasZeroRemaining()
    {
        var exam = new Exam { Id = 2, Name = "Done", Date = new DateTime(2026, 6, 10), MinutesPerTopic = 30 };
        var pace = _svc.BuildPace(exam, Topics(total: 4, done: 4), Today);

        Assert.Equal(0, pace.RemainingTopics);
        Assert.Equal(0, pace.NeedMinutesPerDay);
        Assert.Equal(0, pace.TodayQuota);
        Assert.False(pace.IsUrgent);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests --filter PlannerPaceTest`
Expected: FAIL — `PlannerService`/`ExamPace` не найдены.

- [ ] **Step 3: Create planner DTOs**

Create `src/ExamPlanner.Core/Services/PlannerModels.cs`:
```csharp
namespace ExamPlanner.Core.Services;

public record ExamPace(
    int ExamId,
    string Name,
    DateTime Date,
    int TotalTopics,
    int RemainingTopics,
    int DaysLeft,
    double NeedMinutesPerDay,
    int TodayQuota,
    bool IsUrgent);

public record PlanItem(int ExamId, string ExamName, int TopicId, string TopicTitle, int Minutes);

public record DailyPlan(
    IReadOnlyList<PlanItem> Items,
    double UsedMinutes,
    double AvailableMinutes,
    IReadOnlyList<int> NotFittedExamIds);

public record DashboardSummary(
    IReadOnlyList<ExamPace> Exams,
    double RecommendedMinutesPerDay,
    double AvailableMinutesPerDay,
    bool Behind);
```

- [ ] **Step 4: Create PlannerService with BuildPace**

Create `src/ExamPlanner.Core/Services/PlannerService.cs`:
```csharp
using ExamPlanner.Core.Models;

namespace ExamPlanner.Core.Services;

public class PlannerService
{
    private const int UrgentDaysThreshold = 3;

    public int DaysLeft(DateTime examDate, DateTime today)
        => Math.Max(1, (examDate.Date - today.Date).Days);

    public int TodayQuota(int remaining, int daysLeft)
        => remaining <= 0 ? 0 : (int)Math.Ceiling((double)remaining / daysLeft);

    public ExamPace BuildPace(Exam exam, IReadOnlyList<Topic> topics, DateTime today)
    {
        var total = topics.Count;
        var remaining = topics.Count(t => t.Status != TopicStatus.Done);
        var daysLeft = DaysLeft(exam.Date, today);
        var need = remaining == 0 ? 0d : (double)remaining * exam.MinutesPerTopic / daysLeft;
        var quota = TodayQuota(remaining, daysLeft);
        var urgent = remaining > 0 && daysLeft <= UrgentDaysThreshold;

        return new ExamPace(exam.Id, exam.Name, exam.Date, total, remaining, daysLeft, need, quota, urgent);
    }
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests --filter PlannerPaceTest`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add src/ExamPlanner.Core/Services tests/ExamPlanner.Core.Tests/PlannerPaceTest.cs
git commit -m "feat(core): add PlannerService pace calculation"
```

---

### Task 4: PlannerService — dashboard summary

Сводка по всем экзаменам: рекомендованное время/день против доступности, флаг «не успеваешь».

**Files:**
- Modify: `src/ExamPlanner.Core/Services/PlannerService.cs`
- Test: `tests/ExamPlanner.Core.Tests/PlannerDashboardTest.cs`

**Interfaces:**
- Consumes: `BuildPace` (Task 3), `DashboardSummary` (Task 3).
- Produces on `PlannerService`:
  - `DashboardSummary BuildDashboard(IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data, double availableHoursPerDay, DateTime today)`

- [ ] **Step 1: Write the failing test**

Create `tests/ExamPlanner.Core.Tests/PlannerDashboardTest.cs`:
```csharp
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class PlannerDashboardTest
{
    private static readonly DateTime Today = new(2026, 6, 1);
    private readonly PlannerService _svc = new();

    private static List<Topic> New(int total) =>
        Enumerable.Range(0, total)
            .Select(i => new Topic { Id = i + 1, Position = i, Status = TopicStatus.NotStarted })
            .ToList();

    [Fact]
    public void BuildDashboard_Sums_Need_And_Flags_Behind()
    {
        var data = new List<(Exam, IReadOnlyList<Topic>)>
        {
            (new Exam { Id = 1, Name = "A", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(10)), // 60/day
            (new Exam { Id = 2, Name = "B", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(10)), // 60/day
        };

        var summary = _svc.BuildDashboard(data, availableHoursPerDay: 1.5, Today); // 90 min available

        Assert.Equal(120, summary.RecommendedMinutesPerDay);
        Assert.Equal(90, summary.AvailableMinutesPerDay);
        Assert.True(summary.Behind); // 120 > 90
        Assert.Equal(2, summary.Exams.Count);
    }

    [Fact]
    public void BuildDashboard_NotBehind_When_Available_Enough()
    {
        var data = new List<(Exam, IReadOnlyList<Topic>)>
        {
            (new Exam { Id = 1, Name = "A", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(10)),
        };

        var summary = _svc.BuildDashboard(data, availableHoursPerDay: 2, Today); // 120 min

        Assert.Equal(60, summary.RecommendedMinutesPerDay);
        Assert.False(summary.Behind);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests --filter PlannerDashboardTest`
Expected: FAIL — `BuildDashboard` не найден.

- [ ] **Step 3: Add BuildDashboard to PlannerService**

Add this method inside the `PlannerService` class in `src/ExamPlanner.Core/Services/PlannerService.cs`:
```csharp
    public DashboardSummary BuildDashboard(
        IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data,
        double availableHoursPerDay,
        DateTime today)
    {
        var paces = data.Select(d => BuildPace(d.exam, d.topics, today)).ToList();
        var recommended = paces.Where(p => p.RemainingTopics > 0).Sum(p => p.NeedMinutesPerDay);
        var available = availableHoursPerDay * 60;
        return new DashboardSummary(paces, recommended, available, recommended > available);
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests --filter PlannerDashboardTest`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner.Core/Services/PlannerService.cs tests/ExamPlanner.Core.Tests/PlannerDashboardTest.cs
git commit -m "feat(core): add dashboard summary calculation"
```

---

### Task 5: PlannerService — daily plan

Дневной план «Сегодня»: по срочности набирает дневную квоту тем каждого экзамена в пределах доступного времени; помечает экзамены, чья квота не поместилась.

**Files:**
- Modify: `src/ExamPlanner.Core/Services/PlannerService.cs`
- Test: `tests/ExamPlanner.Core.Tests/PlannerDailyPlanTest.cs`

**Interfaces:**
- Consumes: `DaysLeft`, `TodayQuota` (Task 3), `PlanItem`, `DailyPlan` (Task 3).
- Produces on `PlannerService`:
  - `DailyPlan BuildDailyPlan(IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data, double availableHoursPerDay, DateTime today)`

- [ ] **Step 1: Write the failing test**

Create `tests/ExamPlanner.Core.Tests/PlannerDailyPlanTest.cs`:
```csharp
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class PlannerDailyPlanTest
{
    private static readonly DateTime Today = new(2026, 6, 1);
    private readonly PlannerService _svc = new();

    private static List<Topic> New(int examId, int total) =>
        Enumerable.Range(0, total)
            .Select(i => new Topic { Id = examId * 100 + i, ExamId = examId, Position = i, Title = $"E{examId}T{i}", Status = TopicStatus.NotStarted })
            .ToList();

    [Fact]
    public void BuildDailyPlan_Fills_Quota_By_Urgency_Within_Available()
    {
        // A due in 5 days: quota ceil(10/5)=2 topics * 30 = 60 min
        // B due in 5 days: same, 60 min. Available = 120 -> both quotas fit.
        var data = new List<(Exam, IReadOnlyList<Topic>)>
        {
            (new Exam { Id = 1, Name = "A", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(1, 10)),
            (new Exam { Id = 2, Name = "B", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(2, 10)),
        };

        var plan = _svc.BuildDailyPlan(data, availableHoursPerDay: 2, Today); // 120 min

        Assert.Equal(4, plan.Items.Count);       // 2 + 2
        Assert.Equal(120, plan.UsedMinutes);
        Assert.Empty(plan.NotFittedExamIds);
    }

    [Fact]
    public void BuildDailyPlan_Prioritizes_Earlier_Date_And_Flags_NotFitted()
    {
        // A due 3 Jun (earlier), B due 6 Jun. Available only 90 min.
        // A quota ceil(10/2)=5 topics*10=50; B quota ceil(10/5)=2*30=60.
        // Order: A first (50 min, used=50). B: 90-50=40 left -> only 1 topic (30), used=80; quota 2 not fully placed -> B not fitted.
        var data = new List<(Exam, IReadOnlyList<Topic>)>
        {
            (new Exam { Id = 2, Name = "B", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(2, 10)),
            (new Exam { Id = 1, Name = "A", Date = new DateTime(2026, 6, 3), MinutesPerTopic = 10 }, New(1, 10)),
        };

        var plan = _svc.BuildDailyPlan(data, availableHoursPerDay: 1.5, Today); // 90 min

        Assert.Equal(1, plan.Items[0].ExamId);          // A first (earlier date)
        Assert.Equal(6, plan.Items.Count);              // 5 from A + 1 from B
        Assert.Equal(80, plan.UsedMinutes);
        Assert.Contains(2, plan.NotFittedExamIds);      // B underfilled
        Assert.DoesNotContain(1, plan.NotFittedExamIds);
    }

    [Fact]
    public void BuildDailyPlan_ZeroAvailability_Flags_All_With_Quota()
    {
        var data = new List<(Exam, IReadOnlyList<Topic>)>
        {
            (new Exam { Id = 1, Name = "A", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(1, 10)),
        };

        var plan = _svc.BuildDailyPlan(data, availableHoursPerDay: 0, Today);

        Assert.Empty(plan.Items);
        Assert.Equal(0, plan.UsedMinutes);
        Assert.Contains(1, plan.NotFittedExamIds);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests --filter PlannerDailyPlanTest`
Expected: FAIL — `BuildDailyPlan` не найден.

- [ ] **Step 3: Add BuildDailyPlan to PlannerService**

Add this method inside the `PlannerService` class in `src/ExamPlanner.Core/Services/PlannerService.cs`:
```csharp
    public DailyPlan BuildDailyPlan(
        IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data,
        double availableHoursPerDay,
        DateTime today)
    {
        var available = availableHoursPerDay * 60;
        var items = new List<PlanItem>();
        var notFitted = new List<int>();
        double used = 0;

        var ordered = data.OrderBy(d => d.exam.Date).ToList();
        foreach (var (exam, topics) in ordered)
        {
            var remaining = topics.Where(t => t.Status != TopicStatus.Done)
                                   .OrderBy(t => t.Position)
                                   .ToList();
            var quota = TodayQuota(remaining.Count, DaysLeft(exam.Date, today));
            if (quota == 0) continue;

            var placed = 0;
            foreach (var topic in remaining.Take(quota))
            {
                if (used + exam.MinutesPerTopic <= available)
                {
                    items.Add(new PlanItem(exam.Id, exam.Name, topic.Id, topic.Title, exam.MinutesPerTopic));
                    used += exam.MinutesPerTopic;
                    placed++;
                }
                else break;
            }

            if (placed < quota) notFitted.Add(exam.Id);
        }

        return new DailyPlan(items, used, available, notFitted);
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `"$DOTNET" test tests/ExamPlanner.Core.Tests`
Expected: PASS (all Core tests green).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner.Core/Services/PlannerService.cs tests/ExamPlanner.Core.Tests/PlannerDailyPlanTest.cs
git commit -m "feat(core): add daily plan generation"
```

---

### Task 6: MAUI app — workload, project, DI wiring

Ставит MAUI-ворклоад, создаёт Android-приложение, подключает Core, настраивает DI и Shell-навигацию с пустой заглушкой Dashboard. Деливери: приложение запускается на Android-эмуляторе.

> **Окружение:** этот и последующие таски требуют MAUI-ворклоада и Android SDK/эмулятора. Проще всего — установить в Visual Studio 2026 Installer компонент **«.NET Multi-platform App UI development»** (даёт MAUI + Android SDK + эмулятор). Альтернатива из CLI: `"$DOTNET" workload install maui-android` (Android SDK потребуется отдельно).

**Files:**
- Create: `src/ExamPlanner/` (проект `dotnet new maui`), затем правки:
- Modify: `src/ExamPlanner/ExamPlanner.csproj` (TargetFrameworks → только android; ссылка на Core; пакет MVVM)
- Modify: `src/ExamPlanner/MauiProgram.cs` (DI)
- Create: `src/ExamPlanner/AppShell.xaml` + `.cs` (маршруты) — заменяют шаблонные
- Create: `src/ExamPlanner/Views/DashboardPage.xaml` + `.cs` (заглушка)
- Modify: `project-1.slnx` (добавить проект)

**Interfaces:**
- Consumes: `IPlannerRepository`, `SqlitePlannerRepository`, `PlannerService` from Core.
- Produces: DI-контейнер с зарегистрированными `IPlannerRepository` (singleton, путь `FileSystem.AppDataDirectory/examplanner.db3`), `PlannerService`, страницами и VM; Shell с маршрутами `//dashboard`, `//today`, `//settings`, `exam`, `editexam`, `edittopic`.

- [ ] **Step 1: Install MAUI workload**

Run:
```bash
"$DOTNET" workload install maui-android
```
Expected: `Successfully installed workload(s) maui-android.` (или уже установлено).

- [ ] **Step 2: Create the MAUI project**

Run:
```bash
"$DOTNET" new maui -n ExamPlanner -o src/ExamPlanner
```

- [ ] **Step 3: Trim target frameworks to Android and add references**

Replace the `<TargetFrameworks>` line in `src/ExamPlanner/ExamPlanner.csproj` with:
```xml
    <TargetFrameworks>net8.0-android</TargetFrameworks>
```
Then run:
```bash
"$DOTNET" add src/ExamPlanner reference src/ExamPlanner.Core
"$DOTNET" add src/ExamPlanner package CommunityToolkit.Mvvm --version 8.2.2
```

- [ ] **Step 4: Wire DI in MauiProgram.cs**

Replace `src/ExamPlanner/MauiProgram.cs` with:
```csharp
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Services;
using ExamPlanner.Views;
using Microsoft.Extensions.Logging;

namespace ExamPlanner;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "examplanner.db3");
        builder.Services.AddSingleton<IPlannerRepository>(_ => new SqlitePlannerRepository(dbPath));
        builder.Services.AddSingleton<PlannerService>();

        builder.Services.AddTransient<DashboardPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
```

> Note: для MVP используем только `CommunityToolkit.Mvvm` (без `CommunityToolkit.Maui`). Если шаблон где-то добавил `using CommunityToolkit.Maui;` или `.UseMauiCommunityToolkit()` — убери их. Оставляй только реально используемые using.

- [ ] **Step 5: Create AppShell with routes**

Replace `src/ExamPlanner/AppShell.xaml` with:
```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="ExamPlanner.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:views="clr-namespace:ExamPlanner.Views"
    Title="ExamPlanner">

    <TabBar>
        <ShellContent Title="Экзамены" Route="dashboard" ContentTemplate="{DataTemplate views:DashboardPage}" />
    </TabBar>
</Shell>
```

Replace `src/ExamPlanner/AppShell.xaml.cs` with:
```csharp
namespace ExamPlanner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 6: Create the placeholder DashboardPage**

Create `src/ExamPlanner/Views/DashboardPage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="ExamPlanner.Views.DashboardPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    Title="Экзамены">
    <VerticalStackLayout Padding="16">
        <Label Text="ExamPlanner работает" FontSize="20" />
    </VerticalStackLayout>
</ContentPage>
```

Create `src/ExamPlanner/Views/DashboardPage.xaml.cs`:
```csharp
namespace ExamPlanner.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 7: Register the project in the .slnx**

Add to the `/src/` folder in `project-1.slnx`:
```xml
    <Project Path="src/ExamPlanner/ExamPlanner.csproj" />
```

- [ ] **Step 8: Build and run on Android emulator**

Run (build only, to confirm it compiles):
```bash
"$DOTNET" build src/ExamPlanner -f net8.0-android
```
Expected: `Build succeeded`.

Then deploy to a running Android emulator (start one from VS 2026 Device Manager first), or launch from Visual Studio (F5). Verify the app opens and shows «ExamPlanner работает».

- [ ] **Step 9: Commit**

```bash
git add src/ExamPlanner project-1.slnx
git commit -m "feat(app): scaffold MAUI Android app with DI and shell"
```

---

### Task 7: Dashboard screen

Показывает список экзаменов (прогресс, срочность) и сводку по времени. Кнопка добавления экзамена и переход в детали.

**Files:**
- Create: `src/ExamPlanner/ViewModels/DashboardViewModel.cs`
- Modify: `src/ExamPlanner/Views/DashboardPage.xaml` + `.cs`
- Modify: `src/ExamPlanner/MauiProgram.cs` (регистрация VM)
- Modify: `src/ExamPlanner/AppShell.xaml.cs` (регистрация маршрутов детальных страниц — добавится в Task 8/9)

**Interfaces:**
- Consumes: `IPlannerRepository`, `PlannerService`, `DashboardSummary`, `ExamPace`.
- Produces: `DashboardViewModel` with `ObservableCollection<ExamPace> Exams`, `string SummaryText`, `bool Behind`, `Task LoadAsync()`, commands `AddExamCommand`, `OpenExamCommand(ExamPace)`.

- [ ] **Step 1: Create DashboardViewModel**

Create `src/ExamPlanner/ViewModels/DashboardViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IPlannerRepository _repo;
    private readonly PlannerService _planner;

    public ObservableCollection<ExamPace> Exams { get; } = new();

    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private bool _behind;

    public DashboardViewModel(IPlannerRepository repo, PlannerService planner)
    {
        _repo = repo;
        _planner = planner;
    }

    public async Task LoadAsync()
    {
        var exams = await _repo.GetExamsAsync();
        var data = new List<(Exam, IReadOnlyList<Topic>)>();
        foreach (var exam in exams)
            data.Add((exam, await _repo.GetTopicsAsync(exam.Id)));

        var settings = await _repo.GetSettingsAsync();
        var summary = _planner.BuildDashboard(data, settings.AvailableHoursPerDay, DateTime.Now);

        Exams.Clear();
        foreach (var pace in summary.Exams)
            Exams.Add(pace);

        Behind = summary.Behind;
        SummaryText = $"Рекомендуем сегодня ≈ {summary.RecommendedMinutesPerDay / 60:0.0} ч · у тебя {summary.AvailableMinutesPerDay / 60:0.0} ч";
    }

    [RelayCommand]
    private async Task AddExam()
        => await Shell.Current.GoToAsync("editexam");

    [RelayCommand]
    private async Task OpenExam(ExamPace pace)
        => await Shell.Current.GoToAsync($"exam?examId={pace.ExamId}");
}
```

- [ ] **Step 2: Update DashboardPage XAML to bind the VM**

Replace `src/ExamPlanner/Views/DashboardPage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="ExamPlanner.Views.DashboardPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:ExamPlanner.ViewModels"
    xmlns:svc="clr-namespace:ExamPlanner.Core.Services;assembly=ExamPlanner.Core"
    x:DataType="vm:DashboardViewModel"
    Title="Экзамены">
    <Grid RowDefinitions="Auto,*" Padding="16" RowSpacing="12">
        <VerticalStackLayout Grid.Row="0" Spacing="4">
            <Label Text="{Binding SummaryText}" FontSize="16" />
            <Label Text="Не успеваешь при текущей нагрузке!" TextColor="Red"
                   IsVisible="{Binding Behind}" FontAttributes="Bold" />
        </VerticalStackLayout>

        <CollectionView Grid.Row="1" ItemsSource="{Binding Exams}">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="svc:ExamPace">
                    <Grid Padding="8" ColumnDefinitions="*,Auto">
                        <VerticalStackLayout Grid.Column="0">
                            <Label Text="{Binding Name}" FontSize="18" FontAttributes="Bold" />
                            <Label Text="{Binding Date, StringFormat='до {0:dd.MM.yyyy}'}" FontSize="13" />
                            <Label FontSize="13"
                                   Text="{Binding RemainingTopics, StringFormat='осталось {0} тем'}" />
                        </VerticalStackLayout>
                        <Label Grid.Column="1" VerticalOptions="Center" TextColor="Red"
                               Text="🔥" IsVisible="{Binding IsUrgent}" />
                        <Grid.GestureRecognizers>
                            <TapGestureRecognizer
                                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:DashboardViewModel}}, Path=OpenExamCommand}"
                                CommandParameter="{Binding .}" />
                        </Grid.GestureRecognizers>
                    </Grid>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

        <Grid.ToolbarItems>
            <ToolbarItem Text="＋" Command="{Binding AddExamCommand}" />
        </Grid.ToolbarItems>
    </Grid>
</ContentPage>
```

- [ ] **Step 3: Wire VM into the page code-behind**

Replace `src/ExamPlanner/Views/DashboardPage.xaml.cs`:
```csharp
using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
```

- [ ] **Step 4: Register the VM in DI**

In `src/ExamPlanner/MauiProgram.cs`, add after the `AddTransient<DashboardPage>()` line:
```csharp
        builder.Services.AddTransient<DashboardViewModel>();
```

- [ ] **Step 5: Build, run, verify**

Run:
```bash
"$DOTNET" build src/ExamPlanner -f net8.0-android
```
Expected: `Build succeeded`. Deploy to emulator: Dashboard shows the summary line and an (empty) list with a «＋» toolbar button. (Adding exams comes in Task 8.)

- [ ] **Step 6: Commit**

```bash
git add src/ExamPlanner/ViewModels/DashboardViewModel.cs src/ExamPlanner/Views/DashboardPage.xaml src/ExamPlanner/Views/DashboardPage.xaml.cs src/ExamPlanner/MauiProgram.cs
git commit -m "feat(app): dashboard screen with exam list and time summary"
```

---

### Task 8: Add/Edit Exam screen

Форма создания и редактирования экзамена: имя, дата, среднее время на тему. Регистрирует маршрут `editexam`.

**Files:**
- Create: `src/ExamPlanner/ViewModels/EditExamViewModel.cs`
- Create: `src/ExamPlanner/Views/EditExamPage.xaml` + `.cs`
- Modify: `src/ExamPlanner/MauiProgram.cs` (регистрация)
- Modify: `src/ExamPlanner/AppShell.xaml.cs` (маршрут `editexam`)

**Interfaces:**
- Consumes: `IPlannerRepository`, `Exam`.
- Produces: `EditExamViewModel` with query property `int ExamId` (0 = новый), `string Name`, `DateTime Date`, `int MinutesPerTopic`, `Task LoadAsync()`, `SaveCommand`, `DeleteCommand`. Route `editexam` (+ optional `?examId=`).

- [ ] **Step 1: Create EditExamViewModel**

Create `src/ExamPlanner/ViewModels/EditExamViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;

namespace ExamPlanner.ViewModels;

[QueryProperty(nameof(ExamId), "examId")]
public partial class EditExamViewModel : ObservableObject
{
    private readonly IPlannerRepository _repo;

    [ObservableProperty] private int _examId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Today.AddDays(7);
    [ObservableProperty] private int _minutesPerTopic = 30;
    [ObservableProperty] private bool _isExisting;

    public EditExamViewModel(IPlannerRepository repo) => _repo = repo;

    partial void OnExamIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        if (ExamId == 0) { IsExisting = false; return; }
        var exam = await _repo.GetExamAsync(ExamId);
        if (exam == null) return;
        Name = exam.Name;
        Date = exam.Date;
        MinutesPerTopic = exam.MinutesPerTopic;
        IsExisting = true;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        await _repo.SaveExamAsync(new Exam
        {
            Id = ExamId,
            Name = Name.Trim(),
            Date = Date,
            MinutesPerTopic = MinutesPerTopic <= 0 ? 30 : MinutesPerTopic
        });
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (ExamId != 0)
            await _repo.DeleteExamAsync(ExamId);
        await Shell.Current.GoToAsync("..");
    }
}
```

- [ ] **Step 2: Create EditExamPage**

Create `src/ExamPlanner/Views/EditExamPage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="ExamPlanner.Views.EditExamPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:ExamPlanner.ViewModels"
    x:DataType="vm:EditExamViewModel"
    Title="Экзамен">
    <VerticalStackLayout Padding="16" Spacing="12">
        <Label Text="Название" />
        <Entry Text="{Binding Name}" Placeholder="Например, Матанализ" />
        <Label Text="Дата экзамена" />
        <DatePicker Date="{Binding Date}" Format="dd.MM.yyyy" />
        <Label Text="Среднее время на тему (мин)" />
        <Entry Text="{Binding MinutesPerTopic}" Keyboard="Numeric" />
        <Button Text="Сохранить" Command="{Binding SaveCommand}" />
        <Button Text="Удалить" Command="{Binding DeleteCommand}"
                IsVisible="{Binding IsExisting}" BackgroundColor="DarkRed" />
    </VerticalStackLayout>
</ContentPage>
```

Create `src/ExamPlanner/Views/EditExamPage.xaml.cs`:
```csharp
using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class EditExamPage : ContentPage
{
    public EditExamPage(EditExamViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
```

- [ ] **Step 3: Register DI + route**

In `src/ExamPlanner/MauiProgram.cs`, add:
```csharp
        builder.Services.AddTransient<EditExamPage>();
        builder.Services.AddTransient<EditExamViewModel>();
```

In `src/ExamPlanner/AppShell.xaml.cs`, register the route inside the constructor after `InitializeComponent();`:
```csharp
        Routing.RegisterRoute("editexam", typeof(Views.EditExamPage));
```

- [ ] **Step 4: Build, run, verify**

Run: `"$DOTNET" build src/ExamPlanner -f net8.0-android`
Expected: `Build succeeded`. On the emulator: tap «＋» on Dashboard → form opens → fill name/date/minutes → Save → returns to Dashboard and the exam appears in the list.

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner/ViewModels/EditExamViewModel.cs src/ExamPlanner/Views/EditExamPage.xaml src/ExamPlanner/Views/EditExamPage.xaml.cs src/ExamPlanner/MauiProgram.cs src/ExamPlanner/AppShell.xaml.cs
git commit -m "feat(app): add/edit/delete exam screen"
```

---

### Task 9: Exam detail screen (topics + status cycling)

Список тем экзамена со статусами; тап по теме циклически меняет статус (Не начата → В процессе → Пройдена → …); добавление/удаление тем; строка темпа. Регистрирует маршрут `exam`.

**Files:**
- Create: `src/ExamPlanner/ViewModels/ExamDetailViewModel.cs`
- Create: `src/ExamPlanner/Views/ExamDetailPage.xaml` + `.cs`
- Modify: `src/ExamPlanner/MauiProgram.cs`, `src/ExamPlanner/AppShell.xaml.cs`

**Interfaces:**
- Consumes: `IPlannerRepository`, `PlannerService`, `Topic`, `TopicStatus`, `ExamPace`.
- Produces: `ExamDetailViewModel` with query `int ExamId`, `ObservableCollection<Topic> Topics`, `string PaceText`, `string NewTopicTitle`, `Task LoadAsync()`, commands `AddTopicCommand`, `CycleStatusCommand(Topic)`, `DeleteTopicCommand(Topic)`, `EditExamCommand`. Route `exam?examId=`.

- [ ] **Step 1: Create ExamDetailViewModel**

Create `src/ExamPlanner/ViewModels/ExamDetailViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.ViewModels;

[QueryProperty(nameof(ExamId), "examId")]
public partial class ExamDetailViewModel : ObservableObject
{
    private readonly IPlannerRepository _repo;
    private readonly PlannerService _planner;

    [ObservableProperty] private int _examId;
    [ObservableProperty] private string _title = "Экзамен";
    [ObservableProperty] private string _paceText = string.Empty;
    [ObservableProperty] private string _newTopicTitle = string.Empty;

    public ObservableCollection<Topic> Topics { get; } = new();

    public ExamDetailViewModel(IPlannerRepository repo, PlannerService planner)
    {
        _repo = repo;
        _planner = planner;
    }

    partial void OnExamIdChanged(int value) => _ = LoadAsync();

    public async Task LoadAsync()
    {
        var exam = await _repo.GetExamAsync(ExamId);
        if (exam == null) return;
        Title = exam.Name;

        var topics = await _repo.GetTopicsAsync(ExamId);
        Topics.Clear();
        foreach (var t in topics) Topics.Add(t);

        var pace = _planner.BuildPace(exam, topics, DateTime.Now);
        PaceText = pace.RemainingTopics == 0
            ? "Все темы пройдены 🎉"
            : $"Осталось {pace.RemainingTopics} тем, {pace.DaysLeft} дн → ~{pace.TodayQuota} тем/день";
    }

    [RelayCommand]
    private async Task AddTopic()
    {
        if (string.IsNullOrWhiteSpace(NewTopicTitle)) return;
        await _repo.SaveTopicAsync(new Topic
        {
            ExamId = ExamId,
            Title = NewTopicTitle.Trim(),
            Status = TopicStatus.NotStarted,
            Position = Topics.Count
        });
        NewTopicTitle = string.Empty;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task CycleStatus(Topic topic)
    {
        topic.Status = topic.Status switch
        {
            TopicStatus.NotStarted => TopicStatus.InProgress,
            TopicStatus.InProgress => TopicStatus.Done,
            _ => TopicStatus.NotStarted
        };
        await _repo.SaveTopicAsync(topic);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteTopic(Topic topic)
    {
        await _repo.DeleteTopicAsync(topic.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task EditExam()
        => await Shell.Current.GoToAsync($"editexam?examId={ExamId}");
}
```

- [ ] **Step 2: Create ExamDetailPage**

Create `src/ExamPlanner/Views/ExamDetailPage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="ExamPlanner.Views.ExamDetailPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:ExamPlanner.ViewModels"
    xmlns:models="clr-namespace:ExamPlanner.Core.Models;assembly=ExamPlanner.Core"
    x:DataType="vm:ExamDetailViewModel"
    Title="{Binding Title}">
    <Grid RowDefinitions="Auto,Auto,*" Padding="16" RowSpacing="10">
        <Label Grid.Row="0" Text="{Binding PaceText}" FontSize="15" FontAttributes="Bold" />

        <Grid Grid.Row="1" ColumnDefinitions="*,Auto" ColumnSpacing="8">
            <Entry Grid.Column="0" Text="{Binding NewTopicTitle}" Placeholder="Новая тема" />
            <Button Grid.Column="1" Text="Добавить" Command="{Binding AddTopicCommand}" />
        </Grid>

        <CollectionView Grid.Row="2" ItemsSource="{Binding Topics}">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:Topic">
                    <SwipeView>
                        <SwipeView.RightItems>
                            <SwipeItems>
                                <SwipeItem Text="Удалить" BackgroundColor="DarkRed"
                                           Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ExamDetailViewModel}}, Path=DeleteTopicCommand}"
                                           CommandParameter="{Binding .}" />
                            </SwipeItems>
                        </SwipeView.RightItems>
                        <Grid Padding="10" ColumnDefinitions="Auto,*">
                            <Label Grid.Column="0" FontSize="18" Text="{Binding Status, Converter={StaticResource StatusToIcon}}" />
                            <Label Grid.Column="1" Margin="10,0,0,0" VerticalOptions="Center"
                                   Text="{Binding Title}" FontSize="16" />
                            <Grid.GestureRecognizers>
                                <TapGestureRecognizer
                                    Command="{Binding Source={RelativeSource AncestorType={x:Type vm:ExamDetailViewModel}}, Path=CycleStatusCommand}"
                                    CommandParameter="{Binding .}" />
                            </Grid.GestureRecognizers>
                        </Grid>
                    </SwipeView>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>

        <ContentPage.ToolbarItems>
            <ToolbarItem Text="✎" Command="{Binding EditExamCommand}" />
        </ContentPage.ToolbarItems>
    </Grid>
</ContentPage>
```

Create `src/ExamPlanner/Views/ExamDetailPage.xaml.cs`:
```csharp
using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class ExamDetailPage : ContentPage
{
    private readonly ExamDetailViewModel _vm;

    public ExamDetailPage(ExamDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
```

- [ ] **Step 3: Add the status→icon converter**

Create `src/ExamPlanner/Converters/StatusToIconConverter.cs`:
```csharp
using System.Globalization;
using ExamPlanner.Core.Models;

namespace ExamPlanner.Converters;

public class StatusToIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is TopicStatus s
            ? s switch
            {
                TopicStatus.Done => "✅",
                TopicStatus.InProgress => "🟡",
                _ => "⬜"
            }
            : "⬜";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
```

Register it in `src/ExamPlanner/App.xaml` inside `<Application.Resources><ResourceDictionary>`:
```xml
        <converters:StatusToIconConverter x:Key="StatusToIcon"
            xmlns:converters="clr-namespace:ExamPlanner.Converters" />
```

- [ ] **Step 4: Register DI + route**

In `src/ExamPlanner/MauiProgram.cs`:
```csharp
        builder.Services.AddTransient<ExamDetailPage>();
        builder.Services.AddTransient<ExamDetailViewModel>();
```
In `src/ExamPlanner/AppShell.xaml.cs`:
```csharp
        Routing.RegisterRoute("exam", typeof(Views.ExamDetailPage));
```

- [ ] **Step 5: Build, run, verify**

Run: `"$DOTNET" build src/ExamPlanner -f net8.0-android`
Expected: `Build succeeded`. On emulator: open an exam → add topics → tap a topic to cycle ⬜→🟡→✅ → pace line updates → swipe to delete. Return to Dashboard shows updated «осталось N тем».

- [ ] **Step 6: Commit**

```bash
git add src/ExamPlanner/ViewModels/ExamDetailViewModel.cs src/ExamPlanner/Views/ExamDetailPage.xaml src/ExamPlanner/Views/ExamDetailPage.xaml.cs src/ExamPlanner/Converters/StatusToIconConverter.cs src/ExamPlanner/App.xaml src/ExamPlanner/MauiProgram.cs src/ExamPlanner/AppShell.xaml.cs
git commit -m "feat(app): exam detail with topics and status cycling"
```

---

### Task 10: Today screen (daily plan)

Экран «Сегодня»: сгенерированный дневной план по срочности; отметка темы «Пройдена» из плана. Добавляет вкладку `today`.

**Files:**
- Create: `src/ExamPlanner/ViewModels/TodayViewModel.cs`
- Create: `src/ExamPlanner/Views/TodayPage.xaml` + `.cs`
- Modify: `src/ExamPlanner/MauiProgram.cs`, `src/ExamPlanner/AppShell.xaml`

**Interfaces:**
- Consumes: `IPlannerRepository`, `PlannerService`, `DailyPlan`, `PlanItem`, `Topic`, `TopicStatus`.
- Produces: `TodayViewModel` with `ObservableCollection<PlanItem> Items`, `string HeaderText`, `bool HasWarning`, `Task LoadAsync()`, `MarkDoneCommand(PlanItem)`. Tab route `today`.

- [ ] **Step 1: Create TodayViewModel**

Create `src/ExamPlanner/ViewModels/TodayViewModel.cs`:
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.ViewModels;

public partial class TodayViewModel : ObservableObject
{
    private readonly IPlannerRepository _repo;
    private readonly PlannerService _planner;

    public ObservableCollection<PlanItem> Items { get; } = new();

    [ObservableProperty] private string _headerText = string.Empty;
    [ObservableProperty] private bool _hasWarning;

    public TodayViewModel(IPlannerRepository repo, PlannerService planner)
    {
        _repo = repo;
        _planner = planner;
    }

    public async Task LoadAsync()
    {
        var exams = await _repo.GetExamsAsync();
        var data = new List<(Exam, IReadOnlyList<Topic>)>();
        foreach (var exam in exams)
            data.Add((exam, await _repo.GetTopicsAsync(exam.Id)));

        var settings = await _repo.GetSettingsAsync();
        var plan = _planner.BuildDailyPlan(data, settings.AvailableHoursPerDay, DateTime.Now);

        Items.Clear();
        foreach (var item in plan.Items) Items.Add(item);

        HeaderText = $"План на сегодня: {plan.Items.Count} тем · {plan.UsedMinutes / 60:0.0} из {plan.AvailableMinutes / 60:0.0} ч";
        HasWarning = plan.NotFittedExamIds.Count > 0;
    }

    [RelayCommand]
    private async Task MarkDone(PlanItem item)
    {
        var topic = (await _repo.GetTopicsAsync(item.ExamId)).FirstOrDefault(t => t.Id == item.TopicId);
        if (topic != null)
        {
            topic.Status = TopicStatus.Done;
            await _repo.SaveTopicAsync(topic);
        }
        await LoadAsync();
    }
}
```

- [ ] **Step 2: Create TodayPage**

Create `src/ExamPlanner/Views/TodayPage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="ExamPlanner.Views.TodayPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:ExamPlanner.ViewModels"
    xmlns:svc="clr-namespace:ExamPlanner.Core.Services;assembly=ExamPlanner.Core"
    x:DataType="vm:TodayViewModel"
    Title="Сегодня">
    <Grid RowDefinitions="Auto,Auto,*" Padding="16" RowSpacing="10">
        <Label Grid.Row="0" Text="{Binding HeaderText}" FontSize="16" FontAttributes="Bold" />
        <Label Grid.Row="1" Text="Не всё вмещается в доступное время — часть тем перенесена."
               TextColor="OrangeRed" IsVisible="{Binding HasWarning}" />
        <CollectionView Grid.Row="2" ItemsSource="{Binding Items}">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="svc:PlanItem">
                    <Grid Padding="10" ColumnDefinitions="*,Auto">
                        <VerticalStackLayout Grid.Column="0">
                            <Label Text="{Binding TopicTitle}" FontSize="16" />
                            <Label FontSize="12" TextColor="Gray"
                                   Text="{Binding ExamName, StringFormat='{0} · '}" />
                        </VerticalStackLayout>
                        <Button Grid.Column="1" Text="Готово"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:TodayViewModel}}, Path=MarkDoneCommand}"
                                CommandParameter="{Binding .}" />
                    </Grid>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
    </Grid>
</ContentPage>
```

Create `src/ExamPlanner/Views/TodayPage.xaml.cs`:
```csharp
using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _vm;

    public TodayPage(TodayViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
```

- [ ] **Step 3: Add the tab + DI**

In `src/ExamPlanner/AppShell.xaml`, add inside `<TabBar>`:
```xml
        <ShellContent Title="Сегодня" Route="today" ContentTemplate="{DataTemplate views:TodayPage}" />
```

In `src/ExamPlanner/MauiProgram.cs`:
```csharp
        builder.Services.AddTransient<TodayPage>();
        builder.Services.AddTransient<TodayViewModel>();
```

- [ ] **Step 4: Build, run, verify**

Run: `"$DOTNET" build src/ExamPlanner -f net8.0-android`
Expected: `Build succeeded`. On emulator: the «Сегодня» tab lists today's topics ordered by exam urgency; tapping «Готово» marks the topic Done and it drops off the plan; warning shows when availability is too low.

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner/ViewModels/TodayViewModel.cs src/ExamPlanner/Views/TodayPage.xaml src/ExamPlanner/Views/TodayPage.xaml.cs src/ExamPlanner/AppShell.xaml src/ExamPlanner/MauiProgram.cs
git commit -m "feat(app): today screen with generated daily plan"
```

---

### Task 11: Settings screen (availability)

Экран настроек: доступность (часов в день). Влияет на рекомендацию и дневной план. Добавляет вкладку `settings`.

**Files:**
- Create: `src/ExamPlanner/ViewModels/SettingsViewModel.cs`
- Create: `src/ExamPlanner/Views/SettingsPage.xaml` + `.cs`
- Modify: `src/ExamPlanner/MauiProgram.cs`, `src/ExamPlanner/AppShell.xaml`

**Interfaces:**
- Consumes: `IPlannerRepository`, `AppSettings`.
- Produces: `SettingsViewModel` with `double AvailableHoursPerDay`, `Task LoadAsync()`, `SaveCommand`. Tab route `settings`.

- [ ] **Step 1: Create SettingsViewModel**

Create `src/ExamPlanner/ViewModels/SettingsViewModel.cs`:
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;

namespace ExamPlanner.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IPlannerRepository _repo;

    [ObservableProperty] private double _availableHoursPerDay = 2;
    [ObservableProperty] private string _statusText = string.Empty;

    public SettingsViewModel(IPlannerRepository repo) => _repo = repo;

    public async Task LoadAsync()
    {
        var settings = await _repo.GetSettingsAsync();
        AvailableHoursPerDay = settings.AvailableHoursPerDay;
    }

    [RelayCommand]
    private async Task Save()
    {
        var hours = AvailableHoursPerDay < 0 ? 0 : AvailableHoursPerDay;
        await _repo.SaveSettingsAsync(new AppSettings { Id = 1, AvailableHoursPerDay = hours });
        StatusText = "Сохранено ✓";
    }
}
```

- [ ] **Step 2: Create SettingsPage**

Create `src/ExamPlanner/Views/SettingsPage.xaml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="ExamPlanner.Views.SettingsPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:ExamPlanner.ViewModels"
    x:DataType="vm:SettingsViewModel"
    Title="Настройки">
    <VerticalStackLayout Padding="16" Spacing="12">
        <Label Text="Сколько часов в день можешь заниматься" />
        <Stepper Minimum="0" Maximum="16" Increment="0.5"
                 Value="{Binding AvailableHoursPerDay}" />
        <Label Text="{Binding AvailableHoursPerDay, StringFormat='{0:0.0} ч/день'}" FontSize="18" />
        <Button Text="Сохранить" Command="{Binding SaveCommand}" />
        <Label Text="{Binding StatusText}" TextColor="Green" />
    </VerticalStackLayout>
</ContentPage>
```

Create `src/ExamPlanner/Views/SettingsPage.xaml.cs`:
```csharp
using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
```

- [ ] **Step 3: Add tab + DI**

In `src/ExamPlanner/AppShell.xaml`, add inside `<TabBar>`:
```xml
        <ShellContent Title="Настройки" Route="settings" ContentTemplate="{DataTemplate views:SettingsPage}" />
```
In `src/ExamPlanner/MauiProgram.cs`:
```csharp
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<SettingsViewModel>();
```

- [ ] **Step 4: Build, run, verify end-to-end**

Run: `"$DOTNET" build src/ExamPlanner -f net8.0-android`
Expected: `Build succeeded`. Full manual pass on emulator:
1. Settings → set 2.5 ч → Save.
2. Add exam «Матан», дата +5 дней, 30 мин/тема.
3. Open exam → add 10 topics.
4. Dashboard → summary reflects recommended vs available; urgent flag if near date.
5. Today → shows the daily quota ordered by urgency; mark a couple «Готово».
6. Reopen app → data persists (SQLite).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner/ViewModels/SettingsViewModel.cs src/ExamPlanner/Views/SettingsPage.xaml src/ExamPlanner/Views/SettingsPage.xaml.cs src/ExamPlanner/AppShell.xaml src/ExamPlanner/MauiProgram.cs
git commit -m "feat(app): settings screen for daily availability"
```

---

## Final verification

- [ ] Run full Core test suite: `"$DOTNET" test tests/ExamPlanner.Core.Tests` → all green.
- [ ] Build the app: `"$DOTNET" build src/ExamPlanner -f net8.0-android` → succeeds.
- [ ] Manual end-to-end pass from Task 11 Step 4 on the emulator.
- [ ] Confirm the MVP scope from the spec is covered; deferred features remain in the spec roadmap.
