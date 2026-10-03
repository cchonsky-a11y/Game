# P1_PROPOSALS.md — numbers and readings awaiting Corey's approval

Like P0_PROPOSALS: each entry is a placeholder or a provisional reading, referenced from `data/tuning.json` or the code, until Corey confirms or replaces it.

| Id | Area | Proposal | Status |
|---|---|---|---|
| P1-01 | Scene routing | A scene past the two-in-a-row soft cap keeps ×0.15 of its weight (`scenes.repeatedCategoryWeight`). From the P1 Sprint 1 package. | Proposed |
| P1-02 | Calendar | Durations keep calendar time: N two-month turns become 2N months; machine assessment 2 months (Corey). The era stays AD 155–175. Attention over the era doubles to 960; the P0 1.4× demand rule is reported, not retuned. | Provisional, asked 2026-10-03 |
| P1-03 | Institutions | During migration, membership and office stand in for the old stake thresholds that P0 systems read: member = influence (10%), officer = voice (25%), head = control (50%). | Provisional, asked 2026-10-03 |
| P1-04 | Jump | The 25–60-year range stays until Corey decides (the live AD 164 → 247 session was 83 years). | Open question |
| P1-05 | Commissions | The first commission (Cassianus's flooded cellar, via Felix, from month 5): 1 Attention to look (unpaid); 300 denarii now and 500 on completion, Cassianus buys materials; three 1-month stages at 1 Attention; asking for more (700) is accepted 4 in 10, refused 4 in 10, and loses the work 2 in 10. | Proposed |
| P1-06 | Invitations | The guild's path: guest supper 2 months after Felix knows your work, asked back 3 months later, sponsorship 4 months after that, the vote a month later; each supper 1 Attention and 50 denarii for the wine; a refused inviter waits 6 months (`invitations.inviterPatienceMonths`). | Proposed |
| P1-07 | Capabilities | A capability can't pass a level its prerequisites haven't reached, capped at reproducible (beyond that, prerequisites need only be reproducible). Commission stages map to levels: prototype/failure/refinement → prototype, craft adaptation/repeatability → reproducible, adoption/spread → adopted. The 16-node graph is a first draft. | Proposed |
