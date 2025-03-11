# DonationPlatforms
 숲을 제외한 치지직, 투네이션과 연결하는 유틸

# 주의사항
 이 프로젝트는 WebSocket4Net 및 RestAPI, Newtonsoft.Json 을 사용합니다.
 프로젝트의 Nuget 관리자에서 반드시 받아주세요.
 https://www.nuget.org/packages/WebSocket4Net/
 버전은 상관없습니다.

# ChzzkChat.cs
 치지직 공식 API 를 이용한 채팅보내기 샘플입니다.
 https://developers.chzzk.naver.com/
 해당 페이지에서 사용하고자 하는 앱을 먼저 등록하신 뒤 이용하세요.
 
 https://chzzk.gitbook.io/chzzk
 공식 레퍼런스는 이쪽입니다.

# Chzzk.cs / ChzzkVideo.cs
 (원본 코드)
 https://github.com/JoKangHyeon/ChzzkUnity/tree/main
 유니티에서 사용하시던 코드를 C# WebSocket4Net 으로 변환한 코드입니다.
 사용되는 클래스와 현재 후원 데이터 구조가 조금 바뀌어서 변경했습니다.

# Toonation.cs
 투네이션 이벤트를 구독하고 후원이 들어올시 인지하고 메세지를 반환합니다.
 원본이 있는 스크립트입니다. (너무 옛날에 받아서 출처를 모름...)
