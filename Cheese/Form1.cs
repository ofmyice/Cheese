using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Cheese.Properties;

namespace Cheese
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //关闭跨线程检查
            Control.CheckForIllegalCrossThreadCalls = false;
    

            questioncache = new StringBuilder();
            imagecache = new StringBuilder();
        }


        // 调用通用文字识别, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
        // 如果有可选参数
        private Dictionary<string, object> options = new Dictionary<string, object> { { "language_type", "CHN_ENG" }, };

        /// <summary>
        /// 定一个线程
        /// </summary>
        private Thread th1;

        /// <summary>
        /// 默认解析的APP
        /// </summary>
        private string AppName = "芝士超人";

        /// <summary>
        /// 上次的截图地址.用于删除
        /// </summary>
        private string oldFullScreen = String.Empty;

        private string oldPhoneScreen = String.Empty;
        private string oldQuestionScreen = String.Empty;
        private static string Apikey = String.Empty;
        private static string Secretkey = String.Empty;
        private Baidu.Aip.Ocr.Ocr client;
        private string saveFilePath = String.Empty;
        private string fullScreenPath = String.Empty;
        private string questionPath = String.Empty;

        /// <summary>
        /// 已经查询过得问题缓存
        /// </summary>
        private StringBuilder questioncache;

        /// <summary>
        /// 已经解析过得图片缓存
        /// </summary>
        private StringBuilder imagecache;

        /// <summary>
        /// 开始截屏识别
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtapikey.Text))
            {

                label1.Text = "请填写API KEY 以及 SECRET KEY";
                return;
            }
            if (string.IsNullOrWhiteSpace(txtsecretkey.Text))
            {
                label1.Text = "请填写API KEY 以及 SECRET KEY";
                return;
            }
            button1.Enabled = false;
            txtapikey.Enabled = false;
            txtsecretkey.Enabled = false;
            Apikey = txtapikey.Text;
            Secretkey = txtsecretkey.Text;
            client = new Baidu.Aip.Ocr.Ocr(txtapikey.Text, txtsecretkey.Text);
            th1 = new Thread(() =>
            {
                try
                {
                    StartCut();
                }
                catch (Exception)
                {
                    if (th1 != null)
                    {
                        th1.Abort();
                    }
                    System.Environment.Exit(0);
                }


            });
            th1.Start();
        }

        /// <summary>
        /// 切割包含题目的答题卡
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        private string CutImg(string filepath)
        {
            Image fromImage = Image.FromFile(filepath);
            int width = 546;//因为是对整屏截图切割 ,定义一个宽度
            int x = 0; //截取X坐标   
            int y = 0; //截取Y坐标   
            int height = 0; //新的图片高度

            if (AppName == "芝士超人")
            {
                x = 0;
                y = 170;
                height = 110;
            
            }
            if (AppName == "百万英雄")
            {
                x = 0;
                y = 190;
                height = 140;
            }
            if (AppName == "冲顶大会")
            {
                x = 0;
                y = 240;
                height = 210;
            }
            //创建新图位图 
            Bitmap bitmap = new Bitmap(width, height);

            Graphics graphic = Graphics.FromImage(bitmap);

            //创建作图区域   

            //截取原图相应区域写入作图区   new Rectangle(矩形左上角的 x 坐标。, 矩形左上角的 y 坐标。, 矩形的宽度, 矩形的高度。)
            graphic.DrawImage(fromImage, 0, 0, new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            //从作图区生成新图   
            Image saveImage = Image.FromHbitmap(bitmap.GetHbitmap());

            saveFilePath = string.Format(Environment.CurrentDirectory + @"\Question\{0}.jpg", DateTime.Now.Ticks);
            saveImage.Save(saveFilePath, ImageFormat.Jpeg);

            fromImage.Dispose();
            bitmap.Dispose();
            graphic.Dispose();
            saveImage.Dispose();
            return saveFilePath;
        }

        /// <summary>
        /// 获取文件MD5
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        /// <summary>
        /// 开始进行解析
        /// </summary>
        private void StartCut()
        {
            while (true)
            {
                #region 主流程

                if (!string.IsNullOrWhiteSpace(oldFullScreen))
                {
                    if (File.Exists(oldFullScreen))
                    {
                        File.Delete(oldFullScreen);
                    }
                }

                //截屏
                Bitmap bit = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                Graphics g = Graphics.FromImage(bit);
                g.CopyFromScreen(new Point(0, 0), new Point(0, 0), bit.Size);
                fullScreenPath = string.Format(Environment.CurrentDirectory + @"\fullScreen\{0}.jpg", "1");
                oldFullScreen = fullScreenPath;
                bit.Save(fullScreenPath, ImageFormat.Jpeg);
                bit.Dispose();
                g.Dispose();

                /*
                 之前的思路是 截图整屏,切割手机大小的一个图,然后切割答题卡.现在不进行切割.直接对整屏进行答题卡判和取答题卡
                 */
                //if (!string.IsNullOrWhiteSpace(oldPhoneScreen))
                //{
                //    if (File.Exists(oldPhoneScreen))
                //    {
                //        File.Delete(oldPhoneScreen);
                //    }
                //}
                //
                //整瓶转换手机屏
                //string phonePath = FullScreenToPhone(fullScreenPath);
                //oldPhoneScreen = phonePath;
                //判断是否有答题卡
                // if (!HaveAnwserCard(phonePath))
                if (!HaveAnwserCard(fullScreenPath))
                {
                    label1.Text = "没有答题卡";
                    GC.Collect(0, GCCollectionMode.Forced);
                    continue;
                }
                label1.Text = "检测到答题卡,正在解析...";


                //选取问题框
                questionPath = CutImg(fullScreenPath);
               // string questionPath = CutImg(phonePath);
                // 判断答题卡是不是已经识别过
                if (IsOldAnwserCard(questionPath))
                {
                    label1.Text = "这是一个识别过的答题卡:(";
                    GC.Collect(0, GCCollectionMode.Forced);
                    continue;
                }
                ReadQuestion(questionPath); 
              
                GC.Collect(0, GCCollectionMode.Forced);
                #endregion
                Thread.Sleep(600);
            }
        }
        /// <summary>
        /// 判断是不是一个解析过得答题卡
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool IsOldAnwserCard(string filePath)
        {


            string md5 = GetMD5HashFromFile(filePath);
            //这问题已经扫描过
            if (imagecache.ToString().Contains(md5))
            {

                return true;
            }
            else
            {
                imagecache.Append(md5);
                return false;
            }
        }

        /// <summary>
        /// 判断是否有答题卡
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool HaveAnwserCard(string filePath)
        {
            Bitmap bit = new Bitmap(filePath);
            int line_y = 0;//确认颜色的坐标;
            List<Color> colorlist = new List<Color>();

            line_y = 170;
            if (AppName == "冲顶大会")
            {
                line_y = 240;
            }
            //取 4 个像素点 判断颜色是否一致 一致表示出现了答题卡
            colorlist.Add(bit.GetPixel(55, line_y));
            colorlist.Add(bit.GetPixel(110, line_y));
            colorlist.Add(bit.GetPixel(165, line_y));
            colorlist.Add(bit.GetPixel(210, line_y));
            bit.Dispose();
            return ColorEques(colorlist);
        }
        /// <summary>
        /// 读取图片中的题目
        /// </summary>
        /// <param name="image"></param>
        private void ReadQuestion(string image)
        {
            try
            {

                // 带参数调用通用文字识别, 图片参数为本地图片
                var cxxx = File.ReadAllBytes(image);
                var result = client.GeneralBasic(cxxx, options);
                cxxx = null;
                if (result == null)
                {
                    File.Delete(image);
                    return;

                }
                var words = result["words_result"];
                if (words == null)
                {
                    File.Delete(image);
                    return;
                }
                int c = words.Count();
                if (c == 0)
                {
                    File.Delete(image);
                    label1.Text = "没有识别到问题 :(";
                    return;

                }

                StringBuilder question = new StringBuilder();
                for (int i = 0; i < c; i++)
                {
                    question.Append(words[i]["words"]);
                }

                if (string.IsNullOrWhiteSpace(question.ToString()))
                {
                    File.Delete(image);
                    label1.Text = "没有识别到问题 :(";
                    return;

                }
                //特别短的文字 也不是一个正常是别的问题
                if (question.Length < 6)
                {
                    File.Delete(image);
                    label1.Text = "没有识别到问题 :(";
                    return;
                }
                //这问题已经扫描过
                if (questioncache.ToString().Contains(question.ToString()))
                {
                    File.Delete(image);
                    label1.Text = "这是一个查询过得问题:(";
                    return;
                }
                this.textBox1.AppendText(question + "\r\n");
                questioncache.Append(question);
                Image oldimage = pictureBox1.Image;
                //确定是一个新的问题,再显示这个图片 
                pictureBox1.Image = Image.FromFile(image);
                if (oldimage != null)
                {
                    oldimage.Dispose();
                }

                string newquestion = question.ToString().Replace("以下", "");

                Post(newquestion);
                var googleurl = string.Format(@"http://www.sogou.com/web?query={0}&ie=utf8", newquestion);
                var baiduurl = string.Format(@"https://www.baidu.com/s?wd={0}", newquestion);
                LoadWeb(googleurl, baiduurl);
            }
            catch (Exception e)
            {
                return;
            }

        }
        /// <summary>
        /// 加载网页
        /// </summary>
        /// <param name="googleurl"></param>
        /// <param name="baiduurl"></param>
        private void LoadWeb(string googleurl, string baiduurl)
        {
            this.webBrowser1.Url = new Uri(baiduurl);
            //this.webBrowser2.Url = new Uri(googleurl);
        }

        private void Post(string question)
        {
            try
            {
                string url = "http://wx.sharetogether.cn/api/question/noticequestion";
                string postString = "question=" + question;
                byte[] postData = Encoding.UTF8.GetBytes(postString);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                request.Method = "POST";
                //for some customer,use proxy, so add this.
                //refer:http://www.cnblogs.com/cxd4321/archive/2012/01/30/2331621.html
                request.ServicePoint.Expect100Continue = false;
                int timeout = 5000;
                if (timeout < 5000 || timeout > 15000)
                    timeout = 5000;
                request.Timeout = timeout;
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postData.Length;
                using (Stream requestStream = request.GetRequestStream())
                {
                    requestStream.Write(postData, 0, postData.Length);
                }
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader stream = new StreamReader(response.GetResponseStream(), Encoding.Default);
                string srcString = stream.ReadToEnd();
                response.Close();
                stream.Close();
                Trace.WriteLine(srcString);
            }
            catch (Exception ex)
            {
                GC.Collect();
            }
        }


        /// <summary>
        /// 判断颜色素组的颜色是否一致
        /// </summary>
        /// <param name="colorlist"></param>
        /// <returns></returns>
        private bool ColorEques(List<Color> colorlist)
        {
            if (colorlist.Count == 0)
            {
                return false;
            }

            byte r = colorlist[0].R;
            byte g = colorlist[0].G;
            byte b = colorlist[0].B;
            //
            if (r == 29 && g == 20 && b == 67)
            {
                return false;
            }
            if (r == 29 && g == 19 && b == 69)
            {
                return false;
            }
            if (r == 0 && g == 0 && b == 0)
            {
                return false;
            }
            bool result = true;
            foreach (var color in colorlist)
            {
                if (r != color.R)
                {
                    result = false;
                    break;

                }
                if (g != color.G)
                {
                    result = false;
                    break;
                }
                if (b != color.B)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
        /// <summary>
        /// 全屏截图转换为手机屏幕截图
        /// </summary>
        /// <param name="fullScreenPath"></param>
        /// <returns></returns>
        private string FullScreenToPhone(string fullScreenPath)
        {
            Image fromImage = Image.FromFile(fullScreenPath);

            //创建新图位图   
            Bitmap bitmap = new Bitmap(546, 1006);

            Graphics graphic = Graphics.FromImage(bitmap);

            //创建作图区域   

            // new Rectangle(矩形左上角的 x 坐标。, 矩形左上角的 y 坐标。, 矩形的宽度, 矩形的高度。)
            graphic.DrawImage(fromImage, 0, 0, new Rectangle(0, 0, 546, 1006), GraphicsUnit.Pixel);
            Image saveImage = Image.FromHbitmap(bitmap.GetHbitmap());

            //从作图区生成新图   

            string PhoneScreenPath = string.Format(Environment.CurrentDirectory + @"\PhoneScreen\{0}.jpg", "2");// DateTime.Now.Ticks);
            saveImage.Save(PhoneScreenPath, ImageFormat.Jpeg);

            fromImage.Dispose();
            bitmap.Dispose();
            graphic.Dispose();
            saveImage.Dispose();
            return PhoneScreenPath;
        }


        /// <summary>
        /// 停止识别
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if (th1 != null)
            {
                //pictureBox1.Image = null;
                //string p = Environment.CurrentDirectory + "\\Question";
                //DeleteFolder(p);
                th1.Abort();
                button1.Enabled = true;
                client = null;
            }
        }

        public void DeleteFolder(string dir)
        {
            //如果存在这个文件夹删除之   
            if (Directory.Exists(dir))
            {
                foreach (string d in Directory.GetFileSystemEntries(dir))
                {
                    if (File.Exists(d))
                        File.Delete(d);//直接删除其中的文件   
                    else DeleteFolder(d);//递归删除子文件夹    
                }
                Directory.Delete(dir);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            radioButton1.Checked = true;
            if (!System.IO.Directory.Exists(Environment.CurrentDirectory + "\\fullScreen"))
            {
                System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + "\\fullScreen");
            }
            if (!System.IO.Directory.Exists(Environment.CurrentDirectory + "\\PhoneScreen"))
            {
                System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + "\\PhoneScreen");
            }
            if (!System.IO.Directory.Exists(Environment.CurrentDirectory + "\\Question"))
            {
                System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + "\\Question");
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            this.AppName = "芝士超人";
            this.Text = this.AppName + "辅助查询";
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            this.AppName = "百万英雄";
            this.Text = this.AppName + "辅助查询";
        }
        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            this.AppName = "冲顶大会";
            this.Text = this.AppName + "辅助查询";
        }
        /// <summary>
        /// 窗体关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (th1 != null)
            {
                th1.Abort();

            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            StartCut();
        }

    }
}
