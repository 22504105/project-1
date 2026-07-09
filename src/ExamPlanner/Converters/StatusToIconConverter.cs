using System.Globalization;
using ExamPlanner.Core.Models;

namespace ExamPlanner.Converters;

public class StatusToIconConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is TopicStatus s
			? s switch
			{
				TopicStatus.Done => "●",       // ● filled
				TopicStatus.InProgress => "◐",  // ◐ half
				_ => "○"                         // ○ ring
			}
			: "○";

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}
