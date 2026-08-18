# Security Policy

Data Organizer handles password-protected, encrypted user data, so security
reports are taken seriously and are always welcome.

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues,
discussions, or pull requests.**

Instead, report them privately through GitHub's built-in reporting:

1. Open the [**Security**](https://github.com/alexeyfala/DataOrganizer/security)
   tab of this repository.
2. Click **Report a vulnerability**.
3. Fill in the advisory form with the details listed below.

This opens a private channel visible only to the maintainer. The report stays
confidential until a fix is available and an advisory is published.

## What to Include

To help reproduce and assess the issue, please provide as much as you can:

- A description of the vulnerability and its potential impact.
- Steps to reproduce, or a proof of concept.
- The affected version, commit, or build.
- Your operating system and .NET runtime version.
- Any suggested mitigation, if you have one.

## Response

- Reports are acknowledged within 7 days.
- You will be kept informed as the issue is investigated and resolved.
- With your consent, your contribution will be credited in the published
  advisory.

## Supported Versions

Security fixes are applied to the latest release. Older versions are not
maintained — please update to the most recent version before reporting an
issue.

## Known Limitations

The following are known and accepted, so there is no need to report them.

- **Erasing is best-effort.** Overwriting a file before deleting it does not
  guarantee the old bytes are unrecoverable: SSD wear leveling, copy-on-write
  file systems, snapshots and shadow copies may keep earlier versions.
- **SQLite rollback journal.** During a transaction the journal holds plaintext
  pre-images of the pages being replaced. `journal_mode = MEMORY` would avoid
  the file at the cost of a corrupted database after a crash — an unacceptable
  trade.
- **Traces left by other applications.** A file opened externally is written
  decrypted into a sandbox folder and erased when it is closed, but the opening
  application keeps its own autosave and recovery copies, and the operating
  system records the file name in recent items and jump lists.
- **Record names are not encrypted.** Encryption covers contents and notes.
  Names are stored as plain text in the database and appear in exported files.
- **Encrypted values are not bound to the record holding them.** A value is tied
  to the key of its protected folder and to the kind of field it belongs to, but
  not to the record it is stored in. Whoever can write to the database file can
  move an encrypted value between records of the same protected folder, and the
  application opens it without noticing the move. Binding a value to its record
  would keep an import from renumbering records, which the import has to do.
- **An export is as sensitive as the database.** An exported file carries the
  same encrypted contents and the same wrapped key, so a copy left outside the
  application allows the password to be guessed offline, at the pace of whoever
  holds the file.
- **The strength of a password is shown, not required.** A new password is
  rated as it is typed, but only its length is enforced, and the rating is a
  heuristic that can be too kind. A password weak enough to be guessed offline
  undermines the whole scheme, whatever the key derivation costs.
- **Decrypted data in memory.** While the session is unlocked, keys and
  decrypted contents live in RAM and may reach the page or hibernation file.
  Auto-lock shortens that window.
- **The password input leaves fragments.** The entered password is held in
  pinned memory and every value the input field replaces is wiped, but some
  strings are out of reach: the one carried by each keystroke event, the one
  handed over by the clipboard on paste, and any copy the garbage collector
  makes while moving objects.
