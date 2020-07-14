using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Siemens.Opc.Da;
using System.Collections;
using System.Diagnostics;


namespace Siemens.Opc.DaClient
{
    static class Constants
    {
        public const int updaterate = 200;
        public const string MasterStateTagname = "@RM_MASTER";
        public const string opc_server_exe = "CCSsmRTServer";
        public const string opc_server_name = "OPCServer.WinCC.1";
        //public const string opc_server_name = "easyopc.da2.1";
        //public const string opc_server_exe = "easyopc";
        const string serverUrl = "opcda://localhost/OPCServer.WinCC.1";
        public const string serverUrlPrefix = "opcda://localhost/";
        public const string FilesExtension = "csv";

    }

    // class StateRedundancyController вынесен в additional

    class OPCClientCustom
    {

        #region Private Members
        Dictionary<string, string> lastvalues = new Dictionary<string, string>();
        private Server fServer = null;
        private Subscription fSubscription = null;
        public bool Subscribed = false;
        public bool Connected = false;
        private Queue fChannelToDisk = null;
        private Queue fChannelToForm = null;
        private string fProgId = "";
        private int fScanTime = 200;
        private List<string> all_tags= null;
        private bool LocalOPCServerIsMaster = false;
        #endregion

        public OPCClientCustom(Queue DiskWriterChannel, Queue FormChannel, string progid, int scantime)
        {
            this.fChannelToDisk = DiskWriterChannel;
            this.fChannelToForm = FormChannel;
            this.fProgId = progid;
            this.fServer = new Server();
            this.fScanTime = scantime;
        }

        public bool CheckServerExe()
        {
            Process[] localByName = Process.GetProcessesByName(Constants.opc_server_exe);
            return localByName.Length > 0;
        }

        #region Server public methods

        public bool Connect()
        {
            try
            {
                fServer.Connect(Constants.serverUrlPrefix + fProgId);
                Connected = true;
                return true;
            }
            catch (Exception ex)
            {
                Connected = false;
                Disconnect(false);
                using (StreamWriter we = File.AppendText("errors.log"))
                {

                    we.WriteLine(dtTools.GetNowString() + "Connect OPC failed."+ ex.Message);
                }
                return false;
            }
        }

        public int CheckConnect()
        {
            int result = 0;

            try
            {
                bool xDataValue =(bool) fServer.Read(all_tags[0]);
                //string xDataValue = (string)fServer.Read(all_tags[1]);
                //if (xDataValue)
                //{
                    result = 1;
                //}
                if (!this.Connected)
                {
                    using (StreamWriter we = File.AppendText("errors.log"))
                    {

                        we.WriteLine(dtTools.GetNowString() + ": OPC Connected");
                    }
                }

            }
            catch (Exception ex)
            {
                if (this.Connected)
                {

                    using (StreamWriter we = File.AppendText("errors.log"))
                    {
                        this.Connected = false;
                        stopMonitorItems();
                        Disconnect(false);

                        we.WriteLine(dtTools.GetNowString() + ": OPC Disconnected, "+ex.Message);
                    }

                }
                result = 2;
            }
             
            return result;
        }

        #endregion


        #region Connect and Disconnect Server
        /// <summary>
        /// Handles connect procedure
        /// </summary>

        public string startMonitorItems(List<string> tags_list, bool LogErrorsFlag)
        {
            this.all_tags = tags_list;
            string result = "";
            // Check if we have a subscription. If not - create a new subscription.
            if (fSubscription == null)
            {
                try
                {
                    // Create subscription
                    fSubscription = fServer.CreateSubscription("Read", Constants.updaterate, OnDataChange);
                }
                catch (Exception ex)
                {
                    using (StreamWriter we = File.AppendText("errors.log"))
                    {
                        we.WriteLine(dtTools.GetNowString() + ": Unexpected error in the startmonitoritems:\n" + ex.Message);
                    }
                }
            }
            int idx = 0;
            //all_tags.Clear();
            foreach (string element in tags_list)
            {
                //all_tags.Add(element);
                // Add item 1
                try
                {
                    fSubscription.AddItem(
                        element,
                        idx);
                }
                catch
                {
                    result += element + "\n";

                }
                idx++;
            }
            if (result != "")
            {
                if (LogErrorsFlag)
                {
                    using (StreamWriter we = File.AppendText("errors.log"))
                    {
                        we.WriteLine(dtTools.GetNowString() + " Tags not found:\n" + result.Replace("\n", "; "));
                    }
                }
            }

            return result;
        }

        bool stopMonitorItems()
        {
            bool result = false;
            if (fSubscription != null)
            {
                try
                { 
                    try
                    {
                        fServer.DeleteSubscription(fSubscription);

                    }
                    catch (Exception ex)
                    {
                        result = false;
                        using (StreamWriter we = File.AppendText("errors.log"))
                        {
                            we.WriteLine(dtTools.GetNowString() + " Unexpected error in the stopmonitoritems:\n\n" + ex.Message);
                        }
                    }
                }
                finally
                {
                    fSubscription = null;
                }
            }
            return result;
        }

        /// <summary>
        /// Handles disconnect procedure
        /// </summary>
        private void Disconnect(bool freeandnil)
        {

            if (fServer == null)
            {
                return;
            }

            try
            {
                // delete all subscriptions
                stopMonitorItems();

                // Disconnect
                fServer.Disconnect();

                // Cleanup
                if (freeandnil) fServer = null;
            }
            catch (Exception ex)
            {
                using (StreamWriter we = File.AppendText("errors.log"))
                {
                    we.WriteLine(dtTools.GetNowString() + ": Unexpected error in the disconnect:\n\n" + ex.Message);
                }
                //LogText(exception.Message + " Disconnect failed");
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Show shutdown message
        /// When receiving a shutdown message just disconnect.
        /// </summary>
        public void ShutDownRequest(string reason)
        {
            Disconnect(true);
        }
        
        public void CallToLastValues()
        {
            if (this.Connected)
            {
                foreach (var pair in lastvalues)
                {
                    string d_line = dtTools.GetNowString() + ";" + pair.Key + ";" + pair.Value;
                    fChannelToDisk.Enqueue(d_line);
                }
            }
        }

        /// <summary>
        /// callback to receive datachanges
        /// </summary>
        /// <param name="clientHandle"></param>
        /// <param name="value"></param>
        private void OnDataChange(IList<DataValue> DataValues)
        {
            DataValue testvalue= null;
            try
            {
                //// We have to call an invoke method 
                //if (this.InvokeRequired)
                //{
                //    // Asynchronous execution of the valueChanged delegate
                //    this.BeginInvoke(new DataChange(OnDataChange), DataValues);
                //    return;
                //}

                foreach (DataValue value in DataValues)
                {
                    testvalue = value;
                    // 1 is Item1, 2 is Item2, 3 is ItemBlockRead
                    if (value.ClientHandle == 0)
                    {
                        LocalOPCServerIsMaster = (bool)value.Value;
                    }
                    // Print data change information for variable - check first the result code
                    if (value.Error != 0)
                    {
                        
                        // The node failed - print the symbolic name of the status code
                        string e_line = dtTools.GetNowString() + ";" + all_tags[value.ClientHandle] + " Error: 0x" + value.Error.ToString("X");
                        
                        //LogText(e_line);
                        using (StreamWriter we = File.AppendText("errors.log"))
                        {
                            we.WriteLine(e_line);
                        }

                    }
                    else
                    {
                        string d_line = dtTools.GetNowString() + ";" + all_tags[value.ClientHandle] + ";" + value.Value.ToString() + ";" + value.Quality.ToString()+";"+ LocalOPCServerIsMaster.ToString();
                        // The node succeeded - print the value as string
                        lastvalues[all_tags[value.ClientHandle]] = value.Value.ToString() + ";" + value.Quality.ToString() + ";" + LocalOPCServerIsMaster.ToString();
                        fChannelToDisk.Enqueue(d_line);
                        if (this.fChannelToForm != null)
                        {
                            fChannelToForm.Enqueue(new TagPair(value.ClientHandle, value.Value.ToString()));
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                using (StreamWriter we = File.AppendText("errors.log"))
                {
                    
                    
                    we.WriteLine(dtTools.GetNowString() +" Item["+ testvalue.ClientHandle.ToString() + "] ["+ all_tags[testvalue.ClientHandle] + "] Unexpected error in the data change callback:\n\n" + ex.Message);
                }
            }
        }
        #endregion



    }
}
