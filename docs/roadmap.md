# LIGHTNING — Roadmap

Spec: spec.final.md  
Status: PROPOSED  
Roadmap Version: v1.0.0  
Last Updated (UTC): 2025-12-21  
Owner: Sam Ciero

---

## 0. Purpose

This roadmap decomposes the project defined in `spec.final.md` into milestones/releases with:

- explicit scope boundaries (in/out),
- traceability to `REQ-####` requirements and `AT-####` acceptance tests,
- gating criteria and Definition of Done per milestone,
- risks, dependencies, and decision points.

---

## 1. Roadmap Operating Rules

### 1.1 Scope Control

- **Rule:** A roadmap item is “in scope” only if it maps to at least one `REQ-####` in the spec【824441473965294†L123-L137】.
- **Rule:** Any change that modifies behavior **MUST** be accompanied by:
  - a spec update,
  - updated decision tables (if impacted),
  - updated acceptance tests,
  - and a roadmap update noting the change (Section 8).

### 1.2 Milestone Types

- **M** = Milestone (user-facing slice; may include internal work)
- **R** = Release (ship boundary; can coincide with a milestone)
- **H** = Hardening (quality/security/perf/operability)
- **S** = Spike (time-boxed discovery; produces a decision and/or prototype)

### 1.3 Gating (Stop‑the‑Line)

A milestone cannot be marked complete unless:

- all listed `REQ-####` have planned `AT-####` and those tests pass (or are explicitly deferred with rationale),
- exit criteria for the milestone are satisfied,
- and all blockers are resolved or explicitly re‑scoped.

---

## 2. Milestone Summary (Top‑Level)

The roadmap decomposes the LIGHTNING project into **six** distinct milestones (M0–M5). This count balances granularity and delivery cadence: each milestone targets a coherent functional slice—foundation/configuration, bootstrapping MetaF, forward conversion, reverse conversion, mapping repair/relink, and final hardening/release—while keeping the total number manageable. Six milestones ensure incremental progress without over‑splitting the work or bundling unrelated tasks together.

| ID | Name | Type | Target Date (UTC) | Primary Outcome | Depends On |
| --- | --- | --- | ---: | --- | --- |
| M0 | Foundation & Configuration Setup | M | — | Establishes the baseline Windows application, configuration parsing, trust boundary enforcement and UI skeleton【824441473965294†L123-L137】【824441473965294†L748-L756】 | None |
| M1 | MetaF Bootstrap & Installer | M | — | Implements the MetaF bootstrap workflow: installs and pins MetaF, computes bundle hash, logs bootstrap attempts【824441473965294†L452-L496】 | M0 |
| M2 | Forward Conversion (.met→.af) | M | — | Delivers the deterministic Plan/Preview, `.met→.af` conversions, staging, archive and Git publish mechanics【824441473965294†L497-L566】 | M1 |
| M3 | Reverse Conversion (.af→.met) | M | — | Implements `.af→.met` workflow with strict mapping validation, safe overwrite archives and atomic publish【824441473965294†L567-L578】【824441473965294†L589-L634】 | M2 |
| M4 | Mapping Repair & Relink | M | — | Provides Repair Mapping and Relink workflows to handle fingerprint drift, retirement, and `.af` moves/renames【824441473965294†L591-L634】 | M2 |
| M5 | Observability, Hardening & Release | R | — | Finalizes determinism, logging, error codes, transactional writes, performance, compatibility and ensures all tests pass【824441473965294†L748-L856】【824441473965294†L821-L824】 | M3, M4 |

---

## 3. Release Plan (If Applicable)

### 3.1 Versioning

 - Version scheme: **SemVer** — semantic version numbers in the form `vX.Y.Z`, where `X` increments for backwards‑incompatible changes, `Y` for milestone‑sized feature releases, and `Z` for bug fixes.
- Release cadence: **On‑demand** — releases are cut when a milestone's deliverables and associated acceptance tests are complete.
- Supported upgrade paths: upgrades must preserve persisted data (`config.yaml`, `mapping.yaml`, archives) without corruption as required by `REQ-0901`【824441473965294†L860-L867】.

### 3.2 Release Criteria

A release is cut only when:

- all `AT-####` for included scope pass (Definition of Done)【824441473965294†L871-L889】,
- compatibility rules in spec §14 are satisfied【824441473965294†L860-L867】,
- observability/logging requirements in spec §12 are satisfied【824441473965294†L808-L823】,
- and rollback/upgrade notes exist in Section 7.

---

## 4. Work Breakdown Structure

### 4.1 Epics → Features → Tasks

- **Epic E‑01 Foundation and Config:** Implements platform baseline and trust boundary enforcement.
  - **Feature F‑01 UI & Config Skeleton:** Build WPF UI shell; implement configuration loading and validation.
    - **Task T‑01** Implement `ConfigService` to load/validate `config.yaml`【824441473965294†L175-L186】.
    - **Task T‑02** Build initial settings UI for configuring roots and MetaF install settings【824441473965294†L206-L214】.
  - **Feature F‑02 Boundary Enforcement:** Implement `BoundaryFileSystem` enforcing allowed roots, volume constraints and reparse point policy【824441473965294†L748-L756】.
- **Epic E‑02 MetaF Bootstrap:** Install and pin MetaF bundle.
  - **Feature F‑03 Bootstrap Installer:** Validate prerequisites, install MetaF, compute bundle hash and write bootstrap logs【824441473965294†L452-L496】.
- **Epic E‑03 Forward Conversion:** `.met→.af` conversion flow.
  - **Feature F‑04 Plan & Preview:** Build deterministic Plan and Preview enumerating all writes and side effects【824441473965294†L639-L661】.
  - **Feature F‑05 Conversion & Publish:** Implement scanning, archiving, MetaF invocation, staging, Git commit/push and conflict handling【824441473965294†L497-L566】.
- **Epic E‑04 Reverse Conversion:** `.af→.met` conversion flow.
  - **Feature F‑06 Reverse Conversion Logic:** Validate mapping, plan destination paths, archive overwritten `.met` files and publish safely【824441473965294†L567-L578】.
- **Epic E‑05 Mapping Repair and Relink:** Handle fingerprint drift and missing `.af` files.
  - **Feature F‑07 Repair Mapping:** Update fingerprint, activate/retire items and write mapping transactionally【824441473965294†L591-L613】.
  - **Feature F‑08 Relink:** Detect unmapped `.af` files, match by size/hash, and support Locate/Skip selection【824441473965294†L615-L633】.
- **Epic E‑06 Hardening and Release:** Observability, determinism and final gating.
  - **Feature F‑09 Logging and Error Codes:** Ensure logs exclude secrets and errors surface stable codes【824441473965294†L821-L823】.
  - **Feature F‑10 Deterministic I/O:** Canonical hashing, deterministic encoding and transactional writes【824441473965294†L776-L804】.
  - **Feature F‑11 Performance and Compatibility:** Keep UI responsive and ensure upgrades do not corrupt data【824441473965294†L851-L867】.
  - **Feature F‑12 Final Release:** Integrate all acceptance tests and cut release when `REQ-1001` is satisfied【824441473965294†L871-L889】.

### 4.2 Naming Convention
The work items follow a simple prefix convention:

 - **Epics** use the prefix `E-` followed by a two‑digit identifier and a descriptive title (e.g., `E-01 Foundation and Config`).
 - **Features** use the prefix `F-` followed by a two‑digit identifier and a title (e.g., `F-03 Plan & Preview`).
 - **Tasks** use the prefix `T-` followed by a two‑digit identifier and a brief title (e.g., `T-07 Build Staging Layout`).
 - **Decisions** use the prefix `D-` followed by a two‑digit identifier and a concise description (and must link to a spec section or appendix).

---

## 5. Milestone Detail Template

Below each milestone uses the same numbering scheme (`M0` – `M5`) defined in Section 2. Owners are set to Sam Ciero throughout.

### M0: Foundation & Configuration Setup

Type: M  
Target Date (UTC): —  
Owner(s): Sam Ciero  
Spec Sections: 2 (Goals & Constraints), 6 (User Interface), 7.1 (Configuration), 10.2 (Trust Boundary), 13 (Performance)

#### 5.1 Goal

Establish a baseline Windows 10+ desktop application that enforces trust boundaries, parses and validates configuration, provides a basic WPF UI for configuring roots and MetaF installation, and ensures all user‑content I/O occurs within allowed roots【824441473965294†L123-L137】【824441473965294†L748-L756】. This milestone also implements optional integrations gating and ensures the UI remains responsive during scanning and configuration【824441473965294†L206-L214】【824441473965294†L851-L856】.

#### 5.2 In Scope

- Implement `ConfigService` to load and parse `config.yaml` stored under `APP_CONFIG_DIR` with lenient unknown key handling【824441473965294†L247-L256】.
- Validate configuration keys: absolute local paths, non‑overlapping fixed volumes, existence and flatness of `MET_SOURCE_DIR`, `.git` presence in `AF_REPO_DIR`, and creatability of `APP_WORK_DIR`【824441473965294†L260-L268】.
- Provide UI to configure directory roots and MetaF install settings; enforce preview/approval gating for actions as a placeholder for later workflows【824441473965294†L206-L214】.
- Implement `BoundaryFileSystem` enforcing allowed roots, canonicalization, final‑path checks, and reparse point policy【824441473965294†L748-L756】.
- Implement gating flags for VS Code workspace management and optional integrations (no blocking if absent)【824441473965294†L216-L243】.
- Enforce that conversion runs are user‑triggered; no background conversions【824441473965294†L123-L137】.
- Ensure UI offloads heavy work off the UI thread for responsiveness【824441473965294†L851-L856】.

#### 5.3 Out of Scope

- Actual MetaF installation and execution (handled in M1).
- Conversion workflows (`.met→.af` or `.af→.met`) and Git publish mechanics.
- Repair mapping or relink features.
- Archive retention enforcement and conflict policies.
- Final determinism, logging and error handling (deferred to M5).

#### 5.4 Deliverables

- **Foundation Codebase** (artifact: repository code) with WPF UI skeleton, configuration parsing and boundary enforcement modules.
- **Configuration Guide** (doc) describing how to prepare `config.yaml` and set up directories.
- **Trust Boundary Test Report** (doc) verifying enforcement of allowed roots and reparse policies.

#### 5.5 Requirement Coverage (Traceability)

| Requirement ID | Summary | Acceptance Tests | Notes |
| --- | --- | --- | --- |
| REQ-0001 | Windows 10+ local desktop, no cloud dependency【824441473965294†L123-L127】 | AT-0001 | Baseline platform constraint |
| REQ-0002 | Use `LIGHTNING.*` namespaces【824441473965294†L128-L129】 | AT-0001 | Coding convention enforcement |
| REQ-0003 | Enforce trust boundary rules【824441473965294†L130-L132】 | AT-0002, AT-0003 | Implemented in BoundaryFileSystem |
| REQ-0005 | Runs shall be user‑triggered【824441473965294†L135-L136】 | AT-0001 | UI disallows background conversions |
| REQ-0102 | Open Workspace best‑effort; non‑blocking【824441473965294†L216-L219】 | AT-0001 | Provide optional VS Code integration toggles |
| REQ-0103 | Workspace file management gated and optional【824441473965294†L220-L221】 | AT-0001 | Flag `MANAGE_VSCODE_WORKSPACE_FILES` |
| REQ-0104 | Offline conversion after MetaF pinned【824441473965294†L222-L223】 | AT-0001 | Document requirement; actual offline after M1 |
| REQ-0105 | Optional integrations non‑blocking【824441473965294†L241-L243】 | AT-0001 | Integrations disabled by default |
| REQ-0201 | `config.yaml` stored under `APP_CONFIG_DIR` and parsed leniently【824441473965294†L247-L256】 | AT-0002 | Implemented by ConfigService |
| REQ-0202 | Config validation rules (paths, volumes, non‑overlap, existence)【824441473965294†L260-L268】 | AT-0002, AT-0030 | Validation logic |
| REQ-0203 | Versioned data upgrade is forward‑only and idempotent【824441473965294†L341-L343】 | AT-9001 | Documented; upgrade routines stubbed |
| REQ-0501 | Enforce allowed roots, canonicalization and final path checks【824441473965294†L748-L756】 | AT-0002 | BoundaryFileSystem enforcement |
| REQ-0502 | Reparse point policy enforcement【824441473965294†L754-L760】 | AT-0003 | Policy options implemented |
| REQ-0504 | Validate external inputs and reject violations【824441473965294†L765-L772】 | AT-0002 | Input validation built into services |
| REQ-0801 | UI should remain responsive【824441473965294†L851-L856】 | AT-9001 | UI uses asynchronous tasks |
| REQ-0901 | Upgrades shall not corrupt persisted data【824441473965294†L860-L867】 | AT-9001 | Document upgrade strategy |

#### 5.6 Decision Tables Impact (If Any)

- Appendix A tables impacted: A.2 (Conflict Policy) – none yet; A.3 (Publish Policy) – none yet.
- Changes required: None.
- New rows/cases to add: None.

#### 5.7 Acceptance Criteria (Milestone DoD)

- [ ] Foundation modules compiled without errors and integrated into UI.
- [ ] All mapping configuration validations implemented and verified.
- [ ] Boundary enforcement passes AT‑0001, AT‑0002 and AT‑0003.
- [ ] UI is responsive during configuration and scanning.
- [ ] Optional integrations gating toggles appear and default to off.
- [ ] Initial documentation and guides published.

#### 5.8 Demo Script (Optional but Recommended)

1. Launch the LIGHTNING app on a Windows 10 machine and configure `MET_SOURCE_DIR`, `AF_REPO_DIR`, and `APP_WORK_DIR` via the UI.
2. Attempt to select an invalid UNC path or overlapping directories; observe validation errors.
3. Navigate the UI to optional VS Code settings; toggle them on/off and verify no errors occur.

#### 5.9 Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation | Owner |
| --- | --- | --- | --- | --- |
| Misconfigured directory roots cause later conversion failures | M | H | Provide clear validation messages and documentation; include path selection dialogs | Sam Ciero |
| UI responsiveness issues on large directory scans | M | M | Use background threads and throttled progress updates | Sam Ciero |
| Early enforcement may block legitimate scenarios (e.g., network drives) | L | M | Provide configuration flags for additional volumes in future releases | Sam Ciero |

#### 5.10 Dependencies

- Internal: `ConfigService`, `BoundaryFileSystem`, UI framework.
- External: Windows 10+, .NET runtime (bundled with WPF app).

#### 5.11 Exit Artifacts Checklist

- [ ] Release notes entry for baseline features.
- [ ] Changelog entry describing configuration validation and trust boundary.
- [ ] Test report demonstrating success of AT‑0001, AT‑0002, AT‑0003.
- [ ] Configuration guide published.
- [ ] Security review notes for boundary enforcement.

---

### M1: MetaF Bootstrap & Installer

Type: M  
Target Date (UTC): —  
Owner(s): Sam Ciero  
Spec Sections: 8.1 (Bootstrap), 11 (Canonical Hashing), 12.1 (Logging)

#### 5.1 Goal

Enable the application to bootstrap and pin MetaF in an app‑owned cache. This milestone installs MetaF using either a release ZIP or a dotnet publish, computes the canonical `METAF_BUNDLE_SHA` manifest hash and persists bootstrap outputs in `config.yaml`. The installer logs up to 50 attempts and avoids global system mutation【824441473965294†L452-L496】.

#### 5.2 In Scope

- Validate bootstrap inputs and ensure required executables (`git`, `dotnet`) exist for chosen install mode【824441473965294†L461-L466】.
- Download and install MetaF under `APP_WORK_DIR`, either from a release ZIP or by building from a commit; compute `METAF_BUNDLE_SHA` using the manifest algorithm【824441473965294†L461-L482】【824441473965294†L776-L804】.
- Persist `METAF_ROOT_DIR`, `METAF_EXE_PATH` and `METAF_BUNDLE_SHA` back into `config.yaml`【824441473965294†L452-L470】.
- Write bootstrap logs under `APP_WORK_DIR\logs\bootstrap\` and retain the most recent 50 logs【824441473965294†L452-L485】.
- On bootstrap failure, perform one clean‑cache retry before surfacing an error【824441473965294†L486-L487】.
- Ensure MetaF prerequisites are satisfied without mutating global machine state and optionally provision an app‑managed `.NET` runtime under `APP_WORK_DIR\deps\dotnet\`【824441473965294†L489-L495】.

#### 5.3 Out of Scope

- Conversion workflows (`.met→.af` and `.af→.met`), mapping logic and Git operations.
- Plan/Preview generation and conflict handling.
- Repair mapping and relink workflows.

#### 5.4 Deliverables

- **MetaF Bootstrap Installer** (binary/service) that installs and validates MetaF.
- **Bootstrap Logs** (files) demonstrating logging and retention.
- **Updated Config** (doc + file) containing installed paths and bundle hash.

#### 5.5 Requirement Coverage (Traceability)

| Requirement ID | Summary | Acceptance Tests | Notes |
| --- | --- | --- | --- |
| REQ-0301 | Install MetaF under `APP_WORK_DIR` and pin by bundle SHA【824441473965294†L471-L473】 | AT-0011 | Installation destination and pinning |
| REQ-0302 | Supported install modes: `repo_release_zip` or `dotnet_publish`【824441473965294†L474-L476】 | AT-0009, AT-0010 | Validate mode enumeration |
| REQ-0303 | Release tag must provide exactly one `.zip` asset【824441473965294†L477-L479】 | AT-0009 | Failure handling on multiple/no assets |
| REQ-0304 | dotnet_publish requires 40‑hex commit SHA【824441473965294†L480-L482】 | AT-0010 | Validate commit reference |
| REQ-0305 | Bootstrap logs retained up to 50 entries【824441473965294†L483-L485】 | AT-0012 | Log retention policy |
| REQ-0306 | One clean‑cache retry on bootstrap failure【824441473965294†L486-L487】 | AT-0009, AT-0010 | Retry logic |
| REQ-0307 | Invoke MetaF per input file with explicit paths【824441473965294†L488-L490】 | AT-0014, AT-0015 | Partially prepared; full usage in M2/M3 |
| REQ-0308 | Provide prerequisites without global mutation【824441473965294†L489-L495】 | AT-0013 | Use app‑managed runtime |
| REQ-0601 | Canonical hashing with SHA‑256 for bundle hash【824441473965294†L785-L804】 | AT-0011 | Manifest algorithm implemented |
| REQ-0701 | Logs exclude secrets【824441473965294†L821-L823】 | AT-0024 | Ensure sensitive data redacted |
| REQ-0702 | Stable error codes on user‑visible failures【824441473965294†L821-L823】 | AT-0023 | Error taxonomy integrated |

#### 5.6 Decision Tables Impact

- Appendix A tables impacted: none (bootstrapping does not invoke conflict or publish policies).
- Changes required: None.
- New rows/cases: None.

#### 5.7 Acceptance Criteria (Milestone DoD)

- [ ] MetaF installs successfully via both supported modes with computed `METAF_BUNDLE_SHA`.
- [ ] Bootstrap logs captured with retention limited to most recent 50 entries.
- [ ] `config.yaml` updated with installed paths and bundle hash.
- [ ] Bootstrap failure triggers a clean‑cache retry.
- [ ] Installation does not alter system PATH or registry.
- [ ] AT‑0009, AT‑0010, AT‑0011, AT‑0012, AT‑0013 pass.

#### 5.8 Demo Script

1. Using the UI, trigger “Install MetaF” with `repo_release_zip` mode; confirm MetaF is downloaded, installed under `APP_WORK_DIR`, and `config.yaml` reflects new settings.
2. Simulate a failure (e.g., corrupted download) and verify that the installer deletes the install directory, retries once, then surfaces an error with a stable code.
3. Repeat installation using `dotnet_publish` mode with a specific commit SHA; verify that the manifest hash matches expectation.
4. Inspect the bootstrap logs directory and verify retention of only the most recent 50 logs.

#### 5.9 Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation | Owner |
| --- | --- | --- | --- | --- |
| Network failures interrupt downloading MetaF | M | M | Provide resume and retry logic; allow manual download via offline mode in future | Sam Ciero |
| Incorrect manifest computation yields wrong pin | L | H | Unit‑test manifest algorithm and verify using test bundles | Sam Ciero |
| Bootstrapping modifies system state inadvertently | L | H | Run installer in isolated process; test on clean machines | Sam Ciero |

#### 5.10 Dependencies

- Internal: Output from M0 (configuration service and UI).
- External: GitHub or internal server hosting MetaF assets; `git` and `dotnet` executables.

#### 5.11 Exit Artifacts Checklist

- [ ] Bootstrap installer binary and scripts.
- [ ] Updated config and manifest documentation.
- [ ] Bootstrap test report with AT‑0009 through AT‑0013 results.
- [ ] Changelog entry describing installation and logging features.
- [ ] Security review notes on external downloads.

---

### M2: Forward Conversion (.met→.af)

Type: M  
Target Date (UTC): —  
Owner(s): Sam Ciero  
Spec Sections: 7.3 (Naming & mapping), 7.4 (Archive Policy), 8.2 (met→af), 8.2.1 (Git behavior), 9 (Plan, Preview & Execution), 10 (Security & Trust)

#### 5.1 Goal

Implement the deterministic `.met→.af` conversion workflow that scans the `MET_SOURCE_DIR`, loads and validates `mapping.yaml`, builds a deterministic Plan and Preview enumerating all intended writes and Git side effects, stages and archives `.met` files, invokes MetaF for each file, and publishes `.af` outputs plus an updated `mapping.yaml` into `AF_REPO_DIR` via atomic operations【824441473965294†L497-L566】【824441473965294†L639-L681】. This milestone also enforces archive naming, retention policies and commit message conventions.

#### 5.2 In Scope

- Scan `MET_SOURCE_DIR` (flat) for `.met` files; reject directories under the root【824441473965294†L526-L528】.
- Load and strictly validate `mapping.yaml`, enforcing key invariants and uniqueness for `MET_REL` and `AF_REL`【824441473965294†L358-L374】【824441473965294†L376-L382】.
- Detect `.met` renames/replacements: update `MET_REL` in mapping or prompt the user to update‑in‑place or skip【824441473965294†L529-L535】.
- Build a deterministic Plan capturing mapping snapshot hash, toolchain fingerprint, inputs snapshot and intended outputs【824441473965294†L639-L655】; ensure Plan stability for identical inputs【824441473965294†L657-L660】.
- Render Preview enumerating all filesystem writes and Git side effects【824441473965294†L659-L661】.
- On user approval, archive each `.met` input immediately before conversion using `before_convert` naming rules【824441473965294†L520-L537】【824441473965294†L404-L424】.
- Invoke MetaF per file into a staging directory and generate the next `mapping.yaml`.
- Classify destination collisions (`existing-mapped` vs `new-mapped`) and apply `CONFLICT_POLICY` (fail, suffix, fail_all) before Publish【824441473965294†L698-L708】.
- Publish staged `.af` files and `mapping.yaml` into `AF_REPO_DIR` via atomic temp‑then‑replace; stage and commit only planned outputs and push based on `GIT_PUSH_POLICY`【824441473965294†L556-L566】.
- Generate deterministic commit messages: `LIGHTNING: met→af COUNT files`【824441473965294†L558-L561】.
- Enforce repo cleanliness if `REQUIRE_CLEAN_REPO=true`【824441473965294†L561-L563】.
- Run plan snapshot integrity checks before Publish and abort if mapping or toolchain changed【824441473965294†L687-L695】.
- Acquire a run exclusivity lock on the tuple (`AF_REPO_DIR`, `MET_SOURCE_DIR`)【824441473965294†L682-L684】.
- Apply archive retention and cap enforcement after the run completes【824441473965294†L427-L448】.

#### 5.3 Out of Scope

- Bootstrapping MetaF (handled in M1).
- Reverse conversion (`.af→.met`) flows (M3).
- Repair mapping and relink features (M4).
- Logging/error code infrastructure (M5).
- Non‑flat `MET_SOURCE_DIR` (such runs fail fast by design).

#### 5.4 Deliverables

- **Planner & Preview Engine** (service/code) producing deterministic Plan and Preview.
- **Conversion Pipeline** (code) handling scanning, archiving, MetaF invocation and staging.
- **Git Adapter Implementation** (code) performing explicit staging, committing and optional pushing.
- **User Flow Documentation** (doc) for `.met→.af` conversions including conflict policies and prompts.
- **Updated Mapping Artifacts** (`mapping.yaml` file) with enforced invariants.

#### 5.5 Requirement Coverage (Traceability)

| Requirement ID | Summary | Acceptance Tests | Notes |
| --- | --- | --- | --- |
| REQ-0004 | Require Preview and explicit approval before Publish【824441473965294†L133-L134】 | AT-0005, AT-0006 | Implemented via Plan/Preview gating |
| REQ-0101 | UI gating on Preview and approval【824441473965294†L216-L217】 | AT-0005, AT-0006 | Enforced before conversion begins |
| REQ-0204 | Strict parsing of `mapping.yaml`; unknown keys/invariant violations block conversion【824441473965294†L346-L377】 | AT-0016 | Validated during mapping load |
| REQ-0205 | Fingerprint drift blocks runs, triggers Repair Mapping【824441473965294†L376-L380】 | AT-0017 | Drift detection surfaces error in this milestone; repair in M4 |
| REQ-0206 | Validate `MET_REL` and `AF_REL` naming and uniqueness【824441473965294†L360-L374】 | AT-0016 | Enforced on load and before write |
| REQ-0207 | Deterministic naming and sanitization rules for outputs【824441473965294†L390-L407】 | AT-0004 | Applied when selecting `AF_REL` names |
| REQ-0208 | Archive stored only under `APP_WORK_DIR` with naming/collision rules【824441473965294†L410-L424】 | AT-0026 | Implementation of `.met` archives |
| REQ-0209 | Archive retention and cap rules【824441473965294†L427-L440】 | AT-0026 | Enforcement routine after runs |
| REQ-0210 | Retention enforcement triggers on startup and after runs【824441473965294†L446-L448】 | AT-0026 | Called at application start and run end |
| REQ-0310 | Plan captures mapping snapshot hash and toolchain fingerprint【824441473965294†L519-L521】【824441473965294†L639-L655】 | AT-0004, AT-0028 | Plan object includes snapshot fields |
| REQ-0311 | Archive `.met` inputs before conversion; commit only `.af` and `mapping.yaml`【824441473965294†L522-L525】 | AT-0014 | Achieved via archiving step and Git Adapter |
| REQ-0312 | Treat `MET_SOURCE_DIR` as flat and fail fast on subdirectories【824441473965294†L526-L528】 | AT-0030 | Enforced during scanning |
| REQ-0314 | Update `MET_REL` on rename only after successful run【824441473965294†L529-L532】 | AT-0019 | Staged mapping updates committed on success |
| REQ-0315 | Prompt user on `.met` replacement; update‑in‑place retains `AF_REL`【824441473965294†L532-L535】 | AT-0020 | UI prompt integrated |
| REQ-0318 | Use `before_convert` archive naming【824441473965294†L536-L537】 | AT-0014 | Archive naming implemented |
| REQ-0319 | Stage only planned outputs; exclude archives and work dir【824441473965294†L556-L558】 | AT-0022 | Git Adapter uses explicit file list |
| REQ-0320 | Deterministic commit message format `LIGHTNING: met→af COUNT files`【824441473965294†L558-L561】 | AT-0027 | Implement commit message generator |
| REQ-0321 | Abort when repo not clean and `REQUIRE_CLEAN_REPO=true`【824441473965294†L561-L563】 | AT-0027 | Preflight check |
| REQ-0322 | Push behavior follows `GIT_PUSH_POLICY`【824441473965294†L562-L565】 | AT-0027 | Git Adapter supports `always`/`never` |
| REQ-0401 | Plan stability given identical inputs【824441473965294†L657-L658】 | AT-0004 | Verified via plan hash tests |
| REQ-0402 | Preview enumerates all writes and side effects【824441473965294†L658-L661】 | AT-0005 | Preview UI enumerates operations |
| REQ-0403 | Cancel before Publish yields no Publish【824441473965294†L676-L678】 | AT-0006 | Cancellation aborts run pre-Publish |
| REQ-0404 | Publish uses staging and atomic replace; apply conflict policy【824441473965294†L679-L683】 | AT-0007, AT-0008 | Staging and conflict logic implemented |
| REQ-0405 | Run lock on tuple prevents concurrent runs【824441473965294†L682-L684】 | AT-0021 | Acquire exclusivity lock |
| REQ-0406 | Revalidate plan snapshot and bundle before Publish【824441473965294†L687-L695】 | AT-0028 | Abort if state drifted |
| REQ-0407 | Preflight classification of collisions (`existing-mapped` vs `new-mapped`)【824441473965294†L698-L708】 | AT-0029 | Implemented in Publish preflight |
| REQ-0408 | All outputs written into staging layout【824441473965294†L710-L722】 | AT-0029 | Verified in staging design |
| REQ-0503 | Git operations stage only planned outputs【824441473965294†L762-L763】 | AT-0022 | Reaffirmed with Git Adapter |

#### 5.6 Decision Tables Impact

- Appendix A.2 (Conflict Policy): Implement fail/suffix/fail_all behaviors for new‑mapped collisions.
- Appendix A.3 (Publish Policy): Adopt publish actions and atomicity rules for `.af`, `mapping.yaml` and ensure no rollback for archives【824441473965294†L919-L927】.
- Changes required: None; implement as specified.
- New rows/cases: None.

#### 5.7 Acceptance Criteria (Milestone DoD)

- [ ] Plan and Preview engine produces stable output lists given identical inputs.
- [ ] Mapping loader enforces strict schema and fails on unknown keys or invariants.
- [ ] `.met→.af` conversion flows produce archived `.met` files, staged `.af` outputs and updated `mapping.yaml`.
- [ ] Git Adapter stages only planned outputs, commits with deterministic message and respects `GIT_PUSH_POLICY`.
- [ ] Archive retention enforcement runs after conversion.
- [ ] Conflict policy behavior tested across fail, suffix and fail_all.
- [ ] AT‑0004, AT‑0005, AT‑0006, AT‑0007, AT‑0008, AT‑0014, AT‑0019, AT‑0020, AT‑0021, AT‑0022, AT‑0026, AT‑0027, AT‑0028, AT‑0029, AT‑0030 pass.

#### 5.8 Demo Script

1. Populate `MET_SOURCE_DIR` with several `.met` files and run “Convert .met→.af”; review the Plan and Preview, note that all writes and Git actions are enumerated.
2. Approve the conversion; observe each `.met` is archived before conversion, `.af` files appear in staging and are committed to Git with the specified commit message.
3. Trigger a second conversion without changing inputs; verify Plan output list and ordering remain identical.
4. Create a new `.met` that collides with an existing path; run conversion under each conflict policy (fail, suffix, fail_all) and verify behavior matches Appendix A.2.
5. Simulate a dirty Git repo; run conversion with `REQUIRE_CLEAN_REPO=true` and confirm preflight aborts with a stable error code.
6. Force a drift in `mapping.yaml` or change the MetaF bundle between Plan and execution; verify execution aborts due to snapshot mismatch.

#### 5.9 Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation | Owner |
| --- | --- | --- | --- | --- |
| Mapping invariants or fingerprint drift cause frequent blocking | M | M | Provide clear error messages and link to Repair Mapping (M4) | Sam Ciero |
| Large number of `.met` files impacts Plan performance | M | M | Use streaming scans and limit UI updates; optimize hashing | Sam Ciero |
| Git operations fail due to repository state | M | M | Provide preflight checks and clear remedial instructions; support push policy `never` | Sam Ciero |

#### 5.10 Dependencies

- Internal: Output from M1 (installed MetaF), `ConfigService`, `BoundaryFileSystem`.
- External: Git executable and configuration.

#### 5.11 Exit Artifacts Checklist

- [ ] Plan/Preview component and documentation.
- [ ] Conversion pipeline code and staging layout.
- [ ] Git Adapter implementation and test results.
- [ ] Updated `mapping.yaml` and archive files.
- [ ] User guide for `.met→.af` conversion.
- [ ] Test report covering acceptance tests for this milestone.

---

### M3: Reverse Conversion (.af→.met)

Type: M  
Target Date (UTC): —  
Owner(s): Sam Ciero  
Spec Sections: 8.3 (af→met), 7.3 (Mapping), 7.4 (Archive Policy), 9.3 (Execution)

#### 5.1 Goal

Implement the `.af→.met` conversion workflow that uses the existing `mapping.yaml` to determine destination paths within `MET_SOURCE_DIR`, validates mapping fields strictly, builds Plan/Preview, invokes MetaF to produce `.met` files into staging, archives existing destination `.met` files before overwrite and publishes new `.met` outputs via atomic replace【824441473965294†L567-L578】.

#### 5.2 In Scope

- Load and strictly validate `mapping.yaml` and ensure required fields for `.af→.met` are present; fail fast if missing or invalid【824441473965294†L574-L587】.
- Build Plan and Preview enumerating `.met` destination writes and side effects; ensure determinism similar to M2【824441473965294†L639-L661】.
- Invoke MetaF per active mapping entry into staging.
- On approval, archive any existing destination `.met` using `before_overwrite` naming【824441473965294†L583-L589】.
- Publish `.met` outputs into `MET_SOURCE_DIR` via atomic temp‑then‑replace.
- Apply Plan snapshot integrity checks, conflict classification and run exclusivity lock (shared with M2).
- Archive retention enforcement after run completion.

#### 5.3 Out of Scope

- Bootstrapping MetaF.
- `.met→.af` conversion flows and Git operations.
- Repair mapping and relink logic.
- Logging and hardening features.

#### 5.4 Deliverables

- **Reverse Conversion Pipeline** (code) implementing `.af→.met` path planning, staging, archiving and publishing.
- **Update to Plan/Preview Engine** to support reverse flows.
- **User Documentation** for reverse conversion and overwrite prompt semantics.

#### 5.5 Requirement Coverage (Traceability)

| Requirement ID | Summary | Acceptance Tests | Notes |
| --- | --- | --- | --- |
| REQ-0316 | `.af→.met` outputs written only to `MET_SOURCE_DIR\MET_REL` and archives created before overwrite【824441473965294†L582-L589】 | AT-0015 | Implements overwrite archive kind `before_overwrite` |
| REQ-0317 | Invalid or missing mapping fields block `.af→.met` runs【824441473965294†L583-L587】 | AT-0016 | Validation enforced |
| REQ-0323 | Use `before_overwrite` archive kind for overwrite【824441473965294†L583-L589】 | AT-0015 | Archive naming consistent |
| REQ-0401–REQ-0408 | Plan stability, preview enumeration, cancellation, staging, run lock, preflight classification and staging layout【824441473965294†L639-L722】 | AT-0004–AT-0008, AT-0021, AT-0028, AT-0029 | Reused from M2 for reverse flows |
| REQ-0204–REQ-0207 | Strict mapping parsing, fingerprint drift detection, naming rules【824441473965294†L346-L377】【824441473965294†L376-L382】【824441473965294†L390-L407】 | AT-0016, AT-0017, AT-0004 | Shared across conversions |
| REQ-0208–REQ-0210 | Archive location, retention and triggers【824441473965294†L410-L448】 | AT-0026 | Enforcement continues |
| REQ-0318 | `before_convert` naming (met→af) not applicable here | — | Not applicable to `.af→.met` |
| REQ-0319–REQ-0322 | Git staging and commit rules | — | Not applicable (no Git in reverse flow) |

#### 5.6 Decision Tables Impact

- Appendix A.3 (Publish Policy): Reverse conversions apply temp‑then‑replace for `.met` files and archive before overwrite【824441473965294†L919-L926】.
- Appendix A.2 (Conflict Policy): Only applies when collisions occur in `.met` dest; follow same policy as M2.
- Changes required: None.

#### 5.7 Acceptance Criteria (Milestone DoD)

- [ ] Reverse conversion pipeline produces correct `.met` files in `MET_SOURCE_DIR` and archives any overwritten destinations.
- [ ] Mapping validation rejects missing or invalid fields and surfaces stable error codes.
- [ ] Plan/Preview for reverse flows enumerates all writes and side effects.
- [ ] Archive retention enforcement runs after reverse conversions.
- [ ] AT‑0015, AT‑0016, AT‑0028, AT‑0029 pass.

#### 5.8 Demo Script

1. Populate `AF_REPO_DIR` with `.af` files and a valid `mapping.yaml` mapping to `MET_SOURCE_DIR`; run “Convert .af→.met”; examine Plan/Preview and approve.
2. Approve conversion; verify `.met` outputs appear in `MET_SOURCE_DIR`, previously existing files are archived with kind `before_overwrite`, and new files replace old files atomically.
3. Modify `mapping.yaml` to remove a required field; attempt conversion and confirm that the run fails with a stable error code.
4. Introduce a drift in `mapping.yaml` fingerprint; verify the run fails and instructs the user to perform Repair Mapping.

#### 5.9 Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation | Owner |
| --- | --- | --- | --- | --- |
| Mapping errors block legitimate reverse conversions | M | M | Provide clear validation errors and link to Repair Mapping | Sam Ciero |
| Archive retention rules may lead to unexpected deletions | L | M | Document retention policy and allow user configuration in future releases | Sam Ciero |

#### 5.10 Dependencies

- Internal: Output from M2 (Plan/Preview engine, MetaF installed).
- External: None beyond those previously listed.

#### 5.11 Exit Artifacts Checklist

- [ ] Reverse conversion pipeline code and tests.
- [ ] Updated documentation for reverse conversion flows.
- [ ] Test report demonstrating AT‑0015, AT‑0016 and cross‑plan tests.
- [ ] Changelog entry summarizing reverse conversion support.
- [ ] Archive audit notes.

---

### M4: Mapping Repair & Relink

Type: M  
Target Date (UTC): —  
Owner(s): Sam Ciero  
Spec Sections: 8.4 (Repair Mapping), 8.5 (Relink), 7.3 (Mapping Invariants)

#### 5.1 Goal

Implement workflows to repair the mapping file when the fingerprint of `MET_SOURCE_DIR` drifts and to relink mapping entries when `.af` files are moved or renamed. This milestone updates the fingerprint, deterministically activates or retires items, enforces invariants, writes `mapping.yaml` transactionally, and allows the user to locate or skip missing `.af` files【824441473965294†L591-L634】.

#### 5.2 In Scope

- Detect `MET_SOURCE_ROOT_FINGERPRINT` mismatch at run start and block conversion until repair is performed【824441473965294†L376-L380】.
- Implement Repair Mapping workflow: compute new fingerprint, rescan `MET_SOURCE_DIR`, set item states to `active` or `retired`, update `MET_REL` and retire timestamps, enforce invariants, and write `mapping.yaml` transactionally; support cancel without writes【824441473965294†L598-L613】.
- Implement Relink workflow: when referenced `AF_REL` is missing or unmapped `.af` files are present, search by size and hash; if exactly one match, update `AF_REL`; otherwise prompt user to Locate or Skip; restrict Locate selections to `AF_REPO_DIR`【824441473965294†L615-L633】.
- Update Plan/Preview engine to include Repair and Relink outcomes in its display.

#### 5.3 Out of Scope

- Conversions (`.met→.af` and `.af→.met`) themselves.
- Bootstrap and logging hardening.
- Modifying naming or sanitization algorithms.

#### 5.4 Deliverables

- **Repair Mapping Module** (code) with UI flow for fingerprint drift detection and correction.
- **Relink Module** (code) with UI to choose locate or skip and update mapping snapshot.
- **Updated Documentation** describing when to repair mapping and how to use relink.

#### 5.5 Requirement Coverage (Traceability)

| Requirement ID | Summary | Acceptance Tests | Notes |
| --- | --- | --- | --- |
| REQ-0205 | Fingerprint drift blocks runs and enables Repair Mapping【824441473965294†L376-L380】 | AT-0017 | Detection occurs in M2; repair implemented here |
| REQ-0324 | Repair Mapping updates fingerprint, applies deterministic activation/retirement and writes transactionally; cancel has no writes【824441473965294†L591-L613】 | AT-0017 | Implementation ensures no partial writes on cancel |
| REQ-0325 | Relink matches missing entries by size/hash; Locate restricts to `AF_REPO_DIR`; Skip avoids changes【824441473965294†L615-L633】 | AT-0018 | Search and UI selection implemented |
| REQ-0206 | `MET_REL`/`AF_REL` validation and uniqueness enforced【824441473965294†L360-L374】 | AT-0016 | Ensured when writing updated mapping |
| REQ-0207 | Deterministic naming/sanitization rules for new items【824441473965294†L390-L407】 | AT-0004 | Applies when computing new `AF_REL` on relink |
| REQ-0401–REQ-0408 | Plan stability, preview enumeration, cancellation, staging, snapshot integrity, collision classification, staging layout【824441473965294†L639-L722】 | AT-0004–AT-0008, AT-0028, AT-0029 | Plan includes repair and relink outcomes |
| REQ-0501–REQ-0504 | Boundary enforcement and input validation【824441473965294†L748-L772】 | AT-0002, AT-0003 | Continue to enforce boundaries when relinking |

#### 5.6 Decision Tables Impact

- Appendix A.2 (Conflict Policy): Not directly impacted.
- Appendix A.3 (Publish Policy): Relink does not publish; repair mapping writes `mapping.yaml` transactionally similar to `.met→.af`.
- Changes required: None.
- New rows/cases: None.

#### 5.7 Acceptance Criteria (Milestone DoD)

- [ ] Repair Mapping detects fingerprint drift, shows preview of activations/retirements and writes updated `mapping.yaml` only on approval.
- [ ] Relink workflow matches missing entries by size and hash; user can locate within `AF_REPO_DIR` or skip.
- [ ] Canceling Repair Mapping or Relink produces no writes.
- [ ] AT‑0017 and AT‑0018 pass.

#### 5.8 Demo Script

1. Simulate a fingerprint drift by moving `MET_SOURCE_DIR` to a new volume; run conversion and verify that the app blocks the run and offers Repair Mapping.
2. In the Repair Mapping UI, preview proposed activations/retirements; cancel and verify no changes; rerun and approve and confirm `mapping.yaml` is updated.
3. Rename an `.af` file in the repository; run conversion to trigger Relink; observe that the app matches by size and hash or prompts to locate; select a correct file and verify mapping updates.
4. Choose Skip for a missing `.af` file and verify mapping entry remains unchanged.

#### 5.9 Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation | Owner |
| --- | --- | --- | --- | --- |
| Incorrect matching in Relink updates wrong `AF_REL` | M | H | Use strict size and hash matching; require explicit user confirmation when multiple candidates | Sam Ciero |
| Repair Mapping accidental retirements cause data loss | L | H | Provide clear preview and require explicit approval; support manual backup of mapping | Sam Ciero |

#### 5.10 Dependencies

- Internal: Output from M2 (Plan/Preview engine) and M3 (mapping updates).
- External: None.

#### 5.11 Exit Artifacts Checklist

- [ ] Repair Mapping module and test report.
- [ ] Relink module and test report.
- [ ] Updated `mapping.yaml` documentation.
- [ ] Changelog entry describing repair and relink capabilities.
- [ ] Decision log noting activation/retirement rules.

---

### M5: Observability, Hardening & Release

Type: R  
Target Date (UTC): —  
Owner(s): Sam Ciero  
Spec Sections: 11 (Determinism), 12 (Observability), 13 (Performance), 14 (Compatibility), 15 (Definition of Done)

#### 5.1 Goal

Finalize the LIGHTNING project by implementing determinism and reproducibility, robust logging and error handling, transactional writes for generated files, UI performance optimizations and upgrade/rollback support. This milestone also runs all acceptance tests and prepares release notes. Only when all acceptance tests pass and documentation is complete is the project considered done【824441473965294†L776-L804】【824441473965294†L821-L889】.

#### 5.2 In Scope

- Implement canonical hashing for `MET_ID`, `AF_SHA256` and `METAF_BUNDLE_SHA` using SHA‑256 and ensure deterministic encoding for generated files (`mapping.yaml`, workspace files) with LF line endings【824441473965294†L776-L804】.
- Ensure all generated files are written transactionally via temp file then atomic replace within their target directories【824441473965294†L801-L804】.
- Add detailed logging to bootstrap and run workflows, redacting secrets and sensitive tokens【824441473965294†L821-L823】.
- Surface stable error codes in the `LIGHTNING_{CATEGORY}_{NNNN}` format for all user‑visible failures【824441473965294†L821-L823】.
- Provide a comprehensive error taxonomy and exit code table as developer documentation【824441473965294†L825-L846】.
- Optimize UI responsiveness during scanning and execution (finish implementation of REQ‑0801)【824441473965294†L851-L856】.
- Ensure upgrades do not corrupt persisted data and document supported upgrade and rollback steps【824441473965294†L860-L867】.
- Produce upgrade and migration notes, release notes and changelog entries.
- Run the entire suite of acceptance tests and ensure `REQ-1001` is satisfied【824441473965294†L871-L889】.

#### 5.3 Out of Scope

- New feature development or changes to workflows and mapping logic.
- Additional integrations or optional enhancements beyond Appendix E.

#### 5.4 Deliverables

- **Determinism & Transactional Modules** (code) implementing hashing and atomic write operations.
- **Logging & Error Handling Framework** (code and configuration).
- **Performance Tuning Report** (doc) covering UI responsiveness improvements.
- **Release Package** (binary/doc) including release notes, upgrade instructions, rollback plan and compliance check results.
- **Comprehensive Test Report** (doc) with evidence of passing all `AT-####`.

#### 5.5 Requirement Coverage (Traceability)

| Requirement ID | Summary | Acceptance Tests | Notes |
| --- | --- | --- | --- |
| REQ-0601 | Canonical hashes for all identifiers using SHA‑256【824441473965294†L776-L800】 | AT-0011 | Implemented across all workflows |
| REQ-0602 | Deterministic encoding and transactional writes【824441473965294†L801-L804】 | AT-0025 | Implements atomic replace for mapping and workspace files |
| REQ-0701 | Logs exclude secrets or tokens【824441473965294†L821-L823】 | AT-0024 | Logging framework redacts sensitive info |
| REQ-0702 | Surface stable error codes【824441473965294†L821-L823】 | AT-0023 | Error handling infrastructure |
| REQ-0801 | UI should remain responsive during scanning and execution【824441473965294†L851-L856】 | AT-9001 | Final performance tuning and measurement |
| REQ-0901 | Upgrades do not corrupt persisted data【824441473965294†L860-L867】 | AT-9001 | Upgrade/rollback plan tested |
| REQ-1001 | Project done only when all acceptance tests pass【824441473965294†L871-L889】 | AT-9001 | Final gating for release |
| REQ-0305 | Bootstrap log retention ensures logs remain under limit【824441473965294†L483-L485】 | AT-0012 | Reviewed and tuned |
| REQ-0503 | Git stages only explicit outputs【824441473965294†L762-L763】 | AT-0022 | Validated across release |
| REQ-0601–REQ-0602 | Reiterated to ensure determinism for release | AT-0011, AT-0025 | Verified at project end |

#### 5.6 Decision Tables Impact

- Appendix A.1 (Cancellation Guarantees): Validate that cancellation results match table expectations in all phases【824441473965294†L895-L903】.
- Appendix A.3 (Publish Policy): Confirm compliance with publish actions and atomicity across all artifact types【824441473965294†L919-L927】.
- Changes required: None; documentation updated.
- New rows/cases: None.

#### 5.7 Acceptance Criteria (Milestone DoD)

- [ ] Canonical hashing and transactional write modules integrated and unit tested.
- [ ] Logging outputs redact sensitive data and error codes follow `LIGHTNING_{CATEGORY}_{NNNN}` format.
- [ ] UI performance metrics meet responsiveness goals during scanning and conversions.
- [ ] Upgrade, migration and rollback plans documented and validated.
- [ ] Release notes, upgrade guides, changelog and test report completed.
- [ ] All acceptance tests AT‑0001 through AT‑0030, AT‑9000 and AT‑9001 pass, satisfying REQ‑1001.

#### 5.8 Demo Script

1. Run full conversion cycles (.met→.af and .af→.met) with logging enabled; inspect logs to ensure no secrets are written and error codes follow the taxonomy.
2. Simulate user cancellations at various phases and verify outcomes match Appendix A.1.
3. Modify `mapping.yaml` in a controlled failure and verify the app surfaces a stable mapping error code.
4. Perform upgrade from a previous version; verify persisted data is intact and rollback steps are documented.
5. Run automated test suite and present the report demonstrating all acceptance tests have passed.

#### 5.9 Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation | Owner |
| --- | --- | --- | --- | --- |
| Missed edge cases lead to nondeterministic outputs | L | H | Extensive automated tests and code reviews focusing on determinism | Sam Ciero |
| Logs inadvertently contain secrets | L | H | Implement log scanning tests and redaction filters; peer review | Sam Ciero |
| Upgrade path fails on rare configurations | M | M | Provide comprehensive migration scripts and fallback; beta releases | Sam Ciero |

#### 5.10 Dependencies

- Internal: All previous milestones completed.
- External: None beyond those previously listed.

#### 5.11 Exit Artifacts Checklist

- [ ] Determinism modules and hashing tests.
- [ ] Logging and error handling framework documentation.
- [ ] Performance tuning report.
- [ ] Release notes, upgrade instructions and rollback strategy.
- [ ] Comprehensive acceptance test report.
- [ ] Roadmap change log updated.

---

## 6. Backlog (Not Yet Scheduled)

### 6.1 Candidate Features

| ID | Feature | Rationale | Spec Link | Notes |
| --- | --- | --- | --- | --- |
| F‑13 | Optional Job‑object execution of MetaF | Provide stricter process‑tree cancellation and resource limits | Appendix E【824441473965294†L1207-L1213】 | Spike needed to assess feasibility |
| F‑14 | Managed creation/update of VS Code workspace files | Simplify workspace management beyond manual gating | Appendix E【824441473965294†L1207-L1213】 | Depends on user feedback |
| F‑15 | Single‑file publish of LIGHTNING app | Reduce installation footprint | Appendix E【824441473965294†L1207-L1213】 | Evaluate packaging options |
| F‑16 | Export all mode in toolchains | Support bulk exports in future workflows | Appendix E【824441473965294†L1207-L1213】 | Defer to future release |

### 6.2 Deferred Enhancements (From Spec Appendix E)

- Optional Job Object execution of MetaF【824441473965294†L1207-L1213】.
- Optional managed creation/update of `.vscode/extensions.json` and other workspace files【824441473965294†L1207-L1213】.
- Optional single‑file publish of the LIGHTNING app【824441473965294†L1207-L1213】.
- Optional “export all” modes for future integrated toolchains【824441473965294†L1207-L1213】.

---

## 7. Upgrade, Migration, and Rollback Plan

### 7.1 Upgrade Notes

- From version: `v0.0.0` (initial prototype).
- To version: `v1.0.0`.
- Steps:
  1. Ensure backup of `config.yaml`, `mapping.yaml` and archives.
  2. Install new version of LIGHTNING.
  3. On first run, migrate any data formats using forward‑compatible upgrade routines (REQ‑0203)【824441473965294†L341-L343】.
  4. Verify `METAF_BUNDLE_SHA` remains valid; if not, re‑bootstrap MetaF.
  5. Run regression tests covering acceptance tests to confirm compatibility.

### 7.2 Data Migration Notes

- Migration ID: MIG‑0001.
- Trigger: Upgrading to any version with modified `mapping.yaml` schema.
- Backward compatibility: Provide migration scripts to convert old `mapping.yaml` entries and mark retired items; maintain original archives.
- Recovery: On failure, restore from backup and contact support; do not attempt manual edits.

### 7.3 Rollback Strategy

- Rollback supported: Yes.
- Rollback steps:
  1. Ensure backups of `config.yaml`, `mapping.yaml` and archives are available.
  2. Uninstall the new version and reinstall the previous release.
  3. Restore backed‑up configuration and mapping files.
  4. Delete any archives created by the failed upgrade.
- Data safety notes: Always archive original `.met` files and mapping snapshots before upgrade; avoid editing `mapping.yaml` manually.

---

## 8. Roadmap Change Log

| Version | Date (UTC) | Author | Change Type | Summary | Spec Impact |
| --- | ---: | --- | --- | --- | --- |
| v0.1 | 2025-12-21 | Sam Ciero | ADD | Initial roadmap creation aligning milestones to spec requirements | None |

---

## 9. Appendix — Traceability Index

### 9.1 Requirement‑to‑Milestone Index

| Requirement ID | Milestone(s) | Notes |
| --- | --- | --- |
| REQ-0001 | M0 | Platform baseline |
| REQ-0002 | M0 | Namespace naming |
| REQ-0003 | M0 | Trust boundary enforcement |
| REQ-0004 | M2 | Preview & approval before Publish |
| REQ-0005 | M0 | User‑triggered runs only |
| REQ-0101 | M2 | UI gating for conversions |
| REQ-0102 | M0 | Optional VS Code integration |
| REQ-0103 | M0 | Gated workspace management |
| REQ-0104 | M0 | Offline conversion post‑bootstrap |
| REQ-0105 | M0 | Optional integrations non‑blocking |
| REQ-0201 | M0 | Config location & lenient parse |
| REQ-0202 | M0 | Config validation rules |
| REQ-0203 | M0 | Forward‑only data upgrade routines |
| REQ-0204 | M2,M3 | Strict `mapping.yaml` parsing |
| REQ-0205 | M2,M4 | Fingerprint drift blocks runs; Repair Mapping handles drift |
| REQ-0206 | M2,M4 | Validate `MET_REL`/`AF_REL` and enforce uniqueness |
| REQ-0207 | M2,M4 | Deterministic naming/sanitization |
| REQ-0208 | M2,M3 | Archive location & naming rules |
| REQ-0209 | M2,M3 | Archive retention & cap |
| REQ-0210 | M2,M3 | Retention enforcement triggers |
| REQ-0301 | M1 | MetaF installed & pinned |
| REQ-0302 | M1 | Install mode enumeration |
| REQ-0303 | M1 | Release zip asset uniqueness |
| REQ-0304 | M1 | dotnet_publish pinning |
| REQ-0305 | M1,M5 | Bootstrap logging retention |
| REQ-0306 | M1 | Single clean‑cache retry |
| REQ-0307 | M1,M2,M3 | MetaF invoked per input file |
| REQ-0308 | M1 | Prerequisites without global mutation |
| REQ-0310 | M2 | Plan captures mapping snapshot & bundle |
| REQ-0311 | M2 | met→af archives & commit scope |
| REQ-0312 | M2 | Flat `MET_SOURCE_DIR` enforcement |
| REQ-0314 | M2 | met rename update after success |
| REQ-0315 | M2 | met replacement prompt |
| REQ-0316 | M3 | af→met destination & overwrite archives |
| REQ-0317 | M3 | Invalid mapping blocks af→met |
| REQ-0318 | M2 | met→af archive kind before_convert |
| REQ-0319 | M2 | Stage only planned outputs |
| REQ-0320 | M2 | Deterministic commit message format |
| REQ-0321 | M2 | Clean repo enforcement |
| REQ-0322 | M2 | Push policy semantics |
| REQ-0323 | M3 | af→met archive kind before_overwrite |
| REQ-0324 | M4 | Repair Mapping deterministic & transactional |
| REQ-0325 | M4 | Relink matching & locate restrictions |
| REQ-0401 | M2,M3,M4 | Plan stability |
| REQ-0402 | M2,M3 | Preview enumerates writes/side effects |
| REQ-0403 | M2,M3 | Cancel before Publish prevents writes |
| REQ-0404 | M2,M3 | Staging & atomic publish & conflict policy |
| REQ-0405 | M2 | Run exclusivity lock |
| REQ-0406 | M2,M3 | Plan snapshot integrity revalidation |
| REQ-0407 | M2,M3 | Preflight collision classification |
| REQ-0408 | M2,M3 | Staging layout requirement |
| REQ-0501 | M0,M2,M3,M4 | Enforce allowed roots & canonicalization |
| REQ-0502 | M0 | Reparse point policy enforcement |
| REQ-0503 | M0,M2,M5 | Stage only explicit outputs |
| REQ-0504 | M0 | Validate external inputs |
| REQ-0601 | M1,M5 | Canonical hashing |
| REQ-0602 | M5 | Deterministic encoding & transactional writes |
| REQ-0701 | M1,M5 | Log redaction |
| REQ-0702 | M1,M5 | Stable error codes |
| REQ-0801 | M0,M5 | UI responsiveness |
| REQ-0901 | M0,M5 | Upgrade does not corrupt data |
| REQ-1001 | M5 | All acceptance tests pass |

### 9.2 Acceptance‑Test‑to‑Milestone Index

| Acceptance Test | Milestone(s) | Notes |
| --- | --- | --- |
| AT-0001 | M0 | Platform, naming and user‑triggered runs |
| AT-0002 | M0 | Boundary enforcement & config validation |
| AT-0003 | M0 | Reparse point policy |
| AT-0004 | M2,M3 | Plan stability and deterministic naming |
| AT-0005 | M2 | Preview enumerates writes |
| AT-0006 | M2 | Cancel before Publish prevents writes |
| AT-0007 | M2 | Staging & atomic publish |
| AT-0008 | M2 | Conflict policy behavior |
| AT-0009 | M1 | Bootstrap ZIP mode asset selection |
| AT-0010 | M1 | Bootstrap dotnet_publish pinning |
| AT-0011 | M1,M5 | MetaF bundle hash manifest correctness |
| AT-0012 | M1 | Bootstrap logs retention |
| AT-0013 | M1 | MetaF prerequisites without global mutation |
| AT-0014 | M2 | met→af archives & commit contents |
| AT-0015 | M3 | af→met overwrite archives |
| AT-0016 | M3 | mapping.yaml strictness & invariants |
| AT-0017 | M4 | Fingerprint drift & Repair Mapping |
| AT-0018 | M4 | Relink locate restriction |
| AT-0019 | M2 | met rename handling |
| AT-0020 | M2 | met replacement prompt |
| AT-0021 | M2 | Run exclusivity lock |
| AT-0022 | M2 | Git guardrails (explicit staging) |
| AT-0023 | M1,M5 | Stable error codes |
| AT-0024 | M1,M5 | Log redaction |
| AT-0025 | M5 | Transactional writes for generated files |
| AT-0026 | M2,M3 | Archive naming, retention & cap enforcement |
| AT-0027 | M2 | Repo cleanliness & push policy |
| AT-0028 | M2,M3 | Plan snapshot integrity |
| AT-0029 | M2,M3 | Staging layout & collision classification |
| AT-0030 | M2 | Flat `MET_SOURCE_DIR` enforcement |
| AT-9000 | M5 | Spec lint — Normative isolation (implicit) |
| AT-9001 | M0,M5 | Spec lint — Traceability & UI responsiveness |

---

This completes the detailed roadmap for the LIGHTNING project.
