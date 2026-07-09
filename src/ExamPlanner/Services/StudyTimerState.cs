using Microsoft.Maui.Storage;

namespace ExamPlanner.Services;

/// <summary>
/// A snapshot of an in-progress study timer. Elapsed time is derived from the
/// wall clock (a persisted start timestamp + accumulated paused time), never
/// from a UI counter — so it stays correct across minimize/restore and even a
/// full process kill.
/// </summary>
public sealed class StudyTimerSnapshot
{
	/// <summary>A session is in progress (running or paused).</summary>
	public bool Active { get; set; }

	/// <summary>Currently ticking (false while paused).</summary>
	public bool Running { get; set; }

	/// <summary>Seconds accumulated in previous run segments (frozen at each pause).</summary>
	public double AccumulatedSeconds { get; set; }

	/// <summary>UTC ticks at the moment the current run segment started/resumed.</summary>
	public long ResumedAtTicksUtc { get; set; }

	public int ExamId { get; set; }
	public int? TopicId { get; set; }

	/// <summary>Elapsed time computed from the wall clock.</summary>
	public TimeSpan Elapsed
	{
		get
		{
			var seconds = AccumulatedSeconds;
			if (Running)
				seconds += (DateTime.UtcNow.Ticks - ResumedAtTicksUtc) / (double)TimeSpan.TicksPerSecond;
			return TimeSpan.FromSeconds(Math.Max(0, seconds));
		}
	}
}

/// <summary>
/// Preferences-backed persistence for the single active study timer.
/// Client-side only (UI concern) — the finished session is still saved to the
/// Core DB on Stop; this just keeps the live clock honest while it runs.
/// </summary>
public static class StudyTimerState
{
	private const string KActive = "timer.active";
	private const string KRunning = "timer.running";
	private const string KAccum = "timer.accumSeconds";
	private const string KResumed = "timer.resumedAtTicksUtc";
	private const string KExam = "timer.examId";
	private const string KTopic = "timer.topicId"; // -1 == null

	public static StudyTimerSnapshot Load() => new()
	{
		Active = Preferences.Default.Get(KActive, false),
		Running = Preferences.Default.Get(KRunning, false),
		AccumulatedSeconds = Preferences.Default.Get(KAccum, 0d),
		ResumedAtTicksUtc = Preferences.Default.Get(KResumed, 0L),
		ExamId = Preferences.Default.Get(KExam, 0),
		TopicId = Preferences.Default.Get(KTopic, -1) is var t && t >= 0 ? t : null,
	};

	public static void Save(StudyTimerSnapshot s)
	{
		Preferences.Default.Set(KActive, s.Active);
		Preferences.Default.Set(KRunning, s.Running);
		Preferences.Default.Set(KAccum, s.AccumulatedSeconds);
		Preferences.Default.Set(KResumed, s.ResumedAtTicksUtc);
		Preferences.Default.Set(KExam, s.ExamId);
		Preferences.Default.Set(KTopic, s.TopicId ?? -1);
	}

	public static void Clear()
	{
		Preferences.Default.Remove(KActive);
		Preferences.Default.Remove(KRunning);
		Preferences.Default.Remove(KAccum);
		Preferences.Default.Remove(KResumed);
		Preferences.Default.Remove(KExam);
		Preferences.Default.Remove(KTopic);
	}
}
