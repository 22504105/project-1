using System.Globalization;
using ExamPlanner.Core.Models;

namespace ExamPlanner.Converters;

public class DifficultyToTextConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is TopicDifficulty d
			? d switch
			{
				TopicDifficulty.Easy => "Лёгкий",
				TopicDifficulty.Hard => "Тяжёлый",
				_ => "Средний"
			}
			: "Средний";

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}
