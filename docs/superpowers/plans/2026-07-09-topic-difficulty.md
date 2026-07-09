# Topic Difficulty Time-Estimation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Дать каждой теме уровень сложности (Лёгкий/Средний/Тяжёлый) и учитывать его во времени: оценка и дневной план становятся взвешенными по сложности, число тем в день — без изменений.

**Architecture:** Чистое расширение `ExamPlanner.Core`. Новый enum `TopicDifficulty` и поле `Topic.Difficulty` (дефолт `Medium`); `PlannerService` получает `EffectiveMinutes(topic, exam) = round(exam.MinutesPerTopic × множитель)` и использует её в `BuildPace` (need) и `BuildDailyPlan` (минуты). `TodayQuota` и `BuildDashboard` не меняются. UI не входит.

**Tech Stack:** .NET 8 · sqlite-net-pcl · xUnit.

## Global Constraints

- Только проект `ExamPlanner.Core` и его тесты `tests/ExamPlanner.Core.Tests`. **Не трогать** `src/ExamPlanner` (MAUI UI — зона frontend-агента).
- Репозиторий: `E:\Projects\project-1` (ASCII-путь обязателен). `dotnet` вызывать полным путём: `"C:\Program Files\dotnet\dotnet.exe"` (в PATH его нет у Bash).
- Работать в ветке `feature/topic-difficulty` (не коммитить в `main`). Идентичность коммитов: `22504105 <universgroup13@gmail.com>`.
- Коэффициенты сложности фиксированы: Лёгкий 0.5 · Средний 1.0 · Тяжёлый 1.5. Округление эффективных минут — `Math.Round(x, MidpointRounding.AwayFromZero)`, результат `int`.
- `TopicDifficulty.Medium = 0` — обязательно, для бесшовной миграции (старые строки БД получают 0 = Средний).
- Обратная совместимость: существующие 14 тестов должны остаться зелёными без правок (все темы по умолчанию `Medium` ×1.0 → те же числа).
- Публичные сигнатуры/record-типы (`ExamPace`, `PlanItem`, `DailyPlan`, `DashboardSummary`) не меняются.
- TDD: сначала падающий тест, потом минимальная реализация. Частые коммиты.

**Setup (один раз перед Task 1):**
```bash
cd "E:/Projects/project-1"
git switch -c feature/topic-difficulty
```

---

### Task 1: Модель сложности — enum + поле Topic.Difficulty

Вводит `TopicDifficulty` (с `Medium = 0` для миграции) и поле `Topic.Difficulty` (дефолт `Medium`). Проверяет значение по умолчанию и персистентность через SQLite-репозиторий (круговой рейс, заодно подтверждает, что новая колонка создаётся).

**Files:**
- Create: `src/ExamPlanner.Core/Models/TopicDifficulty.cs`
- Modify: `src/ExamPlanner.Core/Models/Topic.cs`
- Modify (test): `tests/ExamPlanner.Core.Tests/ModelsTest.cs`
- Modify (test): `tests/ExamPlanner.Core.Tests/RepositoryTest.cs`

**Interfaces:**
- Produces:
  - `enum TopicDifficulty { Medium = 0, Easy = 1, Hard = 2 }`
  - `Topic.Difficulty` типа `TopicDifficulty`, дефолт `TopicDifficulty.Medium`.

- [ ] **Step 1: Написать падающие тесты модели**

Добавить в конец класса `ModelsTest` в `tests/ExamPlanner.Core.Tests/ModelsTest.cs` (перед закрывающей `}` класса):
```csharp
    [Fact]
    public void TopicDifficulty_Has_Medium_As_Zero_For_Migration()
    {
        Assert.Equal(0, (int)TopicDifficulty.Medium);
        Assert.Equal(1, (int)TopicDifficulty.Easy);
        Assert.Equal(2, (int)TopicDifficulty.Hard);
    }

    [Fact]
    public void Topic_Defaults_To_Medium_Difficulty()
    {
        var topic = new Topic { Title = "Limits" };
        Assert.Equal(TopicDifficulty.Medium, topic.Difficulty);
    }
```

Добавить в конец класса `RepositoryTest` в `tests/ExamPlanner.Core.Tests/RepositoryTest.cs` (перед закрывающей `}` класса):
```csharp
    [Fact]
    public async Task SaveTopic_RoundTrips_Difficulty()
    {
        var examId = await _repo.SaveExamAsync(new Exam { Name = "Matan" });
        await _repo.SaveTopicAsync(new Topic { ExamId = examId, Title = "Series", Difficulty = TopicDifficulty.Hard });

        var topics = await _repo.GetTopicsAsync(examId);
        Assert.Single(topics);
        Assert.Equal(TopicDifficulty.Hard, topics[0].Difficulty);
    }
```

- [ ] **Step 2: Запустить тесты — убедиться, что падают**

Run:
```bash
"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests
```
Expected: FAIL — `TopicDifficulty` и `Topic.Difficulty` не существуют (ошибки компиляции).

- [ ] **Step 3: Создать enum**

Create `src/ExamPlanner.Core/Models/TopicDifficulty.cs`:
```csharp
namespace ExamPlanner.Core.Models;

public enum TopicDifficulty
{
    Medium = 0,
    Easy = 1,
    Hard = 2
}
```

- [ ] **Step 4: Добавить поле в Topic**

В `src/ExamPlanner.Core/Models/Topic.cs` добавить свойство `Difficulty` после `Position`:
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
    public TopicDifficulty Difficulty { get; set; } = TopicDifficulty.Medium;
}
```

- [ ] **Step 5: Запустить тесты — убедиться, что проходят**

Run:
```bash
"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests
```
Expected: PASS — все тесты зелёные (14 существующих + 3 новых = 17).

- [ ] **Step 6: Commit**

```bash
git add src/ExamPlanner.Core/Models/TopicDifficulty.cs src/ExamPlanner.Core/Models/Topic.cs tests/ExamPlanner.Core.Tests/ModelsTest.cs tests/ExamPlanner.Core.Tests/RepositoryTest.cs
git commit -m "feat(core): add TopicDifficulty enum and Topic.Difficulty field"
```

---

### Task 2: PlannerService.EffectiveMinutes — минуты по сложности

Добавляет расчёт эффективных минут на тему: `round(exam.MinutesPerTopic × множитель(difficulty))` с множителями 0.5/1.0/1.5 и округлением от нуля. Чистая функция, покрыта таблицей случаев.

**Files:**
- Modify: `src/ExamPlanner.Core/Services/PlannerService.cs`
- Create (test): `tests/ExamPlanner.Core.Tests/DifficultyTest.cs`

**Interfaces:**
- Consumes: `Topic.Difficulty` (Task 1), `Exam.MinutesPerTopic`.
- Produces on `PlannerService`: `int EffectiveMinutes(Topic topic, Exam exam)`.

- [ ] **Step 1: Написать падающий тест**

Create `tests/ExamPlanner.Core.Tests/DifficultyTest.cs`:
```csharp
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class DifficultyTest
{
    private readonly PlannerService _svc = new();

    [Theory]
    [InlineData(TopicDifficulty.Easy, 30, 15)]
    [InlineData(TopicDifficulty.Medium, 30, 30)]
    [InlineData(TopicDifficulty.Hard, 30, 45)]
    [InlineData(TopicDifficulty.Easy, 25, 13)]  // 12.5 -> 13 (AwayFromZero)
    [InlineData(TopicDifficulty.Hard, 25, 38)]  // 37.5 -> 38 (AwayFromZero)
    [InlineData(TopicDifficulty.Medium, 40, 40)]
    public void EffectiveMinutes_ScalesByDifficulty(TopicDifficulty difficulty, int basePerTopic, int expected)
    {
        var exam = new Exam { MinutesPerTopic = basePerTopic };
        var topic = new Topic { Difficulty = difficulty };
        Assert.Equal(expected, _svc.EffectiveMinutes(topic, exam));
    }
}
```

- [ ] **Step 2: Запустить — убедиться, что падает**

Run:
```bash
"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests --filter DifficultyTest
```
Expected: FAIL — `EffectiveMinutes` не существует (ошибка компиляции).

- [ ] **Step 3: Реализовать EffectiveMinutes**

В `src/ExamPlanner.Core/Services/PlannerService.cs` добавить внутри класса `PlannerService` (например, сразу после `TodayQuota`):
```csharp
    public int EffectiveMinutes(Topic topic, Exam exam)
        => (int)Math.Round(
            exam.MinutesPerTopic * Multiplier(topic.Difficulty),
            MidpointRounding.AwayFromZero);

    private static double Multiplier(TopicDifficulty difficulty)
        => difficulty switch
        {
            TopicDifficulty.Easy => 0.5,
            TopicDifficulty.Hard => 1.5,
            _ => 1.0
        };
```

- [ ] **Step 4: Запустить — убедиться, что проходит**

Run:
```bash
"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests --filter DifficultyTest
```
Expected: PASS (6 случаев Theory).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner.Core/Services/PlannerService.cs tests/ExamPlanner.Core.Tests/DifficultyTest.cs
git commit -m "feat(core): add PlannerService.EffectiveMinutes (difficulty multipliers)"
```

---

### Task 3: BuildPace — need взвешен по сложности

Меняет расчёт `NeedMinutesPerDay` в `BuildPace`: вместо `осталось × MinutesPerTopic ÷ дни` теперь сумма `EffectiveMinutes` по оставшимся темам ÷ дни. `RemainingTopics`, `TodayQuota`, `IsUrgent` — без изменений.

**Files:**
- Modify: `src/ExamPlanner.Core/Services/PlannerService.cs:15-25` (метод `BuildPace`)
- Modify (test): `tests/ExamPlanner.Core.Tests/DifficultyTest.cs`

**Interfaces:**
- Consumes: `EffectiveMinutes` (Task 2).
- Produces: без новых сигнатур — меняется только вычисление `ExamPace.NeedMinutesPerDay`.

- [ ] **Step 1: Написать падающие тесты**

Добавить в класс `DifficultyTest` в `tests/ExamPlanner.Core.Tests/DifficultyTest.cs` (перед закрывающей `}` класса). Данные подобраны так, чтобы взвешенная и старая плоская формулы давали РАЗНЫЕ числа — иначе тест не поймает регресс:
```csharp
    private static readonly DateTime Today = new(2026, 6, 1);

    [Fact]
    public void BuildPace_Need_Is_DifficultyWeighted()
    {
        // base 30, daysLeft = 6 Jun - 1 Jun = 5. Easy 15 + Easy 15 + Hard 45 = 75 -> weighted need = 75/5 = 15.
        // (old flat formula would give 3*30/5 = 18, so this distinguishes weighted vs flat.)
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, Position = 0, Difficulty = TopicDifficulty.Easy },
            new Topic { Id = 2, Position = 1, Difficulty = TopicDifficulty.Easy },
            new Topic { Id = 3, Position = 2, Difficulty = TopicDifficulty.Hard },
        };

        var pace = _svc.BuildPace(exam, topics, Today);

        Assert.Equal(3, pace.RemainingTopics);
        Assert.Equal(15d, pace.NeedMinutesPerDay);
    }

    [Fact]
    public void BuildPace_Need_Excludes_Done_Topics()
    {
        // Hard topic is Done -> ignored. One Hard remains -> weighted need = 45/5 = 9.
        // (old flat formula would give remaining(1)*30/5 = 6, so this also distinguishes weighted vs flat.)
        var exam = new Exam { Id = 2, Name = "M2", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, Position = 0, Difficulty = TopicDifficulty.Hard, Status = TopicStatus.Done },
            new Topic { Id = 2, Position = 1, Difficulty = TopicDifficulty.Hard, Status = TopicStatus.NotStarted },
        };

        var pace = _svc.BuildPace(exam, topics, Today);

        Assert.Equal(1, pace.RemainingTopics);
        Assert.Equal(9d, pace.NeedMinutesPerDay);
    }
```

- [ ] **Step 2: Запустить — убедиться, что падают**

Run:
```bash
"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests --filter DifficultyTest
```
Expected: FAIL — старый `BuildPace` считает плоско (`осталось × MinutesPerTopic ÷ дни`): даёт 18 и 6 вместо ожидаемых 15 и 9.

- [ ] **Step 3: Переписать BuildPace на взвешенный need**

Заменить метод `BuildPace` в `src/ExamPlanner.Core/Services/PlannerService.cs` целиком на:
```csharp
    public ExamPace BuildPace(Exam exam, IReadOnlyList<Topic> topics, DateTime today)
    {
        var total = topics.Count;
        var remainingTopics = topics.Where(t => t.Status != TopicStatus.Done).ToList();
        var remaining = remainingTopics.Count;
        var daysLeft = DaysLeft(exam.Date, today);
        var need = remaining == 0
            ? 0d
            : (double)remainingTopics.Sum(t => EffectiveMinutes(t, exam)) / daysLeft;
        var quota = TodayQuota(remaining, daysLeft);
        var urgent = remaining > 0 && daysLeft <= UrgentDaysThreshold;

        return new ExamPace(exam.Id, exam.Name, exam.Date, total, remaining, daysLeft, need, quota, urgent);
    }
```

- [ ] **Step 4: Запустить весь набор — убедиться, что зелёный**

Run:
```bash
"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests
```
Expected: PASS — новые тесты проходят, существующие `PlannerPaceTest`/`PlannerDashboardTest` остаются зелёными (темы по умолчанию `Medium` → те же числа).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner.Core/Services/PlannerService.cs tests/ExamPlanner.Core.Tests/DifficultyTest.cs
git commit -m "feat(core): weight BuildPace need by topic difficulty"
```

---

### Task 4: BuildDailyPlan — минуты по сложности

Меняет `BuildDailyPlan`: бюджет времени и `PlanItem.Minutes` берутся из `EffectiveMinutes(topic, exam)` для каждой темы вместо плоского `exam.MinutesPerTopic`. Порядок набора (по дате экзамена, затем `Position`), квота и флаг «не помещается» — без изменений.

**Files:**
- Modify: `src/ExamPlanner.Core/Services/PlannerService.cs:38-73` (метод `BuildDailyPlan`)
- Modify (test): `tests/ExamPlanner.Core.Tests/DifficultyTest.cs`

**Interfaces:**
- Consumes: `EffectiveMinutes` (Task 2).
- Produces: без новых сигнатур — меняется вычисление `PlanItem.Minutes` и бюджета.

- [ ] **Step 1: Написать падающие тесты**

Добавить в класс `DifficultyTest` (перед закрывающей `}` класса):
```csharp
    [Fact]
    public void BuildDailyPlan_Uses_DifficultyWeighted_Minutes()
    {
        // daysLeft = 2 Jun - 1 Jun = 1 -> quota = ceil(2/1) = 2. Easy 15 + Hard 45 = 60 <= 60 available.
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 2), MinutesPerTopic = 30 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, ExamId = 1, Position = 0, Title = "E", Difficulty = TopicDifficulty.Easy },
            new Topic { Id = 2, ExamId = 1, Position = 1, Title = "H", Difficulty = TopicDifficulty.Hard },
        };
        var data = new List<(Exam, IReadOnlyList<Topic>)> { (exam, topics) };

        var plan = _svc.BuildDailyPlan(data, availableHoursPerDay: 1, Today); // 60 min

        Assert.Equal(2, plan.Items.Count);
        Assert.Equal(15, plan.Items[0].Minutes);
        Assert.Equal(45, plan.Items[1].Minutes);
        Assert.Equal(60d, plan.UsedMinutes);
        Assert.Empty(plan.NotFittedExamIds);
    }

    [Fact]
    public void BuildDailyPlan_Flags_NotFitted_When_Hard_Topic_Too_Big()
    {
        // available 30 min. quota = 2. Easy 15 fits (used 15); Hard 45 -> 15+45=60 > 30 -> break. placed 1 < 2 -> not fitted.
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 2), MinutesPerTopic = 30 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, ExamId = 1, Position = 0, Title = "E", Difficulty = TopicDifficulty.Easy },
            new Topic { Id = 2, ExamId = 1, Position = 1, Title = "H", Difficulty = TopicDifficulty.Hard },
        };
        var data = new List<(Exam, IReadOnlyList<Topic>)> { (exam, topics) };

        var plan = _svc.BuildDailyPlan(data, availableHoursPerDay: 0.5, Today); // 30 min

        Assert.Single(plan.Items);
        Assert.Equal(15, plan.Items[0].Minutes);
        Assert.Equal(15d, plan.UsedMinutes);
        Assert.Contains(1, plan.NotFittedExamIds);
    }
```

- [ ] **Step 2: Запустить — убедиться, что падают**

Run:
```bash
"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests --filter DifficultyTest
```
Expected: FAIL — плоский `BuildDailyPlan` кладёт `PlanItem.Minutes = 30` для обеих тем и считает бюджет по 30, поэтому ассерты (15, 45, 60 / 15, not-fitted) не сходятся.

- [ ] **Step 3: Переписать внутренний цикл BuildDailyPlan**

В `src/ExamPlanner.Core/Services/PlannerService.cs` в методе `BuildDailyPlan` заменить блок набора тем (цикл `foreach (var topic in remaining.Take(quota))`) на версию с `EffectiveMinutes`:
```csharp
            var placed = 0;
            foreach (var topic in remaining.Take(quota))
            {
                var minutes = EffectiveMinutes(topic, exam);
                if (used + minutes <= available)
                {
                    items.Add(new PlanItem(exam.Id, exam.Name, topic.Id, topic.Title, minutes));
                    used += minutes;
                    placed++;
                }
                else break;
            }

            if (placed < quota) notFitted.Add(exam.Id);
```
(Остальная часть метода — `available`, `ordered`, отбор `remaining`, `quota`, `return` — без изменений.)

- [ ] **Step 4: Запустить весь набор — убедиться, что зелёный**

Run:
```bash
"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests
```
Expected: PASS — все тесты зелёные. Существующий `PlannerDailyPlanTest` остаётся зелёным (темы по умолчанию `Medium` → `EffectiveMinutes = MinutesPerTopic`, поведение идентично).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner.Core/Services/PlannerService.cs tests/ExamPlanner.Core.Tests/DifficultyTest.cs
git commit -m "feat(core): weight BuildDailyPlan minutes by topic difficulty"
```

---

## Финальная проверка

- [ ] Весь набор Core-тестов зелёный: `"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests` → ожидается ~23 теста, 0 упавших.
- [ ] Существующие 14 тестов не менялись и остались зелёными (обратная совместимость).
- [ ] `src/ExamPlanner` (UI) не тронут.
- [ ] Отправить ветку `feature/topic-difficulty` агенту-тестировщику на независимую проверку (юнит-тесты + сборка + миграция), исправить отчётные замечания, затем предложить владельцу мерж.
