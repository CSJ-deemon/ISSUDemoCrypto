using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISSUDemoCrypto
{
    class CardErrorTips
    {
        public const string ReadFail = "未能识别此卡";
        public const string SetSucc = "设置成功";
        public const string SetFail = "设置失败";
        public const string ChangePassWordSucc = "修改密码成功";
        public const string ChangePassWordFail = "修改密码失败";
        public const string Mat_MakeSucc = "{0}制作成功";
        public const string Mat_MakeFail = "{1}制作失败";
        public const string CleanPassWordSucc = "清除密码成功";
        public const string CleanPassWordFail = "清除密码失败";
        public const string PassWordLengthError = "秘钥错误!请刷入8位字符";
        public const string UnReadCardNum = "请放入卡片并打开正确串口";
        public const string InitFail = "卡格式化失败";
        public const string InitSucc = "卡格式化成功";
        public const string InputMoneySucc = "充值成功";
        public const string InputMoneyFail = "充值失败";
        public const string OutputMoneySucc = "退款成功";
        public const string OutputMoneyFail = "退款失败";
        public const string OpenComFail = "没有打开串口";
        public const string CheckPowerOff = "请进行秘钥验证";
        public const string CheckPowerFail = "秘钥验证失败";
        public const string TextNull = "存在参数为空，请重新填写";
    }
}
