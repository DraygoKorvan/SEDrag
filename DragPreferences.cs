using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SEDrag
{
	public enum DebugLevel
	{
		None = 0,
		Error,
		Info,
		Verbose,
		Custom
	}
	public class DragPreferences
	{
		private const string FILE = "dragpreferences.xml";
		private bool m_smoke = true;
		private bool m_burn = true;
		private bool m_warn = true;
		private bool m_ShowWindIndicator = true;
		private bool m_ShowHeatIndicator = true;
		private bool m_debugtext = false;
		private CenterOfLift m_CoLSetting = CenterOfLift.Auto;
		private DebugLevel m_debug = DebugLevel.None;
		private Vector2D m_DebugPosition = new Vector2D(-0.7, 1);
		private Vector2D m_HeatAlarmGuiPosition = new Vector2D(0, -0.2);
		private Vector2D m_WindIndicatorPosition = new Vector2D(0.2, -0.65);
		private Vector2D m_HeatIndicatorPosition = new Vector2D(1d, 0.8d);

		public bool Smoke
		{
			get
			{
				return m_smoke;
			}
			set
			{
				if(value != m_smoke)
				{
					m_smoke = value;
					saveXML(this);
				}
			}
		}

		public bool Burn
		{
			get
			{
				return m_burn;
			}
			set
			{
				if (value != m_burn)
				{
					m_burn = value;
					saveXML(this);
				}
			}
		}

		public bool Show_Warning
		{
			get
			{
				return m_warn;
			}
			set
			{
				if (value != m_warn)
				{
					m_warn = value;
					saveXML(this);
				}
			}
		}

		public bool ShowWindIndicator
		{
			get
			{
				return m_ShowWindIndicator;
			}
			set
			{
				if (value != m_ShowWindIndicator)
				{
					m_ShowWindIndicator = value;
					saveXML(this);
				}
			}
		}
		public Vector2D WindIndicatorPosition
		{
			get
			{
				return m_WindIndicatorPosition;
			}
			set
			{
				if (m_WindIndicatorPosition != value)
				{
					m_WindIndicatorPosition = value;
					saveXML(this);
				}
			}
		}

		public bool ShowHeatIndicator
		{
			get
			{
				return m_ShowHeatIndicator;
			}
			set
			{
				if (value != m_ShowHeatIndicator)
				{
					m_ShowHeatIndicator = value;
					saveXML(this);
				}
			}
		}
		public Vector2D HeatIndicatorPosition
		{
			get
			{
				return m_HeatIndicatorPosition;
			}
			set
			{
				if (m_HeatIndicatorPosition != value)
				{
					m_HeatIndicatorPosition = value;
					saveXML(this);
				}
			}
		}
		


		public CenterOfLift CoLSetting
		{
			get
			{
				return m_CoLSetting;

			}
			set
			{
				if(value != m_CoLSetting)
				{
					m_CoLSetting = value;
					saveXML(this);
				}
			}
		}
		public bool DebugText
		{
			get
			{
				return m_debugtext;
			}
			set
			{
				if (value != m_debugtext)
				{
					m_debugtext = value;
					saveXML(this);
				}
			}
		}
		public DebugLevel LogDebugLevel
		{
			get
			{
				return m_debug;
			}
			set
			{
				if(m_debug != value)
				{
					m_debug = value;
					saveXML(this);
				}
			}
		}

		public Vector2D DebugPosition
		{
			get
			{
				return m_DebugPosition;
			}
			set
			{
				if(m_DebugPosition != value)
				{
					m_DebugPosition = value;
					saveXML(this);
				}
			}
		}
		
		public Vector2D HeatAlarmGuiPosition
		{
			get
			{
				return m_HeatAlarmGuiPosition;
			}
			set
			{
				if (m_HeatAlarmGuiPosition != value)
				{
					m_HeatAlarmGuiPosition = value;
					saveXML(this);
				}
			}
		}
		public static void saveXML(DragPreferences Pref)
		{
			DebugLevel _debug = Pref.m_debug;
            if (Pref.m_debug == DebugLevel.Verbose)
				Pref.m_debug = DebugLevel.Info;
			//Log.DebugWrite(DebugLevel.Info, "Saving XML");
			var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FILE, typeof(DragSettings));
			writer.Write(MyAPIGateway.Utilities.SerializeToXML(Pref));
			Pref.m_debug = _debug;
			writer.Flush();
			writer.Close();
			//Log.DebugWrite(DebugLevel.Info, "Save Complete");
		}
		public static DragPreferences loadXML(bool l_default = false)
		{
			
			if (l_default)
				return new DragPreferences();
			try
			{
				if (MyAPIGateway.Utilities.FileExistsInLocalStorage(FILE, typeof(DragPreferences)))
				{
					var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(FILE, typeof(DragPreferences));
					var xmlText = reader.ReadToEnd();
					reader.Close();
					return MyAPIGateway.Utilities.SerializeFromXML<DragPreferences>(xmlText);
				}
			}
			catch (Exception ex)
			{

			}

			return new DragPreferences();
		}

		internal Dictionary<int, bool> GetAsDictionary(Dictionary<int, bool> dictionary)
		{
			dictionary.Add(dictionary.Count, Show_Warning);
			dictionary.Add(dictionary.Count, Burn);
			dictionary.Add(dictionary.Count, Smoke);
			return dictionary;
		}
	}
}
