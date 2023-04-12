using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
	class SessionManager
	{
		static SessionManager _session = new SessionManager();
		public static SessionManager Instance { get { return _session; } }

		//티켓번호
		int _sessionId = 0;
		//클라이언트 세션 관리
		Dictionary<int, ClientSession> _sessions = new Dictionary<int, ClientSession>();
		object _lock = new object();

		public ClientSession Generate()
		{
			lock (_lock)
			{

				int sessionId = ++_sessionId;

				//차후에 풀링방식으로 구현시 먼저 생성후 관리할수도잇다.
				ClientSession session = new ClientSession();
				//생성된 클라이언트세션에 티켓번호 부여
				session.SessionId = sessionId;
				//생성된 클라이언트 세션 추가
				_sessions.Add(sessionId, session);

				//프로그램 시작후 초기생성이후 확인
				//이후 확인해본결과 먼저 생성된 클라이언트 세션이 디스커넥시 
				//새로 생성되는데 확인 필요
				Console.WriteLine($"Connected : {sessionId}");

				return session;
			}
		}

		//세션 찾기
		public ClientSession Find(int id)
		{
			lock (_lock)
			{
				ClientSession session = null;
				_sessions.TryGetValue(id, out session);
				return session;
			}
		}

		public void Remove(ClientSession session)
		{
			lock (_lock)
			{
				_sessions.Remove(session.SessionId);
			}
		}
	}
}
