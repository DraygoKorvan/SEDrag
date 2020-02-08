using Sandbox.Definitions;
using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRage;
using Sandbox.Game.Entities;

namespace SEDrag
{
	class CharacterDrag : DragBase
	{
		IMyCharacter m_Character;
		//bool dirty = false;
		bool registered = false;
		bool init = false;

		BoundingBox dragBox;
		private float drag = 0f;
		private double heat = 0d;
		private double heatCache = 0f;
		double heatDelta = 0d;
		private bool OccludeDirty = true;

		Vector3D ClosetPlanet = Vector3D.MaxValue;

		private Vector3D LastWind = Vector3D.Zero;
		private StringBuilder HeatWarning = new StringBuilder();


		private float small_max
		{
			get
			{
				return CoreDrag.instance.small_max;
			}
		}
		private float large_max
		{
			get
			{
				return CoreDrag.instance.large_max;
			}

		}
		private float Max_Speed
		{
			get
			{
				return Math.Max(small_max, large_max);
			}
		}

		public CharacterDrag(IMyEntity obj)
		{

			if (CoreDrag.instance == null)
				return;
			m_Character = obj as IMyCharacter;
			if (m_Character != null)
			{
				CoreDrag.instance.Register(obj, this);
				registered = true;
                CoreDrag.UpdateHook += Update;//register
				CoreDrag.DrawHook += Draw;
				dragBox = m_Character.LocalAABB;
			}
		}

		public override void Update()
		{
			

			if (CoreDrag.instance == null)
				return;
			
			if (m_Character.Physics == null || !m_Character.Physics.Enabled || m_Character.Physics.Mass == 0f)
			{
				
				return;
			}
			if (m_Character.MarkedForClose)
				return;




			if (!(CoreDrag.instance.isServer || ( MyAPIGateway.Session?.ControlledObject?.Entity?.EntityId == m_Character?.EntityId) ))
			{
				
				return;//save cycles
			}
			if (!CoreDrag.instance.isServer)
			{
				if (!CoreDrag.instance._recievedGameSettings)
				{
					
					return;
				}
					
			}
			if (!CoreDrag.instance.settings.EnableCharacterDrag)
			{
				if(!CoreDrag.instance.isDedicated && (MyAPIGateway.Session?.ControlledObject?.Entity?.EntityId == m_Character?.EntityId))
				{
					CoreDrag.instance.HeatWarningVisible(false);
					//CoreDrag.instance.UpdateHeatWarning(HeatWarning);
				}
				
				return;
			}
			
			iterator++;
			iterator %= 80;


			var dragForce = Vector3.Zero;
			float atmosphere = 0;
			Vector3D WindDirection = Vector3D.Zero;
			ClosetPlanet = Vector3D.MaxValue;
            Vector3D CharPosition = m_Character.GetPosition();

			foreach (var kv in CoreDrag.instance.planets)
			{
				var planet = kv.Value;

				if (planet.Closed || planet.MarkedForClose)
				{
					continue;
				}

				if (planet.HasAtmosphere)
				{
					if (iterator % 10 == 0 || ClosetPlanet == Vector3D.MaxValue)
					{

						//every 10 ticks
						Vector3D CP = planet.GetClosestSurfacePointGlobal(ref CharPosition) - CharPosition;

						if (CP.LengthSquared() < ClosetPlanet.LengthSquared())
							ClosetPlanet = CP;
					}

					var add = planet.GetAirDensity(CharPosition);
					if (add > 0f)
					{
						atmosphere += add;
						if(CoreDrag.instance.settings.SimulateWind)
							WindDirection += CoreDrag.instance.GetWeatherAtPoint(planet, CharPosition, ClosetPlanet.Length(), m_Character) * add;
					}

				}
			}


			heatLoss(atmosphere);
				
			if (m_Character.Parent != null)
			{
				
				return;
			}


			overheatCheck();
			if(!CoreDrag.instance.isDedicated)
				refreshLightGrid();

			LastWind = Vector3D.Lerp(LastWind, WindDirection, 0.016d);
			if (CoreDrag.instance.settings.AtmosphericMinimum > 0)
			{
				if (atmosphere < CoreDrag.instance.settings.AtmosphericMinimum / 100f)
				{
					atmosphere = CoreDrag.instance.settings.AtmosphericMinimum / 100f;
				}
			}
			if (atmosphere < 0.02f)
			{
				
				return;
			}


			CalculateOcclusion();
			dragForce = -m_Character.Physics.LinearVelocity + (LastWind);

			//workaround for some funny stuff
			var dLen = dragForce.Length();

			dLen = MathHelper.Min(dLen, Max_Speed * 2);





			Vector3 dragNormal = Vector3.Normalize(dragForce);
			dragForce = dragNormal * dLen;

			MatrixD dragMatrix = MatrixD.CreateFromDir(dragNormal);
			MatrixD mat = MatrixD.Invert(m_Character.WorldMatrix);
			dragMatrix = dragMatrix * mat;
				
			double aw = 0;
			double ah = 0;
			double ad = 0;
			double a = getArea(dragBox, Vector3.Normalize(dragMatrix.Forward), ref aw, ref ah, ref ad);
			
			float c = (float)(0.05d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() * a);
			float adj = 1;

			adj = 104.4f / Max_Speed;

			if (adj < 0.2f) adj = 0.2f;

			var speed = (m_Character.Physics.LinearVelocity).Length();
			//var dragspeed = speed;
			var dragmin = (1 / adj) * 75;
			var dragmax = (1 / adj) * 95;
			dragmax -= dragmin;
			speed -= dragmin;

			var dragadj = 1.0f;


			if (speed > dragmax)
			{
				dragadj = 0.1f;
			}
			else
			{
				dragadj = 1 - (float)Math.Pow((speed / dragmax), 3);
			}



			if (dragadj < 0.1f) dragadj = 0.1f;
			if (dragadj > 1.0f) dragadj = 1.0f;

			drag = c * (CoreDrag.instance.settings.mult / 100f) * adj * dragadj * (float)CheckOcclusion();
			if (float.IsNaN(drag))
				return;
			dragForce = Vector3.Multiply(dragForce, drag);
			
			
			if (CoreDrag.instance.isServer) //lets try only applying force if were the server. 
				m_Character.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE, dragForce / (m_Character.Physics.Mass * 60 ), m_Character.WorldMatrix.Translation, null);
			applyHeat(-Vector3D.Multiply(Vector3D.Normalize(dragMatrix.Forward), drag / dragadj));
			


		}

		private void applyHeat(Vector3D dragVector)
		{

			var x = dragVector.X / Math.Pow(dragBox.Width, 2);
			var y = dragVector.Y / Math.Pow(dragBox.Height, 2);
			var z = dragVector.Z / Math.Pow(dragBox.Depth, 2);

			double scale = 60000;
			x /= scale;
			y /= scale;
			z /= scale;
			double nheatDelta = Math.Abs(x) + Math.Abs(y) + Math.Abs(z);
			heatDelta = (heatDelta > nheatDelta ? heatDelta - 0.01 : nheatDelta);
			heat += (float)nheatDelta;

		}

		private void overheatCheck()
		{

			bool critical = false;
			bool warn = false;


			if (heat > 750) critical = true;

			heatCache = heat;
			if (!critical)
			{
				if (heat > 500)
				{
					warn = true;
				}
			}
			if (warn || critical || heatDelta > 4.0 && heat > 400)
			{
				showlight = true;
			}
			else
				showlight = false;
			//playsmoke(critical);
			if (!CoreDrag.instance.settings.heat)
				return;
			if (critical)
				doDamage();
			if (MyAPIGateway.Session?.ControlledObject?.Entity == null)
			{
				return;
			}

			if (MyAPIGateway.Session.ControlledObject.Entity.EntityId == m_Character.EntityId)
			{
				CoreDrag.instance.HeatWarningVisible(false);
				CoreDrag.instance.HeatAlarmVisible(false);
				if (CoreDrag.instance.Pref.ShowHeatIndicator && CoreDrag.instance.NewHud)
				{
						if (heat > 5)
						{
							HeatWarning.Clear();
							HeatWarning.AppendFormat("Heat\n"
								+ "<color=blue>1: <color={0}>{1:N1}\n",
								getColor(heat), heat
								);
							CoreDrag.instance.HeatWarningVisible(true);
							CoreDrag.instance.UpdateHeatWarning(HeatWarning);
						}
				}
				if (CoreDrag.instance.Pref.Show_Warning)
				{
					if (warn)
						ShowNotification("Heat Level: Warning {1}{0:N0}", heat, 16, MyFontEnum.White);
					else if (critical)
						ShowNotification("Heat Level: Critical {1}{0:N0}", heat, 16, MyFontEnum.Red);
					else if (heat > 250)
						ShowNotification("Heat Level: {1}{0:N0}", heat, 16, MyFontEnum.White);
				}
			}
		}
		public override Vector3D GetWind()
		{
			return LastWind;
		}

		public override BoundingBox GetBoundingBox()
		{
			return dragBox;
		}
		public override MyTuple<double, double, double, double, double, double> GetHeat()
		{
			return new MyTuple<double, double, double, double, double, double>(heat, heat, heat, heat, heat, heat);
		}
		private void heatLoss(float _atmosphere)
		{
		
			if (_atmosphere < 0.05f) _atmosphere = 0.05f;//good enough for space

			heat -= (heat * 0.001d * _atmosphere * (CoreDrag.instance.settings.radMult / 500d));
		}


		void refreshLightGrid()
		{ 

			if (showlight)
			{
				MyLightMethod();
				return;
			}
			else
			{
				CloseLight();
			}
		}
		private void CloseLight()
		{
			if (burninglight != null)
			{
				burninglight.LightOn = false;
				burninglight.GlareOn = false;
				burninglight.ReflectorOn = false;
				MyLights.RemoveLight(burninglight);
				burninglight = null;
			}
		}

		private void MyLightMethod()
		{
			if (burninglight == null)
			{
				burninglight = MyLights.AddLight();
				if (burninglight == null)
					return;
				burninglight.Start("HeatLight");
				burninglight.LightOn = true;
				burninglight.GlareOn = true;
				burninglight.ReflectorOn = true;
				//burninglight.LightOwner = MyLight.LightOwnerEnum.LargeShip;
				burninglight.CastShadows = true;
				burninglight.Falloff = 1;
			}
			if (burninglight == null)
				return;
			if (heatCache < 500)
			{
				CloseLight();
				return;
			}

			burninglight.Position = (m_Character.WorldAABB.Center + Vector3D.Multiply(Vector3D.Normalize(m_Character.Physics.LinearVelocity), m_Character.LocalAABB.HalfExtents.Length()));
			int delta = (int)(heatDelta > 165 ? 165 : heatDelta);
			Color color = MyMath.VectorFromColor(255, (byte)(delta), 0, 100);
			burninglight.Intensity = (float)(heatCache > 500 ? (heatCache - 500) / 250 : 0);
			burninglight.Range = m_Character.LocalAABB.Extents.Length();
			burninglight.LightOn = true;
			burninglight.Color = color;
			burninglight.UpdateLight();
		}

		private void Draw()
		{

		}

		int iterator = 0;
		Vector3D[] GridCorners = new Vector3D[8];
		List<IHitInfo> HitInfo = new List<IHitInfo>();
		bool[] freecorners = new bool[8] { false, false, false, false, false, false, false, false };

		private void CalculateOcclusion()
		{
			if(OccludeDirty || m_Character?.ControllerInfo?.ControllingIdentityId != null)
			{
				OccludeDirty = false;
				m_Character.WorldAABBHr.GetCorners(GridCorners);
				OccludeCheck(GridCorners, 0);
				OccludeCheck(GridCorners, 1);
				OccludeCheck(GridCorners, 2);
				OccludeCheck(GridCorners, 3);
				OccludeCheck(GridCorners, 4);
				OccludeCheck(GridCorners, 5);
				OccludeCheck(GridCorners, 6);
				OccludeCheck(GridCorners, 7);
				return;
			}
			if (iterator % 10 == 0)
			{
				m_Character.WorldAABBHr.GetCorners(GridCorners);
				OccludeCheck(GridCorners, (iterator / 10) % 8);
			}
		}

		public override double CheckOcclusion()
		{
			double k = 0;
			for (int i = 0; i < 8; i++)
			{
				if (freecorners[i])
				{
					k += 1d;
				}
			}

			return (k / 8d);
		}
		//static Vector4 WhiteCheck = Color.White.ToVector4();
		//static Vector4 RedCheck = Color.Red.ToVector4();
		private void OccludeCheck(Vector3D[] gridCorners, int v)
		{
			if (CoreDrag.instance.settings.EnableOcclusion == true)
			{
				HitInfo.Clear();
				var PointA = m_Character.WorldAABB.Center;
				var PointB = GridCorners[v] - Vector3D.Normalize(m_Character.WorldAABB.Center - GridCorners[v]) * 45d;
				MyAPIGateway.Physics.CastRay(PointA, PointB, HitInfo);
				foreach (var obj in HitInfo)
				{
					if (obj.HitEntity == m_Character)
						continue;
					if (obj.HitEntity == null)
						continue;

					freecorners[v] = false;

					//MySimpleObjectDraw.DrawLine(PointA, PointB, MyStringId.GetOrCompute("Square"), ref RedCheck, 0.5f);
					return;
				}
			}
			freecorners[v] = true;
			//MySimpleObjectDraw.DrawLine(PointA, PointB, MyStringId.GetOrCompute("Square"), ref WhiteCheck, 0.5f);

		}
		int tick = 0;
		private void doDamage()
		{
			tick++;
			if (tick < 50) return;

			tick = 0;
			if (heat > 750)
				applyDamage(heat);

		}

		private void applyDamage(double dmg)
		{
			if (!CoreDrag.instance.isServer)//server only
				return;
			
			double min = 750;
			double mult = 1.0;
			try
			{

				IMyDestroyableObject damagedChar = m_Character as IMyDestroyableObject;
				if(damagedChar.Integrity > 0)
				{
					float damage = (float)(dmg - min);
					if (damage <= 0.0d) return;
					damage /= 10;
					damage *= ((float)mult);
					

					damagedChar.DoDamage(damage, MyDamageType.Fire, true/*, hit, 0*/);
				}


			}
			catch
			{

			}

		}


		public override void Close()
		{
			if(registered)
			{
				CoreDrag.UpdateHook -= Update;
				CoreDrag.DrawHook -= Draw;
			}
			if(CoreDrag.instance != null)
				CoreDrag.instance.Unregister(m_Character, this);
			base.Close();
		}
	}
}
