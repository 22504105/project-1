using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class TimerPage : ContentPage
{
	public TimerPage(TimerViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
