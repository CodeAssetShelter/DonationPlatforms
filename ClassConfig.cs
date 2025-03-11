using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public enum SslProtocolsHack
{
    Tls = 192,
    Tls11 = 768,
    Tls12 = 3072
}


public enum CHAT_CMD
{
    PING = 0,
    PONG = 10000,
    CONNECT = 100,
    CONNECTED = 10100,
    REQUEST_RECENT_CHAT = 5101,
    RECENT_CHAT = 15101,
    EVENT = 93006,
    CHAT = 93101,
    DONATION = 93102,
    KICK = 94005,
    BLOCK = 94006,
    BLIND = 94008,
    NOTICE = 94010,
    PENALTY = 94015,
    SEND_CHAT = 3101
}

public enum CHAT_TYPE
{
    TEXT = 1,
    IMAGE = 2,
    STICKER = 3,
    VIDEO = 4,
    RICH = 5,
    DONATION = 10,
    SUBSCRIPTION = 11,
    SYSTEM_MESSAGE = 30
}

#region Sub-classes
namespace DonateText
{
    [Serializable]
    public class LiveStatus
    {
        public int code;
        public string message;
        public Content content;

        [Serializable]
        public class Content
        {
            public string liveTitle;
            public string status;
            public int concurrentUserCount;
            public int accumulateCount;
            public bool paidPromotion;
            public bool adult;
            public string chatChannelId;
            public string categoryType;
            public string liveCategory;
            public string liveCategoryValue;
            public string livePollingStatusJson;
            public string faultStatus;
            public string userAdultStatus;
            public bool chatActive;
            public string chatAvailableGroup;
            public string chatAvailableCondition;
            public int minFollowerMinute;
        }
    }

    [Serializable]
    public class AccessTokenResult
    {
        public int code;
        public string message;
        public Content content;
        [Serializable]
        public class Content
        {
            public string accessToken;

            [Serializable]
            public class TemporaryRestrict
            {
                public bool temporaryRestrict;
                public int times;
                public int duration;
                public int createdTime;
            }
            public bool realNameAuth;
            public string extraToken;
        }
    }

    [Serializable]
    public class Profile
    {
        public string userIdHash;
        public string nickname;
        public string profileImageUrl;
        public string userRoleCode;
        public Badge badge;
        public Title title;
        public string verifiedMark;
        public List<ActivityBadges> activityBadges;
        public StreamingProperty streamingProperty;
        [Serializable]
        public class StreamingProperty
        {

        }
    }

    [Serializable]
    public class ActivityBadges
    {
        public int badgeNo;
        public string badgeId;
        public string imageUrl;
        public bool activated;
    }

    [Serializable]
    public class Badge
    {
        public string imageUrl { get; set; }
    }
    [Serializable]
    public class Title
    {
        public string name;
        public string color;
    }

    [Serializable]
    public class SubscriptionExtras
    {
        public int month;
        public string tierName;
        public string nickname;
        public int tierNo;
    }

    [Serializable]
    public class DonationExtras
    {
        System.Object emojis;
        public bool isAnonymous;
        public string payType;
        public int payAmount;
        public string streamingChannelId;
        public string nickname;
        public string osType;
        public string donationType;

        public List<WeeklyRank> weeklyRankList;
        [Serializable]
        public class WeeklyRank
        {
            public string userIdHash;
            public string nickName;
            public bool verifiedMark;
            public int donationAmount;
            public int ranking;
        }
        public WeeklyRank donationUserWeeklyRank;
    }

    [Serializable]
    public class ChannelInfo
    {
        public int code;
        public string message;
        public Content content;

        [Serializable]
        public class Content
        {
            public string channelId;
            public string channelName;
            public string channelImageUrl;
            public bool verifiedMark;
            public string channelType;
            public string channelDescription;
            public int followerCount;
            public bool openLive;
        }
    }
}

namespace DonateVideo
{
    [Serializable]
    public class SessionUrl
    {
        public string code;
        public object message;
        public Content content;

        [Serializable]
        public class Content
        {
            public string sessionUrl;
        }
    }


    public class DonationControl
    {
        int startSecond;
        int endSecond;
        bool stopVideo;
        bool titleExpose;
        string donationId;
        int payAmount;
        bool isAnonymous;
        bool useSpeech;
    }

    public class VideoDonationListConverter : JsonConverter<VideoDonationList>
    {
        public override VideoDonationList ReadJson(JsonReader reader, Type objectType, VideoDonationList existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // 입력이 null이면 바로 반환
            if (reader.TokenType == JsonToken.Null)
                return null;

            JToken token = JToken.Load(reader);
            VideoDonationList result = new VideoDonationList();

            if (token.Type == JTokenType.Array)
            {
                // serializer를 사용해서 변환
                result.videoDonation = token.ToObject<List<string>>(serializer);
            }
            else if (token.Type == JTokenType.Object)
            {
                JToken donationToken = token["videoDonation"];
                if (donationToken != null && donationToken.Type == JTokenType.Array)
                {
                    result.videoDonation = donationToken.ToObject<List<string>>(serializer);
                }
                else
                {
                    result.videoDonation = new List<string>();
                }
            }
            return result;
        }

        public override void WriteJson(JsonWriter writer, VideoDonationList value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.videoDonation);
        }
    }

    [JsonConverter(typeof(VideoDonationListConverter))]
    [Serializable]
    public class VideoDonationList
    {
        public List<string> videoDonation { get; set; }
    }

    [Serializable]
    public class VideoDonation
    {
        public int startSecond;
        public int endSecond;
        public string videoType;
        public string videoId;
        public string playMode;
        public bool stopVideo;
        public bool titleExpose;
        public string donationId;
        public string profile;
        public int payAmount;
        public string donationText;
        public string replayRequest;
        public bool isAnonymous;
        public int tierNo;
        public bool useSpeech;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    [Serializable]
    public class Profile
    {
        public string userIdHash;
        public string nickname;
        public string profileImageUrl;
        public string userRoleCode;
        public string badge;
        public string title;
        public bool verifiedMark;
        public List<ActivityBadge> activityBadges;

        [Serializable]
        public class ActivityBadge
        {
            public int badgeNo;
            public string badgeId;
            public string imageUrl;
            public bool activated;
        }

        public StreamingProperty streamingProperty;
        [Serializable]
        public class StreamingProperty
        {
            public Subscription subscription;
            [Serializable]
            public class Subscription
            {
                public int accumulativeMonth;
                public int tier;
                public Badge badge;
                public class Badge
                {
                    public string imageUrl;
                }
            }
            public NicknameColor nicknameColor;
            [Serializable]
            public class NicknameColor
            {
                public string colorCode;
            }
        }
    }
}

namespace DonateToonation
{
    [System.Serializable]
    public class ToonDonateData
    {
        public string test = "";
        public string code = "";
        public string code_ex = ""; // 도네 타입을 지정하는 듯 // 1200 랜덤박스
        public string replay = "0"; // 다시 재생하는거면 1
        public ToonContent content;
    }
    [System.Serializable]
    public class ToonContent
    {
        public int amount;
        public string uid;
        public string account; // 로그인 계정명
        public string name; // 닉네임
        public string target_acc; // 구독을 선물한 대상
        public string target_name;// 구독을 선물받은 대상

        public string message; // 도네 메세지

        public ToonRoulette roulette;
        public ToonLuckyBox luckybox; // 럭키박스
    }
    [System.Serializable]
    public class ToonLuckyBox
    {
        public string luckybox_id; // 럭키박스 유니크키
        public int donate_amount; // 도네 금액 (성공시 상금)
        public int duration; // 선택 시간인듯
        public int state; // 1 : 럭키박스 시작, 2 : 모르겠음, 3 : 선택함
        public int box_count; // 선택지 갯수
        public int bounty = 0; // 선택으로 인해 얻은 보상 // 이거랑 donate_amount 같으면 성공인듯
        public string selected_box; // 선택한 박스 // 1부터 시작
        public string box_winning; // 정답 박스
    }
    [System.Serializable]
    public class ToonRoulette
    {
        public string hash; // 해시 값
        public int grade; // 별 갯수로 보임
        public int randomized_index; // 설정한 보상에서 획득한 인덱스 값
    }
}
#endregion Sub-classes

