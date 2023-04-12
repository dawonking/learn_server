using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/*
 Session이란?

네트워크 분야에서 반영구적이고 상호작용적인 
정보 교환을 전제하는 둘 이상의 통신 장치나 컴퓨터와 사용자 간의 
대화나 송수신 연결상태를 의미하는 보안적인 다이얼로그(dialogue) 및 시간대를 가리킨다. 
따라서 세션은 연결상태를 유지하는것보다 연결상태의 안정성을 더 중요시 하게된다.
간단하게 말하면 서버와 클라간의 데이터가 오고가는 통로라고 이해하면 된다.
세션으로 통로를 만들고 packet(데이터 형식과 양)을 양방향에서 주고 받으며 통신이 이어진다. 
 */

namespace ServerCore
{

	public abstract class PacketSession : Session
	{
		public static readonly int HeaderSize = 2;

		// [size(2)][packetId(2)][ ... ][size(2)][packetId(2)][ ... ]
		//sealed 재정의 할수없게 한다.
		//위 배열에서 size는 총 데이터 크기이다.
		//예를들어 헤더에서 size(2)에서 얼마만큼의 데이터가 들어왔는지로 데이터를 읽어들인다.
		public sealed override int OnRecv(ArraySegment<byte> buffer)
		{
			//내가 몇바이트를 처리했는지?
			int processLen = 0;
			int packetCount = 0;

			while (true)
			{
				// 최소한 헤더는 파싱할 수 있는지 확인
				if (buffer.Count < HeaderSize)
					break;

				// 패킷이 완전체로 도착했는지 확인
				ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
				if (buffer.Count < dataSize)
					break;

				// 여기까지 왔으면 패킷 조립 가능
				OnRecvPacket(new ArraySegment<byte>(buffer.Array, buffer.Offset, dataSize));
				packetCount++;

				processLen += dataSize;
				buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + dataSize, buffer.Count - dataSize);
			}

			if (packetCount > 1)
				Console.WriteLine($"패킷 모아보내기 : {packetCount}");

			return processLen;
		}

		//ClientSession에서 Override정의
		//
		public abstract void OnRecvPacket(ArraySegment<byte> buffer);
	}

	

	// abstract 는 선언시 반드시 구현해야하는 것들이다.	
	public abstract class Session
	{
		Socket _socket;
		int _disconnected = 0;

		
		RecvBuffer _recvBuffer = new RecvBuffer(65535);

		object _lock = new object();
		Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
		List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
		SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
		SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

		public abstract void OnConnected(EndPoint endPoint);
		public abstract int  OnRecv(ArraySegment<byte> buffer);
		public abstract void OnSend(int numOfBytes);
		public abstract void OnDisconnected(EndPoint endPoint);

		void Clear()
		{
			lock (_lock)
			{
				_sendQueue.Clear();
				_pendingList.Clear();
			}
		}

		public void Start(Socket socket)
		{			
			//Listener에서 전달받은 tcp와 udp소켓을 전달받음
			//
			_socket = socket;
			//송수신 이벤트 등록
			//만약 udp도 추가된다면 udp관련 이벤트도 추가
			_recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
			_sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

			RegisterRecv();
		}

		public void Send(List<ArraySegment<byte>> sendBuffList)
		{
			if (sendBuffList.Count == 0)
				return;

			lock (_lock)
			{
				foreach (ArraySegment<byte> sendBuff in sendBuffList)
					_sendQueue.Enqueue(sendBuff);

				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Send(ArraySegment<byte> sendBuff)
		{
			lock (_lock)
			{
				_sendQueue.Enqueue(sendBuff);
				if (_pendingList.Count == 0)
					RegisterSend();
			}
		}

		public void Disconnect()
		{
			//현재 세션에서의 디스커넥트 여부 확인
			if (Interlocked.Exchange(ref _disconnected, 1) == 1)
				return;

			OnDisconnected(_socket.RemoteEndPoint);
			_socket.Shutdown(SocketShutdown.Both);
			_socket.Close();
			Clear();
		}

		#region 네트워크 Send 통신

		//Send에서 사용, 비동기 송신, 송신 완료시 OnSendCompleted호출
		void RegisterSend()
		{
			if (_disconnected == 1)
				return;

			while (_sendQueue.Count > 0)
			{
				ArraySegment<byte> buff = _sendQueue.Dequeue();
				_pendingList.Add(buff);
			}
			_sendArgs.BufferList = _pendingList;

			try
			{
				bool pending = _socket.SendAsync(_sendArgs);
				if (pending == false)
					OnSendCompleted(null, _sendArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterSend Failed {e}");
			}
		}

		void OnSendCompleted(object sender, SocketAsyncEventArgs args)
		{
			lock (_lock)
			{
				if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
				{
					try
					{
						_sendArgs.BufferList = null;
						_pendingList.Clear();

						//몇바이트 보냈는지 
						//OnSend(_sendArgs.BytesTransferred);

						if (_sendQueue.Count > 0)
							RegisterSend();
					}
					catch (Exception e)
					{
						Console.WriteLine($"OnSendCompleted Failed {e}");
					}
				}
				else
				{
					Disconnect();
				}
			}
		}

        #endregion

        #region 네트워크 Recv 통신

        void RegisterRecv()
		{
			if (_disconnected == 1)
				return;

			//버퍼초기화
			_recvBuffer.Clean();

			ArraySegment<byte> segment = _recvBuffer.WriteSegment;
			//배열, 시작위치 , 사용할 바이트 수
			_recvArgs.SetBuffer(segment.Array, segment.Offset, segment.Count);

			try
			{
				//ReceiveAsync - 비동기적으로 데이터를 소켓에서 수신하기위해 사용
				//사용법
				//비동기 이벤트 객체 생성, 이벤트 핸들러를 설정
				//수신할 데이터를 저장할 버퍼를 설정
				//ReceiveAsync 호출하여 비동기 수신작업 시작
				//데이터를 수신, 해당 데이터를 지정된 버퍼에 저장하는 작업을 백그라운드에 실행, 완료된 후 완료이벤트를 발생
				//_recvArgs에 등록된 
				bool pending = _socket.ReceiveAsync(_recvArgs);
				if (pending == false)
					OnRecvCompleted(null, _recvArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine($"RegisterRecv Failed {e}");
			}
		}

		void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
		{
			//받은 바이트수가 0보다 크고, 연결 상태가 true 일때
			if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
			{
				try
				{
					// Write 커서 이동
					// recv에서 받은 버퍼크기가 
					if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
					{
						Disconnect();
						return;
					}

					// 컨텐츠 쪽으로 데이터를 넘겨주고 얼마나 처리했는지 받는다
					// 현재 들여온 값들만 가지고 계산
					// 예를들어 저장크기가 1024이지만 , 0~100까지만 데이터를 받았다면
					// 전체를 읽는게 아니라 0~100까지만 읽는다.
					int processLen = OnRecv(_recvBuffer.ReadSegment);
					if (processLen < 0 || _recvBuffer.DataSize < processLen)
					{
						Disconnect();
						return;
					}

					// Read 커서 이동
					if (_recvBuffer.OnRead(processLen) == false)
					{
						Disconnect();
						return;
					}

					//이벤트 완료후 재등록
					RegisterRecv();
				}
				catch (Exception e)
				{
					Console.WriteLine($"OnRecvCompleted Failed {e}");
				}
			}
			else
			{
				Disconnect();
			}
		}

		#endregion
	}
}
