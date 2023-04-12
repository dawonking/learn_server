using ServerCore;
using System;
using System.Collections.Generic;

public class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	PacketManager()
	{
		Register();
	}

	//key = ushort , Func 는 PacketSession을 매개변수로 받고 , ArraySegment형식으로 반환
	Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
	Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();
	
	//_makeFunc - 보유형식 확인
	//_handler - 실제 적용 및 확인

	public void Register()
	{
		//초기 시작시 _makeFunc에 떠나기/ 움직임 을 등록
		_makeFunc.Add((ushort)PacketID.C_LeaveGame, MakePacket<C_LeaveGame>);		
		_makeFunc.Add((ushort)PacketID.C_Move, MakePacket<C_Move>);
		
		_handler.Add((ushort)PacketID.C_LeaveGame, PacketHandler.C_LeaveGameHandler);
		_handler.Add((ushort)PacketID.C_Move, PacketHandler.C_MoveHandler);

	}

    //Action은 반환값이 없는 메서드를 참조, 즉 반환값이 void인 메서드.
    //예를 들어 Action<int, string>은 int와 string 형식의 매개변수를 받고 반환 값이 없는 메서드 
    //PacketManager.Instance.OnRecvPacket(this, buffer);
	//현재 세션, buffer,
    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
	{
		ushort count = 0;

		//버퍼에서 패킷의 크기를 가져옵니다.
		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		//버퍼에서 패킷의 ID를 가져옵니다.
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		//즉 초기 크기를 가져와 -> 버퍼의 패킷 아이디를 가져온다 -> 그뒤 _makeFunc에 패킷ID에 맞는 역할 시행

		//패킷을 생성하는 함수를 저장할 변수를 선언합니다
		Func<PacketSession, ArraySegment<byte>, IPacket> func = null;		
		//패킷 ID와 연관된 함수를 찾습니다.
		//움직이는지? 방을떠나는지? 가 확인
		if (_makeFunc.TryGetValue(id, out func))
		{
			//찾은 함수를 사용하여 패킷을 생성합니다.
			//id를 통해 _makeFunc에 등록된 것을 찾는다.
			//위의 _makeFunc.TryGetValue는 id에 맞는것을 찾고 , out func에 저장한다.
			//func.Invoke(session, buffer) -> func에 저장된것을 실행한다.
			//그리고 등록된 func를 invoke한다.
			//예를들어 5번의 경우 C_Move이다.
			IPacket packet = func.Invoke(session, buffer);
			//사전에 받은 콜백이 있는지 확인
			if (onRecvCallback != null)
				onRecvCallback.Invoke(session, packet);
			else				
				//처음 클라이언트가 입장시 패킷구성
				//protocol = 5 나머지 0 / 5는 C_Move이다.
				HandlePacket(session, packet);
        }
    }

	//_makeFunc.Add((ushort)PacketID.C_LeaveGame, MakePacket<C_LeaveGame>);	
	//where T : IPacket , new() -> T는 반드시 매개변수가 없는 생성자가 있어야 한다.
	//왜 이렇게 하는가? -> 일반화를 사용하면 코드의 중복을 줄이고 유연성을 높일수 있다.
	//위의 예시로 볼때 MakePacket<C_LeaveGame> 인데 C_LeaveGame은 IPacket을 상속받는데
	//IPacket은 protocol , Read , write를 가지고 있다.
	T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
    {
        T pkt = new T();
        pkt.Read(buffer);
        return pkt;
    }


    public void HandlePacket(PacketSession session, IPacket packet)
	{
		Action<PacketSession, IPacket> action = null;
		if (_handler.TryGetValue(packet.Protocol, out action))
			action.Invoke(session, packet);
	}
}