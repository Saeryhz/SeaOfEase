using PcapDotNet.Core;
using SeaOfEase.SeaOfThieves.Game;
using System.Collections.Generic;

namespace SeaOfEase.SeaOfThieves.Util
{
    public class Loader
    {
        private List<Listener> Listeners = new List<Listener>();
        private List<LivePacketDevice> EthDevices = new List<LivePacketDevice>();

        public void DefineSOT()
        {
            Listener listener = new Listener { Title = Constants.SeaOfThievesTitle, Executable = Constants.SeaOfThievesExe };
            listener.FindAppPID();
            Listeners.Add(listener);
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
            Listeners[0].NetstatAppPID();
        }

        public void InterceptListener()
        {
            Listeners[0].InterceptPortPackets(EthDevices);
        }
    }
}
