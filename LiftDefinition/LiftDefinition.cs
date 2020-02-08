using System.Collections.Generic;


namespace SEDrag.Definition
{
	public class LiftDefinition
	{
		private Dictionary<string, LiftData> m_data;

		public Dictionary<string, LiftData> data
		{
			get { return m_data; }
			set { m_data = value; }
		}

		public void Init()
		{
			//init
		}

		public void Load()
		{
			//loading function
		}
	}
}
