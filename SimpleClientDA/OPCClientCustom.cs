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
        public const string opc_server_exe = "CCSsmRTServer";
        public const string opc_server_name = "OPCServer.WinCC.1";
        const string serverUrl = "opcda://localhost/OPCServer.WinCC.1";
    }

    // class StateRedundancyController вынесен в additional

    class OPCClientCustom
    {
        #region Private Members
        private Server m_Server = null;
        private Subscription m_Subscription = null;
        private bool canclose = false;
        private string MachineName = Environment.MachineName;
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
                m_Server.Connect(txtServerUrl.Text);

                // Change GUI settings
                btnConnect.Text = "Disconnect";
                txtServerUrl.Enabled = false;

                // enable buttons
                btnMonitor.Enabled = true;

            }
            catch (Exception exception)
            {
                // Cleanup
                m_Server = null;

                MessageBox.Show(exception.Message, "Connect failed");
            }
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


                // Change GUI settings
                btnConnect.Text = "Connect";
                txtServerUrl.Enabled = true;

                // disable buttons
                btnMonitor.Enabled = false;

                // Disconnect
                m_Server.Disconnect();

                // Cleanup
                m_Server = null;
            }
            catch (Exception exception)
            {
                LogText(exception.Message + " Disconnect failed");
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

        void startMonitorItems()
        {
            // Check if we have a subscription. If not - create a new subscription.
            if (m_Subscription == null)
            {
                try
                {
                    // Create subscription
                    m_Subscription = m_Server.CreateSubscription("Subscription1", Constants.updaterate, OnDataChange);
                    btnMonitor.Text = "Stop";

                    // disable changing the itemID
                    //txtMonitorTags.Enabled = false;

                }
                catch (Exception exception)
                {
                    LogText("Create subscription failed:\n\n" + exception.Message);
                    return;
                }
            }
            int idx = 0;
            //all_tags.Clear();
            foreach (string element in all_tags)
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
                    LogText("[" + element + "]," + exception.Message);

                }
                idx++;
            }
        }

        void stopMonitorItems()
        {
            if (m_Subscription != null)
            {
                try
                {
                    m_Server.DeleteSubscription(m_Subscription);
                    m_Subscription = null;

                    btnMonitor.Text = "Monitor";
                    //txtMonitorTags.Enabled =  true;
                }
                catch (Exception ex)
                {
                    LogText("Stopping data monitoring failed:\n\n" + ex.Message);
                }
            }
        }
    }
}
