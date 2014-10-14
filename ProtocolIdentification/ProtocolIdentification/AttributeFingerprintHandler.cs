namespace ProtocolIdentification
{
    using ProtocolIdentification.AttributeMeters;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Threading;
    using System.Xml;

    public class AttributeFingerprintHandler
    {
        private IAttributeMeter attributeMeter;
        private string attributeMeterName;
        private static AttributeFingerprintHandler emptySingletonInstance;
        private Fingerprint fingerprint;
        private TimeSpan observationComputingTime;
        private Stopwatch observationStopWatch;

        public AttributeFingerprintHandler(IAttributeMeter attributeMeter) : this(attributeMeter.AttributeName)
        {
            this.attributeMeter = attributeMeter;
            this.fingerprint = new Fingerprint();
        }

        private AttributeFingerprintHandler(string attributeMeterName)
        {
            this.attributeMeterName = attributeMeterName;
            this.observationStopWatch = new Stopwatch();
            this.observationComputingTime = new TimeSpan();
        }

        public AttributeFingerprintHandler(string attributeMeterName, double[] fingerprintProbabilityDistributionVector, ulong measurementCount) : this(attributeMeterName)
        {
            this.attributeMeter = null;
            if (measurementCount > 0x3fffffffffffffffL)
            {
                measurementCount /= (ulong) 4L;
            }
            this.fingerprint = new Fingerprint(fingerprintProbabilityDistributionVector, measurementCount);
        }

        public AttributeFingerprintHandler(string attributeMeterName, double[] fingerprintProbabilityDistributionVector, ulong measurementCount, TimeSpan observationComputingTime) : this(attributeMeterName, fingerprintProbabilityDistributionVector, measurementCount)
        {
            this.observationComputingTime = this.observationComputingTime.Add(observationComputingTime);
        }

        public void AddObservation(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            this.observationStopWatch.Start();
            foreach (int num in this.attributeMeter.GetMeasurements(frameData, packetStartIndex, packetLength, packetTimestamp, packetDirection, packetOrderNumberInSession))
            {
                this.fingerprint.IncrementFingerprintCounterAtIndex(num);
            }
            this.observationStopWatch.Stop();
        }

        public double GetKullbackLeiblerDivergenceFrom(AttributeFingerprintHandler protocolModelAttribute)
        {
            double num = 0.0;
            if (protocolModelAttribute.AttributeFingerprint != null)
            {
                for (int i = 0; i < Fingerprint.FINGERPRINT_LENGTH; i++)
                {
                    num += this.AttributeFingerprint.ObservationProbabilityVector[i] * (this.AttributeFingerprint.ObservationLogarithmicProbabilityVector[i] - protocolModelAttribute.AttributeFingerprint.ModelLogarithmicProbabilityVector[i]);
                }
            }
            return num;
        }

        public double GetTotalEntropyForMeasurements(IEnumerable<int> measurements)
        {
            double num = 0.0;
            foreach (int num2 in measurements)
            {
                num -= this.fingerprint.ModelLogarithmicProbabilityVector[num2];
            }
            return num;
        }

        public XmlElement GetXml(XmlDocument xmlDoc)
        {
            XmlElement element = xmlDoc.CreateElement("attributeFingerprint");
            element.SetAttribute("attributeMeterName", this.attributeMeterName);
            element.SetAttribute("measurementCount", this.fingerprint.MeasurementCount.ToString());
            for (int i = 0; i < this.fingerprint.ProbabilityVector.Length; i++)
            {
                XmlElement newChild = xmlDoc.CreateElement("bin");
                newChild.SetAttribute("i", i.ToString());
                newChild.InnerText = this.fingerprint.ProbabilityVector[i].ToString("G", CultureInfo.InvariantCulture);
                element.AppendChild(newChild);
            }
            return element;
        }

        public AttributeFingerprintHandler MergeWith(AttributeFingerprintHandler otherFingerprint)
        {
            if (otherFingerprint.AttributeMeterName != this.AttributeMeterName)
            {
                throw new Exception("Fingerprints must be of the same attribute in order to be merged!");
            }
            return new AttributeFingerprintHandler(this.attributeMeterName, this.fingerprint.MergeWith(otherFingerprint.fingerprint).ProbabilityVector, this.fingerprint.MeasurementCount + otherFingerprint.fingerprint.MeasurementCount, this.observationComputingTime.Add(otherFingerprint.ObservationComputingTime));
        }

        public Fingerprint AttributeFingerprint
        {
            get
            {
                return this.fingerprint;
            }
        }

        public IAttributeMeter AttributeMeter
        {
            get
            {
                return this.attributeMeter;
            }
        }

        public string AttributeMeterName
        {
            get
            {
                return this.attributeMeterName;
            }
        }

        public static AttributeFingerprintHandler EmptySingletonInstance
        {
            get
            {
                if (emptySingletonInstance == null)
                {
                    emptySingletonInstance = new AttributeFingerprintHandler("EMPTY_SINGLETON_INSTANCE");
                }
                return emptySingletonInstance;
            }
        }

        public TimeSpan ObservationComputingTime
        {
            get
            {
                return this.observationComputingTime.Add(this.observationStopWatch.Elapsed);
            }
        }

        public class Fingerprint
        {
            public const ushort FINGERPRINT_BITS = 8;
            private long[] fingerprintCounterVector;
            private double[] fingerprintProbabilityVector;
            private bool fingerprintProbabilityVectorIsIpdated;
            private ulong measurementCount;
            private bool measurementCountIsIpdated;
            private double[] modelLogarithmicProbabilityVector;
            private bool modelLogarithmicProbabilityVectorIsIpdated;
            private double[] modelProbabilityVector;
            private bool modelProbabilityVectorIsIpdated;
            private double[] observationLogarithmicProbabilityVector;
            private bool observationLogarithmicProbabilityVectorIsIpdated;
            private double[] observationProbabilityVector;
            private bool observationProbabilityVectorIsIpdated;
            private const ulong PRESERVATION = 0x2fL;

            internal Fingerprint()
            {
                this.FingerprintCounterVectorChanged();
                this.measurementCount = 0L;
                this.fingerprintProbabilityVector = new double[FINGERPRINT_LENGTH];
                this.fingerprintCounterVector = new long[FINGERPRINT_LENGTH];
                this.modelProbabilityVector = null;
                this.observationProbabilityVector = null;
                this.modelLogarithmicProbabilityVector = null;
                this.observationLogarithmicProbabilityVector = null;
            }

            internal Fingerprint(long[] fingerprintCounterVector) : this()
            {
                if (fingerprintCounterVector.Length != FINGERPRINT_LENGTH)
                {
                    throw new Exception("Wrong length of fingerprintRawData");
                }
                this.fingerprintCounterVector = fingerprintCounterVector;
            }

            internal Fingerprint(double[] fingerprintProbabilityDistributionVector, ulong measurementCount) : this()
            {
                if (fingerprintProbabilityDistributionVector.Length != FINGERPRINT_LENGTH)
                {
                    throw new Exception("Wrong length of fingerprintProbabilityData");
                }
                if (measurementCount > 0x3fffffffffffffffL)
                {
                    measurementCount = 0x3fffffffffffffffL;
                }
                fingerprintProbabilityDistributionVector.CopyTo(this.fingerprintProbabilityVector, 0);
                for (int i = 0; i < FINGERPRINT_LENGTH; i++)
                {
                    this.fingerprintCounterVector[i] = (long) (fingerprintProbabilityDistributionVector[i] * measurementCount);
                }
                this.fingerprintProbabilityVectorIsIpdated = true;
            }

            internal void Clear()
            {
                this.FingerprintCounterVectorChanged();
                this.fingerprintProbabilityVector = new double[FINGERPRINT_LENGTH];
                this.fingerprintCounterVector = new long[FINGERPRINT_LENGTH];
            }

            private void FingerprintCounterVectorChanged()
            {
                this.measurementCountIsIpdated = false;
                this.fingerprintProbabilityVectorIsIpdated = false;
                this.modelProbabilityVectorIsIpdated = false;
                this.observationProbabilityVectorIsIpdated = false;
                this.modelLogarithmicProbabilityVectorIsIpdated = false;
                this.observationLogarithmicProbabilityVectorIsIpdated = false;
            }

            internal void IncrementFingerprintCounterAtIndex(int index)
            {
                this.FingerprintCounterVectorChanged();
                Interlocked.Increment(ref this.fingerprintCounterVector[index]);
                if (this.fingerprintCounterVector[index] >= (ulong.MaxValue / ((ulong) FINGERPRINT_LENGTH)))
                {
                    lock (this.fingerprintCounterVector.SyncRoot)
                    {
                        for (int i = 0; i < FINGERPRINT_LENGTH; i++)
                        {
                            this.fingerprintCounterVector[i] /= 2L;
                        }
                    }
                }
            }

            internal AttributeFingerprintHandler.Fingerprint MergeWith(AttributeFingerprintHandler.Fingerprint otherFingerprint)
            {
                if (this.fingerprintCounterVector.Length != otherFingerprint.fingerprintCounterVector.Length)
                {
                    throw new Exception("Fingerprint lengths do not match!");
                }
                long[] fingerprintCounterVector = (long[]) this.fingerprintCounterVector.Clone();
                ulong measurementCount = otherFingerprint.MeasurementCount;
                for (int i = 0; i < this.fingerprintCounterVector.Length; i++)
                {
                    fingerprintCounterVector[i] += (long) (measurementCount * otherFingerprint.ProbabilityVector[i]);
                }
                return new AttributeFingerprintHandler.Fingerprint(fingerprintCounterVector);
            }

            private void UpdateFingerprintProbabilityVector()
            {
                ulong measurementCount = this.MeasurementCount;
                lock (this.fingerprintProbabilityVector.SyncRoot)
                {
                    lock (this.fingerprintCounterVector.SyncRoot)
                    {
                        this.fingerprintProbabilityVectorIsIpdated = false;
                        if (measurementCount == 0L)
                        {
                            for (int i = 0; i < FINGERPRINT_LENGTH; i++)
                            {
                                this.fingerprintProbabilityVector[i] = 1.0 / ((double) FINGERPRINT_LENGTH);
                            }
                        }
                        else
                        {
                            double num3 = 1.0 / ((double) measurementCount);
                            for (int j = 0; j < FINGERPRINT_LENGTH; j++)
                            {
                                this.fingerprintProbabilityVector[j] = num3 * this.fingerprintCounterVector[j];
                            }
                        }
                        this.fingerprintProbabilityVectorIsIpdated = true;
                    }
                }
            }

            private void UpdateModelLogarithmicProbabilityVector()
            {
                this.modelLogarithmicProbabilityVectorIsIpdated = false;
                if (this.modelLogarithmicProbabilityVector == null)
                {
                    this.modelLogarithmicProbabilityVector = new double[FINGERPRINT_LENGTH];
                }
                for (int i = 0; i < FINGERPRINT_LENGTH; i++)
                {
                    this.modelLogarithmicProbabilityVector[i] = Math.Log(this.ModelProbabilityVector[i], 2.0);
                }
                this.modelLogarithmicProbabilityVectorIsIpdated = true;
            }

            private void UpdateModelProbabilityVector()
            {
                this.modelProbabilityVectorIsIpdated = false;
                double modelMultiplicator = this.ModelMultiplicator;
                double modelIncrement = this.ModelIncrement;
                if (this.modelProbabilityVector == null)
                {
                    this.modelProbabilityVector = new double[FINGERPRINT_LENGTH];
                }
                for (int i = 0; i < FINGERPRINT_LENGTH; i++)
                {
                    this.modelProbabilityVector[i] = (this.ProbabilityVector[i] * modelMultiplicator) + modelIncrement;
                }
                this.modelProbabilityVectorIsIpdated = true;
            }

            private void UpdateObservationLogarithmicProbabilityVector()
            {
                this.observationLogarithmicProbabilityVectorIsIpdated = false;
                if (this.observationLogarithmicProbabilityVector == null)
                {
                    this.observationLogarithmicProbabilityVector = new double[FINGERPRINT_LENGTH];
                }
                for (int i = 0; i < FINGERPRINT_LENGTH; i++)
                {
                    this.observationLogarithmicProbabilityVector[i] = Math.Log(this.ObservationProbabilityVector[i], 2.0);
                }
                this.observationLogarithmicProbabilityVectorIsIpdated = true;
            }

            private void UpdateObservationProbabilityVector()
            {
                this.observationProbabilityVectorIsIpdated = false;
                double observationMultiplicator = this.ObservationMultiplicator;
                double observationIncrement = this.ObservationIncrement;
                if (this.observationProbabilityVector == null)
                {
                    this.observationProbabilityVector = new double[FINGERPRINT_LENGTH];
                }
                for (int i = 0; i < FINGERPRINT_LENGTH; i++)
                {
                    this.observationProbabilityVector[i] = (this.ProbabilityVector[i] * observationMultiplicator) + observationIncrement;
                }
                this.observationProbabilityVectorIsIpdated = true;
            }

            public static ushort FINGERPRINT_LENGTH
            {
                get
                {
                    return 0x100;
                }
            }

            internal ulong MeasurementCount
            {
                get
                {
                    if (!this.measurementCountIsIpdated)
                    {
                        this.measurementCount = 0L;
                        foreach (long num in this.fingerprintCounterVector)
                        {
                            this.measurementCount += (ulong) num;
                        }
                        this.measurementCountIsIpdated = true;
                    }
                    return this.measurementCount;
                }
            }

            private double ModelIncrement
            {
                get
                {
                    return (1.0 / (((ulong) (0x30L * FINGERPRINT_LENGTH)) + this.MeasurementCount));
                }
            }

            internal double[] ModelLogarithmicProbabilityVector
            {
                get
                {
                    if (!this.modelLogarithmicProbabilityVectorIsIpdated)
                    {
                        this.UpdateModelLogarithmicProbabilityVector();
                    }
                    return this.modelLogarithmicProbabilityVector;
                }
            }

            private double ModelMultiplicator
            {
                get
                {
                    return (((double) (this.MeasurementCount + ((ulong) (0x2fL * FINGERPRINT_LENGTH)))) / (((ulong) (0x30L * FINGERPRINT_LENGTH)) + this.MeasurementCount));
                }
            }

            internal double[] ModelProbabilityVector
            {
                get
                {
                    if (!this.modelProbabilityVectorIsIpdated)
                    {
                        this.UpdateModelProbabilityVector();
                    }
                    return this.modelProbabilityVector;
                }
            }

            private double ObservationIncrement
            {
                get
                {
                    return 0.0;
                }
            }

            internal double[] ObservationLogarithmicProbabilityVector
            {
                get
                {
                    if (!this.observationLogarithmicProbabilityVectorIsIpdated)
                    {
                        this.UpdateObservationLogarithmicProbabilityVector();
                    }
                    return this.observationLogarithmicProbabilityVector;
                }
            }

            private double ObservationMultiplicator
            {
                get
                {
                    return 1.0;
                }
            }

            internal double[] ObservationProbabilityVector
            {
                get
                {
                    if (!this.observationProbabilityVectorIsIpdated)
                    {
                        this.UpdateObservationProbabilityVector();
                    }
                    return this.observationProbabilityVector;
                }
            }

            internal double[] ProbabilityVector
            {
                get
                {
                    if (!this.fingerprintProbabilityVectorIsIpdated)
                    {
                        this.UpdateFingerprintProbabilityVector();
                    }
                    return this.fingerprintProbabilityVector;
                }
            }
        }

        public enum PacketDirection
        {
            Unknown,
            ClientToServer,
            ServerToClient
        }
    }
}

