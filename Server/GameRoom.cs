using ServerCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
	class GameRoom : IJobQueue
	{
		List<ClientSession> _sessions = new List<ClientSession>();
		JobQueue _jobQueue = new JobQueue();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();

		/// <summary>
		/// 함수푸쉬,Program에서 () => Room.Flush() 푸쉬
		/// </summary>
		/// <param name="job"></param>
		public void Push(Action job)
		{
			//JobQueue의 Push를 호출			
            //어디서? 누가 적용되나?
            //1.Room에서 FlushRoom에서 처음 적용
            //2.ClientSession에서 OnDisconnected에서 적용
			//3.FlushRoom에서 적용, GameRoom에서의 Flush()적용
            _jobQueue.Push(job);
		}

		/// <summary>
		/// 기본적인 통신들은 전부 _pendingList 에 넣어두고 program에서 일정주기로 실행
		/// </summary>
		//
		public void Flush()
		{
			//룸의 모든 캐릭터들에게 전송
			//ClinetSession모두에게전달
			foreach (ClientSession s in _sessions)
				s.Send(_pendingList);
			//Console.WriteLine($"Flushed {_pendingList.Count} items");
			_pendingList.Clear();			
		}

		/// <summary>
		/// 플레이어의 이동, 입장, 퇴장을 다른 플레이어들에게 전달
		/// </summary>
		/// <param name="segment"></param>
		public void Broadcast(ArraySegment<byte> segment)
		{
			_pendingList.Add(segment);			
		}

		//플레이어 진입
		//ClientSession에서 Onconnect시에 실행
		public void Enter(ClientSession session)
		{
			// 플레이어 추가하고
			//SessionManager는 ClientSession을 관리하는 클래스
			//GameRoom의 _sessions는 룸 내부에잇는 ClinetSession만 관리
			_sessions.Add(session);
			session.Room = this;

			// 신입생한테 모든 플레이어 목록 전송
			S_PlayerList players = new S_PlayerList();

			//룸내의 모든 클라이언트에게 전달
			foreach (ClientSession s in _sessions)
			{
				players.players.Add(new S_PlayerList.Player()
				{
					isSelf = (s == session),
					playerId = s.SessionId,
					posX = s.PosX,
					posY = s.PosY,
					posZ = s.PosZ,
				});
			}

			session.Send(players.Write());

			// 신입생 입장을 모두에게 알린다
			S_BroadcastEnterGame enter = new S_BroadcastEnterGame();
			enter.playerId = session.SessionId;
			enter.posX = 0;
			enter.posY = 0;
			enter.posZ = 0;
			Broadcast(enter.Write());
		}

		public void Leave(ClientSession session)
		{
			// 플레이어 제거하고
			_sessions.Remove(session);

			// 모두에게 알린다
			S_BroadcastLeaveGame leave = new S_BroadcastLeaveGame();
			leave.playerId = session.SessionId;
			Broadcast(leave.Write());
		}

		public void Move(ClientSession session, C_Move packet)
		{
			// 좌표 바꿔주고
			session.PosX = packet.posX;
			session.PosY = packet.posY;
			session.PosZ = packet.posZ;

			// 모두에게 알린다
			S_BroadcastMove move = new S_BroadcastMove();
			move.playerId = session.SessionId;
			move.posX = session.PosX;
			move.posY = session.PosY;
			move.posZ = session.PosZ;
			Broadcast(move.Write());
		}
	}
}
