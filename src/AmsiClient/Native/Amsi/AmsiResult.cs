namespace AmsiClient.Native;

internal enum AmsiResult
{
    /// <summary>
    /// Known good. No detection found, and the result is likely not going to change after a future definition update.
    /// </summary>
    AMSI_RESULT_CLEAN,

    /// <summary>
    /// No detection found, but the result might change after a future definition update.
    /// </summary>
    AMSI_RESULT_NOT_DETECTED,

    /// <summary>
    /// Administrator policy blocked this content on this machine (beginning of range).
    /// </summary>
    AMSI_RESULT_BLOCKED_BY_ADMIN_START = 0x4000,

    /// <summary>
    /// Administrator policy blocked this content on this machine (end of range).
    /// </summary>
    AMSI_RESULT_BLOCKED_BY_ADMIN_END = 0x4fff,

    /// <summary>
    /// Detection found. The content is considered malware and should be blocked.
    /// </summary>
    AMSI_RESULT_DETECTED = 0x8000,
}
