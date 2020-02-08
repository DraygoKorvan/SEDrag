using VRageMath;


namespace SEDrag.Definition
{
	public class HeatData
	{
		private Base6Directions.Direction m_front = Base6Directions.Direction.Forward; //which direction the block considers to be its 'forward'
		private Base6Directions.Direction m_top = Base6Directions.Direction.Up; //which direction the block considers to be its 'up'

		private double m_heatMult_f = 1.0;
		private double m_heatMult_b = 1.0;
		private double m_heatMult_u = 1.0;
		private double m_heatMult_d = 1.0;
		private double m_heatMult_l = 1.0;
		private double m_heatMult_r = 1.0;

		private double m_heatThresh_f = 750.0;
		private double m_heatThresh_b = 750.0;
		private double m_heatThresh_u = 750.0;
		private double m_heatThresh_d = 750.0;
		private double m_heatThresh_l = 750.0;
		private double m_heatThresh_r = 750.0;
		//etc

		//should have some variables for stabilization
		//some for adjusting the center of lift. 
		public HeatData()
		{
			heatThresh = 750;
			heatMult = 1.0;
		}
		public HeatData(double multiplier, double threshhold)
		{
			heatThresh = threshhold;
			heatMult = multiplier;
		}
		public double heatThresh
		{
			set
			{
				m_heatThresh_f = (value >= 750.0 ? value : 750.0);
				m_heatThresh_b = (value >= 750.0 ? value : 750.0);
				m_heatThresh_u = (value >= 750.0 ? value : 750.0);
				m_heatThresh_d = (value >= 750.0 ? value : 750.0);
				m_heatThresh_l = (value >= 750.0 ? value : 750.0);
				m_heatThresh_r = (value >= 750.0 ? value : 750.0);
			}
		}
		public double heatThresh_f
		{
			get { return m_heatThresh_f; }
			set { m_heatThresh_f = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_b
		{
			get { return m_heatThresh_b; }
			set { m_heatThresh_b = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_u
		{
			get { return m_heatThresh_u; }
			set { m_heatThresh_u = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_d
		{
			get { return m_heatThresh_d; }
			set { m_heatThresh_d = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_l
		{
			get { return m_heatThresh_l; }
			set { m_heatThresh_l = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_r
		{
			get { return m_heatThresh_r; }
			set { m_heatThresh_r = (value >= 750.0 ? value : 750.0); }
		}

		public double heatMult
		{
			set
			{
				m_heatMult_f = (value >= 0.0 ? value : 0.0);
				m_heatMult_b = (value >= 0.0 ? value : 0.0);
				m_heatMult_l = (value >= 0.0 ? value : 0.0);
				m_heatMult_r = (value >= 0.0 ? value : 0.0);
				m_heatMult_u = (value >= 0.0 ? value : 0.0);
				m_heatMult_d = (value >= 0.0 ? value : 0.0);
			}
		}
		public double heatMult_f
		{
			get { return m_heatMult_f; }
			set { m_heatMult_f = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_b
		{
			get { return m_heatMult_b; }
			set { m_heatMult_b = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_d
		{
			get { return m_heatMult_d; }
			set { m_heatMult_d = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_u
		{
			get { return m_heatMult_u; }
			set { m_heatMult_u = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_l
		{
			get { return m_heatMult_l; }
			set { m_heatMult_l = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_r
		{
			get { return m_heatMult_r; }
			set { m_heatMult_r = (value >= 0.0 ? value : 0.0); }
		}

		public double getHeatMult(Base6Directions.Direction dir)
		{
			switch(dir)
			{
				case Base6Directions.Direction.Forward:
					return heatMult_f;
				case Base6Directions.Direction.Backward:
					return heatMult_b;
				case Base6Directions.Direction.Left:
					return heatMult_l;
				case Base6Directions.Direction.Right:
					return heatMult_r;
				case Base6Directions.Direction.Up:
					return heatMult_u;
				case Base6Directions.Direction.Down:
					return heatMult_d;
			}
			return heatMult_f;//should NEVER be triggered but compiler error otherwise. 
        }
		public double getHeatTresh(Base6Directions.Direction dir)
		{
			switch (dir)
			{
				case Base6Directions.Direction.Forward:
					return heatThresh_f;
				case Base6Directions.Direction.Backward:
					return heatThresh_b;
				case Base6Directions.Direction.Left:
					return heatThresh_l;
				case Base6Directions.Direction.Right:
					return heatThresh_r;
				case Base6Directions.Direction.Up:
					return heatThresh_u;
				case Base6Directions.Direction.Down:
					return heatThresh_d;
			}
			return heatThresh_f; //should never hit this. 
		}
	}
}


