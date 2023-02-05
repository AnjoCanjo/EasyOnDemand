using LibVLCSharp.Shared;
using System.Diagnostics;

namespace EasyOnDemand
{
	public partial class Form2 : Form
	{
		private readonly LibVLC _libVLC = new LibVLC();
		Form1 Main = new Form1();
		Point cursorPosition = new Point();
		private int initialSize;
		int ticked = 0;
		bool operated = false;
		public Form2()
		{
			InitializeComponent();
		}
		public Form2(Form1 main, MediaPlayer mp)
		{
			InitializeComponent();
			Main = main;
			videoView1.MediaPlayer = mp;
			videoView1.MediaPlayer.Play();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			if (videoView1.MediaPlayer!.Rate > 3)
			{
				videoView1.MediaPlayer.SetRate(videoView1.MediaPlayer.Rate - 0.25f);
			}
		}

		private void Form2_FormClosing(object sender, FormClosingEventArgs e)
		{
			Main.Show();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			Debug.WriteLine(videoView1.MediaPlayer!.State.ToString());
			if (videoView1.MediaPlayer!.State.Equals(VLCState.Playing))
			{
				button2.BackgroundImage = (Image)EasyOnDemand.Properties.Resources.pause;
				videoView1.MediaPlayer.Pause();
			}
			else
			{
				button2.BackgroundImage = (Image)EasyOnDemand.Properties.Resources.play;
				videoView1.MediaPlayer.Play();
			}
		}

		private void button3_Click(object sender, EventArgs e)
		{
			videoView1.MediaPlayer!.Stop();
			Main.Show();
			this.Close();
		}

		private void button4_Click(object sender, EventArgs e)
		{
			Debug.WriteLine(videoView1.MediaPlayer!.Rate);
			if (videoView1.MediaPlayer!.Rate < 3)
			{
				videoView1.MediaPlayer.SetRate(videoView1.MediaPlayer.Rate + 0.25f);
			}
		}

		private void button5_Click(object sender, EventArgs e)
		{
			if (this.WindowState == FormWindowState.Maximized)
			{
				this.FormBorderStyle = FormBorderStyle.Fixed3D;
				this.WindowState = FormWindowState.Normal;
			}
			else
			{
				this.FormBorderStyle = FormBorderStyle.None;
				this.WindowState = FormWindowState.Maximized;
			}
		}
		private void HideControl()
		{
			while (panel1.Size.Height > 0)
			{
				panel1.Size = new System.Drawing.Size(panel1.Size.Width, panel1.Size.Height - 1);
			}
			timer1.Start();
		}
		private void ShowControl()
		{
			while (panel1.Size.Height < initialSize)
			{
				panel1.Size = new System.Drawing.Size(panel1.Size.Width, panel1.Size.Height + 1);
			}
			timer1.Start();
		}

		private void Form2_Load(object sender, EventArgs e)
		{
			initialSize = panel1.Size.Height;
			timer1.Interval = 1;
			timer1.Start();
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			//Debug.WriteLine("ticket;" + ticked);
			//Debug.WriteLine("loc1;" + panel1.Location.Y);
			//Debug.WriteLine("init;" + initialY);
			//Debug.WriteLine("fin;" + finalY);
			//Debug.WriteLine("lastPos;" + cursorPosition);
			//Debug.WriteLine("mousePos;" + Cursor.Position);
			if (cursorPosition == Cursor.Position)
			{
				ticked++;
			}
			else
			{
				ticked = 0;
			}
			if (ticked > 100 && panel1.Size.Height == initialSize && !operated)
			{
				operated = true;
				timer1.Stop();
				HideControl();
			}
			if (ticked == 0 && panel1.Size.Height == 0)
			{
				operated = false;
				timer1.Stop();
				ShowControl();
			}
			cursorPosition = Cursor.Position;
		}
	}
}
