using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PMReplay
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            BuildSplineChart();
        }

        private void BuildSplineChart()
        {
            StackChart.Series.Clear();

            StackChart.Titles.Add("Stack History");
            StackChart.ChartAreas[0].AxisX.Title = "Hand Number";
            StackChart.ChartAreas[0].AxisY.Title = "Dollars";

            Series series = StackChart.Series.Add("Stack Amount");
            series.ChartType = SeriesChartType.Spline;
            
            int handNumber = 0;
            foreach (double handstack in Global.playerStack)
            {
                handNumber += 1;
                series.Points.AddXY(handNumber, handstack);
            }

        }
    }
}
