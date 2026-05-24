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
	public int GetAppProcessesCount()
	{
		Process process = Process.GetCurrentProcess();

		string? currentPath = process
			.MainModule?
			.FileName;

		return Process.GetProcessesByName(process.ProcessName).Count(x =>
		{
			try
			{
				return x.MainModule is not null && x.MainModule.FileName == currentPath;
			}
			catch
			{
				return false;
			}
		});
	}

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
	public void OpenAppDirectory()
	{
		switch (AppUtils.CurrentOs)
		{
			case OperatingSystemType.Windows:
				Process.Start(AppUtils.PlatformSpecificExplorer, "/select, " + Environment.ProcessPath);
				break;

			case OperatingSystemType.Linux:
				Process.Start(AppUtils.PlatformSpecificExplorer, Environment.CurrentDirectory);
				break;

			case OperatingSystemType.MacOs:
				Process.Start(AppUtils.PlatformSpecificExplorer, GetMacOsReveal(AppDomain.CurrentDomain.BaseDirectory));
				break;

			default:
				throw new NotImplementedException();
		}
	}

	/// <inheritdoc />
	public void OpenDirectory(string directoryPath)
	{
		Process.Start(
			AppUtils.PlatformSpecificExplorer,
			directoryPath.SurroundWithQuotesIfNeeded());
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
	/// Combines the path with the folder expansion argument for <see cref="OperatingSystemType.MacOs" />.
	/// </summary>
	private static string GetMacOsReveal(string argument) => $@"-R ""{argument}""";

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
