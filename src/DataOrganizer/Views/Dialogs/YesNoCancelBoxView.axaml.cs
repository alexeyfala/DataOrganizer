using Avalonia.Controls;
using DataOrganizer.ViewModels.Dialogs;

namespace DataOrganizer.Views.Dialogs;

/// <summary>
/// Dialog that asks a question with the Yes, No and Cancel answers.
/// </summary>
public partial class YesNoCancelBoxView : UserControl
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="YesNoCancelBoxView" /> class.
	/// </summary>
	public YesNoCancelBoxView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="YesNoCancelBoxView" /> class.
	/// </summary>
	public YesNoCancelBoxView(YesNoCancelBoxViewModel viewModel) : this() => DataContext = viewModel;
	#endregion
}
