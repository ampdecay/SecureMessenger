using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SecureMessenger
{
    public partial class Form1 : Form
    {
        #region Properties
        public Server server;
        public Client client;

        public delegate void UpdateTextCallback(string text);
        private delegate void ReconnectCallback();//Handle Cross-Thread exception.

        //private Socket sock;
        //private byte[] data = new byte[1024];
        #endregion

        //global encyption objects
        public DES myDES = new DESCryptoServiceProvider();
        public MD5 myMD5 = new MD5CryptoServiceProvider();

        public Form1()
        {
            InitializeComponent();
            //gets ip address of current machine by matching a regex to the address list
            Regex ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            IPHostEntry IPHost = Dns.GetHostEntry(Dns.GetHostName());
            string ipText = "0.0.0.0";
            for (int i = 0; i < IPHost.AddressList.Length; i++)
            {
                if (ip.IsMatch(IPHost.AddressList[i].ToString()))
                {
                    ipText = IPHost.AddressList[i].ToString();
                }
            }
            string[] IPs = ipText.Split(new char[] { '.' });
            textBox3.Text = IPs[0];
            textBox4.Text = IPs[1];
            textBox5.Text = IPs[2];
            textBox6.Text = IPs[3];
        }
        #region Event Handlers

        //Buttons
        private void button1_Click(object sender, EventArgs e)//Send
        {
            if (textBox2.Text != "")
            {
                if (radioButton1.Checked == true)//Server Mode
                {
                    try
                    {
                        if (server != null)
                        {
                            
                            //if string length % 8 = 0 get weird padding issues so adds extra blank character
                            string text = textBox2.Text;
                            if (text.Length % 8 == 0)
                                text += " ";
                            byte[] bytes = EncryptString(myDES, text);
                            server.Send(bytes);
                            string str = "";
                            str = "\r\nServer said: (@" + DateTime.Now.ToString() + ")\r\n" + textBox2.Text;
                            textBox1.AppendText(str + "\r\n");
                            str = "\r\nServer said: (@" + DateTime.Now.ToString() + ")\r\n" + Encoding.ASCII.GetString(bytes);
                            textBox8.AppendText(str + "\r\n");
                            textBox2.Clear();
                           
                        }
                    }
                    catch (SocketException se)
                    {
                        MessageBox.Show("Server send error!\r\n" + se.Message);
                    }
                }
                else if (radioButton2.Checked == true)//Client Mode
                {
                    try
                    {
                        if (client != null)
                        {
                           
                            //if string length % 8 = 0 get weird padding issues so adds extra blank character
                            string text = textBox2.Text;
                            if (text.Length % 8 == 0)
                                text += " ";
                            byte[] bytes = EncryptString(myDES, text);
                            client.Send(bytes);
                            string str = "";
                            str = "\r\nClient said: (@" + DateTime.Now.ToString() + ")\r\n" + textBox2.Text;
                            textBox1.AppendText(str + "\r\n");
                            str = "\r\nClient said: (@" + DateTime.Now.ToString() + ")\r\n" + Encoding.ASCII.GetString(bytes);
                            textBox8.AppendText(str + "\r\n");
                            textBox2.Clear();
                          
                        }
                    }
                    catch (SocketException se)
                    {
                        MessageBox.Show("Client send error!\r\n" + se.Message);
                    }
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)//Connect
        {
            if (ppBox.Text == "")
            {
                MessageBox.Show("Please enter a passphrase!!!");
            }
            else
            {
                setUpEncryption();
                string ipAddr = textBox3.Text + "." + textBox4.Text + "." + textBox5.Text + "." + textBox6.Text;
                string port = textBox7.Text;
                if (IsValidIPAddress(ipAddr) == true)
                {
                    if (radioButton1.Checked == true)//Server Mode
                    {
                        try
                        {
                            if (server == null)
                                server = new Server(this);

                            server.Connect(ipAddr, port);
                            button2.Enabled = false;
                            button3.Enabled = true;
                            textBox2.Focus();
                        }
                        catch (SocketException se)
                        {
                            MessageBox.Show("Server Connect Error.\r\n" + se.ToString());
                        }
                    }
                    else if (radioButton2.Checked == true)//Client Mode
                    {
                        try
                        {
                            if (client == null)
                                client = new Client(this);

                            client.Connect(ipAddr, port);
                            
                            button2.Enabled = false;
                            button3.Enabled = true;
                            textBox2.Focus();
                        }
                        catch (SocketException se)
                        {
                            MessageBox.Show("Client Connect Error.\r\n" + se.ToString());
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Invalid IP address input.");
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)//Disconnect
        {
            if (radioButton1.Checked == true)//Server Mode
            {
                server.Disconnect();
                if (button2.Enabled == false)
                {
                    button2.Enabled = true;
                    button3.Enabled = false;
                }
            }
            else if (radioButton2.Checked == true)//Client Mode
            {
                client.Disconnect();
                if (button2.Enabled == false)
                {
                    button2.Enabled = true;
                    button3.Enabled = false;
                }
            }

        }
        private void button4_Click(object sender, EventArgs e)//Reconnect
        {
            if (button3.Enabled == true)
                button3_Click(sender, e);//Disconnect
            Thread.Sleep(200);
            button2_Click(sender, e);//Connect
        }
        private void button5_Click(object sender, EventArgs e)//Clear
        {
            textBox1.Clear();
            textBox8.Clear();
        }
        //TextBox2
        private void textBox2_KeyDown(object sender, KeyEventArgs e)//Send the message
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(sender, (EventArgs)e);//Send
            }
        }
        private void textBox2_KeyUp(object sender, KeyEventArgs e)//Clear the text in textBox2
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (textBox2.Lines.Length > 0)
                    textBox2.Lines = new string[] { };
            }
        }
        //Timer
        private void timer1_Tick(object sender, EventArgs e)//Timer1
        {
            toolStripStatusLabel2.Text = DateTime.Now.ToLocalTime().ToLongDateString() + " " + DateTime.Now.ToLocalTime().ToLongTimeString();
        }
        #endregion


        #region Helper Methods
        /// <summary>
        /// Sets up the encryption algorithim DES by hashing the password using MD5
        /// Then Uses the first 8 bytes for the key and the last 8 bytes for the IV
        /// DES uses CBC mode in c#.net padding mode set to zeros
        /// </summary>
        public void setUpEncryption()
        {
            byte[] passphrase = Encoding.ASCII.GetBytes(ppBox.Text);
            myMD5.ComputeHash(passphrase);
            byte[] computedHash = myMD5.Hash;
            byte[] key = new byte[8];
            byte[] IV = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                key[i] = computedHash[i];
                IV[i] = computedHash[i+8];
            }
            myDES.Key = key;
            myDES.IV = IV;
            myDES.Padding = PaddingMode.Zeros;
        }

        public static byte[] EncryptString(SymmetricAlgorithm symAlg, string inString)
        {
            byte[] inBlock = Encoding.ASCII.GetBytes(inString);
            string tempString = Convert.ToBase64String(inBlock);
            inBlock = Convert.FromBase64String(tempString);
            ICryptoTransform xfrm = symAlg.CreateEncryptor();
            byte[] outBlock = xfrm.TransformFinalBlock(inBlock, 0, inBlock.Length);

            return outBlock;
        }

        public static string DecryptBytes(SymmetricAlgorithm symAlg, byte[] inBytes)
        {
            ICryptoTransform xfrm = symAlg.CreateDecryptor();
            byte[] outBlock = xfrm.TransformFinalBlock(inBytes, 0, inBytes.Length);

            return Encoding.ASCII.GetString(outBlock);
        }

        public void UpdateText(string text)//Update the text on textBox1
        {
            if (this.textBox1.InvokeRequired)
            {
                UpdateTextCallback temp = new UpdateTextCallback(UpdateText);
                this.Invoke(temp, new object[] { text });
            }
            else
            {
                
                string str = "";
                //decode and display ciphertext as plain text
                byte[] bytesToDecode = Convert.FromBase64String(text);
                String plainText = DecryptBytes(myDES, bytesToDecode);
                if (radioButton1.Checked == true) str = "\r\nClient said: (@" + DateTime.Now.ToString() + ")\r\n" + plainText;
                else if (radioButton2.Checked == true) str = "\r\nServer said: (@" + DateTime.Now.ToString() + ")\r\n" + plainText;
                textBox1.AppendText(str);
                //display cipher text
                byte[] temp = Convert.FromBase64String(text);
                str = Encoding.ASCII.GetString(temp);
                if (radioButton1.Checked == true) str = "\r\nClient said: (@" + DateTime.Now.ToString() + ")\r\n" + str;
                else if (radioButton2.Checked == true) str = "\r\nServer said: (@" + DateTime.Now.ToString() + ")\r\n" + str;
                textBox8.AppendText(str);

            }
        }
        private bool IsValidIPAddress(string ipaddr)//Validate the input IP address
        {
            try
            {
                IPAddress.Parse(ipaddr);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "IsValidIPAddress Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        private void Reconnect()//Reconnect the Ethernet
        {
            try
            {
                if (button4.InvokeRequired)
                {
                    ReconnectCallback r = new ReconnectCallback(Reconnect);
                    this.Invoke(r, new object[] { });
                }
                else
                {
                    button4_Click(null, null);//Reconnect
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Reconnect failed.  Please restart.\r\n" + e.Message, "Reconnect Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        #endregion

    }

    /// <summary>
    /// A helper class wraps a Socket and an array of byte.
    /// </summary>
    public class KeyValuePair
    {
        public Socket socket;
        public byte[] dataBuffer = new byte[2];
    }
}