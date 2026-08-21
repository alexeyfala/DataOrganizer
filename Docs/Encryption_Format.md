# Encrypted blob formats

Reference for the on-the-wire layout of everything `EncryptionService` writes, and for the rule that
keeps those layouts readable. Written for whoever edits the encryption stack; the user-facing security
statement lives in [SECURITY.md](../SECURITY.md).

## The rule

The first byte of every blob is its format version. **Any change to a field layout, to a key
derivation, to the composition of the associated data or to the numbering of `ContentPurpose`
requires a new version byte.** A version byte, once used, is never reused for a different layout.

The application reads only the current version of each format. A blob marked with any other version
is refused before a key is derived, and no reader for a retired version exists — encrypted data from
an older build of the application cannot be opened.

The rule exists because it has already been broken once: commit `ff35173` gave format `0x01` six bytes
of header and gave every format associated data, while leaving the version byte at `0x01`. Blobs
written before that date stopped opening, and nothing in the build said so.

## Taken version bytes

| Version | Secret | Written by | Where it lives |
|---|---|---|---|
| `0x01` | Password | `Encrypt` | `FolderModel.EncryptedDek`, the `History.key` file of the clipboard history |
| `0x02` | Data encryption key | `EncryptWithDek` | `FileModel.Contents`, the notes of files and folders, the `History.bin` journal |
| `0x03` | Session secret | `EncryptWithSessionId` | Memory only: the wrappers `SessionKeyStore` holds |

Next free byte: `0x04`.

## Common layout

    [version:1][header][salt][check][nonce][ciphertext+tag]

Every field but the version and the nonce is present only in the formats that need it; a field of
size zero is absent rather than empty, so offsets shift accordingly (`BlobFormat`).

The cipher is XChaCha20-Poly1305 as libsodium implements it, through NSec: a nonce of 24 bytes and a
tag of 16 bytes appended to the ciphertext.

### Associated data

    [label:20]["DataOrganizer.Aad.v1"][purpose:1][version:1][header][salt][check]

The label and the purpose byte come from `ContentIdentity`; the rest is the prefix of the blob up to
the nonce. The nonce itself stays out — the algorithm authenticates it on its own. The identifier of
the object is deliberately **not** authenticated, because an import renumbers every object; the
limitation is recorded in `SECURITY.md`.

`ContentPurpose` values are part of the authenticated data, so their numbering is as binding as the
version byte:

| Value | Purpose |
|---|---|
| 1 | Contents of a file |
| 2 | Note of a file or of a folder |
| 3 | Data encryption key of a password keeper |
| 4 | Data encryption key of the clipboard history |
| 5 | Journal of the clipboard history |

Next free value: 6.

## `0x01` — password

    [version:1][header:6][salt:16][check:16][nonce:24][ciphertext:32][tag:16]   = 111 bytes

The plaintext is always a single 32-byte key, therefore the blob has exactly one valid length.

The header records the cost the derivation ran with, so raising the cost of new blobs leaves the
existing ones readable:

    [memory:4 little-endian, kibibytes][passes:1][parallelism:1]

Argon2id derives 48 bytes from the password and the salt: the first 32 become the AEAD key, the last
16 become the check value stored in the blob. The check value is compared in fixed time before the
ciphertext is touched, which is what tells a wrong password from damaged data. Bounds accepted when
reading the header live in `Argon2Settings`; new blobs are written with the moderate level of
libsodium (256 MiB, 3 passes, 1 lane).

## `0x02` — data encryption key

    [version:1][nonce:24][ciphertext:n][tag:16]

The secret is the key itself, so there is no header, no salt and nothing to prove: the tag is the
only verdict. The plaintext may be of any size, and this is the only format that carries content
rather than key material.

## `0x03` — session secret

    [version:1][salt:16][nonce:24][ciphertext:32][tag:16]                       = 89 bytes

The plaintext is always a single 32-byte key. HKDF-SHA256 derives the AEAD key from the session
secret, the per-blob salt and the fixed info label `DataOrganizer.SessionDek.v1`; a memory-hard
derivation would buy nothing here, as the session secret is high-entropy random material. There is
no check value: a secret of the running session is never the wrong one.

Blobs of this format never reach the disk — they exist while the process holds unlocked keys.

## What the build enforces

- `EnsureLayout` refuses a blob that is too short, that opens with a foreign version byte, or whose
  length does not fit the fixed plaintext size of its format.
- `BlobScheme` carries the format together with the derivation that opens it, so a format cannot be
  read with the derivation of another.
- `EncryptedBlobs_Keep_Their_Layout` pins the version byte and the exact length of all three formats.
  Note what it cannot do: an edit that changes a layout **and** the test together passes, so the rule
  above stays a matter of discipline, not of tooling.
