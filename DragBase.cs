using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRageMath;
using VRage;

namespace SEDrag
{
	public class DragBase
	{
		internal MyLight burninglight;
		internal bool m_showlight = false;
		internal StringBuilder HeatNotification = new StringBuilder();

		internal bool showsmoke
		{
			get
			{
				return CoreDrag.instance.Pref.Smoke;
			}
		}
		internal bool showlight
		{
			get
			{
				if (CoreDrag.instance.Pref.Burn)
					return m_showlight;
				else
					return false;
			}
			set
			{
				m_showlight = value;
			}
		}
		public virtual void Update()
		{

		}
		public virtual void Close()
		{

		}
		public virtual Vector3D GetWind()
		{
			return Vector3D.Zero;
		}

		public virtual BoundingBox GetBoundingBox()
		{
			return new BoundingBox(Vector3.MinusOne, Vector3.One);
		}

		public virtual MyTuple<double, double, double, double, double, double> GetHeat()
		{
			return new MyTuple<double, double, double, double, double, double>(0d, 0d, 0d, 0d, 0d, 0d);
		}

        public virtual double CheckOcclusion()
		{
			return 0d;
		}

		public virtual void WriteHeat(CoreDrag.HeatDataMessage heatData)
		{
			
		}

		internal double getLiftCI(double width, float x)
		{
			//double _ret = Math.Pow(width, 2) * x;
			return ((Math.Cos(x * Math.PI * 2) * -1 + 1) / 2 * width * width * Math.Sign(x));// 
		}
		internal double getArea(BoundingBox dragBox, Vector3 _v, ref double areawidth, ref double areaheight, ref double areadepth)
		{
			areawidth = dragBox.Width * Math.Abs(_v.X);// ((-Math.Cos((Math.Abs(_v.X)) * Math.PI) + 1) / 2);////Math.Abs(_v.X)
			areaheight = dragBox.Height * Math.Abs(_v.Y);// ((-Math.Cos((Math.Abs(_v.X)) * Math.PI) + 1) / 2);//Math.Abs(_v.Y)
			areadepth = dragBox.Depth * Math.Abs(_v.Z);// ((-Math.Cos((Math.Abs(_v.X)) * Math.PI) + 1) / 2);//Math.Abs(_v.Z)
			return Math.Pow(areawidth + areaheight + areadepth, 2);
		}

		internal string getColor(double heat)
		{
			if (heat > 750)
				return "red";
			else if (heat > 500)
				return "yellow";
			return "white";

		}



		internal void ShowNotification(string str, double value, int ms, string color)
		{

			if (!CoreDrag.instance.NewHud)
				MyAPIGateway.Utilities.ShowNotification(string.Format(str, value, ""), ms, color);
			else
			{
				HeatNotification.Clear();
				CoreDrag.instance.HeatAlarmVisible(true);
				HeatNotification.AppendFormat(str, value, string.Format("<color={0}>", color == MyFontEnum.White ? "white" : "red"));
				CoreDrag.instance.UpdateHeatAlarm(HeatNotification);
				//CoreDrag.instance.TextHUD.Send(new HUDTextNI.HUDMessage(2, 5, new Vector2D(0, -0.2), 1, true, false, Color.Black, message));
			}
		}
	}
}
