using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;
using System.Threading;
using System.Text;

namespace Siemens.Opc.DaClient
{
    class Writer2Disk
    {
        public static bool SaveToMinuteFile(List<string> StringList)
        {
            bool result = false;

            using (StreamWriter w = File.AppendText(dtTools.GetMinuteFileName() + ".json"))
            {
                foreach (string xline in StringList)
                {
                    int trycount = 0;
                    while (trycount < 5)
                    {
                        try
                        {
                            w.WriteLine(xline);
                            trycount = 100;
                        }
                        catch (Exception me)
                        {
                            trycount++;
                            using (StreamWriter we = File.AppendText("errors.log"))
                            {
                                try
                                {
                                    we.WriteLine( dtTools.GetNowString() + " ERROR " +trycount.ToString() +" : " +  xline + me.Message);
                                }
                                catch
                                {

                                }
                            }

                        }

                    }

                }
            }
            return result;
       }

       
    }

    class ThreadedDiskWriter
    {
        private Queue ExternalDataChannel;
        private ManualResetEvent StopEvent = new ManualResetEvent(false);
        public ThreadedDiskWriter(Queue datachannel)
        {
            ExternalDataChannel = datachannel;
            Thread t = new Thread(new ThreadStart(this.Execute));
            t.Start();

        }

        public void StopWrite()
        {
            StopEvent.Set();
        }

        private void Execute()
        {
            List<string> translist = new List<string>();
            while (!StopEvent.WaitOne(0))
            {
                if (ExternalDataChannel == null)
                {
                    continue;
                }
                else
                {
                    var zzz = ExternalDataChannel.Count;
                    try
                    {
                        while (zzz > 0)
                        {
                            translist.Add(ExternalDataChannel.Dequeue().ToString());
                            zzz--;
                        }
                        if (translist.Count > 0)
                        {
                            Writer2Disk.SaveToMinuteFile(translist);
                        }
                    }
                    finally
                    {
                        translist.Clear();
                    }
                }
            }
        }
    }
}
