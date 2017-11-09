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
    /// <summary>
    /// This classes manages the summary tab page.  The summary page is created when
    /// ever there is more than one counter page (i.e.- user specified more than one computer).
    /// The summary page works by reading the collection of datasets from the counter pages and 
    /// adding their values together.  It also provides some additional context menu items that
    /// allow for logically resetting the summed counter values to zero.
    ///  
    /// 
    /// TODO:   Clear function will show negative values if an app domain recycles and sets its
    ///         values to zero.  Should check for this and set the lastClearedTable to null.
    /// </summary>
    public partial class SummaryPage : PageBase
    {
        private List<IMonitor> monitors;
        private List<DataTable> monitorTableMementos;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem clearToolStripMenuItem;
        private ToolStripMenuItem undoClearToolStripMenuItem;
        private long totalUpTime;
        private float averageCpu;

        // synchronize updates to grid since both the user and the worker are
        // sharing access to the underlying table data.
        private object syncTableUpdates = new object();

        public SummaryPage(List<IMonitor> monitors)
            : base()
        {
            // list of monitors from the other tab pages.
            if (monitors == null || monitors.Count < 1)
            {
                throw new ArgumentException("Monitor list is null or empty.", "monitors");
            }

            this.monitors = monitors;
            this.dataSet = new DataSet("summary");
            // initialize this dataset's table with the first one in the monitors collection.
            this.dataSet.Tables.Add(monitors[0].GetDataSet().Tables[0].Copy());
            // bind the dataset's table to the grid.
            this.BindingDataSource = dataSet.Tables[0];
            this.dataGrid.Name = "Summary";

            /*
             * 
             * NOTE: disabled the clear/undo clear because of potential
             * negative values occuring for certain counters that can decrement in value,
             * i.e. - Msec, Executing, PerSec, PerMin, etc...
             * 
             * 

            //  Add two new menu items for clearing and unclearing the summary values
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(110, 6);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem = new ToolStripMenuItem();
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.clearToolStripMenuItem.Text = "Clear";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
            // 
            // undoClearToolStripMenuItem
            // 
            this.undoClearToolStripMenuItem = new ToolStripMenuItem();
            this.undoClearToolStripMenuItem.Name = "undoClearToolStripMenuItem";
            this.undoClearToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.undoClearToolStripMenuItem.Text = "Undo Clear";
            //           this.undoClearToolStripMenuItem.Enabled = false;
            this.undoClearToolStripMenuItem.Click += new System.EventHandler(this.undoClearToolStripMenuItem_Click);
            //
            // add the new menu items to the base context menu
            //
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                new ToolStripSeparator(),
                this.clearToolStripMenuItem,
                this.undoClearToolStripMenuItem}
                );

           
           */
        }


        /// <summary>
        /// This is the background thread proc.  All non-ui updates are done here.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void DoBackgroundWork()
        {
            lock (syncTableUpdates)
            {
                UpdateSummaryTable();
            }
        }


        /// <summary>
        /// Handle UI updates.
        /// </summary>
        protected override void DoForegroundWork()
        {
            totalUpTime = 0;
            averageCpu = 0;

            foreach (IMonitor monitor in monitors)
            {
                totalUpTime += monitor.Uptime;
                averageCpu += monitor.Cpu;
            }

            try
            {

                TimeSpan elapsed = new TimeSpan(0, 0, Convert.ToInt32(totalUpTime));
                tbUptime.Text = string.Format("{0:D3}:{1:D2}:{2:D2}:{3:D2}", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);

                averageCpu /= monitors.Count;
                tbCPU.Text = string.Format("{0}%", averageCpu.ToString("###.#"));
                progressBar.Value = Convert.ToInt32(averageCpu);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Update the summary dataTable from each monitor dataTable.
        /// </summary>
        private void UpdateSummaryTable()
        {
            DataTable thisTable = dataSet.Tables[0];

            lock (thisTable)  // prevent foreground access until updates are done.
            {
                // for each column
                for (int x = 0; x < thisTable.Columns.Count; x++)
                {
                    if (!thisTable.Columns[x].ExtendedProperties.Contains("SummaryActionIgnore"))
                    {
                        // for each row
                        for (int y = 0; y < thisTable.Rows.Count; y++)
                        {
                            double sumVal = 0;
                            int newValCnt = 0;

                            // for each table
                            for (int z = 0; z < monitors.Count; z++)
                            {
                                double newVal = Convert.ToDouble(monitors[z].GetDataSet().Tables[0].Rows[y][x]);
                                if (monitorTableMementos != null)
                                {
                                    newVal -= Convert.ToDouble(monitorTableMementos[z].Rows[y][x]);
                                }
                                if (newVal > 0)
                                {
                                    newValCnt++;
                                    sumVal += newVal;
                                }
                            }

                            if (thisTable.Columns[x].ExtendedProperties.Contains("SummaryActionAverage") && newValCnt > 1)
                            {
                                thisTable.Rows[y][x] = sumVal / newValCnt;
                            }
                            else
                            {
                                thisTable.Rows[y][x] = sumVal;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Event handler for clearing the grid values.  The current values are saved to a table 
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (syncTableUpdates)
            {
                // if already cleared before...
                if (monitorTableMementos != null)
                {
                    // get the table back to an "undo clear" state before re-clearing.
                    monitorTableMementos.Clear();
                    monitorTableMementos = null;
                    UpdateSummaryTable();
                }
                monitorTableMementos = new List<DataTable>();
                for (int z = 0; z < monitors.Count; z++)
                {
                    monitorTableMementos.Add(monitors[z].GetDataSet().Tables[0].Copy());
                }
            }
        }
        /// <summary>
        /// Event handle to undo the clear event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void undoClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lock (syncTableUpdates)
            {
                monitorTableMementos.Clear();
                monitorTableMementos = null;
            }
        }

        protected override void dataGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            /*
             * Occasionally an error occurs that contains the following exception:
             * 
             *      VersionNotFoundException: There is no Original data to access.
             * 
             * My workaround is to throw the errors away here.  This doesn't adversly
             * affect the summary page values.
             * 
             * I did see this issue addressed in .net 1.1 (KB839889) however people are
             * still reporting this error as of 9/2006 against .net 2.0.
             */
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
          this.label3.Size = new System.Drawing.Size(158, 13);
          this.label3.Text = "Process Uptime (ddd:hh:mm:ss):";
          // 
          // SummaryPage
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
          this.Name = "SummaryPage";
          this.Controls.SetChildIndex(this.gridPanel, 0);
          this.gridPanel.ResumeLayout(false);
          this.statusPanel.ResumeLayout(false);
          this.statusPanel.PerformLayout();
          ((System.ComponentModel.ISupportInitialize)(this.nudRefreshSecs)).EndInit();
          this.ResumeLayout(false);

       }
    }
}

