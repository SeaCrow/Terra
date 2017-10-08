using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TerraControlPanel
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            checkBoxSetClock.Checked = Settings.Panel.SetTerraClock;
            checkBoxLEDOff.Checked = Settings.Panel.UseOffLED;
            checkBoxDebug.Checked = Settings.Panel.DebugMode;
            checkBoxRefresh.Checked = Settings.Panel.smallRefresh;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            Settings.Panel.SetTerraClock = checkBoxSetClock.Checked;
            Settings.Panel.UseOffLED = checkBoxLEDOff.Checked;
            Settings.Panel.DebugMode = checkBoxDebug.Checked;
            Settings.Panel.smallRefresh = checkBoxRefresh.Checked;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void checkBoxRefresh_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
