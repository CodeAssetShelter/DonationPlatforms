using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Web;
using WebSocket4Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using File = System.IO.File;
using System.Net.Http.Headers;

namespace ChzzkChatBot
{
    public class ChzzkChat
    {
        [Serializable]
        public class ChzzkApiData
        {
            public string clientId = "your clientId";
            public string clientSecret = "your clientSecret";
            public string state = "Insert new state";
            public string code = "Insert code from interlock url";
            public string accessToken = "";
            public string refreshToken = "";
        }

        public class ChzzkRequest
        {
            public string grantType;
            public string clientId;
            public string clientSecret;
            public string state;
        }

        public class ChzzkRequestCreateToken : ChzzkRequest
        {
            public ChzzkRequestCreateToken(ChzzkApiData _apiData)
            {
                grantType = "authorization_code";
                clientId = _apiData.clientId;
                clientSecret = _apiData.clientSecret;
                state = _apiData.state;
                code = _apiData.code;
            }

            public string code;
        }
        public class ChzzkRequestRefreshToken : ChzzkRequest
        {
            //public string grantType = "authorization_code";
            public ChzzkRequestRefreshToken(ChzzkApiData _apiData)
            {
                grantType = "refresh_token";
                clientId = _apiData.clientId;
                clientSecret = _apiData.clientSecret;
                refreshToken = _apiData.refreshToken;
            }
            public string refreshToken;
        }

        public class ChzzkResponse
        {
            public string code;
            public string message;
            public ChzzkResponseToken content;
        }

        public class ChzzkResponseToken
        {
            public string accessToken;
            public string refreshToken;
            public string tokenType;
            public string expiresIn;
            public string scope;
        }

        public string interlock = "https://chzzk.naver.com/account-interlock?clientId={0}&redirectUri={1}&state={2}";
        public string redirectUri = "Your redirect url";

        public string clientId = "Your clientId";
        public string clientSecret = "Your clientSecret";
        public string state = "Insert Your state";

        // File Path
        public string m_FileBaseUrl = Environment.CurrentDirectory + "/";
        public string m_TokenDataFileUrl = "TokenData.json";

        // Api Path
        public string m_BaseUrl = "https://openapi.chzzk.naver.com";
        public string m_TokenUrl = "/auth/v1/token";
        public string m_ChatUrl = "/open/v1/chats/send";
        public string m_SessionUrl = "/open/v1/sessions/auth/client";

        private HttpClient _client = new();
        private WebSocket _socket;

        public static ChzzkApiData m_ApiData = new();

        public async Task Start()
        {
            // 데이터 초기화
            m_TokenDataFileUrl = m_FileBaseUrl + m_TokenDataFileUrl;
            interlock = string.Format(interlock, clientId, redirectUri, state);

            // 파일에 저장된 토큰 받기
            if (File.Exists(m_TokenDataFileUrl))
            {
                var loaded = File.ReadAllText(m_TokenDataFileUrl);
                m_ApiData = JsonConvert.DeserializeObject<ChzzkApiData>(loaded);
                RefreshAccessToken();
            }
            else
            {
                GetAuthorization();
            }
        }

        public async void GetAuthorization()
        {
            // 파일 경로 재지정
            // 첫 권한 인증 파트
            Console.WriteLine(interlock + " 으로 접속합니다");

            // 타 브라우저로 들어가고 싶다면 이부분을 변경

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                // chrome 을 firefox 로 바꾸면 브라우저를 바꿔서 염
                Arguments = $"/c start chrome \"{interlock}\"",
                CreateNoWindow = true,
                FileName = "CMD.exe"
            });

            Console.WriteLine("\n");

            Console.WriteLine("Code 입력하기");
            string input = Console.ReadLine();
            Console.WriteLine("입력한 Code 값은 : " + input + " 입니다.\n");

            m_ApiData = new();
            m_ApiData.code = input;

            File.WriteAllText(Environment.CurrentDirectory + "/TokenData.json", JsonConvert.SerializeObject(m_ApiData, Formatting.Indented));
            Console.WriteLine("Code 값을 저장했습니다.");
            CreateAccessToken();
        }
        public async void CreateAccessToken()
        {
            ChzzkRequestCreateToken a = new(m_ApiData);

            // 인코딩 안해주면 POST 처리했을 때 무조건 에러남
            // 네이버 공식 API 레퍼런스에 적혀있음
            var data = new StringContent(JsonConvert.SerializeObject(a), Encoding.UTF8, "application/json");
            var url = m_BaseUrl + m_TokenUrl;
            using HttpResponseMessage response = await _client.PostAsync(url, data);
            var o = await response.Content.ReadAsStringAsync();

            ChzzkResponse res = JsonConvert.DeserializeObject<ChzzkResponse>(o);


            if (res.code == "200")
            {
                m_ApiData.refreshToken = res.content.refreshToken;
                m_ApiData.accessToken = res.content.accessToken;
                SaveApiData();
                Console.WriteLine(response.StatusCode);
                SendChat("치지직 챗봇이 시작되었습니다");
            }
            // Body 구조의 문제 또는 올바른 값을 넣어서 전송하지 않음
            else if (res.code == "403")
            {
                Console.WriteLine(response.StatusCode);
                GetAuthorization();
            }
        }

        public async void RefreshAccessToken()
        {
            ChzzkRequestRefreshToken a = new(m_ApiData);

            var data = new StringContent(JsonConvert.SerializeObject(a), Encoding.UTF8, "application/json");
            var url = m_BaseUrl + m_TokenUrl;
            using HttpResponseMessage response = await _client.PostAsync(url, data);
            var o = await response.Content.ReadAsStringAsync();

            ChzzkResponse res = JsonConvert.DeserializeObject<ChzzkResponse>(o);


            if (res.code == "200")
            {
                m_ApiData.refreshToken = res.content.refreshToken;
                m_ApiData.accessToken = res.content.accessToken;
                SaveApiData();
                Console.WriteLine(response.StatusCode);
                SendChat("치지직 챗봇이 시작되었습니다");
            }
            // 토큰이 만료된 경우
            // access,refresh 둘 중 하나만 만료되어도 뜨는 듯?
            else if (res.code == "401")
            {
                Console.WriteLine(response.StatusCode);
                CreateAccessToken();
            }
            // 기타
            else
            {
                Console.WriteLine(response.StatusCode);
            }
        }

        // 채팅을 보내는 API
        public async void SendChat(string _msg = "Empty Message")
        {
            var a = new Dictionary<string, string>()
            {
                { "message", _msg }
            };

            var _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {m_ApiData.accessToken}");
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var data = new StringContent(JsonConvert.SerializeObject(a), Encoding.UTF8, "application/json");
            var url = m_BaseUrl + m_ChatUrl;
            using HttpResponseMessage response = await _client.PostAsync(url, data);
            var o = await response.Content.ReadAsStringAsync();

            ChzzkResponse res = JsonConvert.DeserializeObject<ChzzkResponse>(o);


            if (res.code == "200")
            {
                Console.WriteLine(response.StatusCode);
            }
            else if (res.code == "401")
            {
                RefreshAccessToken();
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                Console.WriteLine(response.StatusCode);
            }
        }

        public void SaveApiData()
        {
            File.WriteAllText(Environment.CurrentDirectory + "/TokenData.json", JsonConvert.SerializeObject(m_ApiData, Formatting.Indented));
        }
    }
}
