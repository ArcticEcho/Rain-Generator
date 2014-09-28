using System;



namespace RainGenerator
{
	public class Generator
	{
		private readonly Random r = new Random();
		private readonly float sampleRate;
		private int sampleCount;
		private float[] samples;
		private bool addDrop;
		private bool addedDrop = true;
		private int dropFreq;
		private float totalDropDuration;
		private float singleDropDuration;
		private int currentDropDuration;
		private double amplitude;
		private bool addWind;
		private bool addedWind = true;
		private double totalWindDuration;
		private int windDuration;
		private int ii;



		public Generator(float sampleRate)
		{
			this.sampleRate = sampleRate;
		}



		public float[] Generate(TimeSpan duration, float rainIntensity = 0.005f, int lowerDropFreq = 4000, int higherDropFreq = 130001)
		{
			sampleCount = (int)(duration.TotalSeconds * sampleRate);
			samples = new float[sampleCount];

			for (var i = 0; i < sampleCount; i++)
			{
				if (addedDrop)
				{
					addDrop = r.NextDouble() < 1.0 / sampleRate / rainIntensity; // Calc rain intensity.
					dropFreq = r.Next(lowerDropFreq, higherDropFreq);            // Pick the drop's frequency.
					singleDropDuration = sampleRate / dropFreq;                  // Samples for one full wave at specified frequency (i.e., sample count from peak to peak).
					totalDropDuration = r.Next(1, 7) * singleDropDuration;       // Total number of oscillations ("full waves") long this drop will be drop.
					amplitude = r.NextDouble();                                  // Choose drop "loudness".

					if (addedWind)
					{
						addWind = r.NextDouble() < 1 / (10.0 * sampleRate); // Determine whether or not to add wind.
						totalWindDuration = r.NextDouble();                 // 0.1 = 10 secs, 0.5 = 2 secs, 1 = 1 sec etc..

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
					samples[i] += (float)(GetBackgroundNoise(i, dropFreq) * 0.1);
				}

				if (addDrop)
				{
					samples[i] += (float)(amplitude * Math.Sin(((Math.PI * 2 * dropFreq) / sampleRate) * i) * 4);

					samples[i] *= currentDropDuration / totalDropDuration;

					samples[i] *= 1 - currentDropDuration / totalDropDuration;

					addedDrop = false;
					currentDropDuration++;

					if (currentDropDuration > totalDropDuration)
					{
						addedDrop = true;
						addDrop = false;
						currentDropDuration = 0;
					}
				}

				samples[i] *= 0.2f;
			}

			return samples;
		}



		private double GetBackgroundNoise(int i, int freq)
		{
			return (Math.Sin(((Math.PI * 2 * freq) / (sampleRate + r.Next(-(int)(sampleRate * 0.01), (int)(sampleRate * 0.01)))) * i) * 0.5) + (r.NextDouble() * 0.5);
		}
	}
}
