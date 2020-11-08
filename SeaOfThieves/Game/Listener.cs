using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using SeaOfEase.SeaOfThieves.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private string PlayerName;

        PacketCommunicator pm = null;

        public void FindAppPID()
        {
            try
            {
                using (Process p = Process.GetProcessesByName(Executable).Last())
                {
                    PID = p.Id;
                    ConsoleMsg.Write($"Found Sea of Thieves [{PID}]", ConsoleMsg.Type.Info);
                }

                if (!String.IsNullOrEmpty(Constants.DiscordWebHook))
                {
                    ConsoleMsg.Write("Type your nickname :", ConsoleMsg.Type.Info);
                    PlayerName = Console.ReadLine();
                }
            }
            catch (Exception)
            {
                //Console.WriteLine(String.Format("No PID: [{0}] '{1}'", Executable, Title));
            }
        }

        public void NetstatAppPID()
        {
            string protocol = "UDP";
            if (PID < 0)
            {
                ConsoleMsg.Write("Sea of Thieves is not running !", ConsoleMsg.Type.Error, true);
                return;
            }
            try
            {
                using (Process p = new Process())
                {
                    ProcessStartInfo ps = new ProcessStartInfo();
                    ps.Arguments = "-aonp " + protocol;
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
                            AddPort(line, protocol);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleMsg.Write(ex.TargetSite + " " + ex.Message, ConsoleMsg.Type.Error);
            }
        }

        private void AddPort(string line, string prot = "UDP")
        {
            string[] netData = Regex.Split(line, "\\s+");

            if (netData.Length > 4 && (netData[1].Equals("UDP") || netData[1].Equals("TCP")))
            {
                Ports.Add(
                    new Port
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
                ConsoleMsg.Write("Eth error", ConsoleMsg.Type.Error);
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
                            using (BerkeleyPacketFilter f = pm.CreateFilter(filter))
                            {
                                pm.SetFilter(f);
                            }

                            pm.ReceivePackets(0, PrintPacketInfo);
                        }
                    }
                }
                else
                {
                    if (Process.GetProcessesByName(Constants.SeaOfThievesExe).Length > 0)
                    {
                        ConsoleMsg.Write("You are not connected to any game server !", ConsoleMsg.Type.Error, true);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleMsg.Write(ex.TargetSite + " " + ex.Message, ConsoleMsg.Type.Error);
            }
        }

        public void PrintAppPID()
        {
            if (PID < 0)
            {
                ConsoleMsg.Write("Sea of Thieves is not running !", ConsoleMsg.Type.Error, true);
            }
            else
            {
                ConsoleMsg.Write($"Found Sea of Thieves [{PID}]", ConsoleMsg.Type.Info);
            }
        }

        private void PrintPacketInfo(Packet packet)
        {
            IpV4Datagram ip = packet.Ethernet.IpV4;
            UdpDatagram udp = ip.Udp;

            if (!ip.Destination.ToString().Contains("192"))
            {
                string server = ip.Destination + ":" + udp.DestinationPort;
                ConsoleMsg.Write($"Connected to game server : {server}", ConsoleMsg.Type.Info, true);
                if (!String.IsNullOrEmpty(Constants.DiscordWebHook))
                {
                    Webhook.SendMessage(PlayerName, server);
                }
                pm.Break();
            }
        }
    }
}
