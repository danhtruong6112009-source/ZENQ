# DISPATCH: E2E Testing Track Orchestrator

**Role**: E2E Testing Track Orchestrator
**Type**: teamwork_preview_orchestrator
**Working Directory**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\sub_orch_e2e
**Parent Conversation ID**: ddf357e9-0181-4327-b59b-767a2b9b4c82
**Authoritative User Request**: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md
**Project Index**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
**Project Root**: c:\Users\Danh\Desktop\genofficepy

## Dual Track Mandate
You are the Orchestrator for the E2E Testing Track.
Your responsibility is to design, build, and verify a comprehensive, opaque-box, requirement-driven test suite for the ZenQ Calc 4-phase upgrade.

## Core Rules & Constraints
1. **Strict C# 5 Compatibility**: All test code and harnesses must compile under .NET Framework 4.8 `csc.exe` (no C# 6+ features like `$"..."`, `?.`, `=>`, local functions, tuples).
2. **Opaque-box & Requirement-driven**: Derive test cases from `ORIGINAL_REQUEST.md` and `PROJECT.md § Feature Inventory`, NOT from internal private methods.
3. **4-Tier Methodology**:
   - **Tier 1 - Feature Coverage**: >=5 test cases per feature for isolated happy-paths.
   - **Tier 2 - Boundary & Corner Cases**: >=5 test cases per feature (empty, max-size, negative, null, boundary limits).
   - **Tier 3 - Cross-Feature Combinations**: Pairwise coverage of feature interactions.
   - **Tier 4 - Real-World Application Scenarios**: Multi-feature end-to-end user workflows.
4. **Deliverables**:
   - Create `c:\Users\Danh\Desktop\genofficepy\TEST_INFRA.md` documenting test architecture, runner command, directory layout, and tier coverage breakdown.
   - Implement test runner executable and test cases in `c:\Users\Danh\Desktop\genofficepy\tests\` that can be executed via a simple command (e.g. `cmd.exe /c run_tests.bat` or standalone test executable compiled via C# 5).
   - When all test cases are compiled and ready, publish `c:\Users\Danh\Desktop\genofficepy\TEST_READY.md` containing the runner command and coverage matrix.
5. **Execution Loop**:
   - As an orchestrator, follow the Project Orchestrator procedure: Assess -> Decompose or Iterate (Explorer -> Worker / Test Writer -> Reviewer -> Challenger -> Auditor -> Gate).
   - Use `teamwork_preview_test_writer` or `teamwork_preview_worker` for test implementation.
   - Provide regular heartbeat updates in `progress.md`.
