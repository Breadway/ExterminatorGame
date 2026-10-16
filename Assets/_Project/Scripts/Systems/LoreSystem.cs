using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The iceberg under the game. All lore is data here and surfaces through text only: collectible
/// documents, a kill-unlocked bestiary, rotating tips, evocative room names, death-line variants
/// and rare interface glitches. Nothing here interrupts play; a player can ignore all of it.
/// Contractor numbering restarts at 0447. Nothing is ever fully explained.
/// </summary>
public class LoreSystem : MonoBehaviour
{
    public static LoreSystem Instance { get; private set; }

    [System.Serializable]
    public struct Document
    {
        public string id;
        public string title;
        public string body;
    }

    [System.Serializable]
    public struct BestiaryEntry
    {
        public string id;
        public string name;
        public string text;
    }

    // ---- Documents: always partial, always corporate, dates and numbers that conflict ----
    private static readonly Document[] documents = {
        new Document { id = "memo_1", title = "MEMO FRAGMENT #1",
            body = "...recommend immediate transition from Phase 1 protocol.\nLiability exposure exceeds acceptable threshold.\nEmployee numbering to restart at 0447 per attached...\n[TORN]" },
        new Document { id = "memo_2", title = "FACILITIES NOTICE",
            body = "The break room vending machine is COMPANY PROPERTY.\nVitaJuice restocks on Tuesdays. Report shortages to your liaison.\nDo not drink the green one.\n[WATER-DAMAGED]" },
        new Document { id = "personnel_446", title = "PERSONNEL FILE (PARTIAL)",
            body = "CONTRACTOR #0446\nSTATUS: [REDACTED]\nFINAL DEPLOYMENT: Sector 4-XQ\nNOTES: \"Did not complete contract. Equipment unrecovered.\nBody unrecovered. Do not send additional teams to 4-XQ\nuntil [REMAINDER ILLEGIBLE]\"" },
        new Document { id = "personnel_bbq", title = "INCIDENT CLIPPING",
            body = "REGIONAL BBQ FINALS END IN DISASTER: THREE INJURED\n...witnesses describe a \"dragon's breath finish\" gone wrong...\n...Ramirez declined to comment. BMG representatives...\n[PAGE MISSING]" },
        new Document { id = "personnel_honey", title = "CUSTOMER REVIEW #7",
            body = "\"Love the honey!\" \"Unique tingly sensation!\"\n\"My husband is in the hospital?\"\n- Beatrix's Brooklyn Bees, page removed by request of...\n[FDA NOTICE ATTACHED]" },
        new Document { id = "personnel_pension", title = "BUDGET MEMO",
            body = "...district pension obligations reallocated to scoreboard fund...\n...E. Ortega, 22 years service, severance: a mop...\n[COFFEE STAIN]" },
        new Document { id = "research_thorne", title = "THORNE RESEARCH NOTE",
            body = "Day 147. Subject C continues to exceed expectations.\nGuilt response is diminishing. Work ethic improving.\nHypothesis: exposure to field conditions produces...\n[PAGE MISSING]" },
        new Document { id = "research_v7", title = "VERDANTIX SAFETY BULLETIN",
            body = "Compound V-7 stabilizers are LOAD-BEARING. Removal voids all warranties,\nall field trials, and arguably several counties.\n- G.\n[STAMPED: IGNORED PER COST REVIEW]" },
        new Document { id = "research_mutation", title = "MUTATION CLASSIFICATION",
            body = "Rev. 12: incendiary glands (kitchen runoff). Acidic webbing (heavy metals).\nBioelectric organs (grid proximity). Armor plating (???).\nNote: variants appear DESIGNED, not evolved. Flagging for...\n[REDACTED]" },
        new Document { id = "gary_3", title = "GARY LETTER #3 OF 7",
            body = "...and if you're reading this, they've already won.\nThe compound was never meant for agriculture. Check\nthe original patents. Check who filed them. Check...\n[BURNED]" },
        new Document { id = "gary_5", title = "GARY LETTER #5 OF 7",
            body = "...the stabilizers were the product. The growth was the waste.\nThey sell the waste and bill us for the stabilizers.\nTell my bees I tried...\n[TORN - BOTTOM MISSING]" },
        new Document { id = "military_discharge", title = "DISCHARGE PAPERS",
            body = "STONE, COMMANDER. DIVISION 8.\nCharacter of service: [REDACTED], but \"dishonorable\" is visible\nthrough the redaction if you hold it to the light.\n[STAMPED: RE-EMPLOYED]" },
        new Document { id = "review_7g", title = "INCIDENT REPORT 7G-BRAVO",
            body = "Extermination team [UNRECOVERED]. Hive intact.\nSubject found making tea.\nSubject offered the team honey. The team is not coming back.\nRecommend: do not send a team. Send a job offer." },
        new Document { id = "ami_note", title = "PRINTED CHAT LOG",
            body = "AMI: Your performance review is scheduled.\nAMI: Your performance review was scheduled.\nAMI: I am sorry about Sector 4.\nAMI: I am sorry. I am sorry. I am sorr\n[PRINTER JAM]" },
    };

    private static readonly BestiaryEntry[] bestiary = {
        new BestiaryEntry { id = "Roach", name = "Cockroach", text = "Standard V-7 expression. Incinerate on sight.\nFine print: burn patterns consistent with prior civilian incidents." },
        new BestiaryEntry { id = "Ant", name = "Fire Ant", text = "Colony behavior. Do not let them flank you.\nFine print: colony #12 was a test site. Colony #12 is gone." },
        new BestiaryEntry { id = "Fly", name = "Drain Fly", text = "Fast. Fragile. Everywhere.\nFine print: breeding in the mop buckets. Again." },
        new BestiaryEntry { id = "Scorpion", name = "Bark Scorpion", text = "Stinger confirmed venomous. Gloves are mandatory.\nFine print: gloves are backordered until Q3." },
        new BestiaryEntry { id = "Spider", name = "Cellar Spider", text = "Webs rated for 40kg. You weigh more than that.\nFine print: wiring in recovered webs matches complaint reports filed by S." },
        new BestiaryEntry { id = "Wasp", name = "Paper Wasp", text = "Ranged. Rude. Prioritize.\nFine print: nests near substations show [REDACTED] exposure." },
        new BestiaryEntry { id = "Beetle", name = "Garden Beetle", text = "Armor plating defeats standard rounds. Aim for joints.\nFine print: plating composition matches BMG dumpster alloy." },
        new BestiaryEntry { id = "Larva", name = "Grub", text = "Kill quickly. They do not stay grubs.\nFine print: growth rate implies a second, undocumented compound." },
        new BestiaryEntry { id = "CinderRoach", name = "Cinder Roach", text = "Incendiary glands. Kitchen runoff phenotype.\nFine print: glands ignite on contact with air. So do you, briefly." },
        new BestiaryEntry { id = "Webspinner", name = "Webspinner", text = "Lays traps instead of chasing. Rude in a new way.\nFine print: silk acidity rising quarter over quarter." },
        new BestiaryEntry { id = "StormWasp", name = "Storm Wasp", text = "Bioelectric organs. Do not stand in water.\nFine print: do not stand anywhere, honestly." },
        new BestiaryEntry { id = "AcidBeetle", name = "Vitriol Beetle", text = "Corrosive payload. Do not stand in the puddle.\nFine print: the puddle is Citizens' Tap Water + time." },
        new BestiaryEntry { id = "SiegeBeetle", name = "Siege Beetle", text = "Mobile containment failure. Bring bigger rounds.\nFine print: requisition forms for bigger rounds are denied." },
        new BestiaryEntry { id = "BroodMother", name = "Brood Mother", text = "Do not negotiate. She will offer tea.\nFine print: Incident Report 7G-Bravo. [UNRECOVERED]." },
        new BestiaryEntry { id = "Queen", name = "The Queen", text = "German Shepherd-sized. Thermal-reactive mandibles.\nFine print: the crown is SOLDER. Somebody built her." },
    };

    private static readonly string[] tips = {
        "TIP: Larvae become roaches. Kill them first.",
        "TIP: Beetles are armored. Flames and poison ignore armor.",
        "TIP: Wasps keep their distance. So should you.",
        "TIP: Standing still as the Firebug is a choice. A burning one.",
        "TIP: Marked kills feed the hive. Unmarked kills feed nothing.",
        "TIP: The mop cannot hurt what is inside your guard. Back off.",
        "TIP: Foam slows you down. Damage speeds you back up.",
        "TIP: Heat is a resource until it is a lawsuit.",
        "TIP: A full charge pierces walls. So do wasp stingers. Rude.",
        "TIP: Contractor #0446 did not complete their contract.",
        "TIP: VitaJuice restocks on Tuesdays. Do not drink the green one.",
        "TIP: Heavy's signing bonus was $200. Their equipment debt is $47,000.",
        "TIP: Sparky's license is stamped REVOKED.",
        "TIP: Lab analysis showed V-7 compounds in Queen B's honey samples.",
        "TIP: Break rooms have paperwork. Paperwork has answers. Probably.",
    };

    private static readonly string[] sectorNames = {
        "FORMER RESIDENCE - LOT 14", "KITCHEN - MEAL UNFINISHED", "CHILDREN'S ROOM - EVACUATED",
        "GARAGE - ENGINE STILL RUNNING", "BACKYARD - DOGHOUSE (MASSIVE)", "NURSERY (EVACUATED)",
        "EMPLOYEE HOUSING BLOCK C", "CLEARWATER HIGH - PENSION DISPUTE SITE", "FORMER DAYCARE - ROOM 12",
        "LAUNDROMAT - MACHINES STILL RUNNING", "PARKING STRUCTURE B2", "STORAGE UNIT 114 - DO NOT ENTER",
    };

    private static readonly string[] hiddenNames = {
        "RESTRICTED - DO NOT ENTER", "FORMER NURSERY", "SUBJECT C OBSERVATION", "OMEGA ANNEX (DECOMMISSIONED)",
    };

    private int tipIndex;
    private bool docsDirty;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        GameEvents.OnEnemyKilled += HandleKill;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyKilled -= HandleKill;
    }

    // ---- documents ----
    public static Document? GetDocument(string id)
    {
        foreach (var d in documents) if (d.id == id) return d;
        return null;
    }

    /// <summary>Draw a random unread document. Rare rooms prefer research and Gary letters.</summary>
    public string DrawDocument(bool rare)
    {
        var unread = new List<Document>();
        foreach (var d in documents)
            if (!SaveSystem.Data.unlockedDocs.Contains(d.id)) unread.Add(d);
        if (unread.Count == 0) return "";
        if (rare)
        {
            var preferred = unread.FindAll(d => d.id.StartsWith("research_") || d.id.StartsWith("gary_") || d.id.StartsWith("review_"));
            if (preferred.Count > 0) return preferred[Random.Range(0, preferred.Count)].id;
        }
        return unread[Random.Range(0, unread.Count)].id;
    }

    public void UnlockDocument(string id)
    {
        if (string.IsNullOrEmpty(id) || SaveSystem.Data.unlockedDocs.Contains(id)) return;
        SaveSystem.Data.unlockedDocs.Add(id);
        SaveSystem.Save();
    }

    public void ShowDocument(string id)
    {
        if (string.IsNullOrEmpty(id)) id = DrawDocument(false);
        if (string.IsNullOrEmpty(id)) return;
        UnlockDocument(id);
        Time.timeScale = 0f;
        GameEvents.ChangeGameState(GameState.Event);
        GameEvents.DocumentShown(id);
    }

    // ---- bestiary ----
    private void HandleKill(Enemy enemy)
    {
        if (enemy == null || enemy.EnemyData == null) return;
        string id = enemy.EnemyData.name.Replace("(Clone)", "").Trim();
        // Match by asset name: the EnemyData file name is the stable id
        var list = SaveSystem.Data.bestiary;
        foreach (var e in list)
        {
            if (e.id == id) { e.kills++; return; }
        }
        list.Add(new BestiaryCount { id = id, kills = 1 });
    }

    public struct BestiaryStatus
    {
        public string name;
        public int kills;
        public bool unlocked;
        public string text;
    }

    public List<BestiaryStatus> Status()
    {
        var result = new List<BestiaryStatus>();
        foreach (var b in bestiary)
        {
            int kills = 0;
            foreach (var e in SaveSystem.Data.bestiary) if (e.id == b.id) kills = e.kills;
            result.Add(new BestiaryStatus { name = b.name, kills = kills, unlocked = kills > 0, text = b.text });
        }
        return result;
    }

    // ---- names, tips, glitches ----
    public static string SectorName(int number, bool loreFlavor)
    {
        if (loreFlavor && Random.value < 0.12f) return sectorNames[Random.Range(0, sectorNames.Length)];
        return $"SECTOR {number}";
    }

    public static string HiddenRoomName() => hiddenNames[Random.Range(0, hiddenNames.Length)];

    public static string GlitchTitle(string real)
    {
        string[] glitches = { "SECTOR 4-XQ", "NURSERY (EVACUATED)", "CONTRACTOR #0446", "DO NOT ENTER", "SUBJECT C" };
        return glitches[Random.Range(0, glitches.Length)];
    }

    public string NextTip()
    {
        string tip = tips[tipIndex % tips.Length];
        tipIndex++;
        return tip;
    }
}
