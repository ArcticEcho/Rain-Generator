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
				}

				// Add sound of distant rain.
				samples[i] += (float)(GetBackgroundNoise(i, dropFreq) * 0.1);

				if (addDrop)
				{
					// Add drop.
					samples[i] += (float)(amplitude * Math.Sin(((Math.PI * 2 * dropFreq) / sampleRate) * i) * 4);

					// Soften drop.
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
			}

			return samples;
		}



		private double GetBackgroundNoise(int i, int freq)
		{
			return (Math.Sin(((Math.PI * 2 * freq) / (sampleRate + r.Next(-(int)(sampleRate * 0.01), (int)(sampleRate * 0.01)))) * i) * 0.5) + (r.NextDouble() * 0.5);
		}
	}
}
