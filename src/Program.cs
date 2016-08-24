using System.Net;
using System.Net.Sockets;
using OutputColorizer;

namespace ConsoleApplication
{
    public class Program
    {
        const int UDP_PORT = 9; // for now, hard code the port.
        public static void Main(string[] args)
        {
            byte[] mac;
            if (!TryConvertMACStringToBytes(args[0], out mac))
            {
                return;
            }

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

            Colorizer.WriteLine("About to send magic packet to [Yellow!{0}]...", args[0]);
            using (UdpClient client = new UdpClient())
            {
                client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(IPAddress.Broadcast, UDP_PORT)).Wait();
                Colorizer.WriteLine("[Green!Packet sent!]");
            }
        }

        public static bool TryConvertMACStringToBytes(string MAC, out byte[] macAsBytes)
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
