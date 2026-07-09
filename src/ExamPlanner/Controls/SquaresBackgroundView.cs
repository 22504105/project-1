using Microsoft.Maui.Graphics;

namespace ExamPlanner.Controls;

/// <summary>
/// A lightweight, theme-aware animated backdrop: soft rounded squares that
/// slowly pulse "closer then farther" (scale + opacity depth) and drift.
/// Purely decorative — input-transparent so it never blocks taps.
/// The timer only runs while the view is on screen (Loaded/Unloaded).
/// </summary>
public sealed class SquaresBackgroundView : GraphicsView
{
	private readonly SquaresDrawable _drawable = new();
	private IDispatcherTimer? _timer;

	public SquaresBackgroundView()
	{
		Drawable = _drawable;
		InputTransparent = true;
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object? sender, EventArgs e)
	{
		if (_timer is null)
		{
			_timer = Dispatcher.CreateTimer();
			_timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 fps
			_timer.Tick += OnTick;
		}
		_timer.Start();
	}

	private void OnUnloaded(object? sender, EventArgs e) => _timer?.Stop();

	private void OnTick(object? sender, EventArgs e)
	{
		_drawable.Advance(0.033);
		Invalidate();
	}

	private sealed class SquaresDrawable : IDrawable
	{
		private readonly struct Square
		{
			public Square(float x, float y, float size, float phase, float speed, float rot, float rotSpeed, float drift)
			{
				X = x; Y = y; Size = size; Phase = phase; Speed = speed; Rot = rot; RotSpeed = rotSpeed; Drift = drift;
			}
			public float X { get; }        // 0..1 relative position
			public float Y { get; }
			public float Size { get; }     // base edge length (dp)
			public float Phase { get; }    // depth phase offset
			public float Speed { get; }    // depth oscillation speed
			public float Rot { get; }      // base rotation (rad)
			public float RotSpeed { get; }
			public float Drift { get; }    // drift amplitude phase
		}

		private readonly Square[] _squares;
		private double _t;

		public SquaresDrawable()
		{
			var rnd = new Random(7);
			var list = new List<Square>();
			for (int i = 0; i < 18; i++)
			{
				list.Add(new Square(
					x: (float)rnd.NextDouble(),
					y: (float)rnd.NextDouble(),
					size: (float)(46 + rnd.NextDouble() * 64),
					phase: (float)(rnd.NextDouble() * Math.PI * 2),
					speed: (float)(0.25 + rnd.NextDouble() * 0.45),
					rot: (float)(rnd.NextDouble() * Math.PI),
					rotSpeed: (float)((rnd.NextDouble() - 0.5) * 0.15),
					drift: (float)(rnd.NextDouble() * Math.PI * 2)));
			}
			_squares = list.ToArray();
		}

		public void Advance(double dt) => _t += dt;

		public void Draw(ICanvas canvas, RectF rect)
		{
			bool dark = Application.Current?.RequestedTheme == AppTheme.Dark;
			// Muted green that reads on both the gray-green light and near-black dark bg.
			var baseColor = dark ? Color.FromRgb(0x4E, 0x9E, 0x77) : Color.FromRgb(0x2E, 0x6B, 0x4F);

			foreach (var sq in _squares)
			{
				double depth = Math.Sin(_t * sq.Speed + sq.Phase);      // -1..1 (far..near)
				float unit = (float)((depth + 1) / 2);                    // 0..1
				float scale = 0.6f + unit * 0.9f;                         // 0.6..1.5
				float size = sq.Size * scale;
				float alpha = dark ? 0.05f + unit * 0.09f : 0.05f + unit * 0.10f;

				float cx = sq.X * rect.Width + (float)Math.Sin(_t * 0.2 + sq.Drift) * rect.Width * 0.03f;
				float cy = sq.Y * rect.Height + (float)Math.Cos(_t * 0.16 + sq.Drift) * rect.Height * 0.02f;

				canvas.SaveState();
				canvas.FillColor = baseColor.WithAlpha(alpha);
				canvas.Rotate((sq.Rot + (float)_t * sq.RotSpeed) * 57.29578f, cx, cy);
				canvas.FillRoundedRectangle(cx - size / 2f, cy - size / 2f, size, size, size * 0.22f);
				canvas.RestoreState();
			}
		}
	}
}
