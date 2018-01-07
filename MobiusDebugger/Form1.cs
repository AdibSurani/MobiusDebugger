using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MobiusDebugger
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;
            WindowState = FormWindowState.Maximized;
        }

        IEnumerator<MobiContainer.Frame> frames;
        int frameNo = -1;
        MobiDecoder decoder;

        private void Form1_Load(object sender, EventArgs e)
        {
            //LoadVideo(@"C:\Users\Adib\Downloads\PL2.mods");
            //LoadVideo(@"C:\Users\Adib\Downloads\out.264");
        }

        Bitmap baseImage;

        int scale = 4;
        PredictInfo[,] table;
        PredictInfo lastInfo;

        private void nextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frames == null) return;
            if (!frames.MoveNext()) return;
            var frame = frames.Current;
            table = new PredictInfo[frame.Width, frame.Height];
            if (decoder == null) decoder = new MobiDecoder(frame.Width, frame.Height);
            //var frameImage = decoder.Parse(frame.Stream);
            scale = Math.Min(pictureBox1.Width / frame.Width, pictureBox1.Height / frame.Height);
            if (scale == 0) scale = 1;
            var bmp = BlowUp(decoder.Parse(frame.Stream), scale);
            if (decoder.predictInfos.Any())
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    foreach (var info in decoder.predictInfos)
                    {
                        g.DrawRectangle(Pens.Red, scale * info.x, scale * info.y, scale * info.width, scale * info.height);
                        for (int y = 0; y < info.height; y++)
                            for (int x = 0; x < info.width; x++)
                            {
                                //if (table[x + info.x, y + info.y] != null) throw new Exception();
                                table[x + info.x, y + info.y] = info;
                            }
                    }
                }
            }
            pictureBox1.BackgroundImage = bmp;
            //BackgroundImage = baseImage;
        }

        public Bitmap BlowUp(Bitmap source, int factor)
        {
            var result = new Bitmap(source.Width * factor, source.Height * factor);
            using (var g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.ScaleTransform(factor, factor);
                g.DrawImage(source, 0, 0, source.Width, source.Height);
            }
            return result;
        }

        void SetInfo(PredictInfo info)
        {
            if (info == lastInfo) return;
            if (info == null)
            {
                label1.Text = "Not a motion-predicted block";
            }
            else
            {
                label1.Text = $"Vector = ({-info.dx * 0.5}, {-info.dy * 0.5}) from {info.srcFrame} frames back";
            }
            lastInfo = info;
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            //(mouseX, mouseY) = (e.X, e.Y);
            if (table == null) return;
            var (x, y) = (e.X / scale, e.Y / scale);
            if (x >= table.GetLength(0)) return;
            if (y >= table.GetLength(1)) return;
            SetInfo(table[x, y]);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            //var (x, y) = (mouseX / scale, mouseY / scale);
            //if (table == null) return;
            //if (x >= table.GetLength(0)) return;
            //if (y >= table.GetLength(1)) return;
            var info = lastInfo;
            if (info == null) return;
            using (var brush1 = new SolidBrush(Color.FromArgb(128, Color.Yellow)))
            using (var brush2 = new SolidBrush(Color.FromArgb(128, Color.Green)))
            using (var pen = new Pen(Brushes.Red))
            {
                //new Tran
                e.Graphics.FillRectangle(brush1, new Rectangle(scale * info.x, scale * info.y, scale * info.width, scale * info.height));
                e.Graphics.FillRectangle(brush2, new Rectangle(scale * info.x + scale * info.dx / 2, scale * info.y + scale * info.dy / 2, scale * info.width, scale * info.height));
                pen.EndCap = LineCap.ArrowAnchor;
                e.Graphics.DrawLine(pen,
                    (info.x + (info.dx + info.width) / 2) * scale,
                    (info.y + (info.dy + info.height) / 2) * scale,
                    (info.x + info.width / 2) * scale,
                    (info.y + info.height / 2) * scale);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "Mobiclip videos|*.moflex;*.mods;*.264" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    LoadVideo(ofd.FileName);
                }
            }
        }

        void LoadVideo(string path)
        {
            //var inputPath = @"C:\Users\Adib\Downloads\PL2.mods";
            //var inputPath = @"C:\ffmpeg\OP.moflex";
            frames = MobiContainer.Demux(path).GetEnumerator();
            decoder = null;
            nextToolStripMenuItem_Click(null, null);
            BackgroundImageLayout = ImageLayout.None;
            SetInfo(null);
            //pictureBox1.Image = new MobiDecoder(frame0.Width, frame0.Height).Parse(frame0.Stream);
        }
    }
}
