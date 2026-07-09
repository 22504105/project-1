namespace ExamPlanner;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("editexam", typeof(Views.EditExamPage));
		Routing.RegisterRoute("exam", typeof(Views.ExamDetailPage));
	}
}
