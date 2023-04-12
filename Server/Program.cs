using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;

namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();
		public static GameRoom Room = new GameRoom();

		

		static void FlushRoom()
		{
			//한번 룸에 푸쉬해 실행
			Room.Push(() => Room.Flush());
			//다음실행을 미리 예약
			//상위 루프에서 Flush()실행
			JobTimer.Instance.Push(FlushRoom, 250);
		}

		static void Main(string[] args)
		{						

			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 9999);
			//ip, port정의

			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

			//FlushRoom();
			//JobTimer->push(action)
			JobTimer.Instance.Push(FlushRoom);

			//유니티의 메인 업데이트?
			while (true)
			{
				//무한반복돌면서 Flush() 실행
				//현재 시각이 작업시각이 넘었는지 확인 후 작업
				//그렇지 않다면 그냥빠져나옴
				JobTimer.Instance.Flush();
			}
		}
	}
}
