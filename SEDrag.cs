using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Game.Components;
using Sandbox.Definitions;
//using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using ParallelTasks;
using SEDrag.Definition;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using Sandbox.Game.Lights;
using Draygo.API;
using System.Threading;
using System.Text;
using VRage.Utils;
using VRage;
using Sandbox.Game.Entities.Cube;
using VRage.Game.Entity;

using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.Game.Models;

namespace SEDrag
{
	public class SEDrag : DragBase
	{


		public delegate void CallbackDel(GridCalculation data);

		private MyObjectBuilder_EntityBase objectBuilder;
		private IMyCubeGrid Grid = null;
		private bool init = false;
		private bool initcomplete = false;
		private bool dirty = false;//we force an update in Init
		private int lastupdate = 0;
		private bool OccludeDirty = true;
        private int dirtycounter = 10;
		public bool MarkedForClose = false;
		public bool m_IsConcealed = false;

		private const int SHOWCENTEROFLIFTREMOVALDELAYMAX = 180;
		private int m_ShowCenterOfLiftRemovalDelay = 0;
		private StringBuilder HeatWarning = new StringBuilder();

		private Vector3D LastWind = Vector3D.Zero;
		private Vector3D ClosetPlanet = Vector3D.Zero;
		private MyPlanet ClosePlanet;

		private Dictionary<Vector3I, MyParticleEffect> m_xmax_burn = new Dictionary<Vector3I, MyParticleEffect>();
		private Dictionary<Vector3I, MyParticleEffect> m_ymax_burn = new Dictionary<Vector3I, MyParticleEffect>();
		private Dictionary<Vector3I, MyParticleEffect> m_zmax_burn = new Dictionary<Vector3I, MyParticleEffect>();
		private Dictionary<Vector3I, MyParticleEffect> m_xmin_burn = new Dictionary<Vector3I, MyParticleEffect>();
		private Dictionary<Vector3I, MyParticleEffect> m_ymin_burn = new Dictionary<Vector3I, MyParticleEffect>();
		private Dictionary<Vector3I, MyParticleEffect> m_zmin_burn = new Dictionary<Vector3I, MyParticleEffect>();

		private GridCalculation Calc = new GridCalculation();

		private GridCalculation CalcT = new GridCalculation(); 

		private GridHeatData heat = new GridHeatData();

		private bool m_doDraw = false;
		private bool dontUpdate = false;
		private double drag = 0;
		private IMyEntity lightEntity;
		//private float GridSize = 0;
		private int tick = 0;
		private double heatDelta = 0;
		private double heatCache = 0;
		//private ThreadManagerTask task;
		private bool taskRunning = false;
		private bool resetpending = false;

		private int burnfcnt = 0;
		private int burnbcnt = 0;
		private int burnucnt = 0;
		private int burndcnt = 0;
		private int burnlcnt = 0;
		private int burnrcnt = 0;

		private Dictionary<side, Vector3I> c_xmax = new Dictionary<side, Vector3I>();
		private Dictionary<side, Vector3I> c_ymax = new Dictionary<side, Vector3I>();
		private Dictionary<side, Vector3I> c_zmax = new Dictionary<side, Vector3I>();
		private Dictionary<side, Vector3I> c_xmin = new Dictionary<side, Vector3I>();
		private Dictionary<side, Vector3I> c_ymin = new Dictionary<side, Vector3I>();
		private Dictionary<side, Vector3I> c_zmin = new Dictionary<side, Vector3I>();
		private List<IMySlimBlock> m_blocks = new List<IMySlimBlock>();
		private HashSet<side> lx = new HashSet<side>();
		private HashSet<side> ly = new HashSet<side>();
		private HashSet<side> lz = new HashSet<side>();

		Vector3D[] GridCorners = new Vector3D[8];
		List<IHitInfo> HitInfo = new List<IHitInfo>();
		bool[] freecorners = new bool[8] { false, false, false, false, false, false, false, false };

		private List<side> keylist = new List<side>();
		public SEDrag()
		{
			
		}
		public void Register(IMyEntity obj)
		{
			IMyEntity Entity = obj;
			if (CoreDrag.instance == null)
				return;
			if (Entity is IMyCubeGrid)
			{


				CoreDrag.instance.Register(Entity, this);

				Grid = Entity as IMyCubeGrid;
				//GridSize = Grid.GridSize;
				dirty = true;
				taskRunning = CoreDrag.instance.TaskManager.Add(refreshDragBox, calcComplete);
				//task = MyAPIGateway.Parallel.Start(refreshDragBox, calcComplete);
				CoreDrag.UpdateHook += Update;//register
				CoreDrag.DrawHook += Draw;

			}

			//error attempted to register Entity that is not a grid. 

		}
		public void Reset()
		{
			if(taskRunning)
			{
				//dont reset yet once the task completes we will reset. 
				resetpending = true;
				return;
			}
			if (CoreDrag.instance != null)
			{
				if (CoreDrag.UpdateHook != null)
				{
					CoreDrag.UpdateHook -= Update;


				}
				if (CoreDrag.DrawHook != null)
				{
					CoreDrag.DrawHook -= Draw;
				}
			}

			resetpending = false;
			objectBuilder = null;
			if(Grid != null)
			{
				Grid.OnBlockAdded -= blockChange;
				Grid.OnBlockRemoved -= blockChange;
				Grid.OnClosing -= onClose;
				Grid.OnGridSplit -= handleSplit;
			}

			Grid = null;
			init = false;
			initcomplete = false;
			dirty = false;//we force an update in Init
			lastupdate = 0;
			OccludeDirty = true;
			dirtycounter = 10;
			MarkedForClose = false;
			m_IsConcealed = false;


			m_ShowCenterOfLiftRemovalDelay = 0;
				HeatWarning.Clear();

			LastWind = Vector3D.Zero;
			ClosetPlanet = Vector3D.Zero;
			ClosePlanet = null;

			m_xmax_burn.Clear();
			m_ymax_burn.Clear();
			m_zmax_burn.Clear();
			m_xmin_burn.Clear();
			m_ymin_burn.Clear();
			m_zmin_burn.Clear();

			Calc.Clear();

			CalcT.Clear();

			heat.Clear();

			m_doDraw = false;
			dontUpdate = false;
			drag = 0;
			lightEntity = null;

			tick = 0;
			heatDelta = 0;
			heatCache = 0;

			

			burnfcnt = 0;
			burnbcnt = 0;
			burnucnt = 0;
			burndcnt = 0;
			burnlcnt = 0;
			burnrcnt = 0;

			c_xmax.Clear();
			c_ymax.Clear();
			c_zmax.Clear();
			c_xmin.Clear();
			c_ymin.Clear();
			c_zmin.Clear();
			m_blocks.Clear();

			lx.Clear();
			ly.Clear();
			lz.Clear();

			HitInfo.Clear();

			for (int i = 0; i < 8; i++)
			{
				freecorners[i] = false;
				GridCorners[i] = Vector3D.Zero;
			}



			keylist.Clear();
			if (CoreDrag.instance != null)
				CoreDrag.instance.AddToPool(this);
		}

		public override void Close()
		{
			CloseLight();
			//if (MarkedForClose) return;
			MarkedForClose = true;
			if (CoreDrag.instance != null)
			{
				if (CoreDrag.UpdateHook != null)
				{
					CoreDrag.UpdateHook -= Update;
					

				}
				if(CoreDrag.DrawHook != null)
				{
					CoreDrag.DrawHook -= Draw;
				}
				CoreDrag.instance.Unregister(Grid, this);
				//if (task != null) task.Valid = false;

				if (Grid != null)
				{
					Grid.OnClosing -= onClose;
					Grid.OnBlockAdded -= blockChange;
					Grid.OnBlockRemoved -= blockChange;
					Grid.OnGridSplit -= handleSplit;
				}


				if (lightEntity != null && !lightEntity.Closed)
					lightEntity.Close();//close our 'effect'


				removeBurnEffect(ref m_xmax_burn);
				removeBurnEffect(ref m_ymax_burn);
				removeBurnEffect(ref m_zmax_burn);
				removeBurnEffect(ref m_xmin_burn);
				removeBurnEffect(ref m_ymin_burn);
				removeBurnEffect(ref m_zmin_burn);
				Reset();//reset the object
			}

		}
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
				if (Grid.GridSizeEnum == MyCubeSize.Large)
					return CoreDrag.instance.large_max;
				else
					return CoreDrag.instance.small_max;
			}

		}
		private CenterOfLift showcenter
		{
			get
			{
				return CoreDrag.instance.Pref.CoLSetting;
			}
		}

		public static void ComputeMin(MyCubeBlockDefinition definition, MyBlockOrientation orientation, ref Vector3I max, out Vector3I min)
		{
			Vector3I b = definition.Size - 1;
			MatrixI matrixI = new MatrixI(orientation);
			Vector3I.TransformNormal(ref b, ref matrixI, out b);
			Vector3I.Abs(ref b, out b);
			min = max - b;
		}
		private void handleSplit(IMyCubeGrid orig, IMyCubeGrid newgrid)
		{
			dirty = true;
			if(orig.EntityId == Grid.EntityId)
			{
				GridHeatData data = new GridHeatData();
				CoreDrag.instance.heatTransferCache.Add(newgrid.EntityId, heat);
			}
			else
			{
				CoreDrag.instance.heatTransferCache.Add(orig.EntityId, heat);
			}


		}
		private void refreshDragBox()
		{
			if (dontUpdate) return;
			m_blocks.Clear();
			lx.Clear();
			ly.Clear();
			lz.Clear();

			c_xmax.Clear();
			c_ymax.Clear();
            c_zmax.Clear();
			c_xmin.Clear();
            c_ymin.Clear();
			c_zmin.Clear();

			Vector3D _centerOfLift = Vector3D.Zero;

			bool willNeedUpdate = false;
			double xadj = 0;
			double yadj = 0;
			double zadj = 0;
			int wheelcount = 0;
			int count = 0;
			Vector3D center = Vector3D.Zero;
			//IMyCubeGrid _grid;
			CalcT.Clear();//blank it out

			try
			{
				//only call when blocks are added/removed
				Vector3D comw = Grid.Physics.CenterOfMassWorld - Grid.GetPosition();
				
				double t_x = 0;
				double t_y = 0;
				double t_z = 0;
				Vector3I t;
				bool ignore = false;
				
				
				if (!Grid.Flags.HasFlag(EntityFlags.Save))
					ignore = true;
				//string blockstring = "";
				Grid.GetBlocks(m_blocks, delegate (IMySlimBlock e)
				{

					count++;
					if (ignore)
						return false;
					if (e.CubeGrid != Grid)
						return false;
					Vector3I Min;
					Vector3I Pos = Vector3I.Zero;
					Vector3I ITER = Vector3I.Zero;
					Vector3I Max = e.Max;
					if (e.FatBlock != null)
					{
						Min = e.FatBlock.Min;
						if (e.FatBlock.BlockDefinition.TypeIdString == "MyObjectBuilder_Wheel")
						{
							wheelcount++;
						}
					}
					else
					{
						//Min = e.GetObjectBuilder().Min;
						ComputeMin((MyCubeBlockDefinition)e.BlockDefinition, e.Orientation, ref Max, out Min);
					}
					if (e.IsDestroyed) willNeedUpdate = true;
				
                    for (Vector3I_RangeIterator it = new Vector3I_RangeIterator( ref Min, ref Max); it.IsValid(); it.GetNext(out ITER) )
					{
						Pos = it.Current;

						var x = new side(Pos.Y, Pos.Z);
						var y = new side(Pos.X, Pos.Z);
						var z = new side(Pos.Y, Pos.X);

						if (!lx.Contains(x))
						{
							lx.Add(x);
							c_xmax.Add(x, Pos);
							c_xmin.Add(x, Pos);
						}
						else
						{
							if (c_xmax.TryGetValue(x, out t))
							{
								if (t.X > Pos.X)
								{
									c_xmax.Remove(x);
									c_xmax.Add(x, Pos);
								}
							}
							if (c_xmin.TryGetValue(x, out t))
							{
								if (t.X < Pos.X)
								{
									c_xmin.Remove(x);
									c_xmin.Add(x, Pos);

								}
							}
						}
						if (!ly.Contains(y))
						{
							ly.Add(y);
							c_ymax.Add(y, Pos);
							c_ymin.Add(y, Pos);
						}
						else
						{
							if (c_ymax.TryGetValue(y, out t))
							{
								if (t.Y > Pos.Y)
								{
									c_ymax.Remove(y);
									c_ymax.Add(y, Pos);
								}
							}
							if (c_ymin.TryGetValue(y, out t))
							{
								if (t.Y < Pos.Y)
								{
									c_ymin.Remove(y);
									c_ymin.Add(y, Pos);
								}
							}
						}
						if (!lz.Contains(z))
						{
							lz.Add(z);
							c_zmax.Add(z, Pos);
							c_zmin.Add(z, Pos);
						}
						else
						{
							if (c_zmax.TryGetValue(z, out t))
							{
								if (t.Z > Pos.Z)
								{
									c_zmax.Remove(z);
									c_zmax.Add(z, Pos);
								}
							}
							if (c_zmin.TryGetValue(z, out t))
							{
								if (t.Z < Pos.Z)
								{
									c_zmin.Remove(z);
									c_zmin.Add(z, Pos);
								}
							}
						}
					}

					return false;
				});
				if (ignore)
				{
					MyAPIGateway.Utilities.InvokeOnGameThread(() =>
					{
						dontUpdate = true;
						dirty = false;
					});
					return;
				}

				center = WorldtoGrid(Grid.Physics.CenterOfMassWorld);
				
				xadj = Math.Round(center.X, 2);
				yadj = Math.Round(center.Y, 2);
				zadj = Math.Round(center.Z, 2);
				//get parimeter blocks
				

				var bb = new BoundingBox(Vector3.Zero, ( new Vector3(Math.Sqrt(lx.Count), Math.Sqrt(ly.Count), Math.Sqrt(lz.Count)) + new Vector3(0.5, 0.5, 0.5)) * Grid.GridSize);
				CalcT.dragBox = new BoundingBox(-bb.Center, bb.Center);//center the box

				calculateArea(ref t_x, ref t_y, ref t_z, ref c_xmax, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref c_xmin, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref c_ymax, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref c_ymin, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref c_zmax, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref c_zmin, ref xadj, ref yadj, ref zadj);

				CalcT.centerOfLift = new Vector3D(calcCenter(t_x, lx.Count * 2), calcCenter(t_y,ly.Count * 2), calcCenter(t_z, lz.Count * 2));

				if (Math.Abs(_centerOfLift.X) < Grid.GridSize) _centerOfLift.X = 0;
				if (Math.Abs(_centerOfLift.Y) < Grid.GridSize) _centerOfLift.Y = 0;
				if (Math.Abs(_centerOfLift.Z) < Grid.GridSize) _centerOfLift.Z = 0;

				CalcT.xmax = c_xmax;
				CalcT.xmin = c_xmin;
				CalcT.ymax = c_ymax;
				CalcT.ymin = c_ymin;
				CalcT.zmax = c_zmax;
				CalcT.zmin = c_zmin;

				if (willNeedUpdate) CalcT.dirty = true;
				if (wheelcount == 1 && count == 1)
					CalcT.skipDrag = true;
				else
					CalcT.skipDrag = false;
				CalcT.Reset = false;

			}
			catch
			{
				CalcT.dirty = true;//failed update
			}
		}
		private void calculateArea(ref double t_x, ref double t_y, ref double t_z, ref Dictionary<side, Vector3I> side, ref double xadj, ref double yadj, ref double zadj)
		{
			//debug += string.Format("{0}\n", side.Count);
			foreach (KeyValuePair<side, Vector3I> entry in side)
			{
				//add them up
				
				double x = entry.Value.X - xadj;
				double y = entry.Value.Y - yadj;
				double z = entry.Value.Z - zadj;

				t_x += x;
				t_y += y;
				t_z += z;
				//debug += string.Format("{0:N2} {1:N2} {2:N2}|{3:N2} {4:N2} {5:N2}\n", x, y, z, t_x, t_y, t_z);
	
			}
		}
		private double calcCenter(double t, int cnt)
		{
			if (cnt == 0) return 0.0f;
			return Math.Sqrt(Math.Abs(t / cnt)) * Math.Sign(t);
		}

		void refreshCenterOfLift()
		{
			try
			{
				if (Grid.IsStatic) return;
				if (CoreDrag.instance == null || CoreDrag.instance.isDedicated)
					return;
				if (m_ShowCenterOfLiftRemovalDelay > 0)
					m_ShowCenterOfLiftRemovalDelay--;
                if (CoreDrag.instance.LastEntityIdForCenterOfLift.HasValue)
				{
					if(CoreDrag.instance.LastEntityIdForCenterOfLift.Value == Grid.EntityId)
					{
						m_ShowCenterOfLiftRemovalDelay = SHOWCENTEROFLIFTREMOVALDELAYMAX;
					}
				}
                if ((showcenter == CenterOfLift.On || (CoreDrag.instance.settings.advancedlift && showcenter == CenterOfLift.Auto && m_ShowCenterOfLiftRemovalDelay > 0) ) && !dontUpdate && initcomplete && !Calc.skipDrag)
				{

					//draw lines
					MatrixD mat = new MatrixD(Grid.WorldMatrix);
					mat.Translation = Grid.Physics.CenterOfMassWorld;
					
					double boxsize = Grid.GridSize / 8d;
					double boxlength = Grid.LocalAABB.Extents.AbsMax() / 1.5d + boxsize * 2d;
					BoundingBoxD box = new BoundingBoxD(new Vector3D(-boxlength, -boxsize, -boxsize), new Vector3D(boxlength, boxsize, boxsize));
					Color col = Color.Purple;
					col.A = 120;
					MySimpleObjectDraw.DrawTransparentBox(ref mat, ref box, ref col, MySimpleObjectRasterizer.Solid, 1, Grid.GridSize, blendType: BlendTypeEnum.PostPP);
					box = new BoundingBoxD(new Vector3D(-boxsize, -boxlength, -boxsize), new Vector3D(boxsize, boxlength, boxsize));
					MySimpleObjectDraw.DrawTransparentBox(ref mat, ref box, ref col, MySimpleObjectRasterizer.Solid, 1, Grid.GridSize, blendType: BlendTypeEnum.PostPP);
					box = new BoundingBoxD(new Vector3D(-boxsize, -boxsize, -boxlength), new Vector3D(boxsize, boxsize, boxlength));
					MySimpleObjectDraw.DrawTransparentBox(ref mat, ref box, ref col, MySimpleObjectRasterizer.Solid, 1, Grid.GridSize, blendType: BlendTypeEnum.PostPP);


					mat.Translation = Vector3D.Transform(Vector3D.Multiply((WorldtoGrid(Grid.Physics.CenterOfMassWorld) + Calc.centerOfLift), Grid.GridSize), Grid.WorldMatrix);
					box = new BoundingBoxD(new Vector3D(-boxlength, -boxsize, -boxsize), new Vector3D(boxlength, boxsize, boxsize));
					col = Color.Yellow;
					col.A = 120;
					MySimpleObjectDraw.DrawTransparentBox(ref mat, ref box, ref col, MySimpleObjectRasterizer.Solid, 1, Grid.GridSize, blendType: BlendTypeEnum.PostPP);
					box = new BoundingBoxD(new Vector3D(-boxsize, -boxlength, -boxsize), new Vector3D(boxsize, boxlength, boxsize));
					MySimpleObjectDraw.DrawTransparentBox(ref mat, ref box, ref col, MySimpleObjectRasterizer.Solid, 1, Grid.GridSize, blendType: BlendTypeEnum.PostPP);
					box = new BoundingBoxD(new Vector3D(-boxsize, -boxsize, -boxlength), new Vector3D(boxsize, boxsize, boxlength));
					MySimpleObjectDraw.DrawTransparentBox(ref mat, ref box, ref col, MySimpleObjectRasterizer.Solid, 1, Grid.GridSize, blendType: BlendTypeEnum.PostPP);
				}
			}
            catch (Exception)
			{
				//MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("{0}", ex.Message));
				//Log.DebugWrite(DebugLevel.Error, "Error in refreshCenterOfLift");
			}
		}
		Vector3D WorldtoGrid(Vector3D coords)
		{
			Vector3D localCoords = Vector3D.Transform(coords, Grid.WorldMatrixNormalizedInv);
			localCoords /= Grid.GridSize;
			return localCoords;
		}
		void refreshLightGrid()
		{
			if (Grid.IsStatic) return;

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
			if(burninglight != null)
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
			if(burninglight == null)
			{
				burninglight = MyLights.AddLight();
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
			if(heatCache < 500)
			{
				CloseLight();
				return;
			}
			
			burninglight.Position = (Grid.WorldAABB.Center + Vector3D.Multiply(Vector3D.Normalize(Grid.Physics.LinearVelocity), Grid.LocalAABB.HalfExtents.Length()));
			int delta = (int)(heatDelta > 165 ? 165 : heatDelta);
			Color color = MyMath.VectorFromColor(255, (byte)(delta),0 , 100);
			burninglight.Intensity = (float)(heatCache > 500 ? (heatCache - 500) / 250 : 0);
			burninglight.Range = Grid.LocalAABB.Extents.Length() * Grid.GridSize;
			burninglight.LightOn = true;
			burninglight.Color = color;
			burninglight.UpdateLight(); 
		}

		private void refreshBoxParallel()
		{
			if (dontUpdate) return;
			if (m_IsConcealed) return;

			if (dirty && !taskRunning && lastupdate <= 0)
			{
				lastupdate = 15;//2x a second if needed. Yea for other threads!
				dirty = false;
				taskRunning = CoreDrag.instance.TaskManager.Add(refreshDragBox, calcComplete);
            }
			else
			{
				if(dirty)
					lastupdate--;
			}
		}
		public void calcComplete()
		{
			initcomplete = true;
			taskRunning = false;
			if (CalcT.Reset)
			{
				CalcT.dirty = true;
			}
			var tCalc = Calc;
			Calc = CalcT;
			CalcT = tCalc;//swap. 
			if(Calc.dirty == true)
				dirty = true;
			if(resetpending)
			{
				Reset();
			}

		}
		private void blockChange(IMySlimBlock obj)
		{
			dirty = true;
		}

		private void init_grid()
		{
			//Log.DebugWrite(DebugLevel.Verbose, string.Format("Entity {0}: Init Grid", Grid.EntityId));
			if (!init && CoreDrag.instance != null)
			{
				init = true;
				Grid.OnBlockAdded += blockChange;
				Grid.OnBlockRemoved += blockChange;
				Grid.OnClosing += onClose;
				Grid.OnGridSplit += handleSplit;
				dirty = true;
				Calc.dragBox = Grid.LocalAABB;
				GridHeatData temp;
				if (CoreDrag.instance.heatTransferCache.TryGetValue(Grid.EntityId, out temp))
				{
					CoreDrag.instance.heatTransferCache.Remove(Grid.EntityId);
					heat.Copy(temp);
				}
				else
				{

				}
			}
		}
		private void onClose(IMyEntity obj)
		{
			Close();//close out
		}
		int iterator = 0;
		public override void Update()
		{
			if (dontUpdate)
				return;
			if (MarkedForClose)
				Close();//still getting updates!? Lets retry closing it. 
			if (CoreDrag.instance == null || MyAPIGateway.Utilities == null)
				return;

			if (Grid == null)
			{

				CoreDrag.UpdateHook -= Update;
				CoreDrag.DrawHook -= Draw;
				//CoreDrag.instance.Unregister(Grid, this);
				return;
			}
			else if (!(CoreDrag.instance.isServer || MyAPIGateway.Session?.ControlledObject?.Entity?.Parent?.EntityId == null || MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Grid.EntityId))
			{
				
				return;//save cycles
			}

			if (!init)
				init_grid();
			if (!init || Grid.Physics == null || !Grid.Physics.Enabled)
			{
				return;
			}
			
            if (Grid.Flags.HasFlag((EntityFlags)4))
			{
				
				m_IsConcealed = true;
				return;//grid is concealed. 
			}
			if (m_IsConcealed)
			{
				dirty = true;//reset!
				lastupdate = 0;//trigger update right away. 
				m_IsConcealed = false;
            }

			refreshBoxParallel();

			iterator++;
			if (iterator >= 60)
				iterator = 0;

			var dragForce = Vector3.Zero;
			float atmosphere = 0;
			Vector3D WindDirection = Vector3D.Zero;

			Vector3D GridPosition = Grid.GetPosition();
            try
			{
				foreach (var kv in CoreDrag.instance.planets)
				{
					var planet = kv.Value;

					if (planet.Closed || planet.MarkedForClose)
					{
						continue;
					}

					if (planet.HasAtmosphere)
					{
						if(iterator % 10 == 0)
						{
							
							//every 10 ticks
							Vector3D CP = planet.GetClosestSurfacePointGlobal(ref GridPosition) - GridPosition;
							if (CP.LengthSquared() > ClosetPlanet.LengthSquared())
							{
								ClosetPlanet = CP;
								ClosePlanet = planet;
							}
							
						}

						var add = planet.GetAirDensity(GridPosition);
						if(add > 0f)
						{
							atmosphere += add;
							WindDirection = CoreDrag.instance.GetWeatherAtPoint(planet, GridPosition, ClosetPlanet.Length(),  Grid) * atmosphere;
						}
                      
					}
				}

				
				heatLoss(atmosphere);
				if (Calc.skipDrag)
					return;
				overheatCheck();
				refreshLightGrid();

				LastWind = Vector3D.Lerp(LastWind, WindDirection, 0.016d);
				if (Grid.IsStatic || !Grid.Physics.CanUpdateAccelerations)
					return;
				if(CoreDrag.instance.settings.AtmosphericMinimum > 0)
				{
					if (atmosphere < CoreDrag.instance.settings.AtmosphericMinimum / 100f)
					{
						atmosphere = CoreDrag.instance.settings.AtmosphericMinimum / 100f;
                    }
				}

				if (atmosphere < 0.02f)
					return;



				CalculateOcclusion();

				dragForce = -Grid.Physics.LinearVelocity + (LastWind * CheckOcclusion());

				Vector3 dragNormal = Vector3.Normalize(dragForce);

				MatrixD dragMatrix = MatrixD.CreateFromDir(dragNormal);
				MatrixD mat = MatrixD.Invert(Grid.WorldMatrix);
				dragMatrix = dragMatrix * mat;


				double aw = 0;
				double ah = 0;
				double ad = 0;
				var forwardVector = Vector3.Normalize(dragMatrix.Forward);
                double a = getArea(Calc.dragBox, forwardVector, ref aw, ref ah, ref ad);
				
				double up =      getLiftCI(ah, forwardVector.Y);
				double left =    getLiftCI(aw, forwardVector.X);
				double forward = getLiftCI(ad, forwardVector.Z);
				
				float c = (float)(0.25d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() * a);
				float adj = 1;
				if (Grid.GridSizeEnum == MyCubeSize.Small && small_max > 0)
					adj = 104.4f / small_max;
				if (Grid.GridSizeEnum == MyCubeSize.Large && large_max > 0)
					adj = 104.4f / large_max;
				if (adj < 0.2f) adj = 0.2f;

				var deflectadj = (1 / adj) * 90;
				var deflectstart = (1 / adj) * 40;
				var speed = Grid.Physics.LinearVelocity.Length();

				var dragspeed = speed;
				var min = deflectstart;
				var max = deflectadj - deflectstart;
				var dragmin = (1 / adj) * 75;
				var dragmax = (1 / adj) * 100;
				dragmax -= dragmin;
				speed = speed - min;
				dragspeed -= dragmin;
				var liftadj = 1.0f;
				var dragadj = 1.0f;
				if (speed <= 0.0f)
				{
					//nothing
				}
				else if (speed >= max)
				{
					liftadj = 0.2f;
					
                }
				else
				{
					liftadj = 1 - (float)Math.Pow((speed / max), 6);
					if (liftadj < 0.2f) liftadj = 0.2f;
				}
				
				if (dragspeed > dragmax)
				{
					dragadj = 0.1f;
				}
				else
				{
					dragadj = 1 - (float)Math.Pow((dragspeed / dragmax), 3);
				}
				if (Grid.GridSizeEnum == MyCubeSize.Large)
				{
					liftadj = 0.0f;
				}
				
				if (liftadj < 0.0f) liftadj = 0.0f;
				if (dragadj < 0.1f) dragadj = 0.1f;
				if (dragadj > 1.0f) dragadj = 1.0f;
				
				liftadj *= CoreDrag.instance.settings.DeflectionMult / 100f;

				drag = c * CoreDrag.instance.settings.mult / 100 * adj * dragadj;
				var maxforce = Grid.Physics.Mass * Max_Speed * 2f * 60f;
				var dragMult = MathHelper.Clamp(drag, -maxforce, maxforce);

				bool applylift = false;
				float u =0f, l = 0f, f = 0f;
				Vector3 liftup = Vector3.Zero, liftleft = Vector3.Zero, liftforw = Vector3.Zero;
				if (liftadj > 0.0f)
				{
					applylift = true;
					u = (float)(up * 10.0d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared()) * liftadj;
					l = (float)(left * 10.0d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared()) * liftadj;
					f = (float)(forward * 10.0d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared()) * liftadj;
					u = MathHelper.Clamp(u, -maxforce, maxforce);
					l = MathHelper.Clamp(l, -maxforce, maxforce);
					f = MathHelper.Clamp(f, -maxforce, maxforce);

					liftup = Vector3.Multiply(Grid.WorldMatrix.Up, u * CoreDrag.instance.settings.mult / 100f * adj);
					liftleft = Vector3.Multiply(Grid.WorldMatrix.Right, l * CoreDrag.instance.settings.mult / 100f * adj);
					liftforw = Vector3.Multiply(Grid.WorldMatrix.Backward, f * CoreDrag.instance.settings.mult / 100f * adj);

				}

				dragForce = Vector3.Multiply(dragNormal, (float)drag);

				var controller = MyAPIGateway.Players.GetPlayerControllingEntity(Grid);
				if (!CoreDrag.SERVER_ONLY_PHYSICS || CoreDrag.instance.isServer)
				{
					if (CoreDrag.instance.settings.advancedlift)
					{
						Vector3D pos = Vector3D.Zero;
						if (Calc.centerOfLift == Vector3D.Zero)
							pos = Grid.Physics.CenterOfMassWorld;
						else
							pos = Vector3D.Transform(Vector3D.Multiply((WorldtoGrid(Grid.Physics.CenterOfMassWorld) + Calc.centerOfLift), Grid.GridSize), Grid.WorldMatrix);
						if (applylift)
							Grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, (liftforw + liftleft + liftup), null, null, null);

						Grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, Vector3D.Multiply(dragForce, 0.52), pos, null, applyImmediately: true);
						Grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, Vector3D.Multiply(dragForce, 0.48), null, null);



						if (Grid.GridSizeEnum == MyCubeSize.Small)
						{
							DifferentialCalculation DifferentialAdj = GetDifferentialCOLAdjustment(Grid, Calc.centerOfLift);
							Grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, DifferentialAdj.Force1 * (Grid.Physics.Mass / 2000), DifferentialAdj.Point1, null);
							Grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, DifferentialAdj.Force2 * (Grid.Physics.Mass / 2000), DifferentialAdj.Point2, null);
						}
					}
					else
					{

						if (applylift)
							Grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, (liftforw + liftleft + liftup), null, null);
						Grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, dragForce, null, null);

					}
				}
				//need an anti rotational force.
				if(Grid.Physics.AngularVelocity.LengthSquared() > 0)
				{
					float mod = 0.4f;
					if (Grid.GridSizeEnum == MyCubeSize.Large)
						mod = 0.1f;
					if (Grid.Physics.Mass > 0)
						Grid.Physics.AngularDamping = (c / (Grid.Physics.Mass * 1.5f)) * mod * dragadj * ( CoreDrag.instance.settings.RotationalDragMultiplier / 100f );
					else
						Grid.Physics.AngularDamping = 0f;
				}
				else
				{
					Grid.Physics.AngularDamping = 0;
				}

				applyHeat(-Vector3D.Multiply(Vector3D.Normalize(dragMatrix.Forward), (c + ( ( l + f + u) / 75d ) ) * (CoreDrag.instance.settings.mult / 100d) * adj));
			}
			catch
			{
				//Log.DebugWrite(DebugLevel.Error, string.Format("Exception in drag update: {0}", ex.ToString()));

			}
		}


		public override Vector3D GetWind()
		{
			return LastWind;
		}



		public override MyTuple<double, double, double, double, double, double> GetHeat()
		{
			return new MyTuple<double, double, double, double, double, double>(heat.front, heat.back, heat.left, heat.right, heat.up, heat.down);
		}

		private void CalculateOcclusion()
		{
			if (OccludeDirty)
			{
				OccludeDirty = false;
				Grid.WorldAABBHr.GetCorners(GridCorners);
				OccludeCheck(GridCorners, 0);
				OccludeCheck(GridCorners, 1);
				OccludeCheck(GridCorners, 2);
				OccludeCheck(GridCorners, 3);
				OccludeCheck(GridCorners, 4);
				OccludeCheck(GridCorners, 5);
				OccludeCheck(GridCorners, 6);
				OccludeCheck(GridCorners, 7);
			}
			if (iterator % 10 == 0)
			{
				Grid.WorldAABBHr.GetCorners(GridCorners);
				OccludeCheck(GridCorners, (iterator / 10) % 8);
			}
		}
		public override double CheckOcclusion()
		{

			double k = 0;
			for ( int i = 0; i < 8; i++)
			{
				if (freecorners[i])
				{
					k += 1d;
				}
			}

			return  (k / 8d);
		}

		private void OccludeCheck(Vector3D[] gridCorners, int v)
		{
			if(CoreDrag.instance.settings.EnableOcclusion == true)
			{
				HitInfo.Clear();
				var PointA = GridCorners[v];
				var test = Grid.RayCastBlocks(PointA, Grid.WorldAABB.Center);
				if (test.HasValue)
				{
					PointA = Grid.GridIntegerToWorld(test.Value);
				}
				var PointB = GridCorners[v] - Vector3D.Normalize(Grid.WorldAABB.Center - GridCorners[v]) * 45d;
				//var white = Color.White.ToVector4();
				//MySimpleObjectDraw.DrawLine(PointA, PointB, MyStringId.GetOrCompute("Square"), ref white, 0.5f);
				MyAPIGateway.Physics.CastRay(PointA, PointB, HitInfo);

				foreach (var obj in HitInfo)
				{
					if (obj.HitEntity == null)
						continue;
					if (obj.HitEntity == Grid)
						continue;
					
					freecorners[v] = false;
					return;
				}
			}

			freecorners[v] = true;
		}

		protected struct DifferentialCalculation
		{
			public Vector3D Point1, Point2, Force1, Force2;
		}
		float lastdiff = 0;
		private DifferentialCalculation GetDifferentialCOLAdjustment(IMyCubeGrid mGrid, Vector3D CoL)
		{
			var HalfExtent = Grid.WorldAABB.HalfExtents;
			var CoM = mGrid.Physics.CenterOfMassWorld;
			var Vel = mGrid.Physics.LinearVelocity;
			var VelTrans = WorldtoGrid(Vel+mGrid.WorldMatrix.Translation);

			var Direction = Base6Directions.GetClosestDirection(VelTrans);

			//cancel out largest vector
			switch(Base6Directions.GetAxis(Direction))
			{
				case Base6Directions.Axis.ForwardBackward:
					//VelTrans.Z = 0;
					CoL.Z = 0;
					break;
				case Base6Directions.Axis.LeftRight:
					//VelTrans.X = 0;
					CoL.X = 0;
					break;
				case Base6Directions.Axis.UpDown:
					//VelTrans.Y = 0;
					CoL.Y = 0;
					break;
			}
			if(CoL == Vector3.Zero)
			{
				return new DifferentialCalculation() { Force1 = Vector3.Zero, Force2 = Vector3.Zero, Point1 = Vector3.Zero, Point2 = Vector3.Zero };
			}
			var CoLDirection = Base6Directions.GetClosestDirection(CoL);

			Vector3D Plus = Vector3D.Zero, Minus = Vector3D.Zero, PlusForce = Vector3D.Zero, MinusForce = Vector3D.Zero;
			float Diff = 0.0f;
			switch (CoLDirection)
			{
				case Base6Directions.Direction.Up:
					if (Base6Directions.GetAxis(Direction) == Base6Directions.Axis.ForwardBackward)
					{
						Plus = mGrid.WorldMatrix.Right * HalfExtent.Y + CoM;
						Minus = mGrid.WorldMatrix.Left * HalfExtent.Y + CoM;
					}
					else
					{
						Plus = mGrid.WorldMatrix.Forward * HalfExtent.Y + CoM;
						Minus = mGrid.WorldMatrix.Backward * HalfExtent.Y + CoM;
					}
					PlusForce = mGrid.WorldMatrix.Up;
					MinusForce = mGrid.WorldMatrix.Down;
					break;
				case Base6Directions.Direction.Down:
					if (Base6Directions.GetAxis(Direction) == Base6Directions.Axis.ForwardBackward)
					{
						Plus = mGrid.WorldMatrix.Right * HalfExtent.Y + CoM;
						Minus = mGrid.WorldMatrix.Left * HalfExtent.Y + CoM;
					}
					else
					{
						Plus = mGrid.WorldMatrix.Forward * HalfExtent.Y + CoM;
						Minus = mGrid.WorldMatrix.Backward * HalfExtent.Y + CoM;
					}
					PlusForce = mGrid.WorldMatrix.Down;
					MinusForce = mGrid.WorldMatrix.Up;
					break;
				case Base6Directions.Direction.Forward:
					if (Base6Directions.GetAxis(Direction) == Base6Directions.Axis.LeftRight)
					{
						Plus = mGrid.WorldMatrix.Up * HalfExtent.Z + CoM;
						Minus = mGrid.WorldMatrix.Down * HalfExtent.Z + CoM;
					}
					else
					{
						Plus = mGrid.WorldMatrix.Right * HalfExtent.Z + CoM;
						Minus = mGrid.WorldMatrix.Left * HalfExtent.Z + CoM;
					}
					PlusForce = mGrid.WorldMatrix.Forward;
					MinusForce = mGrid.WorldMatrix.Backward;
					break;
				case Base6Directions.Direction.Backward:
					if (Base6Directions.GetAxis(Direction) == Base6Directions.Axis.LeftRight)
					{
						Plus = mGrid.WorldMatrix.Up * HalfExtent.Z + CoM;
						Minus = mGrid.WorldMatrix.Down * HalfExtent.Z + CoM;
					}
					else
					{
						Plus = mGrid.WorldMatrix.Right * HalfExtent.Z + CoM;
						Minus = mGrid.WorldMatrix.Left * HalfExtent.Z + CoM;
					}
					PlusForce = mGrid.WorldMatrix.Backward;
					MinusForce = mGrid.WorldMatrix.Forward;
					break;
				case Base6Directions.Direction.Left:
					if (Base6Directions.GetAxis(Direction) == Base6Directions.Axis.ForwardBackward)
					{
						Plus = mGrid.WorldMatrix.Up * HalfExtent.X + CoM;
						Minus = mGrid.WorldMatrix.Down * HalfExtent.X + CoM;
					}
					else
					{
						Plus = mGrid.WorldMatrix.Forward * HalfExtent.X + CoM;
						Minus = mGrid.WorldMatrix.Backward * HalfExtent.X + CoM;
					}
					PlusForce = mGrid.WorldMatrix.Left;
					MinusForce = mGrid.WorldMatrix.Right;
					break;
				case Base6Directions.Direction.Right:
					if (Base6Directions.GetAxis(Direction) == Base6Directions.Axis.ForwardBackward)
					{
						Plus = mGrid.WorldMatrix.Up * HalfExtent.X + CoM;
						Minus = mGrid.WorldMatrix.Down * HalfExtent.X + CoM;
					}
					else
					{
						Plus = mGrid.WorldMatrix.Forward * HalfExtent.Y + CoM;
						Minus = mGrid.WorldMatrix.Backward * HalfExtent.X + CoM;
					}
					PlusForce = mGrid.WorldMatrix.Right;
					MinusForce = mGrid.WorldMatrix.Left;
					break;
			}
			var VelPlus = mGrid.Physics.GetVelocityAtPoint(Plus);
			var VelMinus = mGrid.Physics.GetVelocityAtPoint(Minus);
			float VelDotPlus = Vector3.Dot(Vector3.Normalize(Vel), Vector3.Normalize(VelPlus));
			float VelDotMinus = Vector3.Dot(Vector3.Normalize(Vel), Vector3.Normalize(VelMinus));

			Diff = (VelPlus.LengthSquared()) - (VelMinus.LengthSquared());

			if(Diff < 25 && Diff > -25)
			{
				Diff = 0;
			}
			else
			{
				Diff = Diff - (Math.Sign(Diff) * 25);
			}
			DifferentialCalculation RetVal = new DifferentialCalculation();
			lastdiff = MathHelper.Lerp(lastdiff, Diff, 0.5f);
			RetVal.Force1 = PlusForce * lastdiff;
			RetVal.Force2 = MinusForce * lastdiff;
			RetVal.Point1 = Plus;
			RetVal.Point2 = Minus;
			return RetVal;
		}

		public override void WriteHeat(CoreDrag.HeatDataMessage heatData)
		{
			heat.Copy(heatData);
		}

		
		private void applyHeat(Vector3D dragVector)
		{
			
			var x = dragVector.X / Math.Pow(Calc.dragBox.Width,2);
			var y = dragVector.Y / Math.Pow(Calc.dragBox.Height, 2);
			var z = dragVector.Z / Math.Pow(Calc.dragBox.Depth, 2);

			double scale = 30000;
			x /= scale;
			y /= scale;
			z /= scale;
			double nheatDelta = Math.Abs(x) + Math.Abs(y) + Math.Abs(z);
			heatDelta = (heatDelta > nheatDelta ? heatDelta - 0.01 : nheatDelta);
			if (heatDelta < 0) heatDelta = 0;
            if (x > 0)
			{
				//left
				heat.left += x;
			}
			if (x < 0)
			{
				//right
				heat.right += -x;
			}
			if (y > 0)
			{
				//up
				heat.up += y;
			}
			if (y < 0)
			{
				//down
				heat.down += -y;
			}
			if (z > 0)
			{
				//backward
				heat.back += z;
			}
			if (z < 0)
			{
				//forward
				heat.front += -z;
			}

			if(CoreDrag.instance.isServer)
				CoreDrag.instance.UpdateClients(Grid, heat);
        }

		private void heatLoss(float _atmosphere)
		{
            if (_atmosphere < 0.05f) _atmosphere = 0.05f;//good enough for space
			disappate(ref heat.front, _atmosphere);
			disappate(ref heat.back, _atmosphere);
			disappate(ref heat.left, _atmosphere);
			disappate(ref heat.right, _atmosphere);
			disappate(ref heat.up, _atmosphere);
			disappate(ref heat.down, _atmosphere);

			
		}

		private void disappate(ref double heatpart, float atmo)
		{
			heatpart -= (heatpart * 0.001d * atmo * CoreDrag.instance.settings.radMult/500d);//* (2.5d / Grid.GridSize)
		}



		private void overheatCheck()
		{
			bool critical = false;
			bool warn = false;
			double tHeat = heat.front;

			if (heat.back > tHeat) tHeat = heat.back;
			if (heat.up > tHeat) tHeat = heat.up;
			if (heat.left > tHeat) tHeat = heat.left;
			if (heat.right > tHeat) tHeat = heat.right;
			if (heat.down > tHeat) tHeat = heat.down;

			if (tHeat > 750) critical = true;
			heatCache = tHeat;
			if (!critical)
			{
				if (tHeat > 500)
				{
					warn = true;
				}
			}
			if (warn || critical || heatDelta > 4.0 && tHeat > 400)
			{
				showlight = true;
			}
			else
				showlight = false;
			playsmoke(critical);
			if (!CoreDrag.instance.settings.heat)
				return;
			if (critical)
				doDamage();
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null || MyAPIGateway.Session.ControlledObject.Entity == null || MyAPIGateway.Session.ControlledObject.Entity.Parent == null)
			{
				return;
			}

			if (MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Grid.EntityId )
			{
				CoreDrag.instance.HeatWarningVisible(false);
				CoreDrag.instance.HeatAlarmVisible(false);
				if (CoreDrag.instance.Pref.ShowHeatIndicator && CoreDrag.instance.NewHud)
				{

					if (tHeat > 5)
					{
						//var msg = CoreDrag.instance.HeatMsg;
						HeatWarning.Clear();
						HeatWarning.AppendFormat("Side   Heat\n"
							+ "<color=blue>1: <color={0}>{1:N1}\n"
							+ "<color=blue>2: <color={2}>{3:N1}\n"
							+ "<color=blue>3: <color={4}>{5:N1}\n"
							+ "<color=blue>4: <color={6}>{7:N1}\n"
							+ "<color=blue>5: <color={8}>{9:N1}\n"
							+ "<color=blue>6: <color={10}>{11:N1}\n",
							getColor(heat.up), heat.up,
							getColor(heat.down), heat.down,
							getColor(heat.right), heat.right,
							getColor(heat.left), heat.left,
							getColor(heat.front), heat.front,
							getColor(heat.back), heat.back
							);
						//msg.message = heathud;
						CoreDrag.instance.HeatWarningVisible(true);
						CoreDrag.instance.UpdateHeatWarning(HeatWarning);
					}
				}
				if (CoreDrag.instance.Pref.Show_Warning)
				{


					if (warn)
						ShowNotification("Heat Level: Warning {1}{0:N0}", tHeat, 16, MyFontEnum.White);
					else if (critical)
						ShowNotification("Heat Level: Critical {1}{0:N0}", tHeat, 16, MyFontEnum.Red);
					else if (tHeat > 250)
						ShowNotification("Heat Level: {1}{0:N0}", tHeat, 16, MyFontEnum.White);

				}

			}
		}

		private void Draw()
		{
			refreshCenterOfLift();
			if (showsmoke && m_doDraw)
			{
				UpdateBurnHelper(heat.front,	ref m_zmax_burn);
				UpdateBurnHelper(heat.back,		ref m_zmin_burn);
				UpdateBurnHelper(heat.up,		ref m_ymax_burn);
				UpdateBurnHelper(heat.down,		ref m_ymin_burn);
				UpdateBurnHelper(heat.right,	ref m_xmax_burn);
				UpdateBurnHelper(heat.left,		ref m_xmin_burn);
				
			}
		}
		private void UpdateBurnHelper(double heat, ref Dictionary<Vector3I, MyParticleEffect> particles)
		{
			if (heat > 500)
				updateBurnEffect(ref particles);
		}
		private void playsmoke(bool critical)
		{
			m_doDraw = false;

			if (showsmoke )
			{

				if (heat.front > 500)
				{
					if(heat.front < 900)
					{
						if(heat.stage_front == 1)
						{
							removeBurnEffect(ref m_zmax_burn);
						}
						heat.stage_front = 0;
					}
					else
					{
						if(heat.stage_front == 0)
						{
							removeBurnEffect(ref m_zmax_burn);
						}
						heat.stage_front = 1;
					}
					if (m_zmax_burn.Count == 0 || burnfcnt++ > 180)
					{
						burnfcnt = 0;

						createBurnEffect(Calc.zmax, ref m_zmax_burn, heat.stage_front);
					}
					m_doDraw = true;

				}
				else
				{
					removeBurnEffect(ref m_zmax_burn);
				}
				if (heat.back > 500)
				{
					if (heat.back < 900)
					{
						if (heat.stage_back == 1)
						{
							removeBurnEffect(ref m_zmin_burn);
						}
						heat.stage_back = 0;
					}
					else
					{
						if (heat.stage_back == 0)
						{
							removeBurnEffect(ref m_zmin_burn);
						}
						heat.stage_back = 1;
					}
					if (m_zmin_burn.Count == 0 || burnbcnt++ > 180)
					{
						burnbcnt = 0;

						createBurnEffect(Calc.zmin, ref m_zmin_burn, heat.stage_back);
					}

					m_doDraw = true;

				}
				else
				{
					removeBurnEffect(ref m_zmin_burn);
				}
				if (heat.up > 500)
				{
					if (heat.up < 900)
					{
						if (heat.stage_up == 1)
						{
							removeBurnEffect(ref m_ymax_burn);
						}
						heat.stage_up = 0;
					}
					else
					{
						if (heat.stage_up == 0)
						{
							removeBurnEffect(ref m_ymax_burn);
						}
						heat.stage_up = 1;
					}
					if (m_ymax_burn.Count == 0 || burnucnt++ > 180)
					{
						burnucnt = 0;
						createBurnEffect(Calc.ymax, ref m_ymax_burn, heat.stage_up);
					}
					m_doDraw = true;
				}
				else
				{
					removeBurnEffect(ref m_ymax_burn);
				}
				if (heat.down > 500)
				{
					if (heat.down < 900)
					{
						if (heat.stage_down == 1)
						{
							removeBurnEffect(ref m_ymin_burn);
						}
						heat.stage_down = 0;
					}
					else
					{
						if (heat.stage_down == 0)
						{
							removeBurnEffect(ref m_ymin_burn);
						}
						heat.stage_down = 1;
					}
					if (m_ymin_burn.Count == 0 || burndcnt++ > 180 )
					{
						burndcnt = 0;
						createBurnEffect(Calc.ymin, ref m_ymin_burn, heat.stage_down);
					}
					m_doDraw = true;
				}
				else
				{
					removeBurnEffect(ref m_ymin_burn);
				}
				if (heat.right > 500)
				{
					if (heat.right < 900)
					{
						if (heat.stage_right == 1)
						{
							removeBurnEffect(ref m_xmax_burn);
						}
						heat.stage_right = 0;
					}
					else
					{
						if (heat.stage_right == 0)
						{
							removeBurnEffect(ref m_xmax_burn);
						}
						heat.stage_right = 1;
					}
					if (m_xmax_burn.Count == 0 || burnrcnt++ > 180)
					{
						burnrcnt = 0;
						createBurnEffect(Calc.xmax, ref m_xmax_burn, heat.stage_right);
					}
					m_doDraw = true;
				}
				else
				{
					removeBurnEffect(ref m_xmax_burn);
				}
				if (heat.left > 500)
				{
					if (heat.left < 900)
					{
						if (heat.stage_left == 1)
						{
							removeBurnEffect(ref m_xmin_burn);
						}
						heat.stage_left = 0;
					}
					else
					{
						if (heat.stage_left == 0)
						{
							removeBurnEffect(ref m_xmin_burn);
						}
						heat.stage_left = 1;
					}
					if (m_xmin_burn.Count == 0 || burnlcnt++ > 180)
					{
						burnlcnt = 0;
						//removeBurnEffect(ref m_xmin_burn);
						createBurnEffect(Calc.xmin, ref m_xmin_burn, heat.stage_left);
					}
					m_doDraw = true;
					//updateBurnEffect(ref m_xmin_burn);
				}
				else
				{
					removeBurnEffect(ref m_xmin_burn);
				}
			}
			else
			{
				removeBurnEffect(ref m_xmax_burn);
				removeBurnEffect(ref m_ymax_burn);
				removeBurnEffect(ref m_zmax_burn);
				removeBurnEffect(ref m_xmin_burn);
				removeBurnEffect(ref m_ymin_burn);
				removeBurnEffect(ref m_zmin_burn);
			}
		}

		private void updateBurnEffect(ref Dictionary<Vector3I, MyParticleEffect> burn)
		{
			List<Vector3I> rem = new List<Vector3I>();
			foreach(KeyValuePair<Vector3I, MyParticleEffect> kval in burn)
			{
				try
				{
					var block = Grid.GetCubeBlock(kval.Key);
					if (block.IsDestroyed)
					{
						rem.Add(kval.Key);
						continue;
					}
					if(kval.Value.IsEmittingStopped)
					{
						rem.Add(kval.Key);
					}
					if(kval.Value.IsStopped)
					{
						rem.Add(kval.Key);
						continue;
					}

					Vector3 normal = -Vector3.Normalize(Grid.Physics.LinearVelocity);
					MatrixD effectMatrix = MatrixD.CreateWorld(Grid.GridIntegerToWorld(kval.Key), normal, Vector3.CalculatePerpendicularVector(normal));
					//kval.Value.Velocity = Grid.Physics.LinearVelocity;
					kval.Value.Velocity = -Grid.Physics.LinearVelocity;
                    kval.Value.WorldMatrix = effectMatrix;

				}
				catch
				{
					try
					{

						rem.Add(kval.Key);
					}
					catch
					{
						//Log.DebugWrite(DebugLevel.Error, "Error in update burn.");
					}
					
				}
			}
			foreach(Vector3I e in rem)
			{
				MyParticleEffect val;
				if(burn.TryGetValue(e, out val))
                {
					if(!val.IsStopped)
						val.Stop();
				}
				burn.Remove(e);
			}
		}

		private void removeBurnEffect(ref Dictionary<Vector3I, MyParticleEffect> burn)
		{
			if(burn.Count > 0)
			foreach(KeyValuePair<Vector3I, MyParticleEffect> kval in burn)
			{
					kval.Value.Stop();
			}
			burn.Clear();
		}

		private void createBurnEffect(Dictionary<side, Vector3I> side, ref Dictionary<Vector3I, MyParticleEffect> burn, int stage)
		{
			try
			{
				foreach (KeyValuePair<side, Vector3I> kpair in side)
				{
					if (burn.ContainsKey(kpair.Value))
						continue;
					MyParticleEffect _effect;
					//213 is full burn // or maybe 101?
					//48 is low speed CPU
					//49 try this one too
					int particle = 0;
					switch(stage)
					{
						case 0: particle = 48;
							break;
						case 1: particle = 213;
							break;
						default: particle = 213;
							break;
					}

                    MyParticlesManager.TryCreateParticleEffect(particle, out _effect);// 28 OK , 32 maybe with editing //451?
					
					if(_effect != null)
					{
						//_effect.Near = kpair.Value.CubeGrid.Render.NearFlag;
						_effect.UserScale = 0.5f * Grid.GridSize;
						Vector3 normal = -Vector3.Normalize(Grid.Physics.LinearVelocity);
						MatrixD effectMatrix = MatrixD.CreateWorld(Grid.GridIntegerToWorld(kpair.Value), normal , Vector3.CalculatePerpendicularVector(normal));
						effectMatrix.Translation = Grid.GridIntegerToWorld(kpair.Value);
						_effect.WorldMatrix = effectMatrix;

						_effect.Play();

						burn.Add(kpair.Value, _effect);
					}
					
	
				}
			}
			catch
			{

			}

		}

		private void doDamage()
		{
			tick++;
			if (tick < 50) return;

			tick = 0;
			if (heat.front > 750)
				applyDamage(heat.front, ref Calc.zmax, Base6Directions.Direction.Forward);
			if (heat.back > 750)
				applyDamage(heat.back, ref Calc.zmin,  Base6Directions.Direction.Backward);
			if (heat.left > 750)
				applyDamage(heat.left, ref Calc.xmin, Base6Directions.Direction.Left);
			if (heat.right > 750)
				applyDamage(heat.right, ref Calc.xmax,  Base6Directions.Direction.Right);
			if (heat.up > 750)
				applyDamage(heat.up, ref Calc.ymin,  Base6Directions.Direction.Up);
			if (heat.down > 750)
				applyDamage(heat.down, ref Calc.ymax, Base6Directions.Direction.Down);
		}

		private void applyDamage(double dmg, ref Dictionary<side, Vector3I> blocks, Base6Directions.Direction dir)
		{
			if (!CoreDrag.instance.isServer)//server only
				return;

			double min = 0;
			double mult = 1.0;
			HeatData data;
			keylist.Clear();
            string subtype = "";

            foreach (KeyValuePair<side, Vector3I> kpair in blocks)
			{
				try
				{
					if (Grid.Closed) return;
					var block = Grid.GetCubeBlock(kpair.Value);
					if(block.CubeGrid != Grid)
					{
						continue; // WTF KEEN???
					}
					if(block == null)
					{
						dirty = true;
						keylist.Add(kpair.Key);
						continue;
					}
					min = 750;
					mult = 1.0;
					subtype = block.BlockDefinition.Id.SubtypeName;


					MyBlockOrientation ndir;
					ndir = block.Orientation;
					Quaternion rot;
					ndir.GetQuaternion(out rot);
					switch (dir)
					{
						case Base6Directions.Direction.Forward:
							dir = Base6Directions.GetForward(rot);
							break;
						case Base6Directions.Direction.Backward:
							dir = Base6Directions.GetFlippedDirection(Base6Directions.GetForward(rot));
							break;
						case Base6Directions.Direction.Up:
							dir = Base6Directions.GetUp(rot);
							break;
						case Base6Directions.Direction.Down:
							dir = Base6Directions.GetFlippedDirection(Base6Directions.GetUp(rot));
							break;
						case Base6Directions.Direction.Right:
							dir = Base6Directions.GetLeft(Base6Directions.GetForward(rot), Base6Directions.GetUp(rot));
							break;
						case Base6Directions.Direction.Left:
							dir = Base6Directions.GetFlippedDirection(Base6Directions.GetLeft(Base6Directions.GetForward(rot), Base6Directions.GetUp(rot)));
							break;
					}
                        

					if (CoreDrag.instance.h_definitions.data.TryGetValue(subtype, out data))
					{
						min = data.getHeatTresh(dir);
						mult = data.getHeatMult(dir);
					}
					

			
					if (block.IsDestroyed)
					{
						dirty = true;
						Grid.RemoveDestroyedBlock(block);
						keylist.Add(kpair.Key);
						continue;
					}
					float damage = (float)(dmg - min);
					if (damage <= 0.0d) continue;
					damage /= 10;
					damage *= ((float)mult * Grid.GridSize);



					IMyDestroyableObject damagedBlock = block as IMyDestroyableObject;
					damagedBlock.DoDamage(damage, MyDamageType.Fire, true, attackerId: ClosePlanet?.EntityId ?? 0);
				}
				catch
				{
					dirty = true;//need an update
					keylist.Add(kpair.Key);
					continue;
				}
			}
			foreach(side key in keylist)
			{
				blocks.Remove(key);//clear
				
			}
        }


	}
	public struct side : IEquatable<side>
	{
		int a;
		int b;


		public side(int a, int b) : this()
		{
			this.a = a;
			this.b = b;
		}
		public bool Equals(side other)
		{
			return this.a == other.a && this.b == other.b;
		}
		public override bool Equals(Object obj)
		{
			return obj is side && this == (side)obj;
		}
		public override int GetHashCode()
		{
			return a.GetHashCode() ^ b.GetHashCode();
		}
		public static bool operator ==(side x, side y)
		{
			return x.a == y.a && x.b == y.b;
		}
		public static bool operator !=(side x, side y)
		{
			return !(x == y);
		}
	}
}
