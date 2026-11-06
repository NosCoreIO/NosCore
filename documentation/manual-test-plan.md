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

- [ ] Naked, open the stat window and note min/max attack and the three defences: they are
      the level and class values, not zeros
- [ ] `$CreateItem` a weapon and an armour, equip both
- [ ] The stat window changes as soon as each piece goes on, and again when it comes off,
      and the numbers include the piece's own values
- [ ] Level up with the window open: the base attack and defences go up
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

### Equipment upgrade

- [ ] Put a +2 weapon of rarity 8 in the bag with Cella Powder (1014) and Soul Gems (1015),
      open Teoman's Use Shop, pick the upgrade tab and drag the weapon in: the window shows
      90 % success, 10 % fixed, 0 % fail, 3,000 gold, 80 Cella Powder and 2 gems
- [ ] Press Upgrade and confirm: the item is never destroyed, and exactly 3,000 gold is taken
- [ ] A +7 weapon of rarity 0 and one of rarity 8 show the same 5 % / 3 % / 92 % and the
      same 400,000 gold, 380 Cella Powder and 2 full gems

### Soul Revival Stone

- [ ] With a specialist card whose soul was destroyed in the bag, double-click a Soul Revival
      Stone (1365 or 9039) and click the card: the client asks "Would you like to use Soul
      Revival Stone?"
- [ ] Cancel: nothing changes. Confirm: "The soul finds a new lease of life." appears, one
      stone is gone, the SP bar fills by 10 000 points and the card can be worn again
- [ ] Drop the stone on a card whose soul is intact, or on any other item: nothing happens and
      the stone stays

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

### Parcels and private shops

- [ ] `$Gift 1012 5 0 0 <your name>`: chat shows "Gift has been delivered." and, once,
      "Parcel received >> Seed of Power x 5" in the client's language; the parcel icon lights up
- [ ] `$SetReputation` above the current value shows "Reputation increased: +N" in green, below it
      "Reputation reduced: -N" in red, with N the difference; the same value shows nothing
- [ ] Give a player `$SetGold` up to the gold cap and have them open a private shop; buying from
      it shows the client's "Trade failed because shop owner has too much money." and no gold moves

### NosBazaar search

- [ ] List a weapon, an armour, a hat, a ring, a potion and a snack from one character, then
      open the NosBazaar on another
- [ ] Each category tab (Weapon, Armour, Equipment, Jewellery, Main, Usable) shows only its items
- [ ] Weapon and armour: the class drop-down, the level band (1-10 ... 81-90, Heroic), rarity and
      upgrade each narrow the list; Equipment and Jewellery: the slot drop-down picks the slot
- [ ] Searching an item by name shows only that item; searching with no name never empties the window
- [ ] With more than 50 listings, page 2 continues the price order of page 1 instead of repeating it

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

### Miniland invite

Two accounts on the same channel. The owner holds a Miniland Bell and a Seed of Power.

- [ ] Outside the miniland, `$Invite <name>` says invitations only work in the miniland
- [ ] Inside an open miniland, `$Invite <name>` uses one Seed of Power and asks the other
      player whether to visit; Confirm puts them in the owner's miniland and the visitor
      counter goes up
- [ ] Cancel leaves them where they are, and a later Confirm on a stale dialog does nothing
- [ ] With the miniland locked, without a seed, to someone offline, or to someone blocking
      Miniland invites in their options: each gives its own message and no seed is used

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
- **Options with nothing behind them yet** — family invites, pet and partner auto-revive,
  group sharing, hat visibility and interface lock are stored and come back on login, but
  no family invite, revive or sharing flow reads them yet.
- **HP display, buff display, quick get-up and mouse aim lock** — the client applies these
  itself; the server only stores them and sends them back on login, so the relog check is
  all there is to see.
- **Family commands on a non-English client** — each client language types its own word
  for the family commands (`%Invitationdefamille`, `%Familieneinladung`, `%Caccia` and so
  on). The cloud client is English only, so only the unit tests cover the other words.
- **Cellon options in the item tooltip and on the character** — options are stored, but the
  server sends no `e_info` for accessories and nothing applies the options to stats yet.
- **Food and snacks in a miniland** — minilands are created after start-up and never get a
  life loop, so nothing that ticks per map (regeneration, digestion) runs there yet.
