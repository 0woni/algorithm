using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 에지검출
{
    public partial class Form1 : Form
    {
        Graphics grBm;
        Image image;
        Bitmap bitmap;
        int[,] grayArray;
        public Form1()
        {
            InitializeComponent();
            setShadowBitmap();
        }
        void setShadowBitmap()
        {
            bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            grBm = Graphics.FromImage(bitmap);
            grBm.Clear(BackColor);
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics gr = e.Graphics;
            gr.DrawImage(bitmap, 0, 0);
        }

        private void 열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = " Open image";
            openFileDialog1.Filter = " All Files(*.*) |*.*| Bitmap File(*.bmp) | *.bmp |Jpeg File(*.jpg) | *.jpg";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string strFilename = openFileDialog1.FileName;
                image = Image.FromFile(strFilename);
                this.ClientSize = new System.Drawing.Size(image.Width, image.Height);
                setShadowBitmap();
                grBm.DrawImage(image, 0, 30, image.Width, image.Height);
                copyBitmap2Array();
            }
            Invalidate();
        }
        void copyBitmap2Array()
        {
            int x, y, brightness;
            Color color;
            grayArray = new int[bitmap.Height, bitmap.Width];
            for (y = 0; y < bitmap.Height; y++)
                for (x = 0; x < bitmap.Width; x++)
                {
                    color = bitmap.GetPixel(x, y);
                    brightness = color.R;
                    grayArray[y, x] = brightness;
                }
        }

        private void 저장ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Title = "Save Image as ...";
            saveFileDialog1.OverwritePrompt = true;
            saveFileDialog1.Filter = " All Files(*.*) |*.*| Bitmap File(*.bmp) | *.bmp | Jpeg File(*.jpg) | *.jpg";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string strFilename = saveFileDialog1.FileName;
                string strLowerFilename = strFilename.ToLower();
                bitmap.Save(strLowerFilename);
            }
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void displayResultArray1(int[,] resultArray, int Width, int Height)
        {
            int x, y;
            Color color;
            Bitmap gBitmap = new Bitmap(Width, Height);
            for (y = 0; y < Height; y++)
                for (x = 0; x < Width; x++)
                {
                    color = Color.FromArgb(resultArray[y, x], resultArray[y, x],resultArray[y, x]);
                    gBitmap.SetPixel(x, y, color);
                }
            grBm.DrawImage(gBitmap, 0, 0, gBitmap.Width, gBitmap.Height);
            Invalidate();
        }

        int[,] convolve2(int[,] G, int Width, int Height, double[,] M, int maskCol, int maskRow)
        {
            int[,] R = new int[Height, Width];
            int x, y, r, c;
            int xPad = maskCol / 2;
            int yPad = maskRow / 2;
            double sum;
            for (y = 0; y < Height - 2 * yPad; y++)
                for (x = 0; x < Width - 2 * xPad; x++)
                {
                    sum = 0.0;
                    for (r = 0; r < maskRow; r++)
                        for (c = 0; c < maskCol; c++)
                            sum += G[y + r, x + c] * M[r, c];
                    sum = Math.Abs(sum);
                    if (sum > 255.0) sum = 255.0;
                    if (sum < 0.0) sum = 0.0;
                    R[y + yPad, x + xPad] = (int)sum;
                }
            for (y = 0; y < yPad; y++)
            {
                for (x = xPad; x < Width - xPad; x++)
                {
                    R[y, x] = R[yPad, x];
                    R[Height - 1 - y, x] = R[Height - 1 - yPad, x];
                }
            }
            for (x = 0; x < xPad; x++)
            {
                for (y = 0; y < Height; y++)
                {
                    R[y, x] = R[y, xPad];
                    R[y, Width - 1 - x] = R[y, Width - 1 - xPad];
                }
            }
            return R;
        }
        int[,] convolveXY(int[,] G, int Width, int Height, double[,] M1, double[,] M2, int maskCol, int maskRow)
        {
            int[,] R = new int[Height, Width];
            int x, y, r, c;
            int xPad = maskCol / 2;
            int ypad = maskRow / 2;
            double sum, sum1, sum2;
            for (y=0; y<Height-2*ypad; y++) 
            {
                for (x=0; x<Width-2*xPad; x++)
                {
                    sum1 = sum2 = 0.0;
                    for (r=0; r<maskRow; r++)
                    {
                        for (c=0; c<maskCol; c++)
                        {
                            sum1 += G[y + r, x + c] * M1[r, c];
                            sum2 += G[y + r, x + c] * M2[r, c];
                        }
                    }
                    sum=Math.Abs(sum1)+Math.Abs(sum2);
                    if (sum > 255.0) sum = 255.0;
                    if (sum < 0.0) sum = 0.0;
                    R[y + ypad, x + xPad] = (int)sum;
                }
            }
            for (y=0; y<ypad; y++)
            {
                for (x=xPad; x<Width-xPad; x++)
                {
                    R[y, x] = R[ypad, x];
                    R[Height - 1 - y, x] = R[Height - 1 - ypad, x];

                }
            }
            for(x=0; x<xPad; x++)
            {
                for(y=0; y<Height; y++)
                {
                    R[y, x] = R[y, xPad];
                    R[y, Width - 1 - x] = R[y, Width - 1 - xPad];
                }
            }
            return R;
        }
        private void 수평소벨마스크ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] mask = { { -1.0, 0.0, 1.0 }, { -2.0, 0.0, 2.0 }, { -1.0, 0.0, 1.0 } };
            int[,] ResultArray = convolve2(grayArray, image.Width, image.Height, mask, 3, 3);
            displayResultArray1(ResultArray, image.Width, image.Height);
        }

        private void 수직소벨마스크ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] mask = { { -1.0, -2.0, -1.0 }, { 0.0, 0.0, 0.0 }, { 1.0, 2.0, 1.0 } };
            int[,] ResultArray = convolve2(grayArray, image.Width, image.Height, mask, 3, 3);
            displayResultArray1(ResultArray, image.Width, image.Height);
        }

        private void 양방향소벨마스크ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double[,] mask1 = { { -1.0, 0.0, 1.0 }, { -2.0, 0.0, 2.0 }, { -1.0, 0.0, 1.0 } };
            double[,] mask2 = { { -1.0, -2.0, -1.0 }, { 0.0, 0.0, 0.0 }, { 1.0, 2.0, 1.0 } };
            // 원래 -1.0, -2.0, -1.0임 변경했던 값 -2.0, -2.0, -1.0
            int[,] ResultArray=convolveXY(grayArray, image.Width,image.Height, mask1, mask2, 3, 3);
            displayResultArray1(ResultArray, image.Width, image.Height);
        }
    }
}
