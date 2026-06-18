using PingTools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;




namespace WindowsFormsApp2
{

    public partial class Form1 : Form
    {
        private readonly object _statLock = new object();
        private Thread _statThread;
        private bool _threadRunning = true;
        private readonly ConcurrentQueue<ulong> _timeQueue = new ConcurrentQueue<ulong>();
        private string Temp1;
        private bool Temp2 = false;
        private string FilePath;
        string a, b;

        private DataMods dataMods;
        public Form1()
        {
            InitializeComponent();
            InitializeDataMods();
            InitDataBinding();
        }

        // 窗体加载时初始化数据模型和绑定
        private void Form1_Load(object sender, EventArgs e)
        {
            //初始化
            initialize();
            dataMods.IsConnected = false;
            checkBox5.Checked = true; // 默认启用时间戳
            checkBox6.Checked = true; // 默认启用自动滚动
            textBox4.Enabled = false; // 默认禁用日志路径输入框，除非用户勾选启用日志
            textBox5.Enabled = false; // 默认禁用日志文件名输入框，除非用户勾选启用日志
            fl.Enabled = false; // 默认禁用日志路径选择按钮，除非用户勾选启用日志
        }

        // 窗体关闭时确保后台线程安全退出
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _threadRunning = false;
            _statThread?.Join(1000); // 等待统计线程结束
        }

        //初始化信息
        private void initialize()
        {
            dataMods.Sends = 0;
            dataMods.LossRate = 0;
            dataMods.Losses = 0;
            dataMods.Receives = 0;
            dataMods.MaxTime = 0;
            dataMods.Mintime = 0;
            dataMods.AverageTime = 0;
            dataMods.TotalTime = 0;
        }

        // 初始化数据模型实例
        private void InitializeDataMods()
        {
            if (dataMods == null)
            {
                dataMods = new DataMods();
            }
        }

        // 初始化数据绑定
        private void InitDataBinding()
        {
            // 1. 绑定Ping结果列表到ListBox
            // ListBox的DisplayMember指定要显示的模型属性（这里是PingResult的Content）
            //listBox1.DisplayMember = nameof(PingResult.Content);
            //listBox1.DataSource = _pingResultList; // 绑定到BindingList

            // 2. 绑定统计信息到Label（使用Binding）
            // 已发送
            var sentBinding = new Binding(
                "Text",
                dataMods,
                nameof(DataMods.Sends),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
            // 替换FormatString，改用Format事件手动格式化
            sentBinding.Format += (sender, e) => e.Value = $"已发送：{e.Value}";
            label4.DataBindings.Add(sentBinding);

            //已接收
            var receivedBinding = new Binding(
                "Text",
                dataMods,
                nameof(DataMods.Receives),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
            receivedBinding.Format += (sender, e) => e.Value = $"已接收：{e.Value}";
            label5.DataBindings.Add(receivedBinding);

            //丢包
            var lossBinding = new Binding(
                "Text",
                dataMods,
                nameof(DataMods.Losses),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
            lossBinding.Format += (sender, e) => e.Value = $"丢包：{e.Value}";
            label6.DataBindings.Add(lossBinding);

            //丢包率
            var lossRateBinding = new Binding(
                "Text",
                dataMods,
                nameof(DataMods.LossRate),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
            lossRateBinding.Format += (sender, e) => e.Value = $"丢包率：{e.Value:F1}%";
            label7.DataBindings.Add(lossRateBinding);

            // 平均时间
            var avgTimeBinding = new Binding(
                "Text",
                dataMods,
                nameof(DataMods.AverageTime),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
            avgTimeBinding.Format += (sender, e) => e.Value = $"平均时间：{e.Value:F1}ms";
            label8.DataBindings.Add(avgTimeBinding);

            // 最小时间
            var minTimeBinding = new Binding(
                "Text",
                dataMods,
                nameof(DataMods.Mintime),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
            minTimeBinding.Format += (sender, e) => e.Value = $"最小时间：{e.Value}ms";
            label9.DataBindings.Add(minTimeBinding);

            // 最大时间
            var maxTimeBinding = new Binding(
                "Text",
                dataMods,
                nameof(DataMods.MaxTime),
                true,
                DataSourceUpdateMode.OnPropertyChanged);
            maxTimeBinding.Format += (sender, e) => e.Value = $"最大时间：{e.Value}ms";
            label10.DataBindings.Add(maxTimeBinding);
        }


        //启动按钮
        private void button1_Click(object sender, EventArgs e)
        {
            // 如果已经连接，则停止监测
            if (dataMods.IsConnected)
            {
                StopPing();
                return;
            }

            //初始化 开始监测
            initialize();
            dataMods.IsConnected = true;
            btnStart.Text = "停止";
            btnStart.BackColor = Color.Red;
            dataMods.PingContent = "开始监测 目标IP：" + txtIP.Text;
            listBox1.Items.Add(dataMods.PingContent);

            // 判断是否启用日志记录
            if (checkBox4.Checked)
            {
                flpath();
                PingOutTxt(dataMods.PingContent);
            }

            // 获取当前UI线程的SynchronizationContext，以便在PingLoop中安全更新UI
            var uiSyncContext = SynchronizationContext.Current;
            new Thread(() => PingLoop(uiSyncContext)) { IsBackground = true }.Start();
        }

        //停止监测
        private void StopPing()
        {
            dataMods.IsConnected = false;
            btnStart.Text = "开始";
            btnStart.BackColor = Color.Lime;
            _threadRunning = false;
            _statThread?.Join(1000);
            Temp2 = false;
            a = $"监测已停止。总发送：{dataMods.Sends}，总接收：{dataMods.Receives}，总丢包：{dataMods.Losses}，丢包率：{dataMods.LossRate:F1}%";
            b = $"最大时间：{dataMods.MaxTime}ms，最小时间：{dataMods.Mintime}ms，平均时间：{dataMods.AverageTime:F1}ms";
            listBox1.Items.Add(a);
            listBox1.Items.Add(b);
            if (checkBox4.Checked)
            {
                PingOutTxt(a);
                PingOutTxt(b);
            }
            listBox1.TopIndex = listBox1.Items.Count - 1;
        }

        /// <summary>
        /// 文件路径处理逻辑：如果用户没有输入路径，则默认使用当前目录；如果没有输入文件名，则默认使用IP地址作为文件名。最终组合成完整的日志文件路径。
        /// </summary>
        private void flpath()
        {
            string a, b;
            if (textBox4.Text.Trim() == "")
            {
                a = System.Environment.CurrentDirectory;
                textBox4.Text = a;
            }
            else
            {
                a = textBox4.Text.Trim();
            }
            if (textBox5.Text.Trim() == "")
            {
                b = txtIP.Text.Trim();
                textBox5.Text = b;
            }
            else
            {
                b = textBox5.Text.Trim();
            }
            FilePath = a + "\\" + b + ".txt";

        }

        //文件路径选择按钮
        private void fl_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "选择一个文件夹";

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    // 在这里处理选择的文件夹路径  
                    string folderPath = folderBrowserDialog.SelectedPath;
                    textBox4.Text = folderPath;
                }
            }
        }


        //清屏
        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        //日志选择框
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                textBox4.Enabled = true;
                textBox5.Enabled = true;
                fl.Enabled = true;
            }
            else
            {
                textBox4.Enabled = false;
                textBox5.Enabled = false;
                fl.Enabled = false;
                textBox4.Text = "";
                textBox5.Text = "";
            }
        }

        // 当Ping结果列表发生变化时触发（如添加新项）
        private void PingOutTxt(string txt)
        {
            // 追加写入TXT文件（自动创建文件，编码用UTF-8）
            try
            {
                // true表示“追加模式”，不会覆盖原有内容
                File.AppendAllText(FilePath, txt + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"写日志失败：{ex.Message}");
            }

        }


        // 循环Ping逻辑（传入UI线程上下文，用于安全更新数据源）
        private void PingLoop(SynchronizationContext uiSyncContext)
        {
            string timeNow;
            string ip = txtIP.Text.Trim();
            if (string.IsNullOrEmpty(ip))
            {
                uiSyncContext.Post(_ => MessageBox.Show("请输入IP地址！"), null);
                dataMods.IsConnected = false;
                uiSyncContext.Post(_ => btnStart.Text = "开始", null);
                btnStart.BackColor = Color.Lime;
                return;
            }

            //启用统计线程
            _statThread = new Thread(StatLoop)
            {
                IsBackground = true, // 后台线程，程序退出自动销毁
                Priority = ThreadPriority.BelowNormal
            };
            _statThread.Start();
            _threadRunning = true;

            using (Ping pingSender = new Ping())
            {
                while (dataMods.IsConnected)
                {
                    try
                    {
                        PingReply reply = pingSender.Send(ip);
                        uiSyncContext.Post(_ =>
                        {
                            // 选择是否显示时间戳
                            if (checkBox5.Checked) timeNow = DateTime.Now.ToString("HH:mm:ss.fff") + " - "; else timeNow = "";

                            if (reply.Status == IPStatus.Success)
                            {

                                Temp1 = timeNow + ($"回复来自 {reply.Address}：字节={reply.Buffer.Length} 时间={reply.RoundtripTime}ms TTL={reply.Options.Ttl}");
                                listBox1.Items.Add(Temp1);
                                dataMods.Receives++;
                                _timeQueue.Enqueue((ulong)reply.RoundtripTime);
                            }
                            else
                            {
                                Temp1 = timeNow + "请求超时。";
                                listBox1.Items.Add(Temp1);
                                dataMods.Losses++;
                            }

                            dataMods.PingContent = Temp1;

                            if (listBox1.Items.Count > 40) // 限制列表项数量，避免内存占用过大
                            {
                                // 暂停控件绘制，批量操作完再刷新，大幅消除闪烁
                                listBox1.BeginUpdate();
                                try
                                {
                                    // 删除头部旧数据，此时滚动位置已经锁定底部，不会跳走
                                    if (listBox1.Items.Count > 500)
                                    {
                                        listBox1.Items.RemoveAt(0);
                                    }
                                }
                                finally
                                {
                                    // 恢复绘制，只刷新一次
                                    listBox1.EndUpdate();
                                }
                            }
                            //判断是否启用自动滚动
                            if (checkBox6.Checked) listBox1.TopIndex = listBox1.Items.Count - 1;

                            // 判断是否写入日志 追加写入TXT文件（自动创建文件，编码用UTF-8）
                            if (checkBox4.Checked) PingOutTxt(dataMods.PingContent);
                            

                            panel1.Invalidate();
                            dataMods.Sends++;
                            dataMods.LossRate = dataMods.Sends > 0 ? (float)dataMods.Losses / dataMods.Sends * 100 : 0;
                        }, null);
                    }
                    catch (Exception ex)
                    {
                        uiSyncContext.Post(_ => MessageBox.Show($"Ping失败：{ex.Message}"), null);
                        dataMods.IsConnected = false;
                        uiSyncContext.Post(_ => btnStart.Text = "开始", null);
                    }
                    Thread.Sleep(1000); // 每秒ping一次
                }
            }
        }

        // 绘制进度条（在Panel的Paint事件中调用）
        private void panelProgress_Paint(object sender, PaintEventArgs e)
        {
            if (dataMods.Sends == 0)
            {
                // 还没发送过Ping，直接返回（避免除以0）
                return;
            }

            Panel panel = sender as Panel;
            Graphics g = e.Graphics;

            // 1. 计算比例
            float totalWidth = panel.ClientSize.Width; // Panel总宽度
            float lostRatio = (float)dataMods.LossRate / 100; // 丢包率（0~1）
            float receivedRatio = 1 - lostRatio; // 已接收比例 = 1 - 丢包率

            // 2. 计算各区域宽度
            int receivedWidth = (int)(totalWidth * receivedRatio); // 绿色区域宽度
            //int lostWidth = (int)(totalWidth * lostRatio); // 红色区域宽度
            int lostWidth = (int)totalWidth - receivedWidth; // 红色区域宽度（剩余部分）

            // 3. 绘制绿色（已接收）区域
            using (Brush greenBrush = new SolidBrush(Color.LimeGreen))
            {
                g.FillRectangle(greenBrush, 0, 0, receivedWidth, panel.ClientSize.Height);
            }

            // 4. 绘制红色（丢包）区域（从绿色区域末尾开始）
            using (Brush redBrush = new SolidBrush(Color.Red))
            {
                g.FillRectangle(redBrush, receivedWidth, 0, lostWidth, panel.ClientSize.Height);
            }
        }

        // 后台线程循环：持续处理队列里的耗时数据
        private void StatLoop()
        {
            while (_threadRunning)
            {
                if (_timeQueue.TryDequeue(out ulong time))
                {
                    // 子线程内部执行原Timecode逻辑
                    lock (_statLock)
                    {
                        if (!Temp2)
                        {
                            dataMods.MaxTime = time;
                            dataMods.Mintime = time;
                            Temp2 = true;
                        }
                        if (time > dataMods.MaxTime)
                            dataMods.MaxTime = time;

                        if (time < dataMods.Mintime)
                            dataMods.Mintime = time;

                        dataMods.TotalTime += time;
                        dataMods.AverageTime = dataMods.Sends > 0
                            ? (float)dataMods.TotalTime / dataMods.Sends
                            : 0;
                    }
                }
                else
                {
                    // 队列为空，短暂休眠减少CPU占用
                    Thread.Sleep(1);
                }
            }
        }

        //出入框 按回车键触发开始按钮
        private void txtIP_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnStart.PerformClick(); // 模拟点击开始按钮
                e.IsInputKey = true; // 标记为输入键，防止系统处理
            }
        }

    }
}
