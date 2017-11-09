// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using QuickCounters;

namespace QuickCounterView
{
    public partial class CounterPage : PageBase
    {

       public CounterPage(DataSet dataSet, CalculatedCounter cpuCounter, NonCalculatedCounter uptimeCounter, string computer)
            : base()
        {
            this.dataSet = dataSet;
            this.cpuCounter = cpuCounter;
            this.uptimeCounter = uptimeCounter;
            this.BindingDataSource = dataSet.Tables[0];
            this.dataGrid.Name = computer;
        }

        protected override void DoBackgroundWork()
        {
            InstrumentedApplication.UpdateCounterDataSet(dataSet);
        }

        protected override void DoForegroundWork()
        {
            try
            {
                TimeSpan elapsed = new TimeSpan(0, 0, Convert.ToInt32(uptimeCounter.RawValue));
                tbUptime.Text = string.Format("{0:D3}:{1:D2}:{2:D2}:{3:D2}", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
            }
            catch
            {
                tbUptime.Text = "N/A";
            }

            try
            {
                float cpu = cpuCounter.NextValue;
                tbCPU.Text = string.Format("{0}%", cpu.ToString("###.#"));
                progressBar.Value = Convert.ToInt32(cpu);
            }
            catch
            {
                tbCPU.Text = "N/A";
            }
        }

       private void InitializeComponent()
       {
          this.gridPanel.SuspendLayout();
          this.statusPanel.SuspendLayout();
          ((System.ComponentModel.ISupportInitialize)(this.nudRefreshSecs)).BeginInit();
          this.SuspendLayout();
          // 
          // label3
          // 
          this.label3.Size = new System.Drawing.Size(165, 13);
          this.label3.Text = "Process Uptime (ddd:hh:mm:sec):";
          // 
          // label2
          // 
          this.label2.Location = new System.Drawing.Point(353, 6);
          // 
          // progressBar
          // 
          this.progressBar.Location = new System.Drawing.Point(413, 7);
          this.progressBar.Size = new System.Drawing.Size(218, 17);
          // 
          // tbUptime
          // 
          this.tbUptime.Location = new System.Drawing.Point(175, 8);
          // 
          // tbCPU
          // 
          this.tbCPU.Location = new System.Drawing.Point(369, 6);
          // 
          // nudRefreshSecs
          // 
          this.nudRefreshSecs.Location = new System.Drawing.Point(306, 4);
          // 
          // label1
          // 
          this.label1.Location = new System.Drawing.Point(227, 8);
          // 
          // CounterPage
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
          this.Name = "CounterPage";
          this.Controls.SetChildIndex(this.gridPanel, 0);
          this.gridPanel.ResumeLayout(false);
          this.statusPanel.ResumeLayout(false);
          this.statusPanel.PerformLayout();
          ((System.ComponentModel.ISupportInitialize)(this.nudRefreshSecs)).EndInit();
          this.ResumeLayout(false);

       }
    }
}
