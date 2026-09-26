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

