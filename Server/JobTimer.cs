using System;
using System.Collections.Generic;
using System.Text;
using ServerCore;

namespace Server
{

	//하위 CompareTo 사용을 위해 IComparable사용
	struct JobTimerElem : IComparable<JobTimerElem>
	{
		public int execTick; // 실행 시간
		public Action action;

		public int CompareTo(JobTimerElem other)
		{
			return other.execTick - execTick;
		}
	}

	class JobTimer
	{
		PriorityQueue<JobTimerElem> _pq = new PriorityQueue<JobTimerElem>();
		object _lock = new object();

		public static JobTimer Instance { get; } = new JobTimer();

		/// <summary>
		/// Action, tickAfter
		/// </summary>
		/// <param name="action"></param>
		/// <param name="tickAfter"></param>
		public void Push(Action action, int tickAfter = 0)
		{
			JobTimerElem job;
			//현재 시간 + 입력시간 -> +된 시간에 전송되어야 한다.
			//실행하는 타임
			job.execTick = System.Environment.TickCount + tickAfter;
			job.action = action;

			//다른스레드에서 접근 불가로 만들
			lock (_lock)
			{
				//우선순위정렬큐에 넣기
				//우선순위큐 리스트에 삽입 후 정렬
				_pq.Push(job);
			}
		}

		//큐를 비우다
		public void Flush()
		{
			while (true)
			{
				//현재시간
				int now = System.Environment.TickCount;
				JobTimerElem job;

				//우선순위 큐에서 하나씩 가져온다.
				lock (_lock)
				{
					//큐가 없으면 나옴
					if (_pq.Count == 0)
						break;

					//큐의 최상단 확인
					job = _pq.Peek();
					
					//아직 실행할 시간이 아니니 나오게 된다.
					if (job.execTick > now)
						break;

					//
					_pq.Pop();
				}

				//꺼내온 작업을 실행
				job.action.Invoke();
			}
		}
	}
}
