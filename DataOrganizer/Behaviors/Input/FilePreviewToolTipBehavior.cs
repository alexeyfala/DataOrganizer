using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using FileContents.Interfaces;
using Hierarchy.Dto;
using System.Threading.Tasks;

namespace DataOrganizer.Behaviors.Input;

/// <summary>
/// Shows the start of the text of the file of <see cref="Behavior{T}.AssociatedObject" /> in a tip once the pointer
/// rests over it, and takes the tip away when the pointer leaves.
/// </summary>
internal sealed class FilePreviewToolTipBehavior : PointerHoverBehaviorBase
{
	#region Data
	/// <summary>
	/// <c>True</c> while a preview is read, so that another hover does not read it again meanwhile.
	/// </summary>
	private bool _isReading;
	#endregion

	#region Properties
	/// <inheritdoc cref="IFilePreviewReader" />
	public IFilePreviewReader? Reader
	{
		get => GetValue(ReaderProperty);
		set => SetValue(ReaderProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <inheritdoc cref="Reader" />
	public static readonly StyledProperty<IFilePreviewReader?> ReaderProperty = AvaloniaProperty
		.Register<FilePreviewToolTipBehavior, IFilePreviewReader?>(nameof(Reader));
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnHover() => _ = ShowPreviewAsync();

	/// <inheritdoc />
	protected override void OnPointerExited()
	{
		if (AssociatedObject is not Control target)
		{
			return;
		}

		ToolTip.SetTip(target, null);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads the preview of the file and shows it in an open tip.
	/// </summary>
	private async Task ShowPreviewAsync()
	{
		if (_isReading
			|| AssociatedObject is not Control { DataContext: FileDto file } target
			|| Reader is not { } reader)
		{
			return;
		}

		_isReading = true;

		try
		{
			if (await reader
				.ReadPreviewAsync(file)
				.ConfigureAwait(true) is not { } text)
			{
				return;
			}

			ToolTip.SetTip(target, text);

			ToolTip.SetIsOpen(target, true);
		}
		finally
		{
			_isReading = false;
		}
	}
	#endregion
}
