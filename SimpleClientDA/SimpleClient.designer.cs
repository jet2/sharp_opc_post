namespace Siemens.Opc.DaClient
{
    partial class SimpleClientDA
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimpleClientDA));
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnMonitor = new System.Windows.Forms.Button();
            this.grpBoxBlockRead = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtMonitorTags = new System.Windows.Forms.RichTextBox();
            this.txtMonitorResults = new System.Windows.Forms.RichTextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtServerUrl = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.grpBoxBlockRead.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnConnect
            // 
            this.btnConnect.AutoSize = true;
            this.btnConnect.Location = new System.Drawing.Point(12, 12);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // btnMonitor
            // 
            this.btnMonitor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMonitor.Enabled = false;
            this.btnMonitor.Location = new System.Drawing.Point(455, 13);
            this.btnMonitor.Name = "btnMonitor";
            this.btnMonitor.Size = new System.Drawing.Size(86, 23);
            this.btnMonitor.TabIndex = 24;
            this.btnMonitor.Text = "Monitor";
            this.btnMonitor.UseVisualStyleBackColor = true;
            this.btnMonitor.Click += new System.EventHandler(this.btnMonitor_Click);
            // 
            // grpBoxBlockRead
            // 
            this.grpBoxBlockRead.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpBoxBlockRead.Controls.Add(this.label1);
            this.grpBoxBlockRead.Controls.Add(this.txtMonitorTags);
            this.grpBoxBlockRead.Controls.Add(this.txtMonitorResults);
            this.grpBoxBlockRead.Controls.Add(this.label7);
            this.grpBoxBlockRead.Location = new System.Drawing.Point(12, 41);
            this.grpBoxBlockRead.Name = "grpBoxBlockRead";
            this.grpBoxBlockRead.Size = new System.Drawing.Size(692, 262);
            this.grpBoxBlockRead.TabIndex = 22;
            this.grpBoxBlockRead.TabStop = false;
            this.grpBoxBlockRead.Text = "Subscription";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 13);
            this.label1.TabIndex = 20;
            this.label1.Text = "Monitor Tags";
            // 
            // txtMonitorTags
            // 
            this.txtMonitorTags.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.txtMonitorTags.ForeColor = System.Drawing.SystemColors.WindowText;
            this.txtMonitorTags.Location = new System.Drawing.Point(6, 32);
            this.txtMonitorTags.Name = "txtMonitorTags";
            this.txtMonitorTags.Size = new System.Drawing.Size(266, 224);
            this.txtMonitorTags.TabIndex = 19;
            this.txtMonitorTags.Text = resources.GetString("txtMonitorTags.Text");
            // 
            // txtMonitorResults
            // 
            this.txtMonitorResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtMonitorResults.Location = new System.Drawing.Point(278, 32);
            this.txtMonitorResults.Name = "txtMonitorResults";
            this.txtMonitorResults.ReadOnly = true;
            this.txtMonitorResults.Size = new System.Drawing.Size(408, 224);
            this.txtMonitorResults.TabIndex = 18;
            this.txtMonitorResults.Text = "";
            this.txtMonitorResults.TextChanged += new System.EventHandler(this.txtMonitorResults_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(388, 16);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(80, 13);
            this.label7.TabIndex = 1;
            this.label7.Text = "Monitor Results";
            // 
            // txtServerUrl
            // 
            this.txtServerUrl.FormattingEnabled = true;
            this.txtServerUrl.Items.AddRange(new object[] {
            "opcda://localhost/OPCServer.WinCC.1",
            "opcda://localhost/easyopc.da2.1"});
            this.txtServerUrl.Location = new System.Drawing.Point(93, 13);
            this.txtServerUrl.Name = "txtServerUrl";
            this.txtServerUrl.Size = new System.Drawing.Size(356, 21);
            this.txtServerUrl.TabIndex = 25;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(556, 13);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(124, 22);
            this.button1.TabIndex = 26;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // SimpleClientDA
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(716, 309);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtServerUrl);
            this.Controls.Add(this.grpBoxBlockRead);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnMonitor);
            this.Name = "SimpleClientDA";
            this.Text = "Simple Client OPC COM DA";
            this.grpBoxBlockRead.ResumeLayout(false);
            this.grpBoxBlockRead.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnMonitor;
        private System.Windows.Forms.GroupBox grpBoxBlockRead;
        private System.Windows.Forms.RichTextBox txtMonitorResults;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox txtMonitorTags;
        private System.Windows.Forms.ComboBox txtServerUrl;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
    }
}

