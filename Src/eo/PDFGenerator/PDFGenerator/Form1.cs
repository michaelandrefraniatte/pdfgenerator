using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using System.ComponentModel;
using EO.WebBrowser;
using System.Media;
using System.Globalization;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using NetFwTypeLib;
namespace PDFGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private static int width;
        private static int height;
        public static Form1 form = (Form1)Application.OpenForms["Form1"];
        private static string filename = "";
        private static ProcessStartInfo startInfo;
        private static int[] wd = { 2, 2, 2, 2, 2 };
        private static int[] wu = { 2, 2, 2, 2, 2 };
        private static void valchanged(int n, bool val)
        {
            if (val)
            {
                if (wd[n] <= 1)
                {
                    wd[n] = wd[n] + 1;
                }
                wu[n] = 0;
            }
            else
            {
                if (wu[n] <= 1)
                {
                    wu[n] = wu[n] + 1;
                }
                wd[n] = 0;
            }
        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.pictureBox1.Dock = DockStyle.Fill;
            width = CentimeterToPixel(21.0 + 3.8);
            height = Screen.PrimaryScreen.Bounds.Height - 30;
            this.Location = new System.Drawing.Point(Screen.PrimaryScreen.Bounds.Width / 2 - width / 2, 0);
            this.Size = new System.Drawing.Size(width, height);
            this.textBox1.AutoSize = false;
            this.button1.Location = new System.Drawing.Point(0, 0);
            this.button1.Size = new System.Drawing.Size(111, 21);
            this.button2.Location = new System.Drawing.Point(111, 0);
            this.button2.Size = new System.Drawing.Size(111, 21);
            this.button3.Location = new System.Drawing.Point(width - 111, 0);
            this.button3.Size = new System.Drawing.Size(94, 21);
            this.textBox1.Location = new System.Drawing.Point(222, 0);
            this.textBox1.Size = new System.Drawing.Size(width - 333, 21);
            EO.WebEngine.BrowserOptions options = new EO.WebEngine.BrowserOptions();
            options.EnableWebSecurity = false;
            EO.WebBrowser.Runtime.DefaultEngineOptions.SetDefaultBrowserOptions(options);
            EO.WebEngine.Engine.Default.Options.AllowProprietaryMediaFormats();
            EO.WebEngine.Engine.Default.Options.SetDefaultBrowserOptions(new EO.WebEngine.BrowserOptions
            {
                EnableWebSecurity = false
            });
            this.webView1.Create(pictureBox1.Handle);
            this.webView1.Engine.Options.AllowProprietaryMediaFormats();
            this.webView1.SetOptions(new EO.WebEngine.BrowserOptions
            {
                EnableWebSecurity = false
            });
            this.webView1.Engine.Options.DisableGPU = false;
            this.webView1.Engine.Options.DisableSpellChecker = true;
            this.webView1.Engine.Options.CustomUserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            settofrontView();
            Navigate("");
        }
        private int CentimeterToPixel(double Centimeter)
        {
            double pixel = -1;
            using (Graphics g = this.CreateGraphics())
            {
                pixel = Centimeter * g.DpiY / 2.54d;
            }
            return (int)pixel;
        }
        [STAThread]
        private void button1_Click(object sender, EventArgs e)
        {
            Thread newThread = new Thread(new ThreadStart(showOpenFileDialog));
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start();
        }
        public void showOpenFileDialog()
        {
            textBox1.Text = "";
            DialogResult result = MessageBox.Show(@"Click Ok to load a local file, or click Cancel to reload the local file previously load", "Open", MessageBoxButtons.OKCancel);
            if (result == DialogResult.OK)
            {
                OpenFileDialog op = new OpenFileDialog();
                op.Filter = "All Files(*.*)|*.*";
                if (op.ShowDialog() == DialogResult.OK)
                {
                    filename = op.FileName;
                    LoadPage();
                }
            }
            else
            {
                LoadPage();
            }
        }
        private void LoadPage()
        {
            Navigate("");
            string readText = File.ReadAllText(filename);
            webView1.LoadHtml(readText);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            Navigate(textBox1.Text);
        }
        private void Navigate(string address)
        {
            if (String.IsNullOrEmpty(address))
                return;
            if (address.Equals("about:blank"))
                return;
            if (!address.StartsWith("http://") & !address.StartsWith("https://"))
                address = "https://" + address;
            try
            {
                webView1.Url = address;
            }
            catch (System.UriFormatException)
            {
                return;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            string full_url = textBox1.Text;
            if (full_url == "")
            {
                string url = filename;
                startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = "bin/wkhtmltopdf.exe";
                startInfo.Arguments = @"-T 0 -B 0 -L 0 -R 0 --enable-local-file-access " + url + " output.pdf";
                Process.Start(startInfo);
            }
            else
            {
                string html = this.webView1.GetHtml();
                Uri uri = new Uri(full_url);
                string baseUrl = string.Format("{0}://{1}{2}",
                    uri.Scheme, uri.Authority, uri.AbsolutePath);
                if (baseUrl.LastIndexOf("/") ==
                    baseUrl.LastIndexOf("://") + 2)
                    baseUrl = baseUrl + "/";
                int nPos = baseUrl.LastIndexOf("/");
                baseUrl = baseUrl.Substring(0, nPos + 1);
                EO.Pdf.HtmlToPdf.Options.BaseUrl = baseUrl;
                EO.Pdf.HtmlToPdf.ConvertHtml(html, "output.pdf");
            }
        }
        private void webView1_MouseMove(object sender, EO.Base.UI.MouseEventArgs e)
        {
            valchanged(0, e.Y <= 21);
            if (wd[0] == 1)
            {
                settofrontButtons();
            }
            if (wu[0] == 1)
            {
                settofrontView();
            }
        }
        private void settofrontButtons()
        {
            this.pictureBox1.SendToBack();
        }
        private void settofrontView()
        {
            this.pictureBox1.BringToFront();
            this.webView1.SetFocus();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.webView1.Dispose();
        }
    }
}
