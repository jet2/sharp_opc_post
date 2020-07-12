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
        string LastCalledMinute = "";
        
        // Creates a synchronized wrapper around the Queue.
        static Queue DataChannel = Queue.Synchronized(new Queue());
        static Queue FormOPCChannel = Queue.Synchronized(new Queue());
        static Queue WebSenderChannel = Queue.Synchronized(new Queue());

        private ThreadedDiskWriter DiskWriterX = new ThreadedDiskWriter(DataChannel);
        private ThreadedWebSenderX WebSenderX;
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
            if (all_tags[0] != Constants.MasterStateTagname)
            {
                all_tags.Insert(0, Constants.MasterStateTagname);
            };

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
        private bool AlarmTagnamesConfirmed = false;

        public object ThreadedWebSender { get; private set; }
        #endregion




        private void button1_Click(object sender, EventArgs e)
        {

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
                timer_check_opc.Enabled = true;
                timer_call_to_LastValues.Enabled = true;
                WebSenderX = new ThreadedWebSenderX(WebSenderChannel, addr_post);
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
                this.WebSenderX.StopSend();
            }
            else { 
                this.Hide(); 
            };    
        }

        private void tmr_post_tick(object sender, EventArgs e)
        {
            string[] filePaths = Directory.GetFiles(thisAppFolder, "*."+Constants.FilesExtension);

            listBox1.Items.Clear();
            foreach (string str in filePaths){
                listBox1.Items.Add(Path.GetFileName(str));
            }
            if (dtTools.GetNowSeconds() == 30)
            {
                foreach (string FullPath in filePaths)
                {
                    string left = Path.GetFileName(FullPath);
                    string right = dtTools.GetMinuteFileName() +"."+ Constants.FilesExtension;
                    if ( left != right)
                    {
                        WebSenderChannel.Enqueue(FullPath);
                    }
                }
            }
        }

        private void check_exe_timer_Tick(object sender, EventArgs e)
        {
            timer_check_opc.Enabled = false;
            string state = "0";
            try
            {
                if (dtTools.OPCServerProcessFound())
                {
                    try
                    {
                        if (!ClientOPC.Connected)
                        {
                            if (ClientOPC.Connect())
                            {

                                string badlines = ClientOPC.startMonitorItems(this.all_tags, !AlarmTagnamesConfirmed);

                                if (badlines != "")
                                {
                                    if (!AlarmTagnamesConfirmed)
                                    {
                                        AlarmTagnamesConfirmed = true;
                                        MessageBox.Show("Tagnames not found: \n" + badlines);
                                    }
                                }
                                else
                                {
                                    state = ClientOPC.CheckConnect().ToString();
                                }
                            }
                        }
                        else
                        {
                            state = ClientOPC.CheckConnect().ToString();
                        }

                    }
                    catch
                    {
                        state = "0";
                    };
                    

                }
                else
                {
                    state = "0";
                }
            }
            finally
            {
                if (state == "1")
                {
                    lblOPCstate.Text = "OK";
                }
                else
                {
                    lblOPCstate.Text = "Bad";
                }
                timer_check_opc.Enabled= true;
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

                //LogText("Stopping data monitoring failed:\n\n" + ex.Message);
            };

        }

        private void SimpleClientDA_Activated(object sender, EventArgs e)
        {
            if (canclose) { this.Close(); };

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
                    if (pair.tagId == 0){
                        lblMasterState.Text = pair.tagValue;
                    };
                    zzz--;
                }
            }
            finally
            {
                timer_updateTagValues.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void lblWEBstate_Click(object sender, EventArgs e)
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
                WebSenderChannel.Enqueue(openFileDialog1.FileName);
                //addr_post = "http://localhost:8080/addfile";
                //string xxx = ThreadedWebSenderX.SendFileX(, addr_post);
                //MessageBox.Show(xxx, "Response");
            }
        }

        private void timer_call_to_LastValues_Tick(object sender, EventArgs e)
        {
            timer_call_to_LastValues.Enabled = false;
            try
            {


                string now5 = "";
                if (System.DateTime.Now.Minute % 5 == 0)
                {
                    now5 = "5";
                    if (System.DateTime.Now.Minute % 10 == 0)
                    {
                        now5 = "0";
                    }
                }
                if (now5 != "")
                {
                    if (now5 != LastCalledMinute)
                    {
                        LastCalledMinute = now5;
                        ClientOPC.CallToLastValues();
                    }
                }
            }
            finally
            {

                timer_call_to_LastValues.Enabled = true;
            }

        }
    }
}