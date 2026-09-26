# P0_PROPOSALS.md — Values and rules proposed during the P0 build

> SYSTEMS.md and PROTOTYPE_SCOPE.md leave many P0 numbers and small rules open (marked "tune" or not specified).
> CLAUDE.md rule 3 says to stop and ask when a rule is missing. To keep the build moving, each gap was filled with
> the smallest reasonable **placeholder**, recorded here, and referenced from `data/tuning.json` as `PROPOSED P0-xx`.
>
> **Nothing here is canonical until Corey approves it.** On approval, move the rule into SYSTEMS.md, the GDD and
> DECISIONS.md per BUILD_GUIDE.md §2.1. On rejection, change tuning.json/content and the affected tests.

| ID | Area | Proposal | Why a placeholder was needed |
|---|---|---|---|
| P0-01 | Time | Scenario starts in month 1 (Ianuarius) of AD 155. | No start month specified. |
| P0-02 | Domains | Levels run 0–100. Start: Medicine 50, Governance 55, Economy 55 (equal to the AD 155 benchmark, so the starting Index is 100). | No start levels or scale specified. |
| P0-03 | History | Authored Rome curves per domain (see `history.*` in tuning.json), interpolated linearly: dips for the Antonine Plague (165–180), the Plague of Cyprian (~250) and the Crisis of the Third Century (235–284), recovery under Diocletian (~300), decline to AD 405. Used both as the §6 benchmark and the §12 Index denominator. | SYSTEMS §6 and §12 need historical values; GDD §14 says early values are authored estimates. |
| P0-04 | Priorities | Yearly level change by priority: Protect +1, Maintain −2, Accept Risk −8. | SYSTEMS §5 says levels decay without upkeep but gives no rates. Accept Risk −8 was chosen so accepted risk reaches Strained in 2 years (pass criterion: 2–3). |
| P0-05 | Expectations | The recent peak fades 5 points per year, never below the current level. | SYSTEMS §6 "recent peak fading over time" has no rate. |
| P0-06 | Debt tiers | Strained ≥ 20, Fragile ≥ 60, Critical ≥ 120 debt. | SYSTEMS §6 names tiers without thresholds. The GDD example (≈63 after 3 years) lands in Fragile, as the GDD states. |
| P0-07 | Gold | Start 60 gold. Yearly income = 20 + 0.6 × Economy level + project bonuses, settled each turn pro rata. Upkeep per domain per year: Protect 20, Maintain 8, Accept Risk 0; institutions are paid first and unpaid domain upkeep acts as Accept Risk for its share. Prevention costs 2 gold per level point, so paydown costs 3 gold per debt point (the §6 1.5×). | SYSTEMS §5/§9 give the structure (upkeep split by priority, simple income) but no amounts. |
| P0-08 | Projects | 9 one-time projects, 3 per domain, in `data/content/projects.json` (cost 40–70 gold, 1 Attention per turn of work, 1–3 turns, +5 to +12 level, some with extras: income, plague resilience, institution loyalty/strength, clean water). | SCOPE lists examples only. |
