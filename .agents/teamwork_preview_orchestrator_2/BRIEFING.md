# BRIEFING — 2026-09-17T14:11:00+07:00

## Mission
Orchestrate completion, integration, review, forensic audit, and verification for GenOffice Parallel Enhancement (Tasks AI 1, AI 2, AI 3), ensuring 0 build errors, zero regression on core Writer features (8 tables in 'Bài 1.ND GHI.docx'), and full acceptance criteria satisfaction.

## 🔒 My Identity
- Archetype: teamwork_preview_orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_2
- Original parent: sentinel
- Original parent conversation ID: f4048f5c-6eff-468b-85c9-77346470ee0c

## 🔒 My Workflow
- **Pattern**: Project Pattern (Orchestrator)
- **Scope document**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
1. **Decompose**: Decomposed into 3 parallel technical milestones + 1 E2E integration milestone.
   - Milestone 1 (AI 1): MS Word shortcuts, rapid navigation, caret blink, ribbon state sync (AppForm.cs, WriterView.cs).
   - Milestone 2 (AI 2): Ribbon 1px seam elimination, active tab bridge, DPI scale alignment (RibbonBar.cs).
   - Milestone 3 (AI 3): 60+ FPS Radial Arc Launcher (NewTabRadialLauncher.cs) — COMPLETED & VERIFIED.
   - Milestone 4: Integration, Multi-Agent Review, Adversarial Challenge, Forensic Audit & Acceptance Verification.
2. **Dispatch & Execute**:
   - Dispatch Worker AI 1 and Worker AI 2 in parallel with strict file write ownership.
   - Wire integration with AI 3's NewTabRadialLauncher.
   - Dispatch Reviewers, Challengers, and Forensic Auditor.
   - Run Gate check.
3. **On failure**:
   - Retry: nudge stuck agent or re-send task
   - Replace: spawn fresh agent with partial progress
   - Skip: proceed without (only if non-critical; NEVER skip auditor)
   - Redistribute: split stuck agent's remaining work
   - Redesign: re-partition decomposition
   - Escalate: report to Sentinel
4. **Succession**: Self-succeed at 16 spawns if context is heavy.
- **Work items**:
  1. Initialize orchestrator state & heartbeat [done]
  2. Dispatch Worker AI 1 (Shortcuts & Navigation & AppForm launcher hooks) [in-progress]
  3. Dispatch Worker AI 2 (Ribbon Seam & Tab launcher hook) [in-progress]
  4. Collect Worker handoffs & verify builds [pending]
  5. Multi-Agent Review (2 Reviewers), Empirical Verification (2 Challengers), Forensic Audit (1 Auditor) [pending]
  6. E2E Verification & Acceptance Audit (8 tables non-regression, 60+ FPS launcher, shortcuts, seam) [pending]
  7. Final Report to Sentinel [pending]
- **Current phase**: 2
- **Current focus**: Monitoring Worker AI 1 and Worker AI 2

## 🔒 Key Constraints
- NEVER write, modify, or create source code files directly.
- NEVER run build/test commands yourself — require workers to do so.
- NEVER investigate or explore at the code level — dispatch Explorers/Workers.
- Strict Guardrails (R4): Absolutely DO NOT break or regress core Writer features (8 tables in 'Bài 1.ND GHI.docx', in-cell editing, images, wrap text).
- Clean compile GenOffice.exe with 0 errors via build.bat using C# 5.0 (csc.exe).
- Binary veto: If Forensic Auditor reports INTEGRITY VIOLATION, milestone fails immediately.

## Current Parent
- Conversation ID: f4048f5c-6eff-468b-85c9-77346470ee0c
- Updated: 2026-09-17T14:10:00+07:00

## Key Decisions Made
- Milestone 3 (NewTabRadialLauncher.cs) is confirmed complete and verified from Worker AI 3 handoff.
- Worker AI 1 exclusively owns src/ui/AppForm.cs and src/ui/WriterView.cs, and handles form-level integration of NewTabRadialLauncher.
- Worker AI 2 exclusively owns src/ui/RibbonBar.cs, and handles ribbon-level '+' button click routing to NewTabRadialLauncher.Toggle.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| worker_ai1 | teamwork_preview_worker | M1 Shortcuts, Navigation, AppForm integration | in-progress | dabd8d0f-fa86-4e4a-9669-53c466b00a41 |
| worker_ai2 | teamwork_preview_worker | M2 Ribbon Seam & Ribbon '+' integration | in-progress | 00793bc2-e6c1-400a-8349-bdfa113a4aee |

## Succession Status
- Succession required: no
- Spawn count: 2 / 16
- Pending subagents: dabd8d0f-fa86-4e4a-9669-53c466b00a41, 00793bc2-e6c1-400a-8349-bdfa113a4aee
- Predecessor: teamwork_preview_orchestrator_1
- Successor: not yet spawned

## Active Timers
- Heartbeat cron: task-34 (every 10m)
- Safety timer: scheduled (600s)

## Artifact Index
- ORIGINAL_REQUEST.md — Authoritative user requirements
- PROJECT.md — Architecture, milestones, contracts, code layout
- .agents/teamwork_preview_worker_m3/handoff.md — Completed M3 handoff
- .agents/teamwork_preview_explorer_survey_ai1/handoff.md — Survey blueprint for AI 1
- .agents/teamwork_preview_explorer_survey_ai2/handoff.md — Survey blueprint for AI 2
