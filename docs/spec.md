# Empyrean Codex: LIGHTNING

Target platform: Windows 10+ (local-only)  
Primary toolchain: Visual Studio 2026 (C#/.NET), WPF (XAML), YAML  
**Naming rule:** do **not** use `EC.` prefixing in solution/project/namespaces. Use `LIGHTNING.*`.  
**YAML key rule:** all configuration and mapping keys are **ALL CAPS**.

---

## 0. Executive summary

Build a Windows desktop app that:

- Bootstraps **MetaF** locally on first run (install into an app-owned cache directory under `APP_WORK_DIR`) and pins the installed tool via `METAF_BUNDLE_SHA` (sha256 manifest hash of the installed MetaF bundle).
- Ensures MetaF prerequisites (notably **.NET 9**) are available:
  - Prefer system-installed compatible `.NET 9`.
  - If missing and `ALLOW_MANAGED_DEPENDENCIES: true`, provision a private `.NET` under `APP_WORK_DIR\deps\dotnet\` and run MetaF using it (never mutate PATH).
  - If missing and `ALLOW_MANAGED_DEPENDENCIES: false`, fail fast with a stable error code.
- Converts `.met → .af` and `.af → .met` via MetaF on user command (plan/preview before execution).
- Archives `.met` snapshots locally under `APP_WORK_DIR` (never in Git).
- Commits only `.af` and `mapping.yaml` into a user-selected Git repo.
- Enforces a strict **user-content** filesystem boundary (allowlisted roots, local-drive-only roots, reparse defense, final-path enforcement).
- Provides a one-click **Open Workspace** button that launches VS Code pointed at `AF_REPO_DIR`; required extensions are recommended and best-effort installed (fallback: recommendation-only); no `*.code-workspace` files are created. Optional `.vscode/extensions.json` management is gated by `MANAGE_VSCODE_WORKSPACE_FILES` and is performed only on explicit user action (never during conversion runs).

---

## 1. Product goals

### 1.1 Goals

- Safe, deterministic pipeline for converting `.met ↔ .af` via MetaF (pinned `METAF_BUNDLE_SHA`).
- Keep user Git repos clean: commit only `.af` + `mapping.yaml`; never commit `.met`.
- Make setup easy: install/pin MetaF into an app-owned cache.
- Support user-controlled organization: allow user to move/rename `.af` files within the repo; mapping must survive.
- Be conservative with filesystem and git operations: plan-first, stage-only-known outputs.

### 1.2 Non-goals (initially)

- No cloud sync.
- No multi-user collaboration features.
- No auto-push to remote by default.

---

## 2. Key requirements

### 2.1 Functional requirements

- FR-01: Configure directories and MetaF install preferences via UI.
- FR-02: Plan/preview a conversion job before execution.
- FR-03: Convert `.met → .af` and commit `.af` + `mapping.yaml` to repo.
- FR-04: Convert `.af → .met` back into `MET_SOURCE_DIR` (local only).
- FR-05: Archive `.met` snapshots locally under `APP_WORK_DIR` (never in Git).
- FR-06: Provide per-run logs and retain recent bootstrap logs.
- FR-07: Open Workspace launches VS Code pointed at `AF_REPO_DIR` and surfaces extension recommendations (best-effort install when possible).

### 2.2 Non-functional requirements

- NFR-01: Strict user-content filesystem boundary enforcement (Section 3).
- NFR-02: Deterministic outputs (semantic):
  - Given identical inputs (`.met` bytes + `mapping.yaml`), identical `config.yaml`, and identical `METAF_BUNDLE_SHA`, the app MUST produce identical:
    - `.af` file bytes
    - `AF_REL` allocations (relative paths inside `AF_REPO_DIR`)
    - `mapping.yaml` *semantic* content (same keys/values and ordering for all non-bookkeeping fields)
  - Bookkeeping fields are explicitly excluded from determinism:
    - `CREATED_UTC` (top-level)
    - `LAST_RUN_UTC` (per item)
  - The YAML emitter for `mapping.yaml` MUST be stable (key ordering + formatting) so that, aside from bookkeeping timestamps, byte-level diffs are minimized and deterministic.
  - Exclusions: archive contents/filenames/timestamps, logs, and git commit hashes are NOT required to be deterministic.
- NFR-03: Robust logging per run (plan + stdout/stderr + git output + results).
- NFR-04: Cancellation support for long batch jobs and long scans (planner/relink).
- NFR-05: Safe failure handling:
  - No partial git commit if job fails mid-run.
  - Clear UI error surface with access to logs.
- NFR-06: Distribution: `LIGHTNING.App` MUST be shippable as a *self-contained* Windows app (no preinstalled .NET Desktop Runtime required).

---

## 3. Security & safety boundary (hard constraint)

### 3.1 Allowed roots (explicit allowlist)

All **user-content** filesystem operations MUST be scoped to these canonical root directories:

**Internal (fixed, app-owned):**

- `APP_CONFIG_DIR` (read/write): `%LocalAppData%\EmpyreanCodex\LIGHTNING\`
  - stores `config.yaml`

**User-configured (explicit allowlist roots):**

- `MET_SOURCE_DIR` (read/write): source/target for `.met` files (flat; non-recursive)
- `AF_REPO_DIR` (read/write): git repo root containing `.af` files + `mapping.yaml`
- `APP_WORK_DIR` (read/write): app cache, logs, deps, temp, archive

### 3.2 Canonicalization & descendant checks

- Canonicalize via `Path.GetFullPath(...)` before every **user-content** operation.
- `MET_SOURCE_DIR`, `AF_REPO_DIR`, and `APP_WORK_DIR` MUST be absolute local drive paths (e.g., `C:\...`); UNC paths (`\\server\share\...`) are rejected.
- Descendant checks are **inclusive**: the allowed root itself is permitted.
- For every user-content read/write, enforce the allowlist twice:
  1) On the canonicalized input path, and
  2) On the **final resolved path** (after resolving symlinks/junctions/reparse points) to ensure the target cannot escape the allowed root.
- Reject any path whose final resolved target is not within an allowed root.

### 3.3 Reparse points (symlink/junction defense)

Policy goal: prevent path-escape and “surprise writes” via symlinks/junctions/reparse points.

`REPARSE_POINT_POLICY` supported values:

- `block_writes_allow_reads` (default):
  - Enumeration:
    - MUST NOT traverse reparse points when enumerating under allowlisted roots (treat as leaf entries).
  - Writes (user-content):
    - MUST block writes that encounter any reparse point along the path (including parent directories).
  - Reads (user-content):
    - Reads MAY follow reparse points, but MUST reject if the final resolved target escapes the allowed root.
    - Any read that crosses a reparse point MUST be logged.

- `block_all`:
  - Enumeration: MUST NOT traverse reparse points (same as above).
  - Reads/Writes (user-content): MUST reject if any reparse point is encountered along the path.

- SystemProbe:
  - May read outside the allowlist for tool discovery (read-only), but never writes outside the allowlist.

### 3.4 Git guardrails

- Never run `git add *`.
- Only stage explicit planned outputs: `git add <file1> <file2> ...`
- Only commit if all conversions succeed.
- Default: `GIT_PUSH_POLICY: never`.

---

## 4. Solution architecture (modular monolith)

### 4.1 Visual Studio solution layout (proposed)

- `LIGHTNING.slnx` (generated by Visual Studio 2026; canonical solution file)
  - `LIGHTNING.App` (WPF UI)
  - `LIGHTNING.Core` (domain logic: plans, mapping, policies)
  - `LIGHTNING.Adapters` (filesystem, process runner, git runner, VS Code launcher)
  - `LIGHTNING.Tests` (unit + integration tests)

### 4.2 Core components

- `ConfigService`: load/save `config.yaml` (YAML), validate required keys, warn on unknown keys.
- `BoundaryFileSystem`: allowlist enforcement + canonicalization + reparse policy enforcement.
- `MappingService`: load/save `mapping.yaml`, deterministic allocation, relink moved/renamed `.af`.
- `IngestPlanner`: produce immutable plan (what will be converted, where, what will be staged).
- `PipelineRunner`: execute plan, capture logs, enforce cancellation/timeout, guarantee no writes to `AF_REPO_DIR`, `MET_SOURCE_DIR`, or the git index on cancel (archives/logs under `APP_WORK_DIR` may remain).
- `MetaFInstaller`: install/pin MetaF under `APP_WORK_DIR`, provision prerequisites per config.
- `MetaFRunner`: invoke MetaF with captured stdout/stderr and exit code handling.
- `GitAdapter`: stage-only-planned outputs; commit with deterministic messages; optional per-repo author overrides.
- `SystemProbe`: discover `git` / `dotnet` / `code`, read versions, provide compatibility hints.
- `VSCodeLauncher`: open VS Code pointed at `AF_REPO_DIR`.

### 4.3 MVVM UI pattern

- Setup view: directory selection, MetaF status, validation feedback.
- Dashboard view: plan preview, run logs, commit summary.
- Errors are surfaced with stable codes + links to logs.

### 4.4 Error codes (stable)

- Format: `LIGHTNING_<CATEGORY>_<NNNN>`
  - `<CATEGORY>` is one of: `PLAN`, `FS`, `MAP`, `METAF`, `GIT`, `VSCODE`.
  - `<NNNN>` is a zero-padded integer (e.g., `0001`).
- Each surfaced error MUST include the code and a short human-readable message.
- Error code catalog:
  - MUST be embedded in the app build, and
  - MUST also be exported to disk under `APP_WORK_DIR\docs\error-codes.md` (updated when app version changes).

### 4.5 MetaF bootstrap rules

- Supported `METAF_INSTALL_MODE` values are fixed:
  - `repo_release_zip`
  - `dotnet_publish`
  - Unknown values are errors (stable error code).

- Install mode semantics (MUST be explicit and deterministic):
  - `repo_release_zip`:
    - `METAF_REF` MUST be a GitHub release tag (e.g., `v1.2.3`).
    - The installer MUST download the release asset for that tag, unpack into `METAF_ROOT_DIR` under `APP_WORK_DIR`, and record:
    - Deterministic asset selection:
      - The release MUST contain exactly one `.zip` asset; otherwise fail fast with a stable error code.
      - `METAF_ROOT_DIR`
      - `METAF_EXE_PATH`
      - `METAF_BUNDLE_SHA`
  - `dotnet_publish`:
    - `METAF_REF` MUST be a full git commit SHA (40 hex chars).
    - The installer MUST clone/fetch `METAF_GIT_URL`, checkout the exact commit, run `dotnet publish` with explicit parameters, and install publish outputs into `METAF_ROOT_DIR` under `APP_WORK_DIR`, then record:
      - Publish parameters (minimum): `-c Release -r win-x64 --self-contained false`
      - `METAF_ROOT_DIR`
      - `METAF_EXE_PATH`
      - `METAF_BUNDLE_SHA`

- Pinning (bundle manifest hash):
  - `METAF_BUNDLE_SHA` is `sha256:<hex>` computed from a canonical manifest of **all files** under `METAF_ROOT_DIR`.
  - Canonical manifest v1:
    - Enumerate files recursively under `METAF_ROOT_DIR` (do not include directories).
    - For each file, compute:
      - `REL = relative path from METAF_ROOT_DIR` with `\` normalized to `/`
      - `SIZE = file size in bytes`
      - `SHA = sha256(file_bytes)` as `sha256:<hex>`
    - Sort entries by `REL` (case-insensitive) ascending.
    - Serialize as UTF-8 lines: `REL\tSIZE\tSHA\n`
    - Compute `METAF_BUNDLE_SHA = sha256("v1\n" + <all lines>)`.
  - The bundle hash is the pinned tool identity used by NFR-02 and stored into `mapping.yaml` per item.

- Trust model for external tools:
  - MetaF is treated as a trusted tool **because it is pinned** (by `METAF_BUNDLE_SHA`) and installed into an app-owned cache.
  - The app does not attempt OS-level sandboxing of MetaF.
  - The app MUST validate and enforce boundaries for what it will publish/write (Sections 3, 10, 11).

- Retry policy:
  - If bootstrap fails, auto-retry **once** after deleting the MetaF cache/install directory under `APP_WORK_DIR` (clean-cache retry).

- Runtime prerequisites:
  - The app itself is self-contained.
  - If `ALLOW_MANAGED_DEPENDENCIES: false`, absence of a compatible system `.NET 9` runtime is a hard failure (stable error code).
  - If `ALLOW_MANAGED_DEPENDENCIES: true`, provision a private `.NET` under `APP_WORK_DIR` and run MetaF using it (no PATH mutation).

- Compatibility check before first MetaF run MUST include:
  - `dotnet --list-runtimes` (using the dotnet that will be used to run MetaF), and
  - A probe MetaF invocation (e.g., `--version` or equivalent) to confirm the pinned `METAF_EXE_PATH` is runnable.

---

## 5. Data model (YAML)

### 5.1 `config.yaml` (local, app workspace)

Stored under: `%LocalAppData%\EmpyreanCodex\LIGHTNING\config.yaml`

Parsing policy: **lenient** — unknown keys are ignored with warnings.

Required keys (minimum):

- `VERSION`
- `MET_SOURCE_DIR`
- `AF_REPO_DIR`
- `APP_WORK_DIR`

Validation rules:

- `MET_SOURCE_DIR`, `AF_REPO_DIR`, and `APP_WORK_DIR` MUST be absolute local drive paths (UNC paths rejected).
- `MET_SOURCE_DIR` is treated as **flat** (non-recursive); `.met` outputs are written directly under it.
- If `MANAGED_DOTNET_ROOT` is set, it MUST be within `APP_WORK_DIR` (descendant check); otherwise treat as config error.

MetaF:

- `METAF_GIT_URL`
- `METAF_INSTALL_MODE` (`repo_release_zip` or `dotnet_publish`; unknown values are errors)
- `METAF_REF`:
  - For `repo_release_zip`: GitHub release tag (e.g., `v1.2.3`)
  - For `dotnet_publish`: full git commit SHA (40 hex chars)
- `METAF_ROOT_DIR` (resolved directory under `APP_WORK_DIR` containing the installed MetaF bundle)
- `METAF_EXE_PATH` (resolved path inside `METAF_ROOT_DIR`)
- `METAF_BUNDLE_SHA` (resolved, pinned; `sha256:<hex>` of the canonical bundle manifest under `METAF_ROOT_DIR`)
Managed deps:

- `ALLOW_MANAGED_DEPENDENCIES` (`true/false`)
- `MANAGED_DOTNET_ROOT` (optional override; default under `APP_WORK_DIR\deps\dotnet\`; MUST be within `APP_WORK_DIR` if set)

VS Code convenience:

- `MANAGE_VSCODE_WORKSPACE_FILES` (`true/false`)
- `VSCODE_REQUIRED_EXTENSIONS` (list of extension IDs)
- `VSCODE_PREFERRED_PROFILE` (optional)

Git behavior:

- `REQUIRE_CLEAN_REPO` (`true/false`)
- `GIT_PUSH_POLICY` (default `never`)
- `CONFLICT_POLICY` (default `suffix`)

Per-repo git author identity (stored globally in `config.yaml`):

- `REPO_PROFILES[]`:
  - `AF_REPO_DIR` (canonical path string)
  - `GIT_AUTHOR_NAME` (required once set)
  - `GIT_AUTHOR_EMAIL` (required once set)

Archive retention:

- `ARCHIVE_RETENTION_DAYS` (default `30`; always retain within this window)
- `MAX_ARCHIVE_GB` (cap enforcement only for entries older than `ARCHIVE_RETENTION_DAYS`)

Filesystem safety:

- `REPARSE_POINT_POLICY` (default `block_writes_allow_reads`; supported: `block_writes_allow_reads`, `block_all`)

---

### 5.2 `mapping.yaml` (repo, committed; strict)

Stored under: `<AF_REPO_DIR>\mapping.yaml`

Parsing policy: **strict** — unknown keys are errors; required keys enforced.

#### Stable identity

- **Canonical fingerprint:** `MET_ID = sha256(met_file_bytes)` stored as `sha256:<hex>`.
- `MET_REL` is last-seen filename under `MET_SOURCE_DIR` (MUST be a filename only; no directory separators). It is used for `.af → .met` destination naming.

#### Required top-level keys (v1)

- `VERSION`
- `CREATED_UTC`
- `MET_SOURCE_ROOT_FINGERPRINT`
- `ITEMS` (array)

Bookkeeping timestamps:

- `CREATED_UTC` (top-level) and `LAST_RUN_UTC` (per item) are for audit/UI only and are explicitly excluded from NFR-02 determinism.

#### Required item keys (v1)

Each entry in `ITEMS[]`:

- `MET_ID` (`sha256:<...>`; canonical identity)
- `MET_REL` (`Foo.met`; last-seen filename)
- `AF_REL` (current relative location inside repo; **source of truth**)
- `AF_SHA256` (`sha256:<...>`; used for robust relink)
- `LAST_METAF_BUNDLE_SHA`
- `LAST_RUN_UTC`

#### Optional item keys (v1)

- `AF_SIZE_BYTES` (integer; used to prefilter relink candidates by size before hashing)

#### Uniqueness invariants (MUST)

On load and before write, enforce:

- `MET_ID` MUST be unique across `ITEMS[]`.
- `MET_REL` MUST be unique across `ITEMS[]` (case-insensitive comparison for Windows).
- `AF_REL` MUST be unique across `ITEMS[]` (case-insensitive comparison for Windows).
- If violated: treat as fatal load error (no auto-repair).

---

## 5.3 Fingerprinting `MET_SOURCE_DIR` (safety check)

Purpose: detect drift between the `MET_SOURCE_DIR` currently configured and the mapping’s stored origin.

- Store: `MET_SOURCE_ROOT_FINGERPRINT = sha256("v1|" + canonical_full_path + "|" + volume_serial_if_available)`
- On mismatch:
  - Conversion runs MUST NOT proceed (planner fails fast with a stable error code).
  - UI MUST offer a separate, explicit maintenance action: **Repair Mapping** (no conversions during repair).

**Repair Mapping** (explicit action):

- Recompute and update `MET_SOURCE_ROOT_FINGERPRINT` for the currently configured `MET_SOURCE_DIR`.
- Rescan `MET_SOURCE_DIR` (**flat; non-recursive**) and update `MET_REL` for any entries whose `MET_ID` matches a discovered `.met` file.
- Enforce mapping invariants (including `MET_REL` uniqueness) before write.
- Write `mapping.yaml` transactionally (temp then atomic replace).
- If the repair is canceled, no writes occur.

---

## 6. Deterministic path rules

### 6.0 Deterministic scan and plan ordering

These rules exist to eliminate filesystem-order nondeterminism.

- `.met` enumeration:
  - Scope: `MET_SOURCE_DIR` is **flat**; scan is **non-recursive**.
  - Match: file extension `.met` is case-insensitive.
  - Order: sort by full canonical path (case-insensitive) ascending.
- Duplicate `.met` content handling (same `MET_ID` from multiple `.met` files):
  - Choose the canonical `.met` deterministically:
    1) Prefer the candidate whose filename matches the existing mapping entry’s `MET_REL` (if any), else
    2) Choose the lexicographically smallest `MET_REL` (case-insensitive).
  - Ignore other candidates for conversion in this run; log a warning per ignored file.
- Plan display / serialization order:
  - Sort by `MET_REL` (case-insensitive), then by `MET_ID`.

### 6.1 `.af` initial placement (new `.met`)

When a `.met` has no mapping entry matching `MET_ID`:

- Default `AF_REL` is repo root: `<sanitized_met_basename>.af`
- Collisions are resolved via deterministic suffixing (`CONFLICT_POLICY: suffix`):
  - `Name.af`, then `Name-1.af`, `Name-2.af`, ...

### 6.2 Windows-safe sanitization algorithm (deterministic)

Input: `raw` (string), typically the `.met` basename (without extension).

Rules:

1) Normalize to Unicode NFC.
2) Replace any of the invalid filename characters with `_`:
   - `< > : " / \ | ? *`
   - control characters U+0000–U+001F
3) Trim trailing spaces and trailing periods from the entire name.
4) If the resulting name is empty, use `_`.
5) Reserved device names (case-insensitive) MUST be avoided:
   - `CON PRN AUX NUL COM1..COM9 LPT1..LPT9`
   - If reserved, prefix with `_` (e.g., `_CON`).
6) Length limit (basename only; excludes extension):
   - Hard maximum basename length is `180` characters, **including any later collision suffix** (e.g., `-12`).
   - If truncation is required, truncate the base portion and append `~` + `HASH12`, where:
     - `HASH12 = first 12 hex chars of sha256(UTF-8 bytes of the original raw basename)`
   - Collision suffix handling (to keep the suffix stable and within the limit):
     - When allocating a final name with suffix `S = "-<n>"`, reserve `len(S)` characters.
     - Ensure `(base_with_optional_~HASH12 + S).Length <= 180` by truncating the base portion as needed.
     - Always append `S` last (after any `~HASH12`).

### 6.3 Case normalization (Windows)

- Path comparisons for uniqueness/collisions treat paths case-insensitively.
- When writing `AF_REL`, preserve the chosen casing as generated/selected (first-seen).

---

### 6.4 Windows path length policy

- The app SHOULD attempt long-path I/O when necessary (e.g., via `\\?\` internally).
- If long-path I/O is unsupported on the current system/toolchain and the operation would exceed classic limits, the affected item MUST fail with a stable error code.
- Other items MAY continue to execute for diagnostics, but publish/stage/commit remains all-or-nothing (Section 11).

## 7. Mapping relink semantics (moves + renames)

When a mapped `AF_REL` no longer exists:

1) **Full repo scan** under `AF_REPO_DIR` to build the candidate set of `.af` files.
   - Enumeration MUST NOT traverse reparse points (Section 3.3).
   - Any `.af` file that is only reachable via a reparse point is ignored for relink purposes (logged as skipped).
   - UI MUST show progress and MUST allow cancel.
   - Cancel MUST abort plan generation for that run (no writes to `AF_REPO_DIR` or `MET_SOURCE_DIR`).
   - Candidate ordering for UI and selection MUST be stable: sort by `AF_REL` (case-insensitive) ascending.
2) Candidate selection:
   - Filter by basename match first.
   - If exactly one candidate remains → stage an update of `AF_REL` to that path; recompute and stage `AF_SHA256` (and `AF_SIZE_BYTES`) for that file.
   - If multiple candidates remain and `AF_SHA256` is present:
     - If `AF_SIZE_BYTES` is present, filter candidates by file size first, then compute full SHA256 only for size-matching candidates.
     - If exactly one hash match → stage updates to `AF_REL`, `AF_SHA256`, and `AF_SIZE_BYTES`.
   - If still ambiguous or no match:
     - Require user action per item: **Locate AF** or **Skip**.
       - Locate MUST restrict selection to a file under `AF_REPO_DIR` (and MUST reject selecting a reparse-point path segment).
       - On Locate, recompute and stage `AF_SHA256` (and `AF_SIZE_BYTES`) immediately.
       - On Skip, skip conversions for that mapping entry in this run (no mapping changes).
3) Any mapping change discovered during relink is staged in memory and is only persisted after a successful run (Section 11).

---

## 8. `.met` rename and replacement semantics

### 8.1 `.met` rename (same content, different filename)

If a `.met` file’s `MET_ID` matches an existing mapping entry but `MET_REL` differs:

- Stage an update of the mapping entry’s `MET_REL` to the new filename (no new row).
- Persist `MET_REL` changes only after a successful run (Section 11).

### 8.2 `.met` replacement (same filename, different content)

If a `.met` file has a `MET_REL` equal to an existing entry’s `MET_REL` but a different `MET_ID`:

- UI MUST prompt the user per item:
  - **Update existing entry:** treat this as a replacement-in-place:
    - Update the existing mapping entry’s `MET_ID` to the new content hash.
    - Keep `MET_REL` (must remain unique).
    - Keep `AF_REL`; the generated `.af` will overwrite that path (using the normal `.af` temp-write contract).
  - **Skip:** skip this `.met` for this run (no mapping changes; no outputs; logged).
- To ingest the new `.met` as a distinct new item, the user MUST rename it to a different filename (unique `MET_REL`) and rerun.

### 8.3 `.af → .met` output naming and overwrite semantics

- Destination path:
  - `MET_SOURCE_DIR` is flat; outputs are written to: `<MET_SOURCE_DIR>\<MET_REL>`.
  - `MET_REL` MUST be a filename only (no directory separators).
- If `MET_REL` is missing/invalid in a mapping entry (manual edit / corrupted mapping):
  - Auto-derive `MET_REL = <sanitized_af_basename>.met` (using Section 6.2 on the `.af` basename).
  - If the derived `MET_REL` conflicts with another entry’s `MET_REL` (case-insensitive), hard-fail this entry (no output) with a stable error code; other entries may continue.
  - If no conflict, stage the `MET_REL` update (persist only after a successful run).
- Overwrite policy:
  - If destination exists, archive it first (**before_overwrite**) and then overwrite.
  - After conversion, archive the produced `.met` (**produced**).

---

## 9. Archive policy

Archive root: `APP_WORK_DIR\.archive\met\<YYYY>\<MM>\<DD>\`

### 9.1 On-disk naming

Use: `<utc_timestamp>_<kind>_<name>_<sha256prefix>.met`

Where:

- `utc_timestamp` is UTC with millisecond precision, format: `YYYYMMDDTHHMMSSfffZ`
- Timestamp source: current system time converted to UTC at the moment the archive entry is finalized.
- Collision handling within the same day folder:
  - If the filename already exists, append a counter to the timestamp: `YYYYMMDDTHHMMSSfffZ-1`, `...-2`, etc.
- `kind` ∈ `{ingest, before_overwrite, produced}`
- `name` is sanitized for Windows filenames (Section 6.2).
- `sha256prefix` is the first 12 hex chars of the `.met` content hash.

### 9.2 Archive triggers

- Archive input `.met` at ingest (**ingest**).
- Archive destination `.met` before overwrite (**before_overwrite**), if destination exists.
- Archive produced `.met` after conversion (**produced**).

### 9.3 Retention and cap enforcement

- Always retain entries whose filename-derived UTC timestamps fall within the most recent `ARCHIVE_RETENTION_DAYS` (default `30` days).
- Archive size accounting considers only files under `APP_WORK_DIR\.archive\met\...`.
- Cap enforcement:
  - Never delete entries within the retention window.
  - If total archive size exceeds `MAX_ARCHIVE_GB` and there exist entries older than the retention window, delete the oldest entries older than the retention window until under cap.
  - If the archive exceeds `MAX_ARCHIVE_GB` due solely to entries within the retention window, do not delete; warn prominently in UI/logs.

---

## 10. Git behavior

### 10.1 Staging (explicit + deterministic)

- Never run `git add *`.
- Only stage explicit planned outputs: `mapping.yaml` and the planned `.af` files.
- Staging order MUST be deterministic: sort staged relative paths (case-insensitive) ascending.
- Do not stage until after all conversions succeed (Section 11).

### 10.2 Commit message (deterministic)

- Deterministic subject template:
  - `LIGHTNING: <direction> <count> files (MetaF <METAF_BUNDLE_SHA_SHORT>)`
  - `METAF_BUNDLE_SHA_SHORT = first 12 hex chars of METAF_BUNDLE_SHA` (after the `sha256:` prefix)
- Deterministic commit body template (always present):
  - `Direction: <direction>`
  - `Files: <count>`
  - `MetaF: <METAF_BUNDLE_SHA>`
  - `Staged:` followed by the staged file list, one per line, sorted as in staging.
- Optional user-provided details (if any) are appended after a blank line under `Notes:`.

### 10.3 Author identity

- On first run per `AF_REPO_DIR` (and whenever missing for the current `AF_REPO_DIR`), prompt for `GIT_AUTHOR_NAME` and `GIT_AUTHOR_EMAIL`.
- Persist in `config.yaml` under `REPO_PROFILES[]` for that `AF_REPO_DIR`.
- If not present (unexpected), fall back to system git config.

### 10.4 Dirty repo behavior

Definition of **clean repo** (must hold simultaneously):

- No modified tracked files.
- No staged changes.
- No untracked files.

If `REQUIRE_CLEAN_REPO: true` and repo is not clean:

- Abort the run (no staging, no commits).

If `REQUIRE_CLEAN_REPO: false` and repo is not clean:

- Proceed, but stage only planned outputs.
- Warn prominently in UI and logs that the working tree was not clean.

---

## 11. Cancellation and side effects contract (MUST)

Canceling during plan generation, relink scanning, or execution MUST NOT modify user-controlled working sets:

- `AF_REPO_DIR`: no `.af` writes, no `mapping.yaml` writes, and no `.vscode/...` writes.
- Git index: no staging and no commits.
- `MET_SOURCE_DIR`: no `.met` writes or overwrites.

Notes:

- Archives and logs under `APP_WORK_DIR` do **not** count as user-visible side effects and may remain (append-only). They do not need to be removed or rolled back on cancel.
- Any run-scoped staging/temp directories under `APP_WORK_DIR` MUST be deleted as part of cancel cleanup (best-effort).

### 11.1 Staging and atomic publish (MUST)

- During execution, all would-be writes to `AF_REPO_DIR` and `MET_SOURCE_DIR` MUST be routed to a run-scoped staging directory:
  - `STAGING_DIR = <APP_WORK_DIR>\.staging\<RUN_ID>\`
- File writes inside staging MUST follow the temp-then-rename contract:
  - Write `*.tmp`, flush/close, then atomic rename to final staging path.
- Publishing is a single finalization step that occurs only if:
  - The plan completes without cancellation, and
  - All planned conversions succeed.

Cancellation during publish:

- Cancellation MUST be honored up to the start of the publish step.
- Once publish begins, cancellation is deferred until publish completes (to avoid partial writes and to preserve the contract above).

`.met → .af` staging:

- Stage `.af` outputs under: `<STAGING_DIR>af\<AF_REL>`
- Stage the next `mapping.yaml` under: `<STAGING_DIR>mapping.yaml`

`.af → .met` staging:

- Stage `.met` outputs under: `<STAGING_DIR>met\<MET_REL>`

Publish step (only after success):

- Publish `.af` files (cross-volume safe):
  - For each staged `<STAGING_DIR>af\<AF_REL>`:
    - Write to a temp file in the target directory: `<AF_REPO_DIR>\<AF_REL>.tmp`
    - Flush/close, then atomic rename/replace to `<AF_REPO_DIR>\<AF_REL>`
- Publish `mapping.yaml`:
  - Transactional replace of `<AF_REPO_DIR>\mapping.yaml` (temp then atomic replace)
- Publish `.met` outputs (cross-volume safe):
  - For each destination `<MET_SOURCE_DIR>\<MET_REL>`:
    - If destination exists, archive it first (`before_overwrite`)
    - Write to a temp file in the target directory: `<MET_SOURCE_DIR>\<MET_REL>.tmp`
    - Flush/close, then atomic rename/replace to `<MET_SOURCE_DIR>\<MET_REL>`
    - Then archive the produced bytes (`produced`)

On cancel or any failure:

- Do not publish anything from staging.
- Delete `STAGING_DIR` during cleanup (best-effort).
- Any archives/logs already written under `APP_WORK_DIR` may remain.

### 11.2 Run exclusivity (concurrency lock)

- The app MUST prevent concurrent runs that share the same tuple: (`AF_REPO_DIR`, `MET_SOURCE_DIR`).
- Acquire a single-instance lock at run start (e.g., named mutex + lock file under `APP_WORK_DIR\locks\`).
  - The lock key MUST be derived from the canonical full paths of both roots (e.g., `sha256("v1|" + AF_REPO_DIR + "|" + MET_SOURCE_DIR)`), to avoid false negatives due to path formatting.
- If the lock cannot be acquired, fail fast with a stable error code (no partial work).

### 11.3 Plan snapshot integrity

- The plan MUST capture a content hash of `mapping.yaml` at plan time.
- At execution start, re-hash `mapping.yaml`; if it differs, abort execution and require the user to re-plan (stable error code).

## 12. Definition of Done (MVP)

- User configures directories and MetaF source/ref.
- App installs/builds MetaF into its own cache, pins `METAF_BUNDLE_SHA`, records `METAF_ROOT_DIR` and `METAF_EXE_PATH`.
- `.met → .af` converts and commits only planned `.af` + `mapping.yaml`.
- `.af → .met` converts back into `MET_SOURCE_DIR` safely with archiving.
- Mapping survives `.af` moves/renames via relink + Locate/Skip (Locate restricted to `AF_REPO_DIR`).
- Archive retention respects `ARCHIVE_RETENTION_DAYS` minimum + `MAX_ARCHIVE_GB` cap for older entries.
- User-content I/O never escapes allowlisted roots; system probes are read-only outside allowlist.
- VS Code button opens the repo folder; required extensions are recommended and best-effort installed (if install fails, fall back to recommendation-only); no `*.code-workspace` files are created. Optional `.vscode/extensions.json` management is gated by `MANAGE_VSCODE_WORKSPACE_FILES` and is performed only on explicit user action.
