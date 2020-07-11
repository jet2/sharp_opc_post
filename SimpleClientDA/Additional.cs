using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Siemens.Opc.DaClient
{
    public struct TagPair
    {
        public TagPair(int tagid, string tagvalue)
        {
            tagId = tagid;
            tagValue = tagvalue;
        }
        public int tagId  { get; set; }
        public string tagValue { get; set; }
    }

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
        public static string MachineName = Environment.MachineName;

        public static bool OPCServerProcessFound() {
            return (Process.GetProcessesByName(Constants.opc_server_exe).Length > 0);
        }

        public static string GetNowTimestampString()
        {
            return ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).ToString();
        }

        public static string GetNowString()
        {
            string DateTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");
            return DateTime;
        }

        public static int GetNowSeconds()
        {
            return System.DateTime.Now.Second;
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