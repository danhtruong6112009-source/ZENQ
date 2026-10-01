# BRIEFING — 2026-09-21T13:00:00Z

## Mission
Xây dựng và hoàn thiện hệ thống bóc tách, chuẩn hóa OpenXML từ Microsoft Word và đồng bộ hóa engine kết xuất đồ họa Canvas trong GenOffice (100% kích thước, tỷ lệ vòng tròn 1:1, viền, 200+ SmartArt, Shapes, màu sắc, font chữ và OMML math).

## 🔒 My Identity
- Archetype: teamwork_preview_orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: c:\Users\Danh\Desktop\genofficepy\.agents\teamwork_preview_orchestrator_5
- Original parent: parent
- Original parent conversation ID: 2a0da693-b35b-4873-8671-2a39d8154e3c

## 🔒 My Workflow
- **Pattern**: Project
- **Scope document**: c:\Users\Danh\Desktop\genofficepy\PROJECT.md
1. **Decompose**: Survey -> Milestones (R1 Schema & Analysis, R2 Ground Truth Word Generator/Dataset, R3 Parser & Canvas Renderer, Final E2E)
2. **Dispatch & Execute**: Dispatch Explorers -> Workers -> Reviewers -> Challengers -> Auditors
3. **On failure**: Retry -> Replace -> Skip -> Redistribute -> Redesign -> Escalate
4. **Succession**: At 16 spawns, write handoff.md, spawn successor
- **Work items**:
  1. Survey & Technical Exploration [done]
  2. Master Project Plan & Architecture Decomposition [in-progress]
  3. Milestone 1: OpenXML DrawingML/SmartArt/OMML Schema Extraction & Parser Upgrade [pending]
  4. Milestone 2: 200+ SmartArt/Shapes/Borders/Math Word Ground Truth Generator & Test Infra [pending]
  5. Milestone 3: Canvas Renderer Upgrade (Vòng tròn 1:1, gắn kết không khe hở, viền & màu) [pending]
  6. Milestone 4: Dual Track E2E Verification & Adversarial Hardening [pending]
- **Current phase**: 1 (Synthesis & Planning)
- **Current focus**: Update PROJECT.md and TEST_INFRA.md based on survey findings

## 🔒 Key Constraints
- NEVER write, modify, or create source code files directly.
- NEVER run build/test commands yourself — require workers to do so.
- NEVER investigate or explore the problem at the code level — dispatch Explorers.
- Only edit metadata/state files (.md) in .agents/ folder.
- Never reuse a subagent after it has delivered its handoff.
- Binary veto on integrity violations from auditor.

## Current Parent
- Conversation ID: 2a0da693-b35b-4873-8671-2a39d8154e3c
- Updated: 2026-09-21T13:00:00Z

## Key Decisions Made
- Initiated project survey with 3 parallel Explorers / Spec Miners to investigate C:\Users\Danh\Desktop\xml, ECMA-376 DrawingML/dgm/oMath, and current GenOffice SmartArt/Shape/Renderer code.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| spec_miner_1 | teamwork_preview_spec_miner | Survey C:\Users\Danh\Desktop\xml & ECMA-376 | completed | f52aa09c-2c1c-43b0-a677-581b4ed1cd48 |
| explorer_2 | teamwork_preview_explorer | Survey GenOffice Codebase & Renderers | completed | 737d2a7d-ef5c-4286-9890-edb3eca99dd3 |
| explorer_3 | teamwork_preview_explorer | Survey Word Sample Generator & Ground Truth | completed | 350c73f7-28c7-4f9c-ad35-1e4b8b703504 |
| worker_m1 | teamwork_preview_worker | Milestone 1: OpenXML Parser & Schema Engine | completed | 7ff46a51-034d-4948-bb33-81b649c12385 |
| worker_m2 | teamwork_preview_worker | Milestone 2: Word Sample Generator & Ground Truth | in-progress | 7930edf0-ec75-4b7b-b172-b73db66ff8c5 |
| worker_m3 | teamwork_preview_worker | Milestone 3: Canvas Renderer Upgrade | completed | 30820df5-1c35-4971-a63a-e271c3974e52 |

## Succession Status
- Succession required: no
- Spawn count: 6 / 16
- Pending subagents: 7930edf0-ec75-4b7b-b172-b73db66ff8c5
- Predecessor: none
- Successor: not yet spawned

## Active Timers
- Heartbeat cron: 04fc9965-4b0a-4fc5-9553-20c2a9f1ef15/task-18
- Safety timer: none
- On succession: kill all timers before spawning successor
- On context truncation: run `manage_task(Action="list")` — re-create if missing

## Artifact Index
- .agents/teamwork_preview_orchestrator_5/DISPATCH.md — Initial dispatch prompt
- .agents/teamwork_preview_orchestrator_5/BRIEFING.md — Persistent working memory
- .agents/teamwork_preview_orchestrator_5/plan.md — Concrete execution plan
- .agents/teamwork_preview_orchestrator_5/progress.md — Liveness & status tracking
- PROJECT.md — Master project scope & architecture
