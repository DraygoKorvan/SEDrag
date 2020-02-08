using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using Sandbox.Definitions;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game.ModAPI;
using VRage.Utils;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using Sandbox.Engine.Physics;

namespace SEDrag
{
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_WindTurbine), true)]
	public class TurbineControl : MyGameLogicComponent
	{
		IMyPowerProducer Producer;
		bool onaddcalled = false;
		MyResourceSourceComponent PowerSource;
		float CurrentMaxOutput = 0f;
		MyWindTurbineDefinition m_TurbineDef;
		Vector3D lastWindSpeed = Vector3D.Zero;
		MyPlanet ClosePlanet = null;
		bool init = false;

		float m_OcclusionRatio = 1f;
		float m_AirDensity = 0f;
		private List<IHitInfo> HitInfo = new List<IHitInfo>();

		float m_windSpeed = 0f;

		bool hasMoved = false;
		Vector3D lastLocation = Vector3D.Zero;

		public Vector3D WindSpeed {
			get
			{
				if (CoreDrag.instance == null)
					return Vector3D.Zero;
				else
				{
					return CoreDrag.instance.GetWeatherAtPoint(ClosePlanet, Entity.WorldMatrix.Translation, 0, Entity);
				}
			}
		}
		public float OcclusionRatio
		{
			get
			{
				return m_OcclusionRatio;
			}
			private set
			{
				m_OcclusionRatio = value;
			}
		}

		public float AirDensity
		{
			get
			{
				return m_AirDensity;
			}
			private set
			{
				m_AirDensity = value;
			}

		}


		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			this.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
		}
		public override void OnAddedToContainer()
		{
			if (onaddcalled) //gets called twice when this is object is created. 
				return;
			onaddcalled = true;




		}

		private void InitializeOverrides()
		{

			foreach (var comp in Producer.Components)
			{
				if (comp is MyResourceSourceComponent)
				{
					//MyAPIGateway.
					PowerSource = comp as MyResourceSourceComponent;
					PowerSource.MaxOutputChanged += PowerSource_MaxOutputChanged;
					UpdateClosePlanet();
					UpdateMaxOutput();
				}
			}
		}

		private void PowerSource_OutputChanged(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source)
		{

		}

		private void PowerSource_MaxOutputChanged(MyDefinitionId changedResourceId, float oldOutput, MyResourceSourceComponent source)
		{
			if (source.MaxOutput == CurrentMaxOutput)
				return;
			UpdateMaxOutput();
		}

		private void UpdateMaxOutput()
		{

			if(CoreDrag.instance?.settings?.SimulateWind ?? false)
			{
				if (!Producer.Enabled || hasMoved || !Producer.IsWorking || ((MyCubeGrid)Producer.CubeGrid).IsPreview || ClosePlanet == null || Producer.CubeGrid.Physics == null || ClosePlanet.PositionComp.WorldAABB.Contains(Producer.PositionComp.WorldMatrix.Translation) == ContainmentType.Disjoint)
				{
					CurrentMaxOutput = 0;
				}
				else
				{
					m_windSpeed = (float)WindSpeed.Length();
					CurrentMaxOutput = MathHelper.Clamp(m_windSpeed / 6f, 0f, 1f) * m_TurbineDef.MaxPowerOutput * OcclusionRatio * m_AirDensity;
				}
				PowerSource.SetMaxOutput(CurrentMaxOutput);

			}
			else
			{
				CurrentMaxOutput = PowerSource.MaxOutput;
            }
  
        }

		public override void UpdateBeforeSimulation100()
		{
			InitProducer();

			if (CoreDrag.instance?.settings?.SimulateWind ?? false)
			{
				UpdateClosePlanet();
				UpdateAtmosphere();
				OcclusionCheck();
				var newLocation = Producer.WorldMatrix.Translation;
				hasMoved = (newLocation - lastLocation).LengthSquared() > 1d;
				lastLocation = newLocation;
				UpdateMaxOutput();
			}
			else
			{
				if(simcheck)
				{
					simcheck = false;
					Producer.UpdateIsWorking();
					var state = Producer.Enabled;
					Producer.Enabled = !state;
					Producer.Enabled = state;
				}
			}
        }
		private void InitProducer()
		{
			if (init)
				return;

			init = true;
			Producer = Entity as IMyPowerProducer;

			Entity.OnMarkForClose += Entity_OnMarkForClose;

			MyCubeBlockDefinition BlockDefinition;

			MyDefinitionManager.Static.TryGetCubeBlockDefinition(Producer.BlockDefinition, out BlockDefinition);
			m_TurbineDef = (MyWindTurbineDefinition)BlockDefinition;

			InitializeOverrides();
		}


		private void UpdateAtmosphere()
		{
			if(ClosePlanet != null)
			{
				AirDensity = ClosePlanet.GetAirDensity(Entity.WorldMatrix.Translation);
            }
		}
		bool simcheck = false;
		private void OcclusionCheck()
		{
			if (!init)
				return;
			if (Producer?.CubeGrid == null)
			{
				return;
			}

			if (Producer.Position == null)
			{
				return;
			}
			//Vector4 color = Color.White.ToVector4();
			if (CoreDrag.instance?.settings?.SimulateWind ?? false)
			{
				simcheck = true;
				HitInfo.Clear();
				//check direction of wind. 
				var windNormal = Vector3D.Normalize(WindSpeed);
				//upnormal

				var center = Entity.WorldVolume.Center + Entity.WorldMatrix.Up * 2.5d;
				var start = center + (windNormal * 25d);
				var end = center + (windNormal * -25d);

				var planarratio = (1f - (float)Vector3D.Dot(Entity.WorldMatrix.Up, windNormal));

				MyAPIGateway.Physics.CastRay(center, end, HitInfo);
				MyAPIGateway.Physics.CastRay(center, start, HitInfo);

				float distfraction = 1.0f;
				foreach(var hit in HitInfo)
				{
					if(hit.Fraction < distfraction)
					{
						distfraction = hit.Fraction;
					}
				}
				OcclusionRatio = MathHelper.Clamp(distfraction, 0f, 1f) * planarratio;
            }

		}
		int EffectivityId = 0;
	
		private void UpdateClosePlanet()
		{
			var cp = MyGamePruningStructure.GetClosestPlanet(Entity.WorldMatrix.Translation);
			if(cp != null && ClosePlanet != cp)
			{
				ClosePlanet = cp;
				
			}
		}

		private void Entity_OnMarkForClose(IMyEntity obj)
		{
			Producer = null;
			onaddcalled = false;
			if(Entity != null)
			{
				Entity.OnMarkForClose -= Entity_OnMarkForClose;
			}
			if(PowerSource != null)
			{
				//PowerSource.OutputChanged -= PowerSource_OutputChanged;
				PowerSource.MaxOutputChanged -= PowerSource_MaxOutputChanged;
			}
			PowerSource = null;

		}

		public override void OnBeforeRemovedFromContainer()
		{

		}
	}
}
