using System;
using Genghis;


namespace QuickCounterView
{
    partial class PageBase
    {

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
       private void PageBase_Load(object sender, System.EventArgs e)
       {
          Preferences reader = Preferences.GetUserNode("QuickCounterView");

          int refreshIntervalFromPref = reader.GetInt32("RefreshIntervalSecs", 15);
          this.nudRefreshSecs.Value = Convert.ToDecimal(refreshIntervalFromPref);
       }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        protected void InitializeComponent()
        {
           this.components = new System.ComponentModel.Container();
           System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
           this.gridPanel = new System.Windows.Forms.Panel();
           this.dataGrid = new System.Windows.Forms.DataGridView();
           this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
           this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
           this.statusPanel = new System.Windows.Forms.Panel();
           this.label1 = new System.Windows.Forms.Label();
           this.nudRefreshSecs = new System.Windows.Forms.NumericUpDown();
           this.tbCPU = new System.Windows.Forms.TextBox();
           this.tbUptime = new System.Windows.Forms.TextBox();
           this.progressBar = new System.Windows.Forms.ProgressBar();
           this.label2 = new System.Windows.Forms.Label();
           this.label3 = new System.Windows.Forms.Label();
           this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
           this.gridPanel.SuspendLayout();
           ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
           this.contextMenuStrip.SuspendLayout();
           this.statusPanel.SuspendLayout();
           ((System.ComponentModel.ISupportInitialize)(this.nudRefreshSecs)).BeginInit();
           this.SuspendLayout();
           // 
           // gridPanel
           // 
           this.gridPanel.Controls.Add(this.dataGrid);
           this.gridPanel.Controls.Add(this.statusPanel);
           this.gridPanel.Dock = System.Windows.Forms.DockStyle.Fill;
           this.gridPanel.Location = new System.Drawing.Point(0, 0);
           this.gridPanel.Name = "gridPanel";
           this.gridPanel.Size = new System.Drawing.Size(644, 471);
           this.gridPanel.TabIndex = 0;
           // 
           // dataGrid
           // 
           this.dataGrid.AllowUserToAddRows = false;
           this.dataGrid.AllowUserToDeleteRows = false;
           this.dataGrid.AllowUserToOrderColumns = true;
           this.dataGrid.AllowUserToResizeRows = false;
           dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
           this.dataGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
           this.dataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
           this.dataGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
           this.dataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
           this.dataGrid.ContextMenuStrip = this.contextMenuStrip;
           this.dataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
           this.dataGrid.Location = new System.Drawing.Point(0, 0);
           this.dataGrid.MultiSelect = false;
           this.dataGrid.Name = "dataGrid";
           this.dataGrid.ReadOnly = true;
           this.dataGrid.RowHeadersVisible = false;
           this.dataGrid.RowTemplate.ReadOnly = true;
           this.dataGrid.ShowCellToolTips = false;
           this.dataGrid.ShowEditingIcon = false;
           this.dataGrid.ShowRowErrors = false;
           this.dataGrid.Size = new System.Drawing.Size(644, 441);
           this.dataGrid.TabIndex = 3;
           this.dataGrid.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dataGrid_DataError);
           this.dataGrid.SelectionChanged += new System.EventHandler(this.dataGrid_SelectionChanged);
           // 
           // contextMenuStrip
           // 
           this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem});
           this.contextMenuStrip.Name = "contextMenuStrip";
           this.contextMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
           this.contextMenuStrip.ShowImageMargin = false;
           this.contextMenuStrip.Size = new System.Drawing.Size(86, 26);
           // 
           // copyToolStripMenuItem
           // 
           this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
           this.copyToolStripMenuItem.Size = new System.Drawing.Size(85, 22);
           this.copyToolStripMenuItem.Text = "Copy";
           this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
           // 
           // statusPanel
           // 
           this.statusPanel.Controls.Add(this.label1);
           this.statusPanel.Controls.Add(this.nudRefreshSecs);
           this.statusPanel.Controls.Add(this.tbCPU);
           this.statusPanel.Controls.Add(this.tbUptime);
           this.statusPanel.Controls.Add(this.progressBar);
           this.statusPanel.Controls.Add(this.label2);
           this.statusPanel.Controls.Add(this.label3);
           this.statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
           this.statusPanel.Location = new System.Drawing.Point(0, 441);
           this.statusPanel.Name = "statusPanel";
           this.statusPanel.Size = new System.Drawing.Size(644, 30);
           this.statusPanel.TabIndex = 2;
           // 
           // label1
           // 
           this.label1.AutoSize = true;
           this.label1.Location = new System.Drawing.Point(258, 8);
           this.label1.Name = "label1";
           this.label1.Size = new System.Drawing.Size(73, 13);
           this.label1.TabIndex = 6;
           this.label1.Text = "Refresh (sec):";
           // 
           // nudRefreshSecs
           // 
           this.nudRefreshSecs.Location = new System.Drawing.Point(337, 5);
           this.nudRefreshSecs.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
           this.nudRefreshSecs.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
           this.nudRefreshSecs.Name = "nudRefreshSecs";
           this.nudRefreshSecs.Size = new System.Drawing.Size(41, 20);
           this.nudRefreshSecs.TabIndex = 5;
           this.nudRefreshSecs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
           this.nudRefreshSecs.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
           this.nudRefreshSecs.ValueChanged += new System.EventHandler(this.nudRefreshSecs_ValueChanged);
           // 
           // tbCPU
           // 
           this.tbCPU.BackColor = System.Drawing.SystemColors.Control;
           this.tbCPU.BorderStyle = System.Windows.Forms.BorderStyle.None;
           this.tbCPU.Location = new System.Drawing.Point(410, 7);
           this.tbCPU.Name = "tbCPU";
           this.tbCPU.ReadOnly = true;
           this.tbCPU.Size = new System.Drawing.Size(38, 13);
           this.tbCPU.TabIndex = 4;
           this.tbCPU.Text = "100";
           this.tbCPU.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
           // 
           // tbUptime
           // 
           this.tbUptime.BackColor = System.Drawing.SystemColors.Control;
           this.tbUptime.BorderStyle = System.Windows.Forms.BorderStyle.None;
           this.tbUptime.Location = new System.Drawing.Point(168, 7);
           this.tbUptime.Name = "tbUptime";
           this.tbUptime.ReadOnly = true;
           this.tbUptime.Size = new System.Drawing.Size(63, 13);
           this.tbUptime.TabIndex = 3;
           this.tbUptime.Text = "12345";
           this.tbUptime.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
           // 
           // progressBar
           // 
           this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                       | System.Windows.Forms.AnchorStyles.Right)));
           this.progressBar.Location = new System.Drawing.Point(454, 7);
           this.progressBar.Name = "progressBar";
           this.progressBar.Size = new System.Drawing.Size(176, 17);
           this.progressBar.Step = 1;
           this.progressBar.TabIndex = 2;
           // 
           // label2
           // 
           this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                       | System.Windows.Forms.AnchorStyles.Right)));
           this.label2.AutoSize = true;
           this.label2.Location = new System.Drawing.Point(384, 7);
           this.label2.Name = "label2";
           this.label2.Size = new System.Drawing.Size(32, 13);
           this.label2.TabIndex = 1;
           this.label2.Text = "CPU:";
           // 
           // label3
           // 
           this.label3.AutoSize = true;
           this.label3.Location = new System.Drawing.Point(4, 7);
           this.label3.Name = "label3";
           this.label3.Size = new System.Drawing.Size(158, 13);
           this.label3.TabIndex = 0;
           this.label3.Text = "Process Uptime (ddd:hh:mm:ss):";
           // 
           // backgroundWorker
           // 
           this.backgroundWorker.WorkerReportsProgress = true;
           this.backgroundWorker.WorkerSupportsCancellation = true;
           this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
           this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
           // 
           // PageBase
           // 
           this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
           this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
           this.Controls.Add(this.gridPanel);
           this.Name = "PageBase";
           this.Size = new System.Drawing.Size(644, 471);
           this.Load += new System.EventHandler(this.PageBase_Load);
           this.gridPanel.ResumeLayout(false);
           ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
           this.contextMenuStrip.ResumeLayout(false);
           this.statusPanel.ResumeLayout(false);
           this.statusPanel.PerformLayout();
           ((System.ComponentModel.ISupportInitialize)(this.nudRefreshSecs)).EndInit();
           this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.Panel gridPanel;
        protected System.Windows.Forms.Panel statusPanel;
        protected System.Windows.Forms.Label label3;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.ProgressBar progressBar;
        protected System.ComponentModel.BackgroundWorker backgroundWorker;
        protected System.Windows.Forms.TextBox tbUptime;
        protected System.Windows.Forms.TextBox tbCPU;
        protected System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        protected System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        protected System.Windows.Forms.NumericUpDown nudRefreshSecs;
        protected System.Windows.Forms.Label label1;
        private System.ComponentModel.IContainer components;
        protected System.Windows.Forms.DataGridView dataGrid;
    }
}
