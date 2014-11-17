using System;



namespace RainGenerator
{
	public class Generator32Bit
	{
		private readonly Random r = new Random();
		private readonly float sampleRate;



		/// <summary>
		/// Creates 32-bit rain samples.
		/// </summary>
		/// <param name="sampleRate">The sample rate of the generated rain.</param>
		public Generator32Bit(float sampleRate)
		{
			if (sampleRate < 1) { throw new ArgumentOutOfRangeException("sampleRate", "'sampleRate' must be more than 0."); }

			this.sampleRate = sampleRate;
		}



		/// <summary>
		/// Generates the sound of rain.
		/// </summary>
		/// <param name="duration">The total duration of the rain.</param>
		/// <param name="rainIntensity">The rain intensiy percentage (1 = 100%, 0.5 = 50%, etc.).</param>
		/// <param name="backgroundIntensity">The background rain intensity.</param>
		/// <param name="minDropFreq">The minimum rain drop frequency.</param>
		/// <param name="maxDropFreq">The maximum rain drop frequency.</param>
		/// <param name="maxOscillationsPerDrop">The exclusive maximum number of samples per drop.</param>
		/// <returns>32-bit rain samples at the specified sample rate.</returns>
		public float[] Generate(TimeSpan duration, double rainIntensity = 0.1, double backgroundIntensity = 0.35f, int minDropFreq = 3500, int maxDropFreq = 120001, double maxOscillationsPerDrop = 5)
		{
			if (duration == default(TimeSpan) || duration.TotalSeconds < 1) { throw new ArgumentOutOfRangeException("duration"); }
			if (rainIntensity < 0 || rainIntensity > 1) { throw new ArgumentOutOfRangeException("rainIntensity", "Must be more than 0 (exclusive) and less than 1 (inclusive)."); }
			if (minDropFreq < 1 || minDropFreq > maxDropFreq) { throw new ArgumentOutOfRangeException("minDropFreq", "Must be 1 or higher and less than 'maxDropFreq'."); }
			if (maxDropFreq < 1 || maxDropFreq < minDropFreq) { throw new ArgumentOutOfRangeException("maxDropFreq", "Must be 1 or higher and more than 'minDropFreq'."); }
			if (maxOscillationsPerDrop < 2) { throw new ArgumentOutOfRangeException("maxOscillationsPerDrop", "Must be 2 or higher."); }

			var sampleCount = (int)(duration.TotalSeconds * sampleRate); 
			var combinedAmp = 1 + backgroundIntensity;
			var samples = new float[sampleCount];
			var addDrop = false;
			var dropAdded = true;
			var dropFreq = 0;
			var samplesPerDrop = 0f;
			var currentDropDuration = 0;
			var amplitude = 0f;

			for (var i = 0; i < sampleCount; i++)
			{
				if (dropAdded)
				{
					// Pick the drop's frequency.
					dropFreq = r.Next(minDropFreq, maxDropFreq);   

					// How many samples each oscillation of the drop will be for the specified frequency (i.e., sample count of a sinlge oscillation).
					var samplesPerOscillation = sampleRate / dropFreq;

					// Calc the drop's total sample count.
					samplesPerDrop = r.Next(1, (int)maxOscillationsPerDrop) * samplesPerOscillation;

					// Then calc the max drop count per sec.
					var maxDropsPerSec = sampleRate / samplesPerDrop;

					// Finally we can now calc rain intensity.
					addDrop = r.NextDouble() < (maxDropsPerSec * rainIntensity) / maxDropsPerSec; 
					
					// Choose maximum drop amplitude ("loudness").
					amplitude = (float)r.NextDouble();
					amplitude /= (float)combinedAmp;                                       
				}

				// Add sound of distant rain.
				samples[i] += (float)(GetBackgroundNoise(i, dropFreq) * (backgroundIntensity / combinedAmp));

				if (addDrop)
				{
					// Add drop.
					samples[i] += (float)(amplitude * Math.Sin(((Math.PI * 2 * dropFreq) / sampleRate) * i) * 4);

					// Soften drop.
					samples[i] *= currentDropDuration / samplesPerDrop;
					samples[i] *= 1 - currentDropDuration / samplesPerDrop;

					dropAdded = false;
					currentDropDuration++;

					if (currentDropDuration > samplesPerDrop)
					{
						dropAdded = true;
						addDrop = false;
						currentDropDuration = 0;
					}
				}
			}

			return LinkwitzRileyLowPass(LinkwitzRileyHighPass(samples, 250), 16000);
		}



		private float GetBackgroundNoise(int i, int freq)
		{
			return (float)((Math.Sin(((Math.PI * 2 * freq) / (sampleRate + r.Next(-(int)(sampleRate * 0.01), (int)(sampleRate * 0.01)))) * i) * 0.5) + (r.NextDouble() * 0.5));
		}

		private float[] LinkwitzRileyLowPass(float[] samples, float cutoff)
		{
			if (cutoff < 1 || cutoff >= sampleRate / 2) { throw new ArgumentOutOfRangeException("cutoff", "The cutoff frequency must be between 0 and 'sampleRate' / 2."); }
			if (sampleRate < 1) { throw new ArgumentOutOfRangeException("sampleRate", "'sampleRate' must be more than 0."); }
			if (samples == null || samples.Length == 0) { throw new ArgumentException("'samples' can not be null or empty.", "samples"); }

			var newSamples = new float[samples.Length];
			var wc = (float)(2 * Math.PI * cutoff);
			var wc2 = wc * wc;
			var wc3 = wc2 * wc;
			var wc4 = wc2 * wc2;
			var k = (float)(wc / Math.Tan(Math.PI * cutoff / sampleRate));
			var k2 = k * k;
			var k3 = k2 * k;
			var k4 = k2 * k2;
			var sqrt2 = (float)Math.Sqrt(2);
			var sqTmp1 = sqrt2 * wc3 * k;
			var sqTmp2 = sqrt2 * wc * k3;
			var aTmp = 4 * wc2 * k2 + 2 * sqTmp1 + k4 + 2 * sqTmp2 + wc4;

			var b1 = (4 * (wc4 + sqTmp1 - k4 - sqTmp2)) / aTmp;
			var b2 = (6 * wc4 - 8 * wc2 * k2 + 6 * k4) / aTmp;
			var b3 = (4 * (wc4 - sqTmp1 + sqTmp2 - k4)) / aTmp;
			var b4 = (k4 - 2 * sqTmp1 + wc4 - 2 * sqTmp2 + 4 * wc2 * k2) / aTmp;

			var a0 = wc4 / aTmp;
			var a1 = 4 * wc4 / aTmp;
			var a2 = 6 * wc4 / aTmp;
			var a3 = a1;
			var a4 = a0;

			float ym1 = 0.0f, ym2 = 0.0f, ym3 = 0.0f, ym4 = 0.0f, xm1 = 0.0f, xm2 = 0.0f, xm3 = 0.0f, xm4 = 0.0f, tempy;

			for (var i = 0; i < samples.Length; i++)
			{
				var tempx = samples[i];

				tempy = a0 * tempx + a1 * xm1 + a2 * xm2 + a3 * xm3 + a4 * xm4 - b1 * ym1 - b2 * ym2 - b3 * ym3 - b4 * ym4;
				xm4 = xm3;
				xm3 = xm2;
				xm2 = xm1;
				xm1 = tempx;
				ym4 = ym3;
				ym3 = ym2;
				ym2 = ym1;
				ym1 = tempy;

				newSamples[i] = tempy;
			}

			return newSamples;
		}

		private float[] LinkwitzRileyHighPass(float[] samples, float cutoff)
		{
			if (cutoff < 1 || cutoff >= sampleRate / 2) { throw new ArgumentOutOfRangeException("cutoff", "The cutoff frequency must be between 0 and 'sampleRate' / 2."); }
			if (sampleRate < 1) { throw new ArgumentOutOfRangeException("sampleRate", "'sampleRate' must be more than 0."); }
			if (samples == null || samples.Length == 0) { throw new ArgumentException("'samples' can not be null or empty.", "samples"); }

			var newSamples = new float[samples.Length];
			var wc = (float)(2 * Math.PI * cutoff);
			var wc2 = wc * wc;
			var wc3 = wc2 * wc;
			var wc4 = wc2 * wc2;
			var k = (float)(wc / Math.Tan(Math.PI * cutoff / sampleRate));
			var k2 = k * k;
			var k3 = k2 * k;
			var k4 = k2 * k2;
			var sqrt2 = (float)Math.Sqrt(2);
			var sqTmp1 = sqrt2 * wc3 * k;
			var sqTmp2 = sqrt2 * wc * k3;
			var aTmp = 4 * wc2 * k2 + 2 * sqTmp1 + k4 + 2 * sqTmp2 + wc4;

			var b1 = (4 * (wc4 + sqTmp1 - k4 - sqTmp2)) / aTmp;
			var b2 = (6 * wc4 - 8 * wc2 * k2 + 6 * k4) / aTmp;
			var b3 = (4 * (wc4 - sqTmp1 + sqTmp2 - k4)) / aTmp;
			var b4 = (k4 - 2 * sqTmp1 + wc4 - 2 * sqTmp2 + 4 * wc2 * k2) / aTmp;

			var a0 = k4 / aTmp;
			var a1 = -4 * k4 / aTmp;
			var a2 = 6 * k4 / aTmp;
			var a3 = a1;
			var a4 = a0;

			float ym1 = 0.0f, ym2 = 0.0f, ym3 = 0.0f, ym4 = 0.0f, xm1 = 0.0f, xm2 = 0.0f, xm3 = 0.0f, xm4 = 0.0f, tempy;

			for (var i = 0; i < samples.Length; i++)
			{
				var tempx = samples[i];

				tempy = a0 * tempx + a1 * xm1 + a2 * xm2 + a3 * xm3 + a4 * xm4 - b1 * ym1 - b2 * ym2 - b3 * ym3 - b4 * ym4;
				xm4 = xm3;
				xm3 = xm2;
				xm2 = xm1;
				xm1 = tempx;
				ym4 = ym3;
				ym3 = ym2;
				ym2 = ym1;
				ym1 = tempy;

				newSamples[i] = tempy;
			}

			return newSamples;
		}
	}
}
