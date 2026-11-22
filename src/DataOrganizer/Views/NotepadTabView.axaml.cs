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
	/// <summary>
	/// Initializes a new instance of the <see cref="NotepadTabView" /> class.
	/// </summary>
	public NotepadTabView() => InitializeComponent();

	/// <summary>
	/// Initializes a new instance of the <see cref="NotepadTabView" /> class.
	/// </summary>
	public NotepadTabView(NotepadTabViewModel viewModel) : this() => DataContext = viewModel;
	#endregion

	#region Methods
	/// <summary>
	/// Removes the syntax highlighting of the editor, whose tokenizers keep it in memory.
	/// </summary>
	public void Dispose() => Editor.Dispose();
	#endregion
}
