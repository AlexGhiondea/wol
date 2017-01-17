using CommandLine.Attributes;
using CommandLine.Attributes.Advanced;

internal class CommandLineOptions
{
    [ActionArgumentAttribute]
    public CommandLineActionGroup Action { get; set; }

    [RequiredArgumentAttribute(0, "host", "The name of the host")]
    [ArgumentGroupAttribute(nameof(CommandLineActionGroup.add))]
    [ArgumentGroupAttribute(nameof(CommandLineActionGroup.wake))]
    [ArgumentGroupAttribute(nameof(CommandLineActionGroup.remove))]
    
    public string Host { get; set; }

    #region Add-host Action
    [RequiredArgumentAttribute(1, "mac", "The MAC address of the host")]
    [ArgumentGroupAttribute(nameof(CommandLineActionGroup.add))]
    public string MAC { get; set; }

    #endregion
}