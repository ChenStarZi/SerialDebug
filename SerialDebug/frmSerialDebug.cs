using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;


namespace SerialDebug
{
    public partial class frmMain : Form
    {

        private UInt64 RxCounter = 0;
        private UInt64 TxCounter = 0;
        private List<byte[]> reBytesList = new List<byte[]>();
        private Thread myThread;

        private delegate void UpdateRxTxDel(UInt64 txCount);          // ����ʱ������ʾί��
        UpdateRxTxDel myUpdateTx;
        UpdateRxTxDel myUpdateRx;
        private delegate void TextBoxAppendDel(string str);             // �ı�������ַ�
        TextBoxAppendDel txtReceiveAppend;


        public frmMain()
        {
            InitializeComponent();
            ////���벨����
            //cbBaudRate.Items.Add(110);
            //cbBaudRate.Items.Add(300);
            //cbBaudRate.Items.Add(600);
            //cbBaudRate.Items.Add(1200);
            //cbBaudRate.Items.Add(2400);
            //cbBaudRate.Items.Add(4800);
            //cbBaudRate.Items.Add(9600);
            //cbBaudRate.Items.Add(14400);
            //cbBaudRate.Items.Add(19200);
            //cbBaudRate.Items.Add(28800);
            //cbBaudRate.Items.Add(38400);
            //cbBaudRate.Items.Add(56000);
            //cbBaudRate.Items.Add(57600);
            //cbBaudRate.Items.Add(128000);
            //cbBaudRate.Items.Add(115200);
            //cbBaudRate.Items.Add(256000);
            ////cbBaudRate.SelectedItem = 9600;
            //cbBaudRate.Text = Convert.ToString(9600);

            //��ż����λ
            cbParity.Items.Add(System.IO.Ports.Parity.Even);
            cbParity.Items.Add(System.IO.Ports.Parity.Mark);
            cbParity.Items.Add(System.IO.Ports.Parity.None);
            cbParity.Items.Add(System.IO.Ports.Parity.Odd);
            cbParity.Items.Add(System.IO.Ports.Parity.Space);
            cbParity.SelectedItem = System.IO.Ports.Parity.None;


            //����λ
            cbDataBit.Items.Add(5);
            cbDataBit.Items.Add(6);
            cbDataBit.Items.Add(7);
            cbDataBit.Items.Add(8);
            cbDataBit.SelectedItem = 8;

            //ֹͣλ
            //cbStopBit.Items.Add(System.IO.Ports.StopBits.None);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.One);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.OnePointFive);
            cbStopBit.Items.Add(System.IO.Ports.StopBits.Two);
            cbStopBit.SelectedItem = System.IO.Ports.StopBits.One;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            picPortState.Image = ImageList.Images["close"];
            picTop.Image = imglistTop.Images["nailoff"];
            cbComName.DataSource = SerialPort.GetPortNames();



            this.Text = Application.ProductName + " V" + Application.ProductVersion.Substring(0, 3);

            CheckForIllegalCrossThreadCalls = false;

            myUpdateTx = new UpdateRxTxDel(UpdateTx);
            myUpdateRx = new UpdateRxTxDel(UpdateRx);
            txtReceiveAppend = new TextBoxAppendDel(TextBoxReceiveAppend);
        }


        #region ����������

        /// <summary>
        /// �򿪹رմ��ڲ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPortOpt_Click(object sender, EventArgs e)
        {
            try
            {
                if (btnPortOpt.Text == "�򿪴���")
                {

                    myThread = new Thread(new ThreadStart(ReceiveThreadHandle));
                    myThread.IsBackground = true;
                    myThread.Start();


                    serialPort.PortName = cbComName.SelectedItem.ToString();
                    serialPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                    serialPort.Parity = (System.IO.Ports.Parity)cbParity.SelectedItem;
                    serialPort.DataBits = (int)cbDataBit.SelectedItem;
                    serialPort.StopBits = (System.IO.Ports.StopBits)cbStopBit.SelectedItem;

                    serialPort.ReadBufferSize = 4 * 1024 * 1024;//33554432;           // 32M
                    serialPort.Open();
                    picPortState.Image = ImageList.Images["open"];
                    btnPortOpt.Text = "�رմ���";
                    cbComName.Enabled = false;
                    UpdatalabText();
                }
                else
                {
                    serialPort.Close();
                    picPortState.Image = ImageList.Images["close"];
                    btnPortOpt.Text = "�򿪴���";
                    cbComName.Enabled = true;
                    UpdatalabText();

                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ѡ��ͨ�ſڡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbComName_SelectedIndexChanged(object sender, EventArgs e)
        {
            string portname = serialPort.PortName;
            try
            {
                serialPort.PortName = cbComName.SelectedItem.ToString();
                UpdatalabText();
            }
            catch (Exception ex)
            {
                cbComName.SelectedItem = portname;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// �����ʡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbBaudRate_SelectedIndexChanged(object sender, EventArgs e)
        {
            int bps = serialPort.BaudRate;
            try
            {
                serialPort.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                UpdatalabText();
            }
            catch (Exception ex)
            {
                cbBaudRate.Text = bps.ToString();
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// У��λ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbParity_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.IO.Ports.Parity pt = serialPort.Parity;
            try
            {
                serialPort.Parity = (System.IO.Ports.Parity)cbParity.SelectedItem;
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.Parity = pt;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ����λ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbDataBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            int db = serialPort.DataBits;
            try
            {
                serialPort.DataBits = Convert.ToInt32(cbDataBit.SelectedItem);
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.DataBits = db;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ֹͣλ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbStopBit_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.IO.Ports.StopBits sb = serialPort.StopBits;
            try
            {
                serialPort.StopBits = (System.IO.Ports.StopBits)cbStopBit.SelectedItem;
                UpdatalabText();
            }
            catch (Exception ex)
            {
                serialPort.StopBits = sb;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ˢ��״̬����
        /// </summary>
        private void UpdatalabText()
        {

            string str = serialPort.PortName + "," + serialPort.BaudRate.ToString() + "," +
                serialPort.Parity + "," + (int)serialPort.DataBits + "," + (float)serialPort.StopBits;

            if (serialPort.IsOpen)
            {
                labIsSerialOpen.Text = "ͨ������(" + str + ")";
            }
            else
            {
                labIsSerialOpen.Text = "ͨ�ſ��ѹر�";
            }

        }


        #endregion


        #region �Ҽ��˵�����

        private TextBox txtBoxMenu = new TextBox();

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuUndo_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Undo();
        }

        /// <summary>
        /// ���С�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCut_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Cut();
        }

        /// <summary>
        /// ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuCopy_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Copy();
        }

        /// <summary>
        /// ճ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuPaste_Click(object sender, EventArgs e)
        {
            txtBoxMenu.Paste();
        }

        /// <summary>
        /// ɾ����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDelet_Click(object sender, EventArgs e)
        {
            txtBoxMenu.SelectedText = "";
        }

        /// <summary>
        /// ȫѡ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSelectAll_Click(object sender, EventArgs e)
        {
            txtBoxMenu.SelectAll();
        }

        /// <summary>
        /// �ַ���תʮ�����ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStringToHex_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] bytes = System.Text.ASCIIEncoding.Default.GetBytes(txtBoxMenu.SelectedText);
                txtBoxMenu.Paste(BitConverter.ToString(bytes).Replace('-', ' ').TrimEnd());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ������ת�ַ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHexToString_Click(object sender, EventArgs e)
        {
            try
            {
                //string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Replace(Environment.NewLine, " ").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(HexStringToByte));
                txtBoxMenu.Paste(System.Text.ASCIIEncoding.Default.GetString(bytes));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ������תʮ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuHexToDec_Click(object sender, EventArgs e)
        {
            try
            {
                string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Replace(Environment.NewLine, " ").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder sb = new StringBuilder(strArray.Length);
                foreach (string str in strArray)
                {
                    sb.Append(Convert.ToByte(str, 16).ToString() + " ");
                }
                txtBoxMenu.Paste(sb.ToString().TrimEnd());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ����תʮ�����ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDecToHex_Click(object sender, EventArgs e)
        {
            try
            {
                string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(DecStringToByte));
                txtBoxMenu.Paste(BitConverter.ToString(bytes).Replace('-', ' ').TrimEnd());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// �ַ���תʮ���ơ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuStringToDec_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] bytes = System.Text.ASCIIEncoding.Default.GetBytes(txtBoxMenu.SelectedText);
                StringBuilder sb = new StringBuilder(bytes.Length * 2);
                foreach (byte mbyte in bytes)
                {
                    sb.Append(mbyte.ToString() + " ");
                }
                txtBoxMenu.Paste(sb.ToString().TrimEnd());

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// ʮ����ת�ַ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDecToString_Click(object sender, EventArgs e)
        {
            try
            {
                string[] strArray = txtBoxMenu.SelectedText.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(DecStringToByte));
                txtBoxMenu.Paste(System.Text.ASCIIEncoding.Default.GetString(bytes));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// �����Ҽ��˵���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmenuStrip_Opened(object sender, EventArgs e)
        {
            menuUndo.Enabled = txtBoxMenu.CanUndo;

            if (txtBoxMenu.SelectionLength > 0)
            {
                menuCut.Enabled = true;
                menuCopy.Enabled = true;
                menuDelet.Enabled = true;
                menuStringToHex.Enabled = true;
                menuHexToString.Enabled = true;
                menuHexToDec.Enabled = true;
                menuDecToHex.Enabled = true;
                menuStringToDec.Enabled = true;
                menuDecToString.Enabled = true;
            }
            else
            {
                menuCut.Enabled = false;
                menuCopy.Enabled = false;
                menuDelet.Enabled = false;
                menuStringToHex.Enabled = false;
                menuHexToString.Enabled = false;
                menuHexToDec.Enabled = false;
                menuDecToHex.Enabled = false;
                menuStringToDec.Enabled = false;
                menuDecToString.Enabled = false;
            }

            if (txtBoxMenu.Text == "")
            {
                menuSelectAll.Enabled = false;
            }
            else
            {
                menuSelectAll.Enabled = true;
            }

        }

        /// <summary>
        /// �������������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtReceive_MouseEnter(object sender, EventArgs e)
        {
            txtBoxMenu = txtReceive;
        }

        /// <summary>
        /// �����뷢������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSend_MouseEnter(object sender, EventArgs e)
        {
            txtBoxMenu = txtSend;
        }

        #endregion


        #region ���ܺ���

        /// <summary>
        /// ʮ�������ַ���תʮ���ơ�
        /// </summary>
        /// <param name="hexStr"></param>
        /// <returns></returns>
        byte HexStringToByte(string hexStr)
        {
            return Convert.ToByte(hexStr, 16);
        }

        /// <summary>
        /// ʮ�����ַ���תʮ���ơ�
        /// </summary>
        /// <param name="decStr"></param>
        /// <returns></returns>
        byte DecStringToByte(string decStr)
        {
            return Convert.ToByte(decStr, 10);
        }



        #endregion


        #region ״̬������

        private void picTop_Click(object sender, EventArgs e)
        {
            this.TopMost = !this.TopMost;
            if (this.TopMost == true)
            {
                this.Text = Application.ProductName + " V" + Application.ProductVersion.Substring(0, 3) + "  [�ö�]";
                picTop.Image = imglistTop.Images["nailon"];
            }
            else
            {
                this.Text = Application.ProductName + " V" + Application.ProductVersion.Substring(0, 3);
                picTop.Image = imglistTop.Images["nailoff"];
            }

            string textToolTip;
            if (this.TopMost)
            {
                textToolTip = "ȡ���ö�";
            }
            else
            {
                textToolTip = "�ö�";
            }
            ToolTip.SetToolTip(picTop, textToolTip);
        }

        private void labClearReceive_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtReceive.Clear();
            //GC.Collect();
        }

        private void labClearSend_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            txtSend.Clear();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            labRx.Text = "RX:0";
            RxCounter = 0;
            labTx.Text = "TX:0";
            TxCounter = 0;
        }


        private void labRx_DoubleClick(object sender, EventArgs e)
        {
            labRx.Text = "RX:0";
            RxCounter = 0;
        }

        private void labTx_DoubleClick(object sender, EventArgs e)
        {
            labTx.Text = "TX:0";
            TxCounter = 0;
        }


        private void btnHelp_Click(object sender, EventArgs e)
        {
            bool topmost = this.TopMost;

            this.TopMost = false;
            AboutBox myAboutBox = new AboutBox();
            myAboutBox.ShowDialog();
            this.TopMost = topmost;
        }


        private void btnEnd_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion


        #region ���ڽ�����ʾ


        /// <summary>
        /// ���ڽ����жϡ�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Thread.Sleep(10);
            try
            {
                int bytesLen = 0;

                do
                {
                    bytesLen = serialPort.BytesToRead;
                    if (bytesLen >= 65536)
                    {
                        bytesLen = 65536;
                        break;
                    }
                    else
                    {
                        Thread.Sleep(30);
                    }
                } while (bytesLen != serialPort.BytesToRead);


                //int cnt = 5;
                //do
                //{
                //    bytesLen = serialPort.BytesToRead;
                //    if (bytesLen >= 2048)
                //    {
                //        bytesLen = 2048;
                //        break;
                //    }
                //    Thread.Sleep(30);
                //    cnt--;
                //} while ((bytesLen != serialPort.BytesToRead) || cnt > 0);

                byte[] bytes = new byte[bytesLen];
                serialPort.Read(bytes, 0, bytesLen);
                lock (reBytesList)
                {
                    reBytesList.Add(bytes);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("���ڽ���" + ex.Message);
            }
        }

        /// <summary>
        /// ���մ����̡߳�
        /// </summary>
        private void ReceiveThreadHandle()
        {
            byte[] bytes = null;
            int bytesLen = 0;

            while (true)
            {
                try
                {
                    //bytes = null;
                    lock (reBytesList)
                    {
                        if (reBytesList.Count > 0)
                        {
                            if (reBytesList[0] != null)
                            {
                                bytes = reBytesList[0];
                                reBytesList[0] = null;
                            }
                            reBytesList.RemoveAt(0);
                        }
                    }

                    if (bytes != null)
                    {
                        bytesLen = bytes.Length;
                        if (chkDisplay.Checked)
                        {
                            if (chkReceiveHex.Checked)
                            {
                                if (bytesLen > 0)
                                {
                                    txtReceiveAppend(BitConverter.ToString(bytes).Replace('-', ' '));
                                    txtReceiveAppend(" ");
                                }
                            }
                            else
                            {
                                txtReceiveAppend(System.Text.ASCIIEncoding.Default.GetString(bytes));
                            }

                            if (chkWrap.Checked)                    // �Զ�����
                            {
                                txtReceiveAppend(Environment.NewLine);
                            }
                        }

                        RxCounter = RxCounter + (UInt64)bytesLen;
                        myUpdateRx(RxCounter);

                        bytes = null;
                        Thread.Sleep(10);
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("���ݴ����̣߳�" + ex.Message);
                }
            }
        }

        /// <summary>
        /// ���½��ճ��ȡ�
        /// </summary>
        /// <param name="count"></param>
        private void UpdateRx(UInt64 count)
        {
            labRx.Text = "RX:" + count.ToString();
            labRx.Refresh();
        }

        /// <summary>
        /// ���½����ı���
        /// </summary>
        /// <param name="appendText"></param>
        private void TextBoxReceiveAppend(string appendText)
        {
            txtReceive.AppendText(appendText);
        }

        #endregion



        #region ���ڷ���

        private Thread mySendThread;

        /// <summary>
        /// ����״̬�����ա�
        /// </summary>
        /// <param name="count"></param>
        private void UpdateTx(UInt64 count)
        {
            labTx.Text = "TX:" + count.ToString();
            labTx.Refresh();
        }

        /// <summary>
        /// �����ʼ���ͻ���ֹͣ���͡�
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (btnSend.Text == "��ʼ����")
            {

                if (serialPort.IsOpen == false)
                {
                    MessageBox.Show("����δ�򿪣����ȴ򿪴���", "��������", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                if (txtSend.Text == "")
                {
                    MessageBox.Show("û�пɷ��͵�����", "��������", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                mySendThread = new Thread(new ThreadStart(SendThreadHandle));
                mySendThread.IsBackground = true;
                mySendThread.Start();

                //btnSend.Text = "ֹͣ����";
                SetSendEnable(true);
                myUpdateTx(TxCounter);
            }
            else
            {
                mySendThread.Abort();
                // btnSend.Text = "��ʼ����";
                SetSendEnable(false);
                myUpdateTx(TxCounter);
            }
        }

        /// <summary>
        /// ���÷���ʹ�ܡ�
        /// </summary>
        /// <param name="IsEnable">��ΪTrueʱ��ʾ��ʼ���ͣ�False��ʾֹͣ���͡�</param>
        private void SetSendEnable(bool IsEnable)
        {
            if (IsEnable == true)
            {
                chkSendHex.Enabled = false;
                chkAutoSend.Enabled = false;
                numSendCount.Enabled = false;
                numSendInterval.Enabled = false;
                numSendOnceBytes.Enabled = false;
                btnSend.Text = "ֹͣ����";
            }
            else
            {
                chkSendHex.Enabled = true;
                chkAutoSend.Enabled = true;
                numSendCount.Enabled = true;
                numSendInterval.Enabled = true;
                numSendOnceBytes.Enabled = true;
                btnSend.Text = "��ʼ����";
            }

        }

        /// <summary>
        /// ����һ�����ݡ�
        /// </summary>
        /// <param name="sendBuff"></param>
        private void SendOnce(byte[] sendBuff)
        {
            if (Convert.ToUInt64(numSendOnceBytes.Value) == 0)        // һ�η��������ֽ�
            {
                TxCounter += (UInt64)sendBuff.Length;
                serialPort.Write(sendBuff, 0, sendBuff.Length);

                myUpdateTx(TxCounter);
            }
            else
            {
                int haveSendCnt = 0;
                int sendPerOnce = Convert.ToInt32(numSendOnceBytes.Value);
                int sleepTime = Convert.ToInt32(numSendInterval.Value);
                while (haveSendCnt < sendBuff.Length)
                {
                    if ((haveSendCnt + sendPerOnce) < sendBuff.Length)
                    {

                        serialPort.Write(sendBuff, haveSendCnt, sendPerOnce);
                        haveSendCnt += sendPerOnce;
                        TxCounter += (UInt64)sendPerOnce;


                        myUpdateTx(TxCounter);
                        Thread.Sleep(sleepTime);
                    }
                    else
                    {
                        int cnt = sendBuff.Length - haveSendCnt;
                        serialPort.Write(sendBuff, haveSendCnt, cnt);
                        haveSendCnt += cnt;
                        TxCounter += (UInt64)cnt;


                        myUpdateTx(TxCounter);
                    }



                }
            }
        }

        /// <summary>
        /// ���������̡߳�
        /// </summary>
        private void SendThreadHandle()
        {
            byte[] sendBuff;
            try
            {
                if (chkSendHex.Checked)
                {
                    //string[] strArray = txtSend.Text.TrimEnd().Split(new char[] { ' '}, StringSplitOptions.RemoveEmptyEntries);
                    string[] strArray = txtSend.Text.TrimEnd().Replace(Environment.NewLine, " ").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    sendBuff = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(HexStringToByte));
                }
                else
                {
                    sendBuff = System.Text.ASCIIEncoding.Default.GetBytes(txtSend.Text);
                }

                int sendLen = sendBuff.Length;

            }
            catch (Exception ex)
            {

                SetSendEnable(false);
                MessageBox.Show(ex.Message, "��������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (mySendThread.ThreadState != System.Threading.ThreadState.Aborted)
                {
                    mySendThread.Abort();
                }
                return;
            }

            while (true)
            {
                try
                {
                    if (chkAutoSend.Checked)        // �Զ�����
                    {
                        int reSendCnt = Convert.ToInt32(numSendCount.Value);
                        int sleepTime = Convert.ToInt32(numSendInterval.Value);
                        if (reSendCnt == 0)
                        {
                            while (true)
                            {
                                SendOnce(sendBuff);
                                Thread.Sleep(sleepTime);
                            }
                        }
                        else
                        {
                            while (reSendCnt > 0)
                            {
                                SendOnce(sendBuff);
                                reSendCnt--;
                                if (reSendCnt > 0)
                                {
                                    Thread.Sleep(sleepTime);
                                }
                            }
                            SetSendEnable(false);
                            mySendThread.Abort();
                            return;
                        }
                    }
                    else//�ֶ�����
                    {
                        SendOnce(sendBuff);
                        SetSendEnable(false);
                        mySendThread.Abort();
                        return;
                    }


                }
                catch (Exception ex)
                {
                    SetSendEnable(false);
                    //MessageBox.Show(ex.Message, "��������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Console.WriteLine("�����̳߳���" + ex.Message);
                    if (mySendThread.ThreadState != System.Threading.ThreadState.Aborted)
                    {
                        mySendThread.Abort();
                    }
                    return;
                }
            }
        }

        #endregion



        #region �����ļ��ʹ��ļ�


        /// <summary>
        /// ��������˵���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lnkSaveData_Enter(object sender, EventArgs e)
        {
            if (txtReceive.Text == "")
            {
                cmenuSave.Enabled = false;
            }
            else
            {
                cmenuSave.Enabled = true;
            }
            this.cmenuSave.Show(lnkSaveData, 0, lnkSaveData.Height);
        }


        /// <summary>
        /// ��ԭʼ��ʾ���档
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveStringToText_Click(object sender, EventArgs e)
        {

            sFileDlg.Filter = "�ı��ļ�(*.txt)|*.txt";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sFileDlg.FileName, txtReceive.Text);
                MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        /// <summary>
        /// ��������תΪ��������ʾ��
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveStringToBinary_Click(object sender, EventArgs e)
        {

            sFileDlg.Filter = "�������ļ�(*.bin)|*.bin";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(sFileDlg.FileName, FileMode.OpenOrCreate);
                BinaryWriter bw = new BinaryWriter(fs);
                try
                {
                    byte[] bytes = System.Text.ASCIIEncoding.Default.GetBytes(txtReceive.Text);
                    bw.Write(bytes);
                    MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    bw.Close();
                    fs.Close();
                }
            }

        }


        /// <summary>
        /// ��ʮ�����Ƶ��������ļ���
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuSaveHexToBinary_Click(object sender, EventArgs e)
        {

            sFileDlg.Filter = "�������ļ�(*.bin)|*.bin";
            if (sFileDlg.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(sFileDlg.FileName, FileMode.OpenOrCreate);
                BinaryWriter bw = new BinaryWriter(fs);
                try
                {

                    //string[] strArray = txtReceive.Text.TrimEnd().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] strArray = txtReceive.Text.TrimEnd().Replace(Environment.NewLine, "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    byte[] bytes = Array.ConvertAll<string, byte>(strArray, new Converter<string, byte>(HexStringToByte));
                    bw.Write(bytes);
                    MessageBox.Show("�ļ��ѱ��浽\n" + sFileDlg.FileName, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, sFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    bw.Close();
                    fs.Close();
                }
            }
        }




        private void lnkOpen_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            oFileDlg.Filter = "�ı��ļ�(*.txt)|*.txt|�������ļ�(*.bin)|*.bin|�����ļ�(*.*)|*.*";
            if (oFileDlg.ShowDialog() == DialogResult.OK)
            {
                string strExt = System.IO.Path.GetExtension(oFileDlg.FileName).ToUpper();
                if (strExt == ".TXT")
                {
                    txtSend.Text = File.ReadAllText(oFileDlg.FileName);
                }
                else
                {
                    FileStream fs = new FileStream(oFileDlg.FileName, FileMode.Open);
                    BinaryReader br = new BinaryReader(fs);
                    try
                    {
                        byte[] bytes = br.ReadBytes((int)fs.Length);
                        txtSend.Text = BitConverter.ToString(bytes).Replace('-', ' ').TrimEnd();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, oFileDlg.Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        br.Close();
                        fs.Close();
                    }

                }

            }
        }

        #endregion


        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                if (myThread.IsAlive)
                {
                    myThread.Abort();
                }
            }
            catch { }
        }


        private void cbComName_DropDown(object sender, EventArgs e)
        {
            cbComName.DataSource = SerialPort.GetPortNames();
        }









    }
}