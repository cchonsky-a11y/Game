# BUILD_GUIDE.md — How to Build The Butterfly Effect

**Version:** 1.0 · **Date:** September 25, 2026 · **Aligned with:** GDD v1.3, VISION.md, SYSTEMS.md, PROTOTYPE_SCOPE.md (P0)

---

## 0. BLUF

This guide keeps the design documents, the code, and Claude Code synchronized from the first prototype onward.

- **Five documents, one precedence order.** Scope decides *what* to build now; Systems decides *how it works*; Vision decides *why*; the GDD explains background.
- **Every change follows one path:** SYSTEMS.md → GDD → DECISIONS.md → version bump. Numbers are never changed in only one place.
- **Build in gated milestones:** Setup → P0 → P2 → P3 → P4 → P5. Each has pass and kill criteria. Nothing moves forward on a failed gate.
- **Claude Code works only from PROTOTYPE_SCOPE.md.** The full GDD is reference, never a build list.
- **Timeline to a playable first jump:** about 4–8 months part-time (Section 6).

---

## 1. The Document Set

| Document | Purpose | Changes | Owner |
|---|---|---|---|
| **VISION.md** | Pitch, pillars, constitution, what must be protected | Rarely; only by deliberate decision | Corey |
| **SYSTEMS.md** | Canonical rules, formulas, and numbers | When a rule or tuning value changes | Corey, with Claude Code proposing |
| **PROTOTYPE_SCOPE.md** | The only work currently in scope, with an explicit out-of-scope list | Rewritten at the start of each milestone | Corey |
| **butterfly-effect-gdd.md** (and the Word version) | Full design with reasoning and research | When SYSTEMS or VISION changes | Corey |
| **DECISIONS.md** | Dated log of every decision and its reason, including rejected feedback | Every decision | Corey |
| **BUILD_GUIDE.md** | This process | When the process changes | Corey |
| **CLAUDE.md** | Instructions Claude Code reads at the start of every session | When scope or conventions change | Corey |

### Precedence when documents disagree

| Question | Document that wins |
|---|---|
| Should this be built now? | PROTOTYPE_SCOPE.md |
| How does this system work? What is the number? | SYSTEMS.md |
| Does this feature fit the game? | VISION.md (pillars and constitution) |
| Why was it designed this way? | GDD |

**If two documents conflict, stop.** Record the conflict in DECISIONS.md, resolve it, update both documents, and only then continue building. Claude Code is instructed to stop and ask rather than guess.

---

## 2. Synchronization Rules

### 2.1 Changing a rule or number
1. Update **SYSTEMS.md** first.
2. Update the matching **GDD** section and **Appendix A (Key Numbers)** in the same commit.
3. Update **data/tuning.json** if the value is used in code (see 2.3).
4. Add a **DECISIONS.md** entry (date, change, reason, evidence).
5. Bump the GDD version (1.2 → 1.3 for design changes; 1.2.1 for corrections).

### 2.2 Changing scope
1. Edit **PROTOTYPE_SCOPE.md** only; move items between "In scope" and "Out of scope" explicitly.
2. Add a DECISIONS.md entry.
3. Never expand scope in the middle of a milestone without writing it down first.

### 2.3 Numbers in code
- All tuning values live in **data/tuning.json**, never hard-coded.
- Each key in tuning.json maps to a line in SYSTEMS.md (e.g., `debt.compoundRate = 0.05` ↔ SYSTEMS.md §6).
- A unit test checks the key values that SYSTEMS.md specifies (e.g., institution decay after 250 years). If a test fails after a tuning change, the documents and code are out of sync.

### 2.4 Outside feedback (other AI tools, playtesters, reviewers)
1. Evaluate each point against the pillars and constitution, not on who suggested it.
2. Sort into: **new and worth it**, **already in the design**, **decline (with reason)**, or **test first**.
3. Record the verdicts in DECISIONS.md, including declined points, so they aren't re-litigated.
4. Apply accepted changes through 2.1 or 2.2.

### 2.5 After each milestone
1. Record results in **playtests/** and DECISIONS.md.
2. Apply any design changes through 2.1.
3. Rewrite PROTOTYPE_SCOPE.md for the next milestone.
4. Update CLAUDE.md if conventions changed.

---

## 3. Toolchain

| Tool | Needed from | Purpose |
|---|---|---|
| Mac with Apple silicon | Setup | Required for iOS builds later; runs everything else now |
| Git and a private GitHub repository | Setup | Version control for code and documents together |
| .NET SDK (current LTS) | Setup | Building and testing the C# simulation |
| An editor (VS Code or JetBrains Rider) | Setup | Reviewing and editing code |
| Claude Code | Setup | Writing code from the scope documents |
| Unity 6 | P4 | Presentation layer |
| Xcode | P4 | iOS builds and device testing |
| Apple Developer Program membership | P4 | Installing on devices and TestFlight |

---

## 4. Repository Structure

```
butterfly-effect/
├── CLAUDE.md                     # agent instructions (imports the docs below)
├── docs/
│   ├── VISION.md
│   ├── SYSTEMS.md
│   ├── PROTOTYPE_SCOPE.md
│   ├── DECISIONS.md
│   ├── BUILD_GUIDE.md
│   └── butterfly-effect-gdd.md
├── data/
│   ├── tuning.json               # every tuning value, mapped to SYSTEMS.md
│   └── content/                  # projects, institutions, echoes, text templates
├── src/
│   ├── Butterfly.Core/           # .NET Standard 2.1 library (reusable in Unity)
│   ├── Butterfly.Console/        # text game for human players
│   └── Butterfly.Batch/          # automated strategy runner
├── tests/
│   └── Butterfly.Core.Tests/
├── playtests/                    # notes, verbatim tester quotes, batch reports
└── unity/                        # added at P4
```

**Why the core targets .NET Standard 2.1:** Unity supports it, so the same simulation code moves into Unity at P4 without a rewrite.

---

## 5. CLAUDE.md

Claude Code automatically loads a project CLAUDE.md (at `./CLAUDE.md` or `./.claude/CLAUDE.md`) at the start of each session, and a CLAUDE.md can pull in other files with `@path` imports. Run `/context` in a session to confirm it loaded. A ready-to-use CLAUDE.md is provided alongside this guide. Its key rules:

1. Build only what PROTOTYPE_SCOPE.md lists as in scope.
2. SYSTEMS.md is the source of truth for rules and numbers; values go in data/tuning.json.
3. If documents conflict or a rule is missing, stop and ask.
4. The simulation must be deterministic; all randomness through the seeded generator.
5. Every formula gets a unit test; run all tests before finishing a task.
6. Never add language-model integration, graphics, or out-of-scope systems.
7. Hard constraints in SYSTEMS.md §14 are never violated.

---

## 6. Roadmap and Gates

| Phase | Deliverable | Time (part-time, assumed) | Gate question |
|---|---|---|---|
| **Setup** | Repo, docs, CLAUDE.md, empty solution with one passing test | 1 weekend | Does Claude Code read the scope and build the skeleton correctly? |
| **P0 Butterfly Test** | Text game + batch runner | 1–3 weekends + testing | Do players feel their actions changed the world and want to continue? |
| **P1 Playable Game Structure** (added 2026-10-02) | Vertical-slice architecture in the C# core: monthly turns, menu sections, scene routing, commissions and ledger, relationship-first institutions, capability network, NPC autonomy, jump echoes | — | Can a fresh player arrive, build relationships, take paid work, start a Grand Challenge, earn institution access, jump and see lasting consequences through the intended structure? (General human testing moves to a graphical milestone. Exception, 2026-10-04: a console experience validation of the first return with about 5 testers; see PROTOTYPE_SCOPE.md. If the return fails it, rework it before graphical UI.) |
| **P2 Graphical Vertical Slice** (2026-10-05, Corey) | Unity 6 presentation of the P1 game over the same Core: Rome, people, work, machine, departure, jump and the graphical first return, with placeholder art | — | Can a non-developer play from AD 155 through the first return without a command line? Then the first-return human test (about 5 people) runs on it. |
| **Fuel Puzzle** (formerly P2; renumbering is Corey's call) | Paper or spreadsheet puzzle | 1–2 weekends (can overlap P0 testing) | Does deduction beat guessing, and is it fun? |
| **P3 Headless Core** | Full-scale simulation without graphics | 3–6 weeks | Deterministic, performant, readable "Why?" at full scale? |
| **P4 First Hour** | Unity vertical slice of the first hour in Rome | 2–3 months | Do the minute 10 / 30 / 60 feelings land on an iPhone? |
| **P5 First Jump** | Rome ending → time-lapse → Era 2 arrival | 1–2 months | Is the arrival the payoff? |
| **Decision point** | Evaluate results | — | Continue solo with placeholders, bring in partners or artists, or stop |

**Cumulative time (upper and lower bounds, part-time):**

| Step | Low | High |
|---|---|---|
| Setup | 1 weekend | 1 weekend |
| P0 | +1 weekend | +3 weekends |
| P2 (overlaps P0 testing) | +0 | +2 weekends |
| P3 | +3 weeks | +6 weeks |
| P4 | +2 months | +3 months |
| P5 | +1 month | +2 months |
| **Total** | **≈ 4 months** | **≈ 7–8 months** |

Beyond P5 (eras 2–8, art, monetization, TestFlight, launch) is intentionally not planned in detail until the core is proven.

---

## 7. Milestone Workflow with Claude Code

For every milestone:

1. **Scope.** Rewrite PROTOTYPE_SCOPE.md for the milestone. Commit.
2. **Plan.** Ask Claude Code to read CLAUDE.md and the scope file and propose an implementation plan broken into small slices. Review it against the out-of-scope list before approving.
3. **Build in slices.** One slice per session (e.g., "event log," "debt accrual," "institution decay"). Each slice ends with tests passing and a commit.
4. **Verify determinism** after every slice that touches the simulation.
5. **Run the batch runner** before any human test. Fix dominant strategies first.
6. **Playtest** with humans (Section 9).
7. **Record** results and decisions (Section 2.5).
8. **Gate review.** Pass, fix and retest, or kill.

**Example slice order for P0:**

| # | Slice | Done when |
|---|---|---|
| 1 | Seeded generator, time steps, event log | Same seed produces an identical log hash |
| 2 | Care domains, priorities, expectations, debt | Compounding test matches SYSTEMS.md |
| 3 | Projects and gold | Projects change domains; logged with causes |
| 4 | Plague with 3 warning stages | Stages appear before the outbreak in every run |
| 5 | Two institutions, leaders, decay, drift paths | Decay test matches the 250-year values |
| 6 | Attention, seeded choice, promise | Decisions consume Attention; promise conflicts with jump |
| 7 | Jump in decade steps, Echoes, four-beat arrival | Arrival shows all 3 Echoes |
| 8 | Index and `why` command | Geometric mean correct; "Why?" readable |
| 9 | Batch runner and report | 100 runs × 3 strategies with a summary table |

---

## 8. Testing Standards

| Test type | What it checks | When |
|---|---|---|
| Unit tests | Every formula (debt compounding, decay, Index, legacy) against SYSTEMS.md values | Every slice |
| Determinism test | Same seed + inputs → identical event log hash | Every slice touching simulation |
| Snapshot test | A saved reference playthrough still produces the same outcome | After P0 is stable |
| Batch balance report | Strategy win rates, crisis timing, institution outcomes | Before every human test |
| Human playtests | The milestone's gate question | End of each milestone |

**Reference values to test (from SYSTEMS.md):**

| Rule | Expected |
|---|---|
| Debt: gap 20 per year, 5% compounding, 3 years | 20 → 20 × 1.05 + 20 = 41 → 41 × 1.05 + 20 = 63.05 |
| Decay over 250 years from 80, bare (10% per decade) | 80 × 0.9^25 ≈ 5.7 |
| Chartered (3%) | 80 × 0.97^25 ≈ 37.4 |
| Strong (1%) | 80 × 0.99^25 ≈ 62.2 |
| Legacy: 1 student + written, no institution (**P3 or later**: legacy depends on mortality and succession, out of P0 scope) | 30 + 20 + 20 = 70% |

---

## 9. Playtest Protocol (P0)

- **Testers:** 5, ideally strategy players who haven't seen the design.
- **No coaching.** Observe; ask them to think aloud.
- **After the arrival, ask:**
  1. What were you choosing between during the Rome era?
  2. What changed in the world you came back to?
  3. Did any of that feel connected to you? Which part?
  4. Would you jump again to see what happens next?
- **Record answers verbatim** in playtests/, especially any sentence like the target in PROTOTYPE_SCOPE.md.
- **Score against the pass criteria** in PROTOTYPE_SCOPE.md; note which kill criteria, if any, were hit.

---

## 10. Definition of Done (every milestone)

- All in-scope items built; nothing from the out-of-scope list added.
- All tests pass, including determinism.
- Batch report and playtest notes saved in playtests/.
- Pass and kill criteria scored and recorded in DECISIONS.md.
- SYSTEMS.md, GDD, and tuning.json in sync (Section 2.1).
- PROTOTYPE_SCOPE.md rewritten for the next milestone.

---

## 11. Risk Register

| Risk | Early sign | Mitigation |
|---|---|---|
| The jump and arrival aren't compelling | P0 testers don't mention the returned world | Kill criterion: redesign before building further |
| Players click instead of choosing | Testers can't say what they chose between | Cut decisions before adding systems |
| Scope creep through the coding agent | Code appears for out-of-scope systems | CLAUDE.md rules; review plans against the out-of-scope list |
| One strategy dominates | Batch win rate above 65% | Rework debt or expectations before human tests |
| Documents drift apart | Tests fail after a tuning change; conflicting numbers | Sync rules (Section 2); stop on conflicts |
| Solo production limits | Art and content needs grow at P4–P5 | Placeholder art; decision point after P5 |
| Apple platform changes | Framework or OS requirements shift | Keep Apple-specific code in the native bridge only |

---

## 12. Session Checklist

**Before a Claude Code session**
- [ ] Scope file current for this milestone
- [ ] Pick one slice
- [ ] `git pull`, all tests passing

**After a session**
- [ ] Tests pass, including determinism
- [ ] No out-of-scope code added
- [ ] Any number changed? Sync SYSTEMS.md, GDD Appendix A, tuning.json
- [ ] Any decision made? Add to DECISIONS.md
- [ ] Commit with a clear message
