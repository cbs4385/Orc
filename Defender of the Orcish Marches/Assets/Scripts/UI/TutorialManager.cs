using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button playButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicator;
    [SerializeField] private Image illustrationImage;

    private SceneLoader sceneLoader;
    private int currentPage;

    private struct TutorialPage
    {
        public string title;
        public string body;
        public string spriteName;
    }

    private TutorialPage[] pages;

    private void Awake()
    {
        sceneLoader = GetComponent<SceneLoader>();
        if (sceneLoader == null)
            sceneLoader = gameObject.AddComponent<SceneLoader>();

        BuildPages();
        Debug.Log($"[TutorialManager] Initialized with {pages.Length} pages.");
    }

    private void OnEnable()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        if (backButton != null) backButton.onClick.AddListener(OnBack);
        if (playButton != null) playButton.onClick.AddListener(OnPlay);
        if (exitButton != null) exitButton.onClick.AddListener(OnExit);
    }

    private void OnDisable()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
        if (backButton != null) backButton.onClick.RemoveListener(OnBack);
        if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExit);
    }

    private void Start()
    {
        currentPage = 0;
        ShowPage(currentPage);
    }

    private void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Length) return;

        currentPage = index;
        titleText.text = pages[index].title;
        bodyText.text = pages[index].body;
        pageIndicator.text = $"{index + 1} / {pages.Length}";

        // Load page illustration
        if (illustrationImage != null)
        {
            if (!string.IsNullOrEmpty(pages[index].spriteName))
            {
                var sprite = Resources.Load<Sprite>("Tutorial/" + pages[index].spriteName);
                if (sprite != null)
                {
                    illustrationImage.sprite = sprite;
                    illustrationImage.gameObject.SetActive(true);
                    Debug.Log($"[TutorialManager] Loaded illustration: {pages[index].spriteName}");
                }
                else
                {
                    illustrationImage.gameObject.SetActive(false);
                    Debug.LogWarning($"[TutorialManager] Illustration not found: Tutorial/{pages[index].spriteName}");
                }
            }
            else
            {
                illustrationImage.gameObject.SetActive(false);
            }
        }

        bool isFirst = index == 0;
        bool isLast = index == pages.Length - 1;

        backButton.gameObject.SetActive(!isFirst);
        nextButton.gameObject.SetActive(!isLast);
        playButton.gameObject.SetActive(isLast);

        Debug.Log($"[TutorialManager] Showing page {index + 1}/{pages.Length}: {pages[index].title}");
    }

    private void OnNext()
    {
        ShowPage(currentPage + 1);
    }

    private void OnBack()
    {
        ShowPage(currentPage - 1);
    }

    private void OnPlay()
    {
        Debug.Log("[TutorialManager] Play clicked — loading GameScene.");
        sceneLoader.LoadGameScene();
    }

    private void OnExit()
    {
        Debug.Log("[TutorialManager] Exit clicked — returning to MainMenu.");
        sceneLoader.LoadMainMenu();
    }

    /// <summary>Get the display name for a key binding, with bold tags for tutorial text.</summary>
    private static string K(GameAction action)
    {
        if (InputBindingManager.Instance != null)
            return InputBindingManager.Instance.GetKeyboardDisplayName(action);
        // Fallback if InputBindingManager hasn't loaded
        return InputBindingManager.GetActionDisplayName(action);
    }

    private void BuildPages()
    {
        pages = new TutorialPage[]
        {
            new TutorialPage
            {
                title = "Welcome, Commander!",
                spriteName = "tut_overview",
                body = "You have been chosen to defend the Orcish Marches — a frontier stronghold under siege.\n\n" +
                       "Waves of enemies will assault your fortress. Your job: hold the walls, command defenders, " +
                       "and keep the tower standing.\n\n" +
                       "This tutorial will walk you through everything you need to know."
            },
            new TutorialPage
            {
                title = "Your Fortress",
                spriteName = "tut_fortress",
                body = "At the center of the map stands your <b>Tower</b> — if enemies reach it, the game is over.\n\n" +
                       "Surrounding the tower is a square ring of <b>Walls</b> with octagonal towers " +
                       "at each joint. Your menials and defenders can slip through the narrow gaps " +
                       "between towers, but enemies are too large to fit.\n\n" +
                       "Protect the walls. They are your first line of defense."
            },
            new TutorialPage
            {
                title = "Camera Controls",
                spriteName = "tut_topdown",
                body = "Use the <b>Scroll Wheel</b> to zoom in and out across the battlefield.\n\n" +
                       "The camera is centered on your fortress, giving you a full overhead view " +
                       "of the surrounding terrain.\n\n" +
                       "Enemies initially attack from the <b>west</b>, but the spawn arc widens each day — " +
                       "keep watch in all directions."
            },
            new TutorialPage
            {
                title = "The HUD",
                spriteName = "tut_topdown",
                body = "The bar at the top of the screen shows vital information:\n\n" +
                       "  <b>Gold</b> — your currency for upgrades and defenders\n" +
                       "  <b>Menials</b> — idle / total workforce count\n" +
                       "  <b>Defenders</b> — count of each defender type\n" +
                       "  <b>Phase Timer</b> — elapsed game time\n" +
                       "  <b>Enemy Count</b> — remaining / total enemies this day\n" +
                       "  <b>Kills</b> — total enemies slain\n" +
                       "  <b>Day/Night Wheel</b> — shows the current cycle position"
            },
            new TutorialPage
            {
                title = "Day & Night Cycle",
                spriteName = "tut_side",
                body = "The game alternates between <b>Day</b> and <b>Night</b> phases.\n\n" +
                       "<b>Day (Attack Phase)</b>\nEnemies spawn and assault your fortress. " +
                       "Each day the spawn arc widens, threatening more of your walls. " +
                       "Enemies grow stronger with each passing day.\n\n" +
                       "<b>Night (Build Phase)</b>\nOnce all enemies are cleared and the sun sets, " +
                       "a <b>Build Phase</b> begins. Use this time to place new walls, " +
                       "recruit defenders, and prepare for the next assault."
            },
            new TutorialPage
            {
                title = "Build Mode",
                spriteName = "tut_wallgate",
                body = "When the <b>Build Phase</b> begins at sunset, wall placement starts automatically " +
                       "(if you have an engineer and enough gold).\n\n" +
                       "Time slows to <b>10%</b> during build mode — all units and timers run at reduced speed " +
                       "while you plan your defenses.\n\n" +
                       "A green <b>ghost wall</b> follows your cursor. " +
                       "It snaps to existing walls when close enough.\n\n" +
                       $"  <b>Left Click</b> — place a wall (costs gold)\n" +
                       $"  <b>{K(GameAction.RotateWallLeft)} / {K(GameAction.RotateWallRight)}</b> — rotate the ghost wall\n" +
                       $"  <b>{K(GameAction.ToggleBuildMode)}</b> — exit build mode\n" +
                       $"  <b>Right Click / {K(GameAction.ExitBuildMode)}</b> — exit build mode\n\n" +
                       "The ghost turns <b>red</b> when you can't afford the next wall. " +
                       "Build mode ends automatically when you run out of gold."
            },
            new TutorialPage
            {
                title = "Daily Events",
                spriteName = "tut_side",
                body = "Each new day brings a random <b>Daily Event</b> that modifies the rules.\n\n" +
                       "<b>Beneficial:</b> increased loot, stronger defenders, faster menials, or tougher walls.\n\n" +
                       "<b>Detrimental:</b> more enemy spawns, tougher enemies, faster foes, or siege damage.\n\n" +
                       "<b>Mixed:</b> a bonus paired with a drawback — for example, richer loot but more spawns.\n\n" +
                       "Watch for the event announcement at the start of each day and adapt your strategy."
            },
            new TutorialPage
            {
                title = "The Ballista",
                spriteName = "tut_ballista",
                body = "Your primary weapon is the <b>Ballista</b> (scorpio) mounted on the tower.\n\n" +
                       "<b>Left Click</b> on the battlefield to aim and fire at the target location.\n\n" +
                       "You can purchase additional ballistas from the upgrade panel (up to 3 total). " +
                       $"Press <b>{K(GameAction.SwitchBallista)}</b> to switch between them.\n\n" +
                       "On <b>Easy</b> difficulty, red <b>aim lines</b> show your firing direction. " +
                       "On <b>Hard</b> and above, a slight <b>aim wobble</b> adds challenge to every shot.\n\n" +
                       "Use the ballista to pick off dangerous enemies before they reach your walls."
            },
            new TutorialPage
            {
                title = "Walls & Engineers",
                spriteName = "tut_wallgate",
                body = "<b>Walls</b> form a protective ring around your tower. Each segment is flanked by " +
                       "octagonal towers that connect to adjacent walls.\n\n" +
                       "Walls take damage from enemy attacks. When destroyed, enemies pour through the breach.\n\n" +
                       "New walls placed during <b>Build Mode</b> start under construction — " +
                       "an <b>Engineer</b> must walk over and build them before they become solid.\n\n" +
                       "Engineers also automatically <b>repair</b> damaged walls during combat."
            },
            new TutorialPage
            {
                title = "Resources: Gold",
                spriteName = "tut_treasure",
                body = "Gold is your primary resource for purchasing upgrades and recruiting defenders.\n\n" +
                       "When enemies die, they drop <b>Treasure</b> on the ground.\n\n" +
                       "<b>Right-click</b> near treasure to send the nearest idle menial to collect it. " +
                       "They will pick up loot along their path, then return it to the fortress.\n\n" +
                       "In <b>Nightmare</b> mode, right-click dispatches a menial toward your " +
                       "<b>crosshair</b>.\n\n" +
                       "Spend wisely — there is never enough gold for everything."
            },
            new TutorialPage
            {
                title = "Resources: Menials",
                spriteName = "tut_menial",
                body = "<b>Menials</b> are your workforce — <b>right-click</b> near loot to send them out to collect it.\n\n" +
                       "Menials can also be <b>recruited as Defenders</b> through the upgrade panel. " +
                       "Each defender costs <b>2-3 menials</b> who walk to the tower and convert.\n\n" +
                       "The HUD shows <b>idle/total</b> menials. Balance your workforce: " +
                       "too few menials means uncollected gold, too many recruited means no one to gather resources."
            },
            new TutorialPage
            {
                title = "The Upgrade Panel",
                spriteName = "tut_side",
                body = $"Press <b>{K(GameAction.ToggleUpgrades)}</b> to open the <b>Upgrade Panel</b>.\n\n" +
                       "Here you can spend gold (and menials) on:\n" +
                       "  <b>New Defenders</b> — recruit menials into combat roles\n" +
                       "  <b>Ballista Upgrades</b> — more damage, faster fire rate, or additional ballistas\n\n" +
                       $"Keys <b>{K(GameAction.Upgrade1)}-{K(GameAction.Upgrade9)}</b> act as hotkeys for each upgrade button.\n" +
                       $"Press <b>{K(GameAction.ToggleUpgrades)}</b> again or <b>{K(GameAction.OpenMenu)}</b> to close the panel."
            },
            new TutorialPage
            {
                title = "Defenders",
                spriteName = "tut_defenders",
                body = "Defenders are units that fight alongside you. Each type has a unique role:\n\n" +
                       "  <b>Engineer</b> (30g, 2m) — repairs damaged walls, builds new ones\n" +
                       "  <b>Pikeman</b> (40g, 2m) — melee fighter, engages enemies up close\n" +
                       "  <b>Crossbowman</b> (50g, 2m) — ranged attacker, fires from a distance\n" +
                       "  <b>Wizard</b> (100g, 3m) — powerful AoE magic, longest range\n\n" +
                       "Recruit them from the upgrade panel. Costs scale with how many are currently alive. " +
                       "You need at least one <b>Engineer</b> to enter Build Mode.\n\n" +
                       $"Press <b>{K(GameAction.Recall)}</b> or <b>Middle Click</b> to <b>Recall</b> all defenders and menials to the courtyard."
            },
            new TutorialPage
            {
                title = "Enemy Types",
                spriteName = "tut_enemy",
                body = "You will face a variety of enemies:\n\n" +
                       "  <b>Orc Grunt</b> — standard melee attacker\n" +
                       "  <b>Bow Orc</b> — ranged attacker, fires arrows\n" +
                       "  <b>Troll</b> — heavy hitter, smashes walls quickly\n" +
                       "  <b>Suicide Goblin</b> — fast, explodes on contact\n" +
                       "  <b>Cannoneer</b> — long-range siege unit\n" +
                       "  <b>Orc War Boss</b> — massive, devastating; appears around day 10\n\n" +
                       "Enemies that remain when night falls will <b>retreat</b>, but return the next day."
            },
            new TutorialPage
            {
                title = "Refugees",
                spriteName = "tut_refugee",
                body = "<b>Refugees</b> arrive from the wilds outside your fortress.\n\n" +
                       "If they reach the tower, they join as <b>Menials</b> — adding to your workforce.\n\n" +
                       "Some refugees carry <b>Power-ups</b> (shown by a colored glow). " +
                       "These grant your ballista special abilities like <b>Double Shot</b> or <b>Burst Damage</b>.\n\n" +
                       "Protect them from enemies on their way in."
            },
            new TutorialPage
            {
                title = "Vegetation",
                spriteName = "tut_overview",
                body = "<b>Bushes</b> and <b>Trees</b> grow in the wilderness.\n\n" +
                       "Vegetation spreads at <b>night</b>, filling the battlefield over time. " +
                       "Trees block enemy and defender pathing.\n\n" +
                       "Menials will automatically <b>clear</b> vegetation that blocks their path " +
                       "while collecting loot."
            },
            new TutorialPage
            {
                title = "Breach & Game Over",
                spriteName = "tut_breach",
                body = "If a wall segment is destroyed, enemies will <b>breach</b> your defenses " +
                       "and rush toward the tower.\n\n" +
                       "If any enemy reaches the <b>Tower</b>, the game is <b>over</b>.\n\n" +
                       "Keep your walls standing, station defenders near weak points, " +
                       "and use the ballista to thin enemy ranks before they arrive.\n\n" +
                       "When the enemy path is blocked by walls, they will focus their attacks " +
                       "on the weakest wall to try to break through."
            },
            new TutorialPage
            {
                title = "Relics & Boons",
                spriteName = "tut_side",
                body = "At the end of each night (starting after day 1), you are offered a choice of <b>3 Relics</b>.\n\n" +
                       "Relics provide permanent bonuses for the rest of the run. " +
                       "Pick one, or <b>skip</b> if none suit your strategy.\n\n" +
                       "<b>Offensive:</b> Orcish Whetstone, Sharpened Bolts, Battle Fury\n" +
                       "<b>Defensive:</b> Reinforced Mortar, Slowing Wards\n" +
                       "<b>Economy:</b> War Chest, Plunderer's Charm, Refugee's Beacon\n" +
                       "<b>Risk/Reward:</b> Blood Offering, Rapid Reload, Engineer's Toolkit, Orcish Trophy\n\n" +
                       "Many relics have tradeoffs — read carefully before choosing. " +
                       "Relics <b>stack multiplicatively</b>, so the same type of bonus compounds over multiple nights."
            },
            new TutorialPage
            {
                title = "Commander Classes",
                spriteName = "tut_overview",
                body = "Before starting a run, you can select a <b>Commander Class</b> from the main menu.\n\n" +
                       "Each commander changes the rules for the entire run:\n\n" +
                       "  <b>Warden</b> — walls +30% HP, wall cost -20%, but defenders cost +30%\n" +
                       "  <b>Captain</b> — defenders +20% damage, cost -20%, but walls -20% HP\n" +
                       "  <b>Artificer</b> — ballista upgrades -50% cost, +25% damage, but -1 starting menial\n" +
                       "  <b>Merchant</b> — loot +40%, refugees +30% faster, but enemies +15% HP\n\n" +
                       "Or select <b>None</b> to play with default settings. " +
                       "Your choice persists between sessions until you change it."
            },
            new TutorialPage
            {
                title = "Mutators",
                spriteName = "tut_side",
                body = "<b>Mutators</b> are game-modifying rules you can toggle from the main menu.\n\n" +
                       "Each mutator changes the gameplay significantly and applies a <b>score multiplier</b>:\n\n" +
                       "  <b>Blood Tide</b> — +50% enemies, +30% loot (x1.5)\n" +
                       "  <b>Iron March</b> — enemies move 30% faster (x1.3)\n" +
                       "  <b>Glass Fortress</b> — walls half HP, defenders +50% damage (x1.4)\n" +
                       "  <b>Night Terrors</b> — full waves spawn at night (x1.6)\n" +
                       "  <b>Lone Ballista</b> — no defenders, ballista 2x damage (x1.8)\n\n" +
                       "Mutators unlock through <b>achievements</b>. " +
                       "Score multipliers stack (capped at 5.0x). " +
                       "Some mutators are incompatible and auto-disable each other."
            },
            new TutorialPage
            {
                title = "War Trophies & Upgrades",
                spriteName = "tut_treasure",
                body = "Every run earns <b>War Trophies</b> — a permanent currency that persists across runs.\n\n" +
                       "Trophies earned = (days x 2) + (kills / 10) + (boss kills x 5). Minimum 1 per run.\n\n" +
                       "Spend trophies in the main menu <b>UPGRADES</b> panel on permanent bonuses:\n\n" +
                       "  <b>War Coffers</b> — bonus starting gold\n" +
                       "  <b>Forged Tips / Oiled Gears</b> — ballista damage and fire rate\n" +
                       "  <b>Swift Boots</b> — faster menials\n" +
                       "  <b>Reinforced Foundations</b> — wall HP\n" +
                       "  <b>Keen Eye</b> — loot value\n" +
                       "  <b>Volunteer Corps</b> — extra starting menial\n\n" +
                       "Upgrades have multiple levels with scaling costs. Bonuses apply automatically at run start."
            },
            new TutorialPage
            {
                title = "Achievements & Milestones",
                spriteName = "tut_overview",
                body = "The game tracks your progress through two reward systems:\n\n" +
                       "<b>Achievements</b> have Bronze, Silver, and Gold tiers across 5 categories " +
                       "(Survival, Combat, Economy, Defender, Special). " +
                       "Gold-tier achievements <b>unlock mutators</b>.\n\n" +
                       "<b>Milestones</b> are 26 one-time challenges that award <b>War Trophies</b> " +
                       "(3-15 each) on first completion. Examples: survive 10 days on Hard, " +
                       "kill 500 enemies, defeat a boss.\n\n" +
                       "Your <b>Legacy Rank</b> also accumulates over time, providing small permanent " +
                       "bonuses (starting gold, menial speed, ballista damage, and more).\n\n" +
                       "Check your progress from the main menu: ACHIEVEMENTS, STATISTICS, BESTIARY, and LEGACY."
            },
            new TutorialPage
            {
                title = "Nightmare Difficulty",
                spriteName = "tut_ballista",
                body = "On <b>Nightmare</b> difficulty, you experience the battle from the scorpio itself.\n\n" +
                       "The camera switches to a <b>first-person view</b> mounted on the ballista. " +
                       "Move the mouse to aim — the scorpio rotates and tilts to follow your aim.\n\n" +
                       "Bolts fire in a <b>gravity arc</b> and pass through friendly walls, so aim above " +
                       "distant targets. There are no aim guides — trust your instincts.\n\n" +
                       "An <b>atmospheric mist</b> obscures the battlefield beyond scorpio range. " +
                       "Enemies loom out of the fog — watch for movement and fire early.\n\n" +
                       "During <b>Build Mode</b>, the camera switches back to the overhead view " +
                       "for wall placement, then returns to FPS when building ends.\n\n" +
                       "Nightmare uses <b>Hard</b> mode enemy stats with aim wobble. " +
                       $"Press <b>{K(GameAction.SwitchBallista)}</b> to switch between ballistas. " +
                       "<b>Right-click</b> dispatches menials toward your crosshair."
            },
            new TutorialPage
            {
                title = "Ready to Fight!",
                spriteName = "tut_overview",
                body = "<b>Hotkey Summary:</b>\n\n" +
                       "  <b>Scroll Wheel</b> — Zoom in/out\n" +
                       "  <b>Left Click</b> — Fire ballista (or place wall in build mode)\n" +
                       "  <b>Right Click</b> — Send menial to collect loot\n" +
                       $"  <b>{K(GameAction.Recall)} / Middle Click</b> — Recall all defenders and menials\n" +
                       $"  <b>{K(GameAction.SwitchBallista)}</b> — Switch active ballista\n" +
                       $"  <b>{K(GameAction.ToggleUpgrades)}</b> — Open/close upgrade panel\n" +
                       $"  <b>{K(GameAction.Upgrade1)}-{K(GameAction.Upgrade9)}</b> — Upgrade hotkeys\n" +
                       $"  <b>{K(GameAction.RotateWallLeft)} / {K(GameAction.RotateWallRight)}</b> — Rotate wall in build mode\n" +
                       $"  <b>{K(GameAction.ToggleBuildMode)}</b> — Enter/exit build mode\n" +
                       $"  <b>{K(GameAction.Pause)}</b> — Toggle pause\n" +
                       $"  <b>{K(GameAction.OpenMenu)}</b> — Pause menu / exit build mode\n\n" +
                       "All controls can be rebound from <b>Options > Input Bindings</b>.\n\n" +
                       "<b>Main Menu Features:</b>\n" +
                       "  COMMANDER — choose a class before starting\n" +
                       "  MUTATORS — toggle game modifiers\n" +
                       "  UPGRADES — spend War Trophies on permanent bonuses\n" +
                       "  BESTIARY / STATISTICS / ACHIEVEMENTS / LEGACY\n\n" +
                       "Good luck, Commander. The Marches depend on you!"
            }
        };
    }
}
