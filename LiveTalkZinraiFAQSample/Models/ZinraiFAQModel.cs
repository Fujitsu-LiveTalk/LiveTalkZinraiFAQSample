/*
 * Copyright 2019 FUJITSU SOCIAL SCIENCE LABORATORY LIMITED
 * クラス名　：ZinraiFAQModel
 * 概要      ：Zinrai FAQ APIと連携
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LiveTalkZinraiFAQSample.Models
{
    public class ZinraiFAQModel
    {
        private const string AuthBody = "grant_type=client_credentials&scope=service_contract&client_id={0}&client_secret={1}";
        private string ClientID = "<<<<<client_id>>>>>";
        private string ClientPassword = "<<<<<client_secret>>>>>";
        private string ProxyServer = "";    // PROXY経由なら proxy.hogehoge.jp:8080 のように指定
        private string ProxyId = "";        // 認証PROXYならIDを指定
        private string ProxyPassword = "";  // 認証PROXYならパスワードを指定
        private string AccessToken = string.Empty;
        private string LastAccessTokenError = null;
        private DateTime TokenExpireUtcTime = DateTime.MinValue;

        /// <summary>
        /// アクセストークンを取得する
        /// </summary>
        /// <returns></returns>
        public async Task GetToken()
        {
            this.AccessToken = await GetAccessTokenAsync().ConfigureAwait(false);
            Console.WriteLine("Successfully obtained an access token. \n");
        }
        
        /// <summary>
                 /// FAQを検索する
                 /// </summary>
                 /// <param name="v"></param>
                 /// <returns></returns>
        internal async Task<string> GetAnswer(string question)
        {
            var answer = string.Empty;
            this.AccessToken = await GetAccessTokenAsync();

            try
            {
                // プロキシ設定
                var ch = new HttpClientHandler() { UseCookies = true };
                if (!string.IsNullOrEmpty(this.ProxyServer))
                {
                    var proxy = new System.Net.WebProxy(this.ProxyServer);
                    if (!string.IsNullOrEmpty(this.ProxyId) && !string.IsNullOrEmpty(this.ProxyPassword))
                    {
                        proxy.Credentials = new System.Net.NetworkCredential(this.ProxyId, this.ProxyPassword);
                    }
                    ch.Proxy = proxy;
                }
                else
                {
                    ch.Proxy = null;
                }

                // Web API呼び出し
                using (var client = new HttpClient(ch))
                {
                    using (var request = new HttpRequestMessage())
                    {
                        var questionJsonString = "";
                        {
                            var item = new TFujitsuK5FAQ()
                            {
                                query = new TQuery() { text = question },
                                options = new TOptions()
                                {
                                    highlight = new THighlight() { enable = true, include_related_terms = true },
                                    max_size = 10,
                                },
                            };
                            using (var json = new MemoryStream())
                            {
                                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFujitsuK5FAQ));
                                ser.WriteObject(json, item);
                                questionJsonString = Encoding.UTF8.GetString(json.ToArray());
                            }

                        }
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri("https://zinrai-pf.jp-east-1.paas.cloud.global.fujitsu.com/FAQSearch/v1/documents/_search?key=" + DateTime.Now.ToString("HHmmss"));
                        request.Headers.Add("X-Access-Token", this.AccessToken);
                        request.Headers.Add("X-Service-Code", "FJAI000011-00002");
                        request.Headers.Add("X-Forwarded-User", "LiveTalk");
                        request.Content = new StringContent(questionJsonString, Encoding.UTF8, "application/json");
                        client.Timeout = TimeSpan.FromSeconds(10);
                        var response = await client.SendAsync(request);
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                        {
                            var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFujitsuK5FAQResult));
                            {
                                var result = ser.ReadObject(json) as TFujitsuK5FAQResult;
                                if (result.hits.hits != null && result.hits.hits.Count() > 0)
                                {
                                    answer = result.hits.hits[0]._source.answer;
                                }
                                else
                                {
                                    answer = "大変申し訳ございません。「" + result.terms[0] + "」については判りません。";
                                }
                            }
                        }
                    }
                }
                this.LastAccessTokenError = "";
            }
            catch (Exception ex)
            {
                if (ex.Message != this.LastAccessTokenError)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    this.LastAccessTokenError = ex.Message;
                }
            }
            return answer;
        }

        /// <summary>
        /// K5のOAuth認証を行う
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetAccessTokenAsync()
        {
            string token = null;

            if (this.TokenExpireUtcTime >= DateTime.UtcNow)
            {
                token = this.AccessToken;
                return token;
            }
            try
            {
                // プロキシ設定
                var ch = new HttpClientHandler() { UseCookies = true };
                if (!string.IsNullOrEmpty(this.ProxyServer))
                {
                    var proxy = new System.Net.WebProxy(this.ProxyServer);
                    if (!string.IsNullOrEmpty(this.ProxyId) && !string.IsNullOrEmpty(this.ProxyPassword))
                    {
                        proxy.Credentials = new System.Net.NetworkCredential(this.ProxyId, this.ProxyPassword);
                    }
                    ch.Proxy = proxy;
                }
                else
                {
                    ch.Proxy = null;
                }

                // 認証呼び出し
                using (var client = new HttpClient(ch))
                {
                    using (var request = new HttpRequestMessage())
                    {
                        request.Method = HttpMethod.Post;
                        request.RequestUri = new Uri("https://auth-api.jp-east-1.paas.cloud.global.fujitsu.com/API/oauth2/token?key=" + DateTime.Now.ToString("HHmmss"));
                        client.Timeout = TimeSpan.FromSeconds(10);
                        request.Content = new StringContent(string.Format(AuthBody, this.ClientID, this.ClientPassword), Encoding.UTF8, "application/x-www-form-urlencoded");
                        var response = await client.SendAsync(request);
                        var jsonString = await response.Content.ReadAsStringAsync();
                        using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                        {
                            var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFujitsuK5Auth));
                            {
                                var result = ser.ReadObject(json) as TFujitsuK5Auth;
                                token = result.access_token;
                                this.TokenExpireUtcTime = DateTime.UtcNow.AddSeconds(result.expires_in - 60);
                            }
                        }
                    }
                }
                this.LastAccessTokenError = "";
            }
            catch (Exception ex)
            {
                if (ex.Message != this.LastAccessTokenError)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    this.LastAccessTokenError = ex.Message;
                }
                this.TokenExpireUtcTime = DateTime.MinValue;
            }
            return token;
        }

        [DataContract]
        public class TFujitsuK5Auth
        {
            [DataMember]
            public string access_token { get; set; }
            [DataMember]
            public string token_type { get; set; }
            [DataMember]
            public int expires_in { get; set; }
            [DataMember]
            public string scope { get; set; }
            [DataMember]
            public string client_id { get; set; }
            [DataMember]
            public TContract_Info contract_info { get; set; }
        }

        [DataContract]
        public class TContract_Info
        {
            [DataMember]
            public TContract_List[] contract_list { get; set; }
        }

        [DataContract]
        public class TContract_List
        {
            [DataMember]
            public string service_contract_id { get; set; }
            [DataMember]
            public string service_code { get; set; }
        }


        [DataContract]
        public class TFujitsuK5FAQ
        {
            [DataMember]
            public TQuery query { get; set; }
            [DataMember]
            public TOptions options { get; set; }
        }

        [DataContract]
        public class TQuery
        {
            [DataMember]
            public string text { get; set; }
        }

        [DataContract]
        public class TOptions
        {
            [DataMember]
            public THighlight highlight { get; set; }
            [DataMember]
            public int max_size { get; set; }
        }

        [DataContract]
        public class THighlight
        {
            [DataMember]
            public bool enable { get; set; }
            [DataMember]
            public bool include_related_terms { get; set; }
        }

        [DataContract]
        public class TFujitsuK5FAQResult
        {
            [DataMember]
            public THits hits { get; set; }
            [DataMember]
            public string[] terms { get; set; }
            [DataMember]
            public string search_id { get; set; }
        }

        [DataContract]
        public class THits
        {
            [DataMember]
            public THit[] hits { get; set; }
            [DataMember]
            public int total { get; set; }
        }

        [DataContract]
        public class THit
        {
            [DataMember]
            public string _id { get; set; }
            [DataMember]
            public float _score { get; set; }
            [DataMember]
            public TSource _source { get; set; }
        }

        [DataContract]
        public class TSource
        {
            [DataMember]
            public string dt_answer { get; set; }
            [DataMember]
            public string ctgr { get; set; }
            [DataMember]
            public string answer { get; set; }
            [DataMember]
            public string kind { get; set; }
            [DataMember]
            public string cls_trouble { get; set; }
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string title { get; set; }
            [DataMember]
            public string content { get; set; }
            [DataMember]
            public string url { get; set; }
        }
    }
}

