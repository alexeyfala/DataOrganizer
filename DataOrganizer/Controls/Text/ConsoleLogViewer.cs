using DataOrganizer.Extensions;
using Serilog.Events;
using System;

namespace DataOrganizer.Controls.Text;

/// <summary>
/// <see cref="TextEditorBase" /> for the log, which paints the log levels and keeps the end of the text in view.
/// </summary>
internal sealed class ConsoleLogViewer : TextEditorBase
{
	#region Constructors
	public ConsoleLogViewer()
	{
		foreach (LogEventLevel level in Enum.GetValues<LogEventLevel>())
		{
			TextArea
				.TextView
				.LineTransformers
				.Add(new WordOccurrenceColorizer(level.ToShort(), level.ToBrush()));
		}
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);

		ScrollToEnd();
	}
	#endregion
}
