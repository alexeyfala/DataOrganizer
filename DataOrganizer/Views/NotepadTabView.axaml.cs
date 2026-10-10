using Avalonia.Controls;
using DataOrganizer.ViewModels;
using System;

namespace DataOrganizer.Views;

/// <summary>
/// Editor of the text of a tab of the notepad.
/// </summary>
internal sealed partial class NotepadTabView : UserControl, IDisposable
{
	#region Constructors
	public NotepadTabView() => InitializeComponent();

	public NotepadTabView(NotepadTabViewModel viewModel) : this() => DataContext = viewModel;
	#endregion

	#region Methods
	/// <summary>
	/// Removes the syntax highlighting of the editor, whose tokenizers keep it in memory.
	/// </summary>
	public void Dispose() => Editor.Dispose();
	#endregion
}
