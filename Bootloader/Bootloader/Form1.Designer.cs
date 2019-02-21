namespace Bootloader
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button_serial_port_connect = new System.Windows.Forms.Button();
            this.pictureBox_serial_port_connecting = new System.Windows.Forms.PictureBox();
            this.comboBox_serial_port_flow_control = new System.Windows.Forms.ComboBox();
            this.comboBox_serial_port_parity = new System.Windows.Forms.ComboBox();
            this.comboBox_serial_port_stop_bits = new System.Windows.Forms.ComboBox();
            this.comboBox_serial_port_data_bits = new System.Windows.Forms.ComboBox();
            this.comboBox_serial_port_baut_rate = new System.Windows.Forms.ComboBox();
            this.comboBox_serial_port_name = new System.Windows.Forms.ComboBox();
            this.label16 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button_load_hex_file = new System.Windows.Forms.Button();
            this.textBox_file_path = new System.Windows.Forms.TextBox();
            this.button_start = new System.Windows.Forms.Button();
            this.label_cnt = new System.Windows.Forms.Label();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_serial_port_connecting)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button_serial_port_connect);
            this.groupBox1.Controls.Add(this.pictureBox_serial_port_connecting);
            this.groupBox1.Controls.Add(this.comboBox_serial_port_flow_control);
            this.groupBox1.Controls.Add(this.comboBox_serial_port_parity);
            this.groupBox1.Controls.Add(this.comboBox_serial_port_stop_bits);
            this.groupBox1.Controls.Add(this.comboBox_serial_port_data_bits);
            this.groupBox1.Controls.Add(this.comboBox_serial_port_baut_rate);
            this.groupBox1.Controls.Add(this.comboBox_serial_port_name);
            this.groupBox1.Controls.Add(this.label16);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.label14);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.label12);
            this.groupBox1.Controls.Add(this.label11);
            this.groupBox1.Location = new System.Drawing.Point(23, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(331, 543);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Serial Port ";
            // 
            // button_serial_port_connect
            // 
            this.button_serial_port_connect.BackColor = System.Drawing.SystemColors.Control;
            this.button_serial_port_connect.Location = new System.Drawing.Point(172, 416);
            this.button_serial_port_connect.Name = "button_serial_port_connect";
            this.button_serial_port_connect.Size = new System.Drawing.Size(99, 49);
            this.button_serial_port_connect.TabIndex = 27;
            this.button_serial_port_connect.Text = "CONNECT";
            this.button_serial_port_connect.UseVisualStyleBackColor = true;
            this.button_serial_port_connect.Click += new System.EventHandler(this.button_serial_port_connect_Click);
            // 
            // pictureBox_serial_port_connecting
            // 
            this.pictureBox_serial_port_connecting.Location = new System.Drawing.Point(29, 416);
            this.pictureBox_serial_port_connecting.Name = "pictureBox_serial_port_connecting";
            this.pictureBox_serial_port_connecting.Size = new System.Drawing.Size(54, 49);
            this.pictureBox_serial_port_connecting.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox_serial_port_connecting.TabIndex = 26;
            this.pictureBox_serial_port_connecting.TabStop = false;
            // 
            // comboBox_serial_port_flow_control
            // 
            this.comboBox_serial_port_flow_control.FormattingEnabled = true;
            this.comboBox_serial_port_flow_control.Location = new System.Drawing.Point(172, 338);
            this.comboBox_serial_port_flow_control.Name = "comboBox_serial_port_flow_control";
            this.comboBox_serial_port_flow_control.Size = new System.Drawing.Size(121, 23);
            this.comboBox_serial_port_flow_control.TabIndex = 25;
            this.comboBox_serial_port_flow_control.Visible = false;
            // 
            // comboBox_serial_port_parity
            // 
            this.comboBox_serial_port_parity.FormattingEnabled = true;
            this.comboBox_serial_port_parity.Items.AddRange(new object[] {
            "Even",
            "Odd"});
            this.comboBox_serial_port_parity.Location = new System.Drawing.Point(172, 302);
            this.comboBox_serial_port_parity.Name = "comboBox_serial_port_parity";
            this.comboBox_serial_port_parity.Size = new System.Drawing.Size(121, 23);
            this.comboBox_serial_port_parity.TabIndex = 24;
            this.comboBox_serial_port_parity.Visible = false;
            // 
            // comboBox_serial_port_stop_bits
            // 
            this.comboBox_serial_port_stop_bits.FormattingEnabled = true;
            this.comboBox_serial_port_stop_bits.Location = new System.Drawing.Point(172, 267);
            this.comboBox_serial_port_stop_bits.Name = "comboBox_serial_port_stop_bits";
            this.comboBox_serial_port_stop_bits.Size = new System.Drawing.Size(121, 23);
            this.comboBox_serial_port_stop_bits.TabIndex = 23;
            this.comboBox_serial_port_stop_bits.Visible = false;
            // 
            // comboBox_serial_port_data_bits
            // 
            this.comboBox_serial_port_data_bits.FormattingEnabled = true;
            this.comboBox_serial_port_data_bits.Location = new System.Drawing.Point(172, 233);
            this.comboBox_serial_port_data_bits.Name = "comboBox_serial_port_data_bits";
            this.comboBox_serial_port_data_bits.Size = new System.Drawing.Size(121, 23);
            this.comboBox_serial_port_data_bits.TabIndex = 22;
            this.comboBox_serial_port_data_bits.Visible = false;
            // 
            // comboBox_serial_port_baut_rate
            // 
            this.comboBox_serial_port_baut_rate.FormattingEnabled = true;
            this.comboBox_serial_port_baut_rate.Items.AddRange(new object[] {
            "19200",
            "38400",
            "56000",
            "57600",
            "115200"});
            this.comboBox_serial_port_baut_rate.Location = new System.Drawing.Point(172, 136);
            this.comboBox_serial_port_baut_rate.Name = "comboBox_serial_port_baut_rate";
            this.comboBox_serial_port_baut_rate.Size = new System.Drawing.Size(121, 23);
            this.comboBox_serial_port_baut_rate.TabIndex = 21;
            this.comboBox_serial_port_baut_rate.SelectedValueChanged += new System.EventHandler(this.comboBox_serial_port_baut_rate_SelectedValueChanged);
            // 
            // comboBox_serial_port_name
            // 
            this.comboBox_serial_port_name.FormattingEnabled = true;
            this.comboBox_serial_port_name.Location = new System.Drawing.Point(172, 57);
            this.comboBox_serial_port_name.Name = "comboBox_serial_port_name";
            this.comboBox_serial_port_name.Size = new System.Drawing.Size(121, 23);
            this.comboBox_serial_port_name.TabIndex = 20;
            this.comboBox_serial_port_name.SelectedValueChanged += new System.EventHandler(this.comboBox_serial_port_name_SelectedValueChanged);
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(26, 341);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(111, 15);
            this.label16.TabIndex = 19;
            this.label16.Text = "Flow Control:";
            this.label16.Visible = false;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(26, 305);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(63, 15);
            this.label15.TabIndex = 18;
            this.label15.Text = "Parity:";
            this.label15.Visible = false;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(26, 270);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(87, 15);
            this.label14.TabIndex = 17;
            this.label14.Text = "Stop Bits:";
            this.label14.Visible = false;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(25, 235);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(87, 15);
            this.label13.TabIndex = 16;
            this.label13.Text = "Data Bits:";
            this.label13.Visible = false;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(26, 139);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(87, 15);
            this.label12.TabIndex = 15;
            this.label12.Text = "Baut Rate:";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(25, 60);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(87, 15);
            this.label11.TabIndex = 14;
            this.label11.Text = "Port Name:";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(533, 159);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(462, 23);
            this.progressBar1.TabIndex = 9;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(377, 190);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(748, 365);
            this.richTextBox1.TabIndex = 8;
            this.richTextBox1.Text = "";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.button_load_hex_file);
            this.groupBox2.Controls.Add(this.textBox_file_path);
            this.groupBox2.Location = new System.Drawing.Point(377, 25);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(748, 112);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Load Hex File";
            // 
            // button1
            // 
            this.button1.Enabled = false;
            this.button1.Location = new System.Drawing.Point(7, 64);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(170, 32);
            this.button1.TabIndex = 4;
            this.button1.Text = "CONVERT TO BIN";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Visible = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button_load_hex_file
            // 
            this.button_load_hex_file.Location = new System.Drawing.Point(611, 24);
            this.button_load_hex_file.Name = "button_load_hex_file";
            this.button_load_hex_file.Size = new System.Drawing.Size(94, 27);
            this.button_load_hex_file.TabIndex = 3;
            this.button_load_hex_file.Text = "LOAD";
            this.button_load_hex_file.UseVisualStyleBackColor = true;
            this.button_load_hex_file.Click += new System.EventHandler(this.button_load_hex_file_Click);
            // 
            // textBox_file_path
            // 
            this.textBox_file_path.Location = new System.Drawing.Point(7, 24);
            this.textBox_file_path.Name = "textBox_file_path";
            this.textBox_file_path.Size = new System.Drawing.Size(572, 25);
            this.textBox_file_path.TabIndex = 2;
            // 
            // button_start
            // 
            this.button_start.Enabled = false;
            this.button_start.Location = new System.Drawing.Point(377, 148);
            this.button_start.Name = "button_start";
            this.button_start.Size = new System.Drawing.Size(150, 39);
            this.button_start.TabIndex = 6;
            this.button_start.Text = "UPDATE START";
            this.button_start.UseVisualStyleBackColor = true;
            this.button_start.Click += new System.EventHandler(this.button_start_Click);
            // 
            // label_cnt
            // 
            this.label_cnt.AutoSize = true;
            this.label_cnt.Location = new System.Drawing.Point(1059, 167);
            this.label_cnt.Name = "label_cnt";
            this.label_cnt.Size = new System.Drawing.Size(23, 15);
            this.label_cnt.TabIndex = 10;
            this.label_cnt.Text = "0%";
            // 
            // serialPort1
            // 
            this.serialPort1.BaudRate = 19200;
            this.serialPort1.Parity = System.IO.Ports.Parity.Even;
            this.serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "Hex File(*.bin)|*.bin|All Files(*.*)|*.*";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1155, 602);
            this.Controls.Add(this.label_cnt);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.button_start);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bootloader(1.0.2)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_serial_port_connecting)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button_serial_port_connect;
        private System.Windows.Forms.PictureBox pictureBox_serial_port_connecting;
        private System.Windows.Forms.ComboBox comboBox_serial_port_flow_control;
        private System.Windows.Forms.ComboBox comboBox_serial_port_parity;
        private System.Windows.Forms.ComboBox comboBox_serial_port_stop_bits;
        private System.Windows.Forms.ComboBox comboBox_serial_port_data_bits;
        private System.Windows.Forms.ComboBox comboBox_serial_port_baut_rate;
        private System.Windows.Forms.ComboBox comboBox_serial_port_name;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button_load_hex_file;
        private System.Windows.Forms.TextBox textBox_file_path;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label_cnt;
        private System.IO.Ports.SerialPort serialPort1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}

