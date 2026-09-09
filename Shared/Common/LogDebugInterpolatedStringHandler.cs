using Serilog;
using Serilog.Events;
using System.Runtime.CompilerServices;

namespace Shared.Helpers;

/// <summary>
/// Interpolated string handler that builds the message only when <see cref="LogEventLevel.Debug" /> logging is enabled.
/// </summary>
[InterpolatedStringHandler]
public ref struct LogDebugInterpolatedStringHandler
{
	#region Data
	/// <summary>
	/// <c>True</c> when logging is enabled.
	/// </summary>
	private readonly bool _isEnabled;

	/// <inheritdoc cref="DefaultInterpolatedStringHandler" />
	private DefaultInterpolatedStringHandler _inner;
	#endregion

	#region Constructors
	public LogDebugInterpolatedStringHandler(
		int literalLength,
		int formattedCount,
		ILogger target,
		out bool handlerIsValid)
	{
		_isEnabled = target.IsEnabled(LogEventLevel.Debug);

		handlerIsValid = _isEnabled;

		_inner = _isEnabled
			? new DefaultInterpolatedStringHandler(literalLength, formattedCount)
			: default;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Don't remove this method!<br />
	/// Appends an interpolated hole value.
	/// </summary>
	/// <remarks>
	/// Invoked by compiler-generated code when expanding an interpolated string; not called explicitly.
	/// </remarks>
	public void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);

	/// <summary>
	/// Don't remove this method!<br />
	/// Appends a literal fragment of the interpolated string.
	/// </summary>
	/// <remarks>
	/// Invoked by compiler-generated code when expanding an interpolated string; not called explicitly.
	/// </remarks>
	public void AppendLiteral(string value) => _inner.AppendLiteral(value);

	/// <summary>
	/// Returns the built message and releases the rented buffer.
	/// </summary>
	public string ToStringAndClear() => _inner.ToStringAndClear();
	#endregion
}
