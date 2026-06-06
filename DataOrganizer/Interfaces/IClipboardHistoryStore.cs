using DataOrganizer.DTO.Clipboard;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Encrypts the clipboard history to disk and decrypts it back, holding the data
/// encryption key for the current session.
/// </summary>
public interface IClipboardHistoryStore : IDisposable
{
	#region Properties
	/// <summary>
	/// <c>True</c> once a key has been unlocked / created for this session.
	/// </summary>
	bool IsUnlocked { get; }

	/// <summary>
	/// <c>True</c> when a wrapped key already exists on disk (a previous session persisted history).
	/// </summary>
	bool KeyFileExists { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Erases both the journal and the wrapped key, and forgets the in-session key.
	/// </summary>
	void EraseAll();

	/// <summary>
	/// Erases only the journal file, keeping the wrapped key so the session can keep persisting.
	/// </summary>
	void EraseHistory();

	/// <summary>
	/// Encrypts and writes <paramref name="entries" /> to disk. No-op when not unlocked.
	/// </summary>
	Task SaveAsync(IReadOnlyList<ClipboardHistoryEntryBase> entries, CancellationToken token = default);

	/// <summary>
	/// Unlocks an existing key with <paramref name="password" /> (loading the previous journal),
	/// or creates and stores a new key when none exists yet.
	/// </summary>
	Task<ClipboardHistoryUnlockResult> TryUnlockAsync(byte[] password, CancellationToken token = default);
	#endregion
}
