using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 사다리꼴_과제
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            setShadowBitmap();
        }
        Bitmap bitmap;
        Graphics gr;
        Image image;
        int[,] grayArray;
        void setShadowBitmap()
        {
            bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
            gr = Graphics.FromImage(bitmap);
            gr.Clear(BackColor);
        }
        void loadimage(string path)
        {
            image = Image.FromFile(path);
            bitmap = new Bitmap(image);
            grayArray = new int[bitmap.Height, bitmap.Width];
            for (int i = 0; i < bitmap.Height; i++)
                for (int j = 0; j < bitmap.Width; j++)
                {
                    Color color = bitmap.GetPixel(j, i);
                    int gray = (int)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    grayArray[i, j] = gray;
                }
            Invalidate();
        }

        double fuzzyMembership(double Imin, double Imax, double Immin, double Immin2, double Imid, double p)
        {
            if (p <= Imin || p >= Imax)
                return 0;
            else if (p > Immin2 && p < Imax)
                return (Imax - p) / (Imax - Imid);
            else if (p < Immin && p > Imin)
                return (p - Imin) / (Imid - Imin);
            else if (p >= Immin && p <= Immin2)
                return 1;
            else
                return 0;
        }

        void fuzzyThresholdWithEdge(bool use)
        {
            int w = bitmap.Width;
            int h = bitmap.Height;

            int Xmin = 255, Xmax = 0, rSum = 0, gSum = 0, bSum = 0;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    int gray = (color.R + color.G + color.B) / 3;
                    rSum += color.R;
                    gSum += color.G;
                    bSum += color.B;
                    if (gray < Xmin) Xmin = gray;
                    if (gray > Xmax) Xmax = gray;
                }
            }

            int m1 = (Xmin + Xmax) / 2;
            int m2 = (rSum + gSum + bSum) / (3 * w * h);
            double a1 = (m1 + m2) / 2.0;
            double a2 = a1 / 255.0;

            int Dmax = Math.Abs(Xmax - m2);
            int Dmin = Math.Abs(m2 - Xmin);
            int a = Math.Min(Math.Min(Dmin, Dmax), m2);

            int Imax = m2 + a;
            int Imin = m2 - a;
            int Imid = (Imax + Imin) / 2;
            int w1 = Math.Abs(m1 - m2);
            int w2 = Math.Abs(m1 + m2);

            int Immin = Imid - w1;
            int Immin2 = Imid - w2;

            int[,] fuzzyResult = new int[h, w];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double p = grayArray[y, x];
                    double membership = fuzzyMembership(Imin, Imax, Immin, Immin2, Imid, p);
                    fuzzyResult[y, x] = membership > (0.5 + a2) ? 0 : 255;
                }
            }
            if (!use)
            {
                displayArray(0, 0, fuzzyResult);
                return;
            }
            int[,] edgeResult = sobelEdgeDetection(grayArray);

            // 결합: 경계는 어둡게 유지
            int[,] combined = new int[h, w];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    combined[y, x] = (fuzzyResult[y, x] == 0 || edgeResult[y, x] == 0) ? 0 : 255;
                }
            }

            displayArray(0, 0, combined);
        }

        int[,] sobelEdgeDetection(int[,] gray)
        {
            int h = gray.GetLength(0);
            int w = gray.GetLength(1);
            int[,] result = new int[h, w];

            int[,] gx = new int[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            int[,] gy = new int[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    int sumX = 0, sumY = 0;
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            sumX += gray[y + j, x + i] * gx[j + 1, i + 1];
                            sumY += gray[y + j, x + i] * gy[j + 1, i + 1];
                        }
                    }

                    int magnitude = (int)Math.Sqrt(sumX * sumX + sumY * sumY);
                    result[y, x] = magnitude > 128 ? 0 : 255;  // 임계값 128
                }
            }
            return result;
        }

        void displayArray(int leftTopX, int leftTopY, int[,] array)
        {
            Bitmap output = new Bitmap(array.GetLength(1), array.GetLength(0));
            for (int i = 0; i < output.Height; i++)
                for (int j = 0; j < output.Width; j++)
                {
                    int v = Math.Max(0, Math.Min(255, array[i, j]));
                    output.SetPixel(j, i, Color.FromArgb(v, v, v));
                }
            bitmap = output;
            gr = Graphics.FromImage(bitmap);
            Invalidate();
        }
        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
        private void 열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "이미지 파일|*.bmp;*.jpg;*.png";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                loadimage(dlg.FileName);
            }
        }

        private void 저장ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Title = "파일저장";
            saveFileDialog1.Filter = "All File(*.*)|*.*|BitmapFile(*.bmp)|*.bmp";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string str = saveFileDialog1.FileName;
                string strsaver = str.ToLower();
                image.Save(strsaver);
            }
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void 사다리꼴ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fuzzyThresholdWithEdge(false);
        }

        private void sobel필터ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fuzzyThresholdWithEdge(true);
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(bitmap, 0, 0);
        }
    }
}
