// Use subject to: http://www.codeplex.com/Project/License.aspx?ProjectName=quickcounters

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace QuickCounterView
{
    public interface IMonitor
    {
        void StartMonitoring();
        void StopMonitoring();
        DataSet GetDataSet();
        long Uptime { get;}
        float Cpu { get;}
    }
}
