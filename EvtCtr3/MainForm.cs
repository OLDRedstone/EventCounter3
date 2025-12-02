using EvtCtr3.Assets;
using EvtCtr3.Core;
using RhythmBase.RhythmDoctor.Events;
using System.Diagnostics;

namespace EvtCtr3
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
			eventsui1.Location = new Point(0, 0);
			SizeChanged += (e, s) => eventsui1.Size = this.ClientSize;
			OnSizeChanged(new());
		}
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }
	}
}
