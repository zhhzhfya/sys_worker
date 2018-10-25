using sys_worker.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace sys_worker
{
    public partial class Form1 : Form
    {
        private Dictionary<String, String> unit = new Dictionary<string, string>();
        public Form1()
        {
            unit.Add("小时", "86400000");
            unit.Add("分钟", "3600");
            unit.Add("秒", "1000");

            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.txt_proc_path.Text = Settings.Default.proc_path;
            this.txt_proc_name.Text = Settings.Default.proc_name;
            this.txt_email.Text = Settings.Default.email;
            this.numericUpDown1.Value = Settings.Default.period;
            this.comboBox1.SelectedIndex = Settings.Default.unit;

            String key = this.comboBox1.Items[this.comboBox1.SelectedIndex].ToString();
            this.timer1.Interval = Convert.ToInt32(unit[key]) * (int)this.numericUpDown1.Value;
            
            this.btnStart.PerformClick();//.Click(sender, e);
        }

        [DllImport("user32.dll ")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        //根据任务栏应用程序显示的名称找相应窗口的句柄
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        private const int SW_RESTORE = 9;
        private void OpenSerialPortUtility(object sender, EventArgs e)
        {
            //第一种方法
            //查找状态中的窗口线程句柄来查看目标程序是否在运行运行则前置否则打开
            IntPtr findPtr = FindWindow(this.txt_proc_name.Text, null);
            if (findPtr.ToInt32() != 0)
            {
                ShowWindow(findPtr, SW_RESTORE); //将窗口还原，如果不用此方法，缩小的窗口不能激活
                SetForegroundWindow(findPtr);//将指定的窗口选中(激活)
            }
            else
            {
                try
                {
                    Process proc = System.Diagnostics.Process.Start(this.txt_proc_path.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            addLog("任务启动");
            /// 1 bring the process to front
            OpenSerialPortUtility(sender, e);
            Thread.Sleep(1000);
            /// 2 capture the screen
            captureScreen();
            /// 3 send email
            sendEmail();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenSerialPortUtility(sender, e);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            captureScreen();
        }

        private void captureScreen()
        {
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                }
                bitmap.Save("test.jpg", ImageFormat.Jpeg);
            }
            addLog("截屏完成");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            sendEmail();
        }

        public void sendEmail()
        {
            //实例化一个发送邮件类。
            MailMessage mailMessage = new MailMessage();
            //发件人邮箱地址，方法重载不同，可以根据需求自行选择。
            mailMessage.From = new MailAddress("37729782@qq.com");
            //收件人邮箱地址。
            mailMessage.To.Add(new MailAddress(this.txt_email.Text));
            //邮件标题。
            mailMessage.Subject = "发送邮件测试";
            //邮件内容。
            mailMessage.Body = "这是我给你发送的第一份邮件哦！";
            
            mailMessage.Attachments.Add(new System.Net.Mail.Attachment(System.Environment.CurrentDirectory + "\\test.jpg"));
            //实例化一个SmtpClient类。
            SmtpClient client = new SmtpClient();
            //在这里我使用的是qq邮箱，所以是smtp.qq.com，如果你使用的是126邮箱，那么就是smtp.126.com。
            client.Host = "smtp.qq.com";
            //使用安全加密连接。
            client.EnableSsl = true;
            //不和请求一块发送。
            client.UseDefaultCredentials = false;
            //验证发件人身份(发件人的邮箱，邮箱里的生成授权码);
            client.Credentials = new NetworkCredential("37729782@qq.com", "dzyuqklchtvqbhaa");
            //发送
            client.Send(mailMessage);
            mailMessage.Dispose();
            addLog("邮件发送成功");
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            addLog((int)this.numericUpDown1.Value);
            this.timer1.Enabled = false;
            String key = this.comboBox1.Items[this.comboBox1.SelectedIndex].ToString();
            addLog(Convert.ToInt32(unit[key]));
            if (Convert.ToInt32(unit[key]) * (decimal)this.numericUpDown1.Value < 10)
            {
                addLog("设定错误，必须大于10");
                return;
            }
            this.timer1.Interval = (int)(Convert.ToInt32(unit[key]) * (decimal)this.numericUpDown1.Value);
            this.timer1.Enabled = true;
            addLog("任务重新设定，周期" + this.numericUpDown1.Value + key);
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (!this.timer1.Enabled)
            {
                this.timer1.Enabled = true;
                this.btnStart.Text = "停止";
                addLog("启动成功");
            }
            else {
                this.timer1.Enabled = false;
                this.btnStart.Text = "启动";
                addLog("停止工作");
            }
        }

        private void addLog(object log)
        {
            this.txt_log.AppendText(DateTime.Now.ToLongDateString() + " "+log+"\r\n");
            this.txt_log.Focus();//获取焦点
            this.txt_log.Select(this.txt_log.TextLength, 0);//光标定位到文本最后
            this.txt_log.ScrollToCaret();//滚动到光标处
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            /// 1 bring the process to front
            OpenSerialPortUtility(sender, e);
            Thread.Sleep(1000);
            /// 2 capture the screen
            captureScreen();
            /// 3 send email
            sendEmail();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.Default.email = this.txt_email.Text;
            Settings.Default.proc_path = this.txt_proc_path.Text;
            Settings.Default.proc_name = this.txt_proc_name.Text;
            Settings.Default.period = (int)this.numericUpDown1.Value;
            Settings.Default.unit = this.comboBox1.SelectedIndex;

            Settings.Default.Save();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show((int.MaxValue / 86400000) + "");
        }
    }

    
}
