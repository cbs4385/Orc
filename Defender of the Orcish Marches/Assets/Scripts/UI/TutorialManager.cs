using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static LocalizationManager;

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
        public string titleKey;
        public string bodyKey;
        public string spriteName;
    }

    private TutorialPage[] pages;

    /// <summary>Maps {K:TokenName} strings to GameAction enum values for key binding resolution.</summary>
    private static readonly Dictionary<string, GameAction> KeyTokenMap = new Dictionary<string, GameAction>
    {
        { "RotateWallLeft",  GameAction.RotateWallLeft },
        { "RotateWallRight", GameAction.RotateWallRight },
        { "ToggleBuildMode", GameAction.ToggleBuildMode },
        { "ExitBuildMode",   GameAction.ExitBuildMode },
        { "SwitchBallista",  GameAction.SwitchBallista },
        { "ToggleUpgrades",  GameAction.ToggleUpgrades },
        { "Recall",          GameAction.Recall },
        { "OpenMenu",        GameAction.OpenMenu },
        { "Pause",           GameAction.Pause },
        { "UpgradeSlot1",    GameAction.Upgrade1 },
        { "UpgradeSlot2",    GameAction.Upgrade2 },
        { "UpgradeSlot3",    GameAction.Upgrade3 },
        { "UpgradeSlot4",    GameAction.Upgrade4 },
        { "UpgradeSlot5",    GameAction.Upgrade5 },
        { "UpgradeSlot6",    GameAction.Upgrade6 },
        { "UpgradeSlot7",    GameAction.Upgrade7 },
        { "UpgradeSlot8",    GameAction.Upgrade8 },
        { "UpgradeSlot9",    GameAction.Upgrade9 },
    };

    private static readonly Regex KeyBindingPattern = new Regex(@"\{K:(\w+)\}", RegexOptions.Compiled);

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
        LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
    }

    private void Start()
    {
        currentPage = 0;
        RefreshButtonLabels();
        ShowPage(currentPage);
        LocalizationManager.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        RefreshButtonLabels();
        ShowPage(currentPage);
    }

    private void RefreshButtonLabels()
    {
        SetButtonLabel(nextButton, L("tutorial.btn.next"));
        SetButtonLabel(backButton, L("tutorial.btn.back"));
        SetButtonLabel(playButton, L("tutorial.btn.play"));
        SetButtonLabel(exitButton, L("tutorial.btn.exit"));
    }

    private void SetButtonLabel(Button btn, string text)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    private void ShowPage(int index)
    {
        if (index < 0 || index >= pages.Length) return;

        currentPage = index;
        titleText.text = L(pages[index].titleKey);
        bodyText.text = ResolveKeyBindings(L(pages[index].bodyKey));
        pageIndicator.text = L("tutorial.page_indicator", index + 1, pages.Length);

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

        Debug.Log($"[TutorialManager] Showing page {index + 1}/{pages.Length}: {L(pages[index].titleKey)}");
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

    /// <summary>
    /// Replaces {K:ActionName} tokens in the localized string with the current key binding display name.
    /// For example, {K:SwitchBallista} becomes "Tab" (or whatever the player has rebound it to).
    /// </summary>
    private static string ResolveKeyBindings(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        return KeyBindingPattern.Replace(text, match =>
        {
            string tokenName = match.Groups[1].Value;
            if (KeyTokenMap.TryGetValue(tokenName, out GameAction action))
                return K(action);

            Debug.LogWarning($"[TutorialManager] Unknown key binding token: {{K:{tokenName}}}");
            return match.Value;
        });
    }

    private void BuildPages()
    {
        pages = new TutorialPage[]
        {
            new TutorialPage { titleKey = "tutorial.p1.title",  bodyKey = "tutorial.p1.body",  spriteName = "tut_overview" },
            new TutorialPage { titleKey = "tutorial.p2.title",  bodyKey = "tutorial.p2.body",  spriteName = "tut_fortress" },
            new TutorialPage { titleKey = "tutorial.p3.title",  bodyKey = "tutorial.p3.body",  spriteName = "tut_topdown" },
            new TutorialPage { titleKey = "tutorial.p4.title",  bodyKey = "tutorial.p4.body",  spriteName = "tut_topdown" },
            new TutorialPage { titleKey = "tutorial.p5.title",  bodyKey = "tutorial.p5.body",  spriteName = "tut_side" },
            new TutorialPage { titleKey = "tutorial.p6.title",  bodyKey = "tutorial.p6.body",  spriteName = "tut_wallgate" },
            new TutorialPage { titleKey = "tutorial.p7.title",  bodyKey = "tutorial.p7.body",  spriteName = "tut_side" },
            new TutorialPage { titleKey = "tutorial.p8.title",  bodyKey = "tutorial.p8.body",  spriteName = "tut_ballista" },
            new TutorialPage { titleKey = "tutorial.p9.title",  bodyKey = "tutorial.p9.body",  spriteName = "tut_wallgate" },
            new TutorialPage { titleKey = "tutorial.p10.title", bodyKey = "tutorial.p10.body", spriteName = "tut_treasure" },
            new TutorialPage { titleKey = "tutorial.p11.title", bodyKey = "tutorial.p11.body", spriteName = "tut_menial" },
            new TutorialPage { titleKey = "tutorial.p12.title", bodyKey = "tutorial.p12.body", spriteName = "tut_side" },
            new TutorialPage { titleKey = "tutorial.p13.title", bodyKey = "tutorial.p13.body", spriteName = "tut_defenders" },
            new TutorialPage { titleKey = "tutorial.p14.title", bodyKey = "tutorial.p14.body", spriteName = "tut_enemy" },
            new TutorialPage { titleKey = "tutorial.p15.title", bodyKey = "tutorial.p15.body", spriteName = "tut_refugee" },
            new TutorialPage { titleKey = "tutorial.p16.title", bodyKey = "tutorial.p16.body", spriteName = "tut_overview" },
            new TutorialPage { titleKey = "tutorial.p17.title", bodyKey = "tutorial.p17.body", spriteName = "tut_breach" },
            new TutorialPage { titleKey = "tutorial.p18.title", bodyKey = "tutorial.p18.body", spriteName = "tut_side" },
            new TutorialPage { titleKey = "tutorial.p19.title", bodyKey = "tutorial.p19.body", spriteName = "tut_overview" },
            new TutorialPage { titleKey = "tutorial.p20.title", bodyKey = "tutorial.p20.body", spriteName = "tut_side" },
            new TutorialPage { titleKey = "tutorial.p21.title", bodyKey = "tutorial.p21.body", spriteName = "tut_treasure" },
            new TutorialPage { titleKey = "tutorial.p22.title", bodyKey = "tutorial.p22.body", spriteName = "tut_overview" },
            new TutorialPage { titleKey = "tutorial.p23.title", bodyKey = "tutorial.p23.body", spriteName = "tut_ballista" },
            new TutorialPage { titleKey = "tutorial.p24.title", bodyKey = "tutorial.p24.body", spriteName = "tut_overview" },
            new TutorialPage { titleKey = "tutorial.p25.title", bodyKey = "tutorial.p25.body", spriteName = "tut_overview" },
        };
    }
}
