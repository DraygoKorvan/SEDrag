using VRageMath;


namespace SEDrag.Definition
{
	public class LiftData
	{
		private Base6Directions.Direction m_front = Base6Directions.Direction.Forward; //which direction the block considers to be its 'front'
		private double m_dragMult_f = 1.0;//drag multiplier on the front etc set defaults up here
		//etc

		//should have some variables for stabilization
		//some for adjusting the center of lift. 

		public Base6Directions.Direction front
		{
			get { return m_front; }
			set { m_front = value; }
		}
		public double dragMult_f
		{
			get { return m_dragMult_f; }
			set { m_dragMult_f = value; }
		}
		//etc

	}
}
