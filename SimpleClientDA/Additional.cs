using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

namespace Siemens.Opc.DaClient
{
    class StateRedundancyController
    {
        ManualResetEvent IsMaster;
        public StateRedundancyController(ManualResetEvent myevent)
        {
            IsMaster = myevent;
        }

        public void SetStateMaster()
        {
            this.IsMaster.Set();
        }

        public void SetStateSlave()
        {
            this.IsMaster.Reset();
        }
    }



    
    class dtTools
    {
        public string MachineName = Environment.MachineName;
        public static string GetNowTimestampString()
        {
            return ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
        }

        public static string GetNowString()
        {
            string DateTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");
            return DateTime;
        }


        public static string GetTimeDateFile()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm").Substring(0, 15);
        }

        public static string GetMinuteFileName()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
        }
    }
}