# DECISIONS.md — Decision Log

> Every design decision, with its reason. Includes rejected suggestions so they aren't re-litigated.
> Format: date · decision · reason · source.

## 2026-09-25 — Design phase (GDD v1.0)
- **Start in Rome, AD 155.** Ten years before the Antonine Plague gives the first era a known catastrophe to prepare for. Source: design process.
- **8 playable eras; the 8th jump returns home.** Source: Corey.
- **Hybrid turn-based time model; turns capped at 1 year; ~10 years per standard era.** Reconciles long history with a mortal inventor. Source: design process; Corey chose 10 years.
- **Influence through institutions only; Rome remains an AI actor.** Protects Pillar 3. Source: design process.
- **Gold as a share of the regional economy, not an interest-bearing balance.** Fixed interest over 400 years turns 100 gold into ~275,000. Source: design process.
- **Loss = Hazard × Exposure × (1 − Resilience).** Ties losses to player choices. Source: Corey's request; design process.
- **Abstracted combat with 3 decision points.** Source: design process.
- **Knowledge Web larger than Civilization VII (~865 visible nodes).** Source: Corey's original requirement.
- **Monetization: free through first arrival; one-time campaign unlock; heirloom perks both paid and earnable.** Source: Corey.
- **Unity 6 with an engine-independent C# core; iPhone 13 minimum; iOS 26.** Source: design process.

## 2026-09-25 — ChatGPT review (GDD v1.1)
Adopted: design constitution; promises; Echoes (as curation); institution leaders; Trust memories; ideological drift; Protect / Maintain / Accept Risk; templated projects; physical experiment results over a deducible core; crisis warning stages; failure creates history; descriptive endings with the Index visible; "Influence Mode"; Journal teaches strategy; early succession hint; "healthy years" framing; split development documents; P0 Butterfly Test.

Declined:
- **Cut the visible web to 200–300 concepts.** Conflicts with Corey's requirement.
- **Remove paid heirloom perks.** Corey's decision: keep paid and earnable.
- **Card-first iPhone UI.** Conflicts with the map-centered art direction; replaced by a map home screen with a "matters that need you" tray.
- **PC as a first-class platform.** Scope creep; Apple platforms only.
- **Random mortality instead of a lifespan ceiling.** Breaks life-budget planning.
- **Higher price.** Speculative until production quality is known.
- **Split expectations into social and physical now.** Deferred to P0 testing.

## 2026-09-25 — Post-jump tracing removed (GDD v1.2)
- **No required causal tracing after jumps.** Corey clarified the intent: player actions change the world; the game doesn't have to prove the link. In-era "Why?" remains for decision-making. Arrival reduced to four beats with an optional Learn more; Chronicle Threads tab removed; event log records immediate causes only.

## 2026-09-25 — Grok review (GDD v1.2)
Adopted: smaller P0 (3 domains, 2 institutions, 8–12 decisions); "what were you choosing between?" test; hour-one seeded choice always an Echo in Era 2; pre-authored drift paths in P0; sensitive-history rules enforced in code.

Already in the design (no change): specific Echoes; web opening on the local problem; one puzzle structure across eras; costly fallbacks; ending montage; lifeline display.

Declined:
- **Visibility ramps in only after Stage 2.** Would remove the plague-warning dilemma in Rome; the 2-year grace period already protects new players.
- **Traceable arrival as the top priority.** Conflicts with Corey's intent.
- **Split expectations now.** Incentives to improve outweigh the exploit; test in P0.

Test first: free slice ending at arrival vs. extending ~20 minutes into Era 2.

## 2026-09-25 — Build guide (GDD v1.2.1)
- **Timeline corrected to about 4–8 months part-time** to a playable first jump, matching the step-by-step estimate in BUILD_GUIDE.md §6.

## 2026-09-26 — P0 build (approved by Corey, 2026-09-26)
- **P0 simulation, console game and batch runner built** in slices 1–9 of BUILD_GUIDE §7. Source: Claude Code.
- **Placeholders for unspecified numbers and rules** recorded in docs/P0_PROPOSALS.md (P0-01 to P0-14) and marked `PROPOSED` in data/tuning.json. None are canonical until approved; approved items move into SYSTEMS.md, the GDD and this log per BUILD_GUIDE §2.1. Source: Claude Code.
- **First batch findings** (playtests/batch-report.md): accepted risk reaches Strained in 2 years (pass); plague warnings always precede the outbreak (pass). Pooled over both jump timings, Balanced 52% vs Specialized 48% (pass). Split by timing, Specialized dominates early jumps and Balanced dominates late jumps (fail). This awaits a decision on how to read the 65% criterion (P0_PROPOSALS open question 4). Source: batch runner.
- **Found during tuning:** applied literally over a 250-year absence, the §6 rules produce crisis spirals. Damped with placeholders: a crisis releases all debt and resets expectations, domains converge faster to baseline, and recurrence chances are lower. Open questions 1–2 in P0_PROPOSALS. Source: batch runner.
- **Not built:** the legacy formula (BUILD_GUIDE §8 reference values). It belongs to mortality and succession, which are out of P0 scope. Source: PROTOTYPE_SCOPE.

## 2026-09-26 — Review of P0_PROPOSALS (Corey)
- **Balance gate:** 65% cap applies per jump timing. A second gate requires that neither timing wins more than 65% of runs across all strategies. Both now pass (Early 34 / 61 / 5; Late 58 / 42 / 0; timing Early 37% vs Late 63%). Source: Corey; batch runner.
- **Debt:** compounding stops 30 years after departure. A crisis still clears debt and resets expectations. The rarer-recurrence damping was removed. The jump confirmation still lists debts. Source: Corey.
- **Plague:** odds use Medicine debt only; Governance and Economy debt tiers raise severity. Source: Corey.
- **Legend stand-in:** a loyal leader at departure counts toward "strong", **P0-only**. Source: Corey.
- **Pacing (P0-only, in PROTOTYPE_SCOPE, not SYSTEMS):** 6-month turns, 20 per era, auto-advance, Attention demand ≥ 1.5× supply. Source: Corey.
- **Scope change:** institution gold (endowments grow with the economy and pay domain debt for 30 years) and institutional corruption (audit charter, leader integrity, three severities) were added to P0. The out-of-scope item "Fortune shares, loss events other than the plague" was narrowed accordingly. Source: Corey.

## 2026-09-26 — Second review (Corey): approved and applied
- **Approved:** the P0 build entry and the first-review decisions above. The canonical docs are now updated by Claude Code with Corey's authorization: SYSTEMS.md §6 (30-year compounding cap; Medicine-only plague odds; Governance and Economy raise severity; a crisis resets expectations), §7 (leader integrity, institution gold, debt payment during absence, corruption, audit charter, pre-jump risk bands, Discovery reveal), §9 ("never a balance" replaced: a balance growing with the economy during the 30-year window, frozen afterward in P0, full share model at P3); PROTOTYPE_SCOPE (P0-only pacing, endow and audit actions, corruption and institution gold in scope, 5 batch strategies, debt-at-departure criterion); GDD v1.3 Appendix A; BUILD_GUIDE §8 (legacy test deferred to P3 or later). Source: Corey.
- **C7:** the pre-jump briefing shows corruption risk as Low / Medium / High (thresholds 5% and 10% per decade, tune) and the potential severity mix, never the outcome. The Discovery beat reveals any corruption and its level. Source: Corey.
- **Batch:** Endow and Split strategies added; the report now covers Attention demand vs. supply, debt retired by institutions, holdings, corruption rate and level mix, the Endow / Pay-down / Split gate, and the audit-charter flag (above ~80% of the best runs). Source: Corey.

## 2026-09-27 — Automated-playtest findings G1–G8 (Corey)
- **G1, applied (root fix):** after the 30-year window, a domain that no institution maintains stops accruing new debt and drifts toward the historical baseline. There is no plague outbreak within 30 years of the last one. SYSTEMS.md §6 and tuning.json updated. Reason: the crisis spiral made most arrivals the same collapsed Rome. Source: Corey; playtests/ai/bugs.md.
- **Timing gate: not applicable to P0, deferred to P3.** "Neither timing wins more than 65%" doesn't apply until staying longer has a cost (aging and the risk of the machine being discovered). The strategy gate within each timing stays. Source: Corey.
- **G3, applied:** "End turn" advances one turn; "Wait" advances until something needs the player. Only turns where no action is possible pass on their own. PROTOTYPE_SCOPE updated. Source: Corey.
- **G4, applied:** a plague must always lower the Index in the medium term. Verified: lower after about 10 years in 200 of 200 counterfactual runs, so no damage increase was needed. The outbreak now ends with "The sickness burns itself out. Nothing is fixed; there are simply fewer people left, and the survivors expect less." Source: Corey; PlagueImpactTests.
- **G5, declined:** corruption risk stays as bands only. Source: Corey.
- **G7, applied:** the game-start line explains the prototype's scope. Source: Corey.
- **G8, applied:** plague severity is labeled by the share of the population that dies (Contained under 5%, Severe 5–15%, Catastrophic over 15%), always with the death toll. SYSTEMS.md §6 updated. Source: Corey.
- **G2, G6:** no change; watch in human testing. Source: Corey.
- **Finding after G1 (for decision):** arrivals now converge. Every strategy, Neglectful included, arrives at a mean Index of about 100, and Neglectful wins 21–31% of seeds. A test asserting that neglect is never best on average (under 20%) now fails and has been left failing. Source: batch runner.
