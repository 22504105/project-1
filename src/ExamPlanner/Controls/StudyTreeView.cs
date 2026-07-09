using Microsoft.Maui.Graphics;

namespace ExamPlanner.Controls;

/// <summary>
/// A themed tree that visualises progress toward the daily study goal.
/// <para>
/// <see cref="Fraction"/> = todayStudiedMinutes / dailyTargetMinutes. It grows
/// sapling → full tree as it approaches 1 (goal met). Beyond 1, every extra 15%
/// of the daily target adds one red apple to the foliage (capped at 7).
/// The displayed growth eases smoothly toward the target as the timer ticks.
/// </para>
/// </summary>
public sealed class StudyTreeView : GraphicsView
{
	public const double AppleStep = 0.15;   // one apple per +15% beyond the goal
	public const int MaxApples = 7;

	public static readonly BindableProperty FractionProperty = BindableProperty.Create(
		nameof(Fraction), typeof(double), typeof(StudyTreeView), 0d,
		propertyChanged: (b, _, n) => ((StudyTreeView)b).OnFractionChanged((double)n));

	/// <summary>Progress fraction (0..1 grows the tree, &gt;1 adds apples).</summary>
	public double Fraction
	{
		get => (double)GetValue(FractionProperty);
		set => SetValue(FractionProperty, value);
	}

	private readonly TreeDrawable _drawable = new();
	private IDispatcherTimer? _timer;

	public StudyTreeView()
	{
		Drawable = _drawable;
		InputTransparent = true;
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object? sender, EventArgs e)
	{
		if (Application.Current is not null)
			Application.Current.RequestedThemeChanged += OnThemeChanged;
		EnsureAnimating();
	}

	private void OnUnloaded(object? sender, EventArgs e)
	{
		if (Application.Current is not null)
			Application.Current.RequestedThemeChanged -= OnThemeChanged;
		_timer?.Stop();
	}

	private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => Invalidate();

	private void OnFractionChanged(double value)
	{
		_drawable.Target = value;
		EnsureAnimating();
	}

	// Ease the displayed fraction toward the target, then idle to save battery.
	private void EnsureAnimating()
	{
		if (_timer is null)
		{
			_timer = Dispatcher.CreateTimer();
			_timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 fps
			_timer.Tick += (_, _) =>
			{
				bool moving = _drawable.Advance();
				Invalidate();
				if (!moving) _timer!.Stop();
			};
		}
		if (!_timer.IsRunning) _timer.Start();
	}

	private sealed class TreeDrawable : IDrawable
	{
		public double Target { get; set; }
		private double _shown;

		/// <returns>true while still easing toward the target.</returns>
		public bool Advance()
		{
			var diff = Target - _shown;
			if (Math.Abs(diff) < 0.0008) { _shown = Target; return false; }
			_shown += diff * 0.12; // smooth exponential ease
			return true;
		}

		// Deterministic apple slots inside the canopy (relative to canopy radius).
		private static readonly (float dx, float dy)[] AppleSlots =
		{
			(-0.45f,  0.10f), ( 0.42f, -0.05f), ( 0.05f,  0.35f),
			(-0.20f, -0.35f), ( 0.30f,  0.30f), (-0.55f, -0.18f),
			( 0.58f,  0.20f),
		};

		public void Draw(ICanvas canvas, RectF rect)
		{
			bool dark = Application.Current?.RequestedTheme == AppTheme.Dark;

			var trunkColor = dark ? Color.FromRgb(0x8A, 0x5C, 0x36) : Color.FromRgb(0x6E, 0x4B, 0x2A);
			var canopyBack = dark ? Color.FromRgb(0x2F, 0x6A, 0x4E) : Color.FromRgb(0x24, 0x53, 0x3D);
			var canopyFront = dark ? Color.FromRgb(0x4E, 0x9E, 0x77) : Color.FromRgb(0x2E, 0x6B, 0x4F);
			var appleColor = dark ? Color.FromRgb(0xE2, 0x54, 0x4C) : Color.FromRgb(0xD6, 0x45, 0x3F);
			var appleHi = dark ? Color.FromRgb(0xF3, 0x8C, 0x86) : Color.FromRgb(0xE9, 0x82, 0x7B);
			var groundColor = dark ? Color.FromRgb(0x2E, 0x38, 0x33) : Color.FromRgb(0xBF, 0xCB, 0xC1);

			float g = (float)Math.Clamp(_shown, 0, 1);
			float w = rect.Width, h = rect.Height;
			float baseX = w / 2f;
			float groundY = h * 0.94f;

			// ground line
			canvas.StrokeColor = groundColor;
			canvas.StrokeSize = 2;
			canvas.DrawLine(w * 0.20f, groundY, w * 0.80f, groundY);

			// trunk (tapered), grows in height and thickness
			float trunkH = h * (0.10f + 0.46f * g);
			float trunkTop = groundY - trunkH;
			float bw = 5f + 15f * g;          // base width
			float tw = bw * 0.55f;            // top width
			var trunk = new PathF();
			trunk.MoveTo(baseX - bw / 2f, groundY);
			trunk.LineTo(baseX + bw / 2f, groundY);
			trunk.LineTo(baseX + tw / 2f, trunkTop);
			trunk.LineTo(baseX - tw / 2f, trunkTop);
			trunk.Close();
			canvas.FillColor = trunkColor;
			canvas.FillPath(trunk);

			// a couple of branches once past a sapling
			if (g > 0.35f)
			{
				float bAmt = (g - 0.35f) / 0.65f; // 0..1
				canvas.StrokeColor = trunkColor;
				canvas.StrokeLineCap = LineCap.Round;
				canvas.StrokeSize = Math.Max(2f, bw * 0.35f);
				float by = trunkTop + trunkH * 0.30f;
				float blen = trunkH * 0.35f * bAmt;
				canvas.DrawLine(baseX, by, baseX - blen, by - blen * 0.7f);
				canvas.DrawLine(baseX, by + trunkH * 0.12f, baseX + blen, by - blen * 0.5f);
			}

			// canopy: overlapping blobs, grows from a small bud to a full crown
			float canopyR = h * 0.07f + h * 0.26f * g;
			float cx = baseX;
			float cy = trunkTop - canopyR * 0.35f;

			(float dx, float dy, float r)[] blobs =
			{
				(0f, 0.12f, 1.00f),
				(-0.62f, 0.20f, 0.68f),
				(0.62f, 0.20f, 0.68f),
				(-0.30f, -0.42f, 0.66f),
				(0.32f, -0.40f, 0.66f),
				(0f, -0.10f, 0.90f),
			};

			// back layer (darker) for depth
			canvas.FillColor = canopyBack;
			foreach (var (dx, dy, r) in blobs)
			{
				float rr = canopyR * r * 1.04f;
				canvas.FillCircle(cx + dx * canopyR + rr * 0.06f, cy + dy * canopyR + rr * 0.08f, rr);
			}
			// front layer (lighter)
			canvas.FillColor = canopyFront;
			foreach (var (dx, dy, r) in blobs)
				canvas.FillCircle(cx + dx * canopyR, cy + dy * canopyR, canopyR * r);

			// apples for study beyond the goal
			int apples = 0;
			if (_shown > 1.0)
				apples = Math.Min(MaxApples, (int)Math.Floor((_shown - 1.0) / AppleStep));

			if (apples > 0 && g >= 0.999f)
			{
				float ar = Math.Max(4f, canopyR * 0.12f);
				for (int i = 0; i < apples; i++)
				{
					var (dx, dy) = AppleSlots[i];
					float ax = cx + dx * canopyR;
					float ay = cy + dy * canopyR;
					canvas.FillColor = appleColor;
					canvas.FillCircle(ax, ay, ar);
					canvas.FillColor = appleHi;
					canvas.FillCircle(ax - ar * 0.30f, ay - ar * 0.30f, ar * 0.34f);
				}
			}
		}
	}
}
