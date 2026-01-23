using System;

namespace DataOrganizer.Interfaces;

public interface IFileEditor
{
	#region Properties
	/// <summary>
	/// File identifier.
	/// </summary>
	Guid FileId { get; set; }

	/// <summary>
	/// Initial properties.
	/// </summary>
	string? InitialProperties { get; set; }

	/// <summary>
	/// Returns <c>True</c> if editor is initialized.
	/// </summary>
	bool IsInitialized { get; }

	/// <summary>
	/// Callback to set object's properties.
	/// </summary>
	Action<string>? SetPropertiesCallback { get; set; }

	/// <summary>
	/// Callback to set object's updated date.
	/// </summary>
	Action<DateTime>? SetUpdatedDateCallback { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// Performs initialization.
	/// </summary>
	void Initialize();
	#endregion
}
