using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Bgoon;
using Bgoon.Core;

namespace DeltaEditor.XML.Components {
	public partial class Slider : UserControl {
		private static MainWindow MainWindow => MainWindow.Instance;
		private static LoopCore Core => MainWindow.core;

		public bool IsActive {
			get; private set;
		}
		public float Value {
			get {
				return value;
			}
			set {
				this.value = value;
				UpdateUI();
			}
		}

		private float value;
		public event SingleDelegate<float> OnValueChanged;


		public Slider() {
			InitializeComponent();

			SetActive(true);
			SetEvent();
		}
		private void SetEvent() {
			SliderContext.MouseLeftButtonDown += OnMouseDown;
		}

		public void SetActive(bool active) {
			IsActive = active;
			SliderContext.Opacity = active ? 1d : 0.3d;
		}
		public void SetValue(float value) {
			SetValueNoEvent(value);

			OnValueChanged?.Invoke(Value);
		}
		public void SetValueNoEvent(float value) {
			Value = Mathf.Clamp01(value);
			UpdateUI();
		}

		private void OnMouseDown(object sender, MouseButtonEventArgs e) {
			if (IsActive) {
				Core.AddTask(OnSliderMove, TaskPriolity.EveryFrame, TaskEvent.MouseUpRemove);
			}
		}
		private void OnSliderMove() {
			float value = MouseInput.GetRelativePosition(BackLine).x / (float)BackLine.ActualWidth;
			SetValue(value);
		}

		private void UpdateUI() {
			double pos = Value * BackLine.ActualWidth;
			Btn.RenderTransform = new TranslateTransform(pos, 0d);
			ForeLine.Width = pos;
		}

	}
}