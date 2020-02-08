using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParallelTasks;

namespace SEDrag
{
	internal class ThreadManager
	{
		Queue<ThreadManagerTask> ProcessQueue = new Queue<ThreadManagerTask>(100);
		Stack<ThreadManagerTask> TaskPool = new Stack<ThreadManagerTask>(20);
		Task CurrentTask;
		ThreadManagerTask ThreadTask;
		ThreadManagerTask EnqueueTask;
		internal void Update()
		{

			if (CurrentTask.IsComplete)
			{
				
				if(ThreadTask != null)
				{
					ThreadTask.IsComplete = true;
					TaskPool.Push(ThreadTask);//return to the pool. 
					ThreadTask = null;
				}
				if (ProcessQueue.TryDequeue(out ThreadTask))
				{
					CurrentTask = ThreadTask.Run();

				}
				else
				{

				}
			}
		}


		internal bool Add(Action refreshDragBox, Action calcComplete)
		{
			if (TaskPool.Count == 0)
			{
				EnqueueTask = new ThreadManagerTask(refreshDragBox, calcComplete);
			}
			else
			{
				EnqueueTask = TaskPool.Pop();
				EnqueueTask.Init(refreshDragBox, calcComplete);
			}


			if (ProcessQueue.Count < 100)
			{
				EnqueueTask.Added = true;
				ProcessQueue.Enqueue(EnqueueTask);
			}
			else
			{
				EnqueueTask.Added = false;
			}
			return EnqueueTask.Added;
		}
	}
}
