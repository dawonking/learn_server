using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCore
{
	//GameRoom과 JobQueue에서 사용
	public interface IJobQueue
	{
		void Push(Action job);
	}

	public class JobQueue : IJobQueue
	{
		Queue<Action> _jobQueue = new Queue<Action>();
		object _lock = new object();
		bool _flush = false;

		public void Push(Action job)
		{
			
			bool flush = false;

			lock (_lock)
			{
                //받은 델리게이트를 _jobQueue에 넣는다.
                _jobQueue.Enqueue(job);
				//큐가 전부 비워지면 false로 반환
				if (_flush == false)
					//상태동일하게
					flush = _flush = true;
			}

			if (flush)
				Flush();
		}

		void Flush()
		{
			//추출, 및 적용
			while (true)
			{
				Action action = Pop();
				if (action == null)
					return;

				action.Invoke();
			}
		}

		Action Pop()
		{
			lock (_lock)
			{
				if (_jobQueue.Count == 0)
				{
					//전부 없을시 전환
					_flush = false;
					return null;
				}
				return _jobQueue.Dequeue();
			}
		}
	}
}
