using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.Definitions;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SEDrag.Definition
{
	public class HeatDefinition
	{
		private Dictionary<string, HeatData> m_data = new Dictionary<string, HeatData>();
		private const string FILE = "heat.xml";
		public Dictionary<string, HeatData> data
		{
			get { return m_data; }
			set { m_data = value; }
		}
		public void Init()
		{
			//init
			var def = MyDefinitionManager.Static.GetAllDefinitions();
			Regex reg = new Regex("heat{(.*?)}");
			Regex regcom = new Regex(";");
			Regex regeq = new Regex("=");
			HeatData value;
			//Log.DebugWrite(DebugLevel.Info,"Writing heat defaults");
			data.Clear();
			#region defaults
			data.Add("LargeBlockBatteryBlock",					new HeatData(5.0, 750));
			data.Add("SmallBlockBatteryBlock",					new HeatData(5.0, 750));
			//data.Add("LargeBlockArmorBlock",					new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorSlope",					new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorCorner",					new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorCornerInv",				new HeatData(1.0, 750));
			//data.Add("LargeRoundArmor_Slope",					new HeatData(1.0, 750));
			//data.Add("LargeRoundArmor_Corner",				new HeatData(1.0, 750));
			//data.Add("LargeRoundArmor_CornerInv",				new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorBlock",				new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorSlope",				new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorCorner",			new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorCornerInv",			new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorBlock",					new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorSlope",					new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorCorner",					new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorCornerInv",				new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorBlock",				new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorSlope",				new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorCorner",			new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorCornerInv",			new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorRoundedSlope",			new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorRoundedCorner",			new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorAngledSlope",			new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorAngledCorner",			new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorRoundedSlope",		new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorRoundedCorner",		new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorAngledSlope",		new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorAngledCorner",		new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorRoundedSlope",			new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorRoundedCorner",			new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorAngledSlope",			new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorAngledCorner",			new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorRoundedSlope",		new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorRoundedCorner",		new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorAngledSlope",		new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorAngledCorner",		new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorRoundSlope",				new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorRoundCorner",			new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorRoundCornerInv",			new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorRoundSlope",		new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorRoundCorner",		new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorRoundCornerInv",	new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorRoundSlope",				new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorRoundCorner",			new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorRoundCornerInv",			new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorRoundSlope",		new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorRoundCorner",		new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorRoundCornerInv",	new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorSlope2BaseSmooth",		new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorSlope2TipSmooth",		new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorCorner2BaseSmooth",		new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorCorner2TipSmooth",		new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorInvCorner2BaseSmooth",	new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorInvCorner2TipSmooth",	new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorSlope2BaseSmooth",	new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorSlope2TipSmooth",	new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorCorner2BaseSmooth", new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorCorner2TipSmooth",	new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorInvCorner2BaseSmooth", new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorInvCorner2TipSmooth", new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorSlope2BaseSmooth",		new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorSlope2TipSmooth",		new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorCorner2BaseSmooth",		new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorCorner2TipSmooth",		new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorInvCorner2BaseSmooth",	new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorInvCorner2TipSmooth",	new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorSlope2BaseSmooth",	new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorSlope2TipSmooth",	new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorCorner2BaseSmooth", new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorCorner2TipSmooth",	new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorInvCorner2BaseSmooth", new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorInvCorner2TipSmooth", new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorSlope2Base",				new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorSlope2Tip",				new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorCorner2Base",			new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorCorner2Tip",				new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorInvCorner2Base", new HeatData(1.0, 750));
			//data.Add("LargeBlockArmorInvCorner2Tip", new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorSlope2Base", new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorSlope2Tip", new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorCorner2Base", new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorCorner2Tip", new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorInvCorner2Base", new HeatData(1.0, 750));
			//data.Add("LargeHeavyBlockArmorInvCorner2Tip", new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorSlope2Base", new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorSlope2Tip", new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorCorner2Base", new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorCorner2Tip", new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorInvCorner2Base", new HeatData(1.0, 750));
			//data.Add("SmallBlockArmorInvCorner2Tip", new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorSlope2Base", new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorSlope2Tip", new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorCorner2Base", new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorCorner2Tip", new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorInvCorner2Base", new HeatData(1.0, 750));
			//data.Add("SmallHeavyBlockArmorInvCorner2Tip", new HeatData(1.0, 750));
			data.Add("SmallProgrammableBlock", new HeatData(5.0, 750));
			data.Add("ControlPanel", new HeatData(5.0, 750));
			data.Add("SmallControlPanel", new HeatData(5.0, 750));
			//data.Add("SmallGatlingTurret", new HeatData(1.0, 750));
			//data.Add("SmallMissileTurret", new HeatData(1.0, 750));
			//data.Add("LargeInteriorTurret", new HeatData(1.0, 750));
			//data.Add("LargeBlockRadioAntenna", new HeatData(1.0, 750));
			//data.Add("LargeBlockBeacon", new HeatData(1.0, 750));
			//data.Add("SmallBlockBeacon", new HeatData(1.0, 750));
			//data.Add("LargeBlockFrontLight", new HeatData(1.0, 750));
			//data.Add("SmallLight", new HeatData(1.0, 750));
			//data.Add("LargeWindowSquare", new HeatData(1.0, 750));
			//data.Add("LargeWindowEdge", new HeatData(1.0, 750));
			//data.Add("LargeStairs", new HeatData(1.0, 750));
			//data.Add("LargeRamp", new HeatData(1.0, 750));
			//data.Add("LargeSteelCatwalk", new HeatData(1.0, 750));
			//data.Add("LargeSteelCatwalk2Sides", new HeatData(1.0, 750));
			//data.Add("LargeSteelCatwalkCorner", new HeatData(1.0, 750));
			//data.Add("LargeSteelCatwalkPlate", new HeatData(1.0, 750));
			//data.Add("LargeCoverWall", new HeatData(1.0, 750));
			//data.Add("LargeCoverWallHalf", new HeatData(1.0, 750));
			data.Add("LargeWarhead", new HeatData(50.0, 750));
			data.Add("SmallWarhead", new HeatData(50.0, 750));
			//data.Add("LargeDecoy", new HeatData(1.0, 750));
			//data.Add("SmallDecoy", new HeatData(1.0, 750));
			data.Add("LargeBlockInteriorWall", new HeatData(1.5, 750));
			data.Add("LargeInteriorPillar", new HeatData(1.5, 750));
			data.Add("LargeBlockLandingGear", new HeatData(0.4, 750));
			//data.Add("LargeProjector", new HeatData(1.0, 750));
			//data.Add("SmallProjector", new HeatData(1.0, 750));
			//data.Add("LargeRefinery", new HeatData(1.0, 750));
			//data.Add("Blast Furnace", new HeatData(1.0, 750));
			//data.Add("Big Arc Furnace", new HeatData(1.0, 750));
			//data.Add("BigPreciousFurnace", new HeatData(1.0, 750));
			//data.Add("BigSolidsRefinery", new HeatData(1.0, 750));
			//data.Add("BigGasCentrifugalRefinery", new HeatData(1.0, 750));
			data.Add("LargeAssembler", new HeatData(1.2, 750));
			//data.Add("AmmoAssembler", new HeatData(1.2, 750));
			//data.Add("BaseComponentsAssembler", new HeatData(1.2, 750));
			//data.Add("ElectronicsAssembler", new HeatData(1.2, 750));
			//data.Add("ConstructionComponentsAssembler", new HeatData(1.2, 750));
			//data.Add("LargeOreDetector", new HeatData(1.0, 750));
			data.Add("LargeMedicalRoom", new HeatData(3.0, 750));
			data.Add("LargeJumpDrive", new HeatData(2.0, 750));
			data.Add("LargeBlockCockpit", new HeatData(0.9, 750));
			//data.Add("LargeBlockCockpitSeat", new HeatData(1.0, 750));
			data.Add("DBSmallBlockFighterCockpit", new HeatData(0.7, 800));
			data.Add("SmallBlockCockpit", new HeatData(0.9, 750));
			//data.Add("CockpitOpen", new HeatData(1.0, 750));
			//data.Add("PassengerSeatLarge", new HeatData(1.0, 750));
			//data.Add("PassengerSeatSmall", new HeatData(1.0, 750));
			//data.Add("LargeBlockCryoChamber", new HeatData(1.0, 750));
			data.Add("SmallBlockLandingGear", new HeatData(0.4, 750));
			//data.Add("SmallBlockFrontLight", new HeatData(1.0, 750));
			//data.Add("LargeMissileLauncher", new HeatData(1.0, 750));
			//data.Add("SmallRocketLauncherReload", new HeatData(1.0, 750));
			data.Add("SmallBlockDrill", new HeatData(0.5, 750));
			data.Add("LargeBlockDrill", new HeatData(0.5, 750));
			//data.Add("SmallBlockOreDetector", new HeatData(1.0, 750));
			//data.Add("SmallBlockSensor", new HeatData(1.0, 750));
			//data.Add("LargeBlockSensor", new HeatData(1.0, 750));
			//data.Add("SmallBlockSoundBlock", new HeatData(1.0, 750));
			//data.Add("LargeBlockSoundBlock", new HeatData(1.0, 750));
			data.Add("SmallTextPanel", new HeatData(2.0, 750));
			data.Add("SmallLCDPanelWide", new HeatData(2.0, 750));
			data.Add("SmallLCDPanel", new HeatData(2.0, 750));
			//data.Add("OxygenTankSmall", new HeatData(1.0, 750));
			//data.Add("OxygenGeneratorSmall", new HeatData(1.0, 750));
			data.Add("LargeTextPanel", new HeatData(2.0, 750));
			data.Add("LargeLCDPanel", new HeatData(2.0, 750));
			data.Add("LargeLCDPanelWide", new HeatData(2.0, 750));
			//data.Add("SmallBlockRadioAntenna", new HeatData(1.0, 750));
			//data.Add("LargeBlockRemoteControl", new HeatData(1.0, 750));
			//data.Add("SmallBlockRemoteControl", new HeatData(1.0, 750));
			//data.Add("SmallAirVent", new HeatData(1.0, 750));
			//data.Add("LargeHydrogenTank", new HeatData(1.0, 750));
			//data.Add("SmallHydrogenTank", new HeatData(1.0, 750));
			//data.Add("LargeProductivityModule", new HeatData(1.0, 750));
			//data.Add("LargeEffectivenessModule", new HeatData(1.0, 750));
			//data.Add("LargeEnergyModule", new HeatData(1.0, 750));
			data.Add("SmallBlockSmallContainer", new HeatData(1.5, 750));
			data.Add("SmallBlockMediumContainer", new HeatData(1.5, 750));
			data.Add("SmallBlockLargeContainer", new HeatData(1.5, 750));
			data.Add("LargeBlockSmallContainer", new HeatData(1.5, 750));
			data.Add("LargeBlockLargeContainer", new HeatData(1.5, 750));
			data.Add("SmallBlockSmallThrust", new HeatData(0.8, 1000));
			data.Add("SmallBlockLargeThrust", new HeatData(0.8, 1000));
			data.Add("LargeBlockSmallThrust", new HeatData(0.8, 1000));
			data.Add("LargeBlockLargeThrust", new HeatData(0.8, 1000));
			data.Add("LargeBlockLargeHydrogenThrust", new HeatData(0.8, 1000));
			data.Add("LargeBlockSmallHydrogenThrust", new HeatData(0.8, 1000));
			data.Add("SmallBlockLargeHydrogenThrust", new HeatData(0.8, 1000));
			data.Add("SmallBlockSmallHydrogenThrust", new HeatData(0.8, 1000));
			data.Add("LargeBlockLargeAtmosphericThrust", new HeatData(0.8, 1000));
			data.Add("LargeBlockSmallAtmosphericThrust", new HeatData(0.8, 1000));
			data.Add("SmallBlockLargeAtmosphericThrust", new HeatData(0.8, 1000));
			data.Add("SmallBlockSmallAtmosphericThrust", new HeatData(0.8, 1000));
			data.Add("SmallCameraBlock", new HeatData(0.5, 750));
			data.Add("LargeCameraBlock", new HeatData(0.5, 750));
			data.Add("LargeBlockGyro", new HeatData(10.0, 750));
			data.Add("SmallBlockGyro", new HeatData(10.0, 750));
			data.Add("SmallBlockSmallGenerator", new HeatData(2.0, 750));
			data.Add("SmallBlockLargeGenerator", new HeatData(2.0, 750));
			data.Add("LargeBlockSmallGenerator", new HeatData(2.0, 750));
			data.Add("LargeBlockLargeGenerator", new HeatData(2.0, 750));
			//data.Add("LargePistonBase", new HeatData(1.0, 750));
			//data.Add("LargePistonTop", new HeatData(1.0, 750));
			//data.Add("SmallPistonBase", new HeatData(1.0, 750));
			//data.Add("SmallPistonTop", new HeatData(1.0, 750));
			//data.Add("LargeStator", new HeatData(1.0, 750));
			//data.Add("Suspension3x3", new HeatData(1.0, 750));
			//data.Add("Suspension5x5", new HeatData(1.0, 750));
			//data.Add("Suspension1x1", new HeatData(1.0, 750));
			//data.Add("SmallSuspension3x3", new HeatData(1.0, 750));
			//data.Add("SmallSuspension5x5", new HeatData(1.0, 750));
			//data.Add("SmallSuspension1x1", new HeatData(1.0, 750));
			//data.Add("LargeRotor", new HeatData(1.0, 750));
			//data.Add("SmallStator", new HeatData(1.0, 750));
			//data.Add("SmallRotor", new HeatData(1.0, 750));
			//data.Add("LargeAdvancedStator", new HeatData(1.0, 750));
			//data.Add("LargeAdvancedRotor", new HeatData(1.0, 750));
			//data.Add("SmallAdvancedStator", new HeatData(1.0, 750));
			//data.Add("SmallAdvancedRotor", new HeatData(1.0, 750));
			data.Add("ButtonPanelLarge", new HeatData(3.0, 750));
			data.Add("ButtonPanelSmall", new HeatData(3.0, 750));
			data.Add("TimerBlockLarge", new HeatData(3.0, 750));
			data.Add("TimerBlockSmall", new HeatData(3.0, 750));
			//data.Add("LargeRailStraight", new HeatData(1.0, 750));
			data.Add("LargeBlockSolarPanel", new HeatData(20.0, 750));
			data.Add("SmallBlockSolarPanel", new HeatData(20.0, 750));
			data.Add("LargeBlockOxygenFarm", new HeatData(0.9, 750));
			/*data.Add("Window1x2Slope", new HeatData(1.0, 750));
			data.Add("Window1x2Inv", new HeatData(1.0, 750));
			data.Add("Window1x2Face", new HeatData(1.0, 750));
			data.Add("Window1x2SideLeft", new HeatData(1.0, 750));
			data.Add("Window1x2SideRight", new HeatData(1.0, 750));
			data.Add("Window1x1Slope", new HeatData(1.0, 750));
			data.Add("Window1x1Face", new HeatData(1.0, 750));
			data.Add("Window1x1Side", new HeatData(1.0, 750));
			data.Add("Window1x1Inv", new HeatData(1.0, 750));
			data.Add("Window1x2Flat", new HeatData(1.0, 750));
			data.Add("Window1x2FlatInv", new HeatData(1.0, 750));
			data.Add("Window1x1Flat", new HeatData(1.0, 750));
			data.Add("Window1x1FlatInv", new HeatData(1.0, 750));
			data.Add("Window3x3Flat", new HeatData(1.0, 750));
			data.Add("Window3x3FlatInv", new HeatData(1.0, 750));
			data.Add("Window2x3Flat", new HeatData(1.0, 750));
			data.Add("Window2x3FlatInv", new HeatData(1.0, 750));*/
			//data.Add("SmallBlockConveyor", new HeatData(1.0, 750));
			//data.Add("LargeBlockConveyor", new HeatData(1.0, 750));
			//data.Add("Collector", new HeatData(0.5, 750));
			//data.Add("CollectorSmall", new HeatData(1.0, 750));
			//data.Add("Connector", new HeatData(1.0, 750));
			//data.Add("ConnectorSmall", new HeatData(1.0, 750));
			//data.Add("ConnectorMedium", new HeatData(1.0, 750));
			//data.Add("ConveyorTube", new HeatData(1.0, 750));
			//data.Add("ConveyorTubeSmall", new HeatData(1.0, 750));
			//data.Add("ConveyorTubeMedium", new HeatData(1.0, 750));
			//data.Add("ConveyorFrameMedium", new HeatData(1.0, 750));
			//data.Add("ConveyorTubeCurved", new HeatData(1.0, 750));
			////data.Add("ConveyorTubeSmallCurved", new HeatData(1.0, 750));
			//data.Add("ConveyorTubeCurvedMedium", new HeatData(1.0, 750));
			//data.Add("SmallShipConveyorHub", new HeatData(1.0, 750));
			//data.Add("LargeBlockConveyorSorter", new HeatData(1.0, 750));
			//data.Add("MediumBlockConveyorSorter", new HeatData(1.0, 750));
			//data.Add("SmallBlockConveyorSorter", new HeatData(1.0, 750));
			data.Add("VirtualMassLarge", new HeatData(2.0, 750));
			data.Add("VirtualMassSmall", new HeatData(2.0, 750));
			//data.Add("SpaceBallLarge", new HeatData(1.0, 750));
			//data.Add("SpaceBallSmall", new HeatData(1.0, 750));
			//data.Add("SmallRealWheel1x1", new HeatData(1.0, 750));
			//data.Add("SmallRealWheel", new HeatData(1.0, 750));
			//data.Add("SmallRealWheel5x5", new HeatData(1.0, 750));
			//data.Add("RealWheel1x1", new HeatData(1.0, 750));
			//data.Add("RealWheel", new HeatData(1.0, 750));
			//data.Add("RealWheel5x5", new HeatData(1.0, 750));
			//data.Add("Wheel1x1", new HeatData(1.0, 750));
			//data.Add("SmallWheel1x1", new HeatData(1.0, 750));
			//data.Add("Wheel3x3", new HeatData(1.0, 750));
			//data.Add("SmallWheel3x3", new HeatData(1.0, 750));
			//data.Add("Wheel5x5", new HeatData(1.0, 750));
			//data.Add("SmallWheel5x5", new HeatData(1.0, 750));
			//data.Add("LargeShipGrinder", new HeatData(1.0, 750));
			//data.Add("SmallShipGrinder", new HeatData(1.0, 750));
			//data.Add("LargeShipWelder", new HeatData(1.0, 750));
			//data.Add("SmallShipWelder", new HeatData(1.0, 750));
			//data.Add("LargeShipMergeBlock", new HeatData(1.0, 750));
			//data.Add("SmallShipMergeBlock", new HeatData(1.0, 750));
			//data.Add("ArmorAlpha", new HeatData(1.0, 750));
			//data.Add("ArmorCenter", new HeatData(1.0, 750));
			data.Add("LargeProgrammableBlock", new HeatData(5.0, 750));
			//data.Add("ArmorCorner", new HeatData(1.0, 750));
			//data.Add("ArmorInvCorner", new HeatData(1.0, 750));
			//data.Add("ArmorSide", new HeatData(1.0, 750));
			//data.Add("SmallArmorCenter", new HeatData(1.0, 750));
			//data.Add("SmallArmorCorner", new HeatData(1.0, 750));
			//data.Add("SmallArmorInvCorner", new HeatData(1.0, 750));
			//data.Add("SmallArmorSide", new HeatData(1.0, 750));
			data.Add("LargeBlockLaserAntenna", new HeatData(1.5, 750));
			data.Add("SmallBlockLaserAntenna", new HeatData(1.5, 750));
			data.Add("LargeBlockSlideDoor", new HeatData(0.7, 750));
			#endregion
			//Log.Info("Looping through information");
			foreach (var keys in def)
			{
				try
				{
					//Log.DebugWrite(DebugLevel.Info,keys.Id.SubtypeName);
					//Log.DebugWrite(DebugLevel.Info,keys.DescriptionString);
					if (keys.DescriptionString == null || keys.DescriptionString.Length == 0) continue;
					var res = reg.Split(keys.DescriptionString);
					//Log.IncreaseIndent();
					if (res.Length > 1)
					{
						//Log.Info(res[1]);
						if(m_data.TryGetValue(keys.Id.SubtypeName, out value))
						{
							//already exists!
							data.Remove(keys.Id.SubtypeName);
							//Log.DebugWrite(DebugLevel.Info, string.Format("Removing duplicate match: {0}", keys.Id.SubtypeName));
						}

						
						value = new HeatData();
						var search = regcom.Split(res[1]);
						if (search == null)
						{
							//Log.DecreaseIndent();
							continue;
						}
						foreach (string parts in search)
						{
							var dataeq = regeq.Split(parts);
							if (dataeq.Length == 0)
							{
								//Log.DecreaseIndent();
								continue;
							}
							switch(dataeq[0].ToLower())
							{
								case "all":
									value.heatMult_u = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatMult_d = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatMult_l = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatMult_r = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatMult_f = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatMult_b = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "u":
								case "up":
									value.heatMult_u = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "d":
								case "down":
									value.heatMult_d = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "l":
                                case "left":
									value.heatMult_l = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "r":
								case "right":
									value.heatMult_r = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "f":
								case "forward":
									value.heatMult_f = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
                                case "b":
								case "backward":
									value.heatMult_b = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "tall":
									value.heatThresh_u = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatThresh_d = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatThresh_l = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatThresh_r = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatThresh_f = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									value.heatThresh_b = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "tu":
								case "tup":
									value.heatThresh_u = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "td":
								case "tdown":
									value.heatThresh_d = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "tl":
								case "tleft":
									value.heatThresh_l = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "tr":
								case "tright":
									value.heatThresh_r = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "tf":
								case "tforward":
									value.heatThresh_f = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
								case "tb":
								case "tbackward":
									value.heatThresh_b = Convert.ToDouble(dataeq[1], new CultureInfo("en-US"));
									break;
							}
							

						}
						//Log.DebugWrite(DebugLevel.Info, string.Format("Adding {0}", keys.Id.SubtypeName));

						data.Add(keys.Id.SubtypeName, value);
					}
					//Log.DecreaseIndent();

				}
				catch (Exception ex)
				{
					//Log.DebugWrite(DebugLevel.Error, string.Format("Warning Error in Description: {0} {1}", keys.Id.SubtypeName, ex.ToString()));
				}

			}
			foreach(KeyValuePair<string, HeatData> items in data)
			{
				//Log.DebugWrite(DebugLevel.Info, string.Format("{0} updated.", items.Key));
			}
		}

		internal void Close()
		{
			m_data.Clear();
		}

		public void Load(bool l_default = false)
		{
			//Log.DebugWrite(DebugLevel.Info, "Loading XML");
			if (l_default)
			{
				m_data.Clear();
				Init();
				//Log.DebugWrite(DebugLevel.Info, "Loaded Defaults");
				return;
			}
			try
			{
				if (MyAPIGateway.Utilities.FileExistsInLocalStorage(FILE, typeof(HeatContainerWrapper)) && !l_default)
				{
					var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(FILE, typeof(HeatContainerWrapper));
					var xmlText = reader.ReadToEnd();
					reader.Close();
					//m_data.Clear();
					var savecontainer = MyAPIGateway.Utilities.SerializeFromXML<HeatContainerWrapper>(xmlText);
					loadContainer(savecontainer);
					//Log.DebugWrite(DebugLevel.Info, "Load Complete");
					return;
				}
			}
			catch (Exception ex)
			{
				//Log.DebugWrite(DebugLevel.Error, ex);
			}
		}

		private void loadContainer(HeatContainerWrapper savecontainer)
		{
			HeatData trash = new HeatData();
			foreach (HeatContainer savedata in savecontainer.wrapper)
			{
				
				if(data.TryGetValue(savedata.subtypeid, out trash))
				{
					data.Remove(savedata.subtypeid);
				}
				data.Add(savedata.subtypeid, savedata.value);
			}
		}

		public void Save()
		{
			//Log.DebugWrite(DebugLevel.Info, "Saving XML (heat)");
			HeatContainerWrapper savecontainer = new HeatContainerWrapper();
			foreach(KeyValuePair<string, HeatData> kpair in data)
			{
				//Log.DebugWrite(DebugLevel.Info, kpair.Key);
				savecontainer.wrapper.Add(new HeatContainer(kpair.Key, kpair.Value));
			}
			var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FILE, typeof(HeatContainerWrapper));
			writer.Write(MyAPIGateway.Utilities.SerializeToXML(savecontainer));
			writer.Flush();
			writer.Close();
			//Log.DebugWrite(DebugLevel.Info, "Save Complete");
		}
		public class HeatContainerWrapper
		{
			public List<HeatContainer> wrapper = new List<HeatContainer>();
			public HeatContainerWrapper()
			{

			}
		}
		public class HeatContainer
		{
			public string subtypeid;
			public HeatData value;
			public HeatContainer()
			{
				subtypeid = "";
			}
			public HeatContainer(string key, HeatData value)
			{
				subtypeid = key;
				this.value = value;
			}
		}

	}
}
