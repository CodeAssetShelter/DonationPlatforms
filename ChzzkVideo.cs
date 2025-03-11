using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using WebSocket4Net;
using System.Collections;
using System.Diagnostics;
using Newtonsoft.Json;
using DonateVideo;
using Discord;
using System.Text.RegularExpressions;

namespace Donate
{
    public class ChzzkVideo
    {
        #region Variables

        //WSS(WS 말고 WSS) 쓰려면 필요함.
        private enum SslProtocolsHack
        {
            Tls = 192,
            Tls11 = 768,
            Tls12 = 3072
        }

        WebSocket? socket = null;

        float timer = 0f;
        bool running = false;

        private const string HEARTBEAT_REQUEST = "2";
        private const string HEARTBEAT_RESPONSE = "3";

        #region Callbacks

        /// <summary>
        /// 영상 도네이션 도착시 호출되는 이벤
        /// </summary>
        public Action<Profile, VideoDonation>? onVideoDonationArrive = null;
        /// <summary>
        /// 영상 도네이션 도착시 호출되는 이벤
        /// </summary>
        public Action<DonationControl>? onVideoDonationControl = null;
        /// <summary>
        /// 웹소켓이 열렸을 때 호출되는 이벤트
        /// </summary>
        public Action? onClose = null;
        /// <summary>
        /// 웹소켓이 닫혔을 때 호출되는 이벤트
        /// </summary>
        public Action? onOpen = null;

        public Dictionary<string, KeyValuePair<Profile, VideoDonation>> activeVideo;


        #endregion Callbacks

        #endregion Variables


        int closedCount = 0;
        bool reOpenTrying = false;
        string wssUrl;

        // 42 영상 후원을 받았을 때
        // playMode
        // ALERT_PLAY : 후원을 먼저 받은 뒤, 스트리머가 재생 눌러서 플레이해야 재생
        // AUTO_PLAY : 후원이 오면 자동 재생
        // VIDEO_PLAY : 리모컨을 통해 직접 재생하는경우

        public string m_VideoUrl = "Insert your video donation url";

        // 진입점
        public async Task Start()
        {
            wssUrl = await GetWssUrlFromMissionUrl(m_VideoUrl);
            Connect();
        }

        #region Connection Methods

        /// <summary>
        /// 전체 URL에서 필요한 ID만 추출
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        /// https://chzzk.naver.com/mission-donation/mission@<MissionWSSID>
        public string GetMissionWSSId(string url)
        {
            return url.Split("@")[1];
        }

        /// <summary>
        /// SessionURL을 받아옴
        /// </summary>
        /// <param name="missionWSSId">GetMissionWSSId 함수의 값을 사용</param>
        /// <returns></returns>
        public async Task<string> GetSessionURL(string missionWSSId)
        {
            var url = $"https://api.chzzk.naver.com/manage/v1/alerts/video@{missionWSSId}/session-url";
            using HttpResponseMessage response = await new HttpClient().GetAsync(url);
            var res = await response.Content.ReadAsStringAsync();

            SessionUrl sessionUrl = null;

            if(response.StatusCode == HttpStatusCode.OK)
            {
                //Cid 획득
                sessionUrl = JsonConvert.DeserializeObject<SessionUrl>(res);
            }

            return sessionUrl.content.sessionUrl;
        }

        /// <summary>
        /// SessionURL에서 Auth를 추출
        /// </summary>
        /// <param name="sessionURL">GetSessionURL 참고</param>
        /// <returns></returns>
        public string MakeWssURL(string sessionUrl)
        {
            string auth = sessionUrl.Split("auth=")[1];
            string server = sessionUrl.Split(".nchat")[0].Substring(12);
            return $"wss://ssio{server}.nchat.naver.com/socket.io/?auth={auth}&EIO=3&transport=websocket";
        }


        public async Task<string> GetWssUrlFromMissionUrl(string missionUrl)
        {
            string wssId = GetMissionWSSId(missionUrl);
            string sessionUrl = await GetSessionURL(wssId);
            return MakeWssURL(sessionUrl);
        }

        public async Task Connect()
        {
            if (socket != null && socket.State == WebSocketState.Open)
            {
                socket.Close();
                socket = null;
            }


            socket = new WebSocket(wssUrl);

            //wss라서 ssl protocol을 활성화 해줘야 함.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls;

            //이벤트 등록
            socket.MessageReceived += OnMessageRecieved;
            socket.Closed += OnClosed;
            socket.Opened += OnOpened;

            onVideoDonationArrive += OnVideoDonationArrive;

            //연결
            await socket.OpenAsync();
        }

        void OnVideoDonationArrive(Profile _profile, VideoDonation _videoDonation)
        {
            // 익명일 시 처리안함
            if(_videoDonation.isAnonymous)
            {
                return;
            }

            // ALERT_PLAY : 영도먼저 받고, 스트리머가 재생 눌러줘야하는 경우
            // AUTO_PLAY : 영도가 오면 자동 재생
            // VIDEO_PLAY : 리모컨을 통해 직접 재생하는경우 <- 이 때만 처리 안함
            switch (_videoDonation.playMode)
            {
                // 이 두개는 스트리머가 제어 할 수 없는 부분
                // 무조건 다시재생이 안되는 후원 데이터만 날아옴
                case "ALERT_PLAY":
                case "AUTO_PLAY":
                    // Do something...

                    break;

                // 다시 재생이 가능한 후원데이터가 넘어옴
                case "VIDEO_PLAY":
                    break;
            }
        }

        void OnOpened(object sender, EventArgs e)
        {
            timer = 0;
            running = true;
            socket.Send(HEARTBEAT_REQUEST);

            SendPing();
        }

        public void StopListening()
        {
            if (socket == null) return;
            socket.Close();
            socket = null;
        }

        public void SendPing()
        {
            if (socket == null) return;
            socket.Send(HEARTBEAT_REQUEST);

            // 15000ms 간격으로 핑 전송
            var Timer = new System.Timers.Timer(15000);
            Timer.Elapsed += (o, p) =>
            {
                socket.Send(HEARTBEAT_REQUEST);
            };
            Timer.Enabled = true;
        }

        #endregion Connection Methods

        #region Socket Event Handlers


        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
        {
#if DEBUG
            Console.WriteLine(e.Message.Replace("\\\\/", "").Replace("\\\"", "").Replace("\\r\\n", "<br/>").Replace("\\n", "<br/>").Replace("\\\\", ""));
#endif
            if (e.Message == HEARTBEAT_REQUEST)
            {
                timer = 0;
                socket.Send(HEARTBEAT_RESPONSE);
                return;
            }
            else if (e.Message == HEARTBEAT_RESPONSE)
            {
                return;
            }
            else if (e.Message == "40")
            {
                return;
            }

            // Json 앞에 붙어나오는 정수를 제거하는 용도
            string jsonPart = Regex.Replace(e.Message, @"^\d+", "");
            VideoDonationList donations = JsonConvert.DeserializeObject<VideoDonationList>(jsonPart);


            if (donations.videoDonation == null || !donations.videoDonation.Any()) return;

            switch (donations.videoDonation[0])
            {
                case "donation":
                    //List<KeyValuePair<Profile, VideoDonation>> donationList = new List<KeyValuePair<Profile, VideoDonation>>();

                    for (int i = 1; i < donations.videoDonation.Count; i++)
                    {
                        VideoDonation donationObject = JsonConvert.DeserializeObject<VideoDonation>(donations.videoDonation[i]);
                        Console.WriteLine(donationObject);
                        if (donationObject.isAnonymous)
                        {
                            // 익명유저 처리는 여기서
                        }

                        // 후원 데이터를 어떻게 적용할지는 여기서
                        Profile profile = JsonConvert.DeserializeObject<Profile>(donationObject.profile);
                        //donationList.Add(new KeyValuePair<Profile, VideoDonation>(profile, donationObject));
                        onVideoDonationArrive.Invoke(profile, donationObject);
                    }

                    /*
                    foreach (KeyValuePair<Profile, VideoDonation> donation in donationList)
                    {
                        if (activeVideo.ContainsKey(donation.Value.donationId))
                        {
                            activeVideo[donation.Value.donationId] = donation;
                        }
                        else
                        {
                            activeVideo.Add(donation.Value.donationId, donation);
                        }
                    }*/

                    break;

                case "donationControl":
                    List<DonationControl> controlList = new List<DonationControl>();

                    for (int i = 1; i < donations.videoDonation.Count; i++)
                    {
                        DonationControl controlObject = JsonConvert.DeserializeObject<DonationControl>(donations.videoDonation[i]);
                        Console.WriteLine(controlObject);
                        controlList.Add(controlObject);
                        onVideoDonationControl.Invoke(controlObject);
                    }
                    break;
            }

        }


        private async void OnClosed(object sender, EventArgs e)
        {
            Console.WriteLine("Err - 연결이 해제되었습니다");
            Console.WriteLine(e.ToString());

            await Task.Delay(1000);

            Connect();
        }

#endregion Socket Event Handlers
    }
}