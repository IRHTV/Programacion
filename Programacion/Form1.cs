using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace Programacion
{
    public partial class Form1 : Form
    {
        int _TimeOffsetMin = 0;
        string DateTimeStr = "";
        List<Schedule> RvLst = new List<Schedule>();
        public Form1()
        {
            InitializeComponent();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            string[] Timesvl = ConfigurationSettings.AppSettings["RenderIntervalMin"].ToString().Trim().Split('#');
            foreach (string item in Timesvl)
            {
                if ((DateTime.Now >= Convert.ToDateTime(item)) && (DateTime.Now <= Convert.ToDateTime(item).AddMinutes(1)))
                {
                    timer1.Enabled = false;
                    button1_Click(new object(), new EventArgs());
                }
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            _TimeOffsetMin = int.Parse(ConfigurationSettings.AppSettings["TimeOffsetMin"].ToString().Trim());
            timer1.Interval = 3000;
            timer1.Enabled = true;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            // timer1.Interval = int.Parse(ConfigurationSettings.AppSettings["RenderIntervalMin"].ToString().Trim()) * 60 * 1000;
            DateTimeStr = string.Format("{0:0000}", DateTime.Now.AddMinutes(_TimeOffsetMin).Year) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Month) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Day) + "_" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Hour) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Minute) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Second);

            button1.ForeColor = Color.White;
            button1.Text = "Started";
            button1.BackColor = Color.Red;

            richTextBox1.Text = "";
            LoadData();
            StartRender();

            button1.ForeColor = Color.White;
            button1.Text = "Start";
            button1.BackColor = Color.Navy;
            timer1.Enabled = true;
        }
        protected void LoadData()
        {
            try
            {
                string date = DateTime.Now.AddMinutes(_TimeOffsetMin).ToString("yyyy-MM-dd HH;mm;ss"); //2015-08-27%205;50;50?no=10
                HttpWebRequest request =
                            (HttpWebRequest)WebRequest.Create(System.Configuration.ConfigurationSettings.AppSettings["ScheduleService"].Trim() + date + "?no=10");
                WebResponse response = request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                var result = reader.ReadToEnd();
                stream.Dispose();
                reader.Dispose();
                List<Schedule> RvLst = JsonConvert.DeserializeObject<List<Schedule>>(result);
                if (RvLst.Count == 10)
                {
                    StringBuilder Data = new StringBuilder();
                    Data.Append("titulos = [");
                    foreach (var item in RvLst)
                    {
                        Data.Append("\"" + NormalizeTitle(item.Prog) + "\",");
                    }
                    Data.Append(" ];");

                    Data.Append("\r\n times = [");
                    foreach (var item in RvLst)
                    {
                        Data.Append("\"" + item.Hour + " GMT\",");
                    }
                    Data.Append(" ];");
                    string DataTxtFile = ConfigurationSettings.AppSettings["DataTxtPath"].ToString().Trim();
                    if (!File.Exists(DataTxtFile))
                    {
                        FileStream Fst = File.Create(DataTxtFile);
                        Fst.Close();
                    }
                    StreamWriter Sw = new StreamWriter(DataTxtFile, false, System.Text.Encoding.UTF8);
                    Sw.Write(Data.ToString());
                    Sw.Close();
                }
            }
            catch (Exception Exp)
            {
                richTextBox1.Text += "\nError : " + Exp.Message + "\n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
            }
        }
        protected void StartRender()
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = "\"" + ConfigurationSettings.AppSettings["AeRenderPath"].ToString().Trim() + "\"";
                DateTimeStr = string.Format("{0:0000}", DateTime.Now.AddMinutes(_TimeOffsetMin).Year) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Month) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Day) + "_" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Hour) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Minute) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Second);

                try
                {
                    string[] Dirsold = Directory.GetDirectories(ConfigurationSettings.AppSettings["OutputPath"].ToString().Trim());
                    foreach (var item in Dirsold)
                    {
                        DirectoryInfo Dd = new DirectoryInfo(item);
                        if (Dd.CreationTime.AddDays(3) < DateTime.Now.AddMinutes(_TimeOffsetMin))
                            Dd.Delete(true);
                    }

                }
                catch
                {

                }

                DirectoryInfo Dir = new DirectoryInfo(ConfigurationSettings.AppSettings["OutputPath"].ToString().Trim() + string.Format("{0:0000}", DateTime.Now.AddMinutes(_TimeOffsetMin).Year) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Month) + "-" + string.Format("{0:00}", DateTime.Now.AddMinutes(_TimeOffsetMin).Day));

                if (!Dir.Exists)
                {
                    Dir.Create();
                }

                proc.StartInfo.Arguments = " -project " + "\"" + ConfigurationSettings.AppSettings["AeProjectFile"].ToString().Trim() + "\"" + "   -comp   \"" + ConfigurationSettings.AppSettings["Composition"].ToString().Trim() + "\" -output " + "\"" + Dir + "\\" + ConfigurationSettings.AppSettings["OutPutFileName"].ToString().Trim() + "_" + DateTimeStr + ".mp4" + "\"";
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = true;
                proc.EnableRaisingEvents = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;

                if (!proc.Start())
                {
                    return;
                }

                proc.PriorityClass = ProcessPriorityClass.Normal;
                StreamReader reader = proc.StandardOutput;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (richTextBox1.Lines.Length > 10)
                    {
                        richTextBox1.Text = "";
                    }
                    richTextBox1.Text += (line) + " \n";
                    richTextBox1.SelectionStart = richTextBox1.Text.Length;
                    richTextBox1.ScrollToCaret();
                    Application.DoEvents();

                }
                proc.Close();
            }
            catch (Exception Exp)
            {
                richTextBox1.Text += " \n" + Exp + " \n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
                Application.DoEvents();
            }
        }
        protected string NormalizeTitle(string ProgName)
        {
            //2014-01-25 Replace Documentry by Doc
            ProgName = ProgName.Replace("documentary", "Doc");
            ProgName = ProgName.Replace("Documentary", "Doc");
            ProgName = ProgName.Replace("\"", "'");

            int FirstIndex = ProgName.IndexOf("-");
            int SecondIndex = 0;
            string SubTtl = "";
            if (FirstIndex > 0)
            {                
                if (ProgName.ToUpper().StartsWith("DOC") || ProgName.ToUpper().StartsWith("SERI") || ProgName.ToUpper().StartsWith("PELÍ"))
                {
                    SecondIndex = ProgName.IndexOf("-", FirstIndex + 1);
                    if (SecondIndex > FirstIndex)
                    {
                        SubTtl= ProgName.Substring(FirstIndex, SecondIndex - FirstIndex + 1);
                        ProgName = ProgName.Remove(FirstIndex, SecondIndex - FirstIndex + 1);
                        ProgName = ProgName.Insert(FirstIndex, ":");
                    }
                    ProgName = ProgName.Replace("  :", ":");
                    ProgName = ProgName.Replace(" :", ":");
                    if (!ProgName.ToUpper().StartsWith("PELÍ"))
                    {
                       ProgName += SubTtl.Replace("-","");
                    }
                    else
                    {
                        ProgName = ProgName.Replace("Películas -", "Películas:");
                    }
                }
                else
                {
                   
                    ProgName = ProgName.Remove(FirstIndex, ProgName.Length - FirstIndex);
                }
            }

            int TitleLenght = int.Parse(ConfigurationSettings.AppSettings["TitleLenght"].ToString().Trim());
            if (ProgName.Length > TitleLenght)
            {
                char[] delimiters = new char[] { ' ' };
                string[] PrgNameList = ProgName.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                int CutIndex = 0;
                string OutName = "";
                bool AllowAdd = true;
                foreach (string item in PrgNameList)
                {
                    if (CutIndex + item.Length + 1 < TitleLenght)
                    {
                        if (AllowAdd)
                        {
                            CutIndex += item.Length + 1;
                            OutName += item + " ";
                        }
                    }
                    else
                    {
                        AllowAdd = false;
                    }
                }
                ProgName = OutName + "...";
            }
            return ProgName;
        }
    }
}
