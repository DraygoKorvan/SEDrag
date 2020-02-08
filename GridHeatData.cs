using System;

namespace SEDrag
{
	public class GridHeatData
	{
		public double front = 0;
		public double back = 0;
		public double up = 0;
		public double down = 0;
		public double left = 0;
		public double right = 0;

		public int stage_front = 0;
		public int stage_back = 0;
		public int stage_up = 0;
		public int stage_down = 0;
		public int stage_left = 0;
		public int stage_right = 0;

		public GridHeatData()
		{

		}
		public GridHeatData(double l, double r, double u, double d, double f, double b)
		{
			front = f;
			back = b;
			up = u;
			down = d;
			left = l;
			right = r;
		}
		public void Clear()
		{
			front = 0;
			back = 0;
			up = 0;
			down = 0;
			left = 0;
			right = 0;

			stage_front = 0;
			stage_back = 0;
			stage_up = 0;
			stage_down = 0;
			stage_left = 0;
			stage_right = 0;
		}

		internal void Copy(GridHeatData heatData)
		{
			front = heatData.front;
			back = heatData.back;
			up = heatData.up;
			down = heatData.down;
			left = heatData.left;
			right = heatData.right;
		}
		internal void Copy(CoreDrag.HeatDataMessage heatData)
		{
			front = heatData.Front;
			back = heatData.Back;
			up = heatData.Up;
			down = heatData.Down;
			left = heatData.Left;
			right = heatData.Right;
		}
	}
}
