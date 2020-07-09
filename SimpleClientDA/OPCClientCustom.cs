using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Siemens.Opc.Da;

namespace Siemens.Opc.DaClient
{
    static class Constants
    {
        public const int updaterate = 200;
        public const string MasterStateTagname = "@RM_STATE";
        public const string opc_server_exe = "CCSsmRTServer";
        public const string opc_server_name = "OPCServer.WinCC.1";
        const string serverUrl = "opcda://localhost/OPCServer.WinCC.1";
        const string serverUrlPrefix = "opcda://localhost/";
        

    }

    // class StateRedundancyController вынесен в additional

    class OPCClientCustom
    {
        #region Private Members
        private Server m_Server = null;
        private Subscription m_Subscription = null;
        private bool subscribed = false;
        #endregion

        #region Server public methods

        public bool Connect(string progid)
        {
            m_Server.Connect(progid);
            return true;
        }

        public int CheckConnect()
        {
            int result = 0;

            try
            {
                DataValue xDataValue = (DataValue)m_Server.Read(Constants.MasterStateTagname);
            }
            catch
            {
                result = 2;
            }
             
            return result;
        }

        public bool Subscribe()
        {
            bool result = true;

            try
            {
                DataValue xDataValue = (DataValue)m_Server.Read(Constants.MasterStateTagname);
            }
            catch
            {
                result = false;
            }

            return result;
        }

        #endregion


        #region Connect and Disconnect Server
        /// <summary>
        /// Handles connect procedure
        /// </summary>
        private void OnConnect()
        {
            if (m_Server == null)
            {
                // Create a server object
                m_Server = new Server();
            }

            try
            {
                // connect to the server
                m_Server.Connect(Constants.opc_server_name);

            }
            catch (Exception exception)
            {
                // Cleanup
                m_Server = null;

//                MessageBox.Show(exception.Message, "Connect failed");
            }
        }

        public bool startMonitorItems(List<string> tags_list)
        {
            bool result = true;
            // Check if we have a subscription. If not - create a new subscription.
            if (m_Subscription == null)
            {
                try
                {
                    // Create subscription
                    m_Subscription = m_Server.CreateSubscription("ReadGroup", Constants.updaterate, OnDataChange);

                }
                catch (Exception exception)
                {
                    result = false;
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
                    m_Subscription.AddItem(
                        element,
                        idx);
                }
                catch (Exception exception)
                {
                    result = false;
                    //LogText("[" + element + "]," + exception.Message);

                }
                idx++;
            }

            return result;
        }

        bool stopMonitorItems()
        {
            bool result = false;
            if (m_Subscription != null)
            {
                try
                {
                    m_Server.DeleteSubscription(m_Subscription);
                    m_Subscription = null;
                }
                catch (Exception ex)
                {
                    result = false;
                }
            }
            return result;
        }

        /// <summary>
        /// Handles disconnect procedure
        /// </summary>
        private void OnDisconnect()
        {

            if (m_Server == null)
            {
                return;
            }

            try
            {
                // delete all subscriptions
                stopMonitorItems();


                //// Change GUI settings
                //btnConnect.Text = "Connect";
                //txtServerUrl.Enabled = true;

                //// disable buttons
                //btnMonitor.Enabled = false;

                // Disconnect
                m_Server.Disconnect();

                // Cleanup
                m_Server = null;
            }
            catch (Exception exception)
            {
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
            OnDisconnect();
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
                // We have to call an invoke method 
                if (this.InvokeRequired)
                {
                    // Asynchronous execution of the valueChanged delegate
                    this.BeginInvoke(new DataChange(OnDataChange), DataValues);
                    return;
                }

                foreach (DataValue value in DataValues)
                {
                    // 1 is Item1, 2 is Item2, 3 is ItemBlockRead

                    // Print data change information for variable - check first the result code
                    if (value.Error != 0)
                    {
                        // The node failed - print the symbolic name of the status code
                        string e_line = dtTools.GetNowString() + " " + all_tags[value.ClientHandle] + " Error: 0x" + value.Error.ToString("X");
                        LogText(e_line);
                        using (StreamWriter we = File.AppendText("errors.log"))
                        {
                            we.WriteLine(e_line);
                        }

                    }
                    else
                    {
                        string d_line = dtTools.GetNowString() + ";" + all_tags[value.ClientHandle] + ";" + value.Value.ToString() + ";" + value.ToString();
                        // The node succeeded - print the value as string
                        LogText(d_line);
                        listView1.Items[value.ClientHandle].SubItems[1].Text = value.Value.ToString();
                        using (StreamWriter w = File.AppendText(dtTools.GetTimeDateFile() + ".json"))
                        {
                            w.WriteLine(d_line);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                LogText("Unexpected error in the data change callback:\n\n" + ex.Message);
            }
        }
        #endregion

        #region Internal Helper Methods

    }
}
