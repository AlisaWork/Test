using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BaoYou.Pay.WeiXin.lib;
using CreditCardsInvoicing.Core;
using LitJson;

namespace BaoYou.Pay.WeiXin.business
{
    public class JsSdkHelper
    {
        /// <summary>
        /// 获取AccessToken值
        /// </summary>
        /// <returns></returns>
        public string GetAccessToken()
        {
            var obj = BSPCache.Get("wxAccessToken");
            if (obj != null)
            {
                return obj.ToString();
            }
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", WxPayConfig.APPID, WxPayConfig.APPSECRET);
            string result = HttpService.Get(url);
            JsonData jd = JsonMapper.ToObject(result);
            try
            {
                string access_token = (string)jd["access_token"];
                int expires_in = (int)jd["expires_in"];
                BSPCache.Insert("wxAccessToken--GetAccessToken", access_token, expires_in);
                return access_token;
            }
            catch (Exception ex)
            {
                Log.Error("JsSdkHelper", "数据：" + result + "异常：" + ex.Message);
            }
            return null;
        }
        /// <summary>
        /// 获取jsapi_ticket
        /// </summary>
        /// <returns></returns>
        public string GetJsApiTicket()
        {
            var obj = BSPCache.Get("wxJsApiTicket");
            if (obj != null)
            {
                return obj.ToString();
            }
            string accessToken = GetAccessToken();
            string url = string.Format(
                "https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token={0}&type=jsapi", accessToken);
            string result = HttpService.Get(url);
            JsonData jd = JsonMapper.ToObject(result);
            try
            {
                string ticket = (string)jd["ticket"];
                int expires_in = (int)jd["expires_in"];
                BSPCache.Insert("wxJsApiTicket", ticket, expires_in);
                return ticket;
            }
            catch (Exception ex)
            {
                Log.Error("JsSdkHelper--GetJsApiTicket", "数据：" + result + "异常：" + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// 获取微信初始化配置数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public  WxPayData GetJsWxConfigData(string url)
        {
            Dictionary<string, string> dics = new Dictionary<string, string>();
            dics.Add("timestamp", WxPayApi.GenerateTimeStamp());
            dics.Add("noncestr", WxPayApi.GenerateNonceStr());
            string jsapiTicket = GetJsApiTicket();
            dics.Add("jsapi_ticket", jsapiTicket);
            dics.Add("url", url);
            var dicSort = from objDic in dics orderby objDic.Key select objDic;//升序排列，从小到大
            string strSha1In = "";
            WxPayData wxPayData = new WxPayData();
            foreach (var dic in dicSort)
            {
                strSha1In += dic.Key + "=" + dic.Value + "&";
                wxPayData.SetValue(dic.Key, dic.Value);
            }
            strSha1In = strSha1In.TrimEnd('&');
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] bytes_sha1_in = UTF8Encoding.Default.GetBytes(strSha1In);
            byte[] bytes_sha1_out = sha1.ComputeHash(bytes_sha1_in);
            string str_sha1_out = BitConverter.ToString(bytes_sha1_out);
            str_sha1_out = str_sha1_out.Replace("-", "").ToLower();
            wxPayData.SetValue("signature", str_sha1_out);
            wxPayData.SetValue("appid", WxPayConfig.APPID);
            wxPayData.SetValue("url", url);
            return wxPayData;
        }
    }
}
