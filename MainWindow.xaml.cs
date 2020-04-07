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
using System.IO.Ports;
using System.IO;
using System.ComponentModel;

namespace ISSUDemoCrypto
{

    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool IsCheckPowerOk = false;
        private CardInfo gCardInfo = new CardInfo();
        private CardInfo m_BindingCardInfo = new CardInfo();

        public MainWindow()
        {
            InitializeComponent();

            tbox_Prices.IsEnabled = false;

            string[] Ports = SerialPort.GetPortNames();
            for (int i = 0; i < Ports.Length; i++)
            {
                cbox_COM.Items.Add(new ComboBoxItem { Content = Ports[i], IsSelected = i == 0 ? true : false });
            }

            //gbox_CardDataView.Visibility = Visibility.Hidden;
            //gbox_Debug.Visibility = Visibility.Hidden;
            //gbox_Setting.Visibility = Visibility.Hidden;

            //gbox_CardDataView.Visibility = Visibility.Visible;
            //gbox_Debug.Visibility = Visibility.Visible;
            //gbox_Setting.Visibility = Visibility.Visible;
            //cbox_ManagerType.Items.Clear();
        }


        #region 管理卡
        /// <summary>
        /// 充值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_InputMoney_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_BindingCardInfo.CardType != (byte)CardType.Card_Type_Binding_Mother)
                {
                    MessageBox.Show(CardErrorTips.CheckPowerOff);
                    return;
                }

                //写电子钱包;
                string strValue = tbox_InputMoney.Text;
                double Value = Convert.ToDouble(strValue);
                Value = Value * 100;
                UInt32 nValue = (UInt32)Value;
                if (M50CardHelper.AddValue(nValue) == true)
                {
                    //获取卡片信息;
                    CardInfo card = new CardInfo();
                    object obj = (object)card;
                    if (M50CardHelper.GetCardInfo(ref obj) == true)
                    {  
                        int balance = (card.Banlance[0] << 24) + (card.Banlance[1] << 16) + 
                            (card.Banlance[2] << 8) + card.Banlance[3];
                        double dBalance = (double)balance / (double)100;
                        HelperDatabase.AddRecord("充值", card.strId, Encoding.ASCII.GetString(card.UsrId), 
                            Encoding.ASCII.GetString(card.Gens), "+" + strValue, dBalance.ToString("0.00"));
                        UpdateCardInfoUi(card);
                    }
                    MessageBox.Show(CardErrorTips.InputMoneySucc);
                }
                else
                {
                    MessageBox.Show(CardErrorTips.InputMoneyFail);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_OutputMoney_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_BindingCardInfo.CardType != (byte)CardType.Card_Type_Binding_Mother)
                {
                    MessageBox.Show(CardErrorTips.CheckPowerOff);
                    return;
                }

                string strValue = tbox_OutputMoney.Text;
                double Value = Convert.ToDouble(strValue);
                Value = Value * 100;
                UInt32 nValue = (UInt32)Value;
                if (M50CardHelper.SubValue(nValue) == true)
                {
                    //获取卡片信息;
                    CardInfo card = new CardInfo();
                    object obj = (object)card;
                    if (M50CardHelper.GetCardInfo(ref obj) == true)
                    {
                        int balance = (card.Banlance[0] << 24) + (card.Banlance[1] << 16) +
                            (card.Banlance[2] << 8) + card.Banlance[3];
                        double dBalance = (double)balance / (double)100;
                        HelperDatabase.AddRecord("退款", card.strId, Encoding.ASCII.GetString(card.UsrId),
                            Encoding.ASCII.GetString(card.Gens), "-" + strValue, dBalance.ToString("0.00"));

                        UpdateCardInfoUi(card);
                    }

                    MessageBox.Show(CardErrorTips.OutputMoneySucc);
                }
                else
                {
                    MessageBox.Show(CardErrorTips.OutputMoneyFail);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 建卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_CreateCard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                CardInfo card = new CardInfo();
                if(ReadCardCheck(out card) == false)
                {
                    UpdateCardInfoUi(null);
                    return;
                }

                if(gCardInfo.CardType != (byte)CardType.Card_Type_Binding_Mother)
                {
                    MessageBox.Show(CardErrorTips.CheckPowerOff);
                        return ;
                }

                if (string.IsNullOrEmpty(tbox_UsrId.Text) == true ||
                        tbox_UsrId.Text.Length != 8)
                {
                    throw new Exception(CardErrorTips.PassWordLengthError);
                }
                if (
                    string.IsNullOrEmpty(tbox_UsrId.Text) ||
                    string.IsNullOrEmpty(tbox_Banlance.Text) ||
                    string.IsNullOrEmpty(tbox_StopCarPrice.Text) ||
                    string.IsNullOrEmpty(tbox_ElectmentPrice.Text))
                {
                    throw new Exception(CardErrorTips.TextNull);
                }
                byte[] gene = m_BindingCardInfo.Gens;
                card.Gens = gene;
                card.CardType = (byte)CardType.Card_Type_Gengeral;
                card.UsrType = (byte)UserType.User_Type_FaceValue; //0,面值卡 1 充值卡 2联网卡
                card.Acount = (byte)cbox_Acount.SelectedIndex;
                card.IsPwd = (byte)PassworldType.Psw_Type_UnUse;
                card.IsLock = (byte)LockType.Lock_Type_UnLock;
                card.UsrId = Encoding.ASCII.GetBytes(tbox_UsrId.Text.Trim());
                card.PaymentFlag = (byte)PaymentType.Payment_Type_Use;

                string strValue = tbox_Banlance.Text.Trim();
                double Value = Convert.ToDouble(strValue);
                Value = Value * 100;
                int nValue = (int)Value;
                card.Banlance[0] = (byte)(nValue >> 24);
                card.Banlance[1] = (byte)(nValue >> 16);
                card.Banlance[2] = (byte)(nValue >> 8);
                card.Banlance[3] = (byte)nValue;

                UInt32 nPwd = Convert.ToUInt32(0);
                card.UsrPwd[0] = (byte)(nPwd >> 24);
                card.UsrPwd[1] = (byte)(nPwd >> 16);
                card.UsrPwd[2] = (byte)(nPwd >> 8);
                card.UsrPwd[3] = (byte)(nPwd);

                strValue = tbox_StopCarPrice.Text.Trim();
                Value = Convert.ToDouble(strValue);
                Value = Value * 100;
                nValue = (int)Value;
                card.StopCarPrice[0] = (byte)(nValue >> 8);
                card.StopCarPrice[1] = (byte)Value;


                strValue = tbox_ElectmentPrice.Text.Trim();
                Value = Convert.ToDouble(strValue);
                Value = Value * 100;
                nValue = (int)Value;
                card.ElePrice[0] = (byte)(nValue >> 8);
                card.ElePrice[1] = (byte)nValue;

                //====================
                if (M50CardHelper.NewCard(card) == true)
                {
                    MakeCardErrorTip(true, card);

                    HelperDatabase.AddRecord("新建", card.strId, tbox_UsrId.Text.Trim(),
                            Encoding.ASCII.GetString(card.Gens), "0", tbox_Banlance.Text.Trim());

                    UpdateCardInfoUi(card);
                }
                else
                {
                    MakeCardErrorTip(false, card);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 销卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_DeleteCard_Click(object sender, RoutedEventArgs e)
        {
            CardInfo card = new CardInfo();
            object obj = (object)card;
            if (M50CardHelper.GetCardInfo(ref obj) == false)
            {
                MessageBox.Show(CardErrorTips.ReadFail);
                return;
            }

            if (card.CardType != (byte)CardType.Card_Type_Gengeral)
            {
                MessageBox.Show("操作对象错误!");
                return;
            }

            if (gCardInfo.CardType != (byte)CardType.Card_Type_Admin &&
                gCardInfo.CardType != (byte)CardType.Card_Type_Binding_Mother)
            {
                MessageBox.Show("权限错误！此操作需要管理卡!");
                return;
            }
            int nValue = card.Banlance[0];
            nValue <<= 8;
            nValue += card.Banlance[1];
            nValue <<= 8;
            nValue += card.Banlance[2];             
            nValue <<= 8;
            nValue += card.Banlance[3];
            double dValue = (double)nValue / (double)100;

            MessageBoxResult mbr = MessageBox.Show(string.Format("确定要销卡吗,\r\n卡上当前余额:{0}",
                dValue.ToString("0.00")), "提示", MessageBoxButton.OKCancel);
            if (mbr == MessageBoxResult.OK)
            {
                if (M50CardHelper.InitCard() == true)
                    MessageBox.Show("销卡成功!");
                else MessageBox.Show("销卡失败!");
            }
        }

        private bool ReadCardCheck(out CardInfo outCard)
        {
            outCard = new CardInfo();
            object obj = (object)outCard;
            if (M50CardHelper.GetCardInfo(ref obj) == false)
            {
                if (M50CardHelper.GetComOpen == false)
                {
                    MessageBox.Show(CardErrorTips.OpenComFail);
                    return false;
                }

                MessageBox.Show(CardErrorTips.ReadFail);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 读卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ReadCard_Click(object sender, RoutedEventArgs e)
        {
            CardInfo card = new CardInfo();
            if(ReadCardCheck(out card) ==false)
            {
                UpdateCardInfoUi(null);
                return;
            }

            UpdateCardInfoUi(card);
        }
        #endregion

        private void btn_CheckPower_Click(object sender, RoutedEventArgs e)
        {
            object obj = (object)gCardInfo;
            if (M50CardHelper.GetCardInfo(ref obj) == true)
            {
                CheckPowerRefreshUi(gCardInfo);
            }
            else
            {
                tbox_TipCard.Content = CardErrorTips.CheckPowerFail;
                MessageBox.Show(CardErrorTips.CheckPowerFail);
            }
        }

        private void tbox_ReadAdd_Click(object sender, RoutedEventArgs e)
        {
            //tbox_CardData.Text = "";

            byte[] Datas0 = new byte[64];
            if (M50CardHelper.ReadData(0x01, ref Datas0) == false)
                return;


            byte[] Datas1 = new byte[64];
            if (M50CardHelper.ReadData(0x02, ref Datas1) == false)
                return;

            string strTemp = "";
            for (byte i = 0; i < 64; i++)
            {
                if ((i % 16) == 0 && i != 0)
                    strTemp += "\r\n";
                else if ((i % 4) == 0 && i != 0)
                    strTemp += " ";
                strTemp += Datas0[i].ToString("X2") + " ";
            }
           // tbox_CardData.Text = strTemp;

            strTemp = "\r\n\r\n";
            for (byte i = 0; i < 64; i++)
            {
                if ((i % 16) == 0 && i != 0)
                    strTemp += "\r\n";
                else if ((i % 4) == 0 && i != 0)
                    strTemp += " ";
                strTemp += Datas1[i].ToString("X2") + " ";
            }
            //tbox_CardData.Text += strTemp;

            string strCardType = GetCardType(Datas0[0]);
            string strGene = Encoding.ASCII.GetString(Datas0, 1, 8);
            string strAccoutFlag = Datas0[9].ToString("X2") + (Datas0[9] == 1 ? " 计费" : " 不计费");

            strTemp = "未知";
            if (Datas0[10] == 0)
                strTemp = "面值卡";
            else if (Datas0[10] == 1)
                strTemp = "充值卡";
            else if (Datas0[10] == 2)
                strTemp = "联网卡";
            string strUesFlag = Datas0[10].ToString("X2") + " " + strTemp;

            string strLockFlag = Datas0[11].ToString("X2") + (Datas0[11] == 1 ? " 上锁" : " 解锁");
            string strPwdFlag = Datas0[12].ToString("X2") + (Datas0[12] == 1 ? " 启用" : " 不启用");
            string strPaymentFlag = Datas0[13].ToString("X2") + (Datas0[13] == 1 ? " 交易开始" : " 交易结束");
            string strUsrId = Encoding.ASCII.GetString(Datas0, 0 + 16, 8);
            if (Datas0[0] == (byte)CardType.Card_Type_Setting)
            {
                strAccoutFlag = Datas0[9].ToString("X2") + (Datas0[9] == 1 ? " 双路" : " 单路");
                strUesFlag = Datas0[10].ToString("X2") + (Datas0[10] == 1 ? " 即插即充" : " RFID");
            }

            UInt32 nValue = 0;
            nValue = Datas0[8 + 16];
            nValue <<= 8;
            nValue += Datas0[9 + 16];
            nValue <<= 8;
            nValue += Datas0[10 + 16];
            nValue <<= 8;
            nValue += Datas0[11 + 16];
            string strPwd = nValue.ToString();


            nValue = Datas0[12 + 16];
            nValue <<= 8;
            nValue += Datas0[13 + 16];
            double dValue = (double)nValue / (double)100;
            string strPrices = Datas0[12 + 16].ToString("X2") + " " +
                Datas0[13 + 16].ToString("X2") + ", " + dValue.ToString("0.00");

            nValue = Datas0[14 + 16];
            nValue <<= 8;
            nValue += Datas0[15 + 16];
            dValue = (double)nValue / (double)100;
            string strStopPrices = Datas0[14 + 16].ToString("X2") + " " +
                Datas0[15 + 16].ToString("X2") + ", " + dValue.ToString("0.00");

            nValue = Datas1[3];
            nValue <<= 8;
            nValue += Datas1[2];
            nValue <<= 8;
            nValue += Datas1[1];
            nValue <<= 8;
            nValue += Datas1[0];
            dValue = (double)nValue / (double)100;
            string strBanlance = Datas1[0].ToString("X2") + " " + Datas1[1].ToString("X2") + " " +
                Datas1[2].ToString("X2") + " " + Datas1[3].ToString("X2") + ", " + dValue.ToString("0.00");

            nValue = Datas1[16 + 0];
            nValue <<= 8;
            nValue += Datas1[16 + 1];
            nValue <<= 8;
            nValue += Datas1[16 + 2];
            nValue <<= 8;
            nValue += Datas1[16 + 3];
            string strStartTime = TicksToTime(nValue);

            string strDevId = Encoding.ASCII.GetString(Datas1, 16 + 4, 8);
            strTemp = string.Format("\r\n==========================================\r\n卡类型：{0},\r\n基因:{1},\r\n计费类型:{2},\r\n使用类型:{3},\r\n锁卡标志:{4},\r\n密码标志:{5},\r\n交易标志:{6},\r\n用户编号:{7},\r\n用户密码:{8},\r\n电价:{9},\r\n停车场:{10},\r\n余额:{11},\r\n启动时间:{12},\r\n设备编号:{13},\r\n",
                strCardType, strGene, strAccoutFlag, strUesFlag, strLockFlag, strPwdFlag, strPaymentFlag, strUsrId, strPwd, strPrices, strStopPrices, strBanlance, strStartTime, strDevId);

            //tbox_CardData.Text += strTemp;
        }

        private void tbox_CleanAll_Click(object sender, RoutedEventArgs e)
        {
            //tbox_CardData.Text = "";
        }

        private void tblk_ManagerReadCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //CardInfo card = new CardInfo();
            //object obj = (object)card;
            //if (M50CardHelper.GetCardInfo(ref obj) == false)
            //{
            //    tblk_ManagerTip.Text = "读卡失败！ 未识别此卡";
            //    rdbtn_CardPwd.IsChecked = true;
            //    tbox_Gene.Text = "";
            //    tbox_CardType.SelectedIndex = -1;
            //    return;
            //}

            //tbox_ManagerGene.Text = Encoding.ASCII.GetString(card.Gens, 0, 8);
            //tblk_ManagerCardId.Text = card.strId;

            //if (card.CardType < (byte)CardType.Card_Type_Binding_Mother)
            //    rdbtn_CardPwd.IsChecked = true;
            //else rdbtn_CardBinding.IsChecked = true;
        }

        private void btn_ManagerCreate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //if (string.IsNullOrEmpty(tbox_ManagerGene.Text) == true || tbox_ManagerGene.Text.Length !=8)
                //{
                //    throw new Exception("基因输入错误！\r\n不能为空，且只能为8个长度!");
                //}
                //byte[] genes = Encoding.ASCII.GetBytes(tbox_ManagerGene.Text.Trim());

                if (IsCheckPowerOk == false)
                    throw new Exception(CardErrorTips.CheckPowerFail);

                CardInfo cardinfo = new CardInfo();
                object obj = (object)cardinfo;
                if (M50CardHelper.GetCardInfo(ref obj) == false)
                    throw new Exception(CardErrorTips.ReadFail);

                byte type = GetUsrCreateCardType();
                if (type == 0xff) return;

                CardInfo ci = (CardInfo)obj;
                if (ci.Gens[0] != 'H' || ci.Gens[1] != 'D')
                    throw new Exception(CardErrorTips.ReadFail);

                CardInfo newCard = new CardInfo();
                newCard.CardType = type;
                //genes.CopyTo(newCard.Gens, 0);
                //if (type == (byte)CardType.Card_Type_Super_Admin ||
                //    type == (byte)CardType.Card_Type_Binding_Super)
                //{
                //    Array.Copy(gCardInfo.Gens, newCard.Gens, 2);
                //    newCard.Gens[2] = genes[0];
                //    newCard.Gens[3] = genes[1];
                //}
                //if (type == (byte)CardType.Card_Type_Binding_Admin ||
                //    type == (byte)CardType.Card_Type_Admin)
                //{
                //    Array.Copy(gCardInfo.Gens, newCard.Gens, 4);
                //    newCard.Gens[4] = genes[0];
                //    newCard.Gens[5] = genes[1];
                //}
                //if (type == (byte)CardType.Card_Type_Binding_Usr)
                //{
                //    Array.Copy(gCardInfo.Gens, newCard.Gens, 6);
                //    newCard.Gens[6] = genes[0];
                //    newCard.Gens[7] = genes[1];
                //}

                if (M50CardHelper.NewCard(newCard) == true)
                    throw new Exception(string.Format(CardErrorTips.Mat_MakeSucc,((CardType)cardinfo.CardType).GetDescription()));
                else throw new Exception(string.Format(CardErrorTips.Mat_MakeFail, ((CardType)cardinfo.CardType).GetDescription()));

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "提示");
            }
        }

        private byte GetUsrCreateCardType()
        {
            //bool isPwdCard = (bool)rdbtn_CardPwd.IsChecked;
            byte type = 0xff;
            //if (cbox_ManagerType.Text.Equals("超级管理员卡"))//创建超级管理员卡;
            //{
            //    type = isPwdCard ? (byte)CardType.Card_Type_Super_Admin : (byte)CardType.Card_Type_Binding_Super;
            //}
            //else if (cbox_ManagerType.Text.Equals("管理员卡"))//创建管理员卡;
            //{
            //    type = isPwdCard ? (byte)CardType.Card_Type_Admin : (byte)CardType.Card_Type_Binding_Admin;
            //}
            //else if (cbox_ManagerType.Text.Equals("用户卡"))//创建空白卡;
            //{
            //    if (isPwdCard == false)//用户卡
            //    {
            //        type = (byte)CardType.Card_Type_Binding_Usr;
            //    }
            //}
            return type;
        }

        private void btn_ManagerDelete_Click(object sender, RoutedEventArgs e)
        {
            CardInfo card = new CardInfo();
            object obj = (object)card;
            if (M50CardHelper.GetCardInfo(ref obj) == false)
                return;

            tblk_CardId.Text = card.strId;
            if (card.Gens[0] != 'H' || card.Gens[1] != 'D')
            {
                MessageBox.Show(CardErrorTips.ReadFail);
                return;
            }

            if (MessageBox.Show("是否要销除此卡?", "提示", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                CardInfo newcard = new CardInfo();
                newcard.Gens[0] = (byte)'H';
                newcard.Gens[1] = (byte)'D';
                newcard.CardType = (byte)CardType.Card_Type_Empty;
                if (M50CardHelper.NewCard(newcard) == true)
                {
                    MessageBox.Show("销卡成功!");
                }
                else
                {
                    MessageBox.Show("销卡失败!");
                }
            }
        }

        private void cbox_COM_DropDownOpened(object sender, EventArgs e)
        {
            cbox_COM.Items.Clear();
            string[] Ports = SerialPort.GetPortNames();
            for (int i = 0; i < Ports.Length; i++)
            {
                cbox_COM.Items.Add(new ComboBoxItem { Content = Ports[i], IsSelected = i == 0 ? true : false });
            }
        }

        private void btn_OpenCom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btn_CheckPower.IsEnabled = false;
                string strContent = btn_OpenCom.Content.ToString();
                if (strContent == "打开串口")
                {
                    btn_CheckPower.IsEnabled = true;
                    bool isInitOk = M50CardHelper.Init(cbox_COM.Text, Convert.ToInt32(cbox_Bandrate.Text));
                    if (isInitOk == true)
                    {
                        btn_OpenCom.Foreground = Brushes.Red;
                        btn_OpenCom.Content = "关闭串口";
                    }
                    else
                    {
                        MessageBox.Show("打开失败!");
                        return;
                    }
                }
                else
                {
                    M50CardHelper.Close();
                    btn_OpenCom.Foreground = Brushes.Black;
                    btn_OpenCom.Content = "打开串口";
                }
                ChangeTipsCard();
            }
            catch (System.Exception ex)
            {
            }
        }

        //========================================================================
        private static byte[] strToToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        public static string byteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
        }

        public static string GetCardType(byte type)
        {
            if (type == (byte)CardType.Card_Type_Empty)
                return (type.ToString("X2") + " 空白卡");
            else if (type == (byte)CardType.Card_Type_Mother)
                return (type.ToString("X2") + " 母卡");
            else if (type == (byte)CardType.Card_Type_Super_Admin)
                return (type.ToString("X2") + " 超级管理员卡");
            else if (type == (byte)CardType.Card_Type_Setting)
                return (type.ToString("X2") + "配置卡");
            else if (type == (byte)CardType.Card_Type_Admin)
                return (type.ToString("X2") + " 管理员卡");
            else if (type == (byte)CardType.Card_Type_Gengeral)
                return (type.ToString("X2") + " 普通卡");
            else if (type == (byte)CardType.Card_Type_Binding_Mother)
                return (type.ToString("X2") + " 绑定母卡");
            else if (type == (byte)CardType.Card_Type_Binding_Super)
                return (type.ToString("X2") + " 绑定超级管理员卡");
            else if (type == (byte)CardType.Card_Type_Binding_Admin)
                return (type.ToString("X2") + " 绑定管理员卡");
            else if (type == (byte)CardType.Card_Type_Binding_Usr)
                return (type.ToString("X2") + " 绑定用户卡");
            else return (type.ToString("X2") + " 未知");
        }
        /*
         * 验证
         * 老版:通过建立母卡->超级管理员->管理员->普通卡
         * 新版:通过绑定卡进行验证->普通卡 
         */
        public void CheckPowerRefreshUi(CardInfo card)
        {
            IsCheckPowerOk = false;

            string strResult = "";
            if (card.CardType == (byte)CardType.Card_Type_Mother)
            {
                strResult = "成功, [母卡]";
                IsCheckPowerOk = true;
            }
            else if (card.CardType == (byte)CardType.Card_Type_Super_Admin)
            {
                strResult = "成功, [超级管理员卡]";
                IsCheckPowerOk = true;
            }
            else if (card.CardType == (byte)CardType.Card_Type_Admin)
            {
                strResult = "成功, [管理员卡]";
                IsCheckPowerOk = true;
            }
            else if (card.CardType == (byte)CardType.Card_Type_Binding_Mother)
            {
                strResult = "验证成功";
                IsCheckPowerOk = true;
                m_BindingCardInfo = card;
            }
            else if (card.CardType == (byte)CardType.Card_Type_Binding_Super)
            {
                strResult = "成功, [绑定超级管理员卡]";
                IsCheckPowerOk = true;
            }
            else if (card.CardType == (byte)CardType.Card_Type_Binding_Admin)
            {
                strResult = "成功, [绑定管理员卡]";
                IsCheckPowerOk = true;
            }
            else
            {
                gCardInfo = new CardInfo();
                m_BindingCardInfo = new CardInfo();
                strResult = CardErrorTips.CheckPowerFail;
            }
            tbox_TipCard.Content = strResult;
        }

        private string TicksToTime(UInt32 Ticks)
        {
            DateTime dtBase = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime convertTime = dtBase.Add(new TimeSpan(Ticks * TimeSpan.TicksPerSecond)).ToLocalTime();

            return convertTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void btn_Setting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               
                CardInfo card = new CardInfo();
                if(ReadCardCheck(out card) == false)
                {
                    return;
                }

                if (m_BindingCardInfo.CardType != (byte)CardType.Card_Type_Binding_Mother)
                {
                    MessageBox.Show(CardErrorTips.CheckPowerOff);
                    return;
                }

                tblk_CardId.Text = card.strId;

                CardInfo newCard = new CardInfo();
                newCard.Gens = m_BindingCardInfo.Gens;
                newCard.CardType = (byte)CardType.Card_Type_Setting;
                newCard.Acount = Convert.ToByte(cbox_ChargerChannel.SelectedIndex);
                newCard.UsrType = Convert.ToByte(cbox_ChargerMode.SelectedIndex);
                if (M50CardHelper.NewCard(newCard) == true)
                {
                    MessageBox.Show(string.Format(CardErrorTips.Mat_MakeSucc,((CardType)card.CardType).GetDescription()));
                }
                else
                {
                    MessageBox.Show(string.Format(CardErrorTips.Mat_MakeFail, ((CardType)card.CardType).GetDescription()));
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_CreateMother_Click(object sender, RoutedEventArgs e)
        {
            CardInfo newCard = new CardInfo();
            newCard.Gens[0] = (byte)'H';
            newCard.Gens[1] = (byte)'D';
            newCard.CardType = (byte)CardType.Card_Type_Mother;
            if (M50CardHelper.NewCard(newCard) == true)
            {
                MessageBox.Show(string.Format(CardErrorTips.Mat_MakeSucc, ((CardType)newCard.CardType).GetDescription()));
            }
            else
            {
                MessageBox.Show(string.Format(CardErrorTips.Mat_MakeFail, ((CardType)newCard.CardType).GetDescription()));
            }
        }

        private void btn_CreateEmpty_Click(object sender, RoutedEventArgs e)
        {
            CardInfo newCard = new CardInfo();
            newCard.Gens[0] = (byte)'H';
            newCard.Gens[1] = (byte)'D';
            newCard.CardType = (byte)CardType.Card_Type_Empty;
            if (M50CardHelper.NewCard(newCard) == true)
            {
                MessageBox.Show(string.Format(CardErrorTips.Mat_MakeSucc, ((CardType)newCard.CardType).GetDescription()));
            }
            else
            {
                MessageBox.Show(string.Format(CardErrorTips.Mat_MakeFail, ((CardType)newCard.CardType).GetDescription()));
            }
        }

        private void btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (M50CardHelper.InitCard() == true)
            {
                MessageBox.Show("销卡成功!");
            }
            else
            {
                MessageBox.Show("销卡失败!");
            }
        }

        private void btn_CreateBindingMother_Click(object sender, RoutedEventArgs e)
        {
            CardInfo newCard = new CardInfo();
            newCard.Gens[0] = (byte)'H';
            newCard.Gens[1] = (byte)'D';
            newCard.CardType = (byte)CardType.Card_Type_Binding_Mother;
            if (M50CardHelper.NewCard(newCard) == true)
            {
                MessageBox.Show(string.Format(CardErrorTips.Mat_MakeSucc, ((CardType)newCard.CardType).GetDescription()));
            }
            else
            {
                MessageBox.Show(string.Format(CardErrorTips.Mat_MakeFail, ((CardType)newCard.CardType).GetDescription()));
            }
        }

        private void btn_SettingSrv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CardInfo card = new CardInfo();
                if (ReadCardCheck(out card) == false)
                {
                    return;
                }
                tblk_CardId.Text = card.strId;
                //if (card.Gens[0] != 'H' || card.Gens[1] != 'D')
                //{
                //    MessageBox.Show("未识别此卡，读卡失败!", "提示");
                //    return;
                //}

                string[] strParam = tbox_SrvAddress.Text.Split(':');
                string[] strIp = strParam[0].Split('.');
                byte[] byServerIp = new byte[4];
                for (byte i = 0; i < 4; i++)
                {
                    byServerIp[i] = Convert.ToByte(strIp[i]);
                }

                UInt16 port = Convert.ToUInt16(strParam[1]);
                byte[] byServerPort = new byte[2];
                byServerPort[0] = (byte)(port);
                byServerPort[1] = (byte)(port >> 8);


                CardInfo newCard = new CardInfo();
                //newCard.Gens[0] = (byte)'H';
                //newCard.Gens[1] = (byte)'D';
                newCard.CardType = (byte)CardType.Card_Type_Setting_Ip;
                newCard.Param[0] = byServerIp[0];
                newCard.Param[1] = byServerIp[1];
                newCard.Param[2] = byServerIp[2];
                newCard.Param[3] = byServerIp[3];
                newCard.Param[4] = byServerPort[0];
                newCard.Param[5] = byServerPort[1];
                if (M50CardHelper.NewCard(newCard) == true)
                {
                    MessageBox.Show(string.Format(CardErrorTips.Mat_MakeSucc, ((CardType)newCard.CardType).GetDescription()));
                }
                else
                {
                    MessageBox.Show(string.Format(CardErrorTips.Mat_MakeFail, ((CardType)newCard.CardType).GetDescription()));
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_SettingId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               
                CardInfo card = new CardInfo();
                if (ReadCardCheck(out card) == false)
                {
                    return;
                }

                tblk_CardId.Text = card.strId;
                //if (card.Gens[0] != 'H' || card.Gens[1] != 'D')
                //{
                //    MessageBox.Show("未识别此卡，读卡失败!", "提示");
                //    return;
                //}

                byte id = 0;
                if (cbox_Channel.Text == "A")
                    id = 1;
                else id = 0;
                byte[] chargeId = Encoding.ASCII.GetBytes(tbox_ChargeId.Text.Substring(0, 8));

                CardInfo newCard = new CardInfo();
                //newCard.Gens[0] = (byte)'H';
                //newCard.Gens[1] = (byte)'D';
                newCard.CardType = (byte)CardType.Card_Type_Setting_Addr;
                newCard.Param[0] = id;
                for (byte i = 0; i < 8; i++)
                {
                    newCard.Param[1 + i] = chargeId[i];
                }
                if (M50CardHelper.NewCard(newCard) == true)
                {
                    MakeCardErrorTip(true, card);
                }
                else
                {
                    MakeCardErrorTip(false, card);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_SettingCommType_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               
                CardInfo card = new CardInfo();
                if (ReadCardCheck(out card) == false)
                {
                    return;
                }

                tblk_CardId.Text = card.strId;
                //if (card.Gens[0] != 'H' || card.Gens[1] != 'D')
                //{
                //    MessageBox.Show("未识别此卡，读卡失败!", "提示");
                //    return;
                //}

                byte btCommType = (byte)cbox_CommType.SelectedIndex;


                CardInfo newCard = new CardInfo();
                //newCard.Gens[0] = (byte)'H';
                //newCard.Gens[1] = (byte)'D';
                newCard.CardType = (byte)CardType.Card_Type_Setting_CommType;
                newCard.Param[0] = btCommType;
                if (M50CardHelper.NewCard(newCard) == true)
                {
                    MakeCardErrorTip(true, card);
                }
                else
                {
                    MakeCardErrorTip(false, card);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btn_SettingPrices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               
                CardInfo card = new CardInfo();
                if (ReadCardCheck(out card) == false)
                {
                    return;
                }

                tblk_CardId.Text = card.strId;
                //if (card.Gens[0] != 'H' || card.Gens[1] != 'D')
                //{
                //    MessageBox.Show("未识别此卡，读卡失败!", "提示");
                //    return;
                //}

                byte[] byCard = new byte[3];
                if (cbox_PricesType.SelectedIndex == 0)
                {
                    byCard[0] = 0;
                    byCard[1] = 0;
                    byCard[2] = 0;
                }
                else if (cbox_PricesType.SelectedIndex == 1)
                {
                    double price = Convert.ToDouble(tbox_Prices.Text);
                    UInt16 nPrice = (UInt16)(price * 100);
                    byCard[0] = 1;
                    byCard[1] = (byte)(nPrice >> 8);
                    byCard[2] = (byte)(nPrice);
                }

                CardInfo newCard = new CardInfo();
                //newCard.Gens[0] = (byte)'H';
                //newCard.Gens[1] = (byte)'D';
                newCard.CardType = (byte)CardType.Card_Type_Setting_Price;
                newCard.Param[0] = byCard[0];
                newCard.Param[1] = byCard[1];
                newCard.Param[2] = byCard[2];

                if (M50CardHelper.NewCard(newCard) == true)
                {
                    MakeCardErrorTip(true, newCard);
                }
                else
                {
                    MakeCardErrorTip(false, newCard);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cbox_PricesType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                string strType = cbox_PricesType.Text;
                if (strType.Equals("桩电价"))
                {
                    tbox_Prices.IsEnabled = false;
                }
                else
                {
                    tbox_Prices.IsEnabled = true;
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        private void btn_AddPwd_Click(object sender, RoutedEventArgs e)
        {
            //KEYA=C2F1BC6CCE28;
            //KEYB=A3D82230900D;
            bool bRet = M50CardHelper.SetPassword(7);
            if (bRet)
            {
                bRet = M50CardHelper.SetPassword(7 + 4);
                bRet = M50CardHelper.SetPassword(7 + 8);
                if (bRet)
                {
                    MessageBox.Show(CardErrorTips.ChangePassWordSucc);
                    return;
                }
            }

            MessageBox.Show(CardErrorTips.ChangePassWordFail);
        }

       

        private void btn_SettingType0_Click(object sender, RoutedEventArgs e)
        {
            
            CardInfo card = new CardInfo();
            if (ReadCardCheck(out card) == false)
            {
                return;
            }

            if(m_BindingCardInfo.CardType != (byte)CardType.Card_Type_Binding_Mother)
            {
                MessageBox.Show(CardErrorTips.CheckPowerOff);
                return;
            }

            tblk_CardId.Text = card.strId;

            
            CardInfo newCard = new CardInfo();
            newCard.Gens = m_BindingCardInfo.Gens;
            string strText = cbox_CardType0.Text;
            if (strText == "启停卡")
            {
                newCard.CardType = (byte)CardType.Card_Type_StartStop;
            }
 			//else if (strText == "16A配置")
            //{
            //    newCard.CardType = (byte)CardType.Card_Type_StartStop + 2;
            //}
            //else if (strText == "32A配置")
            //{
            //    newCard.CardType = (byte)CardType.Card_Type_StartStop + 3;
            //}
            //else if (strText == "灯带配置卡")
            //{
            //    newCard.CardType = (byte)CardType.Card_Type_StartStop + 4;
            //}
            //else if (strText == "即插即充配置卡");
            //{
            //    newCard.CardType = (byte)CardType.Card_Type_StartStop + 5;
            //}
            //else if (strText == "RFID/APP配置卡")
            //{
            //    newCard.CardType = (byte)CardType.Card_Type_StartStop + 6;
            //}

            ////<ComboBoxItem >16A配置卡</ComboBoxItem>
            ////<ComboBoxItem >32A配置卡</ComboBoxItem>
            ////<ComboBoxItem >灯带配置卡</ComboBoxItem>
            ////<ComboBoxItem >LED配置卡</ComboBoxItem>
            ////<ComboBoxItem >即插即充配置卡</ComboBoxItem>
            ////<ComboBoxItem >RFID/APP配置卡</ComboBoxItem>
            ////<ComboBoxItem >以太网配置卡</ComboBoxItem>
            ////<ComboBoxItem >2G通信配置卡</ComboBoxItem>

            if (M50CardHelper.NewCard(newCard) == true)
            {
                MakeCardErrorTip(true, newCard);
            }
            else
            {
                MakeCardErrorTip(false, newCard);
            }
        }

        private void btn_CleanPwd_Click(object sender, RoutedEventArgs e)
        {
            bool bRet = M50CardHelper.CleanPassword(7);
            if (bRet)
            {
                bRet = M50CardHelper.CleanPassword(7 + 4);
                bRet = M50CardHelper.CleanPassword(7 + 8);
                if (bRet)
                {
                    MessageBox.Show(CardErrorTips.CleanPassWordSucc);
                    return;
                }
            }

            MessageBox.Show(CardErrorTips.CleanPassWordFail);
        }
        
        private void cbox_EmMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ComboBoxItem cbi = (ComboBoxItem)cbox_EmMode.SelectedItem;
                int text = cbox_EmMode.SelectedIndex;
                string strText = (string)cbi.Content;
                if (strText == "加密版")
                {
                    byte[] KeyA = new byte[] { 0xC2, 0xF1, 0xBC, 0x6C, 0xCE, 0x28 };
                    byte[] KeyB = new byte[] { 0xA3, 0xD8, 0x22, 0x30, 0x90, 0x0D };
                    for (int i = 0; i < KeyA.Length; i++)
                    {
                        M50CardHelper.DefaultPwdA[i] = KeyA[i];
                        M50CardHelper.DefaultPwdB[i] = KeyB[i];
                    }
                }
                else if (strText == "普通版")
                {
                    byte[] KeyA = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
                    byte[] KeyB = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
                    for (int i = 0; i < KeyA.Length; i++)
                    {
                        M50CardHelper.DefaultPwdA[i] = KeyA[i];
                        M50CardHelper.DefaultPwdB[i] = KeyB[i];
                    }
                }
            }
            catch (System.Exception ex)
            {
            	
            }
        }


        private void btn_CreateBanding_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CardInfo card = new CardInfo();
                if (ReadCardCheck(out card) == false)
                {
                    return;
                }

                string strBandingGene = tbox_BandingGene.Text.Trim();
                if (strBandingGene.Length != 8)
                {
                    MessageBox.Show(CardErrorTips.PassWordLengthError);
                    return;
                }

                byte[] genes = Encoding.ASCII.GetBytes(strBandingGene);
                CardInfo newCard = new CardInfo();
                newCard.CardType = (byte)CardType.Card_Type_Binding_Mother;
                Array.Copy(genes, newCard.Gens, 8);
                if (M50CardHelper.NewCard(newCard) == true)
                {
                    MakeCardErrorTip(true, newCard);
                    tbox_BandingGene.Text = "";
                    return;
                }
                MakeCardErrorTip(false, newCard);
            }
            catch (System.Exception ex)
            {
                
            }
        }

        //创建密钥卡；
        private void btn_CreateKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string strKeyGene = "";//tbox_KeyGene.Text.Trim();
                if (strKeyGene.Length != 8)
                {
                    MessageBox.Show(CardErrorTips.PassWordLengthError);
                    return;
                }

                byte[] genes = Encoding.ASCII.GetBytes(strKeyGene);
                CardInfo newCard = new CardInfo();
                newCard.CardType = (byte)CardType.Card_Type_Gengeral;
                Array.Copy(genes, newCard.Gens, 8);
                if (M50CardHelper.NewCard(newCard) == true)
                {
                    MakeCardErrorTip(true, newCard);
                    return;
                }
                MakeCardErrorTip(false, newCard);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "提示");
            }
        }

        private void tbox_UserId_SelectChanged(object sender, RoutedEventArgs e)
        {

            TextBoxChangeCheck(e, tbox_UserIdTip, 8);
        }

        public void TextBoxChangeCheck(RoutedEventArgs e ,TextBlock tip, int length)
        {
            TextBox tbx = (TextBox)e.Source;

            if (tbx.Text.Length != 8 && tbx.Text.Length != 0)
            {
                tip.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                tip.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        private void ChangeTipsCard()
        {
            string tip = "";
            if (!btn_CheckPower.IsEnabled)
            {
                tip = "请打开串口";
            }
            else if (!IsCheckPowerOk && btn_CheckPower.IsEnabled)
            {
                tip = "如需制卡，请进行秘钥验证";
            }   

            tbox_TipCard.Content = tip;
        }

        public bool AddPsw()
        {
            bool bRet = M50CardHelper.SetPassword(7);
            if (bRet)
            {
                bRet = M50CardHelper.SetPassword(7 + 4);
                bRet = M50CardHelper.SetPassword(7 + 8);
                if (bRet)
                {
                    return true;
                }
            }
            return false;
        }


        public bool CleanPsw(bool tips = false)
        {
            bool bRet = M50CardHelper.CleanPassword(7);
            if (bRet)
            {
                bRet = M50CardHelper.CleanPassword(7 + 4);
                bRet = M50CardHelper.CleanPassword(7 + 8);
                if (bRet)
                {
                    if(tips)
                    {
                        MessageBox.Show(CardErrorTips.CleanPassWordSucc);
                    }
                    return true;
                }
            }
            if(tips)
            {
                MessageBox.Show(CardErrorTips.CleanPassWordFail);
            }
            return false;
        }

        private void btn_BandingGene_SelectChanged(object sender, RoutedEventArgs e)
        {
            TextBoxChangeCheck(e, tblk_BandingGenTips, 8);
        }

        private void Btn_Clean(object sender, RoutedEventArgs e)
        {
            M50CardHelper.InitCard();
            CleanPsw(true);
        }

        private void UpdateCardInfoUi(CardInfo card)
        {
            if(card ==null || string.IsNullOrEmpty(card.strId) == true)
            {
                tblk_CardId.Text = "";
                tbox_CardType.Text = "";
                tbox_Gene.Text = "";
                tbox_UsrId.Text = "";
                tbox_Banlance.Text = "";
                tbox_StopCarPrice.Text = "";
                tbox_ElectmentPrice.Text = "";
                cbox_Acount.SelectedIndex = (byte)AcountType.Acount_Type_Acount;
                return;
            }
            tblk_CardId.Text = card.strId;

            byte[] temp = new byte[8];
            string stemp = Encoding.ASCII.GetString(temp);
            string str = Encoding.ASCII.GetString(card.Gens, 0, 8);
            
            tbox_Gene.Text = (string.Equals(stemp,str)) ? "" : str;
            tbox_CardType.Text = "";
            if (card.CardType == (byte)CardType.Card_Type_Empty)
                tbox_CardType.Text = "空白卡";
            else if (card.CardType == (byte)CardType.Card_Type_Mother)
                tbox_CardType.Text = "母卡";
            else if (card.CardType == (byte)CardType.Card_Type_Super_Admin)
                tbox_CardType.Text = "超级管理员卡";
            else if (card.CardType == (byte)CardType.Card_Type_Admin)
                tbox_CardType.Text = "管理员卡";
            else if (card.CardType == (byte)CardType.Card_Type_Gengeral)
                tbox_CardType.Text = "普通卡";
            else if (card.CardType == (byte)CardType.Card_Type_Project)
                tbox_CardType.Text = "工程启停卡";
            else if (card.CardType == (byte)CardType.Card_Type_Binding_Mother)
                tbox_CardType.Text = "管理秘钥卡";
            else if (card.CardType == (byte)CardType.Card_Type_Setting)
                tbox_CardType.Text = "配置卡";

            //tbox_CardType.SelectedIndex = card.CardType;

            str = Encoding.ASCII.GetString(card.UsrId);
            tbox_UsrId.Text = (string.Equals(stemp, str)) ? "" : str;

            cbox_Acount.SelectedIndex = card.Acount;

            UInt32 nValue = 0;
            nValue = card.Banlance[0];
            nValue <<= 8;
            nValue += card.Banlance[1];
            nValue <<= 8;
            nValue += card.Banlance[2];
            nValue <<= 8;
            nValue += card.Banlance[3];
            double dValue = (double)nValue / (double)100;
            tbox_Banlance.Text = dValue.ToString("0.00");

            nValue = card.StopCarPrice[0];
            nValue <<= 8;
            nValue += card.StopCarPrice[1];
            dValue = (double)nValue / (double)100;
            tbox_StopCarPrice.Text = dValue.ToString("0.00");

            nValue = card.ElePrice[0];
            nValue <<= 8;
            nValue += card.ElePrice[1];
            dValue = (double)nValue / (double)100;
            tbox_ElectmentPrice.Text = dValue.ToString("0.00");
        }

        private void btn_InitCard_Click(object sender, RoutedEventArgs e)
        {
            string strid = "";
            if(M50CardHelper.ReadCardNum(out strid) == false)
            {
                MessageBox.Show(CardErrorTips.UnReadCardNum);
                return;
            }


            AddPsw();
            if(M50CardHelper.InitCard() == false)
            {
                MessageBox.Show(CardErrorTips.InitFail);
                return;
            }
            CardInfo card = new CardInfo();
            object obj = (object)card;
            if(M50CardHelper.NewCard(obj) == false)
            {
                MessageBox.Show(CardErrorTips.InitFail);
                return;
            }
            MessageBox.Show(CardErrorTips.InitSucc);
        }

        public void MakeCardErrorTip(bool succ, CardInfo card)
        {
            if(succ)
            {
                MessageBox.Show(string.Format(CardErrorTips.Mat_MakeSucc, ((CardType)card.CardType).GetDescription()));
            }
            else
            {
                MessageBox.Show(string.Format(CardErrorTips.Mat_MakeFail, ((CardType)card.CardType).GetDescription()));
            }
        }

    }
}
