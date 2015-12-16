using System.Collections.Generic;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using VRage.ModAPI;
using Sandbox.ModAPI;
using System.Text;
using System;
using Sandbox.Definitions;
using Sandbox.Game.Entities;

namespace SEDrag
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Core : MySessionComponentBase
	{
		private bool init = false;
		public static Core instance;
		public bool isDedicated = false;
		public bool isServer = false;

		public DragSettings settings;

		private const string FILE = "dragsettings.xml";

		private const string MOD_NAME = "SEDrag";
		private int resolution = 0;
		public Dictionary<long, MyPlanet> planets = new Dictionary<long, MyPlanet>();
		public float small_max = 104.4f;
		public float large_max = 104.4f;
		private readonly ushort HELLO_MSG = 54001;
		private readonly ushort RESPONSE_MSG = 54002;
		private bool sentHello = false;
		private bool _registerClient = false;
		private bool _registerServer = false;

		public bool showCenterOfLift = false;
		public static string NAME
		{
			get
			{
				return MOD_NAME;
			}
		}

		public void Init()
		{
			if (init) return;//script already initialized, abort.


			//Log.Info("Initialized");
			instance = this;
			init = true;
			isServer = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;
			isDedicated = (MyAPIGateway.Utilities.IsDedicated && isServer);
			settings = new DragSettings();


			MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
			MyAPIGateway.Utilities.MessageEntered += MessageEntered;

			if (!isServer)
			{
				MyAPIGateway.Multiplayer.RegisterMessageHandler(RESPONSE_MSG, recieveData);
				_registerClient = true;
				sentHello = false; 

				return;
			}
			else
			{
				try
				{

					HashSet<IMyEntity> ent = new HashSet<IMyEntity>();
					MyAPIGateway.Entities.GetEntities(ent, delegate (IMyEntity e)
					{
						//Log.Info(e.DisplayName);
						if (e.DisplayName == "LightDummy35")
							return true;
						return false;
					});
					foreach (IMyEntity entity in ent)
					{
						entity.Close();
					}
					
				}
				catch
				{
					//
				}
				sentHello = true;//were a listener not a sender
				if (MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
				{
					MyAPIGateway.Multiplayer.RegisterMessageHandler(HELLO_MSG, recieveHello);
					_registerServer = true;
				}


			}
			loadXML();
		}

		private void recieveData(byte[] obj)
		{
			try
			{
				string str = new string(Encoding.UTF8.GetChars(obj));
				string[] words = str.Split(' ');
				int data = Convert.ToInt32(words[0]);
				int advlift = Convert.ToInt32(words[1]);
				settings.mult = data;
				int heat = Convert.ToInt32(words[2]);
				int dragMult = Convert.ToInt32(words[3]);
				//int auto = Convert.ToInt32(words[4]);
				settings.mult = dragMult;
                settings.advancedlift = (advlift == 1 ? true : false);
                settings.heat = (heat == 1 ? true : false);
				//settings.auto_advancedlift = (auto == 1 ? true : false);
			}
			catch(Exception ex)
			{

			}
		}

		private void recieveHello(byte[] obj)
		{
			try
			{
				ulong steamid = Convert.ToUInt64(new string(Encoding.UTF8.GetChars(obj)));

				string settingstr = string.Format("{0} {1} {2} {3}", settings.mult.ToString(), (settings.advancedlift == true ? "1" : "0"), (settings.heat == true ? "1" : "0"), settings.radMult.ToString()/*, (settings.auto_advancedlift == true ? "1" : "0")*/);
                MyAPIGateway.Multiplayer.SendMessageTo(RESPONSE_MSG, Encoding.UTF8.GetBytes(settingstr), steamid, true);
			}
			catch(Exception ex)
			{

			}

		}

		private void sendHello()
		{
			sentHello = true;
            MyAPIGateway.Multiplayer.SendMessageToServer(HELLO_MSG, Encoding.UTF8.GetBytes(MyAPIGateway.Session.Player.SteamUserId.ToString()) , true);
        }

		private void MessageEntered(string msg, ref bool visible)
		{
			if (msg.Equals("/drag", StringComparison.InvariantCultureIgnoreCase))
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Valid Server Commands /drag-get /drag-center /drag-save /drag-load /drag-mult /drag-advlift");
			if (!msg.StartsWith("/drag-", StringComparison.InvariantCultureIgnoreCase))
				return;
			if (msg.StartsWith("/drag-get", StringComparison.InvariantCultureIgnoreCase))
			{
				visible = false;
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Drag multiplier is {0}. AdvLift: {1} Heat: {2}", instance.settings.mult.ToString(), instance.settings.advancedlift.ToString(), instance.settings.heat.ToString()));
				return;
			}
			if (msg.StartsWith("/drag-center", StringComparison.InvariantCultureIgnoreCase))
			{
				visible = false;
				showCenterOfLift = true;
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Getting center of lift for piloted craft.");
                return;
				//MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Drag multiplier is {0}. AdvLift: {1}", instance.settings.mult.ToString(), instance.settings.advancedlift.ToString()));
				//return;
			}
			if (isServer)
			{

				visible = false;
				if (msg.StartsWith("/drag-advlift", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1 )
					{
						if (words[1].ToLower() == "on")
						{
							instance.settings.advancedlift = true;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Advanced Lift Simulation Enabled");
							return;
						}
						else if (words[1].ToLower() == "off")
						{
							instance.settings.advancedlift = false;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Advanced Lift Simulation disabled");
							return;
						}
						/*else if (words[1].ToLower() == "auto")
						{
							instance.settings.advancedlift = !instance.settings.advancedlift;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Auto-Advanced Lift Simulation " + (instance.settings.advancedlift ? "Enabled" : "Disabled"));
							return;
						}*/
					}

					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Command: /drag-advlift [on/off]");
					return;

				}
				if (msg.StartsWith("/drag-heat", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1)
					{
						if (words[1].ToLower() == "on")
						{
							instance.settings.heat = true;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Heat Damage Enabled");
							return;
						}
						else if (words[1].ToLower() == "off")
						{
							instance.settings.heat = false;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Heat Damage Disabled");
							return;
						}
					}
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Command: /drag-heat [on/off]");
					return;

				}
				if (msg.StartsWith("/drag-save", StringComparison.InvariantCultureIgnoreCase))
				{
					saveXML();
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "saved");
					return;
				}
				if (msg.StartsWith("/drag-load", StringComparison.InvariantCultureIgnoreCase))
				{
					saveXML();
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "loaded");
					return;
				}
				if (msg.StartsWith("/drag-mult", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1)
					{
						instance.settings.mult = Convert.ToInt32(words[1]);
						MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Drag multiplier set to {0}", instance.settings.mult.ToString()));

					}
					else
					{
						MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Command: /drag-mult [#]"));
					}
					return;
				}
				if (msg.StartsWith("/drag-radMult", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1)
					{
					instance.settings.radMult = Convert.ToInt32(words[1]);
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Heat Radiation Multiplier set to {0}", instance.settings.mult.ToString()));
					}
					else
					{
						MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Command: /drag-mult [#]"));
					}
					return;
				}
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Valid Server Commands /drag-get /drag-center /drag-save /drag-load /drag-mult /drag-advlift /drag-heat /drag-radMult");
			}
			else
			{
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Valid Client Commands /drag-get /drag-center");
			}
		}

		public void saveXML()
		{
			var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FILE, typeof(DragSettings));
			writer.Write(MyAPIGateway.Utilities.SerializeToXML(instance.settings));
			writer.Flush();
			writer.Close();
		}
		public void loadXML(bool l_default = false)
		{
			try
			{
				if (MyAPIGateway.Utilities.FileExistsInLocalStorage(FILE, typeof(DragSettings)) && !l_default)
				{
					var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(FILE, typeof(DragSettings));
					var xmlText = reader.ReadToEnd();
					reader.Close();
					instance.settings = MyAPIGateway.Utilities.SerializeFromXML<DragSettings>(xmlText);
					return;
				}
			}
			catch (Exception ex)
			{
				//Log.Info("Could not load configuration - loading defaults: " + ex.ToString());
			}
			try
			{
				if (MyAPIGateway.Utilities.FileExistsInGlobalStorage(FILE))
				{

					var reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(FILE);
					var xmlText = reader.ReadToEnd();
					reader.Close();
					settings = MyAPIGateway.Utilities.SerializeFromXML<DragSettings>(xmlText);
				}
			}
			catch (Exception ex)
			{
				//Log.Info("Could not load configuration: " + ex.ToString());
			}

		}

		public override void UpdateAfterSimulation()
		{
			Update();
		}

		private void Update()
		{
			if (!init)
			{
				if (MyAPIGateway.Session == null)
					return;
				if (MyAPIGateway.Multiplayer == null && MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
					return;
				Init();
			}
			if (MyAPIGateway.Session == null)
			{
				unload();
			}
			if (MyAPIGateway.Session.Player == null)
				return;
			if (!sentHello)
				sendHello();
			if (resolution % 20000 == 0 || planets.Count == 0) // mod should only be run on a map with planets, otherwise whats the point?
			{
				HashSet<IMyEntity> ents = new HashSet<IMyEntity>();
				if (MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed > 100f)
					small_max = MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;
				if (MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed > 100f)
					large_max = MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed;
				MyAPIGateway.Entities.GetEntities(ents, delegate (IMyEntity e)
				{
					if (e is MyPlanet)
					{
						if (!planets.ContainsKey(e.EntityId))
							planets.Add(e.EntityId, e as MyPlanet);
					}

					return false; // no reason to add to the list
				});
				resolution = 1;
			}
			else
				resolution++;
		}

		protected override void UnloadData()
		{
			unload();
		}


		public void unload()
		{
			//Log.Info("Unloading mod");
			MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
			init = false;
			isServer = false;
			isDedicated = false;
			settings = null;
			//Log.Close();
			if (_registerServer)
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(HELLO_MSG, recieveHello);
			if(_registerClient)
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(RESPONSE_MSG, recieveData);
			//CommunicationManager.unload();

		}
	}
}
