namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Text of a log that grows at its end and keeps no undo history.
/// </summary>
public interface ILogText
{
	#region Methods
	/// <summary>
	/// Appends a record to the end, then removes leading lines so that at most <paramref name="maxLines" /> remain.
	/// </summary>
	void Append(string record, int maxLines);

	/// <summary>
	/// Removes the whole text.
	/// </summary>
	void Clear();
	#endregion
}
