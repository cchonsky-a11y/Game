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

## 2026-09-27 — Long-run target after the window (Corey)
- **Decision (option 2):** after the 30-year window, each domain drifts 25% per decade toward a long-run target = historical baseline + 0.3 × (departure level − baseline at departure), above or below. Surviving maintaining institutions add their maintenance on top. SYSTEMS.md §8 (rule) and §6 (reference), tuning `jump.longRun.*`. Reason: with pure convergence to the baseline, every strategy arrived at an Index of about 100. Source: Corey.
- **Result** (playtests/batch-report.md): arrivals vary (arrival Index SD 17.6; Wrongness spread across six variants, "as history" 17%). Investing strategies beat Neglectful by 38.7 Index points on average (Early 31.8, Late 45.7) and on 100% of seeds. The late strategy gate passes. **The early strategy gate fails:** Specialized wins 89% among Balanced/Specialized/Neglectful and 74% among all five. Awaiting a decision; nothing tuned. Source: batch runner.

## 2026-09-27 — Stakes, influence and rival institutions (Corey)
- **Decision (build now):** you no longer found the Circle, the faction or the guild; they already exist and you buy into them. The first purchase is **1%** (a member, no say). **10%** counts toward influence but gives no oversight; **25%** gives a voice (priorities, policy, a plague response); **50%** is needed to oversee and control. Each 1% costs more than the last, and the price depends on the domain. Founding your own institution costs about **65%** of buying 50% of an established one in the same domain: you control it, but it starts weak, has little hold over its domain, and is more likely to fail; you build it up with time and gold. A couple of rivals per domain compete for control and influence. Source: Corey.
- **Built:** 9 institutions (2 established + 1 own per domain); `buy`, `found`, `invest`, `institutions` commands; influence = Σ control factor × domain share; sway = min(1, 2 × influence) scales priorities, policy and their upkeep; absence maintenance scales the same way; own institutions may fail below strength 25; rivals strike a dominant institution you control. Amounts proposed as P0-18. SYSTEMS §5, §7, §9; PROTOTYPE_SCOPE; GDD Appendix A; tuning `stakes.*`, `founding.*`, `rivalry.*`.
- **Batch result** (100 runs): gate A fails late (Balanced 82% vs Specialized 17%; early 34 / 55 / 11 passes); gate B passes in both timings (late: Balanced, Endow and Split 25% each, Specialized 15%, FreeMarket 9%); gate C passes. FreeMarket no longer dominates. Control is expensive, so few institutions are chartered and endowed; most arrive Dissolved. Specialized's own school failed in 9% of runs. Nothing tuned; awaiting a decision. Source: batch runner.

## 2026-09-27 — Early-gate decision: plague foreknowledge for all strategies (Corey)
- **Decision:** every scripted strategy knows the plague arrives around AD 165 and responds from turn one in its own style: Balanced, Endow and Split Protect Medicine and Maintain the rest; Specialized keeps its Medicine focus; Neglectful keeps Medicine at Maintain and Accepts Risk elsewhere. Medicine's protective effect is unchanged. Scripts will not be adjusted again just to pass the gate. Source: Corey.
- **Result** (playtests/batch-report.md): among Balanced / Specialized / Neglectful, **Early 87 / 13 / 0** and **Late 79 / 21 / 0**. Among all five strategies no one exceeds 65% (Early: Balanced 51%, Endow 23%, Split 23%, Specialized 3%; Late: Balanced 40%, Endow 30%, Split 18%, Specialized 13%). Investing beats Neglectful by 38.6 Index points (Early 31.3, Late 45.9) on 100% of seeds. Arrival Index SD 17.3.
- **Recorded as a P0 artifact (early jumps):** with a single crisis type, the strategy that prepares best for that one known crisis dominates. Revisit in P3 with multiple crises. Not tuned further, as decided. Source: Corey.
- **Late jumps:** see the next entry. Source: batch runner.

## 2026-09-27 — Late-jump dominance recorded as the same P0 artifact (Corey)
- **Rule:** record late-jump dominance as the same P0 artifact only if (1) the dominant strategy leans toward Medicine, (2) its win rate is 80% or less, and (3) Neglectful never dominates and the invest-vs-neglect gap favors investing. Otherwise stop and report. Source: Corey.
- **Late-jump win rates** (playtests/batch-report.md, 100 seeds): among Balanced / Specialized / Neglectful, **79% / 21% / 0%**. Among all five strategies: Balanced (Pay-down) 40%, Endow 30%, Split 18%, Specialized 13%, Neglectful 0%. **Dominant strategy: Balanced (Pay-down).**
- **Check:** (1) Balanced leans toward Medicine: since the foreknowledge decision it Protects Medicine from turn one (its only Protect priority) and Maintains the rest. ✓ (2) 79% ≤ 80%. ✓ (3) Neglectful wins 0% of late jumps; investing beats Neglectful by 45.9 Index points on late jumps, on 100% of seeds. ✓
- **Decision:** all three hold, so late-jump dominance is recorded as the same P0 artifact as early jumps (a single crisis type rewards preparing for the one known crisis). Revisit in P3 with multiple crises. No tuning. Source: Corey; batch runner.

## 2026-09-27 — No passive income before institutions (Corey, from play)
- **Decision:** the inventor doesn't benefit from the economy without leverage. Gold comes from work (better-paid work costs more Attention), from owned property (workshop, warehouses; scales with Economy) and from institutions. Each institution earns at its own rate, scaled by loyalty and strength, so margins are tight until it is established. It pays its own running cost (different per institution) and the upkeep of the domain it maintains (option A). Its surplus goes to the inventor; its shortfall is covered by the inventor. Priorities work from the start. SYSTEMS.md §9, PROTOTYPE_SCOPE, tuning, P0-16. Source: Corey.
- **G4 check:** under the new economy the plague raised the Index in 2 of 200 counterfactual runs, so plague level damage was raised 20% per the G4 rule; it now lowers the Index in 200 of 200. Source: PlagueImpactTests.
- **Batch result:** investing beats Neglectful by 28.9 Index points on 100% of seeds (Neglectful wins 0%). **All three balance gates fail:** Balanced (Pay-down) dominates early jumps (96% among the scope three, 69% among all five), Endow dominates late jumps (93% among all five), and Specialized collapses (Index at departure 28–66). Awaiting a decision; nothing tuned. Source: batch runner.

## 2026-09-27 — Stakes, influence and rival institutions (Corey)
- **Decision (build now):** you no longer found the Circle, the faction or the guild; they already exist and you buy into them. The first purchase is **1%** (a member, no say). **10%** counts toward influence but gives no oversight; **25%** gives a voice (priorities, policy, a plague response); **50%** is needed to oversee and control. Each 1% costs more than the last, and the price depends on the domain. Founding your own institution costs about **65%** of buying 50% of an established one in the same domain: you control it, but it starts weak, has little hold over its domain, and is more likely to fail; you build it up with time and gold. A couple of rivals per domain compete for control and influence. Source: Corey.
- **Built:** 9 institutions (2 established + 1 own per domain); `buy`, `found`, `invest`, `institutions` commands; influence = Σ control factor × domain share; sway = min(1, 2 × influence) scales priorities, policy and their upkeep; absence maintenance scales the same way; own institutions may fail below strength 25; rivals strike a dominant institution you control. Amounts proposed as P0-18. SYSTEMS §5, §7, §9; PROTOTYPE_SCOPE; GDD Appendix A; tuning `stakes.*`, `founding.*`, `rivalry.*`.
- **Batch result** (100 runs): gate A fails late (Balanced 82% vs Specialized 17%; early 34 / 55 / 11 passes); gate B passes in both timings (late: Balanced, Endow and Split 25% each, Specialized 15%, FreeMarket 9%); gate C passes. FreeMarket no longer dominates. Control is expensive, so few institutions are chartered and endowed; most arrive Dissolved. Specialized's own school failed in 9% of runs. Nothing tuned; awaiting a decision. Source: batch runner.

## 2026-09-27 — Start as a stranger: no domain responsibilities (Corey, from play)
- **Decision:** at the start you don't protect anything; you survive by working with what you know from the future. Work income is taxed (10%, placeholder) instead of you paying domain upkeep. You gain priorities over a domain only through an institution that maintains it, and that institution pays the upkeep (option A). Other domains run without you. SYSTEMS.md §5 and §9, PROTOTYPE_SCOPE. Source: Corey.
- **Batch result:** gates B (all five) and C (debt at departure) pass in both timings; gate A passes early (Balanced 56 / Specialized 17 / Neglectful 27) and **fails late (Balanced 82%)**, above the 80% limit for recording it as a P0 artifact. Investing beats Neglectful by 14.5 Index points (Early 5.4, Late 23.7) on 82% of seeds. The test "neglect is never best on average" fails (Neglectful wins 27% of early jumps among the scope three). Awaiting a decision. Source: batch runner.
- **Noted for design (not built):** buyable businesses, separate from institutions, that are less likely to survive jumps. Source: Corey.

## 2026-09-27 — Starting gold and the workshop stake (Corey)
- **Decision:** the inventor starts with no property and no income, only gold scavenged from the time machine (60). Choosing the workshop in the hour-one choice spends part of it to buy a stake in the smith's business, which pays about 0.1 gold a year per Economy point. Choosing the fountain gives no income. Intro and project text updated. Source: Corey.

## 2026-09-27 — Rome follows history until you can influence it (Corey, from play)
- **Decision:** a domain doesn't slip just because the inventor lacks influence. Without a hold it follows the historical curve; only the player's projects and the consequences of their choices (e.g. the broken fountain's fever) move it. With a hold, priorities act relative to history. **Number change:** Maintain went from −1 to ±0 a year; otherwise founding an institution and choosing Maintain would be worse than having no hold. SYSTEMS.md §5, PROTOTYPE_SCOPE, tuning. Source: Corey.
- **Batch result:** gates B and C pass; A passes early (61 / 17 / 22) and fails late (Balanced 81%, above the 80% artifact limit). Investing beats Neglectful by 13.3 Index points on 83% of seeds; the neglect test still fails. Open for decision. Source: batch runner.

## 2026-09-27 — Economic policy, merchants' guild, boom-bust; 3-month turns (Corey)
- **Decision (build now, option A):** the Economy improves as the player pushes Rome toward an Austrian approach. Policy is set through the senate faction; a new commercial institution (the Merchants' Guild of Ostia) maintains the Economy. Four issues (coinage, prices, property, taxes). Austrian stances are simply better, but those who benefit from intervention push back. Interventionist stances cause boom-bust cycles (a second crisis type, with 3 visible warnings). Scope expanded: 3 institutions, 4 Economy projects (mint audit), the bust crisis, 7 batch strategies. SYSTEMS.md §6 and §9, PROTOTYPE_SCOPE, GDD Appendix A, P0-17. Source: Corey.
- **Decision:** 3-month turns, 40 per era (P0-only); project and mentoring durations doubled in turns so they last the same time; Attention demand 250 vs supply 160 (1.6×). Source: Corey.
- **G4 check:** with 3-month turns the plague raised the Index in 14 of 200 counterfactual runs; plague level damage raised to 1.8× the original under the G4 rule, restoring 200 of 200. Source: PlagueImpactTests.
- **Batch result** (7 strategies): FreeMarket beats Interventionist on 75% of early and 100% of late seeds, arriving at Index 111 / 143 vs 102 / 33. FreeMarket wins 71% of late runs (gate B fails; 55% early). Interventionist busts very often (14.5 per run early, 22.7 late, mostly during the absence). Gate A fails in both timings (Balanced 67% early, 87% late). Investing beats Neglectful by 24.8 Index points on 85% of seeds. Arrival variety high (SD 31.0). Awaiting a decision; nothing tuned. Source: batch runner.

## 2026-09-27 — Stakes, influence and rival institutions (Corey)
- **Decision (build now):** you no longer found the Circle, the faction or the guild; they already exist and you buy into them. The first purchase is **1%** (a member, no say). **10%** counts toward influence but gives no oversight; **25%** gives a voice (priorities, policy, a plague response); **50%** is needed to oversee and control. Each 1% costs more than the last, and the price depends on the domain. Founding your own institution costs about **65%** of buying 50% of an established one in the same domain: you control it, but it starts weak, has little hold over its domain, and is more likely to fail; you build it up with time and gold. A couple of rivals per domain compete for control and influence. Source: Corey.
- **Built:** 9 institutions (2 established + 1 own per domain); `buy`, `found`, `invest`, `institutions` commands; influence = Σ control factor × domain share; sway = min(1, 2 × influence) scales priorities, policy and their upkeep; absence maintenance scales the same way; own institutions may fail below strength 25; rivals strike a dominant institution you control. Amounts proposed as P0-18. SYSTEMS §5, §7, §9; PROTOTYPE_SCOPE; GDD Appendix A; tuning `stakes.*`, `founding.*`, `rivalry.*`.
- **Batch result** (100 runs): gate A fails late (Balanced 82% vs Specialized 17%; early 34 / 55 / 11 passes); gate B passes in both timings (late: Balanced, Endow and Split 25% each, Specialized 15%, FreeMarket 9%); gate C passes. FreeMarket no longer dominates. Control is expensive, so few institutions are chartered and endowed; most arrive Dissolved. Specialized's own school failed in 9% of runs. Nothing tuned; awaiting a decision. Source: batch runner.

## 2026-09-28 — Rivals push back from 20%, harder with more control (Corey)
- **Decision:** rival pushback starts once the player holds 20% of an institution (not when an institution they control holds 40% of the domain), and grows with the stake. P0: each rival's chance a year = min(75%, 0.75% × stake%), 15% at 20%, 37.5% at 50%, 75% for an institution the player owns outright; −3 strength per strike. SYSTEMS §7, PROTOTYPE_SCOPE, P0-18, tuning `rivalry.*`. Source: Corey.
- **Batch result:** gate A still fails late (Balanced 84%, Specialized 15%); gates B and C pass. Specialized's own school now fails in 16% of late runs (9% early) and arrives Dissolved in all runs. Nothing tuned. Source: batch runner.

## 2026-09-28 — Correction: rivals react to domain share, not to your stake (Corey)
- **Clarified:** the 20% is the share of the **domain** an institution you founded holds, not your stake. Taking more control of an institution provokes little from its rivals; they push back when an institution takes more of the domain. Your own institution draws fire from 20% of its domain; an established one once it grows past the share it held at the start. P0: each rival's chance a year = min(75%, 10% + 2% per share point past the threshold), −3 strength. Supersedes the stake-based rule of the same day. SYSTEMS §7, PROTOTYPE_SCOPE, P0-18, tuning `rivalry.*`. Source: Corey.
- **Batch result:** gate A still fails late (Balanced 81%, Specialized 19%; early 29 / 57 / 14 passes); gates B and C pass. Specialized's school fails in the era in 9% of runs. Nothing tuned. Source: batch runner.

## 2026-09-28 — Jump range grows with knowledge; first jump 25–50 years (Corey)
- **Decision:** jumps start short and reach further as technology and knowledge advance. The first jump is about 25–50 years; smaller early jumps let the player make more adjustments at the start. Recorded in SYSTEMS §11 and GDD Appendix A. Source: Corey.
- **Not yet built:** the P0 prototype still jumps 250 years (PROTOTYPE_SCOPE "Jump"; arrival texts, the 30-year institution window and the long-run drift are written for that distance). Changing P0's jump needs a scope change and new arrival texts; awaiting a go-ahead. The rule for how range grows (what counts as knowledge, how far each step reaches) is still open.

## 2026-09-28 — One smith doesn't move Rome's economy (Corey, from play)
- **Decision:** the smith giving up no longer costs the Economy 4 points; one workshop is too small to move an economy the size of Rome. Causality runs the other way: if the workshop is unfunded, the smith leaves in AD 157 only if the Economy is below its historical value (logged with the Economy as a cause); otherwise he struggles on. Both outcomes are logged with the seeded choice as a cause, so the Echo is kept. Tuning `seededChoice.smithLoss` removed; P0-12. Source: Corey.
- **Batch result:** all applicable gates pass for the first time since the stakes model: A early 19 / 64 / 17, late 64 / 36 / 0 (both just under the 65% cap); B and C pass in both timings. Source: batch runner.

## 2026-09-28 — Local investments have local effects (Corey, from play)
- **Decision:** an investment doesn't move Rome's economy by itself; what comes of it does. The workshop no longer raises the Economy (was +6); it only gives you a share of the business (income that rises and falls with the Economy). The fountain repair is local too: Medicine +1 (was +8); its clean water still lowers plague hazard. The AD 157 fever from the broken fountain costs Medicine −1 (was −5). Projects with no Rome-wide effect still log their completion. data/content/projects.json, tuning `seededChoice.feverLoss`, P0-08 and P0-12. Source: Corey.
- **Batch result:** all applicable gates still pass (A early 16 / 63 / 21, late 61 / 36 / 3; B and C pass). Source: batch runner.

## 2026-09-28 — Requirements to join institutions (Corey)
- **Decision:** joining an established institution needs more than gold. Build the proposed list as is now; build it out more later. Requirements apply to the first purchase only: Circle, a Medicine project or the promise; sanctuary, nothing; Caecilian faction, patronage (2 consulting jobs or the patronage project); Junian faction, property; guild, a business of your own; bank, a first purchase of at least 5%. The two factions won't share a member (10%+). Your own institutions have none. The `institutions` list and `why <inst>` show each requirement and whether you meet it. SYSTEMS §7, PROTOTYPE_SCOPE, GDD Appendix A, P0-19, tuning `joining.*`. Source: Corey.
- **Batch result:** all applicable gates pass (A early 15 / 64 / 21, late 48 / 47 / 5; B and C pass). The Balanced scripts now rarely get into the faction (they seldom consult twice). Source: batch runner.

## 2026-09-28 — Entry fee and annual dues (Corey, from play)
- **Decision:** 1.5 gold for a seat in the Merchants' Guild was too cheap. Joining costs an entry fee (with very little influence to show for it), and members pay annual dues that depend on their influence. Built: entry fee per institution (sanctuary 5, Circle 10, guild 20, Junians 25, Caecilians 30, bank none, its 5% minimum deposit serving as one); dues = base (1–5 gold a year) + 0.1 gold a year per percent held, settled each turn; unpaid dues cost loyalty. Own institutions charge neither. Stake prices unchanged. SYSTEMS §7, PROTOTYPE_SCOPE, GDD Appendix A, P0-19, tuning `joining.*`. Source: Corey.
- **Batch result:** all applicable gates pass (A early 16 / 63 / 21, late 57 / 40 / 3; B and C pass). Source: batch runner.

## 2026-09-28 — Influence grows with time and gold; 20-year eras (Corey)
- **Decision:** guilds are made of people who have been in them for years, so influence grows with time or with heavy gold. Built: seniority, +1 percentage point of stake per full year as a paid-up member, up to 25% (a voice, never control); a newcomer premium on stake prices, ×3 on joining, fading to ×1 after 5 years. Entry fees, dues and thresholds unchanged. Source: Corey.
- **Decision:** each era is 20 years. P0: 80 three-month turns, AD 155–175. The batch's Late timing now leaves at AD 175. PROTOTYPE_SCOPE (pacing, scenario), SYSTEMS §7, GDD Appendix A, P0-19, tuning `time.eraTurns`, `stakes.*`. Source: Corey.
- **Resolved below:** Attention demand was 332 against a supply of 320 (1.0×).
- **Batch result:** gates A–C pass (A early 15 / 61 / 24, late 48 / 52 / 0). In early jumps FreeMarket and Interventionist can no longer afford a voice in a faction before AD 160, so they play identically. Late: Interventionist collapses (Index 58, 20.8 busts); Balanced and Specialized both arrive at 125. Source: batch runner.

## 2026-09-28 — Higher stake prices; more Attention costs and more ways to spend it (Corey)
- **Decision:** 61 gold from 1% to 10% of the guild was still too cheap: stake base prices doubled (Medicine 2.4, Governance 3.6, Economy 3 per 1%; with the newcomer premium, 1→10% of the guild now costs about 117 gold; founding costs double with them). Attention costs raised (buy 2, invest 2, found 3, charter 3, audit 3, endow 2, policy 2), and more things to spend Attention on: `attend <inst>` (meetings, 1 Attention; two a year earn extra seniority) and one more project per domain (midwives, night watch, market road). Demand 513 vs supply 320 (1.6×); the Attention test passes. SYSTEMS §7 and §9, PROTOTYPE_SCOPE, GDD Appendix A, P0-20. Source: Corey.
- **Batch result: gates fail.** Specialized (its own school, no fees, dues or premium) wins 73% early and 95% late of A; gate B fails late (Specialized 85%); gate C fails late (Endow 82%: the Pay-down variant spends its gold on debt and falls to Index 82). Scripts not adjusted; nothing tuned; awaiting a decision. Source: batch runner.

## 2026-09-28 — The turn ends by itself when Attention runs out (Corey, from play)
- **Decision:** if a choice uses up the turn's Attention, the turn ends automatically, unless something that costs no Attention is still available. Built: the console ends the turn when Attention hits 0, no prompt is open (seeded choice, promise, outbreak, era end) and no debt can be paid down; priorities count as a standing setting and don't hold the turn. On for keyboard play; scripts opt in with "@autoend on" so older scripts replay unchanged. PROTOTYPE_SCOPE (P0 pacing). Source: Corey.

## 2026-09-28 — Steeper stake prices (Corey, option B)
- **Decision:** buying guild stake was still too cheap. Each 1% now costs base × (1 + stake% / 4) (was / 10), so low stakes stay affordable but a voice and control become very expensive; founding stays at 65% of the cost of control and rises with it (School 556, Club 834, trading house 695 gold). SYSTEMS §7, GDD Appendix A, P0-18, tuning `stakes.costGrowthPerPercent`. Source: Corey.
- **Batch result: gates A and B fail.** Specialized wins 73% early and 88–96% late; the Balanced scripts now reach only 21–50% of the Circle, and no one controls an institution in an early jump. Specialized's own school fails in 20% of late runs. Gate C passes. Nothing tuned; awaiting a decision. Source: batch runner.

## 2026-09-28 — Time machine repair steps (Corey; recorded, not built)
- **Direction:** some options should work toward getting the time machine ready to jump, broken into small steps; to be built out more later. Proposed shape (accepted "for now"): three machine systems, three steps each (9 in all), each step a little gold and 1–2 Attention and each needing something from Rome. Coil housing: source bronze (guild or bank membership, or pay more at the open market), commission the casting at Ostia (ready in 2 turns), fit and test. Coolant: borrow glassware (a finished Medicine project or Circle membership), distil, seal and test. Chronometer: enter the state archives (faction membership, or bribe a clerk), copy the star tables over 3 turns, recalibrate. A `machine` command and a "Machine: n/9" status line.
- **Open:** what the steps do: (A) all required to jump, (B) each extends the jump's range from about 25 to 250 years (recommended; fits the 25–50-year first jump decided today), or (C) each missing step raises the risk of a rough landing. Fuel puzzles, repairs and malfunctions are out of P0 scope, so building any version needs a PROTOTYPE_SCOPE change. Not built; awaiting a go-ahead. Source: Corey.

## 2026-09-28 — Costlier projects, joining benefits, the machine track, a 40-year first jump (Corey)
- **Decisions:** projects cost more (gold ×1.5, except the hour-one fountain and workshop). A voice unlocks a little more: an institution you have a voice in pays a quarter of your projects in its domain. Members earn a little more: each membership raises work pay 10%. All 9 machine repair steps are the minimum to jump (option A); built as recorded earlier, with a `machine` command and `repair <system>`. The prototype switches to the first jump: 40 years (25–50 decided earlier). Meetings keep counting by calendar year for now. PROTOTYPE_SCOPE (scenario, time machine, jump, projects, gold, out-of-scope list), SYSTEMS §7, §9, §11, GDD Appendix A, P0-21, tuning `jump.years`, `stakes.voiceProjectShare`, `joining.workBonusPerMembership`. Source: Corey.
- **Seniority during a jump (Corey, open):** you don't attend meetings while away, so members forget you or never knew you. Not built: in P0 the test ends on arrival; stakes don't grow during the absence. To be designed with later eras.
- **Batch result: gates A and B fail.** Specialized wins 67–93%. With a 40-year jump, leaving early is now very costly: early arrivals average Index 44–81 (the plague strikes in the absence, and its damage and the debt haven't faded yet). Attention demand 528 vs 320. Nothing tuned. Source: batch runner.

## 2026-09-28 — Inventions (Corey)
- **Decision:** an option to work on inventions that affect income, standing and influence. Built as a first version, to be fleshed out later: 5 inventions (wheelbarrow, double-entry bookkeeping, soap and boiled linen, compound pulley crane, water-driven trip hammer), each needing something from Rome, made once, paying income, leader loyalty and stake. Commands `inventions` and `invent <id>`. No visibility or anachronism risk (out of P0 scope). PROTOTYPE_SCOPE, SYSTEMS §7, GDD Appendix A, P0-22. The batch strategies don't invent yet. Source: Corey.

## 2026-09-28 — The machine repair takes a couple of years (Corey, from play)
- **Decision:** the repair was too quick (4 of 9 steps in under two years). Steps now take 2–6 turns each (the star tables 6), and the casting, distilling and dial cost a little more gold (30, 15, 10). The longest system takes 2.5 years even worked alone; with Attention shared across systems, the whole track takes about 3–4 years. P0-21. Source: Corey.

## 2026-09-28 — Why Demetria asks you to stay (Corey, from play)
- **Problem:** Demetria's request had no motive: she has no reason to care whether a stranger stays, and can't know about the machine.
- **Decision:** she asks only once she has seen you do something for the sick (the fountain, a Medicine project, soap and boiled linen, sanctuary membership, or membership in her Circle), at the first plague warning after that. Her reason is historical: when the pestilence came, the rich fled to their villas and famous physicians with them (Galen left Rome in AD 166); she asks you not to flee. From her side your jump looks like flight. If you never gave her a reason, she never asks, and the arrival says so ("she never thought to ask you"). PROTOTYPE_SCOPE "Promise" unchanged. Source: Corey.

## 2026-09-28 — Arrival text fixes from play (bugs)
- A fountain you repaired later was described as "the fountain you didn't choose ... filled in long ago"; it now gets its own lines (still runs / gone dry again). The Discovery beat for "nothing backed" claimed no one had heard of Demetria while her letters survive; it now says no one remembers you. Learn more said "None founded"; now "None you held 10% or more of". The jump briefing now warns when Demetria's request is unanswered. Source: phone playtest (seed 42).

## 2026-09-28 — The jump's distance varies: repairs, time in the era, optional upgrades (Corey, from play)
- **Decision:** an early jump should risk being short (about 25 years), and other machine work should raise the odds of a longer one (50–60). The distance depends on the repairs, the time spent in the era and optional repairs. Built: after the 9 required steps the range is 25–40 years; each of 3 optional upgrades (`upgrade contacts|lens|flywheel`) adds 5; each full 5 years in the era beyond the first 5 adds 5 (up to 10); capped at 60. The distance is drawn at departure from the seeded generator in 5-year steps; the briefing shows only the range, and the arrival says how far you went. The absence supports a final half-decade step. Replaces the fixed 40-year jump. PROTOTYPE_SCOPE, SYSTEMS §11, GDD Appendix A, P0-23, tuning `jump.range.*`. Source: Corey.

- **Batch result:** Specialized dominates (81–93%; gates A and B fail, C passes). The batch strategies don't build upgrades, so they jump 25–40 years early and 35–50 late. Nothing tuned. Source: batch runner.

## 2026-09-28 — Prices rise with a debased coin (Corey, from play)
- **Decision:** with a debasement policy, guild items cost a little more each year. Built broadly: a price level that rises each New Year by the coinage policy's inflation (as history 1.5%, debase 4%, sound 0, blended by sway over Governance), logged with its cause. Projects, stakes, entry fees, dues, inventions and machine steps scale with it; work pay catches up by half, so saving loses value under debasement. Status shows prices since AD 155. SYSTEMS §9, PROTOTYPE_SCOPE, GDD Appendix A, P0-24, tuning `prices.*`. Source: Corey.


## 2026-09-28 — History is the baseline; the plague comes on its historical dates (Corey, from play)
- **Principle:** the baseline for everything, in every domain, is what happened historically, unless the player does something that directly or indirectly changes it. SYSTEMS §6.
- **Problem found in play:** the plague arrived on invented dates (first warning AD 158–159, certain by 162; set when an era was 20 turns), three years before the real one, and the historical curve's plague drop (AD 165–170) would have hit a Rome that had already had its plague. The first warning also cited a Parthian war that hadn't started yet.
- **Decision:** the player changes how hard the plague hits, not when. Warnings come in Iulius 165 (the legions besieging Seleucia), Ianuarius 166 (the army comes home) and Iulius 166 (fever at Ostia and the Subura); the outbreak in October 166 (the army's triumph). Left alone it kills about 10% of Rome unless the player mitigates it, and costs each domain its historical drop (the history curve now steps from AD 166 to 167: Medicine −6, Governance −2, Economy −4). Severity is scaled against history's Hazard and Resilience; a domain following history takes the plague's step once, from the event. Medicine debt now raises the plague's hazard rather than its timing; recurrence odds during an absence still use Medicine debt. The batch's Early jump moves from AD 160 to AD 166 so it still leaves during the warnings. SYSTEMS §6, PROTOTYPE_SCOPE (Crisis), GDD Appendix A, P0-09, tuning `plague.historical.*`, `plague.damage.*`, `history.*` (removed `plague.firstWarningYear`, `firstWarningJitterYears`, `advanceChance.*`, `foulWaterAdvanceBonus`, `forceAdvanceFromYear`). Applies from the next game; run 3 continues on the earlier build. Source: Corey.
- **Batch result: gates A and B fail.** FreeMarket now wins 88–92% of all runs; among Balanced, Specialized and Neglectful, Early passes (54 / 43 / 3) and Late fails (Specialized 87%). Doing nothing now costs almost nothing: investing strategies beat Neglectful by 0.1 Index points on average (was 8.3), because a Rome you have no voice in simply follows history. Neglectful never reaches a debt tier (it has no voice to Accept Risk with). Nothing tuned. Source: batch runner.
- **Open (audit against the principle):** (1) the AD 157 fever from the broken fountain lowers Medicine below history, though the fountain was broken historically; (2) plague recurrences during an absence are random by Medicine tier, not historical (Rome's next great epidemic was AD 189); (3) the historical curve's other crises (the Plague of Cyprian, the third-century crisis) arrive only as drift during a jump, never as events. Source: Claude Code audit.

## 2026-09-28 — Debasement as one of the downfalls of Rome (Corey)
- **Decision:** all three parts, with the first as the core. (1) Near half of Rome's historical economic decline is the coin's, with ties to Governance: the Economy's and (a quarter of) Governance's decline from AD 155 to the coin's low in 268, the plague's step left out, is attributed to debasement. After departure, sound coin defended by a surviving Governance institution spares that share (by sway); a debasement policy adds half again; once no one defends the coin, Rome debases as history did. (2) Rome's own debasement follows the historical silver content (78% in 155, about 50% by 200, 3% by 268), which times the coin's share. (3) The arrival's Wrongness beat shows the coin (true silver, debased by your policy, or as history left it). In-era prices are unchanged (as history 1.5% a year). Coinage no longer counts toward the flat Austrian / interventionist absence bonus, so it isn't counted twice. SYSTEMS §9, PROTOTYPE_SCOPE (Economic policy), GDD Appendix A, P0-25, tuning `history.coin.*`, `policy.coin.*`, data/content/text.json `coin.*`. Source: Corey.
- **Effect in P0 is small by history's own timing:** the steep debasement comes after AD 235, beyond the longest P0 jump (175 + 60). Sound coin at full sway is worth about +3 to +4 Economy and +2 Governance on a 60-year jump. Batch: FreeMarket 91–92% (gates A and B still fail); nothing tuned. Source: batch runner.
- **Refinement (Corey):** the silver curve gains three historical points so its steps fall where they did: Commodus about 70% (AD 186), Philip the Arab about 40% (248), Valerian about 25% (257). P0-25, tuning `history.coin.*`.

## 2026-09-28 — Assess the machine first; put its gold back (Corey)
- **Decision:** time goes into assessing the machine before anyone knows what's wrong: one full assessment (1 Attention a turn for 4 turns) must come before any repair or upgrade; until then `machine` shows only the assessment. And since the inventor scavenged the machine for gold at the start, all of it (60, the starting gold) must go back in before it can jump: `restore <gold>`, in any installments, no Attention. The amount is fixed: the machine's gold is not debased. The intro now says so. Commands `assess`, `restore`; status shows "Machine: not yet assessed, gold 0/60". Batch strategies assess first and put the gold back once repairs are done. SYSTEMS §11, PROTOTYPE_SCOPE (Time machine), GDD Appendix A, P0-21, tuning `machine.restoreGold`, data/content/machine.json `assessment`. Source: Corey.
- **Batch result:** Late: FreeMarket 88%, Specialized 87% of the balance trio (gates A and B fail). Early gate B now reads PASS, but only because FreeMarket and Interventionist tie: with 60 gold going back into the machine, both reach a voice in the faction only at departure and never set a policy before an early jump. Not a real balance result. Attention demand 602 vs 320. Nothing tuned. Source: batch runner.

## 2026-09-28 — An invention tree (Corey)
- **Decision:** inventions form a tree: three branches of three tiers, 9 inventions in all. Mechanics: wheelbarrow → compound pulley crane → water-driven trip hammer. Accounts: double-entry bookkeeping → bills of exchange → a public accounts audit. Hygiene: soap and boiled linen → distilled wine for wounds → a fever ward with case records. Each invention after the first in its branch needs the one before it as well as something from Rome; later tiers cost more and pay more. The whole tree is visible (you know the future); locked ones show what they need. `inventions` lists the tree by branch. A small P0 tree, not the Knowledge Web (still out of scope: no diffusion, absorption or emergent variants). PROTOTYPE_SCOPE (Inventions), SYSTEMS §7, GDD Appendix A, P0-22, data/content/inventions.json. The batch strategies still don't invent. Source: Corey.

## 2026-09-28 — Two-month turns (Corey, from play)
- **Decision:** turns are 2 months long by default (were 3), and a player can change the turn length at any time with `turns <1|2|3>` (SYSTEMS §2: shorten at any time, never beyond the stage cap; P0's fixed Stage 3 caps it at 3 months). Attention stays 4 a turn (SYSTEMS §3) and multi-turn work keeps its turn counts, so there is more Attention a year and work finishes sooner in real time. The era now ends by the calendar (January AD 175) whatever the turn length (tuning `time.eraYears` replaces `time.eraTurns`); plague stages show on the turn that covers their historical date. PROTOTYPE_SCOPE (P0 pacing), GDD Appendix A, tuning `time.*`. The current phone run switched at AD 160 Iulius. Source: Corey.
- **Conflicts resolved (Corey: "only fix the machine"):** the repair was to take a couple of years; machine step turn counts are ×1.5 (rounded up) so the slowest system still takes 30 months (P0-21). The Attention target (demand ≥ 1.5× supply) is loosened to **1.4×**: demand is now about 709 against a supply of 480 (1.48×). Source: Corey.
- **Batch result (2-month turns):** Early: Balanced 92% of the balance trio, FreeMarket 95% of all (gates A and B fail). Late: Specialized 82% of the trio (A fails); FreeMarket 65% of all (B passes at the limit). Gate C passes. Nothing tuned. Source: batch runner.

## 2026-09-28 — The horse collar replaces the pulley crane (Corey, from play)
- **Problem:** the compound pulley crane didn't follow from the wheelbarrow, and Rome already had pulley and treadwheel cranes (the polyspastos), so it wasn't future knowledge.
- **Decision:** the mechanics branch is now wheelbarrow → the padded horse collar → the water-driven trip hammer. The collar (medieval, genuinely new to Rome) lets a horse pull three or four times the load; it needs the workshop and guild or bank membership, costs 60 gold and 4 turns, and pays 5 gold a year, trade loyalty +10 and stake +3. PROTOTYPE_SCOPE, GDD Appendix A, P0-22, data/content/inventions.json. Source: Corey.

## 2026-09-28 — Farming inventions wait for Agriculture (Corey)
- **Decision:** farming equipment (for example a wheeled heavy plough after the horse collar, crop rotation, a seed drill) arrives with the Agriculture domain in a later milestone, not in P0, where Agriculture is out of scope. Not routed through the Economy in the meantime. Source: Corey.

## 2026-09-28 — Workshop inventions; standing across institutions (Corey, from play)
- **Decision:** a fourth invention branch improves your workshop: the treadle lathe → water-powered bellows → a blast furnace, raising the workshop's income by +10% / +15% / +20% (kept modest at Corey's request; all three together +45%). Every invention now names the institution(s) it wins standing in, with stake and loyalty that vary by invention (stake 1–5%, loyalty 5–20), covering all six established institutions (the wheelbarrow now the Junians, the trip hammer and bills of exchange the banking house); rewards apply where you are a member. `inventions` shows each payoff. PROTOTYPE_SCOPE, SYSTEMS §7, GDD Appendix A, P0-22. Source: Corey.

## 2026-09-28 — Bug fix: the Index no longer anticipates history's plague
- The Index compares against history by fractional year, and the history curve's plague step ran across all of AD 166, so real Rome appeared to decline months before its plague and a player's Index rose for no reason (seen in play: 108.5 → 109.9 in Martius 166). The curve now holds its 166 values until AD 166.75 and drops over the outbreak's months (October–December). Yearly trends and the plague's historical drop are unchanged. tuning `history.*` (P0-03). Source: phone playtest.

## 2026-09-28 — Authority for public projects and plague measures; Demetria's motive (Corey, from play)
- **Problem:** anyone could fund "quarantine rules" and have Ostia adopt them, and anyone could direct the outbreak response, contrary to SYSTEMS §7 (a plague response needs a voice). Demetria's request never said how she knew a pestilence was coming.
- **Decision:** public projects (magistrates, census, night watch, market, mint audit, road) need 10% of an institution in their domain; private ones (fountain, physician, midwives, workshop, warehouses, patronage) need none. You know history, so you know the plague is coming, but the first signs are what give you authority you wouldn't otherwise have: quarantine rules need 10% of a Medicine or Governance institution, or only membership once the first warning has come. Directing the outbreak response needs 10% of a Medicine or Governance institution (SYSTEMS §7's voice, lowered because the warnings proved you right); otherwise Rome responds as history did. Demetria now cites the letters from Seleucia. SYSTEMS §5, §7, PROTOTYPE_SCOPE (Projects, Crisis), GDD Appendix A, tuning `authority.*`, data/content/projects.json `authority`. Source: Corey.
- **Batch result (authority rules):** Early: Balanced 87% of the trio, FreeMarket 93% of all (A and B fail). Late: Specialized 89% of the trio (A fails); FreeMarket 54% of all (B passes). C passes. Attention demand 1.5× supply. Nothing tuned. Source: batch runner.

## 2026-09-28 — Two currencies; gold across the jump (Corey)
- **Principle (Corey):** gold isn't debased; Rome's currency is. You keep things in gold so you aren't hit by the inflation. Follow history unless the player's actions change the rate of debasement, and make exchanging an action.
- **Decision:** everyday money is **denarii** at the historical scale (1 aureus = 25 denarii in AD 155); **gold aurei** are a separate purse whose price in denarii rises with the price level (history's 1.5% a year, or faster or slower with the player's coinage policy). Changing money at the money changers is an action (1 Attention, a 2.5% fee each way). You arrive with 60 aurei scavenged from the machine and must change them to live (the hour-one choice may be paid in gold directly); the machine takes aurei back, so rebuying them later costs more denarii. Internal accounting keeps its scale; the console shows and takes denarii. Across the jump: the machine carries 10 aurei; more can be deposited with the banking house (2% a year in gold; risk the venal house fails or embezzles, lower with 10% of the bank) or buried (risk someone finds it); the rest is lost; the Discovery beat reveals what became of it. Commands `exchange <n> aurei|denarii`, `deposit <aurei>`, `bury <aurei>`. SYSTEMS §9, PROTOTYPE_SCOPE (Money), GDD Appendix A, P0-26, P0-27, tuning `currency.*`, `savings.*`. Applies from the next game (run 4 continues on its earlier build). Source: Corey.
- **Batch result (two currencies):** Early: Balanced 61% of the trio (A passes), FreeMarket 92% of all (B fails). Late: Specialized 89% of the trio (A fails), 61% of all (B passes). C passes. The batch strategies change their gold at the start and buy aurei back for the machine; none deposit or bury gold. Nothing tuned. Source: batch runner.
- **Text fix (from play):** the Wrongness line for a strong economy said "the coins are good silver", contradicting the coin line that follows it ("barely half silver now"). The economy line no longer mentions coins; the coin line alone describes them. data/content/text.json. Source: phone playtest.

## 2026-09-28 — Walk around Rome at every arrival (Corey, from playtest)
- **Feedback:** "Economic is important to me… I would like to explore this more: what changed in the world you came back to?"
- **Decision:** at every arrival, including the very beginning, the player can walk around Rome: `visit market | changers | forges | curia | subura` (no Attention). Each place shows what is there now and, after a jump, what changed since you left: wheat prices and a day's wage, the aureus in denarii and the coin's silver, the workshop and your inventions in use, the Curia and your faction's line, the fountain and the population. Present conditions only, no causal chains (PROTOTYPE_SCOPE). Prices now keep rising during an absence with the coin's debasement. Also fixed: the AD 155 emperor is Antoninus Pius, not Marcus Aurelius. PROTOTYPE_SCOPE (Arrival), SYSTEMS §11, GDD Appendix A, P0-28, tuning `walk.*`, text.json `walk.*`. Next: jump again from the arrival (option A). Source: Corey.

## 2026-09-28 — Two jumps: jump again from the arrival (Corey, option A)
- **Feedback:** "Maybe in the next test we do two jumps."
- **Decision (option A):** after the first arrival the player may jump once more at once, with no second era to play (a second era, option B, is roughly P5's work). The machine stays repaired; the range starts over from the repairs and upgrades (no time bonus in the new era), so 25–40 years without upgrades. The second arrival has its own four beats and walk-around, compared with the Rome you left the second time. `jump` after arriving; tuning `jump.maxJumps` = 2. PROTOTYPE_SCOPE (Scenario, P0 pacing, Jump), GDD Appendix A. Source: Corey.
- **Refinement (Corey):** the first walk, in AD 155, is written from the perspective of "what just happened, where am I, what's going on" (the crash, the strange boots, gold but no denarii, the Senate and the coming pestilence only you know about); walks after a jump are about what changed since you left. text.json `walk.*.start`, `walk.intro.start`.

## 2026-09-28 — News of Rome (Corey, from play)
- **A free `news` command** shows what's going on in Rome: history's dated news (data/content/news.json, AD 155–175), world events of the past year from the log, and the market (aureus, prices, silver). New items show as a headline on the turn that covers their date. Reason: Corey wanted a way to follow Rome's goings-on; it also lets historical figures (Antoninus Pius, Marcus Aurelius, Lucius Verus, Galen, Avidius Cassius) appear indirectly without the out-of-scope dialogue and nudge systems. Text only: it draws no random numbers and changes no state, so replays are unaffected. Christian martyrs of the period are left out (SYSTEMS §14 care). No SYSTEMS.md numbers changed.
- **Noted for later, not built:** more institutions per domain (Rome had hundreds of collegia; politics ran through patronage networks rather than two factions) and a more open Rome (things to do at the places you visit, chance encounters). P0 keeps nine institutions so testers can say what they chose between. Source: Corey, from play.
- **Local news (Corey: "get a little creative with the news to include some local stuff").** Invented street-level talk (data/content/news.json "local", 50 items): fires, collapsing tenements, short-weight bakers, the races, curse tablets, festivals in their month, and talk tied to the player's world (the fountain or the workshop, their guild or faction and its leader by name, the plague and rising prices reaching the street). One item every 4 months (P0-29), the player's own world first, each heard once. Presentation only: not logged, no random numbers, so replays and the determinism hash are unchanged. Elections painted on walls were left out: by AD 155 the Senate, not the street, chose Rome's magistrates.

## 2026-09-28 — Make choices matter: lasting mark, tradeoffs, camps and offices, events, the workshop (Corey, after the exploration runs)
- **Finding.** 100 randomized runs through two jumps (playtests/explore-report.md) showed no crashes, but the jump roughly halved the differences between players; free-market and historical policy ended within ~2 Index points; interventionism was nothing or repeated busts; institutions came back "Thriving" on 161 of 162 first arrivals; and second arrivals often repeated the first's text.
- **Decision.** Fix it before human testing, in five steps, each checked with `--explore 100`: (1) the lasting mark (P0-30), (2) tradeoffs on existing actions (P0-31), (3) camps, offices and last orders (P0-32), (4) six historical decision events (P0-33), (5) the workshop (P0-34). Reason: P0's question is whether players feel their actions changed the world; testing with a rule set that erases those changes would waste testers.
- **Offices use period titles** (quinquennalis, curator, quaestor, decurio, patronus; socius and institor; cliens, amicus, consilium; archiater; benefactor, aedituus; scholarch). A foreigner can't hold Senate office or a priesthood, so the factions and the sanctuary have ceilings. Where the sources are thin (the Circle's and the bank's inner ranks) the ladders are reconstructions.
- **Last orders scale with standing** (Corey: people care more or less depending on your influence). A successor is named only from the head's seat or in your own institution (Corey: no successor without a leadership position).
- **Rejected:** a letter to yourself before the jump (Corey: to the inventor the jump lasts a minute).
- **Step 1 built (the lasting mark, P0-30).** The domain's target during an absence now keeps k = 0.6 of the departure lead from the day you leave, at 25% a decade; maintenance doubled; drift shows at 30; Austrian stances build each decade; one bust per absence. The exploration runs now keep the whole departure spread at the first arrival, and the policy actually in force separates arrivals by ~30 Index points. Documents updated: SYSTEMS §8 and §9, GDD Appendix A, tuning.json, P0_PROPOSALS P0-30. Still open: all institutions have drifted by the second arrival; the Index rises on average across the jump; second arrivals repeat the first's Recognition and Personal echo text.
- **Step 2 built (tradeoffs, P0-31) and savings limits (P0-35, Corey: cap the bank and the jar, and make large sums riskier; leave the hour-one choice for now).** New: grievances against institutions you haven't joined are remembered and lower the loyalty you start with. `--explore 1000`: the faction loophole is gone (0 runs, was 36); Rogue institutions on the first arrival 78 (was 10); the spread between players is unchanged (Index SD 18 / 17 / 20 at departure / first / second arrival). Finding: players who join nothing end level with heavy joiners at arrival (121 vs 119) because joining costs gold before the jump and pays after it; the tradeoffs alone don't change that. Documents updated: SYSTEMS §7 and §9, GDD Appendix A, tuning.json (tradeoffs.*, savings caps), P0_PROPOSALS P0-31 and P0-35.
- **Step 3 built (camps, offices, last orders, P0-32).** Meetings are votes between an institution's two camps; offices with period titles (quaestor, decurio, quinquennalis; institor, socius; cliens, amicus, consilium; archiater; benefactor, aedituus) are offered to members who qualify and cost Attention in duties; last orders back a camp and, from the head's seat or your own institution, name a successor, with force by office, loyalty and voting record. Leaders' requests moved to step 4 (decision events). `--explore 10000`: no bugs; but the payoff is too small: last orders move the Index 1.5–4.7 points, heads do worse than officers because duties eat Attention, and almost every institution has drifted by the second arrival. Proposed next (awaiting Corey): let the camp that leads, and your orders, change what the institution does for its domain while you're away (not only which way it drifts); give offices a payoff while you're present (projects and upkeep in its domain); and let your orders hold an institution to its camp so it can come back Thriving. Documents updated: PROTOTYPE_SCOPE (steps 1–3 marked built), P0_PROPOSALS P0-32, GDD Appendix A, tuning.json (offices.*), data/content/institutions.json (offices, ceilings, successors), text.json (discovery.office, discovery.orders.*).

## 2026-09-28 — Neglect tracks history; founding a little easier (Corey)
- **Doing little shouldn't be punished, but should track history almost exactly** (Corey). A new check (`--idle`: players who only work, repair and jump) showed idle players wandering 76–116 by the second arrival. Two causes, both fixed: (1) during an absence the level moved 25% a decade toward a moving historical target and lagged history's swings; now Rome follows history's own change each decade and only the gap from history moves; (2) random plague recurrences, catastrophic and on no historical date, weren't in the history curve; they are off, and the historical ones (AD 189, the 250s) are to be added on their dates with their drops in the curve. Idle players now come back at 101 at both arrivals (range 94–105). A test checks it.
- **Founding: the problem was cost, not failure.** Of players who founded an institution, about 90% kept it; but only ~4% of those who meant to found one could afford it (14,000–21,000 denarii). Founding now costs about 40% of buying control (was 65%; about 8,500–12,800 denarii). Documents updated: SYSTEMS §6, §7 and §8, GDD Appendix A, tuning.json.
- **Ordinary membership pays a little (Corey: go ahead).** A member's dues and voice keep the institution's domain up 0.2 a year, and the institution pays 10% of the member's projects in its domain. `--explore 10000`: heavy joiners now match non-joiners at the first arrival (115 vs 116) and pass them at the second (115 vs 113); officers and deputies 116–117.
- **Arrival text pass.** A second arrival reads as a return: its own Recognition, Wrongness, coin line and Personal echo (templates recognition2, wrongness2, coin2, personal2). Personal echo now adds your highest office ("between two names you don't know is a third, spelled wrong: yours, as quinquennalis") and the institution you founded (standing or gone). `--explore 10000`: second arrivals repeat none of the first's Recognition, Wrongness or Personal echo (were 91% / 72% / 80%); Personal echo has 315 versions (was 12). Recognition on the first arrival still has 11, tied only to the hour-one choice.
- **Step 4 built (decision events and leaders' requests, P0-33).** Six dated choices from Rome's history (the Tiber flood, Galen, the Parthian war levy, Marcus's palace auction, the invasion scare, the late grain fleet) and four leaders' requests to members (Varro's praetor, the sanctuary's new wing, the guild's grain arrangement, the bank's treasury loan). Each comes on its date if its requirement holds, shows in the turn header, is answered with `decide <option>`, and lapses to its last option after 3 turns or at departure. `--explore 10000`: no bugs; Index medians 112.9 / 114.5 / 114.0 (departure / first / second arrival). Finding: the answers barely move the Index (first arrival: generous 116.0, ignore 115.3, profit 115.1); the amounts are placeholders. Documents updated: PROTOTYPE_SCOPE (decision events and leaders' requests marked built), P0_PROPOSALS P0-33, GDD Appendix A, tuning.json (`events.lapseTurns`). No SYSTEMS.md numbers changed.
- **Decision events tied to the arrival (Corey: option 2 first, with a modest part of option 1).** Each answer you give yourself can leave a mark in Rome, a line of present conditions in the arrival's Personal echo: the flood line in the Subura with 'hospes' scratched beside it, a bakers' curse tablet with your name, the clinic wing on the Tiber Island with twenty beds now (or standing empty if the sanctuary is gone). Up to 2 an arrival; the second arrival shows the ones not yet shown first, aged. Lapsed choices leave none. Event amounts raised about 1.5×; the profit options now pay 40–60 aurei. `--explore 10000`: no bugs; Personal echo distinct texts 2,746 (was 315); the Index gap between answer styles is still about 1 point, so the events work through the arrival text more than the numbers. Not built: answers that stack across events (option 3). Documents updated: PROTOTYPE_SCOPE, P0_PROPOSALS P0-33, GDD Appendix A, tuning.json (`events.marksPerArrival`). No SYSTEMS.md numbers changed.
- **Step 5 built (the workshop, P0-34; Corey: sounds good).** Once you own a share of the smith's workshop, each season brings orders to choose between (ship fittings for the guild, tools for the builders, instruments for the physicians, a senator's ironwork), and it can take only one, two with enough apprentices. The smith is now a person, Tertius, with a regard for you and two ambitions that come as decision events: selling your designs to the guild (money and standing now, the workshop loses its edge) and a forge on the river (pay for it, let the guild lend and take a say, or refuse). Free, paid apprentices (SYSTEMS §14) raise output and carry your techniques after you leave; with the smith's regard and the Economy they decide what you find on arrival: a street of forges, a workshop still working, or a stable. `--explore 10000`: no bugs; the workshop's fate separates first arrivals by ~16 Index points (street 123, working 118, gone 107); players who took the most orders arrive ~9 points ahead of those who took none. Open: orders are strictly better than odd work (same Attention and pay, plus effects), so a player with a workshop rarely has a reason to do odd work; the numbers are placeholders. Documents updated: PROTOTYPE_SCOPE, P0_PROPOSALS P0-34, GDD Appendix A, tuning.json (`workshop.*`), data/content/workshop.json, events.json (the smith's two requests), text.json (workshop fates). No SYSTEMS.md numbers changed.
- **Workshop upgrades: a ladder of sizes (Corey: not only invent things but upgrade the workshop; chose the ladder over a menu).** `expand` grows the workshop one size at a time: smithy → workshop with a yard → works on the river → foundry. Each costs gold, Attention and a few turns, needs something from Rome (guild or bank membership; a water right, 10% of a Governance institution; the blast furnace), takes more and better orders and apprentices, and adds a yearly upkeep; one you can't pay shrinks it a size. Tertius's forge on the river now builds the works (and isn't asked for once it's built). The size shows at the forges on arrival (the yard's anvil, the water wheel, the foundry, or the dry mill race of a works that failed) and adds to the workshop's fate. `--explore 10000`: no bugs; first arrival by size: smithy 116, yard 120, works 125; the foundry was reached in only 3 runs. Documents updated: PROTOTYPE_SCOPE, P0_PROPOSALS P0-34, GDD Appendix A, tuning.json (workshop.fate.sizeWeight, carry.sizeWeight, smith.regardLostOnShrink; the fixed order and apprentice counts moved into the sizes), workshop.json, events.json, text.json. No SYSTEMS.md numbers changed.

## 2026-09-28 — Bigger impact for Rome's choices, memberships, founding and policy (Corey, after the 50,000-run exploration)
- **Finding (playtests/explore-report-10000.md, intent against result):** ~80% of players who meant a policy never got a voice to set it; ~90% who meant to found never could afford it; a membership below 10% cost dues and left nothing after the jump; and the explorer's answer styles were muddled (the first option was not always the generous one).
- **Decisions (Corey):**
  - **Rome's choices** give favor to the institutions they touch and stake in those you belong to; they stack (flood relief → cheaper quarantine; Galen or the clinic → a milder plague; the grain arrangement → a worse late fleet; profiteering twice turns the street against you, three generous answers win it). Options are tagged by style.
  - **10% is not enough to set policy**; it opens leadership instead, and an office lets your stake keep growing toward control (seniority cap 30 / 40 / 55% for officer / deputy / head).
  - **Advocacy**: without a voice, argue for a stance at a small sway, only while you're in Rome.
  - **Founding small** (15% of control, strength 10), with a power for each: the school softens the plague, the club carries your policy at strength, the house pays a dividend and eases famine.
  - **Memberships**: below 10% half dues and a small mark after you leave; several memberships make projects cheaper and Rome readier for plague.
- **Result (`--explore 10000`):** no bugs; policy style effect 0.37 (was 0.21), advocacy alone moves a little (free market 119 vs 117.5 without); 765 runs founded (was about a third as many), those who kept it arrive at 126–131 against ~116; at a 25–49% stake, 4 memberships arrive at 130 against 116 for one; answer styles: generous 120.7, principled 119.7, profit 118.7, loyal 116.6, ignore 118.2. Which institution you found still barely changes the Index (126–131); its difference now shows in what it does (plague, policy, income).
- Documents updated: SYSTEMS §7 and §9, PROTOTYPE_SCOPE, P0_PROPOSALS P0-36, GDD Appendix A, tuning.json (founding, joining, network, policy.advocacy, offices.seniorityCap and extraSeniority, events.street), events.json (styles, stakes, flags, conditional effects).

## 2026-09-28 — A numbered action menu in the console (Corey)
- **Request:** a simple interface so playtesters don't have to type the actions.
- **Decision:** the console lists, after each command, what you can do right now, grouped and numbered (decisions waiting, work, the workshop, the machine, money, projects, inventions, institutions, policy or advocacy, priorities, jump preparation, things to look at, the turn), with costs; type a number, or several ("3 7 1") to do them in order. Each number stands for an ordinary command, so a game played by numbers replays exactly. On for keyboard play, off for scripts (`--menu` / `--no-menu`, `menu on|off`). Still text in the console: a graphical or web interface stays out of P0 scope (PROTOTYPE_SCOPE: no UI beyond the console). No rules or numbers changed.

## 2026-09-28 — Before the gates: recurrences, founding, the foundry, placeholders, jump timing (Corey)
- **Batch gate (100 runs × 7 strategies, playtests/batch-report.md): FAIL.** Specialized (founding the school) wins 92% of early jumps and FreeMarket 100% of late jumps; Balanced, Endow and Split played identical games, so the scripted strategies are out of date (they predate the workshop, offices, Rome's choices and advocacy) and must be rewritten before the gate means anything. Open question to Corey: whether free-market policy should be exempt from the 65% rule, since the design makes Austrian stances better.
- **Founding toned down a little (Corey):** 18% of control (was 15%); school +0.05 plague resilience and +2.5 Medicine while away; club policy sway at least 0.65; house dividend 3.5 and +1.5 Economy while away.
- **Historical plague recurrences built now (Corey):** AD 189 and the Plague of Cyprian (about AD 251), on their dates, as steps in the history curve; the player changes how hard they hit. **Conflict resolved:** AD 189 falls 23 years after the Antonine outbreak, inside SYSTEMS §6's 30-year immunity; history is the baseline, so the historical recurrences come on their dates and the immunity applies only to outbreaks on no historical date (SYSTEMS §6 updated). Players who do little still track history (104 and 102 at the two arrivals).
- **The foundry: more likely, still rare (Corey):** it needs the water-powered bellows (was the blast furnace) and costs 100 aurei, upkeep 5 (was 150 and 6). Reached in 23 of 10,000 runs (was ~4).
- **All PROPOSED amounts approved for human testing (Corey)**, to be revisited with the testers' results (P0_PROPOSALS note).
- **Jump timing deferred to P3 (Corey):** staying longer is always better in P0 (early jumps arrive ~108, late ~125); testers will see this; it needs aging and machine-discovery risk.
- Documents updated: SYSTEMS §6 and §7, PROTOTYPE_SCOPE, P0_PROPOSALS (approval note, P0-37), GDD version 1.4 and Appendix A, tuning.json, workshop.json, text.json.
- **Workshop trim and the foundry (Corey: do the trim for work; keep improving the foundry's reach).** Size bonuses cut by about a third (output +7 / +20 / +33%; fate and carry weights down); orders now take 2 Attention (ironwork and contracts 3) and pay a little more, so per Attention they pay less than odd work but bring standing and effects: odd work vs an order is now a choice between money and standing. The foundry: the works also opens with 10% of the guild and costs 60 aurei; the foundry costs 70 and takes 3 turns. A new explorer cross-tab shows where players aiming for a foundry stop: most at the smithy (the yard needs guild or bank membership) and between the works and the bellows. Result (`--explore 10000`): the foundry in 42 runs (about 3% of players who aim for it and join the guild or bank); workshop size still separates first arrivals by ~6 points from smithy to works. Documents updated: PROTOTYPE_SCOPE, P0_PROPOSALS P0-38, GDD Appendix A, workshop.json, tuning.json.

## 2026-09-28 — Free market stays better; the batch gate with updated strategies (Corey)
- **Decision (Corey: keep the free-market idea for now):** Austrian stances stay better by design, so FreeMarket is exempt from the 65% rule. Gate B compares the other six among themselves; FreeMarket's rate among all seven is reported (PROTOTYPE_SCOPE pass criteria updated).
- **Scripted strategies updated** to use what exists now: Rome's choices (answered by style), offers of office, meeting votes, the workshop (orders and two apprentices); and the debt variants now differ: Pay-down pays debt all along, Endow saves in its last two years to endow its institutions, Split does half of each (before, all three played identical games).
- **Result (100 runs, playtests/batch-report.md):** late jumps pass B (Balanced 29%, Endow 41%, Split 31%, FreeMarket 97% of all seven) and C; early jumps fail: Specialized, which founds the school, wins 94% (arrives ~122 against ~106 for the broad strategies, which buy control of the Circle and have little built by AD 166), and Pay-down beats Endow and Split by about one point. Gate A fails both ways: Specialized wins every early seed, Balanced every late one. Not tuned to pass (Corey, earlier: don't tune only to pass the gates); options put to Corey.
- **Free markets only if the player drives them (Corey).** Confirmed in the code: only the `policy` and `advocate` commands change a stance; left alone Rome keeps history's policy, inflation and economy; advocacy ends when the inventor leaves; after departure a stance lasts only while an institution carrying it stands. A test locks this in (ImpactTests.RomeFreesItsMarketsOnlyIfYouDriveIt); SYSTEMS §9 states it. So the FreeMarket exemption rewards a player who earned a voice, not a default.

## 2026-09-28 — Early-jump founding left to the human testers; ready for playtests (Corey)
- **Decision (Corey):** leave the early-jump result (founding the school wins most automated early runs) for the human testers to judge; no further tuning before them. The playtest kit asks the session runner to watch for it.
- **Snapshot test added** (BUILD_GUIDE §8): a fixed reference playthrough (seed 42, the workshop, the guild and the Circle, generous answers, both jumps) must produce the same log hash; a deliberate rule change updates the hash in tests/Butterfly.Core.Tests/SnapshotTests.cs with a note here.
- **Playtest kit updated** (playtests/kit): the numbered menu in the opening line, the two jumps and the walk, what to watch for (early founding, discovering free markets, orders vs work, the menu), and the batch-gate status. The P0 order of work now moves to step 3: five human testers.


## 2026-10-02 — P0 complete; the project moves to P1 (Corey, via the handoff of 2026-10-02)
Source: `docs/handoff/2026-10-02/` (master handoff, decision log, technology ladder, P1 status, test evidence). Where these conflict with older documents, the newer owner decisions win and are synchronized here.
- **Phase:** P0 is done. The five-human test is **deferred to a later graphical milestone** (feedback on a console would not represent the game). P1 = playable game structure / vertical-slice architecture. PROTOTYPE_SCOPE.md rewritten for P1; the P0 scope is archived at `docs/archive/PROTOTYPE_SCOPE_P0.md`.
- **Calendar:** 1 turn = 1 month, fixed. No player-facing turn-length selector. Durations in months. Machine assessment 2 months.
- **Attention:** 4 a month. Spending the last Attention never ends the month. Explicit **End Month**. Fast-forward is opt-in and stops on meaningful interruptions.
- **Institutions:** stake buying is retired from the player-facing game. Access is relationship-first: aware → knows member → guest → invited back → sponsored → member → office, through the five-part invitation gate. Institutions bring obligations, not buffs.
- **Work:** generic consulting is replaced by commissions with explicit terms. Negotiation can fail. A clean ledger.
- **Technology:** a hidden capability network with Roman-baseline checks; Grand Challenges; theory separate from capability.
- **Narrative:** "say it once"; the banned narrator patterns; concrete Roman speech; Roman life as a primary pillar; the new opening; the machine's gold from its components; the quiet R-17 mystery.
- **Scene pacing:** a deterministic router; a third consecutive scene of one category is deprioritized (P1 Sprint 1 package, applied; numbers in tuning `scenes.*`).
- **Menus:** Now, Projects, People, Institutions, Knowledge, Civilization, Machine, Journal.
- **Evidence:** the 10×10, 100-critic and 10×1000 reviews are modeled persona reviews; seeds 611–620 were AI blind testers. None is human testing.

### Provisional readings, awaiting Corey's confirmation (P1_PROPOSALS)
Asked 2026-10-03; implemented provisionally so P1 can proceed. Each is easy to reverse.
- **P1-02 Durations:** keep calendar time. A P0 duration of N two-month turns becomes 2N months, except where Corey set a number (machine assessment: 2 months). The era stays AD 155–175 (240 months). With 4 Attention a month, Attention over the era doubles (960 against 480). The P0 rule "demand ≥ 1.4× supply" is not retuned; the batch report shows the new ratio.
- **P1-03 Stakes during migration:** an institution migrated to the invitation path derives the standing its P0 gates read from membership and office (member = influence, officer = voice, head = control) instead of a purchased stake. Not yet built; one institution at a time.
- **P1-04 Jump range:** stays 25–60 years until Corey decides; the live session's AD 164 → 247 (83 years) exceeds the cap.

## 2026-10-03 — P1 calendar built: 1-month turns, End Month, fast-forward
- **Built:** turns are 1 month, fixed (`time.monthsPerTurn` 1, `maxMonthsPerTurn` 1); the `turns` command and turn-length changes are gone. No auto-end: spending the last Attention never ends the month; `end` (End Month) moves exactly one month, even when all Attention is pledged; `wait` is the opt-in fast-forward and stops when anything needs the player (an open prompt or one of Rome's choices, news, an affordable investment, the era's end). The header shows "Attention: 2 free / 4 total · 2 reserved — <what holds it>".
- **Durations in months** (content key `durationMonths`, provisional P1-02): every P0 duration doubled to keep its calendar length; machine assessment 2 months (Corey). Mentoring 20 months, the hour-one choice by month 8, Rome's choices lapse after 6 months.
- **Attention over the era:** supply 960 (240 × 4), demand 1,203 = **1.25×** (P0 had 1.5× against a 1.4× target). Per-year demands (meetings, oversight, policy) don't double while supply does. Reported, not retuned (P1-02); Corey to decide whether to restore scarcity.
- **The plague** now breaks out in October 166 to the month, and street talk about the outbreak comes that month (it lasts a single month now and could fall between the every-four-months slots).
- **Snapshot reset** (deliberate rule change): the reference playthrough arrives in AD 242 with Index 126.1.
- Docs: SYSTEMS §2 and §3 (already noted 2026-10-02), tuning.json, GDD Appendix A. Scripted playtests: loop cap 400 cycles; 21 of 24 arrive clean, 14–16 end without arrival by design.

## 2026-10-03 — P1: the first commission, end to end; the ledger
- **Built:** the commission model (SYSTEMS §9, P1): content in `data/content/commissions.json`, one commission authored to the P1 rules (Roman baseline: Rome had bronze force pumps; the inventor brings true, replaceable valve seats cut to a gauge). Felix brings Cassianus's flooded cellar in the fifth month; looking is unpaid and says so; the terms name payer, amounts and who buys materials before work; asking for more can win, meet a firm no, or lose the work; three monthly stages hold Attention and count for scene pacing; completion pays through the ledger against the commission's project; Felix's referral is the first step on the invitation path to the Merchants' Guild of Ostia (access: knows a member). Amounts PROPOSED P1-05.
- **Ledger:** every gold change in the event log is a ledger entry (counterparty, reason, project); the monthly settlement is split into income, institutions and upkeep. A test plays the reference game to the jump and checks the ledger accounts for every denarius. Console: `ledger`, `commission`.
- **Not yet retired:** the P0 work (odd/craft/consult) still runs beside commissions; retiring it needs more commissions first.

## 2026-10-03 — P1: the first institution on the invitation path (the Ostia guild)
- **Built:** relationship-first access for the Merchants' Guild of Ostia (`data/content/invitations.json`). The player can't buy a seat (the console refuses and explains; the menu offers none). Felix, known through the cellar-pump commission, invites you as a guest; you're asked back; he offers to sponsor you; the members vote you in. Each forward step passes the five-part gate built from the game's own state (Felix named, the relationship, the commission done, its success, Felix's patience after a refusal). Admission costs the entry fee and brings dues and the guild's obligations (suppers, funerals, mutual aid, disputes). A member counts as holding 10% for the P0 systems still running (P1-03, provisional). Amounts PROPOSED P1-06.
- **Kept for now:** the simulation's legacy `Buy` still works for the P0 regression tools (batch strategies, explorer, snapshot), as the handoff allows ("retain legacy stake code only if required temporarily"). The other eight institutions still sell stakes until each is migrated.

## 2026-10-03 — P1: the locked opening; narrative rules checked by tests
- **Built:** the opening is now Corey's locked version (text.json `opening.*`): "The machine stops screaming before you do.", the non-travel systems test, dirt, the mule cart, Latin, "Ancient Rome. Not ruins. Alive." The machine's gold is pried from its contacts and couplings. Nothing about the plague, jump ranges or Echoes up front (the P0 tester line and the "something is coming from the East" hint are gone). Milestone label: P1 Playable Game Structure.
- **Checks:** a test scans every authored string in `data/content` for the banned narrator patterns (master handoff §7), and the quoted speech for modern managerial words. All current content passes.

## 2026-10-03 — P1: the hidden capability network
- **Built:** `data/content/capabilities.json`, 16 nodes from the capability ladder (handoff 03): shared measures, gauges and fits, true valve seats, recorded trials, dimensioned drawings, bronze recipes, graded tool steel, rigid lathe work, line shafts, interchangeable parts, the hydraulic press, the governor, steam power, comparative case records, electrical experiments, copying at scale. Each states Rome's baseline, the bottleneck and the leap. Rome's level per node lives in the world; every advance is logged (`capability.advance`) with its cause.
- **Rule (PROPOSED P1-07):** a capability can't pass a level its prerequisites haven't reached, up to reproducible ("Knowing is not making").
- **First link:** the cellar-pump commission moves true valve seats to prototype and then reproducible.
- **Hidden:** no console view yet; the Knowledge and Civilization views will surface it.
- Docs: SYSTEMS §13, P1_PROPOSALS P1-07. Tests: graph integrity (known leaps, no cycles, prerequisites exist), the blocker rule, and the commission's advance.

## 2026-10-03 — P1: recurring people with lives of their own
- **Built:** `data/content/people.json`: Felix, Cassianus and Diodoros, each with goal, vulnerability, cares, distrust, household, status, opinion, interest and voice; four life events that happen without the player (Felix's fever, the warehouse fire, Diodoros taking over deliveries, Pollio's bad copy of the pump). Requirements read the game's own state; chances are seeded (PROPOSED P1-08).
- **Consequences:** while Felix is laid up the guild's invitations pause (the inviter isn't "willing to take the risk") and he introduces no one; the bad copy moves true valve seats to adopted, marked distorted, with Cassianus as actor, not the player. Each life event is logged with its causes and actors.
- **Console:** `people` (or `who`) lists the people you know and how they stand now; life events show in the month's news.
- Docs: SYSTEMS §10, P1_PROPOSALS P1-08. Tests: content integrity, who you know, the fever pausing invitations, the fire, the bad copy, determinism, the console view.

## 2026-10-03 — P1: the jump's echo mixture
- **Built:** technical echoes (a capability's level on arrival, with authored lines per level), unintended echoes (bad copies, and who they're blamed on), personal echoes (people you knew, first arrival, at most two) and an institutional echo (the guild's book of suppers, if you were a guest who never joined). All present conditions; no causal chains.
- **Rule (PROPOSED P1-09):** during the absence, a reproducible capability carried by an institution you belong to rises one level per 20 years (tuning `capabilities.carriedYearsPerLevel`), reproducible → adopted → institutionalized; logged with the institution's leader as actor and your departure as cause.
- Content: `capabilities.json` (carriers, echo lines for true valve seats), `people.json` (echoes), `invitations.json` (echoGuest).
- Docs: SYSTEMS §11, tuning.json, P1_PROPOSALS P1-09. GDD Appendix A: add `capabilities.carriedYearsPerLevel` = 20 when Corey confirms P1-09.

## 2026-10-03 — P1: the eight menu sections as view models
- **Built:** `Simulation.ViewOf(section)` gives each of the eight sections (Now, Projects, People, Institutions, Knowledge, Civilization, Machine, Journal) as Active / Available now / Blocked (with why) / Emerging / Archived, from the game's state. Civilization shows the capabilities your work touched and the next steps beside them, with their bottleneck and what they still need; the rest of the network stays hidden. A ready machine says "The machine is ready. You can leave now, or remain in Rome and continue your work." and nothing more.
- **Console:** `view [section]`; the numbered menu is grouped under the sections ("Now · decide", "Projects · commissions", "Institutions · invitations", "Civilization · policy", "Journal · look", …), with the month's controls last.
- No rule or number changed.

## 2026-10-03 — P1: the Roman-baseline check in content
- **Built:** every invention in `inventions.json` now states Rome's baseline, the bottleneck and the leap (apply, improve, combine, formalize, standardize, scale), as the capabilities already do; every commission must name the capability it moves. Tests enforce all three. The console's `inventions` list shows "Rome already: …".
- **For Corey (no change made):** three P0 inventions sit close to what Rome may already have had. Soap was known (the invention is framed as applying it, with boiled linen, to wounds); a few scholars argue for Roman water-powered hammers (framed as combining the wheel with a cam); a one-wheeled barrow may have existed in Greece (framed as applying it to Roman building sites). Names and effects unchanged.

## 2026-10-03 — P1: the first Grand Challenge, with resource bottlenecks
- **Built:** `data/content/challenges.json` with Measurement and standards (capability ladder §4.1), opened by the pump's question. Four self-funded stages, started by the player (`challenge`, `challenge begin standards`), move shared measures and then gauges to reproducible. Stages hold Attention each month, show in the Attention header, and are abandoned (logged) if you leave mid-stage.
- **Bottlenecks:** people (the shared foot needs Felix, and waits while he is laid up), capability (gauges can't pass shared measures), and resources: tin bronze and Noric steel cost twice as much to a stranger, at the usual price through the guild (PROPOSED P1-10). Materials are ledger entries.
- **Echoes:** shared measures and gauges are carried by the guild while you are away and show on arrival.
- Docs: SYSTEMS §13, P1_PROPOSALS P1-10. No tuning value added (amounts live in the content file, like commissions).

## 2026-10-04 — P1 invariants: future Attention, one choice at a time, stale wording
- **Implemented:** a hard future-Attention limit. Every start of multi-month work checks each later month against what is already pledged then (projects, mentoring, machine steps, inventions, commission stages month by month, Grand Challenge stages, office duties); accepting an office checks its standing duties. Refusals name the month and the numbers. SYSTEMS §3.
- **Implemented:** the console's numbered menu takes one number at a time; "3 7 1" and "3,7" are refused with "One choice at a time". This replaces the 2026-09-28 batching (Corey, 2026-10-04).
- **Wording:** one-month actions no longer say "season" (oversee, unpaid dues, odd work, mentoring); the hour-one choice is paid "in the machine's gold", and the money changer is shown one of the aurei "pried from the machine". The workshop's order cycle (every few months) keeps "season", which is a real period there.
- **Snapshot:** log hash updated for the wording change only; the reference playthrough still arrives in AD 242 with Index 126.1.
- No number changed.

## 2026-10-04 — P1: the scene router routes the game
- **Implemented:** a monthly candidate pool across systems. Commission encounters, invitation offers, Grand Challenge openings, people's non-interrupting life events and authored scenes no longer fire on their own: each becomes a candidate with a category and weight, and one router picks at most one a month (P1-11). A third scene of a category in a row is excluded while any other category is available; explicit focus (`focus <category>`) lifts that; interruptions (Felix's fever, the fire, Rome's dated choices, the plague) bypass the router and count for pacing. Candidates not picked wait.
- **Content:** `data/content/scenes.json`: Roman life tied to the game's people and work (the baths, Felix's Lares, a guild funeral, Saturnalia at Cassianus's, the Circus, the lawsuit over Pollio's copy, Prima's vow on the Tiber Island, Diodoros's letter, the smiths' tavern) and the machine mystery (REFERENCE REQUEST ACCEPTED — R-17 with 155-03-ROMA; LINK QUALITY: DEGRADED; REMOTE REFERENCE ACKNOWLEDGED; R-17 ACTIVE, with the inventor's realization that the destination was set before the field came up). R-17 itself is not explained. People now carry a regard for the inventor, moved by shared life.
- **Fix:** Measurement and standards now opens only after the pump commission is finished (its opening scene speaks of a worn seat).
- **Tests:** a stakes test that counted collapses over one monthly year was made robust (two years, 60 seeds; no rule change). Snapshot reset (deliberate rule change: the router draws from the seeded generator): the reference playthrough still arrives in AD 242, now with Index 127.6 (was 126.1).
- Docs: SYSTEMS §2, tuning (`scenes.optionalPerMonth`, `scenes.baseWeight`), P1_PROPOSALS P1-11. GDD Appendix A: add both when Corey confirms P1-11.

## 2026-10-04 — P1: true maturity vs spread; edge levels; the full ladder; human echoes on every arrival
- **Implemented (Corey):** a capability's level is the true maturity of the method; spread (local / copied / widespread), distortion and misattribution are tracked apart and never raise it. Pollio's bad copy now spreads true valve seats as copied, distorted and misattributed; the arrival shows the true lineage and the bad copies as separate echoes ("distorted", then "distorted.later" on a later arrival).
- **Implemented:** dependency edges may name the level they need (`{"id", "level"}`); plain ids keep the P1-07 rule. Steam uses it (P1-12).
- **Implemented:** carried capabilities climb reproducible → manufacturable → economical → adopted → institutionalized, a rung per 20 years; manufacturable and economical have their own arrival lines for shared measures, gauges and valve seats.
- **Implemented:** people echoes on every arrival (the "first arrival only" rule removed), at most `echoes.peoplePerArrival` = 2, never repeated word for word; lines may carry `fromJump` and later arrivals prefer them. Second-arrival lines for Felix, Cassianus and Diodoros: a founders' feast and a mistaken memory of the foreigner, a family split, a praetor's ruling between two families, a bathhouse owner's version of the pump, a tombstone that claims too much.
- Docs: SYSTEMS §11 and §13, tuning (`echoes.peoplePerArrival`), P1_PROPOSALS P1-09 (revised) and P1-12.

## 2026-10-04 — P1: relationship-first migration continued
- **Implemented (Corey):** the Physicians' Circle no longer sells seats; Serenus (a new recurring physician: evidence-minded, blunt, poor patients) brings you in. Each invitation path's evidence is now three separate lists (relationship, work, usefulness) checked from game state; the guild's are knowing Felix, the finished pump and reproducible valve seats. Admission is a seeded vote that can be put off once and then refused (P1-13).
- **Implemented:** fast-forward no longer stops because a stake purchase became affordable. The menu offers generic work only as "odd jobs to get by" while no commission or Grand Challenge stage is under way. A second, repeat commission (Lollius's bilge pumps) comes through the guild (P1-14).
- **Implemented:** offscreen lives have windows, declining hazards, exclusive branches and departures (P1-14); Diodoros can go home to Antioch, with his own arrival echo.
- **Implemented:** old inventions are practical projects: stake rewards replaced by institutional regard; three link to capabilities and are blocked when Rome can't reach that level. "Knowledge · practical projects" in the menu.
- **Open (for Corey):** the Tiber Island sanctuary and the two Senate factions still sell seats (P0 legacy). Who introduces a foreign craftsman into a senator's following within the six-month "socially minor" rule, and does a temple take donors by gift rather than by invitation? Not decided here.
- Docs: SYSTEMS §7, §9, §10; tuning (`invitations.vote.*`); P1_PROPOSALS P1-13, P1-14.

## 2026-10-04 — P1 corrective pass: scripted sessions (AI-run, not human testing)
- **Added:** `playtests/ai/scripts/25-p1-vertical.txt` (seed 42): the workshop, the cellar pump, the guild by invitation and its vote, Lollius's repeat work, Measurement and standards to completion, Serenus, Felix's fever, the fire and Diodoros's departure, the machine repaired with its gold restored, and two jumps. Both arrivals show technical, human, institutional and unintended echoes; no line repeats between them; the bad copies appear apart from the true lineage. All 25 scripts re-run: 22 clean, 14–16 end without arrival by design.
- **Findings for Corey (not changed):** (1) once the two commissions are done the script lives on odd jobs and workshop orders: P1 has too few sources of real work to carry an era without the fallback (content gap, not tuned). (2) Authored Roman-life scenes run out by about AD 158; later years are quiet apart from the machine, the guild and history. (3) No third same-category optional scene in a row was seen; player work (machine steps finishing together) can still cluster, as allowed.

## 2026-10-04 — P1 vertical slice: Powered workshops
- **Implemented:** the second Grand Challenge, Powered workshops (SYSTEMS §13), with three capability nodes only where needed (replaceable bearing blocks, controlled transmission, powered boring and grinding) and arrival lines for line shafts and the new nodes. Six chunky stages; the lessons (smooth water, mud-fouled gearing, hot bearings, feed grooves, wider shells, slack-rope engagement, serviceability) are folded into stage text, not separate scenes.
- **People:** Gaius Fabius Crispus (the bronzesmith from the Clivus who first cut seats to your foot), Livia (Greens, sharp, the household), Marcus Fabius Tertius (eager, defensive, wants his own shop), Lucan ("Do not become a man who only has ideas."), Aulus Septimius Crispus (millwright; "Grease this before the wheel starts. Every morning." "Or buy another bearing." "Move that bench." "Someone will reach across the shaft for a hammer." "Tie your sleeves."), Sextus Nerius (finishing shop, twelve men).
- **Consequence:** completing the challenge triggers Sextus Nerius's question about his men, with five answers (machines change work; admit some will lose it; ask the men; ask whether he wants output or lower wages; inspect his tasks) and different consequences and marks. No answer is preached.
- **Autonomy and Roman life in the arc:** Marcus leaves for Priscus or stays, from his regard (the tally-book scene); Gaius's son's naming day delays a stage by a month; the machine mystery interrupts (the panel wakes when the mill wheel starts, with the same handshake as the crash log; R-17 still unexplained).
- **Engine:** challenge stages can move several capabilities and name a stand-in person with extra months; scenes can delay a stage; events can be triggered by consequences; event effects on people's regard and status.
- **Fix:** event marks are never repeated word for word on a later arrival, and later arrivals prefer people not yet featured.
- **Open (for Corey):** the P0 smith is named Tertius and the arc's Marcus is Marcus Fabius Tertius; the text calls him Marcus throughout. Rename one?
- Docs: SYSTEMS §13, P1_PROPOSALS P1-15, content readmes (challenges, events, capabilities, people).

## 2026-10-04 — R-17: DO NOT JUMP restored (approved design, omitted by accident)
- **Implemented:** the crash log now shows the handshake (REQUEST; REFERENCE REQUEST ACCEPTED — R-17; four seconds, then the 155-03-ROMA lock). After R-17 ACTIVE, `listen` reopens the channel; a month later the interruption: REQUEST RECEIVED / SOURCE: R-17 / DO NOT JUMP, with the inventor laying it beside the crash log (same request, same acceptance, nearly the same interval; then the lock, now this). Three answers (shut down, keep listening, carry on), each with its own later scene; the notebook breadcrumb ("R17", temporal-reference stability, "reciprocal lock"). The warning appears in the departure briefing and never blocks the jump.
- **Open (Corey):** what R-17 is; whether a later deliberate contact or the second arrival should say more.
- Snapshot hash updated for the crash-log wording only (same arrival, AD 242, Index 127.6).

## 2026-10-04 — P1: more paid work, across domains
- **Implemented:** five new commission chains (P1-17), each with explicit payer and materials payer: Pomponia's fevers (via Serenus; records and comparative case notes to demonstrated; Serenus later argues it was bad air, not water), Philippus's baths (the likely lost negotiation; afterwards a curse tablet, then the curator of waters cuts the fuller's illegal tap and his men lose work), Diodoros's jars (a favor; regard and his standing, no money), Vettius's drawings (a profit share; dimensioned drawings to reproducible; copies spread under other names), Statius's mill bearing (via Aulus; walking away costs Aulus's regard in the text).
- **Engine:** optional referral; per-stage capability and level; on-completion regard, status and flags; favor and profit-share terms worded explicitly.
- **Snapshot reset** (deliberate: more routed candidates shift the seeded draws): the reference game now arrives in AD 252 with Index 127.3 (was AD 242, 127.6).

## 2026-10-04 — P1: recurring lives deepened; the smith renamed
- **Implemented:** thirteen new offscreen life events across the cast (Felix made quaestor, Felix's son's fees, Cassianus's daughter's betrothal, Diodoros's mother's death, Serenus's public quarrel, Gaius refusing a second shop, Livia's daughter's betrothal, Marcus's own shop or foreman's post depending on whether he stayed, Lucan's eyes and death, Aulus's back, Sextus's son's first case), all state-gated, windowed and once only; nine private scenes (the marriage contract, Felix's loan, Diodoros's funeral meal, Serenus's lecture, Livia at the races, Marcus's quarrel, Aulus rejecting a gear train, Lucan's funeral, Sextus's shop afterwards); two choices (witness; the loan) with refusals that cost regard. Scenes can trigger a choice.
- **Name:** the P0 smith Tertius is now Successus (a common freedman's name) in content, docs and the playtest kit; Marcus Fabius Tertius is unchanged. The historical P0 scope archive keeps the old name.
- Snapshot reset (deliberate: new candidates shift the seeded draws): the reference game arrives in AD 232 with Index 126.8.

## 2026-10-04 — P1: Roman life that lasts an era
- **Evidence:** a plain scripted run (seed 1) had a routed scene almost every month until AD 159 and almost none after AD 160: the authored scenes ran out.
- **Implemented:** reusable scenes (cooldown and variants, each text once), a scene gold effect, "!x" requirements, and ~35 new Roman-life and city scenes tied to calendar, place and people (market days, the district fountain working or broken, taverns, the insula's neighbors, the baths, temples and processions, the courts and witnesses, Compitalia, Lupercalia, Parentalia, the Ludi, summer heat and winter braziers, the grain dole and shortages, Antoninus Pius's death, the Parthian levy and war news, the vigiles at a fire, a schoolmaster, a freedman's tomb, Saturnalia at Gaius's, Pomponia's dinner, a vow at Fortuna Redux with a choice). The router records every pick (`World.RoutedScenes`).
- **Tuning (PROPOSED P1-11, revised):** with this much texture the plot was starved (an invitation and a commission waited many months), so progression candidates now weigh 3 against texture's 1.
- **Tests:** no authored text repeats; a reusable scene returns only after its cooldown with new words; in plain runs (seeds 1–3) through AD 164, Roman life appears every year, engineering stays under 35% of routed scenes, and at least six kinds of scene appear.
- Snapshot reset: the reference game arrives in AD 232 with Index 127.3.

## 2026-10-04 — P1: institution access cleaned up
- **Implemented:** sixteen event rewards that handed out percentage stakes in the guild, the Circle, the sanctuary and the factions now give **standing**, interpreted per event (flood relief: physicians and sanctuary; backing a client: the faction; selling designs or signing the arrangement: the guild; the river forge's loan becomes guild standing, since the guild gets a say in your business, not you in theirs). Flood relief also raises Serenus's regard. The bank keeps its two financial stakes (vouching for the treasury loan; buying what others dump).
- **Implemented:** institutions carry an access kind. Factions (patronage) can't be bought into from the console or menu; the sanctuary (gifts) takes `give` and lists you as a benefactor; the bank (shares) is unchanged.
- **Open (Corey):** the patron's-introduction path into a senator's following, within the six-month "socially minor" rule; until then Governance policy comes from advocacy or the player's own club.
- **Legacy compatibility (documented):** the stake field still drives offices, voice and policy internally; batch strategies, explorer and snapshot still use the old purchase.

## 2026-10-04 — P1 executable two-jump validation (scripted, not human)
- **Implemented:** `dotnet run --project src/Butterfly.Batch -- --p1-validate <seeds> [--out file]` plays the real build through an era and two jumps with a scripted player and reports paid work, odd jobs, scene categories and runs, invitations, commissions, challenges, Attention conflicts, the ledger, life events, jumps, echoes, duplicate text and quiet stretches. Report: `playtests/ai/live/2026-10-04-p1-two-jump-validation.md` (8 seeds). The pacing state now keeps its history and the router records its picks for this.
- **Fixed from evidence:** losing the first job closed every later door (seed 3): a second chance through Felix (Aemilius's hoist) now leads to the guild, whose evidence accepts it (work: either job; usefulness: valve seats reproducible or Felix's regard). Five mid-era follow-up jobs (Lollius's crane, the Janiculum race, a second household for Serenus, Gaius's singing fountain as shared development, the Clivus drain for the vicus magistrates) cut odd-job months from 45–60 to 15–47 for the fountain seeds. Later arrivals no longer repeat Discovery lines word for word (an unchanged house says so). R-17 ACTIVE is now an interruption so it can't be missed by a same-month jump. Commission regard lines name the job, so they never repeat.
- **Open (Corey):** Measurement and standards opens only from the cellar pump's valve seats; a player who lost that job never sees it. The fountain choice leaves more odd-job months than the workshop.
- Snapshot reset: the reference game arrives in AD 247 with Index 127.6.

## 2026-10-04 — P1 hardening: Grand Challenges have several ways in
- **Evidence:** the two-jump validation (seed 3) never opened Measurement and standards: it opened only from the cellar pump's reproducible valve seats, so walking away from one job closed the challenge and everything chained behind it.
- **Implemented:** challenges carry `routes` (any of; each route's requirements all of), each with its own encounter scene; the routes converge and the challenge opens once, by the first route that holds in authored order (`ChallengeState.OpenedBy`). Standards opens from the pump (unchanged), Aemilius's hoist (another smith's pawl from your drawing won't catch) or Philippus's baths (another plumber's plug won't seat; from the second year). Stage texts no longer assume Cassianus's pump. The single-route form still loads.
- **Fixed:** Philippus's baths job depended on a scene that can only play before the pump is offered; it now also comes after an ordinary afternoon at the baths.
- **Tests:** each route opens it; losing the pump (played out) doesn't lose it; with no route it stays closed; with all routes holding it opens once; every requirement in the content parses.
- No rule numbers changed. Snapshot hash updated (text only: same arrival, AD 247, Index 127.6).

## 2026-10-04 — P1 hardening: the fountain opening's own economy
- **Evidence (scripted runs, not human):** fountain seeds spent 15–47 of 96 months on odd jobs against the workshop's 15–16. Causes: the workshop's seasonal craft orders (intended asymmetry), and structural gates: Gaius (and through him Livia, Marcus, the singing fountain, the Clivus drain, Aulus and the power arc) came only after shared measures, which came only after the pump; the fountain had no paid work of its own.
- **Implemented (PROPOSED P1-20):** the Subura allotment (after the fountain is repaired) and its follow-up on the Argiletum: neighborhood water work, client-paid from street funds, Roman baseline (the water office's stamped calices). The allotment introduces Gaius (his first meeting scene no longer plays afterwards) and is a fourth route into Measurement and standards. Odd-job pay unchanged; the workshop keeps its orders.
- **Result (seeds 1–6, scripted):** fountain odd-job months 22 / 18 / 40 (seed 6 asks for more on every offer and loses three jobs), workshop 16 / 30 / 15 (seed 3, who lost the pump, now opens and finishes both Grand Challenges and does odd jobs to pay for their stages).
- **Tests:** a paid neighborhood chain without a workshop; the fountain opens standards without the pump; Gaius is introduced once; the workshop keeps orders the fountain doesn't get.

## 2026-10-04 — P1 hardening: the progression graph
- **Audit:** every gate in the content classified as exclusive, relationship, capability, economic or accidental single point of failure; written up in docs/P1_PROGRESSION_MAP.md (opportunities, entrances, dependencies, exclusivities, legacy P0 dependencies).
- **Fixed:** the standards "two shops agree" stage needed a guild contact that only Felix's two jobs gave, so a player who came in by the allotment or the baths could open the challenge and stall at stage 2. Gaius can now vouch and do it, with his own text (`standInText`), and the person doing a stage is logged as an actor.
- **Kept, by judgment:** Aulus's single introducer (Gaius never leaves), powered workshops' single route (the sequel to standards), the guild closing if both of Felix's jobs are refused (nothing required sits behind it), Diodoros's thread hanging on the pump (a favor, no money).
- **Legacy:** the chronometer archives step asks for faction membership, which P1 never grants, so it always costs its gold alternative (OPEN, tied to the patron's-introduction question). *Superseded in the freeze pass (2026-10-04): APPROVED and IMPLEMENTED as senator-client access, bribe fallback kept.*

## 2026-10-04 — P1 hardening: strategy matrix, routing and economy stress (scripted, not human)
- **Implemented:** `--p1-matrix <from>-<to> [--weight W]` runs each seed with five scripted profiles (cooperative, negotiator, selective, engineering-focused, relationship/Roman-life; odd seeds workshop, even fountain) through an era and two jumps. It measures routing and waits, Attention use, income by source, odd jobs, challenges, institutions, R-17, jumps and integrity. `--weight` is a counterfactual run in memory only. Report: playtests/ai/live/2026-10-04-p1-strategy-matrix.md (60 seeds, 300 runs).
- **3:1 routing:** recommended for locking. Below 3 the plot starves (weight 1: 133 progression scenes unplayed at departure on the final content, more odd jobs, fewer commissions); above 3 waits shorten but nothing the player keeps changes. Long-tail waits were first attributed to same-category exclusion; the polish pass traced them to a crowded pool instead (see below).
- **Fixed from evidence:**
  - Profit shares were promised in terms and text but never paid; they are now paid on schedule, once each, and lapse at departure (PROPOSED P1-21).
  - Unconverted denarii in hand survived the jump automatically, against SYSTEMS §9 and the briefing (wealth meant to reach the future, the carried purse, bank deposits, the jar and institutions' holdings, was never affected); denarii in hand are now left behind, logged and ledgered.
  - A second sponsorship after a refused vote repeated the first word for word; it now has its own words, and the guild's sponsor scene no longer assumes the pump.
  - A drift description repeated across arrivals; a path already described now says it has gone further down the same road.
  - Felix's fever (and so the vow and Serenus) waited on a guild referral; a player who lost three jobs met no one for six years. It now needs only that you know him.
- **Measured, not changed:** Attention (a third of months end at 0, 40–55% end with 2+ unused, no future overbooking attempted) and the jump range (first jumps 25–55 years, second 25–55, arrivals AD 188–268). Three jump-range models are written up for Corey; the question stays OPEN.
- **Tests:** ten economy stress tests; a second sponsorship in other words; Felix's life without the guild. The fragile "some games have neither branch" assertion is now a deterministic window check. Challenge tests wait for the router instead of assuming the opening month.
- Snapshot reset (deliberate: Felix's fever can now come earlier): the reference game arrives in AD 247 with Index 127.7.

## 2026-10-04 — P1 hardening: later human echoes
- **Audit:** the existing lines cover aging and death, descendants, changed trades, family quarrels, institutional memory, disagreement, misattribution and forgotten people. But Livia, Marcus, Lucan and Aulus had no echo at all; the fountain player's Gaius was remembered only through powered workshops; and arrivals always led with Felix and Cassianus (authored order). In 300 scripted runs, first arrivals had remembered almost only those two.
- **Implemented:**
  - Echoes for Livia (her daughter's bakery; great-granddaughters keeping the accounts), Marcus (foreman's tally board credited to Priscus, or the burial club that forgets his uncle; "Marcus's book" with three claimants), Lucan (his stone with your name misspelled; "too cold" said by men who never knew him) and Aulus (a bearing block nailed up like a horseshoe and credited to him; the race rebuilt, millstones in tenement walls), and two allotment echoes for Gaius (the Crispi calices; the water allotment read aloud at the Compitalia, which began with a dyer).
  - Arrivals now prefer people not yet featured, then those you were closest to.
- **Result (scripted):** first arrivals now remember Felix, Serenus, Gaius, Aulus and Diodoros; second arrivals Gaius, Aulus, Diodoros, Cassianus, Serenus, Livia and others. Still at most two people an arrival. No repeated lines.
- **Open:** echoes are keyed by arrival number, not years elapsed; see the jump-range note in the strategy-matrix report.

## 2026-10-04 — P1 hardening: unintended echoes from more than one thread
- **Evidence (scripted):** unintended echoes came only from Pollio's copy of the pump, so 37 of 300 first arrivals had none, against the P1 target of technical, personal, institutional and unintended echoes.
- **Implemented (PROPOSED P1-22):** three more bad copies in people's lives, each with its own arrival line: Vettius's drawings traced at the wrong scale, Serenus's tables kept with horoscopes instead of water, and brass bearing blocks. Maturity is never raised; spread, distortion and misattribution are recorded.
- **Result:** 298 of 300 first arrivals now show all four kinds.

## 2026-10-04 — P1 readiness audit (not a completion declaration)
- **Written:** docs/P1_COMPLETION_AUDIT_2026-10-04.md: every P1 target step and every must-be-absent item, with evidence from tests, the 300-run scripted matrix and the gate map. 12 PASS, 5 PARTIAL, 0 FAIL.
- **PARTIAL:**
  - understanding without overload (needs people);
  - Attention binds in bursts;
  - factions have no P1 path;
  - "continuing" is the walk and the second jump (later eras are out of scope);
  - generic grind for players who refuse work, and legacy ownership numbers.
- **Not decided here:** whether to freeze P1. That is Corey's call. The audit recommends a content and polish pass, plus Corey's decisions on the open questions, before P2.

## 2026-10-04 — P1 polish: wealth across jumps, made legible
- **Clarified (no rule change):** the departure briefing now puts all money in one block:
  - what the machine carries;
  - what is too much to carry, with the two ways to keep it: deposit (interest, the house can fail) or bury (no interest, it can be found);
  - what the bank and the jar already hold, as risk bands, never outcomes;
  - coin in hand, which stays behind unless changed into aurei or spent (endowing is offered only to someone who controls an institution);
  - money institutions hold, which stays with them in Rome and isn't the inventor's purse.
- **Tests:**
  - carried aurei survive;
  - aurei beyond the purse don't survive unless deposited or buried;
  - deposits follow the bank's rules (interest, or lost) and hoards follow the jar's (found, or gone);
  - denarii stay behind;
  - institutional holdings never come back as coin;
  - the briefing names each kind of money.

## 2026-10-04 — P1 polish: the long progression waits
- **Investigated (scripted, not human):** all 149 waits of 25 or more months in the 300-run matrix, traced month by month.
  - The waiting scene was barred by the two-in-a-row rule in only 2.4% of its waiting months, and player focus wasn't involved for the cooperative players.
  - The long tail is a crowded pool: 15–25 candidates a month, progression at weight 3, and no memory of how long something has waited.
  - The worst case: the Circle's guest-supper invitation (seed 52) waited 49 months after Serenus's conditions held. Meanwhile the guild's invitations came four times.
  - Other long waits: Lollius's crane, the Clivus drain, Vettius's drawings, Felix's son, Cassianus's daughter's betrothal.
- **Player experience:**
  - A person who should have come back to you for three or four years reads as the game forgetting. That is clearest for invitations, whose conditions the player can see met.
  - Late-era commissions and offscreen lives arriving a year late matter less.
- **Built, off (PROPOSED P1-23):** `scenes.progressionAgePerMonth`, a waiting progression candidate's weight × (1 + rate × months waited).
  - At the default 0, routing is exactly as before (deterministic outputs byte-identical).
  - Counterfactual at 0.10: longest wait 49 → 31, mean 7.3 → 5.9; Roman-life share, commissions done and odd jobs unchanged; the two-in-a-row rule untouched.
  - At 0.25: longest 26, mean 4.9.
  - Turning it on changes every seeded game and the snapshot, so it waits for Corey.
- **Not chosen:**
  - Exempting long-waiting scenes from the two-in-a-row rule would not help, since exclusion isn't the cause.
  - Re-categorizing scenes is for the same reason a mismatch.
  - 3:1 stays the working default.


## 2026-10-04 — P1 polish: fallback work reviewed
- **Audit (scripted, not human):**
  - Odd jobs are the safety net for cooperative players: 14–21% of era income, 15–19 of 96 months.
  - They become a main economy only for players who turn down or lose the work offered: 35% of income for the Negotiator on the fountain, 47% for the Engineering-focused fountain player who refuses civic and neighborhood jobs. That is a deliberate consequence, kept.
  - No content disappears unexpectedly any more (Felix's life no longer waits on the guild).
  - Players have alternatives in those months (Roman life, institutions, the machine, challenge stages when affordable).
- **What made it feel like a grind:** every odd job printed the same sentence.
- **Changed (presentation only):** odd-job months now rotate, in a fixed order (no random draw), through generic lines, a line for the opening built, and lines for people the inventor knows (Felix at Ostia, Diodoros's cart, Gaius's bench, Serenus's casebooks).
- **Unchanged:** pay, Attention, the economy and every seeded outcome.
- **Snapshot:** hash updated for the new text only; the reference game still arrives in AD 247 with Index 127.7, and p1-validate, p1-matrix, batch and explorer outputs are byte-identical.
- **Not added:** new commissions. The existing referral and repeat chains already cover each opening.

## 2026-10-04 — P1 polish: a relationship-first way toward a senator's house (foundation only)
- **Implemented:**
  - After month 18 (well past the six months when the inventor is socially minor), a credible intermediary can introduce him to Senator Lucius Caecilius Varro's freedman steward, Hermogenes:
    - Cassianus, who sells the house oil (after the pump and his regard of 2);
    - or Serenus, called to the house's sickroom (his regard of 3).
    One introduction only.
  - The inventor then stands at the morning salutatio as one client among forty. Varro asks him one technical question.
  - The house soon asks a favor: speak for a fuller-client in a water dispute. Every answer costs something:
    - oblige: the Caecilians' regard up, the Junians' down, and Gaius cooler if the inventor measured honest taps with him;
    - measure first: a smaller gain;
    - decline: Hermogenes cooler.
  - Faction access stage: knows a member. The views and console say plainly that this is a client at the salutatio, not a place in the following.
  - Hermogenes is a recurring person with a full profile and two echoes.
- **Not granted:** membership, stake, office, policy voice or historical importance. The factions still take no members in P1 (the following, its offices and the exclusivity between the two factions stay OPEN).
- **Machine archive step (OPEN, not changed):** it still asks for faction membership, so it always costs its gold alternative. *Superseded in the freeze pass (2026-10-04): APPROVED and IMPLEMENTED as senator-client access, bribe fallback kept.* Options for Corey:
  - a patron's introduction is enough (a client may be let into a house's records through its steward);
  - keep membership (unreachable in P1);
  - another route (a library or the Tabularium through Serenus or the guild).
- **Snapshot reset** (deliberate: new routable scenes shift seeded draws): the reference game arrives in AD 247 with Index 127.1 (was 127.7). The fast-forward test now checks its real claim, that the offer was made the month fast-forward stopped, instead of assuming two different games open their first event in the same month.

## 2026-10-04 — P1 freeze pass: Corey's decisions applied
- **Decided (Corey):**
  - Hermogenes and the political-client foundation are approved as built. They are not expanded into a faction system.
  - **The machine's archives step takes political client access.** A senator's note comes once the inventor has stood at Varro's salutatio as his client. Knowing Hermogenes, the house's goodwill, or meeting Cassianus or Serenus is not enough, and faction membership is not required. Bribing a clerk (15 gold) stays the fallback. This supersedes the earlier entries that said the step always costs gold.
  - **Strict Grand Challenge route causality.** A challenge remembers the first route that became eligible.
  - P1-20, P1-21 and P1-22 are approved provisionally, with no retuning.
  - The age bonus (`scenes.progressionAgePerMonth`, P1-23) stays at 0. Routing weights are unchanged.
  - **Not built:** the following (formal Caecilian membership, offices, the Junian path, client exclusivity, recurring obligations), long-jump or elapsed-time echoes, and Attention retuning (NEEDS HUMAN VALIDATION). The jump-range redesign is deferred.
- **Implemented:**
  - Machine requirement `senatorsClient` (the salutatio seen; legacy faction members still qualify for the P0 batch).
  - `ChallengeState.FirstEligibleRoute` / `FirstEligibleTurn`, recorded each month before routing. Same-month ties go by authored order, no randomness is added, a later route never replaces the record, and the challenge opens once.
  - Tests: the archive (stranger, the steward known, the house's goodwill, the client; the bribe price), and the allotment eligible first with the pump later (the allotment keeps the credit and opens once).
- **Scripted evidence (not human):**
  - 510 tests pass. The two-jump validation, the P0 batch gate and the snapshot are unchanged.
  - Matrix: 300 of 300 runs jump twice; the ledger reconciles in 300; no overbooking; 0 repeated arrival sentences.
  - Standards routes unchanged (pump 288, hoist 8, allotment 3, never 1). *Corrected below (benchmark profiles restored): pump 287, baths 1.*
  - Archives: a note in 8 runs, a bribe in 292. The scripted profiles repair early; only the Relationship profile now waits for the salutatio.
  - Both challenges done in 259 runs (was 266). This is that profile's script change, not a game regression. The explorer's output changes because its random players reach the salutatio.
- **Documents updated:** SYSTEMS.md §7, §11, §13; GDD Appendix A; P1_PROPOSALS.md; P1_PROGRESSION_MAP.md; P1_POLISH_2026-10-04.md; P1_COMPLETION_AUDIT_2026-10-04.md; HANDOFF.md; the matrix data. No tuning value changed.

## 2026-10-04 — Benchmark profiles restored (harness and documentation only)
- **Reverted (batch harness only):** the freeze pass had made the matrix's Relationship profile hold its chronometer repair until it had stood at the salutatio (else month 60). The strategy matrix is a stable behavioral benchmark, so the profile is restored to its pre-freeze definition. No other profile changed. No game rule changed: the archive implementation, Grand Challenge causality, routing, Attention and the jump range are as frozen.
- **Kept:** the matrix line that reports how the archives were reached (measurement only).
- **Result (scripted, not human):**
  - Both challenges done in 266 runs (pre-freeze 266; 259 with the deferral).
  - Relationship: 71.5 waiting months a run, longest wait 53 (pre-freeze values exactly). The other four profiles are unchanged.
  - Archives: a note in 0 runs, a bribe in 300. The note route is covered by the focused test and the explorer.
  - Standards credit: pump 287, hoist 8, allotment 3, baths 1, never 1. One fountain run moves from the pump to the baths because the baths raised the question first. The freeze pass's "routes unchanged" figure came from the deferred profile, and this entry corrects it.
- **Documentation:** the archive-access rule is no longer listed as OPEN anywhere. The earlier OPEN entries are marked superseded. It is APPROVED and IMPLEMENTED: senator-client access through the salutatio, with the bribe fallback kept.

## 2026-10-04 — P1 correctness pass (independent review findings)
- **Confirmed and fixed:**
  - **Powered Workshops short-circuit.** Completion fired on "goal capability reached OR stages done". The sluice commission (line shafts to reproducible) finished the challenge after one to five of its six stages in 148 of 300 matrix runs. Now a challenge is complete when its authored stages are. The goal is checked as a postcondition, and content validation requires some stage to reach it.
  - **Silent capability failures.** Commission, challenge and practical-project completions ignored a refused advance. Now:
    - a commission isn't offered or accepted while an advance its work declares is out of reach;
    - a challenge stage checks all its advances in order;
    - a refused advance at completion throws instead of finishing as if Rome had moved;
    - work already at its level finishes without claiming an advance.
    The one shipped case was fountainworks (dimensioned drawings without shared measures, reachable through the allotment). Life events only spread capabilities, and absence carriers climb only when they can (a rule, not a claim).
  - **Unreachable capabilities**, classified as future scope and hidden from the Civilization view's next steps: alloys, lathework, interchange, hydraulicpress, governor, steam, electricity, copying. Every other node is reachable in P1. None was accidentally unreachable.
  - **Access in the interface, not the simulation.** `Buy` now refuses patronage, invitation, gift and founded institutions; only the bank sells shares. The P0 purchase is the internal `BuyLegacyStakeForP0Regression`, used by the P0 batch, the explorer and the snapshot, whose outputs are unchanged. Stale "buy faction / buy into one" text is fixed.
  - **Preview consumed randomness.** `PeekRoutedScene` drew from the game's generator. It now uses `Rng.Clone()`. No other view or query draws.
- **Deliberate, unchanged (Corey's decision, not a bug):** the archive's senator's note needs the salutatio, not the later favor.
- **Consequences (scripted, not human):**
  - both challenges completed in 234 of 300 runs (was 266);
  - players do about 1.5 more challenge stages a run, have somewhat less gold at departure and do slightly more odd jobs;
  - one validation seed (2) does 10 stages instead of 5;
  - jumps (300 of 300 twice), the ledger, overbooking, route tallies and the archive tally are unchanged;
  - P0 batch, explorer and snapshot unchanged.
- **Documented:** docs/SAVE_STATE_BOUNDARY.md (not built). No tuning value changed.

## 2026-10-04 — Corey's decisions after the correctness pass
- **Guild and Physicians' Circle:** a legitimately admitted member still can't buy more stake. Influence there should come later through participation, seniority, relationships, obligations and offices, none of which are built now. The banking house is where buying financial ownership makes sense.
- **Grand Challenge completion:** 234 of 300 matrix runs completing both challenges is the accepted baseline. Don't tune toward the old 266, which was inflated by the Powered Workshops short-circuit. Whether both are too demanding is a question for human testing.
- **Archive note:** reaching Varro's salutatio as his client opens the archives. The later patron favor is not required, deliberately; requiring it would turn an optional moral choice into a hidden machine gate.

## 2026-10-04 — Scope: a narrow console human experience validation of the first return
- **Changed:** human testing stays deferred to a graphical milestone, with one exception. A limited console test with about 5 real testers is authorized to test the first-return hypothesis: whether players hesitate before leaving, care whom they leave, recognize consequences, can reconstruct some causes without being told, find uncertainty interesting, and want to explore and continue.
- **Not evidence about:** final UI usability, menu clarity, graphical hierarchy, accessibility, onboarding or readability of the eventual product. AI, scripted or model runs never count as human testers.
- **Milestone consequence:** if the return fails those tests (no hesitation, nobody matters, no plausible causes, a status report, no wish to explore), rework it before major graphical UI work. If it works, graphical UI proceeds with the return as a primary design target.
- **Updated:** PROTOTYPE_SCOPE.md (out-of-scope line and a new "Authorized" section), CLAUDE.md, BUILD_GUIDE §6, HANDOFF.md. The protocol will be `playtests/human/FIRST_RETURN_PROTOCOL.md`.

## 2026-10-04 — First-return prototype (Part II)
- **Built:** the return chapter after the first jump.
  - 4–6 places in the changed city, chosen without randomness from what actually happened. There are 21 authored patterns in returns.json: 6 human, 4 technical, 4 institutional, 3 unintended, 3 journal, 1 mystery.
  - A visit shows recognition and contradiction; "look closer" follows a lead. Every site keeps the first-life events that justify it as the logged causes of its visit.
  - The second jump waits for 3 places and "done".
  - The departure briefing lists what you'd leave unresolved, as facts only.
  - Four departure threads (foot, copies, marcus, shaft) differ by the state at departure.
  - Six core people have an age and a deterministic lifespan (self / elder / heirs / memory).
  - Journal anchors are written once in the player's words and compared with what survives.
  - On the first arrival the core cast leaves the Personal echo beat, and a site replaces the beat line it tells.
- **Proposed numbers:** P1-24 (return sites: max 6, 2 a kind, min 4 checked, 3 visits) and P1-25 (elapsed time: elder at 60, heirs for 25 years, the six ages and lifespans).
- **Reused, not rebuilt:**
  - the armed-jump briefing (extended);
  - the arrival handling and the walk (`visit` takes a site number or id first);
  - the polish design's elapsed-time bands;
  - the event log for grounding;
  - the requirement grammar, plus `journal:id` and `answered:event:option`.
- **Evidence (scripted, not human):**
  - 549 tests, including a same-seed test: leaving with the gauges unfinished against staying to finish them changes the fittings site (drift → guild's foot, misattributed) and the journal line (luck → the guild's order).
  - 300-run matrix: the return was started and completed in 300 of 300 runs, with 3 or more kinds every run and the second jump never offered early. Jumps, challenges (234), routes, ledger, overbooking and archive are unchanged.
  - The P0 batch is unchanged. The explorer finds no bugs and replays deterministically.
  - The snapshot hash was reset: the reference game still arrives in AD 247 with Index 127.1; the log gains journal notes and return events.
- **Inventions:** kept, not expanded, under design review. The capability network is the primary P1 implementation of "knowing is not making".
- **Not built:** a second era, long jumps or a generational model, a lead network, elapsed time beyond six people, a save system, graphical UI, R-17's explanation.
- **Next:** the limited console human test of the return (about 5 people). Its result decides whether the return is reworked before graphical UI or becomes a primary UI design target.

## 2026-10-04 — First return: sharpening before the human test
- **Principle (design):** a return exposes **primary sources, not omniscient history**. The simulation knows the causal chain, for grounding, testing and consistency; the player sees what plausibly survived (people, objects, records, inscriptions, copied lines). Causes may be obvious, contested or lost, and records may be self-serving or incomplete. It must not become incoherence: a plausible interpretation should usually be possible.
- **Changed:**
  - **Threads first:** a person whose site carries a departure thread (Marcus's path, Aulus's shaft, Cassianus's pump against Pollio's copy) takes the first human place. It is the closest such person, not a fixed character; the rest go by closeness, and without a threaded person nothing changes.
  - **The briefing warns, it doesn't predict:** the return records the threads the briefing named, and a site whose thread it didn't name is a grounded consequence nobody warned of.
  - **Content:** three institutional variants were rewritten as records (the guild's article on measures in marble and its charter's later hand, the Circle's dining book, the fountain's plaque). Aulus's never-started "grain" variant lost its thread tag, and Cassianus's copied pump gained the copies thread.
- **P1-24 and P1-25:** provisionally approved for human validation, not locked; no number changed.
- **Not built (candidates pending human evidence):** Obra-Dinn-style hypothesis locking, "three clues confirm" rules, correctness badges, player-written conclusions that change later eras, hidden countdowns, family trees, roll calls, a manuscript UI, second-era mechanics. The human test watches whether players ask for them.
- **Evidence (scripted, not human):**
  - 556 tests pass.
  - Matrix: core metrics unchanged. People found shifted toward threaded people: Aulus 282, Felix 235, Serenus 60, Marcus 16, Gaius 6, Cassianus 1. 68 threaded sites were named in the briefing and 795 were not.
  - Validation, P0 batch, explorer and snapshot are unchanged.

## 2026-10-05 — Change of direction: the P2 graphical vertical slice
- **Decision (Corey):** real human first-return testing is deferred until the minimal graphical vertical slice is playable. This changes the direction recorded on 2026-10-04 (a console human test of the return before any graphical UI); that entry stands as the history of the earlier plan.
- **Reason:** the return is meant to be experienced as places, people and evidence. A console stand-in risks testing the console rather than the return.
- **Scope:** the P2 graphical vertical slice: Unity 6 presentation over the existing Butterfly.Core, from AD 155 to the end of the first return. P1 gameplay is frozen. Core is the game; Unity is presentation only, through a thin engine-free adapter. No second era, new systems or balance changes.
- **Naming:** "P2" now means the graphical vertical slice. The fuel puzzle, formerly P2, moves later; its number is Corey's call. BUILD_GUIDE §6, PROTOTYPE_SCOPE and CLAUDE.md rule 7 are amended to match.

