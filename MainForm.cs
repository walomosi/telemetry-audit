using System;
using System.Drawing;
using System.Windows.Forms;
using TelemetryAudit.Core;

namespace TelemetryAudit
{
    public class MainForm : Form
    {
        private Button _btnScan;
        private Button _btnDisable;
        private ListView _list;
        private Label _status;

        public MainForm()
        {
            Text = "Telemetry Audit - see what Windows phones home";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            _btnScan = new Button();
            _btnScan.Text = "Scan";
            _btnScan.Width = 100;
            _btnScan.Height = 34;
            _btnScan.Top = 12;
            _btnScan.Left = 12;
            _btnScan.Click += OnScan;

            _btnDisable = new Button();
            _btnDisable.Text = "Disable selected";
            _btnDisable.Width = 140;
            _btnDisable.Height = 34;
            _btnDisable.Top = 12;
            _btnDisable.Left = 120;
            _btnDisable.BackColor = Color.LightSkyBlue;
            _btnDisable.Click += OnDisable;

            _status = new Label();
            _status.Text = "Ready. Click Scan.";
            _status.Top = 20;
            _status.Left = 280;
            _status.Width = 580;
            _status.ForeColor = Color.DimGray;

            _list = new ListView();
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.CheckBoxes = true;
            _list.Top = 56;
            _list.Left = 12;
            _list.Width = 860;
            _list.Height = 490;
            _list.Columns.Add("", 30);
            _list.Columns.Add("Category", 90);
            _list.Columns.Add("Item", 260);
            _list.Columns.Add("Detail", 380);
            _list.Columns.Add("State", 80);

            Controls.Add(_btnScan);
            Controls.Add(_btnDisable);
            Controls.Add(_status);
            Controls.Add(_list);
        }

        private void OnScan(object sender, EventArgs e)
        {
            _list.Items.Clear();
            _status.Text = "Scanning...";
            var items = TelemetryScanner.Scan();
            int on = 0;
            foreach (TelemetryItem it in items)
            {
                ListViewItem lvi = _list.Items.Add("");
                lvi.SubItems.Add(it.Category);
                lvi.SubItems.Add(it.Name);
                lvi.SubItems.Add(it.Detail);
                lvi.SubItems.Add(it.State.ToString());
                if (it.State == ItemState.On)
                {
                    lvi.ForeColor = Color.Firebrick;
                    lvi.Checked = true;
                    on++;
                }
                else
                {
                    lvi.ForeColor = Color.Gray;
                }
                lvi.Tag = it;
            }
            _status.Text = on + " item(s) currently on. Check the ones you want to disable, then click 'Disable selected'.";
        }

        private void OnDisable(object sender, EventArgs e)
        {
            int count = 0;
            foreach (ListViewItem lvi in _list.CheckedItems)
            {
                TelemetryItem it = (TelemetryItem)lvi.Tag;
                if (it == null || it.State != ItemState.On) continue;
                try
                {
                    string[] parts = it.DisableHint.Split(' ');
                    string exe = parts[0];
                    string args = it.DisableHint.Substring(exe.Length).Trim();
                    System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(exe, args);
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardOutput = true;
                    using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi))
                    {
                        p.WaitForExit(8000);
                    }
                    count++;
                    lvi.ForeColor = Color.Gray;
                    lvi.SubItems[4].Text = "off (applied)";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Failed to disable " + it.Name + ":\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            _status.Text = "Applied " + count + " disable action(s). Reboot recommended.";
        }
    }
}
