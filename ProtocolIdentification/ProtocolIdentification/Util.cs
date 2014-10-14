namespace ProtocolIdentification
{
    using ProtocolIdentification.AttributeMeters;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Xml;

    public class Util
    {
        public static List<IAttributeMeter> GetAllAttributeMeters()
        {
            return new List<IAttributeMeter> { 
                new AccumulatedDirectionBytesMeter(), new ActionReactionFirst3ByteHashMeter(), new ByteBitValueMeter(), new ByteFrequencyMeter(), new ByteFrequencyOfFirstPacketBytesMeter(), new BytePairsReocurringCountIn32FirstBytesMeter(), new BytePairsReocurringIn32FirstBytesMeter(), new BytePairsReocurringOffsetsIn32FirstBytesMeter(), new ByteValueOffsetHashOfFirst32BytesInFirst4PacketsMeter(), new DirectionByteFrequencyMeter(), new DirectionPacketLengthDistributionMeter(), new DirectionPacketSizeChange(), new First2OrderedFirst4CharWordsMeter(), new First2OrderedFirstBitPositionsMeter(), new First2OrderedPacketsFirstNByteNibblesMeter(), new First2PacketsFirst16ByteHashCountsMeter(), 
                new First2PacketsFirst3ByteHashAndPacketLengthMeter(), new First2PacketsFirst8ByteHashDirectionCountsMeter(), new First2PacketsPerDirectionFirst5BytesDifferencesMeter(), new First4DirectionFirstNByteNibblesMeter(), new First4OrderedDirectionFirstNByteNibblesMeter(), new First4OrderedDirectionInterPacketDelayMeter(), new First4OrderedDirectionPacketSizeMeter(), new First4PacketsByteFrequencyMeter(), new First4PacketsByteReoccurringDistanceWithByteHashMeter(), new First4PacketsFirst16BytePairsMeter(), new First4PacketsFirst32BytesEqualityMeter(), new FirstBitPositionsMeter(), new FirstPacketPerDirectionFirstNByteNibblesMeter(), new FirstServerPacketFirstBitPositionsMeter(), new NibblePositionFrequencyMeter(), new NibblePositionPopularityMeter(), 
                new PacketLengthDistributionMeter(), new PacketLengthDistributionMeterFirst3(), new PacketPairLengthPrimesMeter()
             };
        }

        public static int GetDatabaseVersion(string filename)
        {
            XmlDocument document = new XmlDocument();
            document.Load(filename);
            return int.Parse(document.DocumentElement.SelectSingleNode("/protocolModels").Attributes["version"].Value);
        }

        public static IEnumerable<ProtocolModel> GetProtocolModelsFromDatabaseFile(string filename, ICollection<string> activeAttributeMeters)
        {
            XmlDocument iteratorVariable0 = new XmlDocument();
            iteratorVariable0.Load(filename);
            if (iteratorVariable0.DocumentElement.SelectSingleNode("/protocolModels").Attributes["fingerprintLength"].Value != AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH.ToString())
            {
                throw new Exception("Fingerprint length is not correct!");
            }
            IEnumerator enumerator = iteratorVariable0.DocumentElement.SelectNodes("/protocolModels/protocolModel").GetEnumerator();
            while (enumerator.MoveNext())
            {
                XmlNode current = (XmlNode) enumerator.Current;
                string protocolName = current.SelectSingleNode("@name").Value;
                int modelTrainingSessionCount = int.Parse(current.SelectSingleNode("@sessionCount").Value);
                ulong observationCount = ulong.Parse(current.SelectSingleNode("@observationCount").Value);
                SortedList<string, AttributeFingerprintHandler> attributeFingerprintHandlers = new SortedList<string, AttributeFingerprintHandler>();
                List<ushort> defaultPorts = new List<ushort>();
                foreach (XmlNode node in current.SelectNodes("defaultPorts/port"))
                {
                    defaultPorts.Add(ushort.Parse(node.InnerText));
                }
                foreach (XmlNode node2 in current.SelectNodes("attributeFingerprint"))
                {
                    string key = node2.SelectSingleNode("@attributeMeterName").Value;
                    ulong measurementCount = ulong.Parse(node2.SelectSingleNode("@measurementCount").Value);
                    XmlNodeList list = node2.SelectNodes("bin");
                    double[] fingerprintProbabilityDistributionVector = new double[list.Count];
                    for (int j = 0; j < fingerprintProbabilityDistributionVector.Length; j++)
                    {
                        fingerprintProbabilityDistributionVector[j] = double.Parse(list[j].InnerText, CultureInfo.InvariantCulture.NumberFormat);
                    }
                    attributeFingerprintHandlers.Add(key, new AttributeFingerprintHandler(key, fingerprintProbabilityDistributionVector, measurementCount));
                }
                yield return new ProtocolModel(protocolName, attributeFingerprintHandlers, modelTrainingSessionCount, observationCount, defaultPorts, activeAttributeMeters);
            }
        }

    }
}

