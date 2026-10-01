# BRIEFING — 2026-09-28T10:41:19Z

## Mission
Orchestrate the 4-phase ZenQ Calc upgrade (Drawings, PDF/ODS/XLS, 50+ Formulas, Tables, Dynamic Arrays, PivotTables) to 100% test pass and zero regressions.

## 🔒 My Identity
- Archetype: teamwork_preview_orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_orchestrator_1
- Original parent: parent
- Original parent conversation ID: 589b33e2-73aa-44c7-adb0-19ffb14c0767

## 🔒 My Workflow
- **Pattern**: Project
- **Scope document**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
1. **Decompose**: Survey existing codebase and specifications across 4 phases, compile Feature Inventory into PROJECT.md, decompose into milestones (Phase 1, Phase 2, Phase 3, Phase 4, E2E Testing Track)
2. **Dispatch & Execute** (pick ONE):
   - **Delegate (sub-orchestrator)**: Spawn sub-orchestrators for milestones and E2E Testing Track. Sub-orchestrators manage Explorer -> Worker -> Reviewer -> Challenger -> Auditor iteration loops.
3. **On failure** (in this order):
   - Retry: nudge stuck agent or re-send task
   - Replace: spawn fresh agent with partial progress
   - Skip: proceed without (only if non-critical)
   - Redistribute: split stuck agent's remaining work
   - Redesign: re-partition decomposition
   - Escalate: report to parent (sub-orchestrators only, last resort)
4. **Succession**: At 16 spawns and all subagents complete, write handoff.md, cancel crons, spawn successor gen2, exit.
- **Work items**:
  1. Survey & Map Codebase & Specifications [in-progress]
  2. E2E Testing Track & Test Infrastructure [pending]
  3. Phase 1: Critical Data Integrity & Core Productivity Tools [pending]
  4. Phase 2: Formula Engine Expansion & File Formats [pending]
  5. Phase 3: Structured Tables & Dynamic Arrays [pending]
  6. Phase 4: PivotTables & Advanced Analytics [pending]
  7. Final Acceptance & E2E Verification [pending]
- **Current phase**: 0 (Survey)
- **Current focus**: Surveying codebase and specifications with 3 parallel Explorers

## 🔒 Key Constraints
- DISPATCH-ONLY orchestrator: NEVER write source code, NEVER run build/test commands directly, NEVER explore codebase directly.
- NEVER reuse a subagent after it has delivered its handoff — always spawn fresh.
- Always pass path to ORIGINAL_REQUEST.md in subagent dispatches.
- Audit is a binary veto: if teamwork_preview_auditor reports INTEGRITY VIOLATION, fail unconditionally.

## Current Parent
- Conversation ID: 589b33e2-73aa-44c7-adb0-19ffb14c0767
- Updated: 2026-09-28T10:41:19Z

## Key Decisions Made
- Chose Project pattern with Dual Track (Implementation Track + E2E Testing Track).
- Initial step is Survey with 3 parallel Explorers to inspect existing ZenQ Calc codebase and specifications.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| survey_1 | teamwork_preview_explorer | Survey Codebase & Architecture | completed | 86ad6850-8017-43b5-8415-a9a3afe8077c |
| survey_2 | teamwork_preview_spec_miner | Survey Phases 1 & 2 Spec/API | completed | 9819c3ba-fef5-492d-bdf9-63c77efb7ed4 |
| survey_3 | teamwork_preview_spec_miner | Survey Phases 3 & 4 Spec/API | completed | 7701b696-a1b0-4ff1-a05a-a87c8b26a745 |
| e2e_writer_1 | teamwork_preview_test_writer | E2E Test Infra & Suites | completed | 499f6ffb-5ffb-4e44-8df6-260527f83e13 |
| p1_exp_1 | teamwork_preview_explorer | Phase 1 OpenXML Drawings | completed | edfefdba-d444-4b60-910b-dfd0cd7d9c29 |
| p1_exp_2 | teamwork_preview_explorer | Phase 1 Native PDF Export | completed | 9fe0dd2e-4eaa-4752-b8a0-3f35662ed353 |
| p1_exp_3 | teamwork_preview_explorer | Phase 1 Core Data Utilities | completed | 0816106d-79af-404c-8d9b-e0ddfff29d0c |
| worker_p1_1 | teamwork_preview_worker | Phase 1 Implementation | failed | 7b169daf-f2ac-4b6c-bcc7-9736d10dd1cd |
| worker_p1_2 | teamwork_preview_worker | Phase 1 Implementation (Repl) | in-progress | f7a07498-e08c-4375-b422-4ab93fc69ae3 |

## Succession Status
- Succession required: no
- Spawn count: 9 / 16
- Pending subagents: f7a07498-e08c-4375-b422-4ab93fc69ae3
- Predecessor: none
- Successor: not yet spawned

## Active Timers
- Heartbeat cron: task-16
- Safety timer: none
- On succession: kill all timers before spawning successor
- On context truncation: run `manage_task(Action="list")` — re-create if missing

## Artifact Index
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\ORIGINAL_REQUEST.md — Authoritative User Request
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_orchestrator_1\DISPATCH.md — Dispatch log
- c:\Users\Danh\Desktop\genofficepy\.agents\teamwork\teamwork_preview_orchestrator_1\progress.md — Liveness & progress tracker
- c:\Users\Danh\Desktop\genofficepy\PROJECT.md — Global project index (architecture, milestones, interfaces, code layout)
