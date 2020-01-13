using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace ISSUDemoCrypto
{
    public enum KeyTypes
    {
        KeyA = 0,
        KeyB = 4
    }

    public enum CMD_TABLE
    {
        GETVERSION = 0x00,
        RFCLOSE = 0x01,
        RFWARMRESET = 0x02,
        RFRESET = 0x03,
        REQUEST = 0x04,
        ANTICOLL = 0x05,
        SELECT = 0x06,
        GETCARDNUM = 0x07,
        AUTHTICATION = 0x08,
        READBLOCK = 0x09,
        WRITEBLOCK = 0x10,
        VALUE = 0x11,
        HALT = 0x12,
    };

    public enum CardType
    {
        Card_Type_Empty = 0,
        Card_Type_Mother = 1,
        Card_Type_Super_Admin = 2,
        Card_Type_Admin = 3,
        Card_Type_Gengeral = 4,
        Card_Type_Setting = 5,
        Card_Type_Setting_Ip = 6,
        Card_Type_Setting_Addr = 7,
        Card_Type_Setting_CommType = 8,
        Card_Type_Setting_Price = 9, 
        Card_Type_Binding_Mother = 10,
        Card_Type_Binding_Super = 11,
        Card_Type_Binding_Admin = 12,
        Card_Type_Binding_Usr = 13,
        Card_Type_StartStop = 14,
        Card_Type_Project = 15
    }

    public class CardInfo
    {
        public string strId;
        public byte CardType;
        public byte[] Gens = new byte[8];
        public byte Acount;
        public byte UsrType;
        public byte IsLock;
        public byte IsPwd;
        public byte PaymentFlag;
        public byte[] UsrId = new byte[8];
        public byte[] UsrPwd = new byte[4];
        public byte[] Banlance = new byte[4];//高字节在前;
        public byte[] ElePrice = new byte[2];
        public byte[] StopCarPrice = new byte[2];
        public byte[] StartTicks = new byte[4];
        public byte[] DevId = new byte[8];
        public byte[] Param = new byte[16];//设置参数,第6块;
    };

    public static class M50CardHelper
    {
        //KEYA=C2F1BC6CCE28;
        //KEYB=A3D82230900D;
        //public static byte[] DefaultPwdA = new byte[] { 0xC2, 0xF1, 0xBC, 0x6C, 0xCE, 0x28 };
        //public static byte[] DefaultPwdB = new byte[] { 0xA3, 0xD8, 0x22, 0x30, 0x90, 0x0D };
        public static byte[] DefaultPwdA = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
        public static byte[] DefaultPwdB = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
        private readonly static byte[] ControlCode = new byte[] { 0xFF, 0x07, 0x80, 0x69 };

        private static ComBase myComBase = new ComBase();
        public static bool Init(string Name, int Bautrate)
        {
            try
            {
                if (myComBase.Opened == false)
                {
                    myComBase.PortNum = Name;
                    myComBase.BaudRate = Convert.ToInt32(Bautrate);
                    myComBase.ReadTimeout = 3 * 1000;
                    myComBase.ReadIntervalTimeout = 50;
                    return myComBase.Open();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                return false;
            }
        }

        public static void Close()
        {
            if (myComBase != null)
            {
                myComBase.Close();
            }
        }

        public static bool GetCardInfo(ref object obj) 
        {
            string strId = "";
            if (ReadCardNum(out strId) == false) return false;
            CardInfo card = (CardInfo)obj;
            card.strId =   strId;

            //验证第一扇区;
            if (SectorAuthtication(KeyTypes.KeyA, 0x01, DefaultPwdA) == false)
                return false;

            //读第一扇区,第0块 数据;
            byte[] Temp = new byte[16];
            byte Cnt = 0;
            if (ReadBlock(0x01 * 4 + 0, ref Temp) == false)
                return false;
            card.CardType = Temp[Cnt++];
            for (byte i = 0; i < 8; i++)
                card.Gens[i] = Temp[Cnt++];
            card.Acount = Temp[Cnt++];
            card.UsrType = Temp[Cnt++];
            card.IsLock = Temp[Cnt++];
            card.IsPwd = Temp[Cnt++];
            card.PaymentFlag = Temp[Cnt++];

            //读第一扇区，第1块 数据;
            Array.Clear(Temp, 0, Temp.Length);
            Cnt = 0;
            if (ReadBlock(0x01 * 4 + 1, ref Temp) == false)
                return false;
            for (byte i = 0; i < 8; i++)
                card.UsrId[i] = Temp[Cnt++];
            for (byte i = 0; i < 4; i++)
                card.UsrPwd[i] = Temp[Cnt++];
            for (byte i = 0; i < 2; i++)
                card.ElePrice[i] = Temp[Cnt++];
            for (byte i = 0; i < 2; i++)
                card.StopCarPrice[i] = Temp[Cnt++];

            //验证第二扇区
            if (SectorAuthtication(KeyTypes.KeyA, 0x02, DefaultPwdA) == false)
                return false;
            Array.Clear(Temp, 0, Temp.Length);
            Cnt = 0;

            //读第二扇区，第0块
            if (ReadValue(0x02 * 4 + 0, ref Temp) == false)
                return false;
            for (byte i = 0; i < 4; i++)
                card.Banlance[i] = Temp[3-i];

            //读第二扇区，第1块
            Array.Clear(Temp, 0, Temp.Length);
            Cnt = 0;
            if (ReadBlock(0x02 * 4 + 1, ref Temp) == false)
                return false;

            for (byte i = 0; i < 4; i++)
                card.StartTicks[i] = Temp[Cnt++];
            for (byte i = 0; i < 8; i++)
                card.DevId[i] = Temp[Cnt++];

            return true;
        }

        public static bool InitCard()
        {
            string strId = "";
            if (ReadCardNum(out strId) == false) return false;

            //验证第一扇区;
            if (SectorAuthtication(KeyTypes.KeyA, 0x01, DefaultPwdA) == false)
                return false;

            byte[] Temp = new byte[16];

            //写第一扇区所有数据;
            if (WriteBlock(0x01 * 4 + 0, Temp) == false)
                return false;
            if (WriteBlock(0x01 * 4 + 1, Temp) == false)
                return false;
            if (WriteBlock(0x01 * 4 + 2, Temp) == false)
                return false;

            //验证第二扇区;
            if (SectorAuthtication(KeyTypes.KeyA, 0x02, DefaultPwdA) == false)
                return false;
            
            //写第二扇区所有数据
            if (WriteBlock(0x02 * 4 + 0, Temp) == false)
                return false;
            if (WriteBlock(0x02 * 4 + 1, Temp) == false)
                return false;
            if (WriteBlock(0x02 * 4 + 2, Temp) == false)
                return false;

            return true;
        }

        public static bool NewCard(object obj)
        {
            CardInfo card = (CardInfo)obj;

            byte[] Temp0 = new byte[16];
            byte[] Temp1 = new byte[16];
            byte[] Temp2 = new byte[16];
            byte[] temp3 = new byte[16];

            Temp0[0] = card.CardType;
            if (card.CardType == (byte)CardType.Card_Type_Empty)
            {
                //写空白卡;
                for (byte i = 0; i < 2; i++)
                    Temp0[1 + i] = card.Gens[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Mother)
            {
                //写母卡;
                for (byte i = 0; i < 2; i++)
                    Temp0[1 + i] = card.Gens[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Super_Admin)
            {
                //写超级管理员卡;
                for (byte i = 0; i < 4; i++)
                    Temp0[1 + i] = card.Gens[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Setting)
            {
                for (byte i = 0; i < 2; i++)
                    Temp0[1 + i] = card.Gens[i];
                Temp0[9] = card.Acount;
                Temp0[10] = card.UsrType;
            }
            else if (card.CardType == (byte)CardType.Card_Type_Setting_Ip)
            {
                Temp0[1] = card.Gens[0];
                Temp0[2] = card.Gens[1];
                for (byte i = 0; i < 16; i++)
                    temp3[i] = card.Param[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Setting_Addr)
            {
                Temp0[1] = card.Gens[0];
                Temp0[2] = card.Gens[1];
                for (byte i = 0; i < 16; i++)
                    temp3[i] = card.Param[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_StartStop)
            {
                Temp0[1] = card.Gens[0];
                Temp0[2] = card.Gens[1];
                for (byte i = 0; i < 16; i++)
                {
                    temp3[i] = 0x00;
                }
            }
            else if (card.CardType == (byte)CardType.Card_Type_Project)
            {
                //Temp0[1] = card.Gens[0];
                //Temp0[2] = card.Gens[1];
                for (byte i = 0; i < 16; i++)
                {
                    temp3[i] = 0x00;
                }
                byte Cnt = 1;
                for (byte i = 0; i < 8; i++)
                    Temp0[Cnt++] = card.Gens[i];
                Temp0[Cnt++] = card.Acount;
                Temp0[Cnt++] = card.UsrType;
                Temp0[Cnt++] = card.IsLock;
                Temp0[Cnt++] = card.IsPwd;
                Temp0[Cnt++] = card.PaymentFlag;

                Cnt = 0;
                for (byte i = 0; i < 8; i++)
                    Temp1[Cnt++] = card.UsrId[i];
                for (byte i = 0; i < 4; i++)
                    Temp1[Cnt++] = card.UsrPwd[i];
                for (byte i = 0; i < 2; i++)
                    Temp1[Cnt++] = card.ElePrice[i];
                for (byte i = 0; i < 2; i++)
                    Temp1[Cnt++] = card.StopCarPrice[i];

                Cnt = 0;
                Temp2[0] = card.Banlance[0];
                Temp2[1] = card.Banlance[1];
                Temp2[2] = card.Banlance[2];
                Temp2[3] = card.Banlance[3];
                for (int i = 0; i < 10; i++)
                {
                    Temp2[4 + i] = 0x00;
                }
                Temp1[14] = card.PaymentFlag;

                for (int i = 0; i < 15; i++)
                {
                    temp3[i] = Temp2[i];
                }
            }
            else if (card.CardType == (byte)CardType.Card_Type_Setting_CommType)
            {
                Temp0[1] = card.Gens[0];
                Temp0[2] = card.Gens[1];
                for (byte i = 0; i < 16; i++)
                    temp3[i] = card.Param[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Setting_Price)
            {
                Temp0[1] = card.Gens[0];
                Temp0[2] = card.Gens[1];
                for (byte i = 0; i < 16; i++)
                    temp3[i] = card.Param[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Admin)
            {
                //写管理员卡;
                for (byte i = 0; i < 6; i++)
                    Temp0[1 + i] = card.Gens[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Binding_Mother)
            {
                //写绑定母卡;
                for (byte i = 0; i < 8; i++)
                    Temp0[1 + i] = card.Gens[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Binding_Super)
            {
                //写绑定超级管理员卡;
                for (byte i = 0; i < 4; i++)
                    Temp0[1 + i] = card.Gens[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Binding_Admin)
            {
                //写绑定管理员卡;
                for (byte i = 0; i < 6; i++)
                    Temp0[1 + i] = card.Gens[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Binding_Usr)
            {
                for (byte i = 0; i < 8; i++)
                    Temp0[1 + i] = card.Gens[i];
            }
            else if (card.CardType == (byte)CardType.Card_Type_Gengeral)
            {
                //写普通卡
                byte Cnt = 1;
                for (byte i = 0; i < 8; i++)
                    Temp0[Cnt++] = card.Gens[i];
                Temp0[Cnt++] = card.Acount;
                Temp0[Cnt++] = card.UsrType;
                Temp0[Cnt++] = card.IsLock;
                Temp0[Cnt++] = card.IsPwd;
                Temp0[Cnt++] = card.PaymentFlag;

                Cnt = 0;
                for (byte i = 0; i < 8; i++)
                    Temp1[Cnt++] = card.UsrId[i];
                for (byte i = 0; i < 4; i++)
                    Temp1[Cnt++] = card.UsrPwd[i];
                for (byte i = 0; i < 2; i++)
                    Temp1[Cnt++] = card.ElePrice[i];
                for (byte i = 0; i < 2; i++)
                    Temp1[Cnt++] = card.StopCarPrice[i];

                Cnt = 0;
                Temp2[0] = card.Banlance[0];
                Temp2[1] = card.Banlance[1];
                Temp2[2] = card.Banlance[2];
                Temp2[3] = card.Banlance[3];
                for (int i = 0; i < 10; i++)
                {
                    Temp2[4 + i] = 0x00;
                }
                Temp1[14] = card.PaymentFlag;

                for (int i = 0; i < 15; i++)
                {
                    temp3[i] = Temp2[i];
                }
                //for (byte i = 0; i < 4; i++)
                //    Temp2[Cnt++] = card.StartTicks[i];
                //for (byte i = 0; i < 8; i++)
                //    Temp2[Cnt++] = card.DevId[i];
            }
            else
            {
                return false;
            }

            string strId = "";
            if (ReadCardNum(out strId) == false) return false;

            //验证第一扇区;
            if (SectorAuthtication(KeyTypes.KeyA, 0x01, DefaultPwdA) == false)
                return false;

            //写第一扇区所有数据;
            if (WriteBlock(0x01 * 4 + 0, Temp0) == false)
                return false;
            if (WriteBlock(0x01 * 4 + 1, Temp1) == false)
                return false;
            if (WriteBlock(0x01 * 4 + 2, temp3) == false)
                return false;

            //验证第二扇区;
            if (SectorAuthtication(KeyTypes.KeyA, 0x02, DefaultPwdA) == false)
                return false;

            //写第二扇区所有数据;
            UInt32 Value = card.Banlance[0];
            Value <<= 8;
            Value += card.Banlance[1];
            Value <<= 8;
            Value += card.Banlance[2];
            Value <<= 8;
            Value += card.Banlance[3];
            if (InitValue(0x02 * 4 + 0, (int)Value) == false)
                return false;
            if (WriteBlock(0x02 * 4 + 1, Temp2) == false)
                return false;
            if (WriteBlock(0x02 * 4 + 2, temp3) == false)
                return false;

            return true;
        }

        public static bool AddValue(UInt32 Value) 
        {
            if (SectorAuthtication(KeyTypes.KeyA, 0x02, DefaultPwdA) == false) 
                return false;

            if (IncreaseValue(0x02 * 4 + 0, Value) == false) return false;

            byte[] Temp = new byte[4];
            if (ReadValue(0x02 * 4 + 0, ref Temp) == false) return false;
            Value = Temp[0];
            Value += (UInt32)(Temp[1] << 8);
            Value += (UInt32)(Temp[2] << 16);
            Value += (UInt32)(Temp[3] << 24);

            byte[] btData = new byte[16];
            byte[] btData1 = new byte[16];
            if (ReadBlock(0x02 * 4 + 1, ref btData) == false) return false;
            if (ReadBlock(0x02 * 4 + 2, ref btData1) == false) return false;
            btData[0] = (byte)(Value >> 24);
            btData[1] = (byte)(Value >> 16);
            btData[2] = (byte)(Value >> 8);
            btData[3] = (byte)(Value);
            btData1[0] = (byte)(Value >> 24);
            btData1[1] = (byte)(Value >> 16);
            btData1[2] = (byte)(Value >> 8);
            btData1[3] = (byte)(Value);
            if (WriteBlock(0x02 * 4 + 1, btData) == false) return false;
            if (WriteBlock(0x02 * 4 + 2, btData1) == false) return false;

            return true;
        }

        public static bool SubValue(UInt32 Value)
        {
            if (SectorAuthtication(KeyTypes.KeyA, 0x02, DefaultPwdA) == false) 
                return false;

            byte[] Temp = new byte[4];
            if (ReadValue(0x02 * 4 + 0, ref Temp) == false) return false;
            UInt32 Money = Temp[0];
            Money += (UInt32)(Temp[1] << 8);
            Money += (UInt32)(Temp[2] << 16);
            Money += (UInt32)(Temp[3] << 24);

            if (Money < (UInt32)Value) 
                return false;

            if (DecreaseValue(0x02 * 4 + 0, Value) == false) return false;

            byte[] btData = new byte[16];
            byte[] btData1 = new byte[16];
            Value = Money - Value;
            if (ReadBlock(0x02 * 4 + 1, ref btData) == false) return false;
            if (ReadBlock(0x02 * 4 + 2, ref btData1) == false) return false;
            btData[0] = (byte)(Value >> 24);
            btData[1] = (byte)(Value >> 16);
            btData[2] = (byte)(Value >> 8);
            btData[3] = (byte)(Value);
            btData1[0] = (byte)(Value >> 24);
            btData1[1] = (byte)(Value >> 16);
            btData1[2] = (byte)(Value >> 8);
            btData1[3] = (byte)(Value);
            if (WriteBlock(0x02 * 4 + 1, btData) == false) return false;
            if (WriteBlock(0x02 * 4 + 2, btData1) == false) return false;

            return true;
        }

        public static bool ReadData(byte Sector, ref byte[] Data)
        {
            string strId = "";
            if (ReadCardNum(out strId) == false) return false;

            if (SectorAuthtication(KeyTypes.KeyA, Sector, DefaultPwdA) == false)
                return false;

            for(byte i = 0; i < 4; i++)
            {
                byte[] Temp = new byte[16];
                ReadBlock((byte)(Sector * 4 + i), ref Temp);
                for (byte j = 0; j < 16; j++)
                    Data[(16 * i + j)] = Temp[j];
            }
            return true;
        }

        public static bool SetPassword(byte Block)
        {
            string strId = "";
            if (ReadCardNum(out strId) == false) return false;
            int id = (int)(Block % 4);
            if (id != 3) return false;

            byte nSector = (byte)(Block / 4);
            byte[] btKeyA = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            if (SectorAuthtication(KeyTypes.KeyB, nSector, btKeyA) == false)
                return false;

            byte[] btBuf = new byte[16];
            byte[] KeyA = new byte[] { 0xC2, 0xF1, 0xBC, 0x6C, 0xCE, 0x28 };
            byte[] KeyB = new byte[] { 0xA3, 0xD8, 0x22, 0x30, 0x90, 0x0D };
            for (int i = 0; i < 6; i++)
            {
                btBuf[i] = KeyA[i];
                btBuf[10 + i] = KeyB[i];
            }
            for (int i = 0; i < 4; i++)
            {
                btBuf[6 + i] = ControlCode[i];
            }

            if (WriteBlock(Block, btBuf) == true)
            {
                return true;
            }
            return false;
        }

        public static bool CleanPassword(byte Block)
        {
            string strId = "";
            if (ReadCardNum(out strId) == false) return false;
            int id = (int)(Block % 4);
            if (id != 3) return false;

            byte nSector = (byte)(Block / 4);
            byte[] btKeyA = new byte[] { 0xC2, 0xF1, 0xBC, 0x6C, 0xCE, 0x28 };
            byte[] btKeyDefault = new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            if (SectorAuthtication(KeyTypes.KeyA, nSector, btKeyA) == false)
                return false;

            byte[] btBuf = new byte[16];
            for (int i = 0; i < 6; i++)
            {
                btBuf[i] = btKeyDefault[i];
                btBuf[10 + i] = btKeyDefault[i];
            }
            for (int i = 0; i < 4; i++)
            {
                btBuf[6 + i] = ControlCode[i];
            }

            if (WriteBlock(Block, btBuf) == true)
            {
                return true;
            }
            return false;
        }

        //===================================================================================
        /// <summary>
        /// 读取卡编号
        /// </summary>
        /// <param name="CardId"></param>
        /// <returns></returns>
        public static bool ReadCardNum(out string CardId)
        {
            CardId = "";
            byte[] InBuffer = new byte[32];
            byte[] OutBuffer = new byte[32];
            byte[] Num = new byte[4];
            InBuffer[0] = (byte)CMD_TABLE.GETVERSION;
            if (Ioctl((byte)CMD_TABLE.GETCARDNUM, InBuffer, 1, out OutBuffer) == false) return false;
            if (OutBuffer[0] == 0x00)
            {
                Num[0] = OutBuffer[7];
                Num[1] = OutBuffer[8];
                Num[2] = OutBuffer[9];
                Num[3] = OutBuffer[10];
                for (int i = 0; i < Num.Length; i++)
                {
                    CardId += Num[i].ToString("x2");
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 扇区认证
        /// </summary>
        /// <param name="KeyType"></param>
        /// <param name="SectorNum"></param>
        /// <param name="KeyBuf">密码</param>
        /// <returns></returns>
        public static bool SectorAuthtication(KeyTypes KeyType, byte SectorNum, byte[] KeyBuf)
        {
            try
            {
                byte Cmd = (byte)CMD_TABLE.AUTHTICATION;
                byte Len = 8;
                byte[] inBuf = new byte[32];
                byte[] outBuf = new byte[32];

                inBuf[0] = (byte)KeyType;    //密钥模式
                inBuf[1] = SectorNum;   //数据块
                for (int i = 0; i < 6; i++)
                {
                    inBuf[2 + i] = KeyBuf[i];
                }
                if (Ioctl(Cmd, inBuf, Len, out outBuf) == false) return false;
                if (outBuf[0] == 0x00)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 写数据块
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="DataBuf"></param>
        /// <returns></returns>
        public static bool WriteBlock(byte Block, byte[] DataBuf)
        {
            try
            {
                byte Len = 17;
                byte[] inBuf = new byte[32];
                byte[] outBuf = new byte[32];
  
                inBuf[0] = Block;   //数据块
                for (int i = 0; i < 16; i++)
                {
                    inBuf[1 + i] = DataBuf[i];
                }
                if (Ioctl((byte)CMD_TABLE.WRITEBLOCK, inBuf, Len, out outBuf) == false) return false;
                if (outBuf[0] == 0x00)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception et)
            {
                return false;
            }
        }

        /// <summary>
        /// 读取块的数据
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="DataBuf"></param>
        /// <returns></returns>
        public static bool ReadBlock(byte Block, ref byte[] DataBuf)
        {
            try
            {
                byte[] InBuf = new byte[32];
                byte[] outBuf = new byte[32];
                InBuf[0] = Block;   //数据块
                if (Ioctl((byte)CMD_TABLE.READBLOCK, InBuf, 1, out outBuf) == false) return false;
                if (outBuf[0] == 0x00)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        DataBuf[i] = outBuf[3 + i];
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 串口的发送与接收
        /// </summary>
        /// <param name="Cmd"></param>
        /// <param name="SendData"></param>
        /// <param name="inLen"></param>
        /// <param name="RecvData"></param>
        /// <returns></returns>
        public static bool Ioctl(byte Cmd, byte[] SendData, byte inLen, out byte[] RecvData)
        {
            RecvData = new byte[0];
            try
            {
                byte retLength = 0, sendlength, i;

                byte[] chIns = new byte[32];
                byte[] chRet = new byte[32];

                byte cs1, cs2;
                cs1 = 0;
                cs2 = 0;

                if (myComBase.Opened == false) return false;

                sendlength = 0;
                chIns[sendlength++] = 0x40;
                cs1 = 0x40;
                cs2 = 0x40;

                chIns[sendlength++] = Cmd;
                cs1 += Cmd;
                cs2 ^= Cmd;
                chIns[sendlength++] = 0x00;
                cs1 += 0x00;
                cs2 ^= 0x00;
                chIns[sendlength++] = inLen;
                cs1 += inLen;
                cs2 ^= inLen;
                for (i = 0; i < inLen; i++)
                {
                    chIns[sendlength++] = SendData[i];
                    cs1 += SendData[i];
                    cs2 ^= SendData[i];
                }
                chIns[sendlength++] = cs1;
                chIns[sendlength++] = cs2;
                chIns[sendlength++] = 0x0D;

                myComBase.Write(chIns, sendlength);
                retLength = (byte)myComBase.Read(out chRet, 32);

                if (chRet[0] != 0x40)
                {
                    return false;
                }

                if (chRet[1] != Cmd)
                {
                    return false;
                }

                if (retLength > (chRet[4] + 8)) retLength = (byte)(chRet[4] + (byte)8);

                cs1 = 0;
                cs2 = 0;
                for (i = 0; i < retLength - 3; i++)
                {
                    cs1 += chRet[i];
                    cs2 ^= chRet[i];
                }
                if (chRet[retLength - 3] != cs1)//"数据校检出错"
                {
                    return false;
                }
                if (chRet[retLength - 2] != cs2)//"数据校检出错"
                {
                    return false;
                }

                RecvData = new byte[retLength - 3];
                for (i = 0; i < chRet[4] + 3; i++)
                {
                    RecvData[i] = chRet[2 + i];
                }
                return true;
            }
            catch (Exception et)
            {
                return false;
            }
        }

        /// <summary>
        /// 初始化值,
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Value">设定值</param>
        /// <returns></returns>
        public static bool InitValue(byte Block, int Value)
        {
            byte[] Data = new byte[4];
            Data = BitConverter.GetBytes(Value);
            try
            {
                byte Len = 17;
                byte[] inBuf = new byte[32];
                byte[] outBuf = new byte[32];
                inBuf[0] = Block;   //数据块
                inBuf[1] = Data[0];
                inBuf[2] = Data[1];
                inBuf[3] = Data[2];
                inBuf[4] = Data[3];
                inBuf[5] = (byte)~Data[0];
                inBuf[6] = (byte)~Data[1];
                inBuf[7] = (byte)~Data[2];
                inBuf[8] = (byte)~Data[3];
                inBuf[9] = Data[0];
                inBuf[10] = Data[1];
                inBuf[11] = Data[2];
                inBuf[12] = Data[3];
                inBuf[13] = 0x00;
                inBuf[14] = 0xFF;
                inBuf[15] = 0x00;
                inBuf[16] = 0xFF;
                if (Ioctl((byte)CMD_TABLE.WRITEBLOCK, inBuf, Len, out outBuf) == false) return false;
                if (outBuf[0] == 0x00)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 加值
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool IncreaseValue(byte Block, UInt32 Value)
        {
            byte[] Data = new byte[4];
            Data = BitConverter.GetBytes(Value);
            try
            {
                byte Len = 7;
                byte[] inBuf = new byte[32];
                byte[] outBuf = new byte[32];
                inBuf[0] = 0xC1;
                inBuf[1] = Block;   //数据块
                inBuf[2] = Data[0];
                inBuf[3] = Data[1];
                inBuf[4] = Data[2];
                inBuf[5] = Data[3];
                inBuf[6] = Block;   //数据块
                if (Ioctl((byte)CMD_TABLE.VALUE, inBuf, Len, out outBuf) == false) return false;
                if (outBuf[0] == 0x00)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 减值
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool DecreaseValue(byte Block, UInt32 Value)
        {
            byte[] Data = new byte[4];
            Data = System.BitConverter.GetBytes(Value);
            try
            {
                byte Len = 7;
                byte[] inBuf = new byte[32];
                byte[] outBuf = new byte[32];
                inBuf[0] = 0xC0;
                inBuf[1] = Block;   //数据块
                inBuf[2] = Data[0];
                inBuf[3] = Data[1];
                inBuf[4] = Data[2];
                inBuf[5] = Data[3];
                inBuf[6] = Block;   //数据块
                if (Ioctl((byte)CMD_TABLE.VALUE, inBuf, Len, out outBuf) == false) return false;
                if (outBuf[0] == 0x00)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 读值
        /// </summary>
        /// <param name="Block"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static bool ReadValue(byte Block, ref byte[] Value)
        {
            try
            {
                byte[] inBuf = new byte[32];
                byte[] outBuf = new byte[32];
                inBuf[0] = Block;   //数据块
                if (Ioctl((byte)CMD_TABLE.READBLOCK, inBuf, 1, out outBuf) == false) return false;
                if (outBuf[0] == 0x00)
                {
                    //判断数值格式是否合法
                    if ((outBuf[3] != (byte)outBuf[11]) || (outBuf[4] != (byte)outBuf[12]) || (outBuf[5] != (byte)outBuf[13]) || (outBuf[6] != (byte)outBuf[14])) return false;
                    if ((outBuf[3] != (byte)~outBuf[7]) || (outBuf[4] != (byte)~outBuf[8]) || (outBuf[5] != (byte)~outBuf[9]) || (outBuf[6] != (byte)~outBuf[10])) return false;
                    if ((outBuf[15] != (byte)~outBuf[16]) || (outBuf[15] != (byte)outBuf[17]) || (outBuf[16] != (byte)outBuf[18])) return false;

                    for (int i = 0; i < 4; i++ )
                    {
                        Value[i] = outBuf[3 + i];
                    }

                    //Value = outBuf[6] * 256 * 256 * 256 + outBuf[5] * 256 * 256 + outBuf[4] * 256 + outBuf[3];
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
