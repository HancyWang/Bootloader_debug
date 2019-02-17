﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO.Ports;
using System.IO;

namespace Bootloader_debug
{
    public partial class Form1 : Form
    {

        private string[] m_oldSerialPortNames;
        private bool m_b_serialPortOpened = false;
        private List<byte> m_buffer = new List<byte>();
        //private bool m_b_startSending = false;
        private bool m_b_load_hex_file_success = false;

        private List<byte> m_hex_file_list = new List<byte>();          //用来存放hex文件
        private List<List<byte>> m_datas_list = new List<List<byte>>();   //用来存放一条一条的数据
        //private Dictionary<int, List<byte>> m_dic_datas_list = new Dictionary<int, List<byte>>();

        
        private List<byte> m_bin_list = new List<byte>();               //用来存放解析hex之后的bin
        private List<string> m_strLog_list = new List<string>();        //用来存放log

        private const int START_ADDRESS = 0x08000000;                          //写入的起始地址
        private const int WRITE_STEP = 0x40;                                   //写入数据的步长，每次0x100=256字节
        private int m_current_Address = 0x00;
        private long m_total_to_be_written_num = 0;                           //总共要写入的次数
        private int m_current_write_cnt = 0;                             //记录当前写入的次数
        private int m_write_fail_cnt = 0;

        //private int m_shift_pos = 0;
        private List<byte> m_write_list = new List<byte>();

        private int m_stop_cnt = 0;         //传输中断计数
        private int m_prev_send_cnt = -1;

        //dubeg专用
        //private byte test_bt = 0x00;
        //private string test_str = "";
        //private short test_st = 0;
        //private Int16 test_int16 = 0;
        //private char test_ch = '0';
        //private List<byte> test_bt_list = new List<byte>();

        private enum SEND_STEP
        {
            STEP_NONE,
            STEP1,
            STEP2,
            STEP3,
            STEP4,
            STEP5_1,
            STEP5_2,
            STEP_ERASE1,
            STEP_ERASE2,
            STEP_PENDING_HEX_FILE_LOADED,
            STEP_WRITE,
            STEP_READ,
            STEP_FINISH
        }

        private SEND_STEP m_send_step = SEND_STEP.STEP1;

        public Form1()
        {
            InitializeComponent();
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
            this.comboBox_serial_port_data_bits.Text = "8";
            this.comboBox_serial_port_stop_bits.Text = "1";
            this.comboBox_serial_port_parity.Text = "Odd";
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

        private void comboBox_serial_port_name_SelectedValueChanged(object sender, EventArgs e)
        {
            this.serialPort1.PortName = this.comboBox_serial_port_name.Text;
        }

        private void timer_serial_port_checking_Tick(object sender, EventArgs e)
        {
            //debug
            #region
            if (m_current_write_cnt>=10)
            {
                if (m_stop_cnt == 50)
                {
                    m_stop_cnt = 0;
                    //MessageBox.Show("wrong");
                }
                else
                {
                    if (m_prev_send_cnt == m_current_write_cnt)
                    {
                        m_stop_cnt++;
                    }
                    else
                    {
                        m_stop_cnt = 0;
                        m_prev_send_cnt = m_current_write_cnt;
                    }
                }
            }
            #endregion

            string[] names = SerialPort.GetPortNames();   //获取当前serial port端口名称

            if (names.Length == 0 || m_oldSerialPortNames == null)  //如果一个端口都没有，返回
            {
                return;
            }

            //将当前获取的端口进行排序
            Array.Sort(names, (a, b) => Convert.ToInt32(((string)a).Substring(3)).CompareTo(Convert.ToInt32(((string)b).Substring(3))));
            int nCount = 0;

            if (names.Length == m_oldSerialPortNames.Length) //不能进行names==m_oldSerialPortNames的判断
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
            if(m_b_serialPortOpened==false)
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
                if (m_send_step == SEND_STEP.STEP1)
                {
                    if (m_buffer[0] == 0x79)   //如果是79，就接收数据
                    {
                        m_send_step = SEND_STEP.STEP2;      //如果接收到79，直接进入到第二步
                    }
                    m_buffer.RemoveAt(0);
                    send_data_by(m_send_step);
                }
                else if (m_send_step == SEND_STEP.STEP2)   //获取version+cmd
                {
                    if (m_buffer.Count == 15)
                    {
                        string strTmp = "Command available: ";
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
                }
                else if (m_send_step == SEND_STEP.STEP3)  //获取bootloader version
                {
                    if (m_buffer.Count == 5)
                    {
                        string strTmp = "Bootloader Version: ";
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
                else if (m_send_step == SEND_STEP.STEP4)  //获取芯片PID
                {
                    if (m_buffer.Count == 5)
                    {
                        string strTmp = "PID: ";
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
                else if (m_send_step == SEND_STEP.STEP5_1)  //在指定的内存读取FLASH容量，SRAM容量，96位芯片唯一序列号
                {
                    if (m_buffer.Count == 23)
                    {
                        //Falsh容量
                        string str_Flash_cap = "FLASH Capacity: ";
                        str_Flash_cap += Convert.ToString(m_buffer[3] + m_buffer[4] * 256) + "KB";
                        m_strLog_list.Add(str_Flash_cap);

                        //SRAM容量
                        string str_SRAM_cap = "SRAM Capacity: ";
                        str_SRAM_cap += Convert.ToString(m_buffer[5] + m_buffer[6] * 256) + "KB";
                        m_strLog_list.Add(str_SRAM_cap);

                        //芯片唯一ID
                        string str_chip_ID = "CPU ID: ";
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
                        string str_tmp = "Read Bytes: ";

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
                        m_buffer.Clear();

                        m_send_step = SEND_STEP.STEP_ERASE2;
                        send_data_by(m_send_step);
                    }
                    else
                    {
                        m_send_step = SEND_STEP.STEP_NONE;

                        m_buffer.Clear();
                        string str_fail = "Erase MCU fail!";
                        m_strLog_list.Add(str_fail);
                        show_log();
                    }
                }
                else if (m_send_step == SEND_STEP.STEP_ERASE2)
                {
                    if (m_buffer.Count == 1 && m_buffer[0] == 0x79)
                    {
                        m_buffer.Clear();

                        string str_success = "Erase MCU successful!"+"\r\nStaring update...";
                        m_strLog_list.Add(str_success);
                        show_log();

                        m_send_step = SEND_STEP.STEP_WRITE;
                        send_data_by(m_send_step);
                        m_current_write_cnt++;
                    }
                    else
                    {
                        m_send_step = SEND_STEP.STEP_NONE;

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
                            m_current_write_cnt = 0;
                            m_send_step = SEND_STEP.STEP_FINISH;

                            m_strLog_list.RemoveAt(m_strLog_list.Count - 1);
                            m_strLog_list.Add("Update Finished!");
                            show_log();
                            MessageBox.Show("finish");
                        }
                        else
                        {
                            m_buffer.Clear();
                            m_send_step = SEND_STEP.STEP_WRITE;
                            send_data_by(m_send_step);
                            m_current_write_cnt++;  //收到应答表示接收成功， 计数加1
                        }

                        this.progressBar1.Value = m_current_write_cnt;
                        this.label_cnt.Text = (m_current_write_cnt).ToString() + "/" + m_total_to_be_written_num.ToString();
                    }
                    else if (m_buffer.Count == 3 && m_buffer[0] == 0x79 && m_buffer[1] == 0x79 && m_buffer[2] == 0x1F)
                    {
                        m_buffer.Clear();

                        m_current_write_cnt--;   //不管发送失败或成功，m_current_write_cnt都做了++，所以失败的时候一定要--
                        send_data_by(m_send_step);

                        m_current_write_cnt++;
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

        private void show_log()
        {
            string str = "";
            for (int i = 0; i < m_strLog_list.Count; i++)
            {
                str += m_strLog_list[i]+"\r\n";
            }
            this.richTextBox1.Text = str;
        }

        private void timer_send_Tick(object sender, EventArgs e)
        {
            //if(m_b_serialPortOpened==false)             //如果串口没开，直接return
            //{
            //    return;
            //}

            //if (m_b_startSending)                       //按钮点击了发送
            //{
            //    send_data_by(m_send_step);
            //}
        }

        private void send_data_by(SEND_STEP step)
        {
            byte[] buffer = null;
            if (m_send_step == SEND_STEP.STEP1)                 //第一步，发送0x7F
            {
                buffer = new byte[1];
                buffer[0] = 0x7F;
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (m_send_step == SEND_STEP.STEP2)            //第二步，发送00 FF,获取Version+Command
            {
                buffer = new byte[2];
                buffer[0] = 0x00;
                buffer[1] = 0xFF;
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (m_send_step == SEND_STEP.STEP3)          //第三步， 发送01 FE,获取版本号
            {
                buffer = new byte[2];
                buffer[0] = 0x01;
                buffer[1] = 0xFE;
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (m_send_step == SEND_STEP.STEP4)          //第四步， 发送02 FD,获取芯片PID
            {
                buffer = new byte[2];
                buffer[0] = 0x02;
                buffer[1] = 0xFD;
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (m_send_step == SEND_STEP.STEP5_1)          //第五步， 1>发送11 EE 1F FF F7 E0 F7 13 EC,读取指定内存
            {
                buffer = new byte[] { 0x11, 0xEE, 0x1F, 0xFF, 0xF7, 0xE0, 0xF7, 0x13, 0xEC };
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (m_send_step == SEND_STEP.STEP5_2)          //第五步， 2>发送11 EE 1F FF F8 00 18 0F F0读取指定内存
            {
                buffer = new byte[] { 0x11, 0xEE, 0x1F, 0xFF, 0xF8, 0x00, 0x18, 0x0F, 0xF0 };
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (m_send_step == SEND_STEP.STEP_ERASE1)            //第六步，擦除芯片，先发送44 BB //擦除必须要分开发
            {
                buffer = new byte[] { 0x44, 0xBB };
                //buffer = new byte[] { 0x43, 0xBC };
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (m_send_step == SEND_STEP.STEP_ERASE2)            //第六步，擦除芯片，再发送FF FF 00
            {
                buffer = new byte[] { 0xFF, 0xFF, 0x00 };
                //buffer = new byte[] { 0xFF, 0x00 };
                this.serialPort1.Write(buffer, 0, Convert.ToInt32(buffer.Length));
            }
            else if (m_send_step == SEND_STEP.STEP_WRITE)            //核心部分，循环写入hex文件，每次写入64字节，然后读取该64字节核对
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
                                //if (m_shift_pos + i - 1 > m_write_list.Count)
                                //{
                                //    MessageBox.Show("err");
                                //}
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

                        //m_write_list.Clear();
                        #region
                        //debug，测试数据，
                        //List<byte> test_list = new List<byte>();
                        //for (int i = 1; i < m_data_buffer.Length; i++)
                        //{
                        //    test_list.Add(m_data_buffer[i]);
                        //}
                        //string str_tmp = "";
                        //int k = 0;
                        //for (int i = 0; i < test_list.Count; i++)
                        //{
                        //    if (k == 16)
                        //    {
                        //        k = 0;
                        //        str_tmp += "\r\n";
                        //    }

                        //    str_tmp += test_list[i].ToString("X2") + " ";

                        //    k++;

                        //}
                        //m_strLog_list.Add(str_tmp);
                        //show_log();
                        #endregion

                        //m_strLog_list.Add(m_current_write_cnt.ToString() + @"/" + m_total_to_be_written_num.ToString());
                        //show_log();
                        //m_strLog_list.RemoveAt(m_strLog_list.Count - 1);
                    }
                }
                else
                {
                    m_send_step = SEND_STEP.STEP_PENDING_HEX_FILE_LOADED;
                    MessageBox.Show("Please load your hex file!");
                }
            }
            else if (m_send_step == SEND_STEP.STEP_READ)     //发送命令读取上一次的数据
            {
                //读取上一次write的数据   11 EE 08 00 00 00 08 3F C0
                buffer = new byte[9];
                buffer[0] = 0x11;
                buffer[1] = 0xEE;

                byte[] address = convert_address_2_bytes(m_current_Address-WRITE_STEP);
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

        private byte cal_checksum(byte[] address,int len)
        {
            byte res = 0x00;
            for (int i = 0; i < len; i++)
            {
                res ^= address[i];  //异或
            }
            return res;
        }

        private void button_start_Click(object sender, EventArgs e)
        {
            //m_b_startSending = true;
            m_send_step = SEND_STEP.STEP1;
            send_data_by(m_send_step);
        }

        private void Init_member_var()
        {
            m_hex_file_list.Clear();
            m_bin_list.Clear();

            m_datas_list.Clear();
            m_strLog_list.Clear();
            m_buffer.Clear();
            m_write_list.Clear();

            m_b_load_hex_file_success = false;
            m_current_Address = 0x00;
            m_current_write_cnt = 0;
            m_total_to_be_written_num = 0;
            m_stop_cnt = 0;
            m_prev_send_cnt = -1;
        }

        private void button_load_hex_file_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {

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

                //if (m_hex_file_list.Count == len)
                if (m_hex_file_list.Count == hex_len)   
                {
                    if (m_send_step == SEND_STEP.STEP_PENDING_HEX_FILE_LOADED)  //状态切换
                    {
                        m_send_step = SEND_STEP.STEP_WRITE;
                    }
                    m_b_load_hex_file_success = true;

                    //计算要发送多少个64字节
                    int bin_len = m_datas_list.Count*16;
                    
                    m_total_to_be_written_num = bin_len % WRITE_STEP == 0 ? (bin_len/ WRITE_STEP) : (bin_len / WRITE_STEP + 1);
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

                    //MessageBox.Show("Load file successful!" + "16*" + m_datas_list.Count.ToString());
                    MessageBox.Show("Load file successful!");
                    //MessageBox.Show(m_datas_list.Count.ToString());
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

        private void Parse_Hex_2_bin_list()
        {
            //if (m_hex_file_list.Count > 0)
            {
                
                bool b_start = false;
                //hex数据格式:3A +待解析的数据+ 0D 0A
                //首先将数据切割出来
                int len = m_hex_file_list.Count;
                for (int i = 0; i < len; )
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
                        //添加到数据链表中
                        //m_dic_datas_list[m_data_num_cnt++] = tmp_list;
                        m_datas_list.Add(tmp_list);

                        //特别注意：困扰我多少的bug终于找到了,m_datas_list.Add()和m_dic_datas_list[m_data_num_cnt++] = tmp_list;
                        //这种操作都是直接将数据引用的，并不会自己去copy一份，所以tmp_list.Clear();直接将数据清空，会导致链表或者字典没有数据了
                        //tmp_list.Clear();


                        //if (m_data_num_cnt == 36388)
                        //{
                        //    int a = 0;
                        //}

                        i += 4;  //拨到下一条数据上去
                        b_start = false;
                    }
                    else
                    {
                        if (m_hex_file_list[i] == 0x3A && m_hex_file_list[i + 1] == 0x31&& m_hex_file_list[i + 2] == 0x30)     //如果是 3A 31 10(:10)才认为是找到了头
                        {
                            i += 9;      //指针拨到数据的第一个字节
                            b_start = true;
                        }
                        else if (m_hex_file_list[i] == 0x3A && m_hex_file_list[i + 1] == 0x30&& m_hex_file_list[i + 2] == 0x32)   //直接过滤掉  3A 30 32(:02),跳过
                        {
                            i += 17;   //碰到表示地址的，直接跳到下一条数据
                        }
                        else if(m_hex_file_list[i] == 0x3A && m_hex_file_list[i + 1] == 0x30 && m_hex_file_list[i + 2] == 0x34)  //如果到这里来了,结束
                        {
                            i = m_hex_file_list.Count;

                            ////debug
                            //string str_tmp = "";
                            //List<byte> list = m_datas_list[0];

                            //for (int m = 0; m < list.Count; m++)
                            //{
                            //    str_tmp += list[m].ToString("X2") + " ";
                            //}
                            //MessageBox.Show(str_tmp);
                        }
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


        private void comboBox_serial_port_baut_rate_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.comboBox_serial_port_baut_rate.Text == "115200")
            {
                this.serialPort1.BaudRate = 115200;
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
    }
}