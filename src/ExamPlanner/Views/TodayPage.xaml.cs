using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class TodayPage : ContentPage
{
	private readonly TodayViewModel _vm;

	public TodayPage(TodayViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}
