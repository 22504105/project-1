using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class ExamDetailPage : ContentPage
{
	private readonly ExamDetailViewModel _vm;

	public ExamDetailPage(ExamDetailViewModel vm)
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
