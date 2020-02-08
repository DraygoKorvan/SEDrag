using System;
using System.Collections.Generic;

namespace SEDrag
{
	public class DragSettings
	{
		private int m_mult = 500;
		private int m_radMult = 400;
		private bool m_lift = false;
		private bool m_heat = true;
		private bool m_digi = false;
		private bool m_SimulateWind = false;
		private bool m_EnableCharacterDrag = true;
		private bool m_EnableOcclusion = true;
		private int m_deflectionMult = 100;
		private int m_atmosphericMinimum = 0;
		private int m_rotationalDragMultiplier = 100;
		//private bool m_autolift = false;

		public bool EnableCharacterDrag
		{
			get
			{
				return m_EnableCharacterDrag;
			}
			set
			{
				if (m_EnableCharacterDrag == value)
					return;
				m_EnableCharacterDrag = value;
			}
		}

		public int DeflectionMult
		{
			get
			{
				return m_deflectionMult;
			}
			set
			{
				if (value >= 0)
					m_deflectionMult = value;
			}
		}

		public int AtmosphericMinimum
		{
			get
			{
				return m_atmosphericMinimum;
			}
			set
			{
				if (value >= 0 && value <= 100)
					m_atmosphericMinimum = value;
			}
		}

		public int RotationalDragMultiplier
		{
			get
			{
				return m_rotationalDragMultiplier;
			}
			set
			{
				if (value >= 0 )
					m_rotationalDragMultiplier = value;
			}
		}

		public bool advancedlift
		{
			get
			{
				return m_lift;
			}
			set
			{
				if (m_lift == value)
					return;

				m_lift = value;

			}
		}

		public bool SimulateWind
		{
			get
			{
				return m_SimulateWind;
			}
			set
			{
				if(value != m_SimulateWind)
				{
					m_SimulateWind = value;
				}
			}
		}

		public int mult
		{
			get
			{
				return m_mult;
			}
			set
			{
				if (value > 0)
					m_mult = value;
			}
		}
		public int radMult
		{
			get
			{
				return m_radMult;
			}
			set
			{
				if (value > 0)
					m_radMult = value;
			}
		}
		public bool heat
		{
			get
			{
				return m_heat;
			}
			set
			{
				m_heat = value;
			}
		}


		public bool digi
		{
			get
			{
				return m_digi;
			}
			set
			{
				if (m_digi == value)
					return;
				m_digi = value;

            }
		}

		public bool EnableOcclusion
		{
			get
			{
				return m_EnableOcclusion;
			}
			set
			{
				if (m_EnableOcclusion == value)
					return;
				m_EnableOcclusion = value;
			}
		}
	}
}
