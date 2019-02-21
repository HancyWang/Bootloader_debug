using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.IO.Ports;
using System.Threading;

namespace Bootloader
{
    public partial class Form1 : Form
    {
        //串口实时监测相关
        private string[] m_oldSerialPortNames;      
        
        //串口开关状态
        private bool m_b_serialPortOpened = false;

        //载入的文件名
        private string m_fileName = "";

        //hex文件相关
        private List<byte> m_hex_file_list = new List<byte>();          //用来存放hex文件
        private bool m_b_load_hex_file_success = false;                 
        private List<List<byte>> m_datas_list = new List<List<byte>>();   //用来存放一条一条的数据
        private List<byte> m_write_list = new List<byte>();           //使用该list来向串口写入数据

        //串口写入相关
        private const int START_ADDRESS = 0x08000000;                          //写入的起始地址
        private const int WRITE_STEP = 0x40;                                   //写入数据的步长，每次0x100=256字节
        private int m_current_Address = 0x00;
        private long m_total_to_be_written_num = 0;                           //总共要写入的次数，每个一次是16字节
        private int m_current_write_cnt = 0;                             //记录当前写入的次数
        private int m_pre_current_write_cnt = 0;
        private bool m_b_start_update = false;
        private int m_BOOT_ERASE_CMD = 0x43;
        private List<byte> m_buffer = new List<byte>();
        private int m_send_fail_cnt = 0;

        //Log记录
        public static List<string> m_strLog_list = new List<string>();        //用来存放log
        public const string LOG_LOAD_HEX_FILE_SUCCESS = ",load Hex file successful!";
        public const string LOG_CONNECTING_DEVICE = "Connecting device...";
        public const string LOG_COMMAND_AVAILABLE = "Command available: ";
        public const string LOG_BOOTLOADER_VERSION = "Bootloader version: ";
        public const string LOG_PID = "PID: ";
        public const string LOG_FLASH_CAPACITY = "Flash capacity: ";
        public const string LOG_SRAM_CAPACITY = "SRAM capacity: ";
        public const string LOG_CPU_ID = "CPU ID: ";
        public const string LOG_READ_BYTES = "Read bytes: ";
        public const string LOG_ERASE_MCU_SUCCESSUL = "Erase MCU successful!";
        public const string LOG_ERASE_MCU_FAIL = "Erase MCU fail!";
        public const string LOG_UPDATE_STARTING = "Update starting...";
        public const string LOG_UPDATE_STOP = "Update stopped";
        public const string LOG_TIME_OUT = "Time out,try again!";
        public const string LOG_UPDATE_FINISH = "Update finished!";
        public const string LOG_UPDATE_FAIL = "Upate fail!";
        public const string LOG_DISCONNECT = "Disconnect!";

        //线程，用于出错的时候重新发送
        public bool m_b_thread_start_send = false;
        public Thread m_thread_resend = null;
        public const int THREAD_SLEEP_DURATION = 500;

        //发送状态
        private enum SEND_STEP
        {
            STEP_NONE,
            STEP_PRE_SEND_FAIL,    //预发送失败
            STEP_SEND_FAIL,        //发送过程中失败
            //STEP_RESEND,           //发送失败，重新发送,还是从step2开始
            STEP_START_RUNNING,    //开始运行代码
            //STEP1,               //第一次发和重发都是从step2开始，不需要step1
            STEP2,
            STEP3,
            STEP4,
            STEP5_1,
            STEP5_2,
            STEP_ERASE1,
            STEP_ERASE2,
            //STEP_PENDING_HEX_FILE_LOADED,
            STEP_WRITE,
            STEP_READ,
            STEP_FINISH,
            STEP_STOP
        }
        //private SEND_STEP m_send_step = SEND_STEP.STEP2;     //初始化为STEP2
        private SEND_STEP m_send_step = SEND_STEP.STEP_NONE;     

        public Form1()
        {
            InitializeComponent();
        }

        private void comboBox_serial_port_name_SelectedValueChanged(object sender, EventArgs e)
        {
            this.serialPort1.PortName = this.comboBox_serial_port_name.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            InitApp();
        }

        private void InitApp()
        {
            //初始化串口设置
            InitSerialPort();

            //加载图片
            LoadPicture();

            //设置进度条
            this.progressBar1.Value = 0;

            //初始化线程
            //m_thread_resend = new Thread(new ParameterizedThreadStart(resendBy));     //带参数的
            m_thread_resend = new Thread(new ThreadStart(resendBy));    //不带参数的


        }

        private void resendBy()
        {
            while(true)
            {
                Thread.Sleep(THREAD_SLEEP_DURATION);  //延迟200ms，发送
                send_data_by(m_send_step);
           
            }
        }


        private void InitSerialPort()
        {
            string[] str_portNames = SerialPort.GetPortNames();  //获取电脑的serial port
            if (str_portNames.Length != 0)  //如果有serial port
            {
                Array.Sort(str_portNames, (a, b) => Convert.ToInt32(((string)a).Substring(3)).CompareTo(Convert.ToInt32(((string)b).Substring(3)))); //闭包，类似Lamda表达式
                m_oldSerialPortNames = str_portNames;
                this.comboBox_serial_port_name.Items.AddRange(str_portNames);
                this.comboBox_serial_port_name.SelectedIndex = 0;
            }

            this.comboBox_serial_port_baut_rate.Text = "115200";
            //this.comboBox_serial_port_baut_rate.Text = "19200";
            this.comboBox_serial_port_data_bits.Text = "8";
            this.comboBox_serial_port_stop_bits.Text = "1";
            this.comboBox_serial_port_parity.Text = "Even";
            this.comboBox_serial_port_flow_control.Text = "None";
        }

        private void LoadPicture()
        {
            if (!m_b_serialPortOpened)
            {
                this.pictureBox_serial_port_connecting.Load(Environment.CurrentDirectory + @"\" + "red.png");
            }
            else
            {
                this.pictureBox_serial_port_connecting.Load(Environment.CurrentDirectory + @"\" + "green.png");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //开了app之后，没有连接串口硬件的时候
            if (m_send_step == SEND_STEP.STEP_NONE)
            {
                checking_serial_port();
            }
            else
            {
                if (m_b_serialPortOpened == false)
                {
                    return;
                }

                if ((m_b_start_update && this.serialPort1.IsOpen)) //串口连接按钮是按下去的，突然把线切断，但是串口硬件还是连接着
                {
                    //如果发送7F 00 FF没有任何反应
                    if (m_send_fail_cnt == 30)
                    {
                        m_send_fail_cnt = 0;

                        m_b_start_update = false;
                        this.button_load_hex_file.Enabled = true;
                        this.button_serial_port_connect.Enabled = true;

                        this.button_start.Text = "UPDATE_START";
                        m_send_step = SEND_STEP.STEP_PRE_SEND_FAIL;

                        m_strLog_list.Add(LOG_TIME_OUT);
                        show_log();
                    }
                    else
                    {
                        if (m_current_write_cnt == 0 && m_send_step == SEND_STEP.STEP2)
                        {
                            m_send_fail_cnt++;
                        }
                    }
                }
                if (m_b_start_update && this.serialPort1.IsOpen && m_send_step == SEND_STEP.STEP_WRITE)
                {
                    if (m_send_fail_cnt == 30)
                    {
                        m_send_fail_cnt = 0;
                        m_pre_current_write_cnt = 0;

                        m_b_start_update = false;
                        this.button_load_hex_file.Enabled = true;
                        this.button_serial_port_connect.Enabled = true;

                        this.button_start.Text = "UPDATE_START";
                        m_send_step = SEND_STEP.STEP_PRE_SEND_FAIL;

                        m_strLog_list.Add(LOG_TIME_OUT);
                        show_log();
                    }
                    else
                    {
                        if (m_pre_current_write_cnt == m_current_write_cnt)
                        {
                            m_send_fail_cnt++;
                        }
                        else
                        {
                            m_pre_current_write_cnt = m_current_write_cnt;
                        }
                    } 

                }
                else if (m_b_start_update && !this.serialPort1.IsOpen)  //串口连接按钮是按下去的，突然拔下了串口硬件
                {
                    if (m_send_fail_cnt == 30)
                    {
                        m_send_fail_cnt = 0;

                        m_b_start_update = false;
                        this.button_load_hex_file.Enabled = true;
                        this.button_serial_port_connect.Enabled = true;
                        this.button_serial_port_connect.Text = "CONNECT";
                        m_b_serialPortOpened = false;
                        LoadPicture();
                        this.comboBox_serial_port_baut_rate.Enabled = true;
                        this.comboBox_serial_port_name.Enabled = true;

                        this.button_start.Text = "UPDATE_START";
                        m_send_step = SEND_STEP.STEP_PRE_SEND_FAIL;

                        m_strLog_list.Add(LOG_DISCONNECT);
                        show_log();
                    }
                    else
                    {
                        m_send_fail_cnt++;

                    }
                }
            }
        }


        //实时检测串口，自动添加和减少
        private void checking_serial_port()
        {
            string[] names = SerialPort.GetPortNames();   //获取当前serial port端口名称

            if (names.Length == 0 )  //如果一个端口都没有，返回
            {
                return;
            }

            //将当前获取的端口进行排序
            Array.Sort(names, (a, b) => Convert.ToInt32(((string)a).Substring(3)).CompareTo(Convert.ToInt32(((string)b).Substring(3))));
            int nCount = 0;

            if (m_oldSerialPortNames!=null&&names.Length == m_oldSerialPortNames.Length) //不能进行names==m_oldSerialPortNames的判断
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] == m_oldSerialPortNames[i])  //将每一项进行比较
                    {
                        nCount++;                          //存在一种可能：length相同，但是具体的端口不一样
                    }
                }
                if (nCount == names.Length)  //如果每个都相同
                {
                    return;
                }
                else               //如果存在length一样，但是具体端口不一样         
                {
                    m_oldSerialPortNames = names;  //如果不匹配，将新的值赋给旧的值
                }
            }
            else
            {
                m_oldSerialPortNames = names;
            }

            this.comboBox_serial_port_name.Items.Clear();
            Array.Sort(names, (a, b) => Convert.ToInt32(((string)a).Substring(3)).CompareTo(Convert.ToInt32(((string)b).Substring(3))));
            this.comboBox_serial_port_name.Items.AddRange(names);
            this.comboBox_serial_port_name.SelectedIndex = 0;
        }

        private void button_load_hex_file_Click(object sender, EventArgs e)
        {
            checking_loaded_file();
        }

        private void checking_loaded_file()
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //检测文件正确性
                string filePath = this.openFileDialog1.FileName;
                int pos = filePath.LastIndexOf(@"\");
                string fileName = filePath.Substring(pos + 1);
                m_fileName = fileName;
                if (fileName.Substring(fileName.LastIndexOf(@".") + 1) != "hex")
                {
                    MessageBox.Show("Please choose hex file!");
                    return;
                }

                Init_member_var();

                #region
                this.textBox_file_path.Text = this.openFileDialog1.FileName;
                FileStream fs = new FileStream(this.openFileDialog1.FileName, FileMode.Open);
                //MessageBox.Show(Convert.ToString(fs.Length));  
                BinaryReader br = new BinaryReader(fs, Encoding.ASCII);

                long hex_len = fs.Length;     //获取文件长度

                //将文件读取到m_hex_file_list链表中
                for (long i = 0; i < hex_len; i++)
                {
                    m_hex_file_list.Add(br.ReadByte());
                }

                //解析HHX文件
                Parse_Hex_2_bin_list();

                if (m_hex_file_list.Count == hex_len)
                {
                    m_b_load_hex_file_success = true;
                    this.button1.Enabled = true;

                    //计算要发送多少个64字节
                    int bin_len = m_datas_list.Count * 16;

                    m_total_to_be_written_num = bin_len % WRITE_STEP == 0 ? (bin_len / WRITE_STEP) : (bin_len / WRITE_STEP + 1);
                    this.progressBar1.Maximum = Convert.ToInt32(m_total_to_be_written_num);
                    //MessageBox.Show(Convert.ToString(m_64Bytes_written_cnt));

                    //将数据存入write_list中

                    for (int i = 0; i < m_datas_list.Count; i++)
                    {
                        for (int j = 0; j < 16; j++)
                        {
                            m_write_list.Add(m_datas_list[i][j]);
                        }
                    }

                    int left = m_write_list.Count % WRITE_STEP;

                    if (left != 0)
                    {
                        byte bt = 0xFF;
                        for (int i = 0; i < WRITE_STEP - left; i++)
                        {
                            m_write_list.Add(bt);
                        }
                    }

                    for(int i=0;i<m_strLog_list.Count;i++)
                    {
                        if (m_strLog_list[i].Contains(LOG_LOAD_HEX_FILE_SUCCESS))
                        {
                            m_strLog_list.RemoveAt(i);
                        }
                    }
                   

                    //if (m_strLog_list.Count==1)  //说明已经load到了hex文件
                    //{
                    //    m_strLog_list.Clear();
                    //}

                    m_strLog_list.Add(@"File Name: """ + m_fileName + @"""" + LOG_LOAD_HEX_FILE_SUCCESS);
                    show_log();
                }
                else
                {
                    MessageBox.Show("Load file fail!");
                    br.Close();
                    fs.Close();
                }

                br.Close();
                fs.Close();

                #endregion
            }
        }

        private void show_log()
        {
            string str = "";
            for (int i = 0; i < m_strLog_list.Count; i++)
            {
                str += m_strLog_list[i] + "\r\n";
            }
            this.richTextBox1.Text = str;
        }

        private void Init_member_var()
        {
            //     private bool m_b_serialPortOpened = false;

            ////hex文件相关
            //private List<byte> m_hex_file_list = new List<byte>();          //用来存放hex文件
            //private bool m_b_load_hex_file_success = false;
            //private List<List<byte>> m_datas_list = new List<List<byte>>();   //用来存放一条一条的数据
            //private List<byte> m_write_list = new List<byte>();           //使用该list来向串口写入数据

            ////串口写入相关
            //private const int START_ADDRESS = 0x08000000;                          //写入的起始地址
            //private const int WRITE_STEP = 0x40;                                   //写入数据的步长，每次0x100=256字节
            //private int m_current_Address = 0x00;
            //private long m_total_to_be_written_num = 0;                           //总共要写入的次数，每个一次是16字节
            //private int m_current_write_cnt = 0;                             //记录当前写入的次数
            //private bool m_b_start_update = false;
            //private int m_BOOT_ERASE_CMD = 0x43;
            //private List<byte> m_buffer = new List<byte>();
            //private string m_fileName = "";
            m_hex_file_list.Clear();
            m_b_load_hex_file_success = false;
            m_datas_list.Clear();
            m_write_list.Clear();

            m_current_Address = 0x00;
            m_total_to_be_written_num = 0;
            m_current_write_cnt = 0;
            m_pre_current_write_cnt = 0;
            m_buffer.Clear();

            m_send_fail_cnt = 0;
            //m_fileName = "";
        }

        private void Parse_Hex_2_bin_list()
        {
            bool b_start = false;
            //hex数据格式:3A +待解析的数据+ 0D 0A
            //首先将数据切割出来
            int len = m_hex_file_list.Count;
            for (int i = 0; i < len;)
            {
                if (b_start)
                {
                    List<byte> tmp_list = new List<byte>();
                    //直接将16条数据全部添加到tmp_list中
                    //特别注意 比如数据0x48 是由0x34(4)和0x38(8)合成的
                    for (int j = 0; j < 16; j++) //循环16次
                    {
                        char ch1 = Convert.ToChar(m_hex_file_list[i++]);
                        char ch2 = Convert.ToChar(m_hex_file_list[i++]);

                        short st1 = convert_char_2_num(ch1);   //将char转换成对应的数字
                        short st2 = convert_char_2_num(ch2);

                        st1 <<= 4;
                        st1 |= st2;

                        byte bt = (byte)st1;

                        tmp_list.Add(bt);
                    }

                    m_datas_list.Add(tmp_list);

                    i += 4;  //拨到下一条数据上去
                    b_start = false;
                }
                else
                {
                    if (m_hex_file_list[i] == 0x3A && m_hex_file_list[i + 1] == 0x31 && m_hex_file_list[i + 2] == 0x30)     //如果是 3A 31 10(:10)才认为是找到了头
                    {
                        i += 9;      //指针拨到数据的第一个字节
                        b_start = true;
                    }
                    else if (m_hex_file_list[i] == 0x3A && m_hex_file_list[i + 1] == 0x30 && m_hex_file_list[i + 2] == 0x32)   //直接过滤掉  3A 30 32(:02),跳过
                    {
                        i += 17;   //碰到表示地址的，直接跳到下一条数据
                    }
                    else if (m_hex_file_list[i] == 0x3A && m_hex_file_list[i + 1] == 0x30 && m_hex_file_list[i + 2] == 0x34)  //如果到这里来了,结束
                    {
                        i = m_hex_file_list.Count;
                    }
                }

            }
        }


        private short convert_char_2_num(char ch)
        {
            #region
            if (ch == '0')
            {
                return (short)0;
            }
            else if (ch == '1')
            {
                return (short)1;
            }
            else if (ch == '2')
            {
                return (short)2;
            }
            else if (ch == '3')
            {
                return (short)3;
            }
            else if (ch == '4')
            {
                return (short)4;
            }
            else if (ch == '5')
            {
                return (short)5;
            }
            else if (ch == '6')
            {
                return (short)6;
            }
            else if (ch == '7')
            {
                return (short)7;
            }
            else if (ch == '8')
            {
                return (short)8;
            }
            else if (ch == '9')
            {
                return (short)9;
            }
            else if (ch == 'A')
            {
                return (short)10;
            }
            else if (ch == 'B')
            {
                return (short)11;
            }
            else if (ch == 'C')
            {
                return (short)12;
            }
            else if (ch == 'D')
            {
                return (short)13;
            }
            else if (ch == 'E')
            {
                return (short)14;
            }
            else if (ch == 'F')
            {
                return (short)15;
            }
            else
            {
                return (short)0;
            }
            #endregion
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(m_b_load_hex_file_success)
            {
                //TODO
                //生成一个 .bin 文件
                if (this.saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    if(m_datas_list!=null&& m_datas_list.Count!=0)
                    {
                        String str = this.saveFileDialog1.FileName;

                        str = str.Insert(str.IndexOf('.'), "");
                        FileStream fs1 = null;
                        try
                        {
                            fs1 = new FileStream(str, FileMode.Create);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            return ;
                        }

                        //StreamWriter sw1 = new StreamWriter(fs1, Encoding.Default);
                        //BinaryReader br1 = new BinaryReader(fs1, Encoding.ASCII);
                        BinaryWriter bw1 = new BinaryWriter(fs1, Encoding.ASCII);

                        for(int i=0;i< m_datas_list.Count;i++)
                        {
                            for(int j=0;j< m_datas_list[0].Count; j++)
                            {
                                bw1.Write(m_datas_list[i][j]);
                            }
                        }

                        bw1.Close();
                        fs1.Close();
                    }
                    
                }

            }
            else
            {
                MessageBox.Show("Please load hex file first");
            }
        }

        

        private void button_serial_port_connect_Click(object sender, EventArgs e)
        {
            m_b_serialPortOpened = !m_b_serialPortOpened;
            if (m_b_serialPortOpened)
            {
                try
                {
                    this.serialPort1.Open();
                }
                catch (Exception ex)
                {
                    m_b_serialPortOpened = false;
                    MessageBox.Show(ex.Message);
                    return;
                }
                this.button_serial_port_connect.Text = "DISCONNECT";

                button_start.Enabled = true;

                m_b_serialPortOpened = true;
                LoadPicture();

                this.comboBox_serial_port_name.Enabled = false;
                this.comboBox_serial_port_baut_rate.Enabled = false;
                this.comboBox_serial_port_data_bits.Enabled = false;
                this.comboBox_serial_port_flow_control.Enabled = false;
                this.comboBox_serial_port_parity.Enabled = false;
                this.comboBox_serial_port_stop_bits.Enabled = false;
            }
            else
            {

                this.button_serial_port_connect.Text = "CONNECT";
                this.serialPort1.Close();
                m_b_serialPortOpened = false;
                LoadPicture();

                this.comboBox_serial_port_name.Enabled = true;
                this.comboBox_serial_port_baut_rate.Enabled = true;
                this.comboBox_serial_port_data_bits.Enabled = true;
                this.comboBox_serial_port_flow_control.Enabled = true;
                this.comboBox_serial_port_parity.Enabled = true;
                this.comboBox_serial_port_stop_bits.Enabled = true;

            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (m_b_serialPortOpened == false)
            {
                return;
            }

            var nPendingRead = this.serialPort1.BytesToRead;   //有多少字节需要读取
            byte[] tmp = new byte[nPendingRead];
            this.serialPort1.Read(tmp, 0, nPendingRead);       //读取数据到tmp中
            
            lock (m_buffer)
            {
                m_buffer.AddRange(tmp);   //将tmp中的数据装入m_buffer中
                #region
                if (m_send_step == SEND_STEP.STEP2)   //获取version+cmd
                {
                    if (m_buffer.Count == 16 && m_buffer[1] == 0x79)
                    {

                        if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                        }

                        m_BOOT_ERASE_CMD = m_buffer[9 + 1];  //获取擦除命令

                        string strTmp = LOG_COMMAND_AVAILABLE;
                        for (int i = 1; i < m_buffer.Count; i++)
                        {
                            strTmp += m_buffer[i].ToString("X2") + " ";
                        }
                        m_strLog_list.Add(strTmp);

                        m_buffer.Clear();
                        show_log();

                        m_send_step = SEND_STEP.STEP3;
                        send_data_by(m_send_step);
                    }
                    else if(m_buffer.Count == 15 && m_buffer[0] == 0x79)   //调试发现，有时候也会出现这种情况
                    {
                        if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                        }

                        m_BOOT_ERASE_CMD = m_buffer[9];  //获取擦除命令

                        string strTmp = LOG_COMMAND_AVAILABLE;
                        for (int i = 0; i < m_buffer.Count; i++)
                        {
                            strTmp += m_buffer[i].ToString("X2") + " ";
                        }
                        m_strLog_list.Add(strTmp);

                        m_buffer.Clear();
                        show_log();

                        m_send_step = SEND_STEP.STEP3;
                        send_data_by(m_send_step);
                    }
                    else if (m_buffer.Count == 1 && m_buffer[0] == 0x1F)
                    {
                        resend_via_thread();
                    }
                    else if (m_buffer.Count >= 2 && m_buffer[1] != 0x79)
                    {
                        resend_via_thread();
                    }
                    else
                    {
                        //do nothing
                    }
                }
                else if (m_send_step == SEND_STEP.STEP3)  //获取bootloader version
                {
                    if (m_buffer.Count == 5)
                    {
                        if (m_buffer[0] == 0x1F && m_buffer[1] == 0x1F && m_buffer[2] == 0x1F &&
                            m_buffer[3] == 0x1F && m_buffer[4] == 0x1F)
                        {
                            m_b_start_update = false;
                            this.button_load_hex_file.Enabled = true;
                            this.button_serial_port_connect.Enabled = true;

                            this.button_start.Text = "UPDATE_START";

#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                            m_send_step = SEND_STEP.STEP_PRE_SEND_FAIL;

                            m_strLog_list.Add(LOG_TIME_OUT);
                            show_log();
                        }
                        else
                        {

                            if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                            {
#pragma warning disable CS0618 // Type or member is obsolete
                                m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                            }

                            string strTmp = LOG_BOOTLOADER_VERSION;
                            for (int i = 0; i < m_buffer.Count; i++)
                            {
                                strTmp += m_buffer[i].ToString("X2") + " ";
                            }
                            m_strLog_list.Add(strTmp);

                            m_buffer.Clear();
                            show_log();

                            m_send_step = SEND_STEP.STEP4;
                            send_data_by(m_send_step);
                        }
                        
                    }
                }
                else if (m_send_step == SEND_STEP.STEP4)  //获取芯片PID
                {
                    if (m_buffer.Count == 5)
                    {
                        if (m_buffer[0] == 0x1F && m_buffer[1] == 0x1F && m_buffer[2] == 0x1F &&
                            m_buffer[3] == 0x1F && m_buffer[4] == 0x1F)
                        {
                            m_b_start_update = false;
                            this.button_load_hex_file.Enabled = true;
                            this.button_serial_port_connect.Enabled = true;

                            this.button_start.Text = "UPDATE_START";

#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                            m_send_step = SEND_STEP.STEP_PRE_SEND_FAIL;

                            m_strLog_list.Add(LOG_TIME_OUT);
                            show_log();
                        }
                        else
                        {
                            if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                            {
#pragma warning disable CS0618 // Type or member is obsolete
                                m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                            }

                            string strTmp = LOG_PID;
                            for (int i = 0; i < m_buffer.Count; i++)
                            {
                                strTmp += m_buffer[i].ToString("X2") + " ";
                            }
                            m_strLog_list.Add(strTmp);

                            m_buffer.Clear();
                            show_log();

                            m_send_step = SEND_STEP.STEP5_1;
                            send_data_by(m_send_step);
                        }
                        
                    }
                }
                else if (m_send_step == SEND_STEP.STEP5_1)  //在指定的内存读取FLASH容量，SRAM容量，96位芯片唯一序列号
                {
                    if (m_buffer.Count == 23)
                    {
                        if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                        }

                        //Falsh容量
                        string str_Flash_cap = LOG_FLASH_CAPACITY;
                        str_Flash_cap += Convert.ToString(m_buffer[3] + m_buffer[4] * 256) + "KB";
                        m_strLog_list.Add(str_Flash_cap);

                        //SRAM容量
                        string str_SRAM_cap = LOG_SRAM_CAPACITY;
                        str_SRAM_cap += Convert.ToString(m_buffer[5] + m_buffer[6] * 256) + "KB";
                        m_strLog_list.Add(str_SRAM_cap);

                        //芯片唯一ID
                        string str_chip_ID = LOG_CPU_ID;
                        for (int i = 11; i <= m_buffer.Count - 1; i++)
                        {
                            str_chip_ID += m_buffer[i].ToString("X2");
                        }
                        m_strLog_list.Add(str_chip_ID);

                        m_buffer.Clear();
                        show_log();

                        m_send_step = SEND_STEP.STEP5_2;
                        send_data_by(m_send_step);
                    }
                }
                else if (m_send_step == SEND_STEP.STEP5_2)  //读取指定内存内容
                {
                    if (m_buffer.Count == 19)
                    {
                        if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                        }

                        string str_tmp = LOG_READ_BYTES;

                        for (int i = 3; i < m_buffer.Count; i++)
                        {
                            str_tmp += m_buffer[i].ToString("X2");
                        }
                        m_strLog_list.Add(str_tmp);

                        m_buffer.Clear();
                        show_log();

                        m_send_step = SEND_STEP.STEP_ERASE1;
                        send_data_by(m_send_step);
                    }
                }
                else if (m_send_step == SEND_STEP.STEP_ERASE1)
                {
                    if (m_buffer.Count == 1 && m_buffer[0] == 0x79)
                    {
                        if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                        }

                        m_buffer.Clear();

                        m_send_step = SEND_STEP.STEP_ERASE2;
                        send_data_by(m_send_step);
                    }
                    else
                    {
                        m_send_step = SEND_STEP.STEP_PRE_SEND_FAIL;

                        m_buffer.Clear();
                        string str_fail = LOG_ERASE_MCU_FAIL;
                        m_strLog_list.Add(str_fail);
                        show_log();
                    }
                }
                else if (m_send_step == SEND_STEP.STEP_ERASE2)
                {
                    if (m_buffer.Count == 1 && m_buffer[0] == 0x79)
                    {
                        if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                        }
                        m_buffer.Clear();

                        if (m_b_load_hex_file_success)
                        {
                            //string str_success = "Erase MCU successful!" + "\r\nStaring update...";
                            string str_success = LOG_ERASE_MCU_SUCCESSUL + "\r\n"+LOG_UPDATE_STARTING;
                            m_strLog_list.Add(str_success);
                            show_log();

                            m_send_step = SEND_STEP.STEP_WRITE;
                            send_data_by(m_send_step);
                            m_current_write_cnt++;
                        }
                    }
                    else
                    {
                        m_send_step = SEND_STEP.STEP_PRE_SEND_FAIL;

                        m_buffer.Clear();
                        string str_fail = "Erase MCU fail!";
                        m_strLog_list.Add(str_fail);
                        show_log();
                    }
                }
                else if (m_send_step == SEND_STEP.STEP_WRITE)
                {
                    if (m_buffer.Count == 3 && m_buffer[0] == 0x79 && m_buffer[1] == 0x79 && m_buffer[2] == 0x79)
                    {
                        if (m_thread_resend.ThreadState == ThreadState.Running || m_thread_resend.ThreadState == ThreadState.WaitSleepJoin)
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                        }

                        #region
                        //if (m_total_to_be_written_num == m_current_write_cnt)
                        //{
                        //    m_current_write_cnt = 0;
                        //    m_send_step = SEND_STEP.STEP_FINISH;
                        //    MessageBox.Show("finish");
                        //}
                        //else
                        //{
                        //    m_current_write_cnt++;  //收到应答表示接收成功， 计数加1
                        //    //m_send_step = SEND_STEP.STEP_READ;
                        //    m_send_step = SEND_STEP.STEP_WRITE;
                        //    m_buffer.Clear();

                        //    this.label_cnt.Text = (m_current_write_cnt).ToString() + "/" + m_total_to_be_written_num.ToString();
                        //}
                        #endregion
                        //m_send_step = SEND_STEP.STEP_READ;
                        if (m_total_to_be_written_num == m_current_write_cnt)
                        {

                            m_send_step = SEND_STEP.STEP_FINISH;

                            send_data_by(SEND_STEP.STEP_START_RUNNING);

                            //m_current_write_cnt = 0;

                            m_strLog_list.RemoveAt(m_strLog_list.Count - 1);
                            m_strLog_list.Add("Update Finished!");
                            show_log();

                            this.button_load_hex_file.Enabled = true;
                            this.button_serial_port_connect.Enabled = true;
                            m_b_start_update = false;
                            this.button_start.Text = "UPDATE START";

                            MessageBox.Show("Update Finished!");
                            //点完提示信息的按钮之后，清空log和进度条
                            m_strLog_list.Clear();
                            this.richTextBox1.Text = "";
                            //show_log();
                            this.progressBar1.Value = 0;
                        }
                        else
                        {
                            m_buffer.Clear();
                            m_send_step = SEND_STEP.STEP_WRITE;
                            send_data_by(m_send_step);
                            m_current_write_cnt++;  //收到应答表示接收成功， 计数加1

                            this.progressBar1.Value = m_current_write_cnt;
                        }

                        
                        //this.label_cnt.Text = (m_current_write_cnt*100/ m_total_to_be_written_num).ToString() +"%" +
                        //    (m_current_write_cnt).ToString() + "/" + m_total_to_be_written_num.ToString();
                        this.label_cnt.Text = (m_current_write_cnt * 100 / m_total_to_be_written_num).ToString() + "%";
                    }
                    else if (m_buffer.Count == 3 && m_buffer[0] == 0x79 && m_buffer[1] == 0x79 && m_buffer[2] == 0x1F)
                    {
                        //出错处理

                        //m_buffer.Clear();

                        //m_current_write_cnt--;   //不管发送失败或成功，m_current_write_cnt都做了++，所以失败的时候一定要--
                        //send_data_by(m_send_step);

                        //m_current_write_cnt++;

                        m_b_start_update = false;
                        this.button_load_hex_file.Enabled = true;
                        this.button_serial_port_connect.Enabled = true;
                        this.button_start.Text = "UPDATE_START";

                        m_send_step = SEND_STEP.STEP_SEND_FAIL;

                        m_strLog_list.Add(LOG_UPDATE_FAIL);
                        show_log();
                    }
                    else
                    {
                        //do nothing
                    }
                }
                else if (m_send_step == SEND_STEP.STEP_READ)
                {
                    #region
                    //if (m_buffer.Count >= 3+ WRITE_STEP)
                    //{

                    //    //string str_show = "";
                    //    //for (int i = 0; i < m_buffer.Count; i++)
                    //    //{
                    //    //    str_show += m_buffer[i].ToString("X2") + " ";
                    //    //}
                    //    //m_strLog_list.Add(str_show);
                    //    //show_log();
                    //    //return;
                    //    if (m_buffer.Count == WRITE_STEP + 3)
                    //    {
                    //        if (m_buffer[0] == 0x79 && m_buffer[1] == 0x79 && m_buffer[2] == 0x79)
                    //        {
                    //            //对比收到的数据和写入的数据是不是一样
                    //            int cnt = 0;
                    //            //string str_tmp = "";
                    //            for (int i = 0; i < WRITE_STEP; i++)
                    //            {
                    //                ////str_tmp += m_buffer[i + 3].ToString("X2") + " ";

                    //                //if (m_buffer[i + 3] == data_buffer[i + 1])
                    //                //{
                    //                //    cnt++;
                    //                //}

                    //                if (m_buffer[i + 3] == m_write_list[m_current_write_cnt*WRITE_STEP + i])
                    //                {
                    //                    cnt++;
                    //                }
                    //            }

                    //            //m_strLog_list.Add(str_tmp);
                    //            //show_log();

                    //            // if (cnt == m_buffer.Count - 3)  //如果cnt==64
                    //            {
                    //                if (m_total_to_be_written_num == m_current_write_cnt)
                    //                {
                    //                    m_send_step = SEND_STEP.STEP_FINISH;
                    //                    MessageBox.Show("finish");
                    //                }
                    //                else
                    //                {
                    //                    m_send_step = SEND_STEP.STEP_WRITE;
                    //                    ++m_current_write_cnt;     //对比完全一样之后，才算写成功一次

                    //                    label_cnt.Text = Convert.ToString(m_current_write_cnt);
                    //                    //MessageBox.Show("got");
                    //                }
                    //            }
                    //            //else
                    //            //{
                    //            //    //如果不一样，重复发送，再来一次
                    //            //    //TODO
                    //            //    MessageBox.Show("出错");
                    //            //}
                    //        }
                    //        m_buffer.Clear();
                    //    }
                    //    else if (m_buffer[0] != 0x79 || m_buffer[1] != 0x79 || m_buffer[2] != 0x79)
                    //    {
                    //        m_buffer.Clear();
                    //    }
                    //}
                    #endregion
                }
                #endregion
            }
        }

        private void resend_via_thread()
        {
            if (m_send_fail_cnt >= 20)
            {
                m_send_fail_cnt = 0;

                m_b_start_update = false;
                this.button_load_hex_file.Enabled = true;
                this.button_serial_port_connect.Enabled = true;

                this.button_start.Text = "UPDATE_START";

#pragma warning disable CS0618 // Type or member is obsolete
                m_thread_resend.Suspend();
#pragma warning restore CS0618 // Type or member is obsolete
                m_send_step = SEND_STEP.STEP_PRE_SEND_FAIL;

                m_strLog_list.Add(LOG_TIME_OUT);
                show_log();
            }
            else
            {
                if (m_thread_resend.ThreadState == ThreadState.Unstarted)
                {
                    m_thread_resend.Start();
                }

                m_buffer.Clear();
                m_send_fail_cnt++;
            }
        }


        private void button_start_Click(object sender, EventArgs e)
        {
            if(!m_b_serialPortOpened)
            {
                MessageBox.Show("Serial port disconnect!");
                return;
            }

            //如果没有载入文件，不允许进行后面的动作
            if(!m_b_load_hex_file_success)
            {
                MessageBox.Show("Please Load hex file!");
                return;
            }


            m_b_start_update = !m_b_start_update;
            if(m_b_start_update)
            {
                //重复发送的时候，线程由suspended变成resume
                if (m_thread_resend.ThreadState == ThreadState.Suspended)
                {
                    m_send_fail_cnt = 0;
#pragma warning disable CS0618 // Type or member is obsolete
                    m_thread_resend.Resume();
#pragma warning restore CS0618 // Type or member is obsolete
                }
                

                m_strLog_list.Clear();
                this.richTextBox1.Text = "";
                //show_log();

                if (m_b_load_hex_file_success)
                {
                    m_strLog_list.Add(@"File Name: """ + m_fileName +@""""+ LOG_LOAD_HEX_FILE_SUCCESS);
                }

                //show_log();

                label_cnt.Text = "0%";
                m_buffer.Clear();

                m_current_Address = 0x00;
                m_current_write_cnt = 0;
                m_pre_current_write_cnt = 0;
                this.progressBar1.Value = m_current_write_cnt;

                //开始传输数据了，串口连接按钮 和 文件load按钮 必须要disable
                this.button_load_hex_file.Enabled = false;
                this.button_serial_port_connect.Enabled = false;

                this.button_start.Text = "UPDATE_STOP";

                m_strLog_list.Add(LOG_CONNECTING_DEVICE);
                show_log();

                m_send_step = SEND_STEP.STEP2;
                send_data_by(m_send_step);

            }
            else
            {
                this.button_load_hex_file.Enabled = true;
                this.button_serial_port_connect.Enabled = true;

                this.button_start.Text = "UPDATE_START";

                m_send_step = SEND_STEP.STEP_STOP;

                m_strLog_list.Add(LOG_UPDATE_STOP);
                show_log();
            }
        }


        private void send_data_by(SEND_STEP step)
        {
            if (!this.serialPort1.IsOpen)
            {
                return;
            }

            if (m_b_serialPortOpened == false)
            {
                //MessageBox.Show("Serial port disconnect!");
                m_send_step = SEND_STEP.STEP_SEND_FAIL;
                return;
            }

            byte[] buffer = null;
            //if (step == SEND_STEP.STEP_RESEND)
            //{
            //    buffer = new byte[] { 0x7F,0x00,0xFF };
            //    this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));

            //    System.Threading.Thread.Sleep(500);
            //}
            if (step == SEND_STEP.STEP_START_RUNNING)   //作用: bootlader失败的时候，点按钮继续，先要reset，否则无法发送7F
            {
                buffer = new byte[] { 0x21, 0xDE, 0x08, 0x00, 0x00, 0x00, 0x08 };
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            //else if (step == SEND_STEP.STEP1)                 //第一步，发送0x7F
            //{
            //    buffer = new byte[1];
            //    buffer[0] = 0x7F;
            //    this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            //}
            else if (step == SEND_STEP.STEP2)            //第二步，发送00 FF,获取Version+Command
            {
                //buffer = new byte[2];
                //buffer[0] = 0x00;
                //buffer[1] = 0xFF;

                buffer = new byte[3];
                buffer[0] = 0x7F;
                buffer[1] = 0x00;
                buffer[2] = 0xFF;
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));

                ////this.button_start.Enabled = false;
                //System.Threading.Thread.Sleep(3000);
                ////System.Threading.Thread.Sleep(500);
            }
            else if (step == SEND_STEP.STEP3)          //第三步， 发送01 FE,获取版本号
            {
                buffer = new byte[2];
                buffer[0] = 0x01;
                buffer[1] = 0xFE;
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (step == SEND_STEP.STEP4)          //第四步， 发送02 FD,获取芯片PID
            {
                buffer = new byte[2];
                buffer[0] = 0x02;
                buffer[1] = 0xFD;
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (step == SEND_STEP.STEP5_1)          //第五步， 1>发送11 EE 1F FF F7 E0 F7 13 EC,读取指定内存
            {
                buffer = new byte[] { 0x11, 0xEE, 0x1F, 0xFF, 0xF7, 0xE0, 0xF7, 0x13, 0xEC };
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (step == SEND_STEP.STEP5_2)          //第五步， 2>发送11 EE 1F FF F8 00 18 0F F0读取指定内存
            {
                buffer = new byte[] { 0x11, 0xEE, 0x1F, 0xFF, 0xF8, 0x00, 0x18, 0x0F, 0xF0 };
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (step == SEND_STEP.STEP_ERASE1)            //第六步，擦除芯片，先发送44 BB //擦除必须要分开发
            {
                if (m_BOOT_ERASE_CMD == 0x43)
                {
                    buffer = new byte[] { 0x43, 0xBC };
                }
                else if (m_BOOT_ERASE_CMD == 0x44)
                {
                    buffer = new byte[] { 0x44, 0xBB };
                }
                else
                {
                    //do nothing
                }

                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (step == SEND_STEP.STEP_ERASE2)            //第六步，擦除芯片，再发送FF FF 00
            {
                if (m_BOOT_ERASE_CMD == 0x43)
                {
                    buffer = new byte[] { 0xFF, 0x00 };
                }
                else if (m_BOOT_ERASE_CMD == 0x44)
                {
                    buffer = new byte[] { 0xFF, 0xFF, 0x00 };
                }
                else
                {
                    //do nothing
                }

                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (step == SEND_STEP.STEP_WRITE)            //核心部分，循环写入hex文件，每次写入64字节，然后读取该64字节核对
            {
                if (m_b_load_hex_file_success)             //如果载入文件成功
                {
                    //if (m_current_write_cnt < m_total_to_be_written_num)
                    {
                        #region
                        ////将数据存入write_list中
                        //for (int i = 0; i < m_datas_list.Count; i++)
                        //{
                        //    for (int j = 0; j < 16; j++)
                        //    {
                        //        m_write_list.Add(m_datas_list[i][j]);
                        //    }
                        //}
                        #endregion

                        //开始写地址
                        //Array.Clear(m_send_buffer, 0, m_send_buffer.Length);
                        buffer = new byte[9 + WRITE_STEP];    //256个字节
                        byte[] data_buffer = new byte[1 + WRITE_STEP];

                        for (int i = 0; i < buffer.Length; i++)     //初始化m_send_buffer全部为FF
                        {
                            buffer[i] = 0xFF;
                        }

                        m_current_Address = START_ADDRESS + m_current_write_cnt * WRITE_STEP;
                        buffer[0] = 0x31;
                        buffer[1] = 0xCE;

                        //填充地址
                        byte[] address = convert_address_2_bytes(m_current_Address);
                        buffer[2] = address[0];
                        buffer[3] = address[1];
                        buffer[4] = address[2];
                        buffer[5] = address[3];

                        //填充地址的checksum
                        buffer[6] = cal_checksum(address, address.Length);

                        //发送的字节长度为0x3F+1=64
                        buffer[7] = Convert.ToByte(WRITE_STEP - 1);

                        //填充数据
                        int shift_pos = m_current_write_cnt * WRITE_STEP;  //偏移64的倍数

                        for (int i = 0; i < WRITE_STEP + 1; i++)
                        {
                            if (i == 0)
                            {
                                data_buffer[0] = Convert.ToByte(WRITE_STEP - 1);   //256个字节
                            }
                            else
                            {
                                //填充256个字节
                                data_buffer[i] = m_write_list[shift_pos + i - 1];
                                buffer[7 + i] = data_buffer[i];
                            }
                        }
                        //填充数据的checksum
                        buffer[buffer.Length - 1] = cal_checksum(data_buffer, data_buffer.Length);
                        //++m_current_write_cnt;        //写完了就累加一次

                        #region
                        //debug
                        //string str_tmp = "";
                        //for (int i = 0; i < send_buffer.Length; i++)
                        //{
                        //    str_tmp += send_buffer[i].ToString("X2") + " ";
                        //}
                        //m_strLog_list.Add(str_tmp);
                        //show_log();
                        #endregion

                        this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
                    }
                }
            }
            else if (step == SEND_STEP.STEP_READ)     //发送命令读取上一次的数据
            {
                //读取上一次write的数据   11 EE 08 00 00 00 08 3F C0
                buffer = new byte[9];
                buffer[0] = 0x11;
                buffer[1] = 0xEE;

                byte[] address = convert_address_2_bytes(m_current_Address - WRITE_STEP);
                buffer[2] = address[0];
                buffer[3] = address[1];
                buffer[4] = address[2];
                buffer[5] = address[3];

                //填充地址的checksum
                buffer[6] = cal_checksum(address, address.Length);

                buffer[7] = Convert.ToByte(WRITE_STEP - 1);
                buffer[8] = 0xC0;

                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
        }

        private byte[] convert_address_2_bytes(int address)
        {

            int tmp1 = address / 256 / 256;
            int tmp2 = address % (256 * 256);

            byte[] bt = new byte[4];
            bt[0] = Convert.ToByte(tmp1 / 256);
            bt[1] = Convert.ToByte(tmp1 % 256);
            bt[2] = Convert.ToByte(tmp2 / 256);
            bt[3] = Convert.ToByte(tmp2 % 256);

            return bt;
        }

        private byte cal_checksum(byte[] address, int len)
        {
            byte res = 0x00;
            for (int i = 0; i < len; i++)
            {
                res ^= address[i];  //异或
            }
            return res;
        }

        private void comboBox_serial_port_baut_rate_SelectedValueChanged(object sender, EventArgs e)
        {
            if (this.comboBox_serial_port_baut_rate.Text == "115200")
            {
                this.serialPort1.BaudRate = 115200;
            }
            else if (this.comboBox_serial_port_baut_rate.Text == "128000")
            {
                this.serialPort1.BaudRate = 128000;
            }
            else if (this.comboBox_serial_port_baut_rate.Text == "230400")
            {
                this.serialPort1.BaudRate = 230400;
            }
            else if (this.comboBox_serial_port_baut_rate.Text == "256000")
            {
                this.serialPort1.BaudRate = 256000;
            }
            else if (this.comboBox_serial_port_baut_rate.Text == "57600")
            {
                this.serialPort1.BaudRate = 57600;
            }
            else if (this.comboBox_serial_port_baut_rate.Text == "56000")
            {
                this.serialPort1.BaudRate = 56000;
            }
            else if (this.comboBox_serial_port_baut_rate.Text == "38400")
            {
                this.serialPort1.BaudRate = 38400;
            }
            else if (this.comboBox_serial_port_baut_rate.Text == "19200")
            {
                this.serialPort1.BaudRate = 19200;
            }
            else
            {
                //do nothing
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                this.serialPort1.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //关闭线程
            try
            {
                m_thread_resend.Abort();
            }
            catch (ThreadStateException)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                m_thread_resend.Resume();
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
    }
}
