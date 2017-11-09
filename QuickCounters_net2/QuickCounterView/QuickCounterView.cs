// Scott Colestock / www.traceofthought.net
// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters

using System;
using System.Configuration.Install;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Threading;
using Genghis;
using QuickCounters;
using System.Collections.Generic;

namespace QuickCounterView
{
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class QuickCounterView : System.Windows.Forms.Form
    {
        private const string formXpos = "formXpos";
        private const string formYpos = "formYpos";
        private const string formHgt = "formHgt";
        private const string formWdt = "formWdt";
        private const string lastComputerSelectedIndex = "lastComputerSelectedIndex";
        private const string lastFileSelectedIndex = "lastFileSelectedIndex";

        private System.Windows.Forms.MainMenu mainMenu;
        private System.Windows.Forms.MenuItem menuItemFile;
        private System.Windows.Forms.MenuItem menuItemOpen;
        private System.Windows.Forms.MenuItem menuItemExit;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnFile;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnGo;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.ComboBox comboBoxFile;
        private System.Windows.Forms.ComboBox comboBoxComputer;
        private System.Windows.Forms.MenuItem menuItemGo;
        private System.Windows.Forms.MenuItem menuItemHelp;
        private TabControl tabControl;
        private System.Windows.Forms.MenuItem menuItemAbout;
        private List<IMonitor> monitors = new List<IMonitor>();
        private MenuItem menuItemInstall;
        private MenuItem menuItemUninstall;
        private SummaryPage summaryPage = null;

        public QuickCounterView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
           this.components = new System.ComponentModel.Container();
           System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QuickCounterView));
           this.mainMenu = new System.Windows.Forms.MainMenu(this.components);
           this.menuItemFile = new System.Windows.Forms.MenuItem();
           this.menuItemOpen = new System.Windows.Forms.MenuItem();
           this.menuItemInstall = new System.Windows.Forms.MenuItem();
           this.menuItemUninstall = new System.Windows.Forms.MenuItem();
           this.menuItemExit = new System.Windows.Forms.MenuItem();
           this.menuItemGo = new System.Windows.Forms.MenuItem();
           this.menuItemHelp = new System.Windows.Forms.MenuItem();
           this.menuItemAbout = new System.Windows.Forms.MenuItem();
           this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
           this.label1 = new System.Windows.Forms.Label();
           this.btnFile = new System.Windows.Forms.Button();
           this.label2 = new System.Windows.Forms.Label();
           this.btnGo = new System.Windows.Forms.Button();
           this.comboBoxFile = new System.Windows.Forms.ComboBox();
           this.comboBoxComputer = new System.Windows.Forms.ComboBox();
           this.tabControl = new System.Windows.Forms.TabControl();
           this.SuspendLayout();
           // 
           // mainMenu
           // 
           this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemFile,
            this.menuItemGo,
            this.menuItemHelp});
           // 
           // menuItemFile
           // 
           this.menuItemFile.Index = 0;
           this.menuItemFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemOpen,
            this.menuItemInstall,
            this.menuItemUninstall,
            this.menuItemExit});
           this.menuItemFile.Text = "&File";
           // 
           // menuItemOpen
           // 
           this.menuItemOpen.Index = 0;
           this.menuItemOpen.Text = "&Open";
           this.menuItemOpen.Click += new System.EventHandler(this.menuItemOpen_Click);
           // 
           // menuItemInstall
           // 
           this.menuItemInstall.Index = 1;
           this.menuItemInstall.Text = "&Install";
           this.menuItemInstall.Click += new System.EventHandler(this.menuItemInstall_Click);
           // 
           // menuItemUninstall
           // 
           this.menuItemUninstall.Index = 2;
           this.menuItemUninstall.Text = "&Uninstall";
           this.menuItemUninstall.Click += new System.EventHandler(this.menuItemUninstall_Click);
           // 
           // menuItemExit
           // 
           this.menuItemExit.Index = 3;
           this.menuItemExit.Text = "E&xit";
           this.menuItemExit.Click += new System.EventHandler(this.menuItemExit_Click);
           // 
           // menuItemGo
           // 
           this.menuItemGo.Index = 1;
           this.menuItemGo.Text = "&Go";
           this.menuItemGo.Click += new System.EventHandler(this.btnGo_Click);
           // 
           // menuItemHelp
           // 
           this.menuItemHelp.Index = 2;
           this.menuItemHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItemAbout});
           this.menuItemHelp.Text = "&Help";
           // 
           // menuItemAbout
           // 
           this.menuItemAbout.Index = 0;
           this.menuItemAbout.Text = "&About";
           this.menuItemAbout.Click += new System.EventHandler(this.menuItemAbout_Click);
           // 
           // label1
           // 
           this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
           this.label1.Location = new System.Drawing.Point(13, 12);
           this.label1.Name = "label1";
           this.label1.Size = new System.Drawing.Size(114, 23);
           this.label1.TabIndex = 1;
           this.label1.Text = "QuickCounters File:";
           // 
           // btnFile
           // 
           this.btnFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
           this.btnFile.FlatStyle = System.Windows.Forms.FlatStyle.System;
           this.btnFile.Location = new System.Drawing.Point(597, 8);
           this.btnFile.Name = "btnFile";
           this.btnFile.Size = new System.Drawing.Size(25, 21);
           this.btnFile.TabIndex = 3;
           this.btnFile.Text = "...";
           this.btnFile.Click += new System.EventHandler(this.menuItemOpen_Click);
           // 
           // label2
           // 
           this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
           this.label2.Location = new System.Drawing.Point(12, 40);
           this.label2.Name = "label2";
           this.label2.Size = new System.Drawing.Size(114, 23);
           this.label2.TabIndex = 1;
           this.label2.Text = "Computer Name(s):";
           // 
           // btnGo
           // 
           this.btnGo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
           this.btnGo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
           this.btnGo.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
           this.btnGo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
           this.btnGo.ForeColor = System.Drawing.Color.White;
           this.btnGo.Location = new System.Drawing.Point(571, 37);
           this.btnGo.Name = "btnGo";
           this.btnGo.Size = new System.Drawing.Size(51, 23);
           this.btnGo.TabIndex = 4;
           this.btnGo.Text = "&Go";
           this.btnGo.UseVisualStyleBackColor = false;
           this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
           // 
           // comboBoxFile
           // 
           this.comboBoxFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                       | System.Windows.Forms.AnchorStyles.Right)));
           this.comboBoxFile.Location = new System.Drawing.Point(133, 9);
           this.comboBoxFile.Name = "comboBoxFile";
           this.comboBoxFile.Size = new System.Drawing.Size(460, 21);
           this.comboBoxFile.TabIndex = 9;
           // 
           // comboBoxComputer
           // 
           this.comboBoxComputer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                       | System.Windows.Forms.AnchorStyles.Right)));
           this.comboBoxComputer.Location = new System.Drawing.Point(133, 39);
           this.comboBoxComputer.Name = "comboBoxComputer";
           this.comboBoxComputer.Size = new System.Drawing.Size(434, 21);
           this.comboBoxComputer.TabIndex = 10;
           // 
           // tabControl
           // 
           this.tabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                       | System.Windows.Forms.AnchorStyles.Left)
                       | System.Windows.Forms.AnchorStyles.Right)));
           this.tabControl.Location = new System.Drawing.Point(15, 66);
           this.tabControl.Name = "tabControl";
           this.tabControl.SelectedIndex = 0;
           this.tabControl.Size = new System.Drawing.Size(606, 379);
           this.tabControl.TabIndex = 11;
           // 
           // QuickCounterView
           // 
           this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
           this.ClientSize = new System.Drawing.Size(633, 457);
           this.Controls.Add(this.tabControl);
           this.Controls.Add(this.comboBoxComputer);
           this.Controls.Add(this.comboBoxFile);
           this.Controls.Add(this.btnGo);
           this.Controls.Add(this.btnFile);
           this.Controls.Add(this.label1);
           this.Controls.Add(this.label2);
           this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
           this.Menu = this.mainMenu;
           this.Name = "QuickCounterView";
           this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
           this.Text = "QuickCounter View";
           this.Closing += new System.ComponentModel.CancelEventHandler(this.QuickCounterView_Closing);
           this.Load += new System.EventHandler(this.QuickCounterView_Load);
           this.ResumeLayout(false);
        }
        #endregion

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.Run(new QuickCounterView());
            }
            finally { }
        }

        private void menuItemExit_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }


        private void menuItemOpen_Click(object sender, System.EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.comboBoxFile.Text = openFileDialog.FileName;
                UpdateMru();
            }
        }

       private void menuItemInstall_Click(object sender, EventArgs e)
       {
          if (openFileDialog.ShowDialog(this) == DialogResult.OK)
          {
             string[] args = 
             {
                String.Format("/quickctrconfig={0}", openFileDialog.FileName),
                QuickCountersAssembly
             };

             ManagedInstallerClass.InstallHelper(args);
             MessageBox.Show(this,"Install succeeded.",this.Text);

             this.comboBoxFile.Text = openFileDialog.FileName;
             UpdateMru();
          }
       }


       private void menuItemUninstall_Click(object sender, EventArgs e)
       {
          // Would be great to do this with some other mechanism than installutil.exe at some point.
          if (openFileDialog.ShowDialog(this) == DialogResult.OK)
          {
             string[] args = 
             {
                "/uninstall",
                String.Format("/quickctrconfig={0}", openFileDialog.FileName),
                QuickCountersAssembly
             };

             ManagedInstallerClass.InstallHelper(args);
             MessageBox.Show(this,"Uninstall succeeded.",this.Text);
          }
       }

       private string QuickCountersAssembly
       {
          get
          {
             return Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "quickcounters.net.dll");
          }
       }

        private void UpdateMru()
        {
            if (!this.comboBoxFile.Items.Contains(comboBoxFile.Text))
                comboBoxFile.Items.Add(this.comboBoxFile.Text);

            if (!this.comboBoxComputer.Items.Contains(comboBoxComputer.Text))
                comboBoxComputer.Items.Add(this.comboBoxComputer.Text);
        }

        private void btnGo_Click(object sender, System.EventArgs e)
        {
            UpdateMru();

            Cursor oldCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                if (monitors.Count != 0)
                {
                    foreach (IMonitor monitor in monitors)
                    {
                        monitor.StopMonitoring();
                    }

                    if (summaryPage != null)
                    {
                        summaryPage.StopMonitoring();
                    }

                    monitors.Clear();
                    tabControl.Controls.Clear();
                    summaryPage = null;
                }

                InstrumentedApplication application = InstrumentedApplication.LoadFromFile(comboBoxFile.Text);

                // get a list of computers to monitor
                string[] comps = comboBoxComputer.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                // set up a self-updating tabs for each monitor
                foreach (string comp in comps)
                {
                   CalculatedCounter cpu = new CalculatedCounter("Processor", "% Processor Time", "_Total", comp);

                   NonCalculatedCounter uptime = new NonCalculatedCounter(application.Components[0].Name, QuickCounters.Component.ProcessUptime, null, comp);

                   CounterPage counterPage = new CounterPage(
                        application.GetCounterDataSet(comp), 
                        cpu,
                        uptime,
                        comp);
                    TabPage tabPage = new TabPage(comp);
                    tabPage.Controls.Add(counterPage);
                    counterPage.Dock = DockStyle.Fill;
                    tabControl.Controls.Add(tabPage);
                    monitors.Add(counterPage);
                    counterPage.StartMonitoring();
                }

                if (comps.Length > 1)
                {
                    summaryPage = new SummaryPage(monitors);
                    TabPage tabPage = new TabPage("Summary");
                    tabPage.Controls.Add(summaryPage);
                    summaryPage.Dock = DockStyle.Fill;
                    tabControl.Controls.Add(tabPage);
                    summaryPage.StartMonitoring();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this, "Error occurred: " + ex.Message);
            }
            finally
            {
                Cursor.Current = oldCursor;
            }
        }
        private void QuickCounterView_Load(object sender, System.EventArgs e)
        {
            this.comboBoxComputer.Text = Environment.MachineName;

            Preferences reader = Preferences.GetUserNode("QuickCounterView");
            for (int i = 0; i < 10; i++)
            {
                string computer = reader.GetString("computer" + i.ToString(), null);
                string file = reader.GetString("file" + i.ToString(), null);

                if (computer != null)
                    comboBoxComputer.Items.Add(computer);

                if (file != null)
                    this.comboBoxFile.Items.Add(file);
            
            }
            if (comboBoxComputer.Items.Count > 0)
            {
               comboBoxComputer.SelectedIndex = reader.GetInt32(lastComputerSelectedIndex, 0);
            }
            if (comboBoxFile.Items.Count > 0)
            {
               comboBoxFile.SelectedIndex = reader.GetInt32(lastFileSelectedIndex, 0);
            }
            int xLoc = reader.GetInt32(formXpos, 50);
            int yLoc = reader.GetInt32(formYpos, 50);
            int hgt = reader.GetInt32(formHgt, 200);
            int wdt = reader.GetInt32(formWdt, 500);
            this.SetDesktopBounds(xLoc, yLoc, wdt, hgt);
        }
        private void QuickCounterView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Preferences writer = Preferences.GetUserNode("QuickCounterView");
            for (int i = 0; i < 10; i++)
            {
                if (comboBoxComputer.Items.Count > i)
                    writer.SetProperty("computer" + i.ToString(), this.comboBoxComputer.Items[i]);

                if (comboBoxFile.Items.Count > i)
                    writer.SetProperty("file" + i.ToString(), this.comboBoxFile.Items[i]);
            }

            if (comboBoxComputer.Items.Count > 0)
            {
               writer.SetProperty(lastComputerSelectedIndex, comboBoxComputer.SelectedIndex);
            }
            if (comboBoxFile.Items.Count > 0)
            {
               writer.SetProperty(lastFileSelectedIndex, comboBoxFile.SelectedIndex);
            }
           writer.SetProperty(formXpos, this.DesktopBounds.X);
           writer.SetProperty(formYpos, this.DesktopBounds.Y);
           writer.SetProperty(formHgt, this.DesktopBounds.Height);
           writer.SetProperty(formWdt, this.DesktopBounds.Width);
        }
        private void menuItemAbout_Click(object sender, System.EventArgs e)
        {
            MessageBox.Show(this, "QuickCounterView 1.1 (Scott Colestock / www.traceofthought.net)");
        }
    }
}
