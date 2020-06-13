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
        List<string> all_tags = new List<string> {};
        bool first = true; 
        static class Constants
        {
            public const int updaterate = 200;
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
            
            // set the sever we want to connet to
           // txtServerUrl.Text = serverUrl;

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
                                string e_line = GetTimeDate() + " " + all_tags[value.ClientHandle] + " Error: 0x" + value.Error.ToString("X"); 
                                LogText(e_line);
                                using (StreamWriter we = File.AppendText("errors.log"))
                                {
                                    we.WriteLine(e_line);
                                }

                            }
                            else
                            {
                                string d_line = GetTimeDate() + ";" + all_tags[value.ClientHandle] + ";" + value.Value.ToString();
                                // The node succeeded - print the value as string
                                LogText(d_line);
                                using (StreamWriter w = File.AppendText("data.csv")) 
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
        void LogText(string output)
        {
            txtMonitorResults.AppendText("\r\n" + output);
            txtMonitorResults.ScrollToCaret();

        } 
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
                    txtMonitorTags.Enabled = false;

                }
                catch (Exception exception)
                {
                    LogText("Create subscription failed:\n\n" + exception.Message);
                    return;
                }
            }
            int idx = 0;
            all_tags.Clear();
            foreach (string element in txtMonitorTags.Lines)
            {
                all_tags.Add(element);
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
                    txtMonitorTags.Enabled =  true;
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
        const string serverUrl = "opcda://localhost/easyopc.da2.1";
        #endregion

        public static string GetTimeDate()
        {
            string DateTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");
            return DateTime;
        } 

        private void txtMonitorResults_TextChanged(object sender, EventArgs e)
        {
            if (txtMonitorResults.Lines.Length > 100)
            {
                txtMonitorResults.Clear();
            }

        }
        public string UploadFile(string fn, string uriString)
        {

            // Create a new WebClient instance.
           // WebClient myWebClient = new WebClient();

            //Console.WriteLine("\nPlease enter the fully qualified path of the file to be uploaded to the URL");
            string fileName = fn;
            string filetext;
            //myWebClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
            using (StreamReader sr = new StreamReader(fn))
            {
                //This allows you to do one Read operation.
                filetext =sr.ReadToEnd();
            }

            string responseFromServer;

            WebRequest request = WebRequest.Create(uriString);
            request.Method = "POST";

            string postData = "marker=" + fn + "&data=" + filetext;
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();

            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            // Get the stream containing content returned by the server.
            // The using block ensures the stream is automatically closed.
            using (dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Display the content.
                Console.WriteLine(responseFromServer);
            }

            // Close the response.
            response.Close();
            return responseFromServer;

        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                WebClient client = new WebClient();

                // Add a user agent header in case the
                // requested URI contains a query.
                Thread myThread = new System.Threading.Thread(delegate()
                {
                    //Your code here
                });
                myThread.Start();

                MessageBox.Show(UploadFile(openFileDialog1.FileName, "http://localhost:8080/insopcdata"), "Response");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {


        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Process[] localByName = Process.GetProcessesByName("CCSsmRTServer");
            checkBox1.Checked = localByName.Length > 0;
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
    }
}