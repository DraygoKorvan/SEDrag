using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRageMath;

namespace SEDrag
{
	public class GridCalculation
	{
		internal Vector3D centerOfLift = Vector3D.Zero;
		internal bool dirty = false;
		internal BoundingBox dragBox = BoundingBox.CreateFromHalfExtent(Vector3.Zero, 1);
		//internal Dictionary<side, MyBlockOrientation> o_xmax = new Dictionary<side, MyBlockOrientation>();
		//internal Dictionary<side, MyBlockOrientation> o_xmin = new Dictionary<side, MyBlockOrientation>();
		//internal Dictionary<side, MyBlockOrientation> o_ymax = new Dictionary<side, MyBlockOrientation>();
		//internal Dictionary<side, MyBlockOrientation> o_ymin = new Dictionary<side, MyBlockOrientation>();
		//internal Dictionary<side, MyBlockOrientation> o_zmax = new Dictionary<side, MyBlockOrientation>();
		//internal Dictionary<side, MyBlockOrientation> o_zmin = new Dictionary<side, MyBlockOrientation>();
		internal bool skipDrag = false;
		//internal Dictionary<side, string> s_xmax = new Dictionary<side, string>();
		//internal Dictionary<side, string> s_xmin = new Dictionary<side, string>();
		//internal Dictionary<side, string> s_ymax = new Dictionary<side, string>();
		//internal Dictionary<side, string> s_ymin = new Dictionary<side, string>();
		//internal Dictionary<side, string> s_zmax = new Dictionary<side, string>();
		//internal Dictionary<side, string> s_zmin = new Dictionary<side, string>();
		internal Dictionary<side, Vector3I> xmax = new Dictionary<side, Vector3I>();
		internal Dictionary<side, Vector3I> xmin = new Dictionary<side, Vector3I>();
		internal Dictionary<side, Vector3I> ymax = new Dictionary<side, Vector3I>();
		internal Dictionary<side, Vector3I> ymin = new Dictionary<side, Vector3I>();
		internal Dictionary<side, Vector3I> zmax = new Dictionary<side, Vector3I>();
		internal Dictionary<side, Vector3I> zmin = new Dictionary<side, Vector3I>();
		internal bool Reset = false;

		internal void Clear()
		{
			centerOfLift = Vector3D.Zero;
			dirty = false;
			dragBox = BoundingBox.CreateFromHalfExtent(Vector3.Zero, 1);
			//o_xmax.Clear();
			//o_xmin.Clear();
			//o_ymax.Clear();
			//o_ymin.Clear();
			//o_zmax.Clear();
			//o_zmin.Clear();
			skipDrag = false;
			//s_xmax.Clear();
			//s_xmin.Clear();
			//s_ymax.Clear();
			//s_ymin.Clear();
			//s_zmax.Clear();
			//s_zmin.Clear();
			xmax.Clear();
			xmin.Clear();
			ymax.Clear();
			ymin.Clear();
			zmax.Clear();
			zmin.Clear();
			Reset = false;
		}
	}
}