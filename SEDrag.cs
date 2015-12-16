using System;
using System.Collections.Generic;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Components;
using Sandbox.Definitions;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

namespace SEDrag
{

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
	public class SEDrag : MyGameLogicComponent
	{
		int resolution = 200;
		IMyCubeGrid grid = null;
		//float small_max = 104.4f;
		//float large_max = 104.4f;
		BoundingBox dragBox;
		private bool init = false;
		private bool dirty = true;
		private int lastupdate = 0;
		Vector3D centerOfLift = Vector3.Zero;
		Dictionary<allside, IMySlimBlock> parimeterBlocks = new Dictionary<allside, IMySlimBlock>();
		Dictionary<side, IMySlimBlock> m_xmax = new Dictionary<side, IMySlimBlock>();
		Dictionary<side, IMySlimBlock> m_ymax = new Dictionary<side, IMySlimBlock>();
		Dictionary<side, IMySlimBlock> m_zmax = new Dictionary<side, IMySlimBlock>();
		Dictionary<side, IMySlimBlock> m_xmin = new Dictionary<side, IMySlimBlock>();
		Dictionary<side, IMySlimBlock> m_ymin = new Dictionary<side, IMySlimBlock>();
		Dictionary<side, IMySlimBlock> m_zmin = new Dictionary<side, IMySlimBlock>();
		private double heat_f = 0;
		private double heat_b = 0;
		private double heat_l = 0;
		private double heat_r = 0;
		private double heat_u = 0;
		private double heat_d = 0;
		private bool showlight = false;
		private bool dontUpdate = false;
		private double drag = 0;
		private Random m_rand = new Random((int)(DateTime.UtcNow.ToBinary()));
		IMyEntity lightEntity;
        int tick = 0;
		private double heatDelta = 0;

		public Dictionary<long, MyPlanet> planets {
			get
			{
				return Core.instance.planets;
			}
		}

		public float small_max
		{
			get
			{
				return Core.instance.small_max;
			}
		}

		public float large_max
		{
			get
			{
				return Core.instance.large_max;
			}

		}

		public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
		{
			return Entity.GetObjectBuilder(copy);
		}
		public void Update()
		{

		}
		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
			
		}

		private void refreshDragBox()
		{
			if (dontUpdate) return;
			//only call when blocks are added/removed
			//dragBox = grid.LocalAABB; //for now, replacing with complex search later. If we are over a certain number of blocks we will NOT do this intensive function. 
			if(dirty && lastupdate <= 0)
			{
				dirty = false;
				lastupdate = 100;
				List<IMySlimBlock> blocks = new List<IMySlimBlock>();
				List<side> lx = new List<side>();
				List<side> ly = new List<side>();
				List<side> lz = new List<side>();
				Dictionary<side, IMySlimBlock> o_xmax = new Dictionary<side, IMySlimBlock>();
				Dictionary<side, IMySlimBlock> o_ymax = new Dictionary<side, IMySlimBlock>();
				Dictionary<side, IMySlimBlock> o_zmax = new Dictionary<side, IMySlimBlock>();
				Dictionary<side, IMySlimBlock> o_xmin = new Dictionary<side, IMySlimBlock>();
				Dictionary<side, IMySlimBlock> o_ymin = new Dictionary<side, IMySlimBlock>();
				Dictionary<side, IMySlimBlock> o_zmin = new Dictionary<side, IMySlimBlock>();
				//double cx, cy, cz = 0;
				Vector3D comw = Entity.Physics.CenterOfMassWorld - Entity.GetPosition();


				double t_x = 0;
				double t_y = 0;
				double t_z = 0;
				IMySlimBlock t;
				bool ignore = false;
                //MyAPIGateway.Utilities.ShowMessage(Core.NAME, "realcenter: " + comw.ToString());
				grid.GetBlocks(blocks, delegate (IMySlimBlock e)
				{

					if (e is IMyInteriorLight)
					{
						var block = (IMyInteriorLight)e;
						if( block.BlockDefinition.SubtypeName == "lightDummy")
						{
							//Log.Info("IGNORING GRID!");
							ignore = true;
							return false;
						}
					}

					ignore = false;
					var x = new side(e.Position.Y, e.Position.Z);
					var y = new side(e.Position.X, e.Position.Z);
					var z = new side(e.Position.Y, e.Position.X);

					if (!lx.Contains(x))
					{
						lx.Add(x);
						o_xmax.Add(x, e);
						o_xmin.Add(x, e);
					}
					else
					{
						
						if (o_xmax.TryGetValue(x, out t))
						{
							if (t.Position.X > e.Position.X)
							{
								o_xmax.Remove(x);
								o_xmax.Add(x, e);
							}
						}
						if (o_xmin.TryGetValue(x, out t))
						{
							if (t.Position.X > e.Position.X)
							{
								o_xmin.Remove(x);
								o_xmin.Add(x, e);
							}
						}
					}

					if (!ly.Contains(y))
					{
						ly.Add(y);
						o_ymax.Add(y, e);
						o_ymin.Add(y, e);

					}
					else
					{
						if (o_ymax.TryGetValue(y, out t))
						{
							if (t.Position.Y > e.Position.Y)
							{
								o_ymax.Remove(y);
								o_ymax.Add(y, e);
							}
						}
						if (o_ymin.TryGetValue(y, out t))
						{
							if (t.Position.Y < e.Position.Y)
							{
								o_ymin.Remove(y);
								o_ymin.Add(y, e);
							}
						}
					}

					if (!lz.Contains(z))
					{
						lz.Add(z);
						o_zmax.Add(z, e);
						o_zmin.Add(z, e);
					}
					else
					{
						if (o_zmax.TryGetValue(z, out t))
						{
							if (t.Position.Z > e.Position.Z)
							{
								o_zmax.Remove(z);
								o_zmax.Add(z, e);
							}
						}
						if (o_zmin.TryGetValue(z, out t))
						{
							if (t.Position.Z < e.Position.Z)
							{
								o_zmin.Remove(z);
								o_zmin.Add(z, e);
							}
						}
					}
					return false;
				});
				if (ignore)
				{
					dontUpdate = true;
					return;
				}

				var center = grid.WorldToGridInteger(grid.Physics.CenterOfMassWorld);
				
				double xadj = center.X;
				double yadj = center.Y;
				double zadj = center.Z;
				//get parimeter blocks
				Dictionary<allside, IMySlimBlock> parim = new Dictionary<allside, IMySlimBlock>();

				generateParimeter(o_xmax, ref parim);
				generateParimeter(o_xmin, ref parim);
				generateParimeter(o_ymax, ref parim);
				generateParimeter(o_ymin, ref parim);
				generateParimeter(o_zmax, ref parim);
				generateParimeter(o_zmin, ref parim);


				m_xmax = o_xmax;
				m_xmin = o_xmin;
				m_ymax = o_ymax;
				m_ymin = o_ymin;
				m_zmax = o_zmax;
				m_zmin = o_zmin;

				var bb = new BoundingBox(Vector3.Zero, new Vector3(Math.Sqrt(lx.Count), Math.Sqrt(ly.Count), Math.Sqrt(lz.Count)) * (grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f));// * (grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f)
				dragBox = new BoundingBox(-bb.Center, bb.Center);//center the box

				foreach (KeyValuePair<allside, IMySlimBlock> entry in parim)
				{
					//add them up

					t_x += entry.Value.Position.X - xadj;
					t_y += entry.Value.Position.Y - yadj;
					t_z += entry.Value.Position.Z - zadj;
	
				}
				centerOfLift = new Vector3D(calcCenter(t_x, lx.Count), calcCenter(t_y, ly.Count), calcCenter(t_z, lz.Count));
				
				centerOfLift = Vector3D.Multiply(centerOfLift, (grid.GridSizeEnum == MyCubeSize.Small ? 0.5d : 2.5d));
				//centerOfLift += new Vector3D((grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f));
				if (Math.Abs(centerOfLift.X) < 1.5) centerOfLift.X = 0;
				if (Math.Abs(centerOfLift.Y) < 1.5) centerOfLift.Y = 0;
				if (Math.Abs(centerOfLift.Z) < 1.5) centerOfLift.Z = 0;
				//
				//
				//centerOfLift = -centerOfLift;//invert

				/*MyAPIGateway.Utilities.ShowMessage(Core.NAME, "center: " + center.ToString());
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("{0} Max: {1} - {0} Min: {2} Adj: {3}", "X", grid.Max.X, grid.Min.X, xadj));
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("{0} Max: {1} - {0} Min: {2} Adj: {3}", "Y", grid.Max.Y, grid.Min.Y, yadj));
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("{0} Max: {1} - {0} Min: {2} Adj: {3}", "Z", grid.Max.Z, grid.Min.Z, zadj));
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, "CenterOfLift_X: " + centerOfLift.X.ToString());
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, "CenterOfLift_Y: " + centerOfLift.Y.ToString());
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, "CenterOfLift_Z: " + centerOfLift.Z.ToString());*/


			}
			else if (dirty)
			{
				lastupdate--;
			}

		}

		void refreshLightGrid()
		{
			try
			{
				if (showlight )
				{
					if (lightEntity == null || lightEntity.Closed)
					{
						var def = MyDefinitionManager.Static.GetPrefabDefinitions();
						var prefab = MyDefinitionManager.Static.GetPrefabDefinition("LightDummy");
						var p_grid = prefab.CubeGrids[0];
						p_grid.PositionAndOrientation = new VRage.MyPositionAndOrientation(grid.Physics.CenterOfMassWorld + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 20), -Entity.WorldMatrix.Forward, Entity.WorldMatrix.Up);
						p_grid.LinearVelocity = Entity.Physics.LinearVelocity;
                        MyAPIGateway.Entities.RemapObjectBuilder(p_grid);
                         lightEntity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(p_grid);
						lightEntity.CastShadows = false;
						lightEntity.Flags |= EntityFlags.Visible;

					}
					else
					{
						
						if (lightEntity is IMyCubeGrid)
						{
							var lgrid = (IMyCubeGrid)lightEntity;
							List<IMySlimBlock> l = new List<IMySlimBlock>();
							Vector3 pos = (grid.Physics.CenterOfMassWorld + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 20));
							var block = grid.RayCastBlocks(pos, (grid.GetPosition() - Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 200)));
							if(block.HasValue)
							{
								pos = grid.GridIntegerToWorld(block.Value) + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 6);
							}
							lgrid.SetPosition(pos);
							if (lgrid.Physics != null)
								lgrid.Physics.LinearVelocity = Entity.Physics.LinearVelocity;
							var mat = lgrid.LocalMatrix;
							mat.Forward = Vector3.Normalize(Entity.Physics.LinearVelocity);
							lgrid.LocalMatrix = mat;
							lgrid.GetBlocks(l, delegate (IMySlimBlock e) {

							if (e.FatBlock is IMyReflectorLight)
							{

								//var color = MyMath.VectorFromColor(255,50,0);
									int delta = (int)(heatDelta/4 > 25 ? 25 : heatDelta/4);
									Color color = MyMath.VectorFromColor(255, (byte)(delta), 0, 100);
									var light = (IMyReflectorLight)e.FatBlock;
									/*if(e.FatBlock is Sandbox.Game.Entities.Blocks.MyLightingBlock)
									{
										var fatBlock = (Sandbox.Game.Entities.Blocks.MyLightingBlock)e.FatBlock;
										fatBlock.Intensity = Entity.Physics.LinearVelocity.Length();
										fatBlock.Falloff = 2;
									}*/
									
									//Log.Info("set intensity");
									light.SetValueFloat("Intensity", (float)heatDelta/2);
									light.SetValueFloat("Radius", grid.LocalAABB.Extents.Length() + 6);

									light.SetValue("Color", color);
									//light.SetColorMaskForSubparts(color);

								}
								return false;
							});
							
							//Log.Info("islight?");

				}
						else
							lightEntity.Close();

					}
				}
				else
				{
					if (lightEntity != null && !lightEntity.Closed)
						lightEntity.Close();
				}
			}
			catch (Exception ex)
			{
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("{0}", ex.Message));
				//Log.Info("Error");
			}
			
		}

		private double calcCenter(double t, int cnt)
		{
			if (cnt == 0) return 0.0f;

			return Math.Sqrt(Math.Abs(t / cnt)) * ( t > 0 ? 1 : -1) ;

		}

		private void generateParimeter(Dictionary<side, IMySlimBlock> edge, ref Dictionary<allside, IMySlimBlock> parim)
		{
			foreach (KeyValuePair<side, IMySlimBlock> entry in edge)
			{
				allside sides = new allside(entry.Value.Position.X, entry.Value.Position.Y, entry.Value.Position.Z);
				if (!parim.ContainsKey(sides))
				{
					parim.Add(sides, entry.Value);
				}
				// do something with entry.Value or entry.Key
			}
		}

		private void refreshBoxParallel()
		{
			if(dirty)
			{

				//MyAPIGateway.Parallel.Start()  or MyAPIGateway.Parallel.StartBackground()
            }
				
		}
		private void doDragBox()
		{


		}
		private void blockChange(IMySlimBlock obj)
		{
			dirty = true;
		}
		private void init_grid()
		{
			if(!init)
			{
				init = true;
				grid.OnBlockAdded += blockChange;
				grid.OnBlockRemoved += blockChange;
				grid.OnClosing += onClose;
				dirty = true;
				dragBox = grid.LocalAABB;
				refreshDragBox();
			}

		}

		private void onClose(IMyEntity obj)
		{
			grid.OnClosing -= onClose;
			grid.OnBlockAdded -= blockChange;
			grid.OnBlockRemoved -= blockChange;
			if (lightEntity != null && !lightEntity.Closed)
				lightEntity.Close();//close our 'effect'
		}

		public override void UpdateBeforeSimulation()
		{
			if (dontUpdate) return;
			if (MyAPIGateway.Utilities == null) return;
			if (grid == null )
			{
				if (Entity == null)
					return;
				if (Entity is IMyCubeGrid)
					grid = (IMyCubeGrid)Entity;
				else
					return;
			}
			if (grid.Physics == null) return;
			if (!init) init_grid();
			if (Core.instance == null)
				return;

			if (Core.instance.showCenterOfLift ) showLift();

            List<long> removePlanets = new List<long>();
			var dragForce = Vector3.Zero;
			float atmosphere = 0;

			try
			{
				foreach (var kv in planets)
				{
					var planet = kv.Value;

					if (planet.Closed || planet.MarkedForClose)
					{
						removePlanets.Add(kv.Key);
						continue;
					}

					if (planet.HasAtmosphere)
					{
						atmosphere += planet.GetAirDensity(Entity.GetPosition());
					}
				}
			
				showheat();

				//1370 is melt tempw
				heatLoss(atmosphere);
                if (atmosphere < 0.05f)
					return;

				if (Entity.Physics == null || Entity.Physics.LinearVelocity == Vector3D.Zero)
					return;//not moving
						   //refreshPlanets();

				dragForce = -Entity.Physics.LinearVelocity;
				
				Vector3 dragNormal = Vector3.Normalize(dragForce);
				MatrixD dragMatrix = MatrixD.CreateFromDir(dragNormal);
				MatrixD mat = MatrixD.Invert(Entity.WorldMatrix);
				dragMatrix = dragMatrix * mat;
				double aw = 0;
				double ah = 0;
				double ad = 0;
				double a = getArea(dragBox, Vector3.Normalize(dragMatrix.Forward), ref aw, ref ah, ref ad);

				double up =      getLiftCI(dragBox.Height, Vector3.Normalize(dragMatrix.Forward).Y);
				double right =   getLiftCI(dragBox.Width,  Vector3.Normalize(dragMatrix.Forward).X);
				double forward = getLiftCI(dragBox.Depth,  Vector3.Normalize(dragMatrix.Forward).Z);

				float c = (float)(0.25d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() * a);

				float u = (float)(up *      0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() );
				float l = (float)(right *   0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() );
				float f = (float)(forward * 0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() );

				float adj = 1;
				if (grid.GridSizeEnum == MyCubeSize.Small && small_max > 0)
					adj = 104.4f / small_max;
				if (grid.GridSizeEnum == MyCubeSize.Large && large_max > 0)
					adj = 104.4f / large_max;
				if (adj < 0.2f) adj = 0.2f;
				drag = c * Core.instance.settings.mult / 100 * adj;
                dragForce = Vector3.Multiply(dragNormal, (float)drag);
				Vector3 liftup    = Vector3.Multiply(Entity.WorldMatrix.Up,      u * Core.instance.settings.mult / 100 * adj);
				Vector3 liftright = Vector3.Multiply(Entity.WorldMatrix.Right,   l * Core.instance.settings.mult / 100 * adj);
				Vector3 liftforw  = Vector3.Multiply(Entity.WorldMatrix.Forward, f * Core.instance.settings.mult / 100 * adj);


				if (Core.instance.settings.advancedlift)
				{

					MatrixD c_lift = MatrixD.CreateTranslation(centerOfLift);
					c_lift *= grid.LocalMatrix.GetOrientation();
					var lift_adj = c_lift.Translation;
					if ((liftforw + liftright + liftup).Length() > 10.0f)
						grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -(liftforw + liftright + liftup), (grid.WorldMatrix.Translation + c_lift.Translation), Vector3.Zero);

				}
				else
				{

					if ((liftforw + liftright + liftup).Length() > 10.0f)
						grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -(liftforw + liftright + liftup), grid.Physics.CenterOfMassWorld, Vector3.Zero);
				}

				//if (dragForce.Length() > grid.Physics.Mass * 100 && grid.Physics.Mass > 0)
				//	spin = Vector3.Multiply(MyUtils.GetRandomVector3Normalized(), dragForce.Length() / (grid.Physics.Mass * 100));

				if (dragForce.Length() > 10.0f)//if force is too small, forget it. 
					grid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, dragForce, grid.Physics.CenterOfMassWorld, Vector3.Zero);
				applyHeat(-Vector3D.Multiply(Vector3D.Normalize(dragMatrix.Forward), c * Core.instance.settings.mult / 100 * adj), aw, ah, ad);

				if (removePlanets.Count > 0)
				{
					foreach (var id in removePlanets)
					{
						planets.Remove(id);
					}

					removePlanets.Clear();
				}

			
			}
			catch(Exception ex)
			{
				//Log.Info(ex.ToString());
			}
		}

		private void applyHeat(Vector3D dragVector, double aw, double ah, double ad)
		{
			var x = dragVector.X / aw;
			var y = dragVector.Y / ah;
			var z = dragVector.Z / ad;

			double scale = 100000;
			x /= scale;
			y /= scale;
			z /= scale;
			double nheatDelta = Math.Abs(x) + Math.Abs(y) + Math.Abs(z);
			heatDelta = (heatDelta > nheatDelta ? heatDelta - 0.01 : nheatDelta);
			if (heatDelta < 0) heatDelta = 0;
            if (x < 0)
			{
				//left
				heat_l += Math.Abs(x);
			}
			if (x > 0)
			{
				//right
				heat_r += Math.Abs(x);
			}
			if (y > 0)
			{
				//up
				heat_u += Math.Abs(y);
			}
			if (y < 0)
			{
				//down
				heat_d += Math.Abs(y);
			}
			if (z < 0)
			{
				//forward
				heat_f += Math.Abs(z);
			}
			if (z > 0)
			{
				//backward
				heat_b += Math.Abs(z);
			}
			overheatCheck();
        }



		private void heatLoss(float atmosphere)
		{
			if (atmosphere < 0.05) atmosphere = 0.05f;//good enough for space
			disappate(ref heat_f, atmosphere, dragBox.Depth);
			disappate(ref heat_b, atmosphere, dragBox.Depth);
			disappate(ref heat_l, atmosphere, dragBox.Width);
			disappate(ref heat_r, atmosphere, dragBox.Width);
			disappate(ref heat_u, atmosphere, dragBox.Height);
			disappate(ref heat_d, atmosphere, dragBox.Height);
		}

		private void disappate(ref double heat, float atmo, double area)
		{
			heat -= (heat * 0.001f * atmo * Core.instance.settings.radMult/100);//area should be removed this is for now. 
		}

		private void showLift()
		{

			if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null || MyAPIGateway.Session.ControlledObject.Entity == null || MyAPIGateway.Session.ControlledObject.Entity.Parent == null)
			{
				return;
			}

			if (MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Entity.EntityId)
			{
				
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("CenterofLift: {0:N4}, {1:N4}, {2:N4}", centerOfLift.X, centerOfLift.Y, centerOfLift.Z) );
				Core.instance.showCenterOfLift = false;
			}
		}
		private void showheat()
		{

			if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null || MyAPIGateway.Session.ControlledObject.Entity == null || MyAPIGateway.Session.ControlledObject.Entity.Parent == null)
			{
				return;
			}
			if (MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Entity.EntityId)
			{

				/*MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat f: {0:N4}", heat_f.ToString()));
				/*MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat b: {0:N4}", heat_b.ToString()));
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat u: {0:N4}", heat_u.ToString()));
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat d: {0:N4}", heat_d.ToString()));
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat l: {0:N4}", heat_l.ToString()));
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat r: {0:N4}", heat_r.ToString()));*/

			}
		}
		private void overheatCheck()
		{
			//MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat: {0:N0}", heatDelta));

			
			bool critical = false;
			bool warn = false;
			double heat = heat_f;

			if (heat_b > heat) heat = heat_b;
			if (heat_u > heat) heat = heat_u;
			if (heat_l > heat) heat = heat_l;
			if (heat_r > heat) heat = heat_r;
			if (heat_d > heat) heat = heat_d;

			if (heat > 750) critical = true;

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
			refreshLightGrid();
			if (!Core.instance.settings.heat)
				return;
			if (critical)
				doDamage();

            if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null || MyAPIGateway.Session.ControlledObject.Entity == null || MyAPIGateway.Session.ControlledObject.Entity.Parent == null)
			{
				return;
			}

			if (MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Entity.EntityId)
			{
				if (warn)
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: Warning {0:N0}", heat), 20, Sandbox.Common.MyFontEnum.White);
				else if (critical)
				{
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: Critical {0:N0}", heat), 20, Sandbox.Common.MyFontEnum.Red);
					
				}
				else if (heat > 250)
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: {0:N0}", heat), 20, Sandbox.Common.MyFontEnum.White);

				//Core.instance.showCenterOfLift = false;
			}
		}

		private void doDamage()
		{
			tick++;
			if (tick < 50) return;

			tick = 0;
			if (heat_f > 750)
			{
				applyDamage(heat_f, m_zmax);
			}
			if (heat_b > 750)
			{

				applyDamage(heat_b, m_zmin);
			}
			if (heat_l > 750)
			{

				applyDamage(heat_l, m_xmin);
			}
			if (heat_r > 750)
			{

				applyDamage(heat_r, m_xmax);
			}
			if (heat_u > 750)
			{

				applyDamage(heat_u, m_ymax);
			}
			if (heat_d > 750)
			{

				applyDamage(heat_d, m_ymin);
			}
		}

		private void applyDamage(double dmg, Dictionary<side, IMySlimBlock> blocks)
		{
			if (!Core.instance.isServer)//server only
				return;
			float damage = (float)(dmg - 750);
			damage /= 100;
			damage += 1;
			damage *= 5;
			damage *= (float)m_rand.NextDouble();
			if (damage < 0) return;
			List<side> keylist = new List<side>();
			//var hit = new Sandbox.Common.ModAPI.MyHitInfo();
			//hit.Position = Entity.Physics.LinearVelocity + Entity.GetPosition();
			//hit.Velocity = -Entity.Physics.LinearVelocity;

            foreach (KeyValuePair<side, IMySlimBlock> kpair in blocks)
			{
				//if (dirty)
				//	break;
				try
				{

					if (grid.Closed) return;
					var block = kpair.Value;
					if(block == null)
					{
						keylist.Add(kpair.Key);
						continue;
					}
					if (block.IsDestroyed)
					{
						grid.RemoveDestroyedBlock(block);
					}
					grid.ApplyDestructionDeformation(block);
                    IMyDestroyableObject damagedBlock = block as IMyDestroyableObject;
					damagedBlock.DoDamage(damage, Sandbox.Common.ObjectBuilders.Definitions.MyDamageType.Fire, true/*, hit, 0*/);
					

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

		private double getLiftCI(float width, float x)
		{
			double _ret = Math.Pow(width, 2) * x;
			return _ret * Math.Pow(Math.Cos(Math.Abs(x) * Math.PI)*-1+1,2)/32;
		}

		private double getArea(BoundingBox dragBox, Vector3 _v, ref double areawidth, ref double areaheight, ref double areadepth)
		{
			areawidth = dragBox.Width * Math.Abs(_v.X);
			areaheight = dragBox.Height * Math.Abs(_v.Y);
			areadepth = dragBox.Depth * Math.Abs(_v.Z);
            return Math.Pow(areawidth + areaheight + areadepth, 2);
		}

		private struct side : IEquatable<side>
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
		private struct allside : IEquatable<allside>
		{
			int a;
			int b;
			int c;


			public allside(int a, int b, int c) : this()
			{
				this.a = a;
				this.b = b;
				this.c = c;
			}
			public bool Equals(allside other)
			{
				return this.a == other.a && this.b == other.b && this.c == other.c;
			}
			public override bool Equals(Object obj)
			{
				return obj is side && this == (allside)obj;
			}
			public override int GetHashCode()
			{
				return a.GetHashCode() ^ b.GetHashCode();
			}
			public static bool operator ==(allside x, allside y)
			{
				return x.a == y.a && x.b == y.b && x.c == y.c;
			}
			public static bool operator !=(allside x, allside y)
			{
				return !(x == y);
			}
		}

	}
}
