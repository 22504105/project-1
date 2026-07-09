# Weekday Availability (increment A) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Позволить задавать часы доступности по дню недели (fallback на общее значение) и дать `PlannerService.HoursForDay` для выбора часов на конкретный день.

**Architecture:** `AppSettings` получает 7 nullable-переопределений (`MonHours..SunHours`), `AvailableHoursPerDay` остаётся базой/fallback. `PlannerService.HoursForDay(settings, day)` возвращает переопределение дня или базу. Планировочные методы не меняются.

**Tech Stack:** .NET 8 · sqlite-net-pcl · xUnit.

## Global Constraints

- Только `ExamPlanner.Core` + `tests/ExamPlanner.Core.Tests`. `src/ExamPlanner` (UI) не трогать.
- `dotnet` полным путём: `"C:\Program Files\dotnet\dotnet.exe"`. Репо `E:\Projects\project-1`.
- Ветка `feature/weekday-availability` (не в `main`). Идентичность: `22504105 <universgroup13@gmail.com>`.
- Nullable-переопределения: `null` → fallback на `AvailableHoursPerDay`; заданный `0` — валиден (день без занятий), не путать с fallback.
- Обратная совместимость: существующие 27 тестов остаются зелёными без правок.
- TDD, частые коммиты.

**Setup (один раз):**
```bash
cd "E:/Projects/project-1" && git switch -c feature/weekday-availability
```

---

### Task 1: AppSettings — переопределения часов по дням недели

**Files:**
- Modify: `src/ExamPlanner.Core/Models/AppSettings.cs`
- Modify (test): `tests/ExamPlanner.Core.Tests/RepositoryTest.cs`

**Interfaces:**
- Produces: `AppSettings.{MonHours,TueHours,WedHours,ThuHours,FriHours,SatHours,SunHours}` типа `double?` (дефолт `null`).

- [ ] **Step 1: Падающий round-trip тест**

Добавить в конец класса `RepositoryTest` (перед закрывающей `}`):
```csharp
    [Fact]
    public async Task SaveSettings_RoundTrips_WeekdayHours()
    {
        await _repo.SaveSettingsAsync(new AppSettings { AvailableHoursPerDay = 2, SatHours = 5, SunHours = 6 });

        var loaded = await _repo.GetSettingsAsync();
        Assert.Equal(5, loaded.SatHours);
        Assert.Equal(6, loaded.SunHours);
        Assert.Null(loaded.MonHours);
    }
```

- [ ] **Step 2: Запустить — падает**

Run: `"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests --filter SaveSettings_RoundTrips_WeekdayHours`
Expected: FAIL — полей нет (ошибка компиляции).

- [ ] **Step 3: Добавить поля**

Заменить `src/ExamPlanner.Core/Models/AppSettings.cs` на:
```csharp
using SQLite;

namespace ExamPlanner.Core.Models;

public class AppSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;
    public double AvailableHoursPerDay { get; set; } = 2;

    public double? MonHours { get; set; }
    public double? TueHours { get; set; }
    public double? WedHours { get; set; }
    public double? ThuHours { get; set; }
    public double? FriHours { get; set; }
    public double? SatHours { get; set; }
    public double? SunHours { get; set; }
}
```

- [ ] **Step 4: Запустить весь набор — зелёный**

Run: `"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests`
Expected: PASS (28: 27 + 1 новый).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner.Core/Models/AppSettings.cs tests/ExamPlanner.Core.Tests/RepositoryTest.cs
git commit -m "feat(core): add nullable per-weekday hour overrides to AppSettings"
```

---

### Task 2: PlannerService.HoursForDay

**Files:**
- Modify: `src/ExamPlanner.Core/Services/PlannerService.cs`
- Create (test): `tests/ExamPlanner.Core.Tests/AvailabilityTest.cs`

**Interfaces:**
- Consumes: `AppSettings` (Task 1).
- Produces: `double PlannerService.HoursForDay(AppSettings settings, DateTime day)`.

- [ ] **Step 1: Падающие тесты**

Create `tests/ExamPlanner.Core.Tests/AvailabilityTest.cs`:
```csharp
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class AvailabilityTest
{
    private readonly PlannerService _svc = new();
    // 2024-01-01 is a Monday; 01..07 span Mon..Sun.

    [Fact]
    public void HoursForDay_MapsEachWeekday()
    {
        var s = new AppSettings
        {
            AvailableHoursPerDay = 2,
            MonHours = 1, TueHours = 2, WedHours = 3, ThuHours = 4,
            FriHours = 5, SatHours = 6, SunHours = 7
        };
        Assert.Equal(1, _svc.HoursForDay(s, new DateTime(2024, 1, 1))); // Mon
        Assert.Equal(2, _svc.HoursForDay(s, new DateTime(2024, 1, 2))); // Tue
        Assert.Equal(3, _svc.HoursForDay(s, new DateTime(2024, 1, 3))); // Wed
        Assert.Equal(4, _svc.HoursForDay(s, new DateTime(2024, 1, 4))); // Thu
        Assert.Equal(5, _svc.HoursForDay(s, new DateTime(2024, 1, 5))); // Fri
        Assert.Equal(6, _svc.HoursForDay(s, new DateTime(2024, 1, 6))); // Sat
        Assert.Equal(7, _svc.HoursForDay(s, new DateTime(2024, 1, 7))); // Sun
    }

    [Fact]
    public void HoursForDay_NullOverride_FallsBackToBase()
    {
        var s = new AppSettings { AvailableHoursPerDay = 2.5 }; // all overrides null
        Assert.Equal(2.5, _svc.HoursForDay(s, new DateTime(2024, 1, 1)));
        Assert.Equal(2.5, _svc.HoursForDay(s, new DateTime(2024, 1, 6)));
    }

    [Fact]
    public void HoursForDay_ZeroOverride_IsRespectedNotFallback()
    {
        var s = new AppSettings { AvailableHoursPerDay = 2, SatHours = 0 };
        Assert.Equal(0, _svc.HoursForDay(s, new DateTime(2024, 1, 6))); // Sat -> 0
        Assert.Equal(2, _svc.HoursForDay(s, new DateTime(2024, 1, 1))); // Mon -> base
    }
}
```

- [ ] **Step 2: Запустить — падает**

Run: `"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests --filter AvailabilityTest`
Expected: FAIL — `HoursForDay` не существует.

- [ ] **Step 3: Реализовать HoursForDay**

Добавить в класс `PlannerService` (например, после `EffectiveMinutes`):
```csharp
    public double HoursForDay(AppSettings settings, DateTime day)
        => (day.DayOfWeek switch
        {
            DayOfWeek.Monday => settings.MonHours,
            DayOfWeek.Tuesday => settings.TueHours,
            DayOfWeek.Wednesday => settings.WedHours,
            DayOfWeek.Thursday => settings.ThuHours,
            DayOfWeek.Friday => settings.FriHours,
            DayOfWeek.Saturday => settings.SatHours,
            _ => settings.SunHours
        }) ?? settings.AvailableHoursPerDay;
```

- [ ] **Step 4: Запустить весь набор — зелёный**

Run: `"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests`
Expected: PASS (31: 28 + 3 новых).

- [ ] **Step 5: Commit**

```bash
git add src/ExamPlanner.Core/Services/PlannerService.cs tests/ExamPlanner.Core.Tests/AvailabilityTest.cs
git commit -m "feat(core): add PlannerService.HoursForDay (per-weekday availability)"
```

---

## Финальная проверка

- [ ] `"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests` → 31 тест, 0 упавших.
- [ ] Существующие 27 тестов не менялись.
- [ ] `src/ExamPlanner` (UI) не тронут.
- [ ] Отправить ветку `feature/weekday-availability` тестировщику; исправить замечания; предложить мёрж.
