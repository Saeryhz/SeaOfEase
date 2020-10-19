using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using SeaOfEase.SeaOfThieves.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Console = Colorful.Console;

namespace SeaOfEase.SeaOfThieves.Game
{
    public class Listener
    {
        public string Title;
        public string Executable;
        private int PID = -1;

        private List<Port> Ports = new List<Port>();

        PacketCommunicator pm = null;

        public void FindAppPID()
        {
            try
            {
                using (Process p = Process.GetProcessesByName(Executable).Last())
                {
                    PID = p.Id;
                    Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
                    Console.Write("Found Sea of Thieves on [", Color.LightCyan);
                    Console.Write(PID, Color.Cyan);
                    Console.WriteLine("]", Color.LightCyan);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(String.Format("No PID: [{0}] '{1}'", Executable, Title));
            }
        }

        public void NetstatAppPID(string prot = "UDP")
        {
            if (PID < 0)
            {
                PrintAppPIDInvalid();
                return;
            }
            try
            {
                using (Process p = new Process())
                {
                    ProcessStartInfo ps = new ProcessStartInfo();
                    ps.Arguments = "-aonp " + prot;
                    ps.FileName = "netstat.exe";
                    ps.UseShellExecute = false;
                    ps.WindowStyle = ProcessWindowStyle.Hidden;
                    ps.RedirectStandardInput = true;
                    ps.RedirectStandardOutput = true;
                    ps.RedirectStandardError = true;

                    p.StartInfo = ps;
                    p.Start();

                    StreamReader stdOutput = p.StandardOutput;
                    StreamReader stdError = p.StandardError;

                    string rawData = stdOutput.ReadToEnd() + stdError.ReadToEnd();
                    string exitStatus = p.ExitCode.ToString();

                    string line;
                    StringReader reader = new StringReader(rawData);
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains(PID.ToString()))
                        {
                            AddPort(line, prot);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("[{0}] {1}", ex.TargetSite, ex.Message));
            }
        }

        private void AddPort(string line, string prot = "UDP")
        {
            string[] netData = Regex.Split(line, "\\s+");

            if (netData.Length > 4 && (netData[1].Equals("UDP") || netData[1].Equals("TCP")))
            {
                Ports.Add(
                    new Game.Port
                    {
                        Number = netData[2].Split(':')[1].Trim(),
                        PID = PID.ToString(),
                        Executable = Executable,
                        Protocol = netData[1]
                    }
                );
            }
        }

        public void InterceptPortPackets(List<LivePacketDevice> devices)
        {
            if (devices.Count == 0)
            {
                Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
                Console.WriteLine("Eth error", Color.PaleVioletRed);
                return;
            }

            try
            {
                if (Ports.Count != 0)
                {
                    foreach (LivePacketDevice dev in devices)
                    {
                        using (pm = dev.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 500))
                        {
                            string filter = Ports[0].Protocol.ToLower() + " port " + Ports[0].Number;
                            //Console.WriteLine(filter);
                            using (BerkeleyPacketFilter f = pm.CreateFilter(filter))
                            {
                                pm.SetFilter(f);
                            }

                            //Console.Write("Listening on device " + dev.Description);
                            pm.ReceivePackets(0, PrintPacketInfo);
                        }
                    }
                }
                else
                {
                    if (Process.GetProcessesByName(Constants.SeaOfThievesExe).Length > 0)
                    {
                        Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
                        Console.WriteLine("You are not connected to any game server !", Color.PaleVioletRed);
                        Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
                        Console.WriteLine("Press enter to refresh...", Color.GreenYellow);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("[{0}] {1}", ex.TargetSite, ex.Message));
            }
        }

        public void PrintAppPID()
        {
            if (PID < 0)
            {
                PrintAppPIDInvalid();
                return;
            }
            Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
            Console.Write("Found Sea of Thieves on [", Color.LightCyan);
            Console.Write(PID, Color.Cyan);
            Console.WriteLine("]", Color.LightCyan);
        }

        private void PrintAppPIDInvalid()
        {
            Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
            Console.WriteLine("Sea of Thieves is not running !", Color.PaleVioletRed);
            Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
            Console.WriteLine("Press enter to refresh...", Color.GreenYellow);
        }

        private void PrintPacketInfo(Packet packet)
        {
            IpV4Datagram ip = packet.Ethernet.IpV4;
            UdpDatagram udp = ip.Udp;

            if (!ip.Destination.ToString().Contains("192"))
            {
                Console.Write($"[{packet.Timestamp.ToString("H:mm:ss")}] ", Color.LightYellow);
                Console.Write("Connected to game server : ", Color.LightCyan);
                Console.Write(ip.Destination, Color.Cyan);
                Console.Write(":", Color.LightCyan);
                Console.Write(udp.DestinationPort, Color.DarkCyan);

                string[] serverLocation = GeoIP.GetIPLocation(ip.Destination.ToString());
                if (serverLocation != null)
                {
                    Console.WriteLine($" -> ({serverLocation[0]}, {serverLocation[2]})", Color.LightCyan);
                }

                // Avoid infinite loop
                pm.Break();
                Console.Write($"[{DateTime.Now.ToString("H:mm:ss")}] ", Color.LightYellow);
                Console.WriteLine("Press enter to refresh...", Color.GreenYellow);
            }
        }

        private void FilterPortPackets(PacketCommunicator pm, string prot = "UDP")
        {
            string f = prot.ToLower() + " port " + Ports[0].Number;
            Console.WriteLine(f);
            using (BerkeleyPacketFilter filter = pm.CreateFilter(f))
            {
                pm.SetFilter(filter);
            }
            pm.ReceivePackets(5, PrintPacketInfo);
        }

        private void FilterPortPacket(PacketCommunicator pm, string prot = "UDP")
        {
            Packet packet;
            PacketCommunicatorReceiveResult result = pm.ReceivePacket(out packet);

            switch (result)
            {
                case PacketCommunicatorReceiveResult.Ok:
                    Console.WriteLine(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length);
                    break;
                default:
                    throw new InvalidOperationException("The result " + result + " should never be reached here");
            }
        }
    }
}
