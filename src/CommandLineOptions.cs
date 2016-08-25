using System;
using OutputColorizer;

internal class CommandLineOptions
{
    // we support the following:
    // wol <address>
    // wol -host <alias>
    //    the <alias> will be retrieved from a text file next to the exe 
    //    that has the format: host=MAC

    public string HostNameOrMAC { get; private set; }
    public bool IsHost { get; private set; }

    public static CommandLineOptions Parse(string[] args)
    {
        CommandLineOptions options = new CommandLineOptions();
        if (args.Length == 1)
        {
            options.HostNameOrMAC = args[0];
        }
        else if (args.Length == 2)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(args[0], "-host"))
            {
                options.HostNameOrMAC = args[1];
                options.IsHost = true;
            }
        }
        else
        {
            Colorizer.WriteLine("[Red!Error:] Invalid syntax");
            //invalid syntax
            Colorizer.WriteLine("Usage:");
            Colorizer.WriteLine("  wol [Cyan!MAC]");
            Colorizer.WriteLine("  wol [Cyan!-host <hostname>]");
            Colorizer.WriteLine("      [Cyan!<hostname>] retrieved from hosts.txt next to wol executable");
            Colorizer.WriteLine("      Syntax for hosts.txt (one per line): host=MAC");
            return null;
        }

        return options;
    }

    private CommandLineOptions()
    {
        HostNameOrMAC = string.Empty;
        IsHost = false;
    }
}