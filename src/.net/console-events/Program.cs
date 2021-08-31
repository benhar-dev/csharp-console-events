using System;
using TwinCAT.Ads;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

[Flags]
public enum ControlMask
{
    ADSLOG_MSGTYPE_HINT = 1,
    ADSLOG_MSGTYPE_WARN = 2,
    ADSLOG_MSGTYPE_ERROR = 4,
    ADSLOG_MSGTYPE_LOG = 6,
    ADSLOG_MSGTYPE_MSGBOX = 32,
    ADSLOG_MSGTYPE_RESOURCE = 64,
    ADSLOG_MSGTYPE_STRING = 128
}

[StructLayout(LayoutKind.Explicit, Size = 36, Pack = 1)]
public struct AdsLogNotificationHeader
{
    [MarshalAs(UnmanagedType.U8)]
    [FieldOffset(0)]
    public Int64 nTimeStamp;

    [MarshalAs(UnmanagedType.U4)]
    [FieldOffset(8)]
    public ControlMask dwMsgCtrl;

    [MarshalAs(UnmanagedType.U4)]
    [FieldOffset(12)]
    public uint nServerPort;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
    [FieldOffset(16)]
    public string sDeviceName;

    [MarshalAs(UnmanagedType.U4)]
    [FieldOffset(32)]
    public uint cbMsgSize;

}

namespace console_events
{
    class Program
    {

        static void Main(string[] args)
        {

            RegisterNotifications();

        }

        static void RegisterNotifications()
        {
            using (AdsClient client = new AdsClient())
            {
                // Add the Notification event handler
                client.AdsNotification += Client_AdsNotification;

                // Connect to target
                client.Connect(AmsNetId.Local, 100);
                uint notificationHandle = 0;

                try
                {
                    notificationHandle = client.AddDeviceNotification(1,0xFFFF, 512, new NotificationSettings(AdsTransMode.Cyclic, 0, 0), null);
                    do { } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                }
                finally
                {
                    client.DeleteDeviceNotification(notificationHandle);
                    client.AdsNotification -= Client_AdsNotification;
                }
            }
        }

        static void Client_AdsNotification(object sender, AdsNotificationEventArgs e)
        {

            Console.Clear();

            var dataArray = e.Data.Span.ToArray();

            Console.WriteLine("Notification as Hex");
            Console.WriteLine("-------------------");
            Console.WriteLine(string.Join(", ", dataArray.Select(b => b.ToString("X2"))));
            Console.WriteLine("");

            AdsLogNotificationHeader header = ByteArrayToAdsLogNotificationHeader(dataArray);

            Console.WriteLine("Timestamp");
            Console.WriteLine(DateTime.FromFileTime(header.nTimeStamp));
            Console.WriteLine("");

            Console.WriteLine("Control mask");
            Console.WriteLine("{0:G}", header.dwMsgCtrl);
            Console.WriteLine("");

            Console.WriteLine("Server port");
            Console.WriteLine(header.nServerPort.ToString());
            Console.WriteLine("");

            Console.WriteLine("Device name");
            Console.WriteLine(header.sDeviceName);
            Console.WriteLine("");

            Console.WriteLine("Message size");
            Console.WriteLine(header.cbMsgSize.ToString());
            Console.WriteLine("");

            string message = ByteArrayToAdsLogNotificationMessage(dataArray);

            Console.WriteLine("Message");   
            Console.WriteLine(message);
            Console.WriteLine("");

        }

        static AdsLogNotificationHeader ByteArrayToAdsLogNotificationHeader(byte[] bytes)
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            AdsLogNotificationHeader adsLogNotificationHeader;

            try
            {
                adsLogNotificationHeader = (AdsLogNotificationHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(AdsLogNotificationHeader));
            }
            finally
            {
                handle.Free();
            }

            return adsLogNotificationHeader;
        }

        static string ByteArrayToAdsLogNotificationMessage(byte[] bytes)
        {
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(AdsLogNotificationHeader));
            return Encoding.ASCII.GetString(bytes).Substring(size);
        }

    }
}
