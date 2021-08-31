using System;
using TwinCAT.Ads;
using System.Text;
using System.Linq;

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
                client.Connect(AmsNetId.Local, 110);
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

            //Console.WriteLine("Hex: {0:X}", dataArray);
            Console.WriteLine("Notification as Hex");
            Console.WriteLine("-------------------");
            Console.WriteLine(string.Join(", ", dataArray.Select(b => b.ToString("X2"))));
            Console.WriteLine("");

            // write the date time
            long fileTimeRaw = BitConverter.ToInt64(dataArray, 0);
            Console.WriteLine("Timestamp");
            Console.WriteLine("---------");
            Console.WriteLine(DateTime.FromFileTime(fileTimeRaw));
            Console.WriteLine("");

            // write the control mask
            Console.WriteLine("Control Mask");
            Console.WriteLine("------------");
            Console.WriteLine("{1:G}", dataArray[8], (ControlMask)dataArray[8]);
            Console.WriteLine("");

            // write server port
            ulong serverPort = BitConverter.ToUInt64(dataArray, 9);
            Console.WriteLine("Server Port");
            Console.WriteLine("---------");
            Console.WriteLine(serverPort.ToString());
            Console.WriteLine("");

            // write the message
            Console.WriteLine("Message");
            Console.WriteLine("-------");
            string message = Encoding.ASCII.GetString(dataArray);
            Console.WriteLine(message.Substring(16));
            Console.WriteLine("");


        }
        
    }
}
