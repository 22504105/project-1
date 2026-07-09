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
