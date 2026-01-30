# 📜 LORE BIBLE: Biosphere Management Group

> **DEVELOPER DOCUMENT — NOT PLAYER-FACING**
> 
> This document contains the complete lore of Hazard Pay. **None of this is delivered directly to players.** The story exists as environmental details, hidden collectibles, cryptic hints, and pieced-together fragments that reward curiosity without punishing players who skip it.

---

## 🎯 LORE DELIVERY PHILOSOPHY

### The Golden Rule: Show, Don't Tell (And Sometimes Don't Even Show)

**Players who want pure gameplay get pure gameplay.** No cutscenes. No dialogue boxes. No AI companion narrating their performance. A player can complete the entire game, unlock every character, beat every boss, and never encounter a single piece of explicit lore if they're not looking.

**Players who want story dig for it.** And when they dig, they find fragments. Contradictions. Redacted documents. Environmental details that don't quite add up. The kind of stuff that makes people pause, screenshot, and post "wait, did anyone else notice..." on Reddit.

### The Iceberg Model

**SURFACE (Everyone sees, no one thinks about):**
- You work for a company called BMG
- There are bugs, you kill them
- The Van, the equipment, the job

**SHALLOW WATER (Casual observers notice):**
- BMG logos on everything—equipment, vending machines, the bugs' containment tags
- Weird corporate slogans on posters
- Room names that hint at what happened ("Former Daycare," "Honeymoon Suite")

**DEEP WATER (Active searchers find):**
- Hidden documents (collectible scraps)
- Environmental storytelling (baby shoes in a hive room, wedding photos covered in web)
- Equipment serial numbers that date to before "the incident"
- Contractor ID numbers that skip sequences (what happened to Contractor #0001-#0446?)

**THE ABYSS (Dedicated investigators piece together):**
- Timeline contradictions in found documents
- The true nature of the "incident"
- What Division 8 actually is
- Why the bugs exhibit certain behaviors
- What happened to Facility Omega
- Who (or what) is actually running BMG

### What Players Experience vs. What They Can Discover

| Players See | Players Can Discover (If Looking) |
|-------------|-----------------------------------|
| Room name: "Sector 7-RG" | Room name: "Former Daycare - Room 12" — wait, what? |
| A vending machine sprite | Upgrade called "VitaJuice" — same name on the health pickup |
| Contractor ID in UI: #0447 | Achievement: "First Day" — why does numbering start at 447? |
| Fire-spitting roaches | Bestiary entry mentions "[REDACTED] exposure" |
| Equipment that sparks (mechanic) | Upgrade description: "Warranty void. Prototype only." |
| Death screen | "CONTRACTOR #0447 [UNRECOVERED]" — that word choice... |

### Practical Lore Delivery (2D Pixel Art Constraints)

**What We CAN Do:**
- **Room Names:** Each room has a title. Most are just coordinates. Some aren't. "Former Nursery." "Employee Break Room." "DO NOT ENTER."
- **Collectible Text Pickups:** Glowing pixel item → opens text popup with document fragment
- **Bestiary/Codex Entries:** Unlocked by killing enemies. Written in corporate voice. Hints buried in fine print.
- **Upgrade/Item Names & Descriptions:** Every upgrade has flavor text. Most is corporate fluff. Some is... odd.
- **Achievement Names:** Cryptic titles that hint at lore. "Employee #447." "What Happened to Gary?" "The Thorne Protocol."
- **Death Screen Variations:** Different text each death. Mostly corporate. Occasionally glitches.
- **Loading Screen Text:** Brief "tips" that are actually lore breadcrumbs.
- **Character Select Flavor:** One-line descriptions that raise questions, don't answer them.

**What We CAN'T Do (and shouldn't try):**
- Detailed environmental props (no readable newspapers, photos, etc.)
- Physical Van exploration (it's a menu)
- Serial numbers or fine print on sprites
- Audio logs or voice acting (probably)
- Cutscenes or cinematics

### Mystery Hooks (Unanswered Questions for Theorists)

These are intentionally never fully explained. Players piece together theories from text fragments:

1. **The Contractor Numbers** — Your ID is #0447. Achievements reference earlier numbers. No explanation.

2. **The Verdantix Founder** — "Gary" mentioned in document fragments. His warnings always cut off.

3. **Facility Omega** — Referenced in bestiary entries. Never a playable zone (or secret unlock?).

4. **The V-7 Variations** — Bestiary hints bugs are being *updated*, not just evolving.

5. **Dr. Thorne's Notes** — If playing as Doc, some upgrade descriptions are written differently.

6. **The Interface Glitches** — Rare: death screen shows wrong number. Menu text changes briefly.

7. **BMG's True Business** — Document fragments suggest BMG *wanted* this. "Market creation."

---

## 🎮 LORE INTEGRATION: Practical Implementation

*What actually works in a 2D pixel art roguelike.*

### Room Names (Primary Subtle Method)

Most rooms are just coordinates: "Sector 7-RG-04"

But occasionally:
- "Former Residence - Kitchen"
- "Nursery (Evacuated)"
- "Employee Housing Block C"
- "RESTRICTED - DO NOT ENTER"
- "Safe Room" (it's not)

Players who pay attention notice the names tell a story. Players who don't just see level numbers.

### Collectible Documents (Text Pickups)

**Format:** Small glowing pixel item. Picking it up opens a text popup. That's it.

**Placement:** Hidden in optional paths or behind destructible walls. Rewards exploration.

**Content Rules:**
- Always partial/damaged (text cuts off, sections [REDACTED])
- Dates, names, numbers that conflict with other documents
- Written in corporate/scientific voice
- Short enough to read in 10 seconds
- Contain dates, names, numbers that conflict with other documents
- Reference events, people, or places never fully explained
- Written in authentic corporate/scientific voice

**Example Document Fragment:**
```
[TORN — TOP MISSING]
...recommend immediate termination of Phase 1 workforce.
Liability exposure unacceptable. Suggest "contractor 
transition program" per Legal's proposal.

Note: Ensure no personnel files survive transition.
Employee numbering to restart at 0447 per attached...
[TORN — BOTTOM MISSING]
```

### Character Background Integration

**Each playable character has hidden lore tied to them, delivered through text-based systems:**

| Character | Surface Story | Hidden Lore (Delivery Method) |
|-----------|---------------|-------------------------------|
| Torch | BBQ guy who likes fire | Unlock achievement: "Third Degree." Description hints at casualties. Bestiary notes mention burn patterns "consistent with prior civilian incidents." |
| Queen B | Beekeeper with weird bees | Collectible text: "Customer Review #7" - describes symptoms. Death screen variant: "ALLERGIC REACTION. FAMILIAR." |
| Mop | Retired janitor | Room name in school zone: "Clearwater High - PENSION DISPUTE SITE." Upgrade "Old Keys" mentions "administrative access that should've been revoked." |
| Heavy | Debt-crushed intern | Character select text cycles through contract fine print snippets. Loading tip: "Heavy's signing bonus was $200. Their equipment debt is $47,000." |
| Sparky | Blacklisted electrician | Achievements track "OSHA violations witnessed." Bestiary entry on electrical enemies: "Wiring matches Sparky's complaint reports." |
| Doc | Scientist who signed off | Collectible texts are her original unredacted reports. Bestiary entries have [REDACTED] sections only she can "read" (unlock with Doc). |

### UI/Interface Lore (Minimal but Present)

**The Van Terminal:**
- Displays contractor ID number (players notice theirs is #0447+)
- Occasionally glitches—old text, old names, corrupted data
- Loading screens have BMG slogans that change over time (get darker as game progresses?)

**Upgrade Descriptions:**
- Written in sterile corporate language
- Occasional references to "previous iterations" or "field testing casualties"
- Never explain WHY the upgrade works or where it came from

**Death Screen:**
- Simple. No narrative. Just: "CONTRACT TERMINATED. CONTRACTOR [#XXXX] UNRECOVERED."
- Optional: Counter showing total "UNRECOVERED" contractors (starts at 446, increments globally/locally?)

---

## 🕳️ THE FULL LORE (Developer Reference Only)

*Everything below is the "truth" that players piece together—or theorize about without ever confirming.*

---

## 🏢 The World: What BMG Actually Is

The year is 2026. America runs on three things: coffee, crippling debt, and **Biosphere Management Group (BMG)**—a pharmaceutical conglomerate so deeply embedded in daily life that most people don't notice it anymore, the same way fish don't notice water and wage slaves don't notice fluorescent lighting.

BMG doesn't just dominate markets. It *is* the market. The aspirin in your cabinet? BMG. The fertilizer on your lawn? BMG subsidiary. The cholesterol medication keeping your father alive? BMG, and they raised the price 340% last quarter because, quote, "shareholder value." The school lunch program? BMG Nutritional Solutions ("Growing Minds, Growing Margins™"). The water treatment plant? Contracted to BMG Municipal Services, which explains why everything tastes vaguely like liability waivers.

Their logo—a stylized double helix wrapped around a dollar sign—appears on hospital gurneys, park benches, highway exit signs, and the coffee cups of senators. There are entire ZIP codes where BMG owns the police contract, the waste management franchise, AND the local news affiliate. Regulatory capture isn't a bug; it's the business model.

**Sector Classification:** BMG carves its territory into numbered **Sectors**—geographic fiefdoms spanning city blocks, suburbs, and rural "resource zones." Within each Sector, **Deployment Zones** are designated by letter and function: R for Residential, I for Industrial, M for Municipal, X for "don't ask." Your work orders arrive as coordinate strings that translate human tragedy into corporate shorthand. "Sector 7-RG" means someone's basement has achieved sentience. "Sector 4-XQ" means you won't be coming back.

**The Public Face:** BMG sponsors little league teams, funds hospital wings (named after executives), and runs heartwarming commercials featuring golden retrievers and diverse families smiling at salads. Their PR department won seven Clio Awards last year. Their legal department has a higher body count than most militaries.

**The Shadow Infrastructure:** Behind the charity galas and the glossy annual reports, there's a second corporation—one that handles "reputation management" through methods that would make the CIA blush. Cleanup crews in unmarked vans. Research facilities that don't appear on any map. A dedicated "Narrative Adjustment" team that can make a three-alarm industrial disaster read as "minor equipment malfunction" by the evening news.

**Everyday Life in the BMG Economy:** Citizens don't buy products anymore; they subscribe to "wellness ecosystems." Health clinics push company-formulated treatments with names like "VitaBoost+" and "CardioGuard Premium" (side effects include: compliance). Vending machines dispense "Productivity Enhancers" that are definitely not just rebranded amphetamines. The fine print on a bag of chips now runs six pages.

Meanwhile, a grey economy thrives in the margins—contractor forums trading intel over encrypted channels, black-market stabilizer rations, gear salvaged from colleagues who "didn't make it to orientation." For most citizens, the Crisis is a weird headline between celebrity gossip and sports scores. For contractors, it's Tuesday. It's always Tuesday.

### 🧬 The "Incident" (Internal Classification: VERDANT COLLAPSE)

It started, as most apocalypses do, with good intentions and a quarterly earnings call.

**Verdantix** (né "Hyper-Gro Labs," né "Gary's Fertilizer Shack") was a scrappy agrochemical startup operating out of a converted strip mall in Kansas. Their pitch was simple: end world hunger with a revolutionary growth accelerant called **Compound V-7**. Crops matured in weeks instead of months. Yields tripled. The stock price did whatever stock prices do when you promise to solve famine (it went up, a lot).

BMG acquired Verdantix for $4.2 billion in September 2023. Within six weeks, they had fired the safety team, doubled production quotas, and replaced the "organic compound stabilizers" with something cheaper that the internal memos called "Good Enough Substitute #7." The original formula required a 90-day cultivation cycle. BMG's version shipped in 12.

Here's the thing about biochemistry: it doesn't care about shareholder value.

**What Went Wrong (Everything):**

Compound V-7 was designed to accelerate cellular growth in plants. It did this by hijacking mitochondrial function and overclocking metabolic pathways. The original Verdantix team had spent three years calibrating stabilizers to ensure the compound ONLY affected plant cells. BMG's cost-cutting removed those stabilizers. 

The reformulated compound didn't discriminate. It integrated with anything that had mitochondria—which, as any high school biology student could tell you, is basically everything.

The first mutations were almost cute: slightly larger aphids, unusually aggressive garden snails, a tomato plant that ate a sparrow. Farmers reported "frisky" livestock. Pest control calls spiked 400%. No one connected the dots because the dots were owned by different BMG subsidiaries that didn't share data.

Then the compound hit the aquifers.

**The Elemental Problem:**

V-7 doesn't just cause growth—it causes *adaptation*. The mutagen rewrites host biochemistry based on environmental chemical exposure, creating what the surviving Verdantix researchers called "phenotypic cascade events" and what everyone else calls "oh god why is that cockroach on fire."

- Roaches in grease-saturated restaurant kitchens developed **incendiary glands**—their bodies producing trace amounts of phosphorus compounds that ignite on contact with air
- Spiders in industrial zones secreted **acidic webbing** after absorbing heavy metals from contaminated soil
- Wasps near power infrastructure evolved **bioelectric organs** similar to electric eels, but somehow worse
- Centipedes in freezer units developed **cryogenic circulatory systems** that radiate lethal cold

The creatures aren't magical. They're just regular bugs that ate the wrong fertilizer and became biology's revenge for every environmental shortcut humanity ever took.

**The Containment Failures:**

BMG's initial response was, charitably, "catastrophically stupid."

*Phase 1: Denial (March-July 2024)*
Legal blamed climate change. PR blamed immigrants. The CEO blamed short-sellers. Three internal whistleblowers were "promoted to external opportunities" (fired and NDAs enforced).

*Phase 2: Incineration (August 2024)*
Someone in Operations had the bright idea to burn the affected zones. This would have worked if V-7 wasn't heat-stable and aerosolized during combustion. The smoke carried mutagenic particulate across seventeen counties. Bug populations quintupled.

*Phase 3: Worse Chemicals (September-November 2024)*
BMG deployed experimental pesticides that killed 90% of exposed insects. The surviving 10% developed resistance AND passed it to offspring within a single generation cycle. Some developed immunity to fire.

*Phase 4: Acceptance (December 2024)*
Someone finally did the math: it was cheaper to create a deniable extermination workforce than to fix the problem or admit fault. Division 8 was born.

**The Cover Story:**

The public explanation, distributed through BMG-friendly media outlets, is that the infestation represents a "novel invasive species event" caused by—and this is real—"illegal exotic pet dumping exacerbated by climate migration patterns." 

There's a dedicated team of "scientific consultants" (actors with lab coats) who appear on morning shows to explain that the giant fire-spitting roaches are "definitely not related to any agricultural products" and that concerned citizens should "trust the process and report unusual sightings to their local BMG Community Safety Liaison."

Most people believe it. People will believe almost anything if the alternative is admitting the salad they had for lunch might have been grown in apocalypse juice.

### 📅 Historical Timeline: The Road to Ruin
*(Compiled from leaked internal documents, whistleblower testimony, and one very drunk BMG executive at an airport bar)*

---

**September 12, 2023 — THE ACQUISITION**
BMG acquires Verdantix for $4.2 billion.
- *Press Release:* "Ushering in a new era of sustainable abundance for generations to come."
- *Internal Memo (leaked):* "Asset strip the safety division. Fire anyone with a conscience. Double production quotas. The stockholders are hungry and they don't eat vegetables."
- *Verdantix Founder's Resignation Letter:* "I hope you all burn. Sincerely, Gary."

---

**October 2023 — THE OPTIMIZATION**
New BMG management implements "efficiency improvements."
- Safety team reduced from 47 to 3 (one quit, one was fired for "negativity," one is now in Division 8)
- Quality control testing window reduced from 90 days to "whatever fits in a weekend"
- Stabilizer compound replaced with discount alternative sourced from a subsidiary that also makes industrial floor cleaner
- Internal codename for new formula: "YOLO-7"

---

**March 15, 2024 — COMMERCIAL ROLLOUT**
"Verdant-X" (consumer-branded V-7) ships to Sector 4 agricultural belt.
- Crop yields increase 200%
- Stock price jumps 45%
- Farmer testimonial: "It's like my corn is angry about growing. In a good way?"
- *Unreported:* 340 cattle in the region gain 15% body mass in two weeks. Three attack their owners.

---

**June 2024 — FIRST SIGNS**
Field reports begin trickling in. All are marked "LOW PRIORITY" by automated systems.
- Pest control calls up 400% in treated zones
- "Unusual insect aggression" noted in 17 counties
- One exterminator describes a spider "the size of a dinner plate." He is dismissed as "hysterical."
- Local news runs a puff piece: "Super Bugs? Or Super Hoax?" Spoiler: not a hoax.

---

**August 2, 2024 — THE BLACK FRIDAY INCIDENT**
A single mutated queen roach breaches the Sector 4 Distribution Center.
- 14 employees lost (11 confirmed dead, 3 listed as "unrecovered")
- The queen is described in the incident report as "approximately the size of a German Shepherd, with thermal-reactive mandibles"
- She is eventually killed by a forklift driver named Eduardo who refuses to give interviews
- *Official Press Release:* "Gas leak explosion claims lives. BMG extends thoughts and prayers."
- *Insurance Payout:* $0. Fine print excluded "Acts of Arthropod."

---

**August-September 2024 — OPERATION CLEAN SWEEP**
Panicking executives authorize "controlled incineration" of affected agricultural zones.
- 200,000 acres burned
- Mutagenic particulate becomes airborne
- Compound spreads to 17 additional counties
- *Classified Internal Assessment:* "We have made it worse. Significantly worse. Recommend prayer."
- The 10% of insects that survive the burns develop heat resistance. Some develop heat *preference.*

---

**October 2024 — THE CHEMICAL ESCALATION**
BMG deploys experimental pesticide "NecroBane-X" (internal classification: "Bug Juice 2: Bug Harder")
- Kills 90% of exposed insects within 72 hours
- Surviving 10% display "aggressive evolutionary response"
- New mutations observed: armor plating, acidic secretions, rudimentary pack tactics
- One field team reports a centipede "coordinating" with other centipedes. They do not elaborate because they did not survive.

---

**November 11, 2024 — THE THANKSGIVING MASSACRE**
Coordinated infestation events across six major metropolitan areas.
- Casualties: 847 confirmed, 2,000+ "displaced"
- BMG stock drops 12% (rebounds within a week after PR offensive)
- *Cover Story Deployed:* "Invasive species from Southeast Asia, exacerbated by climate patterns."
- Actual Southeast Asian entomologists: "Those aren't ours. Please stop."

---

**December 2024 — THE PIVOT**
Emergency board meeting concludes that:
1. Fixing the problem would cost approximately $340 billion
2. Admitting fault would cost "everything, forever"
3. Creating a deniable contractor workforce would cost "basically nothing, they're desperate"

Division 8 is chartered. Recruitment begins immediately, targeting:
- The indebted
- The desperate  
- The criminal
- The "unassignably weird"

---

**January 2026 — CURRENT DAY**
Division 8 operates across 23 metropolitan sectors.
- Contractor mortality rate: 67% (first 30 days)
- Contractor retention rate: "Irrelevant per Legal"
- Public awareness: "Managed"
- The infestation is growing 3% weekly.
- Stock price is up 8% YTD.

*This document is classified LEVEL 7-CRIMSON. Unauthorized distribution will result in termination. Of employment. And possibly other things. Don't test us.*

---

### 🎭 Tone & Atmosphere (For Environmental Design)

The game's tone is communicated through **environment, not exposition**. Players should *feel* the corporate dystopia without being told about it.

**Visual Tone:**
- Industrial grime, not sleek sci-fi
- Duct-taped equipment, peeling logos, flickering lights
- Juxtaposition: cheerful corporate posters in blood-splattered rooms
- The mundane made horrific: suburban homes, office cubicles, daycare centers—all infested

**Audio Tone:**
- Minimal music during gameplay (tension, not drama)
- Environmental sounds: distant screams? Radio static? Or just wind?
- Corporate jingles on vending machines, played straight
- No narrator. No companion AI chattering. Silence lets players fill in the horror.

**The Unspoken Dread:**
Players should occasionally think "wait, what happened here?" without the game answering. A child's backpack in a hive room. An uneaten birthday cake. A wedding dress covered in web. Show, never tell.

---

## 👤 Key Figures (Developer Reference)

*These characters are referenced in documents and environmental details. Players piece together who they are—or don't.*

---

### Director Sterling Voss III (CEO)

**What Players Might Find:**
- His face on motivational posters (always the same photo, suspiciously airbrushed)
- His signature on liability waivers (always identical—printed, not signed)
- News clippings praising "visionary leadership"
- Conspiracy forum posts (in-game collectibles?) questioning if he's real

**The Mystery:** Players may find documents suggesting Voss hasn't been seen in person since 2019. His "appearances" are always pre-recorded. His address is a PO Box. Some documents reference "Voss Protocol" and "Voss Directive" as if he's a *policy*, not a person. Let players theorize.

**Never Confirm:** Is he real? AI-generated? Dead? A committee? A legal fiction? Don't answer.

---

### Dr. Aris Thorne (Chief Research Officer)

**What Players Might Find:**
- Research notes in clinical handwriting (collectible documents)
- References to "Facility Omega" and "Phase 2 trials"
- If playing as Dr. Chen: occasional equipment with handwritten sticky notes. Recent dates. Watching.
- Photos in a lab coat—but his face is always obscured or turned away

**The Mystery:** Thorne is clearly *involved* and clearly *watching*, but players never see him directly. His notes suggest he views the infestation as an experiment and contractors as test subjects. But some notes also suggest... guilt? Regret? Or is that just what he wants readers to think?

**Hidden Connection:** Text-based hints should suggest Thorne and Chen have history. Collectible documents reference "Subject C" with unusual detail. Achievement "Old Colleagues" unlocks when playing as Chen in Thorne's former lab zones. Let players connect the dots.

---

### Commander Stone (Division 8 Operations)

**What Players Might Find (Text-Based):**
- Collectible: Military discharge papers (redacted, but "dishonorable" is visible)
- Collectible: Voice logs from other contractors mentioning "the Commander"
- Document: Mission briefing forms with his signature—getting shakier over time?
- Loading tip: "Commander Stone's prescription refills are auto-approved."

**The Mystery:** Stone is the closest thing to a "handler" but players only see evidence of him, never him directly. Is he protecting contractors? Using them? Both? Text logs suggest he's not heartless—just hollowed out. Some logs cut off mid-sentence. What was he about to say?

---

### A.M.I. (Asset Management Intelligence)

**What Players Experience:**
- The Van's interface. Upgrade terminal. Simple text prompts.
- Sterile, corporate language. No personality. Just function.
- Occasionally... glitches. Text that doesn't belong. Names. Dates. Apologies.

**What Players Might Find:**
- Documents referencing "A.M.I. Version 1.0" through "Version 7.2"—what happened to the other versions?
- Error logs showing A.M.I. trying to access files it shouldn't
- A single corrupted audio file: a human voice saying "I'm sorry, I didn't want—" before cutting to static
- Code comments (if players somehow see them): "// TODO: remove personality module"

**The Mystery:** Is A.M.I. just software? Or was it something else, once? Some documents reference a "human baseline" for the AI's responses. Whose baseline? Why "Asset Management"—is it managing *assets*, or is it *itself* an asset that was managed?

**Implementation Note:** A.M.I. should be *functional*, not chatty. Brief text prompts. "CONTRACT ACCEPTED." "UPGRADE INSTALLED." "CONTRACTOR [UNRECOVERED]." Players who ignore it miss nothing. Players who pay attention notice the cracks.

---

### "Legal" (The Department)

**What Players Experience:**
- Waivers. Everywhere. Fine print on equipment. Disclaimers on health packs.
- Liability language that's almost funny—until you read it twice.

**What Players Might Find:**
- Internal memos from Legal that are MORE horrifying than anything else
- References to "acceptable casualty thresholds" and "narrative adjustment protocols"
- A document that almost explains what Division 8 really is—but key paragraphs are blacked out
- Legal's address is listed as "Sub-Basement 7." No BMG building has a Sub-Basement 7 on any floor plan.

**The Mystery:** Legal is omnipresent but invisible. They approved the contractor program. They drafted the waivers. They ensure no one can sue. But some documents suggest Legal isn't just covering up the problem—they *anticipated* it. Memos dated before the incident reference "post-event workforce solutions." How did they know?

---

## 👷 The Contractors (Playable Characters)

*Each character's full backstory is developer reference. Players learn fragments through text-based systems: collectible documents, bestiary entries, upgrade descriptions, achievements, and room names.*

### Design Philosophy: Silence as Character

**Players choose a character. The game doesn't explain why they're here.**

No opening cutscene. No tragic monologue. Players select "The Pyrotechnic" or "The Custodian" and start playing. The backstory exists—it's just hidden:

- Character select screen has rotating flavor text with subtle hints
- Collectible text documents that reference them by name
- Achievements/unlocks with cryptic descriptions hinting at their past
- Death screen variants that sometimes reference their backstory
- Certain room names only appear when playing specific characters

**The mystery is the point.** Players should finish runs still wondering "wait, why IS the janitor here?" Then find a collectible three runs later that mentions Clearwater High's pension scandal. Then find a budget memo that names specific employees. Then realize...

---

### 1. The Pyrotechnic ("Torch")

**In-Game:** A guy with a flamethrower. No explanation given.

**Player-Facing Details:**
- Weapon leaks fuel (gameplay mechanic)
- BBQ-themed flavor text on character select
- Achievement "Third Degree" unlocks after first fire-kill milestone

**Hidden Lore (Text Collectibles):**
- Document: "Regional BBQ Finals Ends in Disaster: Three Injured"
- Document fragment: "State of Texas vs. Ramirez"
- Loading tip (Torch only): "Torch hasn't touched a grill since the incident."
- Achievement description for "Flame Warden": "His warrant—stamped RESOLVED with BMG's signature."

**Full Backstory (Developer Reference):**
Marcus Ramirez was a three-time Texas state BBQ champion. His signature "Dragon's Breath Finish" involved an open butane torch. At the 2024 Regional Finals, a propane tank malfunction caused an explosion. Three judges hospitalized. One pavilion destroyed. One career ended.

BMG's recruiter found him in county lockup. The offer: charges dropped in exchange for "extended field service." The fuel tank they issued him leaks constantly. The company considers this a feature.

**Mechanic:** Fire spreads infinitely. Standing still ignites the fuel beneath him. Must keep moving.

---

### 2. The Apiarist ("Queen B")

**In-Game:** A woman with a swarm of mutant bees. No explanation given.

**Player-Facing Details:**
- Bees defend her (gameplay mechanic)
- Character select text: "Former artisanal beekeeper. Former."
- Achievement "Hive Mind" description references FDA shutdown

**Hidden Lore (Text Collectibles):**
- Document: Etsy review - "Love the honey!" "Unique tingly sensation!" "My husband is in the hospital?"
- Document: FDA shutdown notice for "Beatrix's Brooklyn Bees"
- Bestiary entry on Queen B's bees: "Incident Report 7G-Bravo: Extermination team [UNRECOVERED]. Subject found making tea."
- Loading tip: "Lab analysis showed V-7 compounds in Queen B's honey samples."

**Full Backstory:**
Beatrix Vance sold artisanal honey from bees foraging near a Verdantix test site. The honey contained V-7. Customers were hospitalized. Her bees mutated, grew to fist-size, and became lethally protective of her.

When exterminators came to destroy the hive, they didn't come back. BMG offered her a job instead of a cell. The bees are the only family she has left.

**Mechanic:** Swarm grows with kills. Damage kills bees instead of HP. Lose the swarm, lose your shield.

---

### 3. The Custodian ("Mop")

**In-Game:** An older woman with an industrial mop-weapon. No explanation given.

**Player-Facing Details:**
- Precise, professional demeanor
- Weapon is clearly improvised from cleaning supplies
- Character select text: "22 years at Clearwater High. 2 years from pension."

**Hidden Lore (Text Collectibles):**
- Document: Clearwater High newsletter - "Budget Cuts Announced"
- Document: Board meeting notes mentioning "affected staff"
- Achievement "Restructured" description: "Pension status: RESTRUCTURED"
- Room name (school zones): "Clearwater High - Budget Meeting Room"

**Full Backstory:**
Eve Ortega was head custodian at Clearwater High for 22 years. Two years from retirement, the district cut her pension to fund a scoreboard. At 58, no one would hire her. BMG did—for "Facilities Maintenance" that turned out to involve giant bugs.

Twenty-two years of handling chaos gave her perfect spatial awareness. The bugs are easier than teenagers. And these ones she's allowed to hit.

**Mechanic:** Maximum range hits deal triple damage. Enemies inside minimum range take zero. Spacing is everything.

---

### 4. The Intern ("Heavy")

**In-Game:** A young person in foam-covered armor. Visibly exhausted. No explanation given.

**Player-Facing Details:**
- Moves slower as armor builds up (gameplay mechanic)
- Equipment is clearly "prototype" (warning labels scratched off)
- Character select text cycles: snippets from their contract's fine print

**Hidden Lore (Text Collectibles):**
- Document: Student loan statement - "$147,000"
- Loading tip: "Heavy has been 'Open to Opportunities' for 18 months."
- Achievement "Entry Level" description: "The job posting said 'Heavy Equipment Operator.' It did not mean forklifts."
- Bestiary note on Heavy's foam: "Equipment manual stamp: NOT FOR HUMAN DEPLOYMENT"

**Full Backstory:**
Jamie Park graduated with honors and crushing debt. After two years of unpaid internships, they applied to every job listing that said "entry-level" without reading further. BMG's "Heavy Equipment Operator" position did not mean forklifts.

The foam sprayer was designed for robots. Jamie is not a robot, but they're too broke to quit. The armor builds up. The exhaustion helps. Feeling less is a feature.

**Mechanic:** Foam creates ablative armor. More armor = slower movement. Damage speeds you up. Constant oscillation.

---

### 5. The Electrician ("Sparky")

**In-Game:** A man with jury-rigged electrical weapons. Clearly knows they're unsafe.

**Player-Facing Details:**
- Mutters about safety codes during combat (if VO added)
- Equipment sparks and overheats (gameplay mechanic)
- Character select text: "OSHA complaint #47291. Status: BLACKLISTED."

**Hidden Lore (Text Collectibles):**
- Document: Original OSHA complaint with detailed safety violations
- Document: Counter-complaint: "frivolous reporting, recommend termination"
- Achievement "Code Violation" tracks OSHA violations witnessed
- Loading tip: "Sparky's license—stamped REVOKED."

**Full Backstory:**
Victor Kowalski was a union electrician who reported his employer for running live wires through water pipes. OSHA investigated. Fines were issued. And Vic's name went on a blacklist.

Thirty years of perfect safety record. Three months behind on rent. BMG's recruiter offered him "creative wiring." His weapon violates eighteen regulations. The irony isn't lost on him.

**Mechanic:** Chain lightning arcs between enemies. Heat builds with use. 100% heat = forced discharge and self-damage.

---

### 6. The Scientist ("Doc")

**In-Game:** A woman with experimental energy weapons. Handles them too competently.

**Player-Facing Details:**
- Weapon is clearly prototype (BMG R&D markings)
- She knows what the bugs are (rare text hints in bestiary when playing as her)
- Character select text: "Former BMG R&D. Emphasis on 'former.'"

**Hidden Lore (Text Collectibles):**
- Document: Verdantix safety reports—signed "S. Chen." With annotations questioning findings.
- Document: Internal memo - "Dr. Chen's concerns are noted. Production continues."
- Achievement "Subject C" unlocks when playing Doc in Thorne's former lab zones
- Bestiary entries (Doc only): unredacted versions reveal what she actually wrote

**Full Backstory:**
Dr. Sarah Chen was lead biochemist on the V-7 project. She raised safety concerns. Her supervisor, Dr. Thorne, told her to sign the revised reports or lose her visa, her career, her everything. She signed.

When the mutations started, she tried to go public. BMG's legal team arrived with the reports bearing her signature—and a choice: Division 8 or prison. She's gathering evidence. Or just surviving. The line is blurry.

Thorne watches. He sends her personalized briefings. He finds it "narratively satisfying."

**Mechanic:** Charged particle beam. Maximum charge = maximum damage, but risk of critical failure and self-damage. Risk/reward every shot.

---

## 📖 Text-Based Lore Elements (Collectibles & UI)

*These are the fragments players can find. Each raises questions without answering them. ALL delivered through text popups, UI, or menus—not visual props.*

### Collectible Document Categories

**Corporate Memos (Common)**
- Internal communications with redacted names/dates
- Always described as partially torn or water-damaged
- Hint at larger conspiracy without explaining it

**Personnel Files (Uncommon)**
- Contractor records—IDs jumping from #0001 to #0447
- Performance reviews for deceased contractors
- Medical records with disturbing entries

**Research Notes (Rare)**
- Dr. Thorne's clinical observations
- Verdantix safety warnings that were ignored
- Mutation classification documents

**The Gary Letters (Very Rare)**
- Fragments of the Verdantix founder's warnings
- Each piece contains one ominous line
- Complete set reveals... something players will debate

### Sample Collectible Documents

**MEMO FRAGMENT #1:**
```
...recommend immediate transition from Phase 1 protocol.
Liability exposure exceeds acceptable threshold.
Employee numbering to restart at 0447 per attached...
[TORN]
```

**PERSONNEL FILE (PARTIAL):**
```
CONTRACTOR #0446
STATUS: [REDACTED]
FINAL DEPLOYMENT: Sector 4-XQ
NOTES: "Did not complete contract. Equipment unrecovered.
Body unrecovered. Do not send additional teams to 4-XQ
until [REMAINDER ILLEGIBLE]"
```

**THORNE RESEARCH NOTE:**
```
Day 147. Subject C continues to exceed expectations.
Guilt response is diminishing. Work ethic improving.
Hypothesis: exposure to field conditions produces...
[PAGE MISSING]
```

**GARY LETTER #3 OF 7:**
```
...and if you're reading this, they've already won.
The compound was never meant for agriculture. Check
the original patents. Check who filed them. Check...
[BURNED]
```

### Room Name Storytelling

**Suburbia Zone Room Names:**
- "FORMER RESIDENCE - LOT 14"
- "KITCHEN - MEAL UNFINISHED"
- "CHILDREN'S ROOM - EVACUATED"
- "GARAGE - ENGINE STILL RUNNING"
- "BACKYARD - DOGHOUSE (MASSIVE)"

**What Room Names Imply:**
- Names transition from sterile ("Sector 7-RG") to disturbing ("Former Daycare - Room 12")
- Parentheticals hint at hidden stories without showing them
- Players fill in the gaps themselves

---

## 🗺️ Map/Zone Design Notes

*Storytelling through room names, zone progression, and collectible placement—not detailed visual props.*

### Sector 7 (Suburbia)

**The Surface Story:** Nice neighborhood got infested. You're cleaning it up.

**The Hidden Story (Via Collectibles & Room Names):**
- Collectible: HOA newsletter mentioning "free fertilizer samples from BMG Agricultural"
- Room name: "LOT 14 - SAMPLE RECIPIENT"
- Document: Distribution records showing this neighborhood was targeted
- Timeline inconsistencies in dates (infestation reports before official "incident" date)

**Discoverable Elements:**
- Documents referencing "Phase 1 residential test sites"
- Room names with dates that predate the official incident
- Achievement "Early Adopters" for clearing all Suburbia zones

### Sector 4 (Sewers) — Future

**The Surface Story:** Bugs in the sewers. Classic exterminator territory.

**The Hidden Story (Via Collectibles & Room Names):**
- Room name: "RUNOFF POINT - FACILITY OMEGA DRAINAGE"
- Collectible: Disposal manifests showing what was flushed
- Document: Reports of "organic material" that doesn't match any known species
- Room name: "SECTOR 4-XQ - DO NOT ENTER" (but you can...)
- Achievement "Unrecovered" for reaching Sector 4-XQ

### Facility Omega — Future

**The Surface Story:** Research lab breach. Clean it up.

**The Hidden Story (Via Collectibles & Room Names):**
- The facility isn't just studying bugs—it's MAKING them
- Room name (Doc only): "DR. CHEN'S FORMER LAB"
- Collectible: Original research proposals vs. what was actually built
- Document: Evidence of what BMG actually planned
- Achievement "Whistleblower" for finding all of Doc's original reports

---

## 🧊 The Iceberg (Theory-Bait Summary)

*For future community discussions/videos. Things players will debate:*

**SURFACE:**
- Giant bugs, corporate satire, roguelike gameplay

**LEVEL 1:**
- BMG caused the outbreak through negligence
- Contractors are expendable labor

**LEVEL 2:**
- Contractor numbers skip 446 people. What happened to them?
- The bugs show signs of being *directed*, not random
- Equipment is clearly prototype—contractors are test subjects

**LEVEL 3:**
- BMG knew about V-7's effects before the acquisition
- Legal documents reference "post-event protocols" dated BEFORE the incident
- Sterling Voss may not be a real person

**LEVEL 4:**
- Facility Omega isn't just researching bugs—it's weaponizing them
- The infestation isn't spreading naturally. It's being cultivated.
- Dr. Chen isn't the only "former researcher" in Division 8

**LEVEL 5:**
- A.M.I.'s "human baseline" was an actual person. Who?
- The Gary Letters, assembled, reveal BMG's true purpose
- The bugs aren't the product. The CONTRACTORS are.

**THE BOTTOM:**
- What is Division 1-7?
- Who is still collecting Voss's bonus checks?

---

## ✅ Implementation Checklist

**For Pure Gameplay (No Lore Interruption):**
- [ ] No cutscenes
- [ ] No mandatory dialogue
- [ ] No AI companion narration
- [ ] UI text is minimal and functional
- [ ] Players can ignore everything and just play

**For Lore Hunters (All Text-Based):**
- [ ] Collectible documents (glowing pixel pickups → text popup)
- [ ] Evocative room names that imply stories
- [ ] Bestiary/Codex entries with hidden lore
- [ ] Upgrade/item descriptions in corporate voice
- [ ] Achievement names and descriptions with cryptic hints
- [ ] Death screen variations referencing past contractors
- [ ] Loading screen tips that get darker over time
- [ ] Character select flavor text with backstory breadcrumbs

**Mystery Maintenance:**
- [ ] Never fully explain the incident
- [ ] Never show Voss, Thorne, or Stone directly
- [ ] Never resolve the "missing contractors" question
- [ ] Leave at least 3 major mysteries unresolved
- [ ] Ensure documents contradict each other slightly

---

*This document is developer reference only. The best lore is the lore players feel clever for finding.*
