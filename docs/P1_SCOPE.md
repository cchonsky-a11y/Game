# P1 — Playable Game Structure

**Status:** active

P1 turns the working P0 simulation into the architecture of the intended game. P1 is not a content-volume milestone. It establishes the player-facing information structure and the reusable state models that later content, graphics, and UI will consume.

## P1 target

A fresh player can arrive in AD 155, establish relationships, complete explicit paid or self-funded work, begin a Grand Challenge, earn institutional access through people, prepare the machine, make a major jump, and see persistent personal, technical, institutional, and unintended consequences through the intended game structure.

## Primary player-facing sections

1. **Now** — current month, immediate choices, interruptions.
2. **Projects** — active commissions, experiments, inventions, Grand Challenges.
3. **People** — relationships, household ties, obligations, NPC status.
4. **Institutions** — guest/member status, sponsors, invitations, obligations.
5. **Knowledge** — theories, discoveries, principles, experimental evidence.
6. **Civilization** — broader capabilities, adoption, propagation, bottlenecks.
7. **Machine** — time-machine systems, mystery, readiness, jump controls.
8. **Journal** — decisions, history, causal echoes, previous jumps.

Depth is retained under the hood. The UI should surface only information relevant to the player's current situation instead of presenting every system at the same level at once.

## P1 Sprint 1 foundations

### Scene routing

Meaningful scene categories:

- Engineering
- Personal / relationships
- Roman life
- Work / economy / reputation
- Machine mystery
- City / historical events
- Exploration
- Institutions / politics

After **two consecutive meaningful scenes of the same category**, a third is strongly deprioritized unless the player explicitly chooses to remain focused. Explicit focus does not make the world stop; natural interruptions may still occur.

### Project state

Substantial projects explicitly track:

- purpose
- owner/client
- payment/material arrangement
- duration in months
- current stage
- collaborators
- dependencies
- whether work can continue without the player

Before substantial work begins, the game states who is paying, what is exchanged, or that the inventor is deliberately self-funding it. No invisible free labor.

### Institution invitation gate

Consequential institutional access normally follows:

**Aware → knows member → guest → invited back → sponsored candidate → member → office/leadership**

A forward invitation requires a specific human causal chain:

**specific inviter + existing relationship + relevant work in that domain + demonstrated usefulness + inviter willing to accept the social/reputational risk**

General fame alone does not qualify. More open associations may support direct application where historically appropriate, but that is not the default path for consequential institutions.

## Deferred from P1 Sprint 1

- Full technology/capability graph data
- NPC autonomous project execution
- Complete economy migration
- Jump-echo data migration
- Final graphical presentation
- Five-real-human gate (moved to a later graphical milestone)

## Compatibility

The initial P1 models are additive so the P0 executable can continue to run while systems migrate incrementally.
