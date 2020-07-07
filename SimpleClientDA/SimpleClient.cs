using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Siemens.Opc;
using Siemens.Opc.Da;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;


namespace Siemens.Opc.DaClient
{
 

    public partial class SimpleClientDA : Form
    {
        List<string> all_tags;
        bool first = true;
        string addr_post;
        string thisAppFolder;
        ManualResetEvent mre;
        //ProcessQueue<string> queue = new ProcessQueue<string>(FileHandler);
        static class Constants
        {
            public const int updaterate = 200;
            public const string opc_server_exe="CCSsmRTServer";
            public const string opc_server_name = "OPCServer.WinCC.1";
            const string serverUrl = "opcda://localhost/OPCServer.WinCC.1";
        } 


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

        #region Construction
        public SimpleClientDA()
        {
            InitializeComponent();
            if (!loadConfig())
            {
                canclose = true;

            };
            // set the sever we want to connet to
           // txtServerUrl.Text = serverUrl;

        }

        public bool loadConfig(){
            lblOPCprogid.Text = Constants.opc_server_name;
            string fn = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + @"\config.txt";
            
            try
            {
                all_tags =new List<string>( File.ReadAllLines(fn, Encoding.UTF8));
            }
            catch (Exception e)
            {
                MessageBox.Show(fn+ " :: " + e.Message);
                return false;
            }
            if (all_tags.Count > 1)
            {
                addr_post = all_tags[0];
            }
            all_tags.RemoveAt(0); 
            lblWEBaddr.Text = addr_post;

            foreach (string line in all_tags)
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = line;
                lvi.SubItems.Add("-");
                listView1.Items.Add(lvi);
                //listView1.Items[listView1.Items.Count - 1].SubItems[1].Text = "1";
            }
            thisAppFolder = System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + @"\";
            return true;
        }
        #endregion

        #region User Actions
        /// <summary>
        /// Handle action when connect button was clicked
        /// </summary>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (m_Server == null)
            {
                OnConnect();
            }
            else if (m_Server.IsConnected)
            {
                OnDisconnect();
            }
            else
            {
                OnDisconnect();
            }
        }


        /// <summary>
        /// Handle action when Monitor button was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMonitor_Click(object sender, EventArgs e)
        {
            // Check if we have a subscription
            //  - No  -> Create a new subscription and create monitored items
            //  - Yes -> Delete Subcription
            if (m_Subscription == null)
            {
                startMonitorItems();
            }
            else
            {
                stopMonitorItems();
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
                                using (StreamWriter w = File.AppendText(dtTools.GetTimeDateFile()+".json")) 
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
                    LogText("["+element+"],"+exception.Message);
                   
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


        #endregion

        #region Private Members
        private Server m_Server = null;
        private Subscription m_Subscription = null;
        private bool canclose = false;
        private string MachineName = Environment.MachineName;
        #endregion




        private void txtMonitorResults_TextChanged(object sender, EventArgs e)
        {
            if (txtMonitorResults.Lines.Length > 100)
            {
                txtMonitorResults.Clear();
            }

        }


        


        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
               // WebClient client = new WebClient();

                // Add a user agent header in case the
                // requested URI contains a query.
                //Thread myThread = new System.Threading.Thread(delegate()
                //{
                //    //Your code here
                //});
                //myThread.Start();
                addr_post = "http://localhost:8080/insopcdata";
                MessageBox.Show(UploaderByPost.UploadFile(openFileDialog1.FileName, addr_post), "Response");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {


        }

        private void timer1_Tick(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
        }

        private void SimpleClientDA_Shown(object sender, EventArgs e)
        {
            if (first) { 
                first = false; this.Hide(); 
            }
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            canclose = true;
            this.Close();
        }

        private void SimpleClientDA_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            if (canclose)
            {
                e.Cancel = false;
            }
            else { 
                this.Hide(); 
            };    

            
        }

        private void tmr_post_tick(object sender, EventArgs e)
        {
            string[] filePaths = Directory.GetFiles(thisAppFolder, "*.json");
            listBox1.Items.Clear();
            foreach (string str in filePaths){
                listBox1.Items.Add(Path.GetFileName(str));
            }
            
        }

        private void check_exe_timer_Tick(object sender, EventArgs e)
        {
            Process[] localByName = Process.GetProcessesByName(Constants.opc_server_exe);
            checkBox1.Checked = localByName.Length > 0;

            string state = "1";
            try
            {
                state = m_Server.Read(all_tags[0]).ToString();
                lblOPCstate.Text = "OK";
            }
            catch
            {
                lblOPCstate.Text = "Bad";
            }
            
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            tmr_webping.Enabled = false;
            Uri myUri = new Uri(lblWEBaddr.Text);
            // Get host part (host name or address and port). Returns "server:8080".

            var client = new WebClient();


    // Specify that the DownloadStringCallback2 method gets called
    // when the download completes.
            string mystring = "000";
            client.DownloadStringCompleted += 
                    (s, hdlr_e) => {
                        try
                        {
                            var result = hdlr_e.Result;
                            mystring = result.ToString();
                        }
                        catch
                        {
                            mystring = "000";
                        }

                        lblWEBstate.Text = "Bad " + mystring;
                        if (mystring == "777")
                        {
                            lblWEBstate.Text = "OK " + mystring;
                        }
                        tmr_webping.Enabled = true;
                    };


            try
            {
                client.DownloadStringAsync(new Uri("http://" + myUri.Authority));
            }
            catch (Exception ex)
            {

                LogText("Stopping data monitoring failed:\n\n" + ex.Message);
            };

        }

        private void SimpleClientDA_Activated(object sender, EventArgs e)
        {
            if (canclose) { this.Close(); };

        }

        void LogText(string output)
        {
            txtMonitorResults.AppendText("\r\n" + output);
            txtMonitorResults.ScrollToCaret();

        }
    }
}