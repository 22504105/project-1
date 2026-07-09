# Авто-предложение недельного графика (инкремент B) — дизайн

- **Проект:** ExamPlanner · **Фаза:** пост-MVP, Core · **Дата:** 2026-07-09 · **Автор:** 22504105
- **Трек:** backend/Core (параллельно frontend-агенту). UI не входит.

## 1. Цель

Дать системе **предлагать готовый недельный график** (сколько часов в какой день недели), покрывающий нагрузку по всем экзаменам к дедлайнам. Пользователь принимает или правит (график хранится через инкремент A). Дополняет A (пользователь задаёт сам) рекомендацией.

## 2. Область

**Входит (только `ExamPlanner.Core`):**
- `PlannerService.ProposeWeeklySchedule(data, settings, today)` → рекомендованные часы по 7 дням + метрики.
- Мелкий рефакторинг `HoursForDay` (выделить маппинг по `DayOfWeek`), DRY.
- Юнит-тесты.

**Не входит:** UI (показ/применение предложения — frontend); точная проверка «успеть к каждому отдельному дедлайну» (упрощённый флаг, доработка позже); доступность по конкретным датам.

## 3. Логика

Новый метод:
```csharp
public WeeklyScheduleProposal ProposeWeeklySchedule(
    IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data,
    AppSettings settings,
    DateTime today)
```

Шаги:
1. `requiredMinsPerDay` = сумма `BuildPace(exam, topics, today).NeedMinutesPerDay` по экзаменам с `RemainingTopics > 0` (то же, что рекомендация дашборда — дедлайн- и сложность-взвешено).
2. «Учебные дни» = дни недели, где `HoursForWeekday(settings, d) > 0`. (Если график не задан и база > 0 — доступны все 7; если база 0 без переопределений — ни одного.)
3. Пусть `k` = число учебных дней.
   - `k == 0` → `Feasible = false`, все 7 дней по `0` ч.
   - Иначе часы на каждый учебный день = `requiredMinsPerDay × 7 / k / 60`, округлённые до 0.5 ч (`Math.Round(x*2, AwayFromZero)/2`); неучебные дни = `0`.
4. `RequiredHoursPerDay` = `requiredMinsPerDay / 60` (сколько в среднем нужно в день).
5. `Feasible` = `k > 0`. (Проверку «хватает ли учебных дней до ближайшего экзамена» откладываем — beta.)

Результат — 7 записей в порядке Пн…Вс.

## 4. Типы (добавляются в `Services/PlannerModels.cs`)

```csharp
public record DayHours(DayOfWeek Day, double Hours);

public record WeeklyScheduleProposal(
    IReadOnlyList<DayHours> Days,   // 7 записей, Пн..Вс
    double RequiredHoursPerDay,
    bool Feasible);
```

## 5. Рефакторинг HoursForDay (DRY)

Чтобы не дублировать маппинг дня, выделяется публичный помощник по `DayOfWeek`, а существующий метод делегирует ему (поведение не меняется):
```csharp
public double HoursForDay(AppSettings settings, DateTime day)
    => HoursForWeekday(settings, day.DayOfWeek);

public double HoursForWeekday(AppSettings settings, DayOfWeek day)
    => (day switch { /* Mon..Sun -> field */ }) ?? settings.AvailableHoursPerDay;
```

## 6. Обратная совместимость

- Новый метод и record-типы только добавляются; сигнатуры существующих методов не меняются. Рефакторинг `HoursForDay` сохраняет поведение (покрыт тестами `AvailabilityTest`).
- Существующие 31 тест остаются зелёными.

## 7. Тестирование

- **Равномерное распределение:** один экзамен, база 2 ч (все 7 дней доступны), известная `requiredMinsPerDay` → каждый день = `required×7/7 = required` (в часах, округл. 0.5); все 7 дней равны; `Feasible = true`.
- **Только учебные дни:** график с 0 в Сб/Вс (5 учебных) → нагрузка спрессована в 5 дней (`×7/5`), Сб/Вс = 0.
- **Нет учебных дней:** база 0, без переопределений → все дни 0, `Feasible = false`.
- **Нет оставшихся тем:** все темы Done или нет экзаменов → `requiredMinsPerDay = 0`, все дни 0, `RequiredHoursPerDay = 0` (Feasible зависит от наличия учебных дней).
- **Сложность учитывается:** тяжёлые темы повышают `required` (через `BuildPace`).
- **Порядок:** первый элемент — Понедельник, последний — Воскресенье.
- **HoursForWeekday** (рефакторинг): маппинг Пн..Вс и fallback как у `HoursForDay` (существующие тесты остаются зелёными).

## 8. Допущения

- Округление часов до 0.5, midpoint — от нуля.
- Распределение равномерное по учебным дням (не приоритизирует ближайшие дедлайны внутри недели — beta-упрощение).
- Часы могут превышать разумный дневной максимум при большой нагрузке/малом числе дней — это сигнал «не успеваешь», отдельного капа нет (UI покажет).
