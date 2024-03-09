using Microsoft.Web.WebView2.Core;
using System;
using System.Drawing;
using System.Windows.Forms;
using WebView2 = Microsoft.Web.WebView2.WinForms.WebView2;

namespace pdf_generator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private static string filename = "";
        public WebView2 webView21 = new WebView2();
        private static int width;
        private static int height;
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
        private async void Form1_Load(object sender, EventArgs e)
        {
            width = CentimeterToPixel(21.0 + 3.8);
            height = Screen.PrimaryScreen.Bounds.Height - 30;
            this.Location = new Point(Screen.PrimaryScreen.Bounds.Width / 2 - width / 2, 0);
            this.Size = new Size(width, height);
            this.pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            this.pictureBox1.Size = new Size(width, height - 21);
            this.pictureBox1.Location = new Point(0, 21);
            this.textBox1.AutoSize = false;
            this.button1.Location = new Point(0, 0);
            this.button1.Size = new Size(111, 21);
            this.button2.Location = new Point(111, 0);
            this.button2.Size = new Size(111, 21);
            this.button3.Location = new Point(width - 111, 0);
            this.button3.Size = new Size(94, 21);
            this.textBox1.Location = new Point(222, 0);
            this.textBox1.Size = new Size(width - 333, 21);
            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disable-web-security --autoplay-policy=no-user-gesture-required", "en");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await webView21.EnsureCoreWebView2Async(environment);
            webView21.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", "assets", CoreWebView2HostResourceAccessKind.DenyCors);
            webView21.Source = new System.Uri("https://google.com");
            webView21.NavigationCompleted += WebView21_NavigationCompleted;
            webView21.MouseMove += WebView21_MouseMove;
            webView21.Dock = DockStyle.Fill;
            this.pictureBox1.Controls.Add(webView21);
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
        private void WebView21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            textBox1.Text = webView21.Source.ToString();
        }
        private void button1_Click(object sender, EventArgs e)
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
            webView21.CoreWebView2.Navigate(@"file:///" + filename);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            webView21.Source = new Uri(textBox1.Text);
        }
        private void button3_Click(object sender, EventArgs e)
        {
            webView21.CoreWebView2.ShowPrintUI();
        }
        private void settofrontButtons()
        {
            this.pictureBox1.SendToBack();
        }
        private void settofrontView()
        {
            this.pictureBox1.BringToFront();
        }
        private void WebView21_MouseMove(object sender, MouseEventArgs e)
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
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            valchanged(1, e.Y <= 21);
            if (wd[1] == 1)
            {
                settofrontButtons();
            }
            if (wu[1] == 1)
            {
                settofrontView();
            }
        }
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            valchanged(2, e.Y <= 21);
            if (wd[2] == 1)
            {
                settofrontButtons();
            }
            if (wu[2] == 1)
            {
                settofrontView();
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            webView21.Dispose();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
        }
    }
}