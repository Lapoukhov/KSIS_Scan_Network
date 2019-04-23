using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ksis_lab
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowNetworkInterfaces();
            Console.WriteLine("\n\n");
            ScanLocalNetwork();
        }

        public static void ShowNetworkInterfaces()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            Console.WriteLine(computerProperties.HostName);
            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("No network interfaces found.");
                return;
            }

            Console.WriteLine("Number of interfaces  : {0}", nics.Length);
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                Console.WriteLine();
                Console.WriteLine(adapter.Description);
                Console.WriteLine(adapter.Name);
                Console.WriteLine("Interface type  : {0}", adapter.NetworkInterfaceType);
                Console.Write("MAC-address  : ");
                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();
                for (int i = 0; i < bytes.Length; i++)
                {
                    Console.Write("{0}", bytes[i].ToString("X2"));
                    if (i != bytes.Length - 1)
                    {
                        Console.Write("-");
                    }
                }
                Console.WriteLine();
            }
        }

        public static void ScanLocalNetwork()
        {
            IPGlobalProperties localComputer = IPGlobalProperties.GetIPGlobalProperties();
            IPAddress[] ips;
            ips = Dns.GetHostAddresses(localComputer.HostName);
            Console.WriteLine("Computer name: {0}", localComputer.HostName);

            string MyMacAdress = string.Empty;
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    MyMacAdress += nic.GetPhysicalAddress().ToString();
                    break;
                }
            }
            Console.WriteLine("MAC-address: {0}", MyMacAdress);

            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    Console.WriteLine("IP: {0}", ip);
                    string IPStr = ip.ToString();
                    int IP = IpToInt32(IPStr);

                    IPAddress mask = GetSubnetMask(ip);
                    Console.WriteLine("mask: {0}\n", mask);
                    string MASKStr = mask.ToString();
                    int MASK = IpToInt32(MASKStr);
                    //диапазон
                    int minCounter = (IP & MASK) + 1;
                    int maxCounter = minCounter + ~MASK - 1;

                    for (int i = minCounter; i <= maxCounter; i++)
                    {
                        IPAddress testIP = IPAddress.Parse(Int32ToIp(i));
                        Console.WriteLine(testIP);
                        Ping pingSender = new Ping();
                        PingReply reply = pingSender.Send(testIP);

                        if (reply.Status == IPStatus.Success)
                        {
                            Console.WriteLine("Address: {0} replies", reply.Address.ToString());
                            Console.WriteLine("MAC-address: {0}", GetMacAddress(reply.Address.ToString()));
                            Console.WriteLine();
                        }
                    }
                }
            }
        }

        public static IPAddress GetSubnetMask(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        if (address.Equals(unicastIPAddressInformation.Address))
                        {
                            return unicastIPAddressInformation.IPv4Mask;
                        }
                    }
                }
            }
            throw new ArgumentException(string.Format("Can't find subnetmask for IP address '{0}'", address));
        }

        private static int IpToInt32(string ipAddress)
        {
            return BitConverter.ToInt32(IPAddress.Parse(ipAddress).GetAddressBytes().Reverse().ToArray(), 0);
        }

        public static string Int32ToIp(int ipAddress)
        {
            return new IPAddress(BitConverter.GetBytes(ipAddress).Reverse().ToArray()).ToString();
        }

        public static string GetMacAddress(string ipAddress)
        {
            string macAddress = string.Empty;
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = "arp";
            pProcess.StartInfo.Arguments = "-a " + ipAddress;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.Start();
            string strOutput = pProcess.StandardOutput.ReadToEnd();
            string[] substrings = strOutput.Split('-');
            if (substrings.Length >= 8)
            {
                macAddress = substrings[3].Substring(Math.Max(0, substrings[3].Length - 2))
                         + "-" + substrings[4] + "-" + substrings[5] + "-" + substrings[6]
                         + "-" + substrings[7] + "-" + substrings[8].Substring(0, 2);
                return macAddress;
            }
            else
            {
                return "not found";
            }
        }
    }
}