namespace ProtocolIdentification
{
    using ProtocolIdentification.AttributeMeters;
    using System;
    using System.Collections.Generic;
    using System.Xml;

    public class ProtocolModel
    {
        private SortedList<string, AttributeFingerprintHandler> attributeFingerprintHandlers;
        private List<ushort> defaultPorts;
        private ulong observationCount;
        private string protocolName;
        private bool sessionIsOpen;
        private int trainingSessionCount;

        public ProtocolModel(string protocolName) : this(protocolName, null)
        {
        }

        public ProtocolModel(string protocolName, ICollection<string> activeAttributeMeterNames)
        {
            this.protocolName = protocolName;
            this.trainingSessionCount = 0;
            this.sessionIsOpen = true;
            this.observationCount = 0L;
            this.defaultPorts = new List<ushort>();
            List<IAttributeMeter> allAttributeMeters = Util.GetAllAttributeMeters();
            this.attributeFingerprintHandlers = new SortedList<string, AttributeFingerprintHandler>();
            foreach (IAttributeMeter meter in allAttributeMeters)
            {
                if ((activeAttributeMeterNames == null) || activeAttributeMeterNames.Contains(meter.AttributeName))
                {
                    this.attributeFingerprintHandlers.Add(meter.AttributeName, new AttributeFingerprintHandler(meter));
                }
            }
            if (activeAttributeMeterNames != null)
            {
                this.TrimAttributeFingerprintHandlers(activeAttributeMeterNames);
            }
        }

        public ProtocolModel(string protocolName, SortedList<string, AttributeFingerprintHandler> attributeFingerprintHandlers, int modelTrainingSessionCount, ulong observationCount, List<ushort> defaultPorts) : this(protocolName)
        {
            this.attributeFingerprintHandlers = attributeFingerprintHandlers;
            this.trainingSessionCount = modelTrainingSessionCount;
            this.observationCount = observationCount;
            this.defaultPorts = defaultPorts;
        }

        public ProtocolModel(string protocolName, SortedList<string, AttributeFingerprintHandler> attributeFingerprintHandlers, int modelTrainingSessionCount, ulong observationCount, List<ushort> defaultPorts, ICollection<string> activeAttributeMeters) : this(protocolName, attributeFingerprintHandlers, modelTrainingSessionCount, observationCount, defaultPorts)
        {
            this.TrimAttributeFingerprintHandlers(activeAttributeMeters);
        }

        public void AddObservation(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection)
        {
            if (packetLength > 0)
            {
                if (!this.sessionIsOpen)
                {
                    throw new Exception("Observations cannot be added to closed session models. Use MergeWith() function instead.");
                }
                foreach (AttributeFingerprintHandler handler in this.attributeFingerprintHandlers.Values)
                {
                    handler.AddObservation(frameData, packetStartIndex, packetLength, packetTimestamp, packetDirection, (int) this.observationCount);
                }
                this.observationCount += (ulong) 1L;
                if (this.trainingSessionCount == 0)
                {
                    this.trainingSessionCount++;
                }
            }
        }

        public void Clear()
        {
            this.sessionIsOpen = true;
            this.trainingSessionCount = 0;
            this.observationCount = 0L;
            foreach (AttributeFingerprintHandler handler in this.attributeFingerprintHandlers.Values)
            {
                handler.AttributeFingerprint.Clear();
            }
        }

        public void Close()
        {
            this.sessionIsOpen = false;
        }

        public AttributeFingerprintHandler GetAttributeFingerprint(string protocolAttributeName)
        {
            if (this.attributeFingerprintHandlers.ContainsKey(protocolAttributeName))
            {
                return this.attributeFingerprintHandlers[protocolAttributeName];
            }
            return AttributeFingerprintHandler.EmptySingletonInstance;
        }

        public double GetAverageKullbackLeiblerDivergenceFrom(ProtocolModel protocolModel)
        {
            double num = 0.0;
            foreach (double num2 in this.GetKullbackLeiblerDivergencesFrom(protocolModel).Values)
            {
                num += num2;
            }
            return (num / ((double) this.attributeFingerprintHandlers.Count));
        }

        public SortedList<string, double> GetKullbackLeiblerDivergencesFrom(ProtocolModel protocolModel)
        {
            SortedList<string, double> list = new SortedList<string, double>(this.attributeFingerprintHandlers.Count);
            foreach (AttributeFingerprintHandler handler in this.attributeFingerprintHandlers.Values)
            {
                list.Add(handler.AttributeMeterName, handler.GetKullbackLeiblerDivergenceFrom(protocolModel.GetAttributeFingerprint(handler.AttributeMeterName)));
            }
            return list;
        }

        public XmlElement GetXml(XmlDocument xmlDoc)
        {
            XmlElement element = xmlDoc.CreateElement("protocolModel");
            element.SetAttribute("name", this.protocolName);
            element.SetAttribute("sessionCount", this.trainingSessionCount.ToString());
            element.SetAttribute("observationCount", this.observationCount.ToString());
            XmlElement newChild = xmlDoc.CreateElement("defaultPorts");
            foreach (uint num in this.defaultPorts)
            {
                XmlElement element3 = xmlDoc.CreateElement("port");
                element3.InnerText = num.ToString();
                newChild.AppendChild(element3);
            }
            element.AppendChild(newChild);
            foreach (AttributeFingerprintHandler handler in this.attributeFingerprintHandlers.Values)
            {
                element.AppendChild(handler.GetXml(xmlDoc));
            }
            return element;
        }

        public ProtocolModel MergeWith(ProtocolModel otherSessionModel)
        {
            SortedList<string, AttributeFingerprintHandler> attributeFingerprintHandlers = new SortedList<string, AttributeFingerprintHandler>(this.attributeFingerprintHandlers.Count);
            foreach (string str in this.attributeFingerprintHandlers.Keys)
            {
                attributeFingerprintHandlers.Add(str, this.attributeFingerprintHandlers[str].MergeWith(otherSessionModel.attributeFingerprintHandlers[str]));
            }
            List<ushort> defaultPorts = new List<ushort>(this.defaultPorts);
            defaultPorts.AddRange(otherSessionModel.defaultPorts);
            ProtocolModel model = new ProtocolModel(this.protocolName, attributeFingerprintHandlers, this.trainingSessionCount + otherSessionModel.TrainingSessionCount, this.observationCount + otherSessionModel.ObservationCount, defaultPorts);
            model.Close();
            return model;
        }

        public override string ToString()
        {
            return this.protocolName;
        }

        private void TrimAttributeFingerprintHandlers(ICollection<string> activeAttributeMeterNames)
        {
            List<string> list = new List<string>();
            foreach (string str in this.attributeFingerprintHandlers.Keys)
            {
                if (!activeAttributeMeterNames.Contains(str))
                {
                    list.Add(str);
                }
            }
            foreach (string str2 in list)
            {
                this.attributeFingerprintHandlers.Remove(str2);
            }
        }

        public SortedList<string, AttributeFingerprintHandler> AttributeFingerprintHandlers
        {
            get
            {
                return this.attributeFingerprintHandlers;
            }
        }

        public List<ushort> DefaultPorts
        {
            get
            {
                return this.defaultPorts;
            }
            set
            {
                this.defaultPorts = value;
            }
        }

        public ulong ObservationCount
        {
            get
            {
                return this.observationCount;
            }
            set
            {
                this.observationCount = value;
            }
        }

        public string ProtocolName
        {
            get
            {
                return this.protocolName;
            }
            set
            {
                this.protocolName = value;
            }
        }

        public bool SessionIsOpen
        {
            get
            {
                return this.sessionIsOpen;
            }
        }

        public int TrainingSessionCount
        {
            get
            {
                return this.trainingSessionCount;
            }
        }
    }
}

