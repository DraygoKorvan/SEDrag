using Sandbox.ModAPI;
using System;
using ParallelTasks;

namespace SEDrag
{
	internal class ThreadManagerTask
	{
		//can be set in other threads. This will be set to false so the thread management knows to skip. 
		internal bool Valid = true;
		internal bool IsComplete = false;
		internal bool Added;
		private Action calcComplete;
		private Action refreshDragBox;

		public ThreadManagerTask()
		{
			Valid = false;
		}

		public ThreadManagerTask(Action refreshDragBox, Action calcComplete)
		{
			Init(refreshDragBox, calcComplete);
		}

		public void Init(Action refreshDragBox, Action calcComplete)
		{
			this.refreshDragBox = refreshDragBox;
			this.calcComplete = calcComplete;
			Valid = true;
		}

		internal Task Run()
		{
			if(Valid)
			{
				return MyAPIGateway.Parallel.StartBackground(refreshDragBox, calcComplete);
			}
			return MyAPIGateway.Parallel.StartBackground(Wait);
		}
		public void Wait()
		{

		}


	}
}