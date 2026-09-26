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
