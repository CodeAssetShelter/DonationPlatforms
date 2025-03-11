using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebSocket4Net;
using System.Net;
using Discord.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DonateToonation;

namespace Donate
{
    class Toonation
    {
        // Twip service is offline
        //public string m_URL_Twip = "wss://io.mytwip.net/socket.io/";
        //public string m_Alertbox_Twip = "offline";

        // 이전 주소 및 포트
        //public string m_URL_Toon = "wss://toon.at:8071/";

        // 현재 주소 및 포트
        public string m_URL_Toon = "wss://ws.toon.at/";
        public int m_URL_Toon_Port = 443;
        public string m_Alertbox_Toon = "Insert your alertbox url hash";

        public WebSocket? m_ToonSocket = null;
        private HttpClient _client;


        // 진입점
        public async Task<WebSocket?> GetToon()
        {
            _client = new();
            using HttpResponseMessage response = await _client.GetAsync("https://toon.at/widget/alertbox/" + m_Alertbox_Toon);
            var o = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //Console.WriteLine(o);

                string pattern = "\"payload\":\"(.*?)\",";
                string payload = string.Empty;
                Match match = Regex.Match(o, pattern);
                if (match.Success)
                {
                    // 그룹 1번이 "payload":"와 ", 사이에 있는 내용
                    payload = match.Groups[1].Value.Trim();
                    Console.WriteLine(payload);
                }

                if (string.IsNullOrEmpty(payload))
                {
                    Console.WriteLine($"payload is null or empty");
                    return null;
                }

                Console.WriteLine(m_URL_Toon + $"{payload}");


                // 웹소켓샾 버전
                m_ToonSocket = new WebSocket(m_URL_Toon + $"{payload}");
                m_ToonSocket.Opened += (o, e) =>
                {
                    //Console.WriteLine(e.ToString());
                    Console.WriteLine("Opened");
                    SendPing();
                };

                m_ToonSocket.MessageReceived += Toon_Recv;
                m_ToonSocket.Closed += Toon_Close;
                m_ToonSocket.Error += Toon_Error;

                await m_ToonSocket.OpenAsync();

                return m_ToonSocket;
            }

            return null;
        }

        public void SendPing()
        {
            if (m_ToonSocket == null) return;
            m_ToonSocket.Send("#ping");

            var Timer = new System.Timers.Timer(12000);
            Timer.Elapsed += (o, p) =>
            {
                //Console.WriteLine("#ping");
                m_ToonSocket.Send("#ping");
            };
            Timer.Enabled = true;
        }

        public void Toon_Recv(object? sender, MessageReceivedEventArgs e)
        {
            // 퐁 메세지 매번 찍히는게 보기 싫어서 처리
            // 또는 파싱에러가 났던 것으로 기억함
#if DEBUG
            if (e.Message.Contains($"#Pong", StringComparison.OrdinalIgnoreCase)) return;
#endif

            Console.WriteLine("Recv : " + e.Message.ToString());
            var item = JsonConvert.DeserializeObject<ToonDonateData>(e.Message.ToString());

            // 도네이터 데이터가 아니면 작동안함
            if (item != null &&  item.content != null &&!string.IsNullOrEmpty(item.content.account))
            {
                Console.WriteLine("name : " + item.content.name);
                Console.WriteLine("account : " + item.content.account);
                Console.WriteLine("replay : " + item.replay);

                bool isReplay = item.replay == "1";
                switch (item.code)
                {
                    case "101": // 도네이션
                        // 1 이라면 리모컨을 통해 다시 재생한 데이터
                        if(!isReplay)
                        {
                            // Do Something...
                        }
                        break;
                    default:
                        Console.WriteLine(e.Message.ToString());
                        return;
                }
            }
        }
        public void Toon_Open(object? sender, EventArgs e)
        {
            Console.WriteLine("Toon Connected, Not send ping every 12 seconds");
            SendPing();
        }

        public void Toon_Close(object? sender, EventArgs e)
        {
            Console.WriteLine("Toon Closed");
        }

        public void Toon_Error(object? sender, EventArgs e)
        {
            Console.WriteLine("Toon Error : " + e.ToString());
        }
    }
}
