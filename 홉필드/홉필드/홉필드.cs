using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; // MemoryStream 사용을 위한 선언

namespace 홉필드
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            // UI 초기화
            // 메인 Form 크기
            this.Size = new Size(590, 360);
            // PictureBox1 속성
            pictureBox1.Location = new Point(20, 40);
            pictureBox1.Size = new Size(456, 656);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            // PictureBox2 속성
            pictureBox2.Location = new Point(496, 40);
            pictureBox2.Size = new Size(456, 656);
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.BorderStyle = BorderStyle.FixedSingle;
        }
        public byte[,] BitmapToByteArray2D(Bitmap bmp)
        {
            byte[,] bmpArray = new byte[bmp.Height * bmp.Width, 3];
            for (int x = 0; x < bmp.Width; x++)
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color pixelColor = bmp.GetPixel(x, y);
                    bmpArray[y * bmp.Width + x, 0] = pixelColor.R;
                    bmpArray[y * bmp.Width + x, 1] = pixelColor.G;
                    bmpArray[y * bmp.Width + x, 2] = pixelColor.B;
                }
            return bmpArray;
        }
        public Bitmap byteArray2DToBitmap(byte[,] byteArray, int width, int height)
        {
            Bitmap newbmp = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Color newColor = Color.FromArgb(byteArray[y * width + x, 0],byteArray[y * width + x, 1], byteArray[y * width + x, 2]);
                    newbmp.SetPixel(x, y, newColor);
                }
            return newbmp;
        }

        private int[,] hopfieldSetWeight(int[,] image, int height, int width)
        {
            int[] S = new int[height * width];
            int[,] W = new int[height * width, height * width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    S[i * width + j] = image[i, j];
                }
            }
            for (int i = 0; i < height * width; i++)
            {
                for (int j = 0; j < height * width; j++)
                {
                    W[i, j] += S[i] * S[j];
                }
                W[i, i] = W[i, i] - 1;
            }
            return W;
        }
        public int[,] hopfieldPredict(int[,] image, int[,] W, int height, int width)
        {
            int[] Y = new int[height * width];
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Y[i * width + j] = image[i, j];
                }
            }
            int[] new_Y = new int[height * width];
            bool update = true;
            while (update)
            {
                update = false;
                for (int i = 0; i < height * width; i++)
                {
                    int net = 0;
                    for (int j = 0; j < height * width; j++)
                    {
                        net += W[j, i] * Y[j];
                    }
                    int new_y = 0;
                    if (net < 0)
                    {
                        new_y = -1;
                    }
                    else if (net > 0)
                    {
                        new_y = 1;
                    }
                    else
                    {
                        new_y = Y[i];
                    }
                    if (Y[i] != new_y)
                    {
                        update = true;
                    }
                    new_Y[i] = new_y;
                }
                for (int i = 0; i < height * width; i++)
                {
                    Y[i] = new_Y[i];
                }
            }
            int[,] result = new int[height, width];
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    result[i, j] = Y[i * width + j];
            return result;
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void hopfieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 0-1 화면 UI 수정
            // PictureBox3 속성
            pictureBox3.Location = new Point(1020, 40);
            pictureBox3.Size = new Size(456, 656);
            pictureBox3.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox3.BorderStyle = BorderStyle.FixedSingle;
            // 0-2 화면 크기 변경
            this.Size = new Size(590, 626);

            // 0-3 학습할 이미지 선택            
            OpenFileDialog open = new OpenFileDialog();
            open.Title = "학습할 이미지 선택";
            open.Filter = "Image Files(*.jpg; *.jpeg; *.png; *.bmp)|*.jpg; *.jpeg; *.png; *.bmp";
            if (open.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = new Bitmap(open.FileName);
            }
            // 0-4 예측할 이미지 선택
            open = new OpenFileDialog();
            open.Title = "예측할 이미지 선택";
            open.Filter = "Image Files(*.jpg; *.jpeg; *.png; *.bmp)|*.jpg; *.jpeg; *.png; *.bmp";
            if (open.ShowDialog() == DialogResult.OK)
            {
                pictureBox2.Image = new Bitmap(open.FileName);
            }
            Bitmap img = (Bitmap)pictureBox1.Image;
            int height = img.Height;
            int width = img.Width;
            byte[,] imgArray = BitmapToByteArray2D(img);

            // 1. 학습 이미지 초기화(이진화 포함)
            int[,] learn_img = new int[height, width];
            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                    learn_img[i, j] = imgArray[i * width + j, 0] > 128 ? 1 : -1;
            // 2. 가중치 배열 학습
            int[,] W = hopfieldSetWeight(learn_img, height, width);
            // 3. 예측할 이미지 초기화
            Bitmap img1 = (Bitmap)pictureBox2.Image;
            int height1 = img1.Height;
            int width1 = img1.Width;
            byte[,] imgArray1 = BitmapToByteArray2D(img1);
            int[,] target_img = new int[height1, width1];
            for (int i = 0; i < height1; i++)
                for (int j = 0; j < width1; j++)
                    target_img[i, j] = imgArray1[i * width1 + j, 0] > 128 ? 1 : -1;
            // 4. 학습된 결과 이미지 예측
            target_img = hopfieldPredict(target_img, W, height1, width1);
            // 5. 결과 이미지 복사
            for (int w = 0; w < width1; w++)
                for (int h = 0; h < height1; h++)
                {
                    imgArray1[h * width1 + w, 0] = target_img[h, w] > 0 ? (byte)255:(byte)0;
                    imgArray1[h * width1 + w, 1] = target_img[h, w] > 0 ? (byte)255:(byte)0;
                    imgArray1[h * width1 + w, 2] = target_img[h, w] > 0 ? (byte)255:(byte)0;
                }
            // 5. 출력
            pictureBox3.Image = byteArray2DToBitmap(imgArray1, width1, height1);
        }
    }
}
