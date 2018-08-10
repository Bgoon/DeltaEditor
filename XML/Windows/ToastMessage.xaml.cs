using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Bgoon;

namespace DeltaEditor.XML.Windows {
	/// <summary>
	/// ToastMessage.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ToastMessage : Window {
		private static MainWindow MainWindow => MainWindow.Instance;
		private static LoopCore Core => MainWindow.core;
		private CoreTask entryMotionTask;
		private CoreTask exitMotionTask;

		private const float FadeSpeed = 0.03f;
		private float DelaySeconds = 1.5f;
		private float alpha;

		private const int WS_EX_TRANSPARENT = 0x00000020;
		private const int GWL_EXSTYLE = (-20);
		private const int WM_NCHITTEST = 0x0084;
		[DllImport("user32.dll")]
		public static extern int GetWindowLong(IntPtr hwnd, int index);
		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

		public ToastMessage() {
			InitializeComponent();
			StartEntryMotion();
		}
		protected override void OnSourceInitialized(EventArgs e) {
			base.OnSourceInitialized(e);

			// Get this window's handle
			IntPtr hwnd = new WindowInteropHelper(this).Handle;

			// Change the extended window style to include WS_EX_TRANSPARENT
			int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
		}
		private void StartEntryMotion() {
			Opacity = 0f;
			entryMotionTask = Core.AddTask(UpdateEntryMotion);
		}
		private void UpdateEntryMotion() {
			alpha = Mathf.Min(1f, alpha + FadeSpeed);
			Opacity = alpha;

			if (alpha >= 1f) {
				StopEntryMotion();
			}
		}
		private async void StopEntryMotion() {
			if (entryMotionTask == null)
				return;

			Core.RemoveTask(entryMotionTask);
			entryMotionTask = null;

			await Task.Delay((int)(DelaySeconds * 1000f));

			StartExitMotion();
		}
		private void StartExitMotion() {
			exitMotionTask = Core.AddTask(UpdateExitMotion);
		}
		private void UpdateExitMotion() {
			alpha = Mathf.Max(0f, alpha - FadeSpeed);
			Opacity = alpha;

			if (alpha <= 0f) {
				StopExitMotion();
				Close();
			}
		}
		private void StopExitMotion() {
			if (exitMotionTask == null)
				return;
			Core.RemoveTask(exitMotionTask);
			exitMotionTask = null;
		}

		public static void Show(string text, float delaySec = 2f) {
			Core.AddJob(() => {
				ToastMessage toast = new ToastMessage();
				toast.DelaySeconds = delaySec;
				toast.MessageText.Content = text;
				toast.Show();
			});
		}
	}
}
