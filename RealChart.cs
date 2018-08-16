using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using cn.bmob.api;
using cn.bmob.tools;
using System.Diagnostics;
using cn.bmob.io;
using System.Net;

namespace DemoSharp
{
    public partial class RealChart : Form
    {
        private Queue<double> dataQueue = new Queue<double>(100); //距离
        private Queue<double> dataQueue1 = new Queue<double>(100);//速度
        private int curValue = 0;

        private int num = 5;//每次删除增加几个点

        public RealChart()
        {
            InitializeComponent();
        }

        public static string HttpDownload(string url, string path)
        {
            // 设置参数
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            //发送请求并获取相应回应数据
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            //直到request.GetResponse()程序才开始向目标网页发送Post请求
            Stream responseStream = response.GetResponseStream();

            //创建本地文件写入流
            Stream stream = new FileStream(path, FileMode.Create);

            byte[] bArr = new byte[1024];
            int size = responseStream.Read(bArr, 0, (int)bArr.Length);
            while (size > 0)
            {
                stream.Write(bArr, 0, size);
                size = responseStream.Read(bArr, 0, (int)bArr.Length);
            }
            stream.Close();
            responseStream.Close();
            return path;
        }

        // 跨线程通讯Handler代理
        private delegate void txtHandeler(object obj);

        /// <summary>
        /// 初始化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnInit_Click(object sender, EventArgs e)
        {
            BmobWindows Bmob = new BmobWindows();
            Bmob.initialize("38cd909fe640756176303b980453db55", "9e8a836ccfd013f87619b284a97de02c"); // 初始化Bmob客户端
            BmobDebug.Register(msg => { Debug.WriteLine(msg); }); // 用于调试输出请求参数

            BmobQuery query = new BmobQuery();
            //新建查询 查询Activityrecord表中所有的数据 按时间正向排序
            Bmob.Find<ReadFile>("Activityrecord", query, (resp, exception) =>
            {
                // 处理查询网络异常
                if (exception != null)
                {
                    return;
                }

                //对返回结果进行处理
                List<ReadFile> list = resp.results;
                foreach (var game in list)
                {
                    BmobFile record = game.record;
                    if(record!=null)
                    {
                        if (File.Exists(record.filename))
                        {
                            Console.WriteLine("跳过:" + record.filename);
                        }
                        else
                        {
                            Console.WriteLine("下载:" + record.filename);
                            //调用HttpDownload进行非重复文件的下载，此处下载为同步下载 即一个下载完才会执行之后代码
                            HttpDownload(record.url, "./" + record.filename);
                        }
                    }
               
                }
                Console.WriteLine("开始写入");
                String[] files = Directory.GetFiles("./", "*.txt", SearchOption.AllDirectories);
                foreach (string f in files)
                {
                    if (File.Exists("db.txt"))
                    {
                        Console.WriteLine("it is existed");
                    }
                    else {
                        FileStream fs = new FileStream("db.txt", FileMode.CreateNew);
                        fs.Close();
                    }
                    Console.WriteLine("查看" + f);
                    if (f.Contains("db.txt")) continue;
                    string a = System.IO.File.ReadAllText(f);
                    string b = System.IO.File.ReadAllText("db.txt");
                    System.IO.File.WriteAllText("db.txt", a + "\r\n" + b);
                    File.Delete(f);
                }
                byte[] byData = new byte[100];
                char[] charData = new char[1000];
                
                // 这里是Bmob的回调函数 也就是查询完之后执行下载了所有文件再执行此处的合并和InitChart
                // 所以需要将这个设置为False 否则会提示操作跨线程错误
                Control.CheckForIllegalCrossThreadCalls = false;
                InitChart();
                Read("db.txt");
            });
            
        }
        public void Read(string path)
        {
            StreamReader sr = new StreamReader(path, Encoding.Default);
            String line;
            while ((line = sr.ReadLine()) != null)//第一次初始化，读取所有人名
            {
                string[] split = line.Split(new char[] { ',' }, 10);
                if(!comboBox1.Items.Contains(split[0]))
                    comboBox1.Items.Add(split[0]);
            }
            sr.Close();
        }
        /// <summary>
        /// 开始事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            this.timer1.Start();
        } 
        
        /// <summary>
        /// 停止事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, EventArgs e)
        {
            this.timer1.Stop();
        }

        /// <summary>
        /// 定时器事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {

        } 
        
        /// <summary>
        /// 初始化图表
        /// </summary>
        private void InitChart() {
            //定义图表区域
            this.chart1.ChartAreas.Clear();
            ChartArea chartArea1 = new ChartArea("C1");
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.ChartAreas[0].AxisY.Minimum = 0;
            this.chart1.ChartAreas[0].AxisY.Maximum =40;
            this.chart1.ChartAreas[0].AxisX.Interval = 1;
            this.chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = System.Drawing.Color.Silver;
            this.chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = System.Drawing.Color.Silver;
            //this.chart1.ChartAreas[0].AxisX.IntervalOffset = 1.00D;

            //设置标题
            this.chart1.Titles.Clear();
            //this.chart1.Titles.Add("S01");
            //this.chart1.Titles[0].Text = "XXX显示";
            //this.chart1.Titles[0].ForeColor = Color.RoyalBlue;
            //this.chart1.Titles[0].Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            //设置图表显示样式
            this.chart1.Series[0].Color = Color.Red;
            this.chart1.Series[1].Color = Color.Blue;

            this.chart1.Series[0].LegendText = "Speed";
            this.chart1.Series[1].LegendText = "Distance";
            
            Legend leg = new Legend(chart1.Name);
            //leg.Position = this.chart1.Top;
            this.chart1.Legends.Add(leg);

            if (rb1.Checked)
            {
              //  this.chart1.Titles[0].Text =string.Format( "XXX {0} 显示",rb1.Text);
              //  this.chart1.Series[0].ChartType = SeriesChartType.Line;
            }
            this.chart1.Series[0].Points.Clear();
            this.chart1.Series[1].Points.Clear();
        }
        
        //更新队列中的值
        private void UpdateQueueValue() {
            dataQueue.Clear();
            dataQueue1.Clear();
            StreamReader sr = new StreamReader("db.txt", Encoding.Default);
            String line;
            String strname;
            double distance, speed;
            strname = comboBox1.Text;
            while ((line = sr.ReadLine()) != null)//读取选择对应数据
            {
                string[] split = line.Split(new char[] { ',' }, 10);
                
                if (strname == split[0])
                {
                    speed = Convert.ToDouble(split[1]);
                    distance = Convert.ToDouble(split[2]);
                    dataQueue.Enqueue(distance);
                    dataQueue1.Enqueue(speed);
                }
            }

            if (dataQueue.Count > 100) {
                //先出列
                for (int i = 0; i < num; i++)
                {
                    dataQueue.Dequeue();
                }
            }
            
        }

        private void checkedListBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void rb1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateQueueValue();
            label1.Visible = true;
            label2.Visible = true;
            this.chart1.Series[0].Points.Clear();
            this.chart1.Series[1].Points.Clear();
            for (int i = 0; i < dataQueue.Count; i++)
            {
                this.chart1.Series[0].Points.AddXY(i, dataQueue.ElementAt(i));
                this.chart1.Series[1].Points.AddXY(i, dataQueue1.ElementAt(i));
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
