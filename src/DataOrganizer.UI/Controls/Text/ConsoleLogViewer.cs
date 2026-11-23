using DataOrganizer.Infrastructure.Diagnostics.Extensions;
using DataOrganizer.UI.Extensions;
using Serilog.Events;
using System;

namespace DataOrganizer.UI.Controls.Text;

/// <summary>
/// <see cref="TextEditorBase" /> for the log, which paints the log levels and keeps the end of the text in view.
/// </summary>
internal sealed class ConsoleLogViewer : TextEditorBase
{
	#region Constructors
	/// <summary>
	/// Colors the short name of each log level with the brush of the level.
	/// </summary>
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
