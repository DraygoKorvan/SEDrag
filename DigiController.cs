using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace SEDrag
{
	public class DigiController
	{
		private bool init = false;
		private const long AERODYNAMICS_WORKSHOP_ID = 473571246;
		private Func<bool> aerodynamicsGetter = null;
		private Action<bool, string> aerodynamicsSetter = null;
		private const string MODNAME = "Draygo ModID:571920453";
        public bool digiAerodynamics
		{
			get
			{
				return aerodynamicsGetter?.Invoke() ?? true;
            }
			set
			{
				if (aerodynamicsSetter == null || aerodynamicsGetter == null)
					return;
				var enabled = aerodynamicsGetter.Invoke();
				if(enabled != value)
				aerodynamicsSetter.Invoke(value, MODNAME);
            }
		}
		public void Init()
		{
			MyAPIGateway.Utilities.RegisterMessageHandler(AERODYNAMICS_WORKSHOP_ID, AerodynamicsMethods);
		}
		public void UnloadData()
		{
			MyAPIGateway.Utilities.UnregisterMessageHandler(AERODYNAMICS_WORKSHOP_ID, AerodynamicsMethods);
		}
		public void Update()
		{
			if (CoreDrag.instance?.settings == null)
				return;
			if(init)
			{
				if (aerodynamicsGetter == null)
					return;
				if (CoreDrag.instance.settings.digi != digiAerodynamics)
				{
					digiAerodynamics = CoreDrag.instance.settings.digi;
				}
				//MyAPIGateway.Utilities.ShowMessage("DigiSetting", digiAerodynamics.ToString());
			}

		}

		 void AerodynamicsMethods(object obj)
		 {

		    if(obj is MyTuple<Func<bool>, Action<bool, string>>)
		    {

					init = true;
					var methods = (MyTuple<Func<bool>, Action<bool, string>>)obj;
					aerodynamicsGetter = methods.Item1;
					aerodynamicsSetter = methods.Item2;

					// avoiding a collection changed exception by calling the unregister after it's done iterating handlers.
					MyAPIGateway.Utilities.InvokeOnGameThread(() => MyAPIGateway.Utilities.UnregisterMessageHandler(AERODYNAMICS_WORKSHOP_ID, AerodynamicsMethods));

				
		    }
		 }
	}
}
