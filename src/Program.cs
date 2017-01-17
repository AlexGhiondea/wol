using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using OutputColorizer;

namespace ConsoleApplication
{
    public class Program
    {
        const int UDP_PORT = 9; // for now, hard code the port.
        public static void Main(string[] args)
        {
            Colorizer.WriteLine("[Magenta!Wake-On-LAN] Magic Packet Generator, [Cyan!v1.0]");

            System.Console.WriteLine();

            CommandLineOptions opt;
            if (!CommandLine.Parser.TryParse(args, out opt))
            {
                return;
            }

            switch (opt.Action)
            {
                case CommandLineActionGroup.wake:
                    WakeAction(opt);
                    return;
                case CommandLineActionGroup.add:
                    AddAction(opt);
                    return;
                case CommandLineActionGroup.list:
                    ListAction(opt);
                    return;
                case CommandLineActionGroup.remove:
                    RemoveAction(opt);
                    return;
            }
        }

        private static void RemoveAction(CommandLineOptions options)
        {
            if (!File.Exists(HostsFile))
            {
                Colorizer.WriteLine("No custom hosts defined");
                return;
            }

            // make sure we have that host.
            string matchedMAC;
            if (!TryReadHostFromFile(options.Host, out matchedMAC))
            {
                Colorizer.WriteLine("[Red!Error]: Could not find host [Cyan!{0}]", options.Host);
                return;
            }

            StringBuilder sb = new StringBuilder();
            using (FileStream fs = new FileStream(HostsFile, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                string line; int lineCount = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    lineCount++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // check to see if the name of the host matches the name passed in.
                    int posFirstEq = line.IndexOf('=');

                    if (posFirstEq < 0)
                    {
                        Colorizer.WriteLine("[Yellow!Warning:] Invalid entry at line [Cyan!{0}]", lineCount);
                        continue;
                    }

                    string hostFromLine = line.Substring(0, posFirstEq);
                    if (!StringComparer.OrdinalIgnoreCase.Equals(hostFromLine, options.Host))
                    {
                        sb.AppendLine(line);
                    }
                }
            }

            // open the file for writing
            using (FileStream fs = new FileStream(HostsFile, FileMode.Create, FileAccess.Write))
            using (StreamWriter sr = new StreamWriter(fs))
            {
                sr.WriteLine(sb.ToString());
            }

            Colorizer.WriteLine("[Green!Done!] Removed host [Cyan!{0}]", options.Host);
        }

        private static void ListAction(CommandLineOptions options)
        {
            if (!File.Exists(HostsFile))
            {
                Colorizer.WriteLine("No custom hosts defined");
                return;
            }

            Colorizer.WriteLine("These are the custom hosts defined:");
            using (FileStream fs = new FileStream(HostsFile, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                string line; int lineCount = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    lineCount++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // check to see if the name of the host matches the name passed in.
                    int posFirstEq = line.IndexOf('=');

                    if (posFirstEq < 0)
                    {
                        Colorizer.WriteLine("[Yellow!Warning:] Invalid entry at line [Cyan!{0}]", lineCount);
                        continue;
                    }

                    string hostFromLine = line.Substring(0, posFirstEq);
                    string matchedMAC = line.Substring(posFirstEq + 1);

                    Colorizer.WriteLine("[Cyan!{0}] = [Yellow!{1}]", hostFromLine, matchedMAC);
                }
            }
        }

        private static void AddAction(CommandLineOptions options)
        {
            // make sure we don't already have that host.
            string matchedMAC;
            if (TryReadHostFromFile(options.Host, out matchedMAC))
            {
                if (!string.IsNullOrEmpty(matchedMAC))
                {
                    Colorizer.WriteLine("[Red!Error]: Host [Cyan!{0}] already exists with value [Yellow!{1}]", options.Host, matchedMAC);
                    return;
                }
            }

            using (FileStream fs = new FileStream("hosts.txt", FileMode.Append, FileAccess.Write))
            {
                using (StreamWriter sr = new StreamWriter(fs))
                {
                    sr.WriteLine($"{options.Host}={options.MAC}");
                }
            }

            Colorizer.WriteLine("[Green!Done]! Added host [Cyan!{0}] with MAC [Yellow!{1}]", options.Host, options.MAC);
        }

        private static void WakeAction(CommandLineOptions options)
        {
            byte[] mac = new byte[0];
            if (!TryLoadMacForHost(options.Host, out mac))
            {
                // it was not found in the hosts.txt file.
                // try to interpret as MAC
                if (!TryConvertMACStringToBytes(options.Host, out mac))
                {
                    Colorizer.WriteLine("[Red!Error:] Could not find host '[Cyan!{0}]' in hosts.txt", options.Host);
                    return;
                }
            }

            Colorizer.WriteLine("Sending magic packet to '[Magenta!{0}]'...", BitConverter.ToString(mac));
            SendMagicPacket(mac);
        }

        private static void SendMagicPacket(byte[] mac)
        {
            // The magic packet consists of 6 bytes all 0xFF
            // Followed by 16 repetitions of the target MAC 
            byte[] magicPacket = new byte[102];
            int pos = 0;

            // add the first 6 bytes (all FF)
            for (int i = 0; i < 6; i++)
            {
                magicPacket[pos++] = 0xFF; // FF to byte
            }

            // add the MAC 16 times.
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    magicPacket[pos++] = mac[j];
                }
            }

            using (UdpClient client = new UdpClient())
            {
                client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, UDP_PORT)).Wait();
                Colorizer.WriteLine("[Green!Packet sent!]");
            }
        }

        private const string HostsFile = "hosts.txt";

        private static bool TryReadHostFromFile(string hostName, out string matchedMAC)
        {
            matchedMAC = null;

            if (!File.Exists(HostsFile))
            {
                return false;
            }

            using (FileStream fs = new FileStream(HostsFile, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs))
            {
                string line; int lineCount = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    lineCount++;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // check to see if the name of the host matches the name passed in.
                    int posFirstEq = line.IndexOf('=');

                    if (posFirstEq < 0)
                    {
                        Colorizer.WriteLine("[Yellow!Warning:] Invalid entry at line [Cyan!{0}]", lineCount);
                        continue;
                    }

                    string hostFromLine = line.Substring(0, posFirstEq);

                    if (StringComparer.OrdinalIgnoreCase.Equals(hostFromLine, hostName))
                    {
                        matchedMAC = line.Substring(posFirstEq + 1);
                        return true;
                    }
                }
            }
            return false;
        }
        private static bool TryLoadMacForHost(string hostName, out byte[] macAsBytes)
        {
            macAsBytes = null;
            // read the host file and stop at the first one you find.
            string matchedMAC;
            if (TryReadHostFromFile(hostName, out matchedMAC))
            {
                Colorizer.WriteLine("Matched host '[Magenta!{0}]' to '[Magenta!{1}]'", hostName, matchedMAC);

                // return the rest of the line converted to bytes.
                return TryConvertMACStringToBytes(matchedMAC, out macAsBytes);
            }

            return false;
        }

        private static bool TryConvertMACStringToBytes(string MAC, out byte[] macAsBytes)
        {
            int pos = 0;
            macAsBytes = new byte[6];
            byte currentByte = 0;

            bool multipleSeparators = false;
            byte consecutiveDigits = 0;

            for (int j = 0; j < MAC.Length; j++)
            {
                int value = AsciiToHex(MAC[j]);
                if (value != -1)
                {
                    if (consecutiveDigits == 2)
                    {
                        //treat this new value as the start of a new digit.
                        macAsBytes[pos++] = currentByte;
                        currentByte = 0;
                        consecutiveDigits = 0;
                    }

                    // this happens for valid hex digits.
                    // take the previous one and convert it to HEX
                    currentByte = (byte)(currentByte * 16 + value);
                    multipleSeparators = false;
                    consecutiveDigits++;
                }
                else
                {
                    if (multipleSeparators)
                    {
                        Colorizer.WriteLine("[Red!Error:] Invalid MAC address (multiple separators at position {0}).", j);
                        return false;
                    }
                    macAsBytes[pos++] = currentByte;
                    currentByte = 0;
                    multipleSeparators = true; consecutiveDigits = 0;
                }
            }
            // make sure to add the last one as well.
            macAsBytes[pos] = currentByte;

            // At this point we should have 6 bytes and should have seen 12 characters
            if (pos != 5)
            {
                Colorizer.WriteLine("[Red!Error:] Invalid MAC address (incomplete address).");
                return false;
            }

            return true;
        }

        public static int AsciiToHex(char ch)
        {
            if (ch >= '0' && ch <= '9') return (char)(ch - '0');
            if (ch >= 'a' && ch <= 'f') return (char)(10 + (ch - 'a'));
            if (ch >= 'A' && ch <= 'F') return (char)(10 + (ch - 'A'));

            return -1;
        }
    }
}
