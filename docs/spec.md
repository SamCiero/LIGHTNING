# LIGHTNING — Specification

Status: ACCEPTED  
Version: v1.0.0  
Last Updated (UTC): 2025-12-20  
Owner: Sam Ciero  
Repo: <https://github.com/SamCiero/LIGHTNING>  
Doc ID: LIGHTNING-SPEC-01

---

## 0. Change Log

| Version | Date (UTC) | Author | Summary |
| --- | ---: | --- | --- |
| v1.0.0 | 2025-12-20 | Sam Ciero | Initial accepted specification. |

---

## 1. Normative Language

This specification uses RFC-style keywords:

- **MUST / MUST NOT**: required for compliance.
- **SHOULD / SHOULD NOT**: recommended; deviations require justification.
- **MAY**: optional.

Every normative requirement **MUST** have a unique requirement ID `REQ-####` and be traceable to at least one acceptance test `AT-####`.

---

## 2. Scope

### 2.1 Goals

- Provide safe, deterministic conversions between `.met` and `.af` via MetaF with a plan/preview step.
- Keep all user content local and avoid any cloud dependency for normal operation after bootstrap.
- Enforce a strict filesystem trust boundary so user-content I/O cannot escape configured allowlisted roots.
- Record and commit `.af` outputs plus `mapping.yaml` to a user-selected Git repository, while keeping `.met` archives local-only.
- Offer a VS Code “Open Workspace” convenience action that is explicitly gated and best-effort.

### 2.2 Non-Goals

- Cross-platform support (Windows-only in v1.0.0).
- Automatic or background conversions; runs are user-triggered.
- In-app editing of `.af` content.

### 2.3 Constraints

- Platform: Windows 10+ desktop application (WPF / .NET).
- Offline/Online: ONLINE_OPTIONAL (bootstrap can require network; conversions can run without network once MetaF is installed and pinned).
- Data locality: LOCAL_ONLY.
- Privacy: NO_TELEMETRY.
- Licensing: Not specified by this document.

### 2.4 Assumptions

- The user has permission to read/write within configured roots and within `%LocalAppData%`.
- `git` is available on the machine for repos that will receive commits.
- MetaF can be executed locally from an app-owned cache folder.
- VS Code is optional; if absent, Open Workspace is disabled or produces a non-fatal error.

---

## 3. Definitions

### 3.1 Glossary (Single Source of Truth)

| Term | Definition |
| --- | --- |
| `.met` | MetaF source file format handled by LIGHTNING. |
| `.af` | MetaF alternate format stored in a Git repo and treated as the primary editable artifact. |
| `MetaF` | External conversion tool invoked by LIGHTNING. It is installed into an app-owned cache and pinned by `METAF_BUNDLE_SHA`. |
| `Plan` | A deterministic, immutable description of a run: inputs snapshot, intended outputs, and intended side effects. |
| `Preview` | A user-visible rendering of the Plan including all prospective filesystem writes and Git actions. |
| `Publish` | Any write into user-controlled working sets (`AF_REPO_DIR`, `MET_SOURCE_DIR`) or any Git side effect (stage/commit/push), and any `.vscode` writes when workspace management is enabled. |
| `Artifact` | A file produced or updated by a run (for example, `.af`, `mapping.yaml`, `.met`, logs, archives). |
| `APP_CONFIG_DIR` | App-owned configuration root at `%LocalAppData%\EmpyreanCodex\LIGHTNING\`. |
| `MET_SOURCE_DIR` | User-selected flat directory containing `.met` files and also receiving `.met` outputs. |
| `AF_REPO_DIR` | User-selected Git repository root containing `.af` files plus `mapping.yaml`. |
| `APP_WORK_DIR` | App-owned working root for caches, staging, logs, archives, and managed dependencies. |
| `STAGING_DIR` | Run-scoped staging folder under `APP_WORK_DIR\.staging\RUN_ID\` used before Publish. |
| `RUN_ID` | A unique identifier for a single run, used in staging and logs. |
| `mapping.yaml` | Strict repo-tracked mapping between `.met` identities and `.af` paths. |
| `MET_ID` | Canonical identity of a `.met` file: `sha256:{hex}` over file bytes. |
| `AF_SHA256` | Canonical identity of a `.af` file: `sha256:{hex}` over file bytes. |
| `METAF_BUNDLE_SHA` | Identity of the installed MetaF bundle: `sha256:{hex}` computed over a canonical manifest of `METAF_ROOT_DIR`. |
| `existing-mapped` | A Publish-time destination that belongs to an item present in the Plan’s mapping snapshot. |
| `new-mapped` | A Publish-time destination introduced by the current run (new mapping entry). |

### 3.2 Canonical Identifiers

- Requirement IDs: `REQ-####` (example: `REQ-0402`).
- Acceptance test IDs: `AT-####` (example: `AT-0015`).
- Error codes: `LIGHTNING_{CATEGORY}_{NNNN}` where `CATEGORY ∈ {PLAN, FS, MAP, METAF, GIT, VSCODE}`.

---

## 4. Project Invariants

These are global truths. No other section redefines them.

- REQ-0001 (**MUST**) LIGHTNING shall be a Windows 10+ local desktop application; it shall not require any cloud service for normal operation after MetaF is installed and pinned.
- REQ-0002 (**MUST**) Source code namespaces and assembly naming shall use `LIGHTNING.*` (or `Lightning.*`) and shall not use an `EC.*` prefix.
- REQ-0003 (**MUST**) All user-content filesystem operations shall enforce the trust boundary rules defined in Section 10.2, including canonicalization and final-path enforcement.
- REQ-0004 (**MUST**) A run shall not Publish until after a Preview is shown and the user explicitly approves execution.
- REQ-0005 (**MUST**) Conversion runs shall be user-triggered; background or automatic conversions are out of scope for v1.0.0.

---

## 5. System Overview

### 5.1 High-Level Description

LIGHTNING is a Windows desktop app that bootstraps and pins MetaF inside an app-owned cache, then uses MetaF to convert between `.met` and `.af` under a strict filesystem trust boundary. Each run produces a deterministic Plan and Preview; user-controlled writes and Git side effects occur only during Publish after explicit approval. The `.af` and `mapping.yaml` outputs are committed to a user-selected Git repository; `.met` snapshots are archived under an app-owned work directory and are not committed.

### 5.2 System Context (Boundary + Actors)

```txt
+-------------------+       +-------------------+
|       User        |       |   External Tools  |
| (approve runs,    |       | MetaF / git /     |
|  choose dirs)     |       | VS Code (optional)|
+---------+---------+       +---------+---------+
          |                           |
          v                           v
+---------------------------------------------+
|                  LIGHTNING                  |
|  Planner / Preview / Runner / UI (WPF)      |
|  Boundary FS / Mapping / Git Adapter        |
|  MetaF Installer & Runner                   |
+---------------------------------------------+
          |
          v
+---------------------------------------------+
| Data Stores (Filesystem)                    |
| APP_CONFIG_DIR / APP_WORK_DIR / AF_REPO_DIR |
| MET_SOURCE_DIR                               |
+---------------------------------------------+
```

### 5.3 Components

| Component | Responsibility | Inputs | Outputs |
| --- | --- | --- | --- |
| `ConfigService` | Load/save `config.yaml`, validate required keys, warn on unknown keys. | `config.yaml` | Validated config + warnings |
| `BoundaryFileSystem` | Enforce allowlisted roots, volume constraints, canonicalization, final-path checks, reparse policies. | Paths + policy | Approved or rejected file ops |
| `MappingService` | Load/save `mapping.yaml`, enforce invariants, allocate deterministic paths, apply relink/repair updates. | `mapping.yaml`, repo scan | Updated mapping snapshot |
| `IngestPlanner` | Build deterministic Plan describing actions, outputs, and side effects. | FS scans, mapping snapshot, config | Plan object |
| `PipelineRunner` | Execute Plan with staging, cancellation semantics, and publish contract. | Plan, cancellation token | Staged artifacts, run log |
| `MetaFInstaller` | Bootstrap MetaF, pin by `METAF_BUNDLE_SHA`, ensure prerequisites. | Git URL/tag/SHA | Installed bundle + pin |
| `MetaFRunner` | Invoke MetaF per file with explicit input/output paths; capture stdout/stderr and exit codes. | Input path, output path | Produced output + logs |
| `GitAdapter` | Stage and commit only planned outputs; optional push by policy. | Repo path, planned files | Git side effects |
| `SystemProbe` | Discover `git`, `dotnet`, `code`; report versions and readiness. | System | Capability report |
| `VSCodeLauncher` | Launch VS Code pointed at `AF_REPO_DIR`. | Repo path, settings | VS Code process spawn |

---

## 6. Interfaces

### 6.1 User-Facing Interface

- UI type: GUI (WPF).
- Primary actions:
  - Configure directory roots and MetaF install settings.
  - Create Plan and Preview for `.met → .af` and `.af → .met` runs.
  - Approve or cancel execution.
  - Run Repair Mapping when `MET_SOURCE_DIR` drift is detected.
  - Open Workspace (VS Code) for `AF_REPO_DIR`.

- REQ-0101 (**MUST**) The UI shall require a Preview and explicit user approval before any Publish step begins.
- REQ-0102 (**MUST**) Open Workspace shall be best-effort and shall not block conversions if VS Code is missing or launch fails.
- REQ-0103 (**MUST**) Workspace file management shall be gated by `MANAGE_VSCODE_WORKSPACE_FILES` and shall never be required for conversions.
- REQ-0104 (**MUST**) Network access shall not be required to run conversions after MetaF is installed and pinned.

### 6.2 CLI (If Applicable)

No public CLI surface is defined for v1.0.0. Any internal diagnostic commands are out of scope for this spec.

### 6.3 External APIs / Integrations

| Integration | Purpose | Auth | Failure Mode |
| --- | --- | --- | --- |
| MetaF | `.met ↔ .af` conversion | None | Stable `LIGHTNING_METAF_*` error; run aborts without Publish |
| Git | Stage/commit/push `.af` + `mapping.yaml` | Local git config | Stable `LIGHTNING_GIT_*` error; conversion output not committed |
| VS Code | Open repo workspace | None | Non-fatal error; conversions unaffected |
| GitHub (bootstrap only) | Fetch MetaF release asset or repo | None | Bootstrap fails; conversions unavailable until resolved |

- REQ-0105 (**MUST**) Integrations not required for conversion correctness (VS Code, GitHub) shall be optional and non-blocking for conversion runs when absent.

---

## 7. Data Model

### 7.1 Configuration

- File: `config.yaml` located at `%LocalAppData%\EmpyreanCodex\LIGHTNING\config.yaml`
- Format: YAML
- Schema versioning: `VERSION` string; bump on breaking schema changes.

Parsing policy: lenient — unknown keys are ignored and logged as warnings.

Required keys (minimum):

- `VERSION`
- `MET_SOURCE_DIR`
- `AF_REPO_DIR`
- `APP_WORK_DIR`

Key validation includes:

- Absolute local drive paths (no UNC).
- Fixed volumes for `MET_SOURCE_DIR`, `AF_REPO_DIR`, and `APP_WORK_DIR`.
- Non-overlapping roots.
- `MET_SOURCE_DIR` exists and is flat (no subdirectories).
- `AF_REPO_DIR` exists and contains a `.git` directory.
- `APP_WORK_DIR` exists or can be created.

MetaF bootstrap keys:

- `METAF_GIT_URL`
- `METAF_INSTALL_MODE` (`repo_release_zip` | `dotnet_publish`)
- `METAF_REF` (tag or 40-hex commit SHA per install mode)
- `METAF_ROOT_DIR` (resolved under `APP_WORK_DIR`)
- `METAF_EXE_PATH` (resolved under `METAF_ROOT_DIR`)
- `METAF_BUNDLE_SHA` (`sha256:{hex}`)

VS Code convenience keys:

- `MANAGE_VSCODE_WORKSPACE_FILES` (default `false`)
- `VSCODE_REQUIRED_EXTENSIONS` (default empty)
- `VSCODE_PREFERRED_PROFILE` (optional)

Git behavior keys:

- `REQUIRE_CLEAN_REPO` (default `true`)
- `GIT_PUSH_POLICY` (`always` | `never`; default `always`)
- `CONFLICT_POLICY` (`fail` | `suffix` | `fail_all`; default `fail`)
- `REPO_PROFILES[]` mapping `AF_REPO_DIR` to author identity overrides

Archive retention keys:

- `ARCHIVE_RETENTION_DAYS` (default `30`)
- `MAX_ARCHIVE_GB`

Filesystem safety:

- `REPARSE_POINT_POLICY` (`block_writes_allow_reads` default | `block_all`)

- REQ-0201 (**MUST**) `config.yaml` shall be stored under `APP_CONFIG_DIR` and parsed leniently with unknown keys ignored and logged as warnings.
- REQ-0202 (**MUST**) Config validation shall reject UNC paths, non-fixed volumes, overlapping roots, non-flat `MET_SOURCE_DIR`, non-repo `AF_REPO_DIR`, and non-creatable `APP_WORK_DIR` with stable error codes.

#### Schema (Pseudo)

```text
config.yaml:
  VERSION: string
  MET_SOURCE_DIR: string
  AF_REPO_DIR: string
  APP_WORK_DIR: string
  METAF_GIT_URL: string?
  METAF_INSTALL_MODE: enum(repo_release_zip, dotnet_publish)?
  METAF_REF: string?
  METAF_ROOT_DIR: string?
  METAF_EXE_PATH: string?
  METAF_BUNDLE_SHA: string?
  MANAGE_VSCODE_WORKSPACE_FILES: bool (default false)
  VSCODE_REQUIRED_EXTENSIONS: string[] (default [])
  VSCODE_PREFERRED_PROFILE: string?
  REQUIRE_CLEAN_REPO: bool (default true)
  GIT_PUSH_POLICY: enum(always, never) (default always)
  CONFLICT_POLICY: enum(fail, suffix, fail_all) (default fail)
  REPO_PROFILES:
    - AF_REPO_DIR: string
      GIT_AUTHOR_NAME: string
      GIT_AUTHOR_EMAIL: string
  ARCHIVE_RETENTION_DAYS: int (default 30)
  MAX_ARCHIVE_GB: number
  REPARSE_POINT_POLICY: enum(block_writes_allow_reads, block_all) (default block_writes_allow_reads)
```

### 7.2 Persistent Storage

- Store type: filesystem.
- Location rule:
  - App-owned: `APP_CONFIG_DIR`, `APP_WORK_DIR`.
  - User-controlled working sets: `AF_REPO_DIR`, `MET_SOURCE_DIR`.
- Migration strategy:
  - `config.yaml` and `mapping.yaml` are versioned; schema changes are handled by explicit version bumps and upgrade routines.

- REQ-0203 (**MUST**) Versioned data files shall be upgraded in a forward-only, idempotent manner when version changes are supported.

### 7.3 Identifiers and Naming

#### 7.3.1 mapping.yaml (repo, committed; strict)

- File: `AF_REPO_DIR\mapping.yaml`
- Parsing policy: strict — unknown keys are fatal errors.

Stable identity:

- `MET_ID = sha256(met_file_bytes)` stored as `sha256:{hex}`.
- `MET_REL` is a leaf filename only (no directory separators) and is used as the `.af → .met` destination name.
- `AF_REL` is a relative path under `AF_REPO_DIR` (nested directories allowed).

Top-level keys (v1):

- `VERSION`, `CREATED_UTC`, `MET_SOURCE_ROOT_FINGERPRINT`, `ITEMS`

Item keys (v1):

- Required: `STATE`, `MET_ID`, `MET_REL`, `AF_REL`, `AF_SHA256`, `LAST_METAF_BUNDLE_SHA`, `LAST_RUN_UTC`
- Optional: `AF_SIZE_BYTES`, `RETIRED_UTC` (required when `STATE=retired`)

Validation rules:

- `STATE` in `{active, retired}`.
- `MET_REL` contains no `\` or `/` and ends with `.met`.
- `AF_REL` is relative (no drive letter, no leading slash), contains no `..` segment, and ends with `.af`.

Uniqueness invariants:

- `MET_ID` unique across all `ITEMS[]`.
- For `STATE=active`: `MET_REL` and `AF_REL` unique under case-insensitive comparison.

- REQ-0204 (**MUST**) `mapping.yaml` shall be parsed strictly; unknown keys or invariant violations shall block conversions.
- REQ-0205 (**MUST**) `MET_SOURCE_ROOT_FINGERPRINT` mismatch shall block conversion runs and enable the Repair Mapping workflow in Section 8.4.
- REQ-0206 (**MUST**) `MET_REL` and `AF_REL` validation rules and case-insensitive uniqueness invariants shall be enforced on load and before write.

#### 7.3.2 `MET_SOURCE_ROOT_FINGERPRINT`

- Value: `sha256("v1|" + canonical_full_path + "|" + volume_serial_if_available)` as `sha256:{hex}`.
- Purpose: detect drift between configured `MET_SOURCE_DIR` and the mapping’s stored origin.

#### 7.3.3 Deterministic naming and sanitization

New `.met` initial placement:

- Default `AF_REL` is `sanitized_met_basename.af` at repo root.
- If unusable (active mapping collision or path already exists), suffix deterministically with `~n` using the smallest `n >= 0` that is free: `Name.af`, `Name~0.af`, `Name~1.af`, ...

Windows-safe sanitization algorithm:

1. Normalize Unicode to NFC.
2. Trim whitespace.
3. Replace invalid Windows filename characters `<>:"/\|?*` with `_`.
4. Replace control characters with `_`.
5. If the leaf name matches a reserved device name (case-insensitive), prefix it with `_`.
6. Collapse runs of `_` to a single `_`.
7. If empty, use `_`.
8. Preserve required `.met` / `.af` extensions.

- REQ-0207 (**MUST**) The naming, suffixing, and sanitization rules in Section 7.3.3 shall be deterministic for identical inputs.

### 7.4 Archive Policy

Archives are app-owned artifacts stored only under `APP_WORK_DIR`. They are never committed to Git.

Archive root:

- `APP_WORK_DIR\.archive\met\YYYY\MM\DD\`

On-disk naming:

- `utc_timestamp_kind_name_sha256prefix.met`
- `utc_timestamp` uses UTC with millisecond precision: `YYYYMMDDTHHMMSSfffZ`
- `kind` in `{before_convert, before_overwrite}`
- `name` uses the sanitization algorithm in Section 7.3.3
- `sha256prefix` uses the first 12 hex characters of the `.met` file’s `MET_ID`
- Collision handling within the same day folder appends a counter to the timestamp: `...Z-1`, `...Z-2`, ...

Retention and cap enforcement:

- Entries within the most recent `ARCHIVE_RETENTION_DAYS` window are retained.
- Size cap accounting includes only files under `APP_WORK_DIR\.archive\met\`.
- If size exceeds `MAX_ARCHIVE_GB` and there exist entries older than the retention window, delete oldest entries older than the window until under cap.
- If size exceeds `MAX_ARCHIVE_GB` due solely to entries within the retention window, keep data and surface a warning.

- REQ-0208 (**MUST**) Archives shall be stored only under `APP_WORK_DIR` using the naming and collision rules in this section.
- REQ-0209 (**MUST**) Archive retention and cap enforcement shall follow the retention-window and “oldest-first beyond window” rules in this section.

Retention enforcement triggers:

- Application startup.
- Successful completion of any run that creates or overwrites `.met` files.

- REQ-0210 (**MUST**) Archive retention and cap enforcement shall run at application startup and after each successful run that creates or overwrites `.met` files.

---

## 8. Core Workflows

### 8.1 Workflow: Bootstrap MetaF

**Trigger:** user initiates bootstrap or first run requires MetaF.  
**Inputs:** `METAF_GIT_URL`, `METAF_INSTALL_MODE`, `METAF_REF`, `APP_WORK_DIR`.  
**Outputs:** installed `METAF_ROOT_DIR`, `METAF_EXE_PATH`, pinned `METAF_BUNDLE_SHA`, bootstrap log.

Steps:

1. Validate bootstrap inputs and required executables (`git` and/or `dotnet`) for selected install mode.
2. Install MetaF into an app-owned location under `APP_WORK_DIR` (mode-specific).
3. Compute `METAF_BUNDLE_SHA` from the installed bundle.
4. Perform a compatibility probe (`--version` or equivalent) to confirm the installed MetaF is runnable.
5. Persist updated bootstrap outputs into `config.yaml`.

- REQ-0301 (**MUST**) MetaF shall be installed under `APP_WORK_DIR` and pinned by `METAF_BUNDLE_SHA` computed by the canonical manifest algorithm in Section 11.1.
- REQ-0302 (**MUST**) `METAF_INSTALL_MODE` shall accept only `repo_release_zip` or `dotnet_publish`; unknown values shall be rejected with a stable error code.
- REQ-0303 (**MUST**) For `repo_release_zip`, the selected release tag shall contain exactly one `.zip` asset; otherwise bootstrap shall fail with a stable error code.
- REQ-0304 (**MUST**) For `dotnet_publish`, `METAF_REF` shall be a full 40-hex commit SHA and the published output installed into `METAF_ROOT_DIR` shall be the basis of `METAF_BUNDLE_SHA`.
- REQ-0305 (**MUST**) Each bootstrap attempt shall write a log file under `APP_WORK_DIR\logs\bootstrap\` named `UTC_TIMESTAMP_RESULT.log` and retain the most recent 50 attempt logs in deterministic filename order.
- REQ-0306 (**MUST**) On bootstrap failure, the installer shall perform exactly one clean-cache retry after deleting the MetaF install directory under `APP_WORK_DIR`.
- REQ-0307 (**MUST**) MetaF execution shall be invoked per input file with explicit input and explicit output paths.
- REQ-0308 (**MUST**) The app shall avoid global machine mutation when supplying MetaF prerequisites; it shall prefer a compatible system `.NET` runtime, and otherwise provision an app-managed runtime under `APP_WORK_DIR\deps\dotnet\` without PATH mutation, registry edits, or elevation prompts.

### 8.2 Workflow: Convert `.met → .af`

**Trigger:** user selects “Convert .met → .af” and approves Preview.  
**Inputs:** `MET_SOURCE_DIR`, `AF_REPO_DIR`, `mapping.yaml` (if present), pinned MetaF bundle.  
**Outputs:** staged `.af` files, staged updated `mapping.yaml`, commit (optional), archives under `APP_WORK_DIR`.

Steps:

1. Scan `MET_SOURCE_DIR` (flat) for `.met` files and validate structural constraints.
2. Load and validate `mapping.yaml` (if present); compute mapping snapshot hash.
3. Apply relink logic for missing `.af` references (Section 8.5).
4. Build Plan and Preview including all writes and side effects.
5. On approval, archive each `.met` input (before conversion) under `APP_WORK_DIR`.
6. Invoke MetaF per file into `STAGING_DIR` and build the staged next `mapping.yaml`.
7. Run publish preflight; then Publish `.af` files and `mapping.yaml` into `AF_REPO_DIR` using atomic temp-then-replace.
8. Stage and commit planned outputs in Git; push based on policy.

- REQ-0310 (**MUST**) Each run shall produce a deterministic Plan that captures the mapping snapshot hash (or a sentinel for first run) and the `METAF_BUNDLE_SHA` used by the Plan.
- REQ-0311 (**MUST**) For `.met → .af`, each input `.met` shall be archived under `APP_WORK_DIR` immediately before invoking MetaF for that file after approval, and Publish shall commit only planned `.af` outputs plus `mapping.yaml`.
- REQ-0312 (**MUST**) `MET_SOURCE_DIR` shall be treated as flat; if any subdirectory exists directly under `MET_SOURCE_DIR` at scan time, planning shall fail fast with a stable error code.
- REQ-0314 (**MUST**) If a `.met` file identity matches an existing mapping entry but its filename differs, the mapping’s `MET_REL` shall be updated in the staged mapping and persisted only after a successful run.
- REQ-0315 (**MUST**) When `.met` replacement is detected, the UI shall prompt per item to either update the existing mapping entry in place (keeping `AF_REL`) or skip; ingesting as a new item shall require a different unused `MET_REL`.
- REQ-0318 (**MUST**) `.met → .af` archive creation shall use kind `before_convert` and follow Section 7.4 naming rules.

#### 8.2.1 Git behavior for `.met → .af`

- Staging includes `mapping.yaml` and all produced `.af` outputs from the Plan.
- Commit message format: `LIGHTNING: met→af COUNT files` where `COUNT` is the number of staged `.af` files.
- Author identity defaults to user git config and can be overridden via `REPO_PROFILES` matching `AF_REPO_DIR`.
- Clean repo definition:
  - No modified tracked files.
  - No staged changes.
  - No unmerged paths.
  - No untracked files that are not ignored.
- Push policy:
  - `GIT_PUSH_POLICY=always` attempts push after successful commit.
  - `GIT_PUSH_POLICY=never` skips push and reports what would have been pushed.

- REQ-0319 (**MUST**) Git staging for `.met → .af` shall include only explicit planned outputs (`mapping.yaml` and planned `.af` files) and shall exclude archives and `APP_WORK_DIR` content.
- REQ-0320 (**MUST**) The commit message format for `.met → .af` shall be deterministic: `LIGHTNING: met→af COUNT files`.
- REQ-0321 (**MUST**) If `REQUIRE_CLEAN_REPO=true`, the run shall abort before Publish preflight completes when the repo is not clean under the definition in this section.
- REQ-0322 (**MUST**) Push behavior shall follow `GIT_PUSH_POLICY` and occur only after a successful commit and successful Publish.

### 8.3 Workflow: Convert `.af → .met`

**Trigger:** user selects “Convert .af → .met” and approves Preview.  
**Inputs:** `AF_REPO_DIR`, `mapping.yaml`, `.af` files.  
**Outputs:** staged `.met` outputs, archives under `APP_WORK_DIR`, Publish into `MET_SOURCE_DIR`.

Steps:

1. Load and validate `mapping.yaml` (strict).
2. Build Plan and Preview including `.met` destination paths under `MET_SOURCE_DIR`.
3. On approval, invoke MetaF per file into `STAGING_DIR`.
4. Run publish preflight; then Publish `.met` outputs into `MET_SOURCE_DIR` via atomic temp-then-replace.
5. Archive any overwritten destination `.met` immediately before overwrite.

- REQ-0316 (**MUST**) For `.af → .met`, outputs shall be written only to `MET_SOURCE_DIR\MET_REL` for active items, and any existing destination shall be archived before overwrite.
- REQ-0317 (**MUST**) If required mapping fields for `.af → .met` are missing or invalid, the run shall fail at mapping validation and shall not proceed to execution.
- REQ-0323 (**MUST**) `.af → .met` archive creation for overwrite shall use kind `before_overwrite` and follow Section 7.4 naming rules.

### 8.4 Workflow: Repair Mapping

**Trigger:** user initiates Repair Mapping after `MET_SOURCE_ROOT_FINGERPRINT` mismatch.  
**Inputs:** current `MET_SOURCE_DIR`, existing `mapping.yaml`.  
**Outputs:** updated `mapping.yaml` (transactional write) or no change on cancel.

Steps:

1. Compute new `MET_SOURCE_ROOT_FINGERPRINT` for the configured `MET_SOURCE_DIR`.
2. Rescan `MET_SOURCE_DIR` (flat) and compute `MET_ID` for each `.met` file.
3. For each mapping item, set state:
   - If `MET_ID` present in scan: set `STATE=active`, update `MET_REL`, clear retire timestamp.
   - If `MET_ID` absent: set `STATE=retired` and set `RETIRED_UTC` if transitioning from active.
4. Enforce invariants and write `mapping.yaml` transactionally.
5. If canceled, perform no writes.

- REQ-0324 (**MUST**) Repair Mapping shall update `MET_SOURCE_ROOT_FINGERPRINT`, apply deterministic activation/retirement rules, enforce invariants, and write `mapping.yaml` transactionally; cancel shall result in no writes.

### 8.5 Workflow: Relink `.af` moves and renames

**Trigger:** run start detects referenced `AF_REL` missing or unmapped `.af` candidates present.  
**Inputs:** `mapping.yaml`, filesystem scan of repo.  
**Outputs:** updated mapping snapshot (staged only; persisted only after successful run).

Strategy per missing mapping entry:

1. Candidate search among unmapped `.af` files by size (when known) and then `AF_SHA256`.
2. If exactly one match: update `AF_REL` to candidate relative path.
3. If multiple or none: prompt user to Locate or Skip.
4. Locate: user selects an `.af` file within `AF_REPO_DIR` only; mapping updates `AF_REL` and `AF_SHA256`.
5. Skip: skip that item for this run; no mapping changes for the entry.

- REQ-0325 (**MUST**) Relink shall match missing entries by size and hash, and Locate shall restrict selection to `.af` files under `AF_REPO_DIR`; Skip shall avoid mapping changes for that entry.

---

## 9. Planning, Preview, and Execution

### 9.1 Plan Object (Deterministic)

Plan includes:

- Inputs snapshot:
  - Canonical paths of roots.
  - File lists used by the run (canonical ordering).
  - Mapping snapshot hash or sentinel.
- Outputs list:
  - All paths intended for Publish.
  - All archives and logs intended under `APP_WORK_DIR`.
- Actions list:
  - Per-file conversions.
  - Git actions: stage/commit/push (when applicable).
- Environment fingerprint:
  - OS version, app version.
- Toolchain fingerprint:
  - `METAF_BUNDLE_SHA` and MetaF executable path.
  - Versions of `git` and `dotnet` used (when applicable).

- REQ-0401 (**MUST**) The Plan shall be stable given identical inputs, configuration, and pinned toolchain.
- REQ-0402 (**MUST**) Preview shall enumerate all filesystem writes and Git side effects that would occur during Publish before execution begins.

### 9.2 Preview

Preview shows:

- Created/updated files in `AF_REPO_DIR` and/or `MET_SOURCE_DIR`.
- Mapping changes and relink outcomes (including any skipped items).
- Conflict detections and how `CONFLICT_POLICY` would resolve them.
- Archive entries that would be created under `APP_WORK_DIR`.

### 9.3 Execution

- Execution modes: `PLAN` and `APPLY`.
- Transactionality: best-effort all-or-nothing Publish via staging plus atomic temp-then-replace operations.
- Rollback: no rollback for archives/logs under `APP_WORK_DIR`; no partial Publish on preflight failure; publish uses atomic replace per file.

- REQ-0403 (**MUST**) If execution is canceled before Publish begins, no Publish shall occur; staging shall be cleaned best-effort; archives and logs may remain under `APP_WORK_DIR`.
- REQ-0404 (**MUST**) Publish shall route all user-controlled writes through `STAGING_DIR` and atomic temp-then-replace; publish preflight shall be all-or-nothing, and `CONFLICT_POLICY` shall be applied before the first Publish write.
- REQ-0405 (**MUST**) The app shall prevent concurrent runs that share the tuple (`AF_REPO_DIR`, `MET_SOURCE_DIR`) using a lock derived from canonical paths.

### 9.4 Plan Snapshot Integrity

- Plan captures:
  - Mapping snapshot hash (or sentinel on first run).
  - `METAF_BUNDLE_SHA` used for the plan.
- At execution start (before any conversion work), these are re-validated against current state.

- REQ-0406 (**MUST**) If the mapping snapshot hash differs from the Plan snapshot or `METAF_BUNDLE_SHA` differs from the Plan snapshot at execution start, execution shall abort and require re-planning.

### 9.5 Publish Preflight and Conflict Handling

Publish preflight occurs immediately before the first Publish write and includes:

- Plan snapshot integrity (Section 9.4).
- Repo cleanliness rule (Section 8.2.1 when enabled).
- Destination collision classification using the Plan’s mapping snapshot:
  - `existing-mapped` destinations are overwrite-eligible.
  - `new-mapped` destinations follow `CONFLICT_POLICY`.

- REQ-0407 (**MUST**) Publish preflight shall verify Plan snapshot integrity and apply destination collision handling using `existing-mapped` and `new-mapped` classification prior to the first Publish write.

### 9.6 Staging Layout

Staging root:

- `STAGING_DIR = APP_WORK_DIR\.staging\RUN_ID\`

Layout:

- `.met → .af`:
  - `.af` outputs: `STAGING_DIR\af\AF_REL`
  - next mapping: `STAGING_DIR\mapping.yaml`
- `.af → .met`:
  - `.met` outputs: `STAGING_DIR\met\MET_REL`

- REQ-0408 (**MUST**) All Publish outputs shall be written into the staging layout in this section before Publish begins.

---

## 10. Security, Safety, and Trust Boundaries

### 10.1 Threat Model (Minimal)

- Assets: user repositories and content under `AF_REPO_DIR` and `MET_SOURCE_DIR`, mapping integrity, pinned MetaF bundle identity, archives/logs under `APP_WORK_DIR`.
- Adversaries: local malware, untrusted repository content, accidental user misconfiguration, path traversal tricks via filesystem links.
- Entry points: configuration values, filesystem enumeration results, MetaF output, Git operations.

### 10.2 Trust Boundary Rules

Allowed roots:

- `APP_CONFIG_DIR`, `MET_SOURCE_DIR`, `AF_REPO_DIR`, `APP_WORK_DIR`.

Boundary enforcement uses:

- Canonicalization using `Path.GetFullPath` on inputs.
- Descendant checks that include the root itself.
- Final-path enforcement: boundary checks are applied both to the canonical input path and to the final resolved path after link resolution.
- Volume constraints: `MET_SOURCE_DIR`, `AF_REPO_DIR`, and `APP_WORK_DIR` are restricted to local fixed volumes; UNC and mapped network volumes are rejected.
- Reparse points:
  - Enumeration does not traverse reparse points (treat as leaf entries).
  - Default policy blocks user-content writes that encounter reparse points and allows reads that remain within boundary after final-path enforcement.

- REQ-0501 (**MUST**) All user-content file operations shall enforce allowed roots, canonicalization, descendant checks, final-path enforcement, and volume constraints.
- REQ-0502 (**MUST**) `REPARSE_POINT_POLICY` shall be enforced as follows: default `block_writes_allow_reads` blocks user-content writes encountering reparse points and logs reads that cross reparse points; `block_all` blocks both reads and writes encountering reparse points, and enumeration does not traverse them.
- REQ-0503 (**MUST**) Git operations shall stage only explicitly planned outputs and shall not stage by wildcard or directory glob.

### 10.3 Input Validation

- Configuration parsing validates required types and supported enum values.
- External tool invocation validates explicit paths and rejects invalid or out-of-bound paths before process execution.

- REQ-0504 (**MUST**) All external inputs (config values, file paths, tool outputs used for decisions) shall be validated and rejected on boundary or schema violation with stable error codes.

---

## 11. Determinism and Reproducibility

- Canonical ordering: case-insensitive ordering of relative paths using ordinal comparison for Windows stability.
- Canonical encoding: UTF-8 with LF line endings for generated text files.
- Hashing algorithm: SHA-256.
- Locale/timezone: timestamps written in UTC; comparisons use canonical UTC forms.

### 11.1 Canonical MetaF bundle manifest and `METAF_BUNDLE_SHA`

Compute `METAF_BUNDLE_SHA` over a canonical manifest of `METAF_ROOT_DIR`:

1. Enumerate all files under `METAF_ROOT_DIR` recursively (exclude directories).
2. For each file compute:
   - `REL`: relative path with `/` separators.
   - `SIZE`: size in bytes.
   - `SHA`: `sha256:{hex}` of file bytes.
3. Sort records by `REL` ascending (case-insensitive).
4. Serialize as UTF-8 lines: `REL\tSIZE\tSHA\n`.
5. Compute: `METAF_BUNDLE_SHA = sha256("v1\n" + all lines)` and store as `sha256:{hex}`.

- REQ-0601 (**MUST**) All hashes (`MET_ID`, `AF_SHA256`, `METAF_BUNDLE_SHA`) shall be computed over canonicalized representations using SHA-256 as defined in this section.
- REQ-0602 (**MUST**) Generated files written by LIGHTNING (`mapping.yaml` and any managed workspace files) shall use canonical encoding and deterministic key ordering, and shall be written transactionally (temp then atomic replace) within the target directory.

---

## 12. Observability and Error Handling

### 12.1 Logging

- Locations:
  - Bootstrap logs: `APP_WORK_DIR\logs\bootstrap\`
  - Run logs: `APP_WORK_DIR\logs\runs\RUN_ID\`
- Redaction:
  - Logs avoid storing secrets, tokens, or credentials.
- Retention:
  - Bootstrap logs follow the retention rule in REQ-0305.
  - Run log retention is implementation-defined; logs reside under `APP_WORK_DIR` and can be deleted by user choice.

- REQ-0701 (**MUST**) Logs shall not include secrets or sensitive tokens.
- REQ-0702 (**MUST**) All user-visible failures shall surface a stable error code in the `LIGHTNING_{CATEGORY}_{NNNN}` format.

### 12.2 Error Taxonomy

| Code | Category | User Action | Retryable |
| --- | --- | --- | --- |
| `LIGHTNING_FS_0001` | Filesystem boundary | Fix paths or reparse points | No |
| `LIGHTNING_PLAN_0001` | Plan invalid | Re-plan and re-run | Yes |
| `LIGHTNING_MAP_0001` | Mapping invalid | Repair mapping | No |
| `LIGHTNING_METAF_0001` | MetaF bootstrap/run | Fix MetaF install settings | Maybe |
| `LIGHTNING_GIT_0001` | Git operation | Clean repo or fix git config | Maybe |
| `LIGHTNING_VSCODE_0001` | VS Code launch | Install VS Code or adjust path | Yes |

### 12.3 Exit Codes (Process)

| Exit Code | Meaning |
| ---: | --- |
| 0 | Success |
| 1 | User-canceled without Publish |
| 2 | Validation or planning failure |
| 3 | Execution or Publish failure |

---

## 13. Performance and Limits

- Expected scale: hundreds to thousands of `.met` and `.af` files.
- Responsiveness: UI remains interactive during scans and conversion runs, with cancellation available prior to Publish.

- REQ-0801 (**SHOULD**) The UI should remain responsive during scanning and execution by offloading heavy work off the UI thread.

---

## 14. Compatibility

- Supported OS: Windows 10+.
- Dependencies: `.NET` runtime as required for MetaF execution; system-first and otherwise app-managed under `APP_WORK_DIR`.

- REQ-0901 (**MUST**) Upgrades of LIGHTNING shall not corrupt persisted data (`config.yaml`, `mapping.yaml`, archives).

---

## 15. Acceptance Criteria

### 15.1 Definition of Done

- User can configure directory roots and MetaF install settings.
- MetaF is installed into `APP_WORK_DIR`, runnable, and pinned by `METAF_BUNDLE_SHA`.
- `.met → .af` produces `.af` outputs and a valid updated `mapping.yaml`, and commits only planned `.af` plus `mapping.yaml` to Git.
- `.af → .met` produces `.met` outputs in `MET_SOURCE_DIR` safely, archiving any overwritten destinations.
- Mapping survives `.af` moves/renames via relink plus Locate/Skip.
- Archives reside only under `APP_WORK_DIR` and obey retention/cap policy rules.
- User-content I/O stays within allowlisted roots and fixed local volumes.
- VS Code Open Workspace works when VS Code is present, and conversion runs remain correct when it is absent.

- REQ-1001 (**MUST**) The project is “done” only when all acceptance tests in Appendix B pass.

---

## Appendix A. Decision Tables

### A.1 Cancellation Guarantees

| Phase | Cancellation Result | Allowed Side Effects | Required Cleanup |
| --- | --- | --- | --- |
| Plan/Preview | Run aborts; no Publish | Reads and logs under `APP_WORK_DIR` | Best-effort delete run staging if created |
| Execution (pre-Publish) | Run aborts; no Publish | Archives/logs under `APP_WORK_DIR` | Best-effort delete `STAGING_DIR` |
| Publish | Cancellation deferred until Publish completes | Atomic per-file publishes can complete | None beyond existing publish semantics |
| Post-Publish (Git push) | Push can be skipped if not started | Commit already exists | Surface status and logs |

### A.2 Conflict Policy

| Condition | Policy | Resolution | User Messaging |
| --- | --- | --- | --- |
| New-mapped destination collides at Publish | `fail` | Abort Publish before first write | Explain collision and path |
| New-mapped destination collides at Publish | `suffix` | Suffix leaf filename with `~n` and update staged mapping | Explain final chosen path |
| Any destination collides at Publish | `fail_all` | Abort Publish before first write | Explain collisions and paths |
| Existing-mapped destination collides | `fail` / `suffix` | Overwrite allowed | Show overwrite in Preview |

### A.3 Publish Policy

| Output Type | Publish Action | Atomicity | Rollback |
| --- | --- | --- | --- |
| `.af` | Write temp then atomic replace in target dir | Per-file atomic | No rollback; preflight prevents partial start |
| `mapping.yaml` | Transactional replace | Atomic replace | No rollback |
| `.met` | Write temp then atomic replace; archive before overwrite | Per-file atomic | No rollback |
| Archives/logs | Write under `APP_WORK_DIR` | Best-effort | Not rolled back on cancel |

---

## Appendix B. Acceptance Tests (Given/When/Then)

### AT-0001: Invariants — Platform and Naming

Given the project source and this spec  
When inspecting platform and naming constraints  
Then namespaces use `LIGHTNING.*` and the app targets Windows 10+ local desktop use.

### AT-0002: Boundary — Allowed Roots and Fixed Volumes

Given configured roots `MET_SOURCE_DIR`, `AF_REPO_DIR`, and `APP_WORK_DIR`  
When any user-content file operation is attempted  
Then it is rejected unless the canonical and final resolved path remain within an allowed root on a fixed local volume.

### AT-0003: Boundary — Reparse Point Policy

Given `REPARSE_POINT_POLICY=block_writes_allow_reads`  
When a write attempts to traverse a reparse point within an allowed root  
Then the write is rejected with a stable filesystem error code.

### AT-0004: Plan Stability

Given identical configuration, pinned MetaF bundle, mapping snapshot, and filesystem inputs  
When building a Plan twice  
Then the Plan outputs list and action ordering are identical.

### AT-0005: Preview Enumerates Writes

Given a Plan for a run  
When rendering Preview  
Then Preview enumerates every intended Publish write (paths) and every Git side effect (stage/commit/push) before execution begins.

### AT-0006: Cancel Before Publish Produces No Publish

Given a run in Plan/Preview or pre-Publish execution  
When the user cancels the run  
Then no files are written into `AF_REPO_DIR` or `MET_SOURCE_DIR` and no Git staging/commit/push occurs.

### AT-0007: Staging and Atomic Publish

Given a Plan with Publish outputs  
When executing the run  
Then outputs are written to `STAGING_DIR` first and then published via temp-then-atomic-replace into user-controlled directories.

### AT-0008: Conflict Policy Behavior

Given a Publish-time destination collision for a new-mapped output  
When `CONFLICT_POLICY` is `fail`, `suffix`, or `fail_all`  
Then the resolution matches Appendix A.2 and Preview reflects the chosen outcome.

### AT-0009: Bootstrap — repo_release_zip asset selection

Given `METAF_INSTALL_MODE=repo_release_zip` and a tag `METAF_REF`  
When bootstrap runs  
Then it fails unless the tag provides exactly one `.zip` asset, and success records `METAF_BUNDLE_SHA`.

### AT-0010: Bootstrap — dotnet_publish pinning

Given `METAF_INSTALL_MODE=dotnet_publish` and a 40-hex `METAF_REF`  
When bootstrap runs  
Then MetaF is built from that exact commit and the installed output is used for `METAF_BUNDLE_SHA`.

### AT-0011: MetaF Bundle Hash Manifest

Given an installed `METAF_ROOT_DIR`  
When computing `METAF_BUNDLE_SHA`  
Then the canonical manifest algorithm in Section 11.1 is used and produces a stable result for identical bundle contents.

### AT-0012: Bootstrap Logs Retention

Given multiple bootstrap attempts  
When bootstrap logging retention is applied  
Then only the most recent 50 attempt logs remain under `APP_WORK_DIR\logs\bootstrap\` in deterministic filename order.

### AT-0013: MetaF Prerequisites Without Global Mutation

Given MetaF requires a `.NET` runtime  
When the app prepares to run MetaF  
Then it prefers a compatible system runtime or uses an app-managed runtime under `APP_WORK_DIR\deps\dotnet\` without PATH mutation, registry edits, or elevation prompts.

### AT-0014: met→af Archives and Git Commit Contents

Given a `.met → .af` run  
When execution proceeds after approval  
Then each `.met` input is archived with kind `before_convert` before conversion and the Git commit stages only planned `.af` files plus `mapping.yaml`.

### AT-0015: af→met Overwrite Archives

Given an `.af → .met` run where a destination `.met` already exists  
When publishing the output  
Then the destination is archived with kind `before_overwrite` before overwrite and the output is atomically replaced.

### AT-0016: mapping.yaml Strictness and Invariants

Given a `mapping.yaml` with unknown keys or invariant violations  
When the app loads mapping  
Then the load fails with a stable mapping error code and conversion runs are blocked.

### AT-0017: MET_SOURCE_ROOT_FINGERPRINT Drift and Repair Mapping

Given a mismatch between configured `MET_SOURCE_DIR` and `mapping.yaml` fingerprint  
When starting a conversion run  
Then the run is blocked and Repair Mapping updates the fingerprint and item states deterministically on completion.

### AT-0018: Relink Locate Restriction

Given a missing mapped `AF_REL`  
When the user chooses Locate  
Then only `.af` files within `AF_REPO_DIR` are selectable and choosing a file updates `AF_REL` and `AF_SHA256` in the staged mapping.

### AT-0019: `.met` Rename Handling

Given a `.met` file whose `MET_ID` matches a mapping entry but whose filename differs  
When planning a `.met → .af` run  
Then the staged mapping updates `MET_REL` and persists it only after a successful run.

### AT-0020: `.met` Replacement Prompt

Given a `.met` filename matches an active mapping entry but content differs (`MET_ID` differs)  
When planning a `.met → .af` run  
Then the UI prompts to update-in-place or skip, and update-in-place retains `AF_REL`.

### AT-0021: Run Exclusivity Lock

Given two runs targeting the same tuple (`AF_REPO_DIR`, `MET_SOURCE_DIR`)  
When the second run starts  
Then it fails fast with a stable error code before doing partial work.

### AT-0022: Git Guardrails

Given a `.met → .af` run with planned outputs  
When staging occurs  
Then staging uses explicit file paths and never uses wildcard staging or directory globs.

### AT-0023: Stable Error Codes

Given a user-visible failure  
When the app surfaces the error  
Then it includes a stable `LIGHTNING_{CATEGORY}_{NNNN}` code and a short message.

### AT-0024: Log Redaction

Given runs that involve external tools  
When logs are written  
Then logs avoid writing secrets and tokens.

### AT-0025: Transactional Writes for Generated Files

Given a planned update to `mapping.yaml` (and any managed workspace file)  
When publishing that update  
Then the write occurs via temp file then atomic replace within the target directory using canonical UTF-8 with LF.

### AT-0026: Archive Naming, Retention, and Cap Enforcement

Given archive entries under `APP_WORK_DIR\.archive\met\`  
When creating archives and the app starts or a run finishes successfully  
Then naming follows Section 7.4 and retention/cap enforcement deletes oldest entries beyond the retention window first.

### AT-0027: Repo Cleanliness and Push Policy

Given `REQUIRE_CLEAN_REPO=true` and the repo has a dirty working tree  
When starting Publish preflight for `.met → .af`  
Then the run aborts before Publish; and when `GIT_PUSH_POLICY=never`, no push is attempted after commit.

### AT-0028: Plan Snapshot Integrity

Given a Plan created against a mapping snapshot and a pinned `METAF_BUNDLE_SHA`  
When either the mapping file or MetaF bundle identity changes before execution start  
Then execution aborts and requires a new Plan.

### AT-0029: Staging Layout and Publish Preflight Collision Classification

Given a run with staged outputs  
When Publish preflight runs  
Then collisions are classified as `existing-mapped` or `new-mapped` and outputs exist in the staging layout before the first Publish write.

### AT-0030: MET_SOURCE_DIR Flat Enforcement

Given `MET_SOURCE_DIR` contains a subdirectory  
When planning a run that scans `MET_SOURCE_DIR`  
Then planning fails fast with a stable error code and no partial execution occurs.

### AT-9000: Spec Lint — Normative Isolation

Given this spec  
When scanning for RFC keywords in informative sections  
Then no RFC keywords appear outside normative sections.

### AT-9001: Spec Lint — Traceability

Given this spec  
When enumerating all `REQ-####` requirements  
Then each has at least one mapped acceptance test.

---

## Appendix C. Traceability Matrix

| Requirement ID | Statement (Short) | Sections | Acceptance Tests |
| --- | --- | --- | --- |
| REQ-0001 | Platform and local operation | 4 | AT-0001 |
| REQ-0002 | Namespace naming rule | 4 | AT-0001 |
| REQ-0003 | Boundary enforcement required | 4, 10 | AT-0002, AT-0003 |
| REQ-0004 | Preview + approval before Publish | 4, 6 | AT-0005, AT-0006 |
| REQ-0005 | User-triggered runs only | 4 | AT-0001 |
| REQ-0101 | UI gates Publish on approval | 6 | AT-0005, AT-0006 |
| REQ-0102 | VS Code best-effort, non-blocking | 6 | AT-0001 |
| REQ-0103 | Workspace management gated | 6 | AT-0001 |
| REQ-0104 | Conversions work offline post-bootstrap | 6 | AT-0001 |
| REQ-0105 | Optional integrations non-blocking | 6 | AT-0001 |
| REQ-0201 | config.yaml location + lenient parse | 7 | AT-0002 |
| REQ-0202 | Config validation rules | 7 | AT-0002, AT-0030 |
| REQ-0203 | Forward-only, idempotent upgrades | 7 | AT-9001 |
| REQ-0204 | mapping.yaml strict parse | 7 | AT-0016 |
| REQ-0205 | Fingerprint drift blocks runs | 7 | AT-0017 |
| REQ-0206 | MET_REL/AF_REL validation + uniqueness | 7 | AT-0016 |
| REQ-0207 | Deterministic naming/sanitization | 7 | AT-0004 |
| REQ-0208 | Archive storage and naming rules | 7 | AT-0026 |
| REQ-0209 | Archive retention and cap rules | 7 | AT-0026 |
| REQ-0210 | Archive retention enforcement triggers | 7 | AT-0026 |
| REQ-0301 | MetaF installed and pinned by bundle hash | 8 | AT-0011 |
| REQ-0302 | Install mode enum restriction | 8 | AT-0009, AT-0010 |
| REQ-0303 | Release zip asset uniqueness | 8 | AT-0009 |
| REQ-0304 | dotnet_publish pinned by commit | 8 | AT-0010 |
| REQ-0305 | Bootstrap logging + retention 50 | 8 | AT-0012 |
| REQ-0306 | One clean-cache retry | 8 | AT-0009, AT-0010 |
| REQ-0307 | MetaF per-file explicit paths | 8 | AT-0014, AT-0015 |
| REQ-0308 | Prereqs without global mutation | 8 | AT-0013 |
| REQ-0310 | Plan captures mapping + toolchain snapshot | 8 | AT-0004, AT-0028 |
| REQ-0311 | met→af archives + commit scope | 8 | AT-0014 |
| REQ-0312 | MET_SOURCE_DIR flat scan constraint | 8 | AT-0030 |
| REQ-0314 | met rename updates MET_REL after success | 8 | AT-0019 |
| REQ-0315 | met replacement prompt update/skip | 8 | AT-0020 |
| REQ-0316 | af→met destination + archive overwrite | 8 | AT-0015 |
| REQ-0317 | Invalid mapping blocks af→met | 8 | AT-0016 |
| REQ-0318 | met→af archive kind before_convert | 8 | AT-0014 |
| REQ-0319 | Git staging limited to planned outputs | 8 | AT-0022 |
| REQ-0320 | Deterministic commit message format | 8 | AT-0027 |
| REQ-0321 | Clean repo enforcement when enabled | 8 | AT-0027 |
| REQ-0322 | Push policy semantics | 8 | AT-0027 |
| REQ-0323 | af→met archive kind before_overwrite | 8 | AT-0015 |
| REQ-0324 | Repair Mapping deterministic + transactional | 8 | AT-0017 |
| REQ-0325 | Relink match/Locate/Skip rules | 8 | AT-0018 |
| REQ-0401 | Plan stability | 9 | AT-0004 |
| REQ-0402 | Preview enumerates writes and side effects | 9 | AT-0005 |
| REQ-0403 | Cancel pre-Publish prevents Publish | 9 | AT-0006 |
| REQ-0404 | Staging + atomic publish + conflict policy | 9 | AT-0007, AT-0008 |
| REQ-0405 | Run lock by tuple | 9 | AT-0021 |
| REQ-0406 | Snapshot integrity revalidation | 9 | AT-0028 |
| REQ-0407 | Preflight collision classification | 9 | AT-0029 |
| REQ-0408 | Staging layout required | 9 | AT-0029 |
| REQ-0501 | Boundary rules enforced on all user-content ops | 10 | AT-0002 |
| REQ-0502 | Reparse point policy enforcement | 10 | AT-0003 |
| REQ-0503 | Git stages only explicit planned outputs | 10 | AT-0022 |
| REQ-0504 | Validate external inputs and reject violations | 10 | AT-0002 |
| REQ-0601 | Canonicalized SHA-256 hashing | 11 | AT-0011 |
| REQ-0602 | Transactional deterministic generated files | 11 | AT-0025 |
| REQ-0701 | Logs exclude secrets | 12 | AT-0024 |
| REQ-0702 | Stable error codes surfaced | 12 | AT-0023 |
| REQ-0801 | UI responsiveness recommendation | 13 | AT-9001 |
| REQ-0901 | Upgrades do not corrupt persisted data | 14 | AT-9001 |
| REQ-1001 | All acceptance tests pass for done | 15 | AT-9001 |

---

## Appendix D. Open Questions

None.

---

## Appendix E. Deferred Enhancements (Non-Normative)

- Optional Job Object execution of MetaF for stricter process-tree cancellation and resource limits.
- Optional managed creation/update of `.vscode/extensions.json` and other workspace files under explicit user action.
- Optional single-file publish of the LIGHTNING app.
- Optional “export all” modes in related toolchains when integrated into future workflows.
