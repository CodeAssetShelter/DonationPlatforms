using System;
using System.Threading.Tasks;
using Donate;
using System.Timers;
using System.Diagnostics;
using ChzzkChatBot;
using Newtonsoft.Json;
using System.Linq;

namespace MainCore
{    
    class Program
    {
        public static Program Instance;

        public ChzzkVideo m_ChzzkVideo;
        public Chzzk m_Chzzk;
        public ChzzkChat m_ChzzkChat;
        public Toonation m_Toonation;

        /// <summary>
        /// 프로그램의 진입점
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Instance = new Program();
            Instance.BotMain().GetAwaiter().GetResult();   //봇의 진입점 실행
        }

        /// <summary>
        /// 봇의 진입점, 봇의 거의 모든 작업이 비동기로 작동되기 때문에 비동기 함수로 생성해야 함
        /// </summary>
        /// <returns></returns>
        public async Task BotMain()
        {
            // 각 진입점 테스트는 여기서
            await Task.Delay(-1);   //봇이 종료되지 않도록 블로킹
        }
    }
}