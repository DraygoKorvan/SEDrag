using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SEDrag
{
	public class WeatherMapGenerator
	{
		Random RandomGen;
		const int TableSize = 256;
		const int TableSizeMask = TableSize - 1;
		int m_height = 512;
		int m_width = 512;
		float[] Map;
		int[] pTable = new int[TableSize * 2];
		float[] r = new float[TableSize];

		float smoothstep(float t) 
		{ 
			return t * t * (3 - 2 * t); 
		}


		float eval(Vector2 p)
		{ 

			int xi = (int)Math.Floor(p.X);
			int yi = (int)Math.Floor(p.Y);

			float tx = p.X - xi;
			float ty = p.Y - yi;

			int rx0 = xi & TableSizeMask;
			int rx1 = (rx0 + 1) & TableSizeMask;
			int ry0 = yi & TableSizeMask;
			int ry1 = (ry0 + 1) & TableSizeMask;

			// random values at the corners of the cell using permutation table
			float c00 = r[pTable[pTable[rx0] + ry0]]; 
			float c10 = r[pTable[pTable[rx1] + ry0]]; 
			float c01 = r[pTable[pTable[rx0] + ry1]]; 
			float c11 = r[pTable[pTable[rx1] + ry1]]; 
 
			// remapping of tx and ty using the Smoothstep function 
			float sx = smoothstep(tx);
			float sy = smoothstep(ty);

			// linearly interpolate values along the x axis
			float nx0 = MathHelper.Lerp(c00, c10, sx);
			float nx1 = MathHelper.Lerp(c01, c11, sx); 
 
			// linearly interpolate the nx0/nx1 along they y axis
			return MathHelper.Lerp(nx0, nx1, sy);
		}
		public void Swap(int x, int y)
		{
			int t = pTable[x];
			pTable[x] = pTable[y];
			pTable[y] = t;

		}
		public WeatherMapGenerator(int seed)
		{

			Map = new float[m_height * m_width];
            RandomGen = new Random(seed);
			for (int k = 0; k < TableSize; ++k)
			{
				r[k] = (float)RandomGen.NextDouble();
				pTable[k] = k;
			}
			for (int k = 0; k < TableSize; ++k)
			{
				int i = RandomGen.Next(int.MaxValue) & TableSizeMask;
				Swap(k, i);
				pTable[k + TableSize] = pTable[k];
			}


			float frequency = 0.02f;
			float frequencyMult = 1.8f;
			float amplitudeMult = 1.0f;
			int numLayers = 5;
			for (int j = 0; j < m_height; ++j)
			{
				for (int i = 0; i < m_width; ++i)
				{
					Vector2 pNoise = new Vector2(i, j) * frequency;
					float amplitude = 1;
					float noiseValue = 0;
					// compute some fractal noise
					for (int l = 0; l < numLayers; ++l)
					{
						noiseValue += eval(pNoise) * amplitude;
						pNoise *= frequencyMult;
						amplitude *= amplitudeMult;
					}
					// we "displace" the value i used in the sin() expression by noiseValue * 100
					Map[j * m_width + i] = (float)(Math.Sin((i + noiseValue * 100) * 2 * Math.PI / 200f) + 1) / 2f;
				}
			}
		}

		public float GetValueAtPointNormalized(float x, float y, int offset)
		{
			//normal between 0, 1
			int PosX = ((int)(x * m_width) + offset) % m_width;
			int PosY = (int)(y * m_height) % m_height;
			return Map[PosX * m_width + PosY];
		}
	}
}
