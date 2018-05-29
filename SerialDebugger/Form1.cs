﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SerialDebugger
{
    public partial class Form1 : Form
    {
        public string SettingsFolderPath = "Settings";
        ArduinoSerial Arduino = new ArduinoSerial();
        Jason serializer = new Jason();

        /* List for collecting DataSetControl controls */
        public List<DataSetControl> dataControlList = new List<DataSetControl>();

        /* List for serial port data saving and restoring */
        public List<ArduinoSerial.serialportData> dataList = new List<ArduinoSerial.serialportData>();

        public Form1()
        {
            InitializeComponent();
            
            /* Check Settings folder is Exist or not. If not, we need to create it. */
            if(!Directory.Exists(SettingsFolderPath))
            {
                Directory.CreateDirectory(SettingsFolderPath);
            }

            /* Set default serialport parameters for arduino */
            Arduino.ArduinoDefaults(baudrate_cbox, databit_cbox, parity_cbox, stopbit_cbox);

            /* Alive timer... */
            serialPortAliveTimer.Enabled = true;

            /* Data structures for read and handling settings data */
            ArduinoSerial.saveSettings readSettings = new ArduinoSerial.saveSettings();
            ArduinoSerial.serialportData portData = new ArduinoSerial.serialportData();

            List<string> settingsList = new List<string>();

            /* Find all settings file in Settings directory */
            foreach (string filePath in Directory.EnumerateFiles(SettingsFolderPath, "*.settings"))
            {
                settingsList.Add(filePath);
            }

            /* If the result is higher than zero, we have settings */
            if (settingsList.Count > 0)
            {
                foreach (string filePath in settingsList)
                {
                    /* Read settings from json file format */
                    try
                    {
                        readSettings = Jason.ReadFromJsonFile<ArduinoSerial.saveSettings>(filePath);
                        portData = new ArduinoSerial.serialportData(readSettings);
                        dataList.Add(portData);
                        settings_lbox.Items.Add(portData.dataName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Sajna nem sikerült beolvasni");
                    }
                }
            }
        }

        private void connect_btn_Click(object sender, EventArgs e)
        {
            /* Is not open */
            if (!Arduino.serialPort.IsOpen)
            {
                Arduino.SetSerialPort(comport_cbox, baudrate_cbox, databit_cbox, parity_cbox, stopbit_cbox);

                Arduino.serialPort.Open();
            }
            else
            {
                Arduino.serialPort.Close();
                Arduino.serialPort.Dispose();
            }
        }

        private void Arduino_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string incoming = Arduino.serialPort.ReadLine();
                BeginInvoke(new LineReceivedEvent(LineReceived), incoming);
            }
            catch { }
        }

        private delegate void LineReceivedEvent(string incomingData);

        private void LineReceived(string incomingData)
        {
            serialMonitor_rtbox.AppendText(DateTime.Now.ToLongTimeString() + " [Received] - " + incomingData.ToString() + "\n");
            serialMonitor_rtbox.ScrollToCaret();
        }

        private void serialPortAlive_Tick(object sender, EventArgs e)
        {
            if(Arduino.serialPort.IsOpen)
            {
                connect_btn.Enabled = false;
                disconnect_btn.Enabled = true;
                sendCommand_btn.Enabled = true;

                connection_status_tSlabel.Text = "Connected";
                connection_status_tSlabel.BackColor = Color.Green;

                connected_comport_tSlabel.Text = Arduino.serialPort.PortName;
                baudrate_tSlabel.Text = Arduino.serialPort.BaudRate.ToString();
                databits_tSlabel.Text = Arduino.serialPort.DataBits.ToString();
                parity_tSlabel.Text = Arduino.serialPort.Parity.ToString();
                stopbit_tSlabel.Text = Arduino.serialPort.StopBits.ToString();
            }
            else
            {
                connect_btn.Enabled = true;
                disconnect_btn.Enabled = false;
                sendCommand_btn.Enabled = false;

                connection_status_tSlabel.Text = "Disconnected";
                connection_status_tSlabel.BackColor = Color.Empty;

                connected_comport_tSlabel.Text = " - ";
                baudrate_tSlabel.Text = " - ";
                databits_tSlabel.Text = " - ";
                parity_tSlabel.Text = " - ";
                stopbit_tSlabel.Text = " - ";
            }
        }

        private void disconnect_btn_Click(object sender, EventArgs e)
        {
            Arduino.serialPort.Close();
            Arduino.serialPort.Dispose();
        }

        private void sendCommand_btn_Click(object sender, EventArgs e)
        {
            getTemp();
        }

        private void getTemp()
        {
            String msg = "6,0,0,0\n";
            Arduino.serialPort.Write(msg);

            serialMonitor_rtbox.AppendText(DateTime.Now.ToLongTimeString() + " [Sent] - " + msg.ToString() + "\n");
            serialMonitor_rtbox.ScrollToCaret();
        }

        private void comport_cbox_Click(object sender, EventArgs e)
        {
            int existIndex;

            /* Fill comport combobox */
            foreach (string port in Arduino.AvailablePorts())
            {
                /* Check that port name is in the combobox already */
                existIndex = comport_cbox.FindString(port);

                /* If not, than add to combobox */
                if (!(existIndex >= 0))
                {
                    comport_cbox.Items.Add(port);
                }
            }
        }

        private void saveSettings_btn_Click(object sender, EventArgs e)
        {
            if (settingsName_tbox.TextLength > 0)
            {
                if (!settings_lbox.Items.Contains(settingsName_tbox.Text.ToString()))
                {
                    settings_lbox.Items.Add(settingsName_tbox.Text.ToString());
                }
                ArduinoSerial.saveSettings actual = new ArduinoSerial.saveSettings();

                /* Set serialport parameters for saving */
                Arduino.serialPort.PortName = comport_cbox.SelectedItem.ToString();
                Arduino.serialPort.BaudRate = Convert.ToInt32(baudrate_cbox.SelectedItem.ToString());
                Arduino.serialPort.DataBits = Convert.ToInt32(databit_cbox.SelectedItem.ToString());

                switch (parity_cbox.SelectedItem.ToString())
                {
                    case "None":
                        Arduino.serialPort.Parity = Parity.None;
                        break;
                    case "Odd":
                        Arduino.serialPort.Parity = Parity.Odd;
                        break;
                    case "Even":
                        Arduino.serialPort.Parity = Parity.Even;
                        break;
                    case "Mark":
                        Arduino.serialPort.Parity = Parity.Mark;
                        break;
                    case "Space":
                        Arduino.serialPort.Parity = Parity.Space;
                        break;
                }

                switch (stopbit_cbox.SelectedItem.ToString())
                {
                    case "One":
                        Arduino.serialPort.StopBits = StopBits.One;
                        break;
                    case "Two":
                        Arduino.serialPort.StopBits = StopBits.Two;
                        break;
                    case "OnePointFive":
                        Arduino.serialPort.StopBits = StopBits.OnePointFive;
                        break;
                }

                actual = new ArduinoSerial.saveSettings(settingsName_tbox.Text.ToString(), Arduino.serialPort);

                /* Add serial port informations to list, then write to a file (and in this way, we can read) */
                dataList.Add(new ArduinoSerial.serialportData(actual));

                Jason.WriteToJsonFile(SettingsFolderPath + "/" + settingsName_tbox.Text.ToString() + ".settings", actual);

                settingsName_tbox.Text = string.Empty;
            }
            else
            {
                MessageBox.Show("Settings name is EMPTY! You need to write something...", "Error");
            }
        }

        private void deleteSettings_btn_Click(object sender, EventArgs e)
        {
            if(settings_lbox.SelectedIndex > -1)
            {
                File.Delete(SettingsFolderPath + "/" + settings_lbox.SelectedItem.ToString() + ".settings");
                settings_lbox.Items.Remove(settings_lbox.SelectedItem);
            }
        }

        private void loadSettings_btn_Click(object sender, EventArgs e)
        {
            /* If one settings is active in list */
            if (settings_lbox.SelectedIndex >= 0)
            {
                /* Find this settings details in dataList and set serialport parameters for connection */
                foreach (ArduinoSerial.serialportData spData in dataList)
                {
                    /* If selected settings is in the list */
                    if (spData.dataName == settings_lbox.SelectedItem.ToString())
                    {
                        /* If the comport combobox not contain */
                        if (!comport_cbox.Items.Contains(spData.serialInformations.PortName))
                        {
                            /* Add this comport name and select */
                            comport_cbox.Items.Add(spData.serialInformations.PortName);
                            comport_cbox.SelectedIndex = comport_cbox.Items.Count - 1;

                            baudrate_cbox.SelectedItem = spData.serialInformations.BaudRate.ToString();
                            databit_cbox.SelectedItem = spData.serialInformations.DataBits.ToString();
                            stopbit_cbox.SelectedItem = spData.serialInformations.StopBits.ToString();
                            parity_cbox.SelectedItem = spData.serialInformations.Parity.ToString();
                        }
                        else
                        {
                            /* If it contain it, select */
                            comport_cbox.SelectedItem = spData.serialInformations.PortName;
                            baudrate_cbox.SelectedItem = spData.serialInformations.BaudRate.ToString();
                            parity_cbox.SelectedItem = spData.serialInformations.Parity.ToString();
                            databit_cbox.SelectedItem = spData.serialInformations.DataBits.ToString();
                            stopbit_cbox.SelectedItem = spData.serialInformations.StopBits.ToString();
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Choose one settings from list.", "Error");
            }
        }

        private void settings_lbox_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                settingsName_tbox.Text = settings_lbox.SelectedItem.ToString();
            }
            catch { }
        }

        private void addDataSet_btn_Click(object sender, EventArgs e)
        {
            ToolTip toolTip = new ToolTip();
            DataSetControl setControl = new DataSetControl();

            setControl.dataSetName.Text = dataSetName_tbox.Text;
            setControl.MessageID.Text = dataSetID_tbox.Text;
            setControl.Unit.Text = dataSetUnit_tbox.Text;

            try
            {
                /* Set tooltip for this control */
                toolTip.SetToolTip(setControl.dataSetName, "Message ID: " + setControl.MessageID.Text);
                /* Add control to layout panel */
                flowLayoutPanel1.Controls.Add(setControl);
                /* Add control to list */
                dataControlList.Add(setControl);
                /* Add double click event to control */
                setControl.dataSetName.DoubleClick += delegate (object sender2, EventArgs e2)
                    {
                        dataControl_DoubleClick_Event(sender, e, setControl);
                    };
                /* Design... no more */
                setControl.dataSetName.MouseHover += delegate (object sender3, EventArgs e3)
                {
                    dataControl_MouseHover_Event(sender, e, setControl);
                };
                /* Design... no more */
                setControl.dataSetName.MouseLeave += delegate (object sender4, EventArgs e4)
                {
                    dataControl_MouseLeave_Event(sender, e, setControl);
                };
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }

            /* Clear all textboxes */
            dataSetName_tbox.Clear();
            dataSetUnit_tbox.Clear();
            dataSetID_tbox.Clear();
        }

        private void modifyDataSet_btn_Click(object sender, EventArgs e)
        {
            /* Azt kéne csinálni, hogy doubleClick event minden DataSetControl-hoz.
             *  Ebben az event-ben a controlban lévő adatokat vissza kell tölteni a textboxokba 
             *  (index alapján könnyen kiszedhető a listából.
             *  
             *  Modify gomb megnyomására pedig frissíteni a listában az adatokat és reménykedni, hogy az a felületen is frissül.
             * 
             */
            ToolTip toolTip = new ToolTip();

            /* Find double clicked control in List */
            foreach(DataSetControl setControl in dataControlList)
            {
                /* If we found, change properties */
                if (setControl.TabIndex == Convert.ToInt32(dataSetIndex_tbox.Text))
                {
                    setControl.dataSetName.Text = dataSetName_tbox.Text;
                    setControl.Unit.Text = dataSetUnit_tbox.Text;
                    setControl.MessageID.Text = dataSetID_tbox.Text;
                    toolTip.SetToolTip(setControl, "Message ID: " + setControl.MessageID.Text); // not worked...
                }
            }

            /* Clear all textboxes */
            dataSetName_tbox.Clear();
            dataSetID_tbox.Clear();
            dataSetUnit_tbox.Clear();
        }

        private void dataControl_DoubleClick_Event(object sender, EventArgs e, DataSetControl setControl)
        {
            /* Load datas from control to textboxes */
            dataSetName_tbox.Text = setControl.dataSetName.Text;
            dataSetID_tbox.Text = setControl.MessageID.Text;
            dataSetUnit_tbox.Text = setControl.Unit.Text;
            dataSetIndex_tbox.Text = setControl.TabIndex.ToString();
        }

        private void dataControl_MouseHover_Event(object sender, EventArgs e, DataSetControl setControl)
        {
            setControl.BackColor = Color.AliceBlue;
        }

        private void dataControl_MouseLeave_Event(object sender, EventArgs e, DataSetControl setControl)
        {
            setControl.BackColor = Color.White;
        }

        private void deleteDataSet_btn_Click(object sender, EventArgs e)
        {
            /* Find double clicked control in List */
            foreach (DataSetControl setControl in dataControlList)
            {
                /* If we found, change properties */
                if (setControl.TabIndex == Convert.ToInt32(dataSetIndex_tbox.Text))
                {
                    try
                    {
                        flowLayoutPanel1.Controls.RemoveAt(setControl.TabIndex);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                    }
                }
            }

            /* Clear all textboxes */
            dataSetName_tbox.Clear();
            dataSetID_tbox.Clear();
            dataSetUnit_tbox.Clear();
        }
    }
}
