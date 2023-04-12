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

	//key = ushort , Func �� PacketSession�� �Ű������� �ް� , ArraySegment�������� ��ȯ
	Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
	Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();
	
	//_makeFunc - �������� Ȯ��
	//_handler - ���� ���� �� Ȯ��

	public void Register()
	{
		//�ʱ� ���۽� _makeFunc�� ������/ ������ �� ���
		_makeFunc.Add((ushort)PacketID.C_LeaveGame, MakePacket<C_LeaveGame>);		
		_makeFunc.Add((ushort)PacketID.C_Move, MakePacket<C_Move>);
		
		_handler.Add((ushort)PacketID.C_LeaveGame, PacketHandler.C_LeaveGameHandler);
		_handler.Add((ushort)PacketID.C_Move, PacketHandler.C_MoveHandler);

	}

    //Action�� ��ȯ���� ���� �޼��带 ����, �� ��ȯ���� void�� �޼���.
    //���� ��� Action<int, string>�� int�� string ������ �Ű������� �ް� ��ȯ ���� ���� �޼��� 
    //PacketManager.Instance.OnRecvPacket(this, buffer);
	//���� ����, buffer,
    public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> onRecvCallback = null)
	{
		ushort count = 0;

		//���ۿ��� ��Ŷ�� ũ�⸦ �����ɴϴ�.
		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		//���ۿ��� ��Ŷ�� ID�� �����ɴϴ�.
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		//�� �ʱ� ũ�⸦ ������ -> ������ ��Ŷ ���̵� �����´� -> �׵� _makeFunc�� ��ŶID�� �´� ���� ����

		//��Ŷ�� �����ϴ� �Լ��� ������ ������ �����մϴ�
		Func<PacketSession, ArraySegment<byte>, IPacket> func = null;		
		//��Ŷ ID�� ������ �Լ��� ã���ϴ�.
		//�����̴���? ������������? �� Ȯ��
		if (_makeFunc.TryGetValue(id, out func))
		{
			//ã�� �Լ��� ����Ͽ� ��Ŷ�� �����մϴ�.
			//id�� ���� _makeFunc�� ��ϵ� ���� ã�´�.
			//���� _makeFunc.TryGetValue�� id�� �´°��� ã�� , out func�� �����Ѵ�.
			//func.Invoke(session, buffer) -> func�� ����Ȱ��� �����Ѵ�.
			//�׸��� ��ϵ� func�� invoke�Ѵ�.
			//������� 5���� ��� C_Move�̴�.
			IPacket packet = func.Invoke(session, buffer);
			//������ ���� �ݹ��� �ִ��� Ȯ��
			if (onRecvCallback != null)
				onRecvCallback.Invoke(session, packet);
			else				
				//ó�� Ŭ���̾�Ʈ�� ����� ��Ŷ����
				//protocol = 5 ������ 0 / 5�� C_Move�̴�.
				HandlePacket(session, packet);
        }
    }

	//_makeFunc.Add((ushort)PacketID.C_LeaveGame, MakePacket<C_LeaveGame>);	
	//where T : IPacket , new() -> T�� �ݵ�� �Ű������� ���� �����ڰ� �־�� �Ѵ�.
	//�� �̷��� �ϴ°�? -> �Ϲ�ȭ�� ����ϸ� �ڵ��� �ߺ��� ���̰� �������� ���ϼ� �ִ�.
	//���� ���÷� ���� MakePacket<C_LeaveGame> �ε� C_LeaveGame�� IPacket�� ��ӹ޴µ�
	//IPacket�� protocol , Read , write�� ������ �ִ�.
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