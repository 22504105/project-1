using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class StatsPage : ContentPage
{
	private readonly StatsViewModel _vm;

	public StatsPage(StatsViewModel vm)
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
