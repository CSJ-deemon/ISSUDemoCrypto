using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace ISSUDemoCrypto
{
    /// <summary>
    /// HandleRecordUI.xaml 的交互逻辑
    /// </summary>
    public partial class HandleRecordUI : UserControl
    {
        public struct dsHandleRecord
        {
            public int Id { set; get; }
            public string HandleType { set; get; }
            public string MacId { set; get; }
            public string CardId { set; get; }
            public string SerialId { set; get; }
            public string Money { set; get; }
            public string Balance { set; get; }
            public string CreateTime { set; get; }
        }
        private int m_OnePageNums = 15;
        private Random m_random;
        public HandleRecordUI()
        {
            InitializeComponent();

            //return;//在anycpu情况下，不能调用sqlite，因sqlite版本不是x64架构;
            HelperDatabase.Init();
            m_random = new Random();
        }

        private void btn_Query_Click(object sender, RoutedEventArgs e)
        {
            string strCardId = tbox_CardId.Text.Trim();
            int nCount = HelperDatabase.QueryRecordCount(strCardId);
            m_OnePageNums = int.Parse(cbox_OnePageNums.Text);
            if (nCount > 0)
            {
                tblk_PageId.Text = "1/" + (nCount + m_OnePageNums - 1) / m_OnePageNums;
                List<dsHandleRecord> lstRecord = new List<dsHandleRecord>();
                if (HelperDatabase.QueryRecord(0, m_OnePageNums, strCardId, ref lstRecord))
                {
                    dgrd_View.ItemsSource = null;
                    dgrd_View.ItemsSource = lstRecord;
                }
            }
        }

        private void btn_Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string strCardId = tbox_CardId.Text.Trim();
                int nCount = HelperDatabase.QueryRecordCount(strCardId);
                if (nCount > 0)
                {
                    tblk_PageId.Text = "1/" + (nCount + m_OnePageNums - 1) / m_OnePageNums;
                    List<dsHandleRecord> lstRecord = new List<dsHandleRecord>();
                    if (HelperDatabase.QueryRecord(0, nCount, strCardId, ref lstRecord))
                    {
                        string[] strLines = new string[lstRecord.Count];
                        for (int i = 0; i < strLines.Length; i++)
                        {
                            strLines[i] = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}",
                                lstRecord[i].Id, lstRecord[i].HandleType, lstRecord[i].MacId,
                                lstRecord[i].CardId, lstRecord[i].SerialId, lstRecord[i].Money,
                                lstRecord[i].Balance, lstRecord[i].CreateTime);
                        }

                        string strFile = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt";
                        File.WriteAllLines(strFile, strLines);
                        MessageBox.Show("导出成功!\r\n" + strFile);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_LastPage_Click(object sender, RoutedEventArgs e)
        {
            string[] strPageIds = tblk_PageId.Text.Split('/');
            int nStart = int.Parse(strPageIds[0]);
            int nStop = int.Parse(strPageIds[1]);

            if (nStart <= 1) return;
            nStart -= 1;
            int nIndex = (nStart - 1) * m_OnePageNums;

            string strCardId = tbox_CardId.Text.Trim();
            tblk_PageId.Text = nStart + "/" + nStop;
            List<dsHandleRecord> lstRecord = new List<dsHandleRecord>();
            if (HelperDatabase.QueryRecord(nIndex, m_OnePageNums, strCardId, ref lstRecord))
            {
                dgrd_View.ItemsSource = null;
                dgrd_View.ItemsSource = lstRecord;
            }
        }

        private void btn_NextPage_Click(object sender, RoutedEventArgs e)
        {
            string[] strPageIds = tblk_PageId.Text.Split('/');
            int nStart = int.Parse(strPageIds[0]);
            int nStop = int.Parse(strPageIds[1]);

            if (nStart >= nStop) return;
            nStart += 1;
            int nIndex = (nStart - 1) * m_OnePageNums;

            string strCardId = tbox_CardId.Text.Trim();
            tblk_PageId.Text = nStart + "/" + nStop;
            List<dsHandleRecord> lstRecord = new List<dsHandleRecord>();
            if (HelperDatabase.QueryRecord(nIndex, m_OnePageNums, strCardId, ref lstRecord))
            {
                dgrd_View.ItemsSource = null;
                dgrd_View.ItemsSource = lstRecord;
            }
        }

        private void btn_JumpPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string[] strPageIds = tblk_PageId.Text.Split('/');
                int nStart = int.Parse(strPageIds[0]);
                int nStop = int.Parse(strPageIds[1]);
                int nPage = int.Parse(tbox_JumpId.Text.Trim());
                if (nPage == 0 || nPage > nStop) return;

                int nIndex = (nPage - 1) * m_OnePageNums;
                string strCardId = tbox_CardId.Text.Trim();
                tblk_PageId.Text = nPage + "/" + nStop;
                List<dsHandleRecord> lstRecord = new List<dsHandleRecord>();
                if (HelperDatabase.QueryRecord(nIndex, m_OnePageNums, strCardId, ref lstRecord))
                {
                    dgrd_View.ItemsSource = null;
                    dgrd_View.ItemsSource = lstRecord;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            dsHandleRecord record = (dsHandleRecord)dgrd_View.SelectedItem;
            if (HelperDatabase.DeleteRecord(record.MacId))
            {
                MessageBox.Show("删除成功!");
            }
        }

        private void btn_Refresh_Click(object sender, RoutedEventArgs e)
        {
            string[] strPageIds = tblk_PageId.Text.Split('/');
            int nStart = int.Parse(strPageIds[0]);
            int nStop = int.Parse(strPageIds[1]);

            int nIndex = (nStart - 1) * m_OnePageNums;
            string strCardId = tbox_CardId.Text.Trim();
            tblk_PageId.Text = nStart + "/" + nStop;
            List<dsHandleRecord> lstRecord = new List<dsHandleRecord>();
            if (HelperDatabase.QueryRecord(nIndex, m_OnePageNums, strCardId, ref lstRecord))
            {
                dgrd_View.ItemsSource = null;
                dgrd_View.ItemsSource = lstRecord;
            }
        }
    }
}
