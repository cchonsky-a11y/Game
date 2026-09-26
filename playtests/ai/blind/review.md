# Blind review: first-time player (AI subagent)

> **ADVISORY, NOT GATE EVIDENCE.** An AI subagent received **only** the console output of three playthroughs (`reviewed-transcripts.txt`): no docs, no code, no tool use. It was asked to play the role of a first-time strategy player and answer the four BUILD_GUIDE §9 questions plus "Where were you confused?". It did not play; it read recorded sessions whose inputs were scripted. Its answers do not count toward the P0 pass criteria. Only the 5 human testers do.

Sessions: 1 = `02-late-workshop-keep-endow-audit` (seed 102); 2 = `03-early-workshop-break-promise` (seed 103); 3 = `10-faction-big-endowment-no-audit` (seed 110).

## Session 1 (workshop, Circle, kept the promise, left at the end of the era)
1. **Choosing between:** the fountain or the workshop at the start. After that, mostly which one project to fund with spare gold (census, market, physician, magistrates) and whether to promise Demetria I'd stay. At the plague, quarantine or hospice. At the jump, how to split a little gold between an audit, an endowment and paying down debt.
2. **What changed:** the smith's workshop had become a whole street of forges. The fountain had been filled in and people call the spot "fever corner". Demetria carved "For the one who stayed" over a door. The Circle had dissolved. Overall, Rome was "almost exactly as the history books describe" (Index 103).
3. **Connected:** yes, strongly. "Fever corner" linked straight back to my first choice and the AD 157 fever, and the inscription paid off my promise. It hurt a bit that the Circle died even though I'd built it up to strength 100 and loyalty 100.
4. **Jump again:** yes. This ending made me want to keep going.

## Session 2 (workshop, Circle, promised, then left early in AD 160)
1. **Choosing between:** leaving early or keeping my promise and staying through the plague. I also sat on gold instead of starting projects.
2. **What changed:** the workshop was a stable and the fountain was "fever corner". Whole blocks of the Subura had collapsed. Seven pestilences while I was gone, six catastrophic. Index 91 → 15.
3. **Connected:** the tomb line "I kept my promises. You did not keep yours." hit hard and was clearly about me. But I couldn't tell whether leaving early or sitting on 175 gold caused the collapse.
4. **Jump again:** probably, but I'd feel punished rather than curious.

## Session 3 (workshop, senate faction, refused the promise, hoarded gold)
1. **Choosing between:** building the faction instead of the Circle, and refusing the promise. At the jump, endowing the faction or paying down debt.
2. **What changed:** my faction had become the Caecilian Houses, a family holding the city prefecture by inheritance (Governance 91). Medicine and Economy had collapsed to 10. 17 catastrophic pestilences. The workshop was a stable again.
3. **Connected:** yes. The faction turning into a hereditary dynasty is exactly "the thing I founded became something I didn't want". Demetria's letter ("At least it was honest") was a nice touch.
4. **Jump again:** yes, mainly to see what the Caecilian Houses do next.

## 5. Where were you confused?
- Costs are never shown before committing: founding, charter and audit took gold without stating the price.
- Attention usually shows 4/4 even after overseeing and working; unclear what uses it and whether it is being wasted.
- "Adds 6 gold a year", yet income went from +14.5/turn to +19.3/turn. How do those relate?
- The Priority column ("Maintain") never changed; nothing showed how to set it or what it does.
- "(2 turns pass; nothing needed you until now)": the game skipped turns in which I wanted to act.
- Debt tier thresholds aren't shown. After the plague every domain went back to Stable with debt 0, even though the levels fell. Did the plague make things better?
- Some expectations stay fixed while others move in decimals (e.g., 54.3); unclear what sets them.
- "The pestilence is mild (severity 19.7): about 59 thousand dead" is jarring.
- The plague response options differed between games, with no reason given (hospice missing without a Circle?).
- Session 2's learn more said "response: hospice" for AD 162, but I had already left and never chose that.
- Session 3: Demetria asks for a promise though I never met her.
- The charter felt wasted ("chartered but not endowed, so it counts as bare"); I only learned the 60-gold threshold after spending everything.
- Corruption risk "High", yet "lost 0 to corruption". Was the warning meaningless?
- Pestilence recurrences (4, 7, 17 times, often catastrophic): history, my debt, or dice? `why medicine` after arrival gives nothing.
- Index sub-scores (e.g., Medicine 80 at departure while the level was 40.1) took a while to decode; the Index swings a lot turn to turn.
- The workshop became a street of forges once and a stable twice, with no hint why.
- Session 1's Wrongness beat said nothing was wrong ("almost exactly as the history books describe"); it fell flat.
- The physician "keeps open hours near the fountain", but I never fixed the fountain.
- Turns 14–20 were mostly "oversee, work, end", with nothing to decide.
- Nothing warned that unspent gold is lost at the jump.

## The three things I'd most want changed or explained
1. Show costs and effects before I commit: gold, Attention, tier and endowment thresholds ("needs 60 to count as endowed; a charter alone does nothing while you're away").
2. Explain the collapse after the jump, at least roughly (debt? leaving early? unspent gold?), and don't say I chose a response I never picked.
3. Give the mid-to-late era real decisions; make Attention and Priority visibly matter or explain them.

## What was done with this feedback
Text and UI items are fixed; see `../bugs.md` B6 and B10–B11 (costs, Attention left, hospice reason, lost gold, response attribution, Demetria's introduction, the physician text). Gameplay items are proposed, not changed: G1 (collapse spiral), G2 (late-era decisions), G3 (auto-advance), G4 (debt reset), G5 (odds), G8 (severity words). Some confusions come from how the sessions were scripted (priorities never changed; `help` and `why` rarely used) and should be re-checked with humans.
