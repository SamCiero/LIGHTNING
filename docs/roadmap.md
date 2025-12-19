# Roadmap

`spec.md` is the single source of truth.

## Milestones (0–8)

0. Skeleton (foundation + error code scaffold)  
1. MetaF bootstrap + prerequisites + pinning  
2. Planner + run safety contracts (execution envelope)  
3. `.met → .af`: Mapping + Relink (publish to working tree, no git commit)  
4. `.met → .af`: Git + Commit + Push (policy-driven)  
5. `.af → .met`: Round-trip + safe overwrite + archiving  
6. Maintenance + drift recovery + retention enforcement  
7. VS Code workspace integration (explicitly gated)  
8. Ship-quality packaging + integration tests + performance  

---

## 0 — Skeleton (foundation + error code scaffold)

**Goals:**
- Minimal WPF/MVVM app boot + Setup UI. (Spec: §4.3)
- `config.yaml` load/save + validation with lenient parsing. (Spec: §5.1)
- Boundary primitives (allowlist + canonicalization + final-path + reparse policy) in place. (Spec: §3.1–§3.3, §4.2)
- Stable error code scaffold used for all surfaced errors. (Spec: §4.4)
- Test harness exists. (Spec: §4.1)

### Checklist
- [x] Create solution/projects (`LIGHTNING.*`). (Spec: §4.1)
- [x] WPF app boots and shows Setup UI. (Spec: §4.3)
- [x] Config load/save (YAML) working. (Spec: §5.1)
- [x] Directory pickers + validation errors surfaced. (Spec: §4.3, §5.1)
- [x] BoundaryFileSystem scaffolding exists. (Spec: §4.2)
- [x] xUnit test harness in place. (Spec: §4.1)

- [ ] `config.yaml` parsing ignores unknown keys. (Spec: §5.1)
- [ ] `config.yaml` parsing emits a warning for unknown keys. (Spec: §5.1)
- [ ] `config.yaml` required key `VERSION` is enforced. (Spec: §5.1)
- [ ] `config.yaml` required key `MET_SOURCE_DIR` is enforced. (Spec: §5.1)
- [ ] `config.yaml` required key `AF_REPO_DIR` is enforced. (Spec: §5.1)
- [ ] `config.yaml` required key `APP_WORK_DIR` is enforced. (Spec: §5.1)

- [ ] `MET_SOURCE_DIR` validation rejects non-absolute paths. (Spec: §5.1)
- [ ] `AF_REPO_DIR` validation rejects non-absolute paths. (Spec: §5.1)
- [ ] `APP_WORK_DIR` validation rejects non-absolute paths. (Spec: §5.1)
- [ ] `MET_SOURCE_DIR` validation rejects UNC paths. (Spec: §3.2)
- [ ] `AF_REPO_DIR` validation rejects UNC paths. (Spec: §3.2)
- [ ] `APP_WORK_DIR` validation rejects UNC paths. (Spec: §3.2)

- [ ] Canonicalization (`Path.GetFullPath` or equivalent) is performed before each user-content operation. (Spec: §3.2)
- [ ] Descendant check is inclusive of the allowlisted root itself. (Spec: §3.2)
- [ ] Allowlist is enforced on the canonical input path for each user-content operation. (Spec: §3.2)
- [ ] Allowlist is enforced on the final resolved path for each user-content operation. (Spec: §3.2)

- [ ] `REPARSE_POINT_POLICY` supports `block_writes_allow_reads`. (Spec: §3.3, §5.1)
- [ ] `REPARSE_POINT_POLICY` supports `block_all`. (Spec: §3.3, §5.1)

- [ ] Enumeration under allowlisted roots does not traverse reparse points. (Spec: §3.3)
- [ ] Enumeration treats reparse points as leaf entries. (Spec: §3.3)

- [ ] Under `block_writes_allow_reads`, a user-content write is rejected if any reparse point is encountered along the path. (Spec: §3.3)
- [ ] Under `block_writes_allow_reads`, a user-content read is rejected if the final resolved target escapes the allowed root. (Spec: §3.3)
- [ ] Under `block_writes_allow_reads`, any user-content read that crosses a reparse point is logged. (Spec: §3.3)

- [ ] Under `block_all`, a user-content read is rejected if any reparse point is encountered along the path. (Spec: §3.3)
- [ ] Under `block_all`, a user-content write is rejected if any reparse point is encountered along the path. (Spec: §3.3)

- [ ] Error code format `LIGHTNING_<CATEGORY>_<NNNN>` is implemented. (Spec: §4.4)
- [ ] Error code categories are restricted to `PLAN|FS|MAP|METAF|GIT|VSCODE`. (Spec: §4.4)
- [ ] Every surfaced error includes an error code. (Spec: §4.4)
- [ ] Every surfaced error includes a short human-readable message. (Spec: §4.4)
- [ ] Error code catalog is embedded in the app build. (Spec: §4.4)

---

## 1 — MetaF bootstrap + prerequisites + pinning

**Goals:**
- Deterministic, idempotent MetaF install under `APP_WORK_DIR`. (Spec: §4.5)
- Pin tool identity via `METAF_BUNDLE_SHA` (canonical manifest v1). (Spec: §4.5)
- Enforce `.NET 9` prereq rules (system-first; managed deps optional). (Spec: §4.5)
- Bootstrap logging and visibility in UI. (Spec: §2.1 FR-06, §4.3)

### Checklist
- [ ] `METAF_INSTALL_MODE` accepts only `repo_release_zip` and `dotnet_publish`. (Spec: §4.5)
- [ ] `METAF_INSTALL_MODE` rejects unknown values with a stable error code. (Spec: §4.5, §4.4)

- [ ] In `repo_release_zip`, `METAF_REF` is required. (Spec: §4.5)
- [ ] In `repo_release_zip`, `METAF_REF` must be a GitHub release tag. (Spec: §4.5)
- [ ] In `repo_release_zip`, the release must contain exactly one `.zip` asset. (Spec: §4.5)
- [ ] In `repo_release_zip`, asset selection fails fast when the `.zip` asset count is not exactly one. (Spec: §4.5)

- [ ] In `dotnet_publish`, `METAF_REF` is required. (Spec: §4.5)
- [ ] In `dotnet_publish`, `METAF_REF` must be a full 40-hex commit SHA. (Spec: §4.5)
- [ ] In `dotnet_publish`, the installer clones/fetches `METAF_GIT_URL`. (Spec: §4.5)
- [ ] In `dotnet_publish`, the installer checks out the exact commit in `METAF_REF`. (Spec: §4.5)
- [ ] In `dotnet_publish`, the installer runs `dotnet publish -c Release -r win-x64 --self-contained false`. (Spec: §4.5)

- [ ] MetaF is installed under `APP_WORK_DIR`. (Spec: §4.5)
- [ ] `METAF_ROOT_DIR` is persisted to `config.yaml`. (Spec: §4.5, §5.1)
- [ ] `METAF_EXE_PATH` is persisted to `config.yaml`. (Spec: §4.5, §5.1)

- [ ] `METAF_BUNDLE_SHA` is computed from the canonical manifest v1. (Spec: §4.5)
- [ ] Canonical manifest v1 enumerates files recursively under `METAF_ROOT_DIR` (no directories). (Spec: §4.5)
- [ ] Canonical manifest v1 normalizes `REL` with `/` separators. (Spec: §4.5)
- [ ] Canonical manifest v1 includes `SIZE` in bytes per entry. (Spec: §4.5)
- [ ] Canonical manifest v1 includes `SHA` as `sha256:<hex>` per entry. (Spec: §4.5)
- [ ] Canonical manifest v1 sorts entries by `REL` case-insensitive ascending. (Spec: §4.5)
- [ ] Canonical manifest v1 serializes as UTF-8 lines `REL\tSIZE\tSHA\n`. (Spec: §4.5)
- [ ] `METAF_BUNDLE_SHA = sha256("v1\n" + <all lines>)` is implemented. (Spec: §4.5)
- [ ] `METAF_BUNDLE_SHA` is persisted to `config.yaml`. (Spec: §4.5, §5.1)

- [ ] Bootstrap failure triggers one clean-cache retry. (Spec: §4.5)
- [ ] Clean-cache retry deletes the MetaF cache/install directory under `APP_WORK_DIR`. (Spec: §4.5)
- [ ] Clean-cache retry occurs at most once per bootstrap attempt. (Spec: §4.5)

- [ ] If `ALLOW_MANAGED_DEPENDENCIES: false`, missing compatible system `.NET 9` is a hard failure. (Spec: §4.5)
- [ ] If `ALLOW_MANAGED_DEPENDENCIES: true`, a private `.NET` runtime is provisioned under `APP_WORK_DIR`. (Spec: §4.5)
- [ ] MetaF is invoked using an explicit runtime path. (Spec: §4.5)
- [ ] PATH is not mutated to run MetaF. (Spec: §4.5)
- [ ] If `MANAGED_DOTNET_ROOT` is set, it must be within `APP_WORK_DIR`. (Spec: §5.1)

- [ ] Compatibility check runs `dotnet --list-runtimes` using the dotnet that will run MetaF. (Spec: §4.5)
- [ ] Compatibility check runs a probe MetaF invocation (e.g., `--version`). (Spec: §4.5)

- [ ] Bootstrap logs are written under `APP_WORK_DIR`. (Spec: §2.1 FR-06, §5.1)
- [ ] Bootstrap logs for a new attempt do not overwrite prior attempt logs. (Spec: §2.1 FR-06)

---

## 2 — Planner + run safety contracts (execution envelope)

**Goals:**
- Deterministic planning and preview before execution. (Spec: §2.1 FR-02, §6.0)
- Cancellation/no-side-effects contract. (Spec: §11)
- Staging + atomic publish all-or-nothing. (Spec: §11.1)
- Run exclusivity and plan snapshot integrity. (Spec: §11.2, §11.3)
- Per-run logs and error code export. (Spec: §2.2 NFR-03, §4.4)

### Checklist
- [ ] Planner treats `MET_SOURCE_DIR` as flat (non-recursive). (Spec: §6.0)
- [ ] Planner includes only files with `.met` extension (case-insensitive). (Spec: §6.0)
- [ ] Planner sorts candidate `.met` files by canonical full path case-insensitive ascending. (Spec: §6.0)
- [ ] Planner computes `MET_ID` as `sha256(met_file_bytes)` for identity comparisons. (Spec: §5.2, §6.0)
- [ ] When duplicate content exists (same `MET_ID`), planner prefers the lexicographically smallest canonical path. (Spec: §6.0)
- [ ] When duplicate content exists (same `MET_ID`), planner logs warnings for ignored files. (Spec: §6.0)

- [ ] Plan preview exists in UI before execution. (Spec: §2.1 FR-02, §4.3)
- [ ] Execution cannot start without a plan preview step completing. (Spec: §2.1 FR-02)

- [ ] Per-run logs include the plan. (Spec: §2.2 NFR-03)
- [ ] Per-run logs include MetaF stdout/stderr. (Spec: §2.2 NFR-03)
- [ ] Per-run logs include git output when git is invoked. (Spec: §2.2 NFR-03)
- [ ] Per-run logs include results summary. (Spec: §2.2 NFR-03)

- [ ] Error code catalog is exported to `APP_WORK_DIR\docs\error-codes.md`. (Spec: §4.4)
- [ ] Error code export updates when app version changes. (Spec: §4.4)

- [ ] Canceling during plan generation produces no writes to `AF_REPO_DIR`. (Spec: §11)
- [ ] Canceling during plan generation produces no writes to the git index. (Spec: §11)
- [ ] Canceling during plan generation produces no writes to `MET_SOURCE_DIR`. (Spec: §11)

- [ ] Canceling during relink scanning produces no writes to `AF_REPO_DIR`. (Spec: §11)
- [ ] Canceling during relink scanning produces no writes to the git index. (Spec: §11)
- [ ] Canceling during relink scanning produces no writes to `MET_SOURCE_DIR`. (Spec: §11)

- [ ] Canceling during execution produces no writes to `AF_REPO_DIR`. (Spec: §11)
- [ ] Canceling during execution produces no writes to the git index. (Spec: §11)
- [ ] Canceling during execution produces no writes to `MET_SOURCE_DIR`. (Spec: §11)

- [ ] Run-scoped staging directory is `STAGING_DIR = <APP_WORK_DIR>\.staging\<RUN_ID>\`. (Spec: §11.1)
- [ ] Any write destined for `AF_REPO_DIR` is routed to `STAGING_DIR`. (Spec: §11.1)
- [ ] Any write destined for `MET_SOURCE_DIR` is routed to `STAGING_DIR`. (Spec: §11.1)
- [ ] Staging writes use `*.tmp` temp files. (Spec: §11.1)
- [ ] Staging writes flush and close before rename. (Spec: §11.1)
- [ ] Staging writes rename atomically to the final staged path. (Spec: §11.1)

- [ ] Publish does not begin if the plan was canceled. (Spec: §11.1)
- [ ] Publish does not begin if any planned conversion failed. (Spec: §11.1)
- [ ] Cancellation is honored up to the start of publish. (Spec: §11.1)
- [ ] Cancellation is deferred once publish begins. (Spec: §11.1)

- [ ] On cancel, `STAGING_DIR` is deleted best-effort. (Spec: §11)
- [ ] On failure, `STAGING_DIR` is deleted best-effort. (Spec: §11)
- [ ] Archives under `APP_WORK_DIR` may remain after cancel. (Spec: §11)
- [ ] Logs under `APP_WORK_DIR` may remain after cancel. (Spec: §11)

- [ ] A named mutex key is derived from canonical (`AF_REPO_DIR`, `MET_SOURCE_DIR`). (Spec: §11.2)
- [ ] The run fails fast when the mutex cannot be acquired. (Spec: §11.2)

- [ ] `mapping.yaml` is hashed at plan time. (Spec: §11.3)
- [ ] `mapping.yaml` is re-hashed at execute start. (Spec: §11.3)
- [ ] Execution aborts if the hash differs between plan and execute. (Spec: §11.3)
- [ ] A hash mismatch requires re-plan to proceed. (Spec: §11.3)

---

## 3 — `.met → .af`: Mapping + Relink (publish to working tree, no git commit)

**Goals:**
- `mapping.yaml` strict schema + invariants. (Spec: §5.2)
- Deterministic `.af` allocation + sanitization. (Spec: §6.1–§6.3)
- `.met` rename/replacement semantics. (Spec: §8.1, §8.2)
- Relink flow for moved/renamed `.af`. (Spec: §7)
- `.met → .af` staging layout. (Spec: §11.1)
- Archive ingest snapshots. (Spec: §9.2, §9.1)

### Checklist
- [ ] `mapping.yaml` is located at `<AF_REPO_DIR>\mapping.yaml`. (Spec: §5.2)
- [ ] `mapping.yaml` parsing rejects unknown keys. (Spec: §5.2)
- [ ] `mapping.yaml` enforces required key `VERSION`. (Spec: §5.2)
- [ ] `mapping.yaml` enforces required key `CREATED_UTC`. (Spec: §5.2)
- [ ] `mapping.yaml` enforces required key `MET_SOURCE_ROOT_FINGERPRINT`. (Spec: §5.2)
- [ ] `mapping.yaml` enforces required key `ITEMS`. (Spec: §5.2)

- [ ] Each mapping item enforces required key `MET_ID`. (Spec: §5.2)
- [ ] Each mapping item enforces required key `MET_REL`. (Spec: §5.2)
- [ ] Each mapping item enforces required key `AF_REL`. (Spec: §5.2)
- [ ] Each mapping item enforces required key `LAST_METAF_BUNDLE_SHA`. (Spec: §5.2)
- [ ] Each mapping item enforces required key `LAST_RUN_UTC`. (Spec: §5.2)

- [ ] If `AF_SHA256` is present, it must match `sha256:<hex>`. (Spec: §5.2)
- [ ] If `AF_SIZE_BYTES` is present, it must be an integer byte count. (Spec: §5.2)

- [ ] `MET_REL` is enforced as filename-only (no directory separators). (Spec: §5.2)
- [ ] `MET_ID` uniqueness is enforced case-insensitive on Windows. (Spec: §5.2)
- [ ] `MET_REL` uniqueness is enforced case-insensitive on Windows. (Spec: §5.2)
- [ ] `AF_REL` uniqueness is enforced case-insensitive on Windows. (Spec: §5.2)
- [ ] Mapping invariant violations fail with a stable error code. (Spec: §5.2, §4.4)

- [ ] New `.met` without a matching mapping entry defaults `AF_REL` to `<sanitized_met_basename>.af` at repo root. (Spec: §6.1)
- [ ] `CONFLICT_POLICY: suffix` uses `Name.af`, then `Name-1.af`, then `Name-2.af`. (Spec: §6.1, §5.1)

- [ ] Sanitization normalizes to Unicode NFC. (Spec: §6.2)
- [ ] Sanitization replaces invalid Windows filename characters with `_`. (Spec: §6.2)
- [ ] Sanitization trims trailing spaces. (Spec: §6.2)
- [ ] Sanitization trims trailing periods. (Spec: §6.2)
- [ ] Sanitization avoids reserved device names by appending `_`. (Spec: §6.2)
- [ ] Sanitization truncates basenames longer than 180 chars pre-extension. (Spec: §6.2)
- [ ] Sanitization appends `~<HASH12>` when truncation occurs. (Spec: §6.2)

- [ ] Uniqueness/collision checks are case-insensitive (ordinal ignore-case). (Spec: §6.3)
- [ ] The first-seen casing of a chosen path is preserved. (Spec: §6.3)

- [ ] Long-path I/O is attempted when paths exceed legacy limits. (Spec: §6.4)
- [ ] When long-path I/O is unsupported and limits are exceeded, the affected item fails with a stable error code. (Spec: §6.4)

- [ ] `.met` rename is detected when `MET_ID` matches but filename differs. (Spec: §8.1)
- [ ] On `.met` rename, `MET_REL` update is staged but not persisted until successful run completion. (Spec: §8.1, §11.1)

- [ ] `.met` replacement is detected when `MET_REL` matches but `MET_ID` differs. (Spec: §8.2)
- [ ] `.met` replacement prompts the user for Update vs Skip. (Spec: §8.2)
- [ ] Replacement Update keeps `AF_REL` unchanged. (Spec: §8.2)
- [ ] Replacement Update overwrites the `.af` output via the staging/publish contract. (Spec: §8.2, §11.1)
- [ ] Replacement Skip produces no mapping changes. (Spec: §8.2)
- [ ] Replacement Skip produces no outputs for that entry. (Spec: §8.2)

- [ ] Relink scan is triggered when mapped `AF_REL` does not exist. (Spec: §7)
- [ ] Relink scan searches for `.af` files under `AF_REPO_DIR`. (Spec: §7)
- [ ] Relink enumeration does not traverse reparse points. (Spec: §3.3, §7)
- [ ] Relink candidates are sorted by `AF_REL` case-insensitive ascending. (Spec: §7)

- [ ] Relink first filters candidates by matching basename case-insensitive. (Spec: §7)
- [ ] If `AF_SIZE_BYTES` is present, relink uses it as a prefilter before hashing candidates. (Spec: §7, §5.2)
- [ ] If `AF_SHA256` is present, relink uses it for hash match selection. (Spec: §7, §5.2)

- [ ] When relink has no match, user is prompted for Locate vs Skip. (Spec: §7)
- [ ] When relink is ambiguous, user is prompted for Locate vs Skip. (Spec: §7)
- [ ] Locate selection is restricted to paths under `AF_REPO_DIR`. (Spec: §7)
- [ ] Locate rejects any selection that includes a reparse-point path segment. (Spec: §7, §3.3)
- [ ] Skip causes no mapping changes for that entry. (Spec: §7)
- [ ] Skip causes no conversion for that entry. (Spec: §7)

- [ ] Relink-discovered mapping changes are staged in memory. (Spec: §7)
- [ ] Relink-discovered mapping changes are persisted only after successful run completion. (Spec: §7, §11.1)

- [ ] `.met → .af` staging writes `.af` outputs under `<STAGING_DIR>af\<AF_REL>`. (Spec: §11.1)
- [ ] `.met → .af` staging writes the next `mapping.yaml` under `<STAGING_DIR>mapping.yaml`. (Spec: §11.1)
- [ ] `.met → .af` invokes MetaF using pinned `METAF_EXE_PATH`. (Spec: §4.5)
- [ ] `.met → .af` records `LAST_METAF_BUNDLE_SHA` per item. (Spec: §4.5, §5.2)
- [ ] `.met → .af` records `LAST_RUN_UTC` per item. (Spec: §5.2)

- [ ] Archive root is `APP_WORK_DIR\.archive\met\<YYYY>\<MM>\<DD>\`. (Spec: §9)
- [ ] Archive filename format is `<utc_timestamp>_<kind>_<name>_<sha256prefix>.met`. (Spec: §9.1)
- [ ] Archive `utc_timestamp` format is `YYYYMMDDTHHMMSSfffZ`. (Spec: §9.1)
- [ ] Archive collision handling appends `-1`, `-2`, ... to the timestamp within the same day folder. (Spec: §9.1)
- [ ] `.met → .af` archives ingested `.met` bytes with kind `ingest`. (Spec: §9.2)

---

## 4 — `.met → .af`: Git + Commit + Push (policy-driven)

**Goals:**
- Deterministic staging and commit message rules. (Spec: §10.1, §10.2)
- Author identity persistence. (Spec: §10.3, §5.1)
- Dirty repo behavior enforcement. (Spec: §10.4, §5.1)
- Policy-driven push (default `always`). (Spec: §3.4, §10.5, §5.1)

### Checklist
- [ ] `git add *` is never invoked. (Spec: §3.4, §10.1)
- [ ] Only `mapping.yaml` is staged when it is part of the plan outputs. (Spec: §10.1)
- [ ] Only planned `.af` files are staged. (Spec: §10.1)
- [ ] Staged relative paths are sorted case-insensitive ascending. (Spec: §10.1)
- [ ] Staging does not occur until after all conversions succeed. (Spec: §10.1, §11.1)

- [ ] Commit subject matches `LIGHTNING: <direction> <count> files (MetaF <METAF_BUNDLE_SHA_SHORT>)`. (Spec: §10.2)
- [ ] `METAF_BUNDLE_SHA_SHORT` is the first 12 hex chars after `sha256:`. (Spec: §10.2)
- [ ] Commit body includes `Direction: <direction>`. (Spec: §10.2)
- [ ] Commit body includes `Files: <count>`. (Spec: §10.2)
- [ ] Commit body includes `MetaF: <METAF_BUNDLE_SHA>`. (Spec: §10.2)
- [ ] Commit body includes `Staged:`. (Spec: §10.2)
- [ ] Commit body lists staged files one per line. (Spec: §10.2)
- [ ] Commit body staged list is ordered as staging order. (Spec: §10.2)
- [ ] Optional user details are appended only after a blank line under `Notes:`. (Spec: §10.2)

- [ ] If repo profile for current `AF_REPO_DIR` is missing `GIT_AUTHOR_NAME`, the user is prompted. (Spec: §10.3)
- [ ] If repo profile for current `AF_REPO_DIR` is missing `GIT_AUTHOR_EMAIL`, the user is prompted. (Spec: §10.3)
- [ ] `GIT_AUTHOR_NAME` is persisted under `REPO_PROFILES[]` for the canonical `AF_REPO_DIR`. (Spec: §10.3, §5.1)
- [ ] `GIT_AUTHOR_EMAIL` is persisted under `REPO_PROFILES[]` for the canonical `AF_REPO_DIR`. (Spec: §10.3, §5.1)
- [ ] If author identity is unexpectedly unavailable, system git config is used. (Spec: §10.3)

- [ ] Repo “clean” check includes “no modified tracked files”. (Spec: §10.4)
- [ ] Repo “clean” check includes “no staged changes”. (Spec: §10.4)
- [ ] Repo “clean” check includes “no untracked files”. (Spec: §10.4)

- [ ] If `REQUIRE_CLEAN_REPO: true` and repo is not clean, the run aborts before staging. (Spec: §10.4)
- [ ] If `REQUIRE_CLEAN_REPO: true` and repo is not clean, the run produces no commit. (Spec: §10.4)
- [ ] If `REQUIRE_CLEAN_REPO: false` and repo is not clean, the run may proceed. (Spec: §10.4)
- [ ] If `REQUIRE_CLEAN_REPO: false` and repo is not clean, a prominent warning is emitted in UI. (Spec: §10.4)
- [ ] If `REQUIRE_CLEAN_REPO: false` and repo is not clean, a prominent warning is emitted in logs. (Spec: §10.4)

- [ ] `GIT_PUSH_POLICY` supports `always`. (Spec: §10.5, §5.1)
- [ ] `GIT_PUSH_POLICY` supports `never`. (Spec: §10.5, §5.1)
- [ ] `GIT_PUSH_POLICY` defaults to `always` when omitted. (Spec: §3.4, §5.1)

- [ ] Push is attempted only if a commit was produced. (Spec: §10.5)
- [ ] Push is attempted only after publishing completes. (Spec: §10.5, §11.1)

- [ ] If `GIT_PUSH_POLICY: never`, `git push` is not invoked. (Spec: §10.5)
- [ ] If `GIT_PUSH_POLICY: always`, exactly one `git push` attempt is made. (Spec: §10.5)
- [ ] Push does not push tags. (Spec: §10.5)
- [ ] Push stdout/stderr is captured in run logs. (Spec: §10.5)

- [ ] Current branch name is obtained via `git rev-parse --abbrev-ref HEAD`. (Spec: §10.5)
- [ ] If the current branch is `HEAD` (detached), push is skipped. (Spec: §10.5)
- [ ] If detached `HEAD`, a stable warning/error code is emitted in UI. (Spec: §10.5, §4.4)
- [ ] If detached `HEAD`, a stable warning/error code is emitted in logs. (Spec: §10.5, §4.4)

- [ ] Upstream is queried via `git rev-parse --symbolic-full-name @{u}`. (Spec: §10.5)
- [ ] If upstream resolves to `refs/remotes/<remote>/<branch>`, push remote is `<remote>`. (Spec: §10.5)
- [ ] If upstream resolves to `refs/remotes/<remote>/<branch>`, destination branch is `<branch>`. (Spec: §10.5)

- [ ] If upstream is absent, `origin` is required to exist to push. (Spec: §10.5)
- [ ] If `origin` is missing, push is skipped. (Spec: §10.5)
- [ ] If `origin` is missing, a stable warning/error code is emitted in UI. (Spec: §10.5, §4.4)
- [ ] If `origin` is missing, a stable warning/error code is emitted in logs. (Spec: §10.5, §4.4)
- [ ] If falling back to `origin`, destination branch equals the current branch name. (Spec: §10.5)

- [ ] Push command is `git push <remote> HEAD:<dest-branch>`. (Spec: §10.5)

- [ ] If push fails, the commit remains (no rollback attempt). (Spec: §10.5)
- [ ] If push fails, the run emits a prominent warning. (Spec: §10.5)
- [ ] If push fails, the run emits a stable error code. (Spec: §10.5, §4.4)

---

## 5 — `.af → .met`: Round-trip + safe overwrite + archiving

**Goals:**
- Flat `.af → .met` outputs into `MET_SOURCE_DIR` using `MET_REL`. (Spec: §8.3)
- Overwrite safety with before/after archiving. (Spec: §8.3, §9.2)
- `.af → .met` staging layout and publish contract. (Spec: §11.1)

### Checklist
- [ ] `.af → .met` destination path is `<MET_SOURCE_DIR>\<MET_REL>`. (Spec: §8.3)
- [ ] `MET_REL` must be filename-only (no directory separators). (Spec: §8.3)

- [ ] When `MET_REL` is missing or invalid, derive `MET_REL = <sanitized_af_basename>.met`. (Spec: §8.3)
- [ ] When derived `MET_REL` conflicts case-insensitive with another entry, the entry hard-fails (no output). (Spec: §8.3)
- [ ] When derived `MET_REL` conflicts, a stable error code is surfaced. (Spec: §8.3, §4.4)
- [ ] When derived `MET_REL` does not conflict, `MET_REL` update is staged. (Spec: §8.3)
- [ ] Staged `MET_REL` update is persisted only after successful run completion. (Spec: §8.3, §11.1)

- [ ] If destination exists, it is archived with kind `before_overwrite` before overwrite. (Spec: §8.3, §9.2)
- [ ] After conversion, produced `.met` bytes are archived with kind `produced`. (Spec: §8.3, §9.2)

- [ ] `.af → .met` stages outputs under `<STAGING_DIR>met\<MET_REL>`. (Spec: §11.1)

---

## 6 — Maintenance + drift recovery + retention enforcement

**Goals:**
- Block on `MET_SOURCE_ROOT_FINGERPRINT` mismatch. (Spec: §5.3)
- Repair Mapping action to realign `MET_REL` when the source root changes. (Spec: §5.3)
- Archive retention and cap enforcement. (Spec: §9.3)

### Checklist
- [ ] `MET_SOURCE_ROOT_FINGERPRINT` is computed and stored in `mapping.yaml`. (Spec: §5.3, §5.2)
- [ ] Fingerprint mismatch blocks conversion planning. (Spec: §5.3)
- [ ] Fingerprint mismatch surfaces a stable error code. (Spec: §5.3, §4.4)
- [ ] UI provides an explicit “Repair Mapping” action. (Spec: §5.3)

- [ ] Repair Mapping recomputes and updates `MET_SOURCE_ROOT_FINGERPRINT`. (Spec: §5.3)
- [ ] Repair Mapping rescans `MET_SOURCE_DIR` as flat/non-recursive. (Spec: §5.3, §6.0)
- [ ] Repair Mapping updates `MET_REL` when `MET_ID` matches discovered `.met` bytes. (Spec: §5.3)
- [ ] Repair Mapping enforces mapping invariants before write. (Spec: §5.3, §5.2)
- [ ] Repair Mapping writes `mapping.yaml` transactionally (temp then atomic replace). (Spec: §5.3)
- [ ] Canceling Repair Mapping produces no writes. (Spec: §5.3)

- [ ] Retention window always retains entries within `ARCHIVE_RETENTION_DAYS`. (Spec: §9.3, §5.1)
- [ ] Retention uses filename-derived UTC timestamps. (Spec: §9.3)
- [ ] Archive size accounting includes only `APP_WORK_DIR\.archive\met\...`. (Spec: §9.3)
- [ ] If over cap and older-than-retention entries exist, delete oldest older-than-retention entries until under cap. (Spec: §9.3)
- [ ] If over cap due solely to within-retention entries, no deletion occurs. (Spec: §9.3)
- [ ] If over cap due solely to within-retention entries, a prominent warning is emitted. (Spec: §9.3)

---

## 7 — VS Code workspace integration (explicitly gated)

**Goals:**
- Open Workspace launches VS Code pointed at `AF_REPO_DIR`. (Spec: §12)
- Optional `.vscode/extensions.json` management is explicit-action-only. (Spec: §12, §5.1)
- Workspace files are never written during conversion runs. (Spec: §12, §11)

### Checklist
- [ ] Open Workspace launches VS Code pointed at `AF_REPO_DIR`. (Spec: §12)
- [ ] `*.code-workspace` files are not created by LIGHTNING. (Spec: §12)

- [ ] Required extension IDs come from `VSCODE_REQUIRED_EXTENSIONS`. (Spec: §5.1, §12)
- [ ] Extension recommendations are shown to the user. (Spec: §12)
- [ ] Best-effort extension install is attempted when possible. (Spec: §12)
- [ ] When install is unavailable/fails, behavior falls back to recommendation-only. (Spec: §12)

- [ ] `.vscode/extensions.json` is written only when `MANAGE_VSCODE_WORKSPACE_FILES: true`. (Spec: §5.1, §12)
- [ ] `.vscode/extensions.json` is written only on explicit user action. (Spec: §12)
- [ ] Conversion runs do not write `.vscode/...`. (Spec: §12)
- [ ] Cancel during conversion does not write `.vscode/...`. (Spec: §11, §12)
- [ ] Git staging does not stage `.vscode/...`. (Spec: §12, §10.1)
- [ ] Git commit does not include `.vscode/...`. (Spec: §12)

---

## 8 — Ship-quality packaging + integration tests + performance

**Goals:**
- Self-contained Windows app shipping. (Spec: §2.2 NFR-06)
- Determinism guarantees verified. (Spec: §2.2 NFR-02)
- Cancellation and safe-failure handling verified. (Spec: §2.2 NFR-04/05)
- Automated test coverage for boundary, git, and pipeline contracts. (Spec: §3, §10, §11)

### Checklist
- [ ] App ships as a self-contained Windows app (no preinstalled .NET Desktop Runtime required). (Spec: §2.2 NFR-06)

- [ ] Determinism test passes for `.af` bytes under identical inputs and identical `METAF_BUNDLE_SHA`. (Spec: §2.2 NFR-02)
- [ ] Determinism test passes for `AF_REL` allocations under identical inputs and identical `METAF_BUNDLE_SHA`. (Spec: §2.2 NFR-02)
- [ ] Determinism test passes for `mapping.yaml` semantic content excluding bookkeeping timestamps. (Spec: §2.2 NFR-02)
- [ ] `mapping.yaml` YAML emitter is stable such that non-bookkeeping diffs are deterministic. (Spec: §2.2 NFR-02)

- [ ] Cancellation test passes for long planner runs. (Spec: §2.2 NFR-04, §11)
- [ ] Cancellation test passes for long relink scans. (Spec: §2.2 NFR-04, §11)
- [ ] Cancellation test passes for long execution runs. (Spec: §2.2 NFR-04, §11)

- [ ] Failure test proves no partial git commit occurs when a run fails mid-execution. (Spec: §2.2 NFR-05, §11)
- [ ] Failure test proves UI surfaces a stable error code on failure. (Spec: §2.2 NFR-05, §4.4)

- [ ] Automated tests cover allowlist enforcement on canonical input paths. (Spec: §3.2)
- [ ] Automated tests cover allowlist enforcement on final resolved paths. (Spec: §3.2)
- [ ] Automated tests cover `block_writes_allow_reads` write rejection on reparse points. (Spec: §3.3)
- [ ] Automated tests cover `block_writes_allow_reads` logging for reads that cross reparse points. (Spec: §3.3)
- [ ] Automated tests cover `block_all` read rejection on encountering reparse points. (Spec: §3.3)
- [ ] Automated tests cover `block_all` write rejection on encountering reparse points. (Spec: §3.3)

- [ ] Automated tests cover staging layout for `.met → .af`. (Spec: §11.1)
- [ ] Automated tests cover staging layout for `.af → .met`. (Spec: §11.1)
- [ ] Automated tests cover publish gating on “all conversions succeeded”. (Spec: §11.1)

- [ ] Automated tests cover git staging: explicit planned outputs only. (Spec: §10.1)
- [ ] Automated tests cover deterministic commit message templates. (Spec: §10.2)
- [ ] Automated tests cover push policy `never`. (Spec: §10.5)
- [ ] Automated tests cover push policy `always`. (Spec: §10.5)
- [ ] Automated tests cover push skip on detached HEAD. (Spec: §10.5)
- [ ] Automated tests cover push skip when `origin` missing and no upstream exists. (Spec: §10.5)
- [ ] Automated tests cover push failure semantics (commit remains; stable error surfaced). (Spec: §10.5)
