using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ServerCore
{
	public class Listener
	{

		//tcpsocket
		Socket _listenSocket;
		Func<Session> _sessionFactory;

		/// <summary>
		/// Listener생성 - ip,sessionFactory-> _sessionFactory , 최대 등록자 , 최대 대기자
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="sessionFactory"></param>
		/// <param name="register"></param>
		/// <param name="backlog"></param>
		//_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
		public void Init(IPEndPoint endPoint, Func<Session> sessionFactory, int register = 10, int backlog = 100)
		{
			_listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			_sessionFactory += sessionFactory;

			// 문지기 교육
			_listenSocket.Bind(endPoint);

			// 영업 시작
			// backlog : 최대 대기수
			_listenSocket.Listen(backlog);

			
			for (int i = 0; i < register; i++)
			{
				//register만큼 SocketAsyncEventArgs 생성, 처음한번만 등록
				//이 비동기 이벤트는 연결시도를 받아 들이는 비동기 작업을 시작할때 사용한다.
				SocketAsyncEventArgs args = new SocketAsyncEventArgs();
				//SocketAsyncEventArgs.Completed -> OnAcceptCompleted 등록				
				args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
				//초기화 시점에서 한번등록
				RegisterAccept(args);
			}
		}

		//RegisterAccept -> OnAcceptCompleted -> RegisterAccept 가 반복

		void RegisterAccept(SocketAsyncEventArgs args)
		{
			args.AcceptSocket = null;			
			//들어오는 연결시도를 받아 들이는 비동기 작업 시작
			bool pending = _listenSocket.AcceptAsync(args);
			//연결시도 가 실패일때 하위 실행			
			if (pending == false)
				OnAcceptCompleted(null, args);

			
		}

		/// <summary>
		/// 클라이언트가 연결 성공시 -> Session 시작 -> 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
		{
			//연결성공시
			if (args.SocketError == SocketError.Success)
			{				
				//위의 Init에서 _sessionFactory에 등록된 함수를 실행
				//등록된 함수는 SessionManager.Instance.Generate()이다.
				//SessionManager.Instance.Generate() -> SessionManager.Generate() -> new Session()
				//즉 플레이어가 접속할때마다 새로운 세션을 생성한다.
				//이렇게 등록된 세션은 SessionManager의 _sessions에 저장된다.
				Session session = _sessionFactory.Invoke();
				//session.Start -> args에 연결된 socket을 전달 <- 이건 tcpsocket에 연결되어있다.

				session.Start(args.AcceptSocket);
				session.OnConnected(args.AcceptSocket.RemoteEndPoint);
			}
			else
				Console.WriteLine(args.SocketError.ToString());

			//위의 과정이 등록 완료시 다음사람을 위해 등록
			RegisterAccept(args);
		}
	}
}
