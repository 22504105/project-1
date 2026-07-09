# Учёт учебного времени + статистика (Core) — дизайн

- **Проект:** ExamPlanner · **Фаза:** пост-MVP, Core · **Дата:** 2026-07-09 · **Автор:** 22504105
- **Трек:** backend/Core. UI (в т.ч. pomodoro-таймер) не входит.

## 1. Цель

Хранить завершённые учебные сессии (сколько минут реально позанимался) и считать статистику (всего, по дням, по экзаменам). Pomodoro-отсчёт — задача UI; Core получает готовые минуты и агрегирует.

## 2. Область

**Входит (только `ExamPlanner.Core`):**
- Модель `StudySession` + таблица.
- Репозиторий: сохранение/чтение сессий, каскадное удаление при удалении экзамена.
- `StatsService` — чистые агрегаты.
- Юнит-тесты.

**Не входит:** UI/таймер (frontend); влияние факта на планирование (beta — только показ); стрики/цели.

## 3. Модель

```csharp
using SQLite;
namespace ExamPlanner.Core.Models;

public class StudySession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int ExamId { get; set; }
    public int? TopicId { get; set; }   // опционально — если сессия по конкретной теме
    public int Minutes { get; set; }
    public DateTime StartedAt { get; set; }  // локальное время; группировка по .Date
}
```

## 4. Репозиторий

Добавляется в `IPlannerRepository` и `SqlitePlannerRepository`:
- `Task<int> SaveSessionAsync(StudySession session)` — insert (авто-Id).
- `Task<List<StudySession>> GetSessionsAsync(int examId)` — сессии экзамена, по `StartedAt`.
- `Task<List<StudySession>> GetAllSessionsAsync()` — все сессии (для глобальной статистики).
- `InitializeAsync`: добавляется `CreateTableAsync<StudySession>()`.
- `DeleteExamAsync`: добавляется `DELETE FROM StudySession WHERE ExamId = ?` (каскад, как для тем).

## 5. StatsService (новый, чистые функции)

`src/ExamPlanner.Core/Services/StatsService.cs`:
- `int TotalMinutes(IEnumerable<StudySession> sessions)` — сумма минут.
- `int MinutesOn(IEnumerable<StudySession> sessions, DateTime day)` — минуты за конкретный день (по `.Date`, время игнорируется).
- `IReadOnlyDictionary<int,int> MinutesPerExam(IEnumerable<StudySession> sessions)` — сумма минут по `ExamId`.
- `IReadOnlyList<DayMinutes> DailyTotals(IEnumerable<StudySession> sessions, DateTime from, DateTime to)` — непрерывный ряд по дням `[from.Date, to.Date]` включительно, пустые дни = 0 (для графика).

DTO: `public record DayMinutes(DateTime Day, int Minutes);`

## 6. Обратная совместимость / миграция

- Новая таблица `StudySession` создаётся при `CreateTableAsync` (self-init); существующих данных не затрагивает.
- Изменения репозитория — только добавления (+ строка каскада в `DeleteExamAsync`). Существующие 36 тестов остаются зелёными.

## 7. Тестирование

- **Репозиторий:** сохранить сессию → прочитать по экзамену (round-trip, вкл. `TopicId`); `GetAllSessionsAsync` возвращает все; `DeleteExamAsync` удаляет сессии экзамена (каскад).
- **StatsService:**
  - `TotalMinutes` суммирует.
  - `MinutesOn` фильтрует по дню, игнорируя время (сессия 09:00 и 20:00 того же дня складываются; другой день не учитывается).
  - `MinutesPerExam` группирует по экзамену.
  - `DailyTotals` даёт непрерывный ряд с 0-заполнением, корректный порядок и включительные границы; несколько сессий в один день суммируются.

## 8. Допущения

- `StartedAt` — локальное время, передаёт вызывающая сторона; группировка по `.Date`.
- `Minutes` — целое (готовая длительность сессии), Core не валидирует диапазон.
- Факт не влияет на планировщик в этом инкременте (только статистика).
