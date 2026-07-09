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
