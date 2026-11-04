using FileExecution.Interfaces;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace FileExecution.Services;

/// <inheritdoc cref="IProcessManager" />
[ExcludeFromCodeCoverage(Justification = "Starts and stops real processes")]
public sealed class ProcessManager : IProcessManager
{
	#region Data
	/// <inheritdoc cref="IPlatform" />
	private readonly IPlatform _platform;
	#endregion

	#region Constructors
	public ProcessManager(IPlatform platform) => _platform = platform;
	#endregion

	#region Methods
	/// <inheritdoc />
	public IExecutingFileProcess? OpenFile(string filePath)
	{
		return StartExecutingFileProcess(_platform.IsWindows
			? CreateWindowsProcess(filePath)
			: CreateNonWindowsProcess(filePath));
	}

	/// <inheritdoc />
	public IExecutingFileProcess? OpenFile(string appPath, string filePath)
	{
		return StartExecutingFileProcess(new()
		{
			StartInfo = new(appPath.SurroundWithQuotesIfNeeded())
			{
				Arguments = filePath.SurroundWithQuotesIfNeeded(),
				UseShellExecute = false
			}
		});
	}

	/// <inheritdoc />
	public void OpenWithShell(string target)
	{
		using Process? process = Process.Start(new ProcessStartInfo
		{
			FileName = target,
			UseShellExecute = true
		});
	}

	/// <inheritdoc />
	public Process StartProcess(string fileName) => Process.Start(fileName);
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a process to open a file on an operating system other than <see cref="OperatingSystemKind.Windows" />.
	/// </summary>
	private static Process CreateNonWindowsProcess(string filePath) => new()
	{
		StartInfo = new()
		{
			Arguments = filePath.SurroundWithQuotesIfNeeded(),
			CreateNoWindow = true,
			FileName = PlatformInfo.FileOpener,
			UseShellExecute = false
		}
	};

	/// <summary>
	/// Creates a process to open a file in the <see cref="OperatingSystemKind.Windows" /> operating system.
	/// </summary>
	private static Process CreateWindowsProcess(string filePath) => new()
	{
		StartInfo = new(filePath.SurroundWithQuotesIfNeeded())
		{
			UseShellExecute = true
		}
	};

	/// <summary>
	/// Starts <paramref name="process" /> that opens a file and hands it over; <c>null</c>, with the process released, when
	/// it did not start.
	/// </summary>
	private static ExecutingFileProcess? StartExecutingFileProcess(Process process)
	{
		try
		{
			if (process.Start())
			{
				return new(process);
			}
		}
		catch
		{
			process.Dispose();

			throw;
		}

		process.Dispose();

		return null;
	}
	#endregion
}
