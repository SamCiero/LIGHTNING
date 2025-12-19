# Empyrean Codex: LIGHTNING

Target platform: Windows 10+ (local-only).

**Naming rule:** do **not** use `EC.` prefixing in solution/project/namespaces for LIGHTNING; use `LIGHTNING.*` (or `Lightning.*`) instead.

---

## 0. Executive summary

Build a Windows 10+ desktop app that:

- Bootstraps **MetaF** locally on first run (installed into an app-owned cache under `APP_WORK_DIR`).
- Pins the installed MetaF bundle by `METAF_BUNDLE_SHA` (a `sha256:` hash of a canonical manifest of the installed bundle).
- Ensures MetaF runtime prerequisites are available without mutating global machine state:
  - Prefer a compatible system-installed `.NET` runtime when available.
  - If missing, provision an app-managed `.NET` runtime under `APP_WORK_DIR\deps\dotnet\` and run MetaF using it (no PATH mutation).
- Converts `.met → .af` and `.af → .met` via MetaF on explicit user command, using **plan/preview** before execution.
- Archives `.met` snapshots locally under `APP_WORK_DIR` (never committed to Git).
- Commits only `.af` and `mapping.yaml` into a user-selected Git repo (`AF_REPO_DIR`); pushes are policy-driven.
- Enforces a strict **user-content** filesystem boundary (explicit allowlisted roots, local fixed volumes only, canonicalization, descendant checks, reparse-point defense, final-path enforcement).
- Provides a one-click **Open Workspace** action that launches VS Code for `AF_REPO_DIR`; workspace file management and extension installation are explicitly gated and best-effort (never blocks conversions).

---

## 1. Product goals

### 1.1 Goals

- Safe, deterministic conversion flows `.met ↔ .af` with a plan/preview step.
- Local-only operation; no cloud dependency.
- Strong boundary enforcement: never escape allowlisted roots.
- Git-managed `.af` outputs + `mapping.yaml` as the committed record.
- `.met` archives are local-only (app-owned), never committed.
- VS Code workspace convenience (explicitly gated and best-effort).

### 1.2 Non-goals (initially)

- Cross-platform support (Windows-only for now).
- Background/automatic conversions (user-initiated runs only).
- Full in-app `.af` editing (the repo is edited in external tools, e.g., VS Code).

---

## 2. Key requirements

### 2.1 Functional requirements

- FR-01: Setup UI to configure directories and MetaF install mode/ref.
- FR-02: Bootstrap MetaF into `APP_WORK_DIR` and pin it by `METAF_BUNDLE_SHA`.
- FR-03: Build a deterministic plan for each run; show a preview; execute only after explicit approval.
- FR-04: `.met → .af` conversion with mapping allocation and relink behavior.
- FR-05: `.af → .met` conversion with safe overwrite and local archiving.
- FR-06: Provide per-run logs and retain recent bootstrap logs.
- FR-07: Open Workspace launches VS Code pointed at `AF_REPO_DIR` and surfaces extension recommendations (best-effort install when possible).

### 2.2 Non-functional requirements

- NFR-01: Strict user-content filesystem boundary enforcement (allowlist + canonicalization + final-path + reparse policy).
- NFR-02: Determinism: same inputs + same pinned MetaF produce the same `.af` results when outputs are explicitly targeted.
- NFR-03: Safe Git behavior: stage only planned outputs; never `git add *`; policy-driven push.
- NFR-04: Cancellation safety: cancel must not publish; staging cleanup is best-effort; archives/logs may remain under `APP_WORK_DIR`.

---

## 3. Security & safety boundary (hard constraint)

### 3.1 Allowed roots (explicit allowlist)

All **user-content** filesystem operations MUST be scoped to these canonical root directories:

**Internal (fixed, app-owned):**

- `APP_CONFIG_DIR` (read/write): `%LocalAppData%\EmpyreanCodex\LIGHTNING\`
  - Stores `config.yaml`.

**User-configured (explicit allowlist roots):**

- `MET_SOURCE_DIR` (read/write): source/target for `.met` files (**flat; non-recursive**).
- `AF_REPO_DIR` (read/write): git repo root containing `.af` files + `mapping.yaml`.
- `APP_WORK_DIR` (read/write): app cache, logs, deps, temp, archive.

Policy goal: prevent path-escape and “surprise writes” via traversal tricks, reparse points, and unexpected volume types.

### 3.2 Canonicalization, volume constraints, and descendant checks

- Canonicalize via `Path.GetFullPath(path)` before every **user-content** operation.
- `MET_SOURCE_DIR`, `AF_REPO_DIR`, and `APP_WORK_DIR` MUST be absolute local drive paths (e.g., `C:\<PATH>`):
  - UNC paths (`\\server\share\<PATH>`) are rejected.
  - Remote-backed volumes are rejected, including mapped network drives (drive letters that resolve to network storage).
  - Each root MUST be on a `DriveType.Fixed` volume; otherwise fail validation with a stable error code.
- Descendant checks are **inclusive**: the allowed root itself is permitted.
- For every user-content read/write, enforce the allowlist twice:
  1) On the canonicalized input path, and
  2) On the **final resolved path** (after resolving symlinks/junctions/reparse points) to ensure the target cannot escape the allowed root.
- `MET_SOURCE_DIR` structural invariant (MUST):
  - `MET_SOURCE_DIR` MUST be flat: it MUST NOT contain any subdirectories at scan time.
  - If any directory exists directly under `MET_SOURCE_DIR`, the planner MUST fail fast with a stable error code (no partial run).

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
  - Enumeration:
    - MUST NOT traverse reparse points.
  - Reads (user-content):
    - MUST block reads that encounter any reparse point along the path.
  - Writes (user-content):
    - MUST block writes that encounter any reparse point along the path.

### 3.4 Git guardrails

- Never run `git add *` and never stage by directory globs.
- Only stage explicit planned outputs (relative paths): `git add <file1> <file2> <file3>`.
- Only commit if the plan completes without cancellation and all conversions succeed.
- Never modify or delete any files not in the planned output set.

---

## 4. Solution architecture (modular monolith)

### 4.1 Visual Studio solution layout (proposed)

- `LIGHTNING.App` (WPF shell + MVVM views)
- `LIGHTNING.Core` (domain services, planners, runners, validation)
- `LIGHTNING.Adapters` (Git, filesystem, MetaF process runner, system probes)
- `LIGHTNING.Tests` (unit/integration tests)

### 4.2 Core components

- `ConfigService`: load/save `config.yaml` (YAML), validate required keys, warn on unknown keys.
- `BoundaryFileSystem`: allowlist enforcement + canonicalization + volume constraints + reparse policy enforcement.
- `MappingService`: load/save `mapping.yaml`, deterministic allocation, relink moved/renamed `.af`, retire/reactivate entries during Repair Mapping.
- `IngestPlanner`: produce an immutable plan (what will be converted, where, what will be staged, what will be archived).
- `PipelineRunner`: execute the plan, capture logs, enforce cancellation semantics, and guarantee no publish occurs on cancel/failure (archives/logs under `APP_WORK_DIR` may remain).
- `MetaFInstaller`: install/pin MetaF under `APP_WORK_DIR`, compute `METAF_BUNDLE_SHA`, and ensure runtime prerequisites (system-first, otherwise app-managed runtime).
- `MetaFRunner`: invoke MetaF for exactly one input file at a time with explicit output paths; capture stdout/stderr and exit code handling.
- `GitAdapter`: stage-only-planned outputs; commit with deterministic messages; optional per-repo author overrides; push per `GIT_PUSH_POLICY`.
- `SystemProbe`: discover `git` / `dotnet` / `code`, read versions, provide compatibility hints.
- `VSCodeLauncher`: open VS Code pointed at `AF_REPO_DIR` (best-effort extension install, never blocks conversions).

### 4.3 MVVM UI pattern

- Setup view: directory selection, MetaF status, validation feedback.
- Dashboard view: plan preview, run logs, commit summary.
- Errors are surfaced with stable codes + links to logs.

### 4.4 Error codes (stable)

- Format: `LIGHTNING_<CATEGORY>_<NNNN>`
  - `<CATEGORY>` is one of: `PLAN`, `FS`, `MAP`, `METAF`, `GIT`, `VSCODE`.
  - `<NNNN>` is a zero-padded integer (e.g., `0001`).
- Each surfaced error MUST include the code and a short human-readable message.

### 4.5 MetaF bootstrap rules

- Supported `METAF_INSTALL_MODE` values are fixed:
  - `repo_release_zip`
  - `dotnet_publish`
  - Unknown values are errors (stable error code).

- Install mode semantics (MUST be explicit and deterministic):
  - `repo_release_zip`:
    - `METAF_REF` MUST be a GitHub release tag (e.g., `v1.2.3`).
    - The installer MUST download the release asset for that tag, unpack it into `METAF_ROOT_DIR` under `APP_WORK_DIR`, and record:
      - `METAF_ROOT_DIR`
      - `METAF_EXE_PATH`
      - `METAF_BUNDLE_SHA`
    - Deterministic asset selection:
      - The release MUST contain exactly one `.zip` asset; otherwise fail fast with a stable error code.
  - `dotnet_publish`:
    - `METAF_REF` MUST be a full git commit SHA (40 hex chars).
    - The installer MUST clone/fetch the MetaF repository into an app-owned cache under `APP_WORK_DIR`, checkout the exact commit, run `dotnet publish` deterministically, and install the published output into `METAF_ROOT_DIR`.
    - The publish output used to compute `METAF_BUNDLE_SHA` MUST be the installed `METAF_ROOT_DIR` (not the build directory).

- Canonical bundle manifest and pinning (MUST):
  - Compute `METAF_BUNDLE_SHA` over a canonical manifest of `METAF_ROOT_DIR`:
    - Enumerate all files under `METAF_ROOT_DIR` (recursive), excluding directories.
    - For each file, compute:
      - `REL` = relative path using `/` separators (normalize `\` to `/`)
      - `SIZE` = size in bytes
      - `SHA` = `sha256:<hex>` of file bytes
    - Sort by `REL` (case-insensitive) ascending.
    - Serialize as UTF-8 lines: `REL\tSIZE\tSHA\n`
    - Compute `METAF_BUNDLE_SHA = sha256("v1\n" + <all lines>)` and store as `sha256:<hex>`.
  - The bundle hash is the pinned tool identity used by NFR-02 and stored into `mapping.yaml` per item.

- Trust model for external tools:
  - MetaF is treated as a trusted tool because it is pinned (`METAF_BUNDLE_SHA`) and installed into an app-owned cache.
  - The app does not attempt OS-level sandboxing of MetaF.
  - The app MUST validate and enforce boundaries for what it will publish/write (Sections 3, 10, 11).

- Retry policy:
  - If bootstrap fails, auto-retry exactly once after deleting the MetaF install directory under `APP_WORK_DIR` (clean-cache retry).

- Bootstrap logging and retention (MUST):
  - Each bootstrap attempt MUST produce a log file under: `<APP_WORK_DIR>\logs\bootstrap\`
  - Naming MUST be: `<UTC_TIMESTAMP>_<RESULT>.log` where:
    - `UTC_TIMESTAMP` is `YYYYMMDDTHHMMSSfffZ`
    - `RESULT` is `OK` or `FAIL`
  - Retention MUST keep the most recent 50 attempt logs and delete older attempt logs (deterministic order by filename).

- Runtime prerequisites (MUST):
  - The app itself is self-contained.
  - MetaF execution MUST be invoked with explicit input and explicit output paths (no “auto-suffix” behavior).
  - If MetaF requires a `.NET` runtime:
    - Prefer system-installed compatible `.NET` runtime.
    - If missing, provision an app-managed `.NET` runtime under `APP_WORK_DIR\deps\dotnet\` and run MetaF using it.
    - The app MUST NOT mutate global machine state (no PATH mutation, no registry edits, no admin prompts).
  - Compatibility check before first MetaF run MUST include:
    - `dotnet --list-runtimes` (using the dotnet that will be used to run MetaF), and
    - A probe MetaF invocation (e.g., `--version` or equivalent) to confirm `METAF_EXE_PATH` is runnable.

- Optional hardening (implementation option):
  - Run MetaF in a Windows Job Object so cancellation can reliably kill the process tree and enforce per-run timeouts and resource limits.

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

Validation rules (MUST):

- `MET_SOURCE_DIR`, `AF_REPO_DIR`, `APP_WORK_DIR`:
  - MUST be absolute local drive paths.
  - MUST be on `DriveType.Fixed` volumes.
  - MUST NOT be UNC paths.
  - MUST NOT overlap: none of these roots may be a descendant of another.
- `MET_SOURCE_DIR`:
  - MUST exist.
  - MUST be flat (no subdirectories).
- `AF_REPO_DIR`:
  - MUST exist and contain a valid Git repository (`.git` present).
- `APP_WORK_DIR`:
  - MUST exist or be creatable; all app-owned caches/logs/temp/archives live under this root.

MetaF bootstrap / pinning keys:

- `METAF_GIT_URL` (string; repository clone URL)
- `METAF_INSTALL_MODE` (required once setup is complete; supported: `repo_release_zip`, `dotnet_publish`)
- `METAF_REF` (required once setup is complete):
  - For `repo_release_zip`: GitHub release tag (e.g., `v1.2.3`)
  - For `dotnet_publish`: full git commit SHA (40 hex chars)
- `METAF_ROOT_DIR` (resolved directory under `APP_WORK_DIR` containing the installed MetaF bundle)
- `METAF_EXE_PATH` (resolved path inside `METAF_ROOT_DIR`)
- `METAF_BUNDLE_SHA` (`sha256:<hex>`; computed from a canonical manifest of all files under `METAF_ROOT_DIR`)

VS Code convenience (explicitly gated):

- `MANAGE_VSCODE_WORKSPACE_FILES` (`true/false`; default `false`)
- `VSCODE_REQUIRED_EXTENSIONS` (list of extension IDs; default empty)
- `VSCODE_PREFERRED_PROFILE` (optional string)

Git behavior:

- `REQUIRE_CLEAN_REPO` (`true/false`; default `true`)
- `GIT_PUSH_POLICY` (default `always`; supported: `always`, `never`)
- `CONFLICT_POLICY` (default `fail`; supported: `fail`, `suffix`, `fail_all`)
  - Applies only to **publish-time destination collisions** for planned outputs in `AF_REPO_DIR` and `MET_SOURCE_DIR` (Section 11.1).

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
- `MET_REL` is the last-seen leaf filename under `MET_SOURCE_DIR` (MUST be a filename only; no directory separators). It is used for `.af → .met` destination naming.
- `AF_REL` is the relative path under `AF_REPO_DIR` where the `.af` file is stored (nested directories allowed).

#### Required top-level keys (v1)

- `VERSION`
- `CREATED_UTC`
- `MET_SOURCE_ROOT_FINGERPRINT`
- `ITEMS` (array)

#### Required item keys (v1)

- `STATE` (`active` or `retired`)
- `MET_ID` (`sha256:<hex>`)
- `MET_REL` (e.g., `Foo.met`; leaf filename only)
- `AF_REL` (e.g., `Foo.af` or `subdir\Foo.af`; relative, not rooted)
- `AF_SHA256` (`sha256:<hex>`; used for robust relink)
- `LAST_METAF_BUNDLE_SHA` (`sha256:<hex>`)
- `LAST_RUN_UTC` (`YYYY-MM-DDTHH:MM:SSZ`)

#### Optional item keys (v1)

- `AF_SIZE_BYTES` (integer; used to prefilter relink candidates by size before hashing)
- `RETIRED_UTC` (`YYYY-MM-DDTHH:MM:SSZ`; MUST be present when `STATE=retired`)

#### Item validation rules (MUST)

- `STATE` MUST be `active` or `retired`.
- `MET_REL`:
  - MUST NOT contain `\` or `/`.
  - MUST end with `.met` (case-insensitive).
- `AF_REL`:
  - MUST be a relative path (no drive letter, no leading `\` or `/`).
  - MUST NOT contain `..` segments.
  - MUST end with `.af` (case-insensitive).
- When `STATE=retired`, `RETIRED_UTC` MUST be present.

#### Uniqueness invariants (MUST)

On load and before write, enforce:

- `MET_ID` MUST be unique across `ITEMS[]` (all states).
- For items where `STATE=active`:
  - `MET_REL` MUST be unique (case-insensitive comparison for Windows).
  - `AF_REL` MUST be unique (case-insensitive comparison for Windows).

If any invariant is violated: treat as fatal load error (no auto-repair; conversions blocked).

---

### 5.3 Fingerprinting `MET_SOURCE_DIR` and Repair Mapping (safety + drift recovery)

Purpose: detect drift between the `MET_SOURCE_DIR` currently configured and the mapping’s stored origin.

- Store: `MET_SOURCE_ROOT_FINGERPRINT = sha256("v1|" + canonical_full_path + "|" + volume_serial_if_available)`
- On mismatch:
  - Conversion runs MUST NOT proceed (planner fails fast with a stable error code).
  - UI MUST offer a separate, explicit maintenance action: **Repair Mapping** (no conversions during repair).

**Repair Mapping** (explicit maintenance action; deterministic):

- Recompute and update `MET_SOURCE_ROOT_FINGERPRINT` for the currently configured `MET_SOURCE_DIR`.
- Rescan `MET_SOURCE_DIR` (**flat; non-recursive**) and compute `MET_ID` for each discovered `.met`.
- For each mapping item:
  - If its `MET_ID` is found in the scan:
    - Update `MET_REL` to the discovered filename.
    - Set `STATE=active` (unretire if necessary) and clear `RETIRED_UTC` if present.
  - If its `MET_ID` is not found in the scan:
    - Set `STATE=retired`.
    - If transitioning from `active → retired`, set `RETIRED_UTC` to the current UTC timestamp.
- Enforce mapping invariants (Section 5.2) before write.
- Write `mapping.yaml` transactionally (temp then atomic replace).
- If the repair is canceled, no writes occur.

---

## 6. Deterministic naming & sanitization

### 6.1 `.af` initial placement (new `.met`)

When a `.met` has no mapping entry matching `MET_ID`:

- Default `AF_REL` is repo root: `<sanitized_met_basename>.af`
- If the default `AF_REL` is not usable (case-insensitive):
  - It is already used by an `ITEMS[]` entry where `STATE=active`, or
  - A file already exists at `<AF_REPO_DIR>\<AF_REL>`,
- Then resolve deterministically by suffixing the leaf filename with `~<n>` (MetaF-style), choosing the smallest `n >= 0` that is free:
  - `Name.af`, then `Name~0.af`, `Name~1.af`, `Name~2.af`, etc.
- The resolved `AF_REL` MUST be included in the plan preview and persisted to `mapping.yaml` only after a successful run.

### 6.2 Windows-safe sanitization algorithm (deterministic)

Used for:

- `.af` placement names (derived from `.met` basenames)
- Archive entry names (`name` field)

Algorithm:

1. Normalize Unicode to NFC.
2. Trim whitespace.
3. Replace invalid Windows filename characters: `< > : " / \ | ? *` with `_`.
4. Replace control characters with `_`.
5. Disallow reserved device names (case-insensitive): `CON`, `PRN`, `AUX`, `NUL`, `COM1`..`COM9`, `LPT1`..`LPT9`:
   - If reserved, prefix with `_`.
6. Collapse runs of `_` to a single `_`.
7. If empty after sanitization, use `_`.
8. Preserve extension for `.met` and `.af` paths as required by schema.

---

## 7. Mapping relink semantics (moves + renames)

Goal: preserve mapping after user reorganizes `.af` files inside the repo.

### 7.1 Relink trigger

On run start:

- If any mapped `AF_REL` path does not exist at `<AF_REPO_DIR>\<AF_REL>`, treat as “missing”.
- If any `.af` file exists in the repo that is not referenced by any active mapping entry, treat as “unmapped candidate”.

### 7.2 Relink strategy

For each missing mapping entry:

- Try to find a match among unmapped candidates:
  1) If `AF_SIZE_BYTES` matches (if recorded), then
  2) Compare `sha256(file_bytes)` to `AF_SHA256`.
- If exactly one candidate matches, update mapping `AF_REL` to the candidate’s relative path.
- If multiple candidates match, prompt user: Locate/Skip.
- If none match, prompt user: Locate/Skip.

Locate behavior:

- User may pick a file only within `AF_REPO_DIR` (enforced by boundary rules).
- The selected file MUST be `.af`.
- After Locate, update `AF_REL` and update `AF_SHA256`.

Skip behavior:

- Skip conversion for that mapping entry for this run.
- No mapping changes for that entry.

---

## 8. `.met` rename and replacement semantics

### 8.1 `.met` rename (same content, different filename)

If a `.met` file’s `MET_ID` matches an existing mapping entry but `MET_REL` differs:

- Stage an update of the mapping entry’s `MET_REL` to the new filename (no new row).
- Persist `MET_REL` changes only after a successful run (Section 11).

### 8.2 `.met` replacement (same filename, different content)

If a `.met` file has a `MET_REL` equal to an existing **active** entry’s `MET_REL` but a different `MET_ID`:

- UI MUST prompt the user per item:
  - **Update existing entry:** treat this as a replacement-in-place:
    - Update the existing mapping entry’s `MET_ID` to the new content hash.
    - Keep `MET_REL` (must remain unique among `STATE=active`).
    - Keep `AF_REL`; the generated `.af` will overwrite that path using the normal temp-write contract (Section 11.1).
  - **Skip:** skip this `.met` for this run (no mapping changes; no outputs; logged).
- To ingest the new `.met` as a distinct new item, the user MUST rename it to a different filename (`MET_REL`) that is not in use by any `STATE=active` mapping entry, then rerun.

### 8.3 `.af → .met` output naming and overwrite semantics

- Only `STATE=active` mapping entries are eligible for `.af → .met` conversion.
- Destination path:
  - `MET_SOURCE_DIR` is flat; outputs are written to: `<MET_SOURCE_DIR>\<MET_REL>`.
  - `MET_REL` MUST be a filename only (no directory separators).
- Overwrite policy:
  - If destination exists and it is the planned destination for this active mapping entry, archive it first (**before_overwrite**, Section 9) and then overwrite atomically (Section 11.1).
- If a required mapping field for `.af → .met` is missing or invalid (`MET_REL`, `AF_REL`, `MET_ID`, etc.):
  - Treat as a fatal mapping load/validation error (Section 5.2); conversions MUST NOT proceed until the mapping is repaired.

---

## 9. Archive policy

Archive root: `APP_WORK_DIR\.archive\met\<YYYY>\<MM>\<DD>\`

Archives are app-owned artifacts. They are never committed to Git.

### 9.1 On-disk naming

Use: `<utc_timestamp>_<kind>_<name>_<sha256prefix>.met`

Where:

- `utc_timestamp` is UTC with millisecond precision, format: `YYYYMMDDTHHMMSSfffZ`.
- Timestamp source: current system time converted to UTC at the moment the archive entry is finalized.
- Collision handling within the same day folder:
  - If the filename already exists, append a counter to the timestamp:
    - `YYYYMMDDTHHMMSSfffZ-1`, `YYYYMMDDTHHMMSSfffZ-2`, etc.
- `kind` ∈ `{before_convert, before_overwrite}`.
- `name` is sanitized for Windows filenames (Section 6.2).
- `sha256prefix` is the first 12 hex chars of the `.met` content hash.

### 9.2 Archive triggers

- `.met → .af`:
  - Archive the input `.met` immediately before invoking MetaF for that file (**before_convert**), and only after the user has approved the plan.
- `.af → .met` (round-trip overwrite):
  - If the destination `.met` exists, archive the destination immediately before overwrite (**before_overwrite**).

### 9.3 Retention and cap enforcement

- Always retain entries whose filename-derived UTC timestamps fall within the most recent `ARCHIVE_RETENTION_DAYS` window (default `30` days).
- Archive size accounting considers only files under `APP_WORK_DIR\.archive\met\` (recursive).
- Cap enforcement:
  - Never delete entries within the retention window.
  - If total archive size exceeds `MAX_ARCHIVE_GB` and there exist entries older than the retention window:
    - Delete oldest entries older than the retention window until under cap.
  - If the archive exceeds `MAX_ARCHIVE_GB` due solely to entries within the retention window:
    - Do not delete; warn prominently in UI and logs.

---

## 10. Git behavior

### 10.1 Staging (explicit + deterministic)

- Stage exactly the planned outputs:
  - `mapping.yaml`
  - all produced `.af` files (relative paths)
- Never stage `.met` archives.
- Never stage anything under `APP_WORK_DIR`.

### 10.2 Commit message (deterministic)

Format:

`LIGHTNING: <direction> <count> files`

Where:

- `<direction>` is `met→af` or `af→met`
- `<count>` is number of `.af` files staged (for `met→af`) or 0 (for `af→met`, unless `.af` changes are also planned).

### 10.3 Author identity

- Author identity defaults to the user’s global Git config.
- If `REPO_PROFILES[]` contains a matching `AF_REPO_DIR`, override author name/email for commits in that repo.

### 10.4 Dirty repo behavior

Definition of **clean repo** (must hold simultaneously):

- No modified tracked files.
- No staged changes.
- No unmerged paths (e.g., merge conflicts).
- No untracked files that are not ignored.

If `REQUIRE_CLEAN_REPO: true` and repo is not clean:

- Abort the run (no staging, no commits).

If `REQUIRE_CLEAN_REPO: false` and repo is not clean:

- Proceed, but stage only planned outputs.
- Warn prominently in UI and logs that the working tree was not clean.

### 10.5 Push to remote (policy-driven)

Supported `GIT_PUSH_POLICY` values:

- `always`
- `never`

Rules:

- Push is attempted **only** if the run produced a commit (Section 10.2) and the commit was successfully created.
- Push is attempted only after all conversions succeed and publishing completes (Section 11).
- If `GIT_PUSH_POLICY=never`, do not push (but still show what would have been pushed).

---

## 11. Cancellation and side effects contract (MUST)

Definition: **publish** means any write into user-controlled working sets (`AF_REPO_DIR`, `MET_SOURCE_DIR`) and any Git side effects (staging/commit/push), including writes under `.vscode\` when workspace management is enabled.

Canceling during plan generation, relink scanning, or execution MUST NOT publish and MUST NOT modify user-controlled working sets:

- `AF_REPO_DIR`: no `.af` writes, no `mapping.yaml` writes, and no `.vscode\` subtree writes.
- Git index: no staging and no commits.
- `MET_SOURCE_DIR`: no `.met` writes or overwrites.

Notes:

- Archives and logs under `APP_WORK_DIR` do **not** count as user-controlled working set changes. They do not need to be removed or rolled back on cancel.
- Any run-scoped staging/temp directories under `APP_WORK_DIR` MUST be deleted as part of cancel cleanup (best-effort).

### 11.1 Staging, conflict handling, and atomic publish (MUST)

Staging:

- During execution, all would-be writes to `AF_REPO_DIR` and `MET_SOURCE_DIR` MUST be routed to a run-scoped staging directory:
  - `STAGING_DIR = <APP_WORK_DIR>\.staging\<RUN_ID>\`
- File writes inside staging MUST follow the temp-then-rename contract:
  - Write `*.tmp`, flush/close, then atomic rename to final staging path.

Staging layout:

- `.met → .af` staging:
  - Stage `.af` outputs under: `<STAGING_DIR>\af\<AF_REL>`
  - Stage the next `mapping.yaml` under: `<STAGING_DIR>\mapping.yaml`
- `.af → .met` staging:
  - Stage `.met` outputs under: `<STAGING_DIR>\met\<MET_REL>`

Publish preflight (MUST, all-or-nothing):

- Before the first publish write, re-validate:
  - The plan snapshot integrity (Section 11.3).
  - Repo cleanliness requirements (Section 10.4).
  - Destination collision handling per `CONFLICT_POLICY` (below).
- If any preflight check fails: publish MUST NOT begin.

Destination collision handling (`CONFLICT_POLICY`) at publish time:

- A **destination collision** occurs when a planned destination path already exists at publish time.
- Ownership classification is based on the mapping snapshot captured at plan time:
  - **existing-mapped**: the destination path belongs to an item that already existed in the mapping snapshot.
  - **new-mapped**: the destination path belongs to an item that is first introduced in this run (new mapping entry).

Rules:

- `CONFLICT_POLICY=fail` (default):
  - If any **new-mapped** destination collides, abort publish before any writes.
  - **existing-mapped** destinations may overwrite.
- `CONFLICT_POLICY=suffix`:
  - If any **new-mapped** destination collides, resolve by suffixing the leaf filename with `~<n>` (smallest free `n >= 0`, case-insensitive existence check), and:
    - Publish to the suffixed destination, and
    - Update the staged `mapping.yaml` to the suffixed `AF_REL` before publishing it.
  - **existing-mapped** destinations may overwrite.
- `CONFLICT_POLICY=fail_all`:
  - If any destination collides (existing-mapped or new-mapped), abort publish before any writes.

Publishing (only after success; all-or-nothing best-effort):

- Cancellation MUST be honored up to the start of the publish step.
- Once publish begins, cancellation is deferred until publish completes (to avoid partial writes).

Publish `.af` files (cross-volume safe):

- For each staged `<STAGING_DIR>\af\<AF_REL>`:
  - Ensure the destination parent directory exists under `AF_REPO_DIR` (create directories as needed).
  - Write to a temp file in the target directory: `<AF_REPO_DIR>\<AF_REL>.tmp`
  - Flush/close, then atomic rename/replace to `<AF_REPO_DIR>\<AF_REL>`

Publish `mapping.yaml`:

- Transactional replace of `<AF_REPO_DIR>\mapping.yaml` (temp then atomic replace).

Publish `.met` outputs (cross-volume safe):

- For each destination `<MET_SOURCE_DIR>\<MET_REL>`:
  - If destination exists, archive it first (**before_overwrite**, Section 9).
  - Write to a temp file in the target directory: `<MET_SOURCE_DIR>\<MET_REL>.tmp`
  - Flush/close, then atomic rename/replace to `<MET_SOURCE_DIR>\<MET_REL>`

On cancel or any failure before publish:

- Do not publish anything from staging.
- Delete `STAGING_DIR` during cleanup (best-effort).
- Any archives/logs already written under `APP_WORK_DIR` may remain.

### 11.2 Run exclusivity (concurrency lock)

- The app MUST prevent concurrent runs that share the same tuple: (`AF_REPO_DIR`, `MET_SOURCE_DIR`).
- Acquire a single-instance lock at run start (e.g., named mutex + lock file under `APP_WORK_DIR\locks\`).
  - The lock key MUST be derived from canonical full paths (e.g., `sha256(AF_REPO_DIR + "|" + MET_SOURCE_DIR)`), to avoid false negatives due to path formatting.
- If the lock cannot be acquired, fail fast with a stable error code (no partial work).

### 11.3 Plan snapshot integrity

- The plan MUST capture:
  - A content hash of `mapping.yaml` at plan time (or an explicit sentinel meaning “no mapping existed” on first run).
  - The resolved `METAF_BUNDLE_SHA` used for the plan.
- At execution start:
  - Re-hash `mapping.yaml`; if it differs, abort execution and require the user to re-plan (stable error code).
  - If `METAF_BUNDLE_SHA` differs, abort execution and require the user to re-plan (stable error code).

---

## 12. Definition of Done (MVP)

- User configures directories and MetaF source/ref.
- App installs/builds MetaF into its own cache, pins `METAF_BUNDLE_SHA`, records `METAF_ROOT_DIR` and `METAF_EXE_PATH`.
- `.met → .af` converts and commits only planned `.af` + `mapping.yaml`.
- `.af → .met` converts back into `MET_SOURCE_DIR` safely with archiving.
- Mapping survives `.af` moves/renames via relink + Locate/Skip (Locate restricted to `AF_REPO_DIR`).
- Archive retention respects `ARCHIVE_RETENTION_DAYS` minimum + `MAX_ARCHIVE_GB` cap for older entries.
- User-content I/O never escapes allowlisted roots; system probes are read-only outside allowlist.
- VS Code button opens the repo folder; required extensions are recommended and best-effort installed (if install fails, fall back to recommendation-only); no `*.code-workspace` files are created. Optional `.vscode/extensions.json` management is gated by `MANAGE_VSCODE_WORKSPACE_FILES` and is performed only on explicit user action.
