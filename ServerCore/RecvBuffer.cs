using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
	public class RecvBuffer
	{
		// [r][][w][][][][][][][]
		ArraySegment<byte> _buffer;
		//컨텐츠코드에서 사용,
		//현재까지 받은 바이트를 사용할때 기준점이된다.
		int _readPos;
		//recv시 받은만큼 이동
		int _writePos;

		public RecvBuffer(int bufferSize)
		{
			_buffer = new ArraySegment<byte>(new byte[bufferSize], 0, bufferSize);
		}

		//쓰기위치 - 읽기위치, 현재 쓰이고 있는 데이터의 크기
		//유효범위
		public int DataSize { get { return _writePos - _readPos; } }
		//현재 버퍼 크기 - 쓰기위치, 남은 싸이즈
		//남은공간
		public int FreeSize { get { return _buffer.Count - _writePos; } }

		/// <summary>
		/// 받아들인 버퍼
		/// </summary>
		public ArraySegment<byte> ReadSegment
		{
			//[0].....[_readPos] => 0에서 _readPos만큼 반환
			get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _readPos, DataSize); }
		}

		public ArraySegment<byte> WriteSegment
		{
			//현재위치 + writePos , 
			get { return new ArraySegment<byte>(_buffer.Array, _buffer.Offset + _writePos, FreeSize); }
		}

		public void Clean()
		{
			int dataSize = DataSize;
			if (dataSize == 0)
			{
				// 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
				_readPos = _writePos = 0;
			}
			else
			{
				// 남은 찌끄레기가 있으면 시작 위치로 복사
				Array.Copy(_buffer.Array, _buffer.Offset + _readPos, _buffer.Array, _buffer.Offset, dataSize);
				_readPos = 0;
				_writePos = dataSize;
			}
		}

		public bool OnRead(int numOfBytes)
		{
			//받은 사이즈가 오버시 종료
			if (numOfBytes > DataSize)
				return false;

			_readPos += numOfBytes;
			return true;
		}
		
		public bool OnWrite(int numOfBytes)
		{
			//들어온 바이트 수가 얼마나 큰지 확인
			if (numOfBytes > FreeSize)
				//오버시 false
				return false;

			//_writePos 이동
			_writePos += numOfBytes;
			return true;
		}
	}
}
