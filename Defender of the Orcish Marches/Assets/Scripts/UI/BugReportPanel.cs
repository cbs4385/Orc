using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Threading;

public class BugReportPanel : MonoBehaviour
{
    [SerializeField] private BugReportConfig config;

    private Canvas canvas;
    private TMP_InputField commentsInput;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI versionLabel;
    private Button submitButton;
    private Button cancelButton;
    private GameObject sendingOverlay;
    private GameObject dialogPanel;

    private enum SendState { Idle, Sending, Success, Failed }
    // Note: Sending state is used for progress display during multi-part sends
    private volatile SendState sendState = SendState.Idle;
    private volatile string errorMessage;
    private volatile string progressMessage;
    private float autoCloseTimer;

    private const long MAX_ATTACHMENT_BYTES = 24 * 1024 * 1024; // 24MB, safe margin under Gmail's 25MB limit

    public void Show()
    {
        Debug.Log("[BugReportPanel] Showing bug report dialog.");
        if (canvas == null)
        {
            BuildUI();
        }
        canvas.gameObject.SetActive(true);
        sendState = SendState.Idle;
        if (commentsInput != null) commentsInput.text = "";
        if (statusText != null) statusText.text = "";
        if (sendingOverlay != null) sendingOverlay.SetActive(false);
        if (submitButton != null) submitButton.interactable = true;
        autoCloseTimer = 0f;
    }

    public void Hide()
    {
        Debug.Log("[BugReportPanel] Hiding bug report dialog.");
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Show progress updates from background thread
        string progress = progressMessage;
        if (progress != null && sendState == SendState.Sending)
        {
            if (statusText != null)
            {
                statusText.text = progress;
                statusText.color = Color.white;
            }
        }

        switch (sendState)
        {
            case SendState.Success:
                sendState = SendState.Idle;
                if (sendingOverlay != null) sendingOverlay.SetActive(false);
                if (statusText != null)
                {
                    statusText.text = "Bug report sent successfully!";
                    statusText.color = new Color(0.3f, 0.9f, 0.3f);
                }
                Debug.Log("[BugReportPanel] Bug report sent successfully.");
                autoCloseTimer = 2f;
                break;

            case SendState.Failed:
                sendState = SendState.Idle;
                if (sendingOverlay != null) sendingOverlay.SetActive(false);
                if (submitButton != null) submitButton.interactable = true;
                if (statusText != null)
                {
                    statusText.text = $"Failed: {errorMessage}";
                    statusText.color = new Color(0.9f, 0.3f, 0.3f);
                }
                Debug.LogError($"[BugReportPanel] Failed to send bug report: {errorMessage}");
                break;
        }

        if (autoCloseTimer > 0)
        {
            autoCloseTimer -= Time.unscaledDeltaTime;
            if (autoCloseTimer <= 0)
            {
                Hide();
            }
        }
    }

    private void OnSubmitClicked()
    {
        if (config == null)
        {
            Debug.LogError("[BugReportPanel] BugReportConfig is null! Cannot send report.");
            if (statusText != null)
            {
                statusText.text = "Error: No config assigned.";
                statusText.color = new Color(0.9f, 0.3f, 0.3f);
            }
            return;
        }

        if (string.IsNullOrEmpty(config.senderEmail) || string.IsNullOrEmpty(config.senderPassword))
        {
            Debug.LogError("[BugReportPanel] SMTP credentials not configured in BugReportConfig asset.");
            if (statusText != null)
            {
                statusText.text = "Error: SMTP credentials not configured.";
                statusText.color = new Color(0.9f, 0.3f, 0.3f);
            }
            return;
        }

        Debug.Log("[BugReportPanel] Submitting bug report...");
        if (submitButton != null) submitButton.interactable = false;
        if (sendingOverlay != null) sendingOverlay.SetActive(true);
        if (statusText != null)
        {
            statusText.text = "Sending...";
            statusText.color = Color.white;
        }

        // Dump full game state snapshot into the log before flushing
        if (GameManager.Instance != null)
            GameManager.Instance.LogGameSnapshot();

        // Capture values on main thread (PlayerPrefs not thread-safe)
        string logPath = LogCapture.Instance != null ? LogCapture.Instance.FlushAndGetPath() : null;
        string comments = commentsInput != null ? commentsInput.text : "";
        string difficultyName = GameSettings.GetDifficultyName();
        string version = Application.version;
        string platform = Application.platform.ToString();

        // SMTP config values
        string smtpHost = config.smtpHost;
        int smtpPort = config.smtpPort;
        bool enableSsl = config.enableSsl;
        string senderEmail = config.senderEmail;
        string senderPassword = config.senderPassword;
        string recipientEmail = config.recipientEmail;

        // Create zip on main thread
        var attachmentPaths = new List<string>();
        if (!string.IsNullOrEmpty(logPath) && File.Exists(logPath))
        {
            try
            {
                string zipPath = Path.Combine(Application.persistentDataPath, "bug_report.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(logPath, "game_log.txt");
                }

                long zipSize = new FileInfo(zipPath).Length;
                if (zipSize <= MAX_ATTACHMENT_BYTES)
                {
                    attachmentPaths.Add(zipPath);
                    Debug.Log($"[BugReportPanel] Created zip at {zipPath} ({zipSize} bytes, single part).");
                }
                else
                {
                    // Split into chunks that fit Gmail's attachment limit
                    byte[] zipBytes = File.ReadAllBytes(zipPath);
                    int totalParts = (int)Math.Ceiling((double)zipBytes.Length / MAX_ATTACHMENT_BYTES);
                    for (int i = 0; i < totalParts; i++)
                    {
                        int offset = (int)(i * MAX_ATTACHMENT_BYTES);
                        int length = (int)Math.Min(MAX_ATTACHMENT_BYTES, zipBytes.Length - offset);
                        byte[] partBytes = new byte[length];
                        Buffer.BlockCopy(zipBytes, offset, partBytes, 0, length);
                        string partPath = Path.Combine(Application.persistentDataPath, $"bug_report.zip.{(i + 1):D3}");
                        File.WriteAllBytes(partPath, partBytes);
                        attachmentPaths.Add(partPath);
                    }
                    File.Delete(zipPath);
                    Debug.Log($"[BugReportPanel] Split zip ({zipSize} bytes) into {totalParts} parts.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BugReportPanel] Failed to create zip: {e.Message}");
            }
        }

        string[] capturedPaths = attachmentPaths.ToArray();
        sendState = SendState.Sending;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                int totalEmails = Math.Max(1, capturedPaths.Length);
                string body = $"Bug Report\n" +
                    $"==========\n" +
                    $"Version: {version}\n" +
                    $"Platform: {platform}\n" +
                    $"Difficulty: {difficultyName}\n" +
                    $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                    $"Comments:\n{comments}\n";

                using (var smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.EnableSsl = enableSsl;
                    smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    smtp.Timeout = 15000;

                    for (int i = 0; i < totalEmails; i++)
                    {
                        string subject = $"Bug Report - v{version}";
                        if (totalEmails > 1)
                        {
                            subject += $" (Part {i + 1}/{totalEmails})";
                            progressMessage = $"Sending part {i + 1}/{totalEmails}...";
                        }

                        using (var mail = new MailMessage(senderEmail, recipientEmail, subject, body))
                        {
                            if (i < capturedPaths.Length && File.Exists(capturedPaths[i]))
                            {
                                mail.Attachments.Add(new System.Net.Mail.Attachment(capturedPaths[i]));
                            }
                            smtp.Send(mail);
                        }

                        Debug.Log($"[BugReportPanel] Sent email {i + 1}/{totalEmails}.");
                    }
                }

                sendState = SendState.Success;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                sendState = SendState.Failed;
            }
            finally
            {
                progressMessage = null;
                foreach (var path in capturedPaths)
                {
                    try { if (File.Exists(path)) File.Delete(path); } catch { }
                }
            }
        });
    }

    private void BuildUI()
    {
        // Create overlay canvas at high sorting order
        var canvasObj = new GameObject("BugReportCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Full-screen dim background
        var dimObj = new GameObject("DimBackground");
        dimObj.transform.SetParent(canvasObj.transform, false);
        var dimImg = dimObj.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.7f);
        dimImg.raycastTarget = true;
        var dimRect = dimObj.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        // Dialog panel (center, 700x600)
        var panelObj = new GameObject("DialogPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        dialogPanel = panelObj;
        var panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0.15f, 0.12f, 0.08f, 0.95f);
        var panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700, 600);
        panelRect.anchoredPosition = Vector2.zero;

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "REPORT BUG";
        titleTmp.fontSize = 36;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.9f, 0.75f, 0.3f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 50);
        titleRect.anchoredPosition = new Vector2(0, -15);

        // Version label
        var versionObj = new GameObject("VersionLabel");
        versionObj.transform.SetParent(panelObj.transform, false);
        versionLabel = versionObj.AddComponent<TextMeshProUGUI>();
        versionLabel.text = $"v{Application.version} | {GameSettings.GetDifficultyName()}";
        versionLabel.fontSize = 20;
        versionLabel.color = new Color(0.6f, 0.6f, 0.6f);
        versionLabel.alignment = TextAlignmentOptions.Center;
        var versionRect = versionObj.GetComponent<RectTransform>();
        versionRect.anchorMin = new Vector2(0, 1);
        versionRect.anchorMax = new Vector2(1, 1);
        versionRect.pivot = new Vector2(0.5f, 1);
        versionRect.sizeDelta = new Vector2(0, 30);
        versionRect.anchoredPosition = new Vector2(0, -65);

        // Comments input field
        var inputObj = new GameObject("CommentsInput");
        inputObj.transform.SetParent(panelObj.transform, false);
        var inputImg = inputObj.AddComponent<Image>();
        inputImg.color = new Color(0.1f, 0.08f, 0.05f);
        var inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.05f, 0.25f);
        inputRect.anchorMax = new Vector2(0.95f, 0.82f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        // Text area inside input
        var textAreaObj = new GameObject("Text Area");
        textAreaObj.transform.SetParent(inputObj.transform, false);
        var textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 10);
        textAreaRect.offsetMax = new Vector2(-10, -10);

        // Placeholder
        var placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textAreaObj.transform, false);
        var placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderTmp.text = "Describe the bug... What were you doing? What happened vs what you expected?";
        placeholderTmp.fontSize = 20;
        placeholderTmp.fontStyle = FontStyles.Italic;
        placeholderTmp.color = new Color(0.4f, 0.4f, 0.4f);
        placeholderTmp.alignment = TextAlignmentOptions.TopLeft;
        var placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        // Input text
        var inputTextObj = new GameObject("Text");
        inputTextObj.transform.SetParent(textAreaObj.transform, false);
        var inputTextTmp = inputTextObj.AddComponent<TextMeshProUGUI>();
        inputTextTmp.fontSize = 20;
        inputTextTmp.color = Color.white;
        inputTextTmp.alignment = TextAlignmentOptions.TopLeft;
        var inputTextRect = inputTextObj.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = Vector2.zero;
        inputTextRect.offsetMax = Vector2.zero;

        commentsInput = inputObj.AddComponent<TMP_InputField>();
        commentsInput.textViewport = textAreaRect;
        commentsInput.textComponent = inputTextTmp;
        commentsInput.placeholder = placeholderTmp;
        commentsInput.lineType = TMP_InputField.LineType.MultiLineNewline;
        commentsInput.characterLimit = 2000;

        // Status text
        var statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(panelObj.transform, false);
        statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "";
        statusText.fontSize = 20;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.Center;
        var statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.16f);
        statusRect.anchorMax = new Vector2(1, 0.24f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        // Button row
        var btnRowObj = new GameObject("ButtonRow");
        btnRowObj.transform.SetParent(panelObj.transform, false);
        var btnRowRect = btnRowObj.AddComponent<RectTransform>();
        btnRowRect.anchorMin = new Vector2(0.1f, 0.03f);
        btnRowRect.anchorMax = new Vector2(0.9f, 0.14f);
        btnRowRect.offsetMin = Vector2.zero;
        btnRowRect.offsetMax = Vector2.zero;
        var hlg = btnRowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        // Submit button
        submitButton = CreateDialogButton("SubmitButton", "SUBMIT", btnRowObj.transform);
        submitButton.onClick.AddListener(OnSubmitClicked);

        // Cancel button
        cancelButton = CreateDialogButton("CancelButton", "CANCEL", btnRowObj.transform);
        cancelButton.onClick.AddListener(Hide);

        // Sending overlay
        sendingOverlay = new GameObject("SendingOverlay");
        sendingOverlay.transform.SetParent(panelObj.transform, false);
        var overlayImg = sendingOverlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        overlayImg.raycastTarget = true;
        var overlayRect = sendingOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var sendingTextObj = new GameObject("SendingText");
        sendingTextObj.transform.SetParent(sendingOverlay.transform, false);
        var sendingTmp = sendingTextObj.AddComponent<TextMeshProUGUI>();
        sendingTmp.text = "Sending...";
        sendingTmp.fontSize = 32;
        sendingTmp.fontStyle = FontStyles.Bold;
        sendingTmp.color = new Color(0.9f, 0.75f, 0.3f);
        sendingTmp.alignment = TextAlignmentOptions.Center;
        var sendingRect = sendingTextObj.GetComponent<RectTransform>();
        sendingRect.anchorMin = Vector2.zero;
        sendingRect.anchorMax = Vector2.one;
        sendingRect.offsetMin = Vector2.zero;
        sendingRect.offsetMax = Vector2.zero;

        sendingOverlay.SetActive(false);
        canvas.gameObject.SetActive(false);

        Debug.Log("[BugReportPanel] UI built.");
    }

    private Button CreateDialogButton(string name, string label, Transform parent)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        var btn = btnObj.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = new Color(0.3f, 0.2f, 0.1f, 0.9f);
        colors.highlightedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        colors.pressedColor = new Color(0.6f, 0.4f, 0.1f, 1f);
        colors.selectedColor = new Color(0.5f, 0.35f, 0.15f, 1f);
        btn.colors = colors;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.9f, 0.8f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;

        return btn;
    }
}
