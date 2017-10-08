using System;
using System.IO;
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
    public partial class Form1 : Form
    {

        public Form1()
        {

            InitializeComponent();

            Settings.Read();
            

            CommPort com = CommPort.Instance;
            com.StatusChanged += OnStatusChanged;
            com.DataReceived += OnDataReceived;

            LoadSettingsToControls();
            GetPorts();

            comboBoxCOM.MouseWheel += new MouseEventHandler(comboBoxCOM_MouseWheel); // disabling mousewheel in COM select combobox to prevent accidental COM Port change
        }
        public void OnStatusChanged(string status) // Passing status data from CommPort class
        {
            //Handle multi-threading
            if (InvokeRequired)
            {
                Invoke(new StringDelegate(OnStatusChanged), new object[] { status });
                return;
            }

            textBoxStatus.Text = status;
        }

        internal delegate void StringDelegate(string data);
        private String dataStorage = ""; // String storing unfinished serial lines between DataRecived events

        private string prepareData(string str)  // For debug purposes change all unprintable characters in recived string into their respective names;
        {
            string[] charNames = { "NUL", "SOH", "STX", "ETX", "EOT",
                "ENQ", "ACK", "BEL", "BS", "TAB", "LF", "VT", "FF", "CR", "SO", "SI",
                "DLE", "DC1", "DC2", "DC3", "DC4", "NAK", "SYN", "ETB", "CAN", "EM", "SUB",
                "ESC", "FS", "GS", "RS", "US", "Space"};

            foreach (char c in str)
            {

                if (c < 32 && c != 9)
                {
                    str = str + "<" + charNames[c] + ">";
                }

            }


            return str;
        }
        public void OnDataReceived(string dataIn)
        {
            //Handle multi-threading
            if (InvokeRequired)
            {
                Invoke(new StringDelegate(OnDataReceived), new object[] { dataIn });
                return;
            }

            string stringIn = "";

            if (dataStorage.Length > 0)
            {
                stringIn += dataStorage;
                dataStorage = "";
            }
            char c = (char)10;
            if (dataIn.Length > 0 && dataIn.IndexOf(c) != -1)
            {
                int index = dataIn.IndexOf(c);
                stringIn += dataIn.Substring(0, index);
                dataIn.Remove(0, index);

                if (Settings.Panel.DebugMode && checkBoxRaw.Checked)
                    debugBox.Items.Add(prepareData(stringIn));

                ParseRecivedData(stringIn);

                stringIn = "";
            }
            if (dataIn.Length > 0)
                dataStorage = dataStorage + stringIn + dataIn;


        }
        private int nrfErrorCount;
        private bool expectedDownload = false;
        private int expectedDownloadCount;
        private int downloadCount;
        public void ParseRecivedData(string recivedStr)
        {
            string outStr = "";
            foreach (char c in recivedStr)   // removing unprintable characters from string
            {
                if (c > 32)
                {
                    outStr += c;
                }
                if (c == 'F')
                    nrfErrorCount++;
            }

            if (Settings.Panel.DebugMode && checkBoxCleared.Checked)
                debugBox.Items.Add(prepareData(outStr));

            if (expectedDownload)                             // if download was requested and there will be stream od data coming in
            {
                if (!checkBoxRefresh.Checked)
                    textBoxStatus.Text = String.Format("Downloaded {0} out of {1}", downloadCount + 1, expectedDownloadCount); // mark progress on status text box

                if (outStr.Length > 4) // if recived data is data string insted of radio status ( DS - radio sent succesfully, FS - radio failed to sent)
                {
                        

                }
                if (outStr.Length == 13)                // pass data string to be handled
                    HandleRecivedSetting(outStr);

                if (outStr.Length == 26)        // remote module sometimes stick 2 data strings into one
                {
                    HandleRecivedSetting(outStr.Substring(0, 13));   // separating first string and passing it on
                    HandleRecivedSetting(outStr.Substring(13));     // separating second string and passing it on
                    downloadCount++;                                // second string was not counted, count it now
                }

                if (outStr.Length > 13 && outStr.Length < 26)        // rare occurence when remote module sticks few char from end of last data string into begining of next one in line
                    HandleRecivedSetting(outStr.Substring(outStr.Length - 13));     // extract string and pass it to be handled

                if (outStr.Length > 26 && outStr.Length < 33)       // if both rare cases happen, some extra char at start and 2 data strings sticked together
                {
                    outStr = outStr.Substring(outStr.Length - 26);  // remove additional characters from start of string
                    HandleRecivedSetting(outStr.Substring(1, 14));  // extract first string and pass it to be handled
                    HandleRecivedSetting(outStr.Substring(14));     // extract second string and pass it to be handled
                    downloadCount++;                                // second string was not counted, count it now
                }



                if (downloadCount == expectedDownloadCount && !checkBoxRefresh.Checked)         // recived last expected string, do not proceed if parsing for small refresh
                {
                    progressBarCom.Visible = false;                 // hide progress bar
                    expectedDownload = false;                       // and clear download in progress flag
                    timerDownloadTimeout.Stop();                    // stop timer for timeout
                    buttonUpload.Enabled = true;                    // Unlock upload and download buttons allowing start of next transfer session
                    buttonDownload.Enabled = true;
                    checkBoxRefresh.Enabled = true;
                    textBoxStatus.Text = "Download complete";
                    if (Settings.Panel.DebugMode)
                        debugBox.Items.Add(String.Format("Download took {0} seconds", downloadTime));
                }
            }
        }
        public void HandleRecivedSetting(string setting)
        {
            if (Settings.Panel.DebugMode)
                debugBox.Items.Add("Handling:" + setting);
            if (!checkBoxRefresh.Checked)                               // do not proceed if parsing for small refresh
            {
                downloadCount++;                                        // count string as recived for download progress tracking
                progressBarCom.Value += 100 / expectedDownloadCount;    // mark progress on progress bar
            }
            bool ParseCheck = true;

            string code = "";
            byte A = 0;
            byte B = 0;
            byte C = 0;
            /*
            byte A = byte.Parse(setting.Substring(2, 3));
            byte B = byte.Parse(setting.Substring(6, 3));
            byte C = byte.Parse(setting.Substring(10, 3));;
            */
            if (setting.Length == 13)
            {
                code = setting.Substring(0, 2);                             // retrieve code and values from data string
                ParseCheck = byte.TryParse(setting.Substring(2, 3), out A);
                ParseCheck = byte.TryParse(setting.Substring(6, 3), out B);
                ParseCheck = byte.TryParse(setting.Substring(10, 3), out C);
            }
            else { ParseCheck = false; }

            if (ParseCheck)
            {


                if (code == "LD")
                {
                    Settings.Light.LD.R = A;
                    Settings.Light.LD.G = B;
                    Settings.Light.LD.B = C;
                    LoadSettingsToControls();
                }
                if (code == "LN")
                {
                    Settings.Light.LN.R = A;
                    Settings.Light.LN.G = B;
                    Settings.Light.LN.B = C;
                    LoadSettingsToControls();
                }
                if (code == "LS")
                {
                    Settings.Light.LS.R = A;
                    Settings.Light.LS.G = B;
                    Settings.Light.LS.B = C;
                    LoadSettingsToControls();
                }
                if (code == "LZ")
                {
                    Settings.Light.LZ.R = A;
                    Settings.Light.LZ.G = B;
                    Settings.Light.LZ.B = C;
                    LoadSettingsToControls();
                }
                if (code == "TD")
                {
                    Settings.Temperature.TD.I = A;
                    Settings.Temperature.TD.F = B;
                    LoadSettingsToControls();
                }
                if (code == "TN")
                {
                    Settings.Temperature.TN.I = A;
                    Settings.Temperature.TN.F = B;
                    LoadSettingsToControls();
                }
                if (code == "WP")
                {
                    Settings.Water.WP.I = A;
                    Settings.Water.WP.F = B;
                    LoadSettingsToControls();
                }
                if (code == "WG")
                {
                    Settings.Water.WG.I = A;
                    Settings.Water.WG.F = B;
                    LoadSettingsToControls();
                }
                if (code == "CD")
                {
                    Settings.Times.CD.HH = A;
                    Settings.Times.CD.MM = B;
                    LoadSettingsToControls();
                }
                if (code == "CS")
                {
                    Settings.Times.CS.HH = A;
                    Settings.Times.CS.MM = B;
                    LoadSettingsToControls();
                }
                if (code == "CZ")
                {
                    Settings.Times.CZ.HH = A;
                    Settings.Times.CZ.MM = B;
                    LoadSettingsToControls();
                }
                if (code == "CN")
                {
                    Settings.Times.CN.HH = A;
                    Settings.Times.CN.MM = B;
                    LoadSettingsToControls();
                }
                if (code == "CO")
                {
                    Settings.Times.CO.HH = A;
                    Settings.Times.CO.MM = B;
                    LoadSettingsToControls();
                }
                

                if (code == "TT")
                    labelSensorT.Text = A + "." + B;
                if (code == "AA")
                    labelSensorAH.Text = A + "." + B;
                if (code == "GG")
                    labelSensorGH.Text = A + "." + B;
                if (code == "CC")
                {
                    textBoxCCG.Text = A + ":" + B;
                }
            }
            else
            {
                textBoxStatus.Text = "Data Parse failed";
                debugBox.Items.Add(String.Format("Data Parse failed on: " + setting));
            }

        }
        public byte[][] Sensors;
        public void LoadSettingsToControls() // Loading data from Settings class into Form controls
        {

            textBoxTDG.Text = maskedTextBoxTDS.Text = Settings.Temperature.TD.I + "," + Settings.Temperature.TD.F;
            textBoxTNG.Text = maskedTextBoxTNS.Text = Settings.Temperature.TN.I + "," + Settings.Temperature.TN.F;
            textBoxWPG.Text = maskedTextBoxWPS.Text = Settings.Water.WP.I + "," + Settings.Water.WP.F;
            textBoxWGG.Text = maskedTextBoxWGS.Text = Settings.Water.WG.I + "," + Settings.Water.WG.F;


            buttonLSColor.BackColor = Color.FromArgb(Settings.Light.LS.R, Settings.Light.LS.G, Settings.Light.LS.B);
            buttonLDColor.BackColor = Color.FromArgb(Settings.Light.LD.R, Settings.Light.LD.G, Settings.Light.LD.B);
            buttonLZColor.BackColor = Color.FromArgb(Settings.Light.LZ.R, Settings.Light.LZ.G, Settings.Light.LZ.B);
            buttonLNColor.BackColor = Color.FromArgb(Settings.Light.LN.R, Settings.Light.LN.G, Settings.Light.LN.B);

            string hh, mm;
            hh = Settings.Times.CS.HH.ToString(); mm = Settings.Times.CS.MM.ToString();
            if (Settings.Times.CS.HH < 10) { hh = "0" + hh; }
            if (Settings.Times.CS.MM < 10) { mm = "0" + mm; }
            textBoxLSG.Text = maskedTextBoxLSS.Text = hh + ":" + mm;

            hh = Settings.Times.CD.HH.ToString(); mm = Settings.Times.CD.MM.ToString();
            if (Settings.Times.CD.HH < 10) { hh = "0" + hh; }
            if (Settings.Times.CD.MM < 10) { mm = "0" + mm; }
            textBoxLDG.Text = maskedTextBoxLDS.Text = hh + ":" + mm;

            hh = Settings.Times.CZ.HH.ToString(); mm = Settings.Times.CZ.MM.ToString();
            if (Settings.Times.CZ.HH < 10) { hh = "0" + hh; }
            if (Settings.Times.CZ.MM < 10) { mm = "0" + mm; }
            textBoxLZG.Text = maskedTextBoxLZS.Text = hh + ":" + mm;

            hh = Settings.Times.CN.HH.ToString(); mm = Settings.Times.CN.MM.ToString();
            if (Settings.Times.CN.HH < 10) { hh = "0" + hh; }
            if (Settings.Times.CN.MM < 10) { mm = "0" + mm; }
            textBoxLNG.Text = maskedTextBoxLNS.Text = hh + ":" + mm;

            hh = Settings.Times.CO.HH.ToString(); mm = Settings.Times.CO.MM.ToString();
            if (Settings.Times.CO.HH < 10) { hh = "0" + hh; }
            if (Settings.Times.CO.MM < 10) { mm = "0" + mm; }
            textBoxLOG.Text = maskedTextBoxLOS.Text = hh + ":" + mm;

           

            maskedTextBoxLOS.Enabled = Settings.Panel.UseOffLED;
            maskedTextBoxCCS.Enabled = Settings.Panel.SetTerraClock;

        }

        public void GetPorts() // Loading list of available COM ports and applying them com select combo box
        {
            CommPort com = CommPort.Instance;
            com.Close();
            string[] portList = com.GetAvailablePorts();

            comboBoxCOM.Items.Clear();
            comboBoxCOM.Items.AddRange(portList);
        }

        private void comboBoxCOM_SelectedIndexChanged(object sender, EventArgs e) // Opening selected COM port
        {
            CommPort com = CommPort.Instance;
            Settings.Port.PortName = comboBoxCOM.Text;
            com.Open();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) // Closing COM port and saving settings when app closes
        {
            CommPort com = CommPort.Instance;
            com.Close();
            Settings.Write();
        }

        private void buttonSettings_Click(object sender, EventArgs e) // Opening setting window and applying UI changes
        {
            Form2 form2 = new Form2();
            form2.ShowDialog();

            maskedTextBoxLOS.Enabled = Settings.Panel.UseOffLED;
            maskedTextBoxCCS.Enabled = Settings.Panel.SetTerraClock;
            groupBoxDebug.Visible = Settings.Panel.DebugMode;
            groupBoxCurrentValues.Visible = !Settings.Panel.DebugMode;

            if (Settings.Panel.SetTerraClock)
            {
                if (System.DateTime.Now.Hour < 10)
                    maskedTextBoxCCS.Text = "0" + System.DateTime.Now.Hour + ":";
                else
                    maskedTextBoxCCS.Text = System.DateTime.Now.Hour + ":";

                if (System.DateTime.Now.Minute < 10)
                    maskedTextBoxCCS.Text = maskedTextBoxCCS.Text + "0" + System.DateTime.Now.Minute;
                else
                    maskedTextBoxCCS.Text = maskedTextBoxCCS.Text + System.DateTime.Now.Minute;
            }
            else
                maskedTextBoxCCS.Text = "";
        }

        private Color scaleRgbUp(byte R, byte G, byte B)  // scaling colors up for more accurate representation of LED light
        {
            int max = Math.Max(Math.Max(R, G),B);
            int r, g, b;

            r = ((int)R / max) * 150;
            g = ((int)G / max) * 150;
            b = ((int)B / max) * 150;

            return Color.FromArgb(r,g,b);
        }

        private void buttonLSColor_Click(object sender, EventArgs e) // Selecting and applying new color
        {
            colorDialog.Color = Color.FromArgb(Settings.Light.LS.R, Settings.Light.LS.G, Settings.Light.LS.B);
            DialogResult dr = colorDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                buttonLSColor.BackColor = colorDialog.Color;
                Settings.Light.LS.R = colorDialog.Color.R;
                Settings.Light.LS.G = colorDialog.Color.G;
                Settings.Light.LS.B = colorDialog.Color.B;

            }
        }

        private void buttonLDColor_Click(object sender, EventArgs e) // Selecting and applying new color
        {
            colorDialog.Color = Color.FromArgb(Settings.Light.LD.R, Settings.Light.LD.G, Settings.Light.LD.B);
            DialogResult dr = colorDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                buttonLDColor.BackColor = colorDialog.Color;
                Settings.Light.LD.R = colorDialog.Color.R;
                Settings.Light.LD.G = colorDialog.Color.G;
                Settings.Light.LD.B = colorDialog.Color.B;

            }
        }

        private void buttonLZColor_Click(object sender, EventArgs e) // Selecting and applying new color
        {
            colorDialog.Color = Color.FromArgb(Settings.Light.LZ.R, Settings.Light.LZ.G, Settings.Light.LZ.B);
            DialogResult dr = colorDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                buttonLZColor.BackColor = colorDialog.Color;
                Settings.Light.LZ.R = colorDialog.Color.R;
                Settings.Light.LZ.G = colorDialog.Color.G;
                Settings.Light.LZ.B = colorDialog.Color.B;
            }
        }

        private void buttonLNColor_Click(object sender, EventArgs e) // Selecting and applying new color
        {
            colorDialog.Color = Color.FromArgb(Settings.Light.LN.R, Settings.Light.LN.G, Settings.Light.LN.B);
            DialogResult dr = colorDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                buttonLNColor.BackColor = colorDialog.Color;
                Settings.Light.LN.R = colorDialog.Color.R;
                Settings.Light.LN.G = colorDialog.Color.G;
                Settings.Light.LN.B = colorDialog.Color.B;

            }
        }

        private void checkBoxRefresh_CheckedChanged(object sender, EventArgs e) // Start and stop of timer for auto refresh depending on checkbox status and set refresh mode
        {
            if (checkBoxRefresh.Checked)
            {
                if (Settings.Panel.smallRefresh)
                {
                    timerAutoRefresh.Interval = 10000;
                    textBoxStatus.Text = String.Format("Refreshing sensor data every {0} seconds", timerAutoRefresh.Interval / 1000);
                    expectedDownload = true;

                    CommPort com = CommPort.Instance;
                    if (com.IsOpen)
                    {
                        com.Send("DS000.000");

                    }

                }
                else
                {
                    timerAutoRefresh.Interval = 30000;
                    textBoxStatus.Text = String.Format("Refreshing every {0} seconds", timerAutoRefresh.Interval / 1000);
                }
               
                timerAutoRefresh.Start();
            }
            else
            {       
                textBoxStatus.Text = "Autorefresh stopped";
                timerAutoRefresh.Stop();
                expectedDownload = false;
            }
        }

        private void timerAutoRefresh_Tick(object sender, EventArgs e) // If its time for auto refresh download terrarium status 
        {
            if(Settings.Panel.smallRefresh)
            {
                CommPort com = CommPort.Instance;
                if(com.IsOpen)
                {
                    com.Send("DS000.000");
                    textBoxStatus.Text = String.Format("Refreshing sensor data every {0} seconds", timerAutoRefresh.Interval / 1000);

                }
                else
                {
                    checkBoxRefresh.Checked = false;
                }
            }
            else
            {
                buttonDownload.PerformClick();
            }

        }

        private int downloadTime;

        private void buttonDownload_Click(object sender, EventArgs e) // request data download from terrarium control module
        {
            debugBox.Items.Clear();
            checkBoxRefresh.Checked = false;                   // stop auto refesh to prevent interference with download session
            string command = "DL000.000";                      // command to request download
            CommPort com = CommPort.Instance;
            expectedDownloadCount = 17;                        // set how many data strings are expected to arrive 
            downloadCount = 0;                                 // and set count of recived so far at 0
            downloadTime = 0;                                  // reset time count for download
            

            if (com.IsOpen)
            {
                com.Send(command);
                textBoxStatus.Text = "Requested download";     // mark progress on UI in form of text status and progress bar
                expectedDownload = true;                       // set download in progress flag
                progressBarCom.Visible = true;
                progressBarCom.Value = 0;
                buttonUpload.Enabled = false;                  // Lock user out of starting another upload or download session before current is complete.
                buttonDownload.Enabled = false;
                checkBoxRefresh.Enabled = false;
                timerDownloadTimeout.Start();                  // start timer to check for timeout
            }
            else
            {
                textBoxStatus.Text = "Port not open";

            }

        }

        private string[] UploadCommands = new string[14];
        private int progressBarStep;
        private int UploadCommandIndex = 0;

        private void buttonUpload_Click(object sender, EventArgs e)  // start upload of data to terrarium control module
        {
            debugBox.Items.Clear();

            LoadSettingsToControls();

            nrfErrorCount = 0;

            Settings.UpdateAll();                                           // Prepare all data to be sent in strings parsable by remote module and terrarium control module

            UploadCommands[0] = "TD" + Settings.Temperature.TD.Temp;
            UploadCommands[1] = "TN" + Settings.Temperature.TN.Temp;
            UploadCommands[2] = "WP" + Settings.Water.WP.Hum;
            UploadCommands[3] = "WG" + Settings.Water.WG.Hum;
            UploadCommands[4] = "LD" + Settings.Light.LD.RGB;
            UploadCommands[5] = "LN" + Settings.Light.LN.RGB;
            UploadCommands[6] = "LS" + Settings.Light.LS.RGB;
            UploadCommands[7] = "LZ" + Settings.Light.LZ.RGB;
            UploadCommands[8] = "CD" + Settings.Times.CD.Time;
            UploadCommands[9] = "CN" + Settings.Times.CN.Time;
            UploadCommands[10] = "CS" + Settings.Times.CS.Time;
            UploadCommands[11] = "CZ" + Settings.Times.CZ.Time;

            if (Settings.Panel.UseOffLED)
                UploadCommands[12] = "CO" + Settings.Times.CO.Time;
            else
                UploadCommands[12] = "CO" + "200.200";
            if (Settings.Panel.SetTerraClock && maskedTextBoxCCS.Text != "  :")
                UploadCommands[13] = "CC" + "0" + maskedTextBoxCCS.Text.Substring(0, 2) + ".0" + maskedTextBoxCCS.Text.Substring(3);
            else
                UploadCommands[13] = "CC" + "200.200";

            progressBarStep = 100 / UploadCommands.Length;

            if (Settings.Panel.DebugMode)
            {
                debugBox.Items.Add("Commands to be sent to control module: ");
                for (int i = 0; i < UploadCommands.Length; i++)
                {
                    debugBox.Items.Add(UploadCommands[i]);
                }
            }

            CommPort com = CommPort.Instance;
            if (com.IsOpen)
            {
                UploadCommandIndex = 0;                                                                                     // Set index at start of data
                textBoxStatus.Text = String.Format("Uploaded{0} out of {1}", UploadCommandIndex + 1, UploadCommands.Length + 1);// Mark progress on UI in form of both status text 
                progressBarCom.Value = 0;                                                                                   // and progress bar.
                timerUpload.Start();                                                                                        // Start timer delaying sending next lines of data. Remote module cant realiably handle sending data by nRF and reciving data by serial at faster rate.
                buttonUpload.Enabled = false;                                                                               // Lock user out of starting another upload or download session before current is complete.
                buttonDownload.Enabled = false;
                checkBoxRefresh.Enabled = false;
                progressBarCom.Visible = true;
                checkBoxRefresh.Checked = false;                                                                            // stop auto refesh to prevent interference with download session

            }
            else
            {
                textBoxStatus.Text = "Port not open";

            }

        }

        private void timerUpload_Tick(object sender, EventArgs e)
        {

            CommPort com = CommPort.Instance;
            if (com.IsOpen)
            {
                if (UploadCommands.Length > UploadCommandIndex)                                                                       // Check if there is still data left to send
                {
                    com.Send(UploadCommands[UploadCommandIndex]);                                                                  // Send next line of data
                    UploadCommandIndex++;                                                                                          // and move index forward marking line send.
                    textBoxStatus.Text = String.Format("Uploading {0} out of {1}", UploadCommandIndex + 1, UploadCommands.Length + 1); // Mark progress on UI in form of both status text                    
                    progressBarCom.Value += 7;                                                                                     // and progress bar.

                }
                else                                                                                   // All data hes been send.
                {


                    buttonUpload.Enabled = true;                                                       // Unlock upload and download buttons allowing start of next transfer session
                    buttonDownload.Enabled = true;
                    checkBoxRefresh.Enabled = true;
                    textBoxStatus.Text = "Upload finished";                                            // Mark end of upload session on UI status text
                    timerUpload.Stop();                                                                // All data hes been send, timer no longer needed
                    progressBarCom.Visible = false;                                                    // same with progress bar

                    if (nrfErrorCount > 0)
                    {
                        if (Settings.Panel.DebugMode)
                            textBoxStatus.Text = String.Format("There was {0} radio error(s)", nrfErrorCount);
                        else
                            textBoxStatus.Text = "There were some radio errors";
                    }
                }
            }


        }

        private void buttonDebugSend_Click(object sender, EventArgs e)
        {
            if (Settings.Panel.DebugMode)
            {
                CommPort com = CommPort.Instance;
                if (textBoxDebugData.Text != "")
                {
                    com.Send(textBoxDebugData.Text);
                    textBoxDebugData.Text = "";
                }
            }
        }

        private void textBoxDebugData_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonDebugSend.PerformClick();
                e.Handled = true;
            }
        }

        private void buttonClearDebug_Click(object sender, EventArgs e)
        {
            debugBox.Items.Clear();
        }

        private void labelCom_Click(object sender, EventArgs e)
        {
            GetPorts();
        }

        private void timerDownloadTimeout_Tick(object sender, EventArgs e)
        {
            if (downloadTime == 22)                               // how long wait for data
            {
                expectedDownload = false;                       // stop download read from serial
                textBoxStatus.Text = "Download has timed out";  // let user know about timeout
                progressBarCom.Visible = false;                 // and hide progress bar
                timerDownloadTimeout.Stop();                    // stop timer
                buttonUpload.Enabled = true;                    // Unlock upload and download buttons allowing start of next transfer session
                buttonDownload.Enabled = true;
                checkBoxRefresh.Enabled = true;
            }
            downloadTime++;
        }

        void comboBoxCOM_MouseWheel(object sender, MouseEventArgs e) // mouse event that does nothing to prevent accidental COM Port change ( Cast over generic mouse event)
        {
            ((HandledMouseEventArgs)e).Handled = true;
        }

        private void maskedTextBoxTDS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            int i = maskedTextBoxTDS.Text.IndexOf(',');
            int a,b;
            if(int.TryParse(maskedTextBoxTDS.Text.Substring(0,i), out a))
            {
                Settings.Temperature.TD.I = (byte)a;
            }
            if (int.TryParse(maskedTextBoxTDS.Text.Substring(i+1), out b))
            {
                Settings.Temperature.TD.F = (byte)b;
            }
        }

        private void maskedTextBoxTNS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            int i = maskedTextBoxTNS.Text.IndexOf(',');
            int a, b;
            if (int.TryParse(maskedTextBoxTNS.Text.Substring(0, i), out a))
            {
                Settings.Temperature.TN.I = (byte)a;
            }
            if (int.TryParse(maskedTextBoxTNS.Text.Substring(i + 1), out b))
            {
                Settings.Temperature.TN.F = (byte)b;
            }
        }

        private void maskedTextBoxWPS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            int i = maskedTextBoxWPS.Text.IndexOf(',');
            int a, b;
            if (int.TryParse(maskedTextBoxWPS.Text.Substring(0, i), out a))
            {
                Settings.Water.WP.I = (byte)a;
            }
            if (int.TryParse(maskedTextBoxWPS.Text.Substring(i + 1), out b))
            {
                Settings.Water.WP.F = (byte)b;
            }
        }

        private void maskedTextBoxWGS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            int i = maskedTextBoxWGS.Text.IndexOf(',');
            int a, b;
            if (int.TryParse(maskedTextBoxWGS.Text.Substring(0, i), out a))
            {
                Settings.Water.WG.I = (byte)a;
            }
            if (int.TryParse(maskedTextBoxWGS.Text.Substring(i + 1), out b))
            {
                Settings.Water.WG.F = (byte)b;
            }
        }

        private void maskedTextBoxLDS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            int i = maskedTextBoxLDS.Text.IndexOf(':');
            int a, b;
            if (int.TryParse(maskedTextBoxLDS.Text.Substring(0, i), out a))
            {
                if (a < 24)
                    Settings.Times.CD.HH = (byte)a;
            }
            if (int.TryParse(maskedTextBoxLDS.Text.Substring(i + 1), out b))
            {
                if (b < 60)
                    Settings.Times.CD.MM = (byte)b;
            }
        }

        private void maskedTextBoxLZS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            int i = maskedTextBoxLZS.Text.IndexOf(':');
            int a, b;
            if (int.TryParse(maskedTextBoxLZS.Text.Substring(0, i), out a))
            {
                if (a < 24)
                    Settings.Times.CZ.HH = (byte)a;
            }
            if (int.TryParse(maskedTextBoxLZS.Text.Substring(i + 1), out b))
            {
                if (b < 60)
                    Settings.Times.CZ.MM = (byte)b;
            }
        }

        private void maskedTextBoxLOS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            if (Settings.Panel.UseOffLED)
            {
                int i = maskedTextBoxLOS.Text.IndexOf(':');
                int a, b;
                if (int.TryParse(maskedTextBoxLOS.Text.Substring(0, i), out a))
                {
                    if (a < 24)
                        Settings.Times.CO.HH = (byte)a;
                }
                if (int.TryParse(maskedTextBoxLOS.Text.Substring(i + 1), out b))
                {
                    if (b < 60)
                        Settings.Times.CO.MM = (byte)b;
                }
            }
        }

        private void maskedTextBoxLSS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            int i = maskedTextBoxLSS.Text.IndexOf(':');
            int a, b;
            if (int.TryParse(maskedTextBoxLSS.Text.Substring(0, i), out a))
            {
                if (a < 24)
                    Settings.Times.CS.HH = (byte)a;
            }
            if (int.TryParse(maskedTextBoxLSS.Text.Substring(i + 1), out b))
            {
                if (b < 60)
                    Settings.Times.CS.MM = (byte)b;
            }
        }

        private void maskedTextBoxLNS_TextChanged(object sender, EventArgs e)  // pull data from user input
        {
            int i = maskedTextBoxLNS.Text.IndexOf(':');
            int a, b;
            if (int.TryParse(maskedTextBoxLNS.Text.Substring(0, i), out a))
            {
                if (a < 24)
                    Settings.Times.CN.HH = (byte)a;
            }
            if (int.TryParse(maskedTextBoxLNS.Text.Substring(i + 1), out b))
            {
                if (b < 60)
                    Settings.Times.CN.MM = (byte)b;
            }
        }
    }
}
