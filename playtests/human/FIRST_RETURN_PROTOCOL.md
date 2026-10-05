# First-return experience validation: human test protocol

> Authorized by Corey on 2026-10-04 (PROTOTYPE_SCOPE.md, "Authorized: first-return experience validation"). **This test evaluates the game concept, not final UI usability.** The console is a stand-in: don't score menus, readability, onboarding or accessibility from it. AI simulations, scripted personas and language models are never testers for this milestone.

## Who and how many

- About **5 real people**, ideally strategy or history players who haven't seen the design.
- One person at a time, with an observer taking notes. Think-aloud is welcome but not required.

## Setup

- `dotnet run --project src/Butterfly.Console -- --seed <n>`, with a different seed per tester (record it).
- **Say only this:** "You're a modern inventor stranded in Rome with a broken time machine. Play as long as you like. When you can leave, it's your choice."
- **Don't** explain causality, the return chapter, the journal, echoes, threads, or that leaving early changes anything. Don't answer "what happens if…" questions. Do help with console mechanics (how to type a command, what the numbered menu is).
- Let them play the first life until the machine is ready. This takes 60–120 minutes; a tester who wants to stop earlier may.

## Observe (write down what they do and say, separately from your interpretation)

- **When the machine becomes ready:**
  - do they leave at once?
  - do they read the briefing?
  - do they mention anything unresolved, or anyone by name?
  - how long do they stay?
- **After the jump:**
  - which place do they visit first, and why (if they say)?
  - do they recognize things ("that's my…")?
  - do contradictions make them look closer, or move on?
  - do they open the journal without being told?
  - do they visit more places than the three required?
- **At the end of the return:** do they say they want to continue? What do they ask?

## Afterward (ask in this order; record answers verbatim)

1. When the machine became ready, did you want to leave immediately?
2. What made you stay, or consider staying?
3. Who did you care most about leaving?
4. What did you expect to find after the jump?
5. What surprised you?
6. What do you think caused the changes you saw?
7. What evidence made you think that?
8. Was anything intriguingly uncertain?
9. Was anything merely confusing?
10. Did you recognize a consequence of something you chose?
11. Did you recognize a consequence of something you neglected?
12. Did you regret any earlier decision?
13. Did you want to explore more?
14. Did you want to continue the campaign?
15. Was returning more rewarding than simply advancing technology?

**Then ask them to reconstruct what they think happened while they were gone, in their own words.** Afterward, compare it with the run's log, which you can get from the tester's seed and commands. Note what they got right, what they guessed plausibly, and what they missed.

## Record

- One file per tester in `playtests/human/` (`YYYY-MM-DD-tester-N.md`): seed, opening, departure year, jump years, the sites visited, observations, verbatim answers, the reconstruction.
- Keep observations ("left within one month of readiness"), quotes ("I should have stayed for Marcus") and your interpretation in separate sections.

## The decision (patterns, not scores)

**The return passes** if most testers show the core behavior:
- departure brings hesitation or real consideration;
- at least one character or relationship matters to them;
- the return brings recognition;
- they infer at least some causal links without an explanation screen;
- contradiction prompts curiosity rather than shutdown;
- they want to explore more or continue.

**The return needs rework** if:
- readiness brings an automatic "leave immediately" with no trade-off felt;
- nobody matters to them;
- consequences seem random;
- they can't connect future evidence to their earlier play;
- it still reads like a list of results;
- they don't want to explore.

**If it passes**, graphical UI work proceeds with the return as a primary design target. **If it needs rework**, the return is reworked before major graphical UI development. Record the decision and its evidence in DECISIONS.md.
