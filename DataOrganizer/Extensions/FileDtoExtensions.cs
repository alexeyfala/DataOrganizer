using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using Shared.Interfaces;
using System.Text.Json;

namespace DataOrganizer.Extensions;

internal static class FileDtoExtensions
{
	#region Methods
	/// <summary>
	/// Returns the web name of the encoding chosen for the text of a file; <c>null</c> when the text takes the one found
	/// from the contents.
	/// </summary>
	public static string? FindChosenEncoding(this FileDto file, IJsonSerializer jsonSerializer)
	{
		if (file.EditorState is not { } json)
		{
			return null;
		}

		try
		{
			return jsonSerializer
				.Deserialize<FileEditorState>(json)
				.Encoding;
		}
		catch (JsonException)
		{
			// A state that cannot be read holds no choice, and the editor writes a new one once the file opens.
			return null;
		}
	}
	#endregion
}
