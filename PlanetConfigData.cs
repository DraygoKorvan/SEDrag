using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.ModAPI;

namespace SEDrag
{
	public class PlanetConfigData
	{
		private double m_WindVelocity = 20d;
		private long m_ChangeRate = 3600;
		private IMyEntity m_Planet;
		private Action<PlanetConfigData> UpdateClients;


		public PlanetConfigData(IMyEntity e = null, Action<PlanetConfigData> OnUpdate = null)
		{
			m_Planet = e;
			UpdateClients = OnUpdate;
			Load();
			
		}

		public IMyEntity Planet
		{
			get
			{
				return m_Planet;
			}
		}
		public double WindVelocity
		{
			get
			{
				return m_WindVelocity;
			}
			set
			{
				m_WindVelocity = value;
				Save();
			}
		}
		public long WindChangeRate
		{
			get
			{
				return m_ChangeRate;
			}
			set
			{
				m_ChangeRate = value;
				Save();
			}
		}

		public void Save()
		{
			if (Planet == null)
				return;
			if (Planet.Storage == null)
				Planet.Storage = new MyModStorageComponent();

			if (Planet.Storage.ContainsKey(CoreDrag.MODGUID))
				Planet.Storage[CoreDrag.MODGUID] = WindVelocity.ToString() + " " + WindChangeRate.ToString(); 
			else
				Planet.Storage.Add(CoreDrag.MODGUID, WindVelocity.ToString() + " " + WindChangeRate.ToString());
			if(UpdateClients != null)
				UpdateClients(this);
		}

		public void Load()
		{
			if (Planet == null)
				return;
			if (Planet.Storage == null)
				Planet.Storage = new MyModStorageComponent();

			if (Planet.Storage.ContainsKey(CoreDrag.MODGUID) )
			{
				var words = Planet.Storage[CoreDrag.MODGUID].Split(' ');
				if(words.Length > 0)
				{
					double getter;
					if(double.TryParse(words[0], out getter))
					{
						WindVelocity = getter;
					}
				}
				if(words.Length > 1)
				{
					long getter;
					if (long.TryParse(words[1], out getter))
					{
						WindChangeRate = getter;
					}
				}
			}
			if (UpdateClients != null)
				UpdateClients(this);
			
		}

	}
}
