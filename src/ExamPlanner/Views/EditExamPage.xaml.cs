using ExamPlanner.ViewModels;

namespace ExamPlanner.Views;

public partial class EditExamPage : ContentPage
{
	public EditExamPage(EditExamViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}
