using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Bgoon;

namespace DeltaEditor.XML.Windows {
	/// <summary>
	/// ItemSelector.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MenuWindow : Window {
		private MainWindow MainWindow => MainWindow.Instance;
		private LoopCore Core => MainWindow.core;

		private const float ItemHeight = 60f;
		private const float FadeSpeed = 0.2f;
		private float alpha = 0f;
		private float scale = 0f;
		private CoreTask entryTask;
		private CoreTask closingTask;


		public MenuWindow(params MenuWindowItem[] menus) {
			InitializeComponent();

			//Menu Generate
			{
				int menuCount = menus.Length;
				Height = menus.Length * ItemHeight;

				if (menus == null || menuCount == 0) {
					throw new Exception("메뉴 항목이 없습니다.");
				} else if (menuCount == 1) {
					SingleBtn.SetBtnColor();
					SingleBtn.SetOnClick(base.Close);
					SingleBtn.SetOnClick(menus[0].OnClick);
					TopText.Content = menus[0].text;

					TopBtn.Visibility =
					BotBtn.Visibility =
					BotText.Visibility = Visibility.Hidden;
				} else {
					for (int i = 0; i < menuCount; ++i) {
						if (i > 0) {
							Border separator = new Border();
							separator.Height = 1f;
							separator.Background = "#FFFFFF5F".ToBrush();
							separator.VerticalAlignment = VerticalAlignment.Top;
							separator.Margin = new Thickness(0f, ItemHeight * i, 0f, 0f);
							BackPanel.Children.Add(separator);
						}

						if (i == 0) {
							TopBtn.SetBtnColor();
							TopBtn.SetOnClick(base.Close);
							TopBtn.SetOnClick(menus[i].OnClick);
							TopText.Content = menus[i].text;
						} else if (i == menuCount - 1) {
							BotBtn.SetBtnColor();
							BotBtn.SetOnClick(base.Close);
							BotBtn.SetOnClick(menus[i].OnClick);
							BotText.Content = menus[i].text;
						} else {
							Border btn = new Border();
							btn.Height = ItemHeight;
							btn.Background = "#00000000".ToBrush();
							BackPanel.Children.Add(btn);

							btn.SetBtnColor();
							btn.SetOnClick(base.Close);
							btn.SetOnClick(menus[i].OnClick);
						}
					}
				}
			}

			Opacity = 0f;
			Left = MouseInput.AbsolutePosition.x - Width * 0.5f;
			Top = MouseInput.AbsolutePosition.y - Height * 0.5f;

			SetEvent();
		}
		private void SetEvent() {
			Closing += OnClosing;
			Deactivated += OnLostFocus;

			entryTask = Core.AddTask(EntryUpdate);
		}
		private new void Close() {
			if (closingTask != null)
				return;

			RemoveEntryTask();

			closingTask = Core.AddTask(ClosingUpdate);
		}
		private void OnLostFocus(object sender, EventArgs e) {
			Close();
		}
		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			RemoveEntryTask();
		}
		private void EntryUpdate() {
			alpha = Mathf.Min(1f, alpha + FadeSpeed);
			float delta = 1f - scale;
			scale += delta * 0.2f;

			this.Opacity = alpha;
			BackPanel.RenderTransform = new ScaleTransform(scale, scale);

			if (alpha >= 1f && delta < 0.02f) {
				RemoveEntryTask();
			}
		}
		private void RemoveEntryTask() {
			if (entryTask != null) {
				Core.RemoveTask(entryTask);
				entryTask = null;
			}
		}
		private void ClosingUpdate() {
			alpha = Mathf.Max(0f, alpha - FadeSpeed);

			this.Opacity = alpha;

			if (alpha <= 0f) {
				Core.RemoveTask(closingTask);
				base.Close();
			}
		}
	}
	public class MenuWindowItem {
		public string text;
		public Action OnClick;

		public MenuWindowItem(string text, Action OnClick) {
			this.text = text;
			this.OnClick = OnClick;
		}
	}
}
