using DataOrganizer.Interfaces;
using Shared.Common;
using Shared.Enums;
using Shared.Extensions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DataOrganizer.Services;

/// <inheritdoc cref="IProcessUtils" />
public sealed class ProcessUtils : IProcessUtils
{
	#region Methods
	/// <inheritdoc />
	public Process[] GetChildProcesses(int parentProcessId)
	{
		return [.. Process
			.GetProcesses()
			.Where(x => GetParentProcessId(x, out int processId) && processId == parentProcessId)];

		//// Nuget: System.Management
		//return new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={parentProcessId}")
		//	.Get()
		//	.Cast<ManagementObject>()
		//	.Select(x => Process.GetProcessById(Convert.ToInt32(x["ProcessID"])))
		//	.ToArray();
	}

	/// <inheritdoc />
	public bool IsProcessExists(int processId)
	{
		return Process
			.GetProcesses()
			.Any(x => x.Id == processId);
	}

	/// <inheritdoc />
	public void KillProcess(int processId)
	{
		Process
			.GetProcessById(processId)
			.Kill();
	}

	/// <inheritdoc />
	public bool StartProcess(string filePath, out int processId)
	{
		using Process process = AppUtils.IsWindows
			? CreateWindowsProcess(filePath)
			: CreateNonWindowsProcess(filePath);

		if (process.Start())
		{
			processId = process.Id;

			return true;
		}

		processId = default;

		return false;
	}

	/// <inheritdoc />
	public bool StartProcess(
		string appPath,
		string fileArgument,
		out int processId)
	{
		using Process process = new()
		{
			StartInfo = new(appPath.SurroundWithQuotesIfNeeded())
			{
				Arguments = fileArgument.SurroundWithQuotesIfNeeded(),
				UseShellExecute = false
			}
		};

		if (process.Start())
		{
			processId = process.Id;

			return true;
		}

		processId = default;

		return false;
	}

	/// <inheritdoc />
	public Process StartProcess(string fileName) => Process.Start(fileName);
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a process to open a file on an operating system other than <see cref="OperatingSystemType.Windows" />.
	/// </summary>
	private static Process CreateNonWindowsProcess(string filePath) => new()
	{
		StartInfo = new()
		{
			Arguments = filePath.SurroundWithQuotesIfNeeded(),
			CreateNoWindow = true,
			FileName = AppUtils.PlatformSpecificExplorer,
			UseShellExecute = false
		}
	};

	/// <summary>
	/// Creates a process to open a file in the <see cref="OperatingSystemType.Windows" /> operating system.
	/// </summary>
	private static Process CreateWindowsProcess(string filePath) => new()
	{
		StartInfo = new(filePath.SurroundWithQuotesIfNeeded())
		{
			UseShellExecute = true
		}
	};

	/// <summary>
	/// Returns the parent process ID.
	/// </summary>
	private static bool GetParentProcessId(Process process, out int parentProcessId)
	{
		parentProcessId = 0;

		try
		{
			PropertyInfo? property = process
				.GetType()
				.GetProperty("ParentProcessId", BindingFlags.NonPublic | BindingFlags.Instance);

			if (property?.GetValue(process) is int value)
			{
				parentProcessId = value;

				return true;
			}

			return false;
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex.ToStringDemystified());

			return false;
		}
	}
	#endregion
}
