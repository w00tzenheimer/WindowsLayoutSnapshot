using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace WindowsLayoutSnapshot {

    public partial class TrayIconForm : Form {

        private Timer m_snapshotTimer = new Timer();
        private Dictionary<long, (List<Snapshot> userSnapshots, List<Snapshot> autoSnapshots)> m_monitorsKeyToSnapshots = new Dictionary<long, (List<Snapshot> userSnapshots, List<Snapshot> autoSnapshots)>();
        private long currentMonitorsKey;
        private Snapshot? m_menuShownSnapshot = null;

        internal static ContextMenuStrip me { get; set; } = null!;

        public TrayIconForm() {
            InitializeComponent();
            this.quitToolStripMenuItem.Image = SystemIcons.Error.ToBitmap();
            Visible = false;

            m_snapshotTimer.Interval = (int)TimeSpan.FromMinutes(30).TotalMilliseconds;
            m_snapshotTimer.Tick += snapshotTimer_Tick;
            m_snapshotTimer.Enabled = true;

            me = trayMenu;

            TakeSnapshot(false);
        }

        private void snapshotTimer_Tick(object? sender, EventArgs e) {
            ExceptionUtils.Protected(() => TakeSnapshot(false));
        }

        private void snapshotToolStripMenuItem_Click(object sender, EventArgs e) {
            ExceptionUtils.Protected(() => TakeSnapshot(true));
        }

        private void clearSnapshotsToolStripMenuItem_Click(object sender, EventArgs e) {
            ExceptionUtils.Protected(() =>
            {
                m_monitorsKeyToSnapshots.Clear();
                TakeSnapshot(userInitiated: false);
            });
        }

        private void justNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_menuShownSnapshot != null)
            {
                ExceptionUtils.Protected(() => m_menuShownSnapshot.Restore(null, EventArgs.Empty));
            }
        }

        private void justNowToolStripMenuItem_MouseEnter(object sender, EventArgs e) {
            SnapshotMousedOverSafe(sender, e);
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void trayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            m_menuShownSnapshot = Snapshot.TakeSnapshot(false);
            justNowToolStripMenuItem.Tag = m_menuShownSnapshot;

            UpdateRestoreChoicesInMenu();

            // the context menu won't show by default on left clicks.  we're going to have to ask it to show up.
            if (e.Button == MouseButtons.Left)
            {
                // I don't see why we need all these hacks when this works fine:
                trayMenu.Show(Cursor.Position);

                /*
                // try using reflection to get to the private ShowContextMenu() function...which really 
                // should be public but is not.
                var showContextMenuMethod = trayIcon.GetType().GetMethod("ShowContextMenu",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (showContextMenuMethod != null)
                {
                    showContextMenuMethod.Invoke(trayIcon, null);
                }
                else
                {
                    // fallback:
                    // The difference is that trayMenu.Show will show it on the right of the cursor instead of on the left.
                    trayMenu.Show(Cursor.Position);
                }
                */
            }
        }

        private void TrayIconForm_VisibleChanged(object sender, EventArgs e)
        {
            // Application.Run(Form) changes this form to be visible.  Change it back.
            Visible = false;
        }

        (List<Snapshot> userSnapshots, List<Snapshot> autoSnapshots) GetOrCreateSnapshots(long monitorsKey)
        {
            if (!m_monitorsKeyToSnapshots.TryGetValue(monitorsKey, out var snapshots))
            {
                snapshots = (new List<Snapshot>(), new List<Snapshot>());
                m_monitorsKeyToSnapshots.Add(monitorsKey, snapshots);
            }
            return snapshots;
        }

        private void TakeSnapshot(bool userInitiated)
        {
            var s = Snapshot.TakeSnapshot(userInitiated);
            long monitorsKey = this.currentMonitorsKey = s.MonitorsKey;
            (List<Snapshot> userSnapshots, List<Snapshot> autoSnapshots) = GetOrCreateSnapshots(monitorsKey);

            var list = s.UserInitiated ? userSnapshots : autoSnapshots;
            int i = FindSimilarSnapshotIndex(list, s, noWindowFlags: true);
            if (i != -1)
            {
                list.RemoveAt(i);
            }

            list.Add(s);

            // Added new element - check if we have too many

            const int MaxSnapshots = 20;
            const int MaxAutoSnapshots = 15;

            if (s.UserInitiated)
            {
                // '-1' to keep space for at least 1 auto snapshot
                if (userSnapshots.Count > MaxSnapshots - 1)
                {
                    userSnapshots.RemoveRange(0, userSnapshots.Count - (MaxSnapshots - 1));
                }
            }

            if (autoSnapshots.Count > MaxAutoSnapshots)
            {
                autoSnapshots.RemoveRange(0, autoSnapshots.Count - MaxAutoSnapshots);
            }

            UpdateRestoreChoicesInMenu();
        }

        private static int FindSimilarSnapshotIndex(List<Snapshot> list, Snapshot s, bool noWindowFlags)
        {
            bool positionOnly = noWindowFlags;
            for (int i = 0; i < list.Count; ++i)
            {
                if (ReferenceEquals(list[i], s))
                {
                    return i;
                }

                if (s.EqualWindows(list[i], positionOnly: noWindowFlags))
                {
                    return i;
                }
            }

            return -1;
        }

        private class RightImageToolStripMenuItem : ToolStripMenuItem {
            public RightImageToolStripMenuItem(string text, float[] monitorSizes)
                : base(text)
            {
                this.MonitorSizes = monitorSizes;
            }

            public float[] MonitorSizes { get; set; }
            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);

                var icon = global::WindowsLayoutSnapshot.Properties.Resources.monitor;
                var maxIconSizeScaling = ((float)(e.ClipRectangle.Height - 8)) / icon.Height;
                var maxIconSize = new Size((int)Math.Floor(icon.Width * maxIconSizeScaling), (int)Math.Floor(icon.Height * maxIconSizeScaling));
                int maxIconY = (int)Math.Round((e.ClipRectangle.Height - maxIconSize.Height) / 2f);

                int nextRight = e.ClipRectangle.Width - 5;
                for (int i = 0; i < MonitorSizes.Length; i++) {
                    var thisIconSize = new Size((int)Math.Ceiling(maxIconSize.Width * MonitorSizes[i]),
                        (int)Math.Ceiling(maxIconSize.Height * MonitorSizes[i]));
                    var thisIconLocation = new Point(nextRight - thisIconSize.Width, 
                        maxIconY + (maxIconSize.Height - thisIconSize.Height));

                    // Draw with transparency
                    var cm = new ColorMatrix();
                    cm.Matrix33 = 0.7f; // opacity
                    using (var ia = new ImageAttributes()) {
                        ia.SetColorMatrix(cm);

                        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        e.Graphics.DrawImage(icon, new Rectangle(thisIconLocation, thisIconSize), 0, 0, icon.Width,
                            icon.Height, GraphicsUnit.Pixel, ia);
                    }

                    nextRight -= thisIconSize.Width + 4;
                }
            }
        }

        private string MakeMenuItemText(Snapshot snapshot, DateTime utcNow)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(snapshot.TimeTaken.ToLocalTime().ToString("MMM dd, h:mm tt"));

            // doesn't work because we don't recreate the items every time we show the menu
            sb.Append($"  ({UXFormat.FormatShortPositiveDuration(snapshot.GetAge(utcNow))} ago)");

            // Skip this because we display monitor icons
            // if (snapshot.NumMonitors > 1)
            // {
            //    sb.Append($" {snapshot.NumMonitors} monitors");
            // }

            sb.Append($"  {snapshot.NumWindows} apps");

            //snapshot.TimeTaken.ToLocalTime().ToString("MMM dd, h:mm tt")
            return sb.ToString();
        }

        private void UpdateRestoreChoicesInMenu() {
            // construct the new list of menu items, then populate them
            // this function is idempotent

            DateTime utcNow = DateTime.UtcNow;

            var snapshotsOldestFirst = CondenseSnapshots(20, out int nCurrentMonitorElements);
            var newMenuItems = new List<ToolStripItem>();

            newMenuItems.Add(quitToolStripMenuItem);
            newMenuItems.Add(snapshotListEndLine);

            var maxNumMonitors = 0;
            var maxNumMonitorPixels = 0L;
            //var showMonitorIcons = false;
            var showMonitorIcons = true; // let always show the monitor icons
            foreach (var snapshot in snapshotsOldestFirst) {
                if (maxNumMonitors != snapshot.NumMonitors && maxNumMonitors != 0) {
                    showMonitorIcons = true;
                }

                maxNumMonitors = Math.Max(maxNumMonitors, snapshot.NumMonitors);
                foreach (var monitorPixels in snapshot.MonitorPixelCounts) {
                    maxNumMonitorPixels = Math.Max(maxNumMonitorPixels, monitorPixels);
                }
            }

            for (int i = 0; i < snapshotsOldestFirst.Count; i++) {
                Snapshot snapshot = snapshotsOldestFirst[i];

                if (i == nCurrentMonitorElements)
                {
                    newMenuItems.Add(snapshotListStartLine2);
                }

                float[] monitorSizes = showMonitorIcons ? GetMonitorSizes(snapshot, maxNumMonitorPixels) : new float[0];

                var menuItem = new RightImageToolStripMenuItem(MakeMenuItemText(snapshot, utcNow), monitorSizes);
                menuItem.Tag = snapshot;
                menuItem.Click += snapshot.RestoreSafe;
                menuItem.MouseEnter += SnapshotMousedOverSafe;
                if (snapshot.UserInitiated) {
                    menuItem.Font = new Font(menuItem.Font, FontStyle.Bold);
                }
                menuItem.MonitorSizes = monitorSizes;

                newMenuItems.Add(menuItem);
            }

            newMenuItems.Add(justNowToolStripMenuItem);
            newMenuItems.Add(snapshotListStartLine);
            newMenuItems.Add(clearSnapshotsToolStripMenuItem);
            newMenuItems.Add(snapshotToolStripMenuItem);

            if (showMonitorIcons)
            {
                int maxTextLen = newMenuItems.Max(x => x.Text != null ? x.Text.Trim().Length : 0);
                int addPad = 4 + 4 * maxNumMonitors; // delimiter space + space for each icon
                int targetWidth = maxTextLen + addPad;
                foreach (var menuItem in newMenuItems)
                {
                    if (menuItem.Text?.Length < targetWidth)
                    {
                        menuItem.Text = menuItem.Text.PadRight(targetWidth);
                    }
                }
            }

            trayMenu.Items.Clear();
            trayMenu.Items.AddRange(newMenuItems.ToArray());

            static float[] GetMonitorSizes(Snapshot snapshot, long maxNumMonitorPixels)
            {
                var monitorSizes = new List<float>();
                foreach (var monitorPixels in snapshot.MonitorPixelCounts)
                {
                    monitorSizes.Add((float)Math.Sqrt(((float)monitorPixels) / maxNumMonitorPixels));
                }
                return monitorSizes.ToArray();
            }
        }
        // Always returns new list
        private List<Snapshot> CondenseSnapshots(int maxNumSnapshots, out int nCurrentMonitorElements) {
            if (maxNumSnapshots < 2) {
                throw new Exception($"Internal error in CondenseSnapshots: maxNumSnapshots({maxNumSnapshots}) < 2.");
            }

            var snapshots = new List<Snapshot>();
            
            (List<Snapshot> userSnapshots, List<Snapshot> autoSnapshots) = GetOrCreateSnapshots(this.currentMonitorsKey);
            snapshots.AddRange(userSnapshots);
            snapshots.AddRange(autoSnapshots);
            nCurrentMonitorElements = snapshots.Count;
            snapshots.Sort((x, y) => x.TimeTaken.CompareTo(y.TimeTaken)); // latest snapshot at the end of the list

            var snapshotsOtherMonitors = new List<Snapshot>();
            foreach (var keyAndSnapshots in this.m_monitorsKeyToSnapshots) {
                if (keyAndSnapshots.Key == this.currentMonitorsKey)
                {
                    continue;
                }

                (List<Snapshot> user, List<Snapshot> other) = keyAndSnapshots.Value;
                snapshotsOtherMonitors.AddRange(user.Take(3));
                snapshotsOtherMonitors.AddRange(other.Take(3));
            }
            snapshotsOtherMonitors.Sort((x, y) => x.TimeTaken.CompareTo(y.TimeTaken)); // latest snapshot at the end of the list

            snapshots.AddRange(snapshotsOtherMonitors);
            return snapshots;
        }

        private void SnapshotMousedOverSafe(object? sender, EventArgs e)
        {
            ExceptionUtils.Protected(() => SnapshotMousedOver(sender, e));
        }

        private void SnapshotMousedOver(object? sender, EventArgs e) {
            ToolStripMenuItem? toolStripMenuItem = sender as ToolStripMenuItem;
            if (toolStripMenuItem == null)
                return;
            
            var tag = (Snapshot?)toolStripMenuItem.Tag;
            if (tag == null)
                return;

            // We save and restore the current foreground window because it's our tray menu
            // I couldn't find a way to get this handle straight from the tray menu's properties;
            //   the ContextMenuStrip.Handle isn't the right one, so I'm using win32
            // More info RE the restore is below, where we do it
            var currentForegroundWindow = GetForegroundWindow();

            try {
                tag.Restore(sender, e);
            } finally {
                // A combination of SetForegroundWindow + SetWindowPos (via set_Visible) seems to be needed
                // This was determined by trying a bunch of stuff
                // This prevents the tray menu from closing, and makes sure it's still on top
                SetForegroundWindow(currentForegroundWindow);
                trayMenu.Visible = true;
            }
        }


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

    }
}