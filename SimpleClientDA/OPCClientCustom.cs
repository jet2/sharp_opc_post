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
        //public const string opc_server_exe = "CCSsmRTServer";

        //public const string opc_server_name = "OPCServer.WinCC.1";
        public const string opc_server_name = "easyopc.da2.1";
        public const string opc_server_exe = "easyopc";
        const string serverUrl = "opcda://localhost/OPCServer.WinCC.1";
        public const string serverUrlPrefix = "opcda://localhost/";
        

    }

    // class StateRedundancyController вынесен в additional

    class OPCClientCustom
    {
        #region Private Members
        private Server fServer = null;
        private Subscription fSubscription = null;
        public bool Subscribed = false;
        public bool Connected = false;
        private Queue fChannelToDisk = null;
        private Queue fChannelToForm = null;
        private string fProgId = "";
        private int fScanTime = 200;
        private List<string> all_tags= null;
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
            catch
            {
                Connected = false;
                
                return false;
            }
        }

        public int CheckConnect()
        {
            int result = 0;

            try
            {
                DataValue xDataValue = (DataValue) fServer.Read(all_tags[0]);
                if ((bool)xDataValue.Value)
                {
                    result = 1;
                }
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
                        we.WriteLine(dtTools.GetNowString() + ": OPC Disconnected");
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

        public string startMonitorItems(List<string> tags_list)
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
                using (StreamWriter we = File.AppendText("errors.log"))
                {
                    we.WriteLine(dtTools.GetNowString() + " Tags not found:\n" + result.Replace("\n", "; "));
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
                    fServer.DeleteSubscription(fSubscription);
                    fSubscription = null;
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
            return result;
        }

        /// <summary>
        /// Handles disconnect procedure
        /// </summary>
        private void Disconnect()
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
                fServer = null;
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
            Disconnect();
        }

        /// <summary>
        /// callback to receive datachanges
        /// </summary>
        /// <param name="clientHandle"></param>
        /// <param name="value"></param>
        private void OnDataChange(IList<DataValue> DataValues)
        {
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
                    // 1 is Item1, 2 is Item2, 3 is ItemBlockRead

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
                        string d_line = dtTools.GetNowString() + ";" + all_tags[value.ClientHandle] + ";" + value.Value.ToString() + ";" + value.ToString();
                        // The node succeeded - print the value as string
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
                    we.WriteLine(dtTools.GetNowString() + " Unexpected error in the data change callback:\n\n" + ex.Message);
                }
            }
        }
        #endregion



    }
}
