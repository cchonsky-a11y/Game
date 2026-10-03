# Test Evidence Index

This file distinguishes executable evidence from simulated/design evidence.

---

## 1. Claude blind-agent round — seeds 611–620

Older build / blind-agent review.

Headline:
- six finished reports,
- mean score about 7.3,
- five scored ≥7,
- all would jump again.

Strong:
- arrival/jump payoff,
- causal Echo/`why`,
- premise.

Weak:
- mid-era grind,
- hidden plague costs,
- profit backlash,
- menu/batching friction,
- institution wording/control,
- early jump thinness.

Use as historical feedback, not current-build validation.

---

## 2. 50 actual executable runs — seeds 701–750

Files:
- `artifacts/butterfly_50_actual_execution_report_2026-09-28.md`
- `artifacts/butterfly_50_actual_execution_raw_2026-09-28.zip`

Results:
- 50/50 first arrival,
- 50/50 second arrival,
- 0 crashes,
- 0 unknown commands,
- 0 dead ends/impossible-state failures reported,
- first departure AD 159–172, median 166,
- second arrival AD 209–257, median 236.

Known harness issue:
- validator incorrectly assumed a 250-year jump; design expected 25–60 in that P0 context.

This is genuine executable evidence.

---

## 3. 100 executable blind-style runs, 10 batches of 10

Directory evidence came from `butterfly_blind100_batched10`.

Results:
- 100/100 valid executable sessions,
- 0 harness failures,
- 63/100 reached two jumps,
- 20 persona patterns.

Limitation:
- heuristic/persona-driven, not 100 real humans or fresh independent LLM agents.

---

## 4. 100 executable follow-up coverage runs

File:
- `artifacts/butterfly_blind100_followup_batched10.zip`

Results:
- 100/100 executed,
- 100/100 deliberately driven through two jumps.

**Do not compare 100/100 against earlier 63/100 as an improvement.**
The follow-up scripts intentionally forced full-loop coverage.

Important finding:
the executable still visibly had old systems:
- pouch gold,
- generic `odd/craft/consult` work,
- direct institution buy-in,
- menu batch-number instruction,
- old turn framing,
- no real betting loop,
- newer character/reputation/invitation systems not implemented.

---

## 5. 10×10 humanized chat-level design review

File:
- `artifacts/butterfly_humanized10x10_chat_current.zip`

Not executable. Not human.

Results:
- mean 8.45,
- median 8.46,
- 95/100 keep playing,
- 96/100 ≥8.

Dimensions:
- concision 8.74
- historical voice 8.12
- Roman culture 8.68
- character depth 8.26
- early reputation pacing 8.71
- intrigue 8.16
- work progression 8.32
- negotiation 8.29
- institution access 8.94
- games/betting 8.45
- state clarity 8.31

Common concerns:
- modern business language,
- early intrigue escalation,
- state visibility,
- negotiation needs real lost deals,
- private-life scenes,
- contract-ladder overuse,
- spectacle/emotional beats,
- betting uncertainty,
- culture becoming scheduled/checklist-like.

---

## 6. 100 simulated video-game critics, two-jump review

File:
- `artifacts/butterfly_100_critic_two_jump_sim_2026-10-01.zip`

Not human. Not independent executable agents.

Results:
- mean 8.12,
- median 8.15,
- 67/100 ≥8,
- 78/100 would continue,
- first jump payoff 7.94,
- second jump payoff 8.62.

Praise:
- future-inventor fantasy,
- second-jump payoff,
- Grand Challenges,
- Roman life interrupting engineering,
- Roman craftsmen as collaborators,
- relationships,
- replayability,
- machine mystery.

Criticism:
- dialogue explanatory/robotic,
- too many technical scenes,
- pacing rotation must be systemic,
- second jump needs personal echoes,
- micro invention sequences too long,
- NPCs need to derail optimal plans,
- culture can become a checklist,
- economy under-grounded,
- live design ahead of executable.

---

## 7. 10×1000 current humanized design simulation

File:
- `artifacts/butterfly_10x1000_humanized_current_2026-10-02.zip`

**10,000 modeled testers. Not 10,000 humans and not 10,000 independent agents.**

Results:
- overall mean 8.39
- median 8.45
- 70.2% ≥8
- 62.5% continue after two jumps
- jump payoff 8.71
- historical authenticity 8.58
- invention 8.36
- institutions 8.17
- technology ladder 8.41
- character depth 8.24
- pacing 7.82
- agency 8.34

Top risks:
- engineering dominance 19.0%
- too many active systems 17.8%
- functional dialogue 15.0%
- Roman baseline 13.7%
- exposition 11.9%
- tech acceleration too fast 10.9%
- institution gate 10.5%
- economy ledger 10.1%
- early game slow 9.8%
- NPC autonomy 8.8%

Core interpretation:
- do not delete depth,
- organize it through better menu/information architecture,
- preserve jump payoff,
- implement scene rotation,
- implement NPC autonomy,
- keep technology historically grounded,
- keep money explicit.

---

# Evidence hierarchy

When reporting project status:

1. **Executable tests** = build/runtime evidence.
2. **Humanized/persona simulations** = design stress-testing.
3. **Live in-chat play** = exploratory design evidence.
4. **Future real-human tests** = deferred until graphical/UI phase.

Never collapse these categories.
