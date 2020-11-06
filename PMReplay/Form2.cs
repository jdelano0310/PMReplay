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
            //StackChart.Series.Clear();

            StackChart.Titles.Add("Stack History");
            StackChart.ChartAreas[0].AxisX.Title = "Hand Number";
            StackChart.ChartAreas[0].AxisY.Title = "Dollars";

            int handNumber = 0;
            double handStackTotal = 0;
            double handAddOn = 0;
            foreach (double handstack in Global.playerStack)
            {
                handNumber += 1;
                handStackTotal = handstack;
                handAddOn = Global.playerAddon[handNumber];
                if (handAddOn != 0)
                {
                    // back out the amount the player add-on from their total so it can be
                    // delineated in the bar chart
                    handStackTotal -= handAddOn;
                }
                StackChart.Series["StackTotal"].Points.AddXY(handNumber, handStackTotal);
                StackChart.Series["AddOn"].Points.AddXY(handNumber, handAddOn);
            }

        }
    }
}
