/// <summary>
/// Represents information about a tag in the system.
/// </summary>
public class TagInfo
{
    /// <summary>
    /// Gets or sets the PLC identifier.
    /// </summary>
    /// <value>
    /// The PLC identifier.
    /// </value>
    public int plc_id { get; set; }

    /// <summary>
    /// Gets or sets criteria for comparing record values.
    /// </summary>
    /// <value>
    /// The criteria.
    /// </value>
    public int criteria { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of tags.
    /// </summary>
    /// <value>
    /// The dictionary of tags where the key is the tag name and the value is the tag identifier.
    /// </value>
    public Dictionary<string, int>? tags { get; set; }
}