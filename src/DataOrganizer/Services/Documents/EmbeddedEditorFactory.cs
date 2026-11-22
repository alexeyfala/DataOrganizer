using DataOrganizer.Database.Entities.Enums;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.ViewModels;

namespace DataOrganizer.Services.Documents;

/// <inheritdoc cref="IEmbeddedEditorFactory" />
public sealed class EmbeddedEditorFactory : IEmbeddedEditorFactory
{
	#region Data
	/// <inheritdoc cref="IViewModelFactory" />
	private readonly IViewModelFactory _viewModelFactory;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="EmbeddedEditorFactory" /> class.
	/// </summary>
	public EmbeddedEditorFactory(IViewModelFactory viewModelFactory) => _viewModelFactory = viewModelFactory;
	#endregion

	#region Methods
	/// <inheritdoc />
	public EmbeddedEditorViewModelBase? Create(FileDto file)
	{
		EmbeddedEditorViewModelBase? viewModel = file.Kind switch
		{
			EntityKind.Dataset => _viewModelFactory.CreateViewModel<DatasetEditorViewModel>(),
			EntityKind.File => _viewModelFactory.CreateViewModel<EmbeddedFileEditorViewModel>(),
			_ => null
		};

		if (viewModel is null)
		{
			return null;
		}

		viewModel.Initialize(new()
		{
			FileId = file.Id,
			FileName = file.Name,
			InitialEditorState = file.EditorState,
			KeeperId = file.FindUnlockedKeeper()?.Id,
			Kind = file.Kind,
			SetEditorStateCallback = state => file.EditorState = state,
			SetUpdatedAtCallback = updatedAt => file.UpdatedAt = updatedAt
		});

		return viewModel;
	}
	#endregion
}
