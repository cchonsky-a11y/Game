# P1 executable two-jump validation

**These are automated, scripted runs of the actual build (`dotnet run --project src/Butterfly.Batch -- --p1-validate ...`), not human playtests and not persona feedback.** The scripted player: picks the workshop on odd seeds and the fountain on even ones; looks at and accepts every commission (and asks for more on every third seed); accepts every invitation; starts every Grand Challenge stage it can; assesses and repairs the machine, buys back and restores its gold; opens the R-17 channel when it can; answers Rome's choices by seed; takes a workshop order when offered; does odd jobs only when no commission is under way and money is short; jumps once the machine is ready and the year is at least AD 163, then jumps again.

| Seed | Choice | First commission (month) | First paid (month) | Odd-job months / era months | Commissions offered / accepted / done / walked | Invitations offered / accepted / joined | Challenge stages / done | Life events | People known | Attention conflicts | Min gold / at departure (aurei) | Ledger reconciles | R-17 warned |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | workshop | 7 | 9 | 16 / 96 | 12 / 12 / 11 / 0 | 3 / 3 / 1 | 10 / 2 | 9 | 10 | 0 | 0.1 / 223.3 | yes | yes |
| 2 | fountain | 6 | 8 | 39 / 96 | 11 / 11 / 11 / 0 | 6 / 6 / 2 | 9 / 1 | 9 | 9 | 0 | 0.2 / 35.5 | yes | yes |
| 3 | workshop | 6 | 11 | 16 / 96 | 6 / 4 / 4 / 2 | 3 / 3 / 1 | 0 / 0 | 4 | 3 | 0 | 1.7 / 120.0 | yes | yes |
| 4 | fountain | 5 | 7 | 15 / 96 | 12 / 12 / 12 / 0 | 6 / 6 / 2 | 7 / 2 | 12 | 8 | 0 | 0.5 / 75.8 | yes | yes |
| 5 | workshop | 5 | 7 | 15 / 96 | 12 / 12 / 12 / 0 | 3 / 3 / 1 | 7 / 2 | 10 | 8 | 0 | 0.8 / 237.7 | yes | yes |
| 6 | fountain | 5 | 7 | 47 / 96 | 11 / 7 / 7 / 4 | 6 / 6 / 2 | 9 / 1 | 9 | 10 | 0 | 1.0 / 22.3 | yes | yes |
| 7 | workshop | 5 | 7 | 15 / 96 | 12 / 12 / 12 / 0 | 3 / 3 / 1 | 10 / 2 | 12 | 9 | 0 | 1.2 / 281.2 | yes | yes |
| 8 | fountain | 5 | 7 | 24 / 96 | 11 / 11 / 11 / 0 | 6 / 6 / 2 | 10 / 2 | 11 | 9 | 0 | 0.0 / 34.9 | yes | yes |

## Jumps and echoes

| Seed | First jump | Second jump | Echoes, first arrival | Echoes, second arrival | Institutions found (1st; 2nd) | Repeated arrival sentences | Duplicate scene texts in the era | R-17 reached |
|---|---|---|---|---|---|---|---|---|
| 1 | AD 163 → 193 (30 yrs) | AD 193 → 218 (25 yrs) | person 2, technical 6, unintended 1 | person 2, technical 5, unintended 1 | Merchants' Guild of Ostia Thriving; Merchants' Guild of Ostia Drifted | 0 | 0 | r17-notebook, warned |
| 2 | AD 163 → 188 (25 yrs) | AD 188 → 223 (35 yrs) | person 2, technical 3 | person 2, technical 3 | Physicians' Circle of Subura Thriving; Merchants' Guild of Ostia Thriving; Physicians' Circle of Subura Drifted; Merchants' Guild of Ostia Drifted | 0 | 0 | r17-notebook, warned |
| 3 | AD 163 → 198 (35 yrs) | AD 198 → 233 (35 yrs) | person 2 | person 2 | Merchants' Guild of Ostia Thriving; Merchants' Guild of Ostia Drifted | 0 | 0 | r17-dark, warned |
| 4 | AD 163 → 188 (25 yrs) | AD 188 → 228 (40 yrs) | person 2, technical 4, unintended 1 | person 2, technical 4, unintended 1 | Physicians' Circle of Subura Thriving; Merchants' Guild of Ostia Thriving; Physicians' Circle of Subura Drifted; Merchants' Guild of Ostia Drifted | 0 | 0 | r17-notebook, warned |
| 5 | AD 163 → 193 (30 yrs) | AD 193 → 233 (40 yrs) | person 2, technical 4, unintended 1 | person 2, technical 4, unintended 1 | Merchants' Guild of Ostia Drifted; Merchants' Guild of Ostia Drifted | 0 | 0 | r17-notebook, warned |
| 6 | AD 163 → 193 (30 yrs) | AD 193 → 233 (40 yrs) | person 2, technical 3, unintended 1 | person 2, technical 3, unintended 1 | Physicians' Circle of Subura Thriving; Merchants' Guild of Ostia Thriving; Physicians' Circle of Subura Drifted; Merchants' Guild of Ostia Drifted | 0 | 0 | r17-notebook, warned |
| 7 | AD 163 → 193 (30 yrs) | AD 193 → 223 (30 yrs) | person 2, technical 6, unintended 1 | person 2, technical 5, unintended 1 | Merchants' Guild of Ostia Thriving; Merchants' Guild of Ostia Drifted | 0 | 0 | r17-again, warned |
| 8 | AD 163 → 198 (35 yrs) | AD 198 → 223 (25 yrs) | person 2, technical 6, unintended 1 | person 2, technical 5, unintended 1 | Physicians' Circle of Subura Drifted; Merchants' Guild of Ostia Thriving; Physicians' Circle of Subura Drifted; Merchants' Guild of Ostia Drifted | 0 | 0 | r17-notebook, warned |

## Scene categories (era)

| Seed | Engineering | Personal | RomanLife | WorkEconomy | MachineMystery | CityHistory | Exploration | InstitutionsPolitics | Routed total | Longest routed run | Longest run, all scenes | Longest quiet stretch (months) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 2 (23) | 13 (25) | 48 (55) | 8 (32) | 5 (8) | 4 (9) | 5 (7) | 11 (17) | 96 | 7 | 3 RomanLife | 0 |
| 2 | 2 (22) | 14 (26) | 48 (56) | 5 (26) | 5 (8) | 6 (11) | 4 (5) | 12 (21) | 96 | 4 | 4 InstitutionsPolitics | 0 |
| 3 | 1 (7) | 6 (13) | 56 (60) | 4 (19) | 5 (8) | 7 (10) | 0 (0) | 9 (13) | 88 | 5 | 4 RomanLife | 3 |
| 4 | 2 (22) | 14 (27) | 44 (53) | 6 (28) | 5 (8) | 6 (11) | 5 (7) | 14 (23) | 96 | 3 | 3 RomanLife | 0 |
| 5 | 2 (22) | 13 (26) | 46 (54) | 7 (29) | 4 (7) | 8 (13) | 5 (7) | 11 (17) | 96 | 6 | 4 InstitutionsPolitics | 0 |
| 6 | 2 (20) | 12 (22) | 47 (53) | 7 (39) | 6 (9) | 6 (10) | 4 (5) | 12 (20) | 96 | 4 | 3 WorkEconomy | 0 |
| 7 | 2 (23) | 15 (28) | 44 (52) | 9 (33) | 5 (8) | 7 (12) | 5 (7) | 9 (15) | 96 | 4 | 3 WorkEconomy | 0 |
| 8 | 2 (23) | 11 (23) | 47 (56) | 7 (28) | 5 (8) | 5 (10) | 5 (7) | 14 (23) | 96 | 3 | 3 InstitutionsPolitics | 0 |

Category cells: routed optional scenes (all meaningful scenes, including the player's own work and interruptions, in brackets).

## Findings (2026-10-04; scripted runs, not human testing)

- **Paid work:** the first commission arrives in month 5–7 and first pays in month 7–11. With twelve commissions now in the content, five scripted players finished 11–12 of them. Odd jobs ran 15–16 months out of 96 for the workshop seeds and 15–47 for the fountain seeds. The fountain seeds have no workshop orders, and seed 6 also lost four jobs by asking for more. The first run of this pass, before the five mid-era jobs were added, showed 45–60 odd-job months for the fountain seeds.
- **A dead end, found and fixed:** in the first run, seed 3 walked away from the cellar pump, and the guild, the repeat work and both Grand Challenges never opened. A second chance (Aemilius's hoist, through Felix) now leads into the guild. Seed 3 still never opens Measurement and standards, which needs the pump's valve seats. That is an open design question, not a bug fix.
- **Scenes:** Roman life is about half of all routed scenes (44–56 of 88–96). Engineering is 1–2 routed scenes, and 20–23 of all scenes once the player's own work is counted. No quiet month for players who keep working; seed 3's thin path had a 3-month quiet stretch. The longest run of one category, counting everything, is 3–4. Every run of 3 or more happened when no other category was available, or came from the player's own work or interruptions; the router excludes a third optional scene of a category whenever another kind could happen.
- **Machine mystery:** every seed reached the DO NOT JUMP warning and its follow-up scene (the notebook, still listening, or the dark panel). In the first run, seed 1 left in the same month the last repair held, and R-17 ACTIVE never showed. It is now an interruption.
- **Jumps:** the first jump was 25–40 years (AD 163 → 188–203); the second was 25–40 more (to AD 218–243). The jump range itself is an open design question and was not changed.
- **Echoes:** every arrival showed two people. The first arrival showed 4–6 technical lines; seed 3, with no capability past prototype, showed none. Bad copies appeared on both arrivals in 6 of 8 seeds, and the institutional fate of the guild and the Circle appeared in Discovery. No arrival sentence repeated between the two arrivals: an unchanged house now says so instead of being described again. There were no duplicate scene texts in any era.
- **Money and Attention:** the ledger reconciles in every run. There were no future-Attention refusals. Gold at departure ranges from 22 to 281 aurei: the fountain seeds leave poorer. Minimum gold sits near zero in most runs, because the scripted player spends whatever it has on machine work and stages.
- **Grind:** fallback odd jobs are no longer the main loop for the workshop seeds. The fountain choice still leans on them in some seeds, an open design question (see DECISIONS).
