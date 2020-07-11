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



namespace Siemens.Opc.DaClient
{
 

    public partial class SimpleClientDA : Form
    {
        List<string> all_tags;
        bool first = true;
        string addr_post;
        string thisAppFolder;
        
        // Creates a synchronized wrapper around the Queue.
        static Queue DataChannel = Queue.Synchronized(new Queue());
        static Queue FormOPCChannel = Queue.Synchronized(new Queue());

        private ThreadedDiskWriter DiskWriterX = new ThreadedDiskWriter(DataChannel);

        private OPCClientCustom ClientOPC = new OPCClientCustom(DataChannel, FormOPCChannel, Constants.opc_server_name, 200);


        #region Construction
        public SimpleClientDA()
        {
            InitializeComponent();

            if (!loadConfig())
            {
                canclose = true;
            }

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



        #region Private Members
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

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
        }

        private void SimpleClientDA_Shown(object sender, EventArgs e)
        {
            if (first) { 
                first = false;
                if (this.canclose) {
                    Close();

                }
                //this.Hide(); 
                ClientOPC.Connect();

                string badlines = ClientOPC.startMonitorItems(this.all_tags);
                if (badlines != ""){
                    MessageBox.Show("Tagnames not found: \n"+badlines);
                };
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
                this.DiskWriterX.StopWrite();
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
            tmr_check_exe.Enabled = false;
            try
            {
                if (dtTools.OPCServerProcessFound())
                {
                    string state = ClientOPC.CheckConnect().ToString();
                    if (state == "1")
                    {
                        lblOPCstate.Text = "OK";
                    }
                    else
                    {
                        lblOPCstate.Text = "Bad";
                    }
                }
                else
                {
                    lblOPCstate.Text = "Bad";
                }
            }
            finally
            {
                tmr_check_exe.Enabled= true;
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

        private void timer_updateTagValues_Tick(object sender, EventArgs e)
        {
            timer_updateTagValues.Enabled = false;
            TagPair pair;
            var zzz = FormOPCChannel.Count;
            try
            {
                while (zzz > 0)
                {
                    pair = (TagPair)FormOPCChannel.Dequeue();

                    listView1.Items[pair.tagId].SubItems[1].Text = pair.tagValue;
                    zzz--;
                }
            }
            finally
            {
                timer_updateTagValues.Enabled = true;
            }
        }
    }
}