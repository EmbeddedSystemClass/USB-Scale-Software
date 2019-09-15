﻿using System;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace USBScaleSoftware
{
    public partial class Form1 : Form
    {
        SerialPort port;

        uint period, time;

        byte H = 0x48;
        byte Z = 0x5A;
        byte channel = 0x01;

        byte crc = 0xdc;

        public Form1()
        {
            InitializeComponent();
            port = new SerialPort("COM" + COM.Value, 921600, Parity.None, 8, StopBits.One);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            unitCB.SelectedIndex = 0;
        }

        private void readBTN_Click(object sender, EventArgs e)
        {
            port.Dispose();
            port = new SerialPort("COM" + COM.Value, 921600, Parity.None, 8, StopBits.One);

            byte length = 0x10;

            byte destination = 0x01;
            byte source = 0x00;
            byte options = 0x22;
            byte M_C = 0x07;    //code = 1901 to be represented in two bytes
            byte L_C = 0x6D;     // default is gram reading
            switch (unitCB.SelectedItem)
            {
                case "Gram": L_C = 0x6D; break; //code = 1901 ; 076D
                case "Kg": L_C = 0x6E; crc = 0x64;  break; //code = 1902
                case "Ounces": L_C = 0x6F; crc = 0x38; break; //code = 1903
                case "Pounds": L_C = 0x70; crc = 0x63; break; //code = 1904
                default: break;
            }

            
            byte modulePort = 0x03;
            byte module = 0x01;
            

            try { period = uint.Parse(periodTB.Text); } catch { MessageBox.Show("Check period input!"); return; }
            try { time = uint.Parse(timeTB.Text); } catch { MessageBox.Show("Check time input!"); return; }

            if (radioButton2.Checked)
                channel = 0x02;

            byte[] periodBytes = BitConverter.GetBytes(period);
            byte[] timeBytes = BitConverter.GetBytes(time);

            if (!timeTB.Enabled)
                time = 0xFFFFFFFF;    // infinte time code

            byte[] buffer = {
                            destination,
                            source,
                            options,
                            M_C,
                            L_C,
                            channel,
                            periodBytes[3],
                            periodBytes[2],
                            periodBytes[1],
                            periodBytes[0],
                            timeBytes[3],
                            timeBytes[2],
                            timeBytes[1],
                            timeBytes[0],
                            modulePort,
                            module};

            byte[] buffer_crc = {
                            H,
                            Z,
                            length,
                            destination,
                            source,
                            options,
                            L_C,
                            M_C,
                            channel,
                            periodBytes[3],
                            periodBytes[2],
                            periodBytes[1],
                            periodBytes[0],
                            timeBytes[3],
                            timeBytes[2],
                            timeBytes[1],
                            timeBytes[0],
                            modulePort,
                            module,
                            crc};

            try { port.Open(); } catch { }
            try { port.Write(buffer_crc, 0, 20); } catch { MessageBox.Show("Connection Error"); return; }

            receive();

        }

        private void ZeroBTN_Click(object sender, EventArgs e)
        {
            
            byte length = 0x06;

            byte destination = 0x01;
            byte source = 0x00;
            byte options = 0x22;
            byte M_C = 0x07;    //code = 1910 to be represented in two bytes
            byte L_C = 0x76;

            if (radioButton2.Checked)
                channel = 0x02;

            byte[] buffer_crc = {
                            H,
                            Z,
                            length,
                            destination,
                            source,
                            options,
                            L_C,
                            M_C,
                            channel,
                            0xf4};

            try { port.Open(); } catch { }

            
            try { port.Write(buffer_crc, 0, 10); } catch { MessageBox.Show("Connection Error"); return; }
           
        }

        private void StopBTN_Click(object sender, EventArgs e)
        {
            byte length = 0x06;

            byte destination = 0x01;
            byte source = 0x00;
            byte options = 0x22;
            byte M_C = 0x07;    //code = 1905 to be represented in two bytes
            byte L_C = 0x71;

            if (radioButton2.Checked)
                channel = 0x02;

            byte[] buffer_crc = {
                            H,
                            Z,
                            length,
                            destination,
                            source,
                            options,
                            L_C,
                            M_C,
                            channel,
                            0x61};

            try { port.Open(); } catch { }


            try { port.Write(buffer_crc, 0, 10); } catch { MessageBox.Show("Connection Error"); return; }

            try { port.Close(); } catch { };
            sevenSegmentArray1.Value = "STOPED";
            connectionLBL.Text = "Stopped";
            port.Dispose();
            port = new SerialPort("COM" + COM.Value, 921600, Parity.None, 8, StopBits.One);

        }

        private void receive()
        {
            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            try { port.Open(); } catch { }
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Invoke((MethodInvoker)delegate
            {
                int bytes_count = 0;
                byte[] buffer = new byte[4];
                bytes_count = port.Read(buffer, 0, 4);

                string D0 = to_right_hex(buffer[3].ToString("X"));
                string D1 = to_right_hex(buffer[2].ToString("X"));
                string D2 = to_right_hex(buffer[1].ToString("X"));
                string D3 = to_right_hex(buffer[0].ToString("X"));
                string weight = D3 + D2 + D1 + D0;

                try
                {
                    uint weight_number = uint.Parse(weight, System.Globalization.NumberStyles.HexNumber) / 10;
                    if (unitCB.SelectedItem.ToString() == "Kg")
                    {
                        float kg = weight_number / 1000f;
                        sevenSegmentArray1.Value = kg + "";
                        connectionLBL.Text = "Receiving...";
                    }
                    else
                    {
                        sevenSegmentArray1.Value = weight_number + "";
                        connectionLBL.Text = "Receiving...";
                    }
                }
                catch
                {
                    connectionLBL.Text = "Error!";
                }
            });
        }

        private string to_right_hex(string hex)
        {
            switch (hex)
            {
                case "A": hex = "0" + hex; break;
                case "B": hex = "0" + hex; break;
                case "C": hex = "0" + hex; break;
                case "D": hex = "0" + hex; break;
                case "E": hex = "0" + hex; break;
                case "F": hex = "0" + hex; break;
            }
            return hex;
        }

        string weight = "";

        private void UnitCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            StopBTN_Click(sender, e);
            weight = unitCB.SelectedItem.ToString();
            weightLBL.Text = weight;
            readBTN_Click(sender, e);
        }

        private void IfnCB_CheckedChanged(object sender, EventArgs e)
        {
            if (timeTB.Enabled)
                timeTB.Enabled = false;
            else
                timeTB.Enabled = true;
        }
    }
}
