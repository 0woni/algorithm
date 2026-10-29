using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FAM_과제_V2
{
    public partial class Form1 : Form
    {
        private Bitmap oimage, dimage, rimage; // 원본 이미지, 훼손 이미지, 복원 이미지

        private double[,] wr = new double[256, 256]; // 각 R, G, B 3 채널에 대한 가중치
        private double[,] wg = new double[256, 256];
        private double[,] wb = new double[256, 256];


        private const int pixel = 256; // 0-255
        private const double std = 5; // 퍼지화 함수의 표준편차 최소값 1e-3 -> 5
        private const int max = 10; // 반복 학습 최대 횟수
        private const double target = 1.0; // 목표 RMSE 값
        public Form1()
        {
            InitializeComponent();
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom; // 이미지 잘리는 것 방지
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void 열기ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "원본 이미지 열기";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                oimage = new Bitmap(openFileDialog1.FileName);
                pictureBox1.Image = oimage;

                openFileDialog1.Title = "손상된 이미지 열기";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    dimage = new Bitmap(openFileDialog1.FileName);
                    pictureBox2.Image = dimage;
                }
            }
        }

        private void 저장ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (rimage != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "PNG files (*.png)|*.png";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    rimage.Save(sfd.FileName);
                }
            }
        }

        private void 종료ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            double Rmse = double.MaxValue; //RMSE 수치
            int count = 0;

            // FAM 룰 초기화 (새로운 복원 시 매번 초기화)
            wr = new double[pixel, pixel];
            wg = new double[pixel, pixel];
            wb = new double[pixel, pixel];

            while (Rmse > target && count < max) // Rmse 수치가 설정한 값 이상이고, 최대 학습 횟수 이전까지 학습
            {
                FAM(dimage, oimage); // 손상된 이미지와 원본 이미지로 학습
                rimage?.Dispose(); // 이전 복원 이미지가 있다면 해제 (추가/수정)
                rimage = restore(dimage); // 손상된 이미지를 기반으로 복원
                Rmse = RMSE(oimage, rimage);

                count++;
            }

            pictureBox3.Image = rimage;
            MessageBox.Show($"이미지 복원 완료 \n최종 복원 RMSE: {Rmse:F4} (반복: {count}회)");

        }
        private double Fuzzify(double x, double m, double s)
        {
            s = Math.Max(s, std); // 0으로 나누는 것을 방지
            return Math.Exp(-Math.Pow(x - m, 2) / (2 * s * s)); // 가우시안 형태
        }
        private int Defuzzify(int input, double[,] W)
        {
            double a = 0;   // 가중치 * 픽셀 값의 합
            double b = 0; // 가중치의 합

            for (int i = 0; i < pixel; i++)
            {
                double w = W[input, i]; // 입력 픽셀 값에 대한 모든 가능한 출력 픽셀 값의 가중치
                a += w * i;
                b += w;
            }
            // 분모가 0보다 클 경우 계산, 아니면 입력 값 그대로 반환 (가중치가 없을 경우)
            // 픽셀 값은 0-255 범위로 제한
            return b > 0 ? (int)Math.Min(255, Math.Max(0, a / b)) : input;
        }
        private void FAM(Bitmap damage, Bitmap original)
        {
            int w = damage.Width;
            int h = damage.Height;

            // 픽셀 데이터에 직접 접근하기 위한 LockBits
            BitmapData dData = null;
            BitmapData oData = null;
            try
            {
                dData = damage.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                oData = original.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                int s = dData.Stride; // 한 줄(row)의 바이트 수
                byte[] dpixel = new byte[h * s];
                byte[] opixel = new byte[h * s];

                Marshal.Copy(dData.Scan0, dpixel, 0, dpixel.Length);
                Marshal.Copy(oData.Scan0, opixel, 0, opixel.Length);

                // 이미지 전처리: 원본 이미지 픽셀 데이터에 가우시안 블러 적용
                byte[] blurr = Gaussianblur(opixel, w, h, s);

                // 손상된 이미지의 채널별 평균 및 표준편차 계산 (통계)
                List<byte> rvalue = new List<byte>(); // R
                List<byte> gvalue = new List<byte>(); // G
                List<byte> bvalue = new List<byte>(); // B

                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int o = (y * s) + (x * 3); // RGB
                        rvalue.Add(dpixel[o]);
                        gvalue.Add(dpixel[o + 1]);
                        bvalue.Add(dpixel[o + 2]);

                    }
                }

                double sr = 0, sg = 0, sb = 0;
                foreach (byte val in rvalue) sr += val;
                foreach (byte val in gvalue) sg += val;
                foreach (byte val in bvalue) sb += val;

                double mr = sr / rvalue.Count;
                double mg = sg / gvalue.Count;
                double mb = sb / bvalue.Count;

                double vr = 0, vg = 0, vb = 0;
                foreach (byte val in rvalue) vr += (val - mr) * (val - mr);
                foreach (byte val in gvalue) vg += (val - mg) * (val - mg);
                foreach (byte val in bvalue) vb += (val - mb) * (val - mb);

                double stdr = Math.Sqrt(vr / rvalue.Count);
                double stdg = Math.Sqrt(vg / gvalue.Count);
                double stdb = Math.Sqrt(vb / bvalue.Count);

                // FAM 룰 학습 루프
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int o = (y * s) + (x * 3);

                        // RGB 순서로 픽셀 값 가져오기
                        byte d_R = dpixel[o];
                        byte d_G = dpixel[o + 1];
                        byte d_B = dpixel[o + 2];

                        byte o_R = opixel[o];
                        byte o_G = opixel[o + 1];
                        byte o_B = opixel[o + 2];


                        // 손상된 이미지 픽셀과 손상된 이미지 통계 사용
                        //double fuzzr = Fuzzify(d_R, mr, sr);
                        //double fuzzg = Fuzzify(d_G, mg, sg);
                        //double fuzzb = Fuzzify(d_B, mb, sb);

                        // 적응형 퍼지화
                        double localStdR = Adaptive(dpixel, s, x, y, w, h, 0);
                        double localStdG = Adaptive(dpixel, s, x, y, w, h, 1);
                        double localStdB = Adaptive(dpixel, s, x, y, w, h, 2);

                        double fuzzr = Fuzzify(d_R, mr, localStdR);
                        double fuzzg = Fuzzify(d_G, mg, localStdG);
                        double fuzzb = Fuzzify(d_B, mb, localStdB);



                        // FAM 룰 업데이트 (누적 합 방식)
                        wr[d_R, o_R] += fuzzr;  //fuzzr * edgew; 에지 가중치 제거
                        wg[d_G, o_G] += fuzzg; // fuzzg * edgew;
                        wb[d_B, o_B] += fuzzb; // fuzzb * edgew;
                    }
                }
            }
            finally
            {
                if (dData != null) damage.UnlockBits(dData); // 처리 과정 후에 메모리 잠금 해제
                if (oData != null) original.UnlockBits(oData);
            }
        }
        private Bitmap restore(Bitmap damage)
        {
            int w = damage.Width;
            int h = damage.Height;
            Bitmap result = new Bitmap(w, h, PixelFormat.Format24bppRgb);

            BitmapData dData = damage.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData rData = result.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            int s = dData.Stride;
            byte[] dpixel = new byte[h * s];
            byte[] rpixel = new byte[h * s];

            Marshal.Copy(dData.Scan0, dpixel, 0, dpixel.Length);

            // 이미지 복원 루프
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int o = (y * s) + (x * 3);

                    // RGB 순서로 픽셀 값 가져오기
                    byte d_R = dpixel[o];
                    byte d_G = dpixel[o + 1];
                    byte d_B = dpixel[o + 2];

                    // 역퍼지화를 통해 복원된 픽셀 값 계산
                    int r = Defuzzify(d_R, wr);
                    int g = Defuzzify(d_G, wg);
                    int b = Defuzzify(d_B, wb);

                    // 결과 픽셀 배열에 저장
                    rpixel[o] = (byte)r;
                    rpixel[o + 1] = (byte)g;
                    rpixel[o + 2] = (byte)b;
                }
            }

            // 결과 픽셀 배열을 비트맵에 복사
            Marshal.Copy(rpixel, 0, rData.Scan0, rpixel.Length);

            damage.UnlockBits(dData);
            result.UnlockBits(rData);

            return result;
        }
        private double RMSE(Bitmap original, Bitmap restore)
        {
            int w = Math.Min(original.Width, restore.Width);
            int h = Math.Min(original.Height, restore.Height);
            double sumDifference = 0;
            int count = 0;

            BitmapData oData = original.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData rData = restore.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int s = oData.Stride;
            byte[] opixel = new byte[h * s];
            byte[] rpixel = new byte[h * s];

            Marshal.Copy(oData.Scan0, opixel, 0, opixel.Length);
            Marshal.Copy(rData.Scan0, rpixel, 0, rpixel.Length);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int o = (y * s) + (x * 3);

                    // 각 채널별 차이 제곱의 합을 계산
                    sumDifference += Math.Pow(opixel[o] - rpixel[o], 2);         // R
                    sumDifference += Math.Pow(opixel[o + 1] - rpixel[o + 1], 2); // G
                    sumDifference += Math.Pow(opixel[o + 2] - rpixel[o + 2], 2); // B
                    count += 3; // 3채널(R,G,B)
                }
            }
            original.UnlockBits(oData);
            restore.UnlockBits(rData);

            return Math.Sqrt(sumDifference / count);
        }
        private byte[] Gaussianblur(byte[] pixel, int w, int h, int s) 
        {
            // 3x3 가우시안 커널
            double[,] kernel = { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } };
            double ksum = 16.0; // 커널 요소들의 합
            byte[] rpixel = new byte[h * s]; // 반환용

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    double rsum = 0, gsum = 0, bsum = 0;

                    for (int j = -1; j <= 1; j++)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            int X = x + i;
                            int Y = y + j;

                            // 경계 픽셀 처리 : 이미지 범위를 벗어나면 가장 가까운 유효 픽셀 사용
                            if (X < 0) X = 0;
                            else if (X >= w) X = w - 1;
                            if (Y < 0) Y = 0;
                            else if (Y >= h) Y = h - 1;

                            int O = (Y * s) + (X * 3);
                            byte b = pixel[O]; 
                            byte g = pixel[O + 1]; 
                            byte r = pixel[O + 2]; 

                            double W = kernel[j + 1, i + 1];
                            rsum += r * W;
                            gsum += g * W;
                            bsum += b * W;
                        }
                    }

                    // 픽셀 값 클램핑 (0-255)
                    int r1 = Math.Min(255, Math.Max(0, (int)(rsum / ksum)));
                    int g1 = Math.Min(255, Math.Max(0, (int)(gsum / ksum)));
                    int b1 = Math.Min(255, Math.Max(0, (int)(bsum / ksum)));

                    int o = (y * s) + (x * 3);
                    rpixel[o] = (byte)r1;     // R
                    rpixel[o + 1] = (byte)g1; // G
                    rpixel[o + 2] = (byte)b1; // B
                }
            }
            return rpixel;
        }
        private double Adaptive(byte[] pixel, int s, int x, int y, int w, int h, int ch)
        {
            int size = 0;
            double sum = 0;
            double sum1 = 0;

            for (int i = -1; i <= 1; i++) // 3x3
            {
                int a = y + i;
                if (a < 0 || a >= h)
                {
                    continue;
                }

                for (int j = -1; j <= 1; j++)
                {
                    int b = x + j;
                    if (b < 0 || b >= w)
                    {
                        continue;
                    }

                    int o = (a * s) + (b * 3) + ch;
                    byte val = pixel[o];

                    sum += val;
                    sum1 += val * val;
                    size++;
                }
            }

            if (size == 0)
            {
                return std;
            }

            double mean = sum / size;
            double var = (sum1 / size) - (mean * mean);
            return Math.Sqrt(Math.Max(var, std)); // 적응형 퍼지화
        }
    }
}
