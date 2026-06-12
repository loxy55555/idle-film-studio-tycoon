#!/usr/bin/env python3
"""Generate MovieCatalog_Definitive_300.csv — Phase CATÁLOGO 4.0."""
import csv
import re
from collections import Counter, defaultdict

OUT = r"D:\UnityProjects\idlefilm\Assets\Data\Imports\MovieCatalog_Definitive_300.csv"

GENRE_TOTALS = {
    "Action": 45,
    "SciFi": 40,
    "Drama": 35,
    "Horror": 35,
    "Comedy": 35,
    "Fantasy": 30,
    "Romance": 25,
    "Thriller": 25,
    "Animation": 20,
    "Documentary": 10,
}

LEGENDARIES = {
    "Action": "Iron Wolves",
    "SciFi": "Echoes of Titan",
    "Drama": "When the River Sleeps",
    "Horror": "The Hollow House",
    "Comedy": "Detective Potato",
    "Fantasy": "Song of the Forgotten Realm",
    "Romance": "A Sky for Two",
    "Thriller": "Red File 27",
    "Animation": "Little Giants",
    "Documentary": "Voices of the Deep",
}

LEGENDARY_TAGLINES = {
    "Iron Wolves": "An elite unit hunts what the world refused to believe existed.",
    "Echoes of Titan": "Humanity's farthest signal returns with a warning no one wants to hear.",
    "When the River Sleeps": "A family secret surfaces when the drought ends and the river runs again.",
    "The Hollow House": "Every room remembers the scream that never left the walls.",
    "Detective Potato": "The city's strangest detective solves crimes with charm and starch.",
    "Song of the Forgotten Realm": "A lost melody can reopen a kingdom erased from history.",
    "A Sky for Two": "Two strangers share one horizon and one impossible choice.",
    "Red File 27": "Case closed does not mean case finished.",
    "Little Giants": "Small heroes carry the biggest hearts in the biggest adventure.",
    "Voices of the Deep": "The ocean still speaks to those willing to listen.",
}

# (saga_id, display_name, size, primary_genre, first_city)
SAGAS = [
    ("project_avalanche", "Project Avalanche", 6, "Action", 2),
    ("dominion", "Dominion", 6, "SciFi", 3),
    ("atlas_signal", "Atlas Signal", 4, "SciFi", 3),
    ("the_long_road", "The Long Road", 4, "Drama", 2),
    ("hollow_creek", "Hollow Creek", 4, "Horror", 3),
    ("moonkeeper", "Moonkeeper", 4, "Fantasy", 4),
    ("the_last_colony", "The Last Colony", 6, "SciFi", 5),
    ("winter_letters", "Winter Letters", 2, "Drama", 2),
    ("blackwater", "Blackwater", 4, "Thriller", 4),
    ("uncle_gary", "Uncle Gary", 2, "Comedy", 3),
    ("the_hidden_crown", "The Hidden Crown", 4, "Fantasy", 5),
    ("the_glass_forest", "The Glass Forest", 2, "Fantasy", 4),
    ("beneath_the_summer_sky", "Beneath the Summer Sky", 2, "Romance", 3),
    ("the_black_ledger", "The Black Ledger", 4, "Thriller", 5),
    ("shadow_trigger", "Shadow Trigger", 4, "Action", 2),
]

CITY_NON_LEG = [50, 45, 45, 40, 35, 30, 30, 15]
CITY_RARITY = {
    1: {"Common": 47, "Rare": 3, "Epic": 0},
    2: {"Common": 38, "Rare": 7, "Epic": 0},
    3: {"Common": 34, "Rare": 10, "Epic": 1},
    4: {"Common": 26, "Rare": 12, "Epic": 2},
    5: {"Common": 18, "Rare": 14, "Epic": 3},
    6: {"Common": 10, "Rare": 14, "Epic": 6},
    7: {"Common": 4, "Rare": 22, "Epic": 4},
    8: {"Common": 3, "Rare": 8, "Epic": 4},
}

TITLES = {
    "Action": """Midnight Extraction|A hostage rescue with no extraction window.
Steel Horizon|Fleet commanders gamble everything on one offensive.
Broken Protocol|Rules of engagement become suggestions under fire.
Urban Siege|One city block becomes a war zone overnight.
Zero Hour Convoy|Supplies must reach the front before dawn.
Rogue Squadron|Pilots who answer to no flag save the day.
Hardline Justice|A cop crosses every line to stop a syndicate.
Velocity Kill|Speed is the only weapon left on the table.
Iron Descent|An orbital drop team lands in the wrong war.
Fireline|Firefighters battle flames and a hidden conspiracy.
Last Stand Bridge|Hold the bridge until reinforcements arrive.
Night Raid|Special forces enter a fortress in darkness.
Collateral Run|Every second of delay costs civilian lives.
Red Sector|A quarantined district hides a military secret.
Tactical Silence|No comms. No backup. No mistakes.
Hammer Strike|A blunt force operation with surgical precision.
Borderline|Two nations. One fugitive. Total chaos.
Aftershock|When the mission ends, the real fight begins.
Steel Covenant|Allies forged in combat must trust or die.
Outrider|One scout against an advancing army.
Crash Vector|A pilot must land a failing jet in hostile territory.
Ghost Company|A unit erased from every roster except the enemy's.
Pressure Point|One warehouse. One bomb. One chance.
Rapid Thunder|Lightning war in a city that never sleeps.
Final Vector|Intercept the missile or lose a capital.
Blackout Run|Power fails across the city during a prison break.
Steel Rain|Artillery decides the battle before infantry moves.
Frontline Echo|Veterans return to the only war that still matters.
Overwatch Down|A sniper team loses eyes in the sky.
Strike Package|Every asset in the theater on one target.
Convoy Seven|Seventh convoy through the deadliest road on earth.
Hostile Air|Friendly skies turn hostile in seconds.
Breacher Unit|Doors, walls, and regimes fall the same way.
Extraction Point|Get them out before the perimeter collapses.
Warpath|A general's last campaign becomes personal.
Rapid Fire|Bullets, debts, and revenge move at the same speed.
Steel Veil|A covert war fought without uniforms.
Terminal Velocity|Falling is the easy part. Landing is not.
Iron Gate|Storm the gate or lose the hostages.
Nightfall Protocol|When the sun sets, the rules change.
Crossfire Alley|Every alley is an ambush waiting to happen.
Resolute|One soldier refuses to abandon the mission.
Shockwave|One explosion rewrites the city's power map.
Frontline Ghost|Declared dead, he keeps fighting anyway.
Steel Testament|Honor written in smoke and steel.""".strip().split("\n"),
    "SciFi": """Atlas Signal|A deep-space beacon wakes after centuries of silence.
Neon Drift|Two runners chase truth through a neon megacity.
Quantum Fault|Reality fractures along a single bad equation.
Solar Exodus|Earth's last ark leaves with secrets aboard.
Echo Chamber|An AI learns emotion from archived voices.
Orbital Decay|A station falls. A crew must choose who leaves.
Synthetic Dawn|Machines claim personhood in open court.
Void Cartographer|Maps of space that should not exist.
Chrono Debt|Borrow time today. Pay with years tomorrow.
The Last Colony|Humanity's farthest settlement sends a distress call.
Signal Lost|Contact with Mars ends without explanation.
Binary Sun|Two stars. One habitable world. Many lies.
Neural Tide|Thoughts spread like a virus across the net.
Project Helix|Genetic upgrades come with hidden clauses.
Dark Matter Run|Smugglers haul cargo that bends light itself.
Stellar Cartel|Crime syndicates rule the asteroid belt.
Phase Shift|A city flickers between dimensions at midnight.
Core Breach|Drill too deep and something drills back.
Proxy War|Humans fight while AIs negotiate peace.
Glass Orbit|A transparent station hides opaque motives.
Redshift|The universe is expanding faster than expected.
Terraformers|Make a world livable. Unmake the old one.
Null Gravity|Zero-g heist aboard a luxury liner.
Circuit Prophecy|Old code predicts events before they happen.
Helios Gate|A portal to the sun's corona opens trade routes.
Machine Psalm|Religion spreads among self-aware drones.
Cryo Wake|Sleepers awaken to a world that forgot them.
Deep Archive|Human history stored in a bunker on the moon.
Event Horizon Job|Salvage a ship that crossed the line once.
Pulse Drive|Faster-than-light travel breaks more than physics.
Synthetic Eden|Paradise engineered for perfect obedience.
Starfall Treaty|Aliens arrive the day peace is finally signed.
Neural Ghost|A dead pilot still flies through the network.
Ion Storm|Navigation fails. Instinct must succeed.
Gravity Well|Rescue a probe trapped near a black hole.
Code Genesis|Who wrote the first line of sentient software.
Orbital Ransom|Hold a station hostage from the inside.
Parallel Earth|Visitors claim they are from a better timeline.
The Long Signal|A message from outside the galaxy arrives incomplete.""".strip().split("\n"),
    "Drama": """Winter Letters|Old correspondence reveals a family fracture.
The Long Road|A father walks home across a divided country.
River's End|A town disappears when the dam is built.
Paper House|Wealth built on documents that may be forged.
Quiet Mercy|A nurse chooses compassion over policy.
Broken Choir|A church choir reunites after scandal.
Harvest Moon|One last season on the family farm.
Glass Harbor|A port town mourns a ship that never returned.
Still Waters|Calm surfaces hide generations of silence.
The Weight of Rain|Floods wash up truths buried for decades.
Small Mercies|Kindness becomes rebellion in a rigid town.
Painted Sky|An artist paints the sky before going blind.
Borrowed Time|A diagnosis reorders every priority.
Crosswind|Sibling pilots face the same storm differently.
Homeward|Return to the place that never felt like home.
The Last Lesson|A teacher's final year changes every student.
Fading Light|Care for the dying. Learn to let go.
Undertow|Love pulls harder when the tide turns cruel.
Open Field|Childhood friends meet where they once played.
Stone Garden|Grief grows into something unexpectedly alive.
Two Rivers|Two towns. One water rights war. One family.
Winter Letters II|Return to the house the letters never left.
Silent Auction|A charity event exposes private shame.
Blue Hour|Twilight conversations that change everything.
The Goodbye Room|A hospice where every door is a farewell.
Ash and Honey|Bitterness and sweetness in the same jar.
Northbound|A bus ride north becomes a reckoning.
Ledger of Grace|Debts forgiven. Debts remembered.
The Long Road II|The road home is longer the second time.
Field Notes|A journalist stays too long in a border town.
Paper Cranes|Origami left on graves becomes a movement.
Evening Class|Adults learn to read and to hope again.
Salt Line|Coastal erosion mirrors a failing marriage.
Winter Letters III|The final letter arrives without a stamp.""".strip().split("\n"),
    "Horror": """Hollow Creek|Something in the creek keeps calling names.
The Hollow House|Every room remembers a scream.
Black Mirror Lake|Reflections show what should not be there.
Whisper Floor|Footsteps above an empty apartment.
Pale Harvest|Crops grow wrong after the meteor shower.
Night Parish|A church that only opens after midnight.
Bone Lantern|Light made from things that should stay buried.
Static Room|Televisions tune to channels from the dead.
Cold Threshold|Do not open the door when it is cold inside.
Grinning Dark|Darkness wears a familiar smile.
Last Rites|Funeral rites that refuse to end.
Red Nursery|A child's room painted in a color that moves.
Hollow Creek II|The creek runs backward after the flood.
Skinwalker Line|Tracks in the snow that change mid-stride.
The Veil Thin|Boundaries between worlds wear thin at solstice.
Moth Parish|Wings beat against every window at once.
Grave Shift|Night workers hear digging from empty plots.
Hollow Creek III|Names carved into trees that were never cut.
Silent Hive|Bees stop buzzing. Then people stop speaking.
Pale Door|A door appears where a wall should be solid.
Borrowed Skin|Wear someone else's face for one night only.
Crimson Static|Every radio plays the same dying breath.
Hollow Creek IV|The creek remembers every body it took.
Night Garden|Plants grow toward sleeping faces.
The Last Knock|Three knocks. Always three. Never more.
Fever House|Quarantine becomes possession.
Under the Bed|Childhood fear returns with adult teeth.
Hollow Creek V|Five films. One creek. No escape.
Blackwater Echo|Water carries voices from upstream crimes.
Mirror Sleep|Do not sleep facing a mirror in this house.
Pale Signal|Emergency broadcast repeats a personal message.
Hollow Creek VI|Return to the source of every nightmare.
The Unmarked|Graves without names fill overnight.
Cold Communion|Communion wine tastes like river water.
Hollow End|The creek meets the sea and waits.""".strip().split("\n"),
    "Comedy": """Uncle Gary|Gary means well. Chaos follows anyway.
Detective Potato|Produce aisle investigations. Serious results.
Office Panic|Corporate retreat becomes survival comedy.
Wedding Crashers Anonymous|They quit crashing. Weddings find them.
Roommate Protocol|Three strangers. One lease. Infinite rules.
Bad Advice Hotline|Call for help. Receive catastrophe.
The Wrong Speech|Best man reads the wrong notes. Twice.
Parent Trap 2.0|Kids reverse-engineer adult relationships.
Sushi Disaster|One roll destroys a chef's reputation.
Late Shift Laughs|Retail workers versus the universe.
Uncle Gary II|Gary babysits. Parents reconsider life choices.
Dog Mayor|A town elects a dog. Bureaucracy adapts reluctantly.
Startup Chaos|Pitch deck. Product missing. Funding anyway.
Ghost Roommate|Haunting with chores and rent disputes.
The Heist That Wasn't|Amateurs steal the wrong safe. Twice.
Cooking Catastrophe|Celebrity chef meets reality TV reality.
Uncle Gary III|Family reunion. Gary brings fireworks. Literally.
Love in Transit|Commuters fall in love on the wrong train.
Accidental Guru|Accidental wisdom goes viral overnight.
The Backup Plan|Plan B becomes Plan A through incompetence.
Mismatched Pair|Odd couple detectives solve absurd crimes.
Uncle Gary IV|Gary runs for office. Sanity loses.
Room for Error|Airbnb listing omits the haunted annex.
Boss of None|Promoted by mistake. Promoted again anyway.
The Great Mix-Up|Identical bags. Identical chaos.
Uncle Gary V|Gary opens a food truck. Health code weeps.
Laugh Track|Sitcom laugh track follows one man in real life.
Double Booked|Two weddings. One venue. Zero adults in charge.
Uncle Gary VI|Gary's wedding. Everyone else pays the price.
Improv Nation|Nationwide improv tournament. No one is ready.
Uncle Gary VII|Final Gary. Maximum Gary. Still somehow charming.
Pizza Tribunal|Delivery dispute escalates to municipal court.
Accidental Hero|Save the cat. Become mayor accidentally.
Side Gig|A temp job becomes a permanent comedy of errors.
Uncle Gary VIII|The Gary saga ends. Gary disagrees.""".strip().split("\n"),
    "Fantasy": """Song of the Forgotten Realm|A melody reopens a kingdom erased from maps.
The Hidden Crown|A crown hidden in plain sight chooses its heir.
The Glass Forest|Trees of crystal sing when the moon is full.
Moonkeeper|One guardian tends the door between worlds.
Ash Crown|Throne of embers passes to an unlikely ruler.
Dragon's Debt|A dragon saved a village. The bill is due.
Spellbound Market|Magic traded like goods with fine print.
The Hidden Crown II|Rebellion wears the crown's shadow.
Runebreaker|Break the rune. Break the curse. Break yourself.
Starforge|Weapons forged from fallen stars decide a war.
The Glass Forest II|Crystal roots reach toward a dying sun.
Witch's Bargain|Power offered. Price deferred. Never free.
Moonkeeper II|The door opens wider each full moon.
Throne of Thorns|Sit carefully. The throne bites back.
The Hidden Crown III|Three kingdoms. One crown. Zero peace.
River of Stars|A river flows upward carrying constellations.
The Glass Forest III|Forest walks toward the capital at dawn.
Oathbound|Swear an oath. Lose the option to leave.
Moonkeeper III|Guardian becomes prisoner of the threshold.
Spellwright|Write magic into law. Watch law fight back.
The Hidden Crown IV|Coronation day. Assassins RSVP yes.
Dragon's Echo|An old dragon's voice guides a young thief.
The Glass Forest IV|Crystal shards cut more than flesh.
Moonkeeper IV|Final vigil before the door falls forever.
Crown of Embers|Fire crown for a queen who hates heat.
Forgotten Realm|Maps lie. The realm remembers.
The Hidden Crown V|Peace treaty signed in dragon blood.
Glass Crown|Transparency as tyranny in a fairy court.
Moonkeeper V|Last moon. Last keeper. Last chance.
Realmfall|When magic fails, kingdoms fall like cards.""".strip().split("\n"),
    "Romance": """Beneath the Summer Sky|Love blooms under endless summer light.
A Sky for Two|Two strangers share one horizon.
Coffee at Dawn|Daily coffee becomes daily devotion.
Letters in Rain|Love letters survive a stormy season.
Second Chance Waltz|Former lovers meet at a charity ball.
Harbor Lights|Fishermen and poets share the same pier.
The Matchbook|A matchbook from Paris starts everything.
Slow Dance|Patience wears the shape of love.
Paris Delay|Missed flight. Found connection.
Winter Bloom|Flowers in snow. Hearts thaw slowly.
Two Tickets|Extra ticket. Unexpected companion.
Sunset Clause|Contract romance with real feelings.
Rain on Sunday|Stay in. Fall in love. Repeat.
The Last Dance|Ballroom closes. Romance opens.
Starlight Pact|Make a wish on the same star twice.
Beneath the Summer Sky II|Summer ends. Feelings do not.
Crossed Lines|Phone wrong number. Right person.
Golden Hour|Photographers capture more than light.
Quiet Harbor|Small town. Big feelings. Gentle pace.
Midnight Train|Share a compartment. Share a future.
Paper Hearts|Origami notes left on park benches.
The Long Hello|Goodbye took years. Hello takes courage.
Beneath the Summer Sky III|Return to the place where it started.
Love in Minor Key|Musicians compose a shared life.
A Sky for Two II|Same sky. Different cities. Same pull.""".strip().split("\n"),
    "Thriller": """Red File 27|Case closed does not mean finished.
The Black Ledger|Numbers that kill when audited.
Shadow Trigger|One event triggers a cascade of conspiracies.
Blackwater|A town's water hides corporate sins.
Dead Drop|Package exchange. Wrong package. Right danger.
Silent Witness|She saw everything. She says nothing.
The Black Ledger II|Follow the money into the dark.
Glass Witness|Testimony recorded in a mirror maze.
Cold Case Heat|Old evidence burns new trails.
Red File 12|Earlier file. Same handler. Worse secrets.
Undercover Line|Deep cover erases the person inside.
The Black Ledger III|Auditors become targets become hunters.
Night Ledger|Crimes booked after midnight never close.
Shadow Trigger II|Second trigger. Bigger fallout.
Wiretap|Every conversation is evidence or bait.
Red File 27 II|The file reopens without authorization.
Blackwater II|Upstream crimes surface downstream bodies.
The Black Ledger IV|Final account balanced in blood.
Proxy Kill|Assassin hired through layers of deniability.
Shadow Trigger III|Conspiracy outlives its architects.
Red File 27 III|Case number changes. Truth does not.
Silent Contract|Sign or disappear quietly.
Blackwater III|Water tests clean. People do not.
The Black Ledger V|Ledger closed. Lives are not.
Red Horizon|Sunset industry hides dawn crimes.""".strip().split("\n"),
    "Animation": """Little Giants|Small heroes. Big hearts. Huge adventure.
Cloud Riders|Children sail clouds in homemade ships.
Paint the Wind|Colors move with the breeze magically.
The Clockwork Fox|A mechanical fox searches for warmth.
Star Pup|A stray dog maps constellations for kids.
Paper Kingdom|Origami citizens defend their realm.
Tiny Titans|Miniature gods argue over a sandbox world.
Bubble City|Entire civilization lives inside bubbles.
Moonlight Mice|Midnight quests for cheese and courage.
The Last Kite|One kite carries a village's hope aloft.
Robot Best Friend|Friendship programmed beyond specs.
Dragon's Lunchbox|School lunch hides a dragon egg.
Whisper Woods|Trees whisper advice to lost children.
Sky Garden|Floating gardens need young caretakers.
Little Giants II|Giants return. Problems grew too.
Penguin Express|Express delivery across ice continents.
Magic Pencil|Draw something. It comes alive briefly.
The Singing Stone|A stone teaches a village to harmonize.
Tiny Voyagers|Microscopic explorers inside a backyard.
Starlight Parade|Annual parade lights up the night sky.""".strip().split("\n"),
    "Documentary": """Voices of the Deep|Oceanographers record songs of the abyss.
City of Light|Paris artisans keep ancient crafts alive.
The Last Beekeeper|One hive may save a region's crops.
Tracks in Dust|Nomads crossing deserts for water.
Steel and Song|Shipyard workers sing through shifts.
Hidden Rivers|Underground rivers mapped for the first time.
The Glassblower|Molten art in a family workshop.
Voices of the Deep II|Return to trenches no light reaches.
Border Seeds|Farmers plant crops along disputed lines.
The Archivist|One person saves a nation's film heritage.""".strip().split("\n"),
}

SAGA_TITLES = {
    "project_avalanche": [
        ("Steel Horizon", "Action"), ("Broken Protocol", "Action"), ("Urban Siege", "Action"),
        ("Zero Hour Convoy", "Action"), ("Iron Descent", "Action"), ("Warpath", "Action"),
    ],
    "dominion": [
        ("Solar Exodus", "SciFi"), ("Neural Ghost", "SciFi"), ("Signal Lost", "SciFi"),
        ("Machine Psalm", "SciFi"), ("Starfall Treaty", "SciFi"), ("Parallel Earth", "SciFi"),
    ],
    "atlas_signal": [
        ("Atlas Signal", "SciFi"), ("The Long Signal", "SciFi"), ("Deep Archive", "SciFi"), ("Ion Storm", "SciFi"),
    ],
    "the_long_road": [
        ("The Long Road", "Drama"), ("Homeward", "Drama"), ("Northbound", "Drama"), ("The Long Road II", "Drama"),
    ],
    "hollow_creek": [
        ("Hollow Creek", "Horror"), ("Hollow Creek II", "Horror"), ("Hollow Creek III", "Horror"), ("Hollow Creek IV", "Horror"),
    ],
    "moonkeeper": [
        ("Moonkeeper", "Fantasy"), ("Moonkeeper II", "Fantasy"), ("Moonkeeper III", "Fantasy"), ("Moonkeeper IV", "Fantasy"),
    ],
    "the_last_colony": [
        ("The Last Colony", "SciFi"), ("Cryo Wake", "SciFi"), ("Terraformers", "SciFi"),
        ("Gravity Well", "SciFi"), ("Orbital Ransom", "SciFi"), ("Code Genesis", "SciFi"),
    ],
    "winter_letters": [
        ("Winter Letters", "Drama"), ("Winter Letters II", "Drama"),
    ],
    "blackwater": [
        ("Blackwater Echo", "Horror"), ("Blackwater", "Thriller"), ("Blackwater II", "Thriller"), ("Blackwater III", "Thriller"),
    ],
    "uncle_gary": [
        ("Uncle Gary", "Comedy"), ("Uncle Gary II", "Comedy"),
    ],
    "the_hidden_crown": [
        ("The Hidden Crown", "Fantasy"), ("The Hidden Crown II", "Fantasy"),
        ("The Hidden Crown III", "Fantasy"), ("The Hidden Crown IV", "Fantasy"),
    ],
    "the_glass_forest": [
        ("The Glass Forest", "Fantasy"), ("The Glass Forest II", "Fantasy"),
    ],
    "beneath_the_summer_sky": [
        ("Beneath the Summer Sky", "Romance"), ("Beneath the Summer Sky II", "Romance"),
    ],
    "the_black_ledger": [
        ("The Black Ledger", "Thriller"), ("The Black Ledger II", "Thriller"),
        ("The Black Ledger III", "Thriller"), ("The Black Ledger IV", "Thriller"),
    ],
    "shadow_trigger": [
        ("Shadow Trigger", "Thriller"), ("Shadow Trigger II", "Thriller"),
        ("Nightfall Protocol", "Action"), ("Frontline Ghost", "Action"),
    ],
}


def slug_catalog_id(genre: str, title: str) -> str:
    prefix = {
        "Action": "Action", "SciFi": "SciFi", "Drama": "Drama", "Horror": "Horror",
        "Comedy": "Comedy", "Fantasy": "Fantasy", "Romance": "Romance", "Thriller": "Thriller",
        "Animation": "Animation", "Documentary": "Documentary",
    }[genre]
    slug = re.sub(r"[^A-Za-z0-9]+", "_", title).strip("_")
    return f"{prefix}_{slug}"


def poster_file(title: str) -> str:
    base = re.sub(r"[^A-Za-z0-9]", "", title)
    return f"{base}.png"


def parse_title_line(line: str):
    if "|" in line:
        t, tag = line.split("|", 1)
        return t.strip(), tag.strip()
    return line.strip(), f"A {line.strip()} story."


def build_movies():
    saga_assigned = set()
    movies = []

    saga_meta = {s[0]: s for s in SAGAS}

    for saga_id, entries in SAGA_TITLES.items():
        meta = saga_meta[saga_id]
        first_city = meta[4]
        for order, (title, genre) in enumerate(entries, start=1):
            tagline = next((parse_title_line(l)[1] for g, lines in TITLES.items() for l in lines if parse_title_line(l)[0] == title), f"Part {order} of {meta[1]}.")
            movies.append({
                "catalogId": slug_catalog_id(genre, title),
                "movieName": title,
                "genre": genre,
                "rarity": None,
                "cityUnlock": None,
                "sagaId": saga_id,
                "sagaOrder": order,
                "isLegendary": 0,
                "posterFile": poster_file(title),
                "tagline": tagline,
            })
            saga_assigned.add((genre, title))

    for genre, total in GENRE_TOTALS.items():
        non_leg = total - 1
        lines = TITLES[genre]
        count = sum(1 for m in movies if m["genre"] == genre and not m["isLegendary"])
        for line in lines:
            title, tagline = parse_title_line(line)
            if title == LEGENDARIES[genre]:
                continue
            if (genre, title) in saga_assigned:
                continue
            if count >= non_leg:
                break
            movies.append({
                "catalogId": slug_catalog_id(genre, title),
                "movieName": title,
                "genre": genre,
                "rarity": None,
                "cityUnlock": None,
                "sagaId": "",
                "sagaOrder": 0,
                "isLegendary": 0,
                "posterFile": poster_file(title),
                "tagline": tagline,
            })
            count += 1
        if count < non_leg:
            raise SystemExit(f"Not enough titles for {genre}: need {non_leg}, got {count}")

    for genre, title in LEGENDARIES.items():
        movies.append({
            "catalogId": f"{genre}_Legendary" if genre != "SciFi" else "SciFi_Legendary",
            "movieName": title,
            "genre": genre,
            "rarity": "Legendary",
            "cityUnlock": 8,
            "sagaId": "",
            "sagaOrder": 0,
            "isLegendary": 1,
            "posterFile": poster_file(title),
            "tagline": LEGENDARY_TAGLINES[title],
        })

    return movies


def assign_city_rarity(movies):
    non_leg = [m for m in movies if not m["isLegendary"]]
    if len(non_leg) != 290:
        raise SystemExit(f"Expected 290 non-legendary, got {len(non_leg)}")

    saga_meta = {s[0]: s for s in SAGAS}
    city_quota = {c: quota.copy() for c, quota in CITY_RARITY.items()}
    saga_movies = [m for m in non_leg if m["sagaId"]]
    regular = [m for m in non_leg if not m["sagaId"]]

    for m in saga_movies:
        city = saga_meta[m["sagaId"]][4]
        rarity = take_rarity(city_quota[city])
        m["rarity"] = rarity
        m["cityUnlock"] = city

    regular_idx = 0
    for city, count in enumerate(CITY_NON_LEG, start=1):
        saga_in_city = sum(1 for m in saga_movies if m["cityUnlock"] == city)
        slots = count - saga_in_city
        for _ in range(slots):
            if regular_idx >= len(regular):
                raise SystemExit(f"Not enough regular movies for city {city}")
            m = regular[regular_idx]
            regular_idx += 1
            m["rarity"] = take_rarity(city_quota[city])
            m["cityUnlock"] = city

    if regular_idx != len(regular):
        raise SystemExit(f"Unassigned regular movies: {len(regular) - regular_idx}")


def take_rarity(quota):
    for rarity in ("Common", "Rare", "Epic"):
        if quota[rarity] > 0:
            quota[rarity] -= 1
            return rarity
    raise SystemExit("City rarity quota exhausted")


def validate(movies):
    assert len(movies) == 300
    assert Counter(m["genre"] for m in movies) == Counter(GENRE_TOTALS)
    rar = Counter(m["rarity"] for m in movies)
    assert rar["Common"] == 180 and rar["Rare"] == 90 and rar["Epic"] == 20 and rar["Legendary"] == 10
    city = Counter(m["cityUnlock"] for m in movies)
    assert city[1] == 50 and city[8] == 25
    ids = [m["catalogId"] for m in movies]
    assert len(ids) == len(set(ids)), "Duplicate catalogId"
    for s in SAGAS:
        sid = s[0]
        entries = [m for m in movies if m["sagaId"] == sid]
        assert len(entries) == s[2], f"Saga {sid} size {len(entries)} != {s[2]}"


def main():
    movies = build_movies()
    assign_city_rarity(movies)
    validate(movies)

    header = ["catalogId", "movieName", "genre", "rarity", "cityUnlock", "sagaId", "sagaOrder", "isLegendary", "posterFile", "tagline"]
    with open(OUT, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(header)
        for m in sorted(movies, key=lambda x: (x["genre"], x["cityUnlock"], x["catalogId"])):
            w.writerow([
                m["catalogId"], m["movieName"], m["genre"], m["rarity"], m["cityUnlock"],
                m["sagaId"], m["sagaOrder"], m["isLegendary"], m["posterFile"], m["tagline"],
            ])
    print(f"Wrote {len(movies)} rows to {OUT}")


if __name__ == "__main__":
    main()
