using UnityEngine;

[CreateAssetMenu(fileName = "BugReportConfig", menuName = "Game/Bug Report Config")]
public class BugReportConfig : ScriptableObject
{
    [Header("SMTP Settings")]
    public string smtpHost = "smtp.gmail.com";
    public int smtpPort = 587;
    public bool enableSsl = true;

    [Header("Credentials")]
    [Tooltip("Gmail address to send from")]
    public string senderEmail;
    [Tooltip("Gmail App Password (16 chars, requires 2FA enabled)")]
    public string senderPassword;

    [Header("Recipient")]
    public string recipientEmail = "chris.strube@gmail.com";
}
