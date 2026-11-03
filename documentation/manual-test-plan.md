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

### Monsters near the map edge

- [ ] Pull a monster that spawns against the right or bottom border, run out of its range and
      let it walk home: it walks back and the server log shows no `AI tick failed`

---

## Character

### Mounts

- [ ] Mount a vehicle: a three-second bar runs, then the sprite changes **and** you move faster
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
- [ ] `$CreateItem` an item that binds when worn and double-click it: the client's own
      "cannot be traded after wearing" question opens, in the client's language
- [ ] Answer yes: the item is worn; answer no: it stays in the inventory

### Specialist transformation

- [ ] With SP points, log out and back in: the SP bar under the portrait still shows them, and
      transforming works
- [ ] Wear a specialist card with 0 SP points and 0 additional SP points, then transform:
      the client's own "You have 0 specialist points. Transformation cancelled." appears,
      in the client's language, and the character does not transform
- [ ] With points, transform: the five-second bar runs and the character transforms when it
      ends, not before

### Specialist wings

- [ ] Without transforming, use Angel Wings (`$CreateItem 1685`): "You must first transform
      into the specialist whose wings you want to change." appears and the item stays
- [ ] Transform with a +0 specialist and use them: "You can only change the specialist's
      wings when the specialist card has been upgraded to +1 or above." and the item stays
- [ ] Transform with a +1 or higher specialist and use them: the client asks "Would you like
      to change the wings of the current specialist?"; Cancel keeps the item
- [ ] Confirm: the character gets angel wings, "Your specialist wings have been changed."
      appears and the item is gone
- [ ] Untransform and transform again, and relog: the wings stay
- [ ] A second player who walks into the map, or logs in next to you, sees your specialist,
      its upgrade glow, its wings and your fairy

### Medals of Honour

- [ ] `$CreateItem 5899` and use it, confirming the client's "Use the item." box: a green
      "Reputation increased: +200" line appears in chat, the reputation window shows 200 more,
      and the medal is gone
- [ ] The Legendary Medal of Honour (`$CreateItem 5892`) adds 30 000
- [ ] A Specialist Card Holder (`$CreateItem 287`) does not grant reputation

### Magic Lamp

- [ ] Wearing any equipment, use a Magic Lamp (`$CreateItem 5105`): the client's own "remove
      your equipment" message appears and the lamp stays
- [ ] Naked, use it: the client asks whether to use the Magic Lamp to change your gender;
      Cancel keeps the lamp
- [ ] Confirm: the character's gender changes on screen, the gender-changed message appears and
      the lamp is gone
- [ ] Mounted, the lamp does nothing

### Wings of Friendship

Needs two characters who are friends, both online on the same channel.

- [ ] Open the friend list and choose to travel to the friend while holding Wings of Friendship
      (`$CreateItem 2160`): a three-second bar runs, then you appear next to the friend and one
      wing is gone
- [ ] Without the wings: the client's line saying you have no Wings of Friendship appears in chat
- [ ] With the friend logged out: the friend-offline message appears and the wings stay
- [ ] With either of you inside an instance (miniland, raid, time-space): the client's "cannot be
      used here" message appears and the wings stay

### Respawn point

- [ ] Talk to a village NPC that offers to save the respawn point and confirm: the client's
      own "This village is the designated place of revival." appears, in the client's
      language
- [ ] Die and choose to revive in the village: you come back at that NPC

### Dignity items

The maximum comes from the parser, so re-run it on an existing database first.

- [ ] Die a few times so dignity drops below 0 (each death costs 50)
- [ ] `$CreateItem 2156` and use it: the reputation window shows dignity 100 higher, a green
      "Dignity increased: +100" line appears in chat, and the item is gone
- [ ] `$CreateItem 2168` and use it: dignity lands on 100, and the chat line names only the
      points actually added
- [ ] Use another one at 100: "Dignity is at maximum." pops up and the item stays
- [ ] Relog — the restored dignity is still there

### Compliments

Needs two characters on the same channel. The commending one must be level 31 or higher
(`$SetLevel 31`).

- [ ] Right after logging in, commend the other player from their context menu: a red
      "You can commend someone again in N minute(s)." line appears, with N close to 60
- [ ] At level 30, commend after an hour online: a red "Only players above level 30 can
      commend other players." line appears and nothing changes
- [ ] At level 31 after an hour online: a green "You have commended <name>." line for the
      sender, a green "You have been commended by <name>." line for the other player, and
      the other player's compliment count in their info window goes up by one
- [ ] Commend again the same day, from either character on the same account: a red "You
      can only commend the player once per day and account." line appears
- [ ] In Glacernon (`$Teleport` to an Act 4 map), commend: a red "You cannot commend other
      players in Glacernon." line appears and nothing changes
- [ ] Relog both — the other player's compliment count is still there
- [ ] Keep the sender online for another hour after the relog, then commend again: the
      once-per-day line still appears, so the limit survived the relog

### Potions

- [ ] Take some damage, then use a Small Health Potion (`$CreateItem 1002`): the HP bar
      rises by 300 and one potion is gone
- [ ] A Large Recovery Potion (`$CreateItem 1010`) refills both bars, never past their maximum
- [ ] At full HP a health potion stays in the inventory, even with MP missing
- [ ] Potions dragged to the quick bar work the same way as from the inventory

### Resting in combat

- [ ] Sit next to a monster that attacks: you stand up as soon as its hit lands, and a second
      client on the map sees you stand too
- [ ] Between its hits you can sit back down
- [ ] Attack a monster and press sit: nothing happens and no message appears
- [ ] Stop attacking: sitting works again about three seconds after your last attack, even if
      the monster died sooner
- [ ] Sit with a Roasted Herring digesting and let a monster hit you: you stand up and the
      green numbers stop

### Pets

Capture is skill 209, which every new character has. Weaken a monster at or below your level to
under half health first.

- [ ] With no pet out, catch one: "capture successful" shows, the pet appears where the monster
      stood, it is in the party panel, and the pet window lists it
- [ ] With a pet already out, catch another: it joins the pet window only
- [ ] A failed roll says the capture failed; a monster above your level or above half health
      says why it cannot be caught
- [ ] Open the pet window: the list stays, it does not empty
- [ ] Change map: the pet comes along and stays in the party panel
- [ ] Log out and back in: every pet, its level and loyalty are still there
- [ ] With a second client on the map, log out: your pet disappears from their screen too

### Food and snacks

Lose some HP first, otherwise there is nothing to watch.

- [ ] Standing, use a Roasted Herring (`$CreateItem 2017`): the character sits down, the eat
      effect plays, and five green heal numbers of 140 appear two seconds apart
- [ ] Eat one and stand up after the first green number: the rest of the healing stops
- [ ] With nothing digesting, eat one Owlbear Steak (`$CreateItem 2472`): no "I'm full.", and its
      ticks heal 600 each
- [ ] Sitting, eat five in a row: the fifth says "I'm full." and is still used up, and no green
      number goes above 600
- [ ] Use Fried Chicken (`$CreateItem 2027`) while standing or fighting: it heals over five ticks
      without sitting, and keeps healing when a monster hits you
- [ ] Eat six Fried Chicken in a row: the sixth shows "I'm feeling sick..." in chat and stays in
      the inventory
- [ ] Food and a snack eaten together both heal
- [ ] At full HP, food and snacks are still used up

### Wing and Amulet of Return

- [ ] Away from the village, use a Wing of Return (`$CreateItem 2070`): it asks "Do you want to
      save your position and return to the village?"
- [ ] Confirm: after the five-second bar you land on your place of revival, and the wing is gone
- [ ] Use an Amulet of Return (`$CreateItem 2071`): it asks "Would you like to return to the place
      <map> (x, y)?" naming where you used the wing; Confirm, and after the bar you are back there and
      the amulet is gone
- [ ] With no point of return saved, the amulet says "You have not chosen a point of return."
      and stays in the inventory
- [ ] Use a wing somewhere else and Cancel: you still go to the place of revival and the wing is
      gone, but the amulet brings you back to the old point
- [ ] Inside a time-space or raid, either item says it cannot be used here
- [ ] Relog after using a wing: the amulet still knows the point of return

---

## Chat

### Hero chat

Run two world servers (two channels). You need a GM account to set reputation, and five
ordinary player characters on the same server: A and B, plus C, D and E to fill the ranking.
Staff are left out of the reputation ranking, so a GM cannot send hero messages. The ranking reads other characters' saved
reputation, so have each player relog after a `$SetReputation` on them.

- [ ] With the GM: `$SetReputation 100000 A`, and keep B and every other player character
      on that server below it; A relogs
- [ ] A on channel 1, B on channel 2. A opens the hero chat and sends "hello heroes": B sees
      `[A]:hello heroes` shown as a hero message, and A sees it too
- [ ] A sends a message with leading and trailing spaces: it arrives trimmed
- [ ] A sends an empty message: nothing appears on either channel and no error shows
- [ ] Give C, D and E more reputation than B (but less than A) and have them relog. B tries
      the hero chat: the client's own "Only the top three players from the rankings can use
      it." appears in red, in the client's language, and nothing reaches A
- [ ] Give C, D and E more reputation than A and have them relog: A is now refused with the
      same message

---

## Family

### Family membership

No in-game command creates a family yet, so seed one: a `Family` row, then a
`FamilyCharacter` row per member (`Authority` 0 head, 1 assistant, 2 manager, 3 member).
Log two members in on the same map.

- [ ] Each member sees the other's tag under their name as `Name(Rank)`, with the rank
      in the viewer's own language
- [ ] Your own tag shows under your own name after character select
- [ ] A character with no family has no tag
- [ ] Opening the family window shows the name, the head, the level bar and the notice,
      with the notice's spaces intact

---

## Instances

### Time-space entrances

No time-space ships in the seed data, so add one: a `ScriptedInstance` row with `Type` 0
(time-space) on a map you can reach, its `Script` holding the definition XML (level range,
required items, start room and coordinates). Stand on the stone.

- [ ] Walking onto the stone opens the time-space panel with its name, level range and
      rewards
- [ ] A character outside the level range is refused with a message and stays put
- [ ] A character without the required items is refused with a message and keeps what
      they had
- [ ] With the items, START moves you into the first room at its start coordinates
- [ ] The required items leave the inventory, across stacks if split, and no more than asked

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
- **Family experience** — nothing grants it yet, so the level bar only ever shows the
  seeded value.
- **Inside a time-space** — the entrance works, but rooms, objectives, lives and rewards
  do not run yet, so nothing past the first room can be checked.
- **Skipped delay bars** — the server ignores a bar's follow-up that arrives early or without
  the bar. The client always waits for its own bar, so only a modified client could send one.
- **Food and snacks in a miniland** — minilands are created after start-up and never get a
  life loop, so nothing that ticks per map (regeneration, digestion) runs there yet.
