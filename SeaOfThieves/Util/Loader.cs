using PcapDotNet.Core;
using SeaOfEase.SeaOfThieves.Game;
using System.Collections.Generic;

namespace SeaOfEase.SeaOfThieves.Util
{
    public class Loader
    {
        private Listener listener = new Listener();
        private List<LivePacketDevice> EthDevices = new List<LivePacketDevice>();

        public void DefineSOT()
        {
            listener.Executable = Constants.SeaOfThievesExe;
            listener.Title = Constants.SeaOfThievesTitle;
            listener.FindAppPID();
        }

        public void GetEthernetDevice()
        {
            IList<LivePacketDevice> Devices = LivePacketDevice.AllLocalMachine;
            int index = 0;
            int device = 0;

            foreach (LivePacketDevice dev in Devices)
            {
                if (!dev.Description.Contains("Microsoft") && !dev.Description.Contains("Oracle"))
                {
                    //Console.WriteLine(String.Format("[{0}] {1}", index, dev.Description));
                    device = index;
                }
                index++;
            }

            using (PacketCommunicator pm = Devices[device].Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))
            {
                EthDevices.Add(Devices[device]);
            }
        }

        public void NetstatListener()
        {
            listener.NetstatAppPID();
        }

        public void InterceptListener()
        {
            listener.InterceptPortPackets(EthDevices);
        }
    }
}
