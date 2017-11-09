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
using Genghis;

namespace QuickCounterView
{
    public partial class PageBase : UserControl, IMonitor
    {
        // the backing dataset for the DataGrid
        protected DataSet dataSet;
        // refresh interval millis
        protected int sleepInterval = 30000;
        protected CalculatedCounter cpuCounter;
        protected NonCalculatedCounter uptimeCounter;

        protected virtual void DoBackgroundWork() { }
        protected virtual void DoForegroundWork() { }


        /// <summary>
        /// Constructor.
        /// </summary>
        public PageBase()
        {
            InitializeComponent();
        }

        public long Uptime
        {
            get
            {
                try
                {
                    return uptimeCounter.RawValue;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public float Cpu
        {
            get
            {
                try
                {
                    return (float)cpuCounter.CurrentValue;
                }
                catch
                {
                    return 0;
                }
            }

        }

        /// <summary>
        /// Start monitoring the performance counters. This method will start a background worker thread.
        /// </summary>
        public void StartMonitoring()
        {
            backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Stop monitoring the performance counters.  This method will cancel the background worker thread.
        /// </summary>
        public void StopMonitoring()
        {
            backgroundWorker.CancelAsync();
        }

        /// <summary>
        /// Get the dataset behind the DataGrid for this tab page.
        /// </summary>
        /// <returns>Reference to the dataset.</returns>
        public DataSet GetDataSet()
        {
            return dataSet;
        }



        /// <summary>
        /// BindingSource data source.
        /// </summary>
        protected object BindingDataSource
        {
            set { dataGrid.DataSource = value; }
        }


        /// <summary>
        /// Copy event handler for copying table data to clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataTable dt = (DataTable)dataGrid.DataSource;
            if (dt != null)
            {
                lock (this)
                {
                    Clipboard.SetDataObject(TableToString(dt), true);
                }
            }
        }

        /// <summary>
        /// Updates the refresh sleep interval.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void nudRefreshSecs_ValueChanged(object sender, EventArgs e)
        {
            sleepInterval = Convert.ToInt32(nudRefreshSecs.Value) * 1000;
            Preferences writer = Preferences.GetUserNode("QuickCounterView");
            writer.SetProperty("RefreshIntervalSecs", Convert.ToInt32(nudRefreshSecs.Value));
        }


        /// <summary>
        /// Formats the table data in a form suitable for pasting into an excel spreadsheet.
        /// </summary>
        /// <remarks>From: http://www.codeproject.com/csharp/practicalguidedatagrids4.asp?df=100&forumid=33944&exp=0&select=1209241</remarks>
        /// <param name="dt">The DataTable to copy.</param>
        /// <returns>A string containing the formatted output.</returns>
        private string TableToString(DataTable dt)
        {
            string strData = dt.TableName + "\r\n";
            string sep = string.Empty;
            if (dt.Rows.Count > 0)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    if (c.DataType != typeof(System.Guid) &&
                        c.DataType != typeof(System.Byte[]))
                    {
                        strData += sep + c.ColumnName;
                        sep = "\t";
                    }
                }
                strData += "\r\n";
                foreach (DataRow r in dt.Rows)
                {
                    sep = string.Empty;
                    foreach (DataColumn c in dt.Columns)
                    {
                        if (c.DataType != typeof(System.Guid) &&
                            c.DataType != typeof(System.Byte[]))
                        {
                            if (!Convert.IsDBNull(r[c.ColumnName]))
                                strData += sep +
                                    r[c.ColumnName].ToString();
                            else
                                strData += sep + "";
                            sep = "\t";
                        }
                    }
                    strData += "\r\n";
                }
            }
            else
                strData += "\r\n---> Table was empty!";
            return strData;
        }

        /// <summary>
        /// BackgroundWorker's Progress Changed thread proc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DoForegroundWork();
        }

        /// <summary>
        /// BackgroundWorker's thread proc.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;

            while (true)
            {
                if (worker.CancellationPending)
                {
                    break;
                }
                DoBackgroundWork();
                worker.ReportProgress(0);
                System.Threading.Thread.Sleep(sleepInterval);
            }
        }
        protected virtual void dataGrid_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
           if (e.Exception.GetType() != System.Type.GetType("System.Data.VersionNotFoundException"))
           {
              MessageBox.Show(this, String.Format("Data error occurred in DataGridView {0}. ColumnIndex={1}, RowIndex={2}, Exception={3}", dataGrid.Name, e.ColumnIndex, e.RowIndex, e.Exception));
           }
        }

        private void dataGrid_SelectionChanged(object sender, EventArgs e)
        {
            dataGrid.ClearSelection();
        }
    }
}
