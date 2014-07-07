using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Rain_Generator
{
    public class RainGenerator
    {
		public float[] Generate(int sampleRate, TimeSpan duration)
		{
			var sampleCount = (int)duration.TotalSeconds * sampleRate;
			var samples = new float[sampleCount];
			var r = new Random();
			var addDrop = false;
			var addedDrop = true;
			var dropFreq = 0;
			var totalDropDuration = 0.0;
			var singleDropDuration = 0.0;
			var currentDropDuration = 0;
			var softenStart = false;
			var softenEnd = false;
			var amplitude = 0.0;
			var addWind = false;
			var addedWind = true;
			var totalWindDuration = 0.0;
			var windDuration = 0;
			var ii = 0;

			for (var i = 0; i < sampleCount; i++)
			{
				if (addedDrop)
				{
					addDrop = r.NextDouble() < 1.0 / sampleRate / 0.005 ? true : false; // Rain intensity
					dropFreq = r.Next(4000, 13001);                                     // Rain frequency range
					singleDropDuration = sampleRate / dropFreq;                         // Samples for one full wave at specified frequency (i.e, sample count from peak to peak)
					totalDropDuration = r.Next(1, 7) * singleDropDuration;              // Total number of "full waves" for the drop
					softenStart = r.NextDouble() < 0.5 ? true : false;                  // Soften start of drop
					softenEnd = r.NextDouble() < 0.5 ? true : false;                    // Soften end of drop
					amplitude = r.NextDouble();                                         // Select drop loudness

					if (addedWind)
					{
						addWind = r.NextDouble() < 1 / (10.0 * sampleRate) ? true : false;     // Determine whether or not to add wind
						totalWindDuration = r.NextDouble();                                   // 0.1 = 10 seconds, 0.5 = 2 seconds, etc

						if (addWind)
						{
							addedWind = false;
						}
					}
				}

				if (addWind)
				{
					samples[i] += (float)(r.NextDouble() + (r.NextDouble() * (Math.Sin(((Math.PI * 2 * totalWindDuration) / sampleRate) * ii)))) * 0.1f;

					windDuration++;
					ii++;

					if (windDuration >= (1 / totalWindDuration) * sampleRate)
					{
						ii = 0;
						windDuration = 0;
						addedWind = true;
						addWind = false;
					}
				}
				else
				{
					samples[i] += (float)(GetBackgroundNoise(r, i, sampleRate, dropFreq)/*r.NextDouble()*/ * 0.1);
				}


				if (addDrop)
				{
					samples[i] += (float)(amplitude * Math.Sin(((Math.PI * 2 * dropFreq) / sampleRate) * i) * 4);

					if (softenStart)
					{
						samples[i] *= (float)(currentDropDuration / totalDropDuration);
					}

					if (softenEnd)
					{
						samples[i] *= 1 - (float)(currentDropDuration / totalDropDuration);
					}

					addedDrop = false;
					currentDropDuration++;

					if (currentDropDuration > totalDropDuration)
					{
						addedDrop = true;
						addDrop = false;
						softenStart = false;
						softenEnd = false;
						currentDropDuration = 0;
					}
				}
			}

			return samples;
		}


		private double GetBackgroundNoise(Random r, int i, int sampleRate, int freq)
		{
			return (Math.Sin(((Math.PI * 2 * freq) / (sampleRate + r.Next(-(int)(sampleRate * 0.01), (int)(sampleRate * 0.01)))) * i) * 0.5) + (r.NextDouble() * 0.5);
		}
    }
}
