# Empyrean Codex: LIGHTNING

Date: 2025-12-15  
Target platform: Windows 10+ (local-only)  
Primary users: Virindi Tank Meta Authors  
Primary toolchain: Visual Studio (C#/.NET), WPF (XAML), YAML config  
**Naming rule:** do **not** use `EC.` prefixing in solution/project/namespaces. Use **ALL CAPS**: `LIGHTNING.*`.

---

## 0. Executive summary

Build a Windows desktop app that:

- Bootstraps **MetaF** locally on first run (clone/install into an app-owned cache directory).
- Converts `.met → .af` and `.af → .met` via MetaF on user command.
- Archives input/output `.met` files locally (not in Git).
- Commits only `.af` (and `mapping.yml`) into a user-selected Git repo.
- Enforces a strict filesystem boundary: **never read/write outside user-defined directories**.
- Provides a one-click **“Open Workspace”** button that launches the repo in VS Code and ensures required extensions are recommended and best-effort installed.

**Codename expansion:**  
**L**ocally-**I**ntegrated, **G**it-**H**osted **T**ool: **N**ormalized **I**ndexing/**N**aming **G**enerator

---

## 1. Product goals

### 1.1 Goals

- **Automation:** One-click batch conversion and archiving.
- **Round-trip workflow:** Support `.met ↔ .af` without losing file identity.
- **Reproducibility:** Each run records the MetaF commit SHA, inputs, outputs, and results.
- **Safety:** Hard boundary preventing file access outside user-approved roots.
- **Ergonomics:** GUI-first experience with clear previews, logs, and progress.

### 1.2 Non-goals (initially)

- Multi-machine sync / cloud hosting.
- Editing `.met` or `.af` contents inside the app.
- Running MetaF on remote machines.
- Deep Git operations (rebases, merges, PR workflows).

---

## 2. Key requirements

### 2.1 Functional requirements

- FR-01: Configure directories:
  - `MET_SOURCE_DIR` (user-chosen; `.met` source)
  - `AF_REPO_DIR` (user-chosen; Git working repo containing `.af`)
  - `APP_WORK_DIR` (app-owned; cache + archive + logs; default under `%LocalAppData%`)
- FR-02: First-run MetaF bootstrap:
  - Git URL + ref (branch/tag/SHA)
  - Install into app-owned cache (under `APP_WORK_DIR`)
  - Record resolved MetaF SHA and resolved executable path
- FR-03: Convert `.met → .af` (batch):
  - Enumerate `.met` in source dir
  - Copy each `.met` into local archive
  - Run MetaF to produce `.af`
  - Write `.af` into repo at deterministic path
  - Stage/commit only `.af` + `mapping.yml` for that run
- FR-04: Convert `.af → .met` (batch or selection):
  - Use `mapping.yml` to determine logical `.met` identity
  - Run MetaF to produce `.met`
  - Archive output `.met` locally (not committed)
- FR-05: Mapping persistence:
  - Store and maintain `mapping.yml` in the repo
  - Update on every run (adds new items, updates last-run metadata)
- FR-06: Preview mode:
  - Show the plan: which files will be processed, where outputs go, what will be committed
  - No writes outside app work dir during preview
- FR-07: “Open Workspace” button:
  - Launch VS Code opening `AF_REPO_DIR`
  - Ensure required extensions are recommended and best-effort installed (Section 6)

### 2.2 Non-functional requirements

- NFR-01: Strict filesystem boundary enforcement (Section 3)
- NFR-02: Deterministic outcomes (same inputs + same MetaF SHA → same outputs)
- NFR-03: Robust logging per run (plan + stdout/stderr + git log + results)
- NFR-04: Cancellation support for long batch jobs
- NFR-05: Safe failure handling:
  - No partial git commit if job fails mid-run
  - Clear UI error surface with access to logs
- NFR-06: Distribution: `LIGHTNING.App` MUST be shippable as a **self-contained** Windows app (no preinstalled .NET Desktop Runtime required).
- NFR-07: Distribution (future): `LIGHTNING.App` SHOULD support **self-contained + single-file** publish (tracked milestone item).

---

## 3. Security & safety boundary (hard constraint)

### 3.1 Allowed roots (explicit allowlist)

All filesystem operations MUST be scoped to these canonical root directories:

**Internal (fixed, app-owned):**

- `APP_CONFIG_DIR` (read/write): `%LocalAppData%\EmpyreanCodex\LIGHTNING\`  
  - stores `config.yml`  
  - may also host app-owned metadata under subfolders (implementation detail)

**User-defined (from config.yml):**

- `MET_SOURCE_DIR` (read/write: produce `.met`, enumeration and reading)
- `AF_REPO_DIR` (read/write: write `.af`, write mapping, run git)
- `APP_WORK_DIR` (read/write: archive, cache, logs)

Anything outside these roots is forbidden.

### 3.2 Canonicalization & descendant checks

- Canonicalize via `Path.GetFullPath(...)` before every operation.
- Reject any path where `!IsDescendant(path, allowedRoot)`.

### 3.3 Reparse points (symlink/junction defense)

- Default policy: **block** reparse points for enumeration and copy.
- Treat any encountered reparse point as an error surfaced to UI (do not follow).

### 3.4 Git guardrails

- Stage only expected output paths for the run (computed from the plan).
- Never stage `*` / never commit “all changes”.

---

## 4. Solution architecture (modular monolith)

### 4.1 Visual Studio solution layout (proposed)

- `LIGHTNING.slnx`
  - `LIGHTNING.App` (WPF UI)
  - `LIGHTNING.Core` (domain logic: plans, mapping, policies)
  - `LIGHTNING.Adapters` (filesystem, process runner, git runner, VS Code launcher)
  - `LIGHTNING.Tests` (unit + integration tests)

### 4.2 Core components

- **ConfigService**
  - Loads/saves `config.yml` under `APP_CONFIG_DIR` (LocalAppData)
- **MappingService**
  - Loads/saves `mapping.yml` in `AF_REPO_DIR`
- **IngestPlanner**
  - Produces an immutable `RunPlan` from filesystem + mapping + config
- **PipelineRunner**
  - Executes `RunPlan` with progress + cancellation
- **MetaFInstaller**
  - Ensures MetaF is available and pinned (clone/install strategies; records SHA + exe path)
- **MetaFRunner**
  - Invokes MetaF; captures stdout/stderr; enforces timeouts
- **GitAdapter**
  - Stages and commits exactly what the plan says
- **BoundaryFileSystem**
  - The only route to user-data IO; enforces allowlist + reparse policy
- **SystemProbe**
  - Read-only system detection (git/dotnet/code presence + versions) and MetaF runtime compatibility hints
- **VSCodeLauncher**
  - Opens repo and handles extension recommendations/installation (Section 6)
- **DependencyInstaller** (M6)
  - Optional managed installs of prerequisites into `APP_WORK_DIR` (system-first, managed fallback)

### 4.3 MVVM UI pattern

- ViewModels: `SetupVM`, `DashboardVM`, `RunPreviewVM`, `RunProgressVM`, `SettingsVM`, `LogsVM`
- Views: XAML + designer-friendly layout (DataGrid for plans/results, ProgressBar, log viewer)

---

## 5. Data model (YAML)

### 5.1 config.yml (local, app workspace)

Stored under: `%LocalAppData%\EmpyreanCodex\LIGHTNING\config.yml`

Fields (minimum):

- `version`
- `met_source_dir`
- `af_repo_dir`
- `app_work_dir`

MetaF (M1):

- `metaf_git_url`
- `metaf_ref` (branch/tag/SHA requested)
- `metaf_sha` (resolved pinned commit SHA)
- `metaf_exe_path` (resolved cached executable)
- `metaf_install_mode` (`repo_release_zip | dotnet_publish`)

Policies:

- `reparse_point_policy` (`block` default)
- `conflict_policy` (`skip | overwrite | version_suffix`)
- `git_push_policy` (`never | manual | auto`)

VS Code:

- `vscode_required_extensions` (list of extension IDs)
- `vscode_preferred_profile` (optional)
- `manage_vscode_workspace_files` (`false` default; controls writing `.vscode/extensions.json`)

Managed dependencies (M6; optional, opt-in):

- `allow_managed_dependencies` (`false` default)

### 5.2 mapping.yml (repo, committed)

Stored under: `<AF_REPO_DIR>\mapping.yml`

Goals:

- deterministically map logical `.met` identity to `.af` location in repo
- support round-trip conversions while keeping repo `.af`-only

Proposed shape:

- `version`
- `created_utc`
- `met_source_root_fingerprint` (e.g., user-defined GUID or normalized path hash)
- `items[]`:
  - `met_rel` (relative path from MET source root; logical identity)
  - `met_archive_key` (content hash key; e.g. `sha256:<...>`)
  - `af_rel` (relative path inside repo)
  - `last_metaf_sha`
  - `last_run_utc`

---

## 6. VS Code “Open Workspace” implementation

### 6.1 What is controllable

- Reliably open VS Code to a folder.
- Recommend extensions via `.vscode/extensions.json`.
- Attempt to install extensions via CLI (`code --install-extension <id>`) if `code` is discoverable.
- “Active” extensions are ultimately controlled by VS Code; LIGHTNING will:
  - recommend + install best-effort,
  - then open the workspace.

### 6.2 Recommended approach

1) Ensure the repo contains `.vscode/extensions.json` listing required extensions.
2) On button click:
   - Discover `code`:
     - try PATH (`code` / `code.cmd`)
     - try common install locations (user + machine installs)
   - If `code` is found:
     - Install missing extensions (best-effort, non-fatal).
     - Launch: `code "<AF_REPO_DIR>" --reuse-window`
     - Optional: include `--profile "<NAME>"` if configured.
   - Else:
     - Launch URI fallback: `vscode://file/<AF_REPO_DIR>`

### 6.3 Workspace file management rules

- Only write `.vscode/extensions.json` if `manage_vscode_workspace_files: true`.
- Only write inside `AF_REPO_DIR` (allowed root) and only the minimal JSON content for extension recommendations.

---

## 7. CLI integration strategy (MetaF + Git)

### 7.1 MetaF invocation

- Invoke MetaF from cache via `ProcessStartInfo`.
- Capture:
  - exit code
  - stdout/stderr
  - start/end timestamps
- Enforce:
  - timeout (configurable)
  - cancellation (kill process tree)

### 7.2 Git invocation

- Use `git` CLI:
  - `git add <file1> <file2> ...` (only plan outputs)
  - `git commit -m "<message>"` (only if staged changes exist)
  - `git push` only if policy allows
- Validate repo sanity:
  - must be a git repo
  - optional `require_clean_repo` setting (default: false)

### 7.3 Distribution (publishing)

- Short-term default: **self-contained folder publish** (most reliable).
- Long-term goal: **self-contained + single-file publish** (tracked milestone item; target: M5).

Notes:

- Self-contained ensures the app runs without a preinstalled .NET Desktop Runtime.
- Single-file is packaging polish and requires extra validation (WPF + native deps).

---

## 8. Roadmap & milestones

**Current milestone:** **M1 — MetaF bootstrap (vertical slice)**  
**Last completed:** **M0 — Skeleton** ✅ (2025-12-15)

### M0 — Skeleton ✅ Complete (2025-12-15)

**Deliverable (met):** app launches, stores config, validates paths, boundary scaffolding exists, tests green.

### Completed checklist

- [x] Create solution/projects (`LIGHTNING.*`)
- [x] WPF app boots and shows Setup UI
- [x] Config load/save (YAML) working
- [x] Directory pickers + validation errors surfaced
- [x] BoundaryFileSystem scaffolding (allowlist + reparse-point checks)
- [x] Boundary gating in app startup (config-only allowlist, then runtime allowlist after valid config)
- [x] xUnit test harness in place
- [x] Core validation + boundary tests passing (`dotnet test` green)

#### Deferred from M0 (intentional)

- [ ] Navigation framework (Dashboard/Preview/Progress views) beyond the Setup screen
- [ ] Atomic/transactional writes + rollback rules (target: M5)
- [ ] Deep reparse-point coverage (full enumeration/copy policies) beyond baseline checks
- [ ] Plan Preview + per-run logs (next: M2, optional pre-work in M1/M2)

---

### M1 — MetaF bootstrap (vertical slice) ⏳ In progress

**Objective:** MetaF is reproducibly available locally (pinned SHA + resolved executable path) and visible/controllable in UI.

### Design decisions (M1 scope)

- MetaF is installed into an **app-owned cache** under an allowed root: `APP_WORK_DIR\cache\metaf\...`.
- Installation is **pinned**:
  - record requested `metaf_ref`
  - record resolved `metaf_sha` (true pin)
  - record resolved `metaf_exe_path`
- Bootstrap is **idempotent**:
  - if already installed and SHA matches → no-op
  - explicit “Rebuild/Update” action forces refresh
  - internal API supports a `forceRebuild` flag (debug wiring can come later)
- Bootstrap logs retained: keep last **100** attempts.

### Concrete checklist

1) **Config schema wiring (minimal fields for M1)**
   - [ ] Ensure `config.yml` includes: `metaf_git_url`, `metaf_ref`, `metaf_sha`, `metaf_exe_path`, `metaf_install_mode`
   - [ ] Default values: `metaf_git_url=<PLACEHOLDER>`, `metaf_ref=main`, `metaf_sha=""`, `metaf_exe_path=""`, `metaf_install_mode=repo_release_zip`
   - [ ] Validation rules:
     - [ ] URL non-empty and well-formed enough for git
     - [ ] Ref non-empty
     - [ ] If set, `metaf_exe_path` must be a descendant of `APP_WORK_DIR`

2) **Cache layout + invariants**
   - [ ] Define cache roots (under `APP_WORK_DIR`):
     - [ ] `APP_WORK_DIR\cache\metaf\repo\` (git clone)
     - [ ] `APP_WORK_DIR\cache\metaf\install\<sha>\` (installed output per resolved SHA)
   - [ ] Define a single “active” pointer:
     - [ ] store `metaf_sha` and `metaf_exe_path` in config
     - [ ] verify `metaf_exe_path` exists before marking “Ready”

3) **Git clone/update implementation**
   - [ ] Implement `MetaFInstaller` (Core interface + Adapters implementation)
   - [ ] Use `git` CLI via adapter:
     - [ ] clone if missing
     - [ ] fetch
     - [ ] checkout ref
     - [ ] resolve commit SHA (`git rev-parse HEAD`)
   - [ ] Enforce boundary:
     - [ ] clone/install paths are descendants of `APP_WORK_DIR`
     - [ ] no writes outside allowlist roots

4) **Install strategy (pluggable)**
   - [ ] Support install modes:
     - [ ] `repo_release_zip`: select `releases\*.zip` deterministically, extract into `install\<sha>\`, resolve exe
     - [ ] `dotnet_publish`: run `dotnet publish` into `install\<sha>\`, resolve exe
   - [ ] Record mode used and selected artifact in install log

5) **Executable resolution**
   - [ ] Determine executable path deterministically under `install\<sha>\`
   - [ ] Store absolute `metaf_exe_path` in config
   - [ ] Verify it is a descendant of `APP_WORK_DIR`

6) **Prereqs + runtime compatibility**
   - [ ] Implement `SystemProbe` (read-only):
     - [ ] detect `git` presence/version
     - [ ] detect `dotnet` presence/version(s)
     - [ ] optionally detect `code` presence/version
   - [ ] After MetaF install, detect required runtime:
     - [ ] if `*.runtimeconfig.json` exists, parse framework/version and surface a compatibility hint
     - [ ] otherwise treat as self-contained/unknown and rely on run + captured error output

7) **UI: MetaF status panel**
   - [ ] Add a MetaF status panel (Setup or Dashboard):
     - [ ] “Missing prereq / Not installed / Installing / Ready / Failed”
     - [ ] show: `metaf_ref`, resolved `metaf_sha`, `metaf_exe_path`, install mode
   - [ ] Add actions:
     - [ ] “Install/Update”
     - [ ] “Rebuild” (force rebuild/update)

8) **Logging + retention**
   - [ ] Write install logs under `APP_WORK_DIR\logs\metaf\YYYYMMDD-HHMMSS\...`
   - [ ] Retain last 100 install attempts (delete oldest)

9) **Tests (M1)**
   - [ ] Unit test: cache path composition is deterministic
   - [ ] Unit test: boundary rejects cache paths outside allowlist
   - [ ] Adapter test: zip selection deterministic + exe resolved (fake FS/process)

### M1 exit criteria

- MetaF can be installed on a fresh machine with system `git` + `.NET` present.
- App records:
  - requested ref, resolved SHA, resolved executable path
- UI reports Ready/Failed/Missing-prereq reliably and provides update/rebuild actions.

---

### M2 — `.met → .af` end-to-end

- Implement planner + preview grid
- Implement pipeline runner (copy to archive, run MetaF, write `.af` to repo)
- Add per-run logs + results summary
- Implement Git staging + commit of `.af` + `mapping.yml` only

Deliverable: primary workflow works and commits `.af`.

### M3 — Round-trip `.af → .met`

- Implement `.af → .met` plan + execution
- Archive output `.met` locally with traceability
- Update mapping metadata accordingly

Deliverable: round-trip conversions supported without committing `.met`.

### M4 — VS Code launcher

- Implement `VSCodeLauncher`:
  - detect `code`
  - best-effort extension install
  - open repo
- Implement `.vscode/extensions.json` management (feature-flagged)

Deliverable: one-click “Open Workspace” with extension recommendations/install attempts.

### M5 — Hardening

- Cancellation and safe rollback rules:
  - write outputs to temp then move
  - commit only after all conversions succeed (or chunked batches)
- Performance improvements for large sets (optional bounded concurrency)
- UX polish: progress, filters, conflict resolution UI
- Integration tests (temp repos + fake MetaF runner)
- Packaging: enable and validate **self-contained + single-file publish** for `LIGHTNING.App` (win-x64; optional win-arm64)

Deliverable: stable, safe, pleasant.

### M6 — Managed Dependencies (install prerequisites for the user)

**Objective:** LIGHTNING can optionally install and use managed copies of external tools (git/.NET) under `APP_WORK_DIR`, without bundling them in the LIGHTNING repo.

- Prefer system `git` / `.NET` if available.
- Otherwise install managed copies into `APP_WORK_DIR\deps\...` (opt-in via config).

Deliverable: users can run M1/M2 flows on machines missing prerequisites, using managed tool paths.

---

## 9. Risks & mitigations

- **R1: VS Code extension “active” control is limited.**
  - Mitigation: `.vscode/extensions.json` + best-effort CLI install.
- **R2: MetaF build process changes.**
  - Mitigation: isolate bootstrap behind an interface; pin MetaF SHA; support multiple install modes.
- **R3: Path escape via symlinks/junctions.**
  - Mitigation: block reparse points; canonical descendant checks; centralize IO.
- **R4: Git repo state surprises (dirty working tree).**
  - Mitigation: strict staging list + UI warnings + optional “require clean repo”.
- **R5: Partial conversions create inconsistent mapping.**
  - Mitigation: plan-first execution; transactional mapping update (temp write then replace).
- **R6: Managed dependency install increases security surface area.**
  - Mitigation: opt-in; integrity verification; pinned versions; explicit install roots; explicit binary paths (no PATH mutation).

---

## 10. Definition of Done (MVP)

- User configures directories and MetaF source/ref.
- App installs/builds MetaF into its own cache.
- Once configured, app produces `.af` for each `.met` and commits them to repo with updated `mapping.yml`.
- Upon user direction, app produces `.met` for selected or batch `.af` files from repo to `MET_SOURCE_DIR`.
- When producing `.met` files to `MET_SOURCE_DIR`, app moves the file being replaced to `APP_WORK_DIR\.archive\` before writing.
- No IO occurs outside allowlisted roots; reparse points blocked.
- VS Code button opens the repo; required extensions are recommended and install attempted.

---

## Appendix A — Terminology

- **Archive:** local-only store of `.met` keyed by hash; not committed.
- **Mapping:** committed YAML describing stable `.met` identity and corresponding `.af` path in repo.
- **Plan:** immutable list of intended operations produced by planner before execution.

