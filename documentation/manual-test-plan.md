# Manual test plan

Most of what changes in this project is only observable in a running client. Automated
tests cover the arithmetic; this covers the part where a bar moves, a monster walks, or a
skill does what its description says.

Work through the sections that touch what you changed. Tick as you go.

## Setup

- [ ] LoginServer, MasterServer and WorldServer all start and stay up
- [ ] The banner prints and the character list loads
- [ ] Log in with the account password from a client not started by the launcher: the
      character list loads
- [ ] Log in on a GM account (`$Help` lists the commands; if it does nothing, the account
      is not GM and none of the rest will work)

Useful commands: `$CreateItem <vnum> [amount] [upgrade]`, `$SetLevel`, `$SetJobLevel`,
`$Teleport`, `$Position`, `$Kill`, `$ChangeClass`, `$Invisible`, `$ClearInventory`.

Avoid `$Speed` while testing movement — it overrides the value the mount and buff paths
compute and will mask a regression.

---

## Movement

### Walking pace

The server allows each walk packet only once the time since the last accepted one covers the
cells it moves, at 2500 / speed milliseconds a cell with some slack. That rate is the one the
walk check has always assumed; it has not been timed against a client, so this section is what
confirms it. The server log line `walk dropped` names every refused packet.

- [ ] Walk across a large open map by clicking far ahead, then by holding the mouse down, for a
      minute each: the character never snaps back and the log shows no `walk dropped`
- [ ] Do the same on a vehicle, and with a speed buff active: no snap-back
- [ ] Let a speed buff run out, or dismount, in the middle of a long walk: the walk carries on
- [ ] Walk with a second client watching: the walker moves smoothly on both screens and stops on
      the same cell on both
- [ ] Walk along and around walls, through doorways and round corners: nothing is refused
- [ ] On a laggy connection (throttle the client's network), walk for a while: an occasional
      snap-back is acceptable, a steady stream of them is not
- [ ] Cast skills while walking: the casts land and the character is not pulled back
- [ ] Not verifiable from a stock client: packets sent faster than the character walks, or to a
      cell behind a wall, are refused and the client is put back on the server's cell

---

## Combat

### Worn equipment contributes to stats

- [ ] Naked, open the stat window and note min/max attack and the three defences: they are
      the level and class values, not zeros
- [ ] `$CreateItem` a weapon and an armour, equip both
- [ ] The stat window changes as soon as each piece goes on, and again when it comes off,
      and the numbers include the piece's own values
- [ ] Level up with the window open: the base attack and defences go up
- [ ] Hit a monster before and after equipping — the damage is visibly different
- [ ] An upgraded piece (`$CreateItem <vnum> 1 <upgrade>`) contributes more than a +0 of
      the same vnum
- [ ] Transformed into the Ninja card with a water fairy, hit a monster, then wear wing skin
      4442 ("+20 to the equipped fairy's element") and hit again: the second hit is higher

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

### The equipped fairy's element — BCard type 96 subtypes 0 and 1

- [ ] With a fire fairy worn, note a fire skill's hits on a monster, then gain an effect that
      raises "the equipped fairy's element" (an Elemental Blessing buff, or gloves such as
      Steam Gloves): the same skill hits harder
- [ ] Take the fairy off with the effect still running: the hits drop back to no elemental
      bonus at all, the effect does not stand in for a fairy
- [ ] An effect that lowers the fairy's element makes the same skill hit softer

### Casting range

A targeted skill reaches its range plus one cell, measured on the truncated diagonal
distance the walk check uses.

- [ ] Cast a targeted skill at a monster from inside its range: it lands
- [ ] Back away until the monster is more than range + 1 cells off and cast again: the
      cast is refused, the auto-attack stops, no mana is spent and the skill does not go on
      cooldown
- [ ] Step back in and cast immediately: it lands, so the refused cast started nothing
- [ ] A skill centred on yourself (a buff, an area around you) still casts with nothing
      in reach
- [ ] Select a monster far away and cast an area skill centred on yourself: the cast is
      refused; walk up until the monster is inside the area and cast again: it lands, and
      only the monsters around you take damage, not the ones around the selected monster
- [ ] A melee skill whose data has no range (most basic attacks) lands next to a monster and
      is refused from two cells further away
- [ ] Cast while walking: the hit lands and you are not pulled back to an old cell

### Area skills and bystanders

- [ ] Stand a second character next to a monster and cast an area skill at the monster: the
      monster takes damage, the bystander takes none
- [ ] A monster's area skill still hits every character standing in it

### Ground-targeted skills

A skill cast at a cell (`u_as`) hits every monster within its target range of that cell.
The cell must be within the skill's range plus one cell of the caster.

- [ ] Gather three monsters, two near each other and one a few cells away. Cast a ground
      skill on the pair: both take damage, their numbers are drawn as area hits, and the
      third monster is untouched
- [ ] Stand a second character inside the area: it takes no damage, and neither does the
      caster when standing in its own area
- [ ] A monster killed by the area dies, gives its experience and drops loot like a single
      kill
- [ ] Cast on empty ground: mana is spent and the skill goes on cooldown
- [ ] Recast before the cooldown resets: the cast is cancelled and nothing is hit
- [ ] Drop below the skill's mana cost and cast: "You don't have enough MP to cast the
      spell." appears in the client's language and nothing is spent
- [ ] Click a cell well beyond the skill's range: the cast is cancelled and nothing is spent
- [ ] The skill icon comes back when the cooldown ends

### Skills on a list of targets

Some skills (Finishing Blow, 816) make the client send `mtlist` with the extra monsters it
picked, followed by the `u_s` that casts the skill on the main target. That cast also hits
each listed monster within the skill's range plus one cell of the caster, and costs mana and
cooldown once.

- [ ] Line up three monsters, target the nearest and cast the skill: all three take damage,
      and mana drops by one cast's cost
- [ ] With one listed monster moved out of range, the others are still hit and that one is not
- [ ] Cast a single-target skill right after: only its target is hit
- [ ] Recast before the cooldown resets: nothing is hit

---

### Experience from kills

- [ ] At level 5 and job level 5, kill a Baby Dander (60 XP, 120 job XP): `lev` shows 180
      XP and 360 job XP, three times the monster's values
- [ ] At level 15, the kill gives twice the experience; at level 20 and job level 21, the
      plain amount
- [ ] In a group of two at the same level, each member gets 75 % of the kill; in a group of
      three, 70 %

## Monsters

### Aggro and chasing

- [ ] Walk toward a hostile monster: it notices you inside its notice range and comes for you
- [ ] It stops **beside** you rather than on your cell, then attacks
- [ ] Attacks come at an even pace, not in bursts
- [ ] Run off: it follows, then gives up; a monster that gave up walks home at the pace it
      chased at and wanders around its spawn point from there
- [ ] A monster that is not hostile ignores you until you hit it, then fights back
- [ ] Kill a monster: XP arrives, the drops appear where it fell, and it respawns at its
      spawn point

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
- [ ] Walk, cast a skill, or take a hit during the mounting bar: you don't mount
- [ ] Start the mounting bar and transform into your specialist straight after: the specialist
      bar doesn't finish off the mount, and the other way round

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

- [ ] Put a specialist card in the bag (not worn), log out and back in: it is still in the
      EQUIP tab at its slot, with its upgrade and perfection
- [ ] With SP points, log out and back in: the SP bar under the portrait still shows them, and
      transforming works
- [ ] Wear a specialist card with 0 SP points and 0 additional SP points, then transform:
      the client's own "You have 0 specialist points. Transformation cancelled." appears,
      in the client's language, and the character does not transform
- [ ] With points, transform: the five-second bar runs and the character transforms when it
      ends, not before
- [ ] Wear a specialist card whose instance carries attack and defence, hit a monster, then
      transform and hit it again: the transformed hits are larger by about the card's attack
- [ ] Untransform: the hits go back to the untransformed size
- [ ] After re-running the parser on an existing database, transform with a card at
      specialist level 0: the skill bar shows the card's first six skills and the basic attack
      hits. At level 4 the seventh appears, and at level 20 all eleven
- [ ] Wear a fire fairy and use a fire skill on a monster: the hits are larger than the same
      skill with no fairy worn
- [ ] Open the character window while transformed and untransformed: attack, defence and
      the resistances move with the card, and the card counts for nothing until transformed

### Specialist card upgrade

- [ ] Put a +0 specialist card in the bag with angel feathers, full moon crystals and green
      souls, open an Alchemist's shop and press the card upgrade button, then drag the card in:
      the window shows 80 % success, 0 % soul destroyed, 20 % failure, 200 000 gold, 1 full
      moon, 3 feathers and 2 souls
- [ ] Press Upgrade and confirm: on success "Upgrade to next level successful." appears, the
      card turns +1 and the gold, feathers, full moon and souls go down
- [ ] On a failure, the card keeps its level, the souls stay and the rest is spent
- [ ] Upgrade a +9 card at job level 41 or more until the soul is destroyed: "Specialist
      Card's soul was destroyed." appears and the card can no longer be upgraded
- [ ] Try a +5 card below job level 41, or with too few feathers: the matching message
      appears and nothing is spent
- [ ] Drag a +15 card in with 32 full moons, 80 feathers and 2 Dragon Gems (2630): the
      window shows 1.2 % / 70 % / 28.8 %, 1,250,000 gold and the Dragon Gems, and Upgrade
      rolls the attempt instead of saying the maximum was reached
- [ ] A +20 card cannot be upgraded further

### Event specialist card upgrade

The odds are those of a plain card upgrade; no source says whether an event card's own
window uses other ones, so the percentages the window shows are the check.

- [ ] `$CreateItem 900` (Pyjama card) and `$CreateItem 5207` (Pyjama Upgrade Scroll), then
      double-click the scroll away from any NPC: the card upgrade window opens and the scroll
      stays in the bag
- [ ] Drag the card in: note the success, soul destroyed and failure percentages the window
      shows, and that it asks for no gold and no other material. Compare them with the plain
      upgrade window's for the same level above; a difference means the event odds are wrong
- [ ] Press Upgrade: on success "Upgrade to next level successful." appears, the card turns +1,
      one scroll is gone and the gold has not moved
- [ ] On a failure the card keeps its level and the scroll is still spent
- [ ] Do the same with the Chicken card (907) and its scroll (5107), and the Pirate card
      (4099, and the event copy 4100) with its scroll (5519)
- [ ] Drag the Chicken card into the Pyjama window: "This card cannot be upgraded." appears and
      nothing is spent
- [ ] With the window open but no scroll left, press Upgrade: "Not enough ingredients" appears
- [ ] With the scroll's window open, an NPC upgrade sent by a modified client does nothing (not
      verifiable from a stock client)

### Equipment upgrade

- [ ] Put a +2 weapon of rarity 8 in the bag with Cella Powder (1014) and Soul Gems (1015),
      open Teoman's Use Shop, pick the upgrade tab and drag the weapon in: the window shows
      90 % success, 10 % fixed, 0 % fail, 3,000 gold, 80 Cella Powder and 2 gems
- [ ] Press Upgrade and confirm: the item is never destroyed, and exactly 3,000 gold is taken
- [ ] A +7 weapon of rarity 0 and one of rarity 8 show the same 5 % / 3 % / 92 % and the
      same 400,000 gold, 380 Cella Powder and 2 full gems
- [ ] A +10 weapon shows 1,250,000 gold, 1200 Cella Powder, 4 Dimensional Force (2774) and
      1 shard (2748), and can be upgraded up to +13
- [ ] Each failed or protected attempt from +10 fills the pity gauge bar in the item
      window's corner; a full bar makes the next attempt succeed and empties it
- [ ] Below +10 the bar never appears
- [ ] With a Gold Upgrade Scroll (`$CreateItem 5369`) in the bag, upgrade through the gold
      scroll option: one gold scroll is used, any plain scrolls (1218) stay, and a failed
      attempt keeps the item instead of destroying it
- [ ] With plain scrolls but no gold scroll, the gold scroll option is refused with the
      missing ingredients message and nothing is taken
- [ ] The gold scroll window (`wopen 43`) asks for half the gold and Cella Powder of the plain
      upgrade (40 000 gold and 110 Cella at +5), and the upgrade takes exactly that

### Specialist points

- [ ] Wear a level 60 specialist card and right-click the SP button by the portrait: the card
      window opens with its level, name and the skills it has unlocked
- [ ] In the bag's EQUIP tab, right-click a specialist card: the same window opens for it
- [ ] Transformed, spend points on attack, defence, element and HP/MP: the free points go
      down by the cost of each level, and the character window's damage, defences,
      resistances, element rate, HP and MP rise
- [ ] Try to spend more points than the card has, or spend them untransformed: nothing is
      spent; untransformed, the client says the card must be equipped
- [ ] Use a Point Initialisation Potion (`$CreateItem 1366`) through the reset button: every
      point comes back, one potion is gone and the client says the points were restored
- [ ] Reset again with nothing spent: the client says the card was already reset and the
      potion stays; reset without a potion: the matching message appears
- [ ] A level 99 card at +15 has 410 points; at +20 it has 486, and attack can be raised
      past level 100 up to 120

### Specialist card perfection

- [ ] Put a specialist card in the bag with its perfection stones (a Warrior card takes Small
      Rubies of Completion), open an Alchemist's shop, press the SP Perfection button and drag
      the card in: the window asks 5 000 gold and 1 piece
- [ ] Press Upgrade: on success "Perfecting the SP successful: +1 (...)" names the stat that
      went up, and the gold and one stone are gone
- [ ] On a failure "Perfecting the SP failed" appears and the gold and stone are still spent
- [ ] With a card at perfection 20 the window asks 10 000 gold and 2 pieces
- [ ] Without the card's own stone, "Not enough ingredients" appears and nothing is spent

### Soul Revival Stone

- [ ] With a specialist card whose soul was destroyed in the bag, double-click a Soul Revival
      Stone (1365 or 9039) and click the card: the client asks "Would you like to use Soul
      Revival Stone?"
- [ ] Cancel: nothing changes. Confirm: "The soul finds a new lease of life." appears, one
      stone is gone, the SP bar fills by 10 000 points and the card can be worn again
- [ ] Drop the stone on a card whose soul is intact, or on any other item: nothing happens and
      the stone stays

### Specialist card holder

The holders are the box items with effect 69 and effect value 1 or 2, such as 287; the others:
`SELECT "VNum" FROM "Item" WHERE "ItemType" = 5 AND "Effect" = 69 AND "EffectValue" IN (1, 2);`

- [ ] `$CreateItem 287` and a specialist card, raise the card (`$CreateItem <card> 1 7`,
      spend some points, perfect it once). Double-click the holder: the card holder window opens
- [ ] Drop the card on the window: the window closes, "You have placed the Specialist Card in
      the Card Holder." appears, the card's slot empties and the holder stays in its slot
- [ ] Log out and back in: the filled holder is still in the bag
- [ ] Double-click the filled holder: "The Specialist Card has been removed from the Card
      Holder." appears, the holder's slot empties and the card is back. Its window shows the same
      level, upgrade, perfection and spent points as before
- [ ] Trade the filled holder to a second character, who takes the card out: the card arrives
      with its progress intact
- [ ] Transform, then drop another card on an empty holder's window: "You cannot put
      Specialist Cards in the Card Holder while the transformation is in progress." appears
      and both items stay as they were
- [ ] Drop a card whose soul was destroyed: "You cannot put this Specialist Card in a Card
      Holder." appears and nothing moves
- [ ] With every other bag slot full, double-click a filled holder: the not-enough-space
      message appears and the card stays inside

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

### Costume fusion

Which window slot keeps the effects and which lends the look is taken to be the first and
the second; a fusion that swaps them means the order is reversed. No message is sent on
success, on the assumption that the window says "Appearance changed" itself.

- [ ] `$CreateItem 189` (Smart Men's Suit, permanent) and another permanent suit, wear and
      take off each so both are bound, `$CreateItem 5874` (Costume Fusion) and have a million
      gold. Double-click the Costume Fusion item: the fusion window opens and the item stays
- [ ] Put the first suit in the effects slot and the second in the design slot and confirm:
      the second suit is gone, the first keeps its own name and effects, one Costume Fusion
      item and 1,000,000 gold are spent, and the window reports the change. If nothing at all
      appears, the success message is missing
- [ ] Wear the fused suit: the character shows the second suit's look, to you and to a second
      player who walks into the map, and still after a relog
- [ ] Try to fuse the fused suit again: "You cannot use an item whose appearance has been
      changed." appears and nothing is spent
- [ ] Try a 30-day costume: "Both materials must be permanent."; an unbound costume: "Both
      materials must be bound (not tradeable) to change the appearance."
- [ ] With less than a million gold, or without the Costume Fusion item, the fusion is refused
      and nothing is spent
- [ ] A hat and a suit, or a men's and a women's costume, cannot be fused
- [ ] Not verifiable yet: the item's tooltip showing the look it took; the costume tooltip is not
      sent by the server at all yet

### Medals of Honour

- [ ] `$CreateItem 5899` and use it, confirming the client's "Use the item." box: a green
      "Reputation increased: +200" line appears in chat, the reputation window shows 200 more,
      and the medal is gone
- [ ] The Legendary Medal of Honour (`$CreateItem 5892`) adds 30 000
- [ ] A Specialist Card Holder (`$CreateItem 287`) does not grant reputation

### Buff potions

- [ ] `$CreateItem 1246 3` and double-click the Attack Potion in the Main tab: the stack drops
      to 2 and the Attack Potion buff icon appears top left for 10 minutes
- [ ] The Defence, Energy and Experience potions (1247, 1248, 1249) show their own buff icon
- [ ] Dead, the potion stays in the inventory

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
- [ ] Walk, or cast a skill, during the bar: the bar stops, you stay, and the wings stay

### NPC teleporters

Run the parser's teleporter import first (the full import does it), then stand next to each kind
of teleporter NPC with at least 10 000 gold. A database imported before teleporters recorded their
menu needs the import run again: until then those destinations take nobody anywhere.

- [ ] NosVille, Western Krem and Port Alveus Square teleporters: the NosVille option is free,
      Krem costs 1 000 gold and Port Alveus 2 000, and each lands in the town the menu names
- [ ] Act V teleporters (Mortaz Desert Port and the other two Act V camps): 0, 5 000 and
      10 000 gold for the first, second and third destination
- [ ] Port Alveus butler, "Go to Wedding Gazebo. (500 Gold)": 500 gold and the gazebo
- [ ] The Water Grotto guide, the ferry to the Forgotten Archipelago and the ship back from
      the Mortaz Desert Port are free and land on their map
- [ ] Below the fare: the client's not-enough-gold message in yellow, the gold stays and
      nobody moves
- [ ] After re-running the import on an existing database, every teleporter above still works

### Changing map

Two clients, A and B.

- [ ] A walks through a portal onto B's map: B sees A appear once, and A sees the new map's
      monsters, NPCs, items and B
- [ ] A, while a monster is hitting them, uses a return item: A arrives with the HP the last
      hit left, and the players on the old map see A disappear
- [ ] A steps on a portal and uses a return item in the same instant: A ends up on one map,
      can keep walking, and the server log shows no map-change warning
- [ ] A leaves a map as its last player while B walks onto it: half a minute later the
      monsters there still move for B
- [ ] A opens their miniland, B visits, A closes it: B is sent back to their own map and A's
      miniland keeps working on the next visit

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

### Picking up and using items

- [ ] Drop an item and pick it up again: it lands in the bag, the ground item disappears for
      everyone on the map, and the "received" message names it once
- [ ] Two characters standing on the same dropped item press pick-up at the same moment: only
      one of them receives it; the other sees it disappear
- [ ] Spam the pick-up key on a stack of gold: the gold counter rises by that stack only once
- [ ] With the bag full, try to pick up an item: the not-enough-space message appears and the
      item stays on the ground for anyone to pick up
- [ ] Spam double-click on a single box or sealed tarot card: one reward arrives and the box or
      card is gone; with a stack of two, two rapid double-clicks give two rewards and empty the stack
- [ ] With the bag full, double-click a box: the not-enough-space message appears and the box
      stays in its slot
- [ ] Use a title item and a speaker: both still work as before

### Inventory sort

- [ ] Scatter a few different items over the MAIN tab with gaps between them, press the sort
      button at the top of the inventory and confirm: they move to the first slots, in
      alphabetical order of their names, with no gaps
- [ ] The same on the EQUIP and ETC tabs; the other tabs are not touched
- [ ] Two stacks of the same item stay two stacks, the larger first
- [ ] Relog: the sorted order is still there
- [ ] On an account in another language, the order follows that language's names
- [ ] With a personal shop or an exchange open, sorting does nothing
- [ ] Leave gaps between the cards in the card inventory and between the costumes in the
      costume inventory, then open them from the ADDITIONAL tab: the cards close up in order
      of the job level they need, the costumes in item number order

### Potions

- [ ] Take some damage, then use a Small Health Potion (`$CreateItem 1002`): the HP bar
      rises by 300 and one potion is gone
- [ ] A Large Recovery Potion (`$CreateItem 1010`) refills both bars, never past their maximum
- [ ] At full HP a health potion stays in the inventory, even with MP missing
- [ ] Potions dragged to the quick bar work the same way as from the inventory
- [ ] Use potions from the quick bar as fast as the client lets you: each one heals and is used
      up, none is lost
- [ ] In Act 4, use a potion and another one straight after: the second stays in the inventory
      and heals nothing; five seconds later it works

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
- [ ] Capture spends its mana and goes on cooldown, whether the roll succeeds or fails; casting
      it again before the cooldown is over is refused
- [ ] Capture from further away than the skill's range is refused and catches nothing
- [ ] Order a pet out with you to attack a monster: it hits, then waits its usual attack delay
      before the next hit even if the order is spammed
- [ ] Order the pet to attack a monster far from where the pet stands: nothing happens
- [ ] Open the pet window: the list stays, it does not empty
- [ ] Change map: the pet comes along and stays in the party panel
- [ ] Log out and back in: every pet, its level and loyalty are still there
- [ ] With a second client on the map, log out: your pet disappears from their screen too

Managing NosMates. Double-click one of your own NosMates to get Company / Stay / Send back
under its target bar. `/mnt/project-files/pets-partners/reset-mates.sh` sets up a pet and a
partner out plus one of each at home, one of them above your level.

- [ ] Enter your Miniland: the NosMates that are not out stand there, and they are still there
      after leaving and coming back
- [ ] In the Miniland, Company on a NosMate at home: it replaces the one of the same kind in the
      party panel, and the one it replaced stays in the Miniland
- [ ] Company on a NosMate above your level: "Cannot set up NosMates whose level is higher than
      the player's." and nothing changes
- [ ] In the Miniland, Stay or Send back on the one following you: it leaves the party panel and
      stays where it stood
- [ ] Outside the Miniland, Send back: "Should the NosMate return to the Miniland?"; Confirm shows
      a three-second bar, then the NosMate disappears with "<name> has been sent back to the
      Miniland." in the centre and in chat, and it is standing in the Miniland next time
- [ ] Target a NosMate: its level shows on the target bar instead of 0
- [ ] With a pet in the team, kill a monster that gives experience, then reopen the pet's page
      in the character window: its experience went up. A kill that fills the bar plays the
      level-up effect on the pet, and the page shows the next level with HP and MP refilled
- [ ] A pet one level below you keeps a full bar instead of levelling; a kill that levels you up
      levels the pet on that same kill
- [ ] Repeat with a partner in the team: its page in the character window shows the gain, and a
      full bar levels it with the level-up effect (not yet checked in the client)
- [ ] A pet left in the Miniland gains nothing from your kills
- [ ] Relog: the pet's level and experience are kept

#### Partner equipment

Partner weapons and armour are their own items (weapon sub-type 12, armour sub-type 4); any
gloves and boots fit.

- [ ] Character window, partner tab: drag a partner weapon onto the partner. It leaves the
      Equipment bag and shows in the partner's weapon slot
- [ ] Drag a second partner armour onto the same partner: the first one comes back to the bag
- [ ] Drag your own weapon, or gear above the partner's level, onto the partner: "The partner
      cannot use this equipment." and nothing moves
- [ ] Take a piece off the partner: it is back in the Equipment bag; with a full bag it stays on
      and the bag-full message shows
- [ ] Log out and back in: the partner still wears its gear

#### Partner specialist cards

A partner specialist card is a specialist with sub-type 4. Bring a partner out and have such a card
in the Equipment bag; a second player on the map watches the partner.

- [ ] Drag the card onto the partner: it shows in the partner's specialist slot. A player's own
      specialist card is refused with "The partner cannot use this equipment."
- [ ] With the card on, open the Character window's partner tab: the card shows in its slot and the
      partner's HP, MP and stats read the same as without it (nothing is shifted by one field)
- [ ] Press the partner's transform button: a five-second bar runs and the partner dances. Walking
      during the bar cancels it
- [ ] When the bar ends the partner takes the card's look, for you and for the watcher, with the
      transformation effect; its speed is unchanged
- [ ] Press the button again: the partner turns back at once, "Duration of transformation side
      effects: 30 seconds" shows in red and the button greys out
- [ ] Press it within the 30 seconds: "Cannot transform because of transformation side effects"
      with the seconds left, and no bar. After 30 seconds the bar runs again
- [ ] Transformed, take the card off, or send the partner home: it turns back and the cooldown
      starts
- [ ] Without a card on the partner the button says "No Specialist Card equipped."
- [ ] Transformed, change map: the partner arrives transformed

#### Pet and partner items

The client applies these to the NosMate you have targeted: click your pet or partner on the map,
then double-click the item and confirm. `/mnt/project-files/mate-items/reset-mate-items.sh`
sets up a level 30 character, a level 5 pet and a level 10 partner out, a level 3 pet at home,
and the items in the Etc bag.

- [ ] Gourmet Pet Food (2158) on a pet five or more levels below you: it goes up one level with
      the level-up effect, the pet window shows the new level, and the food is used up
- [ ] The same food on a pet less than five levels below you: "Your pet cannot currently level
      up or it would no longer respect you!" and the food stays. Partner Medicine (2159) does
      the same for a partner
- [ ] Pet food on a partner, or partner medicine on a pet: nothing happens and the item stays
- [ ] Your hair does not change when you use any of these
- [ ] Special Pet Food (2187): "The pet can collect items from now on." and the pet window shows
      the pick-up ability; a second one on the same pet stays in the bag
- [ ] NosMate Summoning Book (2419) on a NosMate that cannot be summoned: it can be now; on one
      that already can, "You have already used the NosMate Summoning Book for this NosMate."
- [ ] Release Box (2088) on the pet following you: "You cannot release your NosMate while it is
      accompanying you." On the pet at home it disappears from the pet window and the Miniland
- [ ] Handkerchief of Separation (2171) on a partner wearing gear: "You can only use the
      Handkerchief of Separation when your partner is not carrying any more equipment."
- [ ] With two partners, release the first one: the second keeps its gear after logging out
      and back in
- [ ] Skin for Bob (1103) on Bob: Bob takes the costume look, for you and for a second player
      on the map, and keeps it after changing map; on another partner "You cannot receive
      costumes that do not fit you."

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

### Ordering NosMates

Have a pet and a partner out, and a second client on the same map watching.

- [ ] Order the pet to a free cell a few steps away: it walks there on your screen and on the
      second client's, and stays there until you walk
- [ ] Order the pet and the partner together: both walk, each to its own cell
- [ ] Walk after ordering: both NosMates come back to your side
- [ ] Order the pet onto a wall or into water: it does not move on the second client's screen
- [ ] Become invisible (`$Invisible`) and order the pet: the second client does not see it move,
      and you still do
- [ ] Send the pet back to the Miniland, then try to order it from the party panel: nothing
      happens
- [ ] Become visible again and take the pet back out with you, then make it talk: a speech
      bubble with your sentence shows over the pet on both screens, and the sentence is not cut
      at its first space
- [ ] With a pet that has skills of its own (a pet without any only sends a move order when a
      monster is clicked), select a monster next to the pet and use one of the pet's skills on
      it: the pet plays the skill, the damage number shows on both screens and the monster's bar
      drops
- [ ] Use the same pet skill again at once: nothing happens until its cooldown is over
- [ ] Kill a monster with a pet skill: it collapses as it does from your own hits, and what falls
      is what an ordinary kill of the same monster on the same map drops (drops are rolled, so
      compare a few kills); the pet's share of the damage gives no experience yet, to you or the
      pet
- [ ] Use a pet skill on a monster across the map: the pet does not hit it
- [ ] Use a pet skill on another player or on an NPC: nothing happens

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
- [ ] Use a wing, then walk during the bar: the bar stops and you stay where you are, with the
      wing still in the inventory
- [ ] Use a wing while a monster is hitting you: the client drops the bar on the hit, sends no
      follow-up, and you stay with the wing

### NPC services need the NPC

NPC menus, shops, the bank, the NosBazaar and the upgrade windows only answer while the character
stands within 5 cells of the NPC whose dialog was opened, on the same map. Windows opened by an item
(savings book, NosBazaar medal) work anywhere on the map they were opened on.

- [ ] Click a shop-only NPC (no dialog), buy an item and sell one back: both go through
- [ ] Click a shop NPC whose dialog offers the shop, pick the shop and buy, then sell the item back:
      both go through
- [ ] Click a teleporter NPC from right next to it and pick a destination: the teleport and the fare
      behave as under "NPC teleporters"
- [ ] Open the Cuarry Bank window at the Representative, walk away and deposit: no gold moves; open it
      with the savings book anywhere and deposit: the deposit goes through
- [ ] Open the NosBazaar at its NPC, walk away and search, list or buy: nothing happens; with a medal,
      the NosBazaar skill opens a working NosBazaar away from any NPC
- [ ] Open an upgrade window (equipment upgrade, specialist upgrade, cellon) at its NPC and upgrade:
      it works; walk away first: nothing is consumed
- [ ] Open a private shop on one character, stand next to it with another and buy: it works
- [ ] Respawn point: the NPC's question still appears and confirming saves the point (see
      "Respawn point")
- [ ] Change map with a shop or bank window still open and use it: nothing happens until an NPC is
      clicked again

### Parcels and private shops

- [ ] `$Gift 1012 5 0 0 <your name>`: chat shows "Gift has been delivered." and, once,
      "Parcel received >> Seed of Power x 5" in the client's language; the parcel icon lights up
- [ ] Send yourself three gifts in a row, then open the parcel window: each is listed once, and
      taking each one puts exactly that item in the inventory and removes only that parcel
- [ ] With the main inventory full, take a parcel: "Not enough space" shows, the parcel stays in
      the list, and taking it again after freeing a slot delivers it
- [ ] Delete a parcel and a note from the post box: each disappears and stays gone after a relog
- [ ] Restart the master server with parcels waiting, then log in at once: every parcel is listed
- [ ] `$SetReputation` above the current value shows "Reputation increased: +N" in green, below it
      "Reputation reduced: -N" in red, with N the difference; the same value shows nothing
- [ ] Give a player `$SetGold` up to the gold cap and have them open a private shop; buying from
      it shows the client's "Trade failed because shop owner has too much money." and no gold moves

### Buying from a private shop

- [ ] Open a private shop with a stack of 3 and a single item; from another character, buy 2 of the
      stack: the buyer gets 2, the owner keeps 1 in the same slot and receives the gold
- [ ] Buy the last item of the shop: the shop closes on the owner's side and for everyone around
- [ ] Sell a weapon with shell options from a private shop: after buying, the buyer's weapon shows the
      same options, and still does after both characters log out and back in
- [ ] Log out and back in on both characters after a sale: the bought item, the rest of the stack and
      the gold are kept, and neither character loses an item or reports a save error in the server log
- [ ] Two buyers press buy on the same stack at once: one gets it, the other's shop window updates
      and nothing is charged to them
- [ ] Close the shop, open it again, then click the owner from another character: the item list
      opens once
- [ ] With a shop open, take a portal or log out: the shop disappears for everyone around

### Cuarry Bank

The packet trace has no Cuarry Bank Representative on any map yet; for a test, add a `MapNpc` row with
VNum 2692 and Dialog 535 (bank) or 536 (VIP services, savings book) next to the character. Items
are easiest to use from the quick bar: a `QuicklistEntry` with Type 0, IconType = pocket and
IconVNum = slot puts an inventory slot on it.

- [ ] Talk to the Cuarry Bank Representative and pick "Use Bank Facilities": the Bank window opens
      with the credit rating and fee of the character's reputation (Red Elite: "Gold 1", "Fee 66,000"),
      the gold in custody and the gold carried
- [ ] Deposit 1500: the question reads "Do you want to deposit 1500,000 Gold?"; Confirm moves
      1 500 000 gold from "Gold in possession" to "Gold in custody" and the log shows "You deposit"
      and "Balance" lines; Cancel moves nothing
- [ ] Withdraw 500: the question quotes the fee; Confirm adds 500 000 minus the fee to the gold
      carried and logs "You withdraw ... (Fee: ...)"
- [ ] Withdraw with less gold carried than the fee: "You don't have enough Gold to pay the fee." and
      nothing moves; withdrawing past the gold cap: "You have exceeded the amount of Gold you may carry."
- [ ] With a private shop open, "Use Bank Facilities" answers "You cannot use the bank services while
      you have a private shop open."
- [ ] Log out and back in: both balances are kept
- [ ] Use a Cuarry Bank VIP Medal (5838, 10 days; 5839, 30 days): the chat shows "Cuarry Bank VIP
      Medal (10 Days) effect activated" and the medal is used up; the Bank window then reads "VIP" with
      "Fee 0", and a withdrawal logs "(Fee: 0 Gold)" and charges nothing. A second medal is not used
      while the first is active
- [ ] With the Representative on Dialog 536, "Create Savings Book" gives one Cuarry Bank Savings Book
      ("You have received: 1x Cuarry Bank Savings Book"); asking again while holding one gives nothing
- [ ] Use the savings book anywhere: the Bank window opens without the deposit and withdraw buttons and
      logs the "Balance" line; the book is kept
- [ ] Close the Bank window with its X: nothing is logged as a decoding error on the server

### NosBazaar search

- [ ] List a weapon, an armour, a hat, a ring, a potion and a snack from one character, then
      open the NosBazaar on another
- [ ] Each category tab (Weapon, Armour, Equipment, Jewellery, Main, Usable) shows only its items
- [ ] Weapon and armour: the class drop-down, the level band (1-10 ... 81-90, Heroic), rarity and
      upgrade each narrow the list; Equipment and Jewellery: the slot drop-down picks the slot
- [ ] Searching an item by name shows only that item; searching with no name never empties the window
- [ ] With more than 50 listings, page 2 continues the price order of page 1 instead of repeating it
- [ ] Listings come in item-name order (in the client's language), then by the chosen price or
      amount order; an expired or sold-out listing does not show; the last page holds only the
      remainder

### NosBazaar buying and collecting

- [ ] List 50 of a 99 stack: the listing shows 50 and the seller keeps 49 in the same slot
- [ ] Buy 3 of a 10 listing from another character: the buyer gets exactly 3, pays 3 x the price,
      and the seller's "My listings" shows 3 sold
- [ ] Buy the whole remaining listing: the buyer gets the item and is charged once; the seller's
      listing turns to sold out instead of disappearing
- [ ] The seller collects a partly sold listing: the gold for the sold part minus 10 % (none with a
      medal) is added and the unsold rest comes back to the inventory; collecting again shows nothing
- [ ] The seller collects a sold-out listing priced above 330 000: the gold received is the price
      minus 10 %
- [ ] Change the price of a listing, then buy it from a window opened before the change: the buyer
      sees "The offer has been updated" and nothing is charged
- [ ] Log out and back in on both characters: the bought item, the returned rest and the gold are kept
- [ ] Buy from the NosBazaar, then stop the world server without logging out: after a restart the
      buyer has the item and was charged, and the seller can collect the sale
- [ ] List an item, then stop the world server right away: after a restart the item is either listed
      and gone from the inventory, or still in the inventory and not listed, never both
- [ ] Two characters on different channels buy from the same listing while the seller collects it:
      the seller is paid once for every item a buyer received, and the rest comes back

### Production

Recipes come from the parser, so re-run it on an existing database first. Malcolm Mix, the
NosVille alchemist, makes potions.

- [ ] Use Shop, then the Combine tab: the product list shows the potions he makes
- [ ] Click Small Health Potion: the material list shows 3 Empty Bottles, 5 Magic Potions,
      10 Herbs and 5 Red Fruits
- [ ] With those materials, press Produce and confirm: the progress bar fills, then
      "[Small Health Potion] has been produced." pops up, 10 Small Health Potions appear, the
      materials are gone, and the production effect plays
- [ ] With one material short, produce again: "Raw materials missing." appears and nothing is
      taken
- [ ] Walk away from him and produce with the window still open: nothing happens

### Production scrolls

Scroll recipes are only imported when a capture shows the scroll in use, so seed them by hand
otherwise (a `Recipe` row with `ScrollVNum` set and its `RecipeItem` rows). The Consumables
Recipe (1241) makes Attack Potions from itself, 5 Empty Bottles, 5 Magic Potions, 5 Orichalcum
and 5 Apples.

- [ ] Double-click the scroll in the Main pocket, away from any NPC: the Combine window opens
      with the scroll's products, and a Produce (Loop) button
- [ ] Click Attack Potion: the material list shows the scroll, Empty Bottles, Magic Potions,
      Orichalcum and Apples
- [ ] Press Produce and confirm: after the bar, "[Attack Potion] has been produced." pops up,
      the potion appears, the materials are gone and one scroll is used up

### Group loot

Two characters, A and B, in a party with A as the master, on the same map and channel; a third
character, C, outside the party. Kill monsters on a normal map so they drop items and gold. The
looting mode is the master's "party sharing" line of the options window.

- [ ] Before inviting anyone, A toggles the party sharing line: A sees "Party looting: Share as
      group" or "Party looting: Loot in turn", and the checkbox reads the same mode as the
      message. If the checkbox and the message disagree, the option's on/off meaning is inverted
- [ ] A sets "Share as group": A and B both see "Party item looting has been changed to 'Share
      as group'."; B toggles the same line: B sees "You cannot change party mode because you are
      not the party master." and the mode does not change
- [ ] Share as group: A deals most of the damage to a monster. B picks up its item and its gold
      straight away; B receives them, B's gold counter rises by the whole stack and A's gold does
      not move
- [ ] Share as group: C presses pick-up on the same kind of drop straight away: "You don't have
      permission to pick that up." and the item stays. After 30 seconds C can take it
- [ ] A sets "Loot in turn" (both see "Party item looting has been changed to 'Loot in turn'.").
      Kill several monsters with both standing near: their drops alternate between A and B, and
      each drop refuses the other member for 30 seconds
- [ ] Loot in turn: B walks far from the fight (over 50 cells) or onto another map; drops from A's
      kills all go to A until B comes back
- [ ] A leaves the party: drops from A's kills belong to A alone again

### Character options

Two characters, A and B, on the same map and channel. Open the options window on A
(the game menu's option tab) and toggle each line; every toggle should print the client's
own message, in the client's language.

- [ ] A turns exchange off ("You will block trades from now on."); B requests a trade with
      A: B sees "A is blocking trades." and A gets no dialog
- [ ] A turns exchange back on; B's trade request opens the usual dialog on A
- [ ] A turns friend off; B adds A as a friend: B sees "A has blocked being chosen as a
      friend." and A gets no dialog. Turn it back on and the request goes through
- [ ] Same with B on a different channel: the friend block still applies
- [ ] A turns whisper off; B whispers A: B sees "This person does not want to receive
      whispers." and A receives nothing. Repeat with B on another channel. Turn it back on
      and the whisper arrives
- [ ] A turns party off; B invites A: B sees "This person's party option is blocked." and A
      gets no invite. Turn it back on and the invite arrives
- [ ] A blocks emoticons ("Some visual effects will now be hidden."); B uses an emoticon: A
      does not see it, a third client on the map does. A cannot send emoticons while blocked
- [ ] A blocks hero chat ("You cannot hear the speaker from now on."); B uses a speaker
      (`$CreateItem 2173`): A does not see the message, a third client does
- [ ] With hero chat still blocked on A, a top-three player sends a hero message (see "Hero
      chat" below): A does not see it, on A's channel or another, and a third client does
- [ ] Toggle every other line (family, pet and partner revive, HP display, buff display,
      quick get-up, mouse aim lock, Miniland invites, and the rest of the window): each
      shows the client's own message where it has one, and the checkbox stays where it was
      put
- [ ] Set a mix of options, log out and back in: every checkbox in the options window is
      in the state it was left in, and the blocks above still apply
- [ ] Change map with options set: nothing resets

### Sending rules

Packets now declare the sender states they are refused in, and the server enforces them in one
place. A refused packet does nothing; only the specialist holder answers with a message.

- [ ] Die to a monster. On the death screen, press a potion, food and snack shortcut: nothing
      is drunk or eaten and the items stay in the bag
- [ ] Still dead: chat, whisper, use the family and group chat, open the friend list and invite a
      friend to a group: all of them work
- [ ] Still dead, as a game master: `$` commands still run
- [ ] Revive with each of the death screen's buttons: the revival goes through as before
- [ ] Alive, send `revival 1` from the packet sender if one is available: nothing happens
- [ ] On a vehicle, press a skill key: no skill is cast, and the skill bar still responds once
      you dismount (the server no longer sends a cancel in that case)
- [ ] Transformed, drop a card on the card holder's window: the "while the transformation is in
      progress" message appears and both items stay where they were
- [ ] During a trade: depositing gold in the bank, opening the bank at its NPC, sorting the bag,
      deleting an item, taking off equipment, wearing equipment and using an item all do nothing

### Trading

Two characters, A and B, on the same map, both with exchange allowed in the options window.

- [ ] A requests a trade with B: A sees "You invited B to trade." and B gets the dialog; B
      accepts and the trade window opens on both
- [ ] A requests again, B declines: A sees that B cancelled the trade and B sees that the trade
      was cancelled; no window opens
- [ ] A requests twice within five seconds: the second request shows the client's wait message
      with the seconds left, and B gets one dialog only
- [ ] A requests, B waits more than 30 seconds before accepting: no window opens
- [ ] B puts A on the blacklist; A requests a trade: A sees "B is blocking trades." and B gets
      no dialog. Remove A from the blacklist and the request goes through
- [ ] A offers 1 000 gold and B confirms; A changes the offer to 500 and confirms: the trade does
      not complete until B confirms again, and then 500 gold moves
- [ ] A offers part of a stack B already holds (5 of 10 Seeds of Power into B's 10): B's stack
      reads 15 and A's 5
- [ ] A offers a weapon with rarity, upgrade and shell options: B's item window shows the same
      rarity, upgrade and options. Both log out and back in: B still has it with its options and
      A no longer has it
- [ ] A trades a specialist card with job level and upgrades: B's card shows the same level,
      upgrades and points
- [ ] Complete a trade of gold and an item, then stop the WorldServer process straight away (before
      the next save). After a restart both characters show the traded gold and items, and neither
      has both or neither copy

### Miniland invite

Two accounts on the same channel. The owner holds a Miniland Bell and a Seed of Power.

- [ ] Outside the miniland, `$Invite <name>` says invitations only work in the miniland
- [ ] Inside an open miniland, `$Invite <name>` uses one Seed of Power and asks the other
      player whether to visit; Confirm puts them in the owner's miniland and the visitor
      counter goes up
- [ ] Cancel leaves them where they are, and a later Confirm on a stale dialog does nothing
- [ ] With the miniland locked, without a seed, to someone offline, or to someone blocking
      Miniland invites in their options: each gives its own message and no seed is used
- [ ] From the friend list, visit a friend's open miniland from the open world and from your
      own miniland: both work
- [ ] Try the same from inside a time-space or an arena: the client's "cannot be used here"
      line appears and you stay where you are

### Invite cooldown and blacklist

Three characters, A, B and C, on the same map and channel. A is a family head (for the
family invite) and the miniland owner (for the miniland invite, with two Seeds of Power).

- [ ] A sends B a friend request twice within five seconds: B gets one dialog, and A sees
      "You cannot send the invitation. Please try again in N seconds." with N no higher
      than 5. After five seconds a new request opens the dialog on B again
- [ ] Same with `%Familyinvite` and `$Invite` from the miniland: the second within five
      seconds is refused with the same message and B gets no second dialog; after five
      seconds the dialog comes back. A second miniland invite while the
      first is still unanswered uses no second seed
- [ ] Right after inviting B, A invites C to the same thing: C's dialog arrives at once
- [ ] A party invite twice within five seconds still gives the same countdown message
- [ ] B blacklists A. A whispers B, invites B to a party, adds B as a friend and invites
      B to the family: each shows "You have already been blacklisted."
      on A and nothing reaches B. Repeat the whisper with A on another channel
- [ ] A blacklists B and does the same towards B: still refused, as before
- [ ] B removes A from the blacklist: the whisper arrives and every invite opens its dialog
      again
- [ ] A and C, with no blacklist between them, whisper each other: the message arrives

### Item detail window

Open the inventory and right-click a piece of equipment.

- [ ] A weapon opens its detail window with its damage, hit rate, critical chance and price, the
      upgrade and rarity bonus included (a +5 rarity 3 Short Sword reads 123-149)
- [ ] An armour shows its melee, ranged and magic defence and its dodge
- [ ] A fairy shows its element and its element rate
- [ ] Gloves or boots show their defences and the four resistances
- [ ] A ring, necklace or bracelet lists the cellon options added to it
- [ ] Right-click a worn item in the character window: the same window opens

### Cellon options on accessories

Talk to an NPC whose shop menu offers the accessory shop and "add option" (the Fashion Boutique,
dialog 10), open the shop and switch to the second tab. Drag a necklace, ring or bracelet into
Accessory and a Cellon (`$CreateItem 1017 20` gives twenty level 1 cellons) into Cellon, then Add Option and Confirm.

- [ ] The window shows the price for the cellon's level (700 gold at level 1) and the option list
- [ ] Confirm: "You have successfully added an option.", the gold drops by the price and one
      cellon leaves the stack
- [ ] Keep adding level 1 cellons to the same accessory: once a roll lands on a type it already
      has, "The Cellon has disappeared because you failed to add options." appears, the gold still
      drops by the price and one cellon still leaves the stack
- [ ] A cellon above the accessory's option level: "The Cellon's level is higher than that of the
      accessory.", nothing is charged and the cellon stack is unchanged
- [ ] An accessory already holding its maximum number of options: "You cannot add any more
      options.", nothing is charged and the cellon stack is unchanged

### Cellon options on a worn accessory

Add an HP and an MP option to a necklace, ring or bracelet, then wear it.

- [ ] Wearing it raises the maximum HP and MP by the options' values; taking it off lowers them again
- [ ] Sitting with an HP or MP recovery option, each tick refills that many more points than without it
- [ ] With a critical damage decrease option, a monster's critical hit takes that percentage off its
      critical bonus
- [ ] With an MP consumption option, every skill costs that percentage less MP
- [ ] The same jewel left in the bag changes nothing

### Shells

Give yourself a level 60 rarity 5 weapon shell (`$CreateItem 565 5 60`), a level 70 weapon such as
a Flamberge (`$CreateItem 299 5`), Rainbow Pearls (`$CreateItem 1429 20`) and a Magic Eraser
(`$CreateItem 1430 1`), and wear the weapon.

- [ ] Right-click the shell: the window shows "RequiredLevel: 60Lv", the rarity and no options yet
- [ ] Double-click a Rainbow Pearl: the "Item appraisal" window opens; drop the shell in and it
      reads "Required: 11" (level / 10 + rarity)
- [ ] Appraisal: "The options have been identified.", 11 pearls leave the stack and the shell's
      window now lists its options
- [ ] Appraise the same shell again: "The options have already been identified." and no pearls go
- [ ] Not enough pearls: nothing is identified and the pearls stay
- [ ] Double-click the shell, then click the worn weapon in the Character window: "Do you want to
      add a shell to the item?"; Confirm shows "A new option has been added." and the shell leaves
      the bag
- [ ] The weapon's window shows "Rarity of the effects", "This is your exclusive weapon." and the
      shell's options
- [ ] An unidentified shell, an armour shell on a weapon, a weapon of lower rarity or a weapon
      below the shell's level is refused with its own message and the shell stays
- [ ] A shell on a weapon that already has options asks whether to replace them; confirming
      either replaces them or breaks the shell ("The shell has been broken.")
- [ ] Double-click the Magic Eraser, then click the worn weapon: "All the options have been
      removed.", the eraser is used and the weapon's window no longer lists options
- [ ] The eraser on a weapon without options: "There are no options on this item." and it stays

---

## Arena

### Individual Arena and Arena of Glory

Two characters with at least 1 000 gold each, in different groups, next to the arena NPC
(Titus Trip, NosVille). Keep one of them low on HP so the fall comes quickly.

- [ ] Talk to the NPC: the menu offers the arenas
- [ ] Individual Arena: 500 gold leaves the gold counter and the player lands on the arena
      floor, never on the central platform where the arena merchants stand
- [ ] Arena of Glory: 1 000 gold and the player lands in that arena
- [ ] With less than the fee: the client's not-enough-gold message in yellow, the gold stays and
      nobody moves
- [ ] Asking twice within four seconds: the second request is refused
- [ ] Both players inside: each can target and hit the other, and the damage is half what the
      same skill does to a monster of similar defence
- [ ] Two players in the same group cannot hit each other
- [ ] A player standing on the central platform can neither hit nor be hit
- [ ] Bring one player to 0 HP: they fall and are asked "When you are revived, your HP and MP
      are fully restored and in PvP mode you are invincible for 5 seconds..."; no dignity or
      reputation is lost
- [ ] Confirm: 100 gold is taken, HP and MP are full and the player stands on the cell they
      fell on
- [ ] Hits in the first five seconds after the revive show "MISS"; after that they land again
- [ ] Cancel instead: the player is sent to their respawn point and the arena keeps no gold
- [ ] Walk onto the arena's exit portal: the player is back on the map and cell they entered from
- [ ] A character whose saved map is an arena (an account saved there before the arena became an
      instance) can be selected and logs in next to the arena NPC in NosVille

### Revival protection

Same setup as above, with both players inside the arena.

- [ ] Confirm the arena revive: every player on the map sees the "Invincible in PvP" buff icon on
      the revived player, and its timer starts at five seconds
- [ ] While the icon is up, the other player's skills and basic attacks show "MISS" and the HP bar
      does not move
- [ ] While the icon is up, the revived player's own attacks show "MISS" too, and attacking does
      not remove the icon
- [ ] Walk around while the icon is up: it stays
- [ ] After five seconds the icon disappears for everyone, and hits in both directions land again
- [ ] Outside an arena, revive in place and at the respawn point: no buff icon appears, and a
      monster's hit lands straight away

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
- [ ] The ranking is re-read every 30 seconds: a change of saved reputation can take that long
      to decide who may use the hero chat, but A's own live reputation counts at once

### Whispers and friend messages

Two characters, A and B, on the same channel, then on different channels.

- [ ] A whispers B several times in a row: every whisper arrives, in order
- [ ] A blacklists B, then whispers B straight away: A is refused with the blacklist message and B
      receives nothing. A removes B from the blacklist and whispers again: it arrives
- [ ] B logs in and A whispers B within a few seconds: if A is told B is not playing, a whisper
      a few seconds later arrives (the online list is re-read every 3 seconds)
- [ ] A has several friends, some online, some not: the friend list shows each with its name
      and the right online state; a friend message to an online friend arrives, to an offline
      friend shows "friend offline"
- [ ] The blacklist window lists every blocked character by name

---

## Family

### Founding a family

Three characters with a class (not Adventurer) and no family, in one party, next to the
family NPC (Mimi Mentor in NosVille, dialog 3). The asker carries 200 000 gold.

- [ ] Families > "I want to start a family." opens an "Enter a family name." box
- [ ] Confirm with a 3 to 14 character name: everyone on the channel reads "Congratulations!
      The '<name>' Family has been created!", 200 000 gold leaves the asker, and the asker
      gets `<name>(Familyhead)` and the other two `<name>(Familydeputy)`
- [ ] With only two in the party the NPC answers "You need three members in your party."
      and no box opens
- [ ] A party member already in a family, an Adventurer in the party, less than 200 000
      gold, a taken name, or a name of 2 or 15+ characters each get their own message and
      found nothing

### Disbanding a family

The head of a family next to the family NPC.

- [ ] Families > "I want to disband my family." asks "Are you sure you want to disband your
      family?"
- [ ] Confirm: the head reads "Family disbanded." and every member's family tag disappears
- [ ] A deputy, keeper or member, or someone with no family, gets "Only the family head can
      disband a family." and no question

### Family warehouse

A family with a warehouse: buy one as in "Family extensions", or set `Family.WarehouseSize`
to 21 (or 49). Members stand next to Salim (vnum 1431, dialog 462).

- [ ] With `WarehouseSize` 0, "Open Family Warehouse" shows "You are not qualified to use the
      warehouse."
- [ ] With a warehouse, it opens "Family's warehouse" with `0/21` and the inventory beside it
- [ ] Dragging a stack in stores it with its amount shown and empties the bag slot; the
      counter goes to `1/21`
- [ ] Dragging it to another warehouse slot moves it and leaves the old slot empty
- [ ] Dragging it back into the bag returns the whole stack and empties the warehouse slot
- [ ] Closing and reopening shows the same contents; another member sees them too
- [ ] A member whose rank has no warehouse rights gets "You are not qualified to use the
      warehouse."; with deposit-only rights they can put items in but not take them out
- [ ] An untradable item, or equipment bound to the character, stays in the bag when
      dragged onto the warehouse
- [ ] With a full bag, dragging a stack out leaves it in the warehouse
- [ ] Two members dragging the same stack out at once: only one of them receives it
- [ ] Disbanding the family empties its warehouse

### Family extensions

The parser fills `FamilyUpgrade` only when `packet.txt` holds the family manager's list, e.g.
`n_inv 2 2089 0 9679|2|0 ... 9697|2|18`. Seed a family as in "Family membership" with
`FamilyLevel` set per check, and stand next to Salim (vnum 1431, dialog 462) as its head.

- [ ] "Purchase Family Extensions" opens a shop window with four rows: Family Warehouse
      (Level 1/2) and Increase Max. Family Members (Level 1/2), whatever the family level
- [ ] Double-clicking a row asks for the amount with its price: 500 000, 2 000 000,
      5 000 000, 10 000 000
- [ ] At level 2, buying the warehouse takes 500 000 gold and shows "Purchased: Family
      Warehouse (Level 1)"; "Open Family Warehouse" then opens at `0/21`
- [ ] A row the family is below the level for: "Your family level is insufficient.", gold
      unchanged
- [ ] At level 7 with that warehouse, the second warehouse row expands it to `0/49`
- [ ] At level 5, the first member row raises the family window's member count to `/70`;
      at level 9 the second raises it to `/100`
- [ ] With less gold than the price: "Insufficient Gold available.", gold unchanged
- [ ] As a deputy: "Only the family head can purchase it.", gold unchanged
- [ ] Clicking a row already bought: "This has already been purchased."
- [ ] A member logged in on another channel sees the new member limit in their family
      window without relogging
- [ ] Double-clicking a row quickly takes the gold once and buys it once

### Family membership

Found a family as above, or seed one: a `Family` row, then a `FamilyCharacter` row per
member (`Authority` 0 head, 1 assistant, 2 manager, 3 member). Log two members in on the
same map.

- [ ] Each member sees the other's tag under their name as `Name(Rank)`, with the rank
      in the viewer's own language, and nothing printed after it
- [ ] Your own tag shows under your own name after character select, with nothing after it
- [ ] A character with no family has no tag
- [ ] Opening the family window shows the name, the head, the level bar and the notice,
      with the notice's spaces intact

### Family chat and shout

Same seeded family, with a third character outside it.

- [ ] `:hello` from any member shows in every member's Family tab as `[Name]:hello`,
      and nowhere for the outsider
- [ ] A member on another map sees the line with the sender's name in it
- [ ] `%Familyshout hello` from the head or an assistant puts `<Family shout> hello`
      in the middle of every member's screen, in the reader's language
- [ ] The same shout from a manager works only while the family lets managers shout,
      and does nothing from a plain member

### Family invite, leave and dismiss

Same seeded family, plus a second account whose character has no family, both on the
same map.

- [ ] `%Familyinvite <name>` from the head asks the other player whether to join the
      family; Confirm puts `Testers(Member)` under their name on both screens and every
      member reads `<name> has joined your family.`
- [ ] Cancel tells the head the player does not want to join
- [ ] `%Familyinvite <name>` from a character with no family does nothing at all
- [ ] A plain member's invite, a manager's while the family forbids it, an invite to
      someone in another family or blocking family requests, and an invite into a full
      family each give their own message and send nothing
- [ ] `%Familyleave` from the member says they left and the tag disappears on both
      screens; from the head it says the head cannot leave
- [ ] `%Familydismiss <name>` from the head or an assistant removes a lower rank and
      the tag disappears; nobody can dismiss the head or their own rank

### Family ranks and permissions

Same seeded family, with a plain member on the same map as the head.

- [ ] `%Familydeputy <name>` from the head says a new deputy was appointed and the
      member's tag becomes `Testers(Familydeputy)`; a third deputy is refused
- [ ] `%Familykeeper <name>` from the head or a deputy appoints a keeper; a deputy cannot
      change the head or another deputy
- [ ] `%Familyhead <name>` from the head to a deputy says the head changed and swaps both
      tags; to a plain member it says only a deputy can become head
- [ ] The family window's member list shows every member with level, class, rank and who
      is online, and changing a rank there does the same as the commands
- [ ] Ticking a keeper or member permission in the family window as the head keeps it
      after reopening the window and relogging, and the keeper's invite and shout follow it

### Family notice, daily message, titles and head gender

Same seeded family, with a plain member on the same map as the head.

- [ ] `%Notice <text>` from the head or an assistant shows the text as the notice in the
      family window of every member; a keeper's works only while the family lets keepers
      post notices, and a plain member's says they are not authorised
- [ ] `%Today <text>` from any member shows as that member's line in the family window's
      member list
- [ ] `%Title <name> <rank>` from the head changes that member's title in the member list;
      a name outside the family says it cannot be found, and nobody else can give titles
- [ ] `%Gender <0|1>` from the head switches the family's titles to the matching set and
      clears every member's title

### Family history, ranking and daily message windows

Same seeded family, with a plain member on the same map as the head, and at least two other
families in the database with different levels.

- [ ] Opening the history tab of the family window as the head lists the family's events
      newest first (joins, leaves, appointments, permission changes, daily messages, member
      level milestones), each with how many hours ago it happened
- [ ] With the history window open, `%Familyinvite` a new member and accept: the history
      refreshes on its own and the join is at the top, also on the other member's open window
- [ ] As a plain member, the history tab shows nothing while "members can read the history"
      is unticked, and the entries once the head ticks it; a keeper follows the keeper box
- [ ] The family ranking's experience tab lists families by level, then experience, with
      rank, name, level and experience; families with the same level and experience share a
      rank
- [ ] The reputation tab lists families by the summed reputation of their members
- [ ] Both monthly tabs are empty
- [ ] The daily message button in the family window opens the input box for a member; a
      character without a family gets nothing

### Family history log

Same seeded family, with a plain member on the same map as the head. Each row also appears
in the family window's history tab (see "Family history, ranking and daily message
windows"); the table shows the stored arguments:
`select "FamilyLogType", "FirstArgument", "SecondArgument", "ThirdArgument", "FourthArgument", "Timestamp" from "FamilyLog" order by "Timestamp";`

- [ ] `%Today <text>` from a member writes a row of type 1 with arguments `<name>`, `<text>`; a second
      `%Today` from the same member within a day says `Only possible once per day` and
      leaves both the member list line and the table unchanged
- [ ] Accepting a `%Familyinvite` writes type 11 with `<inviter>`, `<newcomer>`; cancelling
      writes nothing
- [ ] `%Familyleave` from the member and `%Familydismiss <name>` from the head each write
      type 10 with the name of whoever left; the head's refused leave writes nothing
- [ ] `%Familykeeper <name>` from the head writes type 9 with `<head>`, `2`, `<name>`; a refused
      appointment (a third deputy) writes nothing
- [ ] Ticking "keepers can read the history" in the family window as the head writes
      type 8 with `<head>`, `2`, `4`, `1`
- [ ] Levelling a family member from 29 to 30 writes type 6 with `<name>`, `30`; 30 to 31
      writes nothing, and every level past 80 writes one row

### Family experience

Same seeded family, with a member 100 XP short of level 30 and the family at
level 1 with 79 800 / 80 000 experience.

- [ ] Reaching level 30 writes type 4 with `<name>`, `600` and the history reads "<name> has
      earned 600 action points!"; the family window's level bar moves on its own
- [ ] When that fills the bar, every online member sees "Family level increased to 2!", the
      history gains "Reached family level 2!" (type 5 with `2`) and the window shows level 2
      with the remainder
- [ ] Reaching 31, or 20, gives the family nothing; reaching 40 gives 800
- [ ] Members on another channel see the new level too
- [ ] Not verifiable yet: experience from time-spaces, raids and Instant Combat, which do not
      exist yet

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

## Quests

### Talk, pay, inspect and hand-in objectives

Quests of these types used to be accepted and then never advance. A new character with the
opening scene follows the main story script, which hands most of them out.

- [ ] **Talk to an NPC** (Dialog1) — Rookie Training quest 1503, "Go to Calvin Coach to
      improve your skills!": talk to Calvin Coach. "Quest complete" shows, the quest window
      marks it done and the story moves on to its next step
- [ ] Talking to any other NPC while 1503 is open changes nothing in the quest window
- [ ] **Pay an NPC** (TransmitGold) — Whirl of War quest 1659, "Accept Teodor Topp's offer!",
      asks for 10 000 gold. With less than that, talk to Teodor Topp: "not enough gold" shows
      in chat, the gold is untouched and the quest stays open
- [ ] With at least 10 000 gold, talk to him again: exactly 10 000 gold leaves the purse, the
      gold display updates and the quest completes
- [ ] Talk to him once more: no further gold is taken
- [ ] The other paid quests take the amount their text names, for example Glacerus the Ice
      Cold quest 5986, "Pay Serena 100000 Gold"

### Capture objectives

Capture is skill 209; weaken the monster to under half health and stay at or above its level.

- [ ] **Capture** — Rookie Training quest 1501, "Capture a Chicken using your skill!", comes
      right after hunting Baby Danders. Capture a Chicken: "capture successful" shows, the
      Chicken becomes a pet as usual, "Quest complete" shows and the quest window marks it done
- [ ] Before capturing the Chicken, capture some other monster: it becomes a pet and the quest
      window still reads 0/1
- [ ] The next quest, 1502, asks you to hunt with your NosMate, so the Chicken must still be
      in the pet window
- [ ] **Capture without getting the monster** — Suspicious Guard quest 1608, "Capture the
      animals Teodor Topp requires!", needs two each of Angry Hamster, Angry Wooly and Fox
      (the Fox is level 15, the other two level 21 and 22). Capture a Fox: "Capture
      successful! (Quest)" shows, the monster disappears, no pet is added and the quest
      window reads 1/2 on that line
- [ ] Capture a third Fox once that line reads 2/2: this one becomes a pet
- [ ] Capturing the last animal the quest needs completes it

### Collectable map objects

Objects flagged collectable in the NPC data (ETC bit 1), such as the Ice Flowers on Mount
Krem. Stand next to one before each check.

- [ ] Click the Ice Flower: a five second bar runs over the character
- [ ] Let the bar finish: the flower's item lands in the inventory, "received" shows in chat
      and the flower disappears for everyone on the map
- [ ] Walk away or get hit during the bar: it stops and no item is given
- [ ] Click where the flower stood before it is back: nothing happens
- [ ] Wait out the respawn time from the NPC data: the flower reappears in the same spot and
      can be collected again
- [ ] Change map and come back while the flower is gone: it is still absent until it respawns
- [ ] With a full main inventory, finish the bar: "not enough space" shows and the flower stays
- [ ] **Collect a map object** (CollectMapEntity) — with a quest of that type open that names
      the flower, each collect advances the quest window and the last one completes the quest;
      opening the flower without finishing the bar does not count

---

## Login and sessions

Run the WebApi, the master, the login server and two world servers (two channels).

### Launcher login and channel change

- [ ] Log in through the launcher, pick channel 1 from the list: the character list loads
- [ ] Wait more than a minute on the channel list before picking a channel: the world
      server disconnects the client instead of showing the character list
- [ ] Log out to the login screen and log in again through the launcher: it works, with a
      fresh code from the launcher
- [ ] In game, `$ChangeChannel` to channel 2: the character arrives on channel 2 without
      a second login prompt
- [ ] On an account with MFA set, log in through the launcher: the launcher asks for the
      code, the game does not ask again, and changing channel does not ask either

### One session per account

- [ ] Log in on channel 1, then log the same account in from a second client and pick
      channel 2: the second client is disconnected and the first keeps playing
- [ ] Close the first client, wait for its character to leave, then log in again: it works
- [ ] Pick up an item and close the client right away (kill the process). Log back in
      immediately: the item is in the inventory

### MFA, authority and maintenance

- [ ] On an account with MFA, log in without the launcher: the game asks for the code before
      the character list, and nothing on the character screen (select, create, delete)
      works until a correct code is entered
- [ ] Enter three wrong codes: the client is disconnected after the third
- [ ] Set an account's authority to Banned in the database and try to enter the world with
      it: the world server disconnects it
- [ ] On a User account, `$SetMaintenance` and the other GM commands do nothing; on a GM
      account they work
- [ ] `$SetMaintenance 0 1` on a GM account (this channel only), then log a User account in
      and pick that channel: it is disconnected; a GM account still gets in

---

## Regression sweep

Quick pass after any combat or stat change:

- [ ] Log in, walk, attack a monster, take damage, die, respawn
- [ ] HP and MP bars track the real values through all of it
- [ ] Pick up an item, equip it, unequip it
- [ ] Change map, then change back
- [ ] Log out and back in — nothing that was learned, worn or spent has reverted

---

## Quests

### Item objectives

DATA columns read per type: Collect is monster, item, amount; Brings is item, NPC, amount;
Required is NPC, item, amount; Wear reads its item from the second column; Use, Needed and
Product read the item from the first column and Needed and Product the amount from the
second. Use, Wear, Collect, Brings and Required are checked against parsed rows; Needed and
Product are not yet, so check one row of each before testing them and record a mismatch as a
finding.

Quests to try: Use 5978 (Summoning Horn 2420), Wear 1546 (Kovolt Mask 206) and 5928 (Pumpkin
Hat 4169), Collect 1523, Brings 309 and 1598, Required 1518. For Needed and Product:

```sql
SELECT DISTINCT ON (q."QuestType") q."QuestId", q."QuestType", q."LevelMin",
       o."FirstData", o."SecondData", o."ThirdData", o."FourthData"
FROM "Quest" q JOIN "QuestObjective" o ON o."QuestId" = q."QuestId"
WHERE q."QuestType" IN (8, 16)
ORDER BY q."QuestType", q."LevelMin", q."QuestId";
```

Give yourself the quest through its script, or by walking the quest line that leads to it.

- [ ] **Collect (17)**: drop part of the item with `$CreateItem`, then pick it up: the quest
      window counter moves to what you carry, out of the amount in the third column. Pick up
      the rest: "Quest complete" appears and the items stay in the bag. Picking up another item
      leaves the counter alone
- [ ] **Needed (16)**: picking up fewer than asked leaves the quest open; picking up the last
      one completes it and nothing is taken
- [ ] **Brings (4)**: with the items in the bag the counter stays at 0 and the quest stays
      open. Talk to the NPC from the data: the items leave the bag, the counter fills and the
      quest completes. With one item short, talking to the NPC takes nothing. Talking to
      another NPC takes nothing
- [ ] **Required (14)**: same checks as Brings
- [ ] **Product (8)**: produce the item at an NPC or with a scroll: the counter rises by the
      amount made and the quest completes once it is reached. Items of that kind picked up or
      bought do not count
- [ ] **Use (21)**: using the listed item completes the quest; using any other item does not
- [ ] **Wear (15)**: equipping the Kovolt Mask on 1546 completes it; equipping another item
      does not
- [ ] For a quest with a `q_pay` step, the reward is only given once the objectives above are
      met, and for Brings and Required only after the hand-in
- [ ] Relog halfway through a Collect or Product quest: the counter comes back where it was

## Not verifiable yet

Recorded so nobody spends time hunting for them:

- **Sending rules while a private shop is open** — no code sets the shop flag yet, so the
  refusals that apply in a shop never fire; a character with an open private shop is treated
  like any other until it is wired.
- **Bank gold in a trade** — the trade window's bank field accepts no input, so the client never
  offers bank gold and the bank-cap check of a trade ("You cannot deposit any more.") cannot be
  reached. Once the field opens, offer bank gold that would push the other player's custody past
  the cap, from both sides of the trade: that player sees the message in their own language and no
  gold moves. Then check that the field counts in thousands like the bank window (offering 5 moves
  5 000 gold), that an offer above the custody is refused, and that spending the custody before
  both confirm makes the trade fail with nothing moved.
- **Both sides confirming a trade at the same moment** — two people cannot click within the same
  instant reliably; an automated test sends both confirmations concurrently and checks the trade
  settles once.
- **Casting range of skills with a range of 0** — the skill data does not say how far they
  reach, so they are not gated and there is nothing to observe.
- **NPC services sent without the NPC** — an unmodified client only sends a dialog choice, a
  purchase or a bank, NosBazaar or upgrade request from a window it was shown, so the refusals for a
  dialog that was never opened, a player named as the NPC or a request from another map cannot be
  produced from the client. The client also keeps the character still while a shop window is open,
  so buying or selling from a shop left behind cannot be produced either; the unit tests cover it.
- **BCard subtype names and additions** — data-only. Observable only once a skill or card
  using a given subtype is wired to behaviour.
- **The `st` percentage guard** — guards a zero maximum, which is not a state the game
  reaches. It cannot be triggered from the client by design.
- **A family name with a space** — the client's name prompt refuses a space itself ("Can not use
  Space in name") and sends nothing, so the server's refusal ("Contains an invalid character.
  Choose another name.") cannot be produced from an unmodified client; the unit tests cover it.
- **Monthly family rankings** — nothing records experience or reputation per month, so the
  two monthly tabs of the family ranking stay empty on purpose.
- **Reading the family history without permission** — the server sends nothing, but an
  unmodified client may grey the tab out first, in which case the refusal cannot be reached.
- **Family experience** — nothing grants it yet, so the level bar only ever shows the
  seeded value.
- **Inside a time-space** — the entrance works, but rooms, objectives, lives and rewards
  do not run yet, so nothing past the first room can be checked.
- **Skipped delay bars** — the server ignores a bar's follow-up that arrives early or without
  the bar. The client always waits for its own bar, so only a modified client could send one.
- **Options with nothing behind them yet** — family invites, pet and partner auto-revive,
  group sharing, hat visibility and interface lock are stored and come back on login, but
  no family invite, revive or sharing flow reads them yet.
- **HP display, buff display, quick get-up and mouse aim lock** — the client applies these
  itself; the server only stores them and sends them back on login, so the relog check is
  all there is to see.
- **Family commands on a non-English client** — each client language types its own word
  for the family commands (`%Invitationdefamille`, `%Familieneinladung`, `%Caccia` and so
  on). The cloud client is English only, so only the unit tests cover the other words.
- **Food and snacks in a miniland** — minilands are created after start-up and never get a
  life loop, so nothing that ticks per map (regeneration, digestion) runs there yet.
- **Collect-from-a-map-object quests (type 20)** — the ice flowers and water spots they use
  are not on the maps yet, so those quests are still never completed.
- **What shell options do** — the options are rolled, shown and kept on the equipment, but no
  combat, experience or gold formula reads them yet, so there is nothing to observe beyond the
  item window.
- **What a filled card holder contains** — the item window has no description of a holder yet,
  so its card is only visible once taken out.
- **Inspect and Dialog2 quests in the client** — both are wired (inspecting the object, and the
  "report" button that sends `n_run 200 <quest>`), but every quest of either type is handed
  out by a chain that starts at an NPC's `n_run 3006 <quest>` button, which does nothing yet.
  Once it does, check Lost Trade Goods quest 5044 ("Look for the lost trade goods", inspect the
  Abandoned Amakur Merchant Chest in the Port Surroundings), then quest 5045 ("Bring them to
  Harbour Guard Tom", press his report button) — the report button at any other NPC must do
  nothing, and opening Tom without pressing it must not complete 5045. Quest 5064 names four
  chests, and must stay open until all four are inspected.
- **Partner specialist agility and skills** — the card's agility is always sent as 0 and the
  partner keeps its own skills while transformed, since neither is stored or wired yet.
- **Inspect quests and the item they name** — inspecting completes the objective at once.
  The trade goods themselves come from the chest's own drop, which needs the map-object
  collection flow (`guri 400`) that is not wired yet.
- **Reputation target quests** (TargetReput) — no quest in the current client data uses this
  type, so there is nothing to implement or to check.
- **The ground effect of an area skill on other screens** — the cell animation needs the `bs`
  packet, which the packet library does not ship yet; until it does, only the per-monster
  hits are drawn.

## Mount activation

- [ ] Double-click a vehicle in the Mount inventory: the three-second bar appears,
      then the character mounts and moves at the vehicle's speed.
- [ ] Use the vehicle again while mounted: dismount, with normal walking speed restored.
- [ ] Repeat mounting and dismounting from Main inventory or an existing shortcut.
- [ ] Relog and repeat: the vehicle remains usable and is not consumed.

Early or missing delay completion is covered by automated tests; a stock client
waits for its bar before sending the completion.

## Individual faction eggs

- [ ] Outside a family, use Angel's Egg (Individual), item 1624: a confirmation asks
      about changing protection and resetting points. Cancel: faction, points and egg stay.
- [ ] Confirm: protection changes to Angel and exactly one egg disappears. The faction
      display updates; another character on the map sees the new faction.
- [ ] Use another Angel egg: the already-Angel message appears and the egg stays.
- [ ] Repeat with Demon's Egg (Individual), item 1625, from neutral and Angel protection;
      also switch from Demon to Angel. Contribution points reset only on a successful change.
- [ ] Relog after each successful change: the faction and remaining egg count persist.
- [ ] Join a family and try either individual egg: the client explains that the family egg
      is required, with no consumption or faction change.
- [ ] With the matching protection, try the faction ship's Corona crossing; the faction
      requirement accepts the character. Check the opposite faction's route remains refused.

Family eggs (1626/1627) are not implemented by this feature.
