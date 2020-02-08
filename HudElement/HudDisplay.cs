using Draygo.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace SEDrag.HudElement
{
	public class HudDisplay
	{
		HudAPIv2 HUD;
		private bool init = false;
		Vector3D LastSpeed = Vector3D.Zero;
		HudAPIv2.HUDMessage WindSpeed;
		//HudAPIv2.BillBoardHUDMessage Background;
		HudAPIv2.BillBoardHUDMessage DirectionalIndicator;

		HudAPIv2.BillBoardHUDMessage DirectionalGlobe;
		HudAPIv2.BillBoardHUDMessage VerticalIndicator;
		HudAPIv2.BillBoardHUDMessage VerticalIndicatorBox;
		HudAPIv2.BillBoardHUDMessage BackgroundBox;

		StringBuilder WindSpeedStr = new StringBuilder();
		static string WindSpeedDisplayFormat = "Wind: {0:N2} {1}";
		float maxheight = 0f;
		float textoffset = 0f;
		float viMaxwidth = 0f;
        Vector2D m_Origin = new Vector2D(0.2, -0.65);
		bool needsRefresh = false;

		public HudDisplay(HudAPIv2 API)
		{
			HUD = API;
			TryInit();
			
		}
		public bool Visible
		{
			get
			{
				if (!init)
					return false;
				return WindSpeed.Visible;
			}
			set
			{
				if (!init || WindSpeed.Visible == value)
					return;
				WindSpeed.Visible = value;
				DirectionalIndicator.Visible = value;

				DirectionalGlobe.Visible = value;
				VerticalIndicator.Visible = value;
				VerticalIndicatorBox.Visible = value;
				BackgroundBox.Visible = value;
            }
		}

		public Vector2D Origin
		{
			get
			{
				return m_Origin;
			}
			set
			{
				if (m_Origin == value)
					return;
				m_Origin = value;
				if(init)
				{
					WindSpeed.Origin = value;
					DirectionalIndicator.Origin = value;

					DirectionalGlobe.Origin = value;
					VerticalIndicator.Origin = value;
					VerticalIndicatorBox.Origin = value;
					BackgroundBox.Origin = value;
					//Background.Origin = value;
				}
			}
		}
		public Vector2D Size
		{
			get
			{
				if (init)
				{
					return WindSpeed.GetTextLength() * 2d;
                }
				else
				{
					return Vector2D.One / 10d;
				}
					
				
            }
		}

		public bool TryInit()
		{
			if (init)
				return true;
			if(HUD.Heartbeat)
			{
				Init();
				return true;
			}
			return false;
		}

		public void Init()
		{
			if (init)
				return;
			Color backgroundcolor = new Color(167, 220, 255);
            WindSpeed = new HudAPIv2.HUDMessage();
			WindSpeed.Message = WindSpeedStr;
			WindSpeed.Origin = Origin;
			//-.077
			DirectionalIndicator = new HudAPIv2.BillBoardHUDMessage(MyStringId.GetOrCompute("DragWindArrow"), WindSpeed.Origin, Color.Blue, Rotation: 0f, Shadowing: false);
			DirectionalGlobe = new HudAPIv2.BillBoardHUDMessage(MyStringId.GetOrCompute("DragWindGlobe"), WindSpeed.Origin, Color.White, Shadowing: true);
			VerticalIndicator = new HudAPIv2.BillBoardHUDMessage(MyStringId.GetOrCompute("DragVerticalIndicatorBoxFill"), WindSpeed.Origin, Color.Yellow * 0.9f, Rotation: 0f, Shadowing: true);
			VerticalIndicatorBox = new HudAPIv2.BillBoardHUDMessage(MyStringId.GetOrCompute("DragVerticalIndicatorBox"), WindSpeed.Origin, Color.White, Rotation: 0f, Shadowing: true);
			BackgroundBox = new HudAPIv2.BillBoardHUDMessage(MyStringId.GetOrCompute("DragWindPlate"), WindSpeed.Origin, backgroundcolor * 0.1f + Color.Black * 0.4f, Shadowing: true);
			VerticalIndicator.uvEnabled = true;
			VerticalIndicator.TextureSize = 128f;
			init = true;
			Refresh();
		}
		public void Refresh()
		{
			if (!init)
				return;
			

			WindSpeed.Blend = BlendTypeEnum.PostPP;
			WindSpeed.Options |= HudAPIv2.Options.HideHud;
			WindSpeed.Message.Clear();
			var len = WindSpeed.GetTextLength();
			float fixedhw = 500f / (24f * 128f);
			
			DirectionalIndicator.Width = fixedhw;
			DirectionalIndicator.Height = fixedhw;
			DirectionalIndicator.Options |= HudAPIv2.Options.Fixed | HudAPIv2.Options.FOVScale;
			DirectionalIndicator.Blend = BlendTypeEnum.PostPP;

			
			DirectionalGlobe.Width = fixedhw;
			DirectionalGlobe.Height = fixedhw;
			DirectionalGlobe.Options |= HudAPIv2.Options.Fixed | HudAPIv2.Options.FOVScale;
			DirectionalGlobe.Blend = BlendTypeEnum.PostPP;
			
			VerticalIndicator.Offset = new Vector2D(-fixedhw * 0.35, 0);
			VerticalIndicator.Width = (float)len.Y;
			VerticalIndicator.Blend = BlendTypeEnum.PostPP;

			
			VerticalIndicatorBox.Offset = new Vector2D(-fixedhw * 0.35, 0);
			VerticalIndicatorBox.Width = (float)len.Y;
			VerticalIndicatorBox.Blend = BlendTypeEnum.PostPP;

			
			BackgroundBox.Offset = new Vector2D(0, len.Y * -0.4);
			BackgroundBox.Width = fixedhw * 2;
			BackgroundBox.Height = fixedhw * 2;
			BackgroundBox.Options |= HudAPIv2.Options.Fixed | HudAPIv2.Options.FOVScale;
			BackgroundBox.Blend = BlendTypeEnum.PostPP;

			//MyAPIGateway.Utilities.ShowMessage("Y", len.Y.ToString());
			viMaxwidth = (float)len.Y;
            maxheight = (float)len.Y * 2f;
			textoffset = -((float)len.Y * 3.5f) / 2f;
			VerticalIndicatorBox.Height = maxheight;
			WindSpeed.Offset = new Vector2D(len.X / -2d, textoffset);
			VerticalIndicator.Height = maxheight;


			needsRefresh = false;
		}

		public void Update()
		{
			if(TryInit())
			{
				if (MyAPIGateway.Session?.LocalHumanPlayer?.Controller?.ControlledEntity?.Entity == null)
				{
					Visible = false;
					return;
				}
				if(!CoreDrag.instance?.Pref?.ShowWindIndicator ?? true)
				{
					Visible = false;
					return;
				}
				if (needsRefresh)
					Refresh();
				var Ent = MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity?.Entity;
				WindSpeedStr.Clear();

				if (MyAPIGateway.Session?.LocalHumanPlayer?.Controller?.ControlledEntity?.Entity?.Parent != null)
					Ent = MyAPIGateway.Session.LocalHumanPlayer.Controller.ControlledEntity.Entity.Parent;
				var Drag = CoreDrag.instance?.GetDrag(Ent) ?? null;

				if (Drag != null)
				{
					LastSpeed = Drag.GetWind();
					if (LastSpeed.AbsMax() < 0.001d)
					{
						Visible = false;
						WindSpeedStr.AppendFormat(WindSpeedDisplayFormat, 0, "m\\s");
						return;
					}
					else
						Visible = true;
				}
				else
				{
					this.Visible = false;
					WindSpeedStr.AppendFormat(WindSpeedDisplayFormat, 0, "m\\s");
					return;
				}

				var LastSpeedNormal = LastSpeed;
				LastSpeedNormal.Normalize();

                var Normal = Vector3D.TransformNormal(LastSpeed, MatrixD.Normalize(MatrixD.Invert(MyAPIGateway.Session.Camera.WorldMatrix)));
				Normal.Normalize();
				float UpDown = (float)Normal.Y;
				var Directional = new Vector2D(Normal.X, Normal.Z);
				Directional.Normalize();
				var angle = Math.Atan2(Directional.Y, Directional.X);
				DirectionalIndicator.Rotation = (float)angle + (float)Math.PI / 2f;
				VerticalIndicator.uvSize = new Vector2(128f, 64f * Math.Abs(UpDown));
				VerticalIndicator.Height = maxheight * UpDown / 2;
				var off = VerticalIndicator.Offset;
				off.Y = maxheight * -UpDown * 0.25f;
				VerticalIndicator.Offset = off;
				VerticalIndicator.Rotation = (UpDown > 0 ? 0f : (float)Math.PI);

				//VerticalIndicator.Width = (float)MathHelper.Clamp(Math.Log10(Math.Abs(UpDown) * 0.25f + 1) * 24, 0, 1) * viMaxwidth * Math.Sign(UpDown);

				WindSpeedStr.AppendFormat(WindSpeedDisplayFormat, LastSpeed.Length() * Drag.CheckOcclusion(), "m\\s");

				WindSpeed.Offset = new Vector2D(WindSpeed.GetTextLength().X / -2d, textoffset);
				

			}
		}

		internal void MarkRefresh()
		{
			needsRefresh = true;
        }
	}
}
