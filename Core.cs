using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using VRage.Game.Components;
using VRage.Game;
using VRage.ModAPI;
using Sandbox.ModAPI;
using System.Text;
using System;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using SEDrag.Definition;
using VRageMath;
using System.Text.RegularExpressions;
using VRage.Game.ModAPI;
using Draygo.API;
using VRage.Utils;
using ProtoBuf;
using SEDrag.HudElement;
using VRage;


using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using Sandbox.Game.World;
//using VRage.Game.ModAPI.Ingame;

namespace SEDrag
{
	public enum CenterOfLift : int
	{
		Off = 0,
		On = 1,
		Auto = 2
	}

	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class CoreDrag : MySessionComponentBase
	{
		public const bool SERVER_ONLY_PHYSICS = false;
		private bool init = false;
		private long tick = 0;
		public static CoreDrag instance;
		public HudAPIv2 TextHUD;
		public HudAPIv2.HUDMessage HeatMsg;
		public HudAPIv2.BillBoardHUDMessage HeatMsgBackground;
		public HudAPIv2.HUDMessage HeatAlarmMsg;
		public HudAPIv2.HUDMessage DebugMessage;
		public Vector2D HudHeatWarningPos = new Vector2D(1d, 0.8d);
		public bool isDedicated = false;
		public bool isServer = false;
		public const long MODID = 571920453;
		public WeatherMapGenerator WGenerator;
		HudDisplay WindDisplay;

		TimeSpan UpdateTime = TimeSpan.Zero;
		TimeSpan DrawTime = TimeSpan.Zero;

		GridHeatData rHDCache = new GridHeatData();

		private Dictionary<long, PlanetConfigData> WindData = new Dictionary<long, PlanetConfigData>();
		private bool PlanetMenuInit = false;

		public bool NewHud
		{
			get
			{
				return TextHUD != null && TextHUD.Heartbeat;
			}
		}
		public DragSettings settings = new DragSettings();
		public DragPreferences Pref = new DragPreferences();
		private const string FILE = "dragsettings.xml";

		private const string MOD_NAME = "SEDrag";
		private int resolution = 0;
		public Dictionary<long, MyPlanet> planets = new Dictionary<long, MyPlanet>();
		public float small_max = 104.4f;
		public float large_max = 104.4f;
		private readonly ushort HELLO_MSG = 54001;
		private readonly ushort RESPONSE_MSG = 54002;
		private readonly ushort HEATDATA_MSG = 54003;
		private readonly ushort UPDATE_PLANET = 54004;
		private readonly ushort TICK_SYNC = 54005;
		private bool sentHello = false;
		private bool _registerClient = false;
		private bool _registerServer = false;
		public bool _recievedGameSettings = false;

		internal ThreadManager TaskManager = null;
		public HeatDefinition h_definitions = new HeatDefinition();
		public Dictionary<IMyEntity, DragBase> dragDictionary = new Dictionary<IMyEntity, DragBase>();
		public Dictionary<long, GridHeatData> heatTransferCache = new Dictionary<long, GridHeatData>();

		internal static Action UpdateHook;
		internal static Action DrawHook;
		internal static Action CloseHook;

		private DigiController DigiSettings = new DigiController();

		public long? LastEntityIdForCenterOfLift = null;

		public static Guid MODGUID = new Guid("c082d392-537c-41ee-a1b9-ceef29b2ff26");

		private static double  DEFAULTSPEED = 5d;

		private Stack<SEDrag> DragStack = new Stack<SEDrag>(1000);

		int seed = 0;

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
			instance = this;
			init = true;
			isServer = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;
			isDedicated = (MyAPIGateway.Utilities.IsDedicated && isServer);
			TaskManager = new ThreadManager();
			HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
			MyAPIGateway.Entities.GetEntities(entities, delegate (IMyEntity e) {
				Entities_OnEntityAdd(e);
				return false;
			});
			MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
			MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;
			MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
			MyAPIGateway.Utilities.MessageEntered += MessageEntered;
			DigiSettings.Init();

			Pref = DragPreferences.loadXML();
			if (Pref == null)
				Pref = new DragPreferences();
			if (!isDedicated)
			{
				TextHUD = new HudAPIv2(InitTextHud);
				WindDisplay = new HudDisplay(TextHUD);
				TextHUD.OnScreenDimensionsChanged += ResetHud;
				DrawHook += WindDisplay.Update;
				WindDisplay.Origin = Pref.WindIndicatorPosition;
            }

			if (MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed > 100f)
				small_max = MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;
			if (MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed > 100f)
				large_max = MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed;
			if (!isServer)
			{
				MyAPIGateway.Multiplayer.RegisterMessageHandler(RESPONSE_MSG, recieveData);
				MyAPIGateway.Multiplayer.RegisterMessageHandler(HEATDATA_MSG, recieveHeatData);
				MyAPIGateway.Multiplayer.RegisterMessageHandler(TICK_SYNC, recieveTickSync);
				MyAPIGateway.Multiplayer.RegisterMessageHandler(UPDATE_PLANET, recievePlanetUpdate);
				_registerClient = true;
				sentHello = false;
				//loadClientXML(MyAPIGateway.Session.Name);
				return;
			}
			else
			{

				sentHello = true;//were a listener not a sender
				if (MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
				{
					MyAPIGateway.Multiplayer.RegisterMessageHandler(HELLO_MSG, recieveHello);
					MyAPIGateway.Multiplayer.RegisterMessageHandler(TICK_SYNC, recieveTickSync);
					_registerServer = true;
				}
				//generate the weather simulator map. 
				seed = DateTime.UtcNow.Millisecond;
				WGenerator = new WeatherMapGenerator(seed);//going to move this to a later thread


				loadXML();

				if (instance.settings == null)
					instance.settings = new DragSettings();
				if(isDedicated)
				{
					saveXML();//write the settings file so server admins can change it. 
				}
					

				h_definitions.Init();//init definitions
				h_definitions.Load();//load definitions.
									 //register for mod comms
				MyAPIGateway.Utilities.RegisterMessageHandler(MODID, ModCommunication); //http://steamcommunity.com/sharedfiles/filedetails/?id=571920453
				SendModSettings();
				
			}
		}

		internal void AddToPool(SEDrag sEDrag)
		{
			DragStack.Push(sEDrag);

		}

		[ProtoContract]
		struct PlanetConfigDataMessage
		{
			[ProtoMember(1)]
			public long EntityId;
			[ProtoMember(2)]
			public double Speed;
			[ProtoMember(3)]
			public long WindChangeRate;
		}
		private void recievePlanetUpdate(byte[] obj)
		{
			try
			{
				var msg = MyAPIGateway.Utilities.SerializeFromBinary<PlanetConfigDataMessage>(obj);
				//IMyEntity ent = MyAPIGateway.Entities.GetEntityById(msg.EntityId);
				if(WindData.ContainsKey(msg.EntityId))
				{
					PlanetConfigData ConfigDataOut;
					if(WindData.TryGetValue(msg.EntityId, out ConfigDataOut))
					{
						ConfigDataOut.WindChangeRate = msg.WindChangeRate;
						ConfigDataOut.WindVelocity = msg.Speed;
						return;
					}
				}

				var ConfigData = new PlanetConfigData(null, null);
				ConfigData.WindChangeRate = msg.WindChangeRate;
				ConfigData.WindVelocity = msg.Speed;
				WindData.Add(msg.EntityId, ConfigData);
			}
			catch
			{

			}
		}

		private void OnUpdateClients(PlanetConfigData obj)
		{
			if(isServer)
			{
				var Msg = new PlanetConfigDataMessage();
				Msg.EntityId = obj.Planet.EntityId;
				Msg.Speed = obj.WindVelocity;
				Msg.WindChangeRate = obj.WindChangeRate;
				MyAPIGateway.Multiplayer.SendMessageToOthers(UPDATE_PLANET, MyAPIGateway.Utilities.SerializeToBinary(Msg));
			}

		}
		private void SendPlanetDataToClient(ulong pid)
		{
			foreach(var obj in WindData)
            {
				var Msg = new PlanetConfigDataMessage();
				Msg.EntityId = obj.Value.Planet.EntityId;
				Msg.Speed = obj.Value.WindVelocity;
				Msg.WindChangeRate = obj.Value.WindChangeRate;
				MyAPIGateway.Multiplayer.SendMessageTo(UPDATE_PLANET, MyAPIGateway.Utilities.SerializeToBinary(Msg), pid);
			}
		}

		#region WindSystem
		public Vector3D GetWeatherAtPoint(MyPlanet Planet, Vector3D point, double altitude, IMyEntity Obj)
		{
			if (WGenerator == null)
				return Vector3D.Zero;
			//get vector to center point of planet
			if (Planet == null)
				return Vector3D.Zero;
			if (instance?.settings == null || !instance.settings.SimulateWind)
				return Vector3D.Zero;

			//Planet.PositionLeftBottomCorner - Planet.SizeInMetresHalf
			var Vector = Planet.LocationForHudMarker - point;
			var Length = Vector.Length();//expensive but we need this

			//get our 3 points for our map transform. Force will be multiplied by (PI - abs(lamda)) / PI as we want to stay away from the top and bottom edges of our randomized map. 

			var phi1 = GetPhi(Vector.Z, Length);
			var lambda1 = GetLamda(Vector.X, Vector.Y);

			var phi2 = GetPhi(Vector.Y, Length);
			var lambda2 = GetLamda(Vector.X, Vector.Z);

			var phi3 = GetPhi(Vector.X, Length);
			var lambda3 = GetLamda(Vector.Y, Vector.Z);

			Vector3D wnd = new Vector3D(GetWind(Planet, phi1, lambda1) * Math.Abs(phi1), 0);
			MatrixD Trans = MatrixD.Invert(MatrixD.Normalize(MatrixD.CreateFromDir(Vector3D.Normalize(Vector), new Vector3D(0, 0, 1))));
			Vector3D WorldWind = Vector3D.Transform(wnd, Trans);

			wnd = new Vector3D(GetWind(Planet, phi2, lambda2) * Math.Abs(phi2), 0);
			Trans = MatrixD.Invert(MatrixD.Normalize(MatrixD.CreateFromDir(Vector3D.Normalize(Vector), new Vector3D(0, 1, 0))));
			WorldWind += Vector3D.Transform(wnd, Trans);

			wnd = new Vector3D(GetWind(Planet, phi3, lambda3) * Math.Abs(phi3), 0);
			Trans = MatrixD.Invert(MatrixD.Normalize(MatrixD.CreateFromDir(Vector3D.Normalize(Vector), new Vector3D(1, 0, 0))));
			WorldWind += Vector3D.Transform(wnd, Trans);

			var planarNormaltoCenter = Vector3D.Normalize(point - (Planet.PositionLeftBottomCorner + (Vector3D)Planet.SizeInMetresHalf));

			WorldWind = WorldWind * 0.1d + Vector3D.ProjectOnPlane(ref WorldWind, ref planarNormaltoCenter) * 0.9d;


			return WorldWind * GetPlanetSpeed(Planet) * Math.Abs(phi3);
		}
		private Vector2 GetWind(MyPlanet planet, float longitude, float latitude)
		{
			Vector2 Wind = Vector2.Zero;
			Wind.X = GetDirectionalWind(planet, longitude, latitude, -0.2f, 1.0f, 0.0f);
			Wind.Y = GetDirectionalWind(planet, longitude, latitude, -1.0f, 1.0f, 0.25f);
			return Wind;
		}
		private float GetDirectionalWind(MyPlanet planet, float longitude, float latitude, float multx, float multy, float plusl)
		{
			return MathHelper.Lerp(multx, multy, WGenerator.GetValueAtPointNormalized((longitude + plusl) % 1f, latitude % 1f, (int)(GetChangeRate(planet) % 512)));
		}

		private long GetChangeRate(MyPlanet planet)
		{
			long cr = 60;

			PlanetConfigData val;
			if (WindData.TryGetValue(planet.EntityId, out val))
			{
				cr = val.WindChangeRate;
			}

			return (tick / cr);
        }

		/// <summary>
		/// Get Phi value for given up and length
		/// </summary>
		/// <param name="up">Up component of a vector</param>
		/// <param name="len">Length</param>
		/// <returns>value between 0 and 1</returns>
		public float GetPhi(double up, double len)
		{
			return (float)(Math.Acos(up / len) / Math.PI);
		}

		/// <summary>
		/// Returns Lamda of given vector components
		/// </summary>
		/// <param name="X">X component (forward, back, left, or right) </param>
		/// <param name="Y">Y component (forward, back, left, or right)</param>
		/// <returns>value between zero and one</returns>
		public float GetLamda(double X, double Y)
		{
			return (float)((Math.Atan2(X, Y) + Math.PI) / (2d * Math.PI));
        }
		#endregion

		private enum SettingsEnum : int
		{
			digi = 0,
			advlift,
			show_warning,
			showburn,
			showsmoke
		}
		private object GetSetting(int Setting)
		{
			switch((SettingsEnum)Setting)
			{
				case SettingsEnum.digi:
					return instance.settings.digi;
				case SettingsEnum.advlift:
					return instance.settings.advancedlift;
			}
			return null;
		}
	
		private Vector3D GetWindAtPoint(MyPlanet Planet, Vector3D worldpos, IMyEntity ent)
		{

			if(Planet == null)
				Planet = MyGamePruningStructure.GetClosestPlanet(worldpos);
			return GetWeatherAtPoint(Planet, worldpos, 0, ent) * ( GetDrag(ent)?.CheckOcclusion() ?? 1d );
		}
		private void SendModSettings()
		{
			var ModSettings = new MyTuple<Func<int, object>, Func<MyPlanet, Vector3D, IMyEntity, Vector3D>>(GetSetting, GetWindAtPoint);
			var EntitySettings = new MyTuple<Func<IMyEntity, BoundingBox?>, Func<IMyEntity, MyTuple<double, double, double, double, double, double>?>>(GetDragBox, GetEntityHeat);
				 //Dictionary<int, bool> DictionarySettings = Pref.GetAsDictionary(settings.GetAsDictionary());
				 MyAPIGateway.Utilities.SendModMessage(MODID, ModSettings);
		}

		private MyTuple<double, double, double, double, double, double>? GetEntityHeat(IMyEntity arg)
		{
			DragBase get;
			if (!dragDictionary.TryGetValue(arg, out get))
			{
				return null;
			}
			return get.GetHeat();
		}

		private BoundingBox? GetDragBox(IMyEntity arg)
		{
			DragBase get;
			if(!dragDictionary.TryGetValue(arg, out get))
            {
				return null;
			}
			return get.GetBoundingBox();
		}

		private const int GETSETTINGS = 1;
		private void ModCommunication(object obj)
		{
			if (!init)
				return; //??
			int msgType = 0;
			if (obj is Dictionary<int, bool>)
				return;
			if(obj is int)
			{
				msgType = (int)obj;
				switch (msgType)
				{
					case GETSETTINGS:
							SendModSettings();
                        break;
					default:
						return;
				}
			}
		}

		private void Entities_OnEntityRemove(IMyEntity obj)
		{
            DragBase drag;
			if(dragDictionary.TryGetValue(obj, out drag))
			{
				drag.Close();
				dragDictionary.Remove(obj);
			}
			if (obj is MyPlanet)
			{
				if (planets.ContainsKey(obj.EntityId))
					planets.Remove(obj.EntityId);
				if(WindData.ContainsKey(obj.EntityId))
				{
					WindData.Remove(obj.EntityId);
				}
			}
		}

		private void Entities_OnEntityAdd(IMyEntity obj)
		{
			DragBase drag;
			if (dragDictionary.TryGetValue(obj, out drag))
			{
				dragDictionary.Remove(obj);
				drag.Close();
			}
			if (obj is IMyCubeGrid)
			{
				drag = GetOrRegisterNewSEDrag(obj);
				
			}
			if (obj is IMyCharacter)
			{
				drag = new CharacterDrag(obj);
			}
			if (obj is MyPlanet)
			{
				if (!planets.ContainsKey(obj.EntityId))
					planets.Add(obj.EntityId, obj as MyPlanet);
				if (!WindData.ContainsKey(obj.EntityId))
				{
					WindData.Add(obj.EntityId , new PlanetConfigData(obj, OnUpdateClients));
				}
			}
		}

		private SEDrag GetOrRegisterNewSEDrag(IMyEntity obj)
		{
			SEDrag dragobj;
			if (DragStack.Count == 0)
				dragobj = new SEDrag();
			else
				dragobj = DragStack.Pop();
			dragobj.Register(obj);
			return dragobj;
		}

		[ProtoContract]
		struct TickData
		{
			[ProtoMember(1)]
			public ulong PlayerId;
			[ProtoMember(2)]
			public long CurrTick;
			[ProtoMember(3)]
			public long Offset;
		}

		private void SendTickSync(long prevtick = 0, ulong playerid = 0, long prev_offset = 0)
		{
			TickData encode;
			if (isServer)
				encode.PlayerId = playerid;
			else
				encode.PlayerId = MyAPIGateway.Session.Player.SteamUserId;
			encode.CurrTick = tick;
			if (isServer)
			{
				encode.Offset = tick - prevtick;
				encode.Offset /= 2;
			}
			else
			{
				encode.Offset = prev_offset;
            }

			var Data = MyAPIGateway.Utilities.SerializeToBinary(encode);
			if(isServer)
			{
				MyAPIGateway.Multiplayer.SendMessageTo(TICK_SYNC, Data, playerid, true);
			}
			else
			{
				//MyAPIGateway.Multiplayer.SendMessageToServer(TICK_SYNC, Data, true);
			}

		}
		private void recieveTickSync(byte[] obj)
		{
			try
			{
				var data = MyAPIGateway.Utilities.SerializeFromBinary<TickData>(obj);
				if(isServer)
				{
					if(data.CurrTick + data.Offset != tick)
					{
						SendTickSync(data.CurrTick, data.PlayerId);
					}
				}
				else
				{
					tick = data.CurrTick + data.Offset;
					SendTickSync(tick, MyAPIGateway.Session.Player.SteamUserId, data.Offset);
				}
			}
			catch
			{

			}
		}

		private void recieveData(byte[] obj)
		{
			try
			{
				string str = new string(Encoding.UTF8.GetChars(obj));

				string[] words = str.Split(' ');
				int data = Convert.ToInt32(words[0]);
				int advlift = Convert.ToInt32(words[1]);

				instance.settings.mult = data;
				int heat = Convert.ToInt32(words[2]);
				int dragMult = Convert.ToInt32(words[3]);
				int digi = Convert.ToInt32(words[4]);
				int wind = 0;
				if(words.Length > 5)
					wind = Convert.ToInt32(words[5]);
				_recievedGameSettings = true;
				instance.settings.radMult = dragMult;
				instance.settings.advancedlift = (advlift == 1 ? true : false);
				instance.settings.heat = (heat == 1 ? true : false);
				instance.settings.digi = (digi == 1 ? true : false);
				instance.settings.SimulateWind = (wind == 1 ? true : false);
				
				if(words.Length > 6)
				{
					int newseed = 0;
					int.TryParse(words[6], out newseed);
					if(newseed != instance.seed || WGenerator == null)
					{
						instance.seed = newseed;
						WGenerator = new WeatherMapGenerator(newseed);
					}

				}
				if (words.Length > 7)
				{
					bool chardrag;
					if(bool.TryParse(words[7], out chardrag))
					{
						instance.settings.EnableCharacterDrag = chardrag;
					}

				}
				if (words.Length > 8)
				{
					bool occlusion;
					if (bool.TryParse(words[8], out occlusion))
					{
						instance.settings.EnableOcclusion = occlusion;
					}

				}
				if (words.Length > 9)
				{
					int Deflection;
					if (int.TryParse(words[9], out Deflection))
					{
						instance.settings.DeflectionMult = Deflection;
					}
				}
				if (words.Length > 10)
				{
					int AtmoMinimum;
					if (int.TryParse(words[10], out AtmoMinimum))
					{
						instance.settings.AtmosphericMinimum = AtmoMinimum;
					}
				}
				if (words.Length > 11)
				{
					int RotationalDragMult;
					if (int.TryParse(words[11], out RotationalDragMult))
					{
						instance.settings.RotationalDragMultiplier = RotationalDragMult;
					}
				}
				//SendModSettings();
			}
			catch(Exception)
			{
			}
		}

		private void recieveHello(byte[] obj)
		{
			try
			{
				ulong steamid = Convert.ToUInt64(new string(Encoding.UTF8.GetChars(obj)));
				SendModData(steamid);
				SendTickSync(tick, steamid, 0);//start the syncing of ticks. 
				SendPlanetDataToClient(steamid);
			}
			catch(Exception)
			{


			}

		}
		private void SendModData(ulong steamId)
		{

			string settingstr = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", 
				instance.settings.mult.ToString(), (instance.settings.advancedlift == true ? "1" : "0"),
				(instance.settings.heat == true ? "1" : "0"), instance.settings.radMult.ToString(),
				(instance.settings.digi == true ? "1" : "0"),
				instance.settings.SimulateWind == true ? "1" : "0",
				instance.seed.ToString(),
				instance.settings.EnableCharacterDrag == true ? "1" : "0",
				instance.settings.EnableOcclusion == true ? "1" : "0",
				instance.settings.DeflectionMult, 
				instance.settings.AtmosphericMinimum,
				instance.settings.RotationalDragMultiplier
				);
			if(isDedicated)
				MyLog.Default.WriteLineToConsole("Sending mod settings to client: " + settingstr);
			MyAPIGateway.Multiplayer.SendMessageTo(RESPONSE_MSG, Encoding.UTF8.GetBytes(settingstr), steamId, true);


        }
		
		private void OnUpdateModData()
		{
			if (!isServer)
				return;
			List<IMyPlayer> players = new List<IMyPlayer>();
			//call when mod data is changed.
			MyAPIGateway.Players.GetPlayers(players); 
			foreach (var player in players)
			{ 
				if(!player.IsBot && (!isDedicated && player.SteamUserId != Session.Player.SteamUserId))
					SendModData(player.SteamUserId);
			}
		}

		private void sendHello()
		{
			sentHello = true;
            MyAPIGateway.Multiplayer.SendMessageToServer(HELLO_MSG, Encoding.UTF8.GetBytes(MyAPIGateway.Session.Player.SteamUserId.ToString()) , true);
        }

		private void MessageEntered(string msg, ref bool visible)
		{

			msg = msg.ToLower();
			if (msg.Equals("/drag", StringComparison.InvariantCultureIgnoreCase))
			{
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Valid Server Commands /drag-get /drag-center /drag-reset /drag-save /drag-load /drag-mult /drag-deflection /drag-advlift /drag-savedefault /drag-loaddefault");
				visible = false;
			}

			if (!msg.StartsWith("/drag-", StringComparison.InvariantCultureIgnoreCase))
				return;
			if (msg.StartsWith("/drag-get", StringComparison.InvariantCultureIgnoreCase))
			{
				visible = false;
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Drag multiplier is {0}. AdvLift: {1} Heat: {2}", instance.settings.mult.ToString(), instance.settings.advancedlift.ToString(), instance.settings.heat.ToString()));
				return;
			}

			if (msg.StartsWith("/drag-warning", StringComparison.InvariantCultureIgnoreCase))
			{
				visible = false;
				Pref.Show_Warning = !Pref.Show_Warning;
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Warning: {0}", (Pref.Show_Warning ? "on" : "off")));
				
				return;
			}
			if (msg.StartsWith("/drag-center", StringComparison.InvariantCultureIgnoreCase))
			{
				visible = false;
				string[] words = msg.Split(' ');
				if (words.Length > 1)
				{
					switch (words[1])
					{
						case "on":
							Pref.CoLSetting = CenterOfLift.On;

							break;
						case "auto":
							Pref.CoLSetting = CenterOfLift.Auto;

							break;
						case "off":
							Pref.CoLSetting = CenterOfLift.Off;

							break;
					}
					
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Center of lift marker set to {0}", Pref.CoLSetting.ToString()));
					return;

				}
				
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("/drag-center [on/off/auto]"));
                return;
			}
			if (msg.StartsWith("/drag-effect", StringComparison.InvariantCultureIgnoreCase))
			{
				visible = false;
				string[] words = msg.Split(' ');
				if (words.Length > 1)
				{
					switch (words[1])
					{
						case "on":
							Pref.Smoke = true;
							Pref.Burn = true;
							break;
						case "smoke":
							Pref.Smoke= !Pref.Smoke;
							break;
						case "burn":
							Pref.Burn = !Pref.Burn;
							break;
						case "off":
							Pref.Smoke = false;
							Pref.Burn = false;
							break;
					}
					UpdateServerSettings();
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Effects: Burn - {0} and Smoke - {1}", (Pref.Burn ? "on" : "off"), (Pref.Smoke ? "on" : "off")));
					return;
				}
				MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Command: /drag-effect [on/off/burn/smoke]");
				return;

			}
			if (isServer)
			{

				visible = false;
				if (msg.StartsWith("/drag-advlift", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1 )
					{
						if (words[1] == "on")
						{
							instance.settings.advancedlift = true;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Advanced Lift Simulation Enabled");
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Use /drag-center auto to see Center of Lift/Center of Mass markers.");
							OnUpdateModData();
							SendModSettings();
							UpdateServerSettings();
							return;
						}
						else if (words[1] == "off")
						{
							instance.settings.advancedlift = false;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Advanced Lift Simulation Disabled");
							OnUpdateModData();
							SendModSettings();
							UpdateServerSettings();
							return;
						}

					}

					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Command: /drag-advlift [on/off]");
					return;

				}
				if (msg.StartsWith("/drag-digi", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1)
					{
						if (words[1] == "on")
						{
							instance.settings.digi = true;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Digi Wing Physics Enabled (Warning, digi's wing physics can cause issues with certain settings.)");
							OnUpdateModData();
							SendModSettings();
							UpdateServerSettings();
							return;
						}
						else if (words[1] == "off")
						{
							instance.settings.digi = false;
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Digi Wing Physics Disabled.");
							OnUpdateModData();
							SendModSettings();
							UpdateServerSettings();
							return;
						}

					}

					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Command: /drag-digi [on/off]");
					return;

				}
				if (msg.StartsWith("/drag-heat", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1)
					{
						if (words[1] == "on")
						{

							instance.settings.heat = true;
							OnUpdateModData();
							UpdateServerSettings();
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Heat Damage Enabled");
							return;
						}
						else if (words[1] == "off")
						{
							instance.settings.heat = false;
							OnUpdateModData();
							UpdateServerSettings();
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
				if (msg.StartsWith("/drag-savedefault", StringComparison.InvariantCultureIgnoreCase))
				{
					saveXMLDefault();
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "saved new defaults");
					return;
				}
				if (msg.StartsWith("/drag-template", StringComparison.InvariantCultureIgnoreCase))
				{
					h_definitions.Save();
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "template saved");
					return;
				}
				if (msg.StartsWith("/drag-load", StringComparison.InvariantCultureIgnoreCase))
				{
					loadXML();
					OnUpdateModData();
					SendModSettings();
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "loaded");
					return;
				}
				if (msg.StartsWith("/drag-loaddefault", StringComparison.InvariantCultureIgnoreCase))
				{
					loadXML(true);
					OnUpdateModData();
					SendModSettings();
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "loaded default settings");
					return;
				}
				if (msg.StartsWith("/drag-reset", StringComparison.InvariantCultureIgnoreCase))
				{
					ResetSettings();
					OnUpdateModData();
					SendModSettings();
					MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Reset mod settings to default");
					return;
				}
				if (msg.StartsWith("/drag-mult", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1)
					{
						try
						{
							instance.settings.mult = Convert.ToInt32(words[1]);
							OnUpdateModData();
							UpdateServerSettings();
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Drag multiplier set to {0}", instance.settings.mult.ToString()));
						}
						catch (FormatException)
						{
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Value must be a number!"));
						}

					}
					else
					{
						MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Command: /drag-mult [#]"));
					}
					return;
				}
				if (msg.StartsWith("/drag-deflection", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1)
					{
						try
						{
							instance.settings.DeflectionMult = Convert.ToInt32(words[1]);
							OnUpdateModData();
							UpdateServerSettings();
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Deflection multiplier set to {0}", instance.settings.DeflectionMult.ToString()));
						}
						catch (FormatException)
						{
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Value must be a number!"));
						}

					}
					else
					{
						MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Command: /drag-deflection [#]"));
					}
					return;
				}
				if (msg.StartsWith("/drag-radMult", StringComparison.InvariantCultureIgnoreCase))
				{
					string[] words = msg.Split(' ');
					if (words.Length > 1)
					{
						try
						{
							instance.settings.radMult = Convert.ToInt32(words[1]);
							UpdateServerSettings();
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Heat Radiation Multiplier set to {0}", instance.settings.radMult.ToString()));
						}
						catch (FormatException)
						{
							MyAPIGateway.Utilities.ShowMessage(MOD_NAME, string.Format("Value must be a number!"));
						}
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

			var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(FILE, typeof(DragSettings));
			writer.Write(MyAPIGateway.Utilities.SerializeToXML(instance.settings));

			writer.Flush();
			writer.Close();

		}

		/// <summary>
		/// Write a new default file
		/// </summary>
		public void saveXMLDefault()
		{
		
			var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FILE, typeof(DragSettings));

			writer.Write(MyAPIGateway.Utilities.SerializeToXML(instance.settings));

			writer.Flush();
			writer.Close();

		}
		public void loadXML(bool l_default = false)
		{
			bool loaded = false;
			try
			{
				if (MyAPIGateway.Utilities.FileExistsInWorldStorage(FILE, typeof(DragSettings)) && !l_default)
				{
					var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(FILE, typeof(DragSettings));
					var xmlText = reader.ReadToEnd();
					reader.Close();
					instance.settings = MyAPIGateway.Utilities.SerializeFromXML<DragSettings>(xmlText);
					loaded = true;
				}
				//if it cant find individual world settings it will load the default file if it exists. 
				if (!loaded && MyAPIGateway.Utilities.FileExistsInLocalStorage(FILE, typeof(DragSettings)))
				{
					var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(FILE, typeof(DragSettings));
					var xmlText = reader.ReadToEnd();
					reader.Close();
					instance.settings = MyAPIGateway.Utilities.SerializeFromXML<DragSettings>(xmlText);
					loaded = true;
				}
				if(isDedicated)
				{
					MyLog.Default.WriteLineAndConsole("(Drag) Loaded settings: ");
					MyLog.Default.WriteLineAndConsole("  - multi:  " + instance.settings.mult.ToString());
					MyLog.Default.WriteLineAndConsole("  - digi:  " + instance.settings.digi.ToString());
					MyLog.Default.WriteLineAndConsole("  - heat:  " + instance.settings.heat.ToString());
					MyLog.Default.WriteLineAndConsole("  - DeflectionMult:  " + instance.settings.DeflectionMult.ToString());
					MyLog.Default.WriteLineAndConsole("  - advancedlift:  " + instance.settings.advancedlift.ToString());
					MyLog.Default.WriteLineAndConsole("  - AtmosphericMinimum:  " + instance.settings.AtmosphericMinimum.ToString());
					MyLog.Default.WriteLineAndConsole("  - EnableOcclusion:  " + instance.settings.EnableOcclusion.ToString());
					MyLog.Default.WriteLineAndConsole("  - EnableCharacterDrag:  " + instance.settings.EnableCharacterDrag.ToString());
					MyLog.Default.WriteLineAndConsole("  - SimulateWind:  " + instance.settings.SimulateWind.ToString());
					MyLog.Default.WriteLineAndConsole("  - radMult:  " + instance.settings.radMult.ToString());
					MyLog.Default.WriteLineAndConsole("  - RotationalDragMultiplier:  " + instance.settings.RotationalDragMultiplier.ToString());
				}
				UpdateServerSettings();
				return;

			}
			catch (Exception ex )
			{
				if (isDedicated)
				{
					MyLog.Default.WriteLineAndConsole("(Drag) Error loading Settings:");
					MyLog.Default.WriteLineAndConsole(ex.Message);
				}

			}
			instance.settings = new DragSettings();

		}

		private void ResetSettings()
		{
			if (instance != null)
				instance.settings = new DragSettings();
			UpdateServerSettings();
		}
		private static string MakeValidFileName(string name)
		{
			string invalidChars = "[^a-zA-Z0-9 \\- _]";
			return Regex.Replace(name, invalidChars, "_");
		}
		public override void UpdateAfterSimulation()
		{
			Update();
		}

		Stopwatch timer = new Stopwatch();
		
		public override void Draw()
		{
			timer.Restart();
			if(!isDedicated)
			{
				if (DrawHook != null)
					DrawHook();
				if(DebugMessage != null )
				{
					if(Pref.DebugText)
					{
						DebugMessage.Visible = true;
						DebugMessage.Message.Clear();
						DebugMessage.Message.AppendFormat("<color={0},{1},{2},{3}>", Color.Yellow.R, Color.Yellow.G, Color.Yellow.B, Color.Yellow.A);
						DebugMessage.Message.Append("Drag update time: ");
						DebugMessage.Message.AppendFormat("{0:N2} ms\n", UpdateTime.TotalMilliseconds);
						DebugMessage.Message.Append("Draw time:");
						DebugMessage.Message.AppendFormat("{0:N2} ms\n", DrawTime.TotalMilliseconds);
						DebugMessage.Message.Append("Objects: ");
						DebugMessage.Message.AppendFormat("{0:N0} Drag Entities\n", dragDictionary.Count);
					}
					else
					{
						DebugMessage.Visible = false;
					}
				}
				
			}
			timer.Stop();
			DrawTime = timer.Elapsed;

		}


		private void Update()
		{
			timer.Restart();
			if (!init)
			{
				//if(instance == null) instance = this;
				if (MyAPIGateway.Session == null)
					return;
				if (MyAPIGateway.Multiplayer == null && MyAPIGateway.Session.OnlineMode != MyOnlineModeEnum.OFFLINE)
					return;
				Init();
			}
			if (MyAPIGateway.Session == null)
			{
				unload();
				return;
			}
			
			updatedplanet = false;

            if (UpdateHook != null)
			{
				
				UpdateHook();
			}

			
			if (TaskManager != null)
				TaskManager.Update();

			DigiSettings.Update();
			if(!isDedicated)
				UpdateCenterOfLift();
			tick++;
			//MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed = 1000f;
            if (MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed > 100f)
				small_max = MyDefinitionManager.Static.EnvironmentDefinition.SmallShipMaxSpeed;
			if (MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed > 100f)
				large_max = MyDefinitionManager.Static.EnvironmentDefinition.LargeShipMaxSpeed;
			if (MyAPIGateway.Session.Player != null)
				if (!sentHello)
					sendHello();
			timer.Stop();
			UpdateTime = timer.Elapsed;
		}

		#region Interactive Menu

		StringBuilder WindPreset = new StringBuilder("Wind Speed: ");
		StringBuilder WindChangeRatePreset = new StringBuilder("Wind Change Rate: ");

		HudAPIv2.MenuItem PlanetName, RefreshLine;
		HudAPIv2.MenuTextInput SpeedSetterLine, SpeedChangeRateSetterLine;

		HudAPIv2.MenuRootCategory ClientMenu, AdminMenu;

		HudAPIv2.MenuSubCategory ServerSettings, PlanetSettings;

		HudAPIv2.MenuItem ParticleEffectLine, LightingEffectLine, DebugTextLine, ShowWarningLine, CenterLine, ShowHeatIndicator, ShowWindIndicator, ResetGUI;
		HudAPIv2.MenuSubCategory GUIMenu, DebugGui, HeatIndicatorGui, HeatAlarmGui, WindIndicatorGui;
		HudAPIv2.MenuScreenInput DebugGuiPosition, HeatIndicatorGuiPosition, HeatAlarmGuiPosition, WindIndicatorGuiPosition;

		HudAPIv2.MenuSubCategory Physics, HeatSettings, ModCompatablity;

		HudAPIv2.MenuItem AdvLiftLine, SimulateWindLine, EnableOcclusionLine, EnableCharacterDragLine;
		HudAPIv2.MenuTextInput DragMultiplierLine, DeflectionMultiplierLine, AtmosphericMinimumLine, RotationalDragLine;

		HudAPIv2.MenuItem BurnLine;
		HudAPIv2.MenuTextInput HeatDissipationMultiplierLine;

		HudAPIv2.MenuItem DigiPhysicsLine;

		HudAPIv2.MenuItem SaveLine, SaveAsDefaultLine, LoadLine, LoadDefaultsLine, ResetSettingsLine;


		private void InitMenu()
		{
			ClientMenu = new HudAPIv2.MenuRootCategory("Aerodynamic Physics", HudAPIv2.MenuRootCategory.MenuFlag.PlayerMenu, "Aerodynamic Physics Preferences");
			AdminMenu = new HudAPIv2.MenuRootCategory("Aerodynamic Physics", HudAPIv2.MenuRootCategory.MenuFlag.AdminMenu, "Aerodynamic Physics Admin Menu");
			PlanetSettings = new HudAPIv2.MenuSubCategory("Planet Settings", AdminMenu, "Current Planet Settings");
			ServerSettings = new HudAPIv2.MenuSubCategory("Server Settings", AdminMenu, "Server Settings");

			#region  Preferences

			ParticleEffectLine = new HudAPIv2.MenuItem("Particle Effects: ", ClientMenu, ChangeParticleEffect);
			LightingEffectLine = new HudAPIv2.MenuItem("Lighting Effects: ", ClientMenu, ChangeLightEffect);
			CenterLine = new HudAPIv2.MenuItem("Center of Lift Gizmo: ", ClientMenu, ChangeCenterLine);

			GUIMenu = new HudAPIv2.MenuSubCategory("Gui Options", ClientMenu, "Gui Options");

			DebugGui = new HudAPIv2.MenuSubCategory("Debug", GUIMenu, "Debug GUI");
			DebugTextLine = new HudAPIv2.MenuItem("Show Debug Text: ", DebugGui, DebugTextLineOnClick);
			DebugGuiPosition = new HudAPIv2.MenuScreenInput("Debug GUI Position", DebugGui, DebugMessage.Origin, DebugMessage.GetTextLength(), "", DebugGuiSubmit, DebugGuiUpdate, DebugGuiCancel, DebugGUIUpdateSize);

			HeatAlarmGui = new HudAPIv2.MenuSubCategory("Heat Alarm", GUIMenu, "Debug GUI");
			ShowWarningLine = new HudAPIv2.MenuItem("Show Warning: ", HeatAlarmGui, ChangeShowWarningLine);
			HeatAlarmGuiPosition = new HudAPIv2.MenuScreenInput("Heat Alarm GUI Position", HeatAlarmGui, HeatAlarmMsg.Origin, HudAPIv2.APIinfo.ScreenPositionOnePX * 50, "Heat Alarm Message", HeatAlarmSubmit);

			HeatIndicatorGui = new HudAPIv2.MenuSubCategory("Heat Indicator", GUIMenu, "Heat Indicator GUI");
			ShowHeatIndicator = new HudAPIv2.MenuItem("Show Heat Indicator: ", HeatIndicatorGui, ChangeShowHeatIndicatorLine);
			var ln = HeatMsg.GetTextLength();
			ln.X *= -1;
			if(ln.X >= HudAPIv2.APIinfo.ScreenPositionOnePX.X * -200)
			{
				ln.X = HudAPIv2.APIinfo.ScreenPositionOnePX.X * -200;
            }
            HeatIndicatorGuiPosition = new HudAPIv2.MenuScreenInput("Heat Indicator Position", HeatIndicatorGui, HeatMsg.Origin, ln, "", HeatInidcatorOnSubmit, HeatIndicatorOnUpdate, HeatIndicatorOnCancel);

			WindIndicatorGui = new HudAPIv2.MenuSubCategory("Wind Indicator", GUIMenu, "Wind Indicator GUI");
			ShowWindIndicator = new HudAPIv2.MenuItem("Show Wind Indicator: ", WindIndicatorGui, ChangeShowWindIndicatorLine);
			WindIndicatorGuiPosition = new HudAPIv2.MenuScreenInput("Wind Indicator Position", WindIndicatorGui, WindDisplay.Origin, WindDisplay.Size, "", WindIndciatorOnSubmit, WindIndicatorOnUpdate, WindIndicatorOnCancel);

			ResetGUI = new HudAPIv2.MenuItem("Reset GUI", GUIMenu, ResetGuiOnClick);

			UpdatePreferencesMenu();
			
			#endregion

			#region ServerSettings
			Physics = new HudAPIv2.MenuSubCategory("Physics", ServerSettings, "Physics");
			HeatSettings = new HudAPIv2.MenuSubCategory("Heat Settings", ServerSettings, "Heat Settings");
			ModCompatablity = new HudAPIv2.MenuSubCategory("Compatability", ServerSettings, "Compatability");

			AdvLiftLine = new HudAPIv2.MenuItem("", Physics, AdvLiftOnClick);
			DragMultiplierLine = new HudAPIv2.MenuTextInput("", Physics, "Drag Multiplier (Default 500, 100 is realistic)", SetDragMultiplier);
			DeflectionMultiplierLine = new HudAPIv2.MenuTextInput("", Physics, "Deflection Multiplier (Default 100)", SetDeflectionMultiplier);
			RotationalDragLine = new HudAPIv2.MenuTextInput("", Physics, "Rotational Drag (100 is default)", SetRotationalDrag);
			AtmosphericMinimumLine = new HudAPIv2.MenuTextInput("", Physics, "Atmospheric Minimum - Values above 0 apply drag in space", SetAtmosphericMinimum);

			EnableCharacterDragLine = new HudAPIv2.MenuItem("", Physics, EnableCharacterDragOnClick);
			SimulateWindLine = new HudAPIv2.MenuItem("", Physics, SimulateWindOnClick);
			EnableOcclusionLine = new HudAPIv2.MenuItem("", Physics, EnableOcclusionLineOnClick);


			BurnLine = new HudAPIv2.MenuItem("", HeatSettings, BurnOnClick);
			HeatDissipationMultiplierLine = new HudAPIv2.MenuTextInput("", HeatSettings, "Heat Dissipation Multiplier", SetDissipation);

			DigiPhysicsLine = new HudAPIv2.MenuItem("", ModCompatablity, DigiPhysicsOnClick);

			UpdateServerSettings();
			#endregion
			
			#region planetmenu

			PlanetName = new HudAPIv2.MenuItem("Planet:", PlanetSettings, null, false);
			SpeedSetterLine = new HudAPIv2.MenuTextInput("Planet:", PlanetSettings, "Peak Wind Speed", SetWindSpeed);
			SpeedChangeRateSetterLine = new HudAPIv2.MenuTextInput("Planet:", PlanetSettings, "Wind speed change rate", SetWindChangeRate);
			RefreshLine = new HudAPIv2.MenuItem("Get Closest Planet", PlanetSettings, RefreshClosestPlanet);
			RefreshClosestPlanet();

			#endregion

			SaveLine = new HudAPIv2.MenuItem("Save", AdminMenu, SaveLineClick);
			LoadLine = new HudAPIv2.MenuItem("Load", AdminMenu, LoadLineClick);
			SaveAsDefaultLine = new HudAPIv2.MenuItem("Save As Default", AdminMenu, SaveAsDefaultLineClick);
			LoadDefaultsLine = new HudAPIv2.MenuItem("Load Default", AdminMenu, LoadDefaultsLineClick);
			ResetSettingsLine = new HudAPIv2.MenuItem("Reset Settings", AdminMenu, ResetSettingsClick);

		}

		#region Server Settings Methods
		public void UpdateServerSettings()
		{
			EnableOcclusionLineOnVisible();
			CharacterDragLineOnVisible();
			AdvLiftOnVisible();
			HeatDissipationUpdate();
			UpdateSimulateWind();
			DragMultplierUpdate();
			AtmosphericMinimumUpdate();
			RotationalDragUpdate();
			DeflectionMultplierUpdate();
			BurnOnVisible();
			DigiPhysicsUpdate();
        }

		private void EnableOcclusionLineOnVisible()
		{
			if (EnableOcclusionLine == null)
				return;
			EnableOcclusionLine.Text = string.Format("Enable Occlusion: {0}", instance.settings.EnableOcclusion);
		}
		private void EnableOcclusionLineOnClick()
		{
			if(isServer)
			{
				instance.settings.EnableOcclusion = !instance.settings.EnableOcclusion;
				OnUpdateModData();
				SendModSettings();
				EnableOcclusionLineOnVisible();
			}
		}
        private void EnableCharacterDragOnClick()
		{
			if(isServer)
			{
				instance.settings.EnableCharacterDrag = !instance.settings.EnableCharacterDrag;
				OnUpdateModData();
				SendModSettings();
				CharacterDragLineOnVisible();
            }
		}

		private void CharacterDragLineOnVisible()
		{
			if (EnableCharacterDragLine == null)
				return;
			EnableCharacterDragLine.Text = string.Format("Enable Character Drag: {0}", instance.settings.EnableCharacterDrag);
		}

		private void AdvLiftOnClick()
		{
			if(isServer)
			{
				instance.settings.advancedlift = !instance.settings.advancedlift;
				OnUpdateModData();
				SendModSettings();
				AdvLiftOnVisible();
			}

		}

		private void AdvLiftOnVisible()
		{
			if (AdvLiftLine == null)
				return;
			AdvLiftLine.Text = string.Format("Advanced Lift: {0}", instance.settings.advancedlift);
		}



		private void SimulateWindOnClick()
		{
			if (isServer)
			{
				instance.settings.SimulateWind = !instance.settings.SimulateWind;
				OnUpdateModData();
				SendModSettings();
				UpdateSimulateWind();
			}
		}

		private void UpdateSimulateWind()
		{
			if (SimulateWindLine == null)
				return;
			SimulateWindLine.Text = string.Format("Simulate Wind: {0}", instance.settings.SimulateWind);
		}


		private void SetDissipation(string obj)
		{
			if(isServer)
			{
				int getter;
				if(int.TryParse(obj, out getter))
				{
					instance.settings.radMult = getter;
					OnUpdateModData();
					SendModSettings();
					HeatDissipationUpdate();
				}

			}
		}

		private void HeatDissipationUpdate()
		{
			if (HeatDissipationMultiplierLine == null)
				return;
			HeatDissipationMultiplierLine.Text = string.Format("Heat Radiation Multiplier: {0}", instance.settings.radMult.ToString());
		}

		private void SetDragMultiplier(string obj)
		{
			if (isServer)
			{
				int getter;
				if (int.TryParse(obj, out getter))
				{
					instance.settings.mult = getter;
					OnUpdateModData();
					SendModSettings();
					DragMultplierUpdate();
				}
			}
		}
		private void DragMultplierUpdate()
		{
			if (DragMultiplierLine == null)
				return;
			DragMultiplierLine.Text = string.Format("Drag Multiplier: {0}", instance.settings.mult.ToString());
		}


		private void SetAtmosphericMinimum(string obj)
		{
			if (isServer)
			{
				int getter;
				if (int.TryParse(obj, out getter))
				{
					instance.settings.AtmosphericMinimum = getter;
					OnUpdateModData();
					SendModSettings();
					AtmosphericMinimumUpdate();
				}

			}
		}
		private void AtmosphericMinimumUpdate()
		{
			if (DragMultiplierLine == null)
				return;
			AtmosphericMinimumLine.Text = string.Format("Atmospheric Minimum: {0}", instance.settings.AtmosphericMinimum.ToString());
		}

		private void SetRotationalDrag(string obj)
		{
			if (isServer)
			{
				int getter;
				if (int.TryParse(obj, out getter))
				{
					instance.settings.RotationalDragMultiplier = getter;
					OnUpdateModData();
					SendModSettings();
					RotationalDragUpdate();
				}

			}
		}
		private void RotationalDragUpdate()
		{
			if (DragMultiplierLine == null)
				return;
			RotationalDragLine.Text = string.Format("Rotational Drag Multiplier: {0}", instance.settings.RotationalDragMultiplier.ToString());
		}


		private void SetDeflectionMultiplier(string obj)
		{
			if (isServer)
			{
				int getter;
				if (int.TryParse(obj, out getter))
				{
					instance.settings.DeflectionMult = getter;
					OnUpdateModData();
					SendModSettings();
					DeflectionMultplierUpdate();
				}

			}
		}
		private void DeflectionMultplierUpdate()
		{
			if (DragMultiplierLine == null)
				return;
			DeflectionMultiplierLine.Text = string.Format("Deflection Multiplier: {0}", instance.settings.DeflectionMult.ToString());

		}

		private void BurnOnClick()
		{
			if (isServer)
			{
				instance.settings.heat = !instance.settings.heat;
				OnUpdateModData();
				SendModSettings();
				BurnOnVisible();
			}
		}

		private void BurnOnVisible()
		{
			if (BurnLine == null)
				return;
			BurnLine.Text = "Heat Damage: " + (instance.settings.heat ? "Enabled" : "Disabled");
		}

		private void DigiPhysicsOnClick()
		{
			if (isServer)
			{
				instance.settings.digi = !instance.settings.digi;
				OnUpdateModData();
				SendModSettings();
				DigiPhysicsUpdate();
			}
		}

		private void DigiPhysicsUpdate()
		{
			if (DigiPhysicsLine == null)
				return;
			DigiPhysicsLine.Text = "Digi Physics: " + (instance.settings.digi ? "Enabled (may cause unintended effects)" : "Disabled");
		}
		#endregion

		#region Client Preference Methods

		private void ResetGuiOnClick()
		{
			Pref.DebugPosition = new Vector2D(-0.7, 1);
			DebugMessage.Origin = Pref.DebugPosition;
			DebugGuiPosition.Origin = Pref.DebugPosition;

			Pref.HeatAlarmGuiPosition = new Vector2D(0, -0.2);
			HeatAlarmMsg.Origin = Pref.HeatAlarmGuiPosition;
			HeatAlarmGuiPosition.Origin = Pref.HeatAlarmGuiPosition;

			Pref.WindIndicatorPosition = new Vector2D(0.2, -0.65);
			WindDisplay.Origin = Pref.WindIndicatorPosition;
			WindIndicatorGuiPosition.Origin = Pref.WindIndicatorPosition;

			Pref.HeatIndicatorPosition = new Vector2D(1d, 0.8d);
			HudHeatWarningPos = Pref.HeatIndicatorPosition;
			HeatIndicatorGuiPosition.Origin = Pref.HeatIndicatorPosition;
			//HeatMsgBackground = Pref.HeatIndicatorPosition;

		}
		private void DebugGUIUpdateSize()
		{
			DebugGuiPosition.Size = DebugMessage.GetTextLength();
		}

		private void DebugGuiCancel()
		{
			DebugMessage.Origin = DebugGuiPosition.Origin;
		}

		private void DebugGuiUpdate(Vector2D obj)
		{
			DebugMessage.Origin = obj;
		}

		private void DebugGuiSubmit(Vector2D obj)
		{
			Pref.DebugPosition = obj;
			DebugMessage.Origin = obj;
			DebugGuiPosition.Origin = obj;
		}

		private void HeatAlarmSubmit(Vector2D obj)
		{
			Pref.HeatAlarmGuiPosition = obj;
			HeatAlarmMsg.Origin = obj;
			HeatAlarmGuiPosition.Origin = obj;
		}



		private void WindIndicatorOnCancel()
		{
			WindDisplay.Origin = WindIndicatorGuiPosition.Origin;
		}

		private void WindIndicatorOnUpdate(Vector2D obj)
		{
			WindDisplay.Origin = obj;
		}

		private void WindIndciatorOnSubmit(Vector2D obj)
		{
			Pref.WindIndicatorPosition = obj;
			WindDisplay.Origin = obj;
			WindIndicatorGuiPosition.Size = WindDisplay.Size;
			WindIndicatorGuiPosition.Origin = obj;
		}

		private void HeatIndicatorOnCancel()
		{
			HeatMsg.Origin = HeatIndicatorGuiPosition.Origin;
			HeatMsgBackground.Origin = HeatIndicatorGuiPosition.Origin;
		}

		private void HeatIndicatorOnUpdate(Vector2D obj)
		{
			HeatMsg.Origin = obj;
			HeatMsgBackground.Origin = obj;
		}

		private void HeatInidcatorOnSubmit(Vector2D obj)
		{
			Pref.HeatIndicatorPosition = obj;
			//HeatMsg.Origin = Pref.HeatIndicatorPosition;
			//HeatMsgBackground.Origin = Pref.HeatIndicatorPosition;
			HudHeatWarningPos = Pref.HeatIndicatorPosition;
            HeatIndicatorGuiPosition.Origin = obj;

		}

		private void ChangeShowWindIndicatorLine()
		{
			Pref.ShowWindIndicator = !Pref.ShowWindIndicator;
			
			UpdateShowWindIndicatorLine();
		}

		private void UpdateShowWindIndicatorLine()
		{
			ShowWindIndicator.Text = string.Format("Show Wind Indicator: {0}", Pref.ShowWindIndicator);
		}

		private void ChangeShowHeatIndicatorLine()
		{
			Pref.ShowHeatIndicator = !Pref.ShowHeatIndicator;
			UpdateShowHeatIndicatorLine();
		}

		private void UpdateShowHeatIndicatorLine()
		{
			ShowHeatIndicator.Text = string.Format("Show Heat Indicator: {0}", Pref.ShowHeatIndicator);
		}

		public void UpdatePreferencesMenu()
		{
			UpdateDebugTextLine();
			UpdateCoLLine();
			UpdateShowWarningLine();
			UpdateLightEffectLine();
			UpdateParticleEffectLine();
			UpdateShowWindIndicatorLine();
			UpdateShowHeatIndicatorLine();

		}

		StringBuilder m_CenterLineStringBuilderText = new StringBuilder();

		private void UpdateDebugTextLine()
		{
			if (DebugTextLine == null)
				return;
			DebugTextLine.Text = string.Format("Show Debug Text: {0}", Pref.DebugText);
		}

		private void DebugTextLineOnClick()
		{
			Pref.DebugText = !Pref.DebugText;
			if (DebugMessage != null)
				DebugMessage.Visible = Pref.DebugText;
			UpdateDebugTextLine();
		}

		private void ChangeCenterLine()
		{
			switch (Pref.CoLSetting)
			{
				case CenterOfLift.Auto:
					Pref.CoLSetting = CenterOfLift.Off;
					break;
				case CenterOfLift.Off:
					Pref.CoLSetting = CenterOfLift.On;
					break;
				case CenterOfLift.On:
					Pref.CoLSetting = CenterOfLift.Auto;
					break;
			}
			UpdateCoLLine();
		}


		private void UpdateCoLLine()
		{
			if (CenterLine != null)
			{

				m_CenterLineStringBuilderText.Clear();
				m_CenterLineStringBuilderText.Append("Center of Lift Gizmo: ");
				switch (Pref.CoLSetting)
				{
					case CenterOfLift.Auto:
						m_CenterLineStringBuilderText.Append("Auto");
						break;
					case CenterOfLift.On:
						m_CenterLineStringBuilderText.Append("Always On");
						break;
					case CenterOfLift.Off:
						m_CenterLineStringBuilderText.Append("Always Off");
						break;
				}
				CenterLine.Text = m_CenterLineStringBuilderText.ToString();
			}
		}

		private void ChangeShowWarningLine()
		{
			Pref.Show_Warning = !Pref.Show_Warning;
			UpdateShowWarningLine();
		}

		private void UpdateShowWarningLine()
		{
			if (ShowWarningLine != null)
			{
				ShowWarningLine.Text = string.Format("Show Heat Warning: {0}", Pref.Show_Warning.ToString());
			}
		}

		private void ChangeLightEffect()
		{
			Pref.Burn = !Pref.Burn;
			UpdateLightEffectLine();
		}

		private void ChangeParticleEffect()
		{
			Pref.Smoke = !Pref.Smoke;
			UpdateParticleEffectLine();
		}


		private void UpdateLightEffectLine()
		{
			if (LightingEffectLine != null)
			{
				LightingEffectLine.Text = string.Format("Lighting Effect: {0}", Pref.Burn.ToString());
			}
		}

		private void UpdateParticleEffectLine()
		{
			if(ParticleEffectLine != null)
			{
				ParticleEffectLine.Text = string.Format("Particle Effects: {0}", Pref.Smoke.ToString());
			}

		}
		#endregion

		#region SaveLoad
		private void ResetSettingsClick()
		{
			if (isServer)
			{
				ResetSettings();
				OnUpdateModData();
				SendModSettings();
				RefreshServerSettings();
			}

		}

		private void RefreshServerSettings()
		{
			UpdateServerSettings();
		}

		private void LoadDefaultsLineClick()
		{
			if (isServer)
			{
				loadXML(true);
				OnUpdateModData();
				SendModSettings();
				RefreshServerSettings();
			}
		}

		private void LoadLineClick()
		{
			if (isServer)
			{
				loadXML(false);
				OnUpdateModData();
				SendModSettings();
				RefreshServerSettings();
			}
		}

		private void SaveAsDefaultLineClick()
		{
			if (isServer)
			{
				saveXMLDefault();
				//OnUpdateModData();
				//SendModSettings();
			}
		}

		private void SaveLineClick()
		{
			if (isServer)
			{
				saveXML();
				//OnUpdateModData();
				//SendModSettings();
			}
		}
		#endregion

		#region Planets
		private void RefreshClosestPlanet()
		{
			UpdateClosePlanet();
			UpdatePlanetName();
			UpdateWindSpeed();
			UpdateWindChangeRate();
        }

		IMyEntity ClosestPlanet;
		double CloseSpeed = 0d;
		long CloseChangeRate = 60;


		public void UpdatePlanetName()
		{
			if(PlanetName != null)
				PlanetName.Text = "Planet Name: " + (ClosestPlanet?.Name != null ? ClosestPlanet.Name : "");
        }


		bool updatedplanet = false;
		public void UpdateClosePlanet()
		{
			if (updatedplanet)
				return;
			var pos = MyAPIGateway.Session?.Camera?.WorldMatrix.Translation;
			if (pos.HasValue)
			{
				ClosestPlanet = MyGamePruningStructure.GetClosestPlanet(pos.Value);
			}
			
        }


		public void UpdateWindChangeRate()
		{
			if (ClosestPlanet != null)
			{

				CloseChangeRate = GetPlanetChangeRate(ClosestPlanet);
				if (SpeedSetterLine != null)
				{
					SpeedChangeRateSetterLine.Text = WindChangeRatePreset.ToString() + string.Format("{0:N0}", CloseChangeRate);
				}

			}


		}


		public void SetWindChangeRate(string changerate)
		{
			if(isServer)
			{
				long result = 60;
				if (long.TryParse(changerate, out result))
				{

					SetClosePlanetChangeRate(result);
				}
				UpdateWindChangeRate();
			}

		}

		public void UpdateWindSpeed()
		{
			if(ClosestPlanet != null)
			{

				CloseSpeed = GetPlanetSpeed(ClosestPlanet);
				if (SpeedSetterLine != null)
				{
					SpeedSetterLine.Text = WindPreset.ToString() + string.Format("{0:N1}", CloseSpeed);
				}
			}


		}

		public void SetWindSpeed(string speed)
		{
			if(isServer)
			{
				float result = 0f;
				//MyAPIGateway.Utilities.ShowMessage("Entered", speed);
				if (float.TryParse(speed, out result))
				{

					SetClosePlanetSpeed(result);
				}
				UpdateWindSpeed();
			}


		}

		private void SetClosePlanetSpeed(double closeSpeed)
		{
			if (ClosestPlanet != null)
			{

				CloseSpeed = closeSpeed;
				PlanetConfigData val;
				if(WindData.TryGetValue(ClosestPlanet.EntityId, out val))
				{
					val.WindVelocity = CloseSpeed;
				}
				
			}
		}
		private void SetClosePlanetChangeRate(long changerate)
		{
			if (ClosestPlanet != null)
			{

				CloseChangeRate = changerate;
				PlanetConfigData val;
				if (WindData.TryGetValue(ClosestPlanet.EntityId, out val))
				{
					val.WindChangeRate = CloseChangeRate;
				}

			}
		}
		#endregion

		private double GetPlanetSpeed(IMyEntity Planet)
		{
			PlanetConfigData val;
			if (WindData.TryGetValue(Planet.EntityId, out val))
			{
				return val.WindVelocity;
			}
			return 0d;
		}

		private long GetPlanetChangeRate(IMyEntity Planet)
		{
			PlanetConfigData val;
			if (WindData.TryGetValue(Planet.EntityId, out val))
			{
				return val.WindChangeRate;
			}
			return 60;
		}
		#endregion

		private void InitTextHud()
		{
			TextHUD.OnScreenDimensionsChanged = RefreshGUI;
			HeatMsg = new HudAPIv2.HUDMessage();
			HeatMsg.Scale = 1.5;
			HeatMsg.Origin = HudHeatWarningPos;
			HeatMsg.Visible = false;
			HeatMsg.Blend = BlendTypeEnum.PostPP;
			HeatMsg.Options |= HudAPIv2.Options.HideHud;

			HeatAlarmMsg = new HudAPIv2.HUDMessage();
			HeatAlarmMsg.Scale = 1.5;
			HeatAlarmMsg.Origin = Pref.HeatAlarmGuiPosition;
			HeatAlarmMsg.Visible = false;
			HeatAlarmMsg.Blend = BlendTypeEnum.PostPP;
			HeatAlarmMsg.Options |= HudAPIv2.Options.HideHud;

			HeatMsgBackground = new HudAPIv2.BillBoardHUDMessage(MyStringId.GetOrCompute("Square"), HeatMsg.Origin, Color.Black * 0.5f);
			HeatMsgBackground.Visible = false;
			HeatMsgBackground.Blend = BlendTypeEnum.PostPP;
			HeatMsgBackground.Options |= HudAPIv2.Options.HideHud;

			DebugMessage = new HudAPIv2.HUDMessage();
			DebugMessage.Scale = 1.0;
			DebugMessage.Origin = Pref.DebugPosition;
			DebugMessage.Visible = Pref.DebugText;
			DebugMessage.Blend = BlendTypeEnum.PostPP;
			DebugMessage.Options |= HudAPIv2.Options.HideHud;


			InitMenu();
        }

		private void RefreshGUI()
		{
			if(WindDisplay != null)
			WindDisplay.MarkRefresh();
        }

		private void ResetHud()
		{
			UpdateBackground();
			DebugGuiPosition.Size = DebugMessage.GetTextLength();
			HeatAlarmGuiPosition.Size = HudAPIv2.APIinfo.ScreenPositionOnePX * 50;
        }


		public void HeatWarningVisible(bool visible)
		{
			if(HeatMsg != null)
			{
				HeatMsg.Visible = visible;
				HeatMsgBackground.Visible = visible;
			}
		}
		public void HeatAlarmVisible(bool visible)
		{
			if (HeatAlarmMsg != null)
			{
				HeatAlarmMsg.Visible = visible;
			}
		}
		public void UpdateHeatWarning(StringBuilder Message)
		{
			if(HeatMsg != null)
			{
				HeatMsg.Message = Message;

				UpdateBackground();
			}

		}
		public void UpdateHeatAlarm(StringBuilder Message)
		{
			HeatAlarmMsg.Message = Message;
		}
		public void UpdateBackground()
		{
			if(HeatMsgBackground != null)
			{
				HeatMsgBackground.Visible = HeatMsg.Visible;
				if(HeatMsg.Visible)
				{
					HeatMsgBackground.Visible = HeatMsg.Visible;
					var ln = HeatMsg.GetTextLength();
					
					HeatMsg.Origin = HudHeatWarningPos - new Vector2D(ln.X, 0);
                    HeatMsgBackground.Origin = HeatMsg.Origin;
					HeatMsgBackground.Offset = ln / 2;
					HeatMsgBackground.Width = (float)ln.X;
					HeatMsgBackground.Height = (float)ln.Y;
					if (HeatIndicatorGuiPosition != null)
					{
						ln.X = -ln.X;
						HeatIndicatorGuiPosition.Size = ln;
					}
                }

				
			}
		}
		public DragBase GetDrag(IMyEntity Ent)
		{
			if (Ent == null) return null;
			DragBase drag;
			
			if (dragDictionary.TryGetValue(Ent, out drag))
			{
				return drag;
			}
			return null;
		}
		private void UpdateCenterOfLift()
		{
			LastEntityIdForCenterOfLift = null;
			if (MyCubeBuilder.Static == null)
				return;
			if(MyCubeBuilder.Static.IsActivated)
			{
				LastEntityIdForCenterOfLift = MyCubeBuilder.Static?.FindClosestGrid()?.EntityId;
            }
        }
        internal void Register(IMyEntity entityId, DragBase sEDrag)
		{
			DragBase drag;
			if(dragDictionary.TryGetValue(entityId, out drag))
			{
				if (drag == sEDrag)
					return;
				//we have a collision, close one
				dragDictionary.Remove(entityId);
				drag.Close();
			}
			dragDictionary.Add(entityId, sEDrag);
		}

		internal void Unregister(IMyEntity ent, DragBase sEDrag)
		{
			DragBase drag;
			if(dragDictionary.TryGetValue(ent, out drag))
			{
				if(drag == sEDrag)
				{
					dragDictionary.Remove(ent);//remove
				}
			}
		}

		protected override void UnloadData()
		{
			DigiSettings.UnloadData();
			unload();
		}


		public void unload()
		{

			UpdateHook = null;
			if (CloseHook != null)
				CloseHook();
			CloseHook = null;
			DrawHook = null;

            MyAPIGateway.Utilities.MessageEntered -= MessageEntered;
			MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;
			MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
			init = false;
			isServer = false;
			isDedicated = false;
			settings = null;
			//All branch
			h_definitions.Close();
			if(!isDedicated)
			{
				if (TextHUD != null) TextHUD.Close();
			}
			if (_registerServer)
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(HELLO_MSG, recieveHello);
			if(_registerClient)
			{
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(RESPONSE_MSG, recieveData);
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(HEATDATA_MSG, recieveHeatData);
			}
			heatTransferCache.Clear();
			DragStack.Clear();
			DragStack = null;
			if(instance == this)
				instance = null;//die out. 

		}


		internal void UpdateClients(IMyEntity entity, GridHeatData heat)
		{
			if (isServer)
			{
				var player = MyAPIGateway.Players.GetPlayerControllingEntity(entity);
				if(player != null)
				{
					SendHeatData(entity.EntityId, heat, player.SteamUserId);
				}
			}
		}

		[ProtoContract]
		public struct HeatDataMessage
		{
			[ProtoMember(1)]
			public long Id;
			[ProtoMember(2)]
			public double Front;
			[ProtoMember(3)]
			public double Back;
			[ProtoMember(4)]
			public double Left;
			[ProtoMember(5)]
			public double Right;
			[ProtoMember(6)]
			public double Up;
			[ProtoMember(6)]
			public double Down;

			public HeatDataMessage(GridHeatData heat, long entityId) : this()
			{
				Front = heat.front;
				Back = heat.back;
				Left = heat.left;
				Right = heat.right;
				Up = heat.up;
				Down = heat.down;
				Id = entityId;

			}
		}

		private void SendHeatData(long entityId, GridHeatData heat, ulong steamUserId)
		{
			var data = new HeatDataMessage(heat, entityId);
			MyAPIGateway.Multiplayer.SendMessageTo(HEATDATA_MSG, MyAPIGateway.Utilities.SerializeToBinary(data), steamUserId, false);//unreliable new data is more important
		}

		private void recieveHeatData(byte[] obj)
		{
			if (isServer)
				return;//duh. 
			try
			{
				var message = MyAPIGateway.Utilities.SerializeFromBinary<HeatDataMessage>(obj);
				DragBase drag;
				IMyEntity ent;
				if (MyAPIGateway.Entities.TryGetEntityById(message.Id, out ent))
				{
					if (dragDictionary.TryGetValue(ent, out drag))
					{
						drag.WriteHeat(message);
					}
				}
			}
			catch
			{

			}

			
		}
	}
}