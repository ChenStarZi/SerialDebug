using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SerialDebug
{
    public partial class FormNormalSend : Form, ISendForm
    {
        private List<string> SendTempList = new List<string>();
        private int SendTempIndex = 0;
        private bool IsSendByKey = false;

        public FormNormalSend()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ��ʽ����ʾ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chkFormat_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                CSendParam param = new CSendParam(SendParamFormat.Hex, SendParamMode.SendAfterLastSend, Convert.ToInt32(numSendInterval.Value), txtSend.Text);
                if (chkSendHex.Checked && chkFormat.Checked)
                {
                    txtSend.Text = param.Data;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        /// <summary>
        /// ���°������������
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSend_KeyDown(object sender, KeyEventArgs e)
        {
            string text = "";
            if (e.Modifiers == Keys.Control)
            {
                IsCtrlPressed = true;
            }
            else
            {
                IsCtrlPressed = false;
            }

            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.Enter)
            {
                IsSendByKey = true;
                //btnSend.PerformClick();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.Up)
            {
                SendTempIndex--;
                if (SendTempIndex < 0)
                {
                    SendTempIndex = 0;
                    Console.Beep();
                }
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                SendTempIndex++;
                if (SendTempIndex > SendTempList.Count)
                {
                    SendTempIndex = SendTempList.Count;
                    Console.Beep();
                }
                e.Handled = true;
            }
            else
            {
                return;
            }

            lock (SendTempList)
            {
                if (SendTempList.Count > 0)
                {
                    if (SendTempIndex < SendTempList.Count)
                    {
                        text = SendTempList[SendTempIndex];
                    }
                }
            }

            txtSend.Clear();
            txtSend.AppendText(text);
        }

        private bool IsCtrlPressed = false;
        private void txtSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (IsCtrlPressed)
            {
                if (e.KeyChar == '\r' || e.KeyChar == '\n') // �س�
                {
                    e.Handled = true;
                }
            }

        }

        private void updateSendState()
        {
            lock (SendTempList)
            {
                SendTempList.Add(txtSend.Text);
                if (chkSendThenClear.Checked)
                {
                    SendTempIndex = SendTempList.Count;
                    //txtSendUpdate("");
                    txtSend.Clear();
                }
                else
                {
                    SendTempIndex = SendTempList.Count - 1;
                }
            }
        }


        #region ISendForm ��Ա

        public List<CSendParam> getSendList()
        {
            try
            {
                List<CSendParam> list = new List<CSendParam>();

                SendParamFormat format = SendParamFormat.ASCII;
                if (chkSendHex.Checked)
                {
                    format = SendParamFormat.Hex;
                }

                int sendInterval = Convert.ToInt32(numSendInterval.Value);
                CSendParam param = new CSendParam(format, SendParamMode.SendAfterLastSend, sendInterval, txtSend.Text);

                if (chkSendHex.Checked && chkFormat.Checked)
                {
                    txtSend.Text = param.Data;
                }

                int packetLen = Convert.ToInt32(numSendOnceBytes.Value);

                if (packetLen == 0)
                {
                    list.Add(param);
                }
                else
                {
                    byte[] dataBytes = param.DataBytes;

                    int bytesIndex = 0;
                    while (bytesIndex < param.DataLen)
                    {
                        int len = packetLen;
                        if (bytesIndex + packetLen > param.DataLen)
                        {
                            len = param.DataLen - bytesIndex;
                        }

                        int delayTime = sendInterval;

                        //if (bytesIndex==0)
                        //{
                        //    //delayTime = 0;
                        //}
                        CSendParam p = new CSendParam(format, SendParamMode.SendAfterLastSend, delayTime, dataBytes, bytesIndex, len);
                        list.Add(p);

                        bytesIndex += len;
                    }
                }

                updateSendState();

                return list;

            }
            catch (Exception ex)
            {
                // MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw ex;
            }
        }

        public int LoopCount
        {
            get
            {
                if (chkAutoSend.Checked)
                {
                    return Convert.ToInt32(numSendCount.Value);
                }
                else
                {
                    return 1;
                }

            }
        }

        #endregion


    }
}