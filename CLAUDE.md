# Инструкции для Claude по этому репозиторию

Эти правила заданы владельцем репо. Следуй им в каждой сессии.

## Рабочий процесс

- **Глубокий анализ**: при разработке, проектировании, обсуждении задач и проекта
  применяй глубокий анализ — рассматривай альтернативы, последствия и краевые случаи,
  а не выдавай поверхностное решение. Не «лишь бы работало», а продуманно.
- **Superpowers**: используй скиллы `superpowers:*` по собственному усмотрению, когда они уместны
  (brainstorming перед новой фичей, test-driven-development при реализации,
  systematic-debugging при багах, verification-before-completion перед заявлением о готовности и т.д.).
  Не спрашивай разрешения на применение скилла — применяй, когда это помогает делу.
- **Git**: полный доступ на чтение и запись. Можно коммитить, **пушить и мержить самостоятельно**.
- **Деплой**: только после явного апрува владельца. Никогда не деплой без подтверждения.
- **Знания — в репо**: важные технические факты, решения и грабли фиксируй **в этом файле**
  (а не только в session-памяти), чтобы они были под версионным контролем и доступны всем.
  Держи записи краткими; детали — в соответствующих секциях ниже.

## Git-конвенции

- Работай в ветках; не коммить напрямую в `main` для нетривиальных изменений.
- Перед мержем убедись, что тесты проходят (см. verification-before-completion).
- Идентичность коммитов: `22504105 <universgroup13@gmail.com>`.
- Не коммить содержимое `.vs/`, build-артефакты и прочий IDE-кэш (см. `.gitignore`).

## О проекте

- Solution Visual Studio (`project-1.slnx`, новый формат `.slnx`).
- Remote: https://github.com/22504105/project-1
- Основная ветка: `main`.
- **Расположение репо**: `E:\Projects\project-1` (ASCII-путь). Перенесён сюда с
  `C:\Users\Евгений\source\repos\22504105\project-1` — Android-сборка (AAPT2) падает
  с `error APT2265` на кириллическом пути. Работать только из `E:\Projects\...`.
- **Направление продукта**: ExamPlanner — приложение для студентов (темы к экзаменам,
  прогресс, планирование времени). Дизайн MVP и роадмап отложенных фич (сервер/аккаунты,
  iOS, уведомления, тесты-квизы, ИИ и т.д.) — в `docs/superpowers/specs/`. **Напоминание:**
  отложенное не забываем — оно перечислено в разделе «Дальнейшие фазы» спеки.
- Проекты: `src/App` (`.NET 8` console, заготовка) · `src/ExamPlanner.Core` (`net8.0`,
  логика+тесты) · `src/ExamPlanner` (`net10.0-android`, MAUI-приложение MVVM).
- **CI:** GitHub Actions (`.github/workflows/ci.yml`) гоняет `dotnet test tests/ExamPlanner.Core.Tests`
  на push/PR в `main` (только Core — он `net8.0`, без Android).
- **MAUI-приложение** (`src/ExamPlanner`): собрать/запустить на эмуляторе —
  `"C:\Program Files\dotnet\dotnet.exe" build src/ExamPlanner -f net10.0-android -t:Run`.
  DI в `MauiProgram.cs`: `IPlannerRepository` (singleton, БД `FileSystem.AppDataDirectory/examplanner.db3`),
  `PlannerService`; Shell TabBar → `DashboardPage`. Package id `com.companyname.examplanner`.
- Тесты ядра: `"C:\Program Files\dotnet\dotnet.exe" test tests/ExamPlanner.Core.Tests`.
- Формат `.slnx` из CLI требует SDK ≥ 9.0.200; сейчас есть 10.0.301 → должно работать,
  но проекты в `.slnx` всё равно правим ручным редактированием XML (см. план).

## Core API (обзор `ExamPlanner.Core`)

Вся логика — чистая и юнит-тестируемая, без MAUI. UI (`src/ExamPlanner`) её потребляет.

- **Модели** (`Models/`): `Exam`, `Topic` (+ `TopicStatus`, `TopicDifficulty {Medium=0,Easy=1,Hard=2}`),
  `AppSettings` (база `AvailableHoursPerDay` + nullable `MonHours..SunHours`), `StudySession`.
- **Данные** (`Data/`): `IPlannerRepository` / `SqlitePlannerRepository` — self-init таблиц,
  CRUD экзаменов/тем/настроек/сессий; `DeleteExamAsync` каскадит темы и сессии.
- **`PlannerService`** (`Services/`): `EffectiveMinutes` (сложность ×0.5/1.0/1.5),
  `BuildPace`/`BuildDashboard`/`BuildDailyPlan` (взвешены по сложности; дневной план
  пропускает не влезшую тему и продолжает набор), `HoursForDay`/`HoursForWeekday`,
  `ProposeWeeklySchedule` (недельный график + дедлайн-выполнимость через EDF-симуляцию),
  `CalibrationFactor`/`CalibratedNeedMinutesPerDay` (факт vs оценка).
- **`StatsService`**: `TotalMinutes`, `MinutesOn`, `MinutesPerExam`, `DailyTotals`.
- **`BackupService`**: `ExportJsonAsync`/`ImportJsonAsync` (JSON бэкап с ремаппингом id).
- **Грабли тестов:** классы с реальной SQLite (`RepositoryTest`, `BackupTest`) помечены
  `[Collection("Sqlite")]` (не параллелятся) — иначе глобальный `ResetPool()` в их Dispose
  рвёт соединения друг друга.
- **Отложено (с UI/ИИ/сервером):** квизы + «двойная оценка», фаза v2 (серверный бэкенд).

## Окружение (Windows) и грабли

- **Домашний путь с кириллицей**: `C:\Users\Евгений`. Регулярно ломает наивные инструменты —
  следи за кодировками:
  - PowerShell 5.1 → нативный exe: не-ASCII аргументы (пути) идут через ANSI-кодовую страницу
    и искажаются. Обход: `Push-Location $dir; & git ...; Pop-Location` вместо `git -C "$path"`.
  - Чтение stdin в PS 5.1: `[Console]::In.ReadToEnd()` декодирует по кодировке консоли (cp866),
    UTF-8 ввод искажается. Обход: `StreamReader([Console]::OpenStandardInput(), [Text.Encoding]::UTF8)`.
  - `Get-Content -Raw` читает UTF-8 как ANSI (кракозябры). Для проверки: `[IO.File]::ReadAllBytes`
    + `[Text.Encoding]::UTF8.GetString`.
  - Подать файл в stdin процесса: `Start-Process -RedirectStandardInput <file>` (`cmd /c ... <` блокируется песочницей).
- **Android + кириллица (APT2265)**: AAPT2 не умеет не-ASCII пути. Держать репо на
  `E:\Projects\...`. Именно поэтому проект и вынесен с `C:\Users\Евгений\...`.
- **.NET SDK**: установлены 8.0.422 и **10.0.301**. `dotnet` не в PATH у Bash —
  вызывать `"C:\Program Files\dotnet\dotnet.exe"`. Есть VS 2026 + MSBuild 18.7.
- **MAUI-ворклоад установлен** (`maui-android 10.0.20` + `android`/`ios`/`maccatalyst`).
- **Android SDK**: `E:\AndroidSdk` (`ANDROID_HOME`/`ANDROID_SDK_ROOT`); AVD `examplanner`
  в `E:\AndroidAvd` (`ANDROID_AVD_HOME`); `JAVA_HOME` = JDK 21. adb/emulator под `E:\AndroidSdk`.
  Запуск эмулятора: `E:\AndroidSdk\emulator\emulator.exe -avd examplanner`.

## Оснастка Claude Code

- **Статус-строка** (личная, глобально в `~/.claude`, не в репо): скрипт
  `~/.claude/statusline.ps1` (PowerShell, без внешних зависимостей — `jq` не установлен),
  подключён в `~/.claude/settings.json` → `statusLine`. Показывает модель, контекст/токены,
  лимиты 5h/7d с временем до сброса (только Pro/Max, иначе `limits n/a`), git-ветку.

<!-- По мере роста проекта дополняй этот файл: команды сборки/тестов, архитектура, договорённости, грабли. -->
