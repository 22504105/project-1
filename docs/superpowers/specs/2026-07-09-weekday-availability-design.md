# Доступность по дням недели (инкремент A) — дизайн

- **Проект:** ExamPlanner · **Фаза:** пост-MVP, Core · **Дата:** 2026-07-09 · **Автор:** 22504105
- **Трек:** backend/Core (параллельно frontend-агенту). UI не входит.

## 1. Цель

Сейчас доступность — одно число `AvailableHoursPerDay` на все дни. Дать пользователю задавать **свой график по дням недели** (в будни меньше, в выходные больше, хоть 0). Планировщик берёт «часы на сегодня» по дню недели; рекомендация «нужно ≈ X ч/день» (уже есть, `RecommendedMinutesPerDay`) сравнивается с этим графиком.

Это инкремент **A**. Авто-предложение готового графика — отдельный инкремент **B**.

## 2. Область

**Входит (только `ExamPlanner.Core`):**
- `AppSettings`: 7 nullable-переопределений часов по дням недели.
- `PlannerService.HoursForDay(settings, day)` — «часы на этот день».
- Юнит-тесты + round-trip в репозитории.

**Не входит:** UI выбора графика (frontend), авто-предложение графика (инкремент B), доступность по конкретным датам.

## 3. Модель данных

В `AppSettings` добавляются 7 nullable-полей (переопределения на день недели):

```csharp
public double? MonHours { get; set; }
public double? TueHours { get; set; }
public double? WedHours { get; set; }
public double? ThuHours { get; set; }
public double? FriHours { get; set; }
public double? SatHours { get; set; }
public double? SunHours { get; set; }
```

`AvailableHoursPerDay` остаётся — это **база/fallback**. `null` в дне = «на этот день использовать базовое значение».

Почему nullable: sqlite-net при добавлении новой колонки даёт старым строкам `NULL`, а не `0`. Значит существующие настройки читаются как «все дни = `AvailableHoursPerDay`» → поведение идентично текущему (использовать `0` как «не задано» нельзя — это валидное значение «в этот день не занимаюсь»).

## 4. Логика

Новый метод в `PlannerService`:

```csharp
public double HoursForDay(AppSettings settings, DateTime day)
```

Возвращает переопределение для `day.DayOfWeek` если оно не `null`, иначе `settings.AvailableHoursPerDay`.

Существующие `BuildDashboard`/`BuildDailyPlan` **не меняются**: они и дальше принимают `availableHoursPerDay` + `today`. Вызывающая сторона (VM, инкремент по UI) вычисляет часы на сегодня через `HoursForDay(settings, today)` и передаёт их. Так рекомендация («нужно ≈ X ч/день», сумма нужд по всем экзаменам) и флаг «не успеваешь» автоматически сравниваются с графиком на сегодня — без изменения сигнатур.

## 5. Обратная совместимость

- Новые колонки добавляются при `CreateTableAsync<AppSettings>()` (self-init репозитория), у старых строк = `NULL`.
- `HoursForDay` при всех `null` = `AvailableHoursPerDay` → существующие 27 тестов остаются зелёными без правок.
- Публичные сигнатуры `BuildDashboard`/`BuildDailyPlan`/record-типы не меняются.

## 6. Тестирование

- `HoursForDay`: без переопределения → `AvailableHoursPerDay`; с переопределением на конкретный день → это значение; корректный маппинг всех 7 `DayOfWeek` на нужное поле; `0` как заданное значение возвращается как `0` (не путается с fallback).
- Round-trip в `RepositoryTest`: сохранить `AppSettings` с, напр., `SatHours = 5`, загрузить → `SatHours == 5`; незаданные поля = `null`.
- Обратная совместимость: `AppSettings` со всеми `null` → `HoursForDay` для любого дня = `AvailableHoursPerDay`.

## 7. Допущения

- Маппинг: `DayOfWeek.Monday → MonHours`, … , `DayOfWeek.Sunday → SunHours`.
- `HoursForDay` живёт в `PlannerService` (как `EffectiveMinutes`), не в модели.
- Валидация диапазона часов — на стороне UI (инкремент по UI); Core доверяет сохранённым значениям.
