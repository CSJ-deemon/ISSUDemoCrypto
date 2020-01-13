using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;
using System.Windows;

namespace ISSUDemoCrypto
{
    public class HelperDatabase
    {
        private static SQLiteConnection sqlConn;
        public static bool Init()
        {
            try
            {
                if (File.Exists("issudb.sqlite") == false)
                {
                    SQLiteConnection.CreateFile("issudb.sqlite");
                }

                string dbPath = @"Data Source=issudb.sqlite;Pooling=true;FailIfMissing=false";
                sqlConn = new SQLiteConnection(dbPath);
                sqlConn.SetPassword("hd123456");
                sqlConn.Open();

                string cmd = @"Create table if not exists issu_record(
                        HandleType TEXT NOT NULL,
                        MacId TEXT NOT NULL,
                        CardId TEXT NOT NULL,
                        SerialId TEXT NOT NULL,
                        Money TEXT NOT NULL,
                        Balance TEXT NOT NULL,
                        CreateTime TEXT NOT NULL);";
                using (SQLiteCommand command = new SQLiteCommand(cmd, sqlConn))
                {
                    command.ExecuteNonQuery();
                }

                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public static bool AddRecord(string HandleType, string MacId, string CardId,
            string SerialId, string Money, string Balance)
        {
            try
            {
                bool result = false;

                //检查是否重复;
                if (HandleType == "新建")
                {
                    string cmd0 = @"select CardId from issu_record where MacId='" + MacId + "';";
                    using (SQLiteCommand cmdCreateTb = new SQLiteCommand(cmd0, sqlConn))
                    {
                        SQLiteDataReader reader = cmdCreateTb.ExecuteReader();
                        if (reader.Read())
                        {
                            result = false;
                            MessageBox.Show(string.Format("物理卡号 {0},\r\n已经占用 {1}.", MacId, reader.GetString(0)));
                        }
                        else
                        {
                            result = true;
                        }
                    }
                    if (result == false) return false;
                }

                string strTime = DateTime.Now.ToString();
                string cmd = @"Insert into issu_record VALUES('" + HandleType + "','" + MacId + "','" +
                    CardId + "','" + SerialId + "','" + Money + "','" + Balance + "','" + strTime + "');";
                using (SQLiteCommand cmdCreateTb = new SQLiteCommand(cmd, sqlConn))
                {
                    int nRet = cmdCreateTb.ExecuteNonQuery();
                    if (nRet == 1) result = true;
                    else result = false;
                }
                return result;
            }
            catch (System.Exception ex)
            {
            	
            }
            return false;
        }

        public static int QueryRecordCount(string CardId)
        {
            try
            {
                int size = 0;
                string strCmd = "";
                if (string.IsNullOrEmpty(CardId) == true)
                {
                    strCmd = @"select count(*) from issu_record;";
                }
                else
                {
                    strCmd = @"select count(*) from issu_record where CardId like '%" + CardId + "%';";
                }

                using (SQLiteCommand cmdCreateTb = new SQLiteCommand(strCmd, sqlConn))
                {
                    SQLiteDataReader reader = cmdCreateTb.ExecuteReader();
                    if (reader.Read())
                    {
                        size = reader.GetInt32(0);
                    }
                }

                return size;
            }
            catch (System.Exception ex)
            {
                return 0;
            }
        }

        public static bool QueryRecord(int start, int size, string CardId, ref List<HandleRecordUI.dsHandleRecord> lstRecord)
        {
            try
            {
                string strCmd = "";
                if (string.IsNullOrEmpty(CardId) == true)
                {
                    strCmd = "select * from issu_record limit " + start + "," + size + ";";
                }
                else
                {
                    strCmd = "select * from issu_record where CardId like '%" + CardId + "%' limit " + start + "," + size + ";";
                }

                using(SQLiteCommand cmdCreateTb = new SQLiteCommand(strCmd, sqlConn))
                {
                    SQLiteDataReader reader = cmdCreateTb.ExecuteReader();
                    int index = start + 1;
                    while (reader.Read())
                    {
                        HandleRecordUI.dsHandleRecord record = new HandleRecordUI.dsHandleRecord();
                        record.Id = index++;
                        record.HandleType = reader.GetString(0);
                        record.MacId = reader.GetString(1).ToUpper();
                        record.CardId = reader.GetString(2);
                        record.SerialId = reader.GetString(3);
                        record.Money = reader.GetString(4);
                        record.Balance = reader.GetString(5);
                        record.CreateTime = reader.GetString(6);
                        lstRecord.Add(record);
                    }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public static bool DeleteRecord(string MacId)
        {
            try
            {
                bool bResult = false;
                string cmd = @"delete from issu_record Where MacId='" + MacId + "';";
                using (SQLiteCommand cmdCreateTb = new SQLiteCommand(cmd, sqlConn))
                {
                    int nRet = cmdCreateTb.ExecuteNonQuery();
                    if (nRet == 1)
                        bResult = true;
                    else bResult = false;
                }
                return bResult;
            }
            catch (System.Exception ex)
            {
            	
            }
            return false;
        }
    }
}
