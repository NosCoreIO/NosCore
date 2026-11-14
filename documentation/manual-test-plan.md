# Manual test plan

Most of what changes in this project is only observable in a running client. Automated
tests cover the arithmetic; this covers the part where a bar moves, a monster walks, or a
skill does what its description says.

Work through the sections that touch what you changed. Tick as you go.

## What runs on every pull request

The end-to-end scenarios in `test/NosCore.EndToEnd.Tests` start the real master, login and
world servers and play the game's protocol against them. They check what the servers send
and store, on every pull request. They do not open a client, so they say nothing about what
is drawn: a check below that is about a bar, an animation or a window still needs a client
even when its packets are covered here.

| Covered without a client | Scenarios |
|---|---|
| Login: handover to the world, a wrong password, the character list, the same account twice | `LoginToWorldTests` |
| The servers refusing to start on a database that is behind its migrations | `StartupTests` |
| Character screen: creating, a name that is taken, deleting, a creation that cannot be stored | `CharacterScreenTests` |
| The map: seeing someone arrive, walk and talk, portals and their delay, the position saved on leaving | `MapTests` |
| Items: what a character carries on entering, dropping and picking up, an NPC shop selling and buying back | `ItemTests` |
| Trade: gold changing hands and stored at once, a declined trade | `TradeTests` |
| Chat: whispers, a whisper to nobody, a muted account | `ChatTests` |
| Friends and groups: a request accepted, friend and group messages | `SocialTests` |
| Two channels: the channel list, players apart, a whisper across, one account on one channel | `ChannelTests` |
| Combat: a hit and its `su`, a kill and its experience, a dead monster on arrival | `CombatTests` |
| Families: founding and its conditions, chat on one channel and across two, inviting, leaving, disbanding, dismissing, the notice, the shout, deputies | `FamilyTests`, `FamilyManagementTests` |
| Family experience: a member reaching level 30, the history lines, the level-up at the bar, what members on both channels are sent, a purchase the new level allows | `FamilyExperienceTests` |
| NosBazaar: listing and its tax, finding, buying, taking back, packages, a changed price, its limits | `BazaarTests` |
| Parcels: a gift taken from its parcel, taken once, thrown away | `ParcelTests` |
| Warehouses: depositing and withdrawing, the pet basket, the family warehouse and its permissions | `WarehouseTests` |
| The bag: moving, splitting and merging stacks, sorting, wearing and taking off, costumes, potions, dropping, destroying | `InventoryTests` |
| The bank: depositing, withdrawing and its fee, every limit, a window that is not open | `BankTests` |
| Groups: inviting, declining, leaving, disbanding, a full group, a member leaving the world, shared experience | `GroupTests` |
| Fights: a monster hitting back, death and what the dead cannot do, both revivals, range, cooldown and MP | `FightTests` |
| Loot: whose a drop is, loot in turn and shared loot, how far a drop can be picked up from | `LootTests` |
| Minilands: entering, friends and strangers, locking, placing and taking away objects, what visitors see, invitations | `MinilandTests` |
| Private shops: opening one and its refusals, browsing, buying, selling out, closing, what both sides keep | `PrivateShopTests` |
| NPC shops: buying and its refusals, a full bag, selling, what cannot be sold, reputation prices | `NpcShopTests` |
| Friends and the blacklist: requests, deleting, blacklisting, what a blacklisted player can no longer send | `FriendTests` |

Everything else in this file has no scenario yet. When a change adds a check here whose
outcome is a packet or a stored row, add a scenario for it as well (`test/README.md`).

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

### Connections

- [ ] Log in, pick a character and play for a few minutes: nothing disconnects
- [ ] Leave the client on the character list for ten minutes, then pick a character: the
      session is still open
- [ ] Stand still in the world for six minutes: the keep-alive holds the session open
- [ ] Open a busy map (many monsters and players) right after login: everything appears
      and the session stays up
- [ ] Close the client while in the world, log back in at once: the character is not
      reported as already connected and loads normally

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
- [ ] With items imported by the current parser, untransformed and with no maximum HP effect
      active, wear the Hellhound Necklace (331), "Maximum HP is increased by 100": the maximum
      HP in the character window goes up by exactly 100

Both the item model and the instance count. A +0 and a +10 of the same vnum giving the
same damage means only one of the two is being read.

### A player's hit on a monster

- [ ] A level 30 swordsman wielding a +0 Short Sword (1), untransformed and with no buff, hits a
      level 1 Baby Dander with the basic attack: every non-critical hit is between 108 and 116
- [ ] A naked adventurer's hit is its stat window's attack plus 15, less the monster's defence,
      plus the level gap: no further flat bonus on top
- [ ] With skills imported by the current parser, a mage's basic attack (260, "magic attack +60")
      hits 60 harder than the same mage's attack with a skill that carries no attack bonus
- [ ] Upgrade the weapon from +0 to +5: the damage goes up by a share of the weapon's own attack,
      not of the whole attack
- [ ] A critical hit adds its bonus on top of a normal hit; the level gap is not multiplied
- [ ] A level 30 swordsman with a +0 Crossbow (68) fires Crossbow Shot at a level 30 Baby Dander
      from range: every non-critical hit is between 132 and 137
- [ ] Every hit, a monster's on a player included, draws the skill's impact on the target, not
      only the damage number
- [ ] Hit a monster with a class skill, then again once its upgrade is learned: the impact
      changes with the upgrade

### A monster's hit on a player

- [ ] A level 80 swordsman with no gear stands next to a level 60 monster (215): each of its
      non-critical hits is about 180 more than the same monster does at level 44 or below,
      roughly 460 to 560 instead of 265 to 390
- [ ] A monster below level 45 adds nothing on top of its usual hit
- [ ] A level 60 monster hitting another monster or an NPC adds nothing either

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

### Debuffs that grow with the level

A value that scales with a level is "so much per level". The data needs a re-import (run the
parser) after a change to how these are read.

- [ ] Under Resurrection Side Effects (card 44, 2 per level) at level 30, the character window
      (`p`) shows attack and all three defences 60 lower than without it. A naked adventurer goes
      from 59~67 attack and 42 / 44 / 42 defence to 0~7 and 0 / 0 / 0: the window never shows a
      value below zero, while hits taken still grow by what the defence lost below it

### The equipped fairy's element — BCard type 96 subtypes 0 and 1

- [ ] With a fire fairy worn, note a fire skill's hits on a monster, then gain an effect that
      raises "the equipped fairy's element" (an Elemental Blessing buff, or gloves such as
      Steam Gloves): the same skill hits harder
- [ ] Take the fairy off with the effect still running: the hits drop back to no elemental
      bonus at all, the effect does not stand in for a fairy
- [ ] An effect that lowers the fairy's element makes the same skill hit softer

### Fairy Booster look and the info card

- [ ] Wear a fairy and use a Fairy Booster (1296): the fairy floating behind the character
      switches to its boosted look at once, for you and for anyone on the map
- [ ] When the booster ends or is removed, the fairy goes back to its normal look
- [ ] Right-click a player whose fairy has grown past its base element: the info card shows the
      base element plus what the fairy has grown, the same figure as the fairy's tooltip

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

### Cast time

A skill's cast time (Skill.dat, tenths of a second) is waited out on the server, after a
`ct` that shows the wind-up.

- [ ] Cast Slingshot (203, 1 s) at a monster: a cast bar and charging effect appear over the
      character, and the damage lands about a second later, not at once
- [ ] A second character next to you sees the same wind-up
- [ ] Kill the target with another character during the wind-up: the cast ends without a hit
- [ ] A monster with a cast-time skill stops chasing while it winds up, then resumes

### Buffs on a target

- [ ] Have a second character drink an Attack Potion, then click them: the potion's icon shows
      under their HP and MP bars. A permanent buff (a fairy booster) does not.
- [ ] Group with that character first: within a few seconds the same icon shows next to their
      bar in the group window. Their portrait there shows their specialist only while they are
      transformed.

### Debuff resistance

- [ ] Let a monster whose skill causes Blackout hit you: Blackout lands on every hit that
      connects
- [ ] Wear the Monkey Hat ("high chance of protection from Blackouts") and repeat: Blackout
      lands only now and then (about one hit in ten)
- [ ] Wear the Antique Mask ("60% resistance to the effect: Flame Poison and lower") and let
      Flame Poison be put on you repeatedly: it lands on about two attempts in five instead of
      every one, and a weaker card of the same family is resisted the same way

### Costume attack and defence bonuses

- [ ] Wear Flower Hairpin ("All attacks +5%"): the attack range in the character window rises by
      5 %, rounded down (109-117 becomes 114-122)
- [ ] Wear Denim Clothing ("All defence stats +5%"): every defence rises by 5 %

### Debuffs that weaken your hits

- [ ] Under Lethargy (S) ("100% chance to reduce damage by 30%"), your auto-attacks on a monster
      deal about 30% less than without it; when it wears off they return to normal

### HP gained or lost from your own hits

- [ ] Under Spooked (S) ("HP is decreased by 40% of the damage dealt"), each auto-attack on a
      monster costs you 40% of its damage, never more than half your maximum HP in one hit
- [ ] Under Giantism with HP missing, each landed hit heals you by its share of the damage, up to
      your maximum HP

### Chance of receiving critical hits

- [ ] Wear an armour with "probability to receive critical hits is decreased by 20%" (8643) and let a
      monster with a 20% critical chance hit you: no hit lands as a critical; take it off and about
      one landed hit in five is critical again

### Chance-based damage taken

- [ ] Wear an armour with "there is a 100% chance that damage from ranged attacks is reduced by 20%"
      (4012) and let a ranged monster hit you: each landed hit does a fifth less than without it
- [ ] A melee-only reduction (4746) leaves a ranged monster's hits unchanged

### Gear speed bonuses

- [ ] Log in wearing a costume with "movement speed is increased by 2" (4245): the speed in `cond` is two
      above the class speed, and walking is visibly faster
- [ ] Take the costume off and put it back on: the speed drops and rises again without a relog

### Gear attack bonuses

- [ ] Log in wearing the hat costume 8900 ("All attacks +10%"): the attack shown in the character
      window and in `sc` is a tenth above the value without it, for both the main and the
      secondary weapon
- [ ] Gear or a buff that raises the attack or defence level counts like that many extra upgrade
      levels on the weapon or armour

### Gear experience bonuses

- [ ] Wear the hat costume 4579 ("XP +10%") and kill a monster: the XP in `lev` rises by a tenth
      more than without it (a 10 000 XP kill gives 11 000)
- [ ] Gear with a hero XP bonus raises the hero XP of a kill by its percent
- [ ] Gear with a reputation bonus raises the reputation a kill gives by its percent

### Skill cooldown and mana cost bonuses

- [ ] Wear amulet 4360 ("cooldowns -20%") and cast skill 203: its slot is ready again after about
      8 s instead of 10 s
- [ ] Wear amulet 4291 ("mana cost -30%"): a skill that costs 100 MP takes 70 MP off the bar

### Maximum HP and MP buffs

- [ ] Drink buff potion 1248 (max HP and MP +10%): the HP and MP bars' maximums rise by a tenth at
      once, and fall back when the buff ends

### PvP attack and defence bonuses

- [ ] Hit another player while wearing amulet 4359 ("attack in PvP +10%"): the damage shown is a
      tenth above the same hit without it
- [ ] Gear that raises defence in PvP lowers the damage another player deals by its percent, and
      never by more than three quarters
- [ ] The amulet changes nothing when hitting a monster

### Buffs triggered by hits

- [ ] Wear the hat costume 8900 and auto-attack a monster for a minute: now and then the hat's
      own buff (card 4426) appears on your buff bar, and its debuff (card 4424) lands on the
      monster
- [ ] Gear that triggers on defence puts its card on you, or on the attacker, when you are hit

### Double-drop and no-drop chances

- [ ] With card 4324 ("no loot" 100%) on you, kill a Baby Dander: neither its item nor gold drops
- [ ] Wear the costume wings 8879 (double drop 10%) and hunt alone: about one item drop in ten
      lands twice; in a group sharing loot by turn, a drop given to someone else never doubles

### Recovery item bonuses

- [ ] With no other recovery bonus active, wear amulet 4361 ("recovery items +40%") and, with at
      least 1400 HP and 1400 MP missing,
      drink potion 1010 (1000 HP and MP): the bars rise by 1400 each instead of 1000
- [ ] With the amulet on, a snack heals two fifths more HP and MP per tick
- [ ] With the amulet on, a meal heals two fifths more HP and MP per tick, never above the usual
      per-tick ceiling for meals

### Stun, root and speed buffs

- [ ] While Blackout is on you, the no-attack and no-move icons show over your character,
      clicking the ground does not move you and skills do not cast
- [ ] When Blackout ends you walk and cast again at your normal speed
- [ ] A Blackout landed on a monster keeps it in place and stops its attacks until it ends
- [ ] Haste (card 28/29) makes you walk faster; a slowing card makes you walk slower

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
- [ ] Watch from a second character: the caster plays the cast, then the skill's effect is
      drawn on the clicked cell before the hits, even when the area is empty
- [ ] The skill icon comes back when the cooldown ends
- [ ] As the Archer specialist (SP 4495), cast its teleport skill (1123) at a cell a few steps
      away: the character appears on that cell and the skill goes on cooldown
- [ ] Cast it at a wall or a tree: the skill plays but the character stays where it was

### Skills cast on yourself or an ally

A skill whose target is the caster (Battle Cry, Shout of Morale, Spinning Hit) or a friendly
player plays, costs its mana and goes on cooldown. Its hit type decides the rest.

- [ ] As an Adventurer, use Shout of Morale (207): mana drops by its cost and its buff icon
      appears beside the character's portrait
- [ ] Use Spinning Hit (204) with two monsters next to you and one a few cells away: both
      nearby monsters show damage, the far one does not, and the skill icon shows its cooldown
- [ ] In a party, use a skill that buffs everyone around: a member standing close gets the buff,
      a member far away does not
- [ ] Use a skill meant for an ally on a monster: the cast is cancelled and nothing is spent

### Combo attacks

A skill with combo rows (most basic attacks) changes its animation as the same skill keeps
being used in a row.

- [ ] Transformed into the Warrior specialist, let the basic attack run on a monster: the second
      and third blows play different swings from the first, then the cycle starts over
- [ ] Use another skill in the middle of the streak: the next basic attack starts again from
      the first swing

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

### Gold from kills

- [ ] A monster at your level drops 6 to 12 gold per monster level on every kill
- [ ] A monster 30 levels below you never drops gold, and one 20 levels below drops it about
      one kill in twenty
- [ ] Wearing amulet 4361 ("Increases Gold earned by 40%") and costume hat 4259 (+20%), a
      level 30 monster's gold goes above the 359 it can drop without them, up to 574

### Aggro and chasing

- [ ] Walk toward a hostile monster: it notices you inside its notice range and comes for you
- [ ] It stops **beside** you rather than on your cell, then attacks
- [ ] Attacks come at an even pace, not in bursts
- [ ] Run off: it follows, then gives up; a monster that gave up walks home at the pace it
      chased at and wanders around its spawn point from there
- [ ] A monster that is not hostile ignores you until you hit it, then fights back
- [ ] Hit one miner among several (Chicken Farm Path or Mine 1): the other miners within their
      notice range join the fight. Wolves do the same for their pack, but a fox next to them
      does not
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

### Monster stats

The parser derives each monster's HP, MP, defences, dodge, damage, hit rate and critical
values from its race, level and equipment levels, then adds the flat modifiers of its entry.

- [ ] Re-import the monsters, then stand next to a hostile ranged monster (vnum 96 on Chicken
      Farm Path): it hits for a visible amount rather than 0 or a miss every time
- [ ] Target a few monsters and compare the HP shown in the target bar with the published
      values for that monster
- [ ] Kill a monster whose entry has a negative experience column: the kill gives
      `max(0, base + column)`, where the base is 60 per level (70 from level 19). A column
      of -300 on a level 10 monster gives 300; a column of -700 gives 0

---

### Bleeding, poison and regeneration — BCard type 22 with an interval

A card of this family takes or gives a fixed amount of HP or MP every few seconds. When the
amount is "per level", the level is that of whoever inflicted the card, not the bearer's.

- [ ] Let a Hell's Guard Dog (level 38, map 10) hit you until the Heavy Bleeding icon appears:
      a red 152 (4 per level of the dog) rises from your character every 2 seconds for 20
      seconds, and the HP bar drops with it
- [ ] The bleeding never kills: at 1 HP the numbers stop and the character stays alive
- [ ] A second player on the map sees the same numbers over your character
- [ ] Bleeding inflicted on a monster shows the numbers over the monster, for a bystander who
      has not selected it too, and leaves it at 1 HP
- [ ] A card that gives HP on a timer (Recovery Aura, card 34) shows a green number at each
      step and stops at the maximum

### Poisons and regeneration on monsters

- [ ] Inflict a card that drains HP over time on a monster (card 455: 8 % every 4 seconds) and
      select it: its HP bar drops by that share at each step for you and for a second player who
      has it selected, and the drain stops at 1 HP without killing it
- [ ] A regeneration card on a monster (card 1426: 20 % every 6 seconds) refills its bar up to
      its maximum and no further
- [ ] At each step the amount rises from the monster as a number, red for a drain, for everyone
      on the map whether or not they have it selected
- [ ] The same draining card on a Training Stake (monster 195, immune to debuffs): no `bf` is
      sent, no drain number rises from it and its HP bar only drops by the hits themselves. A good
      card still lands on it
- [ ] A Training Stake doesn't open a target window when selected (its data hides it), so read
      its HP from the bar over its head

## Character

### Character card and Self-Introduction

- [ ] Right-click another player's portrait: the card opens and shows their family tag, or none
      when they have no family
- [ ] Use a Self-Introduction (1117), type a line and confirm: "Your self-introduction has been
      registered", the item is used up, and the card shows the line
- [ ] Type more than 60 characters: the card shows the first 60
- [ ] Log out and back in: the card still shows the introduction

### Mounts

- [ ] Mount a vehicle: a three-second bar runs, then the sprite changes **and** you move faster
- [ ] Dismount: the speed returns to the class value
- [ ] Speed survives a map change while mounted
- [ ] With a second player watching, ride out of the map and come back: the watcher sees you on
      the mount, not on foot; the same when the watcher enters a map where you already ride
- [ ] Walk, cast a skill, or take a hit during the mounting bar: you don't mount
- [ ] Start the mounting bar and transform into your specialist straight after: the specialist
      bar doesn't finish off the mount, and the other way round
- [ ] Use a Movement Scroll (5214): after the bar you ride one of the novice mounts at its own
      speed, and the scroll count drops by one; use one again to dismount, and none is used
- [ ] Use a Novice Mount Scroll (5833) below level 70: you ride one of its six mounts, slower than
      that mount on its own, and the scroll stays; at level 70 it says your level is too high
- [ ] With a pet and a partner out, mount: both leave the map. Change map while riding: they stay
      away. Dismount: they come back next to you
- [ ] Sit down (`x`) and mount: you stand up first, then the bar runs
- [ ] Open a personal shop and use the mount: nothing happens
- [ ] Die while riding: you fall off the mount, and revive on foot at walking speed
- [ ] Use a Magical Camel Parchment Scroll (5923): you ride the camel and the scroll is used up
- [ ] Ride the camel of the Grenigas Relic (5914) on a desert map such as the Treasure Hunt Camp
      (200): it is clearly faster than on Chicken Farm Path, and back to its own speed after
      leaving the desert. The same holds for each region a mount's description names (Glacernon,
      NosVille, Port Alveus, Mount Krem, the Region of the Burning Sword, Moritius, Undercity)

### Class change

- [ ] `$ChangeClass` to another class
- [ ] The skill bar holds the new class's skills, not the old ones
- [ ] No leftover icons the client refuses to cast
- [ ] **Relog** and confirm the old skills have not come back — the rows behind the list
      have to be gone, not just the in-memory list
- [ ] With a second player online, `$ChangeClass <class> <name>` changes them the same way:
      their portrait and look switch, and they get the "class changed" message
- [ ] `$Gold <amount> <name>` on another player shows them "You have received: N Gold", and
      `$SetLevel <level> <name>` plays the level-up effect on them

### Skill teachers

- [ ] Talk to a skill teacher (NosVille Training) and pick Learn Skill: the Academy window
      lists the 18 training courses, with their prices and levels on hover
- [ ] The second tab lists only your own class's skills
- [ ] Double-click a course you have the level and gold for and confirm: "You've learned a new
      skill", the gold drops by its price and the course appears under Skill Info > Training
- [ ] Buy the stronger course of the same kind: the weaker one disappears from Training
- [ ] Try the weaker one again: "You've already learned a higher level"
- [ ] Try a course above your level: "You cannot learn that yet", no gold taken
- [ ] **Relog**: the bought course is still there
- [ ] Skill Info > Skill shows "Remaining CP": 40 plus two per job level, minus the CP of the
      class skills you know (training courses cost none)
- [ ] Buy a class skill: Remaining CP drops by its cost straight away
- [ ] Try a class skill that costs more CP than you have left: "You don't have enough CP", no
      gold taken
- [ ] Buy Life Extension (+150 HP) with the Character Info window (`p`) open: the maximum HP
      rises by 150 straight away; Mental Training does the same for MP
- [ ] Efficient Attack and Self-defence raise the attack and defence lines in Character Info
- [ ] **Relog** with those courses known: the maxima and lines keep their bonus
- [ ] The HP and MP recovery courses make the bars refill faster while standing
- [ ] With champion level 0, double-click a [Beginner] PvP Damage Boost book: "You cannot
      learn that yet" and the book stays
- [ ] At champion level 10, double-click it: "You've learned a new skill", the book is used up,
      no gold is taken and the boost appears under Skill Info > Training
- [ ] Double-click a second copy: "You've already learned" and the book stays

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
- [ ] Put a skill on the quick bar while transformed, untransform and transform again: the
      bar shows the normal skills while untransformed and that skill again once transformed
- [ ] After re-running the parser on an existing database, transform with a card at
      specialist level 0: the skill bar shows the card's first six skills and the basic attack
      hits. At level 4 the seventh appears, and at level 20 all eleven
- [ ] Wear a fire fairy and use a fire skill on a monster: the hits are larger than the same
      skill with no fairy worn
- [ ] Open the character window while transformed and untransformed: attack, defence and
      the resistances move with the card, and the card counts for nothing until transformed
- [ ] With a job level different from the card's level, open the Character window (P): while
      transformed "Job level" shows the card's level and its experience, and the job level again
      after untransforming

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
- [ ] Drag a ring or a fairy onto the upgrade window: the slot stays empty
- [ ] With the upgrade window open, accept a trade: the upgrade window closes, and Teoman cannot
      be talked to until the trade ends

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
- [ ] Transformed with a Warrior card and 30 points, keep attacking a monster: the SP point
      bar drops by 3 a second; once it is empty the transformation ends and the 30 second
      cooldown starts
- [ ] Stop attacking for more than 15 seconds while transformed: the bar stops dropping
- [ ] With the basic points gone, the additional points are spent next

### Specialist card perfection

- [ ] Put a specialist card in the bag with its perfection stones (a Warrior card takes Small
      Rubies of Completion), open an Alchemist's shop, press the SP Perfection button and drag
      the card in: the window asks 5 000 gold and 1 piece
- [ ] Press Upgrade: on success "Perfecting the SP successful: +1 (...)" names the stat that
      went up, and the gold and one stone are gone
- [ ] On a failure "Perfecting the SP failed" appears and the gold and stone are still spent
- [ ] With a card at perfection 20 the window asks 10 000 gold and 2 pieces
- [ ] Without the card's own stone, "Not enough ingredients" appears and nothing is spent
- [ ] A second-generation card takes the full-size stone each stone's description names (a
      Death Reaper spends an Obsidian of Completion)

### Soul Revival Stone

- [ ] With a specialist card whose soul was destroyed in the bag, double-click a Soul Revival
      Stone (1365 or 9039) and click the card: the client asks "Would you like to use Soul
      Revival Stone?"
- [ ] Cancel: nothing changes. Confirm: "The soul finds a new lease of life." appears, one
      stone is gone, the SP bar fills by 10 000 points and the card can be worn again
- [ ] Drop the stone on a card whose soul is intact, or on any other item: nothing happens and
      the stone stays

### Buffs of the same family

- [ ] Drink an EXP potion (1249), then use Ancelloan's Blessing (1286): "Ancelloan's Blessing
      effect activated" appears, the blessing icon shows and the potion's icon disappears, since
      both cards are in Card.dat GROUP 86
- [ ] Two buffs of different families (for example the EXP potion and a fairy booster) both stay

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

### Fairy Bead

The Fairy Bead is 4194 (box, effect 69, effect value 3). The client only lets some fairies onto
its window: Sellaim (274) is accepted, Solaris (273) is refused with "Please place your fairy here.".

- [ ] `$CreateItem 4194` and `$CreateItem 274`. Double-click the bead and confirm: the Fairy
      window opens
- [ ] Drop the fairy on the window and press Store: "In Custody." shows for three seconds, then
      the window closes and the fairy's slot empties
- [ ] Log out and back in: the bead is still in the bag
- [ ] Double-click the full bead and confirm: "You have received: 1x Sellaim" appears, the bead
      is gone and the fairy is back at the level it had
- [ ] With every other bag slot full, double-click a full bead: the not-enough-space message
      appears and the fairy stays inside

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
- [ ] Transformed with Titan Wings (`$CreateItem 5372`): the Titan Wings buff icon appears
      and max MP rises by 200; change to Archangel Wings (`5431`) and the icon swaps to
      Archangel Wings; untransform and the icon goes away

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

### Ancelloan's Blessing

- [ ] `$CreateItem 1286 2` and double-click it in the Main tab: the stack drops to 1, the
      "effect activated" message appears and the blessing icon shows in the static buff bar
      with an hour left
- [ ] Use the second one while blessed: nothing happens and it stays in the inventory
- [ ] Kill a monster while blessed: the experience gained is double that of an unblessed kill
- [ ] Die and revive: the blessing icon is still there
- [ ] Note the time left, go back to the character selection and come back a few minutes later:
      the icon is there again with the time it had when you left, not less
- [ ] After an hour the icon disappears and kills give normal experience again

### Fairy Booster

- [ ] Wearing a fairy, `$CreateItem 1296` and double-click it in the Main tab: it is used up,
      the "effect activated" message appears and the booster shows in the static buff bar with an
      hour left
- [ ] Use a second one while boosted: nothing happens and it stays in the inventory
- [ ] Hit a monster with a skill of the fairy's element: the damage is higher than unboosted
- [ ] Log out and back in while boosted: the icon is back with the time left and the damage is
      still the boosted one
- [ ] After an hour the icon disappears and the damage goes back to normal

### Speed Booster

- [ ] On foot, `$CreateItem 5119` and double-click it in the Main tab: nothing happens and it
      stays in the inventory
- [ ] Ride a Magic Scooter (1906, from the mount inventory) and use the booster: it is used up,
      the boost effect plays, a buff icon appears and the scooter runs faster (speed 22 against 20)
- [ ] Use a second one while boosted: nothing happens and it stays in the inventory
- [ ] After 3 seconds the icon goes and the scooter is back to its own speed
- [ ] Dismount while boosted and ride again: the icon is gone and the speed is the scooter's own
- [ ] On a White Unicorn (5319) the booster is used up and the character lands on a random
      walkable cell of the map with the teleport effect
- [ ] On a 'Doni Darkslide' Skateboard (5236) with HP and MP down, the booster restores level × 15
      of each (450 at level 30) with the heal number over the character
- [ ] On Nossi the Dragon (5196) while under a level 4 or lower debuff, the booster removes it;
      a higher level debuff stays
- [ ] On a Horned Sweeper (5152), walk a few cells in the open and use the booster: the rider
      jumps 5 cells further the way it was walking
- [ ] Do the same facing a fence a couple of cells away: the rider stops in front of the fence
- [ ] On a 12th Anniversary Speedy Alpaca (5807) with HP and MP down, the booster restores level × 10
      HP (300 at level 30) with the heal number over the character, and the MP bar stays where it was
- [ ] On a Rodeo Bull (9220) the booster shows two icons, the boost for 8 seconds and Red Flag for 5;
      a second player who selects the rider sees both
- [ ] The same with the mount's own buff on a 14th Anniversary Boxer Kangaroo (9276, Dukes Up!), a
      Royal Rabbit Carriage (9589, Magic Eggshell), a Steam Motorbike (13415, Steam Boost, together
      with the 5 cell jump) and Chi Tu (13752, Divine Gallop)
- [ ] On a Turtlecopter (9149) the boost lasts 9 seconds; when its icon goes the Dizzy icon appears
      and the rider can neither move nor attack for a second, then rides on at the mount's own speed
- [ ] Get off the Turtlecopter while boosted: the boost icon goes and no Dizzy follows
- [ ] On a boosted Turtlecopter next to a monster that hits in melee or from range, about nine hits
      in ten miss; once the boost is over they land as before
- [ ] On a Power Loader (1618) the booster is used up and the loader rides slower for 10 seconds
      (speed 19 against 23), and a level 4 or lower debuff is removed

### Fireworks and event toys

- [ ] `$CreateItem 1983` (Single Fireworks) and double-click it: one is used up and the firework
      goes off over the character for everyone on the map, with its sound
- [ ] An Airwaves gum (1332) shows the chewing emote over the character and is used up

### Ice Flower Oil

- [ ] `$CreateItem 5929` (Ice Flower Oil) and double-click it: one is used up, "effect activated"
      shows and a buff icon with 10 minutes appears
- [ ] Use another one: the time left goes up by 10 minutes, and never past two hours
- [ ] Strong Ice Flower Oil (5916) fills the buff to two hours; at two hours a further oil is kept
- [ ] With the character window (`p`) open, use an oil: melee, ranged and magic defence each
      rise by 5% straight away, without changing map
- [ ] Hit a monster with the oil on and off: the damage is about 5% higher with it
- [ ] When the buff runs out, the window drops back to the old defence

### Heat of the Burning Sword

- [ ] `$Teleport 205 <x> <y>` (Kertos' Jaws, Act 5.2) without heat resistance: the "Heat of the
      Burning Sword" icon appears in the static buff bar
- [ ] Stand still for a few seconds: HP drops by 8 % of the maximum every 2 seconds and stops
      at 1 HP
- [ ] Each drop floats over the character as a red number, for a second player on the map too
- [ ] Use an Ice Flower Oil there: the heat icon goes, the oil's icon shows and HP stops dropping
- [ ] Let the oil run out on the map: the heat comes back
- [ ] Walk or teleport to a map outside Act 5.2: the heat icon goes

### Magic Lamp

- [ ] Wearing any equipment, use a Magic Lamp (`$CreateItem 5105`): the client's own "remove
      your equipment" message appears and the lamp stays
- [ ] Naked, use it: the client asks whether to use the Magic Lamp to change your gender;
      Cancel keeps the lamp
- [ ] Confirm: the character's gender changes on screen, the gender-changed message appears and
      the lamp is gone
- [ ] After the change the portrait at the top left shows the new gender too, the same as after
      `$ChangeGender`; other players see the new look
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

### Marriage

Needs two characters who are friends, both online on the same channel.

- [ ] Select the friend and use Cupid's Arrow (`$CreateItem 1981`): a question asks whether to
      propose to them, and the arrow stays until you confirm
- [ ] Confirm: the arrow is used, you are told you proposed, and the friend gets a dialog asking
      whether to accept
- [ ] The friend accepts: everyone on the channel sees that the two of you are married, and the
      friend list of both shows the other as spouse
- [ ] The friend declines instead: everyone on the channel sees the proposal was declined, and
      nobody is married
- [ ] Propose to someone who is not a friend, or while already married: the matching client
      message appears and the arrow stays
- [ ] Delete the spouse from the friend list: the client's "cannot delete your spouse" message
      appears and the spouse stays
- [ ] Use the separation letter (`$CreateItem 1982`) and confirm: you are told you separated,
      the letter is used, and the spouse is gone from both friend lists
- [ ] Spouses group up: both get the Wedding icon in the static buff bar, and its tooltip reads
      "Increases experience points earned by 5%"
- [ ] A third player in the same group gets no icon
- [ ] One spouse leaves the group, or the group breaks up: both icons go away
- [ ] Separate while grouped: both icons go away
- [ ] With the icon, a kill gives 5% more level and job experience than the same kill without it
- [ ] Die with the icon: it is still there after reviving

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
- [ ] Lock your miniland, take away an object that stands in it, log out and in again and walk
      in: the object is not standing there, and it is still in the miniland inventory
- [ ] Visit a miniland whose introduction has spaces in it: the line across the screen on
      entering reads with its spaces, as the introduction in the corner does

### Respawn point

- [ ] Talk to a village NPC whose dialog has "Set Respawn Point" and press it: a question asks
      whether to make this village your place of revival, in the account's language
- [ ] Cancel it: nothing is saved and no message appears
- [ ] Confirm it: the client's own "This village is the designated place of revival." appears,
      and after logging out the `Respawn` row for that map's group holds the NPC's map and cell
- [ ] Die and choose to revive in the village: you come back at that NPC
- [ ] Die and close the client without answering, then log in again: you stand at your place of
      revival with 1 HP and 1 MP, and no revival question is asked
- [ ] Above level 20, die with 15 Seeds of Power: the dialog asks for 10, and confirming shows
      "You have used 10x Seed of Power.", you get up where you fell, and the Etc pocket shows 5
      seeds straight away
- [ ] With 9 seeds, confirming says you don't have enough and sends you to the save point

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
- [ ] Hold 997 of an item and pick up a drop of 5 of it: the stack shows 999 and the next free
      slot shows 3 straight away, without leaving the map
- [ ] With the bag full, try to pick up an item: the not-enough-space message appears and the
      item stays on the ground for anyone to pick up
- [ ] Spam double-click on a single box or sealed tarot card: one reward arrives and the box or
      card is gone; with a stack of two, two rapid double-clicks give two rewards and empty the stack
- [ ] With the bag full, double-click a box: the not-enough-space message appears and the box
      stays in its slot
- [ ] Double-click a Tarot Card Game: it is used up and five sealed tarot cards from Fool to
      The Sun arrive, with one "received" line per kind of card
- [ ] With one free Main slot and no sealed cards yet, double-click a Tarot Card Game: unless all
      five cards came out the same, the not-enough-space message appears and the game stays
- [ ] Use a title item and a speaker: both still work as before
- [ ] `$CreateItem 1401` (Legendary Treasure Chest) and double-click it: the chest opens at once,
      the reward window shows one item from the list in its description, and the chest is gone
- [ ] `$CreateItem 5183` (Black Bushi Costume Set) and double-click it, confirming the "Use the
      item." box: the costume and the hat both arrive in Equipment with one "received" line each,
      and the set is gone; with the Equipment pocket full the set stays and the not-enough-space
      message appears
- [ ] Double-click a raid package whose description names items the parser cannot match (1562):
      nothing happens

### Perfume

- [ ] Double-click a Perfume: the perfume window opens
- [ ] Put a level 70, rarity 5 weapon bound to another character into the window and confirm:
      80 perfumes and 15 000 gold are taken, the "perfumed" message appears, and the weapon can
      now be worn
- [ ] With 79 perfumes, confirm the same weapon: the not-enough-perfume message appears and
      nothing is taken
- [ ] With 14 999 gold, confirm: the not-enough-gold message appears and nothing is taken
- [ ] Put your own bound item into the window: nothing is taken

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

### Moving into the card and costume inventories

- [ ] Drag a costume from the EQUIP tab onto a taken slot of the costume inventory (ADDITIONAL
      tab): it lands in the first free slot and leaves the EQUIP tab
- [ ] Not verifiable yet: a move onto a taken slot being refused, since the client always picks
      a free slot itself

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
- [ ] Drink the last potion of a stack: its slot is drawn empty at once
- [ ] Use one of a stack of two NosMerchant medals or backpacks: the slot shows 1 straight away,
      not 2

### Resting in combat

- [ ] Sit next to a monster that attacks: you stand up as soon as its hit lands, and a second
      client on the map sees you stand too
- [ ] Between its hits you can sit back down
- [ ] Attack a monster and press sit: nothing happens and no message appears
- [ ] Stop attacking: sitting works again about three seconds after your last attack, even if
      the monster died sooner
- [ ] Sit with a Roasted Herring digesting and let a monster hit you: you stand up and the
      green numbers stop
- [ ] Stand with missing HP: the bar refills every two seconds; sitting refills it faster
- [ ] Stand while a monster keeps hitting you: the bar does not refill until four seconds after
      its last hit, and changing map in between does not reset that wait

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
- [ ] Open the page of a level 48 Baron Scratch (2682): its maximum HP reads 10638 and its
      maximum MP 708, not the creature's own 657/60
- [ ] Level a pet or partner up: the maximum HP and MP on its page and in the party panel grow
      with the level
- [ ] Open the page of a level 40 Otter (683): attack reads 314 to 441 with hit rate 269,
      defence 155 / 142 / 117 and dodge 254, not the level 1 creature's own values
- [ ] Level a pet up: its attack, defence and dodge grow with the level; a partner keeps its
      creature's values

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
- [ ] Transformed, the partner skill bar shows the card's three skills (Shift+Q, W, E). Select a
      monster near the partner and press Shift+Q: the partner plays the skill, the monster's bar
      drops, the partner's MP drops and the slot shows its cooldown
- [ ] Press Shift+Q again during the cooldown: nothing happens; once it is over the skill works again
- [ ] Turn the partner back: the partner skill bar goes away

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
- [ ] Strange Pet Food (2325) on the level 5 pet: it drops to level 4, the pet window shows the
      new level and lower stats, "Your NosMate eats the food very tentatively." shows, and the food
      is used up. On a level 1 pet, or on a partner, nothing happens and the food stays
- [ ] Strange Partner Food (2421) on the level 10 partner: it drops to level 9 with "Your partner
      is eating the food very hesitantly."
- [ ] Name Tag for Pets (2157) on the pet: a window asks for a name. After you confirm, the pet's
      name changes above it, on its portrait and in the pet window, "New name has been assigned to
      your teammate." shows, and one tag is used up. Cancelling the window, or using the tag on a
      partner, keeps the tag
- [ ] Pet Food (2077) on the pet: its loyalty in the pet window goes up by 100, it gains some
      experience, "The pet is happy with the food." shows, and the food is used up. A pet at 3000
      loyalty does not eat it; a partner does not either
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

### NosMate beads

`$CreateItem 335` gives a partner bead and `$CreateItem 348` a pet bead.

- [ ] Use a bead outside your Miniland and confirm: "Can only be used in the Miniland." shows and
      the bead stays
- [ ] Use the partner bead in your own Miniland: it first asks whether to take the partner out,
      and after confirming the bead is gone, the partner stands in the Miniland at the bead's level
      and the "partner is out of the bead" message shows
- [ ] Do the same with the pet bead: the pet comes out, and it is still there after relogging
- [ ] Answer no to the question: nothing happens and the bead stays

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

### A NosMate knocked out

`/mnt/project-files/partner-skills/scene.sh` (stop the stack first) puts a Baby Dander that hits
hard next to your partner, a partner card on the partner and `SEEDS` Seeds of Power (default 7)
in the Main bag; `SPEED=10` lets the Dander walk to the partner.

- [ ] Transform the partner and hit the Dander with a partner skill: the Dander turns on the
      partner and hits it, and its HP in the party panel drops
- [ ] With 5 or more Seeds of Power, let the Dander knock the partner out: it leaves the map, five
      seeds are gone, "5 Seeds of Power used" and "NosMate dead: it will come back in 3 minutes"
      show, the partner is turned back and its skill bar goes away
- [ ] Wait 3 minutes: the partner is back at your side with full HP and MP
- [ ] With fewer than 5 seeds: "NosMate dead: It has returned to your Miniland because you don't
      have any Seeds of Power.", the partner leaves the team panel and is found in the Miniland

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
- [ ] Take a parcel's item with the inventory window open: the item appears in its slot at once,
      without a relog
- [ ] With the main inventory full, take a parcel: "Not enough space" shows, the parcel stays in
      the list, and taking it again after freeing a slot delivers it
- [ ] Send a note to a character that is online: the note icon lights up for them at once, the
      note is in their received box, and the sender has it in the sent box
- [ ] Send a note to a character that is offline: it is in their box when they next log in
- [ ] Open a received note: its text shows, and it is marked read after a relog
- [ ] Send a gift: the receiver gets one parcel and the sender finds the gift in the sent box, not
      the receiver
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

### Forming a party

- [ ] A invites B and B confirms: both see the other in the party panel and "In party" under
      both names, and A is told "You are now the group leader"
- [ ] B toggles the party sharing line: B is told only the leader can do that; A toggles it and
      both see the new looting mode
- [ ] A third joins and A leaves: B is told they lead now
- [ ] A party of two ends: each of the two sees the party-disbanded message once
- [ ] A types `$Party B` (or the client language's alias, such as `$Invitation B`): B gets the
      party invite, the same as from the menu. `$Party Nobody` answers "Unknown person."
- [ ] A types `$Partylocation` (or the client language's alias) alone: the chat says A is not in
      a party. In a party with B on a map, it names B with the map number; with B in a miniland or
      another instance, it says B is somewhere unknown. `$Partylocation B` names only B.

### Leaving a party

`/mnt/project-files/group-disconnect/scene.sh <checkout> <outdir>` groups Tester and Buddy and
closes Buddy's connection.

- [ ] Two characters group up and one logs out: the other's party panel empties and "In party"
      goes away
- [ ] Three characters group up and the leader logs out: the two left stay grouped and one of
      them is told they lead now
- [ ] Two married characters group up and one logs out: the other's Wedding icon goes away
- [ ] In a party, one member is wounded or drinks a potion: within a couple of seconds the HP
      and MP bars of that member in the others' party panel follow

### Sharing the point of return

Two characters in a party, the sharer with a point of return set at a NosVille statue. Open the
party menu (the arrow above the party panel) and pick Return Spot.

- [ ] The sharer is asked to confirm; confirming asks every other member whether to take the
      sharer's point of return, naming the sharer
- [ ] A member confirms: they are told the sharer changed their point of return, the sharer is
      told it worked, and after logging out the member's point of return is the sharer's
- [ ] A member cancels: they are told they refused, and the sharer is told who refused
- [ ] A sharer with no point of return yet is told sharing is not possible and nobody is asked

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
- [ ] A whispers A's own name: the chat shows the line once, with no "(From A)" copy
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
- [ ] A monster hits A; A requests a trade: A sees "Cannot trade during combat." and B gets no
      dialog. Twenty seconds after A's last hit or attack the request goes through
- [ ] A monster hits B; A requests a trade: A sees "Cannot trade with someone in combat."
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
- [ ] To someone who blacklisted the owner, or whom the owner blacklisted: the owner sees
      "You have already been blacklisted.", the other player is not asked, and no seed is used
- [ ] From the friend list, visit a friend's open miniland from the open world and from your
      own miniland: both work
- [ ] Try the same from inside a time-space or an arena: the client's "cannot be used here"
      line appears and you stay where you are

### Miniland points

- [ ] Log in and, before entering the miniland, open the Mini-Land window (menu, house icon):
      the theme, intro, visitors and Make Point are already filled in
- [ ] Place or remove an object in the locked miniland: Make Point keeps showing the production
      points left for the day, not the object's own points

### Personal warehouse, pet basket and partner backpack

`/mnt/project-files/pet-backpack/scene.sh` gives the tester potions, a Miniland Bell and a
warehouse (3005) placed in the miniland; `mates.sh` adds a pet and a partner out and, with
`BONUS=1`, the bought pet basket and partner backpack.

- [ ] In the miniland, open the warehouse shed: the window shows 0/21. Drag potions into it: they
      leave the bag and the counter reads 1/21. Move them to another slot, then drag them back
      into the bag
- [ ] Equipment from the Miniland pocket cannot be dropped in, and nothing moves while trading
- [ ] With the pet basket bought, the pet in the party panel has a small icon; clicking it asks
      "Would you like to open the warehouse?" and Confirm opens the same 21-slot warehouse
      anywhere
- [ ] Without the pet basket the icon is not there
- [ ] With the partner backpack bought, the partner's icon asks "Do you want to open your
      partner's backpack?" and Confirm opens the Partner's Backpack with 50 slots; deposit and
      withdraw work there too, and its content is separate from the warehouse
- [ ] Log out and back in: stored items are still in their slots
- [ ] Deposit part of a stack, or withdraw a stack, then stop the world server without logging
      out: after a restart the items are where the transfer left them, once and only once

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
- [ ] Note the attack in the Character window, then put a shell with "Enhanced damage" on the worn
      weapon: the window's attack rises by that amount straight away, and falls back after the
      Magic Eraser
- [ ] An armour shell with a defence or resistance option raises that line in the Character window
      the same way
- [ ] A weapon shell with an SL option (SL damage, defence, element, energy or all) changes
      nothing until you transform; once transformed, the Character window's attack (or defence,
      element, HP/MP) is higher than with the same card and a weapon without the shell
- [ ] Hit a Baby Dander (a low-level plant) several times with a weapon whose shell carries only
      "Increased Damage to Plants", then with the same weapon after the Magic Eraser: the range
      of damage numbers is higher by that percentage. The same weapon on a Kovolt, an animal or
      an undead monster changes nothing unless the shell names its race
- [ ] Hit a monster with a weapon shell carrying Minor Bleeding, Bleeding, Heavy Bleeding,
      Blackout, Deadly Blackout or Freeze: now and then the debuff icon appears over the monster
      and, for bleeding, its HP keeps dropping between hits

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
- [ ] Right after the revive the buff bar shows the protection icon (card 169) for five seconds
- [ ] Closing the question with Escape counts as Cancel
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

### Muted accounts

A mute is a row in `PenaltyLog` for the account (`Penalty` 0, `DateEnd` in the future). There is
no command for it yet, so insert the row; it is read on every line, so no restart or new login
is needed.

- [ ] Say something in general chat: a speech bubble with the client's own "<I am serving a
      penalty.>" appears over the character, and "Remaining punishment: N minutes" shows in red
      in the chat window. The client still prints the line you typed in your own window
- [ ] A second player on the map sees the bubble and never the words
- [ ] Use a speaker item: the red line shows, nobody hears it and the speaker stays in the bag
- [ ] Set `DateEnd` to the past: the next line is said normally

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

### Dye Bomb

- [ ] Wear a Wig and double-click a Dye Bomb, then confirm: the wig takes another colour, a second
      player on the map sees the same colour, and the bomb is used up
- [ ] Equip a Wig dyed earlier: it shows its own colour rather than the hair colour, also after
      changing map and relogging
- [ ] Use a Dye Bomb with no hat, or with a hat that has no colours: "You're not wearing this
      item." appears and the bomb stays

### Speech bubble

Two characters A and B on the same map; A has a Speech Bubble (2174) in the Etc pocket.

- [ ] A double-clicks the bubble and confirms: a "Speech Bubble Text" window opens
- [ ] A types "Hello world" and confirms: the bubble is used up and "Hello world", spaces
      included, shows above A on both clients within about 5 seconds
- [ ] B leaves the map and comes back: the bubble is above A again within about 5 seconds
- [ ] A confirms the window empty: nothing shows and the bubble stays in the inventory
- [ ] After 30 minutes the bubble is gone on both clients; A relogging removes it at once

### Whispers and friend messages

Two characters, A and B, on the same channel, then on different channels.

- [ ] A whispers B several times in a row: every whisper arrives, in order
- [ ] A blacklists B, then whispers B straight away: A is refused with the blacklist message and B
      receives nothing. A removes B from the blacklist and whispers again: it arrives
- [ ] B logs in and A whispers B at once, also right after A whispered B while B was still
      away: the whisper arrives. The same for a friend message to a friend who has just logged in
- [ ] A types `$Adding B` (or the client language's alias): B is asked "Become friends with A?",
      and confirming makes them friends. `$Adding Nobody` answers "Unknown person."
- [ ] A has several friends, some online, some not: the friend list shows each with its name
      and the right online state; a friend message to an online friend arrives, to an offline
      friend shows "friend offline"
- [ ] B logs out while A has B's conversation window open; A's friend messages from then on,
      including the first ones, each show "Friend is not online"
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
- [ ] "View Warehouse History" opens the history window: every deposit shows as "List
      <name> > <item> X <amount>Piece" and every withdrawal as "Remove ...", newest first, with
      "(within 1 hour(s))" or "(N hour(s) ago)" / "(N day(s) ago)"
- [ ] The history keeps the newest 200 entries; scrolling to the bottom shows the 200th
- [ ] With the window open, another member's deposit appears at the top after pressing Update
- [ ] A member whose rank may not read the history (family settings) gets "Not authorised."
      instead of the window; the head and assistants always see it
- [ ] The family's own history window (fhis) still leaves warehouse entries out

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

### Messenger rankings

The Messenger window (`N`) has three ranking tabs. Each is built once a day at 5 AM server
time, and on the first login after a start, so a value changed in between only shows the next
day or after a restart of the world server.

- [ ] Popularity List: characters by compliments, highest first, as rank, level, name and
      count; a hero level shows as `Lv80(+7)`
- [ ] Rep. Rankings: the same by reputation
- [ ] CP Rankings: the same by Act 4 points
- [ ] A character with 0 in a list's value is not in that list
- [ ] Characters of a game master account are in none of them, however high their values

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

### Family changes keep what was saved elsewhere

Same seeded family, with the head in the world. Before **each** command below, raise the
family and a member behind the session's back, with a new value every time. This stands in
for experience earned on another channel since the head's window was last refreshed:
`update "Family" set "FamilyLevel" = "FamilyLevel" + 1, "FamilyExperience" = "FamilyExperience" + 100 where "Name" = 'Testers';`
`update "FamilyCharacter" set "Experience" = "Experience" + 100 where "CharacterId" = <member>;`

- [ ] After each of `%Notice <text>`, `%Today <text>`, a permission tick, `%Title <name> <rank>`,
      `%Familydeputy <name>` and `%Gender <0|1>` as the head, the family window shows the level
      just set and the two rows still hold the raised level, experience and contribution
- [ ] `%Familydismiss <name>` removes the member's row and leaves the family row as it was
- [ ] With the head in the world and the Family tab open, swap the ranks behind the session
      (`Authority` 3 for the head, 0 for the member), then type `%Notice <text>` as the former head:
      the hint stays as it was, and the window and the tag under the name now show the stored ranks
- [ ] The same demoted character then gets the ordinary refusals: `%Familydismiss <name>` says they
      are not authorised, and ticking a permission changes nothing

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

Same seeded family, with the head and a plain member both in the world and the head's family
window open. Stage the member and the family with both characters logged out, since a character
is saved when it leaves:
`update "Character" set "Level" = 29, "LevelXp" = 1060199 where "Name" = '<member>';`
`update "Family" set "FamilyLevel" = 1, "FamilyExperience" = 79800 where "Name" = 'Testers';`
`update "FamilyCharacter" set "Experience" = 0 where "CharacterId" = <member>;`
The member is then one experience point short of level 30 and the family 200 short of level 2
(`79 800 / 80 000`). `$SetLevel 30` does not stand in for the kill: it sets the level without the
member earning it and gives the family nothing. Any kill that gives the member experience will
do; a monster ten or more levels below gives a tenth of its experience, so pick one that is
worth at least 10.

- [ ] Before the kill the family window shows level 1 with `79 800 / 80 000`
- [ ] The member kills a monster and reaches level 30: the level bar in the family window of
      both characters moves on its own to level 2 with `400 / 96 000`, without reopening it
- [ ] Both read "Family level increased to 2!"; the member is sent it right after their own
      "Your level has increased."
- [ ] The history tab gains three lines, newest first, on both screens without reopening it:
      "Reached family level 2!", "<member> has earned 600 action points!" and the member's
      level 30 line; the table holds type 5 with `2`, type 4 with `<member>`, `600` and type 6
      with `<member>`, `30`
- [ ] The family window's member list shows 600 as the member's experience and 0 as the head's
- [ ] With the family at `0 / 80 000` instead, the same kill only moves the bar to `600 / 80 000`
      and writes the type 4 and type 6 lines: no level-up message, no type 5 line
- [ ] Reaching 31, or 20, gives the family nothing; reaching 40 gives 800
- [ ] With the head on the second channel, the head's window, history and "Family level
      increased to 2!" follow in the same way
- [ ] What is gated on the family level follows: with the head next to Salim (see "Family
      extensions") and carrying 500 000 gold, the first Family Warehouse row is refused before
      the kill with "Your family level is insufficient."; after the kill, without logging out,
      the same row is bought
- [ ] Logging both characters out and in again shows the same level, bar and history
- [ ] Not verifiable yet: experience from time-spaces and raids, which do not exist yet. Winning
      an Instant Combat only counts toward the daily mission (see "Family missions")

### Family achievements

Run the parser's quest import first. `/mnt/project-files/family-quests/reset-family-quests.sh quote`
seeds the family above with 19 of the 20 quotes of the day. The family window's Missions button
opens the list; its Achievements tab holds them.

- [ ] The list keeps the state it had when the window was first built, so pick another entry in
      the filter after logging in: "Enter 20 Quotes of the Day" moves to the top with
      "[Progress: 19/20]"
- [ ] `%Today <text>` with the window open: the 20th quote completes it ("[Completed: <date>]"),
      "Enter 100 Quotes of the Day" shows 20/100, every member sees "Family Testers unlocked
      achievement: Enter 20 Quotes of the Day", and its 10 000 FXP takes the family to level 2,
      which also completes "Family Level 2 Unlocked" with its own message
- [ ] Levelling a member up to fill the bar instead also completes "Family Level 2 Unlocked"
- [ ] Logging in again keeps every completed date and the 20/100 count
- [ ] Not verifiable yet: the other achievements (raids, Rainbow Battle, time-spaces), which have
      nothing that counts them yet

### Family missions

Run the parser's quest import first. `/mnt/project-files/instant-combat/reset-family-mission.sh`
puts Tester's family at 9 of the 10 Instant Combats of "[Daily] Complete 10 Instant Combats".
Open the family window's Missions button and pick "Daily Challenges" in the filter.

- [ ] The mission shows "[9/10] (Total: 0)"
- [ ] Winning an Instant Combat completes it: every member sees the unlock message, the family
      gets its 3 000 FXP and the entry shows today's date
- [ ] A second win by the same member on the same day does not count
- [ ] The next day the mission shows as not started, with its total kept
- [ ] Not verifiable yet: the other missions (raids, Rainbow Battle, Glacernon), which have
      nothing that counts them yet

---

## Instances

### Time-space entrances

No time-space ships in the seed data, so add one: a `ScriptedInstance` row with `Type` 0
(time-space) on a map you can reach, its `Script` naming a file in the world server's
`instances/` folder without the extension (`timespace-01` runs `instances/timespace-01.csx`).
The script holds the level range, required items, rooms and start coordinates. Stand on the
stone.

A script is C# compiled into the world server and runs with the server's full rights, the
same trust as the server's own code, so only the operator should be able to write to
`instances/`. Only files directly in that folder are loaded: a `Script` such as `../x` or
`sub/x` is refused.

- [ ] A `Script` of `../timespace-01` opens no panel, and the world log says it is not a
      script name

- [ ] Walking onto the stone opens the time-space panel with its name and level range, and
      empty reward slots when the script declares no rewards
- [ ] A character outside the level range is refused with a message and stays put
- [ ] A character without the required items is refused with a message and keeps what
      they had
- [ ] With the items, START moves you into the first room at its start coordinates
- [ ] The required items leave the inventory, across stacks if split, and no more than asked
- [ ] A `Script` naming a missing or broken file opens no panel, and the world log says why

### Inside a time-space

Run `timespace-01`, the sample shipped in `instances/`. Its monsters are 58, 59 and 60;
dropping their `MaxHp` makes the run quick.

- [ ] On entry a clock appears and counts down from ten minutes
- [ ] Three hostile monsters appear in the first room and are drawn as soon as the room is, and a padlocked portal stands at its
      east edge; walking onto the padlock does nothing
- [ ] No time-space stone is drawn inside the rooms
- [ ] Killing the third monster opens the padlock; walking onto it moves you to the second
      room, where a single Weak Devil Vampire waits
- [ ] Killing it shows Mission Clear with the monsters killed, the rooms explored and
      the "Perfect" stamp when every summoned monster died
- [ ] About ten seconds later you are back beside the stone on the open world, and the clock
      is gone
- [ ] Let the clock run out instead: the score shows a time-out and you are sent back the
      same way
- [ ] Log out inside a room and log back in: you appear on the open-world map you entered
      from, not in a room, and the world log shows the run's rooms being removed and no
      failed disconnect step
- [ ] Copy `timespace-01.csx` to a new script whose `OnFirstEnter` loops forever, and enter
      it: about thirty seconds later (`InstanceScriptTimeout`) the score shows a failure, the
      world log says the script did not return, and ten seconds after that you are sent back
- [ ] Do the same with an `OnFirstEnter` that throws: the failure score comes at once and
      the world log shows the exception
- [ ] Monsters killed inside a run do not respawn
- [ ] Inside a run, the client's exit button (`rxit`) sends you back beside the stone, and a
      run you were alone in is gone (entering again starts a new one)
- [ ] An `escape` sent inside a run does the same as `rxit`: the player is back beside the
      stone, and other players standing there see them reappear. Inside a raid it leaves the
      raid team as `rxit` does; outside an instance it does nothing
- [ ] Not verifiable yet: the button that sends `escape`. It belongs to the time-space room
      map, which this client does not draw yet, so drive it from a second, headless player
- [ ] Disconnect inside a run you were alone in, then log back in: you are on the open world,
      not in a room

### Time-space reward chest

Copy `timespace-01.csx` to `timespace-chest.csx`, point the row's `Script` at it, and add to its
definition `.WithGold(1000).WithReputation(50).WithReward(1012, 2).WithSpecialReward(2282, 1)
.WithDraw(1013, 3).WithDraw(1014, 1).WithFamilyExperience(200)`. Play it with a character in a family.

- [ ] The stone's panel lists the two draw items, the special item and the bonus item in their
      slots
- [ ] Win the run: the client opens the chest as the score window appears. It shows the bonus,
      special and drawn items; chat says 1000 gold and 50 reputation were earned, and the gold
      and reputation shown on the character go up by those amounts
- [ ] The bonus, special and one of the two draw items are in the inventory
- [ ] Confirming the score window gives nothing more
- [ ] The family window's history shows 200 family experience from the character
- [ ] With a full inventory, the items arrive in the parcel box instead
- [ ] Near the gold limit, the gold stops at the limit
- [ ] Let the clock run out: the score has no chest to open and nothing is given

### Raids

Set the `Script` column of a raid row (`Type` 1 in `ScriptedInstance`; the row only carries the script) to
`raid-mother-cuby`, the raid shipped in `instances/`. It is opened by item 1127, for levels
30 to 49 and teams of 5 to 15; the boss is 282 and its helpers 283, and dropping their `MaxHp`
makes the run quick. Use five characters at level 30 to 49, called A to E below, all on one map.
The raid's gate is the swirling portal its `ScriptedInstance` row places on a map (Mother Cuby's
stands at the top of Jelly Entrance, 2510 at 20,5). "A starts" means A steps on that gate and
confirms "Do you want to create the raid map?".

- [ ] A uses the seal: "You are the raid leader" appears, the seal is gone from the inventory
- [ ] A uses a second seal: "already in a raid" appears and the seal stays
- [ ] B in a party with C uses a seal: "cannot create a raid while in a group" appears
- [ ] A invites B (the client sends `rd 1 <B>`): B is asked to join A's team; accepting puts B in it and
      A sees that B joined
- [ ] B answers an invitation A never sent (e.g. from an old dialog): nothing happens
- [ ] B leaves: B is told they are out and A sees that B left. A kicks C: C is told they were
      kicked
- [ ] A disbands the team before starting: every member is told the team was disbanded
- [ ] A leaves while B and C stay: B becomes the leader and everyone is told
- [ ] With only A and B, A starts: "too few raid members" appears and nobody moves
- [ ] With D on another map, A starts: "all raid members should be on" appears
- [ ] With E at level 20, A starts: "level does not match" appears
- [ ] With A to E on A's map, A starts: all five land at the bottom of Jelly Entrance and a
      40-minute clock appears; the portal at the top leads to Jelly Square, where Mother Cuby
      waits in the middle among Cubys, with a pack of jellies in each corner
- [ ] Killing a Cuby does nothing; killing Mother Cuby ends the raid: every member still inside
      gets one Raid Box of Mother Cuby (4684) and a "received" line, the team is dissolved, and
      about ten seconds later everyone is back on the map they started from
- [ ] A member with a full inventory gets "Not enough space" instead of the box, and the others
      still get theirs
- [ ] B dies: "You will be revived in 20 seconds. You can only be revived once." appears, B
      stays down for 20 seconds, then stands up with full HP and MP where they fell
- [ ] B dies a second time: the message now says the next death takes B out of the raid, and B
      stands up again 20 seconds later
- [ ] B dies a third time: "You have used all your lives." appears, B is back on the map they
      started from with 1 HP and out of the team, and the raid goes on for the others
- [ ] Let the clock run out: the raid fails, nobody gets a box, the team is dissolved and
      everyone is sent back
- [ ] B presses the exit button inside the raid: B is back on the map they started from and out
      of the team, and the raid goes on for the others
- [ ] C disconnects inside the raid: the others see that C left; when the last member
      disconnects, logging back in finds no raid room left
- [ ] D, not invited, cannot get into the team by any button while the raid runs
- [ ] A invites B, then A leaves and opens a new team with another seal; B accepts the old
      question: B stays out of A's new team
- [ ] With only A and B in the team, B disconnects before the start: A stays in the raid team
      and sees that B left
- [ ] On Jelly Entrance the gate shows at the top of the path; Chicken Farm Path, whose raid row
      has no script, shows none
- [ ] D, in no raid team, steps on the gate: "A team must first be created for the raid." appears
- [ ] B steps on the gate: "Team leader must lead the raid." appears. A team opened with another
      raid's seal gets "The raid does not match the seal."
- [ ] A uses the seal: the "Raid team" panel opens on the right with A alone, out of 15
- [ ] B joins, then C leaves: every member's panel adds B, then drops C, with A first
- [ ] Inside the raid, B dies once: B's line in every panel counts one death
- [ ] The raid board opens on the server's `rl`, and no menu that sends `rl 0` has been found yet,
      so the board checks below are run from a headless character's `rl` packets
- [ ] A registers the team (`rl 1`): D, outside any team, sends `rl 0` and gets A's team with A's
      name, level and class, the raid and the member count
- [ ] D asks to join from the board (`rl 3 A`): D joins without an invitation and every panel adds D
- [ ] A removes the team from the board: "Removed from the raid list." appears and D's board
      no longer shows it; B trying the same is told only the leader can
- [ ] A registers twice: "Already registered in the raid list." appears
- [ ] A starts the raid: the team is gone from everyone's board

### Instant Combat

Run `$Event InstantCombat` as a GM to skip the countdown, or wait for the next even hour (the
countdown starts at :55). Use two characters on a base map.

- [ ] The countdown says "In 5 minute(s) Instant Combat will begin.", then 1 minute, 30 seconds
      and 10 seconds, in the chat and as a notice, in the client's language
- [ ] "An Instant Combat has started" shows and the join question appears as an icon; a second
      `$Event InstantCombat` answers that the event is already running
- [ ] After 30 seconds whoever said yes is moved to the Instant Combat map; whoever did not has
      the question removed
- [ ] Characters of level 45 and 85 land in different rooms, each fighting its own bracket's
      monsters
- [ ] "A horde of monsters is slowly approaching" comes 10 seconds before each wave and
      "Monsters have appeared" with it; five waves, the last one with a boss
- [ ] Gold piles and items appear around the map before the next wave
- [ ] Killed monsters do not come back
- [ ] Dying sends the character back where they came from with 1 HP, without the revive question
- [ ] Clearing the last wave once 12 minutes have passed shows "You have won the Instant Combat",
      and gives level x 50 reputation, level x 1000 gold and level x 100 SP points
- [ ] Ten seconds later everyone is back where they came from
- [ ] Not clearing it in 15 minutes shows "Instant Combat time has elapsed" and gives nothing
- [ ] The fight's timing comes from `instances/instant-combat.csx`: changing a wave's time there
      and restarting the world server moves that wave
- [ ] Leaving the room as its last player closes it; a new Instant Combat still starts afterwards

### Icebreaker

Run `$Event IceBreaker` as a GM to skip the countdown, or wait for the next half hour (the countdown
starts at :25). The first run is for levels 1-25, then 20-40, 35-55, 50-70, 65-85 and 80-99. Use two
characters in the bracket on a base map, each with at least 500 gold.

- [ ] The countdown says "In 5 minute(s) 1~25 Icebreaker will start for all levels.", then
      1 minute, 30 seconds and 10 seconds, in the chat and as a notice, in the client's language
- [ ] "Icebreaker has started" shows; only characters in the bracket get the join question, which
      asks for 500 gold
- [ ] Saying yes takes 500 gold and moves the character to the Icebreaker map at once; without
      500 gold it says "You don't have enough Gold"
- [ ] Saying yes after 10 seconds says "You cannot join Icebreaker once it has started!"
- [ ] With one character in the map, it ends at once: "Icebreaker is over" and the reward
- [ ] With two, "PvP will be available shortly..." shows three times, then "Begin attack!"; before
      that, nobody can hit anybody
- [ ] After "Begin attack!", players hit each other, but never members of their own group
- [ ] A character whose HP reaches zero is not offered a revive: "X is frozen." shows, they stand
      up with full HP, cannot move or attack, the ice effect plays on them, and nobody can hit them
- [ ] When one player (or one group) is left standing, "Icebreaker is over" shows and they get the
      bracket's gold (100 / 1 000 / 3 000 / 5 000 / 10 000 / 20 000), level x 10 reputation, and
      dignity raised to 100 if it was lower; frozen players get nothing
- [ ] "You will return to the starting position in 10 seconds." follows, then everyone is back
      where they came from and can move and attack again
- [ ] After 10 minutes without a winner, "Icebreaker will end in 30 seconds" then "Icebreaker time
      has elapsed" show and nobody is rewarded
- [ ] The warnings, the start of PvP and the ten-minute limit come from `instances/ice-breaker.csx`:
      changing a time there and restarting the world server changes it in game
- [ ] A frozen player who logs out does not stop the others' fight, and the last player leaving
      the map closes it

### Land of Death

Run `$Event LandOfDeath` as a GM, or wait for 14:00 or 19:00 server time. It lasts an hour. Use a
level 55+ character in a family, standing next to the gatekeeper on map 98 (5,37), with a Saver
(item 1211) or two.

- [ ] Without a family, talking to the gatekeeper says "Only family members can enter the Land of Death."
- [ ] Before the event, or after the first Flying Fire Devil has gone, it says "You are not authorised to
      enter this place."
- [ ] Under level 55 it says "Your level is too low. You can only enter from level 55."
- [ ] Otherwise it asks "Would you like to move to the Land of Death?"; yes moves the character to
      (153,145) of a Land of Death only their family shares, with the clock counting down from the
      time left, and the land is full of its usual monsters
- [ ] A second member of the same family lands in the same copy; a member of another family lands in
      another one
- [ ] Monsters drop no gold, and anyone can pick up an item; "X has received an item" shows to everyone
- [ ] With 30 minutes left "The hunt for the Flying Fire Devil has begun." shows and the devil appears
      on the last player who came in and attacks them; experience and drops are tripled
- [ ] A minute later "The Flying Fire Devil has vanished." shows, the devil disappears and the rates
      go back to normal; it comes back every 4 minutes until the end
- [ ] Falling asks whether to use a Saver: yes spends one and revives on the spot at full HP; no,
      no Saver, or no answer within 30 seconds says "You will be returned to the entrance of the Land
      of Death." and puts the character back next to the gatekeeper with 1 HP
- [ ] With 5 minutes left "The Land of Death will be closed soon." shows
- [ ] At the end, everyone is back on map 98 about 10 seconds later and "You are now out of the Land of
      Death" shows
- [ ] The portal at (172,171) leads back to map 98 at any time
- [ ] However the character leaves, the clock disappears from the bottom left
- [ ] Once the last member of a family has left, their land closes; the next member to go in finds
      a fresh land, with the clock and the devil still on the event's time (entering during a hunt
      brings the devil out at once)
- [ ] The hunts, the rates and the closing warning come from `instances/land-of-death.csx`: changing
      a time there and restarting the world server changes it in game

### Arena of Talents

Run `$Event TalentArena` as a GM, or wait for 18:00; it stays open four hours. Six level-80
characters (levels within five of each other) stand in the Individual Arena (map 2006, marked as an
arena) next to Tei Ken, the left NPC on the steps. Learned skills are needed to hit anybody.

- [ ] Tei Ken offers "Register for Arena of Talents"; before the event it refuses, outside the arena
      it says to register at the arena, and a character in a group is refused
- [ ] Registering opens the Arena of Talents window with "Search for registered team" and a 5:00
      clock; three characters within five levels become a team ("Found registered team")
- [ ] Cancel alone closes the window; Cancel with teammates asks whether to leave first, and the
      teammates are told who left and go back to searching
- [ ] Two full teams whose average levels lie within five are matched: both go through the "Team
      Arena" loading screen into their own copy of map 2015, on their team's bench
- [ ] The HUD shows "Arena of Talents" with Zenas 0 vs 0 Erenia, both teams' fighters (own team on
      top) and "Zenas" or "Erenia" in the chat
- [ ] After 15 seconds the first fighter of each team is put in the ring, a countdown and
      "START!!" play, the clock shows 5:00 and the current fighters' names turn gold
- [ ] Before the countdown ends, and from the bench at any time, nobody can be hit; once it ends
      the two ring fighters hit each other and the HP bars in the HUD follow
- [ ] A fighter whose HP reaches zero: "X has defeated Y." shows, they stand up with full HP on
      their bench, their heart turns grey, the score goes up and the next fighter of their team
      comes in; a fighter who disconnects loses the round the same way
- [ ] A round that runs out sends both fighters out with no point to either team
- [ ] Every fighter gets a small window at the bottom left with two call buttons (II, III) and a
      camera button; only the fighter in the ring has the call buttons lit, and the window is gone
      after the match
- [ ] The fighter in the ring double-clicks a teammate's call button: the caller flashes,
      their team's coins in the HUD lose one (grey), and the teammate is told for three seconds that they
      will be called, then stands next to the caller for 30 seconds. While called they can hit
      the opponent and be hit; afterwards, or once they fall, they go back to the bench
- [ ] A team gets five calls a match; a teammate who has fallen, is already called, or a
      fighter on the bench pressing the button calls nobody
- [ ] Not verifiable yet: spectating a match by name (`taw`), which this client never sends
- [ ] When a team has nobody left the result window shows WIN/LOSE with won rounds and lives;
      winners get 1 Gold Medal, losers 3 Silver Medals, each with a chat line
- [ ] Afterwards, right-clicking a fighter's HP bar at the top of the screen shows the match under
      "Highscore: Arena of Talents" (one more won or lost); a draw changes neither, and a fighter
      who left the match map while staying online counts one more surrendered
- [ ] Ten seconds later (or after Close) everyone is back where they registered, the HUD is gone,
      and players can be hit only where they could before
- [ ] The match runs from `instances/talent-arena.csx`: setting its preparation to 30 seconds and
      restarting the world server makes the first fighters wait 30 seconds, with the HUD clock to match

---

## Quests

### Script target steps

The opening story script marks places on the minimap ("target" steps). The client acknowledges
such a step as soon as it shows the marker, and the script used to stop there.

- [ ] Put a character on script 1 step 365 (the target after Rookie Training quest 1508) and
      enter the world: the quest window opens with Rookie Training, "Conversation with Mimi
      Mentor" (quest 1999)
- [ ] The minimap shows the marker the step points at

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

### Collect quest drops

Collect quests name a monster, the item it drops and a chance per 10 000. Put a character on
Chicken Farm Path with quest 1600 open (item 1152 from monster 2, chance 250) and a monster 2
next to them.

- [ ] Kill monster 2 often enough: item 1152 falls where it died
- [ ] Another player cannot pick that item up; the quest holder can, and the quest window moves
      to 1/1
- [ ] Once the item is in the bag, monster 2 no longer drops it
- [ ] A player without the quest never sees item 1152 drop
- [ ] Quest 1532 asks for items monsters 27 and 33 already drop for everyone: they still drop at
      their usual rate, not twice

### Quests offered in NPC dialogs

Many NPC dialogs have a button that hands out a quest (`n_run 3006 <quest>`). Put a level 90
character next to NPC 2541 with dialog 9771, whose first page offers daily quest 7516.

- [ ] Talk to the NPC and press "Calm down!": the quest's start dialog shows and "Chill Out! (Daily)"
      appears in the quest window
- [ ] Press the button again while holding the quest: nothing changes
- [ ] A dialog quest that follows another one (its quest.dat VNUM names the earlier quest) is not
      handed out until the earlier one is done
- [ ] A character below the quest's level gets the level-too-low message

### Daily quests

The quests whose title ends in "(Daily)" can be taken again once a day has passed since they were
done. Use the "Quests offered in NPC dialogs" scene with daily quest 7516 already completed.

- [ ] Done two days ago: pressing "Calm down!" hands the quest out again
- [ ] Done three hours ago: "The quest can only be completed once per day." shows and no quest is added
- [ ] A story quest done before (for example the rookie training quests 100 to 113) is never handed
      out a second time

### Story chapters from "Get Quest"

After the tutorial, each story chapter is handed out by an NPC's "Get Quest" list rather than
starting on its own. Put a level 60 character on the last step of chapter 11 next to Sandra Brake
(NPC 394, dialog 51).

- [ ] "Get Quest" opens her list of chapters 56, 57, 60 and 65, with "Level 56" highlighted
- [ ] Picking "Level 56" starts the chapter with her greeting dialogue
- [ ] Picking "Level 57" does nothing while chapter 56 is not done
- [ ] At level 50, picking "Level 56" does nothing
- [ ] Finishing a chapter's last step does not start the next chapter until it is taken from its NPC

### Quests the client knows are done

The client only hides a finished quest's offer, and only shows a hand-in button that needs a
finished quest, once the server has told it which quests are done. Put a level 90 character with
quest 6818 done and quest 6819 open next to the fishing NPC (dialog 467), and open "Fishing Side
Quest".

- [ ] The list shows "Catch the Biggest Fish" with the hand-in tick, not the blue offer mark
- [ ] Pressing it completes 6819: "Quest complete", and it leaves the quest window
- [ ] After a relog the list still treats 6818 and 6819 as done
- [ ] A daily quest done more than a day ago is offered again after a relog

### Time-space and raid wins

Give a character quest 1509 (win time-space 1) and walk into time-space 1's stone.

- [ ] Finishing the time-space with a success completes 1509 with "Quest complete" and its reward
- [ ] Leaving or failing the time-space keeps 1509 open
- [ ] With quest 6160 open, winning the Mother Cuby raid completes it for every member still in the raid
- [ ] With daily quest 7500 ("Defeat 5 enemies in the Time-Space") open, every monster killed inside a
  time-space moves its counter (x/5) and the fifth completes it; kills on an ordinary map do not count

### Quest window

A level 80 character has quests 2100 and 2101 done, and both 2102 (main line 2100) and 5006 (secondary) open.

- [ ] After entering the game the quest window lists both "Attack on the Harbour" and "Dark Revenger"
- [ ] Progress on one quest updates its own row and leaves the other one alone
- [ ] With 2102 open, Harbour Guard Tom still hands out "Level 80 - Attack on the Harbour" (a main quest no
  longer blocks other quests; only five open main or five open secondary quests do)

### Raid collect quests

A level 80 character has quest 2001 ("Give Pyjama a pillow!": 50 Chicken Feathers from Angry Chickens) open,
with an Angry Chicken (vnum 340) nearby.

- [ ] The quest window reads 0/50 after entering the game
- [ ] Killing the chicken drops one Chicken Feathers on the ground; picking it up removes it from the ground,
  the quest window reads 1/50 and the bag does not hold a feather
- [ ] After 50 feathers no more drop and the quest is ready to hand in to the Pyjama Fairy
- [ ] The count survives a relog

### Quests that only hand out their hand-in

A level 80 character with quests 5000-5005 done talks to Harbour Guard Tom (NPC 1062, dialog 100).

- [ ] Get Quest lists "Level 80 - Attack on the Harbour"; taking it shows "Quest complete!" and the quest
  window moves straight on to "Conversation with Hamid" (quest 5006), instead of a 0/0 quest that never ends
- [ ] The button is gone when the dialog is opened again, and talking to Hamid (NPC 1085) hands 5006 in

### Rewards

Put a level 26, job level 16 character with 1 000 gold on Chicken Farm Path with quest 1595 open
and an NPC of vnum 300 next to them. Its prizes: 15 000 gold, 37% of
the level 26 bar and 30% of the job level 16 bar.

- [ ] Talk to the NPC: "Quest complete" shows, then the Quest Clear window with a job
      experience, an experience and a gold icon, and "15000 Gold" under them
- [ ] The gold display goes up by 15 000 and both experience bars move
- [ ] A quest whose prize is items (for example 1575: 20 x item 1003) puts them in the bag and
      the window shows the item with its count
- [ ] A quest that pays through its script (`q_pay`) shows the same window when the script
      reaches that step, and nothing is paid twice
- [ ] With a full bag, a `q_pay` quest with an item prize says "not enough space" and stays open
- [ ] A level prize (17/18) raises a lower level and leaves a higher one alone
- [ ] A hunt quest whose prize is "gold worth N monsters" (for example 100: 5 of monster 25) pays
      between 6 and 12 gold per monster level for each of the N monsters, and the window shows that sum
- [ ] A hunt quest with an "experience as many as killed" prize (for example 31) moves the bar by
      the killed monsters' experience

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

### A character creation that fails

Needs a world server whose starter pack names an item that does not exist: copy
`configuration/`, change one `VNum` under `BasicEquipments: Adventurer` to an unused number
and start the world server with `--config <copy>`.

- [ ] Create a character: the client says "Creation failed. Please try again." and stays on
      the creation screen
- [ ] Log in again: the slot is still empty and the name can still be used

### Timed bans

A ban is a row in `PenaltyLog` for the account (`Penalty` 1, `DateEnd` in the future).

- [ ] Log in with the account: the client shows its "banned" message instead of the server list
      and closes
- [ ] Set `DateEnd` to the past: the login works again, and the row is still there

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

## Miniland minigames

- [ ] With 0 production points and some durability, open a minigame (e.g. Basic Quarry) and press
      Start Game: "You cannot be rewarded because you don't have enough production points. Start
      game anyway?" appears
- [ ] Confirm: the minigame that was opened starts; Cancel: nothing happens
- [ ] A visitor standing next to a placed minigame in someone else's miniland sees the blue arrow;
      clicking it and Open opens the minigame window, as it does for the owner
- [ ] Play a game to the end: the reward level matches the score, and choosing a reward gives one
      item; a second choice for the same game gives nothing
- [ ] Quit a game within its first ten seconds with a score: no reward level is offered
- [ ] Management window: restoring durability takes 100 gold per point; a restore past the
      object's maximum durability, or costing more gold than you hold, changes nothing
- [ ] Yield opens the gifts window with each level's box count; opening a box batch there gives
      the items, the chat line names each item with its real amount, and that level's count
      drops to 0
- [ ] A visitor can play and earn rewards but cannot open the management or gifts window of the
      owner's object
- [ ] Choosing a reward takes durability from the object: 10, 15, 20, 25 or 30 for reward
      levels 1 to 5 (check the Durability window before and after). A reward refused for lack of
      production points takes none. At 0 durability, Start Game says the object needs repairing
- [ ] A chosen reward appears in its inventory slot at once, with the "received" chat line, without
      relogging. With that pocket full, the reward arrives in the parcel box instead. Items from
      an opened box batch behave the same way

## Miniland points

Use a character that has never logged in before, or one whose points were spent yesterday.

- [ ] After logging in, the minigame window shows 2000 production points
- [ ] Each reward chosen after a minigame takes 100 points from the player who chose it, in
      their own miniland or as a visitor
- [ ] With fewer than 100 points, starting a game asks whether to play without production
      points, and choosing a reward afterwards gives nothing and leaves the points unchanged
- [ ] Spend some points, wait past midnight UTC while still logged in: within a minute the
      points are back to 2000 and the daily visit count is 0
- [ ] Spend some points, log out and back in the same day: the points are unchanged; log in
      after midnight UTC: they are back to 2000
- [ ] Visits to the miniland do not change its points
- [ ] Have a second player visit the miniland a few times, then wait past midnight UTC while the
      owner is logged in: within a minute the owner gets 2 reputation per visit of the day, with
      the "reputation increased" chat line, and the day's visit count goes back to 0. An owner who
      was offline at midnight gets the same reputation, silently, at their next login

## Not verifiable yet

Recorded so nobody spends time hunting for them:

- **Upgrading or rerolling a ring, necklace or fairy** — the client never puts one into the Item
  Upgrade window, so the server's refusal of such an `up_gr` only shows in the unit tests.
- **Blacklisting or unfriending someone who is not there** — the client only offers Blacklist
  on a visible character and Delete on a listed friend, so a `blins` for an offline character
  or an `fdel` for someone who is no friend only shows in the unit tests and a packet log.
- **Upgrading during a trade** — the client closes the upgrade window when a trade opens and does
  not let the player talk to an NPC while trading, so the server's refusal of an `up_gr` sent
  during a trade only shows in the unit tests.

- **Being muted or banned while playing** — nothing writes a `PenaltyLog` row from inside the
  game yet. A mute given by hand applies from the next line; a ban given by hand only applies at
  the next login, the player who is online is not disconnected.
- **A dead monster on arrival** — a monster waiting to respawn is no longer sent to a player
  who enters the map, but the client never drew one it received at 0 HP, so the screen is the
  same either way. The end-to-end scenario checks the packet.
- **Bleeding on a monster, by playing** — no skill of a low-level character inflicts a bleeding
  card, so the monster check was run with a database row that makes the adventurer's basic
  attack inflict Heavy Bleeding (card 42): a level 40 character's hit makes a Big Wild Pii Pod
  lose 160 every 2 seconds until 1 HP is left.
- **Poisons and regeneration on monsters, by playing** — no skill a player can learn inflicts
  one of the cards that take or give a share of HP on a timer (427, 431, 455, 1437), so the
  section above needs a database row that makes a skill inflict one (a skill BCard of type 25,
  subtype 11, 100 % chance, the card as second value). With card 455 on the adventurer's basic
  attack, a Big Wild Pii Pod (level 32, 1 210 HP, 382 MP) shows a red 96 over its head 4, 8 and
  12 seconds after one hit, and its bars lose 96 HP and 22 MP each time.
- **Resurrection Side Effects by playing** — nothing gives a character card 44 yet, so the
  check under "Debuffs that grow with the level" needs a `StaticBuff` row for the character
  (`CardId` 44, any `RemainingTime`) and a new login.
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
- **A family changed from two channels at the same moment** — two channels saving the same
  family within the same instant cannot be produced by hand. Every family write is made by the
  master server one at a time; unit tests cover joining two families at once, a full family
  and a founder who already has a family.
- **A family name with a space** — the client's name prompt refuses a space itself ("Can not use
  Space in name") and sends nothing, so the server's refusal ("Contains an invalid character.
  Choose another name.") cannot be produced from an unmodified client; the unit tests cover it.
- **Monthly family rankings** — nothing records experience or reputation per month, so the
  two monthly tabs of the family ranking stay empty on purpose.
- **Reading the family history without permission** — the server sends nothing, but an
  unmodified client may grey the tab out first, in which case the refusal cannot be reached.
- **Family experience from content** — time-spaces, raids and Instant Combat grant none yet;
  only member level-ups and achievement rewards move the bar.
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

## Mount activation

- [ ] Double-click a vehicle in the Mount inventory: the three-second bar appears,
      then the character mounts and moves at the vehicle's speed.
- [ ] Use the vehicle again while mounted: dismount, with normal walking speed restored.
- [ ] Repeat mounting and dismounting from Main inventory or an existing shortcut.
- [ ] Ride mounts from different years and check each shows its own look and speed, for example
      Magic Jaguar (5834), Turtlecopter (9149), Royal Rabbit Carriage (9443), Chi Tu (13752),
      12th Anniversary Speedy Alpaca (5807). The `cond` speed is the one in the mount's
      description.
- [ ] The limited jeeps (9090 to 9094) show the colour of their name and the limited Black
      Unicorn (9085) is a unicorn.
- [ ] A female character on the same mounts shows the female rider.
- [ ] Ride a mount whose description says "You receive the buff ...": Power Loader (1618,
      Stealth Field), 12th Anniversary Speedy Alpaca (5807, Shaggy Fur), Dasher (13468,
      Operation Christmas), Chi Tu (13752, Best Horse Ever). The buff's icon appears in the
      buff bar on mounting and goes on dismounting. A mount without such a line gives none.
- [ ] Relog and repeat: the vehicle remains usable and is not consumed.

Early or missing delay completion is covered by automated tests; a stock client
waits for its bar before sending the completion.

## Individual faction eggs

- [ ] Outside a family, use Angel's Egg (Individual), item 1624: a confirmation asks
      about changing protection and resetting points. Cancel: faction, points and egg stay.
- [ ] Confirm: protection changes to Angel and exactly one egg disappears. The faction
      display updates; in Glacernon, another character on the map sees the new faction.
- [ ] Use another Angel egg: the already-Angel message appears and the egg stays.
- [ ] Repeat with Demon's Egg (Individual), item 1625, from neutral and Angel protection;
      also switch from Demon to Angel. Contribution points reset only on a successful change.
- [ ] Relog after each successful change: the faction and remaining egg count persist.
- [ ] After a successful change, the player's info card shows 0 Act 4 kills, deaths and points.
- [ ] Join a family and try either individual egg: the client explains that the family egg
      is required, with no consumption or faction change.
- [ ] With the matching protection, try the faction ship's Corona crossing; the faction
      requirement accepts the character. Check the opposite faction's route remains refused.

Family eggs (1626/1627) are not implemented by this feature.

## Act 4 faction war

Two characters, an angel and a demon (faction eggs 1624/1625), with `$Teleport` into Glacernon,
for example map 134. The war is per channel and starts at 0% on every server start.

- [ ] Entering any Glacernon map sends `fc` with both bars at 0% and the minutes left until
      the monthly reset
- [ ] In Glacernon each player sees the other with the angel or demon emblem; on a normal map
      neither shows one
- [ ] The angel attacks the demon in Glacernon and the hit lands; two angels cannot target each
      other, nor can a neutral character hit either side
- [ ] On a normal map the angel cannot hit the demon
- [ ] The angel kills the demon: every player in Glacernon gets "<angel> has defeated <demon>"
      in chat, green for angels and red for demons, and the angel bar of the HUD moves to 1%
- [ ] The demon loses no dignity for that death; the death dialog's save-point answer sends
      the demon to the demon citadel (map 131); an angel who falls is sent to map 130
- [ ] Right-click the angel: the info card shows one Act 4 kill and two points; the demon's card
      shows one death. A victim more than ten levels away gives the killer no points
- [ ] A fall in Glacernon to a monster costs dignity as anywhere else

The faction HUD bar does not draw: the client applies `fc` to a window it only opens on its
own, so check the bar values in the packet log instead of on screen.

### Lord Mukraju and the Glacernon raids

A side needs 100 kills to fill its bar. Both characters need a family and level 60 or more.

- [ ] The angels' 100th kill: everyone in Glacernon reads "Lord Mukraju has appeared in the
      Angel Camp", the angel bar goes back to 0% and `fc` shows the angel side in mode 1 with
      300 seconds; Lord Mukraju (556) stands in the angel camp (map 132, 114/92)
- [ ] Kills made while Mukraju is out do not move the angel bar
- [ ] Left alone for five minutes, Mukraju disappears and the angel side is back in mode 0
- [ ] Killing Mukraju: `fc` shows the angel side in mode 3 with 3600 seconds and one raid flag
      set; on map 134 an angel sees a raid gate at 139/99, a demon does not
- [ ] An angel without a family pressing the gate is told they are not authorised; one below
      level 60 is told the entry level
- [ ] Entering puts the angel in the raid's first map with `dg <raid> 1 <seconds> 0`; a second
      family member arrives in the same run, a member of another family in a run of their own
- [ ] Half an hour after Mukraju died, a portal to the boss room opens on the first map; from
      then on "The enemies are getting reinforcements!" comes with six more guards every 90 s
- [ ] A player who falls in the raid stands up again where they fell 20 seconds later
- [ ] Killing the boss: "Congratulations! You defeated the boss!", gold, chicken, cellon and
      potions are thrown around the boss, and everyone inside gets 10000 reputation and the
      raid's box (882, 185, 942 or 999), then is sent back to map 134 at 139/100
- [ ] The family cannot go back in until the next raid opens
- [ ] After the hour the gate goes grey, players still inside are sent back, and the angel
      side is back in mode 0

- [ ] The raid box arrives with a rarity between -2 and 6; opened, it gives one of its listed items
      (Hatus 185: a shell, Magic Eraser, 5 Rainbow Pearls, Hatus' Dark Lance, Abyss Bow, Infernal
      Staff, Small Obsidian of Completion, Turik's Essence, the spell gun or the Shadowheart card),
      and a weapon or armour from it carries the box's rarity

## Act 4 channel and the ship to Glacernon

Start a second world as the Act 4 channel (`ACT4_CHANNEL=true WORLD_PORT=1338`) next to the
ordinary one. Use an angel with at least 50 000 gold in Port Alveus (map 145), beside Leika
Passage (48,41). Boarding opens at :05 and :35, closes at :25 and :55, and the ship sails at :00
and :30; `$Event Act4Ship` opens boarding at once.

- [ ] The login channel list shows only the ordinary channel
- [ ] Outside the boarding window, "Board Ship" > "Yes": "The ship hasn't returned yet."
- [ ] Boarding just before :25 or :55 puts the player aboard; just after, "The ship hasn't returned
      yet."
- [ ] A neutral character: "Only those who have the Power of Protection can enter Glacernon."
- [ ] Less than 50 000 gold: "You don't have enough gold" in chat, and the player stays ashore
- [ ] During boarding: the angel is put on the ship (map 148) with Leika Passage and Fabian Frost
      aboard; a demon boarding at the same time is on a deck of their own
- [ ] Leika Passage aboard > "Leave Ship": back in Port Alveus where the player boarded
- [ ] Four minutes before sailing, then one minute, 30 and 10 seconds: "The ship will leave in ..."
      in the middle of the screen; at departure "Okay, we're setting off! Raise the anchor!"
- [ ] Ten seconds later 50 000 gold are taken, the client shows the crossing and reconnects to
      the Act 4 channel in the angel citadel (map 130); a demon lands in map 131
- [ ] A passenger who spent their gold while waiting is put back in Port Alveus instead
- [ ] On the Act 4 channel, the Leika Passage in the citadel > "Return to Port Alveus": the
      client reconnects to the ordinary channel in Port Alveus
- [ ] Log out in Glacernon and log in on the ordinary channel: the client moves straight to the
      Act 4 channel, on the map the character left
- [ ] Without an Act 4 channel running, the ship lands in the citadel on the same channel
