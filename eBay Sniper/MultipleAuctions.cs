﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.ListViewItem;

namespace eBay_Sniper
{
    public partial class MultipleAuctions : Form
    {
        List<string> itemIds = new List<string>();
        List<double> maxBids = new List<double>();

        List<string> items = new List<string>();

        List<string> finishedIDs = new List<string>();
        string itemsString = "";

        Point button = new Point(0, 0);

        public MultipleAuctions()
        {
            InitializeComponent();
        }

        public void updateList()
        {
            //Adds new data to a formatted string, which is added to the main list
            string idString = "";
            for (int ic = 0; ic < itemIds.Count; ic++)
            {
                idString += itemIds[ic] + ",";
            }

            string callName;
            if (itemIds.Count < 1)
            {
                callName = "GetSingleItem";
            }
            else
            {
                callName = "GetMultipleItems";
            }

            if (itemIds.Count != 0)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load("http://open.api.ebay.com/shopping?callname=" + callName + "&responseencoding=XML&appid=GregoryM-mailer-PRD-a45ed6035-97c14545&siteid=0&version=967&ItemID=" + idString);
                //Gets item information to find end time

                XmlNodeList nodes;
                if (callName == "GetSingleItem")
                {
                    nodes = doc.GetElementsByTagName("GetSingleItemResponse");
                }
                else
                {
                    nodes = ((XmlElement)doc.GetElementsByTagName("GetMultipleItemsResponse")[0]).GetElementsByTagName("Item");
                }

                items.Clear();
                itemTable.Items.Clear();
                int i = 0;
                foreach (XmlElement item in nodes)
                {
                    string endTime = item.GetElementsByTagName("EndTime")[0].InnerText;
                    string[] components1 = endTime.Split('T');
                    string[] date = components1[0].Split('-');
                    string[] time = components1[1].Split(':');
                    time[2] = time[2].Substring(0, time[2].IndexOf('.'));
                    DateTime endTimeDt = new DateTime(int.Parse(date[0]), int.Parse(date[1]), int.Parse(date[2]), int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2]));
                    endTimeDt = endTimeDt.AddHours(-8);
                    endTimeDt = endTimeDt.AddSeconds(2);

                    DateTime yes = DateTime.Now;
                    yes = yes.AddSeconds(30);

                    string dateTime = endTimeDt.Month + "/" + endTimeDt.Day + "/" + endTimeDt.Year + " " + endTimeDt.Hour + ":" + endTimeDt.Minute + ":" + endTimeDt.Second;
                    //string dateTime = yes.Month + "/" + yes.Day + "/" + yes.Year + " " + yes.Hour + ":" + yes.Minute + ":" + yes.Second;

                    TimeSpan span = endTimeDt - DateTime.Now;
                    //TimeSpan span = yes - DateTime.Now;
                    string timeLeft = span.Days + "d " + span.Hours + "h " + span.Minutes + "m " + span.Seconds + "s";

                    StringBuilder sb = new StringBuilder(item.GetElementsByTagName("Title")[0].InnerText);
                    sb.Replace(',', ' ');
                    string item2 = sb.ToString();

                    //MessageBox.Show(maxBids[i].ToString());
                    items.Add(item2 + "," + item.GetElementsByTagName("ItemID")[0].InnerText + "," + item.GetElementsByTagName("ConvertedCurrentPrice")[0].InnerText + "," + endTimeDt.Year + "," + endTimeDt.Month + "," + endTimeDt.Day + "," + endTimeDt.Hour + "," + endTimeDt.Minute + "," + endTimeDt.Second + "," + maxBids[i]);
                    //items.Add(item2 + "," + item.GetElementsByTagName("ItemID")[0].InnerText + "," + item.GetElementsByTagName("ConvertedCurrentPrice")[0].InnerText + "," + yes.Year + "," + yes.Month + "," + yes.Day + "," + yes.Hour + "," + yes.Minute + "," + yes.Second + "," + maxBid.Text);
                    itemsString += items[items.Count - 1] + ";";

                    ListViewItem itemToAdd = new ListViewItem(new string[] { item2, item.GetElementsByTagName("ItemID")[0].InnerText, item.GetElementsByTagName("ConvertedCurrentPrice")[0].InnerText, dateTime, maxBids[i].ToString() });
                    itemTable.Items.Add(itemToAdd);

                    i++;
                }
            }


            if (itemIds.Count != 0)
                Log("Added item number " + itemIds[itemIds.Count - 1]);
        }

        private void MultipleAuctions_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void addItem_Click(object sender, EventArgs e)
        {
            itemIds.Add(itemNumber.Text);
            maxBids.Add(double.Parse(maxBid.Text));
            updateList();
        }

        bool okayToBid = true;
        string closestID = "";
        string closestSpan = "";
        private void updateTime_Tick(object sender, EventArgs e)
        {
            DateTime closestTime = new DateTime(9999, 12, 31, 23, 59, 59);
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    string[] components = items[i].Split(',');
                    DateTime endTime = new DateTime(int.Parse(components[3]), int.Parse(components[4]), int.Parse(components[5]), int.Parse(components[6]), int.Parse(components[7]), int.Parse(components[8]));

                    TimeSpan span = endTime - DateTime.Now;
                    TimeSpan closest = closestTime - DateTime.Now;

                    if (span < closest)
                    {
                        closestTime = endTime;
                        closestID = items[i].Split(',')[1];
                        closestSpan = span.Days + "d " + span.Hours + "h " + span.Minutes + "m " + span.Seconds + "s";
                        moveItemToTop(items[i]);
                        Log("Set closest item to " + components[1] + " at " + closestSpan);

                        if (span.TotalMinutes < 10)
                        {
                            label1.ForeColor = Color.Red;
                        }
                        else
                        {
                            label1.ForeColor = Color.Black;
                        }

                        if (span.TotalMilliseconds < (double)numericUpDown1.Value)
                        {
                            BidOnItem(components[1], components[9]);
                            itemIds.Remove(components[1]);
                            maxBids.Remove(double.Parse(components[9]));
                            items.Remove(items[i]);
                            RemoveItemFromTable(components[1]);
                            closestTime = new DateTime(9999, 12, 31, 23, 59, 59);
                            closestSpan = null;
                            closestID = "";
                            label1.Text = "Next Upcoming Auction:";
                            label1.ForeColor = Color.Black;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (closestID == "" || closestSpan == "")
            {
                label1.Text = "Next Upcoming Auction:";
                label1.ForeColor = Color.Black;
            }
            else
            {
                label1.Text = "Next Upcoming Auction: " + closestID + " in " + closestSpan;
            }
        }

        public void RemoveItemFromTable(string ID)
        {
            for (int i = 0; i < itemTable.Items.Count; i++)
            {
                if (itemTable.Items[i].SubItems[1].Text == ID)
                {
                    itemTable.Items.Remove(itemTable.Items[i]);
                }
            }
        }

        public void moveItemToTop(string iID)
        {
            //Moves item to the top of the list
            items.Remove(iID);
            items.Insert(0, iID);
        }

        private void checkTimes_Tick(object sender, EventArgs e)
        {

        }

        private void BidOnItem(string ID, string maxBid)
        {
            okayToBid = false;

            //Cancels if the item has been bid on already
            if (finishedIDs.Contains(ID))
                return;

            XmlDocument doc2 = new XmlDocument();
            doc2.Load("http://open.api.ebay.com/shopping?callname=GetSingleItem&responseencoding=XML&appid=GregoryM-mailer-PRD-a45ed6035-97c14545&siteid=0&version=967&ItemID=" + ID + "&IncludeSelector=Details");
            //Gets item information for current price

            string price;

            try
            {
                price = ((XmlElement)doc2.GetElementsByTagName("GetSingleItemResponse")[0]).GetElementsByTagName("MinimumToBid")[0].InnerText;
            }
            catch
            {
                price = ((XmlElement)((XmlElement)doc2.GetElementsByTagName("GetSingleItemResponse")[0]).GetElementsByTagName("Item")[0]).GetElementsByTagName("ConvertedCurrentPrice")[0].InnerText;
            }
            //Finds the minimum price that needs to be bid

            try
            {
                if (double.Parse(price) < double.Parse(maxBid) && !finishedIDs.Contains(ID))
                {
                    //If specified bid is eligible, sends hidden WebBrowser to a confirmation screen
                    string finalPrice = maxBid;
                    
                    ProcessStartInfo startInfo = new ProcessStartInfo("chrome", "https://offer.ebay.com/ws/eBayISAPI.dll?MfcISAPICommand=MakeBid&uiid=1859999246&co_partnerid=2&fb=2&item=" + ID + "&maxbid=" + (double.Parse(maxBid)) + "&Ctn=Continue");
                    startInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    Process.Start(startInfo);

                    Thread.Sleep(1500);

                    Cursor.Position = button;
                    LeftMouseClick(button.X, button.Y);

                    //SendKeys.Send("{ENTER}");
                    finishedIDs.Add(ID);
                    Log("Bid " + String.Format(maxBid, "C") + " on item number " + itemNumber.Text);
                }
                else
                {
                    //Logs failure of auctions whose current prices exceeded bid
                    Log("Bid amount " + String.Format(maxBid, "C") + " exceeded requested bid of " + price + " on item number " + itemNumber.Text);
                    finishedIDs.Add(ID);
                }
            }
            catch
            {
                Log("Exception");
            }

            okayToBid = true;
        }

        //This is a replacement for Cursor.Position in WinForms
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        //This simulates a left mouse click
        public static void LeftMouseClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        private void logIn_Click(object sender, EventArgs e)
        {
            //Opens a sign in screen
            new signIn().ShowDialog();
            //Import.Enabled = true;
            maxBid.Enabled = true;
            itemNumber.Enabled = true;
            //addItem.Enabled = true;
            removeItem.Enabled = true;
            logIn.Enabled = false;
            viewLog.Enabled = true;
            button2.Enabled = true;

            Log("User logged in");
        }

        public void Log(string message)
        {
            //Logs events to a local file depending on date
            string logData = "[" + DateTime.Now.Year + " - " + DateTime.Now.Month + " - " + DateTime.Now.Day + " " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + ":" + DateTime.Now.Second + "] " + message + "\n";

            bool success = false;
            try
            {
                File.AppendAllText(CurrentDatePath(), logData + Environment.NewLine);
            }
            catch
            {
                while (success == false)
                {
                    try
                    {
                        File.AppendAllText(CurrentDatePath(), logData + Environment.NewLine);
                        success = true;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        private string CurrentDatePath()
        {
            return @"Past Logs\auctionlog_" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + ".txt";
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            Log("Browser navigated to " + webBrowser1.Url.ToString());
        }

        private void checkWebpage_Tick(object sender, EventArgs e)
        {
            //try
            //{
            //    //Checks to see if confirmation screen is loaded
            //    if (webBrowser1.Document.GetElementsByTagName("html")[0].InnerHtml.Contains("position:relative;"))
            //    {
            //        try
            //        {
            //            HtmlDocument doc3 = webBrowser1.Document;
            //            HtmlElement head2 = doc3.GetElementsByTagName("html")[0];
            //            HtmlElement s2 = doc3.CreateElement("script");
            //            s2.SetAttribute("text", "function clickButton2() { document.getElementById('but_v4-2').click(); }");
            //            head2.AppendChild(s2);
            //            string html = webBrowser1.Document.GetElementsByTagName("html")[0].InnerHtml;
            //            webBrowser1.Document.InvokeScript("clickButton2");
            //            //Sends confirmation request and redirects the page
            //        }
            //        catch { }
            //    }
            //}
            //catch { }
        }

        private string GetCurrentPrice(string id)
        {
            //Gets current price of an item from the API
            XmlDocument doc2 = new XmlDocument();
            doc2.Load("http://open.api.ebay.com/shopping?callname=GetSingleItem&responseencoding=XML&appid=GregoryM-mailer-PRD-a45ed6035-97c14545&siteid=0&version=967&ItemID=" + id + "&IncludeSelector=Details");
            string price = ((XmlElement)((XmlElement)doc2.GetElementsByTagName("GetSingleItemResponse")[0]).GetElementsByTagName("Item")[0]).GetElementsByTagName("ConvertedCurrentPrice")[0].InnerText;
            return price;
        }

        private void removeItem_Click(object sender, EventArgs e)
        {
            //Removes an item from the list depending on the item number
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Split(',')[1] == itemNumber.Text)
                {
                    for (int q = 0; q < itemTable.Items.Count; q++)
                    {
                        if (itemTable.Items[q].SubItems[1].Text == items[i].Split(',')[1])
                        {
                            itemTable.Items.Remove(itemTable.Items[q]);
                            items.RemoveAt(i);
                            int index = itemIds.IndexOf(itemNumber.Text);
                            itemIds.Remove(itemNumber.Text);
                            maxBids.RemoveAt(index);

                            if (closestID == itemNumber.Text)
                            {
                                label1.Text = "Next Upcoming Auction:";
                                label1.ForeColor = Color.Black;
                            }
                        }
                    }


                    Log("Removed item number " + itemNumber.Text);
                }
            }
        }

        private void Import_Click(object sender, EventArgs e)
        {
            //Imports a spreadsheet of item numbers and bids
            if (openCSV.ShowDialog() == DialogResult.OK)
            {
                //Thread.Sleep(200);

                StreamReader reader = new StreamReader(openCSV.FileName);
                string data = reader.ReadToEnd();

                string[] lines = data.Split('\n');
                lines[0] = "";
                for (int i = 0; i < lines.Length; i++)
                {
                    try
                    {
                        if (lines[i] == "")
                            continue;

                        string[] comp = lines[i].Split(',');
                        itemIds.Add(comp[0]);

                        if (comp[1].Contains("\r"))
                        {
                            maxBids.Add(double.Parse(comp[1].Substring(0, comp[1].Length - 1)));
                        }
                        else
                        {
                            maxBids.Add(double.Parse(comp[1]));
                        }

                        //MessageBox.Show(itemNumber.Text + " " + maxBid.Text);
                    }
                    catch
                    {
                        MessageBox.Show("Failed to add item " + lines[i]);
                        continue; //first line headings
                    }
                }

                itemNumber.Text = "";
                maxBid.Text = "";

                Log("Imported item spreadsheet at " + openCSV.FileName);
            }
        }

        private void removeItem_TextChanged(object sender, EventArgs e)
        {

        }

        private void itemNumber_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {
            
        }

        private void itemNumber_TextChanged(object sender, EventArgs e)
        {
            if (itemNumber.Text == "")
            {
                removeItem.Text = "Clear Items";
            }
            else
            {
                removeItem.Text = "Remove Item";
            }
        }

        private void MultipleAuctions_Load(object sender, EventArgs e)
        {
            //System.Timers.Timer timer = new System.Timers.Timer(2500);

            // Hook up the Elapsed event for the timer.
            //timer.Elapsed += new ElapsedEventHandler(updateTime_Tick);

            //timer.Enabled = true;
        }

        private void viewLog_Click(object sender, EventArgs e)
        {
            //Opens current day's log for viewing
            Process.Start("notepad", CurrentDatePath());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start(@"Past Logs");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (itemNumber.Text == "")
            {
                MessageBox.Show("Please enter a valid active item number.");
            }
            else
            {
                MessageBox.Show("An eBay confirmation page will be opened. Within five seconds, place your cursor over the \"Confirm Bid\" button.");

                try
                {
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo("chrome", "https://offer.ebay.com/ws/eBayISAPI.dll?MfcISAPICommand=MakeBid&uiid=1859999246&co_partnerid=2&fb=2&item=" + itemNumber.Text + "&maxbid=" + (double.Parse(maxBid.Text)) + "&Ctn=Continue");
                    startInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    process.StartInfo = startInfo;
                    process.Start();
                }
                catch
                {
                    MessageBox.Show("Invalid input.");
                }

                Thread.Sleep(5000);

                this.Activate();

                button = Cursor.Position;
                MessageBox.Show("Bid confirmation button placed at " + button.X + ", " + button.Y);

                addItem.Enabled = true;
                Import.Enabled = true;
            }
        }

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private void update(object sender, EventArgs e)
        {
            updateList();
        }
    }
}
