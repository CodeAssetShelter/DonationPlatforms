using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using WebSocket4Net;
using DonateText;

namespace Donate
{
    public class Chzzk
    {
        #region Variables

        private const string WS_URL = "wss://kr-ss3.chat.naver.com/chat";
        private const string HEARTBEAT_REQUEST = "{\"ver\":\"2\",\"cmd\":0}";
        private const string HEARTBEAT_RESPONSE = "{\"ver\":\"2\",\"cmd\":10000}";

        string cid;
        string token;
        public string channel = "Insert your chzzk channel hash";

        WebSocket? socket = null;

        #region Callbacks

        public Action<Profile, string>? onMessage;
        public Action<Profile, string, DonationExtras>? onDonation;
        public Action<Profile, SubscriptionExtras>? onSubscription;
        public Action? onClose;
        public Action? onOpen;

        #endregion Callbacks

        #endregion Variables

        int closedCount = 0;
        bool reOpenTrying = false;
        
        // 진입점
        public async Task Start()
        {
            onMessage += DebugMessage;
            onDonation += DebugDonation;
            onSubscription += DebugSubscription;

            Connect();
        }


        #region Debug Methods

        // 여기서 채팅받은거 처리 예정
        private void DebugMessage(Profile profile, string str)
        {
            if (profile == null) return;

            Console.WriteLine($"| [Message] {profile.nickname} - {str}");
        }
        private void DebugDonation(Profile profile, string str, DonationExtras donation)
        {
            //isAnonymous가 true면 profile은 null임을 유의
            if (donation.isAnonymous) return;

            Console.WriteLine(donation.isAnonymous
                ? $"| [Donation] 익명 - {str} - {donation.payAmount}/{donation.payType}"
                : $"| [Donation] {profile.nickname} - {str} - {donation.payAmount}/{donation.payType}");
        }
        private void DebugSubscription(Profile profile, SubscriptionExtras subscription)
        {
            Console.WriteLine($"| [Subscription] {profile.nickname} - {subscription.month}");
        }

        #endregion Debug Methods

        #region Public Methods

        public void RemoveAllOnMessageListener() => onMessage = null;
        public void RemoveAllOnDonationListener() => onDonation = null;
        public void RemoveAllOnSubscriptionListener() => onSubscription = null;

        public async Task<ChannelInfo> GetChannelInfo(string channelId)
        {
            var url = $"https://api.chzzk.naver.com/service/v1/channels/{channelId}";
            using HttpResponseMessage response = await new HttpClient().GetAsync(url);
            var res = await response.Content.ReadAsStringAsync();

            ChannelInfo? channelInfo = null;
            Console.WriteLine(res);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                //Cid 획득
                channelInfo = JsonConvert.DeserializeObject<ChannelInfo>(res);
            }
            return channelInfo;
        }

        public async Task<LiveStatus> GetLiveStatus(string channelId)
        {
            var url = $"https://api.chzzk.naver.com/polling/v2/channels/{channelId}/live-status";

            using HttpResponseMessage response = await new HttpClient().GetAsync(url);
            var res = await response.Content.ReadAsStringAsync();

            LiveStatus? liveStatus = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //Cid 획득
                liveStatus = JsonConvert.DeserializeObject<LiveStatus>(res);
            }

            return liveStatus;
        }

        public async Task<AccessTokenResult> GetAccessToken(string cid)
        {
            var url = $"https://comm-api.game.naver.com/nng_main/v1/chats/access-token?channelId={cid}&chatType=STREAMING";

            using HttpResponseMessage response = await new HttpClient().GetAsync(url);
            var res = await response.Content.ReadAsStringAsync();

            AccessTokenResult? accessTokenResult = null;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //Cid 획득
                accessTokenResult = JsonConvert.DeserializeObject<AccessTokenResult>(res);
            }

            return accessTokenResult;
        }

        public async Task Connect()
        {
            if (socket != null && socket.State == WebSocketState.Open)
            {
                socket.Close();
                socket = null;
            }

            LiveStatus liveStatus = await GetLiveStatus(channel);
            cid = liveStatus.content.chatChannelId;
            AccessTokenResult accessTokenResult = await GetAccessToken(cid);
            token = accessTokenResult.content.accessToken;
            socket = new WebSocket(WS_URL);

            //wss라서 ssl protocol을 활성화 해줘야 함.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls;

            //이벤트 등록
            socket.MessageReceived += OnMessageRecieved;
            socket.Closed += OnClosed;
            socket.Opened += OnOpened;

            //연결
            await socket.OpenAsync();
        }

        public void StopListening()
        {
            if (socket == null) return;
            socket.Close();
            socket = null;
        }

        #endregion Public Methods

        #region Socket Event Handlers

        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                IDictionary<string, object>? data = JsonConvert.DeserializeObject<IDictionary<string, object>>(e.Message);
                //Console.WriteLine(e.Message);

                JArray body;
                JObject bodyObject;
                Profile profile;
                string profileText;

                //Cmd에 따라서
                switch ((long)data["cmd"])
                {
                    case 0://HeartBeat Request
                           //하트비트 응답해줌.
                        socket.Send(HEARTBEAT_RESPONSE);
                        //서버가 먼저 요청해서 응답했으면 타이머 초기화해도 괜찮음.
                        break;
                    case 93101://Chat
                        body = (JArray)data["bdy"];
                        foreach (JToken jToken in body)
                        {
                            bodyObject = (JObject)jToken;
                            //프로필이.... json이 아니라 string으로 들어옴.
                            profileText = bodyObject["profile"]?.ToString();
                            if (profileText != null)
                            {
                                profileText = profileText.Replace("\\", "");
                                profile = JsonConvert.DeserializeObject<Profile>(profileText);
                                onMessage?.Invoke(profile, bodyObject["msg"]?.ToString().Trim());
                            }
                        }

                        break;
                    case 93102://Donation & Subscription
                        body = (JArray)data["bdy"];

                        foreach (JToken jToken in body)
                        {
                            bodyObject = (JObject)jToken;

                            //프로필 스트링 변환
                            profileText = bodyObject["profile"].ToString();
                            profileText = profileText.Replace("\\", "");

                            if (!string.IsNullOrEmpty(profileText))
                                profile = JsonConvert.DeserializeObject<Profile>(profileText);
                            else profile = null;

                            var msgTypeCode = int.Parse(bodyObject["msgTypeCode"].ToString());
                            //도네이션과 관련된 데이터는 extra
                            string extraText = null;
                            if (bodyObject.TryGetValue("extra", value: out JToken value))
                            {
                                extraText = value.ToString();
                            }
                            else if (bodyObject.TryGetValue("extras", out JToken value1))
                            {
                                extraText = value1.ToString();
                            }

                            extraText = extraText.Replace("\\", "");

                            switch (msgTypeCode)
                            {
                                case 10: // Donation
                                    var donation = JsonConvert.DeserializeObject<DonationExtras>(extraText);
                                    if (!donation.isAnonymous)
                                        onDonation?.Invoke(profile, bodyObject["msg"].ToString(), donation);
                                    break;
                                case 11: // Subscription
                                    var subscription = JsonConvert.DeserializeObject<SubscriptionExtras>(extraText);
                                    onSubscription?.Invoke(profile, subscription);
                                    break;
                                default:
                                    Console.WriteLine($"Err - MessageTypeCode-{msgTypeCode} is not supported");
                                    Console.WriteLine("Err - " + bodyObject.ToString());
                                    break;
                            }
                        }

                        break;
                    case 93006://Temporary Restrict 블라인드 처리된 메세지.
                    case 94008://Blocked Message(CleanBot) 차단된 메세지.
                    case 94201://Member Sync 멤버 목록 동기화.
                    case 10000://HeartBeat Response 하트비트 응답.
                        break;
                    case 10100://Token ACC
                               //Console.WriteLine(data["cmd"]);
                               //Console.WriteLine(e.Data);
                        onOpen?.Invoke();
                        break;//Nothing to do
                    default:
                        //내가 놓친 cmd가 있나?
                        //Console.WriteLine(data["cmd"]);
                        //Console.WriteLine(e.Data);
                        break;
                }
            }

            catch (Exception er)
            {
                Console.WriteLine("Err - " + er.ToString());
            }
        }

        private async void OnClosed(object sender, EventArgs e)
        {
            Console.WriteLine("Err - 연결이 해제되었습니다");
            Console.WriteLine(e.ToString());

            await Task.Delay(1000);

            Connect();
        }

        private void OnOpened(object sender, EventArgs e)
        {
            Console.WriteLine($"OPENED : {cid} + {token}");

            // WebSocket 을 통해서 메세지를 보내려면
            // 미리 로그인한 웹 쿠키를 들고, Auth 가 SEND 가 되어야한다.
            // 아래 Json 데이터만으로는 읽기 권한만 얻을 수 있음
            // 치지직 공식 API 로 채팅을 보내면 된다.
            var message = $"{{\"ver\":\"2\",\"cmd\":100,\"svcid\":\"game\",\"cid\":\"{cid}\",\"bdy\":{{\"uid\":null,\"devType\":2001,\"accTkn\":\"{token}\",\"auth\":\"READ\"}},\"tid\":1}}";
            socket.Send(message);
            SendPing();
        }

        //15~20초에 한번 HeartBeat 전송해야 함.
        //서버에서 먼저 요청하면 안 해도 됨.
        public void SendPing()
        {
            if (socket == null) return;
            socket.Send(HEARTBEAT_REQUEST);

            var Timer = new System.Timers.Timer(15000);
            Timer.Elapsed += (o, p) =>
            {
                socket.Send(HEARTBEAT_REQUEST);
            };
            Timer.Enabled = true;
        }
        #endregion Socket Event Handlers
    }
}
