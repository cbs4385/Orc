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
                       "Surrounding the tower is a ring of <b>Walls</b> that block enemy advance. " +
                       "A single <b>Gate</b> on the east side allows your units to pass through.\n\n" +
                       "Protect the walls. They are your first line of defense."
            },
            new TutorialPage
            {
                title = "Camera Controls",
                spriteName = "tut_topdown",
                body = "Use the <b>Scroll Wheel</b> to zoom in and out across the battlefield.\n\n" +
                       "The camera is centered on your fortress, giving you a full overhead view " +
                       "of the surrounding terrain.\n\n" +
                       "Keep an eye on the western approach — that is where the enemy hordes come from."
            },
            new TutorialPage
            {
                title = "The HUD",
                spriteName = "tut_topdown",
                body = "The bar at the top of the screen shows vital information:\n\n" +
                       "  <b>Gold</b> — your currency for buying upgrades and defenders\n" +
                       "  <b>Menials</b> — your workforce count\n" +
                       "  <b>Phase Timer</b> — time remaining in the current phase\n" +
                       "  <b>Enemy Count</b> — how many foes remain\n" +
                       "  <b>Day/Night Wheel</b> — shows the current cycle position"
            },
            new TutorialPage
            {
                title = "Day & Night Cycle",
                spriteName = "tut_side",
                body = "The game alternates between <b>Day</b> and <b>Night</b> phases.\n\n" +
                       "<b>Day (Attack Phase)</b>\nEnemies spawn from the <b>west</b> and assault your fortress. " +
                       "Each day the spawn arc widens, threatening more of your walls.\n\n" +
                       "<b>Night (Build Phase)</b>\nNo enemies attack. Use this time to buy upgrades, " +
                       "recruit defenders, and repair walls.\n\n" +
                       "The day/night wheel on the HUD shows where you are in the cycle."
            },
            new TutorialPage
            {
                title = "The Ballista",
                spriteName = "tut_ballista",
                body = "Your primary weapon is the <b>Ballista</b> mounted on the tower.\n\n" +
                       "<b>Click anywhere</b> on the battlefield to aim. The ballista will automatically " +
                       "fire projectiles at the target location.\n\n" +
                       "Use it to pick off dangerous enemies before they reach your walls."
            },
            new TutorialPage
            {
                title = "Walls & Gate",
                spriteName = "tut_wallgate",
                body = "<b>Walls</b> form a protective ring around your tower. Enemies must break through " +
                       "them to reach the interior.\n\n" +
                       "A single <b>Gate</b> on the east side lets your menials and refugees pass through. " +
                       "Enemies approach from the west, so the gate is on the safe side — for now.\n\n" +
                       "Walls take damage from enemy attacks. If a wall segment is destroyed, " +
                       "enemies will pour through the breach."
            },
            new TutorialPage
            {
                title = "Resources: Gold",
                spriteName = "tut_treasure",
                body = "Gold is your primary resource for purchasing upgrades and recruiting defenders.\n\n" +
                       "When enemies die, they drop <b>Treasure</b> on the ground.\n\n" +
                       "Your <b>Menials</b> will automatically collect nearby treasure and bring it back, " +
                       "adding gold to your stockpile.\n\n" +
                       "Spend wisely — there is never enough gold for everything."
            },
            new TutorialPage
            {
                title = "Resources: Menials",
                spriteName = "tut_menial",
                body = "<b>Menials</b> are your workforce — they collect loot dropped by slain enemies.\n\n" +
                       "Menials can also be <b>recruited as Defenders</b> through the upgrade panel. " +
                       "Recruiting a defender consumes one menial.\n\n" +
                       "Be careful: too few menials means uncollected gold. " +
                       "Too many recruited means no one to gather resources."
            },
            new TutorialPage
            {
                title = "The Upgrade Panel",
                spriteName = "tut_side",
                body = "Press <b>B</b> to open the <b>Upgrade Panel</b>.\n\n" +
                       "Here you can spend gold on:\n" +
                       "  <b>Wall Repairs</b> — restore damaged wall segments\n" +
                       "  <b>New Defenders</b> — recruit menials into combat roles\n" +
                       "  <b>Ballista Upgrades</b> — improve your tower weapon\n\n" +
                       "Press <b>B</b> again or <b>Escape</b> to close the panel."
            },
            new TutorialPage
            {
                title = "Defenders",
                spriteName = "tut_defenders",
                body = "Defenders are units that fight alongside you. Each type has a unique role:\n\n" +
                       "  <b>Engineer</b> — repairs damaged walls (range: short)\n" +
                       "  <b>Pikeman</b> — melee fighter, engages enemies up close\n" +
                       "  <b>Crossbowman</b> — ranged attacker, fires from a distance\n" +
                       "  <b>Wizard</b> — powerful AoE magic, longest range\n\n" +
                       "Recruit them from the upgrade panel. Each costs gold and one menial."
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
                       "  <b>Orc War Boss</b> — massive, devastating; appears when the spawn arc reaches a half-circle (day 10)\n\n" +
                       "Each day the enemy spawn arc widens. Adapt your defenses accordingly."
            },
            new TutorialPage
            {
                title = "Refugees",
                spriteName = "tut_refugee",
                body = "<b>Refugees</b> arrive from the wilds outside your fortress.\n\n" +
                       "They will try to reach the <b>East Gate</b>. If they make it inside, " +
                       "they become <b>Menials</b> — adding to your workforce.\n\n" +
                       "Protect them from enemies on their way in. More refugees means more workers " +
                       "and more potential defenders."
            },
            new TutorialPage
            {
                title = "Breach & Game Over",
                spriteName = "tut_breach",
                body = "If a wall segment is destroyed, enemies will <b>breach</b> your defenses " +
                       "and rush toward the tower.\n\n" +
                       "If any enemy reaches the <b>Tower</b>, the game is <b>over</b>.\n\n" +
                       "Keep your walls standing, station defenders near weak points, " +
                       "and use the ballista to thin enemy ranks before they arrive."
            },
            new TutorialPage
            {
                title = "Ready to Fight!",
                spriteName = "tut_overview",
                body = "<b>Hotkey Summary:</b>\n\n" +
                       "  <b>Scroll Wheel</b> — Zoom in/out\n" +
                       "  <b>Left Click</b> — Aim ballista\n" +
                       "  <b>B</b> — Open/close upgrade panel\n" +
                       "  <b>Escape</b> — Pause menu\n" +
                       "  <b>Space</b> — Resume from pause\n\n" +
                       "Good luck, Commander. The Marches depend on you!"
            }
        };
    }
}
