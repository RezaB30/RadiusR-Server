
namespace RezaB.Radius.DAE.TestUnit
{
    partial class MainForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.BindedLocalPortNumeric = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.BindedLocalIPTextbox = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.RequestMessageTypeCombobox = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.SendRequestButton = new System.Windows.Forms.Button();
            this.RequestAttributeAddButton = new System.Windows.Forms.Button();
            this.RequestAttributeValueTextbox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.VendorAttributesPanel = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.MikrotikAttributeTypeCombobox = new System.Windows.Forms.ComboBox();
            this.RequestAttributeTypeCombobox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.RequestAttributesListbox = new System.Windows.Forms.ListBox();
            this.label6 = new System.Windows.Forms.Label();
            this.SecretTextbox = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.DASIPTextbox = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.DASPortNumeric = new System.Windows.Forms.NumericUpDown();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.ResponseLogTextbox = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.BindedLocalPortNumeric)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.VendorAttributesPanel.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DASPortNumeric)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(131, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Binded Local IP (optional):";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Binded Local Port:";
            // 
            // BindedLocalPortNumeric
            // 
            this.BindedLocalPortNumeric.Location = new System.Drawing.Point(144, 45);
            this.BindedLocalPortNumeric.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.BindedLocalPortNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.BindedLocalPortNumeric.Name = "BindedLocalPortNumeric";
            this.BindedLocalPortNumeric.Size = new System.Drawing.Size(144, 20);
            this.BindedLocalPortNumeric.TabIndex = 2;
            this.BindedLocalPortNumeric.Value = new decimal(new int[] {
            3799,
            0,
            0,
            0});
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.BindedLocalIPTextbox);
            this.groupBox1.Controls.Add(this.BindedLocalPortNumeric);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(776, 79);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Dynamic Authorization Client";
            // 
            // BindedLocalIPTextbox
            // 
            this.BindedLocalIPTextbox.Location = new System.Drawing.Point(144, 19);
            this.BindedLocalIPTextbox.MaxLength = 15;
            this.BindedLocalIPTextbox.Name = "BindedLocalIPTextbox";
            this.BindedLocalIPTextbox.Size = new System.Drawing.Size(144, 20);
            this.BindedLocalIPTextbox.TabIndex = 5;
            this.BindedLocalIPTextbox.Text = "10.180.0.2";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.RequestMessageTypeCombobox);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.SendRequestButton);
            this.groupBox2.Controls.Add(this.RequestAttributeAddButton);
            this.groupBox2.Controls.Add(this.RequestAttributeValueTextbox);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.VendorAttributesPanel);
            this.groupBox2.Controls.Add(this.RequestAttributeTypeCombobox);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.RequestAttributesListbox);
            this.groupBox2.Location = new System.Drawing.Point(13, 183);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(381, 371);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Request Packet";
            // 
            // RequestMessageTypeCombobox
            // 
            this.RequestMessageTypeCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RequestMessageTypeCombobox.FormattingEnabled = true;
            this.RequestMessageTypeCombobox.Location = new System.Drawing.Point(127, 341);
            this.RequestMessageTypeCombobox.Name = "RequestMessageTypeCombobox";
            this.RequestMessageTypeCombobox.Size = new System.Drawing.Size(167, 21);
            this.RequestMessageTypeCombobox.TabIndex = 11;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 345);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(80, 13);
            this.label7.TabIndex = 10;
            this.label7.Text = "Message Type:";
            // 
            // SendRequestButton
            // 
            this.SendRequestButton.Location = new System.Drawing.Point(300, 340);
            this.SendRequestButton.Name = "SendRequestButton";
            this.SendRequestButton.Size = new System.Drawing.Size(75, 23);
            this.SendRequestButton.TabIndex = 9;
            this.SendRequestButton.Text = "Send";
            this.SendRequestButton.UseVisualStyleBackColor = true;
            this.SendRequestButton.Click += new System.EventHandler(this.SendRequestButton_Click);
            // 
            // RequestAttributeAddButton
            // 
            this.RequestAttributeAddButton.Location = new System.Drawing.Point(300, 106);
            this.RequestAttributeAddButton.Name = "RequestAttributeAddButton";
            this.RequestAttributeAddButton.Size = new System.Drawing.Size(75, 23);
            this.RequestAttributeAddButton.TabIndex = 8;
            this.RequestAttributeAddButton.Text = "Add";
            this.RequestAttributeAddButton.UseVisualStyleBackColor = true;
            this.RequestAttributeAddButton.Click += new System.EventHandler(this.RequestAttributeAddButton_Click);
            // 
            // RequestAttributeValueTextbox
            // 
            this.RequestAttributeValueTextbox.Location = new System.Drawing.Point(128, 80);
            this.RequestAttributeValueTextbox.MaxLength = 253;
            this.RequestAttributeValueTextbox.Name = "RequestAttributeValueTextbox";
            this.RequestAttributeValueTextbox.Size = new System.Drawing.Size(247, 20);
            this.RequestAttributeValueTextbox.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 83);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(79, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Attribute Value:";
            // 
            // VendorAttributesPanel
            // 
            this.VendorAttributesPanel.Controls.Add(this.label4);
            this.VendorAttributesPanel.Controls.Add(this.MikrotikAttributeTypeCombobox);
            this.VendorAttributesPanel.Location = new System.Drawing.Point(-1, 46);
            this.VendorAttributesPanel.Name = "VendorAttributesPanel";
            this.VendorAttributesPanel.Size = new System.Drawing.Size(382, 27);
            this.VendorAttributesPanel.TabIndex = 5;
            this.VendorAttributesPanel.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 6);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(116, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Mikrotik Attribute Type:";
            // 
            // MikrotikAttributeTypeCombobox
            // 
            this.MikrotikAttributeTypeCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MikrotikAttributeTypeCombobox.FormattingEnabled = true;
            this.MikrotikAttributeTypeCombobox.Location = new System.Drawing.Point(128, 3);
            this.MikrotikAttributeTypeCombobox.Name = "MikrotikAttributeTypeCombobox";
            this.MikrotikAttributeTypeCombobox.Size = new System.Drawing.Size(248, 21);
            this.MikrotikAttributeTypeCombobox.TabIndex = 4;
            // 
            // RequestAttributeTypeCombobox
            // 
            this.RequestAttributeTypeCombobox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RequestAttributeTypeCombobox.FormattingEnabled = true;
            this.RequestAttributeTypeCombobox.Location = new System.Drawing.Point(128, 19);
            this.RequestAttributeTypeCombobox.Name = "RequestAttributeTypeCombobox";
            this.RequestAttributeTypeCombobox.Size = new System.Drawing.Size(247, 21);
            this.RequestAttributeTypeCombobox.TabIndex = 2;
            this.RequestAttributeTypeCombobox.SelectedIndexChanged += new System.EventHandler(this.RequestAttributeTypeCombobox_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(76, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Attribute Type:";
            // 
            // RequestAttributesListbox
            // 
            this.RequestAttributesListbox.FormattingEnabled = true;
            this.RequestAttributesListbox.Location = new System.Drawing.Point(6, 135);
            this.RequestAttributesListbox.Name = "RequestAttributesListbox";
            this.RequestAttributesListbox.Size = new System.Drawing.Size(369, 199);
            this.RequestAttributesListbox.TabIndex = 0;
            this.RequestAttributesListbox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.RequestAttributesListbox_KeyDown);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(376, 22);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(41, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "Secret:";
            // 
            // SecretTextbox
            // 
            this.SecretTextbox.Location = new System.Drawing.Point(423, 19);
            this.SecretTextbox.Name = "SecretTextbox";
            this.SecretTextbox.Size = new System.Drawing.Size(130, 20);
            this.SecretTextbox.TabIndex = 4;
            this.SecretTextbox.Text = "123456";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.DASIPTextbox);
            this.groupBox3.Controls.Add(this.SecretTextbox);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Controls.Add(this.DASPortNumeric);
            this.groupBox3.Location = new System.Drawing.Point(12, 98);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(776, 79);
            this.groupBox3.TabIndex = 5;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Dynamic Authorization Server";
            // 
            // DASIPTextbox
            // 
            this.DASIPTextbox.Location = new System.Drawing.Point(144, 19);
            this.DASIPTextbox.MaxLength = 15;
            this.DASIPTextbox.Name = "DASIPTextbox";
            this.DASIPTextbox.Size = new System.Drawing.Size(144, 20);
            this.DASIPTextbox.TabIndex = 5;
            this.DASIPTextbox.Text = "10.180.0.1";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(7, 47);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(63, 13);
            this.label9.TabIndex = 0;
            this.label9.Text = "Server Port:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 22);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(54, 13);
            this.label8.TabIndex = 0;
            this.label8.Text = "Server IP:";
            // 
            // DASPortNumeric
            // 
            this.DASPortNumeric.Location = new System.Drawing.Point(144, 45);
            this.DASPortNumeric.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.DASPortNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.DASPortNumeric.Name = "DASPortNumeric";
            this.DASPortNumeric.Size = new System.Drawing.Size(144, 20);
            this.DASPortNumeric.TabIndex = 2;
            this.DASPortNumeric.Value = new decimal(new int[] {
            3799,
            0,
            0,
            0});
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.ResponseLogTextbox);
            this.groupBox4.Location = new System.Drawing.Point(401, 184);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(387, 370);
            this.groupBox4.TabIndex = 6;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Response";
            // 
            // ResponseLogTextbox
            // 
            this.ResponseLogTextbox.BackColor = System.Drawing.SystemColors.WindowText;
            this.ResponseLogTextbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ResponseLogTextbox.DetectUrls = false;
            this.ResponseLogTextbox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ResponseLogTextbox.ForeColor = System.Drawing.SystemColors.Window;
            this.ResponseLogTextbox.HideSelection = false;
            this.ResponseLogTextbox.Location = new System.Drawing.Point(3, 16);
            this.ResponseLogTextbox.Name = "ResponseLogTextbox";
            this.ResponseLogTextbox.Size = new System.Drawing.Size(381, 351);
            this.ResponseLogTextbox.TabIndex = 0;
            this.ResponseLogTextbox.Text = "";
            this.ResponseLogTextbox.WordWrap = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 566);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Dynamic Authorization Client Test Unit";
            ((System.ComponentModel.ISupportInitialize)(this.BindedLocalPortNumeric)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.VendorAttributesPanel.ResumeLayout(false);
            this.VendorAttributesPanel.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.DASPortNumeric)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown BindedLocalPortNumeric;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListBox RequestAttributesListbox;
        private System.Windows.Forms.ComboBox RequestAttributeTypeCombobox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox MikrotikAttributeTypeCombobox;
        private System.Windows.Forms.Panel VendorAttributesPanel;
        private System.Windows.Forms.TextBox RequestAttributeValueTextbox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button RequestAttributeAddButton;
        private System.Windows.Forms.TextBox SecretTextbox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button SendRequestButton;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox RequestMessageTypeCombobox;
        private System.Windows.Forms.TextBox BindedLocalIPTextbox;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox DASIPTextbox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown DASPortNumeric;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.RichTextBox ResponseLogTextbox;
    }
}

