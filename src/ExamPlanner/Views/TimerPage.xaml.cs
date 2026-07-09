using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class TimerPage : ContentPage
{
	private readonly TimerViewModel _vm;

	public TimerPage(TimerViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	// Fires when the page appears, including when the app resumes from the
	// background — recomputes elapsed from the wall clock and the tree state.
	protected override void OnAppearing()
	{
		base.OnAppearing();
		_vm.OnAppearing();
	}
}
