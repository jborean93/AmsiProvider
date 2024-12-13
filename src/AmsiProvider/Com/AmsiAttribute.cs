namespace AmsiProvider.Com;

internal enum AmsiAttribute
{
    /// <summary>
    /// LPWSTR, Name/version/GUID string of the calling application.
    /// </summary>
    AMSI_ATTRIBUTE_APP_NAME,

    /// <summary>
    /// LPWSTR, filename, URL, script unique id etc.
    /// </summary>
    AMSI_ATTRIBUTE_CONTENT_NAME,

    /// <summary>
    /// ULONGLONG, size of the input. Mandatory.
    /// </summary>
    AMSI_ATTRIBUTE_CONTENT_SIZE,

    /// <summary>
    /// PVOID, memory address if content is fully loaded in memory. Mandatory unless
    /// Read() is implemented instead to support on-demand content retrieval.
    /// </summary>
    AMSI_ATTRIBUTE_CONTENT_ADDRESS,

    /// <summary>
    /// PVOID, session is used to associate different scan calls, e.g. if the contents
    /// to be scanned belong to the sample original script. Return nullptr if content
    /// is self-contained. Mandatory.
    /// </summary>
    AMSI_ATTRIBUTE_SESSION,

    /// <summary>
    /// ULONGLONG, size of the Microsoft Edge redirect chain. Optional.
    /// </summary>
    AMSI_ATTRIBUTE_REDIRECT_CHAIN_SIZE,

    /// <summary>
    /// PVOID, memory address of the Microsoft Edge redirect chain. Optional.
    /// </summary>
    AMSI_ATTRIBUTE_REDIRECT_CHAIN_ADDRESS,

    // "All Attribute" buffer is provided by Microsoft Edge to pass future attributes without
    // requiring adding new attributes to the amsi interface. It is a multi-string with the following
    // format:
    //   L"Attribute1\0Value1\0Attribute2\0Value2\0...AttributeN\0ValueN\0\0"

    /// <summary>
    /// ULONGLONG, size of the "All Attribute" Microsoft Edge buffer. Optional. 
    /// </summary>
    AMSI_ATTRIBUTE_ALL_SIZE,

    /// <summary>
    /// PVOID, memory address of the "All Attribute" Microsoft Edge buffer. Optional.
    /// </summary>
    AMSI_ATTRIBUTE_ALL_ADDRESS,

    /// <summary>
    /// ULONG deprecated, do not use
    /// </summary>
    AMSI_ATTRIBUTE_QUIET,
}
