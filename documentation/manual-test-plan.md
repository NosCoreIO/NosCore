# Manual test plan

Most of what changes in this project is only observable in a running client. Automated
tests cover the arithmetic; this covers the part where a bar moves, a monster walks, or a
skill does what its description says.

Work through the sections that touch what you changed. Tick as you go.

## Setup

- [ ] LoginServer, MasterServer and WorldServer all start and stay up
- [ ] The banner prints and the character list loads
- [ ] Log in on a GM account (`$Help` lists the commands; if it does nothing, the account
      is not GM and none of the rest will work)

Useful commands: `$CreateItem <vnum> [amount] [upgrade]`, `$SetLevel`, `$SetJobLevel`,
`$Teleport`, `$Position`, `$Kill`, `$ChangeClass`, `$Invisible`, `$ClearInventory`.

Avoid `$Speed` while testing movement — it overrides the value the mount and buff paths
compute and will mask a regression.

---

## Combat

### Worn equipment contributes to stats

- [ ] Naked, open the stat window and note min/max attack and the three defences
- [ ] `$CreateItem` a weapon and an armour, equip both
- [ ] The stat window changes, and the numbers include the piece's own values
- [ ] Hit a monster before and after equipping — the damage is visibly different
- [ ] An upgraded piece (`$CreateItem <vnum> 1 <upgrade>`) contributes more than a +0 of
      the same vnum

Both the item model and the instance count. A +0 and a +10 of the same vnum giving the
same damage means only one of the two is being read.

### Combo chains

- [ ] With a class whose basic attack chains (skill 220 is the Swordsman's), attack the
      same target repeatedly
- [ ] The chain advances through its steps instead of replaying the first
- [ ] The animation and effect change between steps

### Skills that move somebody — BCard type 40

- [ ] **Push (subtype 11)** — the target is pushed back the declared number of cells
- [ ] **Pull (subtype 21)** — the target is drawn *towards* you, not taunted in place.
      Drawing Shot, Rotating Hammer and Spider King's Draw carry this
- [ ] **Charge (subtype 31)** — you move to the target
- [ ] In all three, a wall stops the movement in front of it — nobody ends up inside
      geometry or on the far side
- [ ] A value of 0 on a pull means "up against you", not "no movement"

### Damage as a percentage of HP — BCard type 37 subtype 31

- [ ] A skill declaring this takes the percentage off the target's **maximum** HP, not its
      current HP
- [ ] It stacks on top of the ordinary blow rather than replacing it
- [ ] It can kill, and the death is the ordinary one: corpse, rewards, respawn all behave
      as any other kill

Boss skills declaring 90% should take most of a bar in one hit.

### Guaranteed hit and guaranteed dodge — BCard type 16

- [ ] A skill with subtype 11 lands even against a target that normally dodges often
- [ ] A card granting subtype 21 causes misses against it
- [ ] With both in play nothing locks up or throws — the attacker's guarantee is resolved
      first by design

### Elemental resistance — BCard types 13 and 14

- [ ] A buff that raises your resistance to an element reduces the elemental damage you
      take from it
- [ ] A buff that lowers the enemy's resistance raises the elemental damage you deal
- [ ] An "all elements" effect applies to each of fire, water, light and dark rather than
      being ignored

### Multiplied attack and defence — BCard types 34 and 35

These state a factor, not a percentage, so the change is meant to be obvious rather than
subtle: a card worth 5 is fivefold.

- [ ] A buff carrying "defence is multiplied" makes the same monster's blows land for a
      small fraction of what they did a moment earlier
- [ ] The melee, ranged and magic subtypes each move only their own defence: check with
      three attackers, or one attacker switching weapons
- [ ] A buff carrying "attack power is multiplied" makes your own numbers jump by whole
      multiples, not by a few per cent
- [ ] The decreasing half halves rather than taking a couple of points off

---

## Monsters

### Respawn timing

- [ ] `$Kill` a monster and time the respawn against its `.dat` value: the field is in
      **tenths of a second**, so 400 means forty seconds
- [ ] Monsters do not flicker back within a second of dying
- [ ] A monster whose file value is 0 still respawns, after the one-second minimum

This one is easy to eyeball — before the fix everything came back a hundred times too fast.

---

## Character

### Specialist cooldown

- [ ] Untransform, relog within 30 seconds: wearing the SP again is refused with the
      side-effect message until the window ends
- [ ] Wait out the 30 seconds in-game: the red "side effect gone" line and the cleared
      cooldown arrive roughly on time
- [ ] Server restart inside the window keeps the refusal on the remaining seconds

### Mounts

- [ ] Mount a vehicle: the sprite changes **and** you move faster
- [ ] Dismount: the speed returns to the class value
- [ ] Speed survives a map change while mounted

### Class change

- [ ] `$ChangeClass` to another class
- [ ] The skill bar holds the new class's skills, not the old ones
- [ ] No leftover icons the client refuses to cast
- [ ] **Relog** and confirm the old skills have not come back — the rows behind the list
      have to be gone, not just the in-memory list

### Equipment gates

- [ ] Try to wear an item above your job level: the message says the **job level** is too
      low, not that you are the wrong class
- [ ] Try to wear an item for another class: that one still says wrong class
- [ ] `$SetJobLevel` above the requirement, then equip — it succeeds

---

## Regression sweep

Quick pass after any combat or stat change:

- [ ] Log in, walk, attack a monster, take damage, die, respawn
- [ ] HP and MP bars track the real values through all of it
- [ ] Pick up an item, equip it, unequip it
- [ ] Change map, then change back
- [ ] Log out and back in — nothing that was learned, worn or spent has reverted

---

## Not verifiable yet

Recorded so nobody spends time hunting for them:

- **BCard subtype names and additions** — data-only. Observable only once a skill or card
  using a given subtype is wired to behaviour.
- **The `st` percentage guard** — guards a zero maximum, which is not a state the game
  reaches. It cannot be triggered from the client by design.
- **Family experience and the `gidx` family id** — the family feature itself is not merged
  yet, so neither the corrected curve nor the flattened packet field has a path to the
  client.
- **Mates and instances** — same, still open.
